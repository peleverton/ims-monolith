---
name: testing-patterns
description: Padrões de testes unitários com xUnit e Moq para services e integrations. Use ao escrever testes de Service (mockando integrations), testes de Integration (mockando HttpMessageHandler com .Protected()), testes de validators ou ao diagnosticar como criar Result<T> corretamente nos mocks.
---

# 08. Testing Patterns

## 📋 Visão Geral

O projeto de testes segue uma estrutura organizada que reflete a arquitetura do microsserviço, com foco em **testes unitários** de services e integrations.

**Localização**: [test/Unit.Tests/](../app/test/Unit.Tests/)

**Framework Stack**:
- **xUnit** - Test runner
- **Moq** - Mocking framework
- **AutoFixture** - Test data generation (opcional)
- **Shouldly** - Fluent assertions (opcional)

---

## 🏗️ Estrutura de Testes

```
test/Unit.Tests/
├── Unit.Tests.csproj
├── Core/
│   ├── Services/                # Testes de services
│   │   ├── OrderServiceTests.cs
│   │   ├── ProductServiceTests.cs
│   │   └── PaymentServiceTests.cs
│   ├── Validators/              # Testes de validators
│   │   └── CreateOrderRequestValidatorTests.cs
│   └── BusinessEvent/           # Testes de business events
└── Infrastructure/
    └── Integrations/            # Testes de integrations
        ├── OrderIntegrationTests.cs
        └── ProductIntegrationTests.cs
```

---

## 🎯 Service Tests Pattern

### ⚠️ Importante: Padrões Result<T>

O projeto utiliza a classe `Result<T>` do pacote **Sofisa.Api.Helper** para Railway-Oriented Programming:

**Criar Result**:
```csharp
// Sucesso
var result = new Result<Order>(orderData);

// Falha genérica
var failure = Result<Order>.NoValueFailure;

// Falha com erro específico
var error = OrderErrorsMapping.GetError(OrderErrorsCode.OrderNotFound);
var errorResult = new Result<Order>(error);
```

**Verificar Result**:
```csharp
if (result.IsSuccess)
    var data = result.Value;
else
    var errors = result.Errors;
```

**Mock em Testes**:
- **Integrations** retornam o valor diretamente (ex: `Order[]`) ou `Result<T>`
- **Services** trabalham com `Result<T>` internamente
- Use `.ReturnsAsync(value)` para sucesso ou `.ReturnsAsync(Result<T>.NoValueFailure)` para falha

### Estrutura Básica

```csharp
public class OrderServiceTests
{
    private readonly Mock<IOrderIntegration> _integrationMock;
    private readonly Mock<IProductIntegration> _productMock;
    private readonly Mock<ILogEventService> _logEventMock;
    private readonly OrderService _service;
    
    public OrderServiceTests()
    {
        _integrationMock = new Mock<IOrderIntegration>();
        _productMock = new Mock<IProductIntegration>();
        _logEventMock = new Mock<ILogEventService>();
        
        _service = new OrderService(
            _integrationMock.Object,
            _productMock.Object,
            _logEventMock.Object);
    }
    
    [Fact]
    public async Task GetUserOrders_ShouldReturnOrders_WhenIntegrationSucceeds()
    {
        // Arrange
        var userId = "12345";
        var expectedOrders = new[]
        {
            new Order
            {
                OrderId = "001",
                UserId = userId,
                TotalValue = 10000m,
                Status = "ACTIVE"
            },
            new Order
            {
                OrderId = "002",
                UserId = userId,
                TotalValue = 5000m,
                Status = "ACTIVE"
            }
        };
        
        _integrationMock
            .Setup(x => x.GetOrders(userId))
            .ReturnsAsync(expectedOrders);
        
        // Act
        var result = await _service.GetUserOrders(userId);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Length);
        Assert.Equal("001", result.Value[0].OrderId);
        
        _integrationMock.Verify(
            x => x.GetOrders(userId),
            Times.Once);
    }
    
    [Fact]
    public async Task GetUserOrders_ShouldReturnError_WhenIntegrationFails()
    {
        // Arrange
        var userId = "12345";
        
        _integrationMock
            .Setup(x => x.GetOrders(userId))
            .ReturnsAsync(Result<Order[]>.NoValueFailure);
        
        // Act
        var result = await _service.GetUserOrders(userId);
        
        // Assert
        Assert.False(result.IsSuccess);
    }
    
    [Fact]
    public async Task GetUserOrders_ShouldReturnError_WhenNoOrdersFound()
    {
        // Arrange
        var userId = "12345";
        var emptyOrders = Array.Empty<Order>();
        
        _integrationMock
            .Setup(x => x.GetOrders(userId))
            .ReturnsAsync(emptyOrders);
        
        // Act
        var result = await _service.GetUserOrders(userId);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("OrderNotFound", result.Errors[0].Code);
    }
}
```

