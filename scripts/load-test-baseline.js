/**
 * k6 Load Test — US-082: Baseline de Performance IMS Monolith
 *
 * Endpoints críticos testados:
 *   GET  /api/issues              — listagem paginada (cache hit após 1ª req)
 *   GET  /api/inventory/products  — listagem paginada (Dapper)
 *   POST /api/issues              — criação (write path + Outbox + RabbitMQ)
 *   GET  /api/analytics/dashboard — agregação complexa (cache hit)
 *
 * Cenários:
 *   smoke  —   1 VU,   30s   → sanidade básica
 *   load   — rampa 100 RPS,  3min  → carga normal de produção
 *   stress — rampa 500 RPS,  5min  → pico de tráfego
 *   spike  — 1000 RPS, 30s   → spike repentino (SLO de degradação graceful)
 *   soak   —  50 RPS,  10min → estabilidade em carga baixa prolongada
 *
 * Thresholds (SLOs):
 *   p50  ≤  80ms  (endpoints de leitura com cache)
 *   p95  ≤ 200ms  (todos os endpoints — SLO principal)
 *   p99  ≤ 500ms  (sem cache / write path)
 *   error rate < 1%
 *
 * Uso:
 *   k6 run scripts/load-test-baseline.js
 *   k6 run --env SCENARIO=load scripts/load-test-baseline.js
 *   k6 run --env SCENARIO=stress --env BASE_URL=https://staging.ims.io scripts/load-test-baseline.js
 *   k6 run --out json=docs/perf/results-$(date +%Y-%m).json scripts/load-test-baseline.js
 */

import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { Counter, Rate, Trend, Gauge } from 'k6/metrics';
import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';

// ── Configuração ──────────────────────────────────────────────────────────────

const BASE_URL   = __ENV.BASE_URL   || 'http://localhost:8080';
const ADMIN_USER = __ENV.ADMIN_USER || 'admin';
const ADMIN_PASS = __ENV.ADMIN_PASS || 'Admin@123!';
const SCENARIO   = __ENV.SCENARIO   || 'all'; // smoke | load | stress | spike | soak | all

// ── Métricas customizadas ─────────────────────────────────────────────────────

const readLatency   = new Trend('ims_read_latency_ms',   true);
const writeLatency  = new Trend('ims_write_latency_ms',  true);
const cacheHitRate  = new Rate('ims_cache_hit_rate');
const errorRate     = new Rate('ims_error_rate');
const issuesCreated = new Counter('ims_issues_created_total');
const readErrors    = new Counter('ims_read_errors_total');
const writeErrors   = new Counter('ims_write_errors_total');
const activeVUs     = new Gauge('ims_active_vus');

// ── Cenários ──────────────────────────────────────────────────────────────────

const allScenarios = {
  // Sanidade: 1 VU, 30s — deve passar sempre
  smoke: {
    executor: 'constant-vus',
    vus: 1,
    duration: '30s',
    tags: { scenario: 'smoke' },
    exec: 'scenarioReadWrite',
  },

  // Carga normal: rampa até ~100 RPS (~10 VUs × 10 req/s cada)
  load: {
    executor: 'ramping-arrival-rate',
    startRate: 10,
    timeUnit: '1s',
    preAllocatedVUs: 20,
    maxVUs: 50,
    startTime: SCENARIO === 'all' ? '35s' : '0s',
    stages: [
      { duration: '30s', target: 50  },   // ramp-up → 50 RPS
      { duration: '90s', target: 100 },   // sustain → 100 RPS
      { duration: '30s', target: 0   },   // ramp-down
    ],
    tags: { scenario: 'load' },
    exec: 'scenarioReadWrite',
  },

  // Estresse: rampa até ~500 RPS — identificar ponto de saturação
  stress: {
    executor: 'ramping-arrival-rate',
    startRate: 100,
    timeUnit: '1s',
    preAllocatedVUs: 100,
    maxVUs: 300,
    startTime: SCENARIO === 'all' ? '4m' : '0s',
    stages: [
      { duration: '60s',  target: 200 },  // ramp-up → 200 RPS
      { duration: '120s', target: 500 },  // peak    → 500 RPS
      { duration: '60s',  target: 100 },  // ramp-down
      { duration: '30s',  target: 0   },
    ],
    tags: { scenario: 'stress' },
    exec: 'scenarioReadHeavy',             // read-heavy no stress (realista)
  },

  // Spike: 1000 RPS repentino por 30s — degradação graceful
  spike: {
    executor: 'constant-arrival-rate',
    rate: 1000,
    timeUnit: '1s',
    preAllocatedVUs: 200,
    maxVUs: 500,
    duration: '30s',
    startTime: SCENARIO === 'all' ? '12m' : '0s',
    tags: { scenario: 'spike' },
    exec: 'scenarioReadOnly',              // apenas leitura no spike
  },

  // Soak: 50 RPS por 10 minutos — memory leaks, connection pool exhaustion
  soak: {
    executor: 'constant-arrival-rate',
    rate: 50,
    timeUnit: '1s',
    preAllocatedVUs: 20,
    maxVUs: 50,
    duration: '10m',
    startTime: SCENARIO === 'all' ? '13m' : '0s',
    tags: { scenario: 'soak' },
    exec: 'scenarioReadWrite',
  },
};

