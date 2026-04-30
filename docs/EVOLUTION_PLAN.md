# 🧭 Plano de Evolução — IMS Monolith

> **Autor:** Morpheus (Lead Architect)
> **Última revisão:** Abril 2026
> **Versão atual:** 3.0 — 79 US entregues, monolito modular maduro
> **Status geral:** ✅ Roadmap das Sprints 1–12 concluído. Próxima fase: produção, escala SaaS e produto.

---

## Sumário Executivo

O IMS Monolith concluiu três fases de evolução. As **79 User Stories** entregues cobrem:

- **Backend** — 9 módulos (Auth, UserManagement, Issues, Inventory, InventoryIssues, Analytics, Notifications, Webhooks, Search/Meilisearch, Jobs/Hangfire), CQRS, Outbox, RabbitMQ, OpenTelemetry, RBAC granular, multi-tenancy (PoC), feature flags, rate limiting
- **Frontend** — Next.js 16 BFF, Blazor WASM via Custom Elements, i18n, dark mode, SignalR, React Query, OpenAPI client gerado, Brotli para Blazor, contract tests (Pact)
- **Infraestrutura** — EF Core migrations versionadas, Hangfire, Grafana com alerting, Prometheus, Jaeger, microsserviço Issues PoC com YARP + feature flag
- **Testes** — 217 testes .NET (100% passando), 18 testes de integração, ~50 testes React (Vitest+RTL), suite Playwright E2E, Pact contract tests
- **Segurança** — Refresh token rotativo com revogação no logout, CORS por ambiente, RBAC, rate limiting BFF, multi-tenancy isolation

### Estado dos débitos técnicos (após Sprints 7–12)

| # | Débito | Status |
|---|---|---|
| D-01 a D-10 | Débitos da v1.0 | ✅ Todos resolvidos |
| D-11 | UserManagement sem módulo dedicado | ✅ US-064 |
| D-12 | EnsureCreated em produção | ✅ US-065 |
| D-13 | Paginação cursor-based | 🟡 Postergado — sem volume justificável |
| D-14 | Sem rate limiting no BFF | ✅ US-076 |
| D-15 | Sem health check do frontend | ✅ `/api/health` no Next.js |
| D-16 | Sem testes de contract (Pact) | ✅ US-073 |
| D-17 | Frontend sem React Query | ✅ US-068 |
| D-18 | Sem compressão Brotli no Blazor | ✅ US-072 |
| D-19 | BFF logout não revoga refresh token | ✅ US-063 |
| D-20 | Sem OpenAPI client gerado | ✅ US-077 |
| D-21 | Issues sem `resolvedAt` no DTO | ✅ US-070 |

### Roadmap consumido

```
Sprint 1–6  (v1.0) — Fundação modular: 59 US ✅
Sprint 7    (v1.1) — Produto: Home Dashboard, Kanban, Notifications Panel, UserManagement
Sprint 8    (v1.1) — Infraestrutura: Migrations, Hangfire, Notifications Module, BFF Logout
Sprint 9    (v1.2) — Plataforma: React Query, Webhooks, Full-text search, ResolvedAt
Sprint 10   (v1.2) — Qualidade: Brotli, Pact, Feature Flags
Sprint 11   (v1.2) — Operação: Grafana Alerts, Rate Limiting, OpenAPI client
Sprint 12   (v2.0) — Escala: Multi-tenancy PoC, Issues microservice PoC, Background Jobs
```

---

## 1. Diagnóstico do Estado Atual (v3.0)

### 1.1 Cobertura de testes

```
Backend (.NET 9)
├── Unit + Integration: 217 testes — 100% passando
├── Contract tests (Pact provider): em pipeline CI
└── Cobertura: ~80% handlers, ~85% domain entities

Frontend (Next.js 16 + Blazor WASM)
├── Unit (Vitest + RTL): ~50 testes — components críticos, hooks, lib
├── Contract (Pact consumer): __tests__/contracts/
├── E2E (Playwright):  auth, issues, inventory, analytics + Blazor
└── Cobertura estimada: ~55% componentes críticos
```

