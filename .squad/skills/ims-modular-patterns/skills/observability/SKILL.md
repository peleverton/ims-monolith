---
name: observability
description: Padrões de logging estruturado e business events com Dynatrace e OpenTelemetry. Use ao implementar LogInformation/LogError em services e integrations, criar novos tipos de BusinessEvent, ou definir metadata de eventos de negócio.
---

# 10. Observability Patterns

## 📋 Visão Geral

A **observabilidade** é essencial para monitorar, diagnosticar e otimizar microsserviços em produção. Este documento cobre os padrões de **logging** e **business events** do template base .NET 8.

**Stack de Observabilidade**:
- **Dynatrace** - APM (Application Performance Monitoring)
- **OpenTelemetry** - Traces, metrics e logs
- **Structured Logging** - JSON logs estruturados
- **Business Events** - Métricas de negócio

---

## 🎯 Logging vs Business Events

### Logging (Traces)

**Propósito**: Rastrear o **fluxo de execução** e **diagnosticar problemas**.

**Características**:
- Detalha cada passo do processo
- Inclui contexto técnico (métodos, classes)
- Usado para troubleshooting
- Alta volumetria
- Retenção curta (7-30 dias)

**Quando usar**:
- Início e fim de processos
- Chamadas a integrações externas
- Transformações de dados
- Erros e exceptions
- Debug de fluxos complexos

### Business Events (BizEvent)

**Propósito**: Capturar **eventos de negócio** para **métricas e analytics**.

**Características**:
- Foca em eventos de negócio
- Facilita criação de dashboards
- Usado para KPIs e métricas
- Volumetria moderada
- Retenção longa (90-365 dias)

**Quando usar**:
- Contratação de produto (sucesso/falha)
- Transações financeiras (PIX, TED, pagamentos)
- Ações críticas do usuário (aceite de termos, cancelamentos)
- Conversões e funnel de vendas
- SLAs e performance de negócio

---

## 📝 Logging Patterns

### 1. LogInformation (Informational Logs)

**Uso**: Logs de informação **sem erros**, rastreamento de fluxo normal.

#### Pattern

```csharp
logger.LogInformation(
    "[{ClassName}].[{MethodName}] - {Message} - ClientId: {ClientId}",
    clientId);
```

#### Estrutura

| Componente | Descrição | Exemplo |
|------------|-----------|---------|
| `[{ClassName}]` | Nome da classe | `[ManageCardIntegration]` |
| `[{MethodName}]` | Nome do método | `[GetAccountLimitsVisa]` |
| `{Message}` | Descrição do que está acontecendo | `Inicio do processo`, `Dados obtidos com sucesso` |
| `ClientId: {ClientId}` | Identificador do cliente (não sensível) | `ClientId: 12345` |

#### Exemplos Práticos

**Início de processo**:
```csharp
public async Task<Result<AccountLimits>> GetAccountLimitsVisa(string clientId)
{
    _logger.LogInformation(
        "[ManageCardIntegration].[GetAccountLimitsVisa] - ClientId: {ClientId} - Inicio do processo",
        clientId);
    
    // ... lógica
}
```

**Sucesso em obter dados**:
```csharp
var response = await _httpClient.SendAsync(request);

if (response.IsSuccessStatusCode)
{
    _logger.LogInformation(
        "[ManageCardIntegration].[GetAccountLimitsVisa] - ClientId: {ClientId} - Dados obtidos com sucesso",
        clientId);
}
```

**Ação específica concluída**:
```csharp
var result = await RequestSecondCardVisa(clientId, reason);

if (result.IsSuccess)
{
    _logger.LogInformation(
        "[ManageCardIntegration].[RequestSecondCardVisa] - ClientId: {ClientId} - Reason: {Reason} - Segunda via solicitada com sucesso",
        clientId,
        reason.ToString());
}
```

**Com múltiplos identificadores**:
```csharp
_logger.LogInformation(
    "[OrderService].[GetUserOrderDetails] - UserId: {UserId} - OrderId: {OrderId} - Buscando detalhes do pedido",
    userId,
    orderId);
```

