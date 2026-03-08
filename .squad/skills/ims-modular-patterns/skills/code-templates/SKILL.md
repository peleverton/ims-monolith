---
name: code-templates
description: Templates copy-paste para scaffolding rápido de novos componentes. Use ao criar um novo domínio completo, um novo módulo de API, service, integration, integration model, error mapping, validator ou testes seguindo os padrões estabelecidos.
---

# 09. Code Templates

## 📋 Visão Geral

Templates **copy-paste** para scaffolding rápido de novos componentes seguindo os padrões do template base .NET 8. 

Substitua os placeholders conforme necessário:
- `{Domain}` → Nome do domínio (Order, Product, Payment, User, etc.)
- `{Entity}` → Nome da entidade (Order, Product, User, etc.)
- `{Model}` → Nome do model (CreateOrderRequest, ProductModel, etc.)
- `{domain-route}` → Rota REST (orders, products, payments, etc.)
- `{endpoint-route}` → Subrota do endpoint (/{id}, /search, etc.)

---

## 🌐 Minimal API Module Template

### Template: Módulo Simples

```csharp
using Core.{Domain}.Abstractions.Services;
using Core.{Domain}.Models;
using Core.Generics.Models;
using Microsoft.AspNetCore.Mvc;
 

namespace Api.Modules;

public class {Domain}Module : IEndpointModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/{domain-route}")
            .WithTags("{Domain}");

        group.MapGet("/{endpoint-route}", Get{Entity});
        group.MapPost("/{endpoint-route}", Post{Entity})
            .AddEndpointAuthorizationFilter(
                AuthEAuthType.Token,
                endpoints.ServiceProvider
                    .GetService<EnvironmentConfiguration>()!
                    .UrlApiAuthorization!);

        return endpoints;
    }

    [EndpointDescription("Descrição do endpoint GET")]
    [ProducesResponseType(typeof(Result<{Entity}>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    private static async Task<IResult> Get{Entity}(
        HttpContext httpContext,
        I{Domain}Service service,
        ILogger<{Domain}Module> logger)
    {
        var clientId = httpContext.GetHeaderValue(HeaderInformationEnum.Id);
        
        if (string.IsNullOrEmpty(clientId))
            return TypedResults.Unauthorized();

        var result = await service.Get{Entity}(clientId);
        
        return result.Match(
            onSuccess: TypedResults.Ok,
            onFailure: errors => errors.ToApiError(httpContext));
    }

    [EndpointDescription("Descrição do endpoint POST")]
    [ProducesResponseType(typeof(Result<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    private static async Task<IResult> Post{Entity}(
        HttpContext httpContext,
        I{Domain}Service service,
        ILogger<{Domain}Module> logger,
        [FromBody] {Entity}Request payload)
    {
        var clientId = httpContext.GetHeaderValue(HeaderInformationEnum.Id);
        
        if (string.IsNullOrEmpty(clientId))
            return TypedResults.Unauthorized();

        var result = await service.Create{Entity}(clientId, payload);
        
        return result.Match(
            onSuccess: TypedResults.Ok,
            onFailure: errors => errors.ToApiError(httpContext));
    }
}
```

**Checklist Pós-Criação**:
- [ ] Substituir `{Domain}`, `{Entity}`, `{domain-route}`, `{endpoint-route}`
- [ ] Adicionar usings necessários
- [ ] Ajustar tipos de retorno (`Result<T>`)
- [ ] Configurar autorização apropriada

### Template: Módulo Complexo (com Sub-Módulos)

```csharp
using Api.Modules.{Domain}EndPoints;
using Sofisa.Api.Helper.Endpoint;

namespace Api.Modules;

public class {Domain}Module : IEndpointModule
{
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("{domain-route}");

        {Domain}{Context1}Endpoint.Map{Context1}EndPoints(
            group,
            endpoints.ServiceProvider
                .GetRequiredService<EnvironmentConfiguration>()
                .UrlApiAuthorization);
        
        {Domain}{Context2}Endpoint.Map{Context2}EndPoints(group);

        return endpoints;
    }
}
```

