# Phase 16 Implementation Report

## Current status

`COMPLETE` as of 2026-09-07.

## Delivered

- `compat_devices` table with hashed API keys, branch scope, enum maps, per-device
  cursors, revocation status, last-seen tracking, and tenant RLS.
- Reversible migration `0014_compat_gateway.sql` and Drizzle schema export.
- Device management API for admins: create/list/revoke.
- Public device authentication endpoint returning scoped compat tokens.
- Master pull endpoints for items, parties, accounts, and tax groups using
  `updated_at + id` cursor watermarks and tombstone flags.
- Legacy sales invoice and voucher push endpoints with GlobalID/idempotency support,
  normal sales/treasury module integration, cloud sequence preservation, and status lookup.
- `compat-mappers` for legacy `Inv`, `Inv_Sub`, and voucher DTO shapes, with clear
  `COMPAT_ENUM_UNKNOWN` failures for unknown enum integers unless device maps override.
- Cursor read/write endpoints and legacy document status lookup.
- `docs/LEGACY_COMPAT.md` wire contract for the desktop team.
- Unit tests covering mapping success and unknown enum rejection.

## Verification

`pnpm run verify` passes with type generation, TypeScript, lint, build, smoke checks,
workspace/API tests, migrator tests, compat mapper tests, and OpenAPI export.

## Notes

The gateway deliberately does not mirror legacy tables and does not modify desktop code.
It accepts only the Phase 16 master/doc endpoints and keeps all writes behind cloud domain
services.
