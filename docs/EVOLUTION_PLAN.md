# 🧭 Plano de Evolução — IMS Monolith

> **Autor:** Morpheus (Lead Architect)  
> **Data:** Abril 2026  
> **Versão atual:** 1.0 — 49 US entregues  
> **Status geral:** ✅ MVP completo, pronto para evolução

---

## Sumário Executivo

O IMS Monolith atingiu maturidade de MVP com 49 User Stories entregues, cobrindo backend robusto (.NET 9 Modular Monolith), frontend moderno (Next.js 15 + Blazor WASM), infraestrutura completa (PostgreSQL, Redis, RabbitMQ, SignalR, OpenTelemetry) e CI/CD automatizado.

A análise atual aponta **5 eixos de evolução prioritários**, ordenados por impacto e risco:

| Prioridade | Eixo | Impacto | Esforço |
|---|---|---|---|
| 🔴 Alta | Qualidade — Testes | Alto | Médio |
| 🔴 Alta | Segurança — Hardening | Alto | Médio |
| 🟡 Média | Frontend — UX e completude | Alto | Alto |
| 🟡 Média | Backend — Módulos faltantes | Médio | Alto |
| 🟢 Baixa | Infraestrutura — Escala e extração | Alto | Muito Alto |

---

## 1. Diagnóstico do Estado Atual

### 1.1 Pontos Fortes

- ✅ **Arquitetura sólida** — Modular Monolith com CQRS, Clean Architecture, Result pattern. Fácil de raciocinar e evoluir
- ✅ **Backend feature-completo** — 5 módulos, 130+ endpoints, 25+ domain events, Outbox, Messaging, Observabilidade
- ✅ **Frontend funcional** — Next.js BFF, autenticação completa, Blazor WASM integrado, i18n, dark mode
- ✅ **Pipeline CI/CD** — GitHub Actions, build, test, Docker, GHCR
- ✅ **Observabilidade** — OpenTelemetry, Prometheus, Grafana, Serilog, Jaeger
- ✅ **Eventos e mensageria** — RabbitMQ, Outbox, consumers para LowStock e IssueCreated

### 1.2 Débitos Técnicos Identificados

#### 🔴 Críticos

| # | Débito | Local | Impacto |
|---|---|---|---|
| D-01 | **Testes com build quebrado** — `ILogger` não injetado nos handlers; `InventoryCommandHandlerTests` e `IssueCommandHandlerTests` não compilam | `backend/tests/Modules/` | CI falha silenciosamente |
| D-02 | **Sem testes para Analytics, Auth, InventoryIssues** — 0 testes unitários para 3 módulos inteiros | `backend/tests/` | Regressão invisível |
| D-03 | **Sem UserManagement module** — US-019 está listada como Done, mas o módulo não existe em `backend/src/Modules/` | `backend/src/Modules/` | Funcionalidades de admin sem implementação real |

#### 🟡 Importantes

| # | Débito | Local | Impacto |
|---|---|---|---|
| D-04 | **Roadmap desatualizado no README** — ainda menciona SQLite, fases antigas como "Planned" quando já estão entregues | `README.md` | Confusão para novos contribuidores |
| D-05 | **E2E sem cobertura de analytics/Blazor** — `dashboard.spec.ts` cobre apenas skeleton, não valida dados reais | `frontend/apps/next-shell/e2e/` | Regressão Blazor sem detecção |
| D-06 | **Sem testes de integração para Issues, Analytics** — apenas Inventory possui integration tests | `backend/tests/Integration/` | Cobertura assimétrica |
| D-07 | **Frontend sem testes unitários** — nenhum arquivo `*.test.tsx` no Next.js shell | `frontend/apps/next-shell/` | Componentes críticos sem cobertura |

#### 🟢 Menores

| # | Débito | Local | Impacto |
|---|---|---|---|
| D-08 | **`UnitTest1.cs` placeholder** ainda no projeto | `backend/tests/` | Ruído no relatório |
| D-09 | **Auth module sem refresh token rotativo** — refresh token é válido indefinidamente após emissão | `backend/src/Modules/Auth/` | Vetor de segurança |
| D-10 | **CORS aberto** (`AllowAnyOrigin`) em produção | `backend/src/Program.cs` | Segurança |

---

## 2. Plano de Evolução por Fase

### Fase A — Estabilização (Sprints 1–2) 🔴

> Objetivo: zerar débitos críticos e restabelecer confiança na suite de testes.

#### US-050: Corrigir testes quebrados — injetar ILogger nos handlers

