---
name: error-mapping
description: Padrão de mapeamento de erros por domínio usando ApplicationError e Railway-Oriented Programming. Use ao criar novos domínios, definir enums de código de erro, mapear erros HTTP (400/422/500) ou propagar erros de integration para a API.
---

# 05. Error Mapping

## 📋 Visão Geral

O **Error Mapping** é um padrão consistente para definir, categorizar e mapear erros em cada domínio. Substitui o uso de exceptions para controle de fluxo, seguindo o **Railway-Oriented Programming**.

**Benefícios**:
- Erros tipados e rastreáveis
- Categorização consistente
- Mensagens user-friendly
- Logging estruturado
- Sem overhead de exceptions

---

## 🏗️ Estrutura de Error Mapping

Cada domínio possui dois arquivos na pasta `Core/{Domain}/Errors/`:

```
Core/{Domain}/Errors/
├── {Domain}ErrorsCode.cs        # Enum com códigos de erro
└── {Domain}ErrorsMapping.cs     # Mapeamento de códigos para ApplicationError
```

---

## 🔢 Error Code Enum

**Pattern**: `{Domain}ErrorsCode`

```csharp
public enum OrderErrorsCode
{
    // Integration errors (0-9)
    GetUserOrdersError = 0,
    GetUserOrdersTimeout = 1,
    GetOrderItemsError = 2,
    GetOrderItemsTimeout = 3,
    
    // Business validation errors (10-19)
    OrderNotFound = 10,
    InactiveOrder = 11,
    InvalidOrderStatus = 12,
    
    // Processing errors (20-29)
    OrderValueTooLow = 20,
    OrderValueTooHigh = 21,
    InvalidQuantity = 22,
    
    // Fulfillment errors (30-39)
    OrderAlreadyShipped = 30,
    OrderCancelled = 31,
    
    // Generic error for passthrough
    GenericError = 999
}
```

### Convenções de Numeração

| Faixa | Categoria |
|-------|-----------|
| 0-9 | Integration errors (HTTP calls, timeouts) |
| 10-19 | Business validation errors |
| 20-29 | Sub-category 1 (ex: Processing) |
| 30-39 | Sub-category 2 (ex: Fulfillment) |
| 999 | Generic passthrough error |

---

## 🗺️ Error Mapping Class

**Pattern**: `{Domain}ErrorsMapping`

```csharp
public static class OrderErrorsMapping
{
    public static ApplicationError GetError(
        OrderErrorsCode errorCode,
        params string[] args)
    {
        return errorCode switch
        {
            // Integration errors - UnexpectedError (500)
            OrderErrorsCode.GetUserOrdersError => new ApplicationError(
                errorCode.ToString(),
                "Erro ao buscar pedidos do usuário. Tente novamente mais tarde.",
                ApplicationErrorCategory.UnexpectedError),
            
            OrderErrorsCode.GetUserOrdersTimeout => new ApplicationError(
                errorCode.ToString(),
                "Timeout ao buscar pedidos. Tente novamente.",
                ApplicationErrorCategory.UnexpectedError),
            
            // Business validation - BusinessValidationError (422)
            OrderErrorsCode.OrderNotFound => new ApplicationError(
                errorCode.ToString(),
                $"Pedido {args[0]} não encontrado para o usuário.",
                ApplicationErrorCategory.BusinessValidationError),
            
            OrderErrorsCode.InactiveOrder => new ApplicationError(
                errorCode.ToString(),
                $"Pedido {args[0]} está inativo e não pode ser consultado.",
                ApplicationErrorCategory.BusinessValidationError),
            
            // Input validation - ValidationError (400)
            OrderErrorsCode.OrderValueTooLow => new ApplicationError(
                errorCode.ToString(),
                "Valor do pedido deve ser no mínimo R$ 10,00.",
                ApplicationErrorCategory.ValidationError),
            
            OrderErrorsCode.OrderValueTooHigh => new ApplicationError(
                errorCode.ToString(),
                $"Valor máximo para pedido é R$ {args[0]}.",
                ApplicationErrorCategory.ValidationError),
            
            // Passthrough error - preserva mensagem original
            OrderErrorsCode.GenericError => new ApplicationError(
                errorCode.ToString(),
                args.Length > 0 ? args[0] : "Erro inesperado",
                ApplicationErrorCategory.UnexpectedError),
            
            _ => throw new ArgumentOutOfRangeException(
                nameof(errorCode),
                errorCode,
                $"Error code '{errorCode}' não mapeado")
        };
    }
}
```

---

## 📦 ApplicationError

**Definição** (biblioteca externa `Sofisa.Api.Helper`):

```csharp
public record ApplicationError
{
    public string Code { get; init; }
    public string Details { get; init; }
    public ApplicationErrorCategory Category { get; init; }
    
    public ApplicationError(string code, string details, ApplicationErrorCategory category)
    {
        Code = code;
        Details = details;
        Category = category;
    }
}

public enum ApplicationErrorCategory
{
    UnexpectedError,           // Erros técnicos/integration (500)
    BusinessValidationError,   // Regras de negócio violadas (422)
    ValidationError            // Input inválido (400)
}
```

---

## 🎯 Categorias de Erro

### 1. UnexpectedError (HTTP 500)

**Uso**: Erros técnicos, falhas de integração, timeouts

