# Solution Design — IMS Modular Monolith

> **Author:** Morpheus (Lead / Architect)
> **Date:** 2026-03-03
> **Status:** ✅ Approved — evolutionary architecture
> **Revision:** 2.0 — Full feature scope from IMS legacy + CQRS data access strategy

---

## 1. Vision

IMS (Issue Management System) is a **modular monolith** built with .NET 9, designed to be the single deployable unit during early product stages while retaining the ability to extract modules into independent services when scale demands it.

The system has evolved beyond simple issue tracking to include **inventory management**, **analytics**, **notifications**, **user management**, and a full **observability stack**. This design document captures the complete feature scope discovered in the IMS legacy project and maps it onto the modular monolith architecture.

The architecture intentionally trades distributed complexity for **module boundary discipline** — every module is autonomous in code, data access, and domain logic, communicating only through well-defined contracts.

---

## 2. Architecture Overview

```
┌────────────────────────────────────────────────────────────────────────────┐
│                          Program.cs (Composition Root)                    │
│  ┌────────────────────────────────────────────────────────────────────┐   │
│  │                    ASP.NET Core Pipeline                           │   │
│  │  CorrelationId → Metrics → Auth → Authorization → Routing → API   │   │
│  └────────────────────────────────────────────────────────────────────┘   │
│                                                                          │
│  ┌─────────────────┐  ┌──────────────────┐  ┌──────────────────────┐    │
│  │   Auth Module    │  │   Issues Module   │  │ Inventory Module     │    │
│  │  User, Role      │  │  Issue (AR)       │  │ Product, Supplier    │    │
│  │  JWT, Refresh     │  │  Comment, Tag     │  │ Location, Stock      │    │
│  │  Profiles, RBAC   │  │  Activity (audit) │  │ InventoryIssue       │    │
│  └─────────────────┘  └──────────────────┘  └──────────────────────┘    │
│                                                                          │
│  ┌─────────────────┐  ┌──────────────────┐  ┌──────────────────────┐    │
│  │ Users Module     │  │ Analytics Module  │  │ Notifications Module │    │
│  │  User CRUD       │  │  Issue summary    │  │ In-app, Email        │    │
│  │  Roles, Active   │  │  User workload    │  │ Push, Templates      │    │
│  │  Profile mgmt    │  │  Auto-assign      │  │ SignalR (future)     │    │
│  └─────────────────┘  └──────────────────┘  └──────────────────────┘    │
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │                    Shared Kernel                                   │  │
│  │  BaseEntity │ Result<T> │ PagedResult<T> │ IDomainEvent           │  │
│  └────────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │                    Cross-Cutting Concerns                          │  │
│  │  MediatR Behaviors (Validation, Logging, Caching)                 │  │
│  │  Middleware (CorrelationId, Metrics, Performance, UserContext)     │  │
│  │  HealthChecks (DB, Memory, Disk, Cache)                           │  │
│  │  OpenTelemetry (Traces, Metrics, Prometheus)                      │  │
│  │  Serilog (structured logging, JSON, rolling files)                │  │
│  └────────────────────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────────────────────┘
         │                  │                │                │
         ▼                  ▼                ▼                ▼
   ┌───────────┐    ┌───────────┐    ┌───────────┐    ┌───────────┐
   │  SQLite/   │    │  Redis    │    │  Swagger   │    │ Prometheus│
   │  PostgreSQL│    │  Cache    │    │  /swagger  │    │ Grafana   │
   └───────────┘    └───────────┘    └───────────┘    └───────────┘
```

---

## 3. Architectural Principles

| # | Principle | Enforcement |
|---|-----------|-------------|
| 1 | **Module autonomy** — each module owns its domain, data access, and API surface | Separate DbContexts per module; no cross-module entity references |
| 2 | **Dependency Rule** — dependencies flow inward (Api → Application → Domain ← Infrastructure) | Code review + namespace conventions; Domain has zero dependencies |
| 3 | **Railway-Oriented Programming** — no exceptions for business logic | All operations return `Result<T>` from Shared Kernel |
| 4 | **CQRS via MediatR** — commands mutate, queries read | Separate `Commands/` and `Queries/` directories; handlers are the unit of work |
| 5 | **Write with EF Core, Read with Dapper** — each side uses the best tool | Commands use EF Core (change tracking, transactions, aggregates); Queries use Dapper (raw SQL, no tracking, direct DTO projection) |
| 6 | **Domain Events** — cross-module communication via IDomainEvent | Raised in aggregate root, dispatched via MediatR INotification |
| 7 | **Pipeline Behaviors** — cross-cutting concerns in MediatR pipeline | ValidationBehavior, LoggingBehavior, CachingBehavior |
| 8 | **Explicit registration** — no magic auto-discovery for DI | `AddXxxModule()` extension methods register all services explicitly |
| 9 | **Lean Shared Kernel** — only types needed by 2+ modules live here | Morpheus (Lead) approves all Shared Kernel additions |
| 10 | **Validation at the boundary** — FluentValidation on all incoming requests | Validators registered per module, ValidationBehavior auto-validates |
| 11 | **Observability by default** — every request is traced, metered, and logged | CorrelationId middleware, Serilog structured logging, OpenTelemetry |

---

## 4. Module Catalog

### 4.1 Auth Module

**Purpose:** User registration, authentication, and JWT token issuance.

