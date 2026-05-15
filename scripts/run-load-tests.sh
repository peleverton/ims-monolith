#!/usr/bin/env bash
# =============================================================================
# run-load-tests.sh — US-082: Executa baseline de performance com k6
#
# Uso:
#   ./scripts/run-load-tests.sh [SCENARIO] [BASE_URL]
#
# Exemplos:
#   ./scripts/run-load-tests.sh                            # all cenários, localhost
#   ./scripts/run-load-tests.sh smoke                      # apenas smoke
#   ./scripts/run-load-tests.sh load https://staging.ims.io
#   ./scripts/run-load-tests.sh stress
#
# Saída:
#   docs/perf/results-YYYY-MM-DD.json  — dados brutos k6
#   docs/perf/summary-YYYY-MM-DD.md   — relatório Markdown gerado
# =============================================================================

set -euo pipefail

SCENARIO="${1:-all}"
BASE_URL="${2:-http://localhost:8080}"
DATE="$(date +%Y-%m-%d)"
MONTH="$(date +%Y-%m)"
RESULTS_DIR="docs/perf"
JSON_OUT="${RESULTS_DIR}/results-${DATE}.json"
SUMMARY_OUT="${RESULTS_DIR}/summary-${DATE}.md"

# ── Verificações ──────────────────────────────────────────────────────────────

if ! command -v k6 &>/dev/null; then
  echo "❌  k6 não encontrado. Instale com:"
  echo "    brew install k6         (macOS)"
  echo "    choco install k6        (Windows)"
  echo "    snap install k6         (Linux)"
  exit 1
fi

mkdir -p "${RESULTS_DIR}"

echo "════════════════════════════════════════════════════"
echo "  IMS Monolith — Load Test Baseline (US-082)"
echo "════════════════════════════════════════════════════"
echo "  Cenário  : ${SCENARIO}"
echo "  Target   : ${BASE_URL}"
echo "  Saída    : ${JSON_OUT}"
echo "════════════════════════════════════════════════════"
echo ""

# ── Verifica se a API está de pé ─────────────────────────────────────────────

echo "⏳ Verificando disponibilidade de ${BASE_URL}/health ..."
if ! curl -sf "${BASE_URL}/health" --max-time 10 > /dev/null 2>&1; then
  echo "⚠️  /health não respondeu. Continuando mesmo assim..."
fi
echo ""

# ── Executa k6 ───────────────────────────────────────────────────────────────

echo "🚀 Iniciando k6..."
k6 run \
  --env SCENARIO="${SCENARIO}" \
  --env BASE_URL="${BASE_URL}" \
  --out "json=${JSON_OUT}" \
  scripts/load-test-baseline.js
EXIT_CODE=$?

echo ""

# ── Gera relatório Markdown ───────────────────────────────────────────────────

echo "📝 Gerando relatório em ${SUMMARY_OUT}..."

K6_VERSION="$(k6 version 2>/dev/null | head -1)"

cat > "${SUMMARY_OUT}" << MARKDOWN
# Load Test Baseline — ${DATE}

> **US-082** | Gerado por \`run-load-tests.sh\`
> k6 version: ${K6_VERSION}

## Configuração

| Parâmetro | Valor |
|---|---|
| Cenário | \`${SCENARIO}\` |
| Ambiente | ${BASE_URL} |
| Data | ${DATE} |
| Resultados JSON | \`${JSON_OUT}\` |

## Endpoints Testados

| Endpoint | Tipo | Mix % |
|---|---|---|
| \`GET /api/issues?pageSize=20\` | Leitura (cache) | 40% |
| \`GET /api/inventory/products?pageSize=20\` | Leitura (Dapper) | 30% |
| \`GET /api/analytics/dashboard\` | Leitura (cache pesado) | 15% |
| \`POST /api/issues\` | Escrita (Outbox + RabbitMQ) | 15% |

## SLOs Definidos

| Métrica | SLO | Cenário |
|---|---|---|
| Read p50 | ≤ 80ms | load |
| Read p95 | ≤ 200ms | load |
| Read p95 | ≤ 500ms | stress |
| Read p95 | ≤ 1000ms | spike |
| Read p95 | ≤ 250ms | soak |
| Write p95 | ≤ 500ms | load |
| Write p99 | ≤ 1000ms | load |
| Error rate | < 1% | todos |

## Cenários Executados

### Smoke (1 VU × 30s)
Sanidade básica — todos os endpoints devem responder 200.

### Load (~100 RPS × 3min)
Carga normal de produção esperada. SLO principal: p95 ≤ 200ms.

### Stress (até 500 RPS × 5min)
Identifica ponto de saturação. SLO de degradação: p95 ≤ 500ms.

### Spike (1000 RPS × 30s)
Testa resiliência a picos repentinos. SLO de sobrevivência: p95 ≤ 1000ms.

### Soak (50 RPS × 10min)
Detecta memory leaks e connection pool exhaustion ao longo do tempo.

## Resultados

> Execute o seguinte para extrair métricas do JSON:
> \`\`\`bash
> cat ${JSON_OUT} | python3 scripts/parse-k6-results.py
> \`\`\`
>
> Ou visualize no Grafana com o dashboard **IMS SLI/SLO** (\`ims-slo.json\`).

## Status

$(if [ ${EXIT_CODE} -eq 0 ]; then
  echo "✅ **PASSOU** — todos os thresholds SLO atendidos"
else
  echo "❌ **FALHOU** — um ou mais thresholds SLO foram violados. Consulte os detalhes acima."
fi)

## Próximos Passos

- [ ] Comparar com baseline anterior em \`docs/perf/\`
- [ ] Atualizar dashboard Grafana com novos thresholds se baseline mudou
- [ ] Investigar bottlenecks se stress/spike falharam (DB pool, Redis, RabbitMQ)
- [ ] Agendar execução mensal via CI (ver \`.github/workflows/load-test.yml\`)
MARKDOWN

echo "✅ Relatório salvo em ${SUMMARY_OUT}"
echo ""

# ── Resultado final ───────────────────────────────────────────────────────────

if [ ${EXIT_CODE} -eq 0 ]; then
  echo "════════════════════════════════════════════════════"
  echo "  ✅  BASELINE PASSOU — todos os SLOs atendidos"
  echo "════════════════════════════════════════════════════"
else
  echo "════════════════════════════════════════════════════"
  echo "  ❌  BASELINE FALHOU — SLOs violados"
  echo "     Verifique os thresholds no output acima"
  echo "════════════════════════════════════════════════════"
fi

exit ${EXIT_CODE}