### 2. LogError (Error Logs)

**Uso**: Logs de **erro** ou **falhas**, geralmente com exception.

#### Pattern

```csharp
logger.LogError(
    ex, // Exception (opcional)
    "[{ClassName}].[{MethodName}] - {Message} - ClientId: {ClientId}",
    clientId);
```

#### Exemplos Práticos

**Erro de negócio (sem exception)**:
```csharp
if (collateralLimits == null)
{
    _logger.LogError(
        "[ManageCardService].[GetVisaCardData] - CPF: {Cpf} - FALHA CRÍTICA ao obter limites colaterais - LimiteTotal e LimiteAtivo não poderão ser definidos",
        cpf);
    
    return ManageCardErrorsMapping.GetError(
        ManageCardErrorsCode.CollateralLimitsNotFound);
}
```

**Erro com exception**:
```csharp
catch (HttpRequestException ex)
{
    _logger.LogError(
        ex,
        "[CardConfigurationService].[ToggleDirectDebit] - ClientId: {ClientId} - Error - Mensagem: {Message}",
        clientId,
        ex.Message);
    
    return CardConfigurationErrorsMapping.GetError(
        CardConfigurationErrorsCode.ToggleDirectDebitError);
}
```

**Timeout com exception**:
```csharp
catch (TaskCanceledException ex)
{
    _logger.LogError(
        ex,
        "[OrderIntegration].[GetOrders] - UserId: {UserId} - Timeout ao buscar pedidos",
        userId);
    
    return OrderErrorsMapping.GetError(
        OrderErrorsCode.GetUserOrdersTimeout);
}
```

### 3. Identificadores Não Sensíveis

**⚠️ IMPORTANTE**: Sempre incluir identificadores para rastreabilidade, mas **NUNCA** logar dados sensíveis.

#### ✅ Permitido (Não Sensível)

```csharp
// IDs numéricos
logger.LogInformation("ClientId: {ClientId}", clientId);
logger.LogInformation("ContractNumber: {ContractNumber}", contractNumber);
logger.LogInformation("TransactionId: {TransactionId}", transactionId);

// Tipos/Estados
logger.LogInformation("Status: {Status}", status);
logger.LogInformation("Reason: {Reason}", reason.ToString());

// Valores agregados (não PII)
logger.LogInformation("ContractCount: {Count}", contracts.Length);
```

#### ❌ Proibido (Dados Sensíveis)

```csharp
// ❌ NUNCA logar CPF completo
logger.LogError("CPF: {CPF}", cpf);

// ❌ NUNCA logar dados financeiros detalhados do cliente
logger.LogInformation("Balance: {Balance}", balance);

// ❌ NUNCA logar senhas, tokens, chaves
logger.LogInformation("Password: {Password}", password);
logger.LogInformation("Token: {Token}", jwtToken);

// ❌ NUNCA logar PII (dados pessoais identificáveis)
logger.LogInformation("Email: {Email}", email);
logger.LogInformation("Phone: {Phone}", phone);
```

---

## 📊 Business Events Pattern

### Interface ILogEventService

**Arquivo**: `Core/BusinessEvent/Abstractions/Services/ILogEventService.cs`

```csharp
public interface ILogEventService
{
    Task LogAsync(
        BusinessEventType eventType,
        string clientId,
        object metadata);
}
```

### Business Event Types

**Arquivo**: `Core/BusinessEvent/Model/BusinessEventType.cs`

```csharp
public enum BusinessEventType
{
    // Order Events
    OrderCreated,
    OrderUpdated,
    OrderCancelled,
    OrderShipped,
    OrderDelivered,
    OrderRetrieved,
    
    // Payment Events
    PaymentInitiated,
    PaymentCompleted,
    PaymentFailed,
    PaymentRefunded,
    PixPaymentInitiated,
    PixPaymentCompleted,
    
    // Product Events
    ProductCreated,
    ProductUpdated,
    ProductStockChanged,
    ProductCatalogRetrieved,
    
    // User Events
    UserRegistered,
    UserProfileUpdated,
    UserStatusChanged,
    UserAuthenticated
}
```

