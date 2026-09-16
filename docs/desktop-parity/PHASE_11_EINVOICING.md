# المرحلة 11 — الفاتورة الإلكترونية: زاتكا · ETA · التكاملات

**الحالة: الأجزاء الأول والثاني والثالث مُنجزة** — (1) «⚙️ إعدادات الربط الضريبي - زاتكا ZATCA» من
`Form_WPF/frmZatcaSetting.xaml` (472 سطراً) + `.xaml.cs` (1160) و`Class/ZatcaService.cs`
(546): جدول `einvoice_settings` (ترحيل `0062`) وأعمدة الشهادة على
`einvoice_credentials`، وتسعة مسارات حقيقيّة، و**18** اختباراً و**64** نقطة تحقّق حيّة،
وشاشة `/settings/zatca` (§4). (2) «🧾 الفواتير المرفوعة على موقع الضرائب» من
`Form_WPF/frmSentEinvoice.xaml` (358) + `.xaml.cs` (304) و`Class/ZatcaService.cs`
`IntegrateInvoice` (L78-L410) و`Class/InvoiceOper.cs` `SendZatca` (L2209): الترحيل والتخليص
ووثيقة الهيئة المُصادَقة ورمزها، وأعمدة على `einvoice_submissions` (ترحيل `0063`)، وثلاثة
مسارات جديدة، و**16** اختباراً و**53** نقطة تحقّق حيّة، وشاشة `/settings/zatca/sent`
(§5). (3) «🔄 مزامنة الفواتير - ZATCA» من `Form_WPF/frmInvsSyncStatusZatca.xaml` (559) +
`.xaml.cs` (1165): تقريرٌ مسجّل في محرّك التقارير بالأعمدة والمرشّحات نفسها، و
`POST /einvoice/sync` يرسل ما يختاره الكاشير، و**16** اختباراً و**53** نقطة تحقّق حيّة،
وشاشة `/settings/zatca/status` (§6). (5) «💳 بوابات الدفع — جيديا · NeoLeap» من
`Form_WPF/frmSettings.xaml` (L1726-L1831) و`.xaml.cs` (`BtnSaveGedia_Click` L2456 ·
`testGedia` L2498 · `BtnTestGedia_Click` L2513 · `Btnsavneoleap_Click` L2535 ·
`Btntestneoleap_Click` L4047) و`Class/Geidea.cs` (57) و`Class/NeoLeapService.cs`
(165): جدولان (ترحيل `0064`)، وستة مسارات، و**16** اختباراً و**64** نقطة تحقّق حيّة،
وشاشة `/settings/payment-gateways` (§7). (6) «📱 إرسال الفاتورة عبر واتساب» من
`Form_WPF/frmInvSale.xaml` L1190 و`.xaml.cs` `printwhatsapp` (L3128-L3199) و
`Class/WhatsAppSender.cs` (267) و`Class/Session.cs` (L12-L31): جدولان (ترحيل `0065`)،
وخمسة مسارات، و**17** اختباراً و**71** نقطة تحقّق حيّة، وشاشة `/settings/whatsapp` وبطاقة
«💬 واتساب» على نافذة الفاتورة (§8). الأجزاء مبيّنة في §3، ومعايير القبول في §10، وما أُجِّل
عن قصد في §11.

> **قرار:** 🇪🇬 مصر (`frmEtaSetting` · `EtaService` · `EtaReciptService`) **خارج
> النطاق** — النظام موجّهٌ اليوم للسعودية (زاتكا)، فلا تُبنى بوابةٌ مصرية قبل أن
> يُطلب ذلك؛ ويتبعها نداءا 🚫 إلغاء الفاتورة و❌ رفض الفاتورة لأنهما على وثيقة ETA.
> مصادرها مثبتة في §1 وسببُ الإسقاط في §11.

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
| مصر — **خارج النطاق** (§11) | `Form_WPF/frmEtaSetting.xaml` (348) + `.xaml.cs` (292) · `Class/EtaService.cs` (379) · `Class/EtaReciptService.cs` (268) · `EtaResultData.cs` |
| حالة المزامنة | `Form_WPF/frmInvsSyncStatusZatca.xaml` (559) + `.xaml.cs` (1165) — `ShowInvs` L159 · `GetZatcaMessage` L310 · `RecalculateNetSummary` L329 · `btnShow` L347 · `btnSync` L392 · `BtnDetails` L414 · `SendZatcaAsync` L442 · `BuildZatcaResponse` L527 · `GetZatcaStartDate` L938 · `BuildWhereClause` L863 · `LoadInvTypes` L87 · `ExportToCsv` L1078 · `PrintReport` L970 · `Reports/rptInvSumByClient.repx` · `Form_WPF/frmSentEinvoice.xaml` |
| بوابات الدفع | `Form_WPF/frmSettings.xaml` L1726-L1831 («إعدادات جيديا» + GroupBox «NeoLeap») · `frmSettings.xaml.cs` L2456-L2620 وL4047-L4062 · `Class/Geidea.cs` (57) · `Class/NeoleapService.cs` (165) · `frmPOSBill.xaml.cs` L460-L492 · `frmPOSPay.xaml.cs` L428-L441 |
| واتساب | `Form_WPF/frmInvSale.xaml` L1190 («💬 واتساب») · `frmInvSale.xaml.cs` L3124-L3126 (`SendWhatsapp_Click`) وL3128-L3199 (`printwhatsapp`: «لا يمكن إرسال الفاتورة قبل الحفظ» L3135 · `rptPOSA4.repx` L3143 · `SELECT name, mobile FROM Customers` L3152 · «❌ لا يوجد رقم جوال للعميل» L3161 · `invRef = "INV" + txtNo.Text` L3166 · `فاتورة_{invRef}.pdf` L3169 · `Common.FoundationInfoDT.Rows[0]["nameA"]` L3180 · «🧾 مرحباً …» L3182) · `Class/WhatsAppSender.cs` (267: `chrome-profile` L43-L48 · `InitializeWhatsAppAsync` L66 · `SendInvoiceAsync` L107 · `966` L113-L116 · انتظار المحادثة L119-L142 · «❌ الرقم غير مرتبط بحساب WhatsApp أو لم يتم تحميل المحادثة.» L142 · إرسال النصّ L151-L153 · `input[type='file']` L188) · `Class/Session.cs` L12-L31 |

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
| `Inv.ZatcaSent` | `sales_invoices.zatca_status ∈ (cleared, reported)` | الحالة تُكتبها خطوة الإرسال، لا تُحسب عند العرض |
| `Inv` ∪ `InvContratct` | `sales_invoices` وحدها | فاتورة المقاولات في السحابة **هي** فاتورة بيع (`projects.service.postBill`)، و`progress_bills.invoice_id` يشير إليها |
| `zatcaresponse.Message` | «الرسالة»: `error` ثم `response.errorMessages` ∪ `warningMessages` ثم `response.message` | الديسكتوب يقرأ أول صفٍّ يصادفه؛ هنا أحدث صفّ |

