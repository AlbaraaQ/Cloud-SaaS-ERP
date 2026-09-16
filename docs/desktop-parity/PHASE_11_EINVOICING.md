# المرحلة 11 — الفاتورة الإلكترونية: زاتكا · ETA · التكاملات

**الحالة: الجزءان الأول والثاني مُنجزان** — (1) «⚙️ إعدادات الربط الضريبي - زاتكا ZATCA» من
`Form_WPF/frmZatcaSetting.xaml` (472 سطراً) + `.xaml.cs` (1160) و`Class/ZatcaService.cs`
(546): جدول `einvoice_settings` (ترحيل `0062`) وأعمدة الشهادة على
`einvoice_credentials`، وتسعة مسارات حقيقيّة، و**18** اختباراً و**64** نقطة تحقّق حيّة،
وشاشة `/settings/zatca` (§4). (2) «🧾 الفواتير المرفوعة على موقع الضرائب» من
`Form_WPF/frmSentEinvoice.xaml` (358) + `.xaml.cs` (304) و`Class/ZatcaService.cs`
`IntegrateInvoice` (L78-L410) و`Class/InvoiceOper.cs` `SendZatca` (L2209): الترحيل والتخليص
ووثيقة الهيئة المُصادَقة ورمزها، وأعمدة على `einvoice_submissions` (ترحيل `0063`)، وثلاثة
مسارات جديدة، و**16** اختباراً و**53** نقطة تحقّق حيّة، وشاشة `/settings/zatca/sent`
(§5). الأجزاء مبيّنة في §3، ومعايير القبول في §7، وما أُجِّل عن قصد في §8.

الغرض: نقل **التأهيل والإرسال** كما يفعل الديسكتوب — لا اختراع مسارٍ جديد. محرّك الفاتورة
الإلكترونية موجود في السحابة منذ الإصدار الأول (`modules/einvoicing` بوثيقة UBL 2.1
وتجزئةٍ متسلسلة و QR وعدّاد ICV وسجلّ إرسالات)، لكنه كان **يستقبل الشهادة جاهزة**: لا
توليد، ولا تأهيل، ولا اختبار ربط. هذه المرحلة تملأ الفراغ خطوةً بخطوة.

كل قسم في هذه الوثيقة يُسمّي ملفه من `Desktop_ERP` نصّاً، وتسمياته مأخوذة من تلك الملفات
بالعربية. أي تسمية مخترَعة تُبرَّر صراحةً في ملخّص الجزء.

## 1. المصادر (ملفات `Desktop_ERP`)

| المجال | الملفات |
|---|---|
| نافذة الإعدادات والتأهيل | `Form_WPF/frmZatcaSetting.xaml` (472) + `.xaml.cs` (1160) — النافذة كلها |
| إرسال الفاتورة | `Class/ZatcaService.cs` (546) — `IntegrateInvoice` L78 · `CallReportingAPI` L371/L377 · `LoadZatcaCredential` L430 · `GetEncodedInvoiceQRCode` L460 · `CallComplianceInvoiceAPI` L900 · `Class/InvoiceOper.cs` `SendZatca` L2209 |
| نماذج الشهادة | `Class/ZatcaCredential.cs` (21: `CSR` · `PrivateKey` · `CSID` · `Secret`) · `Class/ZatcaResponse.cs` (11) · `Class/CustZatcaEndDate.cs` |
| الجداول الثلاثة | `SettingZatca` (ID=1) · `CSRProperties` (Id=1) · `ZatcaCredential` (ID=1) — تُكتب من `frmZatcaSetting.xaml.cs` L200 وL230 وL262 |
| مصر | `Form_WPF/frmEtaSetting.xaml` (348) + `.xaml.cs` (292) · `Class/EtaService.cs` (379) · `Class/EtaReciptService.cs` (268) · `EtaResultData.cs` |
| حالة المزامنة | `Form_WPF/frmInvsSyncStatusZatca.xaml` (559) + `.xaml.cs` (1165) · `Form_WPF/frmSentEinvoice.xaml` |
| التكاملات | `Class/Geidea.cs` (57) · `Class/NeoleapService.cs` (165) · `Class/WhatsAppSender.cs` (267) |

