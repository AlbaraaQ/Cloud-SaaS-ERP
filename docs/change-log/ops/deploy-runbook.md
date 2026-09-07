# Deploy runbook — v1.0.0

1. Confirm release branch/commit and PR checks are green.
2. Freeze tenant write traffic or schedule maintenance window.
3. Export pre-deploy backups: database logical dump, WAL restore point and object-storage version marker.
4. Build images for API, worker, admin and customer frontends from the same commit.
5. Apply database migrations with migrator credentials only.
6. Deploy worker with `WORKER=1`; confirm outbox drain health.
7. Deploy API; verify `/health/live`, `/health/ready` and `/metrics`.
8. Deploy admin/customer frontends; verify CSP and origin settings.
9. Run smoke flows: login, sale invoice, purchase invoice, voucher, report export, e-invoice mock submission.
10. Enable tenant feature flags only after owner approval.
