# Incident basics runbook

Severity levels:

- SEV1: data isolation, ledger corruption, total outage, secret disclosure.
- SEV2: degraded posting/reporting/e-invoice path with workaround.
- SEV3: single tenant feature impairment.

Process:

1. Declare incident and assign commander.
2. Capture request ids, tenant ids, affected routes and deployment commit.
3. Freeze risky writes for affected tenants if financial integrity may be impacted.
4. Use `/metrics`, `/health/ready`, logs and audit trail to scope blast radius.
5. Apply rollback/run-forward only with commander approval.
6. Produce postmortem with timeline, root cause, corrective actions and tests added.