**Sub-Endpoint Class**:

```csharp
namespace Api.Modules.{Domain}EndPoints;

public static class {Domain}{Context}Endpoint
{
    public static void Map{Context}EndPoints(
        IEndpointRouteBuilder endpoints,
        string? authUrl = null)
    {
        endpoints.MapGet("/{endpoint}", Get{Entity});
        endpoints.MapPost("/{endpoint}", Post{Entity});
    }

    [EndpointDescription("Descrição")]
    [ProducesResponseType(typeof(Result<{Entity}>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    private static async Task<IResult> Get{Entity}(
        HttpContext httpContext,
        I{Domain}Service service)
    {
        // Implementation
    }
}
```

---

## 🎯 Service Template

### Service Interface

**Arquivo**: `Core/{Domain}/Abstractions/Services/I{Domain}{Entity}Service.cs`

```csharp
using Core.{Domain}.Models;
using Core.Generics.Models;

namespace Core.{Domain}.Abstractions.Services;

public interface I{Domain}{Entity}Service
{
    Task<Result<{Entity}[]>> Get{Entity}List(string clientId);
    
    Task<Result<{Entity}>> Get{Entity}ById(
        string clientId,
        string {entity}Id);
    
    Task<Result<bool>> Create{Entity}(
        string clientId,
        {Entity}Request request);
    
    Task<Result<bool>> Update{Entity}(
        string clientId,
        string {entity}Id,
        {Entity}Request request);
}
```

### Service Implementation

**Arquivo**: `Core/{Domain}/Services/{Domain}{Entity}Service.cs`

```csharp
using Core.{Domain}.Abstractions.Integration;
using Core.{Domain}.Abstractions.Services;
using Core.{Domain}.Errors;
using Core.{Domain}.Models;
using Core.BusinessEvent.Abstractions.Services;
using Core.BusinessEvent.Model;
using Core.Generics.Models;

namespace Core.{Domain}.Services;

public class {Domain}{Entity}Service(
    I{Domain}{Entity}Integration integration,
    ILogEventService logEventService)
    : I{Domain}{Entity}Service
{
    public async Task<Result<{Entity}[]>> Get{Entity}List(string clientId)
    {
        // 1. Call integration
        var result = await integration.Get{Entity}List(clientId);
        
        // 2. Check result
        if (!result.IsSuccess)
            return result.Errors;
        
        // 3. Business logic
        var filtered = result.Value
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.CreatedDate)
            .ToArray();
        
        if (filtered.Length == 0)
        {
            return {Domain}ErrorsMapping.GetError(
                {Domain}ErrorsCode.{Entity}NotFound,
                clientId);
        }
        
        // 4. Log business event
        await logEventService.LogAsync(
            BusinessEventType.{Entity}Retrieved,
            clientId,
            new { Count = filtered.Length });
        
        // 5. Return success
        return filtered;
    }
    
    public async Task<Result<{Entity}>> Get{Entity}ById(
        string clientId,
        string {entity}Id)
    {
        var result = await integration.Get{Entity}ById(clientId, {entity}Id);
        
        if (!result.IsSuccess)
            return result.Errors;
        
        return result.Value;
    }
    
    public async Task<Result<bool>> Create{Entity}(
        string clientId,
        {Entity}Request request)
    {
        // Business validations
        if (request.Value <= 0)
        {
            return {Domain}ErrorsMapping.GetError(
                {Domain}ErrorsCode.Invalid{Entity}Value);
        }
        
        var result = await integration.Create{Entity}(clientId, request);
        
        if (!result.IsSuccess)
            return result.Errors;
        
        await logEventService.LogAsync(
            BusinessEventType.{Entity}Created,
            clientId,
            new { {Entity}Id = result.Value });
        
        return result.Value;
    }
    
    public async Task<Result<bool>> Update{Entity}(
        string clientId,
        string {entity}Id,
        {Entity}Request request)
    {
        var result = await integration.Update{Entity}(clientId, {entity}Id, request);
        
        if (!result.IsSuccess)
            return result.Errors;
        
        return result.Value;
    }
}
```

