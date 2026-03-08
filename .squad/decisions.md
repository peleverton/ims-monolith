# Decisions Log

<!-- Append-only. Newest entries at the bottom. Scribe merges from decisions/inbox/. -->

### 2026-03-03: Team initialized
**By:** Squad (Coordinator)
**What:** AI team created with The Matrix universe casting. Members: Morpheus (Lead), Neo (Backend Dev), Trinity (Tester), Scribe, Ralph. Project: IMS Modular — C#/.NET 9 modular monolith with CQRS, Clean Architecture, Minimal API.
**Why:** Project kickoff — establishing AI team for IMS Modular development.

### 2026-03-03: Architecture conventions established
**By:** Squad (Coordinator)
**What:** Each module follows the pattern: Api/ (endpoints), Application/ (CQRS handlers, validators), Domain/ (entities, value objects), Infrastructure/ (EF Core, repos). Module registration via AddXxxModule() extension methods. Endpoint mapping via XxxModule.Map(app). Shared kernel in Shared/ directory.
**Why:** Captured from existing codebase to ensure all agents follow established patterns.

### 2026-03-03: Skills library imported from ims-modular
**By:** Squad (Coordinator)
**What:** Imported 15 detailed skills from the ims-modular project into `.squad/skills/ims-modular-patterns/skills/`. Skills cover: architecture-overview, api-project-patterns, core-project-patterns, minimal-api-modules, error-mapping, source-generators-aot, infrastructure-integrations, code-templates, eventhub-producer, eventhub-consumer, observability, testing-patterns, code-smells, critical-bugs, security-vulnerabilities. Updated SKILL.md index with full cross-reference table and recommended consultation flow.
**Why:** Skills from the original project provide deep domain knowledge on .NET patterns, SonarQube rules, testing conventions, AOT setup, messaging, and security best practices. All agents must consult relevant skills before generating code.

### 2026-03-03: Project scope documented from README
**By:** Squad (Coordinator)
**What:** Updated team.md with full project scope: all endpoints (Auth, Issues, System), seed data (admin@ims.com / Admin@123), localhost URL (5049), package versions, root namespace (IMS.Modular), target framework (net9.0), BCrypt for password hashing.
**Why:** Complete project context enables all agents to work with accurate information about the existing codebase.

### 2026-03-03: Solution design and README produced by Morpheus
**By:** Morpheus (Lead / Architect)
**What:** Created evolutionary solution design (`.squad/agents/morpheus/solution-design.md`) covering: architecture overview with diagrams, 7 architectural principles, module catalog (Auth, Issues, Shared Kernel), data architecture, request pipeline, 6-phase evolutionary roadmap, quality gates, key decisions with rationale, and non-functional requirements. Also created comprehensive `README.md` at project root covering: architecture, project structure, tech stack, getting started, full API reference, authentication flow, module conventions, domain model, development guide, testing conventions, AI team (Squad) usage, and roadmap.
**Why:** The ims-monolith repository lacked a solution design and project README. These documents serve as the source of truth for all agents and contributors, grounding development in the architecture established by ims-modular while defining a clear evolutionary path.

### 2026-03-06: CQRS data access strategy — EF Core writes + Dapper reads
**By:** Morpheus (Lead / Architect)
**What:** Adopted Dapper for the Query (read) side of CQRS, keeping EF Core for the Command (write) side. Each module will have: `IRepository` (EF Core — write) and `IReadRepository` (Dapper — read). Query handlers use raw SQL with direct DTO projection. Command handlers use EF Core for change tracking, transactions, and aggregate persistence.
**Why:** EF Core excels at writes (change tracking, Unit of Work, cascade, concurrency control) but adds unnecessary overhead to reads (tracking, entity materialization, LINQ translation). Dapper provides raw SQL performance, direct DTO projection, and zero tracking overhead for queries. This is the industry-standard CQRS data access split. The inverse (Dapper writes + EF Core reads) was explicitly rejected because Dapper loses aggregate integrity, automatic transactions, and cascade on the write side.

### 2026-03-07: IMS legacy feature alignment — full scope import
**By:** Morpheus (Lead / Architect)
**What:** Analyzed the IMS legacy project (more advanced than ims-modular) and imported its complete feature set into ims-monolith's architecture and roadmap. Changes:
1. **Solution Design** — Updated module catalog from 3 sections (Auth, Issues, Shared Kernel) to 9 sections (Auth, Issues, Inventory, Inventory Issues, Analytics, User Management, Notifications, Shared Kernel, Cross-Cutting Infrastructure). Added 93+ planned API endpoints.
2. **Roadmap** — Expanded from 6 phases to 8 phases to properly sequence: Foundation → Cross-Cutting Hardening → Inventory Module → Inventory Issues + Analytics → User Management + Notifications → Integration & Messaging → Production → Service Extraction.
3. **Data Architecture** — Updated to include 5 DbContexts (Auth, Issues, Inventory, InventoryIssue, User), Redis distributed cache, and RabbitMQ messaging.
4. **Domain Events** — Documented 25+ domain events across Issues (5), Inventory (15), Users (8) for cross-module communication.
5. **Cross-Cutting** — Added MediatR pipeline behaviors (Validation, Logging, Caching), middleware stack (CorrelationId, Metrics, Performance, UserContext), Redis caching strategy, and observability stack.
6. **README** — Updated project title to "Issue & Inventory Management System", expanded architecture diagram, full API reference (~93 endpoints), complete domain model (Issue, User, Product, Supplier, Location, StockMovement, InventoryIssue), cross-cutting concerns section, updated tech stack with Redis/SignalR/RabbitMQ/OpenTelemetry/Serilog.
7. **Team.md** — Updated project context, stack, module table with phases, cross-cutting components.
8. **Key Decisions** — Added 4 new decisions: Redis caching, SignalR notifications, RabbitMQ messaging, domain events for cross-module communication.
**Why:** The IMS legacy project has significantly more features than ims-modular baseline. Importing this scope ensures ims-monolith is planned to reach feature parity with: complete inventory management (products, stock movements, suppliers, locations), inventory analytics (13 endpoints), inventory issue tracking, comprehensive issue/user analytics with workload-based auto-assignment, user management with full lifecycle and domain events, multi-channel notifications (SignalR, SMTP, message bus), and production-grade infrastructure (Redis, RabbitMQ, OpenTelemetry, Prometheus, Grafana).
