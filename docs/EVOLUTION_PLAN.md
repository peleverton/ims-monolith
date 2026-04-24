# 🧭 Plano de Evolução — IMS Monolith

> **Autor:** Morpheus (Lead Architect)
> **Última revisão:** Abril 2026
> **Versão atual:** 2.0 — 59 US entregues
> **Status geral:** ✅ Sistema estabilizado, testado e com segurança endurecida — pronto para crescimento de produto

---

## Sumário Executivo

O IMS Monolith completou duas fases de evolução com sucesso. As 59 User Stories entregues cobrem:

- **Backend** — 5 módulos completos (Auth, Issues, Inventory, InventoryIssues, Analytics), 130+ endpoints, CQRS, Outbox, RabbitMQ, OpenTelemetry, refresh token rotativo, RBAC granular
- **Frontend** — Next.js 15 BFF, Blazor WASM integrado via Custom Elements, i18n, dark mode, SignalR, export de analytics
- **Testes** — 62 testes unitários .NET (100% passando), 18 testes de integração, 43 testes unitários React, suite E2E Playwright com cobertura de Blazor
- **Segurança** — CORS por ambiente, refresh token rotativo com revogação, RBAC policies por módulo
- **Observabilidade** — OpenTelemetry, Prometheus, Grafana, Jaeger, Serilog estruturado

### Estado dos débitos técnicos originais

| # | Débito | Status |
|---|---|---|
| D-01 | Testes com build quebrado — ILogger não injetado | ✅ Resolvido (US-050) |
| D-02 | Sem testes para Analytics, Auth, InventoryIssues | ✅ Resolvido (US-051/052/053) |
| D-03 | Sem UserManagement module | 🔄 Em planejamento (US-064) |
| D-04 | README desatualizado | ✅ Resolvido |
| D-05 | E2E sem cobertura de analytics/Blazor | ✅ Resolvido (US-059) |
| D-06 | Sem testes de integração para Issues, Analytics | ✅ Resolvido (US-054) |
| D-07 | Frontend sem testes unitários | ✅ Resolvido (US-058) |
| D-08 | UnitTest1.cs placeholder | ✅ Removido |
| D-09 | Refresh token sem rotação | ✅ Resolvido (US-055) |
| D-10 | CORS aberto em produção | ✅ Resolvido (US-056) |

---

## 1. Diagnóstico do Estado Atual

### 1.1 Cobertura de testes (pós US-058/059)

```
Backend (.NET 9)
├── Unit tests:        62 testes — 5 módulos cobertos — 100% passando
├── Integration tests: 18 testes — 3 módulos cobertos (Inventory, Issues, Analytics)
└── Cobertura estimada: ~65% handlers, ~80% domain entities

Frontend (Next.js 15)
├── Unit tests (Vitest+RTL): 43 testes — Button, ExportButton, api-fetch, utils, notifications
├── E2E (Playwright):         auth, issues, inventory, analytics + Blazor WASM
└── Cobertura estimada: ~40% componentes críticos
```

### 1.2 Débitos técnicos identificados (nova rodada)

#### 🔴 Críticos

| # | Débito | Local | Impacto |
|---|---|---|---|
| D-11 | **UserManagement sem módulo dedicado** — operações de user admin acopladas ao módulo Auth | `Auth/Api/UserAdminModule.cs` | Violação de separação de responsabilidades |
| D-12 | **Sem migrations versionadas** — usa `EnsureCreated` (destrutivo em produção) | `*ModuleExtensions.cs` | Impossível fazer zero-downtime deploy em produção real |
| D-13 | **Sem paginação cursor-based** — toda paginação é offset (lenta em tabelas grandes) | `*ReadRepositories.cs` | Degradação de performance com volume de dados |

#### 🟡 Importantes

| # | Débito | Local | Impacto |
|---|---|---|---|
| D-14 | **Sem rate limiting no BFF** — Next.js proxy sem throttling | `app/api/proxy/` | Vetor de abuso / custo de API |
| D-15 | **Sem health check do frontend** — Next.js não expõe `/health` | `next.config.ts` | Monitoramento incompleto |
| D-16 | **Sem testes de contract (Pact)** — nenhuma garantia de que BFF e API .NET são compatíveis | `e2e/` | Quebra silenciosa de contrato |
| D-17 | **Frontend sem React Query** — todos os fetches são bare `fetch()` sem cache/retry/stale | `lib/api-fetch.ts` | Re-renders desnecessários, sem cache client-side |
| D-18 | **Sem compressão Brotli no Blazor WASM** — `.wasm` ~8MB não comprimido no nginx/Kestrel | `Dockerfile.frontend` | TTI alto em conexões lentas |