**Checklist**:
- [ ] Registrar em `Core/DependencyInjection.cs`
- [ ] Criar integration interface
- [ ] Criar error mapping
- [ ] Escrever testes unitários

---

## 🔌 Integration Template

### Integration Interface

**Arquivo**: `Core/{Domain}/Abstractions/Integration/I{Domain}{Entity}Integration.cs`

```csharp
using Core.{Domain}.Models;
using Core.Generics.Models;

namespace Core.{Domain}.Abstractions.Integration;

public interface I{Domain}{Entity}Integration
{
    Task<Result<{Entity}[]>> Get{Entity}List(string clientId);
    
    Task<Result<{Entity}>> Get{Entity}ById(
        string clientId,
        string {entity}Id);
    
    Task<Result<bool>> Create{Entity}(
        string clientId,
        {Entity}Request request);
}
```

### Integration Implementation

**Arquivo**: `Infrastructure/{Domain}/Integration/{Domain}{Entity}Integration.cs`

```csharp
using System.Net;
using System.Text.Json;
using Core.{Domain}.Abstractions.Integration;
using Core.{Domain}.Errors;
using Core.{Domain}.Models;
using Core.Generics.Models;
using Core.SerializerOptionsMapping;
using Infrastructure.Generics;
using Infrastructure.Helpers;
using Infrastructure.{Domain}.Model;
using Microsoft.Extensions.Logging; 
using StackExchange.Redis;

namespace Infrastructure.{Domain}.Integration;

public class {Domain}{Entity}Integration(
    IHttpClientFactory httpClientFactory,
    ILogger<{Domain}{Entity}Integration> logger,
    EnvironmentConfiguration environmentConfiguration,
    IReadWriteCacheCommands readWriteCacheCommands,
    IHttpContextAccessor? httpContextAccessor = null)
    : IntegrationCachingBase(readWriteCacheCommands, logger),
      I{Domain}{Entity}Integration
{
    public async Task<Result<{Entity}[]>> Get{Entity}List(string clientId)
    {
        // 1. Check cache
        var cacheKey = string.Format(
            CacheKeys.{Domain}{Entity}ListKey,
            clientId);
        
        var cache = await GetCache<{Entity}[]>(cacheKey);
        if (cache.found)
            return cache.value!;
        
        // 2. Build HTTP request
        const string path = "/v1/{domain}/{entities}";
        var queryParameter = $"?clientId={clientId}";
        
        var httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(60);
        
        // 3. Local debug support
        if (environmentConfiguration.ContextEnvironment == "Local")
        {
            httpClient.DefaultRequestHeaders.Add(
                "Authorization",
                httpContextAccessor?.HttpContext?.Request
                    .Headers.Authorization.ToString());
        }
        
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{environmentConfiguration.Url{Domain}Api}{path}{queryParameter}");
        
        // 4. Send request with error handling
        try
        {
            var httpResponseMessage = await httpClient.SendAsync(request);
            
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                logger.LogError(
                    "API returned {StatusCode} for client {ClientId}",
                    httpResponseMessage.StatusCode,
                    clientId);
                
                return {Domain}ErrorsMapping.GetError(
                    {Domain}ErrorsCode.Get{Entity}ListError);
            }
            
            // 5. Deserialize
            var content = await httpResponseMessage.Content.ReadAsStringAsync();
            var response = JsonSerializer.Deserialize<{Entity}IntegrationModel[]>(
                content,
                SerializerOptionsMapping.CustomJsonSerializerOptions);
            
            if (response == null)
            {
                logger.LogError(
                    "Failed to deserialize response for client {ClientId}",
                    clientId);
                
                return {Domain}ErrorsMapping.GetError(
                    {Domain}ErrorsCode.Get{Entity}ListError);
            }
            
            // 6. Map to domain models
            var domainModels = response
                .Select(x => ({Entity})x)
                .ToArray();
            
            // 7. Save to cache
            _ = SaveCache(cacheKey, domainModels);
            
            return domainModels;
        }
        catch (TaskCanceledException ex)
        {
            var error = {Domain}ErrorsMapping.GetError(
                {Domain}ErrorsCode.Get{Entity}ListTimeout);
            
            logger.LogError(
                error.ToEventId<{Domain}ErrorsCode>(),
                ex,
                "Timeout getting {entities} for client {ClientId}",
                clientId);
            
            return error;
        }
        catch (HttpRequestException ex)
        {
            var error = {Domain}ErrorsMapping.GetError(
                {Domain}ErrorsCode.Get{Entity}ListError);
            
            logger.LogError(
                error.ToEventId<{Domain}ErrorsCode>(),
                ex,
                "HTTP error getting {entities} for client {ClientId}",
                clientId);
            
            return error;
        }
    }
}
```

