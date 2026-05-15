# Load Testing Baseline — IMS Monolith

> **US-082** | Morpheus | Sprint 13
> Última revisão: Maio 2026

---

## Objetivo

Estabelecer métricas de performance baseline para os endpoints críticos do IMS Monolith,
definir SLOs formais e identificar bottlenecks antes de habilitar multi-tenancy em produção.

---

## Endpoints Testados

| Endpoint | Tipo | Cache | Mix no teste |
|---|---|---|---|
| `GET /api/issues?pageSize=20` | Leitura paginada | Redis 2min | 40% |
| `GET /api/inventory/products?pageSize=20` | Leitura paginada | Redis 60s | 30% |
| `GET /api/analytics/dashboard` | Agregação | Redis 5min | 15% |
| `POST /api/issues` | Escrita + Outbox + RabbitMQ | — | 15% |

---

## SLOs Definidos (contrato de performance)

| Métrica | SLO | Cenário | Resultado 2026-05-08 |
|---|---|---|---|
| Read p50 | ≤ 80ms | load | ✅ 2.21ms |
| Read p95 | ≤ 200ms | load | ✅ 3.73ms |
| Read p95 | ≤ 500ms | stress | ✅ 3.58ms |
| Read p95 | ≤ 1000ms | spike | — (não executado) |
| Read p95 | ≤ 250ms | soak | — (não executado) |
| Read p99 | ≤ 2000ms | load | ✅ 4.85ms |
| Write p95 | ≤ 800ms | load | ✅ 15.95ms |
| Write p99 | ≤ 2000ms | load | ✅ 20.41ms |
| Cache Hit Rate | ≥ 95% | load | ✅ 99.79% |
| Error rate | < 1% | todos | ✅ 0.00% |

> **Nota sobre Write SLO:** O threshold mede apenas a latência HTTP do POST (persistência no DB + enqueue no Outbox).
> A publicação no RabbitMQ é **assíncrona** (Outbox pattern) e **não** está incluída no SLO de escrita.
> O Outbox polling está configurado para 1s no ambiente local.

---

## Cenários k6

### 1. Smoke (1 VU × 30s)
**Objetivo:** sanidade — todos os endpoints respondem 200 com payload válido.
**Passa se:** zero erros, latência qualquer.

### 2. Load (~100 RPS × 3min)
**Objetivo:** carga normal de produção esperada para uma instalação com 2–3 tenants ativos.
```
Ramp-up: 0 → 50 RPS em 30s
Sustain: 50 → 100 RPS em 90s
Ramp-down: 100 → 0 em 30s
```
**SLO de passagem:** p95 ≤ 200ms, error rate < 1%.

### 3. Stress (até 500 RPS × 5min)
**Objetivo:** identificar o ponto de saturação (quando p95 começa a degradar além do SLO).
```
Ramp-up: 100 → 200 RPS em 60s
Peak:    200 → 500 RPS em 120s
Ramp-down: 500 → 0 em 90s
```
**SLO de passagem:** p95 ≤ 500ms, error rate < 1%.
**Bottleneck esperado:** connection pool PostgreSQL (padrão: 10 conexões).

### 4. Spike (1000 RPS × 30s)
**Objetivo:** resiliência a picos repentinos (ex: campanha de marketing, alerta em massa).
Mix apenas leitura (GET) — write path seria bloqueado pelo DB antes do spike.
**SLO de passagem:** p95 ≤ 1000ms, error rate < 5% (degradação graceful).

### 5. Soak (50 RPS × 10min)
**Objetivo:** detectar memory leaks, connection pool exhaustion e degradação gradual.
**SLO de passagem:** p95 ≤ 250ms constante, sem aumento progressivo de latência.

---

## Como Executar

### Pré-requisitos
```bash
brew install k6      # macOS
snap install k6      # Linux
choco install k6     # Windows
```

### Comandos

```bash
# Cenário smoke (sanidade rápida):
k6 run --env SCENARIO=smoke scripts/load-test-baseline.js

# Cenário load (carga normal) com saída para relatório:
k6 run --env SCENARIO=load \
  --out json=docs/perf/results-$(date +%Y-%m-%d).json \
  scripts/load-test-baseline.js

# Todos os cenários (completo ~25min):
bash scripts/run-load-tests.sh all

# Contra ambiente de staging:
bash scripts/run-load-tests.sh load https://staging.ims.io

# Ver/parsear resultados:
cat docs/perf/results-*.json | python3 scripts/parse-k6-results.py

# Comparar dois baselines:
python3 scripts/parse-k6-results.py docs/perf/results-2026-05.json docs/perf/results-2026-06.json
```

