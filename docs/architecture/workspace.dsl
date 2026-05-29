workspace "IMS — Issue & Inventory Management System" "Modular Monolith architecture documentation" {

    model {
        user = person "User" "Internal staff member managing issues and inventory." "User"
        admin = person "Admin" "System administrator managing tenants and configuration." "Admin"

        ims = softwareSystem "IMS Monolith" "Issue and Inventory Management System built as a Modular Monolith." {

            nextFrontend = container "Next.js Frontend" "Primary web UI. Server-side rendered and PWA-enabled." "Next.js 15, TypeScript, Tailwind CSS"
            blazorFrontend = container "Blazor WASM Frontend" "Legacy frontend (retained for reference)." "Blazor WebAssembly, .NET 9"

            backendApi = container "Backend API" "Composition root hosting all business modules as a single deployable unit." ".NET 9, ASP.NET Core Minimal API" {
                authModule      = component "Auth Module"            "JWT issuance, login, registration, refresh tokens." "C#, BCrypt"
                userMgmtModule  = component "UserManagement Module"  "CRUD for users, roles, tenant-scoped permissions." "C#, EF Core"
                issuesModule    = component "Issues Module"          "Issue aggregate root, lifecycle, comments, assignment." "C#, EF Core, MediatR"
                inventoryModule = component "Inventory Module"       "Products, suppliers, locations, stock movements." "C#, EF Core, MediatR"
                invIssuesModule = component "InventoryIssues Module" "Bridge: links inventory stock events to issue creation." "C#, EF Core"
                analyticsModule = component "Analytics Module"       "Read-side analytics dashboards. No write operations." "C#, Dapper"
                notifModule     = component "Notifications Module"   "Real-time push via SignalR hub." "C#, SignalR"
                webhooksModule  = component "Webhooks Module"        "Outbound HTTP webhooks on domain events." "C#, RabbitMQ"
                searchModule    = component "Search Module"          "Full-text search indexing and query via Meilisearch." "C#, Meilisearch"
                jobsModule      = component "Jobs Module"            "Scheduled and background jobs dashboard." "C#, Hangfire"
                billingModule   = component "Billing Module"         "Tenant subscription plans and billing state." "C#, EF Core"
                auditModule     = component "Audit Module"           "Immutable audit log for all write commands." "C#, EF Core"
                featuresModule  = component "Features Module"        "Per-tenant feature flag evaluation." "C#"
                sharedKernel    = component "Shared Kernel"          "Cross-cutting: multi-tenancy, caching, CQRS pipeline, outbox, messaging, observability." "C#"
            }

            postgres  = container "PostgreSQL"   "Primary relational store. Each module has its own schema/DbContext." "PostgreSQL 16"
            redis     = container "Redis"        "Distributed cache and output cache store." "Redis 7"
            meilisearch = container "Meilisearch" "Full-text search engine for issues and products." "Meilisearch"
            rabbitmq  = container "RabbitMQ"     "Async messaging for integration events and webhooks." "RabbitMQ 3"
            hangfire  = container "Hangfire"     "Background job storage (uses PostgreSQL in production, in-memory in dev/test)." "Hangfire"
        }

        emailProvider = softwareSystem "Email Provider" "Transactional email delivery (SMTP/SendGrid)." "External System"
        keycloak      = softwareSystem "Keycloak" "External Identity Provider (PoC — US-090)." "External System"
        webhookTargets = softwareSystem "Webhook Consumers" "External systems receiving IMS domain event webhooks." "External System"

        # Relationships — Users
        user  -> ims.nextFrontend "Uses via browser"
        admin -> ims.nextFrontend "Uses via browser"

        # Frontend → Backend
        ims.nextFrontend  -> ims.backendApi "REST API + SignalR" "HTTPS / WSS"
        ims.blazorFrontend -> ims.backendApi "REST API" "HTTPS"

        # Backend → Infrastructure
        ims.backendApi -> ims.postgres    "Reads / Writes" "EF Core / Dapper"
        ims.backendApi -> ims.redis       "Cache get/set" "StackExchange.Redis"
        ims.backendApi -> ims.meilisearch "Index / Search" "HTTP"
        ims.backendApi -> ims.rabbitmq    "Publish integration events" "AMQP"
        ims.backendApi -> ims.hangfire    "Enqueue / Execute jobs" "Hangfire client"

        # Backend → External
        ims.backendApi -> emailProvider   "Sends emails" "SMTP"
        ims.backendApi -> keycloak        "Token validation (PoC)" "OIDC"
        ims.webhooksModule -> webhookTargets "HTTP POST" "HTTPS"

        # Component relationships (Backend internals)
        ims.issuesModule    -> ims.sharedKernel "Uses CQRS pipeline, outbox, tenant"
        ims.inventoryModule -> ims.sharedKernel "Uses CQRS pipeline, outbox, tenant"
        ims.invIssuesModule -> ims.issuesModule  "Creates issues on stock events"
        ims.invIssuesModule -> ims.inventoryModule "Listens to stock domain events"
        ims.searchModule    -> ims.meilisearch   "Indexes issues & products"
        ims.notifModule     -> ims.backendApi    "Broadcasts via SignalR hub"
        ims.auditModule     -> ims.postgres      "Writes audit log"
        ims.analyticsModule -> ims.postgres      "Read-only Dapper queries"
        ims.webhooksModule  -> ims.rabbitmq      "Publishes to ims.webhooks exchange"
    }

    views {
        systemContext ims "C4-Context" {
            include *
            autoLayout
            title "C4 Level 1 — System Context"
        }

        container ims "C4-Container" {
            include *
            autoLayout
            title "C4 Level 2 — Containers"
        }

        component backendApi "C4-Component-Backend" {
            include *
            autoLayout
            title "C4 Level 3 — Backend API Components"
        }

        theme default
    }
}