### Integration Model

**Arquivo**: `Infrastructure/{Domain}/Model/{Entity}IntegrationModel.cs`

```csharp
using System.Text.Json.Serialization;
using Core.{Domain}.Models;

namespace Infrastructure.{Domain}.Model;

public record {Entity}IntegrationModel
{
    [JsonPropertyName("{entity}_id")]
    public string Id { get; init; }
    
    [JsonPropertyName("client_id")]
    public string ClientId { get; init; }
    
    [JsonPropertyName("value")]
    public decimal Value { get; init; }
    
    [JsonPropertyName("created_date")]
    public DateTime CreatedDate { get; init; }
    
    [JsonPropertyName("is_active")]
    public bool IsActive { get; init; }
    
    // Explicit operator for mapping
    public static explicit operator {Entity}({Entity}IntegrationModel source)
        => new()
        {
            Id = source.Id,
            ClientId = source.ClientId,
            Value = source.Value,
            CreatedDate = source.CreatedDate,
            IsActive = source.IsActive
        };
}
```

**Checklist**:
- [ ] Adicionar cache key em `CacheKeys.cs`
- [ ] Adicionar URL em `EnvironmentConfiguration`
- [ ] Registrar source generator em `SetupInfrastructure.cs`
- [ ] Registrar DI em `Infrastructure/DependencyInjection.cs`
- [ ] Escrever integration tests

---

## ⚠️ Error Mapping Template

### Error Code Enum

**Arquivo**: `Core/{Domain}/Errors/{Domain}ErrorsCode.cs`

```csharp
namespace Core.{Domain}.Errors;

public enum {Domain}ErrorsCode
{
    // Integration errors (0-9)
    Get{Entity}ListError = 0,
    Get{Entity}ListTimeout = 1,
    Get{Entity}ByIdError = 2,
    Get{Entity}ByIdTimeout = 3,
    Create{Entity}Error = 4,
    Create{Entity}Timeout = 5,
    
    // Business validation errors (10-19)
    {Entity}NotFound = 10,
    Invalid{Entity}Value = 11,
    {Entity}AlreadyExists = 12,
    
    // Generic
    GenericError = 999
}
```

### Error Mapping Class

**Arquivo**: `Core/{Domain}/Errors/{Domain}ErrorsMapping.cs`

