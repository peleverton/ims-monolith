---
updated_at: 2026-04-14T00:00:00Z
focus_area: Phase 5 complete — User Management, Notifications, Domain Events delivered. Next: Phase 6 (RabbitMQ + Domain Event Dispatcher) or Phase 7 (PostgreSQL + Docker).
active_issues: []
last_pr: "https://github.com/peleverton/ims-monolith/pull/34"
completed_us: [US-001, US-002, US-003, US-004, US-005, US-006, US-007, US-008, US-009, US-010, US-011, US-012, US-013, US-014, US-015, US-016, US-017, US-018, US-019, US-020, US-021]
next_us: [US-022, US-023, US-024, US-025]
---

# What We're Focused On

**Phase 5 — User Management + Notifications — ✅ COMPLETE**

PR #34 merged. US-019, US-020, US-021 fechadas e movidas para Done no GitHub Project.

## Fases Completas

| Phase | US | Status |
|-------|----|--------|
| 1 — Foundation | US-001, US-002 | ✅ Done |
| 2 — Cross-Cutting | US-003 a US-009 | ✅ Done |
| 3 — Inventory | US-010 a US-013 | ✅ Done |
| 4 — Analytics | US-014 a US-018 | ✅ Done |
| 5 — Users + Notifications | US-019 a US-021 | ✅ Done |

## Próximas US Disponíveis

- **US-022** — Domain Event Dispatcher (SaveChanges interceptor + MediatR INotification)
- **US-023** — RabbitMQ Messaging (async publish, outbox pattern)
- **US-024** — PostgreSQL Migration (production database)
- **US-025** — Docker & Docker Compose (full stack)
- **US-026** — CI/CD Pipeline (GitHub Actions)
- **US-027** — OpenTelemetry (traces + Prometheus + Grafana)

## Skills Available

All agents should consult `.squad/skills/ims-modular-patterns/SKILL.md` for the skill index.
26-03-03T17:00:00Z
focus_area: Team initialization, skills library setup, and project scope documentation
active_issues: []
---

# What We're Focused On

Squad fully configured with 15 imported skills from the ims-modular project. Skills cover architecture, implementation patterns, messaging, observability, testing, code quality (SonarQube), and security (OWASP). Team.md updated with complete project scope (all endpoints, seed data, versions). Ready for first development task.

## Skills Available

All agents should consult `.squad/skills/ims-modular-patterns/SKILL.md` for the skill index. Key rules:
- **Always** check `code-smells` before generating .NET code
- **Always** check `security-vulnerabilities` before implementing auth/sensitive data
- Use `code-templates` for scaffolding new domains/modules
- Follow `testing-patterns` for test structure (xUnit + Moq, AAA pattern)
