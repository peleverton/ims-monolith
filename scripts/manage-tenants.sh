#!/usr/bin/env bash
# =============================================================================
# US-080: Tenant management CLI — criação, ativação e desativação de tenants
# =============================================================================
# Uso:
#   ./scripts/manage-tenants.sh create  <id> <name> [plan] [email]
#   ./scripts/manage-tenants.sh list
#   ./scripts/manage-tenants.sh deactivate <id>
#   ./scripts/manage-tenants.sh activate   <id>
#
# Variáveis de ambiente:
#   API_URL      — base URL da API (default: http://localhost:8080)
#   ADMIN_TOKEN  — Bearer token de um usuário Admin (obrigatório)
# =============================================================================

set -euo pipefail

API_URL="${API_URL:-http://localhost:8080}"
ADMIN_TOKEN="${ADMIN_TOKEN:-}"

if [[ -z "$ADMIN_TOKEN" ]]; then
  echo "❌  ADMIN_TOKEN não definido. Execute:"
  echo "    export ADMIN_TOKEN=\$(curl -s -X POST $API_URL/api/auth/login \\"
  echo "      -H 'Content-Type: application/json' \\"
  echo "      -d '{\"username\":\"admin\",\"password\":\"Admin@123!\"}' | jq -r .accessToken)"
  exit 1
fi

AUTH_HEADER="Authorization: Bearer $ADMIN_TOKEN"
CT="Content-Type: application/json"

cmd="${1:-help}"

case "$cmd" in

  # ── list ──────────────────────────────────────────────────────────────────
  list)
    echo "📋  Tenants registrados em $API_URL:"
    curl -s -H "$AUTH_HEADER" "$API_URL/api/tenants" | \
      python3 -c "
import json,sys
tenants = json.load(sys.stdin)
if not tenants:
    print('  (nenhum tenant encontrado)')
else:
    print(f'  {\"ID\":<30} {\"Nome\":<30} {\"Plano\":<12} {\"Ativo\":<6}')
    print(f'  {\"-\"*30} {\"-\"*30} {\"-\"*12} {\"-\"*6}')
    for t in tenants:
        ativo = '✅' if t.get('isActive') else '❌'
        print(f'  {t[\"id\"]:<30} {t[\"name\"]:<30} {t.get(\"plan\",\"\"):<12} {ativo}')
"
    ;;

  # ── create ────────────────────────────────────────────────────────────────
  create)
    TENANT_ID="${2:-}"
    TENANT_NAME="${3:-}"
    PLAN="${4:-free}"
    EMAIL="${5:-}"

    if [[ -z "$TENANT_ID" || -z "$TENANT_NAME" ]]; then
      echo "Uso: $0 create <id> <name> [plan] [email]"
      echo "Exemplo: $0 create acme-corp 'Acme Corporation' starter ops@acme.com"
      exit 1
    fi

    PAYLOAD=$(python3 -c "
import json
print(json.dumps({
    'id': '$TENANT_ID',
    'name': '$TENANT_NAME',
    'plan': '$PLAN',
    'contactEmail': '${EMAIL}' if '${EMAIL}' else None,
    'notes': None
}))
")

    HTTP_CODE=$(curl -s -o /tmp/tenant_create_resp.json -w "%{http_code}" \
      -X POST "$API_URL/api/tenants" \
      -H "$AUTH_HEADER" -H "$CT" \
      -d "$PAYLOAD")

    if [[ "$HTTP_CODE" == "201" ]]; then
      echo "✅  Tenant '$TENANT_ID' criado com sucesso."
      cat /tmp/tenant_create_resp.json | python3 -m json.tool
    else
      echo "❌  Erro HTTP $HTTP_CODE:"
      cat /tmp/tenant_create_resp.json
      exit 1
    fi
    ;;

  # ── deactivate ────────────────────────────────────────────────────────────
  deactivate)
    TENANT_ID="${2:-}"
    if [[ -z "$TENANT_ID" ]]; then
      echo "Uso: $0 deactivate <id>"
      exit 1
    fi

    HTTP_CODE=$(curl -s -o /tmp/tenant_deact_resp.json -w "%{http_code}" \
      -X DELETE "$API_URL/api/tenants/$TENANT_ID" \
      -H "$AUTH_HEADER")

    if [[ "$HTTP_CODE" == "204" ]]; then
      echo "✅  Tenant '$TENANT_ID' desativado (soft-delete). Dados preservados."
    else
      echo "❌  Erro HTTP $HTTP_CODE:"
      cat /tmp/tenant_deact_resp.json
      exit 1
    fi
    ;;

  # ── activate ──────────────────────────────────────────────────────────────
  activate)
    TENANT_ID="${2:-}"
    if [[ -z "$TENANT_ID" ]]; then
      echo "Uso: $0 activate <id>"
      exit 1
    fi

    HTTP_CODE=$(curl -s -o /tmp/tenant_act_resp.json -w "%{http_code}" \
      -X PUT "$API_URL/api/tenants/$TENANT_ID" \
      -H "$AUTH_HEADER" -H "$CT" \
      -d '{"isActive": true}')

    if [[ "$HTTP_CODE" == "200" ]]; then
      echo "✅  Tenant '$TENANT_ID' reativado."
    else
      echo "❌  Erro HTTP $HTTP_CODE:"
      cat /tmp/tenant_act_resp.json
      exit 1
    fi
    ;;

  # ── help ──────────────────────────────────────────────────────────────────
  *)
    echo "Uso: $0 <comando> [opções]"
    echo ""
    echo "Comandos:"
    echo "  list                              Lista todos os tenants"
    echo "  create <id> <name> [plan] [email] Cria um novo tenant"
    echo "  deactivate <id>                   Desativa um tenant (soft-delete, dados preservados)"
    echo "  activate   <id>                   Reativa um tenant desativado"
    echo ""
    echo "Variáveis de ambiente:"
    echo "  API_URL      Base URL da API (default: http://localhost:8080)"
    echo "  ADMIN_TOKEN  Bearer token de Admin (obrigatório)"
    echo ""
    echo "Exemplo rápido:"
    echo "  export ADMIN_TOKEN=\$(curl -s -X POST \$API_URL/api/auth/login \\"
    echo "    -d '{\"username\":\"admin\",\"password\":\"Admin@123!\"}' | jq -r .accessToken)"
    echo "  ./scripts/manage-tenants.sh create acme-corp 'Acme Corp' starter cto@acme.com"
    ;;
esac