```csharp
using Sofisa.Api.Helper;

namespace Core.{Domain}.Errors;

public static class {Domain}ErrorsMapping
{
    public static ApplicationError GetError(
        {Domain}ErrorsCode errorCode,
        params string[] args)
    {
        return errorCode switch
        {
            // Integration errors
            {Domain}ErrorsCode.Get{Entity}ListError => new ApplicationError(
                errorCode.ToString(),
                "Erro ao buscar lista de {entities}. Tente novamente mais tarde.",
                ApplicationErrorCategory.UnexpectedError),
            
            {Domain}ErrorsCode.Get{Entity}ListTimeout => new ApplicationError(
                errorCode.ToString(),
                "Timeout ao buscar {entities}. Tente novamente.",
                ApplicationErrorCategory.UnexpectedError),
            
            // Business validation
            {Domain}ErrorsCode.{Entity}NotFound => new ApplicationError(
                errorCode.ToString(),
                $"{Entity} com ID {args[0]} não encontrado.",
                ApplicationErrorCategory.BusinessValidationError),
            
            {Domain}ErrorsCode.Invalid{Entity}Value => new ApplicationError(
                errorCode.ToString(),
                "Valor do {entity} inválido. Deve ser maior que zero.",
                ApplicationErrorCategory.ValidationError),
            
            // Passthrough
            {Domain}ErrorsCode.GenericError => new ApplicationError(
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

## ✅ Validator Template

**Arquivo**: `Core/{Domain}/Validators/{Model}Validator.cs`

```csharp
using Core.{Domain}.Models;
using FluentValidation;

namespace Core.{Domain}.Validators;

public class {Model}Validator : AbstractValidator<{Model}>
{
    public {Model}Validator()
    {
        RuleFor(x => x.Field1)
            .NotNull()
            .WithMessage("Field1 é obrigatório")
            .NotEmpty()
            .WithMessage("Field1 não pode ser vazio");
        
        RuleFor(x => x.Value)
            .GreaterThan(0)
            .WithMessage("Value deve ser maior que zero")
            .LessThanOrEqualTo(100000)
            .WithMessage("Value máximo é 100.000");
        
        RuleFor(x => x.Date)
            .NotNull()
            .Must(BeValidDate)
            .WithMessage("Data inválida");
    }
    
    private bool BeValidDate(DateTime date)
    {
        return date > DateTime.UtcNow && date <= DateTime.UtcNow.AddYears(1);
    }
}
```

**Registrar em** `Core/DependencyInjection.cs`:

```csharp
services.AddScoped<IValidator<{Model}>, {Model}Validator>();
```

---

## 🧪 Test Templates

### Service Test Template

**Arquivo**: `test/Unit.Tests/Core/Services/{Domain}{Entity}ServiceTests.cs`

```csharp
using Core.{Domain}.Abstractions.Integration;
using Core.{Domain}.Errors;
using Core.{Domain}.Models;
using Core.{Domain}.Services;
using Core.BusinessEvent.Abstractions.Services;
using Core.Generics.Models;
using Moq;
using Xunit;

namespace Unit.Tests.Core.Services;

public class {Domain}{Entity}ServiceTests
{
    private readonly Mock<I{Domain}{Entity}Integration> _integrationMock;
    private readonly Mock<ILogEventService> _logEventMock;
    private readonly {Domain}{Entity}Service _service;
    
    public {Domain}{Entity}ServiceTests()
    {
        _integrationMock = new Mock<I{Domain}{Entity}Integration>();
        _logEventMock = new Mock<ILogEventService>();
        
        _service = new {Domain}{Entity}Service(
            _integrationMock.Object,
            _logEventMock.Object);
    }
    
    [Fact]
    public async Task Get{Entity}List_ShouldReturnList_WhenIntegrationSucceeds()
    {
        // Arrange
        var clientId = "12345";
        var expected = new[]
        {
            new {Entity} { Id = "1", ClientId = clientId, Value = 100m },
            new {Entity} { Id = "2", ClientId = clientId, Value = 200m }
        };
        
        _integrationMock
            .Setup(x => x.Get{Entity}List(clientId))
            .ReturnsAsync(expected);
        
        // Act
        var result = await _service.Get{Entity}List(clientId);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Length);
        