### 1.2 Estado de produção

| Pré-requisito | Status |
|---|---|
| Imagens Docker assinadas e em GHCR | ✅ CI/CD |
| Health checks em todos os serviços | ✅ |
| Migrations versionadas e idempotentes | ✅ |
| Multi-tenancy (PoC, feature flag OFF) | ✅ — pronto para piloto |
| Microsserviço Issues (PoC, feature flag OFF) | ✅ — strangler fig pattern |
| Observabilidade (Prometheus, Grafana, Jaeger) | ✅ |
| Alertas Grafana (SLA, anomalias) | ✅ |
| Rate limiting (BFF + Auth) | ✅ |
| Refresh token rotativo + revogação | ✅ |
| Contract tests BFF ↔ API | ✅ |

### 1.3 Novos débitos técnicos identificados (v3.0)

#### 🔴 Críticos para produção real

| # | Débito | Local | Impacto |
|---|---|---|---|
| D-22 | **Multi-tenancy só em PoC** — feature flag OFF, queries não exercitadas em produção | `Shared/MultiTenancy/`, `*DbContext` | Bloqueador para SaaS multi-cliente |
| D-23 | **Sem load testing** — não conhecemos o ponto de saturação | infra | Risco em primeiro pico de tráfego real |
| D-24 | **Backup/restore PostgreSQL não automatizado** | infra | Perda de dados em desastre |
| D-25 | **Sem disaster recovery documentado** (RTO/RPO) | docs | Não atende compliance básico |

#### 🟡 Importantes

| # | Débito | Local | Impacto |
|---|---|---|---|
| D-26 | **Cobertura E2E em CI ainda flaky** — testes de login real dependem de backend live | `.github/workflows/ci-frontend.yml` | Bloqueia merge ocasionalmente |
| D-27 | **Hangfire dashboard sem RBAC fino** — qualquer Admin enxerga jobs de todos tenants | `Modules/Jobs/` | Vazamento de informação cross-tenant |
| D-28 | **Sem testes de carga para webhooks outbound** — vazão real desconhecida | `Modules/Webhooks/` | Pode degradar API em alta vazão |
| D-29 | **Frontend sem PWA / offline-first** — UX limitada em conexões instáveis | `next.config.ts` | UX em mobile/2G |

#### 🟢 Menores

| # | Débito | Local | Impacto |
|---|---|---|---|
| D-30 | **Sem internationalização nas notificações por email** | `Modules/Notifications/Templates/` | Templates só em PT-BR |
| D-31 | **Search/Meilisearch sem reindex automatizado em CI** | `Modules/Search/` | Drift entre DB e índice em produção |
| D-32 | **Sem audit log persistido** — apenas logs Serilog | `Shared/` | Compliance/forensics limitada |

---

## 2. Próximos Eixos — Visão v3.0 → v4.0

| Prioridade | Eixo | Impacto | Esforço | Sprint alvo |
|---|---|---|---|---|
| 🔴 Alta | **SaaS-readiness** — multi-tenancy em produção, billing básico | Muito Alto | Alto | Sprint 13–14 |
| 🔴 Alta | **DR & Compliance** — backup, restore, audit log | Alto | Médio | Sprint 13 |
| 🔴 Alta | **Load testing & capacity planning** | Alto | Médio | Sprint 13 |
| 🟡 Média | **Frontend PWA** — offline-first, push notifications | Médio | Médio | Sprint 14 |
| 🟡 Média | **i18n nas notificações** — emails em EN/PT | Baixo | Baixo | Sprint 14 |
| 🟡 Média | **Hangfire RBAC tenant-aware** | Médio | Baixo | Sprint 14 |
| 🟢 Baixa | **Cursor-based pagination** | Médio | Médio | Sprint 15 |
| 🟢 Baixa | **Audit log com event sourcing leve** | Médio | Alto | Sprint 15 |
| 🟢 Baixa | **AI-assisted analytics** — sumarização LLM de issues | Alto | Alto | Sprint 16+ |

