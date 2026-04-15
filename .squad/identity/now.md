---
updated_at: 2026-04-14T00:00:00Z
focus_area: Phase 6 — Domain Event Dispatcher (US-022) and RabbitMQ Messaging (US-023)
active_issues: []
---

# What We're Focused On

Phase 6 delivery complete. All changes on branch `feat/us-022-023-domain-events-rabbitmq`.

## US-022 — Domain Event Dispatcher (BaseDbContext)

- **`src/Shared/Domain/BaseDbContext.cs`** (new): centraliza coleta + publicação de `IDomainEvent` após `SaveChangesAsync` para todos os módulos.
- **Refatorados** para herdar de `BaseDbContext` (sem duplicação):
  - `Modules/Issues/Infrastructure/IssuesDbContext.cs`
  - `Modules/Inventory/Infrastructure/InventoryDbContext.cs`
  - `Modules/InventoryIssues/Infrastructure/InventoryIssuesDbContext.cs`

## US-023 — RabbitMQ Messaging + Outbox Pattern

- **`src/Shared/Abstractions/IMessageBus.cs`**: abstração com `PublishAsync<T>` e `SubscribeAsync<T>`.
- **`src/Shared/Messaging/RabbitMqOptions.cs`**: opções tipadas (seção `"RabbitMQ"` no appsettings).
- **`src/Shared/Messaging/RabbitMqMessageBusService.cs`**: implementação usando RabbitMQ.Client v7 (async API), com retry via Polly v8, conexão singleton com `AutomaticRecoveryEnabled`.
- **`src/Shared/Messaging/MessagingExtensions.cs`**: `AddImsMessaging()` — auto-detecta RabbitMQ; usa `NullMessageBusService` quando `Host` não está configurado.
- **Outbox Pattern** (`src/Shared/Outbox/`):
  - `OutboxMessage.cs`: entidade persistida no banco antes da publicação.
  - `OutboxDbContext.cs`: DbContext para a tabela `OutboxMessages`.
  - `IOutboxService.cs` + `OutboxService.cs`: persiste mensagens no Outbox dentro da mesma transação.
  - `OutboxOptions.cs`: polling interval, batch size, max retries (seção `"Outbox"`).
  - `OutboxProcessor.cs`: `BackgroundService` que processa mensagens pendentes a cada N segundos.
  - `OutboxExtensions.cs`: `AddImsOutbox()` registra tudo no DI.
- **`docker-compose.dev.yml`** (raiz do repo): RabbitMQ 3.13-management + Redis 7 para dev local.
- **`appsettings.json`** e **`appsettings.Development.json`**: seções `RabbitMQ` e `Outbox` adicionadas.
- **`Program.cs`**: `AddImsMessaging()` e `AddImsOutbox()` registrados.
- **`.csproj`**: `RabbitMQ.Client v7` e `Polly v8` já adicionados (sessão anterior).

## Skills Available

All agents should consult `.squad/skills/ims-modular-patterns/SKILL.md` for the skill index. Key rules:
- **Always** check `code-smells` before generating .NET code
- **Always** check `security-vulnerabilities` before implementing auth/sensitive data
- Use `code-templates` for scaffolding new domains/modules
- Follow `testing-patterns` for test structure (xUnit + Moq, AAA pattern)
