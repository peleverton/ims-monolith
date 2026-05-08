#!/usr/bin/env python3
"""
parse-k6-results.py — US-082: Extrai e formata métricas do JSON de saída do k6.

Uso:
    cat docs/perf/results-YYYY-MM-DD.json | python3 scripts/parse-k6-results.py
    python3 scripts/parse-k6-results.py docs/perf/results-YYYY-MM-DD.json
    python3 scripts/parse-k6-results.py docs/perf/results-A.json docs/perf/results-B.json  # diff
"""

import json
import sys
import statistics
from collections import defaultdict
from datetime import datetime

SLOS = {
    "ims_read_latency_ms":  {"p50": 80,   "p95": 200, "p99": 500},
    "ims_write_latency_ms": {"p95": 500,  "p99": 1000},
    "ims_error_rate":       {"rate": 0.01},
    "http_req_failed":      {"rate": 0.01},
}

CYAN   = "\033[96m"
GREEN  = "\033[92m"
RED    = "\033[91m"
YELLOW = "\033[93m"
BOLD   = "\033[1m"
RESET  = "\033[0m"


def load_results(path: str) -> dict:
    """Lê arquivo JSON de saída do k6 (formato NDJSON)."""
    metrics = defaultdict(list)
    meta = {}

    with open(path) as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            try:
                obj = json.loads(line)
            except json.JSONDecodeError:
                continue

            if obj.get("type") == "Metric":
                name = obj["data"]["name"]
                meta[name] = obj["data"]

            elif obj.get("type") == "Point":
                name = obj["metric"]
                val  = obj["data"].get("value")
                if val is not None:
                    metrics[name].append(float(val))

    return {"metrics": dict(metrics), "meta": meta}


def percentile(data: list, p: float) -> float:
    if not data:
        return 0.0
    sorted_data = sorted(data)
    k = (len(sorted_data) - 1) * p / 100
    lo, hi = int(k), min(int(k) + 1, len(sorted_data) - 1)
    return sorted_data[lo] + (sorted_data[hi] - sorted_data[lo]) * (k - lo)


def format_ms(val: float) -> str:
    return f"{val:.1f}ms"


def check_slo(metric: str, stat: str, value: float, threshold: float) -> bool:
    if stat == "rate":
        return value <= threshold
    return value <= threshold


def print_metric(name: str, data: list, slo: dict | None = None):
    if not data:
        print(f"  {YELLOW}{name}{RESET}: sem dados")
        return

    p50  = percentile(data, 50)
    p95  = percentile(data, 95)
    p99  = percentile(data, 99)
    mean = statistics.mean(data)
    rate = sum(1 for v in data if v > 0) / len(data) if data else 0

    print(f"\n  {BOLD}{CYAN}{name}{RESET} ({len(data):,} amostras)")

    is_rate_metric = "rate" in (slo or {})

    if is_rate_metric:
        threshold = (slo or {}).get("rate", 1.0)
        ok = rate <= threshold
        status = f"{GREEN}✅{RESET}" if ok else f"{RED}❌{RESET}"
        print(f"    rate  : {rate:.4f} ({rate*100:.2f}%)  SLO ≤ {threshold*100:.1f}%  {status}")
    else:
        print(f"    mean  : {format_ms(mean)}")

        p50_thr = (slo or {}).get("p50")
        p95_thr = (slo or {}).get("p95")
        p99_thr = (slo or {}).get("p99")

        p50_ok  = p50 <= p50_thr if p50_thr else True
        p95_ok  = p95 <= p95_thr if p95_thr else True
        p99_ok  = p99 <= p99_thr if p99_thr else True

        def slo_str(thr): return f"SLO ≤ {thr}ms" if thr else ""
        def status_icon(ok): return f"{GREEN}✅{RESET}" if ok else f"{RED}❌{RESET}"

        print(f"    p50   : {format_ms(p50):>10}  {slo_str(p50_thr):15}  {status_icon(p50_ok) if p50_thr else ''}")
        print(f"    p95   : {format_ms(p95):>10}  {slo_str(p95_thr):15}  {status_icon(p95_ok) if p95_thr else ''}")
        print(f"    p99   : {format_ms(p99):>10}  {slo_str(p99_thr):15}  {status_icon(p99_ok) if p99_thr else ''}")
        print(f"    max   : {format_ms(max(data))}")


