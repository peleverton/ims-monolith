---
updated_at: 2026-04-14T00:00:00Z
focus_area: Phase 7 — PostgreSQL Migration (US-024) and Docker Full Stack (US-025)
active_issues: []
---

# What We're Focused On

Phase 7 delivery in progress. US-024 e US-025 concluídas na branch `feat/us-024-025-postgres-docker`.

## US-024 — PostgreSQL Migration (Production Database)

- **`src/Shared/Database/DatabaseExtensions.cs`** (novo): `UseImsDatabase()` — seleciona SQLite em Development, PostgreSQL (com retry automático) nos demais ambientes. Lógica centralizada, usada por todos os módulos.
- **Refatorados** para usar `UseImsDatabase()`:
  - `AuthModuleExtensions.cs` — DbContext + `InitializeAuthModuleAsync` (EnsureCreated dev / MigrateAsync prod)
  - `IssuesModuleExtensions.cs` — idem
  - `InventoryModuleExtensions.cs` — DbContext + `IDbConnection` condicional (SqliteConnection / NpgsqlConnection)
  - `InventoryIssuesModuleExtensions.cs` — idem
  - `Shared/Outbox/OutboxExtensions.cs` — Outbox usa mesmo provider
- **`src/appsettings.Production.json`** — connection strings, RabbitMQ, JWT e OTel configurados para produção (valores via env vars).
- **NuGet adicionados**: `Npgsql.EntityFrameworkCore.PostgreSQL v9`, `AspNetCore.HealthChecks.Rabbitmq v9`.

## US-025 — Docker & Docker Compose (Full Stack Deployment)

- **`Dockerfile`** (multi-stage, raiz): restore → build → publish → runtime Alpine. Usuário não-root, HEALTHCHECK, EXPOSE 8080.
- **`.dockerignore`**: exclui bin/, obj/, logs, .db, .git, node_modules.
- **`docker-compose.yml`** (completo): app + PostgreSQL 16 + Redis 7 + RabbitMQ 3.13. Health checks em todas as dependências, `depends_on` com `condition: service_healthy`.
- **`docker-compose.observability.yml`** (overlay): Prometheus 2.51 + Grafana 10.4. Separado para não poluir o compose principal.
- **`docker-compose.dev.yml`** (mantido): RabbitMQ + Redis para dev local sem PostgreSQL.
- **`infra/prometheus/prometheus.yml`** — scrape jobs: app, postgres-exporter, rabbitmq, redis-exporter.
- **`infra/grafana/provisioning/`** — datasource (Prometheus) e dashboard IMS auto-provisionados.
- **`infra/grafana/provisioning/dashboards/ims-app.json`** — dashboard com HTTP rate, latency P95, 5xx, GC heap, DB connections, RabbitMQ queues, Outbox pending, Redis memory.
- **`.env.example`** — template com todas as variáveis necessárias.
- **`README.md`** — seção Getting Started atualizada com instruções dev/Docker/full-stack.

## Skills Available

All agents should consult `.squad/skills/ims-modular-patterns/SKILL.md` for the skill index. Key rules:
- **Always** check `code-smells` before generating .NET code
- **Always** check `security-vulnerabilities` before implementing auth/sensitive data
- Use `code-templates` for scaffolding new domains/modules
- Follow `testing-patterns` for test structure (xUnit + Moq, AAA pattern)


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
