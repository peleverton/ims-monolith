---
name: code-smells
description: Catálogo de code smells e práticas não recomendadas detectadas pelo SonarQube. Use SEMPRE antes de gerar qualquer código .NET para garantir que nenhuma das práticas proibidas seja reproduzida.
---

# 11. Code Smells e Práticas Não Recomendadas

## 📋 Visão Geral

Este documento cataloga **code smells** e **práticas de código que devem ser evitadas** durante o desenvolvimento de aplicações .NET. Todas são identificadas e reprovadas por **SonarQube** e **SonarLint**.

> **NENHUMA das práticas listadas deve ser reproduzida.** Este guia serve como referência **proibitiva** para desenvolvedores e modelos de IA durante geração de código.

---

## 🛑 Restrições Absolutas para IAs

Modelos de IA **NUNCA** devem gerar código que:

1. ❌ Use campos públicos (`public int Count;`)
2. ❌ Lance `Exception` genérico
3. ❌ Concatene strings com `+` em loops
4. ❌ Tenha métodos com > 6 parâmetros
5. ❌ Use `throw ex;` (deve ser `throw;`)
6. ❌ Hardcode URLs, conexões, secrets
7. ❌ Tenha código comentado
8. ❌ Use `goto` statements
9. ❌ Chame `GC.Collect()`
10. ❌ Ignore exceções silenciosamente

---

## 🔒 1. Segurança e Vulnerabilidades (CWE)

| Code Smell | Severidade |
|------------|------------|
| Fields should not have public accessibility | 🔴 Alta |
| General exceptions should never be thrown | 🔴 Alta |
| Generic exceptions should not be ignored | 🔴 Alta |
| Multiline blocks should be enclosed in curly braces | 🔴 Alta |
| Mutable fields should not be "public static" | 🔴 Alta |
| Track uses of "TODO"/"FIXME" tags | 🟡 Média |

```csharp
// ❌ INCORRETO - Campo público mutável
public class UserService
{
    public List<User> ActiveUsers; // ❌ Qualquer código externo modifica
}

// ✅ CORRETO - Encapsulamento adequado
public class UserService
{
    private readonly List<User> _activeUsers = new();
    public IReadOnlyCollection<User> ActiveUsers => _activeUsers.AsReadOnly();
}

// ❌ INCORRETO - Exceção genérica
throw new Exception("Invalid order");

// ✅ CORRETO - Resultado tipado (Railway-Oriented)
return OrderErrorsMapping.GetError(OrderErrorsCode.InvalidTotal);
```

---

## ⚡ 2. Performance

| Code Smell | Impacto |
|------------|---------|
| Strings should not be concatenated using '+' in a loop | O(n²) alocações |
| Multiple "OrderBy" calls should not be used | Múltiplas ordenações completas |
| Properties should not make collection or array copies | Alocações desnecessárias |
| Duplicate casts should not be made | Desperdício de CPU |

```csharp
// ❌ INCORRETO - String concatenation em loop
string report = "";
foreach (var order in orders)
    report += $"Order {order.Id}\n"; // 💥 Nova string a cada iteração

// ✅ CORRETO - StringBuilder
var builder = new StringBuilder();
foreach (var order in orders)
    builder.AppendLine($"Order {order.Id}");

// ❌ INCORRETO - Múltiplos OrderBy (descarta a primeira ordenação)
var sorted = orders.OrderBy(o => o.CustomerId).OrderBy(o => o.Date);

// ✅ CORRETO - ThenBy para ordenação composta
var sorted = orders.OrderBy(o => o.Date).ThenBy(o => o.CustomerId);
```

---

## 🧠 3. Complexidade e Manutenibilidade

| Code Smell | Limite |
|------------|--------|
| Cognitive Complexity of methods should not be too high | < 15 |
| Methods should not have too many parameters | ≤ 4-5 |
| Ternary operators should not be nested | 0 níveis |
| "goto" statement should not be used | Nunca |

```csharp
// ❌ INCORRETO - Muitos parâmetros
public Result<Order> CreateOrder(Guid customerId, string name, string email,
    string shippingAddress, string city, List<OrderItem> items,
    PaymentMethod paymentMethod, string promoCode) // 💥 8+ parâmetros

// ✅ CORRETO - Parameter Object pattern
public record CreateOrderCommand(
    CustomerInfo Customer,
    Address ShippingAddress,
    List<OrderItem> Items,
    PaymentMethod PaymentMethod,
    string? PromoCode = null);

public Result<Order> CreateOrder(CreateOrderCommand command) // ✅ 1 parâmetro
```

