# IMS Architecture Documentation

> C4 diagrams, module dependency map, and multi-tenancy data flow for the IMS Modular Monolith.

## Diagrams

| Diagram | Description |
|---|---|
| [C4 Context](./c4-context.svg) | System boundaries and external actors |
| [C4 Container](./c4-container.svg) | Deployable containers and their relationships |
| [C4 Component](./c4-component.svg) | Backend API internal module breakdown |
| [Module Event Graph](./module-graph.svg) | Which modules publish / consume MediatR domain events |
| [Multi-Tenancy Flow](./multitenancy-flow.svg) | Request → middleware → CQRS → data with tenant isolation |

The Structurizr DSL source for all C4 levels lives in [`workspace.dsl`](./workspace.dsl).  
Mermaid source files live in [`src/`](./src/).

---

## C4 Level 1 — System Context

```mermaid
C4Context
  title C4 Level 1 — System Context: IMS Monolith

  Person(user, "User", "Internal staff member<br/>managing issues and inventory")
  Person(admin, "Admin", "System administrator<br/>managing tenants and config")

  System(ims, "IMS Monolith", "Issue and Inventory Management System<br/>.NET 9 · Next.js 15 · Modular Monolith")

  System_Ext(email, "Email Provider", "Transactional email delivery<br/>SMTP / SendGrid")
  System_Ext(keycloak, "Keycloak", "External Identity Provider<br/>(PoC — US-090)")
  System_Ext(webhookTargets, "Webhook Consumers", "External systems receiving<br/>IMS domain event webhooks")

  Rel(user,  ims, "Manages issues and inventory", "HTTPS")
  Rel(admin, ims, "Configures system, manages tenants", "HTTPS")
  Rel(ims, email, "Sends transactional emails", "SMTP")
  Rel(ims, keycloak, "Delegates auth (PoC)", "OIDC")
  Rel(ims, webhookTargets, "Delivers domain event webhooks", "HTTPS")

  UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="1")
```

---

## C4 Level 2 — Containers

```mermaid
C4Container
  title C4 Level 2 — Containers: IMS Monolith

  Person(user,  "User",  "Internal staff")
  Person(admin, "Admin", "System admin")

  System_Boundary(ims, "IMS Monolith") {
    Container(nextjs,   "Next.js Frontend",  "Next.js 15, TypeScript, Tailwind CSS", "Primary web UI — SSR + PWA")
    Container(backend,  "Backend API",        ".NET 9, ASP.NET Core Minimal API",    "Composition root for all business modules")
    ContainerDb(postgres,    "PostgreSQL",    "PostgreSQL 16",    "Primary relational store — one schema per module")
    ContainerDb(redis,       "Redis",         "Redis 7",          "Distributed cache and output cache")
    Container(meilisearch,   "Meilisearch",   "Meilisearch",      "Full-text search for issues and products")
    ContainerQueue(rabbitmq, "RabbitMQ",      "RabbitMQ 3",       "Async integration events and webhook delivery")
    Container(hangfire,      "Hangfire",      "Hangfire",         "Background job scheduler (uses PostgreSQL)")
  }

  System_Ext(email,   "Email Provider",    "SMTP / SendGrid")
  System_Ext(keycloak,"Keycloak",          "OIDC Identity Provider")
  System_Ext(webhooks,"Webhook Consumers", "External HTTP endpoints")

  Rel(user,  nextjs,  "Uses via browser",      "HTTPS")
  Rel(admin, nextjs,  "Uses via browser",       "HTTPS")
  Rel(nextjs, backend,"REST + SignalR",          "HTTPS / WSS")
  Rel(backend, postgres,    "Read / Write",      "EF Core / Dapper")
  Rel(backend, redis,       "Cache get / set",   "StackExchange.Redis")
  Rel(backend, meilisearch, "Index + Query",     "HTTP")
  Rel(backend, rabbitmq,    "Publish events",    "AMQP")
  Rel(backend, hangfire,    "Enqueue / Execute", "Hangfire client")
  Rel(backend, email,       "Send emails",       "SMTP")
  Rel(backend, keycloak,    "Token validation",  "OIDC")
  Rel(backend, webhooks,    "HTTP POST webhooks","HTTPS")

  UpdateLayoutConfig($c4ShapeInRow="4", $c4BoundaryInRow="1")
```

---

## Module Event Graph