---

## 3. Backlog Detalhado — Próximas USs

### Sprint 13 — Production-readiness 🔴

#### US-080: Multi-tenancy em produção (graduação do PoC)

Promover o multi-tenancy de PoC para feature suportada em produção:
- Migrations idempotentes para todos os contextos
- Suite de testes ampliada (≥ 95% nas queries com filtro de tenant)
- Observabilidade por tenant (labels Prometheus, traces tagueados)
- Documentação operacional (criação de tenant, isolamento, cobrança básica)

**Esforço:** 2 semanas

#### US-081: Backup automatizado e Disaster Recovery

- `pg_dump` cron com upload para S3-compatible
- Procedimento de restore documentado e testado em ambiente staging
- RTO 4h, RPO 24h definidos
- Runbook em `docs/RUNBOOK.md`

**Esforço:** 3 dias

#### US-082: Load testing baseline com k6

- Cenários: 100/500/1000 RPS em endpoints críticos (`/api/issues`, `/api/inventory`)
- Identificar bottlenecks (DB, RabbitMQ, Redis)
- Resultados publicados em `docs/perf/`
- Configurar Grafana dashboard de SLI/SLO

**Esforço:** 4 dias

#### US-083: Audit log estruturado

Persistir eventos sensíveis (login, role change, data deletion):
- Tabela `AuditLog` particionada por mês
- Middleware captura automaticamente endpoints protegidos
- Endpoint `/api/audit?userId=...` com paginação

**Esforço:** 4 dias

---

### Sprint 14 — UX e Operação 🟡

#### US-084: Frontend PWA — Offline-first

- `next-pwa` ou implementação manual com service worker
- Cache strategies: stale-while-revalidate para listagens
- Web push notifications integradas com SignalR
- Manifest com ícones, splash screen, theme color

**Esforço:** 1 semana

#### US-085: i18n para emails de notificação

- Templates Razor com `IStringLocalizer`
- `Accept-Language` do usuário guia o template
- Locale fallback PT-BR

**Esforço:** 2 dias

#### US-086: Hangfire RBAC tenant-aware

- Custom `IDashboardAuthorizationFilter` valida tenant claim
- Admin de tenant A só vê jobs do tenant A
- Job filter automático aplica `TenantId` quando `IFeatureFlag.EnableMultiTenancy` ativo

**Esforço:** 2 dias

---

### Sprint 15 — Performance e Compliance 🟢

#### US-087: Cursor-based pagination

- Substituir offset paging por keyset (`(CreatedAt, Id)` cursor)
- Manter offset como fallback opcional
- Aplicar primeiro em `/api/issues` e `/api/inventory/products`

**Esforço:** 4 dias

#### US-088: Reindex automatizado do Meilisearch

- Job semanal compara hash do conteúdo no DB vs índice
- Reindex incremental se drift > 1%
- Métricas Prometheus expostas (`meilisearch_drift_ratio`)

**Esforço:** 3 dias

#### US-089: Compliance — LGPD/GDPR data export & delete

- Endpoint `/api/users/{id}/data-export` (JSON com todos dados)
- Endpoint `/api/users/{id}/delete-request` (soft-delete + scheduled hard-delete em 30d)
- Audit log obrigatório nesses eventos

**Esforço:** 1 semana

---

### Sprint 16+ — Inovação 🟢

#### US-090: AI-assisted issue triage (PoC)

- Endpoint `/api/issues/{id}/summarize` chama LLM (OpenAI/Anthropic) e devolve resumo + tags sugeridas
- Feature flag `EnableAITriage` (off por padrão)
- Custos rastreados por tenant

**Esforço:** 1 semana

#### US-091: Real-time collaborative kanban

- WebRTC ou Yjs sobre o canal SignalR existente para sync de cursores e edição concorrente
- Resolução de conflitos via CRDT

**Esforço:** 2 semanas

---

## 4. Roadmap Resumido — Próximas 4 Sprints

