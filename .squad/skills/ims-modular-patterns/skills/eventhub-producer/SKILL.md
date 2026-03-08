---
name: eventhub-producer
description: Guia para criar producers de mensageria com Azure Event Hub / Kafka (Confluent). Use ao implementar um novo producer de eventos de domínio do zero, incluindo DTO de payload, abstração, producer técnico genérico, registro no DI e serialização AOT.
---

# 14. Skill Copilot — Criar Producer de Mensageria (Event Hub)

## 🎯 Objetivo da Skill

Padronizar como o GitHub Copilot deve criar **novos producers de mensageria** com Event Hub/Kafka (Confluent), inclusive em projetos que **ainda não possuem implementação prévia**.

Esta skill descreve um padrão **reutilizável** e orientado a scaffolding, no mesmo estilo de templates dos demais arquivos da pasta `docs`.

---

## 📋 Visão Geral

Ao implementar um producer do zero, o fluxo recomendado é:

1. Criar DTO de payload.
2. Criar abstração do producer de domínio.
3. Criar producer técnico/genérico (`IEventProducer<T>`).
4. Criar producer específico por evento.
5. Registrar no DI.
6. Configurar broker/topic/destination key.
7. Garantir serialização AOT (quando aplicável).

---

## 🏗️ Estrutura sugerida

```text
src/
├── Core/
│   └── Common/Queue/
│       ├── Abstractions/
│       │   ├── IEventProducer.cs
│       │   └── I{NomeDoEvento}EventProducer.cs
│       └── Dto/
│           └── Producer{NomeDoEvento}Dto.cs
└── Infrastructure/
  ├── EventProducers/
  │   ├── EventProducer.cs
  │   └── {NomeDoEvento}EventProducer.cs
  └── DependencyInjection.cs
```

### 1) Criar DTO de payload

- Local sugerido: `src/Core/Common/Queue/Dto/`
- Convenção: `Producer{NomeDoEvento}Dto`
- Preferir `record` com propriedades `required` quando aplicável.

Template:

```csharp
namespace Core.Common.Queue.Dto;

public record Producer{NomeDoEvento}Dto
{
  public required string Id { get; init; }
  public required string CorrelationId { get; init; }
  public DateTime CreatedAtUtc { get; init; }
}
```

### 2) Criar interface específica do producer

- Local sugerido: `src/Core/Common/Queue/Abstractions/`
- Convenção: `I{NomeDoEvento}EventProducer`
- Assinatura esperada:

```csharp
public interface I{NomeDoEvento}EventProducer
{
    Task<Producer{NomeDoEvento}Dto> ProduceAsync(
        string destinationKey,
        Producer{NomeDoEvento}Dto payload);
}
```

### 3) Criar contrato técnico de producer genérico (se ainda não existir)

```csharp
using Core.Common.Queue.Dto;

namespace Core.Common.Queue.Abstractions;

public interface IEventProducer<T>
{
  Task PostMessageAsync(string queue, string topic, Message<T> message);
}
```

### 4) Implementar classe de producer em Infrastructure

- Local sugerido: `src/Infrastructure/EventProducers/`
- Convenção: `{NomeDoEvento}EventProducer`
- Injetar:
  - `IEventProducer<Producer{NomeDoEvento}Dto>`
  - `EnvironmentConfiguration`
- Publicar via `PostMessageAsync` com:
  - `queue: destinationKey`
  - `topic: environmentConfig.{NomeDoEvento}MessageBrokerTopic`
  - `message: Message<Producer{NomeDoEvento}Dto>.Create(payload, string.Empty, string.Empty)`
- Retornar o próprio payload.

Template:

```csharp
using Core.Common.Queue.Abstractions;
using Core.Common.Queue.Dto;
using Core.Generics.Models;

namespace Infrastructure.EventProducers;

public class {NomeDoEvento}EventProducer(
    IEventProducer<Producer{NomeDoEvento}Dto>? eventProducer,
    EnvironmentConfiguration environmentConfig)
: I{NomeDoEvento}EventProducer
{
    public async Task<Producer{NomeDoEvento}Dto> ProduceAsync(
        string destinationKey,
        Producer{NomeDoEvento}Dto payload)
    {
        await eventProducer!.PostMessageAsync(
            queue: destinationKey,
            topic: environmentConfig.{NomeDoEvento}MessageBrokerTopic,
            message: Message<Producer{NomeDoEvento}Dto>.Create(
                payload,
                string.Empty,
                string.Empty));

        return payload;
    }
}
```

### 5) Implementar producer técnico/genérico (se ainda não existir)

