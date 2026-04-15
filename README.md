# IMS — Issue & Inventory Management System

> Modular Monolith • .NET 9 • Minimal API • CQRS • Clean Architecture • Redis • SignalR • OpenTelemetry

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Modular%20Monolith-blue)]()
[![License](https://img.shields.io/badge/License-MIT-green)]()

A modular monolith issue and inventory management system built with .NET 9, featuring **6 business modules**, CQRS with MediatR, Clean Architecture, Minimal APIs, domain events, distributed caching (Redis), real-time notifications (SignalR), comprehensive analytics, and full observability (OpenTelemetry + Serilog + Prometheus).

---

## Table of Contents

- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Tech Stack](#tech-stack)
- [Getting Started](#getting-started)
- [API Reference](#api-reference)
- [Authentication](#authentication)
- [Modules](#modules)
- [Shared Kernel](#shared-kernel)
- [Domain Model](#domain-model)
- [Cross-Cutting Concerns](#cross-cutting-concerns)
- [Development Guide](#development-guide)
- [Testing](#testing)
- [AI Team (Squad)](#ai-team-squad)
- [Roadmap](#roadmap)
- [Contributing](#contributing)

---

## Architecture

IMS follows a **Modular Monolith** architecture — a single deployable unit where each business domain is an isolated module with its own layers, data access, and API surface.

```
┌────────────────────────────────────────────────────────────────────────────┐
│                          Program.cs (Composition Root)                    │
│  ┌────────────────────────────────────────────────────────────────────┐   │
│  │                    ASP.NET Core Pipeline                           │   │
│  │  CorrelationId → Metrics → Performance → Auth → UserContext       │   │
│  │  → Authorization → OutputCache → Routing → API                    │   │
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
│  │  User CRUD       │  │  Issue summary    │  │ In-app (SignalR)     │    │
│  │  Roles, Active   │  │  User workload    │  │ Email (SMTP)         │    │
│  │  Profile mgmt    │  │  Auto-assign      │  │ Message Bus          │    │
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
   │  SQLite/   │    │  Redis    │    │  RabbitMQ  │    │ Prometheus│
   │  PostgreSQL│    │  Cache    │    │  Messaging │    │ Grafana   │
   └───────────┘    └───────────┘    └───────────┘    └───────────┘
```

**Key principles:**
- Each module owns its **domain, data access, and API endpoints**
- Modules communicate through **domain events** (`IDomainEvent` via MediatR), not shared entities
- **Separate EF Core DbContexts** per module (shared database, isolated contexts)
- **Result\<T\> pattern** — no exceptions for business logic (Railway-Oriented Programming)
- **CQRS via MediatR** — commands mutate state, queries read state
- **EF Core for writes, Dapper for reads** — each CQRS side uses the best tool for the job
- **Pipeline Behaviors** — automatic validation, logging, and caching on every request
- **Rich domain events** — 25+ domain events for cross-module communication
- Designed for **future service extraction** without rewriting

> 📐 Full solution design: [`.squad/agents/morpheus/solution-design.md`](.squad/agents/morpheus/solution-design.md)

---

## Project Structure

```
ims-modular/
├── Program.cs                              # Composition root — module registration + pipeline
├── appsettings.json                        # Configuration (JWT, DB, Redis, Email, etc.)
├── Modules/
│   ├── Auth/                               # Authentication & Authorization module
│   │   ├── Api/AuthEndpoints.cs            # POST /api/auth/register, /api/auth/login
│   │   ├── Application/                    # DTOs, Validators
│   │   ├── Domain/                         # User (AR), Role, UserRole
│   │   ├── Infrastructure/                 # AuthDbContext, JwtService
│   │   └── AuthModuleExtensions.cs
│   │
│   ├── Issues/                             # Issue Management module
│   │   ├── Api/IssuesEndpoints.cs          # 7 REST endpoints
│   │   ├── Application/                    # Commands, Queries, DTOs, Validators
│   │   ├── Domain/                         # Issue (AR), Comment, Activity, Tag
│   │   ├── Infrastructure/                 # IssuesDbContext, Repos (EF Core + Dapper)
│   │   └── IssuesModuleExtensions.cs
│   │
│   ├── Inventory/                          # Inventory Management (Phase 3)
│   │   ├── Api/                            # Products, StockMovements, Suppliers, Locations, Analytics
│   │   ├── Application/                    # Commands, Queries, DTOs, Validators
│   │   ├── Domain/                         # Product (AR), StockMovement, Supplier, Location, 15 events
│   │   └── Infrastructure/                 # InventoryDbContext, Repos
│   │
│   ├── InventoryIssues/                    # Inventory Issue Tracking (Phase 4)
│   │   ├── Api/                            # 16 endpoints (CRUD + lifecycle + filters)
│   │   ├── Application/                    # Commands, Queries, DTOs, Validators
│   │   ├── Domain/                         # InventoryIssue (AR)
│   │   └── Infrastructure/                 # InventoryIssueDbContext, Repos
│   │
│   ├── Analytics/                          # Analytics & Reporting (Phase 4)
│   │   ├── Api/                            # Issue stats, workload, dashboard, export
│   │   └── Domain/                         # IIssueAssignmentService (auto-assign)
│   │
│   ├── UserManagement/                     # User Management (Phase 5)
│   │   ├── Api/                            # Full CRUD, profiles, roles
│   │   ├── Domain/                         # User (extended), 8 domain events
│   │   └── Infrastructure/                 # UserDbContext, Repos
│   │
│   └── Notifications/                      # Notifications (Phase 5)
│       ├── Api/                            # SignalR Hub
│       └── Infrastructure/                 # SignalR, SMTP, MessageBus (RabbitMQ)
│
├── Shared/
│   └── Kernel/
│       ├── BaseEntity.cs                   # Id (Guid), CreatedAt, UpdatedAt
│       ├── Result.cs                       # Result<T> pattern (Success/Failure)
│       ├── PagedResult.cs                  # Paged collection wrapper
│       └── IDomainEvent.cs                 # Domain event interface
│
├── Infrastructure/
│   ├── Behaviors/                          # ValidationBehavior, LoggingBehavior, CachingBehavior
│   ├── Middleware/                          # CorrelationId, Metrics, Performance, UserContext
│   ├── Caching/RedisCacheService.cs        # ICacheService → IDistributedCache
│   └── Services/                           # NotificationService, EmailService, MessageBusService
│
├── .squad/                                 # AI team configuration (see "AI Team" section)
├── .github/                                # GitHub Actions workflows
└── ims-modular.csproj                      # Project file (.NET 9)
```

---

## Tech Stack

| Category | Technology | Version |
|----------|-----------|---------|
| Runtime | .NET | 9.0 |
| Framework | ASP.NET Core Minimal API | 9.x |
| ORM (Write) | Entity Framework Core | 9.x |
| Data Access (Read) | Dapper | 2.x |
| Database | SQLite (dev) / PostgreSQL (prod) | — |
| CQRS | MediatR | 12.x |
| Validation | FluentValidation | 11.x |
| Auth | JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer) | 9.x |
| Password Hashing | BCrypt.Net-Next | 4.x |
| Distributed Cache | Redis (StackExchange.Redis / IDistributedCache) | — |
| Real-Time | SignalR | 9.x |
| Messaging | RabbitMQ (RabbitMQ.Client) | 7.x |
| Email | SMTP (System.Net.Mail) | — |
| Observability | OpenTelemetry (traces + metrics) | — |
| Logging | Serilog (structured, JSON, rolling files) | — |
| Metrics | Prometheus (via OpenTelemetry + MetricsMiddleware) | — |
| Health Checks | ASP.NET Core Health Checks | 9.x |
| Output Caching | ASP.NET Core Output Caching | 9.x |
| API Docs | Swashbuckle (Swagger) | 7.x |

---

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- A terminal (bash, zsh, PowerShell)

### Run

```bash
cd ims-modular
dotnet restore
dotnet build
dotnet run
```

The API will be available at **http://localhost:5049**.

Swagger UI: **http://localhost:5049/swagger**

### Quick Smoke Test

```bash
# Health check
curl http://localhost:5049/health/ping

# Login with seed admin
TOKEN=$(curl -s -X POST http://localhost:5049/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@ims.com","password":"Admin@123"}' | jq -r '.token')

echo "Token: $TOKEN"

# Create an issue
curl -X POST http://localhost:5049/api/issues \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "title": "First issue",
    "description": "Testing the API",
    "type": 0,
    "priority": 1,
    "tags": ["test"]
  }'

# List issues
curl http://localhost:5049/api/issues?page=1\&pageSize=10 \
  -H "Authorization: Bearer $TOKEN"
```

---

## API Reference

### Auth Endpoints

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `POST` | `/api/auth/register` | Register a new user | ❌ |
| `POST` | `/api/auth/login` | Login and receive JWT token | ❌ |

### Issues Endpoints

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/issues` | List issues (paginated) | ✅ |
| `GET` | `/api/issues/{id}` | Get issue by ID | ✅ |
| `POST` | `/api/issues` | Create a new issue | ✅ |
| `PUT` | `/api/issues/{id}` | Update an issue | ✅ |
| `PUT` | `/api/issues/{id}/status` | Update issue status | ✅ |
| `POST` | `/api/issues/{id}/comments` | Add a comment to an issue | ✅ |
| `DELETE` | `/api/issues/{id}` | Delete an issue | ✅ |

### Inventory — Products Endpoints (Phase 3)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/inventory/products` | List products (paginated, filtered by category/status/location/supplier) | ✅ |
| `GET` | `/api/inventory/products/{id}` | Get product by ID | ✅ |
| `GET` | `/api/inventory/products/sku/{sku}` | Get product by SKU | ✅ |
| `POST` | `/api/inventory/products` | Create a new product | ✅ |
| `PUT` | `/api/inventory/products/{id}` | Update a product | ✅ |
| `DELETE` | `/api/inventory/products/{id}` | Delete a product | ✅ |
| `POST` | `/api/inventory/products/{id}/adjust-stock` | Adjust stock (in/out/adjustment) | ✅ |
| `POST` | `/api/inventory/products/{id}/transfer` | Transfer stock between locations | ✅ |
| `POST` | `/api/inventory/products/{id}/discontinue` | Discontinue a product | ✅ |
| `PUT` | `/api/inventory/products/{id}/pricing` | Update product pricing | ✅ |

### Inventory — Stock Movements Endpoints (Phase 3)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/inventory/stock-movements` | List stock movements (filtered) | ✅ |
| `POST` | `/api/inventory/stock-movements` | Create a stock movement | ✅ |
| `POST` | `/api/inventory/stock-movements/bulk` | Bulk create stock movements | ✅ |
| `POST` | `/api/inventory/stock-movements/adjust` | Stock adjustment | ✅ |
| `POST` | `/api/inventory/stock-movements/transfer` | Stock transfer | ✅ |
| `DELETE` | `/api/inventory/stock-movements/{id}` | Delete a stock movement | ✅ |

### Inventory — Suppliers Endpoints (Phase 3)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/inventory/suppliers` | List suppliers | ✅ |
| `GET` | `/api/inventory/suppliers/{id}` | Get supplier by ID | ✅ |
| `POST` | `/api/inventory/suppliers` | Create a supplier | ✅ |
| `PUT` | `/api/inventory/suppliers/{id}` | Update a supplier | ✅ |
| `DELETE` | `/api/inventory/suppliers/{id}` | Delete a supplier | ✅ |
| `PATCH` | `/api/inventory/suppliers/{id}/activate` | Activate a supplier | ✅ |
| `PATCH` | `/api/inventory/suppliers/{id}/deactivate` | Deactivate a supplier | ✅ |

### Inventory — Locations Endpoints (Phase 3)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/inventory/locations` | List locations | ✅ |
| `GET` | `/api/inventory/locations/{id}` | Get location by ID | ✅ |
| `POST` | `/api/inventory/locations` | Create a location | ✅ |
| `PUT` | `/api/inventory/locations/{id}` | Update a location | ✅ |
| `DELETE` | `/api/inventory/locations/{id}` | Delete a location | ✅ |
| `PATCH` | `/api/inventory/locations/{id}/activate` | Activate a location | ✅ |
| `PATCH` | `/api/inventory/locations/{id}/deactivate` | Deactivate a location | ✅ |

### Inventory — Analytics Endpoints (Phase 3)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/inventory/analytics/summary` | Inventory summary (total value, products, stock status counts) | ✅ |
| `GET` | `/api/inventory/analytics/value` | Inventory value (total value, cost, profit, margin) | ✅ |
| `GET` | `/api/inventory/analytics/stock-summary` | Stock summary (in stock, low, out, overstock, discontinued) | ✅ |
| `GET` | `/api/inventory/analytics/stock-status` | Stock status percentages | ✅ |
| `GET` | `/api/inventory/analytics/categories` | Category distribution | ✅ |
| `GET` | `/api/inventory/analytics/categories/value` | Value by category (with profit margins) | ✅ |
| `GET` | `/api/inventory/analytics/top-products` | Top products by movement | ✅ |
| `GET` | `/api/inventory/analytics/stock-trends` | Stock trends over time | ✅ |
| `GET` | `/api/inventory/analytics/turnover` | Inventory turnover rate | ✅ |
| `GET` | `/api/inventory/analytics/expiring` | Expiring products report | ✅ |
| `GET` | `/api/inventory/analytics/location-capacity` | Location capacity utilization | ✅ |
| `GET` | `/api/inventory/analytics/supplier-performance` | Supplier performance metrics | ✅ |
| `GET` | `/api/inventory/analytics/movement-history` | Stock movement history | ✅ |

### Inventory Issues Endpoints (Phase 4)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/inventory-issues` | List all inventory issues | ✅ |
| `GET` | `/api/inventory-issues/{id}` | Get inventory issue by ID | ✅ |
| `POST` | `/api/inventory-issues` | Create an inventory issue | ✅ |
| `PUT` | `/api/inventory-issues/{id}` | Update an inventory issue | ✅ |
| `PATCH` | `/api/inventory-issues/{id}/status` | Change status | ✅ |
| `PATCH` | `/api/inventory-issues/{id}/assign` | Assign to user | ✅ |
| `POST` | `/api/inventory-issues/{id}/resolve` | Resolve | ✅ |
| `POST` | `/api/inventory-issues/{id}/close` | Close | ✅ |
| `POST` | `/api/inventory-issues/{id}/reopen` | Reopen | ✅ |
| `DELETE` | `/api/inventory-issues/{id}` | Delete | ✅ |
| `GET` | `/api/inventory-issues/product/{id}` | Filter by product | ✅ |
| `GET` | `/api/inventory-issues/location/{id}` | Filter by location | ✅ |
| `GET` | `/api/inventory-issues/type/{type}` | Filter by type | ✅ |
| `GET` | `/api/inventory-issues/open` | Open issues only | ✅ |
| `GET` | `/api/inventory-issues/high-priority` | High priority issues | ✅ |
| `GET` | `/api/inventory-issues/overdue` | Overdue issues | ✅ |
| `GET` | `/api/inventory-issues/search` | Full-text search | ✅ |
| `GET` | `/api/inventory-issues/statistics` | Statistics summary | ✅ |

### Analytics Endpoints (Phase 4)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/analytics/issues/summary` | Issue summary (by status, priority, assignment, dates) | ✅ |
| `GET` | `/api/analytics/issues/trends` | Issue trends over time (configurable days) | ✅ |
| `GET` | `/api/analytics/issues/resolution-time` | Resolution time stats (avg, median, min, max by priority) | ✅ |
| `GET` | `/api/analytics/issues/statistics` | Detailed stats with date filters | ✅ |
| `GET` | `/api/analytics/issues/by-status` | Issues grouped by status | ✅ |
| `GET` | `/api/analytics/issues/by-priority` | Issues grouped by priority | ✅ |
| `GET` | `/api/analytics/issues/by-assignee` | Issues grouped by assignee | ✅ |
| `GET` | `/api/analytics/issues/{id}/suggest-assignee` | Suggest best assignee (workload-based) | ✅ |
| `GET` | `/api/analytics/users/workload` | All users workload overview | ✅ |
| `GET` | `/api/analytics/users/{userId}/workload` | Individual user workload detail | ✅ |
| `GET` | `/api/analytics/users/statistics` | User activity statistics | ✅ |
| `GET` | `/api/analytics/dashboard` | Dashboard KPIs | ✅ |
| `GET` | `/api/analytics/export/report` | Export report (JSON/CSV) | ✅ |
| `POST` | `/api/analytics/reports/export` | Export report (JSON/CSV/PDF) | ✅ |

### User Management Endpoints (Phase 5)

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/api/users` | List users (paginated + search, Admin only) | ✅ Admin |
| `GET` | `/api/users/{id}` | Get user by ID (self or Admin) | ✅ |
| `GET` | `/api/users/me` | Get current user profile | ✅ |
| `GET` | `/api/users/role/{roleId}` | Get users by role (Admin) | ✅ Admin |
| `GET` | `/api/users/active` | Get active users (Admin) | ✅ Admin |
| `PUT` | `/api/users/{id}` | Update user (self or Admin) | ✅ |
| `PUT` | `/api/users/{id}/profile` | Update user profile | ✅ |
| `PATCH` | `/api/users/{id}/activate` | Activate user (Admin) | ✅ Admin |
| `PATCH` | `/api/users/{id}/deactivate` | Deactivate user (Admin) | ✅ Admin |
| `PUT` | `/api/users/{id}/password` | Change password | ✅ |
| `POST` | `/api/users/{id}/roles/{roleId}` | Assign role (Admin) | ✅ Admin |
| `DELETE` | `/api/users/{id}/roles/{roleId}` | Remove role (Admin) | ✅ Admin |
| `DELETE` | `/api/users/{id}` | Delete user (Admin) | ✅ Admin |

### System Endpoints

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/health/ping` | Health check | ❌ |

---

## Authentication

The API uses **JWT Bearer** tokens.

### Flow

1. **Register** a user via `POST /api/auth/register`
2. **Login** via `POST /api/auth/login` → receive a JWT token
3. **Include the token** in the `Authorization` header: `Bearer {token}`

### Seed Data

On first startup, the system creates:

| Entity | Details |
|--------|---------|
| Roles | `Admin`, `User` |
| Admin User | **Email:** `admin@ims.com` **Password:** `Admin@123` |

### Configuration

JWT settings in `appsettings.json`:

```json
{
  "Jwt": {
    "Key": "super-secret-key-ims-modular-2024-min-32-chars!",
    "Issuer": "IMS.Modular",
    "Audience": "IMS.Modular.Users"
  }
}
```

---

## Modules

### Module Conventions

Every module follows the same 4-layer structure with **CQRS data access split**:

```
Modules/{Name}/
├── Api/                            # Minimal API endpoints
├── Application/
│   ├── Commands/                   # State-changing ops → use IRepository (EF Core)
│   ├── Queries/                    # Read ops → use IReadRepository (Dapper)
│   ├── DTOs/                       # Data transfer objects
│   └── Validators/                 # FluentValidation rules
├── Domain/
│   ├── I{Name}Repository.cs       # Write interface (EF Core)
│   └── I{Name}ReadRepository.cs   # Read interface (Dapper)
├── Infrastructure/
│   ├── {Name}DbContext.cs          # EF Core context (write)
│   ├── {Name}Repository.cs        # Write implementation (EF Core)
│   └── {Name}ReadRepository.cs    # Read implementation (Dapper)
└── {Name}ModuleExtensions.cs       # DI registration + initialization
```

### Module Registration Pattern

Each module provides two extension methods:

```csharp
// 1. DI registration (called in Program.cs)
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddIssuesModule(builder.Configuration);
builder.Services.AddInventoryModule(builder.Configuration);
builder.Services.AddInventoryIssuesModule(builder.Configuration);
builder.Services.AddAnalyticsModule(builder.Configuration);
builder.Services.AddUserManagementModule(builder.Configuration);
builder.Services.AddNotificationsModule(builder.Configuration);

// 2. Initialization — seed data, table creation (called after app.Build())
await app.InitializeAuthModuleAsync();
await app.InitializeIssuesModuleAsync();
await app.InitializeInventoryModuleAsync();
// ... etc

// 3. Endpoint mapping
app.MapAuthEndpoints();
app.MapIssuesEndpoints();
app.MapInventoryEndpoints();
app.MapInventoryIssuesEndpoints();
app.MapAnalyticsEndpoints();
app.MapUserManagementEndpoints();
```

### Adding a New Module

1. Create `Modules/{Name}/` with all 4 layers
2. Create `{Name}ModuleExtensions.cs` with `Add{Name}Module()` and `Initialize{Name}ModuleAsync()`
3. Register in `Program.cs`
4. Consult the `code-templates` skill at `.squad/skills/ims-modular-patterns/skills/code-templates/SKILL.md`

---

## Shared Kernel

The Shared Kernel contains **only** types used by 2+ modules. It must stay lean.

### `BaseEntity`

Abstract base class for all entities:

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

### `Result<T>`

Railway-oriented result pattern — all business operations return `Result<T>` instead of throwing exceptions:

```csharp
// Success
return Result<IssueDto>.Success(dto);

// Failure
return Result<IssueDto>.Failure("Issue not found");
```

### `PagedResult<T>`

Paged collection for list endpoints:

```csharp
return new PagedResult<IssueDto>(items, totalCount, page, pageSize);
```

---

## Domain Model

### Issue (Aggregate Root)

```
Issue
├── Title (string, required)
├── Description (string)
├── Type (Bug | Feature | Task | Improvement)
├── Status (Open | InProgress | Testing | Resolved | Closed)
├── Priority (Low | Medium | High | Critical)
├── AssigneeId (Guid?)
├── ReporterId (Guid)
├── DueDate (DateTime?)
├── Tags (IssueTag[]) — value objects
├── Comments (IssueComment[]) — child entities
└── Activities (IssueActivity[]) — audit trail
```

### User (Aggregate Root)

```
User
├── Username (string, required)
├── Email (string, unique)
├── FullName (string)
├── PasswordHash (string, BCrypt)
├── IsActive (bool)
├── UserProfile — value object
├── UserRoles (UserRole[]) — join entity → Role
└── Domain Events: Created, ProfileUpdated, Activated, Deactivated,
    PasswordChanged, RoleAssigned, RoleRemoved, LoggedIn
```

### Product (Aggregate Root — Inventory Module)

```
Product
├── Name (string, required)
├── SKU (string, unique)
├── Barcode (string?)
├── Description (string?)
├── Category (ProductCategory — 20 categories)
├── CurrentStock (int)
├── MinimumStockLevel (int)
├── MaximumStockLevel (int)
├── UnitPrice (decimal)
├── CostPrice (decimal)
├── Unit (string, e.g., "un")
├── Currency (string)
├── LocationId (Guid?)
├── SupplierId (Guid?)
├── ExpiryDate (DateTime?)
├── StockStatus (InStock | LowStock | OutOfStock | Overstock | Discontinued)
├── IsActive (bool)
└── Domain Events: ProductCreated, StockChanged, LowStockAlert, OutOfStock,
    StockReplenished, ProductDiscontinued, PriceChanged, ProductExpiringSoon, ProductExpired
```

### Supplier (Entity — Inventory Module)

```
Supplier
├── Name (string, required)
├── Code (string, unique)
├── ContactPerson, Email, Phone
├── Address, City, State, Country, PostalCode
├── TaxId (string?)
├── CreditLimit (decimal)
├── PaymentTermsDays (int)
├── IsActive (bool)
├── Notes (string?)
└── Domain Events: SupplierCreated, SupplierDeactivated
```

### Location (Entity — Inventory Module)

```
Location
├── Name (string, required)
├── Code (string, unique)
├── Type (Warehouse | Store | Aisle | Shelf | DistributionCenter | Manufacturing | ReturnCenter | Transit)
├── Capacity (int)
├── Description (string?)
├── ParentLocationId (Guid?) — hierarchical
├── Address, City, State, Country, PostalCode
├── IsActive (bool)
└── Domain Events: LocationCreated, LocationDeactivated
```

### StockMovement (Entity — Inventory Module)

```
StockMovement
├── ProductId (Guid, required)
├── MovementType (InitialStock | StockIn | StockOut | Adjustment | Transfer | Sale | Purchase |
│                  Return | Damage | Loss | Expired | LocationChanged | PriceAdjustment |
│                  Updated | Discontinued)
├── Quantity (int)
├── LocationId (Guid?)
├── Reference (string?)
├── Notes (string?)
└── MovementDate (DateTime)
```

### InventoryIssue (Aggregate Root — Inventory Issues Module)

```
InventoryIssue
├── Title (string, required)
├── Description (string)
├── Type (InventoryIssueType — Damage, Discrepancy, Missing, QualityIssue, Expired, etc.)
├── Priority (Low | Medium | High | Critical)
├── Status (Open | InProgress | Resolved | Closed)
├── ProductId (Guid?)
├── LocationId (Guid?)
├── ReporterId (Guid)
├── AssigneeId (Guid?)
├── DueDate (DateTime?)
└── Resolution (string?)
```

---

## Cross-Cutting Concerns

### MediatR Pipeline Behaviors

| Behavior | Purpose |
|----------|---------|
| `ValidationBehavior` | Auto-validates all commands using FluentValidation before handler execution |
| `LoggingBehavior` | Structured logging of request/response with timing |
| `CachingBehavior` | Auto-caches query results (SHA256 key generation, 5-min TTL). Only caches requests ending with "Query" |

### Middleware Stack

| Middleware | Purpose |
|------------|---------|
| `CorrelationIdMiddleware` | Generates/propagates `X-Correlation-Id` header for request tracing |
| `MetricsMiddleware` | Tracks request count and latency for Prometheus |
| `PerformanceTimingMiddleware` | Logs slow requests exceeding threshold |
| `UserContextMiddleware` | Extracts user info from JWT claims for downstream use |

### Caching Strategy

- **Distributed cache**: Redis via `ICacheService` / `IDistributedCache`
- **Output cache**: ASP.NET Core Output Caching with policies per module (Analytics: 10min, Users: 5min, Inventory: 30s)
- **Pipeline cache**: `CachingBehavior` auto-caches MediatR queries

### Domain Events

25+ domain events across all modules:

| Category | Events |
|----------|--------|
| Issues | `IssueCreatedEvent`, `IssueStatusChangedEvent`, `IssueAssignedEvent`, `IssueCompletedEvent`, `IssueCommentAddedEvent` |
| Inventory | `ProductCreatedEvent`, `StockChangedEvent`, `LowStockAlertEvent`, `OutOfStockEvent`, `StockReplenishedEvent`, `ProductDiscontinuedEvent`, `PriceChangedEvent`, `StockTransferInitiatedEvent`, `StockTransferCompletedEvent`, `ProductExpiringSoonEvent`, `ProductExpiredEvent` |
| Suppliers & Locations | `SupplierCreatedEvent`, `SupplierDeactivatedEvent`, `LocationCreatedEvent`, `LocationDeactivatedEvent` |
| Users | `UserCreatedEvent`, `UserProfileUpdatedEvent`, `UserActivatedEvent`, `UserDeactivatedEvent`, `UserPasswordChangedEvent`, `UserRoleAssignedEvent`, `UserRoleRemovedEvent`, `UserLoggedInEvent` |

---

## Development Guide

### Key Patterns

| Pattern | Where | Details |
|---------|-------|---------|
| CQRS | `Application/Commands/` and `Application/Queries/` | Commands mutate, queries read. Each has an `IRequest<T>` and `IRequestHandler<T>` |
| EF Core (Write) | `Infrastructure/{Module}Repository.cs` | Command handlers use EF Core for change tracking, transactions, aggregate persistence |
| Dapper (Read) | `Infrastructure/{Module}ReadRepository.cs` | Query handlers use Dapper for raw SQL, direct DTO projection, no tracking overhead |
| Result Pattern | All handlers and services | Return `Result<T>.Success(value)` or `Result<T>.Failure(error)` — never throw for business errors |
| FluentValidation | `Application/Validators/` | All commands have a corresponding `AbstractValidator<T>` |
| Repository Pattern | `Domain/` interface, `Infrastructure/` implementation | Write repos (`IRepository`) use EF Core; Read repos (`IReadRepository`) use Dapper |
| Minimal API | `Api/` | Static extension methods mapping routes with lambda handlers |

### Skills Reference

The project has **15 detailed skills** covering architecture, implementation, testing, messaging, and security:

```
.squad/skills/ims-modular-patterns/skills/
├── architecture-overview/     # Clean Architecture structure and conventions
├── api-project-patterns/      # Program.cs, middleware, DI, Swagger config
├── core-project-patterns/     # Services, abstractions, domain models, validators
├── minimal-api-modules/       # Endpoint modules, route groups, authorization
├── error-mapping/             # ApplicationError, domain error enums, HTTP mapping
├── source-generators-aot/     # Native AOT, JsonSerializerContext
├── infrastructure-integrations/ # HttpClient, Redis cache, external APIs
├── code-templates/            # Copy-paste templates for scaffolding
├── eventhub-producer/         # Event Hub / Kafka producer patterns
├── eventhub-consumer/         # Event Hub / Kafka consumer patterns
├── observability/             # Structured logging, Dynatrace, OpenTelemetry
├── testing-patterns/          # xUnit + Moq, AAA pattern, Result<T> testing
├── code-smells/               # SonarQube rules (MUST check before coding)
├── critical-bugs/             # Runtime bug catalog
└── security-vulnerabilities/  # OWASP, crypto, injection, data exposure
```

**Before writing any code**, consult the skill index at `.squad/skills/ims-modular-patterns/SKILL.md`.

---

## Testing

### Conventions

- **Framework:** xUnit
- **Mocking:** Moq
- **Pattern:** AAA (Arrange, Act, Assert)
- **Coverage target:** ≥ 80% on new code

### Test Structure

```
Tests/
├── Unit/
│   ├── Modules/
│   │   ├── Auth/
│   │   │   └── Application/   # Handler tests, validator tests
│   │   └── Issues/
│   │       ├── Application/    # Command handler tests, query handler tests
│   │       └── Domain/         # Entity behavior tests
│   └── Shared/
│       └── Kernel/             # Result<T> tests, PagedResult tests
└── Integration/
    └── Api/                    # Endpoint integration tests
```

### Running Tests

```bash
dotnet test
```

> 📖 See the `testing-patterns` skill for detailed test templates and conventions.

---

## AI Team (Squad)

This project uses **Squad**, an AI team framework for collaborative development with specialized agents.

### Team

| Agent | Role | Responsibility |
|-------|------|----------------|
| **Morpheus** | Lead / Architect | Architecture decisions, code review, domain model governance |
| **Neo** | Backend Developer | API endpoints, CQRS handlers, EF Core, module implementation |
| **Trinity** | Tester | Unit tests, integration tests, coverage analysis |
| **Scribe** | Logger | Session logging (automatic, background) |
| **@copilot** | Coding Agent | Autonomous work on well-defined tasks via GitHub issues |

### How to Assign Work

1. **Create a GitHub issue** with the `squad` label → Morpheus triages it
2. **Label with `squad:{agent}`** to route directly to an agent
3. **Chat directly** in VS Code Copilot Chat — mention the agent's name and role
4. **Edit `.squad/identity/now.md`** to set focus areas for the next session

### Configuration

```
.squad/
├── team.md              # Team roster and project context
├── routing.md           # Work routing rules
├── decisions.md         # Architecture decisions log
├── ceremonies.md        # Team meetings (design review, retro)
├── copilot-instructions.md  # Instructions for @copilot coding agent
├── identity/
│   ├── now.md           # Current focus and active issues
│   └── wisdom.md        # Reusable patterns and anti-patterns
├── agents/
│   ├── morpheus/        # Charter, history, solution design
│   ├── neo/             # Charter, history
│   ├── trinity/         # Charter, history
│   └── scribe/          # Charter
├── skills/              # 15 detailed skill guides
└── log/                 # Session logs
```

---

## Roadmap

| Phase | Status | Description |
|-------|--------|-------------|
| **1. Foundation** | ✅ Done | Auth + Issues modules, JWT, CQRS, SQLite, EF Core + Dapper |
| **2. Cross-Cutting Hardening** | 🔜 Next | Pipeline behaviors (Validation, Logging, Caching), middleware (CorrelationId, Metrics, Performance, UserContext), global error handling, Serilog, health checks, Redis cache, output caching, rate limiting |
| **3. Inventory Module** | 📋 Planned | Products (CRUD, stock, pricing, SKU, categories), Stock Movements (15 types, bulk, transfer), Suppliers (credit, payment terms), Locations (hierarchical, capacity), Inventory Analytics (13 endpoints) |
| **4. Inventory Issues + Analytics** | 📋 Planned | Inventory-specific issue tracking (16 endpoints), comprehensive issue/user analytics, dashboard KPIs, workload-based auto-assignment, export (JSON/CSV/PDF) |
| **5. User Management + Notifications** | ✅ Done | Full user lifecycle (profile, roles, activate/deactivate), SignalR real-time notifications, SMTP email, message bus |
| **6. Integration & Messaging** | 📋 Planned | Domain events (MediatR INotification), RabbitMQ full implementation, outbox pattern, Polly retry policies |
| **7. Production** | 📋 Planned | PostgreSQL, Docker + Compose (PostgreSQL, Redis, RabbitMQ, Prometheus, Grafana), CI/CD, OpenTelemetry, feature flags |
| **8. Extraction** | 🔮 Future | Extract modules into independent services if scale requires |

> 📐 Full roadmap details: [`.squad/agents/morpheus/solution-design.md`](.squad/agents/morpheus/solution-design.md)

---

## Contributing

1. Read `.squad/team.md` for project context
2. Check `.squad/skills/ims-modular-patterns/SKILL.md` for coding conventions
3. **Always** consult `code-smells` skill before writing .NET code
4. Follow the [module structure conventions](#module-conventions) for new features
5. Write tests following `testing-patterns` skill (xUnit + Moq, AAA)
6. Open a PR — Morpheus reviews architecture, Trinity reviews tests

---

## License

MIT

---

<sub>Generated by Morpheus (Lead/Architect) — `.squad/agents/morpheus/` • Last updated: 2026-03-07</sub>
