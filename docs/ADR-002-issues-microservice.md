# ADR-002: Microservice Extraction — Issues Domain (US-079)

## Status

Accepted

## Date

2026-04-27

## Context

The IMS Modular Monolith was designed with vertical slice architecture and bounded contexts.
The Issues module has emerged as the highest-traffic bounded context, with:

- ~60% of API calls originating from Issues endpoints
- Independent release cadence from Inventory and Auth modules
- Growing team ownership (a second squad now owns Issues)
- Complex background processing (SLA tracking, escalation rules)

A full microservice rewrite carries high risk. Instead we apply the **Strangler Fig pattern**:
extract the Issues domain incrementally behind a feature flag with a YARP reverse proxy.

## Decision

Extract the `Issues` bounded context into `ims-issues-service`, an independent .NET 9 WebAPI:

1. **Shared Database (Phase 1):** Both monolith and microservice point to the same PostgreSQL database.
   The `Issues` table is owned by the microservice; the monolith's `IssuesModule` remains available as fallback.

2. **Feature Flag Gate:** The feature flag `UseIssuesMicroservice` (Microsoft.FeatureManagement) controls routing.
   - `false` (default): Monolith handles `/api/issues/**` — zero risk.
   - `true`: YARP (Yarp.ReverseProxy) routes `/api/issues/**` to `ims-issues-service:8081`.

3. **Integration Events via RabbitMQ:** The microservice publishes events (`issues.created`, `issues.status_changed`, `issues.deleted`) to the `ims.issues` exchange. The monolith consumes them for cache invalidation and analytics.

4. **JWT Shared Secret:** Both services validate the same JWT (same `Issuer`, `Audience`, `SecretKey`). No additional identity service required in Phase 1.

5. **Health Checks:** `ims-issues-service` exposes `/health/live` and `/health/ready`. Docker Compose and the orchestration layer use these for readiness gating.

## Consequences

### Positive

- Zero-downtime migration via feature flag toggle.
- Issues team can deploy independently once flag is enabled.
- Performance isolation: Issues load no longer affects Inventory or Auth.
- Clear bounded context boundary enforced at the infrastructure level.

### Negative / Mitigations

| Risk | Mitigation |
|---|---|
| Shared DB coupling | Phase 2: migrate to separate DB + events for cross-context reads |
| Distributed transaction complexity | Use Outbox pattern + idempotent consumers |
| Increased operational overhead | Unified observability (OpenTelemetry, same Grafana dashboard) |
| Fallback complexity | Feature flag makes rollback instantaneous |

## Alternatives Considered

1. **Keep in Monolith** — Rejected: team scaling requires independent deployability.
2. **Full separate DB from day one** — Rejected: too risky for Sprint 12 scope; deferred to Phase 2.
3. **GraphQL federation** — Rejected: premature; no federation infrastructure exists.

## Implementation

```
services/
└── ims-issues-service/
    ├── Dockerfile
    ├── Program.cs              ← Full Issues CRUD + JWT auth
    ├── Messaging/
    │   ├── IssuesEventPublisher.cs   ← Publishes to RabbitMQ
    │   ├── IssuesEventConsumer.cs    ← Consumes monolith events
    │   └── RabbitMqOptions.cs
    └── Infrastructure/
        └── IssuesServiceDbContext.cs ← Shared DB, Issues table

backend/src/
└── Shared/
    └── Proxy/
        └── IssuesProxyExtensions.cs  ← YARP registration + mapping
```

### Feature Flag Toggle

```bash
# Enable microservice routing (production)
curl -X PATCH /api/features/UseIssuesMicroservice \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"enabled": true}'

# Rollback instantly
curl -X PATCH /api/features/UseIssuesMicroservice \
  -d '{"enabled": false}'
```

## Phase 2 Roadmap

- [ ] Migrate Issues to separate PostgreSQL instance
- [ ] Implement saga/choreography for Issue ↔ Inventory cross-context workflows
- [ ] Add dedicated Grafana dashboard for `ims-issues-service`
- [ ] Extract Issues team CI/CD pipeline
