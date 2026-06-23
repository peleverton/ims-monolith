/**
 * k6 Load Test — Epic 1-4: New Modules Performance Baseline
 *
 * Endpoints testados:
 *   GET  /api/demand-forecasting/forecasts     — listagem de previsões
 *   GET  /api/demand-forecasting/summary       — resumo de risco
 *   POST /api/bin-packing/packaging-types      — criação de tipo de embalagem
 *   GET  /api/bin-packing/packaging-types      — listagem
 *   POST /api/bin-packing/suggest              — algoritmo FFD
 *   GET  /api/markdown/rules                   — listagem regras
 *   POST /api/markdown/rules                   — criação de regra
 *   GET  /api/anomaly-detection/alerts         — listagem alertas
 *   GET  /api/anomaly-detection/summary        — resumo anomalias
 *
 * Cenários:
 *   smoke  —  1 VU,  30s  → sanidade
 *   load   — 50 VU,  2min → carga normal
 *   stress — 200 VU, 3min → pico
 *
 * Uso:
 *   k6 run scripts/load-test-new-modules.js
 *   k6 run --env SCENARIO=load scripts/load-test-new-modules.js
 */

import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const BASE_URL   = __ENV.BASE_URL   || 'http://localhost:8080';
const ADMIN_USER = __ENV.ADMIN_USER || 'admin';
const ADMIN_PASS = __ENV.ADMIN_PASS || 'Admin@123!';
const SCENARIO   = __ENV.SCENARIO   || 'smoke';

const readLatency  = new Trend('new_modules_read_ms', true);
const writeLatency = new Trend('new_modules_write_ms', true);
const errorRate    = new Rate('new_modules_error_rate');

const scenarios = {
    smoke: {
        executor: 'constant-vus',
        vus: 1,
        duration: '30s',
    },
    load: {
        executor: 'ramping-vus',
        startVUs: 0,
        stages: [
            { duration: '30s', target: 50 },
            { duration: '1m',  target: 50 },
            { duration: '30s', target: 0 },
        ],
    },
    stress: {
        executor: 'ramping-vus',
        startVUs: 0,
        stages: [
            { duration: '30s', target: 100 },
            { duration: '1m',  target: 200 },
            { duration: '1m',  target: 200 },
            { duration: '30s', target: 0 },
        ],
    },
};

export const options = {
    scenarios: SCENARIO === 'all'
        ? scenarios
        : { [SCENARIO]: scenarios[SCENARIO] || scenarios.smoke },
    thresholds: {
        'new_modules_read_ms': ['p(95)<300', 'p(99)<800'],
        'new_modules_write_ms': ['p(95)<500', 'p(99)<1000'],
        'new_modules_error_rate': ['rate<0.01'],
    },
};

let authToken = '';

export function setup() {
    const loginRes = http.post(`${BASE_URL}/api/auth/login`, JSON.stringify({
        username: ADMIN_USER,
        password: ADMIN_PASS,
    }), { headers: { 'Content-Type': 'application/json' } });

    check(loginRes, { 'login succeeded': (r) => r.status === 200 });

    const body = JSON.parse(loginRes.body);
    return { token: body.accessToken || '' };
}

function authHeaders(data) {
    return {
        headers: {
            'Authorization': `Bearer ${data.token}`,
            'Content-Type': 'application/json',
        },
    };
}

export default function (data) {
    group('Demand Forecasting', () => {
        const r1 = http.get(`${BASE_URL}/api/demand-forecasting/forecasts`, authHeaders(data));
        check(r1, { 'forecasts 2xx': (r) => r.status >= 200 && r.status < 300 });
        readLatency.add(r1.timings.duration);
        errorRate.add(r1.status >= 400);

        const r2 = http.get(`${BASE_URL}/api/demand-forecasting/summary`, authHeaders(data));
        check(r2, { 'summary 2xx': (r) => r.status >= 200 && r.status < 300 });
        readLatency.add(r2.timings.duration);
        errorRate.add(r2.status >= 400);
    });

    group('Bin Packing', () => {
        const r1 = http.get(`${BASE_URL}/api/bin-packing/packaging-types`, authHeaders(data));
        check(r1, { 'packaging-types 2xx': (r) => r.status >= 200 && r.status < 300 });
        readLatency.add(r1.timings.duration);
        errorRate.add(r1.status >= 400);

        // Create a packaging type
        const createPayload = JSON.stringify({
            name: `LoadTest Box ${Date.now()}`,
            code: `LT-${Date.now()}`.substring(0, 20),
            maxLengthCm: 40,
            maxWidthCm: 30,
            maxHeightCm: 20,
            maxWeightKg: 10,
            costPerUnit: 3.50,
        });
        const r2 = http.post(`${BASE_URL}/api/bin-packing/packaging-types`, createPayload, authHeaders(data));
        check(r2, { 'create packaging type 201': (r) => r.status === 201 });
        writeLatency.add(r2.timings.duration);
        errorRate.add(r2.status >= 400);
    });

    group('Markdown Optimizer', () => {
        const r1 = http.get(`${BASE_URL}/api/markdown/rules`, authHeaders(data));
        check(r1, { 'rules 2xx': (r) => r.status >= 200 && r.status < 300 });
        readLatency.add(r1.timings.duration);
        errorRate.add(r1.status >= 400);

        // Create a rule
        const createPayload = JSON.stringify({
            name: `LoadTest Rule ${Date.now()}`,
            priority: 50,
            daysToExpiryThreshold: 7,
            minimumStockThreshold: 100,
            discountPercent: 10.0,
        });
        const r2 = http.post(`${BASE_URL}/api/markdown/rules`, createPayload, authHeaders(data));
        check(r2, { 'create rule 201': (r) => r.status === 201 });
        writeLatency.add(r2.timings.duration);
        errorRate.add(r2.status >= 400);
    });

    group('Anomaly Detection', () => {
        const r1 = http.get(`${BASE_URL}/api/anomaly-detection/alerts`, authHeaders(data));
        check(r1, { 'alerts 2xx': (r) => r.status >= 200 && r.status < 300 });
        readLatency.add(r1.timings.duration);
        errorRate.add(r1.status >= 400);

        const r2 = http.get(`${BASE_URL}/api/anomaly-detection/summary`, authHeaders(data));
        check(r2, { 'summary 2xx': (r) => r.status >= 200 && r.status < 300 });
        readLatency.add(r2.timings.duration);
        errorRate.add(r2.status >= 400);
    });

    sleep(0.5);
}