---

## Bottlenecks Conhecidos e Mitigações

### PostgreSQL Connection Pool
- **Padrão:** 10 conexões (EF Core default)
- **Sintoma:** p99 de write explode acima de ~200 RPS concorrentes
- **Mitigação:** `MaxPoolSize=50` na connection string, ou migrar para PgBouncer
- **Configurar em:** `appsettings.Production.json` → `ConnectionStrings:DefaultConnection`

```json
"DefaultConnection": "Host=...;Database=ims;...;Maximum Pool Size=50;Connection Idle Lifetime=60"
```

### Redis Cache Miss Storm
- **Sintoma:** p95 de leitura sobe para >500ms nos primeiros 30s após restart
- **Causa:** cache frio — todas as requisições batem no PostgreSQL simultaneamente
- **Mitigação:** aquecimento de cache no startup (ver `Program.cs`) + Circuit Breaker

### RabbitMQ Outbox Pressure
- **Sintoma:** write p99 ultrapassa 2000ms sob stress
- **Causa:** Outbox insert + poll interval cria backpressure
- **Mitigação aplicada (local):** `Outbox__PollingIntervalSeconds=1` via docker-compose env var
- **Recomendação produção:** `Outbox:PollingIntervalSeconds=5` no appsettings.Production.json

### Analytics — Queries SQLite vs PostgreSQL
- **Problema detectado (2026-05-08):** `AnalyticsReadRepository` continha funções SQLite
  (`datetime()`, `strftime()`, `julianday()`, `IsActive=1`) incompatíveis com PostgreSQL
- **Corrigido:** convertido para `NOW()`, `TO_CHAR()`, `EXTRACT(EPOCH…)`, `IsActive=TRUE`
- **Lição:** testes de integração devem rodar contra PostgreSQL, não SQLite in-memory

---

## Infraestrutura de Monitoramento

### Dashboard Grafana
- **Dashboard:** `IMS — SLI/SLO Performance` (`ims-slo.json`)
- **Painéis:** Error Rate, Throughput (RPS), p95/p99 latência, top endpoints, SLO compliance
- **URL:** http://localhost:3001/d/ims-slo-dashboard

### Alertas Grafana (já configurados em `alert-rules.yml`)
- `ims-high-5xx-rate`: error rate > 5 req/min por 2min → **critical**
- `ims-high-p99-latency`: p99 > 2s por 5min → **warning**
- `ims-tenant-high-error-rate`: error rate > 10% por tenant por 5min → **critical**

---

## Agendamento Automático

O workflow `.github/workflows/load-test.yml` executa o cenário `load` automaticamente:
- **Mensal:** todo primeiro dia do mês às 03:00 UTC
- **Manual:** via Actions UI com seleção de cenário e URL

Resultados são salvos como artefatos por 90 dias.

---

## Histórico de Baselines

| Data | Cenário | p50 Read | p95 Read | p95 Write | Cache Hit | Error Rate | Status |
|---|---|---|---|---|---|---|---|
| 2026-05-08 | smoke (1 VU, 30s) | 3.46ms | 5.97ms | 28.72ms | 98.27% | 0.00% | ✅ PASS |
| 2026-05-08 | load (100 RPS, 2.5min) | 2.21ms | 3.73ms | 15.95ms | 99.79% | 0.00% | ✅ PASS |
| 2026-05-08 | stress (500 RPS, 4.5min) | 1.15ms | 3.58ms | 19.54ms | 97.02% | 0.00% | ⚠️ p99 excede (cold cache) |

> Atualizar esta tabela após cada execução de baseline.
> Comparar com `python3 scripts/parse-k6-results.py resultA.json resultB.json`.

---

## Próximos Passos (pós-baseline)

- [ ] Executar baseline completo (`all`) contra ambiente de staging com PostgreSQL real
- [ ] Preencher tabela de histórico com resultados reais
- [ ] Ajustar `MaxPoolSize` no PostgreSQL se stress falhar
- [ ] Configurar PgBouncer se pool não for suficiente
- [ ] Adicionar teste de write em `/api/inventory/products` (não coberto ainda)
- [ ] Avaliar cursor-based pagination (US-087) após baseline de offset pagination
