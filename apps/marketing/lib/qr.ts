/**
 * ZATCA QR (TLV) decoding, done in the browser.
 *
 * The QR printed on a Saudi e-invoice is a base64 TLV blob: tag byte, length byte, value.
 * Tags 1-5 are the human-readable fields; 6-8 carry the hash, signature and public key, whose
 * presence tells you the invoice was cryptographically stamped. Decoding it locally means the
 * portal can answer "what does this QR actually say" without an endpoint, an account, or any
 * chance of leaking another customer's data.
 */
export type ZatcaQr = {
  sellerName: string;
  vatNumber: string;
  timestamp: string;
  total: string;
  vatTotal: string;
  signed: boolean;
  tags: Array<{ tag: number; length: number }>;
};

function fromBase64(value: string): Uint8Array {
  const binary = globalThis.atob(value.trim().replace(/\s+/g, ''));
  const bytes = new Uint8Array(binary.length);
  for (let index = 0; index < binary.length; index += 1) bytes[index] = binary.charCodeAt(index);
  return bytes;
}

export function decodeZatcaQr(base64: string): ZatcaQr {
  const bytes = fromBase64(base64);
  if (bytes.length < 4) throw new Error('القيمة المدخلة ليست رمز QR صالحاً');
  const decoder = new TextDecoder('utf-8');
  const values = new Map<number, Uint8Array>();
  const tags: Array<{ tag: number; length: number }> = [];

  let cursor = 0;
  while (cursor + 2 <= bytes.length) {
    const tag = bytes[cursor]!;
    const length = bytes[cursor + 1]!;
    const start = cursor + 2;
    if (start + length > bytes.length) throw new Error('رمز QR غير مكتمل أو تالف');
    values.set(tag, bytes.slice(start, start + length));
    tags.push({ tag, length });
    cursor = start + length;
  }
  if (tags.length === 0) throw new Error('لم يتم العثور على حقول داخل رمز QR');

  const text = (tag: number) => (values.has(tag) ? decoder.decode(values.get(tag)!) : '');
  const result: ZatcaQr = {
    sellerName: text(1),
    vatNumber: text(2),
    timestamp: text(3),
    total: text(4),
    vatTotal: text(5),
    signed: values.has(6) && values.has(7),
    tags,
  };
  if (!result.sellerName && !result.vatNumber) throw new Error('رمز QR لا يتبع صيغة هيئة الزكاة والضريبة والجمارك');
  return result;
}
