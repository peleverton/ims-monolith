---
updated_at: 2026-03-03T17:00:00Z
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