## 3. الأجزاء

| الجزء | النوافذ | الحالة |
|---|---|---|
| 1 | ⚙️ إعدادات الربط الضريبي — `frmZatcaSetting` (الإعدادات · خصائص CSR · التأهيل الأربع · إيقاف الربط · التجديد) | ✅ مُنجز (§4) |
| 2 | 🧾 الإرسال والتوقيع والسلسلة — `frmSentEinvoice` + `ZatcaService.IntegrateInvoice` (ترحيل/تخليص، QR بثمانية وسوم، إعادة المحاولة) | ✅ مُنجز (§5) |
| 3 | 📊 حالة المزامنة — `frmInvsSyncStatusZatca` (شبكة الفواتير وحالاتها ومرشّحاتها) | ✅ مُنجز (§6) |
| 4 | 🇪🇬 مصر — `frmEtaSetting` + `EtaService` + `EtaReciptService` | ⛔ خارج النطاق (§11) |
| 5 | 💳 بوابات الدفع — `Geidea` · `NeoleapService` (إعدادات · 🧪 اختبار · 💳 تحصيل · سجل) | ✅ مُنجز (§7) |
| 6 | 📱 إرسال الفاتورة عبر واتساب — `WhatsAppSender` · «💬 واتساب» على `frmInvSale` | ✅ مُنجز (§8) |

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
> المصريّان مؤجَّلان مع الجزء الرابع (§11).

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

## 6. الجزء الثالث — 📊 حالة المزامنة (`frmInvsSyncStatusZatca`)

### 6.1 النافذة

`Form_WPF/frmInvsSyncStatusZatca.xaml` (559) + `.xaml.cs` (1165): «🔄 مزامنة الفواتير -
ZATCA» في الرأس، و«🔍 خيارات البحث» في لوحةٍ على اليمين، وشبكةٌ من اثني عشر عموداً،
وشريطٌ سفليّ فيه 📊 تصدير Excel · 🖨️ طباعة · 👁️ معاينة · ✖ خروج. ما تبحث عنه النافذة هو
سؤال الكاشير في آخر النهار: **أيّ هذه الفواتير قبلتها زاتكا؟**

المرشّحات، من اللوحة نصّاً:

| المرشّح | القيم | في السحابة |
|---|---|---|
| 🔄 حالة المزامنة ZATCA | 🔵 الكل · ✅ مرسل · ❌ غير مرسل | `zatca_status ∈ (cleared, reported)` |
| 📋 نوع الفاتورة | مبيعات · نقطة بيع · إشعار · مقاولات · أندرويد (+ صندوق «الكل» يعطّل القائمة) | `kind` و`order_type`/`shift_id` و`progress_bills.invoice_id` |
| 📅 الفترة الزمنية | 📌 كل الفترة · من · إلى | `posted_at::date` |
| الفرع | — | `branchId` (الديسكتوب يقيدها بفرع الجهاز) |

والأعمدة كذلك: م · ID · الفرع · نوع الفاتورة · رقم الفاتورة · التاريخ · العميل · المستخدم ·
الصافي · الرسالة · حالة المزامنة · تفاصيل — وعمودان مخفيّان (`DgvProcType` · `DgvStore`)
يطبعهما التقرير ولا ترسمهما الشبكة.

### 6.2 من الديسكتوب إلى السحابة

