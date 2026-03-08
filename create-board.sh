#!/bin/bash
# =============================================================================
# IMS Modular Monolith — GitHub Project Board Setup
# Creates User Stories (as Issues) with Task checklists and adds to Project
# =============================================================================

set -euo pipefail

REPO="peleverton/ims-monolith"
PROJECT_NUMBER=2

add_to_project() {
  local issue_url="$1"
  gh project item-add "$PROJECT_NUMBER" --owner peleverton --url "$issue_url" 2>/dev/null || true
}

echo "============================================"
echo "PHASE 1 — Foundation (Already Done ✅)"
echo "============================================"

# US-001: Auth Module
US001=$(gh issue create --repo "$REPO" \
  --title "US-001: Auth Module — JWT Registration & Login" \
  --label "type:user-story,phase:1-foundation,module:auth,priority:high,agent:neo" \
  --body '## User Story
**As a** user of the IMS system,
**I want to** register an account and log in with JWT authentication,
**So that** I can securely access protected resources.

## Acceptance Criteria
- [x] POST `/api/auth/register` creates a new user with hashed password (BCrypt)
- [x] POST `/api/auth/login` returns a JWT token (HS256) with user claims
- [x] Roles: Admin, User — seeded on startup
- [x] Admin seed: `admin@ims.com` / `Admin@123`
- [x] FluentValidation on RegisterRequest and LoginRequest
- [x] AuthDbContext with EF Core (User, Role, UserRole entities)
- [x] JwtService for token generation

## Tasks
- [x] Domain: `User` aggregate root, `Role`, `UserRole` join entity
- [x] Infrastructure: `AuthDbContext`, `JwtService` (HS256 token generation)
- [x] Application: `RegisterRequest`, `LoginRequest`, `TokenResponse` DTOs
- [x] Application: FluentValidation validators for register/login
- [x] Api: `AuthEndpoints.cs` — register + login endpoints
- [x] DI: `AddAuthModule()` extension method
- [x] Seed: Roles (Admin, User) + admin user on startup

## Status: ✅ Complete
')
echo "Created: US-001 → $US001"
add_to_project "$US001"

# US-002: Issues Module
US002=$(gh issue create --repo "$REPO" \
  --title "US-002: Issues Module — CRUD + Status Workflow + Comments" \
  --label "type:user-story,phase:1-foundation,module:issues,priority:high,agent:neo" \
  --body '## User Story
**As an** authenticated user,
**I want to** create, manage, and track issues with status transitions and comments,
**So that** I can manage work items through their lifecycle.

## Acceptance Criteria
- [x] Full CRUD on Issues (create, read, update, delete)
- [x] Status transitions (Open → InProgress → Resolved → Closed, with Reopen)
- [x] Comments on issues (add, list)
- [x] Pagination on issue list
- [x] All endpoints require JWT authentication
- [x] CQRS via MediatR (separate commands and queries)
- [x] EF Core for writes, Dapper for reads

## Tasks
- [x] Domain: `Issue` aggregate root, `Comment` entity, `IssueStatus` enum
- [x] Infrastructure: `IssuesDbContext` (EF Core), Dapper read queries
- [x] Application: MediatR Commands (CreateIssue, UpdateIssue, DeleteIssue, ChangeStatus, AddComment)
- [x] Application: MediatR Queries (GetIssues, GetIssueById)
- [x] Application: DTOs and FluentValidation validators
- [x] Api: `IssuesEndpoints.cs` — 7 endpoints under `/api/issues`
- [x] DI: `AddIssuesModule()` extension method

## Status: ✅ Complete
')
echo "Created: US-002 → $US002"
add_to_project "$US002"

echo ""
echo "============================================"
echo "PHASE 2 — Cross-Cutting Hardening"
echo "============================================"

# US-003: MediatR Pipeline Behaviors
US003=$(gh issue create --repo "$REPO" \
  --title "US-003: MediatR Pipeline Behaviors — Validation, Logging, Caching" \
  --label "type:user-story,phase:2-cross-cutting,cross-cutting,priority:high,agent:neo" \
  --body '## User Story
**As a** developer,
**I want** cross-cutting concerns handled automatically in the MediatR pipeline,
**So that** every command/query is validated, logged, and optionally cached without boilerplate.

## Acceptance Criteria
- [ ] `ValidationBehavior<TRequest, TResponse>` — auto-validates requests via FluentValidation; returns `Result.Failure` on validation errors
- [ ] `LoggingBehavior<TRequest, TResponse>` — logs request type, handler name, execution time, correlation ID
- [ ] `CachingBehavior<TRequest, TResponse>` — auto-caches query responses using SHA256 key with configurable TTL (default 5 min)
- [ ] Behaviors registered in DI pipeline in correct order (Validation → Logging → Caching → Handler)

## Tasks
- [ ] Implement `ValidationBehavior` with FluentValidation integration
- [ ] Implement `LoggingBehavior` with Serilog structured logging
- [ ] Implement `CachingBehavior` with `ICacheService` and SHA256 key generation
- [ ] Create `ICacheable` marker interface for cacheable queries
- [ ] Register behaviors in `AddSharedInfrastructure()` DI method
- [ ] Unit tests for each behavior (Trinity)

## Skill Reference
`core-project-patterns`
')
echo "Created: US-003 → $US003"
add_to_project "$US003"

# US-004: Middleware Stack
US004=$(gh issue create --repo "$REPO" \
  --title "US-004: Middleware Stack — CorrelationId, Metrics, Performance, UserContext" \
  --label "type:user-story,phase:2-cross-cutting,cross-cutting,priority:high,agent:neo" \
  --body '## User Story
**As a** DevOps engineer,
**I want** every HTTP request enriched with correlation IDs, metrics, and performance data,
**So that** I can trace, monitor, and debug requests across the system.

## Acceptance Criteria
- [ ] `CorrelationIdMiddleware` — reads/generates `X-Correlation-Id` header, adds to Serilog LogContext
- [ ] `MetricsMiddleware` — records request count and latency histograms (Prometheus-compatible)
- [ ] `PerformanceTimingMiddleware` — logs warning for requests exceeding 500ms threshold
- [ ] `UserContextMiddleware` — extracts JWT claims (UserId, Email, Roles) into scoped `IUserContext`
- [ ] Middleware registered in correct pipeline order in `Program.cs`

## Tasks
- [ ] Implement `CorrelationIdMiddleware` with header propagation
- [ ] Implement `MetricsMiddleware` with `System.Diagnostics.Metrics` or Prometheus .NET
- [ ] Implement `PerformanceTimingMiddleware` with configurable threshold
- [ ] Implement `UserContextMiddleware` and `IUserContext` service
- [ ] Register middleware in `Program.cs` in correct order
- [ ] Unit tests for each middleware (Trinity)

## Skill Reference
`api-project-patterns`
')
echo "Created: US-004 → $US004"
add_to_project "$US004"

# US-005: Global Error Handling
US005=$(gh issue create --repo "$REPO" \
  --title "US-005: Global Error Handling — Exception Middleware + ProblemDetails" \
  --label "type:user-story,phase:2-cross-cutting,cross-cutting,priority:high,agent:neo" \
  --body '## User Story
**As a** API consumer,
**I want** consistent error responses in RFC 7807 ProblemDetails format,
**So that** I can reliably handle errors in my client application.

## Acceptance Criteria
- [ ] Global exception middleware catches unhandled exceptions and returns `ProblemDetails`
- [ ] `Result<T>` failures mapped to appropriate HTTP status codes (400, 404, 409, 422, 500)
- [ ] Validation errors return 422 with field-level error details
- [ ] All error responses include correlation ID
- [ ] No stack traces in production responses

## Tasks
- [ ] Implement `ExceptionHandlingMiddleware`
- [ ] Create `Result<T>` → `IResult` mapping extension methods
- [ ] Define error code catalog (validation, not-found, conflict, unauthorized, internal)
- [ ] Register middleware in `Program.cs`
- [ ] Unit tests for error mapping (Trinity)

## Skill Reference
`error-mapping`
')
echo "Created: US-005 → $US005"
add_to_project "$US005"

# US-006: Structured Logging
US006=$(gh issue create --repo "$REPO" \
  --title "US-006: Structured Logging — Serilog + JSON + Correlation ID Enrichment" \
  --label "type:user-story,phase:2-cross-cutting,cross-cutting,priority:medium,agent:neo" \
  --body '## User Story
**As a** operator monitoring the system,
**I want** structured JSON logs enriched with correlation IDs and request context,
**So that** I can efficiently search, filter, and trace issues in production.

## Acceptance Criteria
- [ ] Serilog configured with structured log templates
- [ ] JSON output sink (file + console)
- [ ] Rolling file logs (daily, 31-day retention)
- [ ] Correlation ID enrichment on every log entry
- [ ] Request/response logging with configurable verbosity
- [ ] Sensitive data (passwords, tokens) excluded from logs

## Tasks
- [ ] Configure Serilog in `Program.cs` with JSON + File sinks
- [ ] Create `CorrelationIdEnricher` for Serilog
- [ ] Configure rolling file policy (daily, 100MB limit, 31-day retention)
- [ ] Add structured log properties (module, handler, userId, correlationId)
- [ ] Verify sensitive data exclusion

## Skill Reference
`observability`
')
echo "Created: US-006 → $US006"
add_to_project "$US006"

# US-007: Health Checks
US007=$(gh issue create --repo "$REPO" \
  --title "US-007: Health Checks — DB, Memory, Disk, Cache" \
  --label "type:user-story,phase:2-cross-cutting,cross-cutting,priority:medium,agent:neo" \
  --body '## User Story
**As a** infrastructure operator,
**I want** health check endpoints that report the status of all dependencies,
**So that** load balancers and monitoring systems can detect unhealthy instances.

## Acceptance Criteria
- [ ] `/health` — overall system health (Healthy/Degraded/Unhealthy)
- [ ] DB connectivity check (EF Core DbContext)
- [ ] Memory pressure check (configurable threshold)
- [ ] Disk space check (configurable threshold)
- [ ] Redis cache connectivity check (when available)
- [ ] JSON response with per-check details

## Tasks
- [ ] Add `AspNetCore.HealthChecks` NuGet packages
- [ ] Register DB health check per DbContext
- [ ] Implement memory pressure health check
- [ ] Implement disk space health check
- [ ] Register Redis health check (conditional on Redis config)
- [ ] Map health check endpoint in `Program.cs`
- [ ] Integration tests for health endpoint (Trinity)

## Skill Reference
`api-project-patterns`
')
echo "Created: US-007 → $US007"
add_to_project "$US007"

# US-008: Output Caching + Redis
US008=$(gh issue create --repo "$REPO" \
  --title "US-008: Output Caching + Redis Distributed Cache" \
  --label "type:user-story,phase:2-cross-cutting,cross-cutting,priority:medium,agent:neo" \
  --body '## User Story
**As a** API consumer,
**I want** frequently-accessed data served from cache,
**So that** API responses are fast and database load is minimized.

## Acceptance Criteria
- [ ] `ICacheService` interface with Get/Set/Remove/Exists methods
- [ ] `RedisCacheService` implementing `ICacheService` via `IDistributedCache`
- [ ] `InMemoryCacheService` fallback when Redis is not configured
- [ ] ASP.NET Core Output Caching with policies: Analytics (10min), Users (5min), Inventory (30s)
- [ ] Cache key generation with SHA256 hashing
- [ ] JSON serialization for cached objects
- [ ] Sliding expiration support

## Tasks
- [ ] Define `ICacheService` interface in Shared Kernel
- [ ] Implement `RedisCacheService` with `IDistributedCache`
- [ ] Implement `InMemoryCacheService` fallback
- [ ] Configure Output Caching policies in `Program.cs`
- [ ] Add Redis connection string to `appsettings.json`
- [ ] Register cache services in DI (auto-detect Redis availability)
- [ ] Unit tests for cache service (Trinity)

## Skill Reference
`infrastructure-integrations`
')
echo "Created: US-008 → $US008"
add_to_project "$US008"

# US-009: Rate Limiting
US009=$(gh issue create --repo "$REPO" \
  --title "US-009: Rate Limiting — Auth Endpoints Protection" \
  --label "type:user-story,phase:2-cross-cutting,cross-cutting,priority:medium,agent:neo" \
  --body '## User Story
**As a** security engineer,
**I want** rate limiting on authentication endpoints,
**So that** brute-force attacks are mitigated.

## Acceptance Criteria
- [ ] Fixed window rate limiter on `/api/auth/login` (10 requests/min per IP)
- [ ] Fixed window rate limiter on `/api/auth/register` (5 requests/min per IP)
- [ ] Global rate limiter (100 requests/min per IP) as fallback
- [ ] `429 Too Many Requests` response with `Retry-After` header
- [ ] Rate limit configuration via `appsettings.json`

## Tasks
- [ ] Configure ASP.NET Core rate limiting middleware
- [ ] Define rate limiting policies (auth, global)
- [ ] Apply policies to auth endpoints
- [ ] Add rate limit settings to `appsettings.json`
- [ ] Integration tests for rate limiting (Trinity)

## Skill Reference
`security-vulnerabilities`
')
echo "Created: US-009 → $US009"
add_to_project "$US009"

echo ""
echo "============================================"
echo "PHASE 3 — Inventory Module"
echo "============================================"

# US-010: Inventory Products
US010=$(gh issue create --repo "$REPO" \
  --title "US-010: Inventory Products — Product Catalog & Stock Management" \
  --label "type:user-story,phase:3-inventory,module:inventory,priority:high,agent:neo" \
  --body '## User Story
**As a** warehouse manager,
**I want to** manage a product catalog with stock levels, pricing, and expiry tracking,
**So that** I can maintain accurate inventory records.

## Acceptance Criteria
- [ ] Full CRUD on Products (create, read, update, delete)
- [ ] Product properties: Name, SKU, Barcode, Category, Description
- [ ] Stock levels: CurrentStock, MinimumStock, MaximumStock
- [ ] Pricing: UnitPrice, CostPrice
- [ ] Expiry date tracking
- [ ] Stock status auto-calculation (InStock, LowStock, OutOfStock, Overstocked)
- [ ] Paginated list with search/filter (by category, status, name)
- [ ] Stock adjustment endpoint
- [ ] Stock transfer between locations
- [ ] Discontinue product endpoint
- [ ] ~10 endpoints under `/api/inventory/products`

## Tasks
- [ ] Domain: `Product` aggregate root with stock calculation logic
- [ ] Domain: `StockStatus` enum (InStock, LowStock, OutOfStock, Overstocked)
- [ ] Domain: Domain events (ProductCreated, LowStockAlert, OutOfStock, PriceChanged, ProductDiscontinued)
- [ ] Infrastructure: `InventoryDbContext` with Product entity configuration
- [ ] Application: CQRS Commands (Create, Update, Delete, AdjustStock, Transfer, Discontinue)
- [ ] Application: CQRS Queries (GetAll paginated, GetById, GetByCategory, GetByStatus)
- [ ] Application: DTOs, validators, mappers
- [ ] Api: `ProductEndpoints.cs`
- [ ] DI: `AddInventoryModule()` extension method
- [ ] Unit tests (Trinity)

## Skill Reference
`minimal-api-modules`, `core-project-patterns`
')
echo "Created: US-010 → $US010"
add_to_project "$US010"

# US-011: Stock Movements
US011=$(gh issue create --repo "$REPO" \
  --title "US-011: Stock Movements — Track All Stock Changes" \
  --label "type:user-story,phase:3-inventory,module:inventory,priority:high,agent:neo" \
  --body '## User Story
**As a** warehouse operator,
**I want to** record every stock change with full traceability,
**So that** I have a complete audit trail of inventory movements.

## Acceptance Criteria
- [ ] Create stock movement (single)
- [ ] Bulk create stock movements
- [ ] Adjust stock (correction)
- [ ] Transfer between locations
- [ ] Delete movement (soft)
- [ ] 15 movement types: StockIn, StockOut, Adjustment, Transfer, Sale, Purchase, Return, Damage, Loss, Expired, WriteOff, Correction, InitialStock, Recount, Other
- [ ] Each movement records: Product, Location, Quantity, Type, Reference, Notes, Timestamp, UserId
- [ ] Movement automatically updates Product stock levels
- [ ] 5 endpoints under `/api/inventory/stock-movements`

## Tasks
- [ ] Domain: `StockMovement` entity, `MovementType` enum (15 types)
- [ ] Domain: Domain events (StockChanged, StockTransferInitiated, StockTransferCompleted)
- [ ] Infrastructure: EF Core entity configuration for StockMovement
- [ ] Application: Commands (CreateMovement, BulkCreate, Adjust, Transfer, Delete)
- [ ] Application: Queries (GetMovements with filters)
- [ ] Application: DTOs, validators
- [ ] Api: `StockMovementEndpoints.cs`
- [ ] Unit tests (Trinity)
')
echo "Created: US-011 → $US011"
add_to_project "$US011"

# US-012: Suppliers
US012=$(gh issue create --repo "$REPO" \
  --title "US-012: Inventory Suppliers — Supplier Relationship Management" \
  --label "type:user-story,phase:3-inventory,module:inventory,priority:medium,agent:neo" \
  --body '## User Story
**As a** procurement manager,
**I want to** manage suppliers with contact details, credit limits, and payment terms,
**So that** I can track supplier relationships and procurement data.

## Acceptance Criteria
- [ ] Full CRUD on Suppliers
- [ ] Activate/deactivate supplier
- [ ] Contact management (email, phone, address)
- [ ] Credit limit and payment terms
- [ ] Geographic data (city, state, country)
- [ ] Paginated list with search
- [ ] 7 endpoints under `/api/inventory/suppliers`

## Tasks
- [ ] Domain: `Supplier` entity with contact properties
- [ ] Domain: Domain events (SupplierCreated, SupplierDeactivated)
- [ ] Infrastructure: EF Core entity configuration
- [ ] Application: CQRS Commands + Queries
- [ ] Application: DTOs, validators
- [ ] Api: `SupplierEndpoints.cs`
- [ ] Unit tests (Trinity)
')
echo "Created: US-012 → $US012"
add_to_project "$US012"

# US-013: Locations
US013=$(gh issue create --repo "$REPO" \
  --title "US-013: Inventory Locations — Warehouse & Storage Management" \
  --label "type:user-story,phase:3-inventory,module:inventory,priority:medium,agent:neo" \
  --body '## User Story
**As a** warehouse manager,
**I want to** manage hierarchical storage locations with capacity tracking,
**So that** I can organize and optimize warehouse space.

## Acceptance Criteria
- [ ] Full CRUD on Locations
- [ ] Hierarchical locations (ParentLocationId)
- [ ] Location types: Warehouse, Store, Aisle, Shelf, Bin, Zone, Area, Other
- [ ] Capacity tracking (max capacity, current usage)
- [ ] Activate/deactivate location
- [ ] 7 endpoints under `/api/inventory/locations`

## Tasks
- [ ] Domain: `Location` entity with hierarchy and capacity
- [ ] Domain: `LocationType` enum
- [ ] Domain: Domain events (LocationCreated, LocationDeactivated)
- [ ] Infrastructure: EF Core entity configuration (self-referencing FK)
- [ ] Application: CQRS Commands + Queries
- [ ] Application: DTOs, validators
- [ ] Api: `LocationEndpoints.cs`
- [ ] Unit tests (Trinity)
')
echo "Created: US-013 → $US013"
add_to_project "$US013"

echo ""
echo "============================================"
echo "PHASE 4 — Inventory Issues + Analytics"
echo "============================================"

# US-014: Inventory Issues
US014=$(gh issue create --repo "$REPO" \
  --title "US-014: Inventory Issues — Inventory-Specific Issue Tracking" \
  --label "type:user-story,phase:4-analytics,module:inventory,priority:high,agent:neo" \
  --body '## User Story
**As a** warehouse supervisor,
**I want to** track inventory-specific problems (damage, loss, discrepancies) with a full lifecycle,
**So that** I can resolve inventory issues efficiently and maintain data quality.

## Acceptance Criteria
- [ ] Full lifecycle: Create → Update → Assign → Resolve → Close → Reopen → Delete
- [ ] Filter by: product, location, type, priority, status, reporter, assignee
- [ ] Overdue issues view
- [ ] High-priority issues view
- [ ] Statistics endpoint (counts by status, by priority, by type)
- [ ] Search by description/title
- [ ] 16 endpoints under `/api/inventory-issues`

## Tasks
- [ ] Domain: `InventoryIssue` aggregate root, `InventoryIssueType` enum, `InventoryIssuePriority` enum
- [ ] Infrastructure: EF Core configuration + Dapper read queries
- [ ] Application: Commands (Create, Update, Assign, Resolve, Close, Reopen, Delete)
- [ ] Application: Queries (GetAll, GetById, Filter, Overdue, HighPriority, Statistics, Search)
- [ ] Application: DTOs, validators
- [ ] Api: `InventoryIssueEndpoints.cs`
- [ ] Unit tests (Trinity)
')
echo "Created: US-014 → $US014"
add_to_project "$US014"

# US-015: Issue Analytics
US015=$(gh issue create --repo "$REPO" \
  --title "US-015: Issue Analytics — Summary, Trends, Resolution Time, Statistics" \
  --label "type:user-story,phase:4-analytics,module:analytics,priority:high,agent:neo" \
  --body '## User Story
**As a** project manager,
**I want** comprehensive analytics on issue metrics (trends, resolution times, workload),
**So that** I can make data-driven decisions about team capacity and process improvements.

## Acceptance Criteria
- [ ] Issue summary (total, open, closed, in-progress, resolved)
- [ ] Issue trends over time (configurable period)
- [ ] Average resolution time by priority/status
- [ ] Issue statistics (by status, by priority, by assignee, by category)
- [ ] Group issues by status/priority/assignee
- [ ] Auto-assign suggestion based on workload
- [ ] 8 endpoints under `/api/analytics/issues`

## Tasks
- [ ] Domain: Analytics value objects (IssueSummary, IssueTrend, ResolutionTime)
- [ ] Infrastructure: Dapper queries for analytics aggregations
- [ ] Application: Queries (Summary, Trends, ResolutionTime, Statistics, ByStatus, ByPriority, ByAssignee, SuggestAssignee)
- [ ] Application: DTOs
- [ ] Api: `IssueAnalyticsEndpoints.cs`
- [ ] Output caching (10-min TTL)
- [ ] Unit tests (Trinity)

## Skill Reference
`minimal-api-modules`
')
echo "Created: US-015 → $US015"
add_to_project "$US015"

# US-016: User Analytics
US016=$(gh issue create --repo "$REPO" \
  --title "US-016: User Workload Analytics — Workload Distribution & Statistics" \
  --label "type:user-story,phase:4-analytics,module:analytics,priority:medium,agent:neo" \
  --body '## User Story
**As a** team lead,
**I want** to see user workload distribution and productivity metrics,
**So that** I can balance work assignments and identify bottlenecks.

## Acceptance Criteria
- [ ] All users workload summary (assigned issues by status)
- [ ] Individual user workload detail
- [ ] User statistics (avg resolution time, completion rate)
- [ ] 3 endpoints under `/api/analytics/users`

## Tasks
- [ ] Infrastructure: Dapper queries for user workload aggregation
- [ ] Application: Queries (WorkloadAll, WorkloadByUser, UserStatistics)
- [ ] Application: DTOs
- [ ] Api: `UserAnalyticsEndpoints.cs`
- [ ] Output caching (5-min TTL)
- [ ] Unit tests (Trinity)
')
echo "Created: US-016 → $US016"
add_to_project "$US016"

# US-017: Dashboard + Export
US017=$(gh issue create --repo "$REPO" \
  --title "US-017: Analytics Dashboard & Export — KPIs + JSON/CSV/PDF Export" \
  --label "type:user-story,phase:4-analytics,module:analytics,priority:medium,agent:neo" \
  --body '## User Story
**As a** executive stakeholder,
**I want** a unified dashboard with key KPIs and the ability to export reports,
**So that** I can monitor system health and share data with non-technical stakeholders.

## Acceptance Criteria
- [ ] Dashboard endpoint with combined KPIs (issues, users, inventory)
- [ ] Export endpoint (GET for quick export, POST for custom parameters)
- [ ] Export formats: JSON, CSV, PDF
- [ ] 3 endpoints: GET `/api/analytics/dashboard`, GET + POST `/api/analytics/export`

## Tasks
- [ ] Application: Dashboard query combining multiple analytics sources
- [ ] Application: Export service with format strategy pattern (JSON, CSV, PDF)
- [ ] Api: Dashboard + Export endpoints
- [ ] Output caching on dashboard (10-min TTL)
- [ ] Unit tests (Trinity)
')
echo "Created: US-017 → $US017"
add_to_project "$US017"

# US-018: Inventory Analytics
US018=$(gh issue create --repo "$REPO" \
  --title "US-018: Inventory Analytics — Value, Trends, Turnover, Reports" \
  --label "type:user-story,phase:4-analytics,module:analytics,module:inventory,priority:medium,agent:neo" \
  --body '## User Story
**As a** inventory manager,
**I want** detailed analytics on inventory value, stock trends, and supplier performance,
**So that** I can optimize inventory levels and reduce waste.

## Acceptance Criteria
- [ ] Inventory value (total, by category, by location)
- [ ] Inventory summary (total products, active, discontinued)
- [ ] Stock status distribution
- [ ] Stock trends over time
- [ ] Category distribution
- [ ] Turnover rate calculation
- [ ] Expiring products report
- [ ] Location capacity utilization
- [ ] Supplier performance metrics
- [ ] Top products (by value, by movement)
- [ ] 13 endpoints under `/api/inventory/analytics`

## Tasks
- [ ] Infrastructure: Dapper queries for inventory analytics
- [ ] Application: Queries (Value, Summary, StockStatus, Trends, Categories, Turnover, Expiring, Capacity, SupplierPerformance, TopProducts)
- [ ] Application: DTOs
- [ ] Api: `InventoryAnalyticsEndpoints.cs`
- [ ] Output caching (10-min TTL)
- [ ] Unit tests (Trinity)
')
echo "Created: US-018 → $US018"
add_to_project "$US018"

echo ""
echo "============================================"
echo "PHASE 5 — User Management + Notifications"
echo "============================================"

# US-019: User Management
US019=$(gh issue create --repo "$REPO" \
  --title "US-019: User Management — Full User CRUD, Profiles, Roles, Lifecycle" \
  --label "type:user-story,phase:5-users-notifications,module:users,priority:high,agent:neo" \
  --body '## User Story
**As an** administrator,
**I want** full user lifecycle management including profiles, roles, and activation,
**So that** I can control who has access to the system and their permissions.

## Acceptance Criteria
- [ ] Paginated user list with search (Admin only)
- [ ] Get user by ID
- [ ] `/me` endpoint for authenticated user profile
- [ ] Get users by role
- [ ] Get active users
- [ ] Update user profile
- [ ] Activate/deactivate user
- [ ] Change password (self-service)
- [ ] Assign/remove roles (Admin only)
- [ ] Delete user (Admin only, soft delete)
- [ ] Domain events for all user lifecycle changes
- [ ] 11+ endpoints under `/api/users`

## Tasks
- [ ] Domain: Extended `User` entity (profile fields, IsActive, LastLoginAt)
- [ ] Domain: Domain events (UserCreated, ProfileUpdated, Activated, Deactivated, PasswordChanged, RoleAssigned, RoleRemoved, LoggedIn)
- [ ] Infrastructure: User queries (Dapper for reads)
- [ ] Application: Commands (UpdateProfile, Activate, Deactivate, ChangePassword, AssignRole, RemoveRole, Delete)
- [ ] Application: Queries (GetAll, GetById, GetMe, GetByRole, GetActive)
- [ ] Application: DTOs, validators
- [ ] Api: `UserEndpoints.cs`
- [ ] Authorization: Admin-only endpoints with policy
- [ ] Output caching (5-min TTL on list queries)
- [ ] Unit tests (Trinity)
')
echo "Created: US-019 → $US019"
add_to_project "$US019"

# US-020: Notifications — SignalR
US020=$(gh issue create --repo "$REPO" \
  --title "US-020: Real-Time Notifications — SignalR WebSocket Hub" \
  --label "type:user-story,phase:5-users-notifications,module:notifications,priority:high,agent:neo" \
  --body '## User Story
**As an** active user,
**I want** real-time notifications when issues are assigned to me or status changes,
**So that** I can respond promptly without manually polling for updates.

## Acceptance Criteria
- [ ] SignalR hub at `/hubs/notifications`
- [ ] User-specific notifications (by connection/user ID)
- [ ] Group notifications (by role, by team)
- [ ] Broadcast notifications (all connected users)
- [ ] Domain event → SignalR notification mapping
- [ ] Notification types: IssueAssigned, IssueStatusChanged, CommentAdded, LowStockAlert, SystemAlert

## Tasks
- [ ] Infrastructure: `NotificationHub` (SignalR)
- [ ] Infrastructure: `INotificationService` → `SignalRNotificationService`
- [ ] Application: Domain event handlers that trigger notifications
- [ ] Application: Notification DTOs (type, title, message, metadata)
- [ ] Api: Map SignalR hub in `Program.cs`
- [ ] DI: `AddNotificationsModule()` extension method
- [ ] Integration tests (Trinity)

## Skill Reference
`infrastructure-integrations`
')
echo "Created: US-020 → $US020"
add_to_project "$US020"

# US-021: Notifications — Email
US021=$(gh issue create --repo "$REPO" \
  --title "US-021: Email Notifications — SMTP Service with Templates" \
  --label "type:user-story,phase:5-users-notifications,module:notifications,priority:medium,agent:neo" \
  --body '## User Story
**As a** user who is offline,
**I want** email notifications for important events,
**So that** I do not miss critical assignments or escalations.

## Acceptance Criteria
- [ ] `IEmailService` interface
- [ ] SMTP email sender with configurable settings
- [ ] HTML email templates (issue assigned, status change, daily digest)
- [ ] Async email sending (fire-and-forget with retry)
- [ ] Email settings via `appsettings.json`

## Tasks
- [ ] Define `IEmailService` interface
- [ ] Implement `SmtpEmailService` with `MailKit` or `System.Net.Mail`
- [ ] Create HTML email templates
- [ ] Add email configuration to `appsettings.json`
- [ ] Domain event handlers for email triggers
- [ ] Unit tests (Trinity)
')
echo "Created: US-021 → $US021"
add_to_project "$US021"

echo ""
echo "============================================"
echo "PHASE 6 — Integration & Messaging"
echo "============================================"

# US-022: Domain Events
US022=$(gh issue create --repo "$REPO" \
  --title "US-022: Domain Events — In-Process Cross-Module Communication" \
  --label "type:user-story,phase:6-integration,module:shared-kernel,priority:high,agent:neo" \
  --body '## User Story
**As an** architect,
**I want** modules to communicate via domain events without direct references,
**So that** module boundaries remain clean and modules can be extracted independently.

## Acceptance Criteria
- [ ] `IDomainEvent` marker interface in Shared Kernel
- [ ] `BaseEntity.AddDomainEvent()` / `ClearDomainEvents()` on aggregate roots
- [ ] Domain events dispatched via MediatR `INotification` after SaveChanges
- [ ] Event handlers in consuming modules (e.g., Notifications listens to Issues events)
- [ ] Domain events are dispatched within the same transaction (eventual consistency opt-in)

## Tasks
- [ ] Define `IDomainEvent : INotification` in Shared Kernel
- [ ] Implement `AddDomainEvent()` / `ClearDomainEvents()` in `BaseEntity`
- [ ] Create `DomainEventDispatcher` (triggered by EF Core SaveChanges interceptor)
- [ ] Implement event handlers in Notifications module for Issue/Inventory events
- [ ] Unit tests for event dispatch (Trinity)

## Skill Reference
`architecture-overview`
')
echo "Created: US-022 → $US022"
add_to_project "$US022"

# US-023: RabbitMQ
US023=$(gh issue create --repo "$REPO" \
  --title "US-023: RabbitMQ Messaging — Async Event Publishing" \
  --label "type:user-story,phase:6-integration,cross-cutting,priority:high,agent:neo" \
  --body '## User Story
**As an** architect,
**I want** domain events published to RabbitMQ for external consumers,
**So that** the system supports event-driven integration with external services.

## Acceptance Criteria
- [ ] `IMessageBusService` interface with Publish/Subscribe methods
- [ ] `RabbitMqMessageBusService` implementation using RabbitMQ.Client v7.x async API
- [ ] Queue and exchange configuration
- [ ] JSON serialization of event payloads
- [ ] Outbox pattern for reliable event publishing
- [ ] Connection resilience with retry/reconnect
- [ ] Configuration via `appsettings.json`

## Tasks
- [ ] Define `IMessageBusService` interface
- [ ] Implement `RabbitMqMessageBusService` (async channel, basic publish, exchange declaration)
- [ ] Implement outbox pattern (store events in DB, publish in background)
- [ ] Add RabbitMQ connection settings to `appsettings.json`
- [ ] Domain event → message bus publishing handler
- [ ] `InMemoryMessageBusService` fallback for dev/test
- [ ] Unit + integration tests (Trinity)

## Skill Reference
`eventhub-producer`, `eventhub-consumer`
')
echo "Created: US-023 → $US023"
add_to_project "$US023"

echo ""
echo "============================================"
echo "PHASE 7 — Production Readiness"
echo "============================================"

# US-024: PostgreSQL Migration
US024=$(gh issue create --repo "$REPO" \
  --title "US-024: PostgreSQL Migration — Production Database" \
  --label "type:user-story,phase:7-production,cross-cutting,priority:high,agent:neo" \
  --body '## User Story
**As a** DevOps engineer,
**I want** the system running on PostgreSQL in production,
**So that** we have a production-grade database with proper scaling capabilities.

## Acceptance Criteria
- [ ] Npgsql EF Core provider configured
- [ ] Connection string swappable via environment/appsettings
- [ ] EF Core migrations generated for PostgreSQL
- [ ] SQLite retained for development (`IsDevelopment()` check)
- [ ] Data seeding works on both providers
- [ ] All existing queries compatible with PostgreSQL

## Tasks
- [ ] Add `Npgsql.EntityFrameworkCore.PostgreSQL` NuGet package
- [ ] Configure conditional provider selection in DbContext registration
- [ ] Generate initial PostgreSQL migration
- [ ] Test migration on PostgreSQL Docker instance
- [ ] Verify all Dapper queries work with PostgreSQL syntax
- [ ] Update `appsettings.Production.json`
- [ ] Integration tests on PostgreSQL (Trinity)

## Skill Reference
`infrastructure-integrations`
')
echo "Created: US-024 → $US024"
add_to_project "$US024"

# US-025: Docker & Compose
US025=$(gh issue create --repo "$REPO" \
  --title "US-025: Docker & Docker Compose — Full Stack Deployment" \
  --label "type:user-story,phase:7-production,cross-cutting,priority:high,agent:neo" \
  --body '## User Story
**As a** developer/operator,
**I want** a single `docker-compose up` to start the entire stack,
**So that** the system is easy to deploy and test locally with all dependencies.

## Acceptance Criteria
- [ ] Multi-stage Dockerfile (restore → build → publish → runtime)
- [ ] docker-compose.yml with: app, PostgreSQL, Redis, RabbitMQ
- [ ] docker-compose.observability.yml with: Prometheus, Grafana (optional overlay)
- [ ] Health check integration for all services
- [ ] Environment-based configuration
- [ ] Volume mounts for data persistence

## Tasks
- [ ] Create multi-stage `Dockerfile`
- [ ] Create `docker-compose.yml` (app + PostgreSQL + Redis + RabbitMQ)
- [ ] Create `docker-compose.observability.yml` (Prometheus + Grafana)
- [ ] Add Prometheus scrape config for app metrics
- [ ] Add Grafana dashboard JSON for IMS metrics
- [ ] Create `.env.example` with required environment variables
- [ ] Documentation in README
- [ ] End-to-end test with compose (Trinity)
')
echo "Created: US-025 → $US025"
add_to_project "$US025"

# US-026: CI/CD
US026=$(gh issue create --repo "$REPO" \
  --title "US-026: CI/CD Pipeline — GitHub Actions Build + Test + Deploy" \
  --label "type:user-story,phase:7-production,cross-cutting,priority:high,agent:neo" \
  --body '## User Story
**As a** developer,
**I want** automated CI/CD that builds, tests, and validates every PR,
**So that** code quality is enforced and deployments are reliable.

## Acceptance Criteria
- [ ] GitHub Actions workflow triggered on PR to `main`
- [ ] Steps: restore → build → test → coverage report
- [ ] SonarQube/SonarCloud analysis (optional)
- [ ] Minimum 80% coverage gate on new code
- [ ] Docker image build + push on merge to `main`
- [ ] Status checks required for merge

## Tasks
- [ ] Create `.github/workflows/ci.yml` (build + test)
- [ ] Create `.github/workflows/cd.yml` (Docker build + push)
- [ ] Configure test coverage reporting (Coverlet + ReportGenerator)
- [ ] Add branch protection rules documentation
- [ ] Integration with SonarCloud (optional)
- [ ] End-to-end smoke test after deploy

## Skill Reference
`code-smells`, `testing-patterns`
')
echo "Created: US-026 → $US026"
add_to_project "$US026"

# US-027: OpenTelemetry
US027=$(gh issue create --repo "$REPO" \
  --title "US-027: OpenTelemetry — Traces, Metrics, Prometheus + Grafana" \
  --label "type:user-story,phase:7-production,cross-cutting,priority:medium,agent:neo" \
  --body '## User Story
**As an** SRE,
**I want** distributed traces, metrics, and dashboards,
**So that** I can monitor system performance and troubleshoot issues in production.

## Acceptance Criteria
- [ ] OpenTelemetry SDK configured for ASP.NET Core
- [ ] Traces exported to Jaeger (or OTLP collector)
- [ ] Metrics exported to Prometheus
- [ ] Custom metrics: request count, latency histogram, active connections, DB query time
- [ ] Grafana dashboard with key IMS metrics
- [ ] Correlation ID propagated through traces

## Tasks
- [ ] Add OpenTelemetry NuGet packages
- [ ] Configure trace and metrics exporters in `Program.cs`
- [ ] Add custom `ActivitySource` for IMS-specific spans
- [ ] Create Prometheus scrape configuration
- [ ] Create Grafana dashboard JSON
- [ ] Document observability setup in README
- [ ] Verify traces in Jaeger UI (Trinity)

## Skill Reference
`observability`
')
echo "Created: US-027 → $US027"
add_to_project "$US027"

echo ""
echo "============================================"
echo "✅ DONE — All User Stories Created"
echo "============================================"
echo ""
echo "Summary:"
echo "  Phase 1 (Foundation):        US-001, US-002 ✅"
echo "  Phase 2 (Cross-Cutting):     US-003 to US-009"
echo "  Phase 3 (Inventory):         US-010 to US-013"
echo "  Phase 4 (Analytics):         US-014 to US-018"
echo "  Phase 5 (Users+Notif):       US-019 to US-021"
echo "  Phase 6 (Integration):       US-022, US-023"
echo "  Phase 7 (Production):        US-024 to US-027"
echo ""
echo "Total: 27 User Stories with Tasks"
echo "Project: https://github.com/users/peleverton/projects/2"
