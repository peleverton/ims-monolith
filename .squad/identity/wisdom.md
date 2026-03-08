---
last_updated: 2026-03-03T20:00:00Z
---

# Team Wisdom

Reusable patterns and heuristics learned through work. NOT transcripts — each entry is a distilled, actionable insight.

## Patterns

<!-- Append entries below. Format: **Pattern:** description. **Context:** when it applies. -->

**Pattern:** Every module follows Api/ → Application/ → Domain/ → Infrastructure/ structure with a ModuleExtensions.cs at the root. **Context:** When creating or modifying any module in the IMS Modular project.

**Pattern:** Module registration uses two-step approach: `AddXxxModule(config)` for DI and `XxxModule.Map(app)` for endpoints. **Context:** When adding new modules or modifying startup pipeline.

**Pattern:** Always consult `code-smells` skill before generating any .NET code. SonarQube rules are strict and enforced. **Context:** Before every code generation task.

**Pattern:** Railway-Oriented Programming with Result<T> pattern — no exceptions for business logic, all operations return Result<T>. **Context:** When implementing services, handlers, and integrations.

**Pattern:** Skills library in `.squad/skills/ims-modular-patterns/skills/` contains 15 detailed guides covering architecture, implementation, messaging, observability, testing, and security. Always check the index skill for the recommended consultation flow. **Context:** Before starting any implementation task.

**Pattern:** CQRS data access split — Command handlers use EF Core (change tracking, Unit of Work, aggregate integrity), Query handlers use Dapper (raw SQL, direct DTO projection, zero tracking overhead). Each module defines `IRepository` (write) and `IReadRepository` (read) interfaces. **Context:** When implementing any MediatR handler — check if it's a Command (use EF Core repo) or Query (use Dapper read repo).

**Pattern:** Never use Dapper for writes — EF Core provides change tracking, automatic transactions, cascade operations, and optimistic concurrency that would require manual implementation with Dapper. **Context:** When deciding data access strategy for a new command handler.

## Anti-Patterns

<!-- Things we tried that didn't work. **Avoid:** description. **Why:** reason. -->