**Problema:** Os construtores dos command handlers foram evoluídos para incluir `ILogger<T>`,
mas os testes não foram atualizados. O projeto de testes não compila.

**Solução:**
```csharp
// Nos testes — injetar logger mock
var logger = Mock.Of<ILogger<CreateIssueCommandHandler>>();
var handler = new CreateIssueCommandHandler(context, cache, logger);
```

**Arquivos:** `IssueCommandHandlerTests.cs`, `InventoryCommandHandlerTests.cs`  
**Esforço:** 2h

---

#### US-051: Testes unitários — Módulo Analytics

Cobrir handlers e repositório de Analytics:
- `GetIssueSummaryQueryHandler`
- `GetDashboardQueryHandler`
- `GetIssueStatsByStatusQueryHandler`
- `GetIssueStatsByPriorityQueryHandler`
- Mock de `IAnalyticsReadRepository` com Moq

**Esforço:** 1 dia

---

#### US-052: Testes unitários — Módulo Auth

Cobrir:
- `RegisterCommandHandler` — happy path + email duplicado
- `LoginCommandHandler` — happy path + senha errada
- `JwtTokenService.GenerateToken()` — valida claims
- `AuthValidators` — email, senha, username

**Esforço:** 1 dia

---

#### US-053: Testes unitários — Módulo InventoryIssues

Cobrir:
- `CreateInventoryIssueCommandHandler`
- `ResolveInventoryIssueCommandHandler`
- `LowStockConsumerService` — simular mensagem RabbitMQ e verificar criação de issue
- `InventoryIssueValidators`

**Esforço:** 1 dia

---

#### US-054: Testes de integração — Issues e Analytics

Expandir `IntegrationWebAppFactory` para cobrir:
- Criação e atualização de Issue via API
- Fluxo completo: Create → InProgress → Resolved
- Endpoints de analytics retornam 200 com dados válidos após seed

**Esforço:** 2 dias

---

### Fase B — Segurança e Hardening (Sprint 3) 🔴

> Objetivo: fechar vetores de segurança antes de qualquer exposição pública.

#### US-055: Refresh Token rotativo + revogação

**Problema:** O refresh token atual não é rotativo — pode ser reutilizado indefinidamente mesmo após logout.

**Solução:**
- Adicionar tabela `RefreshTokens` (hash, userId, expiresAt, revokedAt)
- Ao usar um refresh token, revogar o atual e emitir um novo
- `POST /api/auth/logout` revoga o refresh token

**Esforço:** 3 dias

---

#### US-056: Restringir CORS para origens conhecidas

**Problema:** `AllowAnyOrigin()` em produção permite CSRF e acesso de origens maliciosas.

**Solução:**
```json
// appsettings.Production.json
"Cors": {
  "AllowedOrigins": ["https://ims.yourdomain.com"]
}
```

**Esforço:** 2h

---

#### US-057: Autorização granular por módulo (RBAC policies)

Atualmente `RequireAuthorization()` é genérico. Adicionar policies por recurso:
- `Policy.CanManageUsers` → role Admin
- `Policy.CanViewAnalytics` → roles Admin, Manager
- `Policy.CanCreateIssue` → qualquer usuário autenticado

**Esforço:** 2 dias

---

### Fase C — Frontend — Completude e UX (Sprints 4–6) 🟡

> Objetivo: tornar o frontend production-ready com cobertura de testes e UX polida.

#### US-058: Testes unitários React — Componentes críticos

Instalar Vitest + React Testing Library. Cobrir:
- `<AuthForm />` — validação, submit, erro
- `<IssueCard />` — render de dados
- `<InventoryTable />` — paginação, filtros
- `hooks/useAuth.ts` — token refresh, logout
- `lib/api-fetch.ts` — mock fetch, error handling

**Esforço:** 3 dias

---

#### US-059: E2E Playwright — Analytics e Blazor

Expandir `dashboard.spec.ts` para validar:
- Blazor WASM carrega completamente (aguardar `analytics-dashboard` ter conteúdo)
- KPI cards exibem valores numéricos reais (não zero)
- Gráfico de linha renderiza com dados dos últimos 30 dias
- Atualização via botão Refresh funciona

**Esforço:** 2 dias

---

#### US-060: Dashboard principal (Home) — Visão consolidada

A `app/page.tsx` atual é apenas um redirect. Criar uma home com:
- 4 KPI cards (total issues, open, inventory itens, low stock)
- Atividade recente (últimas 5 issues modificadas)
- Links rápidos para as seções principais
- Dados via Server Components (sem loading spinner inicial)

**Esforço:** 3 dias

