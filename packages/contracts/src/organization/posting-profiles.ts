import { z } from 'zod';

import { uuidSchema } from '../ids.js';

import { orgAuditDtoSchema, orgListQuerySchema, versionSchema } from './common.js';

/**
 * Branch posting profiles — API_CONTRACT §3, DATABASE_DESIGN §5.
 *
 * Replaces the legacy global `SettingGeneral.*Acc` columns *and* the per-branch
 * `Branches.*Acc` overrides (DOMAIN_MODEL §3) with one versioned JSON document per
 * `(branch, doc_type)`. Posting engines from PHASE_08 onwards ask
 * `resolvePostProfile(branchId, docType)` instead of reading account columns, so adding
 * a mapping never means another migration.
 */

/** `doc_type` codes — DOMAIN_MODEL §1 (frozen vocabulary). */
export const docTypeSchema = z.enum([
  'sales_invoice',
  'sales_return',
  'credit_note',
  'debit_note',
  'purchase_invoice',
  'purchase_return',
  'receipt_voucher',
  'payment_voucher',
  'journal_entry',
  'stock_adjustment',
  'stock_transfer',
  'cash_transfer',
  'payroll_run',
  'shift_close',
  'progress_bill',
  'rent_invoice',
  'installment_contract',
  'opening_balance',
]);

export type DocType = z.infer<typeof docTypeSchema>;

/** `*` is the catch-all profile — the direct heir of the legacy global mapping. */
export const POSTING_PROFILE_WILDCARD = '*';

export const postingProfileDocTypeSchema = z.union([docTypeSchema, z.literal(POSTING_PROFILE_WILDCARD)]);
export type PostingProfileDocType = z.infer<typeof postingProfileDocTypeSchema>;

/**
 * The account ids a profile may carry. Exported as data so PHASE_07 can iterate them
 * when it adds the "account exists and is postable" check (`ACCOUNT_NOT_POSTABLE`)
 * without duplicating the key list.
 */
export const POST_PROFILE_ACCOUNT_KEYS = [
  'salesAccountId',
  'salesReturnAccountId',
  'purchasesAccountId',
  'purchaseReturnAccountId',
  'discountGivenAccountId',
  'discountReceivedAccountId',
  'vatOutputAccountId',
  'vatInputAccountId',
  'inventoryAccountId',
  'cogsAccountId',
  'cashAccountId',
  'bankAccountId',
  'receivableAccountId',
  'payableAccountId',
] as const;

export type PostProfileAccountKey = (typeof POST_PROFILE_ACCOUNT_KEYS)[number];

const accountMapping = Object.fromEntries(
  POST_PROFILE_ACCOUNT_KEYS.map((key) => [key, uuidSchema.nullable().optional()]),
) as Record<PostProfileAccountKey, z.ZodOptional<z.ZodNullable<typeof uuidSchema>>>;

/**
 * `PostProfileV1` — the versioned JSONB payload (PHASE_05 §5.5). The literal `version`
 * discriminator is what lets PHASE_08+ introduce a V2 shape without a data migration:
 * the reader switches on it.
 */
export const postProfileV1Schema = z
  .object({
    version: z.literal(1),
    ...accountMapping,
    costCenterId: uuidSchema.nullable().optional(),
  })
  .strict()
  .refine(
    (value) => POST_PROFILE_ACCOUNT_KEYS.some((key) => value[key] !== undefined && value[key] !== null),
    { message: 'A posting profile must map at least one account' },
  );

export type PostProfileV1 = z.infer<typeof postProfileV1Schema>;

export const postingProfileDtoSchema = orgAuditDtoSchema.extend({
  id: uuidSchema,
  /** NULL = the tenant-wide default profile used when a branch has no override. */
  branchId: uuidSchema.nullable(),
  docType: z.string(),
  mapping: postProfileV1Schema,
});

export type PostingProfileDto = z.infer<typeof postingProfileDtoSchema>;

export const postingProfileUpsertSchema = z
  .object({
    branchId: uuidSchema.nullable().optional(),
    docType: postingProfileDocTypeSchema,
    mapping: postProfileV1Schema,
    version: versionSchema.optional(),
  })
  .strict();

export type PostingProfileUpsert = z.infer<typeof postingProfileUpsertSchema>;

/** `GET /branch-posting-profiles/resolve?branchId=…&docType=sales_invoice` */
export const postingProfileResolveQuerySchema = z
  .object({
    branchId: uuidSchema,
    docType: docTypeSchema,
  })
  .strict();

export type PostingProfileResolveQuery = z.infer<typeof postingProfileResolveQuerySchema>;

export const postingProfileResolutionDtoSchema = z.object({
  branchId: uuidSchema,
  docType: z.string(),
  mapping: postProfileV1Schema,
  /** Which rung of the fallback chain answered — never guess in the caller. */
  matchedBranchId: uuidSchema.nullable(),
  matchedDocType: z.string(),
});

export type PostingProfileResolutionDto = z.infer<typeof postingProfileResolutionDtoSchema>;

export const POSTING_PROFILE_FILTERS = ['branchId', 'docType'] as const;
export const POSTING_PROFILE_SORT_COLUMNS = ['docType', 'createdAt'] as const;

export const postingProfileListQuerySchema = orgListQuerySchema;
export type PostingProfileListQueryDto = z.infer<typeof postingProfileListQuerySchema>;
