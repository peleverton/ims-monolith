---
name: core-project-patterns
description: Padrões do projeto Core: services, abstractions, domain models, validators e error mapping. Use ao criar lógica de negócio, interfaces de service/integration, DTOs com record, validators FluentValidation ou registrar dependências no DI.
---

# 04. Core Project Patterns

## 📋 Visão Geral

O projeto **Core** contém a **lógica de negócio** do microsserviço, organizado por domínios independentes.

**Responsabilidades**:
- Lógica de negócio (business rules)
- Abstrações (interfaces)
- Domain models e DTOs
- Validação de entrada (FluentValidation)
- Mapeamento de erros

**Princípio**: Core **não depende** de Infrastructure. Usa apenas abstrações (interfaces).

---

## 🏗️ Estrutura por Domínio

Cada domínio segue esta organização:

```
Core/{Domain}/
├── Abstractions/
│   ├── Services/            # Interfaces de services
│   │   └── I{Domain}Service.cs
│   └── Integration/         # Interfaces de integrations
│       └── I{Domain}Integration.cs
├── Models/                  # DTOs e domain models
│   ├── {Entity}.cs
│   ├── {Entity}Request.cs
│   └── {Entity}Response.cs
├── Services/                # Implementação de services
│   └── {Domain}Service.cs
├── Validators/              # FluentValidation validators
│   └── {Model}Validator.cs
└── Errors/                  # Error mapping
    ├── {Domain}ErrorsCode.cs
    └── {Domain}ErrorsMapping.cs
```

---

## 🎯 Abstractions

### 1. Service Interfaces

**Localização**: `Core/{Domain}/Abstractions/Services/`

```csharp
public interface IOrderService
{
    Task<Result<Order[]>> GetUserOrders(string userId);
    
    Task<Result<OrderItem[]>> GetOrderItems(
        string userId,
        string orderId);
    
    Task<Result<bool>> CreateOrder(
        string userId,
        CreateOrderRequest request);
}
```

**Características**:
- Todos os métodos retornam `Task<Result<T>>`
- Sem lançamento de exceptions para lógica de negócio
- Parâmetros explícitos (não usar DTOs genéricos)

### 2. Integration Interfaces

**Localização**: `Core/{Domain}/Abstractions/Integration/`

```csharp
public interface IOrderIntegration
{
    Task<Result<Order[]>> GetOrders(string userId);
    Task<Result<OrderItem[]>> GetItems(string userId, string orderId);
    Task<Result<CreateOrderResponse>> CreateOrder(string userId, CreateOrderRequest request);
}
```

**Diferença entre Service e Integration**:
- **Service**: Lógica de negócio, orquestração, transformações
- **Integration**: Contrato de API externa, sem lógica de negócio

---

## 🛠️ Services Implementation

**Localização**: `Core/{Domain}/Services/`

### Estrutura com Primary Constructor

```csharp
public class OrderService(
    IOrderIntegration orderIntegration,
    IProductIntegration productIntegration,
    ILogEventService logEventService)
    : IOrderService
{
    public async Task<Result<Order[]>> GetUserOrders(string userId)
    {
        // 1. Chama integration
        var result = await orderIntegration.GetOrders(userId);
        
        // 2. Valida resultado
        if (!result.IsSuccess)
            return result.Errors;
        
        // 3. Business logic
        var activeOrders = result.Value
            .Where(x => x.Status == "ACTIVE")
            .OrderByDescending(x => x.OrderDate)
            .ToArray();
        
        // 4. Log evento de negócio
        await logEventService.LogAsync(
            BusinessEventType.OrdersRetrieved,
            userId,
            new { OrderCount = activeOrders.Length });
        
        // 5. Retorna sucesso
        return activeOrders;
    }
}
```

### Padrões de Implementação

#### 1. Primary Constructor (C# 12)

```csharp
// ✅ Moderno (C# 12)
public class MyService(
    IMyIntegration integration,
    ILogger<MyService> logger)
    : IMyService
{
    // Usa diretamente: integration, logger
}
```

#### 2. Result Pattern

```csharp
// Propagação de erros
if (!result.IsSuccess)
    return result.Errors;

// Conversão implícita de sucesso
return activeContracts; // Implicit: Result<T>.Success(activeContracts)

// Conversão implícita de erro
return PersonalLoanErrorsMapping.GetError(
    PersonalLoanErrorsCode.ContractNotFound); // Implicit: Result<T>.Failure(error)
```

#### 3. Orquestração de Múltiplas Integrations

```csharp
public async Task<Result<EnrichedData>> GetEnrichedData(string clientId)
{
    // Chamadas paralelas
    var contractTask = _contractIntegration.GetContracts(clientId);
    var simulationTask = _simulationIntegration.GetSimulations(clientId);
    
    await Task.WhenAll(contractTask, simulationTask);
    
    var contractResult = await contractTask;
    var simulationResult = await simulationTask;
    
    if (!contractResult.IsSuccess)
        return contractResult.Errors;
    
    if (!simulationResult.IsSuccess)
        return simulationResult.Errors;
    
    var enriched = MergeData(contractResult.Value, simulationResult.Value);
    
    return enriched;
}
```

---

## 📦 Models

**Localização**: `Core/{Domain}/Models/`

### Tipos de Models

#### 1. Domain Models (Entities)

```csharp
public record Order
{
    public string OrderId { get; init; }
    public string UserId { get; init; }
    public decimal TotalValue { get; init; }
    public DateTime OrderDate { get; init; }
    public string Status { get; init; }
}
```