> **تنبيه:** كل نداءات البوابة في الديسكتوب تمرّ بمكتبة `AuditorAPI` المترجَمة
> (`CSRGenerator` · `ApiRequestLogic` · `ZatcaIntegrationSDK`)، وهي ليست في هذا
> المستودع ولا يمكن قراءتها. لذلك تُنفَّذ البوابة هنا نصّاً على المواصفة المنشورة لهيئة
> الزكاة (`/compliance` · `/production/csids` · `/compliance/invoices` ·
> `/invoices/reporting|clearance/single`)، ويُسمّى ذلك في كل موضع.

## 2. من جداول الديسكتوب إلى السحابة

| الديسكتوب | السحابة | ملاحظة |
|---|---|---|
| `SettingZatca` (ID=1) | `einvoice_settings` بمفتاح `(tenant_id, authority)` | الصفّ الوحيد يصير صفّاً لكل مؤسسة؛ الأعمدة نفسها |
| `isProduction` · `IsSimulation` | `environment` (`compliance`\|`production`) · `simulation` | الراديوان والصندوق في النافذة |
| `IsActive` · `SyncManual` | `active` · `sync_manual` | ✅ تمكين · Sync manual |
| `StartDate` · `EndDate` | `start_date` · `end_date` (= البداية + سنة، L248-L249) | تُحسب دائماً، ولا تُقبل نهايةٌ أخرى |
| `filePath` | — | مجلدٌ على جهاز الكاشير: لا مجلد على الخادم |
| `CSRProperties` (Id=1) | تسعة أعمدة في `einvoice_settings` | تُوقَّع في الشهادة، فلا تُترك في `jsonb` |
| `ZatcaCredential.RequestID/CSID/Secret` | `request_id` · `csid_enc` · `secret_enc` | مشفّرة؛ تُقرأ مقنّعة |
| `ZatcaCredential.P_RequestID/P_CSID/P_Secret` | `p_request_id` · `p_csid_enc` · `p_secret_enc` | جديد |
| — | `csr_generated_at` · `compliance_csid_at` · `production_csid_at` · `compliance_checked_at` · `renewed_at` · `last_compliance_check` | النافذة كانت تُظهر الحاضر فقط؛ القائمة تحتاج تواريخه |
| `ZatcaResponse` (رسالة الهيئة) | `einvoice_submissions.authority_status` · `response.errorMessages` · `response.warningMessages` · `cleared_invoice` (ترحيل `0063`) | `insert ZatcaResponse` بعد كل إرسال؛ و`ClearedInvoice` وثيقة الهيئة لا وثيقتنا |
| `Inv.EncodedInvoice` · `QRCode` · `InvoiceHash` · `UUID` · `ZatcaSent` | `sales_invoices.zatca_encoded_invoice` · `zatca_qr` · `zatca_hash` · `zatca_uuid` · `zatca_status` | تُكتب بعد كل إرسال ناجح (L388-L391) |
| `PIH` · `InvoiceNo` (ICV) | `einvoice_chain.last_hash` · `counter` | صفٌّ واحد لكل `(مستأجر · سلطة · بيئة)` |

## 3. الأجزاء

| الجزء | النوافذ | الحالة |
|---|---|---|
| 1 | ⚙️ إعدادات الربط الضريبي — `frmZatcaSetting` (الإعدادات · خصائص CSR · التأهيل الأربع · إيقاف الربط · التجديد) | ✅ مُنجز (§4) |
| 2 | 🧾 الإرسال والتوقيع والسلسلة — `frmSentEinvoice` + `ZatcaService.IntegrateInvoice` (ترحيل/تخليص، QR بثمانية وسوم، إعادة المحاولة) | ✅ مُنجز (§5) |
| 3 | 📊 حالة المزامنة — `frmInvsSyncStatusZatca` (شبكة الفواتير وحالاتها ومرشّحاتها) | ⬜ |
| 4 | 🇪🇬 مصر — `frmEtaSetting` + `EtaService` + `EtaReciptService` | ⬜ |
| 5 | 💳 التكاملات — `Geidea` · `NeoleapService` · `WhatsAppSender` | ⬜ |

## 4. الجزء الأول — ⚙️ إعدادات الربط الضريبي - زاتكا ZATCA

### 4.1 النافذة وما تكتبه