---

#### US-061: Notificações em tempo real — Painel de histórico

Atualmente as notificações SignalR aparecem como toasts mas não são persistidas.
Adicionar:
- Painel lateral "Notificações" com histórico da sessão
- Badge com contador de não lidas no ícone da sidebar
- Marcar como lida individualmente ou limpar todas

**Esforço:** 2 dias

---

#### US-062: Issues — Kanban Board view

Alternativa à lista tabular de issues, mais visual:
- 4 colunas: Open / InProgress / Testing / Resolved
- Drag-and-drop para mover entre colunas (chama `PUT /api/issues/{id}/status`)
- Filtro por assignee e prioridade

**Esforço:** 4 dias

---

#### US-063: Relatórios — Export de analytics no frontend

Expor o endpoint `GET /api/analytics/export` na UI:
- Botão "Exportar" na página de analytics
- Escolha de formato: JSON / CSV
- Download direto via browser

**Esforço:** 1 dia

---

### Fase D — Backend — Módulos e Features Faltantes (Sprints 7–9) 🟡

#### US-064: UserManagement Module — CRUD completo

O módulo referenciado como "Done" (US-019) não existe como módulo independente em `backend/src/Modules/`.
As operações de user admin estão embebidas em `Auth/Api/UserAdminModule.cs`.

Criar módulo dedicado `UserManagement/` com:
- `UserManagementDbContext` separado do Auth
- Commands: `UpdateUser`, `ActivateUser`, `DeactivateUser`, `ChangePassword`, `AssignRole`, `RemoveRole`
- Queries: `GetUsers`, `GetUserById`, `GetUsersByRole`, `GetActiveUsers`
- Domain events: `UserActivated`, `UserDeactivated`, `UserRoleAssigned`
- Migrar endpoints de `/api/users/*` de `UserAdminModule` para o novo módulo

**Esforço:** 5 dias

---

#### US-065: Notifications Module — Email com templates

Atualmente não há módulo de Notifications. O SignalR Hub existe em `Shared`.
Criar `Notifications/` com:
- `EmailService` com templates HTML (Razor templates ou HBS)
- Templates: `WelcomeEmail`, `IssueAssigned`, `LowStockAlert`, `PasswordChanged`
- Consumer de eventos de domínio → envia email em background
- `NotificationsDbContext` para persistir histórico de notificações enviadas

**Esforço:** 4 dias

---

#### US-066: Feature Flags — Toggle por ambiente

Adicionar suporte a feature flags simples para controlar rollout:
- Integração com `Microsoft.FeatureManagement`
- Flags em `appsettings.json` e override via variáveis de ambiente
- Exemplos: `Analytics.EnableExport`, `Inventory.EnableExpiryAlerts`

**Esforço:** 2 dias

---

#### US-067: Webhooks outbound — Notificações para sistemas externos

Permitir que clientes se cadastrem para receber eventos via HTTP webhook:
- `POST /api/webhooks` — registrar endpoint + secret
- `GET /api/webhooks` — listar registros
- Entrega assíncrona com retry (3x, backoff exponencial)
- Assinatura HMAC-SHA256 no header `X-IMS-Signature`
- Eventos suportados: `issue.created`, `issue.resolved`, `stock.low`, `product.discontinued`

**Esforço:** 5 dias

---

#### US-068: Search full-text — Elasticsearch / Meilisearch

Busca textual avançada cross-módulo:
- Indexar Issues (título, descrição, tags), Produtos (nome, SKU, descrição), InventoryIssues
- `GET /api/search?q=termo` retorna resultados ranqueados de todos os módulos
- Sync via domain events (indexação assíncrona)

**Esforço:** 1 semana

---

### Fase E — Infraestrutura e Escala (Sprints 10–12) 🟢

> Objetivo: preparar para crescimento e eventual extração de serviços.

#### US-069: Multi-tenancy — Isolamento por organização

Adicionar suporte a multi-tenancy via `TenantId`:
- Middleware `TenantResolutionMiddleware` (subdomínio ou header)
- `ITenantContext` injetado nos DbContexts (filtro global)
- Isolamento de dados no nível de query

**Esforço:** 1 semana

---

#### US-070: Background Jobs — Hangfire ou Quartz.NET

Jobs recorrentes que hoje não existem:
- `ExpiryCheckJob` — diário: varrer produtos com `ExpiryDate` nos próximos 30 dias
- `OverdueIssuesJob` — a cada 6h: marcar issues com `DueDate` passada
- `AnalyticsSnapshotJob` — semanal: snapshot de KPIs históricos para trends de longo prazo
- Dashboard de jobs via `/hangfire` (autenticado, Admin only)