| Layer | Key Components |
|-------|----------------|
| Api | `AuthEndpoints.cs` — POST `/api/auth/register`, POST `/api/auth/login` |
| Application | `LoginRequest`, `RegisterRequest`, `TokenResponse` DTOs; FluentValidation validators |
| Domain | `User` (aggregate root), `Role`, `UserRole` (join entity) |
| Infrastructure | `AuthDbContext`, `JwtService` (token generation with HS256) |

**Seed data:** Roles (Admin, User) + admin user (`admin@ims.com` / `Admin@123`) created on startup.

**Security:** BCrypt password hashing, JWT Bearer with issuer/audience validation.

### 4.2 Issues Module

**Purpose:** Full CRUD lifecycle for issue tracking with status management, comments, and activity audit trail.

| Layer | Key Components |
|-------|----------------|
| Api | `IssuesEndpoints.cs` — 7 endpoints (CRUD + status + comments) |
| Application | `CreateIssueCommand`, `UpdateIssueCommand`, `UpdateIssueStatusCommand`, `AddCommentCommand`, `GetIssueByIdQuery`, `GetPagedIssuesQuery`; validators for all commands |
| Domain | `Issue` (aggregate root), `IssueComment`, `IssueActivity`, `IssueTag` (value object); enums: `IssueStatus`, `IssueType`, `IssuePriority` |
| Infrastructure | `IssuesDbContext` (EF Core — writes), `IssueRepository` (write repo), `IssueReadRepository` (Dapper — read queries) |

**Key patterns:**
- Aggregate root (`Issue`) controls all child entity mutations
- Status transitions tracked via `IssueActivity` (audit trail)
- Tags as value objects (not entities)
- Paged queries return `PagedResult<T>`
- **Command handlers** use `IIssueRepository` (EF Core) — change tracking, transactions, aggregate persistence
- **Query handlers** use `IIssueReadRepository` (Dapper) — raw SQL, direct DTO projection, no tracking overhead

### 4.3 Inventory Module

**Purpose:** Complete inventory management — products, stock movements, locations, suppliers, and inventory analytics.

| Layer | Key Components |
|-------|----------------|
| Api | `ProductsController` — CRUD + stock adjust + transfer + discontinue + pricing; `StockMovementsController` — create, bulk, adjust, transfer; `SuppliersController` — CRUD + activate/deactivate + contact update; `LocationsController` — CRUD + activate/deactivate + capacity update; `InventoryAnalyticsController` — value, summary, trends, reports |
| Application | **Commands:** `CreateProduct`, `UpdateProduct`, `DeleteProduct`, `AdjustStock`, `TransferStock`, `DiscontinueProduct`, `UpdateProductPricing`; `CreateStockMovement`, `BulkCreateStockMovements`, `AdjustStock`, `TransferStock`, `DeleteStockMovement`; `CreateSupplier`, `UpdateSupplier`, `DeleteSupplier`, `ActivateSupplier`, `DeactivateSupplier`, `UpdateSupplierContact`; `CreateLocation`, `UpdateLocation`, `DeleteLocation`, `ActivateLocation`, `DeactivateLocation`, `UpdateLocationCapacity` |
| Application | **Queries:** `GetProducts` (paginated, filtered by category/status/location/supplier), `GetProductById`, `GetProductBySKU`; `GetStockMovements` (filtered by product/date range); `GetSuppliers`, `GetSupplierById`; `GetLocations`, `GetLocationById` |
| Application | **Analytics Queries:** `GetInventorySummary`, `GetInventoryValue`, `GetStockSummary`, `GetCategoryDistribution`, `GetValueByCategory`, `GetTopProducts`, `GetStockStatus`, `GetExpiringProductsReport`, `GetStockTrends`, `GetTurnoverRate`, `GetLocationCapacity`, `GetSupplierPerformance`, `GetStockMovementHistory` |
| Domain | `Product` (aggregate root): Name, SKU, Barcode, Description, Category, CurrentStock, MinimumStockLevel, MaximumStockLevel, UnitPrice, CostPrice, Unit, ExpiryDate, StockStatus, IsActive; `Supplier`: Name, Code, ContactPerson, Email, Phone, Address, City, State, Country, PostalCode, TaxId, CreditLimit, PaymentTermsDays; `Location`: Name, Code, Type, Capacity, Description, ParentLocationId (hierarchical); `StockMovement`: ProductId, MovementType, Quantity, LocationId, Reference, Notes |
| Domain | **Enums:** `ProductCategory` (Electronics, Food, Beverages, Clothing, Furniture, Books, Toys, Sports, Tools, Automotive, Health, Medical, Beauty, Home, Garden, Office, Pet, Baby, Other); `StockStatus` (InStock, LowStock, OutOfStock, Overstock, Discontinued); `StockMovementType` (InitialStock, StockIn, StockOut, Adjustment, Transfer, Sale, Purchase, Return, Damage, Loss, Expired, LocationChanged, PriceAdjustment, Updated, Discontinued); `LocationType` (Warehouse, Store, Aisle, Shelf, DistributionCenter, Manufacturing, ReturnCenter, Transit) |
| Domain | **Domain Events:** `ProductCreatedEvent`, `StockChangedEvent`, `LowStockAlertEvent`, `OutOfStockEvent`, `StockReplenishedEvent`, `ProductDiscontinuedEvent`, `PriceChangedEvent`, `StockTransferInitiatedEvent`, `StockTransferCompletedEvent`, `SupplierCreatedEvent`, `SupplierDeactivatedEvent`, `LocationCreatedEvent`, `LocationDeactivatedEvent`, `ProductExpiringSoonEvent`, `ProductExpiredEvent` |
| Infrastructure | `InventoryDbContext` (EF Core — writes), `ProductRepository`, `SupplierRepository`, `LocationRepository`, `StockMovementRepository` (write repos); `ProductReadRepository`, `StockMovementReadRepository`, `SupplierReadRepository`, `LocationReadRepository` (Dapper — read repos) |

