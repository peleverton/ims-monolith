---
name: minimal-api-modules
description: Criação e organização de módulos de Minimal API por domínio de negócio. Use ao criar novos endpoints, implementar validação com FluentValidation, configurar autorização de transação ou organizar rotas com MapGroup.
---

# 03. Minimal API Modules

## 📋 Visão Geral

Os **módulos** são a forma de organizar endpoints da Minimal API por **domínio de negócio**. Cada módulo implementa a interface `IEndpointModule` e agrupa endpoints relacionados.

**Localização**: `app/src/Api/Modules/`

---

## 🎯 Interface IEndpointModule

```csharp
public interface IEndpointModule
{
    static abstract IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints);
}
```

**Características**:
- Método estático abstrato (C# 11+)
- Recebe `IEndpointRouteBuilder` para registrar rotas
- Auto-descoberto pelo source generator

---

## 🏗️ Padrões de Módulos

### 1. Módulo Simples

```csharp
public class ProductModule : IEndpointModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var catalogGroup = endpoints
            .MapGroup("/catalog")
            .WithTags("Products");

        catalogGroup.MapGet("/Products", GetProducts);
        catalogGroup.MapPost("/CreateProduct", CreateProduct)
            .AddEndpointAuthorizationFilter(
                AuthEAuthType.PasswordAndToken,
                endpoints.ServiceProvider
                    .GetService<EnvironmentConfiguration>()!
                    .UrlApiAuthorization!);

        return endpoints;
    }

    private static async Task<IResult> GetProducts(/*...*/) { }
    private static async Task<IResult> CreateProduct(/*...*/) { }
}
```

### 2. Módulo Complexo (Com Sub-Módulos)

```csharp
public class OrderModule : IEndpointModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var orderGroupBuilder = endpoints.MapGroup("orders");

        // Delega para sub-módulos (quando há muitos endpoints por domínio)
        OrderListEndpoint.MapOrderEndPoints(
            orderGroupBuilder,
            endpoints.ServiceProvider
                .GetRequiredService<EnvironmentConfiguration>()
                .UrlApiAuthorization);

        OrderDetailsEndpoints.MapDetailsEndPoints(orderGroupBuilder);

        return endpoints;
    }
}
```

**Quando usar sub-módulos**:
- Domínio com muitos endpoints (>5)
- Endpoints com contextos distintos
- Facilitar manutenção e testes

---

## 🎨 Estrutura de Endpoint Method

### Assinatura Padrão

```csharp
[EndpointDescription("Descrição do que o endpoint faz")]
[ProducesResponseType(typeof(Result<MyModel>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
private static async Task<IResult> GetMyLimits(
    HttpContext httpContext,                    // 1. Contexto HTTP (sempre primeiro)
    IColateralServices colateralService,        // 2. Services (DI)
    ILogger<ColateralModule> logger,            // 3. Logger
    [FromBody] MyPayload? payload = null)       // 4. Body (opcional)
{
    // Implementação
}
```

### Parâmetros

**HttpContext** (sempre primeiro):
- Acesso a headers, user claims, request/response
- Usado para extrair `clientId`, `email`, etc.

**Body** com `[FromBody]`:
- Deserialização automática via source generators
- Validação via FluentValidation (se registrado)

**Query Parameters**: `[FromQuery] string? filter = null`

**Route Parameters**: `string contractNumber` (bindado automaticamente se rota é `/contracts/{contractNumber}`)

---

## 🔐 Validação e Autorização

### 1. Validação de Headers

```csharp
private static async Task<IResult> GetProducts(
    HttpContext httpContext,
    IProductService productService)
{
    var userId = httpContext.GetHeaderValue(HeaderInformationEnum.Id);
    
    if (string.IsNullOrEmpty(userId))
        return TypedResults.Unauthorized();
    
    var result = await productService.GetProducts(userId);
    
    return result.Match(
        onSuccess: TypedResults.Ok,
        onFailure: errors => errors.ToApiError(httpContext));
}
```

**Headers Comuns**:
- `HeaderInformationEnum.Id` - User/Entity ID
- `HeaderInformationEnum.Email` - Email do usuário
- `HeaderInformationEnum.Authorization` - JWT Token

### 2. Validação com FluentValidation

```csharp
objectiveGroup.MapPost("/CreateProduct", CreateProduct)
    .AddEndpointFilter<ValidationFilter<CreateProductPayload>>();
```

### 3. Autorização de Transação

```csharp
managementGroup.MapPost("/PostMyLimits", PostMyLimits)
    .AddEndpointAuthorizationFilter(
        AuthEAuthType.PasswordAndToken,  // Tipo: PasswordAndToken | Token | Biometry
        urlApiAuthorization);            // URL da API de autorização
```

---

## 🔄 Railway-Oriented Programming nos Endpoints

### Match Pattern

```csharp
var result = await colateralService.GetMyLimits(int.Parse(clientId));

return result.Match(
    onSuccess: data => TypedResults.Ok(data),           // 200 OK
    onFailure: errors => errors.ToApiError(httpContext)); // 400/422/500
```

### Conversão de Erros

```csharp
public static IResult ToApiError(
    this ApplicationError[] errors,
    HttpContext httpContext)
{
    var firstError = errors[0];
    
    var statusCode = firstError.Category switch
    {
        ApplicationErrorCategory.ValidationError => StatusCodes.Status400BadRequest,
        ApplicationErrorCategory.BusinessValidationError => StatusCodes.Status422UnprocessableEntity,
        ApplicationErrorCategory.UnexpectedError => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status500InternalServerError
    };
    
    return TypedResults.Problem(
        title: firstError.Code,
        detail: firstError.Details,
        statusCode: statusCode,
        extensions: new Dictionary<string, object?>
        {
            ["traceId"] = httpContext.TraceIdentifier,
            ["errors"] = errors
        });
}
```

---

## 🎯 Grupos de Rotas (MapGroup)

### Prefixos e Tags

```csharp
var catalogGroup = endpoints
    .MapGroup("/catalog")        // Prefixo de rota
    .WithTags("Products");       // Tag Swagger

catalogGroup.MapGet("/Products", GetProducts);
// Rota final: GET /catalog/Products
```

### Múltiplos Grupos

```csharp
public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
{
    var group1 = endpoints.MapGroup("/prefix1").WithTags("Tag1");
    group1.MapGet("/endpoint1", Method1);
    
    var group2 = endpoints.MapGroup("/prefix2").WithTags("Tag2");
    group2.MapGet("/endpoint2", Method2);
    
    return endpoints;
}
```

---

## 📚 Exemplos Completos

### Exemplo 1: GET Simples

```csharp
[EndpointDescription("Retorna informações de produtos")]
[ProducesResponseType(typeof(Result<ProductList>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
private static async Task<IResult> GetProducts(
    HttpContext httpContext,
    IProductService productService,
    ILogger<ProductModule> logger)
{
    var userId = httpContext.GetHeaderValue(HeaderInformationEnum.Id);
    
    if (string.IsNullOrEmpty(userId))
        return TypedResults.Unauthorized();

    var result = await productService.GetProducts(userId);
    
    return result.Match(
        onSuccess: TypedResults.Ok,
        onFailure: errors => errors.ToApiError(httpContext));
}
```

### Exemplo 2: POST com Payload e Autorização

```csharp
[EndpointDescription("Cria um novo produto")]
[ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
private static async Task<IResult> CreateProduct(
    HttpContext httpContext,
    IProductService productService,
    ILogger<ProductModule> logger,
    [FromBody] CreateProductPayload payload)
{
    var userId = httpContext.GetHeaderValue(HeaderInformationEnum.Id);
    var email = httpContext.GetHeaderValue(HeaderInformationEnum.Email);
    
    if (string.IsNullOrEmpty(userId))
        return TypedResults.Unauthorized();

    var result = await productService.CreateProduct(userId, email, payload);
    
    return result.Match(
        onSuccess: TypedResults.Ok,
        onFailure: errors => errors.ToApiError(httpContext));
}

// Registro com autorização
catalogGroup.MapPost("/CreateProduct", CreateProduct)
    .AddEndpointAuthorizationFilter(
        AuthEAuthType.PasswordAndToken,
        urlApiAuthorization);
```

### Exemplo 3: Sub-Módulo

```csharp
// OrderListEndpoint.cs
public static class OrderListEndpoint
{
    public static void MapOrderEndPoints(
        IEndpointRouteBuilder endpoints,
        string authUrl)
    {
        endpoints.MapGet("/list", GetOrders);
        endpoints.MapGet("/{orderId}", GetOrderDetails);
        endpoints.MapPost("/create", CreateOrder)
            .AddEndpointAuthorizationFilter(AuthEAuthType.Token, authUrl);
    }

    private static async Task<IResult> GetOrders(/*...*/) { }
    private static async Task<IResult> GetOrderDetails(/*...*/) { }
    private static async Task<IResult> CreateOrder(/*...*/) { }
}
```

---

## ✅ Checklist: Criar Novo Endpoint

1. **Defina o módulo**:
   - [ ] Existe módulo para o domínio? Se não, crie `{Domain}Module.cs`
   - [ ] Implemente `IEndpointModule`

2. **Crie o service**:
   - [ ] Interface em `Core/{Domain}/Abstractions/Services/`
   - [ ] Implementação em `Core/{Domain}/Services/`
   - [ ] Registre em `Core/DependencyInjection.cs`

3. **Crie o endpoint method**:
   - [ ] Método privado estático
   - [ ] Assinatura: `HttpContext`, services, logger, payload
   - [ ] Attributes: `[EndpointDescription]`, `[ProducesResponseType]`

4. **Valide entrada**:
   - [ ] Extraia headers necessários
   - [ ] Retorne `Unauthorized` se headers ausentes
   - [ ] Adicione `ValidationFilter` se houver body

5. **Implemente lógica**:
   - [ ] Chame service com `await`
   - [ ] Use `result.Match()` para tratar resposta
   - [ ] Converta erros com `ToApiError()`

6. **Registre Source Generator**:
   - [ ] Adicione types em `SourceGeneratorMappings.cs`
   - [ ] Compile e valide sem erros AOT

7. **Teste**:
   - [ ] Escreva teste unitário
   - [ ] Valide response codes (200, 400, 401, 500)