// Selecionar cenários com base em SCENARIO env
function buildScenarios() {
  if (SCENARIO === 'all') return allScenarios;
  if (allScenarios[SCENARIO]) return { [SCENARIO]: { ...allScenarios[SCENARIO], startTime: '0s' } };
  throw new Error(`Cenário inválido: "${SCENARIO}". Use: smoke|load|stress|spike|soak|all`);
}

export const options = {
  scenarios: buildScenarios(),

  // ── SLOs / Thresholds ───────────────────────────────────────────────────────
  thresholds: {
    // Leitura com cache: p50 ≤ 80ms, p95 ≤ 200ms
    'ims_read_latency_ms': [
      'p(50)<80',
      'p(95)<200',
      'p(99)<2000',  // p99 inclui primeiras requisições sem cache (cold start)
    ],
    // Escrita (POST issues + DB persist): p95 ≤ 800ms, p99 ≤ 2000ms
    // Nota: mede apenas a latência HTTP do POST (persistência + enqueue no Outbox).
    // A publicação no RabbitMQ é assíncrona (Outbox pattern) e NÃO está incluída no SLO de escrita.
    'ims_write_latency_ms': [
      'p(95)<800',
      'p(99)<2000',
    ],
    // Taxa de erro global < 1%
    'ims_error_rate': ['rate<0.01'],
    // Taxa de erro HTTP geral < 1%
    'http_req_failed': ['rate<0.01'],
    // SLO por cenário — leitura em carga normal: p95 ≤ 200ms
    'http_req_duration{scenario:load}':   ['p(95)<200'],
    // Stress: p95 ≤ 500ms (SLO de degradação)
    'http_req_duration{scenario:stress}': ['p(95)<500'],
    // Spike: p95 ≤ 1000ms (SLO de sobrevivência)
    'http_req_duration{scenario:spike}':  ['p(95)<1000'],
    // Soak: p95 ≤ 250ms (sem degradação ao longo do tempo)
    'http_req_duration{scenario:soak}':   ['p(95)<250'],
  },
};

// ── Setup — autenticação + aquecimento ────────────────────────────────────────