#### 🟢 Menores

| # | Débito | Local | Impacto |
|---|---|---|---|
| D-19 | **Auth: sem `POST /api/auth/logout` no BFF** — BFF não repassa logout para o backend | `app/api/auth/logout/` | Refresh token não é revogado no servidor ao fazer logout |
| D-20 | **Sem OpenAPI client gerado** — frontend chama endpoints manualmente sem type safety | `lib/api/` | Risco de discrepância de tipos |
| D-21 | **Issues sem campo `resolvedAt` na query read** — DTO não retorna `ResolvedAt` | `IssuesReadRepository.cs` | Relatórios de SLA incompletos |

---

## 2. Eixos de Evolução Futura

| Prioridade | Eixo | Impacto | Esforço | Sprint alvo |
|---|---|---|---|---|
| 🔴 Alta | Produto — Home Dashboard + Kanban | Alto | Médio | Sprint 7 |
| 🔴 Alta | Backend — UserManagement Module | Alto | Médio | Sprint 7 |
| 🔴 Alta | Infra — Migrations versionadas | Alto | Alto | Sprint 8 |
| 🟡 Média | Backend — Notifications Module | Médio | Médio | Sprint 8 |
| 🟡 Média | Backend — Background Jobs | Médio | Baixo | Sprint 8 |
| 🟡 Média | Frontend — React Query + cache client | Médio | Médio | Sprint 9 |
| 🟡 Média | Backend — Webhooks outbound | Médio | Alto | Sprint 9 |
| 🟢 Baixa | Backend — Full-text search | Alto | Alto | Sprint 10 |
| 🟢 Baixa | Infra — Feature Flags | Baixo | Baixo | Sprint 10 |
| 🟢 Baixa | Infra — Background jobs scheduler | Médio | Médio | Sprint 10 |
| 🟢 Baixa | Infra — Multi-tenancy | Alto | Muito Alto | Sprint 12+ |
| 🟢 Baixa | Infra — Extração de microserviço (PoC) | Alto | Muito Alto | Sprint 12+ |

---

## 3. Plano Detalhado por Sprint

### Sprint 7 — Produto e Módulos Faltantes 🔴

#### US-060: Home Dashboard — Visão consolidada

A `app/page.tsx` atual é um redirect para `/issues`. Criar uma home real com Server Components:

```
/app/page.tsx → Server Component
├── 4 KPI cards: Total Issues, Open Issues, Total Products, Low Stock
├── Últimas 5 issues modificadas (atividade recente)
├── Últimas 3 alertas de estoque
└── Links rápidos: Nova Issue, Ajustar Estoque, Ver Analytics
```

**Implementação:**
- `GET /api/issues?pageSize=5&sort=updatedAt` + `GET /api/analytics/issues/summary`
- `GET /api/inventory/analytics/summary` para contagens de estoque
- Zero loading spinner inicial (tudo via Server Components, dados frescos no request)
- Responsivo, dark mode compatível

**Esforço:** 2 dias

---

#### US-061: Painel de histórico de notificações

As notificações SignalR existem como toasts efêmeros. Persistir na sessão:

```
Sidebar → ícone 🔔 com badge (unreadCount)
         → clica → NotificationPanel (slide-over)
                   ├── Lista de notificações com timestamp
                   ├── Marcar como lida (individual)
                   ├── Marcar todas como lidas
                   └── Limpar histórico
```

**Status:** Infraestrutura já existe em `NotificationProvider` — falta apenas o painel visual.
**Arquivos:** `notification-panel.tsx` (já existe esqueleto), `notification-bell.tsx`, `sidebar.tsx`
**Esforço:** 1 dia

---

#### US-062: Issues — Kanban Board view

Alternativa visual à lista tabular:

```
/issues?view=kanban
├── 4 colunas: Open │ InProgress │ Testing │ Resolved
├── Drag-and-drop → PATCH /api/issues/{id}/status
├── Filtro por priority e assignee
└── Toggle List/Kanban na toolbar
```

