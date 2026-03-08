---
name: architecture-overview
description: Visão geral da arquitetura Clean Architecture do microsserviço .NET 8. Use quando precisar entender Railway-Oriented Programming, Result<T>, padrões de design (Primary Constructor, Explicit Operator), estrutura de pastas ou convenções de nomenclatura.
---

# 01. Architecture Overview

## 📐 Visão Geral da Arquitetura

Este **template base para microsserviços .NET 8** segue uma **arquitetura em camadas limpa** (Clean Architecture) com separação clara de responsabilidades. Esta documentação serve como modelo adaptável para qualquer domínio de negócio:

- **API Layer**: Endpoints HTTP, validação de entrada, autorização
- **Core Layer**: Lógica de negócio, abstrações (interfaces), domain models
- **Infrastructure Layer**: Implementações concretas de integrações externas, cache, repositories

---

## 🎯 Princípios Arquiteturais

### 1. **Railway-Oriented Programming**

Todas as operações retornam `Result<T>` ao invés de lançar exceptions:

```csharp
public async Task<Result<OrderModel[]>> GetOrders(string entityId)
{
    var result = await _integration.GetOrders(entityId);
    
    if (!result.IsSuccess)
        return result.Errors; // Propaga erros
    
    return result.Value; // Retorna sucesso
}
```

**Vantagens**:
- Fluxo explícito de erros
- Sem try-catch para lógica de negócio
- Pattern matching para tratamento
- Composição funcional

### 2. **Dependency Injection**

```csharp
// Core define interfaces
public interface I{Domain}Service { }

// Infrastructure implementa
public class {Domain}Integration : I{Domain}Integration { }

// API consome via DI
private static async Task<IResult> GetData(
    I{Domain}Service service) // ← Injetado automaticamente
{
    // ...
}
```

**Fluxo de Dependências**:
```
API → Core (Abstractions) ← Infrastructure
```

### 3. **Interface Segregation**

Cada domínio tem duas camadas de abstrações:

- **Services** (Core/Abstractions/Services): Lógica de negócio
- **Integrations** (Core/Abstractions/Integration): Contratos de APIs externas

### 4. **Native AOT Compilation**

Source generators substituem reflection:

```csharp
[JsonSerializable(typeof(Result<ProductContract>[]))]
internal partial class ResultProductContractSourceGenerator : JsonSerializerContext;
```

**Benefícios**:
- Startup 4x mais rápido
- 60% menos uso de memória
- Deploy menor (~30MB vs ~90MB)

### 5. **Fail-Safe with Cache**

```csharp
// 1. Tenta cache
var cache = await GetCache<Model[]>(cacheKey);
if (cache.found) return cache.value;

// 2. Chama API externa
var result = await CallExternalApi();

// 3. Salva em cache (fire-and-forget)
_ = SaveCache(cacheKey, result.Value);
```

Redis com TTL de 10 minutos e degradação graceful em falhas.

---

## 🏗️ Estrutura de Projetos

### Organização de Pastas

```
app/
├── Microservice.{Domain}.sln
├── global.json                  # .NET 8 SDK version
└── src/
    ├── Api/                     # 🌐 Camada de Apresentação
    │   ├── Api.csproj           # PublishAot=true
    │   ├── Program.cs           # Bootstrap e middleware
    │   ├── DependencyInjection.cs
    │   ├── appsettings.json
    │   ├── Modules/             # Endpoints por domínio
    │   │   ├── OrderModule.cs
    │   │   ├── ProductModule.cs
    │   │   ├── PaymentModule.cs
    │   │   └── OrderEndPoints/  # Sub-módulos
    │   └── SourceGeneratorHelper/
    │       ├── DomainSourceGenerator.cs
    │       ├── SourceGeneratorMappings.cs
    │       └── EndpointRegistrationExtensions.cs
    │
    ├── Core/                    # 🎯 Lógica de Negócio
    │   ├── Core.csproj
    │   ├── DependencyInjection.cs
    │   ├── SerializerOptionsMapping.cs  # JSON options centralizadas
    │   ├── Order/
    │   │   ├── Abstractions/
    │   │   │   ├── Services/IOrderService.cs
    │   │   │   └── Integration/IOrderIntegration.cs
    │   │   ├── Models/          # DTOs e domain models
    │   │   ├── Services/        # Implementação de services
    │   │   ├── Validators/      # FluentValidation
    │   │   └── Errors/          # Error mapping
    │   └── BusinessEvent/       # Logging de eventos
    │
    └── Infrastructure/          # 🔧 Integrações Externas
        ├── Infrastructure.csproj
        ├── DependencyInjection.cs
        ├── SetupInfrastructure.cs  # Source generators
        ├── Order/
        │   ├── Integration/OrderIntegration.cs
        │   └── Model/           # External API contracts
        └── Generics/
            └── IntegrationCachingBase.cs

test/
└── Unit.Tests/                 # 🧪 Testes
    ├── Unit.Tests.csproj
    ├── Core/
    │   ├── Services/            # Testes de services
    │   └── Validators/          # Testes de validação
    └── Infrastructure/
        └── Integrations/        # Testes de integração HTTP
```

---

## 📋 Convenções de Nomenclatura

### Módulos e Classes