Criar uma implementação única `EventProducer<T>` responsável por:

- Publicar no broker.
- Aplicar política de resiliência (retry/circuit breaker/timeout).
- Serializar `Message<T>` com estratégia do projeto.
- Centralizar logging de sucesso/falha.

> Essa classe evita duplicação de código e garante padrão de observabilidade em todos os producers.

### 6) Registrar producer no DI

Arquivo sugerido: `src/Infrastructure/DependencyInjection.cs`

#### 6.1 AddBuildProducers

- Criar `ProducerBuilder<long, string>` para o novo evento usando `BuildProducerConfig(...)`.
- Incluir no `Dictionary<string, object>` com a chave de destino (destination key).
- Registrar `IEventProducer<Producer{NomeDoEvento}Dto>` para `EventProducer<Producer{NomeDoEvento}Dto>`.

#### 6.2 AddEventProducers

- Registrar interface específica:

```csharp
.AddScoped<I{NomeDoEvento}EventProducer, {NomeDoEvento}EventProducer>()
```

### 7) Garantir serialização AOT (obrigatório quando o projeto usa AOT)

Sem isso, o `EventProducer<T>` falha em runtime ao serializar `Message<T>`.

#### 7.1 Adicionar source generators

Arquivo sugerido: `src/Core/SerializerOptionsMapping.cs`

- Adicionar:

```csharp
[JsonSerializable(typeof(Producer{NomeDoEvento}Dto[]))]
internal partial class Producer{NomeDoEvento}DtoGenerator : JsonSerializerContext;

[JsonSerializable(typeof(Message<Producer{NomeDoEvento}Dto>[]))]
internal partial class MessageProducer{NomeDoEvento}DtoGenerator : JsonSerializerContext;
```

- Incluir os `*.Default` no `TypeInfoResolverChain`.

#### 7.2 Mapear tipo no dicionário AOT

Arquivo sugerido: `src/Core/Generics/Statics/JsonTypeInfoMapping.cs`

- Incluir:

```csharp
AddSafe(
    typeof(Message<Producer{NomeDoEvento}Dto>),
    MessageProducer{NomeDoEvento}DtoGenerator.Default
        .GetTypeInfo(typeof(Message<Producer{NomeDoEvento}Dto>)));
```

### 8) Adicionar configurações de ambiente

Arquivo sugerido: `src/Core/Generics/Models/EnvironmentConfiguration.cs`

Garantir propriedades para o novo tópico/fila seguindo padrão existente:
- `{NomeDoEvento}MessageBrokerBootstrapServers`
- `{NomeDoEvento}MessageBrokerConnectionString`
- `{NomeDoEvento}MessageBrokerTopic`
- `{NomeDoEvento}DestinationKey`

> Também atualizar origem dessas variáveis no bootstrap/configuração do ambiente da aplicação.

---

## ✅ Checklist de Pronto

- [ ] DTO criado em `Core/Common/Queue/Dto`
- [ ] Interface específica criada em `Core/Common/Queue/Abstractions`
- [ ] Interface genérica `IEventProducer<T>` criada (se não existir)
- [ ] Implementação genérica `EventProducer<T>` criada (se não existir)
- [ ] Classe `{NomeDoEvento}EventProducer` criada em `Infrastructure/EventProducers`
- [ ] Registro em `AddBuildProducers` (builder + dictionary + `IEventProducer<T>`)
- [ ] Registro em `AddEventProducers` (interface específica)
- [ ] Source generators adicionados em `SerializerOptionsMapping` (quando AOT habilitado)
- [ ] `JsonTypeInfoMapping` atualizado para `Message<Producer{NomeDoEvento}Dto>` (quando AOT habilitado)
- [ ] `EnvironmentConfiguration` atualizado com chaves do broker

---

## 🚫 Regras de Não Regressão

- Não bypassar `EventProducer<T>`: toda publicação deve passar por ele.
- Não serializar sem AOT mapping (`JsonTypeInfoMapping` + `JsonSerializerContext`) quando projeto usa AOT.
- Não hardcode de topic, bootstrap server ou connection string fora da configuração de ambiente.
- Não alterar o contrato de `ProduceAsync(string destinationKey, Producer...Dto payload)` sem necessidade arquitetural.

---

## 🧪 Validação mínima recomendada

- Build da solução sem erros.
- Smoke test do fluxo que dispara o producer.
- Verificação de log de sucesso em `EventProducer<T>`:
  - `"[EventProducer] Message {Key} sent to topic '{Topic}'"`
- Verificação de falha controlada:
  - timeout/retry/circuit breaker conforme políticas já existentes.