**Decisão técnica:** usar `@dnd-kit/core` (leve, acessível, sem jQuery) em vez de `react-beautiful-dnd` (deprecated).
**Esforço:** 3 dias

---

#### US-064: UserManagement Module — Módulo dedicado

O módulo `UserAdminModule.cs` está acoplado ao módulo Auth. Extrair para módulo independente:

```
backend/src/Modules/UserManagement/
├── Domain/
│   ├── Entities/UserProfile.cs     — dados de perfil (separar de credenciais)
│   └── Events/UserEvents.cs        — UserActivated, UserDeactivated, UserRoleChanged
├── Application/
│   ├── Commands/                    — UpdateProfile, ActivateUser, ChangeRole
│   └── Handlers/UserCommandHandlers.cs
├── Infrastructure/
│   └── UserManagementDbContext.cs  — schema compartilhado via view ou tabela separada
└── Api/UserManagementModule.cs     — migrar endpoints de /api/admin/users
```

**Estratégia de migração:**
1. Criar novo módulo com os mesmos endpoints sob `/api/users` (novo path)
2. Manter `/api/admin/users` funcionando (deprecated) por 1 sprint
3. Remover `UserAdminModule.cs` do módulo Auth

**Esforço:** 3 dias

---

### Sprint 8 — Infraestrutura Crítica e Notificações 🔴

#### US-065: Migrations versionadas com EF Core

O uso de `EnsureCreated` é destrutivo em produção (recria o schema). Substituir por migrations:

```bash
dotnet ef migrations add InitialSchema --project backend/src --startup-project backend/src
```

**Por módulo:**
- `AuthDbContext` → `Migrations/Auth/`
- `IssuesDbContext` → `Migrations/Issues/`
- `InventoryDbContext` → `Migrations/Inventory/`
- `InventoryIssuesDbContext` → `Migrations/InventoryIssues/`

**Estratégia:**
- `dotnet ef database update` no startup (não `EnsureCreated`)
- Manter `EnsureCreated` apenas para SQLite em testes de integração
- CI verifica que migrations estão atualizadas (`dotnet ef migrations has-pending-model-changes`)

**Esforço:** 2 dias

---

#### US-066: Notifications Module — Email e histórico persistido

```
backend/src/Modules/Notifications/
├── Domain/
│   ├── Entities/Notification.cs    — id, userId, type, title, body, sentAt, readAt
│   └── Templates/                  — Razor/HBS templates
├── Application/
│   ├── Commands/SendNotificationCommand.cs
│   └── Consumers/                  — IssueAssignedConsumer, LowStockEmailConsumer
└── Infrastructure/
    ├── NotificationsDbContext.cs
    ├── EmailService.cs              — SmtpClient ou SendGrid
    └── NotificationReadRepository.cs
```

**Endpoints novos:**
```
GET  /api/notifications          — histórico do usuário autenticado
POST /api/notifications/{id}/read — marcar como lida
```

**Esforço:** 3 dias

---

#### US-067: Background Jobs com Hangfire

Jobs recorrentes que hoje não existem:

```csharp
// Registrar jobs no startup
RecurringJob.AddOrUpdate<ExpiryCheckJob>(
    "expiry-check", job => job.ExecuteAsync(), Cron.Daily);

RecurringJob.AddOrUpdate<OverdueIssuesJob>(
    "overdue-issues", job => job.ExecuteAsync(), "0 */6 * * *");

RecurringJob.AddOrUpdate<AnalyticsSnapshotJob>(
    "analytics-snapshot", job => job.ExecuteAsync(), Cron.Weekly);
```

**Jobs:**
| Job | Trigger | O que faz |
|---|---|---|
| `ExpiryCheckJob` | Diário 00:00 | Cria `InventoryIssue` para produtos com expiração nos próximos 30 dias |
| `OverdueIssuesJob` | A cada 6h | Marca issues com `DueDate < now` e status `Open/InProgress` como Overdue via evento |
| `AnalyticsSnapshotJob` | Semanal | Salva snapshot de KPIs em tabela `AnalyticsSnapshots` para trends históricos |
| `TokenCleanupJob` | Noturno | Remove `RefreshTokens` revogados/expirados há mais de 30 dias |

