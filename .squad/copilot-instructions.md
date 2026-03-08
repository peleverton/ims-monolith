# Copilot Coding Agent — Squad Instructions

You are working on a project that uses **Squad**, an AI team framework. When picking up issues autonomously, follow these guidelines.

## Team Context

Before starting work on any issue:

1. Read `.squad/team.md` for the team roster, member roles, and your capability profile.
2. Read `.squad/routing.md` for work routing rules.
3. If the issue has a `squad:{member}` label, read that member's charter at `.squad/agents/{member}/charter.md` to understand their domain expertise and coding style — work in their voice.

## Project-Specific Context

This is **IMS Modular** — a C#/.NET 9 modular monolith. Key patterns:
- Modules: Auth, Issues (each with Api/Application/Domain/Infrastructure layers)
- CQRS via MediatR, validation via FluentValidation
- **Write side:** EF Core (change tracking, transactions, aggregate persistence)
- **Read side:** Dapper (raw SQL, direct DTO projection, no tracking)
- Minimal API endpoints, EF Core + SQLite (write), Dapper + SQLite (read), JWT Bearer auth
- Root namespace: IMS.Modular, target: net9.0
- URL: http://localhost:5049
- Seed data: admin@ims.com / Admin@123
- Read `.squad/skills/ims-modular-patterns/SKILL.md` for full conventions and skill index

### Skills — MUST READ before writing code
- **ALWAYS** check `.squad/skills/ims-modular-patterns/skills/code-smells/SKILL.md` before generating any .NET code
- **ALWAYS** check `.squad/skills/ims-modular-patterns/skills/security-vulnerabilities/SKILL.md` for auth/security work
- Use `code-templates` skill for scaffolding new domains/modules
- Use `testing-patterns` skill for test structure (xUnit + Moq, AAA pattern)
- Full index of 15 skills at `.squad/skills/ims-modular-patterns/SKILL.md`

## Capability Self-Check

Before starting work, check your capability profile in `.squad/team.md` under the **Coding Agent → Capabilities** section.

- **🟢 Good fit** — proceed autonomously.
- **🟡 Needs review** — proceed, but note in the PR description that a squad member should review.
- **🔴 Not suitable** — do NOT start work. Instead, comment on the issue:
  ```
  🤖 This issue doesn't match my capability profile (reason: {why}). Suggesting reassignment to a squad member.
  ```

## Branch Naming

Use the squad branch convention:
```
squad/{issue-number}-{kebab-case-slug}
```
Example: `squad/42-fix-login-validation`

## PR Guidelines

When opening a PR:
- Reference the issue: `Closes #{issue-number}`
- If the issue had a `squad:{member}` label, mention the member: `Working as {member} ({role})`
- If this is a 🟡 needs-review task, add to the PR description: `⚠️ This task was flagged as "needs review" — please have a squad member review before merging.`
- Follow any project conventions in `.squad/decisions.md`

## Decisions

If you make a decision that affects other team members, write it to:
```
.squad/decisions/inbox/copilot-{brief-slug}.md
```
The Scribe will merge it into the shared decisions file.