def print_report(path: str, results: dict):
    metrics = results["metrics"]

    print(f"\n{BOLD}{'═'*60}{RESET}")
    print(f"{BOLD}  IMS Load Test — Relatório de Baseline{RESET}")
    print(f"{'═'*60}")
    print(f"  Arquivo : {path}")
    print(f"  Data    : {datetime.now().strftime('%Y-%m-%d %H:%M')}")

    print(f"\n{BOLD}── Latência de Leitura ──────────────────────────────────{RESET}")
    print_metric("ims_read_latency_ms",  metrics.get("ims_read_latency_ms",  []), SLOS["ims_read_latency_ms"])

    print(f"\n{BOLD}── Latência de Escrita ──────────────────────────────────{RESET}")
    print_metric("ims_write_latency_ms", metrics.get("ims_write_latency_ms", []), SLOS["ims_write_latency_ms"])

    print(f"\n{BOLD}── HTTP Geral ───────────────────────────────────────────{RESET}")
    print_metric("http_req_duration",    metrics.get("http_req_duration",    []))

    print(f"\n{BOLD}── Taxa de Erros ────────────────────────────────────────{RESET}")
    print_metric("ims_error_rate",       metrics.get("ims_error_rate",       []), SLOS["ims_error_rate"])
    print_metric("http_req_failed",      metrics.get("http_req_failed",      []), SLOS["http_req_failed"])

    print(f"\n{BOLD}── Contadores ───────────────────────────────────────────{RESET}")
    for key in ["ims_issues_created_total", "ims_read_errors_total", "ims_write_errors_total"]:
        vals = metrics.get(key, [])
        total = int(sum(vals)) if vals else 0
        print(f"  {key}: {total:,}")

    print(f"\n{BOLD}── VUs ──────────────────────────────────────────────────{RESET}")
    vus = metrics.get("vus", [])
    if vus:
        print(f"  max VUs : {int(max(vus)):,}")
        print(f"  mean VUs: {statistics.mean(vus):.1f}")

    print(f"\n{'═'*60}\n")


def compare_reports(path_a: str, results_a: dict, path_b: str, results_b: dict):
    """Compara dois relatórios e mostra diffs de SLO."""
    ma = results_a["metrics"]
    mb = results_b["metrics"]

    print(f"\n{BOLD}{'═'*60}{RESET}")
    print(f"{BOLD}  Comparação de Baselines{RESET}")
    print(f"{'═'*60}")
    print(f"  A: {path_a}")
    print(f"  B: {path_b} (mais recente)")

    key_metrics = ["ims_read_latency_ms", "ims_write_latency_ms", "http_req_duration"]
    for metric in key_metrics:
        da = ma.get(metric, [])
        db = mb.get(metric, [])
        if not da or not db:
            continue
        p95_a = percentile(da, 95)
        p95_b = percentile(db, 95)
        diff  = p95_b - p95_a
        pct   = (diff / p95_a * 100) if p95_a else 0
        icon  = f"{GREEN}↓{RESET}" if diff < 0 else (f"{RED}↑{RESET}" if diff > 5 else "→")
        print(f"\n  {metric} p95:")
        print(f"    A  : {format_ms(p95_a)}")
        print(f"    B  : {format_ms(p95_b)}  {icon} {diff:+.1f}ms ({pct:+.1f}%)")

    print(f"\n{'═'*60}\n")


def main():
    args = sys.argv[1:]

    # Leitura de stdin se não houver argumentos
    if not args:
        import tempfile, os
        tmp = tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False)
        tmp.write(sys.stdin.read())
        tmp.close()
        args = [tmp.name]

    if len(args) == 1:
        path = args[0]
        results = load_results(path)
        print_report(path, results)

    elif len(args) == 2:
        path_a, path_b = args
        results_a = load_results(path_a)
        results_b = load_results(path_b)
        print_report(path_b, results_b)
        compare_reports(path_a, results_a, path_b, results_b)

    else:
        print("Uso: python3 parse-k6-results.py [arquivo_a.json] [arquivo_b.json]")
        sys.exit(1)


if __name__ == "__main__":
    main()