---

## 📐 4. Convenções de Nomenclatura

| Code Smell | Regra |
|------------|-------|
| Types should be named in PascalCase | `OrderStatus` não `orderStatus` |
| Enumeration types should comply with a naming convention | Singular, sem sufixo "Enum" |
| Flags enumerations zero-value members should be named "None" | `None = 0` |
| Literal suffixes should be upper case | `1000L`, `3.14F`, `1.5M` |

```csharp
// ❌ INCORRETO
[Flags]
public enum OrderStatuses { Pending = 0, Active = 1 } // Plural, sem "None"
public enum ColorEnum { red, green, blue }             // Sufixo, lowercase

// ✅ CORRETO
[Flags]
public enum Permission { None = 0, Read = 1, Write = 2, Delete = 4 }
public enum OrderStatus { Pending, Processing, Shipped, Delivered }
public enum Color { Red, Green, Blue }

// ❌ INCORRETO - Sufixos minúsculos
var amount = 1000.50m;  // ❌ m minúsculo
var big = 1000000l;     // ❌ l minúsculo (parece 1)

// ✅ CORRETO
var amount = 1000.50M;  // ✅ M maiúsculo
var big = 1000000L;     // ✅ L maiúsculo
```

---

## ⚙️ 5. Async/Await e Concorrência

| Code Smell | Problema |
|------------|----------|
| Parameter validation in "async"/"await" methods should be wrapped | Validação adiada |
| "ValueTask" should be consumed correctly | ValueTask não pode ser awaited duas vezes |
| Threads should not lock on objects with weak identity | Race conditions |
| Instance members should not write to "static" fields | Race conditions |

```csharp
// ❌ INCORRETO - Validação assíncrona (lazy)
public async Task<Order> GetOrderAsync(Guid orderId)
{
    ArgumentNullException.ThrowIfNull(orderId); // Só executa ao await
    return await _repository.GetByIdAsync(orderId);
}

// ✅ CORRETO - Validação síncrona imediata
public Task<Order> GetOrderAsync(Guid orderId)
{
    ArgumentNullException.ThrowIfNull(orderId); // Executa imediatamente ✅
    return GetOrderCoreAsync(orderId);
}

private async Task<Order> GetOrderCoreAsync(Guid orderId)
    => await _repository.GetByIdAsync(orderId);

// ❌ INCORRETO - ValueTask awaited múltiplas vezes
var task = GetCachedOrderAsync(123);
var order1 = await task; // OK
var order2 = await task; // 💥 Exception!

// ✅ CORRETO - Await imediato ou converte para Task
var order = await GetCachedOrderAsync(123);   // Await imediato
// OU
var task = GetCachedOrderAsync(123).AsTask(); // Converte para Task
```

---

## 🚨 6. Tratamento de Erros e Exceções

| Code Smell | Regra |
|------------|-------|
| Exceptions should not be explicitly rethrown | Use `throw;` não `throw ex;` |
| "catch" clauses should do more than rethrow | Adicione lógica ou remova |
| Exceptions should not be thrown from property getters | Properties devem ser simples |
| Exceptions should not be thrown in finally blocks | Finally não deve lançar |

```csharp
// ❌ INCORRETO - throw ex perde stack trace
catch (Exception ex)
{
    _logger.LogError(ex, "Failed");
    throw ex; // 💥 Perde stack trace original!
}

// ✅ CORRETO - throw preserva stack trace
catch (Exception ex)
{
    _logger.LogError(ex, "Failed");
    throw; // ✅ Preserva stack trace
}

// ❌ INCORRETO - Exception em property getter
public decimal Total
{
    get
    {
        if (_total < 0)
            throw new InvalidOperationException("Invalid total"); // ❌
        return _total;
    }
}

// ✅ CORRETO - Validação no setter
public decimal Total
{
    get => _total; // ✅ Getter simples
    private set
    {
        if (value < 0)
            throw new ArgumentException("Total cannot be negative", nameof(value));
        _total = value;
    }
}
```

