---
name: eventhub-consumer
description: Guia para criar consumers de mensageria com Azure Event Hub / Kafka (Confluent). Use ao implementar um novo consumer de eventos, incluindo processamento idempotente, configuração de grupo, tratamento de DLQ e registro no DI.
---

# 15. Skill Copilot — Criar Consumer de Mensageria (Event Hub)

## 🎯 Objetivo da Skill

Padronizar como o GitHub Copilot deve criar **novos consumidores de mensageria** com Event Hub/Kafka (Confluent), inclusive em projetos que **ainda não possuem consumidores implementados**.

Esta skill é orientada a scaffolding e templates, no mesmo estilo dos demais documentos da pasta `docs`.

---

## 📋 Visão Geral

Ao implementar um consumer do zero, o fluxo recomendado é:

1. Criar classe `BackgroundService` dedicada ao consumo.
2. Configurar `ConsumerConfig` com credenciais e segurança.
3. Assinar (`Subscribe`) o tópico do evento.
4. Implementar loop de consumo com processamento e commit.
5. Tratar falhas de consumo/processamento com logging.
6. Registrar o consumer como hosted service no startup.
7. Configurar variáveis de ambiente para broker/topic/group.

---

## 🏗️ Estrutura sugerida

```text
src/
├── Api/
│   ├── Consumers/
│   │   └── {NomeDoEvento}Consumer.cs
│   └── Program.cs
└── Core/
    └── Generics/Models/
        └── EnvironmentConfiguration.cs
```

### 1) Criar classe de consumer como BackgroundService

- Local sugerido: `src/Api/Consumers/`
- Convenção: `{NomeDoEvento}Consumer`
- Herança: `BackgroundService`
- Injetar dependências de negócio necessárias + `EnvironmentConfiguration` + `ILogger<{Consumer}>`.
- Criar um campo `IConsumer<long, string>` e inicializar via método `BuildConsumer()`.
- Manter segundo construtor recebendo `IConsumer<long, string>` para facilitar testes unitários.

Template:

```csharp
using Confluent.Kafka;
using Core.Common.Queue.Dto;
using Core.Generics.Models;

namespace Api.Consumers;

public class {NomeDoEvento}Consumer : BackgroundService
{
    private readonly EnvironmentConfiguration _configuration;
    private readonly ILogger<{NomeDoEvento}Consumer> _logger;
    private readonly IConsumer<long, string> _consumer;

    public {NomeDoEvento}Consumer(
        EnvironmentConfiguration configuration,
        ILogger<{NomeDoEvento}Consumer> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _consumer = BuildConsumer();
    }

    public {NomeDoEvento}Consumer(
        EnvironmentConfiguration configuration,
        ILogger<{NomeDoEvento}Consumer> logger,
        IConsumer<long, string> consumer)
    {
        _configuration = configuration;
        _logger = logger;
        _consumer = consumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(stoppingToken);
                _logger.LogInformation("[{NomeDoEvento}Consumer] Consumed message: {Message}", consumeResult.Message.Value);

                var message = Message<{Payload}>.Deserialize(consumeResult.Message.Value);
                var payload = message.Body;

                // Regras de negócio / processamento
                // var result = await service.ProcessAsync(payload, stoppingToken);

                // Em sucesso: commit explícito
                _consumer.Commit(consumeResult);
            }
            catch (ConsumeException e)
            {
                _logger.LogError("[{NomeDoEvento}Consumer] Consume error: {Error}", e.Error.Reason);
            }
            catch (Exception e)
            {
                _logger.LogError("[{NomeDoEvento}Consumer] Processing error: {Error}", e);
            }
        }
    }

    private IConsumer<long, string> BuildConsumer()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration.{NomeDoEvento}MessageBrokerBootstrapServers,
            SaslPassword = _configuration.{NomeDoEvento}MessageBrokerConnectionString,
            GroupId = _configuration.{NomeDoEvento}MessageBrokerConsumerGroup,
            SecurityProtocol = SecurityProtocol.SaslSsl,
            SaslMechanism = SaslMechanism.Plain,
            SocketTimeoutMs = _configuration.MessageBrokerSocketTimeoutMs,
            SessionTimeoutMs = _configuration.MessageBrokerSessionTimeoutMs,
            SaslUsername = _configuration.MessageBrokerSaslUsername,
            SslCaLocation = _configuration.MessageBrokerCaCertLocation,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            BrokerVersionFallback = "1.0.0",
        };

        var consumer = new ConsumerBuilder<long, string>(config)
            .SetKeyDeserializer(Deserializers.Int64)
            .SetValueDeserializer(Deserializers.Utf8)
            .Build();

        consumer.Subscribe(_configuration.{NomeDoEvento}MessageBrokerTopic);
        return consumer;
    }
}
```

