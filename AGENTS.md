# IMS Monolith — Guia de Agentes

## Visão Geral

Este repositório usa múltiplos agentes de IA trabalhando em paralelo.
Cada agente tem uma especialidade e ownership claro sobre um conjunto de USs.

---

## 👥 Agentes Ativos

| Agente | Label | Especialidade | USs Ativas |
|---|---|---|---|
| **Morpheus** | `agent:morpheus` | Arquiteto / Lead — decisões cross-cutting, auth, compliance, infra | ~~US-080 (#108)~~ ✅ ~~US-082 (#109)~~ ✅ ~~US-089 (#116)~~ ✅ — US-090 (#122) 🔜 fix #126 🔜 |
| **Neo** | `agent:neo` | Backend / Full-stack — módulos de negócio, integrações, shared kernel | ~~US-083 (#110)~~ ✅ ~~US-085 (#112)~~ ✅ ~~US-086 (#113)~~ ✅ ~~US-087 (#114)~~ ✅ ~~US-088 (#115)~~ ✅ — US-091 (#123) 🔜 US-092 (#124) 🔜 |
| **Trinity** | `agent:trinity` | UI / DX — theming, acessibilidade, user experience, frontend | ~~US-084 (#111)~~ ✅ merged PR #121 — US-093 (#125) 🔜 |

---

## 📋 Sprint 13/14/15 — Production SaaS-ready (concluído ✅)

```
Onda 1 (paralela — sem dependências entre si):
├── Morpheus  → feat/US-080-multitenancy-production   (#108) ✅ merged PR #117
├── Morpheus  → feat/US-082-load-testing-k6           (#109) ✅ merged PR #118
└── Neo       → feat/US-083-audit-log                 (#110) ✅ merged PR #120

Onda 2:
├── Trinity   → feat/US-084-pwa-offline               (#111) ✅ merged PR #121
├── Neo       → feat/US-085-i18n-emails               (#112) ✅ merged PR #120
└── Neo       → feat/US-086-hangfire-rbac-tenant      (#113) ✅ merged PR #120

Sprint 15:
├── Neo       → feat/US-087-cursor-pagination         (#114) ✅ merged PR #120
├── Neo       → feat/US-088-meilisearch-reindex       (#115) ✅ merged PR #120
└── Morpheus  → feat/US-089-lgpd-gdpr                 (#116) ✅ merged PR #119
```

### ✅ Dependência crítica US-080 → US-086: resolvida
US-080 foi mergeada antes de US-086. Ambas concluídas.

---

## 📋 Sprint 16 — SaaS Identity, Billing & Documentação (atual)

```
├── Morpheus  → feat/US-090-keycloak-poc              (#122) 🔜 a iniciar
├── Neo       → feat/US-091-billing-subscription      (#123) 🔜 a iniciar
├── Neo       → feat/US-092-tenant-onboarding         (#124) 🔜 a iniciar
├── Trinity   → feat/US-093-codemaps-c4               (#125) 🔜 a iniciar
└── Morpheus  → fix/debug-tenant-endpoint             (#126) 🔜 a iniciar
```

### Dependências Sprint 16
- US-092 (onboarding) depende de US-091 (billing) para definir o plano inicial no signup.

---

## 🔀 Branches

| Branch | Agente | Issue | Status |
|---|---|---|---|
| `feat/US-080-multitenancy-production` | Morpheus | #108 | ✅ merged PR #117 |
| `feat/US-082-load-testing-k6` | Morpheus | #109 | ✅ merged PR #118 |
| `feat/US-083-audit-log` | Neo | #110 | ✅ merged PR #120 |
| `feat/US-084-pwa-offline` | Trinity | #111 | ✅ merged PR #121 |
| `feat/US-085-i18n-emails` | Neo | #112 | ✅ merged PR #120 |
| `feat/US-086-hangfire-rbac-tenant` | Neo | #113 | ✅ merged PR #120 |
| `feat/US-087-cursor-pagination` | Neo | #114 | ✅ merged PR #120 |
| `feat/US-088-meilisearch-reindex` | Neo | #115 | ✅ merged PR #120 |
| `feat/US-089-lgpd-gdpr` | Morpheus | #116 | ✅ merged PR #119 |
| `feat/US-090-keycloak-poc` | Morpheus | #122 | 🔜 a iniciar |
| `feat/US-091-billing-subscription` | Neo | #123 | 🔜 a iniciar |
| `feat/US-092-tenant-onboarding` | Neo | #124 | 🔜 a iniciar |
| `feat/US-093-codemaps-c4` | Trinity | #125 | 🔜 a iniciar |
| `fix/debug-tenant-endpoint` | Morpheus | #126 | 🔜 a iniciar |

---

## 📐 Regras de Convivência

### 1. Arquivos compartilhados — lock implícito
Antes de editar qualquer arquivo abaixo, avise os outros agentes via commit de WIP:

| Arquivo | Owner atual |
|---|---|
| `frontend/apps/next-shell/app/(dashboard)/layout.tsx` | Qualquer (notificar) |
| `frontend/apps/next-shell/components/sidebar.tsx` | Trinity (US-040/041) |
| `frontend/apps/next-shell/lib/auth.ts` | Morpheus (US-037) |
| `frontend/apps/next-shell/next.config.ts` | Morpheus (US-042) |
| `frontend/apps/next-shell/tailwind.config.*` | Trinity (US-041) |

### 2. Nunca commitar direto em `main`
Sempre via PR com squash merge.

### 3. Convenção de commits
```
feat(US-0XX): descrição curta
fix(US-0XX): descrição
```

### 4. Conflitos de merge
- Se dois agentes editarem o mesmo arquivo, o **segundo** a abrir PR resolve o conflito.
- Usar `git rebase origin/main` antes de abrir PR.

### 5. Criação de componentes UI compartilhados
Novos componentes em `components/ui/` devem ser genéricos e documentados com JSDoc.
Trinity é responsável pela aprovação de novos componentes de UI base.

### 6. Documentação deve acompanhar o código
Toda alteração de comportamento, arquitetura ou contrato de API **deve atualizar a documentação correspondente** na mesma PR:
- Novos módulos → atualizar `docs/architecture/` (workspace.dsl + regenerar SVGs)
- Novos endpoints ou mudança de contrato → atualizar `README.md` (seção API Reference) e `docs/TECHNICAL_GUIDE.md`
- Mudança de evento de domínio (novo, removido ou renomeado) → atualizar `docs/architecture/src/module-graph.mmd` e regenerar `module-graph.svg`
- Mudança na lógica de multi-tenancy → atualizar `docs/architecture/src/multitenancy-flow.mmd`
- Decisões de arquitetura cross-cutting → criar ou atualizar um ADR em `docs/ADR-*.md`

---

## 🏗️ Estrutura de pastas relevante

```
frontend/apps/next-shell/
├── app/
│   ├── (auth)/          # login, register — Morpheus (US-037)
│   ├── (dashboard)/
│   │   ├── layout.tsx   # COMPARTILHADO — notificar antes de editar
│   │   ├── inventory/   # Neo (US-038)
│   │   ├── analytics/   # existente
│   │   ├── issues/      # existente
│   │   ├── admin/       # Trinity (US-040) — novo
│   │   └── profile/     # Trinity (US-040) — novo
│   └── api/
│       ├── auth/        # Morpheus (US-037)
│       └── proxy/       # Neo (US-038/039)
├── components/
│   ├── ui/              # Trinity (US-041) — base components
│   ├── notifications/   # Neo (US-039) — novo
│   ├── admin/           # Trinity (US-040) — novo
│   └── inventory/       # Neo (US-038) — novo
├── lib/
│   ├── api-client.ts    # Morpheus (US-037) — CRÍTICO, outros dependem
│   ├── auth.ts          # Morpheus (US-037)
│   ├── signalr-client.ts # Neo (US-039) — novo
│   └── api/
│       ├── inventory.ts # Neo (US-038) — novo
│       └── users.ts     # Trinity (US-040) — novo
└── messages/            # Morpheus (US-042) — novo
    ├── pt.json
    └── en.json
```

---

## 🔒 Multi-Tenancy Isolation (US-078/080) — Concluído

### Problema raiz identificado e resolvido
A falha de isolamento entre tenants (`Issues_BetaTenant_CanSeeOnlyBetaIssues`) foi causada por um **vazamento de cache cross-tenant** no `CachingBehavior<TRequest, TResponse>`.

O `GetAllIssuesQuery` implementa `ICacheable` com prefixo `"issues-list"`. A chave de cache era gerada apenas com base nos parâmetros da requisição (paginação, filtros), **sem incluir o tenant**. O resultado do tenant Alpha era cacheado e devolvido ao tenant Beta quando os parâmetros eram idênticos.

### Correção aplicada
**`backend/src/Shared/Behaviors/CachingBehavior.cs`**
- Injetado `ITenantService` (opcional, via DI)
- Chave de cache agora tem formato: `{prefix}:{tenantId}:{sha256-hash}`
- Fix é **global** — protege todos os módulos que usam `ICacheable` automaticamente

### Outras correções de multi-tenancy nesta sessão
| Componente | Correção |
|---|---|
| `InventoryReadRepositories.cs` | Dapper queries filtradas por `TenantId` via `ITenantService` |
| `InventoryQueryHandlers.cs` | Cache keys manuais incluem `tenantId` |
| `IssueQueryHandlers.cs` | `IgnoreQueryFilters()` + `WithTenantFilter(tenantService)` explícito |
| `TenantAwareDbContext.cs` | `TenantModelCacheKeyFactory` para isolar modelos compilados por tenant |
| `TenantQueryExtensions.cs` | Helper `WithTenantFilter<T>()` para aplicar WHERE por tenant em qualquer `IQueryable<ITenantEntity>` |
| `CachingBehavior.cs` | **Fix final** — tenant ID incluído em TODAS as cache keys de `ICacheable` |

### Resultado
- **224/224 testes passando**, incluindo todos os 7 testes de `MultiTenancyRealIsolationTests`
- Nenhuma regressão

---

## ✅ Checklist antes de abrir PR

- [ ] `npm run typecheck` passa sem erros
- [ ] `npm run lint` passa sem erros
- [ ] `npm run build` passa sem erros
- [ ] Novos E2E adicionados para o fluxo implementado (se aplicável)
- [ ] `git rebase origin/main` feito antes do push final
- [ ] Sem `console.log` de debug no código final
- [ ] **Documentação atualizada** — README, docs/architecture, ADRs conforme regra #6
