# 🏢 Multi-Tenancy — Guia Operacional (US-080)

> **Versão:** 1.0 — Maio 2026  
> **Status:** Production-ready (feature flag `EnableMultiTenancy`)  
> **RTO:** 4h | **RPO:** 24h (em linha com RUNBOOK.md)

---

## 1. Visão Geral

O IMS usa **row-level isolation** — todos os tenants compartilham o mesmo banco de dados e schemas, com isolamento garantido pela coluna `TenantId` em cada tabela e filtragem automática nas queries.

```
┌─────────────────────────────────────────────┐
│            HTTP Request                      │
│  Header: X-Tenant-Id: acme-corp             │
└──────────────────┬──────────────────────────┘
                   │
         ┌─────────▼──────────┐
         │  TenantMiddleware   │  Valida tenant existe e IsActive=true
         │  (US-080)          │  → 403 se inválido
         └─────────┬──────────┘
                   │ TenantContext.TenantId = "acme-corp"
         ┌─────────▼──────────┐
         │  Handler/DbContext  │  IgnoreQueryFilters() + WithTenantFilter()
         │  (EF Core / Dapper)│  WHERE TenantId = 'acme-corp'
         └─────────┬──────────┘
                   │
         ┌─────────▼──────────┐
         │  CachingBehavior   │  Cache key: prefix:acme-corp:hash
         └────────────────────┘
```

---

## 2. Ativando Multi-Tenancy

### Staging / Desenvolvimento
Já ativo em `appsettings.Development.json`:
```json
{
  "FeatureManagement": {
    "EnableMultiTenancy": true
  }
}
```

### Produção
Alterar `appsettings.Production.json` ou variável de ambiente:
```bash
# Via variável de ambiente (recomendado para produção)
export FeatureManagement__EnableMultiTenancy=true
```

> ⚠️ **Antes de ativar em produção:**
> 1. Confirmar que todas as tabelas têm coluna `TenantId` (migrations aplicadas)
> 2. Rodar `./scripts/manage-tenants.sh list` e confirmar tenants cadastrados
> 3. Popular `TenantId` nos registros existentes (script SQL abaixo)

### Script de população inicial em produção
```sql
-- Migrar registros existentes para o tenant "default" (single-tenant legacy)
UPDATE "Issues"     SET "TenantId" = 'default' WHERE "TenantId" IS NULL;
UPDATE "Products"   SET "TenantId" = 'default' WHERE "TenantId" IS NULL;
UPDATE "Suppliers"  SET "TenantId" = 'default' WHERE "TenantId" IS NULL;
UPDATE "Locations"  SET "TenantId" = 'default' WHERE "TenantId" IS NULL;
UPDATE "StockMovements" SET "TenantId" = 'default' WHERE "TenantId" IS NULL;
```

---

## 3. Gerenciamento de Tenants

### Via CLI (recomendado)
```bash
# Autenticar
export ADMIN_TOKEN=$(curl -s -X POST $API_URL/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin","password":"Admin@123!"}' | jq -r .accessToken)

# Listar tenants
./scripts/manage-tenants.sh list

# Criar tenant
./scripts/manage-tenants.sh create acme-corp "Acme Corporation" starter cto@acme.com

# Desativar (soft-delete — dados preservados)
./scripts/manage-tenants.sh deactivate acme-corp

# Reativar
./scripts/manage-tenants.sh activate acme-corp
```

### Via API REST
```bash
# GET /api/tenants              — lista todos
# GET /api/tenants/{id}         — busca por ID
# POST /api/tenants             — cria novo
# PUT /api/tenants/{id}         — atualiza
# DELETE /api/tenants/{id}      — soft-delete (IsActive=false)
```

Todos os endpoints requerem `Authorization: Bearer <token>` de um usuário Admin.

---

## 4. Fluxo de Onboarding de Novo Cliente

```
1. Criar tenant via CLI ou API
2. Criar usuário admin do tenant (POST /api/auth/register com claim tenant_id)
3. (Opcional) Migrar dados existentes via SQL com TenantId correto
4. Entregar credenciais + X-Tenant-Id ao cliente
5. Monitorar no Grafana: dashboard "IMS — Multi-Tenancy Overview"
```

---

## 5. Observabilidade por Tenant

### Métricas Prometheus
| Métrica | Tipo | Labels |
|---|---|---|
| `ims_tenant_requests_total` | Counter | `tenant_id`, `http_method`, `http_status_code` |

### Queries PromQL úteis
```promql
# Requests por segundo por tenant
sum by (tenant_id) (rate(ims_tenant_requests_total[2m]))

# Taxa de erro por tenant
sum by (tenant_id) (rate(ims_tenant_requests_total{http_status_code=~"5.."}[2m]))
/ sum by (tenant_id) (rate(ims_tenant_requests_total[2m]))

# Top 5 tenants mais ativos (últimas 24h)
topk(5, sum by (tenant_id) (increase(ims_tenant_requests_total[24h])))
```

### Dashboard Grafana
Dashboard `IMS — Multi-Tenancy Overview` disponível em `infra/grafana/provisioning/dashboards/ims-tenants.json`.

### Alertas configurados
| Alerta | Condição | Severidade |
|---|---|---|
| `ims-tenant-high-error-rate` | >5% requisições 5xx por tenant por 2min | warning |
| `ims-tenant-no-traffic` | Sem tráfego por 15min | info |

### Traces OpenTelemetry
Cada trace é tagueado com `tenant.id`. No Jaeger, filtre por:
```
tenant.id = acme-corp
```

---

## 6. Rollback

### Desativar multi-tenancy (emergência)
```bash
# Variável de ambiente (sem restart se usando reload dinâmico)
export FeatureManagement__EnableMultiTenancy=false

# Ou alterar appsettings.Production.json e reiniciar
docker compose restart app
```

Após desativar, todas as queries ignoram `TenantId` — comportamento single-tenant restaurado sem perda de dados.

### Rollback de migration
```bash
# Reverter para migration anterior (especificar nome da migration anterior)
dotnet ef database update <migration-anterior> --project backend/src
```

---

## 7. Troubleshooting

### Tenant recebe 403 inesperado
```bash
# Verificar se tenant existe e está ativo
./scripts/manage-tenants.sh list

# Verificar feature flag
curl -s $API_URL/api/debug/tenant -H "X-Tenant-Id: acme-corp" \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

### Tenant vê dados de outro tenant
1. Verificar `TenantId` nos registros suspeitos via SQL
2. Verificar se `CachingBehavior` está injetando `ITenantService` (deve estar com a US-080)
3. Verificar se Dapper queries têm `WHERE TenantId = @TenantId`

### Cache cross-tenant
O `CachingBehavior` inclui `tenantId` em todas as cache keys desde US-078/080.
Para limpar cache Redis manualmente:
```bash
redis-cli -h localhost KEYS "issues-list:acme-corp:*" | xargs redis-cli DEL
```

---

## 8. Segurança

- Tenant não pode ser modificado via request — apenas `X-Tenant-Id` header ou claim JWT `tenant_id`
- Tenant `default` não pode ser desativado (proteção no endpoint DELETE)
- Todos os contextos EF Core têm `ApplyTenantFilter<T>()` configurado
- Queries Dapper filtram explicitamente por `TenantId = @TenantId`
- `CachingBehavior` sempre inclui `tenantId` na cache key
