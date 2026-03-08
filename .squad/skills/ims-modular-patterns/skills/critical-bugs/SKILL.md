---
name: critical-bugs
description: Catálogo de bugs críticos detectados pelo SonarQube que causam falhas em runtime. Use ao revisar código existente, corrigir bugs ou validar implementações antes de merge.
---

# 12. Bugs Críticos e Erros Lógicos

## 📋 Visão Geral

Este documento cataloga **bugs detectados pelo SonarQube** que representam **erros lógicos** e **comportamentos incorretos** que causam falhas em runtime.

> **Bugs são BLOQUEADORES de merge.** Nunca devem chegar em produção.

### Diferença: Bug vs Code Smell

| Aspecto | Bug 🐛 | Code Smell 👃 |
|---------|--------|---------------|
| **Impacto** | Causa falha em runtime | Reduz qualidade |
| **Urgência** | Corrigir **imediatamente** | Corrigir antes de merge |
| **Exemplo** | NullReferenceException | Método com muitos parâmetros |

---

## 📊 Resumo dos Bugs por Categoria

| Categoria | Severidade | Exemplos |
|-----------|------------|----------|
| 🔴 Segurança e CWE | Crítica | Null dereference, IDisposable leak, nullable access |
| ⚠️ Lógica e Fluxo | Alta | Branches idênticos, condições duplicadas, loops infinitos |
| 🔧 Async/Threading | Crítica | Async void, Task null, lock incorreto |
| 🎯 Operadores | Alta | `=+` vs `+=`, NaN, self-assignment |
| 🏗️ Implementação | Alta | Properties errados, exceptions não lançadas |
| 📋 Coleções/Strings | Média | Modifica coleções em iteração, format strings |

---

## 🔴 1. Bugs Críticos - Segurança e CWE

| Bug | Severidade |
|-----|------------|
| Null pointers should not be dereferenced | 🔴 Crítica |
| "IDisposables" should be disposed | 🔴 Crítica |
| Empty nullable value should not be accessed | 🔴 Crítica |
| "Equals()" and "GetHashCode()" should be overridden in pairs | 🔴 Crítica |
| "ToString()" method should not return null | 🔴 Alta |
| Results of integer division should not be assigned to floating point variables | 🔴 Alta |

### ❌ Null Pointer Dereference (EVITAR)

```csharp
// 💥 NullReferenceException - GetCustomer pode retornar null
public decimal CalculateDiscount(Order order)
{
    Customer customer = GetCustomer(order.CustomerId);
    if (customer.IsVip) // 💥 Crash se customer for null!
        return order.Total * 0.15m;
    return 0;
}

// 💥 NullReferenceException - FirstOrDefault pode ser null
public string GetCustomerName(Guid customerId)
{
    var customer = _customers.FirstOrDefault(c => c.Id == customerId);
    return customer.Name; // 💥 Crash se não encontrado!
}
```

```csharp
// ✅ CORRETO - Null check adequado
public decimal CalculateDiscount(Order order)
{
    var customer = GetCustomer(order.CustomerId);
    if (customer == null) return 0;
    return customer.IsVip ? order.Total * 0.15m : 0;
}

// ✅ MELHOR - Null-conditional operator
public decimal CalculateDiscount(Order order)
    => GetCustomer(order.CustomerId)?.IsVip == true ? order.Total * 0.15m : 0;
```

### ❌ IDisposable Não Disposed (EVITAR)

```csharp
// 💥 Resource leak - handles ficam abertos
public async Task<string> ReadFileAsync(string path)
{
    var stream = File.OpenRead(path);      // ❌ Não disposed
    var reader = new StreamReader(stream); // ❌ Não disposed
    return await reader.ReadToEndAsync();
}

// 💥 Socket exhaustion - um HttpClient por chamada
public HttpResponseMessage CallApi()
{
    var client = new HttpClient(); // ❌ NUNCA criar HttpClient a cada chamada!
    return client.GetAsync("https://api.example.com").Result;
}
```

```csharp
// ✅ CORRETO - using garante dispose
public async Task<string> ReadFileAsync(string path)
{
    using var stream = File.OpenRead(path);
    using var reader = new StreamReader(stream);
    return await reader.ReadToEndAsync();
}

// ✅ MELHOR - Framework gerencia recursos
public async Task<string> ReadFileAsync(string path)
    => await File.ReadAllTextAsync(path);

// ✅ CORRETO - HttpClient via IHttpClientFactory
public class ApiClient(HttpClient httpClient)
{
    public async Task<HttpResponseMessage> CallAsync()
        => await httpClient.GetAsync("https://api.example.com");
}
// No DI: services.AddHttpClient<ApiClient>();
```

### ❌ Nullable Value Access (EVITAR)

