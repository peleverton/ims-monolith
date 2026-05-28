# Keycloak — Guia Operacional (US-090)

## Visão Geral

Keycloak é o identity provider opcional do IMS, controlado pela feature flag `UseKeycloak`.  
Quando ativo, delega toda autenticação ao Keycloak via OIDC — SSO, OAuth2, social login e gestão de usuários out-of-the-box.

---

## Subindo o Keycloak localmente

```bash
# Sobe toda a infra de dev incluindo Keycloak (porta 8180)
docker compose -f docker-compose.dev.yml up -d

# Aguarda o Keycloak inicializar (~30-60s na primeira vez)
docker logs -f ims-keycloak
```

**Admin UI:** http://localhost:8180  
**Credenciais:** `admin` / `admin`  
**Realm:** `ims` (importado automaticamente de `infra/keycloak/ims-realm.json`)

### Usuários de teste

| Usuário | Senha | Roles |
|---|---|---|
| `admin` | `admin123` | Admin, User |
| `manager` | `manager123` | Manager, User |
| `user` | `user123` | User |

---

## Habilitando o Keycloak

### Backend (ASP.NET Core)

1. Certifique-se que o Keycloak está rodando
2. Atualize `appsettings.Development.json`:

```json
{
  "FeatureManagement": {
    "UseKeycloak": true
  },
  "Keycloak": {
    "Authority": "http://localhost:8180/realms/ims",
    "Audience": "ims-backend",
    "ClientId": "ims-frontend",
    "RequireHttpsMetadata": false
  }
}
```

3. Reinicie o backend (a feature flag é lida no startup):

```bash
cd backend
dotnet run
```

### Frontend (Next.js)

1. Adicione ao `.env.local`:

```env
NEXT_PUBLIC_USE_KEYCLOAK=true
NEXT_PUBLIC_KEYCLOAK_AUTHORITY=http://localhost:8180/realms/ims
NEXT_PUBLIC_KEYCLOAK_CLIENT_ID=ims-frontend
```

2. Reinicie o frontend:

```bash
cd frontend/apps/next-shell
npm run dev
```

A página de login exibirá o botão **"Entrar com Keycloak (SSO)"**.

---

## Fluxo de Autenticação PKCE

```
Usuário → "Entrar com Keycloak"
  ↓
GET /api/auth/keycloak-login
  ↓ gera code_verifier + code_challenge (S256)
  ↓ armazena verifier em cookie HttpOnly (TTL 120s)
Redirect → http://localhost:8180/realms/ims/protocol/openid-connect/auth
  ↓ usuário faz login no Keycloak
Redirect → /api/auth/keycloak-callback?code=...&state=...
  ↓ valida state (CSRF)
  ↓ troca code + verifier por tokens (token endpoint Keycloak)
  ↓ seta cookies HttpOnly: ims_access_token + ims_refresh_token
Redirect → /issues (ou callbackUrl)
```

---

## Realm `ims` — Configuração

O realm é definido em `infra/keycloak/ims-realm.json` e importado automaticamente no primeiro boot.

### Clients

| Client ID | Tipo | Uso |
|---|---|---|
| `ims-backend` | bearer-only | Backend valida tokens (não emite) |
| `ims-frontend` | public + PKCE | BFF Next.js — Authorization Code + PKCE |

### Protocol Mapper

O client scope `ims-roles` inclui um mapper que emite as roles do realm como claim `roles` no JWT:

```json
{
  "claim.name": "roles",
  "multivalued": "true",
  "protocolMapper": "oidc-usermodel-realm-role-mapper"
}
```

Isso garante que `KeycloakClaimsTransformation` consiga mapear `roles` → `ClaimTypes.Role` no backend.

---

## Claims Mapping

| Claim Keycloak | Tipo .NET | Onde usado |
|---|---|---|
| `sub` | `ClaimTypes.NameIdentifier` | `UserContext.UserId` |
| `email` | `ClaimTypes.Email` | `UserContext.Email` |
| `preferred_username` | `ClaimTypes.Name` | `user.Identity.Name` |
| `realm_access.roles` | `ClaimTypes.Role` (via transformer) | Authorization policies |
| `roles` (flat, via mapper) | `ClaimTypes.Role` (via transformer) | Authorization policies |

---

## Produção (Docker Compose full stack)

Para habilitar Keycloak em produção, adicione ao `docker-compose.yml`:

```yaml
keycloak:
  image: quay.io/keycloak/keycloak:24.0
  command: start --import-realm
  environment:
    KEYCLOAK_ADMIN: ${KEYCLOAK_ADMIN}
    KEYCLOAK_ADMIN_PASSWORD: ${KEYCLOAK_ADMIN_PASSWORD}
    KC_DB: postgres
    KC_DB_URL: jdbc:postgresql://postgres:5432/${POSTGRES_DB}
    KC_DB_USERNAME: ${POSTGRES_USER}
    KC_DB_PASSWORD: ${POSTGRES_PASSWORD}
    KC_HOSTNAME: ${KEYCLOAK_HOSTNAME}
    KC_HTTPS_CERTIFICATE_FILE: /opt/keycloak/conf/tls.crt
    KC_HTTPS_CERTIFICATE_KEY_FILE: /opt/keycloak/conf/tls.key
  volumes:
    - ./infra/keycloak:/opt/keycloak/data/import
    - keycloak_certs:/opt/keycloak/conf
  ports:
    - "8443:8443"
```

E ative o flag via variável de ambiente:

```env
FeatureManagement__UseKeycloak=true
Keycloak__Authority=https://keycloak.yourdomain.com/realms/ims
Keycloak__Audience=ims-backend
Keycloak__RequireHttpsMetadata=true
```

---

## Troubleshooting

### Token inválido / 401 após login Keycloak

1. Verifique se `Keycloak:Authority` aponta para o realm correto
2. Verifique se `Keycloak:Audience` bate com o `clientId` do `ims-backend`
3. Inspecione o JWT em https://jwt.io — verifique `iss` e `aud`

### Realm não importado

```bash
# Reimportar manualmente
docker exec ims-keycloak /opt/keycloak/bin/kc.sh import --dir /opt/keycloak/data/import
```

### Resetar dados do Keycloak

```bash
docker compose -f docker-compose.dev.yml down -v
docker compose -f docker-compose.dev.yml up -d
```

---

## Roadmap — Migração completa

1. **US-090 (atual)**: PoC — flag `UseKeycloak`, PKCE flow, claims mapping
2. **Próxima US**: Migrar criação de usuários para Keycloak Admin API
3. **Próxima US**: Deprecar `/api/auth/login` custom quando `UseKeycloak=true`
4. **Próxima US**: Social login (Google/GitHub) via Keycloak Identity Providers
5. **Futuro**: Remover `AuthenticationService` custom + `JwtTokenService`