**Key patterns:**
- Products track stock levels with automatic `StockStatus` calculation (InStock, LowStock, OutOfStock, Overstock, Discontinued)
- Stock movements record every change (audit trail)
- Locations support hierarchical structure (ParentLocationId) and capacity tracking
- Suppliers manage credit limits and payment terms
- Rich analytics: inventory value, turnover rates, expiring products, supplier performance, category distribution
- Domain events for cross-module communication (e.g., `LowStockAlertEvent` → Notifications module)

### 4.4 Inventory Issues Module

**Purpose:** Track and manage issues specifically related to inventory — damaged stock, discrepancies, missing items, quality problems.

| Layer | Key Components |
|-------|----------------|
| Api | `InventoryIssuesController` — full lifecycle: CRUD, status changes (resolve, close, reopen), assign, search, statistics; filtered views (by product, location, type, priority, status, reporter, assignee, open, high-priority, overdue) |
| Application | **Commands:** `CreateInventoryIssue`, `UpdateInventoryIssue`, `ChangeInventoryIssueStatus`, `AssignInventoryIssue`, `ResolveInventoryIssue`, `CloseInventoryIssue`, `ReopenInventoryIssue`, `DeleteInventoryIssue` |
| Application | **Queries:** `GetInventoryIssueById`, `GetAllInventoryIssues`, `GetByProduct`, `GetByLocation`, `GetByType`, `GetByPriority`, `GetByStatus`, `GetByReporter`, `GetByAssignee`, `GetOpenIssues`, `GetHighPriority`, `GetOverdue`, `SearchInventoryIssues`, `GetInventoryIssueStatistics` |
| Domain | `InventoryIssue` (aggregate root): Title, Description, Type (`InventoryIssueType`), Priority, Status, ProductId, LocationId, ReporterId, AssigneeId, DueDate, Resolution |
| Domain | **Enums:** `InventoryIssueType` (Damage, Discrepancy, Missing, QualityIssue, Expired, etc.) |
| Infrastructure | `InventoryIssueDbContext`, `InventoryIssueRepository` (write), `InventoryIssueReadRepository` (read) |

**Key patterns:**
- Links inventory issues to specific products and locations
- Full status lifecycle: Open → InProgress → Resolved/Closed, with Reopen capability
- Assignment with workload awareness (integrates with Analytics)
- Statistics endpoint for dashboard integration

### 4.5 Analytics Module

**Purpose:** Comprehensive analytics and reporting for issues and inventory.

| Layer | Key Components |
|-------|----------------|
| Api | **Issue Analytics:** `GET /api/analytics/issues/summary` — status, priority, assignment, date breakdowns; `GET /api/analytics/issues/trends` — daily trend over configurable period; `GET /api/analytics/issues/resolution-time` — average, median, min, max resolution hours by priority; `GET /api/analytics/issues/statistics` — detailed stats with date filters; `GET /api/analytics/issues/by-status`, `by-priority`, `by-assignee` — grouping endpoints; `GET /api/analytics/issues/{id}/suggest-assignee` — AI-driven workload-based assignment suggestion |
| Api | **User Analytics:** `GET /api/analytics/users/workload` — all users workload overview; `GET /api/analytics/users/{userId}/workload` — individual user workload detail; `GET /api/analytics/users/statistics` — created, assigned, resolved issue counts per user |
| Api | **Dashboard & Reports:** `GET /api/analytics/dashboard` — aggregated KPIs for dashboard; `GET /api/analytics/export/report` — export analytics (JSON/CSV); `POST /api/analytics/reports/export` — export with POST (supports JSON/CSV/PDF) |
| Domain | `IIssueAssignmentService` — domain service: `SuggestAssigneeAsync()` (lowest-workload algorithm), `GetUserWorkloadAsync()`, `CanAcceptNewIssueAsync()`, `GetUserActiveIssuesAsync()` |
| Domain | `IIssueValidationService` — domain-level validation rules for issues |

**Key patterns:**
- **Output caching** on analytics endpoints (10-minute cache for summary, 2-minute for statistics)
- **Workload-based auto-assignment** — suggests the active user with lowest active issue count
- **Trend analysis** — daily issue creation trends over configurable periods (1–365 days)
- **Resolution time analytics** — median, average, min, max by priority
- **Export capability** — JSON, CSV, PDF report generation

### 4.6 User Management Module

**Purpose:** Full user lifecycle management, profile operations, role management, and access control.

| Layer | Key Components |
|-------|----------------|
| Api | `UsersController` — paginated list with search (Admin), get by ID (self or Admin), `GET /me` (current user profile), get by role, get active users, update user, update profile, activate/deactivate user, change password, assign/remove roles, delete user |
| Application | DTOs: `UserDto`, `UpdateUserRequest`, `UpdateProfileRequest`, `ChangePasswordRequest` |
| Domain | `User` (aggregate root): Username, Email, FullName, PasswordHash, IsActive, UserProfile (value object); `Role`, `UserRole` (join entity) |
| Domain | **Domain Events:** `UserCreatedEvent`, `UserProfileUpdatedEvent`, `UserActivatedEvent`, `UserDeactivatedEvent`, `UserPasswordChangedEvent`, `UserRoleAssignedEvent`, `UserRoleRemovedEvent`, `UserLoggedInEvent` |
| Domain | **Domain Services:** `IUserValidationService` — validates email uniqueness, username rules, etc. |
| Infrastructure | `UserDbContext`, `UserRepository` (write), `UserReadRepository` (read) |