| الديسكتوب | السحابة |
|---|---|
| `SELECT … FROM Inv UNION ALL SELECT … FROM InvContratct` (L176-L186) | `sales_invoices` وحدها؛ وفاتورة المقاولات تُعرف بـ`EXISTS (progress_bills…)` |
| `proc_type IN (1,2)` و`IS_Deleted=0` (L866 وL935) | `kind ∈ (sale, sale_return, credit_note, debit_note)` و`status='posted'` و`voided_at IS NULL` |
| `ZatcaSent` (L899-L904) | `zatca_status ∈ (cleared, reported)` |
| `GetZatcaMessage` (L310-L327) | «الرسالة»: الغلطة، ثم رسائل التحقّق والتحذير، ثم سبب التوقّف |
| `GetInvoiceTypeAr` + `GetCustomerTaxType` (L346 · L302) | «نوع الفاتورة»: عميلٌ له رقم ضريبي ⇒ ضريبية، وإلا مبسّطة؛ والمرتجع إشعارٌ دائن |
| `RecalculateNetSummary` (L329-L345) و`NetTotal` (L1058) | ثلاث بطاقات: الفواتير · المرتجعات والإشعارات · الصافي (= الأولى ناقص الثانية) |
| `BuildZatcaResponse` (L527-L565) | صفّ `einvoice_submissions` بـ`authority_status` و`response` |
| `SendZatcaAsync`: «هل انت متأكد من مزامنة الفواتير المختارة ؟» ثم إرسالٌ سطراً سطراً (L442-L565) | `POST /einvoice/sync` بـ`{ ids }`، ونتيجةٌ لكل سطر |
| `ExportToCsv` (L1078) | `POST /reports/einvoice-sync-status/export` — ملفٌّ حقيقي من الخادم |
| `PrintReport` (L970) و`rptInvSumByClient.repx` | `GET /reports/print/einvoice-sync-status` — ورقة محرّك الطباعة بالترويسة والتذييل والختم |

### 6.3 🔄 مزامنة ZATCA

| المسار | الصلاحية | ما هو |
|---|---|---|
| `GET /reports/einvoice-sync-status` | `reporting.view` | الشبكة والمرشّحات، كما يعرضها محرّك التقارير |
| `POST /einvoice/sync` | `einvoice.submit` | 🔄 مزامنة ZATCA: يرسل المحدَّد سطراً سطراً، ويُرجع نتيجة كل سطر |

و`POST /einvoice/sync` يُجيب بـ`{ requested, sent, failed, skipped, results[], message }`:
«تمت العملية بنجاح ✅» حين تُقبل كلها، وعددَ ما أُرسل وما لم يُقبل حين لا تُقبل،
وعبارةً لكل سطر — لأنّ دفعةً نصفُها ناجح يجب أن تقول نصفها الآخر.

### 6.4 أربع قواعد

1. **المسوَّدة والمُرسلة صفوفٌ مُتجاوَزة لا فاشلة.** الديسكتوب يعيد إرسالها فتردّها الهيئة؛
   ونحن نرفض الإرسال الثاني أصلاً (الجزء الثاني)، فتُبلَّغ هنا عبارةً أمام السطر.
2. **⏸ إيقاف الربط يقولها.** الديسكتوب يخفي النداء خلف `if (ZatcaIntegerationActive)`
   فيصمت الزرّ؛ هنا كل سطر «مُتجاوَز» ورسالته «الربط موقوف…»، ولا وثيقة تُحفظ.
3. **الشبكة والملفّ والورقة مصدرها واحد.** التقرير مسجّل في محرّك التقارير، فالتصدير
   يُعيد تشغيله على الخادم والورقةُ تُطبع منه — لا يمكن أن يختلف الملفّ عمّا على الشاشة.
4. **الورقة تحمل مجاميعها.** `RecalculateNetSummary` تحسب مجموعين والديسكتوب يطبع
   فرقهما؛ ونحن نعرض الثلاثة، لأنّ المفتّش يسأل عنها كلها.

### 6.5 ما اخترعناه

| التسمية | السبب |
|---|---|
| ثلاث بطاقات («إجمالي الفواتير» · «إجمالي المرتجعات والإشعارات» · «الصافي») | `RecalculateNetSummary` تحسب الاثنين و`BuildReportDataSet` يطبع فرقهما وحده؛ فالثاني والثالث اسمان لموجود، والأول كذلك |
| «تحديد الكل» | `SelectionMode="MultipleRow"` عند الديسكتوب مربّعٌ في رأس الشبكة؛ ولا مقابلَ لاسمه في ملفّاته |
| «مزامنة الفواتير» في فتات الخبز | النافذة لا تُفتح من قائمةٍ في الديسكتوب؛ والعنوان من `Title` نفسها |
| حدٌّ لعدد الفواتير في الدفعة (200) | الديسكتوب يعمل على جهازٍ واحد وقاعدةٍ واحدة فلا يحتاجه؛ و`422 EINVOICE_SYNC_TOO_MANY` عبارةٌ صريحة |
| 📊 تصدير Excel يُنتج `xlsx` | الزرّ باسم Excel، والديسكتوب يكتب `.csv` خلفه؛ والملفّ الحقيقي أوفى بالاسم |

