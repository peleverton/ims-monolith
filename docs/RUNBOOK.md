# 📕 IMS Monolith — Operational Runbook

> **Owner:** Platform / SRE
> **Última revisão:** Abril 2026 (Sprint 13 — US-081)
> **Escopo:** Procedimentos de operação, backup, restore e disaster recovery
> para o stack IMS (PostgreSQL, Redis, RabbitMQ, Meilisearch, .NET API,
> Next.js BFF, Hangfire).

Este runbook é a **fonte da verdade operacional**. Toda mudança em scripts
de backup, restore ou DR deve ser refletida aqui no mesmo PR.

---

## 1. SLOs / RTO / RPO

| Métrica | Alvo | Como medimos |
|---|---|---|
| **RTO** (Recovery Time Objective) | ≤ 4 horas | Tempo entre incidente declarado e API `/health` voltando 200 |
| **RPO** (Recovery Point Objective) | ≤ 24 horas | Idade máxima do último backup íntegro disponível |
| Disponibilidade da API | 99.5% / mês | Probe externo + Grafana SLO |
| p95 `/api/issues` | ≤ 200 ms a 500 RPS | k6 + Prometheus (US-082) |

> Backups são **diários**, com tier semanal e mensal. Para reduzir o RPO no
> futuro, considerar WAL archiving / `pg_basebackup` contínuo (fora do escopo
> da US-081).

---

## 2. Estratégia de Backup (US-081)

### 2.1 O que é feito

- `pg_dump --format=plain` da database principal, comprimido com `gzip --best`.
- Upload para bucket S3-compatível (`s3://${S3_BUCKET}/${S3_PREFIX}/<tier>/`).
- Tier automático em função da data:
  - `monthly` — dia 01 do mês (UTC).
  - `weekly` — domingo (UTC).
  - `daily` — qualquer outro dia.
- Retenção por tier: `RETENTION_DAILY=7`, `RETENTION_WEEKLY=4`, `RETENTION_MONTHLY=12`.
- Pruning idempotente baseado em `LastModified` do S3.

Arquivos:
- `scripts/backup-postgres.sh` — execução do dump + upload + pruning.
- `scripts/restore-postgres.sh` — restore com salvaguardas.
- `Dockerfile.backup` — imagem mínima Alpine + `postgresql16-client` + `aws-cli`.
- `docker-compose.yml` — serviço `backup` no profile `backup`.

### 2.2 Variáveis de ambiente

Ver `.env.example` (seção *Backup & Disaster Recovery*). O serviço
`backup` no `docker-compose.yml` mapeia variáveis com prefixo `BACKUP_*`
para os nomes esperados pelos scripts (`S3_BUCKET`, `AWS_*`, `RETENTION_*`).

Mínimo necessário (host / `.env`):

```
POSTGRES_DB=ims_db
POSTGRES_USER=ims
POSTGRES_PASSWORD=...
BACKUP_S3_BUCKET=ims-backups
BACKUP_S3_PREFIX=postgres
BACKUP_S3_ENDPOINT_URL=         # vazio = AWS S3
BACKUP_AWS_ACCESS_KEY_ID=...
BACKUP_AWS_SECRET_ACCESS_KEY=...
BACKUP_AWS_REGION=us-east-1
BACKUP_RETENTION_DAILY=7
BACKUP_RETENTION_WEEKLY=4
BACKUP_RETENTION_MONTHLY=12
```

Quando executando os scripts diretamente (sem compose), use os nomes nativos:
`S3_BUCKET`, `S3_PREFIX`, `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`,
`AWS_REGION`, `RETENTION_DAILY`, `RETENTION_WEEKLY`, `RETENTION_MONTHLY`.

### 2.3 Como agendar

#### Opção A — Docker Compose (dev/staging)

```bash
# Executar um backup ad-hoc:
docker compose --profile backup run --rm backup

# Em produção containerizada, dispare via cron do host:
0 3 * * *  cd /opt/ims && docker compose --profile backup run --rm backup >> /var/log/ims-backup.log 2>&1
```

#### Opção B — Kubernetes (produção)

`CronJob` diário às 03:00 UTC executando a imagem `Dockerfile.backup` com
as mesmas variáveis (via `Secret` + `ConfigMap`).

```yaml
apiVersion: batch/v1
kind: CronJob
metadata: { name: ims-postgres-backup }
spec:
  schedule: "0 3 * * *"
  successfulJobsHistoryLimit: 3
  failedJobsHistoryLimit: 3
  jobTemplate:
    spec:
      template:
        spec:
          restartPolicy: OnFailure
          containers:
            - name: backup
              image: ghcr.io/<org>/ims-backup:latest
              envFrom:
                - secretRef:  { name: ims-backup-secrets }
                - configMapRef: { name: ims-backup-config }
```

### 2.4 Verificações pós-backup

1. Job termina com exit code `0`.
2. Objeto presente em `s3://${S3_BUCKET}/${S3_PREFIX}/<tier>/`.
3. Tamanho > 1 KiB e `Content-Encoding`/metadata corretos.
4. Métrica/alerta: configurar alarme no S3 (idade do objeto mais recente em
   `daily/` > 26h → PagerDuty).

---

## 3. Restore — passo a passo