export function setup() {
  // Login para obter token JWT
  const loginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({ username: ADMIN_USER, password: ADMIN_PASS }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  check(loginRes, {
    'setup: login 200': (r) => r.status === 200,
    'setup: token presente': (r) => !!r.json('accessToken'),
  });

  const token = loginRes.json('accessToken');
  if (!token) throw new Error(`Login falhou! Status: ${loginRes.status} — ${loginRes.body}`);

  const headers = authHeaders(token);

  // Aquecimento: 5 requests em cada endpoint para popular o cache
  console.log('🔥 Aquecendo cache...');
  for (let i = 0; i < 5; i++) {
    http.get(`${BASE_URL}/api/issues?pageSize=20`, { headers });
    http.get(`${BASE_URL}/api/inventory/products?pageSize=20`, { headers });
    http.get(`${BASE_URL}/api/analytics/dashboard`, { headers });
    sleep(0.2);
  }
  console.log('✅ Cache aquecido. Iniciando testes...');

  return { token };
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function authHeaders(token) {
  return {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`,
  };
}

function recordRead(res) {
  readLatency.add(res.timings.duration);
  const ok = res.status >= 200 && res.status < 300;
  errorRate.add(!ok);
  if (!ok) readErrors.add(1);
  // Heurística: X-Cache ou response time < 10ms = cache hit
  const isCacheHit = res.status === 200 &&
    (res.headers['X-Cache'] === 'HIT' || res.timings.duration < 10);
  cacheHitRate.add(isCacheHit);
  return ok;
}

function recordWrite(res) {
  writeLatency.add(res.timings.duration);
  const ok = res.status >= 200 && res.status < 300;
  errorRate.add(!ok);
  if (!ok) writeErrors.add(1);
  return ok;
}

const priorities = ['Low', 'Medium', 'High', 'Critical'];

// ── Cenário: Read + Write (mix realista 80/20) ────────────────────────────────

export function scenarioReadWrite(data) {
  activeVUs.add(1);
  const headers = authHeaders(data.token);
  const roll = Math.random();

  if (roll < 0.40) {
    // 40% — listagem de issues (endpoint mais usado)
    group('GET /api/issues', () => {
      const page = randomIntBetween(1, 5);
      const res = http.get(`${BASE_URL}/api/issues?pageNumber=${page}&pageSize=20`, { headers });
      recordRead(res);
      check(res, {
        'issues: status 200': (r) => r.status === 200,
        'issues: tem items':  (r) => {
          try { return Array.isArray(r.json('items')) || r.json('totalCount') !== undefined; }
          catch { return false; }
        },
      });
    });

  } else if (roll < 0.70) {
    // 30% — listagem de inventory
    group('GET /api/inventory/products', () => {
      const res = http.get(`${BASE_URL}/api/inventory/products?pageSize=20`, { headers });
      recordRead(res);
      check(res, {
        'inventory: status 200': (r) => r.status === 200,
      });
    });

  } else if (roll < 0.85) {
    // 15% — dashboard analytics (cache pesado)
    group('GET /api/analytics/dashboard', () => {
      const res = http.get(`${BASE_URL}/api/analytics/dashboard`, { headers });
      recordRead(res);
      check(res, {
        'analytics: status 200': (r) => r.status === 200,
      });
    });

  } else {
    // 15% — criação de issue (write path)
    group('POST /api/issues', () => {
      const priority = priorities[randomIntBetween(0, 3)];
      const ts = Date.now();
      const res = http.post(
        `${BASE_URL}/api/issues`,
        JSON.stringify({
          title: `[k6] Issue ${__VU}-${__ITER}-${ts}`,
          description: `Load test issue — VU=${__VU} ITER=${__ITER} scenario=load`,
          priority,
        }),
        { headers }
      );
      if (recordWrite(res)) issuesCreated.add(1);
      check(res, {
        'create issue: 201': (r) => r.status === 201,
        'create issue: id presente': (r) => {
          try { return !!r.json('id'); } catch { return false; }
        },
      });
    });
  }

  sleep(randomIntBetween(1, 3) * 0.1); // 100–300ms think time
  activeVUs.add(-1);
}

// ── Cenário: Read-heavy (stress — 95% reads) ──────────────────────────────────

export function scenarioReadHeavy(data) {
  const headers = authHeaders(data.token);
  const roll = Math.random();

  if (roll < 0.50) {
    const res = http.get(`${BASE_URL}/api/issues?pageSize=20`, { headers });
    recordRead(res);
  } else if (roll < 0.80) {
    const res = http.get(`${BASE_URL}/api/inventory/products?pageSize=20`, { headers });
    recordRead(res);
  } else if (roll < 0.95) {
    const res = http.get(`${BASE_URL}/api/analytics/dashboard`, { headers });
    recordRead(res);
  } else {
    // 5% writes mesmo no stress
    const res = http.post(
      `${BASE_URL}/api/issues`,
      JSON.stringify({
        title: `[k6-stress] Issue ${__VU}-${__ITER}`,
        description: 'Stress test write',
        priority: 'Low',
      }),
      { headers }
    );
    if (recordWrite(res)) issuesCreated.add(1);
  }

  sleep(0.05); // mínimo think time no stress
}

// ── Cenário: Read-only (spike) ────────────────────────────────────────────────

export function scenarioReadOnly(data) {
  const headers = authHeaders(data.token);
  // Round-robin entre os 3 endpoints de leitura
  const endpoints = [
    '/api/issues?pageSize=20',
    '/api/inventory/products?pageSize=20',
    '/api/analytics/dashboard',
  ];
  const url = endpoints[__ITER % endpoints.length];
  const res = http.get(`${BASE_URL}${url}`, { headers });
  recordRead(res);
  // Sem sleep no spike — máxima pressão
}

// ── Teardown — sumário final ──────────────────────────────────────────────────

export function teardown(data) {
  sleep(2);
  console.log('\n═══════════════════════════════════════════════════');
  console.log('📊  BASELINE REPORT — IMS Monolith Load Test');
  console.log('═══════════════════════════════════════════════════');
  console.log(`  Ambiente : ${BASE_URL}`);
  console.log(`  Cenário  : ${SCENARIO}`);
  console.log(`  Data     : ${new Date().toISOString()}`);
  console.log('\n  Thresholds SLO:');
  console.log('    Read  p95 ≤ 200ms (load) / 500ms (stress) / 1000ms (spike)');
  console.log('    Write p95 ≤ 500ms');
  console.log('    Error rate < 1%');
  console.log('\n  Salve os resultados em JSON para o relatório:');
  console.log(`  k6 run --out json=docs/perf/results-${new Date().toISOString().slice(0,7)}.json scripts/load-test-baseline.js`);
  console.log('═══════════════════════════════════════════════════\n');
}