        _integrationMock.Verify(
            x => x.Get{Entity}List(clientId),
            Times.Once);
    }
    
    [Fact]
    public async Task Get{Entity}List_ShouldReturnError_WhenIntegrationFails()
    {
        // Arrange
        var clientId = "12345";
        
        _integrationMock
            .Setup(x => x.Get{Entity}List(clientId))
            .ReturnsAsync(Result<{Entity}[]>.NoValueFailure);
        
        // Act
        var result = await _service.Get{Entity}List(clientId);
        
        // Assert
        Assert.False(result.IsSuccess);
    }
}
```

### Integration Test Template

**Arquivo**: `test/Unit.Tests/Infrastructure/Integrations/{Domain}{Entity}IntegrationTests.cs`

```csharp
using System.Net;
using System.Text;
using Core.{Domain}.Errors;
using Core.{Domain}.Models;
using Infrastructure.{Domain}.Integration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Sofisa.Api.Helper;
using StackExchange.Redis;
using Xunit;

namespace Unit.Tests.Infrastructure.Integrations;

public class {Domain}{Entity}IntegrationTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<{Domain}{Entity}Integration>> _loggerMock;
    private readonly Mock<IReadWriteCacheCommands> _cacheMock;
    private readonly EnvironmentConfiguration _config;
    private readonly {Domain}{Entity}Integration _integration;
    
    public {Domain}{Entity}IntegrationTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<{Domain}{Entity}Integration>>();
        _cacheMock = new Mock<IReadWriteCacheCommands>();
        
        _config = new EnvironmentConfiguration
        {
            Url{Domain}Api = "http://localhost:5000",
            ContextEnvironment = "Test"
        };
        
        _integration = new {Domain}{Entity}Integration(
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _config,
            _cacheMock.Object);
    }
    
    [Fact]
    public async Task Get{Entity}List_ShouldReturnList_WhenApiReturnsSuccess()
    {
        // Arrange
        var clientId = "12345";
        var apiResponse = @"[{""id"":""1"",""value"":100.00}]";
        
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(apiResponse, Encoding.UTF8, "application/json")
            });
        
        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);
        
        _cacheMock
            .Setup(x => x.StringGetAsync(It.IsAny<string>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        
        // Act
        var result = await _integration.Get{Entity}List(clientId);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value);
    }
}
```

---

## 📚 Checklist Completo: Novo Domínio

### 1. Core Layer
- [ ] `Core/{Domain}/Abstractions/Services/I{Domain}Service.cs`
- [ ] `Core/{Domain}/Abstractions/Integration/I{Domain}Integration.cs`
- [ ] `Core/{Domain}/Models/{Entity}.cs`
- [ ] `Core/{Domain}/Models/{Entity}Request.cs`
- [ ] `Core/{Domain}/Models/{Entity}Response.cs`
- [ ] `Core/{Domain}/Services/{Domain}Service.cs`
- [ ] `Core/{Domain}/Validators/{Model}Validator.cs`
- [ ] `Core/{Domain}/Errors/{Domain}ErrorsCode.cs`
- [ ] `Core/{Domain}/Errors/{Domain}ErrorsMapping.cs`
- [ ] Registrar em `Core/DependencyInjection.cs`

### 2. Infrastructure Layer
- [ ] `Infrastructure/{Domain}/Integration/{Domain}Integration.cs`
- [ ] `Infrastructure/{Domain}/Model/{Entity}IntegrationModel.cs`
- [ ] Adicionar cache key em `Infrastructure/Helpers/CacheKeys.cs`
- [ ] Registrar em `Infrastructure/DependencyInjection.cs`
- [ ] Adicionar source generator em `Infrastructure/SetupInfrastructure.cs`

### 3. API Layer
- [ ] `Api/Modules/{Domain}Module.cs`
- [ ] Adicionar source generator em `Api/SourceGeneratorHelper/SourceGeneratorMappings.cs`

### 4. Configuration
- [ ] Adicionar URL em `EnvironmentConfiguration`
- [ ] Adicionar variável de ambiente
- [ ] Atualizar Helm values

### 5. Tests
- [ ] `test/Unit.Tests/Core/Services/{Domain}ServiceTests.cs`
- [ ] `test/Unit.Tests/Infrastructure/Integrations/{Domain}IntegrationTests.cs`

### 6. Compile & Test
- [ ] `dotnet build`
- [ ] `dotnet test`
- [ ] `dotnet publish -c Release` (verificar AOT)