**Key patterns:**
- Self-service profile management (users can update their own profile)
- Admin-only operations (list all, activate/deactivate, role management)
- RBAC (Role-Based Access Control) with `[Authorize(Roles = "Admin")]`
- Output caching on user lists (5-minute cache)
- Domain events for audit trail (login tracking, profile changes, role changes)
- Facade pattern planned (`IUserManagementFacade`) for cross-module access

### 4.7 Notifications Module

**Purpose:** Multi-channel notification delivery for system events — in-app (SignalR), email (SMTP), and message bus.

| Layer | Key Components |
|-------|----------------|
| Api | SignalR Hub (`NotificationHub`) for real-time notifications |
| Application | `INotificationService` — send to user, send to multiple users, send to all, send to group |
| Application | `IEmailService` — send email (single, multiple, with CC/BCC), HTML templates |
| Application | `IMessageBusService` — publish to queue, publish to exchange, subscribe |
| Infrastructure | `NotificationService` — SignalR `IHubContext<NotificationHub>` implementation |
| Infrastructure | `EmailService` — SMTP client (configurable host/port/credentials) |
| Infrastructure | `MessageBusService` — RabbitMQ placeholder (async API v7.x planned) |

**Key patterns:**
- **SignalR** for real-time in-app notifications (user-specific, group, broadcast)
- **SMTP** email service with configurable sender, HTML body support
- **Message bus** abstraction for future RabbitMQ/Kafka integration
- Domain events trigger notifications (e.g., `IssueAssignedEvent` → notify assignee, `LowStockAlertEvent` → notify managers)

### 4.8 Shared Kernel

| Type | Purpose |
|------|---------|
| `BaseEntity` | Abstract base with `Id` (Guid), `CreatedAt`, `UpdatedAt` timestamps |
| `Result<T>` | Railway-oriented result: `Success(value)` / `Failure(error)` — no exceptions for business flow |
| `PagedResult<T>` | Paged collection with `Items`, `TotalCount`, `Page`, `PageSize` |
| `IDomainEvent` | Interface for domain events: `EventId` (Guid), `OccurredOn` (DateTime) |

### 4.9 Cross-Cutting Infrastructure

| Component | Purpose | Implementation |
|-----------|---------|----------------|
| **MediatR Pipeline Behaviors** | Auto-validation, logging, caching on all commands/queries | `ValidationBehavior<TRequest, TResponse>` — FluentValidation before handler; `LoggingBehavior<TRequest, TResponse>` — structured logging of request/response; `CachingBehavior<TRequest, TResponse>` — automatic cache for queries (SHA256 key, 5-min TTL) |
| **Middleware** | HTTP pipeline concerns | `CorrelationIdMiddleware` — generates/propagates X-Correlation-Id header; `MetricsMiddleware` — request count, latency metrics for Prometheus; `PerformanceTimingMiddleware` — logs slow requests; `UserContextMiddleware` — extracts user info from JWT claims |
| **Caching** | Distributed cache abstraction | `ICacheService` → `RedisCacheService` (IDistributedCache with JSON serialization, sliding expiration) |
| **Health Checks** | Application health monitoring | DB connectivity, memory pressure, disk space, cache availability |
| **Observability** | Tracing, metrics, logging | OpenTelemetry (traces → Jaeger, metrics → Prometheus → Grafana); Serilog (structured JSON, rolling files, correlation ID enrichment) |

---

## 5. Data Architecture

```
┌───────────────────────────────────────────────────────────────────────────┐
│                  SQLite (dev) / PostgreSQL (prod)                        │
│                                                                         │
│  ┌──────────────┐  ┌────────────────┐  ┌────────────────────────────┐  │
│  │ AuthDbContext │  │ IssuesDbContext │  │ InventoryDbContext          │  │
│  │              │  │                │  │                            │  │
│  │ Users        │  │ Issues         │  │ Products                   │  │
│  │ Roles        │  │ IssueComments  │  │ StockMovements             │  │
│  │ UserRoles    │  │ IssueActivities│  │ Suppliers                  │  │
│  │              │  │ IssueTags      │  │ Locations                  │  │
│  └──────────────┘  └────────────────┘  └────────────────────────────┘  │
│                                                                         │
│  ┌─────────────────────────┐  ┌──────────────────────────────────────┐ │
│  │ InventoryIssueDbContext  │  │ UserDbContext                        │ │
│  │                          │  │                                      │ │
│  │ InventoryIssues          │  │ Users (extended profile)             │ │
│  │                          │  │ UserProfiles                         │ │
│  └─────────────────────────┘  └──────────────────────────────────────┘ │
└───────────────────────────────────────────────────────────────────────────┘

┌─────────────────────┐  ┌──────────────────────────┐
│  Redis               │  │  RabbitMQ (future)        │
│  Distributed cache   │  │  Message queues           │
│  Query caching       │  │  Domain event publishing  │
│  Session storage     │  │  Async notifications      │
└─────────────────────┘  └──────────────────────────┘
```

