# Team Roster

> Issue Management System — Modular Monolith with Minimal API, CQRS, and Clean Architecture

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs and reviewer gates. Does not generate domain artifacts. |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Morpheus | Lead | `.squad/agents/morpheus/charter.md` | ✅ Active |
| Neo | Backend Dev | `.squad/agents/neo/charter.md` | ✅ Active |
| Trinity | Tester | `.squad/agents/trinity/charter.md` | ✅ Active |
| Scribe | Session Logger | `.squad/agents/scribe/charter.md` | 📋 Silent |
| Ralph | Work Monitor | — | 🔄 Monitor |

## Coding Agent

<!-- copilot-auto-assign: false -->

| Name | Role | Charter | Status |
|------|------|---------|--------|
| @copilot | Coding Agent | — | 🤖 Coding Agent |

### Capabilities

**🟢 Good fit — auto-route when enabled:**
- Bug fixes with clear reproduction steps
- Test coverage (adding missing tests, fixing flaky tests)
- Lint/format fixes and code style cleanup
- Dependency updates and version bumps
- Small isolated features with clear specs
- Boilerplate/scaffolding generation
- Documentation fixes and README updates

**🟡 Needs review — route to @copilot but flag for squad member PR review:**
- Medium features with clear specs and acceptance criteria
- Refactoring with existing test coverage
- API endpoint additions following established patterns
- Migration scripts with well-defined schemas
- New MediatR handler following existing CQRS pattern

**🔴 Not suitable — route to squad member instead:**
- Architecture decisions and system design
- Multi-module integration requiring coordination
- Ambiguous requirements needing clarification
- Security-critical changes (JWT auth, encryption, access control)
- Performance-critical paths requiring benchmarking
- Changes to Shared kernel or cross-cutting concerns
- New module creation (requires architectural alignment)

## Project Context

- **Owner:** Leverton Borges
- **Stack:** C# / .NET 9 / ASP.NET Core Minimal API / EF Core 9 (writes) / Dapper (reads) / SQLite (dev) / PostgreSQL (prod) / MediatR 12 / FluentValidation 11 / JWT Bearer / BCrypt / Redis / SignalR / Serilog / OpenTelemetry / Prometheus / RabbitMQ
- **Description:** Issue & Inventory Management System — monólito modular com CQRS, Clean Architecture, 6+ módulos de negócio, domain events, caching distribuído, notificações real-time, analytics avançado, e observabilidade completa
- **Root Namespace:** IMS.Modular
- **Target Framework:** net9.0
- **Database:** SQLite (dev), PostgreSQL (prod), EF Core for writes, Dapper for reads
- **Created:** 2026-03-03T20:00:00Z
- **URL:** `http://localhost:5049`

### Modules

| Module | Description | Phase | Endpoints |
|--------|-------------|-------|-----------|
| Auth | JWT authentication/authorization (register, login) | ✅ Phase 1 | POST `/api/auth/register`, POST `/api/auth/login` |
| Issues | Issue management CRUD + status + comments (requires auth) | ✅ Phase 1 | 7 endpoints under `/api/issues` |
| Inventory | Products, stock movements, suppliers, locations, inventory analytics | 📋 Phase 3 | ~35 endpoints under `/api/inventory/*` |
| Inventory Issues | Inventory-specific issue tracking with full lifecycle | 📋 Phase 4 | 16 endpoints under `/api/inventory-issues` |
| Analytics | Issue analytics, user workload, dashboard, export | 📋 Phase 4 | 14 endpoints under `/api/analytics/*` |
| User Management | Full user CRUD, profiles, roles, activate/deactivate | 📋 Phase 5 | 13 endpoints under `/api/users` |
| Notifications | SignalR (real-time), SMTP email, message bus | 📋 Phase 5 | SignalR hub + internal services |

### Cross-Cutting (Phase 2)
| Component | Description |
|-----------|-------------|
| MediatR Behaviors | ValidationBehavior, LoggingBehavior, CachingBehavior |
| Middleware | CorrelationId, Metrics, Performance, UserContext |
| Caching | Redis distributed cache + Output Caching |
| Observability | Serilog + OpenTelemetry + Prometheus |
| Health Checks | DB, Memory, Disk, Cache |

### System Endpoints
| Endpoint | Description |
|----------|-------------|
| GET `/api/ping` | Health check (pong) |
| GET `/api/status` | Service status, version, modules, environment |

### Seed Data
- **Roles:** Admin, User
- **Admin User:** `admin@ims.com` / `Admin@123`

### Skills Reference
O projeto possui 15 skills detalhadas em `.squad/skills/ims-modular-patterns/skills/`:
- **Arquitetura:** architecture-overview, api-project-patterns, core-project-patterns, minimal-api-modules
- **Implementação:** error-mapping, source-generators-aot, infrastructure-integrations, code-templates
- **Mensageria:** eventhub-producer, eventhub-consumer
- **Qualidade:** observability, testing-patterns, code-smells, critical-bugs, security-vulnerabilities

Consultar o índice em `.squad/skills/ims-modular-patterns/SKILL.md` para saber qual skill usar em cada contexto.