`Form_WPF/frmZatcaSetting.xaml` ثلاث بطاقات بالعنوان «⚙️ إعدادات الربط الضريبي - زاتكا
ZATCA»:

- **البطاقة الأولى:** 📅 التاريخ · 🔑 OTP · ✅ تمكين Activate · 🧪 Simulation تجريبي
  (مخفي) · Sync manual (مخفي) · 🔴 Production ربط فعلي · 🔵 Compliance تجريبي
  (راديوان، L213-L227).
- **البطاقة الثانية «📋 خصائص شهادة CSR»:** زرّ 🔄 تعبئة تلقائي، ثم تسعة حقول:
  🖥️ Serial Number (سريال الجهاز، مقروء فقط) · 🌍 Country Name (الدولة) ·
  🏢 Common Name (اسم المنشأة) · 🔢 Organization Identifier (الرقم الضريبي) ·
  🏭 Organization Name (اسم المنشأة) · 📄 Invoice Type (نوع الفواتير) ·
  🏗️ Industry (النشاط التجاري) · 🏬 Organization Unit (اسم الفرع) ·
  📍 Address (العنوان المختصر). وثلاثة صناديق مخفيّة: `txtCSR` · `txtPrivateKey` ·
  `txtPath`.
- **البطاقة الثالثة:** 💾 حفظ الإعدادات — Save Settings · 🧪 اختبار الربط — Test
  Compliance · 🔐 حفظ مفتاح التشفير — Get PCSID · 🔄 Renews CSID — تجديد الشهادة بعد
  5 سنوات · ⚡ توليد — Generate (مخفي) · ☁️ Load Data · ⏸ إيقاف الربط.

السريال يُولَّد آلياً بصيغة الديسكتوب `1-Auditor|2-{الإصدار}|3-{guid}`
(`GenerateSerialNo` L151-L157)؛ وصار في السحابة `1-CloudERP|2-{الإصدار}|3-{uuid}`.

💾 حفظ الإعدادات في الديسكتوب يفعل أربعة أشياء متتالية (L193-L196):
`Generate()` → `SaveCSR()` → `ComplianceCSID()` → كتابة الجداول الثلاثة. وفي السحابة
صارت أربعة مسارات، حتى تفشل كل خطوة على حدة ويظهر سببها:

| الخطوة | الزرّ | المسار |
|---|---|---|
| 1 | ⚡ توليد — Generate | `POST /einvoice/csr/generate` |
| 2 | 🔵 compliance CSID (بـ 🔑 OTP) | `POST /einvoice/onboarding/compliance-csid` |
| 3 | 🔐 حفظ مفتاح التشفير — Get PCSID | `POST /einvoice/onboarding/production-csid` |
| 4 | 🧪 اختبار الربط — Test Compliance | `POST /einvoice/onboarding/compliance-check` |

ثم 🔄 Renews CSID = `POST /einvoice/onboarding/renew`، و⏸ إيقاف الربط / ▶ تشغيل =
`POST /einvoice/link/toggle`، و🔄 تعبئة تلقائي =
`POST /einvoice/settings/fill-from-company`، والقراءة `GET /einvoice/settings` والحفظ
`PUT /einvoice/settings`.

### 4.2 طلب التوقيع (PKCS#10) حقيقي

`AuditorAPI` مكتبةٌ مغلقة، فكُتب الطلب على المواصفة: مفتاح على المنحنى `secp256k1`، وموضوع
`C · OU · O · CN`، و`subjectAltName` بخمس خصائص يقرأ منها زاتكا التسجيل —
`SN` = 🖥️ السريال · `UID` = 🔢 الرقم الضريبي · `title` = 📄 نوع الفواتير ·
`registeredAddress` = 📍 العنوان · `businessCategory` = 🏗️ النشاط — مع الامتداد
`1.3.6.1.4.1.311.20.2 = ZATCA-Code-Signing`، وتوقيع ECDSA-SHA256 فوق
`CertificationRequestInfo`. الدليل في الاختبارات: `openssl req -verify` يقول
`Certificate request self-signature verify OK`.

### 4.3 البوابة: ثلاثة أوضاع

| 🧪 Simulation | 🔵 Compliance | 🔴 Production | المضيف |
|---|---|---|---|
| ✓ | – | – | لا شيء — تُجاب محلياً |
| – | ✓ | – | `…/e-invoicing/developer-portal` |
| – | – | ✓ | `…/e-invoicing/core` |

