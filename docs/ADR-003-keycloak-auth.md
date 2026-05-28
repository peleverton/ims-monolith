# ADR-003: Keycloak como Identity Provider opcional (Strangler Fig)

**Status:** Accepted  
**Data:** 2026-05-27  
**US:** US-090  
**Autores:** Morpheus (Copilot Agent)

---

## Contexto

O IMS possui um sistema de autenticação custom: JWT HMAC-SHA256 emitido pelo próprio backend (`AuthenticationService` + `JwtTokenService`). Para o produto SaaS multi-tenant, precisamos de:

- **SSO** (Single Sign-On) entre tenants e aplicações
- **Social Login** (Google, GitHub, etc.)
- **Gestão de usuários** out-of-the-box (reset de senha, MFA, sessões)
- **OAuth2/OIDC** padrão para integrações de terceiros

Keycloak é um identity provider open-source maduro que entrega tudo isso. A migração completa é arriscada; o **Strangler Fig pattern** permite migração incremental com zero downtime.

---

## Decisão

Integrar Keycloak como **identity provider opcional**, controlado pela feature flag `UseKeycloak`.

### Comportamento

| `UseKeycloak` | Auth Provider | Tokens | BFF Flow |
|---|---|---|---|
| `false` (padrão) | Custom JWT (HMAC-SHA256) | Emitido pelo backend | POST /api/auth/login |
| `true` | Keycloak (RSA/OIDC) | Emitido pelo Keycloak | PKCE Authorization Code |

### Componentes

```
┌─────────────────────────────────────────────────────────┐
│  Next.js BFF (Frontend)                                  │
│                                                          │
│  UseKeycloak=false:                                      │
│    POST /api/auth/login → IMS Backend → JWT cookie       │
│                                                          │
│  UseKeycloak=true:                                       │
│    GET /api/auth/keycloak-login                          │
│      ↓ PKCE code_verifier + challenge                    │
│    Redirect → Keycloak Authorization Endpoint            │
│      ↓ code callback                                     │
│    GET /api/auth/keycloak-callback                       │
│      ↓ exchange code + verifier → Keycloak token endpoint│
│    Set cookies (access_token, refresh_token)             │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│  IMS Backend (ASP.NET Core)                             │
│                                                          │
│  UseKeycloak=false:                                      │
│    JwtBearer validates HMAC-SHA256 tokens                │
│    (IssuerSigningKey = Jwt:SecretKey)                    │
│                                                          │
│  UseKeycloak=true:                                       │
│    JwtBearer validates RSA tokens via Keycloak JWKS      │
│    (Authority = Keycloak:Authority)                      │
│    KeycloakClaimsTransformation:                         │
│      realm_access.roles → ClaimTypes.Role                │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│  UserContextMiddleware (unchanged)                       │
│  TenantMiddleware (unchanged)                            │
│  Authorization Policies (unchanged)                      │
└─────────────────────────────────────────────────────────┘
```

---

## Consequências

### Positivas
- **Zero regressão**: `UseKeycloak=false` preserva 100% do comportamento atual
- **Migração incremental**: pode ser ligado/desligado por ambiente via feature flag
- **Sem mudanças em downstream**: `IUserContext`, authorization policies e todos os handlers funcionam sem modificação
- **Claims compatíveis**: `KeycloakClaimsTransformation` mapeia `realm_access.roles` para `ClaimTypes.Role`

### Negativas / Trade-offs
- Quando `UseKeycloak=true`, a emissão de tokens por `/api/auth/login` ainda usa o auth custom (os dois sistemas coexistem por enquanto)
- A migração completa (deprecar o auth custom) requer uma US separada
- A feature flag é lida em startup (`IConfiguration`), não via `IFeatureManager` dinâmico — mudança requer restart

### Riscos
- Keycloak é uma dependência externa; falhas no Keycloak quando o flag está ativo derrubam o auth inteiro
- A realm export (`ims-realm.json`) precisa ser versionada e auditada cuidadosamente

---

## Alternativas consideradas

| Alternativa | Motivo da rejeição |
|---|---|
| ASP.NET Core Identity | Não resolve SSO/OIDC/social login; mais código para manter |
| Auth0 | SaaS pago, vendor lock-in |
| Okta | Idem Auth0 |
| Migração direta (sem feature flag) | Alto risco — zero downtime impossível sem feature flag |

---

## Referências

- [Keycloak Documentation](https://www.keycloak.org/documentation)
- [RFC 7636 — PKCE](https://datatracker.ietf.org/doc/html/rfc7636)
- [Strangler Fig Pattern — Martin Fowler](https://martinfowler.com/bliki/StranglerFigApplication.html)
- ADR-001: Multi-tenancy architecture