| Tipo | Padrão | Exemplo | Localização |
|------|--------|---------|-------------|
| **Module** | `{Domain}Module` | `OrderModule` | `Api/Modules/` |
| **Service Interface** | `I{Domain}{Entity}Service` | `IOrderService` | `Core/{Domain}/Abstractions/Services/` |
| **Service Implementation** | `{Domain}{Entity}Service` | `OrderService` | `Core/{Domain}/Services/` |
| **Integration Interface** | `I{Domain}{Entity}Integration` | `IOrderIntegration` | `Core/{Domain}/Abstractions/Integration/` |
| **Integration Implementation** | `{Domain}{Entity}Integration` | `OrderIntegration` | `Infrastructure/{Domain}/Integration/` |
| **Validator** | `{Model}Validator(s)` | `CreateOrderRequestValidator` | `Core/{Domain}/Validators/` |
| **Error Mapping** | `{Domain}ErrorsMapping` | `OrderErrorsMapping` | `Core/{Domain}/Errors/` |
| **Error Code Enum** | `{Domain}ErrorsCode` | `OrderErrorsCode` | `Core/{Domain}/Errors/` |

### Models

| Tipo | Padrão | Exemplo |
|------|--------|---------|
| **Domain Model** | Descritivo | `Order`, `OrderItem`, `Product` |
| **Request DTO** | `{Action}Request` ou `Request{Entity}` | `CreateOrderRequest`, `UpdateProductRequest` |
| **Response DTO** | `{Entity}Response` | `OrderResponse`, `PaymentResponse` |
| **Integration Model** | `{Entity}Integration{Type}` | `OrderIntegrationModel` |

### Métodos e Endpoints

| Tipo | Padrão | Exemplo |
|------|--------|---------|
| **Endpoint Method** | `{Action}{Entity}` (private static) | `GetOrders`, `CreateProduct` |
| **Service Method** | `{Action}{Entity}` | `GetOrders`, `ProcessPayment` |
| **Test Method** | `{Method}_Should{Result}_When{Condition}` | `GetOrders_ShouldReturnList_WhenUserExists` |

### Cache Keys

```csharp
// Pattern: [Microservice].[Domain].[{Context}].[{Param1}].[{Param2}]...
public const string UserOrdersKey = 
    "[Microservice].[Domain].[UserOrders].[{0}]";
```

---

## 🔄 Padrões de Design

### 1. Result Pattern (Railway-Oriented)

```csharp
// Uso em Service
public async Task<Result<Order[]>> GetOrders(string userId)
{
    var result = await _integration.GetOrders(userId);
    
    if (!result.IsSuccess)
        return result.Errors; // Propaga erros
    
    var orders = result.Value.Where(x => x.IsActive).ToArray();
    
    return orders; // Implicit conversion para Result<T>
}

// Uso em API
return result.Match(
    onSuccess: data => TypedResults.Ok(data),
    onFailure: errors => errors.ToApiError(httpContext));
```

### 2. Primary Constructor Pattern (C# 12)

```csharp
// ✅ Moderno (C# 12)
public class OrderService(
    IOrderIntegration integration,
    ILogEventService logEvent)
    : IOrderService
{
    // Usa diretamente: integration, logEvent
}
```

### 3. Explicit Operator Mapping

```csharp
// Mapping em Infrastructure Model
public static explicit operator Order(
    OrderIntegrationModel source)
    => new()
    {
        OrderNumber = source.OrderNumber,
        TotalValue = source.TotalValue
    };

// Uso
var domainModel = (Order)integrationModel;
```

### 4. Caching Base Pattern

```csharp
public abstract class IntegrationCachingBase
{
    protected async Task<(bool found, T? value)> GetCache<T>(string key) { }
    
    protected Task SaveCache<T>(string key, T value, TimeSpan? ttl = null)
    {
        _ = Task.Run(async () => // Fire-and-forget
        {
            await _cache.StringSetAsync(
                key,
                JsonSerializer.Serialize(value),
                ttl ?? TimeSpan.FromMinutes(10));
        });
        
        return Task.CompletedTask;
    }
}
```

---

## 🔐 Cross-Cutting Concerns

### Segurança

- **JWT Token**: Validação via custom middleware `SecurityHandler`
- **Header Encryption**: Mensagens criptografadas
- **HTTPS Only**: Redirect automático para HTTPS
- **Authorization Filter**: `AddEndpointAuthorizationFilter` para endpoints sensíveis

### Observabilidade

```csharp
// Produção: OpenTelemetry + Dynatrace
services.AddAppTelemetry(
    otlpEndpoint: dynatraceUrl,
    serviceName: "microservice-{domain}",
    environmentName: "prd");

// Logs estruturados JSON
builder.Logging.AddJsonConsole();

// Business Events
await _logEventService.LogAsync(
    eventType: BusinessEventType.OrdersRetrieved,
    userId: userId,
    metadata: new { OrderCount = orders.Length });
```

### Resiliência

- **Cache Fallback**: Retorna cache mesmo se expirado em caso de falha da API
- **Timeout Handling**: 60s default, erro específico para timeouts
- **Graceful Degradation**: Nunca quebra por falha de cache/telemetria
- **Error Categorization**: `UnexpectedError`, `BusinessValidationError`, `ValidationError`

### Performance

- **Native AOT**: Startup 4x mais rápido
- **Redis Cache**: TTL 10 minutos, operações assíncronas
- **Fire-and-Forget Cache Write**: Não bloqueia response
- **HTTP Connection Pooling**: `IHttpClientFactory`