**Dashboard:** `/hangfire` — autenticado, policy `CanManageUsers` (Admin only)
**Esforço:** 2 dias

---

### Sprint 9 — Frontend Qualidade e Webhooks 🟡

#### US-068: React Query — Cache client-side e UX reativa

Todos os fetches do frontend usam `fetch()` bare sem cache. Migrar para `@tanstack/react-query`:

```tsx
// Antes
const data = await apiFetch<IssueSummaryDto>("/api/analytics/issues/summary");

// Depois
const { data, isLoading, error } = useQuery({
  queryKey: ["analytics", "issueSummary"],
  queryFn: () => apiFetch<IssueSummaryDto>("/api/analytics/issues/summary"),
  staleTime: 30_000,   // fresco por 30s
  retry: 2,
});
```

**Benefícios:**
- Deduplicação automática de requests simultâneos
- Cache global — navegar entre páginas não refaz requests
- `invalidateQueries` após mutations (ex: criar issue → atualizar contadores)
- Loading/error states uniformes

**Arquivos afetados:** todas as páginas de dashboard, componentes de analytics
**Esforço:** 2 dias

---

#### US-069: Webhooks outbound

Clientes externos podem receber eventos IMS via HTTP:

```
POST /api/webhooks          — registrar endpoint + events + secret HMAC
GET  /api/webhooks          — listar registros do usuário
DELETE /api/webhooks/{id}   — remover
```

**Payload assinado:**
```http
POST https://cliente.com/webhook
X-IMS-Signature: sha256=<hmac-sha256-do-body>
X-IMS-Event: issue.created
Content-Type: application/json

{ "event": "issue.created", "timestamp": "...", "data": { ... } }
```

**Entrega:** fila RabbitMQ `webhooks.delivery` → consumer com retry 3x + backoff exponencial
**Eventos suportados:** `issue.created`, `issue.resolved`, `stock.low`, `user.invited`
**Esforço:** 4 dias

---

#### US-070: BFF Logout com revogação no backend

Atualmente o BFF apaga cookies de sessão mas não chama `POST /api/auth/logout` no backend. O refresh token permanece ativo no banco até expirar:

```typescript
// app/api/auth/logout/route.ts
// Adicionar: chamar backend para revogar o refresh token
const refreshToken = cookieStore.get("ims_refresh_token")?.value;
if (refreshToken) {
  await fetch(`${API_BASE}/api/auth/logout`, {
    method: "POST",
    body: JSON.stringify({ refreshToken }),
    headers: { "Content-Type": "application/json" },
  });
}
```

**Esforço:** 2h — quick win

---

### Sprint 10 — Produto Avançado e Qualidade 🟡

#### US-071: Full-text search cross-módulo

```
GET /api/search?q=laptop&modules=issues,inventory&page=1&pageSize=20
```

**Resposta:**
```json
{
  "results": [
    { "module": "inventory", "type": "product", "id": "...", "title": "Laptop Dell...", "score": 0.95 },
    { "module": "issues",    "type": "issue",   "id": "...", "title": "Bug no Laptop...", "score": 0.82 }
  ],
  "total": 12
}
```

**Decisão técnica:** Meilisearch (Rust, simples, self-hosted, excelente DX)
- Indexação via domain events (async, eventual consistency)
- Container `meilisearch` no `docker-compose.yml`
- `SearchModule` dedicado com `ISearchService` e implementação `MeilisearchService`

**Esforço:** 5 dias

---

#### US-072: Compressão Brotli para assets Blazor WASM

O `.wasm` (~8MB) carregado a cada visita sem cache agressivo:

```dockerfile
# Dockerfile.frontend
RUN find /app/public/blazor -name "*.wasm" -o -name "*.dll" | \
    xargs -I{} brotli --best --force {} --output {}.br
```

```typescript
// next.config.ts — adicionar headers de cache
headers: [
  {
    source: "/blazor/:path*",
    headers: [
      { key: "Cache-Control", value: "public, max-age=31536000, immutable" },
    ],
  },
]
```

**Resultado esperado:** download inicial reduzido de ~8MB → ~2MB (Brotli)
**Esforço:** 1 dia

---

#### US-073: Testes de contrato (Pact) — BFF ↔ API .NET

Garantir que o BFF Next.js e o backend .NET são compatíveis:

