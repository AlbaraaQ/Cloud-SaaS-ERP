# المرحلة 11 — الفاتورة الإلكترونية: زاتكا · ETA · التكاملات

**الحالة: الجزء الأول مُنجز** — «⚙️ إعدادات الربط الضريبي - زاتكا ZATCA» من
`Form_WPF/frmZatcaSetting.xaml` (472 سطراً) + `.xaml.cs` (1160) و`Class/ZatcaService.cs`
(546): جدول `einvoice_settings` (ترحيل `0062`) وأعمدة الشهادة على
`einvoice_credentials`، وتسعة مسارات حقيقيّة، و**18** اختباراً للواجهة البرمجية
و**64** نقطة تحقّق حيّة، وشاشة `/settings/zatca` بالتسميات نفسها. الأجزاء مبيّنة في §3،
ومعايير القبول في §6، وما أُجِّل عن قصد في §7.

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
| إرسال الفاتورة | `Class/ZatcaService.cs` (546) — `IntegrateInvoice` L78 · `CallReportingAPI` L371 · `CallComplianceInvoiceAPI` L900 |
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

## 3. الأجزاء

| الجزء | النوافذ | الحالة |
|---|---|---|
| 1 | ⚙️ إعدادات الربط الضريبي — `frmZatcaSetting` (الإعدادات · خصائص CSR · التأهيل الأربع · إيقاف الربط · التجديد) | ✅ مُنجز (§4) |
| 2 | 🧾 الإرسال والتوقيع والسلسلة — `frmSentEinvoice` + `ZatcaService.IntegrateInvoice` (ترحيل/تخليص، QR بثمانية وسوم، إعادة المحاولة) | ⬜ |
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

## 5. الأمان

- الأسرار (المفتاح الخاص · CSID · السرّ، والثلاثة للإنتاج) مشفّرة بـ `aes-256-gcm` عند
  التخزين، ولا تُقرأ إلا مقنّعة `****` + آخر أربعة أحرف.
- المفتاح الخاص يُعطى **مرة واحدة** في لحظة التوليد؛ لا مسار يعيده بعدها.
- توليد شهادة جديدة **يُلغي** الشهادات المصدَّرة: الشهادة مرتبطة بالمفتاح الذي طُلبت به،
  والديسكتوب يفعلها بـ `DELETE FROM ZatcaCredential` (L511).
- ⏸ إيقاف الربط مخفيّ (والآن 409) ما لم تُهيَّأ الشهادة — كما يخفي الديسكتوب الزرّ
  (`GetzatcaOnproduction` L1048).
- كل جدولٍ تحت `RLS` بـ `ENABLE` + `FORCE` وسياسة `tenant_id`؛ وعزل المستأجرين مختبر.

## 6. معايير القبول

1. كل تسميةٍ في الشاشة من `frmZatcaSetting.xaml` نصّاً، وما اخترعناه مبرَّر في §4.5.
2. تسعة مسارات حقيقية خلف الشاشة، لا محاكاة.
3. طلب التوقيع يقرأه `openssl` ويتحقق من توقيعه.
4. الترتيب محفوظ: لا CSID إنتاج قبل امتثال، ولا امتثال قبل CSR، ولا اختبار ربط قبل
   الاثنين — ولكل منعٍ عبارة الديسكتوب.
5. 18 اختباراً + 64 نقطة تحقّق حيّة، والسكربت يُعاد تشغيله بلا أثر.
6. طريقٌ حقيقي في شجرة `/settings/zatca` بصلاحية `einvoice.view`.

## 7. ما أُجِّل عن قصد

- 🏗️ Industry لا يُملأ من بطاقة المنشأة: لا عمودَ للنشاط التجاري في السحابة بعد؛
  ويُبلَّغ عنه كتحذير لا كفشل.
- 🧾 الإرسال الفعلي للفواتير (ترحيل/تخليص) و📊 حالة المزامنة و🇪🇬 مصر والتكاملات —
  الأجزاء 2-5.
- ربط مسار الإرسال الحالي (`/sales-invoices/:id/einvoice/submit`) بالبيئة المحفوظة في
  `einvoice_settings` — يأتي مع الجزء الثاني.