### 6.6 الاختبارات والتحقّق الحيّ

| | |
|---|---|
| الاختبارات | `apps/api/test/einvoicing-zatca-sync.spec.ts` (16) |
| التقرير | `apps/api/src/modules/reporting/report-catalog.ts` — `einvoice-sync-status` |
| المسار | `apps/api/src/modules/einvoicing/einvoicing.controller.ts` · `einvoicing.service.ts` (`sync`) |
| التحقّق الحيّ | `scripts/verify-einvoice-zatca-sync.mjs` (53 نقطة في أحد عشر قسماً) |
| الشاشة | `apps/staff/app/settings/zatca/status/page.tsx` + `apps/staff/lib/einvoice.ts` |

## 7. الجزء الخامس — 💳 بوابات الدفع (جيديا · NeoLeap)

### 7.1 النافذة وما تكتبه

`Form_WPF/frmSettings.xaml` L1726-L1831 تابٌ بعنوان «إعدادات جيديا» وفيها بطاقتان: بطاقة
«💳 إعدادات جيديا» وفيها «تفعيل الدفع عن طريق جيديا» و«طباعة ايصال» و«المنفذ» و«المبلغ»
وزرّا «🧪 TEST» و«💾 حفظ»؛ و`GroupBox` بعنوان «NeoLeap» وفيها «تفعيل NeoLeap» و«طباعة إيصال
NeoLeap» و«المنفذ» و«المبلغ» و«Token» وصندوق «Logging» وزرّا «🧪 Test» و«💾 حفظ».

`BtnSaveGedia_Click` (L2456) يكتب صفّ `GediaSetting (id=1)` —
`IsGediaActive` · `GediaPort` · `GediaEnableReceiptPrint` — و`Btnsavneoleap_Click` (L2535)
يحذف صفّ `SettingNeoleap (id=1)` ويكتبه من جديد — `IsNeoLeapActive` · `NeoLeapPort` ·
`NeoLeapEnableReceiptPrint` · `neoleaptoken`. وكلتاهما ترفضان الحفظ بغير كلمة سرّ المدير:
«نأسف ليس لديك الصلاحية لتغيير الإعدادات».

التحصيل نفسه ليس في هذه النافذة: `frmPOSBill.xaml.cs` L460-L492 و`frmPOSPay.xaml.cs`
L428-L441 يطلبان البطاقة في أثناء حفظ الفاتورة، ولا يمرّ الحفظ إلا برمزٍ مقبول.

### 7.2 من الديسكتوب إلى السحابة

| الديسكتوب | السحابة | ملاحظة |
|---|---|---|
| `GediaSetting` (id=1) · `SettingNeoleap` (id=1) | `payment_gateway_settings` بمفتاح `(tenant_id, provider)` | الصفّ الوحيد يصير صفّاً لكل مؤسسة؛ الأعمدة نفسها |
| `Geidea.ConnectGeidea(Payment)` — `"<هللة>;1;1!"` على COM1 بسرعة 38400 عبر `madaapi.dll`، والجواب خمسة بايتات | `gateways/geidea.ts` — نداءات جيديا المنشورة | لا COM على خادم؛ والمال والرموز هي هي |
| الرموز المقبولة `000 · 001 · 003 · 007 · 087 · 089`، وإلا «العملية مرفوضة، يرجى إعادة الدفع» | `responseCode` · `detailedResponseCode` — `000` نجاح، وما عداه خطأ | المفردات نفسها |
| `NeoleapService.ProcessSale` — `requestType` · `merchantToken` · `amount` · `ecrRef` · `ecrToken` · `printFlag` · `cashBack` | `gateways/neoleap.ts` — JSON الطلب نفسه | النقل كان داخل `neoleapconnector` المترجَمة، فصار عنواناً يُضبط |
| `ParseResponse` — `ErrorMsg` ثم `TransactionResult.StatusCode` `00` · `01` · `02` | الحقول نفسها: `ApprovalCode` · `RRN` · `STAN` · `CardScheme.English` · `PAN` · `TransactionType.English` | لا «حالة معلّقة» عند نيوليب: الجهاز يجيب في الحال |
| «المنفذ» (`txtportneoleap`) | `base_url`، ويفترض `http://127.0.0.1:<المنفذ>` | المنفذ وحده ليس عنواناً |
| «Logging» (صندوقٌ في النافذة) | `payment_gateway_settings.last_test` | الجواب يبقى بعد إغلاق النافذة |
| — | `payment_gateway_transactions` | **ليس في الديسكتوب**: لم يكن يسجّل شيئاً؛ طبع إيصالاً ومضى |

### 7.3 المسارات

| المسار | الصلاحية | ما يفعل |
|---|---|---|
| `GET /payment-gateways` | `pos.config.manage` | البطاقتان، والمفتاح مقنَّع |
| `PUT /payment-gateways/:provider` | `pos.config.manage` | 💾 حفظ |
| `POST /payment-gateways/:provider/test` | `pos.config.manage` | 🧪 TEST · 🧪 Test، وجوابه في «Logging» |
| `POST /payment-gateways/:provider/sale` | `sales.invoice.pay` | 💳 التحصيل، وتقييده على الفاتورة إن مُرِّرت |
| `GET /payment-gateways/transactions` | `sales.view` | 📜 السجل |
| `POST /payment-gateways/transactions/:id/refresh` | `sales.invoice.pay` | 🔄 تحديث حالة جلسةٍ لم يدفعها صاحبها بعد |

