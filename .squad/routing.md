# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture & design decisions | Morpheus | Module boundaries, domain model design, shared kernel changes |
| Code review & PR review | Morpheus | Review PRs, check quality, suggest improvements, CQRS pattern review |
| API endpoints & Minimal API | Neo | New endpoints, route handlers, request/response DTOs |
| CQRS handlers (MediatR) | Neo | Commands, queries, handlers, pipeline behaviors |
| EF Core & database | Neo | DbContext, migrations, repositories, data access |
| Module creation & refactoring | Neo | New modules, restructuring, module extensions |
| FluentValidation validators | Neo | Request validators, custom rules |
| JWT/Auth module | Neo | Authentication, authorization, token handling |
| Domain model & entities | Neo | Entities, value objects, domain events, domain services |
| Infrastructure services | Neo | External integrations, email, file storage |
| Scope & priorities | Morpheus | What to build next, trade-offs, decisions |
| Unit tests | Trinity | xUnit/NUnit tests, mock setups, test data |
| Integration tests | Trinity | API tests, database tests, module integration |
| Test coverage & quality gates | Trinity | Coverage analysis, edge cases, test strategy |
| Async issue work (bugs, tests, small features) | @copilot 🤖 | Well-defined tasks matching capability profile |
| Session logging | Scribe | Automatic — never needs routing |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, evaluate @copilot fit, assign `squad:{member}` label | Morpheus |
| `squad:morpheus` | Architecture review, design decisions, code review | Morpheus |
| `squad:neo` | Implementation: APIs, handlers, modules, database, domain | Neo |
| `squad:trinity` | Test creation, quality assurance, coverage gaps | Trinity |
| `squad:copilot` | Assign to @copilot for autonomous work (if enabled) | @copilot 🤖 |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, **Morpheus** triages it — analyzing content, evaluating @copilot's capability profile, assigning the right `squad:{member}` label, and commenting with triage notes.
2. **@copilot evaluation:** Morpheus checks if the issue matches @copilot's capability profile (🟢 good fit / 🟡 needs review / 🔴 not suitable). If it's a good fit, Morpheus may route to `squad:copilot` instead of a squad member.
3. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
4. When `squad:copilot` is applied and auto-assign is enabled, `@copilot` is assigned on the issue and picks it up autonomously.
5. Members can reassign by removing their label and adding another member's label.
6. The `squad` label is the "inbox" — untriaged issues waiting for Morpheus review.

### Lead Triage Guidance for @copilot

When triaging, Morpheus should ask:

1. **Is this well-defined?** Clear title, reproduction steps or acceptance criteria, bounded scope → likely 🟢
2. **Does it follow existing patterns?** Adding a MediatR handler, fixing a known bug, updating a package → likely 🟢
3. **Does it need design judgment?** Architecture, API design, module boundaries → likely 🔴
4. **Is it security-sensitive?** JWT auth, encryption, access control → always 🔴
5. **Is it medium complexity with specs?** Feature with clear requirements, refactoring with tests → likely 🟡

## File Path → Agent Mapping

| Path Pattern | Agent |
|-------------|-------|
| `Modules/*/Api/**` | Neo |
| `Modules/*/Application/**` | Neo |
| `Modules/*/Domain/**` | Morpheus (domain model review), Neo (implementation) |
| `Modules/*/Infrastructure/**` | Neo |
| `Modules/*ModuleExtensions.cs` | Neo |
| `Shared/**` | Morpheus (design), Neo (implementation) |
| `Program.cs` | Neo + Morpheus |
| `*.csproj` | Neo |
| `**/*Test*/**` | Trinity |
| `.squad/**` | Scribe |

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If Neo is building a handler, spawn Trinity to write test cases from requirements simultaneously.
7. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. Morpheus handles all `squad` (base label) triage.
8. **@copilot routing** — when evaluating issues, check @copilot's capability profile in `team.md`. Route 🟢 good-fit tasks to `squad:copilot`. Flag 🟡 needs-review tasks for PR review. Keep 🔴 not-suitable tasks with squad members.