```
Sprint 13: US-080 Multi-tenancy em produção (2sem)
           US-081 Backup & DR (3d)
           US-082 Load testing baseline (4d)
           US-083 Audit log (4d)

Sprint 14: US-084 PWA / offline-first (1sem)
           US-085 i18n emails (2d)
           US-086 Hangfire RBAC tenant-aware (2d)

Sprint 15: US-087 Cursor pagination (4d)
           US-088 Meilisearch reindex (3d)
           US-089 LGPD/GDPR export & delete (1sem)

Sprint 16+: US-090 AI-assisted triage (PoC)
            US-091 Collaborative kanban (PoC)
```

---

## 5. Métricas de Sucesso (v3.0)

| Categoria | Métrica | Estado atual | Meta v4.0 |
|---|---|---|---|
| Testes .NET | Quantidade | 217 ✅ | ≥ 250 |
| Testes .NET | Pass-rate | 100% | Manter |
| Testes Frontend | Componentes cobertos | ~55% | ≥ 75% |
| Pact Contracts | BFF ↔ API .NET | ✅ Implementado | Manter green |
| Performance | TTI Blazor (cold) | ~2s (com Brotli) | ≤ 1.5s |
| Performance | p95 `/api/issues` | desconhecido | ≤ 200ms a 500RPS |
| Multi-tenancy | Cobertura de queries com filtro | PoC | 100% em produção |
| DR | RTO definido | ❌ | ≤ 4h |
| DR | RPO definido | ❌ | ≤ 24h |

---

## 6. Riscos Atuais

| Risco | Prob | Impacto | Mitigação |
|---|---|---|---|
| Multi-tenancy ativo em produção sem cobertura completa | Média | Alto | US-080 — não ativar feature flag em prod sem ≥ 95% de testes nas queries |
| Sem backup automatizado em deploy real | Alta | Alto | US-081 antes de qualquer ambiente com dados de cliente |
| Microsserviço Issues divergir do monolito (drift de schema) | Média | Médio | Pact tests entre os dois + feature flag rollback |
| Hangfire expor jobs cross-tenant | Média | Médio | US-086 antes de habilitar multi-tenancy |
| Webhooks com pico de retries derrubam consumer | Baixa | Médio | DLQ já existe no RabbitMQ; adicionar circuit breaker |

---

## 7. Decisões Arquiteturais Abertas

| Decisão | Opções | Recomendação | Prazo |
|---|---|---|---|
| Estratégia multi-tenant em prod | Row-level (atual PoC) / Schema-per-tenant / DB-per-tenant | **Row-level** mantido — schemas só se cliente enterprise exigir | Sprint 13 |
| Modelo de billing | Stripe / Mercado Pago / próprio | Avaliar quando primeiro cliente pagante surgir | Sprint 14+ |
| Cloud target | AWS / Azure / GCP | Azure (alinhado ao stack .NET + GitHub) | Sprint 13 |
| Audit log: tabela ou event store? | Append-only table / EventStore | Tabela particionada — simplicidade > pureza | Sprint 13 |
| LLM provider | OpenAI / Anthropic / Azure OpenAI | Azure OpenAI (residency BR) | Sprint 16+ |

---

## 8. Visão de Longo Prazo (12–24 meses)

```
v3.0 (atual) — Monolito modular maduro com PoC de microsserviço e multi-tenancy

     ▼ Sprint 13–15
v3.1 — Production SaaS-ready
     ├── Multi-tenancy ativo, DR, audit log, load testing baseline
     └── Compliance LGPD/GDPR

     ▼ Sprint 16–20
v4.0 — Plataforma colaborativa
     ├── PWA, real-time collaboration, AI-assisted triage
     └── Microsserviços: extração gradual de Inventory + Analytics

     ▼ +12 meses
v5.0 — Marketplace (visão)
     ├── Plugins de terceiros via SDK público
     └── Multi-region (latência por geografia)
```

---

*Plano elaborado por Morpheus com base nas 79 USs entregues, débitos técnicos identificados e necessidade de production-readiness real. Revisão a cada 2 sprints ou após mudança estratégica relevante.*
