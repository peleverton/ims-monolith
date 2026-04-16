# IMS Monolith — Guia de Agentes

## Visão Geral

Este repositório usa múltiplos agentes de IA trabalhando em paralelo.
Cada agente tem uma especialidade e ownership claro sobre um conjunto de USs.

---

## 👥 Agentes Ativos

| Agente | Label | Especialidade | USs Ativas |
|---|---|---|---|
| **Morpheus** | `agent:morpheus` | Arquiteto / Lead — decisões cross-cutting, auth, i18n | US-037, US-042 |
| **Neo** | `agent:neo` | Backend / Full-stack — BFF, integrações, módulos de negócio | US-038, US-039 |
| **Trinity** | `agent:trinity` | UI / DX — theming, acessibilidade, user experience | US-040, US-041 |

---

## 📋 Fase 10 — Frontend Quality & Features

```
Onda 1 (paralela — sem dependências entre si):
├── Morpheus  → feat/US-037-auth-hardening          (#52) ✅ PR #59 mergeado
├── Neo       → feat/US-038-inventory-crud-frontend  (#53) ✅ PR #61 mergeado
└── Trinity   → feat/US-041-dark-mode               (#56) 🔄 em andamento

Onda 2 (paralela — inicia após Onda 1):
├── Morpheus  → feat/US-042-i18n                    (#57) ✅ PR #60 mergeado
├── Neo       → feat/US-039-signalr-notifications    (#54) 🔄 em andamento
└── Trinity   → feat/US-040-user-management-frontend (#55) 🔄 em andamento
```

### Dependência crítica: `lib/api-client.ts`
A **US-037** (Morpheus) cria o `api-client` com interceptor de refresh token.
As USs da Onda 2 dependem desse client. **Regra:** não mergear Onda 2 antes de US-037.

---

## 🔀 Branches

| Branch | Agente | Issue | Status |
|---|---|---|---|
| `feat/US-037-auth-hardening` | Morpheus | #52 | ✅ PR #59 mergeado |
| `feat/US-038-inventory-crud-frontend` | Neo | #53 | ✅ PR #61 mergeado |
| `feat/US-039-signalr-notifications` | Neo | #54 | 🔄 em andamento |
| `feat/US-040-user-management-frontend` | Trinity | #55 | 🔄 em andamento |
| `feat/US-041-dark-mode` | Trinity | #56 | 🔄 em andamento |
| `feat/US-042-i18n` | Morpheus | #57 | ✅ PR #60 mergeado |

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

## ✅ Checklist antes de abrir PR

- [ ] `npm run typecheck` passa sem erros
- [ ] `npm run lint` passa sem erros
- [ ] `npm run build` passa sem erros
- [ ] Novos E2E adicionados para o fluxo implementado (se aplicável)
- [ ] `git rebase origin/main` feito antes do push final
- [ ] Sem `console.log` de debug no código final
