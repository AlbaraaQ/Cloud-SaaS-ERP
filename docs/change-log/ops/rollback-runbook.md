# Rollback runbook — v1.0.0

1. Stop new traffic at the load balancer.
2. Disable workers to stop asynchronous side effects.
3. If no post-migration writes occurred, redeploy previous images and run down migrations in reverse order.
4. If post-migration writes occurred, restore the PITR backup to the pre-deploy timestamp and replay only approved deltas.
5. Re-enable API/frontends on the previous version.
6. Run tenant reconciliation reports and obtain rollback sign-off.
7. Document incident cause, customer impact and corrective actions.