```csharp
// 💥 InvalidOperationException se discount for null
decimal? discount = CalculateDiscount(order);
var finalTotal = order.Total - discount.Value; // 💥 .Value sem verificar HasValue

// 💥 Crash se nunca fez login
DateTime? lastLogin = user.LastLoginDate;
return lastLogin.Value; // 💥 Exception se null
```

```csharp
// ✅ CORRETO - Null-coalescing operator
var finalTotal = order.Total - (discount ?? 0);
return user.LastLoginDate ?? DateTime.MinValue;

// ✅ CORRETO - GetValueOrDefault
var finalTotal = order.Total - discount.GetValueOrDefault(0);
```

### ❌ Equals/GetHashCode Inconsistentes (EVITAR)

```csharp
// 💥 Quebra HashSet, Dictionary - override apenas Equals sem GetHashCode
public class Customer
{
    public Guid Id { get; set; }
    
    public override bool Equals(object obj)
        => obj is Customer other && Id == other.Id;
    
    // ❌ GetHashCode não overridden!
}

// Resultado:
var set = new HashSet<Customer> { customer1 };
set.Add(customer2); // 💥 Adiciona mesmo sendo "igual"!
Console.WriteLine(set.Count); // 2 (deveria ser 1)
```

```csharp
// ✅ CORRETO - Override ambos
public class Customer
{
    public Guid Id { get; set; }
    
    public override bool Equals(object obj)
        => obj is Customer other && Id == other.Id;
    
    public override int GetHashCode() => Id.GetHashCode(); // ✅
}

// ✅ MELHOR - record (Equals/GetHashCode automáticos)
public record Customer(Guid Id, string Name);
```

---

## ⚠️ 2. Bugs de Lógica e Fluxo

| Bug | Descrição |
|-----|-----------|
| All branches should not have exactly the same implementation | Condicional inútil |
| Identical expressions should not be used on both sides | `x == x` é sempre true |
| Related "if/else if" should not have the same condition | Código morto |
| Loop update clause should move counter in right direction | Loop infinito |
| Recursion should not be infinite | Sem caso base |

```csharp
// ❌ INCORRETO - Todas as branches iguais
public string GetOrderStatus(Order order)
{
    if (order.IsPaid) return "Processed"; // ❌ Mesmo resultado
    else if (order.IsShipped) return "Processed"; // ❌ Mesmo resultado
    else return "Processed"; // ❌ if é inútil!
}

// ✅ CORRETO - Lógica diferenciada
public string GetOrderStatus(Order order)
{
    if (order.IsShipped) return "Shipped";
    if (order.IsPaid) return "Paid";
    return "Pending";
}

// ❌ INCORRETO - Expressões idênticas (sempre true/0)
if (order.Total > 0 && order.Total > 0)  // 💥 Duplicado
return value - value;                     // 💥 Sempre 0

// ✅ CORRETO
if (order.Total > 0 && order.Items.Count > 0)
return calculatedValue;

// ❌ INCORRETO - Loop infinito
for (int i = items.Count - 1; i >= 0; i++) // 💥 i++ deveria ser i--
    ProcessItem(items[i]);

// ✅ CORRETO
for (int i = items.Count - 1; i >= 0; i--) // ✅
    ProcessItem(items[i]);
```

---

## 🔧 3. Bugs de Async/Threading

| Bug | Descrição |
|-----|-----------|
| "async void" methods should not be used | Exceptions silenciosas, sem await |
| Tasks should not be created without starting | Task nunca executa |
| `Thread.Sleep` should not be used in async context | Bloqueia thread-pool |
| "lock" should not be used on "this", string or Type | Deadlock, contention |

```csharp
// ❌ INCORRETO - async void (exceptions são silenciosas)
public async void ProcessOrderAsync(Order order)
{
    await _service.ProcessAsync(order);
    // 💥 Qualquer exception é perdida silenciosamente!
}

// ✅ CORRETO - async Task
public async Task ProcessOrderAsync(Order order)
{
    await _service.ProcessAsync(order);
}

// ❌ INCORRETO - Task não iniciada
public Task<Order> GetOrderAsync(Guid id)
{
    return new Task<Order>(() => _repository.GetById(id)); // 💥 Nunca executa!
}

// ✅ CORRETO
public Task<Order> GetOrderAsync(Guid id)
    => Task.Run(() => _repository.GetById(id));
// OU melhor:
public async Task<Order> GetOrderAsync(Guid id)
    => await _repository.GetByIdAsync(id);

// ❌ INCORRETO - Thread.Sleep em contexto async
public async Task WaitAndProcessAsync()
{
    Thread.Sleep(1000); // 💥 Bloqueia thread do thread-pool inteiro!
    await ProcessAsync();
}

// ✅ CORRETO
public async Task WaitAndProcessAsync()
{
    await Task.Delay(1000); // ✅ Libera thread durante espera
    await ProcessAsync();
}

// ❌ INCORRETO - Lock em string (contention global)
lock ("my-lock") // 💥 Strings são internadas - contention entre assemblies!
{
    // ...
}

// ✅ CORRETO
private readonly object _lock = new();
lock (_lock) { /* ... */ }
```

