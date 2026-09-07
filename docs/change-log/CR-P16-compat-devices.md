# CR-P16 — Compat devices table

Phase 16 adds one tenant-scoped table, `compat_devices`, to support the frozen desktop
compatibility direction: Desktop → API → Cloud DB. The table stores hashed device keys,
branch scope, per-device cursors, enum overrides, revocation status, rate-window fields,
and last-seen timestamps. It does not mirror legacy tables and is protected by FORCE RLS.