### Naming Convention

**Pattern**: `{MethodName}_Should{ExpectedOutcome}_When{Condition}`

**Exemplos**:
- `GetUserOrders_ShouldReturnOrders_WhenIntegrationSucceeds`
- `GetUserOrders_ShouldReturnError_WhenIntegrationFails`
- `GetUserOrders_ShouldReturnError_WhenNoOrdersFound`
- `CreateOrder_ShouldReturnOrder_WhenParametersAreValid`
- `UpdateOrderStatus_ShouldReturnTrue_WhenOrderIsUpdated`

### Test Organization (AAA Pattern)

```csharp
[Fact]
public async Task MethodName_ShouldOutcome_WhenCondition()
{
    // Arrange - Setup test data and mocks
    var input = CreateTestData();
    _mockDependency.Setup(x => x.Method(It.IsAny<Type>()))
        .ReturnsAsync(expectedResult);
    
    // Act - Execute the method under test
    var result = await _service.MethodUnderTest(input);
    
    // Assert - Verify the outcome
    Assert.True(result.IsSuccess);
    Assert.Equal(expected, result.Value);
    _mockDependency.Verify(x => x.Method(It.IsAny<Type>()), Times.Once);
}
```

---

## 🔌 Integration Tests Pattern

### ⚠️ Importante: Mock de HTTP Client

Nos testes de **Integration**, é **obrigatório** mockar o `HttpMessageHandler` para simular respostas HTTP.

**Padrão Correto**:
```csharp
// 1. Criar mock do HttpMessageHandler
var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

// 2. Configurar o método protegido SendAsync usando .Protected()
httpMessageHandlerMock
    .Protected()
    .Setup<Task<HttpResponseMessage>>(
        "SendAsync",                              // Nome do método protegido
        ItExpr.IsAny<HttpRequestMessage>(),       // Qualquer request
        ItExpr.IsAny<CancellationToken>())        // Qualquer cancellation token
    .ReturnsAsync(new HttpResponseMessage
    {
        StatusCode = HttpStatusCode.OK,
        Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
    });

// 3. Criar HttpClient com o handler mockado
var httpClient = new HttpClient(httpMessageHandlerMock.Object);

// 4. Configurar IHttpClientFactory para retornar o HttpClient mockado
_httpClientFactoryMock
    .Setup(x => x.CreateClient(It.IsAny<string>()))
    .Returns(httpClient);
```

**Por que usar `.Protected()`?**
- O método `SendAsync` do `HttpMessageHandler` é **protected**
- Não pode ser acessado diretamente no mock
- `.Protected()` permite mockar métodos protegidos
- `ItExpr` (ao invés de `It`) é usado para expressões em métodos protegidos

### HTTP Mocking