---

## 🧪 7. Testes e Qualidade

| Code Smell | Regra |
|------------|-------|
| Tests should include assertions | Todo teste precisa de Assert |
| Tests should not be ignored | Sem `[Skip]` sem justificativa |
| Assertion arguments should be passed in the correct order | `(expected, actual)` |

```csharp
// ❌ INCORRETO - Sem assertions (sempre passa!)
[Fact]
public async Task ProcessOrder_ShouldWork()
{
    var order = new Order { Id = Guid.NewGuid(), Total = 100 };
    await _orderService.ProcessOrderAsync(order);
    // ❌ Nenhum Assert!
}

// ✅ CORRETO
[Fact]
public async Task ProcessOrder_WhenValid_ShouldProcessSuccessfully()
{
    var order = new Order { Id = Guid.NewGuid(), Total = 100 };
    var result = await _orderService.ProcessOrderAsync(order);
    
    Assert.True(result.IsSuccess);
    Assert.Equal(OrderStatus.Processed, result.Value.Status); // ✅ (expected, actual)
}

// ❌ INCORRETO - Ordem errada (confunde mensagem de erro)
Assert.Equal(total, 30); // ❌ (actual, expected)

// ✅ CORRETO
Assert.Equal(30, total); // ✅ (expected, actual)
```

---

## 📝 8. Limpeza e Manutenibilidade

| Code Smell | Regra |
|------------|-------|
| Sections of code should not be commented out | Use Git, não comentários |
| Boolean literals should not be redundant | Evite `== true`, `== false` |
| Trivial properties should be auto-implemented | Use auto-property |
| Unused local variables/parameters should be removed | Sem código morto |
| Empty statements/methods should be removed | Sem blocos vazios |

```csharp
// ❌ INCORRETO - Código comentado
// var discount = CalculateDiscount(command.Total);
// if (discount > 0)
//     command.Total -= discount;

// ✅ CORRETO - Usar Git para histórico

// ❌ INCORRETO - Boolean redundante
if (customer.IsVip == true)  // ❌
    return true;
else
    return false;

// ✅ CORRETO
return customer.IsVip; // ✅ Direto
```

---

## 🔧 9. Práticas Específicas de .NET

| Code Smell | Regra |
|------------|-------|
| "GC.Collect" should not be called | Deixe GC gerenciar |
| "IDisposable" should be implemented correctly | Padrão completo com Dispose(bool) |
| "new Guid()" should not be used | Use `Guid.Empty` ou `Guid.NewGuid()` |

```csharp
// ❌ INCORRETO - IDisposable incompleto
public class DatabaseConnection : IDisposable
{
    private SqlConnection _connection;
    public void Dispose() => _connection?.Dispose(); // ❌ Falta GC.SuppressFinalize
}

// ✅ CORRETO - Padrão completo
public class DatabaseConnection : IDisposable
{
    private SqlConnection? _connection;
    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing) _connection?.Dispose();
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this); // ✅
    }

    ~DatabaseConnection() => Dispose(false);
}
```

---

## ✅ Checklist de Qualidade (para IAs)

Ao gerar código .NET, **SEMPRE** verificar:

- [ ] Campos privados com properties públicas (`readonly` quando aplicável)
- [ ] `Result<T>` para operações que podem falhar
- [ ] Exceções específicas — nunca `throw new Exception()`
- [ ] `StringBuilder` para concatenação em loops
- [ ] `ThenBy()` para ordenação composta (nunca múltiplos `OrderBy`)
- [ ] Validação síncrona antes de `async`/`await`
- [ ] `throw;` ao invés de `throw ex;`
- [ ] Assertions em todos os testes (padrão `(expected, actual)`)
- [ ] Sem código comentado (use Git)
- [ ] Sufixos literais em maiúscula: `M`, `L`, `F`, `D`
- [ ] Enums no singular, sem sufixo "Enum"
- [ ] `IDisposable` implementado com padrão completo

### Métricas Obrigatórias no CI/CD

| Métrica | Threshold |
|---------|-----------|
| Coverage | ≥ 80% |
| Maintainability Rating | A |
| Reliability Rating | A |
| Security Rating | A |
| Bugs | 0 |
| Vulnerabilities | 0 |
