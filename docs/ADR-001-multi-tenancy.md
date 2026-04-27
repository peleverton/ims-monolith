# ADR-001: Multi-tenancy Strategy — Row-Level Isolation via TenantId

**Status:** Accepted (PoC)  
**Sprint:** 12  
**US:** US-078  
**Date:** 2026-04-27  

---

## Contexto

O IMS precisa suportar múltiplas organizações (tenants) isolando seus dados.  
A decisão é: qual estratégia de isolamento adotar?

---

## Opções consideradas

| Estratégia | Vantagens | Desvantagens |
|---|---|---|
| **Database por tenant** | Isolamento total, backup granular | Custo operacional alto, migrations N vezes |
| **Schema por tenant (PostgreSQL)** | Bom isolamento, migrations controláveis | search_path complexo com EF Core |
| **Row-level (TenantId em cada tabela)** | Simples, 1 database, 1 schema | Requer global query filter em todos os DbContexts |

---

## Decisão

**Row-level isolation com `TenantId` column + EF Core Global Query Filters.**

### Justificativa

- Compatível com a arquitetura de modular monolith existente
- EF Core 8+ suporta global query filters compostos
- Menor custo operacional (1 banco, 1 schema)
- `TenantAwareDbContext` encapsula a lógica — módulos optam por herdar

---

## Implementação (PoC)

```
Shared/MultiTenancy/
├── ITenantEntity.cs          ← interface para entidades com TenantId
├── ITenantService.cs         ← abstração de leitura do tenant atual
├── TenantContext.cs          ← implementação scoped (populada por middleware)
├── TenantMiddleware.cs       ← lê X-Tenant-Id header ou claim "tid" do JWT
├── TenantAwareDbContext.cs   ← BaseDbContext + global query filter + auto-set
└── MultiTenancyExtensions.cs ← AddMultiTenancy() + UseMultiTenancy()
```

### Módulo PoC: Inventory
- `InventoryDbContext` pode herdar de `TenantAwareDbContext` no rollout completo
- `TenantProductSeed` demonstra o padrão sem alterar `Product` neste sprint

---

## Rollout completo (Sprint 13+)

1. Adicionar `TenantId` às entidades principais (`Product`, `Issue`, etc.)
2. Criar migration `AddTenantIdToAllEntities`
3. Migrar todos os `DbContext`s de `BaseDbContext` → `TenantAwareDbContext`
4. Habilitar feature flag `EnableMultiTenancy`
5. Seed de 2 tenants (`tenant-demo-1`, `tenant-demo-2`)
6. Testes de integração: verificar que queries não vazam dados entre tenants

---

## Consequências

- **Positivo:** Sem mudança de schema necessária para módulos que ainda não adotaram
- **Negativo:** Queries sem tenant retornam todos os dados (system-level) — requer cuidado
- **Neutro:** Feature flag `EnableMultiTenancy=false` em produção enquanto PoC é validado