```csharp
public class OrderIntegrationTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<OrderIntegration>> _loggerMock;
    private readonly Mock<IReadWriteCacheCommands> _cacheMock;
    private readonly EnvironmentConfiguration _config;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly OrderIntegration _integration;
    
    public OrderIntegrationTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<OrderIntegration>>();
        _cacheMock = new Mock<IReadWriteCacheCommands>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        
        // IMPORTANTE: Preencher TODAS as propriedades necessárias
        _config = new EnvironmentConfiguration
        {
            UrlOrdersApi = "http://localhost:5000",  // URL usada pela integration
            ContextEnvironment = "Test"              // Ambiente de teste
        };
        
        _integration = new OrderIntegration(
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _config,
            _cacheMock.Object,
            _httpContextAccessorMock.Object);
    }
    
    [Fact]
    public async Task GetOrders_ShouldReturnOrders_WhenApiReturnsSuccess()
    {
        // Arrange
        var userId = "12345";
        var apiResponse = @"[
            {
                ""orderId"": ""001"",
                ""totalValue"": 10000.00,
                ""orderDate"": ""2024-01-15"",
                ""status"": ""ACTIVE""
            }
        ]";
        
        // IMPORTANTE: Mock do HttpMessageHandler (padrão obrigatório)
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock
            .Protected()  // Acessa métodos protegidos
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",  // Método protegido do HttpMessageHandler
                ItExpr.IsAny<HttpRequestMessage>(),  // Aceita qualquer request
                ItExpr.IsAny<CancellationToken>())   // Aceita qualquer token
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(apiResponse, Encoding.UTF8, "application/json")
            });
        
        // Criar HttpClient com o handler mockado
        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);
        
        // Setup cache mock (miss)
        _cacheMock
            .Setup(x => x.StringGetAsync(It.IsAny<string>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        
        // Act
        var result = await _integration.GetOrders(userId);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("001", result.Value[0].OrderId);
        
        // Verify cache was called
        _cacheMock.Verify(
            x => x.StringSetAsync(
                It.IsAny<string>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }
    
    [Fact]
    public async Task GetOrders_ShouldReturnError_WhenApiReturns500()
    {
        // Arrange
        var userId = "12345";
        
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });
        
        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);
        
        _cacheMock
            .Setup(x => x.StringGetAsync(It.IsAny<string>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        
        // Act
        var result = await _integration.GetOrders(userId);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("GetUserOrdersError", result.Errors[0].Code);
    }
    
    [Fact]
    public async Task GetOrders_ShouldReturnError_WhenTimeout()
    {
        // Arrange
        var userId = "12345";
        
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Timeout"));
        
        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        _httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);
        
        _cacheMock
            .Setup(x => x.StringGetAsync(It.IsAny<string>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);
        
        // Act
        var result = await _integration.GetOrders(userId);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("GetUserOrdersTimeout", result.Errors[0].Code);
    }
    
    [Fact]
    public async Task GetOrders_ShouldReturnCachedData_WhenCacheHit()
    {
        // Arrange
        var userId = "12345";
        var cachedOrders = new[]
        {
            new Order
            {
                OrderId = "001",
                TotalValue = 10000m
            }
        };
        
        var cachedJson = JsonSerializer.Serialize(
            cachedOrders,
            SerializerOptionsMapping.CustomJsonSerializerOptions);
        
        _cacheMock
            .Setup(x => x.StringGetAsync(It.IsAny<string>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(cachedJson));
        
        // Act
        var result = await _integration.GetOrders(userId);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("001", result.Value[0].OrderId);
        
        // Verify HTTP was NOT called
        _httpClientFactoryMock.Verify(
            x => x.CreateClient(It.IsAny<string>()),
            Times.Never);
    }
}
```

### Cenários Comuns de Mock HTTP

```csharp
// ✅ Sucesso (200 OK)
.ReturnsAsync(new HttpResponseMessage
{
    StatusCode = HttpStatusCode.OK,
    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
});

// ✅ Erro 400 Bad Request
.ReturnsAsync(new HttpResponseMessage
{
    StatusCode = HttpStatusCode.BadRequest
});

// ✅ Erro 500 Internal Server Error
.ReturnsAsync(new HttpResponseMessage
{
    StatusCode = HttpStatusCode.InternalServerError
});

// ✅ Timeout (TaskCanceledException)
.ThrowsAsync(new TaskCanceledException("Timeout"));

// ✅ Exception genérica
.ThrowsAsync(new HttpRequestException("Network error"));
```

### ⚠️ Importante: EnvironmentConfiguration

Ao criar `EnvironmentConfiguration` nos testes, é **obrigatório** preencher **TODAS** as propriedades que:
1. Serão usadas pela integration durante o teste
2. Não possuem valor default

**Por que?**
- Se uma propriedade necessária não for preenchida, o teste falhará com **NullReferenceException** ou erro similar
- A integration tentará acessar a propriedade e encontrará `null`

**Padrão Correto**:
```csharp
public OrderIntegrationTests()
{
    // ✅ CORRETO: Preencher TODAS as URLs necessárias
    _config = new EnvironmentConfiguration
    {
        UrlOrdersApi = "http://localhost:5000",        // Usado pela integration
        UrlProductsApi = "http://localhost:5001",      // Usado pela integration
        UrlPaymentsApi = "http://localhost:5002",      // Usado pela integration
        ContextEnvironment = "Test",                   // Sempre necessário
        // Adicionar todas as outras propriedades usadas pela classe testada
    };
}
```

**Como descobrir quais propriedades preencher?**
1. Olhe o construtor ou código da integration sendo testada
2. Identifique quais URLs/configurações são acessadas
3. Preencha todas elas no `EnvironmentConfiguration`

**❌ Erro Comum**:
```csharp
// ❌ INCORRETO: Propriedade faltando
_config = new EnvironmentConfiguration
{
    UrlOrdersApi = "http://localhost:5000"
    // UrlProductsApi não foi preenchida, mas é usada -> NullReferenceException!
};
```

---

## 🎯 Validator Tests

