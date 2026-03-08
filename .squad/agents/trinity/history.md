# Project Context

- **Owner:** Leverton Borges
- **Project:** IMS Modular — Issue Management System, modular monolith with CQRS, Clean Architecture, Minimal API
- **Stack:** C# / .NET 9 / ASP.NET Core Minimal API / EF Core 9 / SQLite / MediatR 12 / FluentValidation 11 / JWT Bearer / Swashbuckle
- **Created:** 2026-03-03T20:00:00Z

## Test Strategy

- **Unit tests:** MediatR handlers, FluentValidation validators, domain logic
- **Integration tests:** API endpoints via WebApplicationFactory, database operations
- **Pattern:** Arrange-Act-Assert
- **Mocking:** Interfaces for unit tests, real SQLite for integration tests
- **Existing modules to test:** Auth (login/register flows, JWT validation), Issues (CRUD, status transitions)

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

📌 Team update (2026-03-03T17:00:00Z): 15 skills imported from ims-modular. Key skill for testing: testing-patterns (xUnit + Moq, AAA pattern, Result<T> mocking, naming convention: {Method}_Should{Result}_When{Condition}). Also check code-smells and critical-bugs before validating implementations. Index: `.squad/skills/ims-modular-patterns/SKILL.md`
