# Monitoring and alert catalogue — Phase 23

## Implemented endpoints

- `/health/live`: liveness only, no dependency touch.
- `/health/ready`: deep readiness with database, process and memory checks plus uptime.
- `/metrics`: Prometheus text format, public for scraper access and excluded from the `/api/v1` global prefix.

## Metric names

- `erp_up`
- `erp_process_uptime_seconds`
- `erp_http_requests_total{method,route,status_class}`
- `erp_http_request_duration_ms_sum/count/bucket{method,route,le}`
- `erp_queue_depth{queue,status}` placeholder; tenant scoped detail remains `/api/v1/jobs/health`.
- `erp_einvoice_failures_total`
- `erp_migration_imported_rows_total`

## Dashboard panels

1. API request rate by route.
2. p95/p99 latency from histogram buckets.
3. 5xx rate and top failing routes.
4. Readiness status and DB connectivity.
5. Outbox backlog and dead jobs by tenant from `/jobs/health`.
6. E-invoice failure count and retry trend.
7. Migration imported rows throughput during cutover windows.

## Alerts

- API readiness degraded for 2 minutes.
- Any route 5xx rate > 1% for 5 minutes.
- Outbox pending backlog older than 10 minutes.
- Dead outbox jobs > 0.
- E-invoice failures increasing for 10 minutes.
- Migration throughput stalls for 5 minutes during an approved cutover.