```csharp
public class CreateOrderRequestValidatorTests
{
    private readonly CreateOrderRequestValidator _validator;
    
    public CreateOrderRequestValidatorTests()
    {
        _validator = new CreateOrderRequestValidator();
    }
    
    [Fact]
    public void Validate_ShouldPass_WhenAllFieldsAreValid()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            TotalValue = 10000m,
            Quantity = 5,
            DeliveryDate = DateTime.UtcNow.AddDays(30)
        };
        
        // Act
        var result = _validator.Validate(request);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public void Validate_ShouldFail_WhenValueIsInvalid(decimal value)
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            TotalValue = value,
            Quantity = 5,
            DeliveryDate = DateTime.UtcNow.AddDays(30)
        };
        
        // Act
        var result = _validator.Validate(request);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "TotalValue");
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public void Validate_ShouldFail_WhenQuantityIsOutOfRange(int quantity)
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            TotalValue = 10000m,
            Quantity = quantity,
            DeliveryDate = DateTime.UtcNow.AddDays(30)
        };
        
        // Act
        var result = _validator.Validate(request);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Quantity");
    }
}
```

---

## 📊 Test Coverage

### Cenários Essenciais

**Service Tests**:
- ✅ Success path
- ✅ Integration failure propagation
- ✅ Business validation errors
- ✅ Empty results handling
- ✅ Data transformation
- ✅ Orchestration de múltiplas integrations

**Integration Tests**:
- ✅ API success response
- ✅ API error responses (400, 500, etc.)
- ✅ Timeout handling
- ✅ Deserialization errors
- ✅ Cache hit
- ✅ Cache miss
- ✅ Cache failure (graceful degradation)

**Validator Tests**:
- ✅ Valid input
- ✅ Invalid fields
- ✅ Edge cases (min/max values)
- ✅ Complex business rules

---

## 🛠️ Test Utilities

### Test Data Builders

```csharp
public static class TestDataBuilder
{
    public static Order CreateOrder(
        string orderId = "001",
        string userId = "12345",
        decimal totalValue = 10000m,
        string status = "ACTIVE")
    {
        return new Order
        {
            OrderId = orderId,
            UserId = userId,
            TotalValue = totalValue,
            Status = status,
            OrderDate = DateTime.UtcNow.AddDays(-7),
            DeliveryDate = DateTime.UtcNow.AddDays(7),
            PaidAmount = totalValue * 0.5m,
            ItemCount = 3
        };
    }
}
```

### Padrões de Criação de Result<T>

```csharp
// ✅ CORRETO: Criar Result com sucesso
var successResult = new Result<Order>(orderData);

// ✅ CORRETO: Criar Result com falha (NoValueFailure)
var failureResult = Result<Order>.NoValueFailure;

// ✅ CORRETO: Criar Result com erro específico
var error = OrderErrorsMapping.GetError(OrderErrorsCode.GetUserOrdersError);
var errorResult = new Result<Order>(error);

// ✅ CORRETO: Mock de integration que retorna valor diretamente
_integrationMock
    .Setup(x => x.GetOrders(userId))
    .ReturnsAsync(expectedOrders);

// ✅ CORRETO: Mock de integration que retorna Result com falha
_integrationMock
    .Setup(x => x.GetOrders(userId))
    .ReturnsAsync(Result<Order[]>.NoValueFailure);

// ❌ INCORRETO: Não existem métodos .Success() ou .Failure()
// Result<Order>.Success(value);  // ❌ NÃO EXISTE
// Result<Order>.Failure(error);  // ❌ NÃO EXISTE
```

---

## ✅ Checklist: Escrever Testes

1. **Setup do teste**:
   - [ ] Criar classe de teste `{ClassUnderTest}Tests`
   - [ ] Mockar dependencies no constructor
   - [ ] Criar instância da classe under test

2. **Nomear testes**:
   - [ ] Seguir pattern: `{Method}_Should{Outcome}_When{Condition}`
   - [ ] Usar `[Fact]` para testes simples
   - [ ] Usar `[Theory]` + `[InlineData]` para múltiplos casos

3. **Estruturar testes (AAA)**:
   - [ ] **Arrange**: Setup test data e mocks
   - [ ] **Act**: Execute o método
   - [ ] **Assert**: Verify outcome

4. **Cobrir cenários**:
   - [ ] Happy path (success)
   - [ ] Error paths (cada tipo de erro)
   - [ ] Edge cases
   - [ ] Boundary conditions

5. **Verificar mocks**:
   - [ ] `Verify()` que métodos foram chamados
   - [ ] `Times.Once`, `Times.Never`, etc.

6. **Assertions claras**:
   - [ ] Usar `Assert.True/False` para booleans
   - [ ] Usar `Assert.Equal` para valores
   - [ ] Usar `Assert.Single/Empty` para collections