**Key decisions:**
- **Shared database, separate DbContexts** — modules share the database but each has its own EF Core context. This allows future extraction: swap to separate databases per service with minimal code change.
- **No cross-context joins** — if a module needs data from another, it goes through the Application layer/domain events, not through shared tables.
- **Table initialization** — Auth uses `EnsureCreatedAsync()`, other modules use `IRelationalDatabaseCreator` with fallback SQL execution to handle multi-context constraints.
- **Redis for distributed caching** — `CachingBehavior` automatically caches query results (5-min TTL, SHA256 key). Manual cache with `ICacheService` for custom expiration.
- **RabbitMQ for async messaging** (future) — domain events published to exchanges for cross-module and cross-service communication.

---

## 6. CQRS Data Access Strategy

O CQRS real separa **tecnologias de acesso a dados** entre Command (escrita) e Query (leitura):

```
┌──────────────────────────────────────────────────────────────────┐
│                        CQRS Data Access                         │
│                                                                  │
│  COMMAND SIDE (Write)              QUERY SIDE (Read)             │
│  ┌──────────────────────┐         ┌──────────────────────────┐  │
│  │  EF Core              │         │  Dapper                   │  │
│  │                       │         │                           │  │
│  │  • Change tracking    │         │  • Raw SQL                │  │
│  │  • Unit of Work       │         │  • Direct DTO projection  │  │
│  │  • Transactions       │         │  • No tracking overhead   │  │
│  │  • Aggregate persist  │         │  • Optimized JOINs        │  │
│  │  • Cascade operations │         │  • Paged queries          │  │
│  │  • Concurrency ctrl   │         │  • Dashboard aggregations │  │
│  └──────────┬───────────┘         └────────────┬─────────────┘  │
│             │                                   │                │
│             ▼                                   ▼                │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                    Database (SQLite / PostgreSQL)          │  │
│  └───────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────┘
```

### Por que EF Core para Commands?

| Benefício | Explicação |
|-----------|-----------|
| **Change tracking** | O EF Core rastreia mudanças no aggregate root (Issue + Comments + Activities + Tags) e gera um único `SaveChangesAsync()` |
| **Transações automáticas** | Adicionar comentário + criar activity + atualizar UpdatedAt = tudo numa transação |
| **Cascade & relacionamentos** | `Issue.Comments.Add(comment)` propaga FKs automaticamente |
| **Optimistic concurrency** | Suporte nativo a `RowVersion` para controle de concorrência |
| **Aggregate integrity** | O DbContext garante que o aggregate root é salvo como uma unidade |

### Por que Dapper para Queries?

| Benefício | Explicação |
|-----------|-----------|
| **Performance** | Sem overhead de change tracker — a query retorna DTOs direto |
| **Projeção direta** | `SELECT` mapeia direto para `IssueDto`, sem Entity → DTO intermediário |
| **SQL puro** | Controle total sobre JOINs, aggregations, window functions |
| **Paginação otimizada** | `OFFSET/FETCH` ou `ROW_NUMBER()` sem tradução LINQ |
| **Dashboard/Relatórios** | Queries complexas com GROUP BY, COUNT, SUM diretamente |

### Estrutura por módulo

```
Modules/{Name}/
├── Application/
│   ├── Commands/                    # IRequestHandler → usa IRepository (EF Core)
│   └── Queries/                     # IRequestHandler → usa IReadRepository (Dapper)
├── Domain/
│   ├── I{Name}Repository.cs        # Write interface (EF Core)
│   └── I{Name}ReadRepository.cs    # Read interface (Dapper)
└── Infrastructure/
    ├── {Name}DbContext.cs           # EF Core context (write)
    ├── {Name}Repository.cs          # EF Core implementation (write)
    └── {Name}ReadRepository.cs      # Dapper implementation (read)
```

### Exemplo — Query Handler com Dapper

```csharp
// Domain/IIssueReadRepository.cs
public interface IIssueReadRepository
{
    Task<IssueDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<IssueListDto>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
}

// Infrastructure/IssueReadRepository.cs
public class IssueReadRepository : IIssueReadRepository
{
    private readonly IDbConnection _connection;

    public IssueReadRepository(IDbConnection connection)
        => _connection = connection;

    public async Task<PagedResult<IssueListDto>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        const string sql = """
            SELECT i.Id, i.Title, i.Status, i.Priority, i.Type, i.CreatedAt,
                   COUNT(c.Id) AS CommentCount
            FROM Issues i
            LEFT JOIN IssueComments c ON c.IssueId = i.Id
            GROUP BY i.Id
            ORDER BY i.CreatedAt DESC
            LIMIT @PageSize OFFSET @Offset;

            SELECT COUNT(*) FROM Issues;
            """;

        using var multi = await _connection.QueryMultipleAsync(sql, new
        {
            PageSize = pageSize,
            Offset = (page - 1) * pageSize
        });

        var items = (await multi.ReadAsync<IssueListDto>()).ToList();
        var totalCount = await multi.ReadSingleAsync<int>();

        return new PagedResult<IssueListDto>(items, totalCount, page, pageSize);
    }
}
```

### Exemplo — Command Handler com EF Core

```csharp
// Application/Commands/CreateIssueCommandHandler.cs
public class CreateIssueCommandHandler : IRequestHandler<CreateIssueCommand, Result<IssueDto>>
{
    private readonly IIssueRepository _repository; // EF Core

    public CreateIssueCommandHandler(IIssueRepository repository)
        => _repository = repository;

    public async Task<Result<IssueDto>> Handle(CreateIssueCommand request, CancellationToken ct)
    {
        var issue = new Issue
        {
            Title = request.Title,
            Description = request.Description,
            Type = request.Type,
            Priority = request.Priority
        };

        foreach (var tag in request.Tags)
            issue.Tags.Add(new IssueTag { Value = tag });

        await _repository.AddAsync(issue, ct);
        await _repository.SaveChangesAsync(ct); // EF Core Unit of Work

        return Result<IssueDto>.Success(issue.ToDto());
    }
}
```