```mermaid
flowchart TD
    subgraph PUBLISHERS["📤 Event Publishers"]
        direction TB
        ISS["Issues Module"]
        INV["Inventory Module"]
        INVI["InventoryIssues Module"]
    end

    subgraph EVENTS["⚡ Domain Events (MediatR INotification)"]
        direction LR
        E1["IssueCreatedEvent"]
        E2["IssueStatusChangedEvent"]
        E3["IssueAssignedEvent"]
        E4["IssueCompletedEvent"]
        E5["IssueCommentAddedEvent"]
        E6["ProductCreatedEvent"]
        E7["StockChangedEvent"]
        E8["LowStockAlertEvent"]
        E9["OutOfStockEvent"]
        E10["StockReplenishedEvent"]
        E11["SupplierCreatedEvent"]
        E12["LocationCreatedEvent"]
    end

    subgraph CONSUMERS["📥 Event Consumers"]
        direction TB
        NOTIF["Notifications Module\n(SignalR push)"]
        SRCH["Search Module\n(Meilisearch index)"]
        HOOK["Webhooks Module\n(HTTP outbound)"]
        AUDIT["Audit Module\n(immutable log)"]
    end

    subgraph INFRA["🏗️ Shared Kernel (cross-cutting)"]
        PIPE["CQRS Pipeline Behaviors\n(Logging · Caching · Validation · Audit)"]
        OUTB["Transactional Outbox\n(reliable event delivery)"]
        CACHE["Redis Cache\n(tenant-scoped keys)"]
    end

    ISS  --> E1 & E2 & E3 & E4 & E5
    INV  --> E6 & E7 & E8 & E9 & E10 & E11 & E12
    INVI --> E8 & E9

    E1 --> SRCH & HOOK & NOTIF
    E4 --> HOOK & NOTIF
    E2 & E3 & E5 --> NOTIF
    E6 --> SRCH
    E7 & E8 & E9 & E10 --> NOTIF
    E11 & E12 --> NOTIF

    ISS & INV --> PIPE
    PIPE --> OUTB --> AUDIT
    PIPE --> CACHE
```

---

## Multi-Tenancy Data Flow

```mermaid
flowchart TD
    REQ(["🌐 HTTP Request\nX-Tenant-ID: alpha"])

    subgraph MIDDLEWARE["ASP.NET Core Pipeline"]
        T1["TenantResolutionMiddleware\n→ resolve tenant from header / subdomain"]
        T2["TenantContext\n→ set ITenantService.CurrentTenant"]
        T3["AuthMiddleware\n→ validate JWT, check tenant claim"]
        T4["UserContextMiddleware\n→ set IUserContext.UserId"]
    end

    subgraph CQRS["CQRS Pipeline (MediatR)"]
        P1["ValidationBehavior"]
        P2["CachingBehavior\n→ key = '{prefix}:{tenantId}:{hash}'"]
        P3["AuditBehavior\n→ write to AuditLog (tenantId stamped)"]
        P4["CommandHandler / QueryHandler"]
    end

    subgraph DATA["Data Layer"]
        EF["TenantAwareDbContext\n→ HasQueryFilter(e.TenantId == current)\n→ OnSave: stamp TenantId"]
        DAPPER["Dapper Queries\n→ WHERE TenantId = @tenantId"]
        REDIS["Redis Cache\n→ keys include tenantId segment"]
    end

    DB[("PostgreSQL\nshared schema\nmulti-tenant rows")]

    REQ --> T1 --> T2 --> T3 --> T4
    T4  --> P1 --> P2 --> P3 --> P4
    P4  --> EF & DAPPER
    P2  --> REDIS
    EF  --> DB
    DAPPER --> DB
```

> **Key invariant (fixed in US-080):** `CachingBehavior` includes `tenantId` in every cache key, preventing cross-tenant cache poisoning.

---

## Regenerating Diagrams Locally

```bash
# Install mermaid-cli once
npm install -g @mermaid-js/mermaid-cli

# Regenerate all SVGs
cd docs/architecture
for f in src/*.mmd; do
  name=$(basename "$f" .mmd)
  mmdc -i "$f" -o "${name}.svg" -t neutral -q
done
```

SVGs are also regenerated automatically on every merge to `main` via the [generate-diagrams](./.github/workflows/generate-diagrams.yml) GitHub Actions workflow.
