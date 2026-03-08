# Project Context

- **Owner:** Leverton Borges
- **Project:** IMS Modular — Issue Management System, modular monolith with CQRS, Clean Architecture, Minimal API
- **Stack:** C# / .NET 9 / ASP.NET Core Minimal API / EF Core 9 / SQLite / MediatR 12 / FluentValidation 11 / JWT Bearer / Swashbuckle
- **Created:** 2026-03-03T20:00:00Z

## Key Architecture

- **Modules:** Auth (authentication/authorization), Issues (issue management)
- **Module structure:** Api/ → Application/ (CQRS) → Domain/ → Infrastructure/
- **Registration pattern:** `builder.Services.AddXxxModule(config)` + `XxxModule.Map(app)`
- **Shared kernel:** Shared/Abstractions, Shared/Common, Shared/Domain
- **Root namespace:** IMS.Modular
- **Database:** SQLite in dev (ims-modular-dev.db), EF Core for data access

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

📌 Team update (2026-03-03T17:00:00Z): 15 skills imported from ims-modular covering architecture, implementation, messaging, observability, testing, code quality (SonarQube), and security (OWASP). Always consult `.squad/skills/ims-modular-patterns/SKILL.md` index before starting architecture reviews. Key skills for Lead: architecture-overview, code-smells, critical-bugs, security-vulnerabilities.

📌 Solution design produced (2026-03-03T21:00:00Z): Created `solution-design.md` with full architecture documentation — module diagram, 7 principles, module catalog, data architecture, request pipeline, 6-phase evolutionary roadmap (Baseline → Hardening → New Modules → Messaging → Production → Extraction), quality gates, and key decisions with rationale. Also created project `README.md` as the public-facing documentation.

📌 Architecture decision (2026-03-06): CQRS data access strategy defined — EF Core for Command (write) side, Dapper for Query (read) side. Each module defines IRepository (EF Core) and IReadRepository (Dapper). Updated solution design to v1.1, README, team.md, copilot-instructions.md, and wisdom.md. The inverse approach (Dapper writes + EF Core reads) was explicitly rejected — EF Core's change tracking, Unit of Work, and aggregate integrity are essential for the write side.