### Registro no DI

```csharp
// IssuesModuleExtensions.cs
public static IServiceCollection AddIssuesModule(this IServiceCollection services, IConfiguration config)
{
    var connectionString = config.GetConnectionString("DefaultConnection");

    // EF Core — write side
    services.AddDbContext<IssuesDbContext>(options => options.UseSqlite(connectionString));
    services.AddScoped<IIssueRepository, IssueRepository>();

    // Dapper — read side
    services.AddScoped<IDbConnection>(_ => new SqliteConnection(connectionString));
    services.AddScoped<IIssueReadRepository, IssueReadRepository>();

    // Validators...
    return services;
}
```

---

## 7. Request Pipeline

```
HTTP Request
    │
    ▼
┌──────────────────┐
│ CorrelationId    │  Generates/propagates X-Correlation-Id header
│ Middleware       │
├──────────────────┤
│ Metrics          │  Request count, latency → Prometheus
│ Middleware       │
├──────────────────┤
│ Performance      │  Logs slow requests (> threshold)
│ Timing           │
├──────────────────┤
│ Authentication   │  JWT Bearer validation
│ Middleware       │
├──────────────────┤
│ UserContext      │  Extracts user info from JWT claims
│ Middleware       │
├──────────────────┤
│ Authorization    │  .RequireAuthorization() + [Authorize(Roles = "Admin")]
│ Middleware       │
├──────────────────┤
│ Output Cache     │  ASP.NET Core Output Caching (Analytics, Users policies)
│ Middleware       │
├──────────────────┤
│ Minimal API      │  Route matching → endpoint delegate
│ Router           │
├──────────────────┤
│ Endpoint         │  Parses request → sends MediatR command/query
│ Handler          │
├──────────────────┤
│ ValidationBehavior │  FluentValidation — auto-validates all commands
│ (MediatR Pipeline) │
├──────────────────┤
│ LoggingBehavior  │  Structured logging of request/response
│ (MediatR Pipeline) │
├──────────────────┤
│ CachingBehavior  │  Auto-cache queries (SHA256 key, 5-min TTL)
│ (MediatR Pipeline) │
├──────────────────┤
│ Domain Logic     │  Entity methods, aggregate root operations
│                  │  Returns Result<T> — raises IDomainEvent
├──────────────────┤
│ Data Access      │  COMMAND → EF Core (change tracking, transactions)
│                  │  QUERY  → Dapper (raw SQL, direct DTO projection)
└──────────────────┘
    │
    ▼
HTTP Response (Result<T> → IResult via endpoint mapping)
```

---

## 8. Evolutionary Roadmap

The architecture is designed to evolve incrementally. Each phase adds capability without rewriting what exists. This roadmap reflects the full feature scope discovered in the IMS legacy project.

### Phase 1 — Foundation (Baseline) ✅

- Two modules: Auth, Issues
- SQLite, single deployable
- JWT authentication + RBAC (Admin, User roles)
- CQRS via MediatR (commands + queries)
- FluentValidation on all commands
- Swagger UI
- Seed data on startup
- EF Core for writes, Dapper for reads

### Phase 2 — Cross-Cutting Hardening 🔜

| Enhancement | Description | Skill Reference |
|-------------|-------------|-----------------|
| **MediatR Pipeline Behaviors** | `ValidationBehavior` (auto-validate), `LoggingBehavior` (structured request/response logging), `CachingBehavior` (auto-cache queries with SHA256 key, 5-min TTL) | `core-project-patterns` |
| **Middleware Stack** | `CorrelationIdMiddleware` (X-Correlation-Id propagation), `MetricsMiddleware` (request count/latency → Prometheus), `PerformanceTimingMiddleware` (slow request logging), `UserContextMiddleware` (JWT claim extraction) | `api-project-patterns` |
| **Global Error Handling** | Exception middleware + `Result<T>` to HTTP mapping (`ProblemDetails` RFC 7807) | `error-mapping` |
| **Structured Logging** | Serilog with structured log templates, JSON output, rolling files, correlation ID enrichment | `observability` |
| **Health Checks** | ASP.NET Core health check framework (DB connectivity, memory pressure, disk space, cache availability) | `api-project-patterns` |
| **Output Caching** | ASP.NET Core Output Caching with policies (Analytics — 10min, Users — 5min, Inventory — 30s) | `api-project-patterns` |
| **Rate Limiting** | ASP.NET Core rate limiter middleware on auth endpoints | `security-vulnerabilities` |
| **Redis Cache** | `ICacheService` → `RedisCacheService` (IDistributedCache, JSON serialization, sliding expiration) | `infrastructure-integrations` |

### Phase 3 — Inventory Module 📋

| Module | Purpose | Key Features |
|--------|---------|-------------|
| **Inventory (Products)** | Product catalog and stock management | CRUD, SKU, barcode, category, stock levels (min/max), pricing (unit + cost), expiry dates, stock status auto-calculation |
| **Inventory (Stock Movements)** | Track all stock changes | Create, bulk create, adjust, transfer between locations; 15 movement types (StockIn, StockOut, Adjustment, Transfer, Sale, Purchase, Return, Damage, Loss, Expired, etc.) |
| **Inventory (Suppliers)** | Supplier relationship management | CRUD, activate/deactivate, contact management, credit limits, payment terms, geographic data |
| **Inventory (Locations)** | Warehouse and storage management | CRUD, hierarchical locations (ParentLocationId), location types (Warehouse, Store, Aisle, Shelf, etc.), capacity tracking, activate/deactivate |