المحاكاة ليست «نجاحاً دائماً»: وثيقةٌ يرفضها الفحص المحلي تُرجع `FAILED` مع أسبابها.
وحين لا تُبلغ البوابة يُقال ذلك صراحةً: `502 EINVOICE_GATEWAY_UNREACHABLE` بعبارة
«تعذّر الاتصال ببوابة هيئة الزكاة والضريبة…»، لا خطأ 500 مبهم.

### 4.4 الوثائق الست لاختبار الربط

`frmZatcaSetting.xaml.cs` L397-L437 يبني ست وثائق بالبيانات الثابتة نفسها (UUID
`8d487816-70b8-4ade-a618-9d620b73814a`، PIH = تجزئة البداية، صنف «قلم رصاص» ×2 بسعر
2.00، ضريبة 15% فـ 4.00 + 0.60 = 4.60، والعميل «Acme Widget's LTD 2») ويقف عند أول
فشل فيعرض `"{checkName} compliance check failed."`. والسحابة تُجري الست وتعرضها:

| # | الاسم في الديسكتوب | النوع/الصنف | الحالة |
|---|---|---|---|
| 1 | Standard Invoice | 388/0100000 | CLEARED |
| 2 | Standard Debit Note | 383/0100000 | CLEARED |
| 3 | Standard Credit Note | 381/0100000 | CLEARED |
| 4 | Simplified Invoice | 388/0200000 | REPORTED |
| 5 | Simplified Debit Note | 383/0200000 | REPORTED |
| 6 | Simplified Credit Note | 381/0200000 | REPORTED |

الفحص المحلي يغلق الحساب (`4.00 + 0.60 = 4.60`) ويطلب الرقم الضريبي للمنشأة على
الفاتورة الضريبية — فمؤسسةٌ لم تُكمل بطاقتها ترى الفشل وأسبابه، لا «تم بنجاح».

### 4.5 ما خالفنا فيه الديسكتوب

1. **☁️ Load Data حُذف.** كان يقرأ `ProductionCsrResponse.txt` و
   `ComplianceCsrResponse.txt` من مجلدٍ على جهاز الكاشير؛ لا مجلد على الخادم.
2. **💾 حفظ الإعدادات لم يعد يولّد شهادةً ويطلب CSID في الخفاء.** فشلُ الطلب يظهر
   بطلبه، لا تحت «تم الحفظ».
3. **نتيجة 🧪 اختبار الربط تبقى على الشاشة** في `last_compliance_check` بدل صندوق
   رسالةٍ واحد.
4. **🔄 Renews CSID يُجدّد فعلاً**، لا صندوق رسالة «سيتم تجديد الشهادة» كما في
   `BtnRenewsCSID_Click` (L1137).

### 4.6 الملفات والاختبارات

| النوع | الملف |
|---|---|
| ترحيل | `packages/database/migrations/0062_einvoice_zatca_settings.sql` + `down/0062…down.sql` |
| مخطّط | `packages/database/src/schema/einvoicing.ts` |
| بوابة | `apps/api/src/modules/einvoicing/zatca/gateway.ts` |
| طلب التوقيع | `apps/api/src/modules/einvoicing/zatca/csr.ts` |
| الوثائق الست | `apps/api/src/modules/einvoicing/zatca/compliance-check.ts` |
| الخدمة | `apps/api/src/modules/einvoicing/zatca-onboarding.service.ts` |
| المسارات | `apps/api/src/modules/einvoicing/einvoicing.controller.ts` |
| اختبارات | `apps/api/test/einvoicing-zatca-onboarding.spec.ts` (18) |
| تحقّق حيّ | `scripts/verify-einvoice-zatca.mjs` (64 نقطة) |
| الشاشة | `apps/staff/app/settings/zatca/page.tsx` + `apps/staff/lib/einvoice.ts` |

## 5. الجزء الثاني — 🧾 الإرسال والتوقيع والسلسلة

### 5.1 النافذة، وما فيها وما ليس منها

`Form_WPF/frmSentEinvoice.xaml` (358) + `.xaml.cs` (304): العنوان «🧾 الفواتير المرفوعة على
موقع الضرائب»، و«📋 قائمة الفواتير المرفوعة»، و«📌 انقر على صف لتحديد UUID والرابط العام»،
و«رقم الصفحة:» و«حجم الصفحة:» و🔍 عرض، و📄 بيانات الفاتورة تعرض `UUID:` و`Public URL:`
(L293-L298)، و🖨️ طباعة، و🚫 إلغاء الفاتورة، و❌ رفض الفاتورة.

> **حقيقةٌ تُغيّر الترتيب:** `.xaml.cs` لا يكلّم زاتكا. مساره
> `api/v1.0/documents/recent?pageNo=&pageSize=` على `https://api.invoicing.eta.gov.eg`
> (L62 وL75)، و`UUID` و`publicUrl` حقّان من حقوق وثيقة **ETA المصرية** لا من حقوق زاتكا،
> و🚫 إلغاء الفاتورة و❌ رفض الفاتورة نداءان مصريّان أيضاً. لذلك: القائمة وأزرارها في هذا
> الجزء، وأعمدتُها من نافذة زاتكا الخالصة `frmInvsSyncStatusZatca.xaml` (559)، والزرّان
> المصريّان مؤجَّلان إلى الجزء الرابع (§8).

الأعمدة — من `frmInvsSyncStatusZatca.xaml` نصّاً: م · رقم الفاتورة · نوع الفاتورة · التاريخ ·
العميل · الفرع · المستخدم · الصافي · حالة المزامنة · الرسالة · تفاصيل. والمرشّحات من النافذة
نفسها: 🔄 حالة المزامنة ZATCA بـ🔵 الكل · ✅ مرسل · ❌ غير مرسل، و📅 الفترة الزمنية بـ«من» وإلى،
و📌 كل الفترة.

### 5.2 من الديسكتوب إلى السحابة

| الديسكتوب | السحابة |
|---|---|
| `name = (customer.VATno != "") ? "0100000" : "0200000"` (L88) | `buildUblInvoice()` — عميلٌ له رقم ضريبي ⇒ ضريبية، وإلا مبسّطة |
| `value = (ProcType != 2) ? "388" : "381"` و`"383"` للمدين (L90-L98) | النوع من `kind` سطراً بسطر، و`383` لإشعار المدين |
| `paymentMeansCode`: `10` · `30` إن `PayType == -1` · `48`/`42` (L127-L135) | `30` للنقد والآجل، `48`/`42` للبنك، و`<cbc:InstructionNote>Refund.</cbc:InstructionNote>` للدائن و`EditPrice.` للمدين |
| `CallReportingAPI(…, isSimplified: false)` (L371) | `POST /invoices/clearance/single` بترويسة `Clearance-Status: 1` |
| `CallReportingAPI(…, isSimplified: true)` (L377) | `POST /invoices/reporting/single` |
| `invoice.EncodedInvoice = response2.ClearedInvoice` (L389) | `cleared_invoice` + `request_payload.clearedXml` |
| `QRCode = GetEncodedInvoiceQRCode(EncodedInvoice)` (L390) | `qrFromClearedInvoice()` — XPath الديسكتوب نفسه (L474-L477): الوسم `AdditionalDocumentReference` الذي `cbc:ID`ـه `QR` |
| `success = !(ErrorMessage != null \|\| validationResults.ErrorMessages.Count > 0)` (L383-L398) | `validationResultsOf()` — لا نجاح مع رسالة خطأ، والتحذير يُسجَّل ولا يُسقط |
| `LoadZatcaCredential`: `CSID = P_CSID` و`Secret = P_Secret` (L446-L447) | `filingContext()` يفضّل زوج الإنتاج، ويرجع إلى زوج الامتثال ما لم يُصدر بعد |
| `SendZatca` → «تم ارسال الفاتورة بنجاح» (L2231-L2233) | `POST /sales-invoices/:id/einvoice/submit` |
| `insert ZatcaResponse` بعد الإرسال | صفّ `einvoice_submissions` بـ`authority_status` و`response` و`error` |

### 5.3 المسارات

| المسار | الصلاحية | ما هو |
|---|---|---|
| `GET /einvoice/filings?pageNo&pageSize&status&from&to` | `einvoice.view` | الشبكة: الأحدث أولاً، موصولة بالفاتورة (رقمها · نوعها · عميلها · فرعها · مستخدمها · صافيها) |
| `GET /einvoice/filings/:id` | `einvoice.view` | 📄 بيانات الفاتورة: الوثيقة المرسلة والوثيقة المُصادَقة، والوسوم الثمانية بأسمائها، ومكانها في السلسلة |
| `GET /einvoice/chain` | `einvoice.view` | آخر تجزئة والعدّاد والتالي |
| `POST /sales-invoices/:id/einvoice/submit` | `einvoice.submit` | يبني ويوقّع ويرسل على البيئة المحفوظة، إلا أن يتجاوزها الجسم بـ`environment` |
| `POST /einvoice/submissions/:id/retry` | `einvoice.submit` | 🔁 إعادة الإرسال |

### 5.4 أربع قواعد

1. **إرسالٌ فاشل لا يعني بيعاً فاشلاً.** لا شبكة، أو رفض TLS، أو 503: الوثيقة محفوظة أصلاً،
   فتُكتب الغلطة على صفّ الإرسال ويُرجَع `201` بحالة `failed`.
2. **رمز الفاتورة المُصادَقة ملك الهيئة.** يُقرأ من الوثيقة العائدة، ويُلغى ما حسبناه محلياً.
3. **الوثيقة تُفحص قبل أن تُتلى.** `inspectInvoiceXml()` يغلق حساب المجاميع ويرفض وثيقةً
   سترفضها الهيئة أصلاً؛ ورقم ضريبيّ غير سعوديّ الشكل تحذيرٌ لا منع.
4. **`cbc:PrepaidAmount` دائماً `0.00`.** لا حقل مدفوعاتٍ مقدَّمة في نموذج الديسكتوب، وإعلانُ
   نقد الصندوق مدفوعاً مقدَّماً يُصفّر `PayableAmount`.

### 5.5 ما اخترعناه

| التسمية | السبب |
|---|---|
| «الصنف» (عمود) | الضريبية/المبسّطة عمودٌ في `frmInvsSyncStatusZatca` باسم «نوع الفاتورة»، وهو عندنا نوع الوثيقة (فاتورة/إشعار)؛ فسُمِّي تمييزُ `0100000`/`0200000` باسم الصنف كما تسمّيه هيئة الزكاة |
| «→ السابق» · «التالي ←» | الديسكتوب ينتقل بصندوقي «رقم الصفحة/حجم الصفحة» فقط، وهما حاضران؛ والزرّان للمتصفح |
| «🔄 تحديث» | لا مقابلَ له في النافذة؛ «🔍 عرض» يعيد البحث كما هو |
| أسماء الوسوم 6-9 («تجزئة الفاتورة (Invoice Hash)» · «التوقيع الرقمي» · «المفتاح العام» · «توقيع الشهادة») | الوسوم أرقامٌ في المواصفة؛ والديسكتوب يسمّيها في `ZatcaService.cs` بأسمائها الإنكليزية (`InvoiceHash` · `DigitalSignature` · `PublicKey`) فعُرِّبت |

### 5.6 الاختبارات والتحقّق الحيّ

| | |
|---|---|
| الاختبارات | `apps/api/test/einvoicing-zatca-filing.spec.ts` (16) |
| المحرّك | `apps/api/src/modules/einvoicing/zatca/filing.ts` · `zatca/gateway.ts` · `einvoicing.service.ts` |
| المسارات | `apps/api/src/modules/einvoicing/einvoicing.controller.ts` |
| الترحيل | `packages/database/migrations/0063_einvoice_filing.sql` (+`.down.sql`) |
| التحقّق الحيّ | `scripts/verify-einvoice-zatca-filing.mjs` (53 نقطة في أحد عشر قسماً) |
| الشاشة | `apps/staff/app/settings/zatca/sent/page.tsx` + `apps/staff/lib/einvoice.ts` |

## 6. الأمان

- الأسرار (المفتاح الخاص · CSID · السرّ، والثلاثة للإنتاج) مشفّرة بـ `aes-256-gcm` عند
  التخزين، ولا تُقرأ إلا مقنّعة `****` + آخر أربعة أحرف.
- المفتاح الخاص يُعطى **مرة واحدة** في لحظة التوليد؛ لا مسار يعيده بعدها.
- توليد شهادة جديدة **يُلغي** الشهادات المصدَّرة: الشهادة مرتبطة بالمفتاح الذي طُلبت به،
  والديسكتوب يفعلها بـ `DELETE FROM ZatcaCredential` (L511).
- ⏸ إيقاف الربط مخفيّ (والآن 409) ما لم تُهيَّأ الشهادة — كما يخفي الديسكتوب الزرّ
  (`GetzatcaOnproduction` L1048).
- كل جدولٍ تحت `RLS` بـ `ENABLE` + `FORCE` وسياسة `tenant_id`؛ وعزل المستأجرين مختبر.
- التجزئة والعدّاد يُسحبان في استعلامٍ واحد بـ`SELECT … FOR UPDATE`: لا فاتورتان تشتركان
  في عدّاد، ولا تُترك ثغرةٌ بين القراءة والكتابة.
- القراءةُ غيرُ الكتابة: `einvoice.view` للشبكة وتفاصيلها والسلسلة، و`einvoice.submit`
  للإرسال وإعادته، و`einvoice.credentials.manage` للتأهيل — مختبرة بصلاحياتٍ حقيقيّة
  (المحاسب يقرأ ولا يُصدر شهادة، وأمين الصندوق لا يقرأ).

## 7. معايير القبول

1. كل تسميةٍ في الشاشتين من `frmZatcaSetting.xaml` و`frmSentEinvoice.xaml` و
   `frmInvsSyncStatusZatca.xaml` نصّاً، وما اخترعناه مبرَّر في §4.5 و§5.5.
2. اثنا عشر مساراً حقيقياً خلف الشاشتين، لا محاكاة.
3. طلب التوقيع يقرأه `openssl` ويتحقق من توقيعه.
4. الترتيب محفوظ: لا CSID إنتاج قبل امتثال، ولا امتثال قبل CSR، ولا اختبار ربط قبل
   الاثنين — ولكل منعٍ عبارة الديسكتوب.
5. الإرسال على الحقيقة: ضريبية إلى التخليص تعود بوثيقة مُصادَقة ورمزها منها، ومبسّطة إلى
   الترحيل تحتفظ برمزها المحسوب؛ والسلسلة تمشي خطوةً خطوة تحت `FOR UPDATE`.
6. 34 اختباراً (18 + 16) + 117 نقطة تحقّق حيّة (64 + 53)، والسكربتان يُعاد تشغيلهما بلا أثر.
7. طريقان حقيقيان تحت `/settings/zatca` بصلاحيّتَي `einvoice.view` و`einvoice.manage`.

1. كل تسميةٍ في الشاشة من `frmZatcaSetting.xaml` نصّاً، وما اخترعناه مبرَّر في §4.5.
2. تسعة مسارات حقيقية خلف الشاشة، لا محاكاة.
3. طلب التوقيع يقرأه `openssl` ويتحقق من توقيعه.
4. الترتيب محفوظ: لا CSID إنتاج قبل امتثال، ولا امتثال قبل CSR، ولا اختبار ربط قبل
   الاثنين — ولكل منعٍ عبارة الديسكتوب.
5. 18 اختباراً + 64 نقطة تحقّق حيّة، والسكربت يُعاد تشغيله بلا أثر.
6. طريقٌ حقيقي في شجرة `/settings/zatca` بصلاحية `einvoice.view`.

## 8. ما أُجِّل عن قصد

- 🏗️ Industry لا يُملأ من بطاقة المنشأة: لا عمودَ للنشاط التجاري في السحابة بعد؛
  ويُبلَّغ عنه كتحذير لا كفشل.
- 🚫 إلغاء الفاتورة و❌ رفض الفاتورة من `frmSentEinvoice`: نداءان على وثيقة ETA، لا على
  زاتكا — يأتيان مع الجزء الرابع (§5.1).
- 📊 حالة المزامنة بوصفها نافذةً مستقلّة (مزامنة جماعية وفلاترها الستة) — الجزء الثالث.
- 🇪🇬 مصر (`frmEtaSetting` · `EtaService` · `EtaReciptService`) والتكاملات
  (`Geidea` · `NeoleapService` · `WhatsAppSender`) — الجزءان الرابع والخامس.
- توقيع XAdES المغلَّف وكتلة `UBLExtensions`: التوقيع يُنتَج، وتغليفُه لم يُنجز بعد.
