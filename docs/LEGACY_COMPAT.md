# LEGACY_COMPAT — Desktop Compatibility Wire Contract

> Level C. Phase 16 bridge for the existing C# desktop after migration. Direction is
> always Desktop → API → Cloud DB; the cloud never reads the legacy database here.

## 1. Device lifecycle

Tenant admins create devices with `POST /compat/devices` using permission
`compat.manage`. The response includes the plaintext `apiKey` once. The server stores
only `SHA-256(COMPAT_KEY_PEPPER:apiKey)` in `compat_devices.api_key_hash`.

```json
{
  "name": "Main cashier desktop",
  "branchId": "018f...branch",
  "enumMaps": {
    "invoiceKind": { "2": "sale", "5": "sale_return" },
    "payMethod": { "1": "cash", "2": "card", "4": "split" }
  }
}
```

Revocation uses `PATCH /compat/devices/{id}/revoke`; revoked devices cannot authenticate
or use an existing compat token.

## 2. Device authentication

`POST /compat/auth/device` is public and accepts:

```json
{ "tenantId": "018f...tenant", "apiKey": "ck_plaintext_once" }
```

Response:

```json
{ "data": { "accessToken": "compat_...", "deviceId": "018f...", "branchId": "018f...", "expiresIn": 900 } }
```

Subsequent compat calls send `X-Tenant-Id` and `X-Compat-Token`. The token is scoped to
the authenticated device and branch.

## 3. Master pulls

Supported endpoints:

- `GET /compat/master/items?since=`
- `GET /compat/master/parties?since=`
- `GET /compat/master/accounts?since=`
- `GET /compat/master/tax-groups?since=`

The cursor is an opaque base64url JSON watermark `{updatedAt,id}`. Responses include
soft-delete tombstones:

```json
{
  "data": [{ "id": "018f...", "sku": "SKU-1", "nameAr": "صنف", "tombstone": false }],
  "meta": { "nextCursor": "eyJ1cGRhdGVkQXQiOi...", "limit": 200, "branchId": "018f..." }
}
```

A second pull with the returned cursor redelivers nothing unless a row was edited or a
new row was inserted after the watermark.

## 4. Sales invoice push

`POST /compat/docs/sales-invoice` accepts the legacy `Inv`/`Inv_Sub` shape. `GlobalID` is
the idempotency key when the HTTP `Idempotency-Key` header is absent.

```json
{
  "GlobalID": "INV-2026-1",
  "BranchID": "018f...branch",
  "StockID": "018f...warehouse",
  "CustID": "018f...party",
  "InvType": 2,
  "PayType": 1,
  "Currency": "SAR",
  "Lines": [{ "ItemID": "018f...item", "Qty": "1", "Price": "100.00", "VAT": "15" }]
}
```

Unknown enum integers are rejected with `COMPAT_ENUM_UNKNOWN` unless configured on the
device `enumMaps`. Accepted sales are mapped to the normal sales module, posted through
cloud sequence allocation, and stored with `legacy_source='compat'` and `legacy_id`.
Duplicates return the original cloud id/number.

### PayType settlement rules

`PayType` is an accounting instruction, not just legacy metadata. The default mapping is
`1 → cash`, `2 → card`, and `4 → split`; a device `enumMaps.payMethod` may also map a
legacy value to `bank` or `credit`.

- `cash` posts a paid invoice to the active **default safe** for the device's branch.
- `card` and `bank` post a paid invoice to the active **default bank** for that branch.
- The selected location must have a posting account. Missing/inactive/default-less setup
  is rejected with `COMPAT_SETTLEMENT_LOCATION_REQUIRED`; the gateway never silently
  changes it to customer credit.
- `credit` creates a normal receivable and therefore needs a valid `CustID` customer.
  A free-text `CashCustomer` cannot be posted to credit and is rejected with
  `SALES_CASH_CUSTOMER_SETTLEMENT_REQUIRED`.
- `split` is rejected with `COMPAT_SPLIT_SETTLEMENT_UNSUPPORTED`. The sales DTO has no
  tender amounts or per-tender locations, so importing it as a guessed credit/cash sale
  would corrupt settlement accounting. Add an explicit split-tender DTO before enabling it.

Sales payloads intentionally do **not** use `SafeID`: unlike vouchers, the legacy invoice
wire shape does not provide a reliable location ID. Configure a branch-local active default
safe/bank in Cloud instead. A device can omit `BranchID`, in which case its own branch is
used; it cannot override it and receives `COMPAT_DEVICE_BRANCH_MISMATCH` (403) if it tries.


## 5. Voucher push

`POST /compat/docs/voucher` accepts the legacy receipt/payment shape:

```json
{
  "GlobalID": "V-100",
  "BranchID": "018f...branch",
  "SafeID": "018f...cashLocation",
  "ReceiptType": 1,
  "PaymentType": 1,
  "Value": "100.00",
  "Date": "2026-09-07"
}
```

Vouchers are created through the treasury module and posted normally. Cheque and complex
allocation details remain explicit DTO extensions, not table mirroring.

## 6. Cursor and status

- `GET /compat/sync/cursor` returns the device cursor JSON.
- `POST /compat/sync/cursor` stores `{entity,cursor}`.
- `GET /compat/docs/status?legacyId=...` resolves legacy `GlobalID` to cloud id, number,
  entity, and status.

## 7. Security notes

No desktop route can elevate permissions. Admin device management still uses normal JWT
RBAC. Compat document pushes are audited through the normal API audit pipeline and carry
source metadata as a device-originated request.

## Phase 19 POS extension note

Desktop restaurant/POS payloads continue to use the Phase 16 compat gateway. POS-specific
legacy values such as `Tables`, `Table_Order`, `SettingOrderMethods`, additions, and
kitchen-category routing map to the Phase 19 `/pos/*` resources and then to normal cloud
sales invoices. The gateway must still reject unknown enum integers unless configured in
the per-device `enumMaps`.