```typescript
// e2e/contracts/analytics.contract.ts
const issuesSummaryContract = {
  state: "issues exist",
  uponReceiving: "GET /api/analytics/issues/summary",
  withRequest: { method: "GET", path: "/api/analytics/issues/summary" },
  willRespondWith: {
    status: 200,
    body: like({ total: integer(), open: integer() }),
  },
};
```

**Esforço:** 2 dias

---

### Sprint 11–12 — Feature Flags e Observabilidade Avançada 🟢

#### US-074: Feature Flags com Microsoft.FeatureManagement

```json
// appsettings.json
"FeatureManagement": {
  "Analytics.EnableExport":       true,
  "Inventory.EnableExpiryAlerts": true,
  "Issues.EnableKanban":          true,
  "Webhooks.Enabled":             false
}
```

```csharp
// Uso nos endpoints
if (!await featureManager.IsEnabledAsync("Webhooks.Enabled"))
    return Results.NotFound();
```

**Override por ambiente:** variáveis de ambiente `FeatureManagement__Webhooks__Enabled=true`
**Esforço:** 1 dia

---

#### US-075: Alertas no Grafana — SLA e anomalias

Configurar alertas automáticos no Grafana (já disponível no stack):

| Alerta | Condição | Canal |
|---|---|---|
| `HighErrorRate` | `rate(http_errors[5m]) > 1%` | Slack / Email |
| `SlowAPI` | `p99 latency > 2s` por 5min | Slack |
| `LowStockCritical` | `stock_level < min_level * 0.5` | Slack |
| `IssueOverdueSurge` | Overdue issues aumentaram > 20% em 1h | Email |

**Esforço:** 1 dia

---

### Sprint 12+ — Escala e Arquitetura 🟢

#### US-076: Multi-tenancy — Isolamento por organização

> ⚠️ Alto risco de regressão. Exige cobertura de testes ≥ 85% antes de iniciar.

**Estratégia:** Row-level security via `TenantId` em todas as entidades (não schema-per-tenant)

```csharp
// Middleware
app.UseTenantResolution(); // extrai TenantId de subdomain/header

// DbContext — filtro global
modelBuilder.Entity<Issue>().HasQueryFilter(x => x.TenantId == _tenantContext.TenantId);
```

**Migração:** script de migration adiciona `TenantId NOT NULL DEFAULT '00000000-...'` + índice
**Esforço:** 1 semana

---

#### US-077: Extração de Issues como microserviço (PoC)

Demonstrar que o design modular permite extração sem reescrita:

```
Antes:  ims-monolith → handles /api/issues/*
Depois: ims-monolith → YARP proxy → ims-issues-service
                     → outros módulos internos
```

**Steps:**
1. Extrair `Issues` para `IMS.Issues.Service` (novo projeto)
2. Comunicação via RabbitMQ (já existe) — sem chamadas HTTP inter-serviço
3. YARP (`Microsoft.ReverseProxy`) roteando `/api/issues/*`
4. Feature flag `Issues.UseExternalService` para rollback imediato

**Esforço:** 1 semana (PoC — não produção)

---

## 4. Backlog Priorizado — Próximas 6 Sprints

```
Sprint 7  (now):  US-060 Home Dashboard
                  US-061 Notification Panel
                  US-062 Kanban Board
                  US-064 UserManagement Module

Sprint 8:         US-065 Migrations versionadas (crítico para produção)
                  US-066 Notifications Module (email + histórico)
                  US-067 Background Jobs (Hangfire)
                  US-070 BFF Logout → revogação no backend (2h, quick win)

Sprint 9:         US-068 React Query no frontend
                  US-069 Webhooks outbound
                  US-071 Full-text search (Meilisearch)

Sprint 10:        US-072 Compressão Brotli Blazor WASM
                  US-073 Contract tests (Pact)
                  US-074 Feature Flags

Sprint 11:        US-075 Alertas Grafana
                  D-19 BFF rate limiting
                  D-20 OpenAPI client gerado automaticamente

Sprint 12+:       US-076 Multi-tenancy
                  US-077 Microserviço Issues (PoC)
```

---

## 5. Métricas de Sucesso Atualizadas