**Domain events added:** `ProductCreatedEvent`, `StockChangedEvent`, `LowStockAlertEvent`, `OutOfStockEvent`, `StockReplenishedEvent`, `ProductDiscontinuedEvent`, `PriceChangedEvent`, `StockTransferInitiatedEvent/CompletedEvent`, `SupplierCreatedEvent/DeactivatedEvent`, `LocationCreatedEvent/DeactivatedEvent`, `ProductExpiringSoonEvent`, `ProductExpiredEvent`

### Phase 4 — Inventory Issues + Analytics 📋

| Module | Purpose | Key Features |
|--------|---------|-------------|
| **Inventory Issues** | Track inventory-specific problems | Full lifecycle (create, update, assign, resolve, close, reopen, delete); filter by product/location/type/priority/status/reporter/assignee; overdue and high-priority views; statistics endpoint |
| **Analytics** | Comprehensive reporting for issues and inventory | Issue summary/trends/resolution time/statistics; user workload analytics; workload-based auto-assignment suggestion; dashboard KPIs; export (JSON/CSV/PDF); grouping by status/priority/assignee |
| **Inventory Analytics** | Inventory-specific analytics | Inventory value/summary; stock status/trends; category distribution; turnover rate; expiring products report; location capacity; supplier performance; top products |

### Phase 5 — User Management + Notifications 📋

| Module | Purpose | Key Features |
|--------|---------|-------------|
| **User Management** | Full user lifecycle beyond auth | Paginated user list with search (Admin); profile management (/me endpoint); get by role; activate/deactivate; change password; assign/remove roles; domain events (login tracking, profile changes, role changes) |
| **Notifications** | Multi-channel notification delivery | **SignalR** — real-time in-app (user-specific, group, broadcast); **SMTP Email** — configurable sender, HTML templates; **Message Bus** — RabbitMQ queue/exchange publishing; domain event → notification mapping |

**Domain events added:** `UserCreatedEvent`, `UserProfileUpdatedEvent`, `UserActivatedEvent`, `UserDeactivatedEvent`, `UserPasswordChangedEvent`, `UserRoleAssignedEvent/RemovedEvent`, `UserLoggedInEvent`

### Phase 6 — Integration & Messaging 📡

| Enhancement | Description | Skill Reference |
|-------------|-------------|-----------------|
| **Domain Events** | In-process domain events via MediatR `INotification` for cross-module communication | `architecture-overview` |
| **RabbitMQ** | `IMessageBusService` → full RabbitMQ.Client v7.x async implementation; outbox pattern for reliable event publishing | `eventhub-producer`, `eventhub-consumer` |
| **External API Integration** | HttpClient-based integrations with retry policies (Polly) and Redis cache | `infrastructure-integrations` |
| **AOT Serialization** | `JsonSerializerContext` source generators for message payloads | `source-generators-aot` |

### Phase 7 — Production Readiness 🚀

| Enhancement | Description | Skill Reference |
|-------------|-------------|-----------------|
| **PostgreSQL Migration** | Swap SQLite for PostgreSQL (change connection string + provider, EF migrations) | `infrastructure-integrations` |
| **Docker & Compose** | Multi-stage build; docker-compose with: PostgreSQL, Redis, RabbitMQ, Prometheus, Grafana, app | `api-project-patterns` |
| **CI/CD Pipeline** | GitHub Actions: build → test → SonarQube → deploy | `code-smells`, `testing-patterns` |
| **OpenTelemetry** | Traces → Jaeger, Metrics → Prometheus → Grafana, Logs → structured JSON | `observability` |
| **Feature Flags** | Microsoft.FeatureManagement for progressive rollouts | — |

### Phase 8 — Service Extraction (Optional) 🔮

When a module needs independent scaling:
1. Extract module directory into a new .NET project
2. Replace in-process MediatR calls with HTTP/gRPC/messaging
3. Replace in-process domain events with RabbitMQ/Kafka events
4. Give the module its own database
5. Deploy independently

**This works because:**
- Modules already have separate DbContexts
- No cross-module entity references
- Communication is through Application layer contracts and domain events, not shared state
- `IMessageBusService` abstraction supports both in-process and distributed messaging

---

## 9. Quality Gates

Every PR must pass:

| Gate | Tool | Reference |
|------|------|-----------|
| Code smells | SonarQube rules | `code-smells` skill |
| Critical bugs | Static analysis | `critical-bugs` skill |
| Security scan | OWASP rules | `security-vulnerabilities` skill |
| Unit tests | xUnit + Moq (AAA pattern) | `testing-patterns` skill |
| Coverage | Minimum 80% on new code | `testing-patterns` skill |
| Architecture fit | Morpheus review | `architecture-overview` skill |

---

## 10. Key Decisions

