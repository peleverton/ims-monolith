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

| Métrica | SLO | Cenário | Justificativa |
|---|---|---|---|
| Read p50 | ≤ 80ms | load | Cache Redis deve responder em <10ms; soma com serialização/rede < 80ms |
| Read p95 | ≤ 200ms | load | SLO principal — 95% das requisições de leitura em carga normal |
| Read p95 | ≤ 500ms | stress | Degradação aceitável em 5× a carga normal |
| Read p95 | ≤ 1000ms | spike | Sobrevivência a picos repentinos (10× carga normal) |
| Read p95 | ≤ 250ms | soak | Sem degradação ao longo do tempo (sem memory leak) |
| Write p95 | ≤ 500ms | load | Inclui DB write + Outbox insert + ACK RabbitMQ |
| Write p99 | ≤ 1000ms | load | Cauda longa aceitável para writes com I/O |
| Error rate | < 1% | todos | HTTP 5xx / total requests |

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
- **Causa:** Outbox insert + poll interval de 15s cria backpressure
- **Mitigação:** reduzir `Outbox:PollingIntervalSeconds` para 5s em produção

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

| Data | Cenário | p95 Read | p95 Write | Error Rate | Status |
|---|---|---|---|---|---|
| 2026-05 | baseline inicial | — | — | — | 🔜 primeiro run |

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