### 2) Padrão do loop de consumo

No `ExecuteAsync`:

1. Consumir mensagem com `_consumer.Consume(stoppingToken)`.
2. Desserializar payload usando `Message<T>.Deserialize(...)`.
3. Executar regra de negócio (serviço de domínio/aplicação).
4. Se sucesso, efetuar `_consumer.Commit(consumeResult)`.
5. Se falha de negócio, **não commitar** para permitir reprocessamento.
6. Tratar `ConsumeException` e exceções genéricas com logging.

### 3) Configuração de commit (escolher 1 padrão e manter consistência)

Use apenas uma estratégia por consumer:

- **Manual commit recomendado para controle fino**
    - `EnableAutoCommit = false`
    - Commit explícito apenas após sucesso do processamento
- **Auto commit (somente se aceito pelo cenário)**
    - `EnableAutoCommit = true`
    - Avaliar impacto em reprocessamento e garantia de entrega

> Em cenários de processamento crítico, prefira commit manual.

### 4) Registrar o consumer no startup

Arquivo sugerido: `src/Api/Program.cs`

Adicionar no pipeline de serviços:

```csharp
.AddHostedService<{NomeDoEvento}Consumer>()
```

### 5) Configurações necessárias em EnvironmentConfiguration

Arquivo sugerido: `src/Core/Generics/Models/EnvironmentConfiguration.cs`

Garantir propriedades para o tópico consumido:
- `{NomeDoEvento}MessageBrokerBootstrapServers`
- `{NomeDoEvento}MessageBrokerConnectionString`
- `{NomeDoEvento}MessageBrokerConsumerGroup`
- `{NomeDoEvento}MessageBrokerTopic`

Além das configurações comuns de broker já usadas no projeto:
- `MessageBrokerSaslUsername`
- `MessageBrokerCaCertLocation`
- `MessageBrokerSocketTimeoutMs`
- `MessageBrokerSessionTimeoutMs`

---

## ✅ Checklist de Pronto

- [ ] Classe `{NomeDoEvento}Consumer` criada em `Api/Consumers`
- [ ] Dois construtores (produção + teste) implementados
- [ ] `BuildConsumer()` com `ConsumerConfig` no padrão do projeto
- [ ] `Subscribe(...)` no tópico correto
- [ ] Loop `ExecuteAsync` com desserialização `Message<T>.Deserialize(...)`
- [ ] Estratégia de commit definida e consistente (`EnableAutoCommit` x commit manual)
- [ ] Tratamento de erro com logs
- [ ] Registro em `Program.cs` com `AddHostedService<...>()`
- [ ] Chaves de configuração presentes em `EnvironmentConfiguration`

---

## 🚫 Regras de Não Regressão

- Não consumir mensagens fora de `BackgroundService` para fluxo contínuo.
- Não hardcode de bootstrap server, connection string, topic ou group id.
- Não remover construtor alternativo com `IConsumer<long, string>` (suporte a testes).
- Não remover logging operacional de consumo/sucesso/erro.
- Não alterar protocolo de segurança (`SaslSsl` + `SaslMechanism.Plain`) sem alinhamento de infraestrutura.
- Não misturar commit manual com `EnableAutoCommit = true` sem justificativa técnica e documentação.

---

## 🧪 Validação mínima recomendada

- Build da solução sem erros.
- Aplicação sobe com hosted service ativo.
- Consumer conectado ao tópico sem exceções de autenticação TLS/SASL.
- Mensagem válida processada com log de sucesso.
- Cenário de falha de negócio sem commit, permitindo reprocessamento.

---

## 📝 Observação de adaptação

Se o projeto já possuir implementações existentes, adapte nomes de namespaces, contratos e organização de pastas para o padrão local, sem alterar os princípios desta skill.