جيديا على مواصفتها المنشورة (https://docs.geidea.net): `POST
/payment-intent/api/v2/direct/session` لفتح الجلسة، و`GET
/pgw/api/v1/direct/order?MerchantReferenceId=…` لسؤالها، وصفحة الدفع
`https://www.ksamerchant.geidea.net/hpp/checkout/?<sessionId>`؛ وتوقيعها
`base64(HMAC-SHA256(كلمة السرّ، المعرّف العام ‖ المبلغ بعشرتين ‖ العملة ‖ المرجع ‖
الطابع الزمني))`. ونيوليب على عقدها كما يظهر في `NeoleapService.cs`: طلب `SALE` واحد
وجوابه `00`/`01`/`02`، بلا مسار حالة — فلا نخترع واحداً.

### 7.4 خمس قواعد

1. **«المنفذ» وحده لا يكفي، ولا يمنع.** الديسكتوب كان ينادي منفذاً على جهازه؛ والخادم
   ينادي عنواناً، فإن لم يُكتب اشتُقّ من المنفذ (`http://127.0.0.1:<المنفذ>`).
2. **🧪 Simulation مفتاحٌ عام، وهو مطفأٌ أبداً في الاختبارات.** كما في زاتكا: لا بوابة
   حقيقيّة تُطلب، ولا بطاقة تُخصم. وفي المحاكاة يُقرَّر جواب صاحب البطاقة من بادئة
   `ecrRef` (`DECLINE-` · `CANCEL-` · `ERROR-` · `UNKNOWN-` · `PENDING-`) — السبيل الوحيد
   لاختبار الرفض بلا بطاقة.
3. **المرجع (`ecrRef`) يحمي من الخصم مرتين.** هو مفتاحٌ فريد لكل `(مستأجر · بوابة)`؛
   فالضغطتان على 💳 بمرجعٍ واحد هما عمليةٌ واحدة (`409 PAYMENT_REFERENCE_DUPLICATED`).
4. **المقبولة تُقيَّد مرةً واحدة.** `SalesService.addPayment` بالوسيلة `card` وبمفتاح
   التكرار نفسه، ثم يُوسَم الصفّ `settled` — فلا يُدفع مرتين وإن أُعيد السؤال.
5. **المرفوضة تُسجَّل ولا تُبتلع.** «العملية مرفوضة، يرجى إعادة الدفع» كانت صندوقَ رسالةٍ
   يمحوه الضغط على «موافق»؛ وهنا تبقى في السجل بكلمة البوابة (`Declined` ·
   `Cancelled or Error` · «تعذّر الوصول») وبردّها الخامّ بعد حذف السرّ منه.

### 7.5 ما اخترعناه

| التسمية | السبب |
|---|---|
| «عنوان البوابة» بجانب «المنفذ» | المنفذ وحده ليس عنواناً لخدمةٍ على الشبكة |
| «رابط الإرجاع» (callbackUrl) | صفحة جيديا المُستضافة تحتاج مكاناً تردّ إليه؛ والديسكتوب قرأ جوابه من الكبل فلم يحتجها |
| 🧪 Simulation (مفتاح عام) | لا يوجد في نافذة الدفع؛ مأخوذٌ من نافذة زاتكا نفسها، وهو الضمان ألّا تُخصم بطاقةٌ حقيقية في عرضٍ أو اختبار |
| بادئات المرجع في المحاكاة (`DECLINE-` · `CANCEL-` · `ERROR-` · `UNKNOWN-` · `PENDING-`) | الرفض قرارُ صاحب البطاقة على جهازه؛ ولا سبيل لاختباره إلا أن يُطلب |
| «📜 آخر العمليات» | الديسكتوب لم يسجّل شيئاً؛ والسجل إضافةٌ مُلزِمة لخادمٍ يتولّى المال |

### 7.6 الاختبارات والتحقّق الحيّ

| | |
|---|---|
| الاختبارات | `apps/api/test/payment-gateways.spec.ts` (16) |
| البوابتان | `apps/api/src/modules/payments/gateways/geidea.ts` · `neoleap.ts` · `index.ts` |
| المسار | `apps/api/src/modules/payments/payments.controller.ts` · `payments.service.ts` |
| الترحيل | `packages/database/migrations/0064_payment_gateways.sql` + `packages/database/src/schema/payments.ts` |
| التحقّق الحيّ | `scripts/verify-payment-gateways.mjs` (64 نقطة في أحد عشر قسماً) |
| الشاشة | `apps/staff/app/settings/payment-gateways/page.tsx` + `apps/staff/lib/payment-gateways.ts` |

## 8. الجزء السادس — 📱 إرسال الفاتورة عبر واتساب

### 8.1 الزرّ وما يفعله

`Form_WPF/frmInvSale.xaml` L1190 عنصر قائمة «💬 واتساب» في قائمة نافذة فاتورة البيع،
ومعالجه `SendWhatsapp_Click` (L3124-L3126) يستدعي `printwhatsapp()` (L3128-L3199):

- **L3132-L3139** — «لا يمكن إرسال الفاتورة قبل الحفظ» (و«An invoice cannot be sent before
  saving.») إن كانت سلة الفاتورة فارغة.
- **L3143-L3145** — الورقة: `print.RptName = "rptPOSA4.repx"`، و`"rptPricingInv.repx"` إن
  كانت الفاتورة تسعيرة (`ProcType == 4`).
- **L3152** — `SELECT name, mobile FROM Customers WHERE id=…`: اسم العميل وجواله من جدول
  العملاء؛ وإن كان الجوال فارغاً: «❌ لا يوجد رقم جوال للعميل» (L3161).
- **L3166-L3169** — `invRef = "INV" + txtNo.Text`، ثم تصدير الورقة إلى
  `فاتورة_{invRef}.pdf` في مجلد البرنامج.
- **L3180** — `foundName = Common.FoundationInfoDT.Rows[0]["nameA"]`: اسم المنشأة من
  بيانات المؤسسة.
- **L3182-L3183** — «🧾 مرحباً {custName}، هذه فاتورتك رقم {invRef} من {foundName}».
- **L3185-L3187** — `await Session.EnsureWhatsAppSessionAsync()` ثم
  `Session.waSender.SendInvoiceAsync(mobile, pdfPath, message)`.

ووراء الزرّ `Class/WhatsAppSender.cs` (267): متصفّح Chrome يعمل على ملف تعريفٍ دائم
`%LocalAppData%\MyApp\chrome-profile` (L43-L48) — أي أن «من أُرسل؟» كان جوابها من مسح رمز
QR على جهاز الكاشير (`InitializeWhatsAppAsync` L66) — ثم `SendInvoiceAsync` (L107):

- **L113-L116** — `if (!text.StartsWith("966")) text = "966" + text.TrimStart('0');`
- **L118-L119** — يفتح `https://web.whatsapp.com/send?phone={text}` وينتظر عشر ثوان.
- **L119-L142** — ينتظر صندوق الكتابة خمساً وعشرين ثانية؛ فإن لم يجده:
  «❌ الرقم غير مرتبط بحساب WhatsApp أو لم يتم تحميل المحادثة.»
- **L151-L153** — يكتب الرسالة ويضغط `Enter`.
- **L188** — يمرّر ملفّ PDF إلى `input[type='file']`.

`Class/Session.cs` L12 `public static WhatsAppSender waSender`؛ و`EnsureWhatsAppSessionAsync`
(L14-L31) يعيد التهيئة إن انتهت الجلسة، ورسالته «❌ فشل في إعادة تهيئة جلسة WhatsApp: ».

### 8.2 من الديسكتوب إلى السحابة

| الديسكتوب | السحابة | ملاحظة |
|---|---|---|
| Chrome + ملف تعريف دائم ومسح QR (`WhatsAppSender.cs` L43-L48 · L66) | `whatsapp_settings` — «عنوان الواتساب» (Phone Number ID) و«الرمز» (Access Token) | لا متصفّح على الخادم: الرقم يُضبط مرة، والرمز يُحفظ مشفّراً ولا يُقرأ إلا مقنَّعاً |
| لا صفّ إعدادات أصلاً: الجلسة في جهاز الكاشير | `whatsapp_settings` صفٌّ واحد لكل مستأجر: تفعيل · رمز الدولة · 📎 إرفاق الفاتورة · 🧪 محاكاة | كان كل شيء ثابتاً في الكود |
| `if (!text.StartsWith("966")) text = "966" + text.TrimStart('0');` (L113-L116) | `normalizePhone(raw, defaultCountryCode)` | رمز الدولة صار إعداداً لا ثابتاً |
| `rptPOSA4.repx` → `ExportToPdf` → `فاتورة_{invRef}.pdf` | «فاتورة-INV…txt» بترميز UTF-8 | لا مصدر PDF على الخادم (§8.5) |
| النصّ أولاً (L151-L153) ثم الملف (L188) | الرسالة أولاً، ثم رفع الملف ثم رسالة المستند | الترتيب نفسه |
| صندوق رسالة يمحوه «موافق» | `whatsapp_messages` — الرقم · النصّ · اسم المرفق وحالته · معرّف ميتا · الخطأ | **ليس في الديسكتوب**: لم يكن يسجّل شيئاً |
| 🧪 اختبار: لا شيء — الأمل أن يكون رمز QR قد مُسح | `GET /{phone-number-id}` | «هل هذا الرقم لنا، وهذا الرمز صالح له؟» سؤالٌ يجيب عنه الخادم |

### 8.3 المسارات

| المسار | الصلاحية | ما يفعل |
|---|---|---|
| `GET /whatsapp/settings` | `tenant.settings.manage` | ⚙️ الإعدادات، والرمز مقنَّع |
| `PUT /whatsapp/settings` | `tenant.settings.manage` | 💾 حفظ |
| `POST /whatsapp/test` | `tenant.settings.manage` | 🧪 اختبار، وجوابه يبقى في «Logging» |
| `POST /whatsapp/send` | `sales.view` | 💬 واتساب — الزرّ على نافذة الفاتورة |
| `GET /whatsapp/messages` | `sales.view` | 📜 السجل: `?status=` · `?invoiceId=` · `?limit=` |

ميتا على مواصفتها المنشورة (Graph `v21.0` على `https://graph.facebook.com`):
`POST /{phone-number-id}/messages` للنصّ وللمستند، و`POST /{phone-number-id}/media` لرفع
الملفّ (مهلة عشرين ثانية، وعمر الملفّ ثلاثون يوماً)، و`GET /{phone-number-id}` لاختبار
الاتصال؛ وخطؤها يُقرأ من `error.message` و`error.code` بلا زيادة.

### 8.4 خمس قواعد

1. **💬 لا يُرسل إلا فاتورةً مرحَّلة.** «لا يمكن إرسال الفاتورة قبل الحفظ» صارت
   `409 SALES_INVOICE_NOT_POSTED` بعبارة «لا يمكن إرسال الفاتورة قبل ترحيلها — رحّلها
   أولاً.»: المسوَّدة لا رقم لها، والتحية تسمّي الفاتورة برقمها.
2. **الرقم يُصاغ كما صاغه الديسكتوب، ورمزه إعداد.** `0551234567` تصير `966551234567`، ورقمٌ
   مكتمل لا يُكرَّر رمزه، و`+966 55 123 4567` كذلك؛ و`default_country_code` يجعل القاعدة
   نفسها تعمل لمن عملاؤه في بلدٍ آخر.
3. **🧪 محاكاة مفتاحٌ عام، وهو مفعولٌ أبداً في الاختبارات.** كما في زاتكا وفي بوابات
   الدفع: لا رسالة حقيقية تُطلب ولا رقم حقيقي يُنادى. وفي المحاكاة يُقرَّر «غير مرتبط
   بواتساب» من الرقم نفسه إن انتهى بـ`0000` — السبيل الوحيد لاختبار الفشل بلا رقم حقيقي.
4. **النصّ قبل الملفّ، والفشل يُسجَّل ولا يُبتلع.** الرسالة تُرسل أولاً كما يفعل
   `SendInvoiceAsync` (L151 ثم L188)؛ فإن فشلت لم يُرفع شيء، وإن نجحت وفشل المرفق بقيت
   الرسالة «مُرسلة» والمرفق «فاشلاً» بعبارة ميتا، والسطر واحد في السجل يحمل الاثنين.
5. **من يرى الفاتورة يُرسلها، ومن يملك الإعدادات يضبطها.** الإرسال والسجل بـ`sales.view`
   (المحاسب وأمين الصندوق كلاهما)، والضبط بـ`tenant.settings.manage` (المالك وحده) — فلا
   يغيّر من يرسل الفاتورة من أين تخرج رسائل المؤسسة.

### 8.5 ما اخترعناه

| التسمية | السبب |
|---|---|
| «عنوان الواتساب» (Phone Number ID) | لا مقابل له في الديسكتوب: كان مسح رمز QR هو الذي يجيب عن «أي رقم هذا؟» |
| «الرمز» (Access Token) | الجلسة كانت ملف تعريف Chrome على جهاز الكاشير؛ والخادم يحتاج رمزاً، وهو سرّ يُشفَّر ولا يُعرض إلا مقنَّعاً |
| «رمز الدولة» | كان «966» ثابتاً في الكود (L113-L116)؛ صار إعداداً لأن النظام يُباع لغير السعودية أيضاً |
| 📎 «إرفاق الفاتورة» | كان يُرفق دائماً؛ صار خياراً لمن يريد الكلمات وحدها |
| 🧪 «محاكاة» | مأخوذة من نافذة زاتكا نفسها: الضمان ألّا تُرسل رسالة حقيقية في عرضٍ أو اختبار |
| «فاتورة-INV…txt» بدل PDF | لا مكتبة PDF في المشروع أصلاً، وتصديرٌ على الخادم لملفٍّ عربي يحتاج خطّاً يشكّل الحروف؛ والطباعة عندنا صفحة HTML يطبعها المتصفّح (`reporting.service.ts`) — فالمرفق نصٌّ عربي UTF-8، وهو نوع مستندٍ تقبله ميتا |
| 📜 «سجل الإرسال» | الديسكتوب لم يسجّل شيئاً؛ و«هل وصلت الفاتورة؟» سؤالٌ على خادمٍ أن يجيب عنه |

### 8.6 الاختبارات والتحقّق الحيّ

| | |
|---|---|
| الاختبارات | `apps/api/test/whatsapp-invoice.spec.ts` (17) |
| البوابة | `apps/api/src/modules/integrations/whatsapp/cloud-api.ts` · `whatsapp.service.ts` · `whatsapp.controller.ts` |
| الترحيل | `packages/database/migrations/0065_whatsapp_messages.sql` + `packages/database/src/schema/messaging.ts` |
| التحقّق الحيّ | `scripts/verify-whatsapp.mjs` (71 نقطة في أحد عشر قسماً) |
| الشاشتان | `apps/staff/app/settings/whatsapp/page.tsx` + بطاقة «💬 واتساب» في `apps/staff/app/sales/invoices/[id]/page.tsx` + `apps/staff/lib/whatsapp.ts` |

## 9. الأمان
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
  للإرسال وإعادته ومزامنته، و`einvoice.credentials.manage` للتأهيل — مختبرة بصلاحياتٍ
  حقيقيّة (المحاسب يقرأ ويُرسل ولا يُصدر شهادة، وأمين الصندوق لا يقرأ).
- التصديرُ صلاحيةٌ وحدها: الشبكة تُقرأ بـ`reporting.view`، وملفُّها يُنتَج بـ
  `reporting.export.execute` — وهو فرقٌ مختبر بصلاحية المحاسب.
- رمز واتساب (`access_token_enc`) مشفّر بـ `aes-256-gcm` بالغلاف نفسه (`v1:iv:tag:data`)،
  ولا يظهر في أيّ ردّ إلا مقنَّعاً `****` + آخر أربعة أحرف؛ وفكّه الفاشل خطؤه صريح
  (`SECRET_DECRYPT_FAILED`) لا رسالةً غامضة.

## 10. معايير القبول

1. كل تسميةٍ في الشاشات من `frmZatcaSetting.xaml` و`frmSentEinvoice.xaml` و
   `frmInvsSyncStatusZatca.xaml` و`frmSettings.xaml` (L1726-L1831) و`frmInvSale.xaml`
   (L1190) نصّاً، وما اخترعناه مبرَّر في §4.5 و§5.5 و§6.5 و§7.5 و§8.5.
2. أربعة وعشرون مساراً حقيقياً خلف الشاشات، لا محاكاة.
3. طلب التوقيع يقرأه `openssl` ويتحقق من توقيعه.
4. الشبكة والملفّ والورقة من استعلامٍ واحد مسجّل في محرّك التقارير؛ و«الصافي» مجموعٌ من
   الصفوف المعروضة فلا يخالفها.
5. الترتيب محفوظ: لا CSID إنتاج قبل امتثال، ولا امتثال قبل CSR، ولا اختبار ربط قبل
   الاثنين — ولكل منعٍ عبارة الديسكتوب.
6. الإرسال على الحقيقة: ضريبية إلى التخليص تعود بوثيقة مُصادَقة ورمزها منها، ومبسّطة إلى
   الترحيل تحتفظ برمزها المحسوب؛ والسلسلة تمشي خطوةً خطوة تحت `FOR UPDATE`؛ و🔄 مزامنة
   ZATCA تبلّغ ما أُرسل وما لم يُقبل سطراً سطراً.
7. 83 اختباراً (18 + 16 + 16 + 16 + 17) + 305 نقطة تحقّق حيّة (64 + 53 + 53 + 64 + 71)،
   والسكربتات الخمسة تُعاد تشغيلها بلا أثر.
8. خمسة طرقٍ حقيقيّة تحت `/settings/zatca` و`/settings/payment-gateways` و
   `/settings/whatsapp` بصلاحيّات `einvoice.view` و`einvoice.manage` و`einvoice.submit` و
   `pos.config.manage` و`sales.invoice.pay` و`tenant.settings.manage`.
9. 💳 بوابة الدفع تُنادى على الحقيقة (جلسة جيديا وسؤالها على مواصفتها المنشورة، و`SALE`
   نيوليب بعقدها)، والمرفوضة تُسجَّل بكلمة البوابة، والمقبولة تُقيَّد على الفاتورة مرةً
   واحدة.
10. 📱 واتساب يُنادى على الحقيقة (Graph `v21.0` على مواصفته المنشورة: رسالة نصّ، ثم رفع
    ملفّ ثم رسالة مستند)، والتحية هي تحية الديسكتوب نصّاً، والرقم يُصاغ بقاعدة
    `WhatsAppSender.SendInvoiceAsync`، و«غير المرتبط بواتساب» يُسجَّل بعبارة الديسكتوب ولا
    يُبتلع.

## 11. ما أُجِّل عن قصد

- 🏗️ Industry لا يُملأ من بطاقة المنشأة: لا عمودَ للنشاط التجاري في السحابة بعد؛
  ويُبلَّغ عنه كتحذير لا كفشل.
- 🚫 إلغاء الفاتورة و❌ رفض الفاتورة من `frmSentEinvoice`: نداءان على وثيقة ETA، لا على
  زاتكا — وهما خارج النطاق مع مصر نفسها (أدناه).
- 🏦 المستودع و«نوع العملية» يظهران في الورقة المطبوعة ولا يظهران في الشبكة — كما
  يُخفيهما الديسكتوب (`DgvProcType` · `DgvStore` بـ`Visible="False"`).
- 🇪🇬 مصر (`frmEtaSetting` · `EtaService` · `EtaReciptService`) — **خارج النطاق بقرارٍ
  صريح**: النظام موجّهٌ اليوم للسعودية (زاتكا)، فلا تُبنى بوابةٌ مصرية قبل أن يُطلب ذلك.
  ملفاتها مثبتة في §1: تُقرأ يوم تُطلب، ويبقى `authority='eta'` في جداول الإرسال قائماً.
- توقيع XAdES المغلَّف وكتلة `UBLExtensions`: التوقيع يُنتَج، وتغليفُه لم يُنجز بعد.