---

## 🎯 4. Bugs de Operadores

| Bug | Descrição |
|-----|-----------|
| `=+` should not be used instead of `+=` | Sobreescreve ao invés de somar |
| Floating point numbers should not be tested for equality | NaN != NaN |
| Non-integer types should not be used as enum underlying types | Compilação/runtime inconsistente |
| Self-assignment should not be used | x = x (sempre inútil) |

```csharp
// ❌ INCORRETO - =+ vs +=
int total = 0;
foreach (var item in items)
    total =+ item.Price; // 💥 Subtrai (unário +)! Deveria ser +=

// ✅ CORRETO
foreach (var item in items)
    total += item.Price; // ✅

// ❌ INCORRETO - Float equality (NaN != NaN)
double result = CalculateRatio(a, b);
if (result == double.NaN) // 💥 Nunca true! NaN != NaN em IEEE 754
    HandleInvalidResult();

// ✅ CORRETO
if (double.IsNaN(result)) // ✅
    HandleInvalidResult();

// ❌ INCORRETO - Self-assignment
this.customerId = this.customerId; // 💥 Inútil - typo provável
```

---

## 🏗️ 5. Bugs de Implementação de Classe

| Bug | Descrição |
|-----|-----------|
| "GetHashCode()" should not reference mutable fields | Hash muda, quebra dicionários |
| Serialization constructors should be non-public | Serialização segura |
| The same value should not be assigned to a property on both branches | Lógica incorreta |
| "abstract" classes should have explicit constructors | Evita defaults inesperados |

```csharp
// ❌ INCORRETO - GetHashCode com campo mutável
public class Order
{
    public string Status { get; set; } // ❌ Mutável
    
    public override int GetHashCode()
        => Status.GetHashCode(); // 💥 Hash muda quando Status muda!
}
// Resultado: order colocado em HashSet sai do lugar quando Status muda!

// ✅ CORRETO - GetHashCode apenas com campos imutáveis
public class Order
{
    public Guid Id { get; init; }  // ✅ Imutável
    public string Status { get; set; }
    
    public override int GetHashCode()
        => Id.GetHashCode(); // ✅ Id nunca muda
}

// ❌ INCORRETO - Mesmo valor em ambas as branches do if
if (isUrgent)
    order.Priority = 1; // ❌ Mesmo valor
else
    order.Priority = 1; // ❌ Mesmo valor - lógica incorreta!

// ✅ CORRETO
order.Priority = isUrgent ? 1 : 3;
```

---

## 📋 6. Bugs de Coleções e Strings

| Bug | Descrição |
|-----|-----------|
| "Collections.emptyList()" should not be returned | Mutabilidade inesperada |
| Collection-specific methods should be preferred | Melhor performance |
| Modifying a collection while iterating it is error-prone | InvalidOperationException |
| Exception types should not be the same in multiple catch clauses | Código morto |

```csharp
// ❌ INCORRETO - Modificar coleção durante iteração
foreach (var item in order.Items)
{
    if (item.IsInvalid)
        order.Items.Remove(item); // 💥 InvalidOperationException!
}

// ✅ CORRETO - Crie nova lista ou use Where
var validItems = order.Items.Where(x => !x.IsInvalid).ToList();
// OU
var itemsToRemove = order.Items.Where(x => x.IsInvalid).ToList();
foreach (var item in itemsToRemove)
    order.Items.Remove(item);

// ❌ INCORRETO - Catch duplicado (segundo nunca executa)
try { /* ... */ }
catch (Exception ex) { _logger.LogError(ex, "Error"); }
catch (Exception ex) { HandleSpecificError(ex); } // 💥 Nunca executa!

// ✅ CORRETO - Exceções específicas primeiro
try { /* ... */ }
catch (HttpRequestException ex) { HandleHttpError(ex); } // Específica primeiro
catch (Exception ex) { _logger.LogError(ex, "Unexpected"); }
```

---

## ✅ Checklist Pre-Merge (Anti-Bug)

- [ ] Sem acesso direto a `nullable.Value` sem verificar `HasValue`
- [ ] Todos `IDisposable` em `using`
- [ ] `HttpClient` via `IHttpClientFactory` (nunca `new HttpClient()`)
- [ ] `Equals()` e `GetHashCode()` overridden juntos
- [ ] Sem `async void` (somente `async Task`)
- [ ] Sem `Thread.Sleep` em contexto async (use `Task.Delay`)
- [ ] Sem lock em `string`, `this` ou `Type`
- [ ] `+=` não `=+` para acumulação
- [ ] `double.IsNaN()` não `== double.NaN`
- [ ] GetHashCode usa apenas campos imutáveis
- [ ] Sem modificação de coleção durante iteração
- [ ] Exceções específicas antes de genéricas no catch