| Decision | Rationale | Alternatives Considered |
|----------|-----------|------------------------|
| Modular monolith over microservices | Team of 1-3 devs; avoid distributed complexity early; extract later if needed | Microservices (too complex for current scale) |
| SQLite for dev, PostgreSQL for prod | Zero-config, file-based, fast startup for dev; PostgreSQL for production scale and features | PostgreSQL from day 1 (overkill for dev) |
| MediatR for CQRS | Decouples handlers from endpoints; enables pipeline behaviors (validation, logging, caching); well-supported | Direct service calls (less flexible), Wolverine (less mature ecosystem) |
| FluentValidation over DataAnnotations | Testable, composable, supports complex rules | DataAnnotations (not testable, limited) |
| JWT Bearer over cookies | API-first design, mobile-friendly, stateless | Cookie auth (not API-friendly) |
| Result<T> over exceptions | Explicit error handling, composable, no hidden control flow | Exceptions (implicit, expensive, hard to compose) |
| Separate DbContexts per module | Module autonomy, future extraction | Single shared DbContext (couples modules) |
| EF Core writes + Dapper reads | CQRS verdadeiro: EF Core oferece change tracking, Unit of Work e integridade de aggregates no write side; Dapper oferece SQL puro, projeção direta para DTOs e zero overhead no read side | EF Core para ambos (overhead em queries, tracking desnecessário em reads), Dapper para ambos (perde change tracking, transações automáticas e integridade de aggregates) |
| Redis for distributed caching | CachingBehavior auto-caches queries; ICacheService for manual control; supports future multi-instance deployment | In-memory cache only (not distributable), no caching (performance loss) |
| SignalR for real-time notifications | Built-in ASP.NET Core support, WebSocket-based, user/group targeting | Polling (inefficient), external push service (complexity) |
| RabbitMQ for messaging | Industry standard, supports queues + exchanges + routing; async v7.x API | Kafka (overkill for current scale), in-process only (no external consumers) |
| Output caching on analytics | Analytics queries are expensive; 10-min cache reduces DB load | No caching (unnecessary DB pressure), CDN (not applicable for API) |
| Domain events for cross-module communication | Loose coupling, event-driven, supports both in-process (MediatR) and distributed (RabbitMQ) | Direct service calls (tight coupling), shared database (hidden dependencies) |

---

## 11. Non-Functional Requirements

| Requirement | Target | Current Status |
|-------------|--------|----------------|
| API response time (p95) | < 200ms | ✅ (SQLite in-process) |
| API response time with cache (p95) | < 50ms | 📋 Pending (Phase 2 — Redis) |
| Startup time | < 3s | ✅ |
| Test coverage | ≥ 80% on new code | 📋 Pending (Trinity) |
| Authentication | JWT Bearer with refresh + RBAC | ⚠️ JWT only (no refresh token yet) |
| Database | PostgreSQL in prod, SQLite in dev | 📋 Pending (Phase 7) |
| Deployment | Docker container + docker-compose (PostgreSQL, Redis, RabbitMQ, Prometheus, Grafana) | 📋 Pending (Phase 7) |
| Observability | Structured logging + traces + metrics | 📋 Pending (Phase 2) |
| Caching | Redis distributed cache (queries: 5-min TTL, analytics: 10-min, users: 5-min) | 📋 Pending (Phase 2) |
| Real-time notifications | SignalR WebSocket | 📋 Pending (Phase 5) |
| Email notifications | SMTP service (configurable) | 📋 Pending (Phase 5) |
| Messaging | RabbitMQ async (queues + exchanges) | 📋 Pending (Phase 6) |
| Health monitoring | DB, memory, disk, cache health checks | 📋 Pending (Phase 2) |
| Analytics | Issue stats, user workload, inventory value, stock trends, export (JSON/CSV/PDF) | 📋 Pending (Phase 4) |
| Inventory management | Products, stock movements, suppliers, locations, 15+ movement types | 📋 Pending (Phase 3) |

---

## 12. API Surface Summary

Total planned endpoints across all modules:

| Module | Endpoint Group | Approximate Endpoints |
|--------|---------------|----------------------|
| Auth | `/api/auth/*` | 2 (register, login) |
| Issues | `/api/issues/*` | 7 (CRUD + status + comments) |
| Inventory Products | `/api/inventory/products/*` | 10+ (CRUD + filters + stock adjust + transfer + discontinue + pricing) |
| Inventory Stock Movements | `/api/inventory/stock-movements/*` | 5 (create, bulk, adjust, transfer, delete) |
| Inventory Suppliers | `/api/inventory/suppliers/*` | 7 (CRUD + activate/deactivate + contact update) |
| Inventory Locations | `/api/inventory/locations/*` | 7 (CRUD + activate/deactivate + capacity update) |
| Inventory Analytics | `/api/inventory/analytics/*` | 13 (value, summary, trends, categories, status, expiring, turnover, capacity, supplier performance) |
| Inventory Issues | `/api/inventory-issues/*` | 16 (CRUD + lifecycle + filters + statistics + search) |
| Analytics (Issues) | `/api/analytics/issues/*` | 8 (summary, trends, resolution-time, statistics, by-status, by-priority, by-assignee, suggest-assignee) |
| Analytics (Users) | `/api/analytics/users/*` | 3 (workload all, workload by user, statistics) |
| Analytics (Dashboard) | `/api/analytics/dashboard` | 1 |
| Analytics (Export) | `/api/analytics/export/*` | 2 (GET + POST export) |
| User Management | `/api/users/*` | 11 (list + search + by-id + me + by-role + active + update + profile + activate/deactivate + password + roles + delete) |
| System | `/health/*` | 1 (ping) |
| **Total** | | **~93 endpoints** |

---

*This is a living document. All changes require Morpheus review and a decision entry in `.squad/decisions.md`.*