```json
{
  "title": "GetUserOrdersError",
  "status": 500,
  "detail": "Erro ao buscar pedidos. Tente novamente mais tarde."
}
```

### 2. BusinessValidationError (HTTP 422)

**Uso**: Regras de negócio violadas, estado inválido

```json
{
  "title": "OrderNotFound",
  "status": 422,
  "detail": "Pedido 123456 não encontrado."
}
```

### 3. ValidationError (HTTP 400)

**Uso**: Entrada inválida, parâmetros fora do range

```json
{
  "title": "OrderValueTooLow",
  "status": 400,
  "detail": "Valor do pedido deve ser no mínimo R$ 10,00."
}
```

---

## 💻 Uso em Integration

```csharp
public async Task<Result<Order[]>> GetOrders(string userId)
{
    try
    {
        var response = await httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("API returned {StatusCode}", response.StatusCode);
            return OrderErrorsMapping.GetError(OrderErrorsCode.GetUserOrdersError);
        }
        
        // ... deserialização e mapeamento
        
        return mapped;
    }
    catch (TaskCanceledException ex)
    {
        _logger.LogError(ex, "Timeout getting orders");
        return OrderErrorsMapping.GetError(OrderErrorsCode.GetUserOrdersTimeout);
    }
    catch (HttpRequestException ex)
    {
        _logger.LogError(ex, "HTTP error getting orders");
        return OrderErrorsMapping.GetError(OrderErrorsCode.GetUserOrdersError);
    }
}
```

---

## 💻 Uso em Service

```csharp
public async Task<Result<Order[]>> GetUserOrders(string userId)
{
    // 1. Chama integration
    var result = await _integration.GetOrders(userId);
    
    // 2. Propaga erro de integration
    if (!result.IsSuccess)
        return result.Errors;
    
    var orders = result.Value;
    
    // 3. Business validation
    if (orders.Length == 0)
        return OrderErrorsMapping.GetError(OrderErrorsCode.OrderNotFound, userId);
    
    // 4. Filtra pedidos inativos
    var activeOrders = orders.Where(x => x.Status == "ACTIVE").ToArray();
    
    if (activeOrders.Length == 0)
        return OrderErrorsMapping.GetError(OrderErrorsCode.InactiveOrder, userId);
    
    return activeOrders;
}
```

---

## 💻 Uso em Endpoint (API)

```csharp
private static async Task<IResult> GetUserOrders(
    HttpContext httpContext,
    IOrderService service)
{
    var userId = httpContext.GetHeaderValue(HeaderInformationEnum.Id);
    
    if (string.IsNullOrEmpty(userId))
        return TypedResults.Unauthorized();
    
    var result = await service.GetUserOrders(userId);
    
    return result.Match(
        onSuccess: orders => TypedResults.Ok(orders),
        onFailure: errors => errors.ToApiError(httpContext));
}
```

---

## 🎨 Padrões de Mensagens

### 1. Mensagens Genéricas (sem parâmetros)

```csharp
OrderErrorsCode.GetUserOrdersError => new ApplicationError(
    errorCode.ToString(),
    "Erro ao buscar pedidos do cliente. Tente novamente mais tarde.",
    ApplicationErrorCategory.UnexpectedError)
```

### 2. Mensagens Parametrizadas

```csharp
OrderErrorsCode.ContractNotFound => new ApplicationError(
    errorCode.ToString(),
    $"Contrato {args[0]} não encontrado para o cliente {args[1]}.",
    ApplicationErrorCategory.BusinessValidationError)

// Uso
return OrderErrorsMapping.GetError(
    OrderErrorsCode.ContractNotFound,
    contractNumber,
    clientId);
```

### 3. Passthrough (preservar mensagem original)

```csharp
OrderErrorsCode.GenericError => new ApplicationError(
    errorCode.ToString(),
    args.Length > 0 ? args[0] : "Erro inesperado",
    ApplicationErrorCategory.UnexpectedError)

// Uso
return OrderErrorsMapping.GetError(
    OrderErrorsCode.GenericError,
    externalApiErrorMessage);
```

---

## ✅ Checklist: Adicionar Novo Erro

1. **Adicionar código ao enum**:
   - [ ] Abrir `Core/{Domain}/Errors/{Domain}ErrorsCode.cs`
   - [ ] Adicionar novo valor com número único
   - [ ] Seguir convenção: `{Action}{ErrorType}`

2. **Adicionar mapeamento**:
   - [ ] Abrir `Core/{Domain}/Errors/{Domain}ErrorsMapping.cs`
   - [ ] Adicionar case no switch expression
   - [ ] Definir mensagem user-friendly
   - [ ] Escolher categoria: `UnexpectedError`, `BusinessValidationError`, `ValidationError`

3. **Usar no código**:
   - [ ] No Integration: mapear HTTP errors e timeouts
   - [ ] No Service: validações de negócio
   - [ ] Testar error path com unit test

---

## 📊 Mapeamento de Status HTTP

| ApplicationErrorCategory | HTTP Status | Quando Usar |
|--------------------------|-------------|-------------|
| `ValidationError` | 400 Bad Request | Entrada inválida (body, query, headers) |
| `BusinessValidationError` | 422 Unprocessable Entity | Regra de negócio violada |
| `UnexpectedError` | 500 Internal Server Error | Erro técnico, integration failure |