**Esforço:** 3 dias

---

#### US-071: Extração de módulo — Issues como microserviço (PoC)

Demonstrar que a arquitetura modular permite extração sem reescrita:
- Extrair `Issues` para projeto separado `IMS.Issues.Service`
- Comunicação via RabbitMQ (eventos já existem)
- API gateway simples (YARP reverse proxy) roteando `/api/issues/*`
- Manter backward compatibility 100%

**Esforço:** 1 semana (PoC)

---

#### US-072: CDN e otimização de assets Blazor WASM

O `.wasm` do Blazor (~8MB) é carregado a cada deploy sem cache persistente:
- Configurar cache headers longos para assets Blazor (`Cache-Control: immutable`)
- Compressão Brotli para `.wasm` e `.dll`
- Lazy loading de módulos Blazor não usados na rota inicial

**Esforço:** 2 dias

---

## 3. Matriz de Priorização

```
                    IMPACTO
                 baixo │ alto
                ───────┼──────────
           alto │  D-08 │ D-01, D-02, D-03
  ESFORÇO       │  D-10 │ US-055, US-064
           baixo│  D-09 │ US-056, US-050~054
                ───────┼──────────
```

**Quick wins** (alto impacto, baixo esforço):
1. US-050 — corrigir compilação dos testes (2h)
2. US-056 — CORS restrito (2h)
3. US-063 — Export no frontend (1 dia)

**Investimentos estratégicos** (alto impacto, alto esforço):
1. US-064 — UserManagement Module
2. US-055 — Refresh Token rotativo
3. US-068 — Full-text search

---

## 4. Métricas de Sucesso por Fase

| Fase | Métrica-chave | Meta |
|---|---|---|
| A — Estabilização | % tests passando | 100% (de 0% atual no CI) |
| A — Estabilização | Cobertura de código | ≥ 70% nos módulos cobertos |
| B — Segurança | Vulnerabilidades OWASP Top 10 | 0 críticas |
| C — Frontend | Lighthouse score | ≥ 90 (perf + accessibility) |
| C — Frontend | E2E pass rate | 100% em CI |
| D — Backend | Endpoints documentados no Swagger | 100% |
| E — Infra | Tempo de deploy (zero-downtime) | < 3 min |

---

## 5. Backlog Priorizado (próximas 6 sprints)

```
Sprint 1:  US-050 (fix tests) + US-051 (analytics tests) + US-056 (CORS)
Sprint 2:  US-052 (auth tests) + US-053 (inventory-issues tests) + US-054 (integration tests)
Sprint 3:  US-055 (refresh token rotativo) + US-057 (RBAC policies)
Sprint 4:  US-058 (React unit tests) + US-059 (E2E analytics/Blazor)
Sprint 5:  US-060 (home dashboard) + US-063 (export frontend) + US-061 (notif history)
Sprint 6:  US-064 (UserManagement module) + US-065 (Notifications module)
```

---

## 6. Riscos

| Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|
| Testes permanecem quebrados em CI | Alta | Alto | US-050 deve ser a primeira task |
| Extração prematura de microserviços sem testes | Média | Alto | Não iniciar Fase E sem cobertura ≥ 70% |
| Blazor WASM aumenta TTI em dispositivos lentos | Média | Médio | US-072 (cache/compressão) + lazy loading |
| Multi-tenancy retroativo quebra queries existentes | Alta | Alto | Migrations e filtros globais precisam de testes extensivos antes de habilitar |

---

## 7. Decisões de Arquitetura a Tomar

| Decisão | Opções | Recomendação |
|---|---|---|
| User Management — módulo separado ou consolidar no Auth? | Módulo separado / consolidar | **Separar** — Auth cuida de credenciais, UserManagement cuida de perfil/roles |
| Full-text search — Elasticsearch ou Meilisearch? | ES (robusto) / Meilisearch (simples, Rust) | **Meilisearch** para porte atual — migrar para ES se necessário |
| Background jobs — Hangfire ou Quartz.NET? | Hangfire (UI, persistência) / Quartz (leve) | **Hangfire** — UI de monitoring alinha com postura de observabilidade do projeto |
| Feature flags — in-process ou serviço externo? | `Microsoft.FeatureManagement` / LaunchDarkly | **FeatureManagement** — sem dependência externa para o porte atual |

---

*Plano elaborado por Morpheus com base em análise de código, estado das issues, testes e arquitetura atual. Revisão sugerida a cada 2 sprints.*