> ⚠️ **Toda restauração é destrutiva**: o script faz `DROP DATABASE` e recria.
> O script captura um *pre-restore dump* automaticamente em `/tmp/`, mas
> sempre confirme com o time antes de rodar em produção.

### 3.1 Restore em staging (ensaio recomendado mensal)

```bash
docker compose --profile backup run --rm \
  --entrypoint /opt/backup/restore-postgres.sh \
  backup \
    postgres/daily/ims-ims_db-daily-20260430T030000Z.sql.gz
```

Se o banco já tem dados, o script aborta. Para forçar:

```bash
FORCE=1 docker compose --profile backup run --rm \
  --entrypoint /opt/backup/restore-postgres.sh \
  -e FORCE=1 backup \
    postgres/daily/ims-ims_db-daily-20260430T030000Z.sql.gz
```

### 3.2 Restore em produção (incidente real)

1. **Declarar incidente** no canal `#ims-incidents`. Comunicar RTO esperado.
2. **Colocar API em modo de manutenção** (escalar `api` para 0 réplicas
   ou ativar feature flag `MaintenanceMode`).
3. **Identificar o backup mais recente íntegro** no S3:
   ```bash
   aws s3 ls s3://${S3_BUCKET}/${S3_PREFIX}/daily/ | sort | tail
   ```
4. **Baixar e validar** integridade local (smoke test em staging primeiro,
   se possível):
   ```bash
   aws s3 cp s3://${S3_BUCKET}/${S3_PREFIX}/daily/<arquivo>.sql.gz /tmp/
   gunzip -t /tmp/<arquivo>.sql.gz   # checa integridade do gzip
   ```
5. **Executar restore** com `FORCE=1`:
   ```bash
   FORCE=1 ./scripts/restore-postgres.sh /tmp/<arquivo>.sql.gz
   ```
6. **Aplicar migrations pendentes** (caso o backup seja anterior a um deploy):
   ```bash
   dotnet ef database update --project backend/src/ims-monolith.csproj
   ```
   ou simplesmente reiniciar a API (a migration runner roda em startup).
7. **Verificar `/health/ready`** — deve retornar 200.
8. **Executar smoke tests** — login admin, listar issues, criar 1 issue.
9. **Reabrir tráfego** — escalar API, desativar `MaintenanceMode`.
10. **Postmortem** — registrar timeline, RTO/RPO observados, próximos passos.

### 3.3 Restore parcial (table-level)

Para recuperar uma única tabela (ex.: `Issues` apagada por engano):

```bash
gunzip -c backup.sql.gz | grep -E '^(COPY public\."Issues"|INSERT INTO public\."Issues")' \
  > issues-only.sql
psql -U ims -d ims_db_recovery -f issues-only.sql
```

Recomenda-se **sempre** restaurar primeiro num banco temporário
(`ims_db_recovery`) e depois mover dados para produção via SQL controlado.

---

## 4. Testes de DR (Disaster Recovery drills)

| Frequência | Atividade | Responsável |
|---|---|---|
| Mensal | Restore de `daily` mais recente em staging | Plataforma |
| Trimestral | Drill completo: simular perda total da DB de prod | SRE + Backend lead |
| Semestral | Revisão deste runbook e ajuste de RTO/RPO | Lead Architect |

Cada drill deve produzir um relatório curto em `docs/dr-drills/YYYY-MM.md`
contendo: data, backup utilizado, RTO observado, RPO observado, problemas e
ações de follow-up.

---

## 5. Outros procedimentos operacionais

### 5.1 Rotação de credenciais

- `JWT_SECRET_KEY` — gerar novo (≥ 32 chars), atualizar em todos os pods,
  invalidar refresh tokens (`UPDATE "RefreshTokens" SET "RevokedAt" = NOW()`).
- `POSTGRES_PASSWORD` — `ALTER USER ims WITH PASSWORD '...';` + atualizar
  secrets + reiniciar API/backup/Hangfire.
- `AWS_*` — rotacionar IAM key, atualizar `Secret` `ims-backup-secrets`.

### 5.2 Limpeza de filas RabbitMQ

DLQs (`*.dlq`) devem ser inspecionadas semanalmente. Para drenar:

```bash
docker compose exec rabbitmq rabbitmqctl purge_queue <queue>.dlq
```

### 5.3 Reindex Meilisearch

Até US-088 (reindex automatizado), em caso de drift:

```bash
curl -X POST http://localhost:8080/api/admin/search/reindex \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

### 5.4 Hangfire — kill switch de jobs

Dashboard em `/hangfire` (Admin only). Para pausar todos os recurring jobs
sem deploy: setar feature flag `Hangfire:Enabled=false` e reiniciar.

---

## 6. Referências

- `docs/EVOLUTION_PLAN.md` — roadmap (US-081 nesta sprint, US-082/083 próximas).
- `docs/ADR-001-multi-tenancy.md` — impacta restore quando multi-tenancy estiver
  ativo (todo dump inclui todos os tenants).
- `scripts/backup-postgres.sh`, `scripts/restore-postgres.sh`.
- `docker-compose.yml` (profile `backup`).

---

*Mantido pelo time de Plataforma. Em caso de incidente, este documento é
o ponto de partida — não conte com memória.*