### Uso em Services

#### Evento de Sucesso

```csharp
public async Task<Result<Order[]>> GetUserOrders(string userId)
{
    var result = await _integration.GetOrders(userId);
    
    if (!result.IsSuccess)
        return result.Errors;
    
    // Log business event
    await _logEventService.LogAsync(
        BusinessEventType.OrderRetrieved,
        userId,
        new
        {
            OrderCount = result.Value.Length,
            TotalValue = result.Value.Sum(x => x.TotalAmount),
            PendingOrders = result.Value.Count(x => x.Status == "PENDING")
        });
    
    return result.Value;
}
```

#### Evento de Criação

```csharp
public async Task<Result<Order>> CreateOrder(
    string userId,
    CreateOrderRequest request)
{
    var result = await _integration.PostOrder(userId, request);
    
    if (!result.IsSuccess)
        return result.Errors;
    
    await _logEventService.LogAsync(
        BusinessEventType.OrderCreated,
        userId,
        new
        {
            OrderId = result.Value.Id,
            TotalAmount = request.TotalAmount,
            ItemsCount = request.Items.Count,
            PaymentMethod = request.PaymentMethod
        });
    
    return result.Value;
}
```

### Metadata Structure

**Boas Práticas**:

```csharp
// ✅ BOA: Metadata estruturado com dados relevantes
await _logEventService.LogAsync(
    BusinessEventType.CardActivated,
    clientId,
    new
    {
        CardType = "Visa",
        CardLast4Digits = "1234",
        ActivationChannel = "Mobile",
        Timestamp = DateTime.UtcNow
    });

// ❌ RUIM: Metadata genérico sem contexto
await _logEventService.LogAsync(
    BusinessEventType.CardActivated,
    clientId,
    new { Success = true });

// ❌ RUIM: Metadata com dados sensíveis
await _logEventService.LogAsync(
    BusinessEventType.CardActivated,
    clientId,
    new { FullCardNumber = "1234567890123456" }); // NUNCA!
```

---

## 📊 Visualização no Dynatrace

### Dashboards de Logs

```kusto
// Query para filtrar logs por clientId
fetch logs
| filter contains(content, "ClientId: 12345")
| sort timestamp desc
| limit 100

// Query para erros específicos
fetch logs
| filter log.level == "ERROR"
| filter contains(content, "[OrderService]")
| summarize count() by bin(timestamp, 1h)
```

### Dashboards de Business Events

```kusto
// Query para contar eventos por tipo
fetch bizevents
| filter event.type == "OrderCreated"
| summarize count() by bin(timestamp, 1h)

// Query para análise de funil
fetch bizevents
| filter event.type in ("OrderCreated", 
                        "OrderShipped",
                        "OrderDelivered")
| summarize count() by event.type
```

---

## ✅ Checklist: Implementar Observabilidade

### Logging

- [ ] **Início de método**: Log `LogInformation` com contexto
- [ ] **Chamadas externas**: Log antes e depois de integrations
- [ ] **Erros**: Log `LogError` com exception quando disponível
- [ ] **Identificadores**: Sempre incluir `ClientId` ou identificador não sensível
- [ ] **Padrão**: Seguir `[ClassName].[MethodName] - Message`
- [ ] **Dados sensíveis**: Validar que NÃO há PII, CPF, senhas, tokens

### Business Events

- [ ] **Criar enum**: Adicionar em `BusinessEventType`
- [ ] **Eventos de sucesso**: Logar com metadata relevante
- [ ] **Eventos de falha**: Logar com `Success = false` e `FailureReason`
- [ ] **Metadata rica**: Incluir dados para análise (valores, tipos, canais)
- [ ] **Eventos críticos**: Contratações, transações, conversões
- [ ] **Testes**: Validar que eventos são criados corretamente

### Dynatrace

- [ ] **Dashboard de logs**: Criar views por service/integration
- [ ] **Dashboard de BizEvents**: Métricas de negócio e KPIs
- [ ] **Alertas**: Configurar para erros críticos
- [ ] **Retenção**: Logs 30 dias, BizEvents 365 dias
