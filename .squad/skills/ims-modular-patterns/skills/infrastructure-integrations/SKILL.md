---
name: infrastructure-integrations
description: Implementação de integrações com APIs externas, cache Redis e mapeamento de models. Use ao criar novas integrations com HttpClient, implementar IntegrationCachingBase, definir CacheKeys, mapear models externos com explicit operator ou configurar DI da camada de Infrastructure.
---

# 07. Infrastructure & Integrations

## 📋 Visão Geral

O projeto **Infrastructure** implementa as integrações com **APIs externas**, gerencia **cache Redis**, e faz **mapeamento** de models entre a camada externa e o domínio interno.

**Localização**: [app/src/Infrastructure/](../app/src/Infrastructure/)

**Responsabilidades**:
- HTTP clients para APIs externas
- Cache management (Redis)
- Model mapping (External → Domain)
- Error handling e timeouts
- Retry policies (futuro)

---

## 🏗️ Estrutura por Domínio

```
Infrastructure/{Domain}/
├── Integration/                 # HTTP client implementations
│   └── {Domain}Integration.cs
└── Model/                       # External API contracts
    ├── {Entity}IntegrationModel.cs
    └── {Entity}IntegrationRequest.cs
```

### Domínios Implementados (Exemplos)

- **Order/** - APIs de pedidos e ordens
- **Product/** - API de catálogo de produtos
- **Payment/** - API de processamento de pagamentos
- **User/** - API de gerenciamento de usuários
- **Inventory/** - API de controle de estoque
- **Generics/** - Base classes e helpers

---

## 🎯 Integration Implementation Pattern

### Base Class: IntegrationCachingBase

**Arquivo**: [Generics/IntegrationCachingBase.cs](../app/src/Infrastructure/Generics/IntegrationCachingBase.cs)

```csharp
public abstract class IntegrationCachingBase
{
    private readonly IReadWriteCacheCommands _cache;
    private readonly ILogger _logger;
    
    protected IntegrationCachingBase(
        IReadWriteCacheCommands cache,
        ILogger logger)
    {
        _cache = cache;
        _logger = logger;
    }
    
    protected async Task<(bool found, T? value)> GetCache<T>(string key)
    {
        try
        {
            var cached = await _cache.StringGetAsync(key);
            
            if (cached.HasValue)
            {
                var deserialized = JsonSerializer.Deserialize<T>(
                    cached,
                    SerializerOptionsMapping.CustomJsonSerializerOptions);
                
                return (true, deserialized);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache read failed for key: {Key}", key);
        }
        
        return (false, default);
    }
    
    protected Task SaveCache<T>(
        string key,
        T value,
        TimeSpan? ttl = null)
    {
        _ = Task.Run(async () => // Fire-and-forget
        {
            try
            {
                var json = JsonSerializer.Serialize(
                    value,
                    SerializerOptionsMapping.CustomJsonSerializerOptions);
                
                await _cache.StringSetAsync(
                    key,
                    json,
                    ttl ?? TimeSpan.FromMinutes(10)); // Default 10min
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache write failed for key: {Key}", key);
            }
        });
        
        return Task.CompletedTask;
    }
}
```

**Características**:
- **Graceful degradation**: Falhas de cache não quebram o fluxo
- **Fire-and-forget write**: Cache save não bloqueia response
- **Default TTL**: 10 minutos
- **Logging**: Warnings para debug

### Integration Implementation

**Exemplo Completo**: OrderIntegration

```csharp
public class OrderIntegration(
    IHttpClientFactory httpClientFactory,
    ILogger<OrderIntegration> logger,
    EnvironmentConfiguration environmentConfiguration,
    IReadWriteCacheCommands readWriteCacheCommands,
    IHttpContextAccessor? httpContextAccessor = null)
    : IntegrationCachingBase(readWriteCacheCommands, logger),
      IOrderIntegration
{
    public async Task<Result<Order[]>> GetOrders(
        string userId)
    {
        // 1. Check cache
        var cacheKey = string.Format(
            CacheKeys.UserOrdersKey,
            userId);
        
        var cache = await GetCache<Order[]>(cacheKey);
        if (cache.found)
            return cache.value!;
        
        // 2. Build HTTP request
        const string path = "/v1/orders";
        var queryParameter = $"?userId={userId}";
        
        var httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(60);
        
        // 3. Local debug support (pass-through auth)
        if (environmentConfiguration.ContextEnvironment == "Local")
        {
            httpClient.DefaultRequestHeaders.Add(
                "Authorization",
                httpContextAccessor?.HttpContext?.Request
                    .Headers.Authorization.ToString());
        }
        
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{environmentConfiguration.UrlOrdersApi}{path}{queryParameter}");
        
        // 4. Send request with error handling
        try
        {
            var httpResponseMessage = await httpClient.SendAsync(request);
            
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                logger.LogError(
                    "API returned {StatusCode} for user {UserId}",
                    httpResponseMessage.StatusCode,
                    userId);
                
                return OrderErrorsMapping.GetError(
                    OrderErrorsCode.GetUserOrdersError);
            }
            
            // 5. Deserialize with custom options
            var content = await httpResponseMessage.Content.ReadAsStringAsync();
            var response = JsonSerializer.Deserialize<OrderIntegrationModel[]>(
                content,
                SerializerOptionsMapping.CustomJsonSerializerOptions);
            
            if (response == null)
            {
                logger.LogError("Failed to deserialize response for user {UserId}", userId);
                return OrderErrorsMapping.GetError(
                    OrderErrorsCode.GetUserOrdersError);
            }
            
            // 6. Map to domain models
            var domainModels = response
                .Select(x => (Order)x)
                .ToArray();
            
            // 7. Save to cache
            _ = SaveCache(cacheKey, domainModels);
            
            return domainModels;
        }
        catch (TaskCanceledException ex)
        {
            var error = OrderErrorsMapping.GetError(
                OrderErrorsCode.GetUserOrdersTimeout);
            
            logger.LogError(
                error.ToEventId<OrderErrorsCode>(),
                ex,
                "Timeout getting orders for user {UserId}",
                userId);
            
            return error;
        }
        catch (HttpRequestException ex)
        {
            var error = OrderErrorsMapping.GetError(
                OrderErrorsCode.GetUserOrdersError);
            
            logger.LogError(
                error.ToEventId<OrderErrorsCode>(),
                ex,
                "HTTP error getting orders for user {UserId}",
                userId);
            
            return error;
        }
    }
}
```

---

## 🗺️ Model Mapping

### Explicit Operator Pattern

**Integration Model** (Infrastructure):

```csharp
public record OrderIntegrationModel
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; init; }
    
    [JsonPropertyName("totalValue")]
    public decimal TotalValue { get; init; }
    
    [JsonPropertyName("orderDate")]
    public DateTime OrderDate { get; init; }
    
    [JsonPropertyName("status")]
    public string Status { get; init; }
    
    // Explicit operator para domain model
    public static explicit operator Order(
        OrderIntegrationModel source)
        => new()
        {
            OrderId = source.OrderId,
            TotalValue = source.TotalValue,
            OrderDate = source.OrderDate,
            Status = source.Status,
            // Transformações adicionais se necessário
        };
}
```

**Domain Model** (Core):

```csharp
public record Order
{
    public string OrderId { get; init; }
    public decimal TotalValue { get; init; }
    public DateTime OrderDate { get; init; }
    public string Status { get; init; }
}
```

**Uso**:

```csharp
var integrationModel = // ... deserialize from API
var domainModel = (Order)integrationModel;
```

---

## 🔑 Cache Keys

**Arquivo**: [Helpers/CacheKeys.cs](../app/src/Infrastructure/Helpers/CacheKeys.cs)

```csharp
public static class CacheKeys
{
    // Format: [Microservice].[Domain].[Context].[Parameters]
    
    // Order - 0: userId
    public const string UserOrdersKey = 
        "[Microservice].[Domain].[UserOrders].[{0}]";
    
    // Order - 0: userId, 1: orderId
    public const string UserOrderItemsKey = 
        "[Microservice].[Domain].[UserOrderItems].[{0}].[{1}]";
    
    // Product - 0: productId
    public const string ProductDetailsKey = 
        "[Microservice].[Domain].[ProductDetails].[{0}]";
    
    // Payment - 0: userId
    public const string PaymentHistoryKey = 
        "[Microservice].[Domain].[PaymentHistory].[{0}]";
}
```

**Uso**:

```csharp
var cacheKey = string.Format(
    CacheKeys.UserOrdersKey,
    userId);
```

**Convenções**:
- Prefixo: `[Microservice].[Domain]`
- Context: Descrição do dado cacheado
- Parameters: `{0}`, `{1}`, etc. em ordem lógica

---

## 🔧 Dependency Injection

**Arquivo**: [Infrastructure/DependencyInjection.cs](../app/src/Infrastructure/DependencyInjection.cs)

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services)
{
    SetupInfrastructure.Execute(); // Source generators
    
    return services
        .AddInfrastructureHealthChecks()
        .AddIntegrations();
}

private static IServiceCollection AddIntegrations(
    this IServiceCollection services)
{
    // Order
    services.AddScoped<IOrderIntegration, 
        OrderIntegration>();
    
    // Product
    services.AddScoped<IProductIntegration, ProductIntegration>();
    
    // Payment
    services.AddScoped<IPaymentIntegration, PaymentIntegration>();
    
    // User
    services.AddScoped<IUserIntegration, UserIntegration>();
    
    // Inventory
    services.AddScoped<IInventoryIntegration, InventoryIntegration>();
    
    return services;
}
```

---

## ✅ Checklist: Adicionar Nova Integration

1. **Criar Interface** (em Core):
   - [ ] `Core/{Domain}/Abstractions/Integration/I{Domain}Integration.cs`
   - [ ] Métodos retornam `Task<Result<T>>`

2. **Criar Integration Models** (em Infrastructure):
   - [ ] `Infrastructure/{Domain}/Model/{Entity}IntegrationModel.cs`
   - [ ] Usar `[JsonPropertyName]` se API usa naming diferente
   - [ ] Adicionar explicit operator para domain model

3. **Criar Integration Class**:
   - [ ] `Infrastructure/{Domain}/Integration/{Domain}Integration.cs`
   - [ ] Herdar de `IntegrationCachingBase`
   - [ ] Implementar interface do Core
   - [ ] Primary constructor com dependencies

4. **Implementar métodos**:
   - [ ] Check cache primeiro
   - [ ] Build HTTP request
   - [ ] Handle timeout com `TaskCanceledException`
   - [ ] Handle HTTP errors
   - [ ] Deserialize com `SerializerOptionsMapping`
   - [ ] Map para domain model
   - [ ] Save cache

5. **Adicionar Cache Key**:
   - [ ] `Infrastructure/Helpers/CacheKeys.cs`
   - [ ] Seguir pattern `[Bff].[App].[Credit].[Context].[{params}]`

6. **Registrar Source Generators**:
   - [ ] `Infrastructure/SetupInfrastructure.cs`
   - [ ] Adicionar `[JsonSerializable]` para integration models

7. **Registrar DI**:
   - [ ] `Infrastructure/DependencyInjection.cs`
   - [ ] `AddScoped<IInterface, Implementation>()`

8. **Testar**:
   - [ ] Integration test com HTTP mocking
   - [ ] Testar cache hit/miss
   - [ ] Testar timeout e error scenarios
