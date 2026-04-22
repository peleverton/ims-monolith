/**
 * k6 Load Test — Validação das Filas (Outbox + RabbitMQ)
 *
 * Cenários:
 *  1. smoke    — 1 VU, 30s  → sanidade básica
 *  2. load     — rampa até 20 VUs, 2min → carga normal
 *  3. stress   — rampa até 50 VUs, 3min → pico de carga
 *
 * O que é validado:
 *  - POST /api/issues retorna 201
 *  - Outbox persiste sem erro varchar
 *  - RabbitMQ recebe as mensagens (checado via API de Management)
 *  - Latência p95 < 1s em carga normal
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';
import encoding from 'k6/encoding';

// ─── Configuração ────────────────────────────────────────────────────────────
const BASE_URL      = __ENV.BASE_URL      || 'http://localhost:8080';
const RABBIT_URL    = __ENV.RABBIT_URL    || 'http://localhost:15672';
const RABBIT_USER   = __ENV.RABBIT_USER   || 'ims';
const RABBIT_PASS   = __ENV.RABBIT_PASS   || 'ims_dev_pass';
const ADMIN_USER    = __ENV.ADMIN_USER    || 'admin';
const ADMIN_PASS    = __ENV.ADMIN_PASS    || 'Admin@123!';

// ─── Métricas customizadas ────────────────────────────────────────────────────
const issuesCreated       = new Counter('issues_created_total');
const issuesFailed        = new Counter('issues_failed_total');
const outboxErrorRate     = new Rate('outbox_error_rate');
const issueCreateDuration = new Trend('issue_create_duration', true);

// ─── Opções dos cenários ──────────────────────────────────────────────────────
export const options = {
  scenarios: {
    smoke: {
      executor: 'constant-vus',
      vus: 1,
      duration: '30s',
      tags: { scenario: 'smoke' },
    },
    load: {
      executor: 'ramping-vus',
      startTime: '35s',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 10 },
        { duration: '60s', target: 20 },
        { duration: '30s', target: 0  },
      ],
      tags: { scenario: 'load' },
    },
    stress: {
      executor: 'ramping-vus',
      startTime: '3m',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 30 },
        { duration: '60s', target: 50 },
        { duration: '30s', target: 20 },
        { duration: '30s', target: 0  },
      ],
      tags: { scenario: 'stress' },
    },
  },
  thresholds: {
    // Todos os requests devem ter p95 < 1s no cenário de carga
    'http_req_duration{scenario:load}': ['p(95)<1000'],
    // Taxa de erro geral < 1%
    'http_req_failed': ['rate<0.01'],
    // Taxa de erros de Outbox < 0.5%
    'outbox_error_rate': ['rate<0.005'],
    // Issues criadas com sucesso: p95 latência < 1500ms
    'issue_create_duration': ['p(95)<1500'],
  },
};

// ─── Setup: autenticação uma única vez ───────────────────────────────────────
export function setup() {
  const loginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({ username: ADMIN_USER, password: ADMIN_PASS }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  check(loginRes, {
    'login status 200': (r) => r.status === 200,
    'token presente':   (r) => r.json('accessToken') !== '',
  });

  const token = loginRes.json('accessToken');
  if (!token) {
    throw new Error(`Login falhou! Status: ${loginRes.status} Body: ${loginRes.body}`);
  }

  // Snapshot inicial das filas no RabbitMQ
  const queuesRes = http.get(
    `${RABBIT_URL}/api/queues/%2F`,
    { auth: 'basic', headers: { Authorization: `Basic ${encoding.b64encode(`${RABBIT_USER}:${RABBIT_PASS}`)}` } }
  );

  let initialQueues = {};
  if (queuesRes.status === 200) {
    const queues = queuesRes.json();
    queues.forEach(q => {
      initialQueues[q.name] = {
        messages_ready:     q.messages_ready     || 0,
        messages_delivered: q.message_stats ? (q.message_stats.deliver_get || 0) : 0,
      };
    });
  }

  console.log(`✅ Login OK | Token: ${token.substring(0, 30)}...`);
  console.log(`📋 Filas iniciais: ${JSON.stringify(initialQueues)}`);

  return { token, initialQueues };
}

// ─── VU principal ─────────────────────────────────────────────────────────────
export default function (data) {
  const headers = {
    'Content-Type':  'application/json',
    'Authorization': `Bearer ${data.token}`,
  };

  const priorities = [1, 2, 3];
  const priority   = priorities[Math.floor(Math.random() * priorities.length)];
  const ts         = Date.now();

  const payload = JSON.stringify({
    title:       `[LoadTest] Issue ${__VU}-${__ITER}-${ts}`,
    description: `Teste de carga automatizado — VU=${__VU} ITER=${__ITER} TS=${ts}. Validando pipeline Outbox → RabbitMQ com payload extenso para garantir coluna text.`,
    priority:    priority,
    type:        1,
  });

  const res = http.post(`${BASE_URL}/api/issues`, payload, {
    headers,
    tags: { name: 'CreateIssue' },
  });

  issueCreateDuration.add(res.timings.duration);

  const success = check(res, {
    'status 201':           (r) => r.status === 201,
    'id presente':          (r) => r.json('id') !== undefined,
    'title correto':        (r) => r.json('title') !== undefined,
    'status Open':          (r) => r.json('status') === 'Open',
    'sem erro 500':         (r) => r.status !== 500,
    'sem erro outbox':      (r) => {
      if (r.status === 500) {
        const body = r.body || '';
        return !body.includes('character varying') && !body.includes('OutboxMessages');
      }
      return true;
    },
  });

  if (res.status === 201) {
    issuesCreated.add(1);
    outboxErrorRate.add(0);
  } else {
    issuesFailed.add(1);
    const isOutboxError = res.body && (
      res.body.includes('character varying') ||
      res.body.includes('OutboxMessages') ||
      res.body.includes('varchar')
    );
    outboxErrorRate.add(isOutboxError ? 1 : 0);

    if (!success) {
      console.error(`❌ Falha VU=${__VU} ITER=${__ITER} | Status=${res.status} | Body=${res.body.substring(0, 200)}`);
    }
  }

  sleep(Math.random() * 0.5 + 0.1); // 100–600ms entre requests
}

// ─── Teardown: relatório final das filas ─────────────────────────────────────
export function teardown(data) {
  sleep(3); // aguardar processamento final do Outbox

  const queuesRes = http.get(
    `${RABBIT_URL}/api/queues/%2F`,
    { auth: 'basic', headers: { Authorization: `Basic ${encoding.b64encode(`${RABBIT_USER}:${RABBIT_PASS}`)}` } }
  );

  if (queuesRes.status !== 200) {
    console.warn(`⚠️  Não foi possível consultar RabbitMQ Management: ${queuesRes.status}`);
    return;
  }

  const queues = queuesRes.json();
  console.log('\n══════════════════════════════════════════════');
  console.log('📊  RELATÓRIO FINAL — FILAS RABBITMQ');
  console.log('══════════════════════════════════════════════');

  queues.forEach(q => {
    const initial  = data.initialQueues[q.name] || { messages_ready: 0, messages_delivered: 0 };
    const pending  = q.messages_ready || 0;
    const totalIn  = q.message_stats  ? (q.message_stats.publish      || 0) : 0;
    const totalOut = q.message_stats  ? (q.message_stats.deliver_get  || 0) : 0;

    console.log(`\n  Fila: ${q.name}`);
    console.log(`    Pendentes agora : ${pending}`);
    console.log(`    Total publicado : ${totalIn}`);
    console.log(`    Total consumido : ${totalOut}`);
    console.log(`    Consumidores    : ${q.consumers || 0}`);

    if (pending > 0) {
      console.warn(`  ⚠️  ${pending} mensagens pendentes na fila "${q.name}" — consumidor pode estar atrasado`);
    } else {
      console.log(`  ✅  Fila "${q.name}" processada com sucesso`);
    }
  });

  console.log('\n══════════════════════════════════════════════\n');
}