**Características**: `record` para imutabilidade, `init` setters.

#### 2. Request DTOs

```csharp
public record CreateOrderRequest
{
    public decimal TotalValue { get; init; }
    public int Quantity { get; init; }
    public DateTime DeliveryDate { get; init; }
}
```

#### 3. Response DTOs

```csharp
public record OrderSummaryResponse
{
    public decimal TotalValue { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal DiscountRate { get; init; }
}
```

### Convenções de Nomenclatura

| Tipo | Pattern | Exemplo |
|------|---------|---------|
| Entity | Descritivo | `Order`, `OrderItem`, `Product` |
| Request | `{Action}Request` | `CreateOrderRequest`, `UpdateProductRequest` |
| Response | `{Entity}Response` | `OrderSummaryResponse`, `PaymentStatusResponse` |

---

## ✅ Validators (FluentValidation)

**Localização**: `Core/{Domain}/Validators/`

```csharp
public class CreateOrderRequestValidator 
    : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.TotalValue)
            .NotNull()
            .WithMessage("Valor total é obrigatório")
            .GreaterThan(0)
            .WithMessage("Valor deve ser maior que zero")
            .LessThanOrEqualTo(100000)
            .WithMessage("Valor máximo é R$ 100.000");
        
        RuleFor(x => x.Quantity)
            .NotNull()
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("Máximo de 100 unidades");
        
        RuleFor(x => x.DeliveryDate)
            .NotNull()
            .Must(BeValidDeliveryDate)
            .WithMessage("Data de entrega inválida");
    }
    
    private bool BeValidDeliveryDate(DateTime deliveryDate)
        => deliveryDate > DateTime.UtcNow && deliveryDate <= DateTime.UtcNow.AddMonths(3);
}
```

### Registro no DI

```csharp
private static IServiceCollection AddFluentValidators(
    this IServiceCollection services)
{
    services.AddScoped<IValidator<CreateOrderRequest>, CreateOrderRequestValidator>();
    services.AddScoped<IValidator<UpdateProductRequest>, UpdateProductRequestValidator>();
    return services;
}
```

### Uso na API

```csharp
endpoints.MapPost("/simulation", PostSimulation)
    .AddEndpointFilter<ValidationFilter<RequestSimulationCredit>>();
```

---

## ⚠️ Error Mapping

**Localização**: `Core/{Domain}/Errors/`

Detalhes completos em: **`.github/skills/error-mapping/SKILL.md`**

### Estrutura Rápida

```csharp
// 1. Enum de códigos
public enum PersonalLoanErrorsCode
{
    GetClientContractsError = 0,
    GetClientContractsTimeout = 1,
    ContractNotFound = 2
}

// 2. Mapping estático
public static class PersonalLoanErrorsMapping
{
    public static ApplicationError GetError(
        PersonalLoanErrorsCode errorCode,
        params string[] args)
    {
        return errorCode switch
        {
            PersonalLoanErrorsCode.ContractNotFound => new ApplicationError(
                errorCode.ToString(),
                $"Contrato {args[0]} não encontrado",
                ApplicationErrorCategory.BusinessValidationError),
            // ...
        };
    }
}

// 3. Uso em Service/Integration
return PersonalLoanErrorsMapping.GetError(
    PersonalLoanErrorsCode.ContractNotFound,
    contractNumber);
```

---

## 🔗 Dependency Injection

**Arquivo**: `Core/DependencyInjection.cs`

```csharp
public static IServiceCollection AddCore(this IServiceCollection services)
{
    return services
        .AddFluentValidators()
        .AddBizEvent()
        .AddServices();
}

private static IServiceCollection AddServices(this IServiceCollection services)
{
    services.AddScoped<IOrderService, OrderService>();
    services.AddScoped<IProductService, ProductService>();
    services.AddScoped<IPaymentService, PaymentService>();
    return services;
}

private static IServiceCollection AddBizEvent(this IServiceCollection services)
{
    services.AddScoped<ILogEventService, LogEventService>();
    return services;
}
```

**Lifetime**: Todos os services são **Scoped** (por request).

---

## ✅ Checklist: Adicionar Novo Service

1. **Criar Interface**:
   - [ ] `I{Domain}Service.cs` em `Core/{Domain}/Abstractions/Services/`
   - [ ] Métodos retornando `Task<Result<T>>`

2. **Criar Implementação**:
   - [ ] `{Domain}Service.cs` em `Core/{Domain}/Services/`
   - [ ] Primary constructor para DI
   - [ ] Injetar integrations necessárias

3. **Criar Models**:
   - [ ] Domain models em `Core/{Domain}/Models/`
   - [ ] Request/Response DTOs
   - [ ] Usar `record` para imutabilidade

4. **Criar Validators** (se necessário):
   - [ ] `{Model}Validator.cs` em `Core/{Domain}/Validators/`
   - [ ] Herdar de `AbstractValidator<T>`
   - [ ] Registrar em `AddFluentValidators()`

5. **Mapear Erros**:
   - [ ] Adicionar códigos em `{Domain}ErrorsCode`
   - [ ] Adicionar mappings em `{Domain}ErrorsMapping`

6. **Registrar DI**:
   - [ ] Adicionar em `Core/DependencyInjection.cs`
   - [ ] `AddScoped<IInterface, Implementation>()`

7. **Testar**:
   - [ ] `{Service}Tests.cs` em `test/Unit.Tests/Core/Services/`
   - [ ] Mockar dependencies
   - [ ] Testar success e failure paths
