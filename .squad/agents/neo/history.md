# Project Context

- **Owner:** Leverton Borges
- **Project:** IMS Modular — Issue Management System, modular monolith with CQRS, Clean Architecture, Minimal API
- **Stack:** C# / .NET 9 / ASP.NET Core Minimal API / EF Core 9 / SQLite / MediatR 12 / FluentValidation 11 / JWT Bearer / Swashbuckle
- **Created:** 2026-03-03T20:00:00Z

## Key Patterns

- **Module structure:** Api/ (endpoints) → Application/ (commands, queries, handlers, validators) → Domain/ (entities, VOs) → Infrastructure/ (EF Core, repos)
- **Registration:** `builder.Services.AddXxxModule(config)` in Program.cs + `XxxModule.Map(app)` for endpoints
- **CQRS:** MediatR — IRequest<TResponse> for commands/queries, IRequestHandler<TRequest, TResponse> for handlers
- **Validation:** FluentValidation — AbstractValidator<TRequest>, registered via assembly scanning
- **Endpoints:** Minimal API with .WithName(), .WithTags(), typed Results
- **Namespace:** IMS.Modular.Modules.{Module}.{Layer}
- **Existing modules:** Auth (JWT login/register), Issues (CRUD + status management)

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

📌 Team update (2026-03-03T17:00:00Z): 15 skills imported from ims-modular. Before writing code, ALWAYS check: code-smells (SonarQube proibições), code-templates (scaffolding), core-project-patterns (services/DTOs/validators). For new modules: architecture-overview → code-templates → core-project-patterns → minimal-api-modules → error-mapping → testing-patterns. For integrations: infrastructure-integrations → source-generators-aot → observability. Index: `.squad/skills/ims-modular-patterns/SKILL.md`
