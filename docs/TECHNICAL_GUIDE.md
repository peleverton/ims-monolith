# 🛠️ Guia Técnico — IMS (Issue & Inventory Management System)

> **Versão:** 1.0 • **Stack:** .NET 9 + Next.js 15 + Blazor WASM • **Data:** Abril 2026

---

## Sumário

1. [Visão Geral da Arquitetura](#visão-geral-da-arquitetura)
2. [Requisitos de Ambiente](#requisitos-de-ambiente)
3. [Estrutura do Repositório](#estrutura-do-repositório)
4. [Setup Local (Dev)](#setup-local-dev)
5. [Variáveis de Ambiente](#variáveis-de-ambiente)
6. [Backend — .NET 9 Modular Monolith](#backend--net-9-modular-monolith)
7. [Frontend — Next.js Shell](#frontend--nextjs-shell)
8. [Frontend — Blazor WASM](#frontend--blazor-wasm)
9. [Banco de Dados](#banco-de-dados)
10. [Cache — Redis](#cache--redis)
11. [Mensageria — RabbitMQ](#mensageria--rabbitmq)
12. [Notificações — SignalR](#notificações--signalr)
13. [Observabilidade](#observabilidade)
14. [CI/CD — GitHub Actions](#cicd--github-actions)
15. [Docker & Docker Compose](#docker--docker-compose)
16. [Testes](#testes)
17. [Convenções e Padrões](#convenções-e-padrões)
18. [Troubleshooting](#troubleshooting)

---

## Visão Geral da Arquitetura

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         FRONTEND                                        │
│  ┌───────────────────────────┐   ┌──────────────────────────────────┐   │
│  │   Next.js 15 (App Router) │   │     Blazor WASM (MudBlazor)      │   │
│  │   BFF / Auth / i18n       │   │   Inventory Grid, Analytics      │   │
│  │   Tailwind 4 / dark mode  │   │   Custom Elements via JS interop │   │
│  └───────────┬───────────────┘   └────────────────┬─────────────────┘   │
└──────────────┼──────────────────────────────────── ┼─────────────────────┘
               │ HTTP (REST)                         │ embedded in Next.js
               ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    BACKEND — .NET 9 Modular Monolith                    │
│                                                                         │
│  ┌──────────┐ ┌──────────┐ ┌───────────┐ ┌──────────┐ ┌────────────┐  │
│  │   Auth   │ │  Issues  │ │ Inventory │ │Analytics │ │   Notif.   │  │
│  │  Module  │ │  Module  │ │  Module   │ │  Module  │ │   Module   │  │
│  └────┬─────┘ └────┬─────┘ └─────┬─────┘ └────┬─────┘ └─────┬──────┘  │
│       └────────────┴─────────────┴─────────────┴─────────────┘         │
│                          Shared Kernel                                  │
│          MediatR • CQRS • FluentValidation • Domain Events              │
│          Outbox • Behaviors • Result<T> • Error Codes                   │
└───────┬─────────────────────────────────────────────────────────────────┘
        │
        ├── PostgreSQL (EF Core)
        ├── Redis (Output Cache + Distributed Cache)
        ├── RabbitMQ (Async Events)
        └── SignalR Hub (/hubs/notifications)
```

---

## Requisitos de Ambiente

| Ferramenta | Versão mínima | Uso |
|---|---|---|
| .NET SDK | 9.0 | Backend |
| Node.js | 20 LTS | Frontend Next.js |
| Docker | 24+ | Infra local |
| Docker Compose | 2.x | Orquestração local |
| Git | 2.x | Controle de versão |
| PostgreSQL | 16 | Banco de dados (via Docker) |
| Redis | 7 | Cache (via Docker) |
| RabbitMQ | 3.13 | Mensageria (via Docker) |

---

## Estrutura do Repositório

```
ims-monolith/
├── backend/
│   ├── src/                          # API + Módulos
│   │   ├── Program.cs                # Composition root
│   │   ├── appsettings*.json         # Configurações por ambiente
│   │   ├── Modules/
│   │   │   ├── Auth/                 # JWT, usuários, roles
│   │   │   ├── Issues/               # Chamados e workflow
│   │   │   ├── Inventory/            # Produtos, estoque, fornecedores
│   │   │   ├── Analytics/            # KPIs e relatórios
│   │   │   ├── InventoryIssues/      # Relação inventory ↔ issues
│   │   │   └── Notifications/        # SignalR hub + email
│   │   └── Shared/
│   │       ├── Abstractions/         # Interfaces base
│   │       ├── Behaviors/            # Pipeline MediatR
│   │       ├── Caching/              # Redis helpers
│   │       ├── Database/             # DbContext base, migrations
│   │       ├── Domain/               # Entity, IAggregateRoot, IDomainEvent
│   │       ├── Errors/               # Result<T>, ErrorCodes, Middleware
│   │       ├── Messaging/            # IEventBus, RabbitMQ
│   │       └── Outbox/               # Outbox pattern
│   └── tests/
│       └── Modules/Issues/           # Unit tests por módulo
├── frontend/
│   └── apps/
│       ├── next-shell/               # Next.js 15 App Router
│       │   ├── app/                  # Pages e layouts
│       │   ├── components/           # UI components
│       │   ├── lib/                  # auth, api-fetch, signalr, i18n
│       │   └── messages/             # i18n PT/EN
│       └── blazor-modules/           # Blazor WASM
│           └── Components/           # MudDataGrid, Charts
├── infra/
│   ├── grafana/                      # Dashboards + datasources
│   └── prometheus/                   # Configuração scrape
├── .github/
│   └── workflows/                    # CI/CD pipelines
├── docker-compose.yml                # Stack completa
├── docker-compose.dev.yml            # Override para dev
├── docker-compose.observability.yml  # Prometheus + Grafana
└── Dockerfile                        # Multi-stage build (.NET)
```

---

## Setup Local (Dev)

> ✅ **Modo rápido:** com Docker instalado, basta executar **dois comandos** para ter o stack completo rodando.

### 1. Clone o repositório

```bash
git clone https://github.com/peleverton/ims-monolith.git
cd ims-monolith
```

### 2. Configure as variáveis de ambiente

```bash
cp .env.example .env
```

O arquivo `.env` já vem com valores funcionais para desenvolvimento. Para produção, troque as senhas marcadas com `change_me_*`.

### 3. ▶️ Suba tudo com Docker Compose

```bash
docker compose up -d
```

Isso sobe **toda a stack** de uma vez:

| Serviço | URL | Descrição |
|---|---|---|
| Frontend (Next.js) | http://localhost:3000 | Interface web |
| API (.NET) | http://localhost:8080 | Backend REST + SignalR |
| Swagger | http://localhost:8080/swagger | Documentação da API |
| PostgreSQL | localhost:5432 | Banco de dados |
| Redis | localhost:6379 | Cache |
| RabbitMQ UI | http://localhost:15672 | Mensageria (ims/ims) |

> Login padrão: `admin@ims.com` / `Admin@123`

### 4. (Opcional) Com Observabilidade

```bash
docker compose -f docker-compose.yml -f docker-compose.observability.yml up -d
```

Adiciona:
- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3001` (admin / admin)

### 5. Parar a stack

```bash
docker compose down
# Para remover volumes (apaga dados):
docker compose down -v
```

---

### Setup manual (sem Docker)

Use apenas se precisar rodar os serviços individualmente para debug.

**Pré-requisito:** PostgreSQL, Redis e RabbitMQ rodando localmente ou via `docker compose -f docker-compose.dev.yml up -d`.

```bash
# Backend
cd backend/src
dotnet run --urls http://localhost:5049

# Frontend (outro terminal)
cd frontend/apps/next-shell
cp .env.local.example .env.local   # ajuste NEXT_PUBLIC_API_URL=http://localhost:5049
npm install
npm run dev
```

---

## Variáveis de Ambiente

### Backend — `appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ims_dev;Username=ims;Password=ims123"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  },
  "Jwt": {
    "Secret": "sua-chave-secreta-minimo-32-chars",
    "Issuer": "ims-api",
    "Audience": "ims-client",
    "ExpiresInMinutes": 60,
    "RefreshExpiresInDays": 7
  }
}
```

### Frontend — `.env.local` (Next.js)

```bash
NEXTAUTH_SECRET=seu-secret-aqui
NEXTAUTH_URL=http://localhost:3000
NEXT_PUBLIC_API_URL=http://localhost:5049
```

---

## Backend — .NET 9 Modular Monolith

### Padrões adotados

| Padrão | Implementação |
|---|---|
| CQRS | MediatR — `ICommand`, `IQuery` |
| Result Pattern | `Result<T>` — sem exceptions para fluxo de negócio |
| Domain Events | `IDomainEvent` + `IEventBus` in-process |
| Outbox | `OutboxMessage` — garante entrega de eventos |
| Validation | FluentValidation + Behavior de pipeline |
| Caching | `ICacheService` (Redis) + `OutputCache` |
| Error Handling | `ExceptionHandlingMiddleware` + `ProblemDetails` |

### Estrutura de um Módulo

```
Modules/Auth/
├── Api/
│   ├── AuthEndpoints.cs         # Minimal API endpoints
│   └── UserAdminModule.cs       # Admin endpoints
├── Application/
│   ├── Commands/                # ICommand handlers
│   ├── Queries/                 # IQuery handlers
│   ├── DTOs/                    # Request/Response DTOs
│   ├── Services/                # Serviços de aplicação
│   └── Validators/              # FluentValidation
├── Domain/
│   ├── Entities/                # Aggregate roots
│   └── Events/                  # Domain events
├── Infrastructure/
│   ├── Persistence/             # DbContext, Repositories
│   └── Services/                # JwtService, etc.
└── AuthModuleExtensions.cs      # DI registration
```

### Adicionando um novo Módulo

1. Crie a pasta em `backend/src/Modules/NomeModulo/`
2. Implemente a estrutura padrão acima
3. Crie `NomeModuloExtensions.cs` com:
   ```csharp
   public static IServiceCollection AddNomeModulo(this IServiceCollection services, IConfiguration config)
   ```
4. Registre em `Program.cs`:
   ```csharp
   builder.Services.AddNomeModulo(builder.Configuration);
   ```

---

## Frontend — Next.js Shell

### Tecnologias

| Lib | Uso |
|---|---|
| Next.js 15 | App Router, RSC, Server Actions |
| TypeScript strict | Tipagem completa |
| Tailwind CSS 4 | Estilos |
| next-themes | Dark mode |
| next-intl | i18n PT-BR / EN-US |
| react-hook-form + zod | Formulários e validação |
| @microsoft/signalr | Notificações em tempo real |
| Radix UI (via shadcn) | Componentes acessíveis |

### Estrutura de rotas

```
app/
├── (auth)/
│   ├── login/page.tsx           # Página de login
│   └── register/page.tsx        # Página de registro
├── (dashboard)/
│   ├── layout.tsx               # Layout com sidebar + notificações
│   ├── issues/page.tsx          # Lista de issues
│   ├── inventory/page.tsx       # Estoque
│   ├── analytics/page.tsx       # Dashboard analytics
│   └── admin/
│       ├── layout.tsx           # Guard: somente Admin
│       └── users/page.tsx       # Gerenciamento de usuários
└── api/
    └── auth/
        ├── login/route.ts       # BFF login
        ├── register/route.ts    # BFF register
        ├── refresh/route.ts     # BFF refresh token
        └── logout/route.ts      # BFF logout
```

### BFF (Backend For Frontend)

O Next.js atua como BFF — todas as chamadas autenticadas passam por `proxy.ts`:

```
Browser → Next.js BFF (cookie HttpOnly) → .NET API (Bearer token)
```

O token JWT **nunca** é exposto ao JavaScript do browser.

### Autenticação

- `lib/auth.ts` — lê o cookie HttpOnly e valida com `jose`
- `middleware.ts` — protege todas as rotas do grupo `(dashboard)`
- Refresh automático via `lib/session-sync.ts`

---

## Frontend — Blazor WASM

- Framework: **.NET 9 Blazor WASM** com **MudBlazor**
- Compilado como **Custom Elements** para integração no Next.js via componente `<BlazorHost>`
- Os artefatos compilados (`.wasm`, `.dll`, `blazor.webassembly.js`) são copiados para `public/_blazor/` durante o build do Next.js
- Componentes principais:
  - `InventoryGrid` — MudDataGrid com CRUD completo de produtos
  - `AnalyticsDashboard` — Charts (linha, donut, barra) e KPI cards com dados em tempo real

### Integração Next.js ↔ Blazor WASM

O Blazor é servido como micro-frontend embarcado no Next.js. O fluxo é:

```
Next.js page → <BlazorHost> component
  → carrega /public/_blazor/blazor.webassembly.js
  → carrega /public/_blazor/_framework/* (dotnet.wasm, dotnet.js, …)
  → renderiza <inventory-grid> ou <analytics-dashboard> (Custom Elements)
```

**Rewrites no `next.config.ts`** garantem que as requisições dos Custom Elements
aos caminhos `/_framework/*` e `/_content/*` sejam resolvidas para os arquivos
estáticos corretos em `public/_blazor/`:

```ts
// next.config.ts — beforeFiles rewrites
{ source: '/_framework/:path*', destination: '/public/_blazor/_framework/:path*' },
{ source: '/_content/:path*',   destination: '/public/_blazor/_content/:path*'  },
// also handles page-prefixed paths (e.g. /analytics/_framework/...)
```

**Build automático do Blazor** — o script `predev` no `package.json` do Next.js
garante que o Blazor seja sempre recompilado antes de `next dev` / `next build`
em ambiente local:

```json
"predev":  "bash ../../scripts/build-blazor.sh",
"prebuild": "bash ../../scripts/build-blazor.sh"
```

> No Docker, o build do Blazor é feito diretamente no `Dockerfile.frontend`
> antes do `next build`, sem usar o script `prebuild` do npm para evitar
> conflitos na imagem.

### Comunicação Blazor → Backend (via BFF)

O `HttpClient` configurado no Blazor aponta para `/api/proxy/` (mesmo origem).
O Next.js BFF (`app/api/proxy/[...path]/route.ts`) injeta o token JWT do cookie
HttpOnly antes de repassar a requisição ao backend .NET:

```
Blazor WASM → GET /api/proxy/analytics/dashboard
  → Next.js BFF (injeta Bearer token do cookie ims_access_token)
  → GET http://app:8080/api/analytics/dashboard  (200 OK)
```

---

## Banco de Dados

### PostgreSQL 16

- ORM: **EF Core 9** com migrations por módulo
- Cada módulo tem seu próprio `DbContext`
- Migrations são aplicadas automaticamente no startup (Development) ou via CI (Production)
- **Dapper** é usado nas queries de leitura (Analytics, Inventory read models) — todas as queries usam sintaxe PostgreSQL com identificadores entre aspas duplas (`"TableName"`) pois o PostgreSQL é case-sensitive

### Convenções de queries Dapper (PostgreSQL)

| Regra | Exemplo |
|---|---|
| Nomes de tabelas/colunas entre aspas duplas | `SELECT "Status" FROM "Issues"` |
| Cast explícito para tipos C# | `COUNT(*)::int`, `AVG(...)::float8` |
| Alias PascalCase para mapear para records C# | `COUNT(*) AS "Count"` |
| Booleanos: usar `true`/`false` literal | `WHERE "IsActive" = true` |
| Datas: usar `CURRENT_DATE`, `INTERVAL '30 days'` | `WHERE "CreatedAt" >= CURRENT_DATE - INTERVAL '30 days'` |

### Rodando migrations manualmente

```bash
cd backend/src
dotnet ef database update --context AuthDbContext
dotnet ef database update --context IssuesDbContext
dotnet ef database update --context InventoryDbContext
```

### Seed de dados

No startup (Development), são criados automaticamente:
- Roles: `Admin`, `User`
- Usuário admin: `admin@ims.com` / `Admin@123`

---

## Cache — Redis

- **Output Cache**: respostas de endpoints GET cacheadas no Redis
- **Distributed Cache**: cache de objetos com `ICacheService`
- TTL padrão: 5 minutos (configurável por endpoint)
- Invalidação automática via domain events

---

## Mensageria — RabbitMQ

- **IEventBus** — interface para publicar eventos de domínio
- Fila padrão: `ims.events`
- Consumers registrados por módulo em `AddModuloConsumers()`
- **Outbox Pattern** garante entrega even-if-crash

---

## Notificações — SignalR

- Hub: `/hubs/notifications` (autenticado)
- Cliente: `lib/signalr-client.ts` — singleton com reconexão exponencial
- Provider: `NotificationProvider` no layout do dashboard
- Eventos suportados:
  - `ReceiveNotification` — notificação genérica
  - `IssueCreated`, `IssueResolved`, `LowStock`

---

## Observabilidade

### Logs — Serilog

- Formato JSON estruturado
- Enrichers: `CorrelationId`, `UserId`, `Environment`
- Sinks: Console + Arquivo (`logs/ims-modular-{date}.log`)

### Metrics — Prometheus

- Endpoint: `GET /metrics`
- Métricas customizadas: requests/s, latência, erros por módulo

### Traces — OpenTelemetry

- Instrumentação automática: ASP.NET Core, EF Core, HttpClient
- Exporta para Jaeger (configurável)

### Grafana

- Dashboard pré-configurado em `infra/grafana/provisioning/dashboards/ims-app.json`
- Datasource Prometheus configurado automaticamente

---

## CI/CD — GitHub Actions

### Workflows

| Arquivo | Trigger | O que faz |
|---|---|---|
| `ci.yml` | push/PR → qualquer branch | Build + Test + Coverage (.NET) + Build Next.js |
| `ci-frontend.yml` | push/PR → qualquer branch | Lint + Type-check Next.js |
| `cd.yml` | push → main | Build Docker + Push GHCR |
| `squad-release.yml` | push → main | Changelog + GitHub Release |

### Secrets necessários (GitHub)

| Secret | Uso |
|---|---|
| `GITHUB_TOKEN` | Automático — push GHCR, releases |
| `CODECOV_TOKEN` | Upload de cobertura para Codecov |

### Permissões dos workflows

Os workflows de CI precisam de:
```yaml
permissions:
  contents: read
  checks: write
  pull-requests: write
```

---

## Docker & Docker Compose

### Build manual da imagem

```bash
docker build -t ims-monolith:local .
```

### Stack completa

```bash
# Sobe tudo: API + PostgreSQL + Redis + RabbitMQ + Next.js
docker compose up -d

# Apenas infra (para rodar API e frontend local)
docker compose -f docker-compose.dev.yml up -d

# Com observabilidade
docker compose -f docker-compose.yml -f docker-compose.observability.yml up -d
```

### Portas padrão

| Serviço | Porta |
|---|---|
| Next.js | 3000 |
| .NET API | 5049 (dev) / 8080 (Docker) |
| PostgreSQL | 5432 |
| Redis | 6379 |
| RabbitMQ | 5672 (AMQP) / 15672 (UI) |
| Prometheus | 9090 |
| Grafana | 3001 |

---

## Testes

### Backend — xUnit

```bash
cd backend/tests
dotnet test --collect:"XPlat Code Coverage"
```

Cobertura gerada em `**/coverage.opencover.xml`.

### Frontend — Playwright (E2E)

```bash
cd frontend/apps/next-shell
npx playwright test
```

Specs em `e2e/`:
- `auth.spec.ts` — login, register, logout
- `issues.spec.ts` — CRUD de issues
- `dashboard.spec.ts` — navegação e layout

### Relatório de testes

```bash
npx playwright show-report
```

---

## Convenções e Padrões

### Git

- Branch: `feat/US-XXX-descricao`, `fix/descricao`, `chore/descricao`
- Commit: Conventional Commits — `feat:`, `fix:`, `docs:`, `chore:`, `refactor:`
- PR obrigatório para `main` — squash merge

### C# / .NET

- `Result<T>` para retornos de serviços — sem `throw` para fluxo de negócio
- DTOs com sufixo `Request` / `Response`
- Validators em `Application/Validators/`
- Nenhuma dependência direta entre módulos — comunicação via eventos

### TypeScript / Next.js

- `strict: true` no `tsconfig.json`
- Server Components por padrão — `"use client"` apenas quando necessário
- API calls server-side via `lib/api-fetch.ts`
- Tipos centralizados em `lib/types.ts`

---

## Troubleshooting

### `npm ci` falha com "package-lock.json out of sync"

```bash
cd frontend/apps/next-shell
rm -rf node_modules package-lock.json
npm install
```

### `dotnet run` falha com "connection refused" (PostgreSQL)

Verifique se o Docker está rodando:
```bash
docker compose -f docker-compose.dev.yml ps
docker compose -f docker-compose.dev.yml up -d
```

### Migration pendente

```bash
cd backend/src
dotnet ef migrations list
dotnet ef database update
```

### SignalR não conecta

1. Verifique se a API está rodando em `http://localhost:5049`
2. Confira o token JWT no cookie — pode estar expirado
3. Veja o log do browser: `Console > Network > WS`

### Workflow GitHub Actions falha com "Resource not accessible by integration"

Adicione as permissões no workflow:
```yaml
permissions:
  checks: write
  pull-requests: write
```

### Docker build falha com "project or solution file not found"

O `dotnet publish` precisa apontar explicitamente para o `.csproj`:
```dockerfile
RUN dotnet publish -c Release --no-restore -o /publish \
    /p:UseAppHost=false backend/src/*.csproj
```

### Analytics dashboard retorna 422 no Blazor

O erro `422 Unprocessable Entity` nas chamadas `/api/proxy/analytics/*` indica
que o token JWT não está sendo enviado. Verifique:

1. Se o cookie `ims_access_token` existe no browser (DevTools → Application → Cookies)
2. Se o Next.js BFF está rodando (container `ims-frontend` healthy)
3. Se o `HttpClient` do Blazor aponta para a URL correta (deve usar `/api/proxy/`, não a URL direta do backend)

### Recursos Blazor retornam 404 (`_framework/dotnet.js`, `_content/*`)

Os arquivos estáticos do Blazor WASM precisam estar em `public/_blazor/` no Next.js. Se estiverem faltando:

```bash
# Recompilar o Blazor e copiar para public/
bash scripts/build-blazor.sh

# Ou em dev local:
cd frontend/apps/next-shell
npm run dev   # executa predev que chama build-blazor.sh automaticamente
```

Para rebuild completo do Docker:
```bash
docker compose build --no-cache frontend
docker compose up -d
```

### "Blazor has already started" no console

Aviso benigno que ocorre em navegação SPA — o Blazor runtime não pode ser
iniciado duas vezes na mesma página. O componente `<BlazorHost>` já possui
guards para evitar o segundo `Blazor.start()`. O aviso pode ser ignorado com
segurança.

---

*Dúvidas técnicas? Abra uma issue no repositório ou consulte o [README principal](../README.md).*