| Fase | Métrica | Estado atual | Meta |
|---|---|---|---|
| Testes .NET | % passando | ✅ 100% (80 testes) | Manter ≥ 100% |
| Testes .NET | Cobertura handlers | ~65% | ≥ 80% |
| Testes React | Componentes cobertos | ~40% | ≥ 70% |
| E2E | Pass rate em CI | ✅ Em definição | 100% |
| Segurança | Refresh token rotativo | ✅ Implementado | — |
| Segurança | RBAC por módulo | ✅ Implementado | — |
| Performance | TTI com Blazor WASM | ~4s (cold) | < 2s (com Brotli) |
| Migrations | Estratégia em produção | ❌ EnsureCreated | EF Migrations |
| Produto | Home Dashboard | ❌ Redirect | Server Component com KPIs |

---

## 6. Matriz de Priorização (atualizada)

```
                    IMPACTO
                 baixo │ alto
                ───────┼────────────────────────────
           alto │      │ US-065 (migrations)
  ESFORÇO       │      │ US-071 (full-text search)
                │      │ US-076 (multi-tenancy)
           baixo│ D-19 │ US-060 (home dashboard)   ← quick wins
                │ D-20 │ US-070 (BFF logout)
                │ D-21 │ US-067 (background jobs)
                ───────┼────────────────────────────
```

**Quick wins recomendados para o Sprint 7:**
1. US-070 — BFF Logout revoga token no backend (2h)
2. US-060 — Home Dashboard com KPIs reais (2 dias)
3. US-061 — Notification panel (1 dia — infraestrutura já existe)

---

## 7. Riscos Atualizados

| Risco | Prob | Impacto | Mitigação |
|---|---|---|---|
| `EnsureCreated` destrói dados em próximo deploy em produção real | Alta | Alto | US-065 deve preceder qualquer deploy em ambiente com dados reais |
| Blazor WASM TTI alto em mobile impacta adoção | Média | Médio | US-072 (Brotli) reduz de 8MB → 2MB |
| Multi-tenancy adicionado sem cobertura ≥ 85% quebra queries | Alta | Alto | Não iniciar US-076 antes de Sprint 12 |
| Webhooks sem retry persistido perdem eventos em falha | Média | Médio | Usar RabbitMQ com DLQ (já disponível no stack) |
| React Query introduz cache stale em dados críticos (estoque) | Baixa | Médio | Configurar `staleTime: 0` para endpoints write-sensitive |

---

## 8. Decisões de Arquitetura Abertas

| Decisão | Opções | Recomendação | Prazo |
|---|---|---|---|
| Migrations: por módulo ou unificadas? | Schema unificado / schemas separados | **Por módulo** — alinha com isolamento de contextos | Sprint 8 |
| Full-text search: Meilisearch ou Elasticsearch? | Meilisearch (simples, Rust) / ES (robusto) | **Meilisearch** para porte atual — migrar para ES se volume > 10M docs | Sprint 10 |
| Background jobs: Hangfire ou Quartz.NET? | Hangfire (UI, persistência) / Quartz (leve) | **Hangfire** — UI de monitoring alinha com observabilidade | Sprint 8 |
| Kanban: dnd-kit ou react-beautiful-dnd? | dnd-kit (ativo, acessível) / rbd (deprecated) | **dnd-kit** | Sprint 7 |
| Webhooks: entrega síncrona ou via fila? | Síncrona (simples) / fila RabbitMQ | **RabbitMQ** — consistente com arquitetura existente + retry grátis | Sprint 9 |

---

## 9. Visão de Longo Prazo (6–12 meses)

```
v1.0 (atual) — Monolith estabilizado
     │
     ▼
v1.1 (Sprint 7–8) — Produto completo
     ├── Home Dashboard, Kanban, Notifications
     └── UserManagement Module, Migrations, Background Jobs

v1.2 (Sprint 9–10) — Plataforma
     ├── Webhooks, React Query, Full-text search
     └── Feature Flags, Contract Tests

v2.0 (Sprint 12+) — Multi-tenant SaaS (decisão estratégica)
     ├── Multi-tenancy + billing basics
     └── Extração gradual de módulos de maior carga (Issues, Analytics)
```

---

*Plano elaborado por Morpheus com base em análise de 59 USs entregues, estado real do codebase, cobertura de testes e débitos técnicos identificados. Revisão a cada 2 sprints ou após mudança estratégica relevante.*
