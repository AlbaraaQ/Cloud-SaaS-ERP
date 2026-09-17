using System.ComponentModel;

namespace SmartAuditERP
{
    public class InvoiceData
    {
        #region Numeric Backing Fields

        private double _contractTotalValue;
        private double _originalWorkAmount;
        private double _specialDiscount;
        private double _amountAfterDiscount;
        private double _vatAmount;
        private double _totalWithVat;
        private double _workGuarantee;
        private double _netDueThisPayment;
        private double _previouslyPaidAmount;
        private double _remainingContractBalance;

        #endregion

        [DisplayName("الرقم العام ")]
        public string InvGlobalID { get; set; } = string.Empty;

        [DisplayName("كيو ار")]
        public string QRCode { get; set; } = string.Empty;

        [DisplayName("تاريخ الانتهاء")]
        public string ItemExpireDate { get; set; } = string.Empty;

        public string WithholdingTaxPerc { get; set; } = string.Empty;

        public string WithholdingTax { get; set; } = string.Empty;

        public string TotalWithholdingTax { get; set; } = string.Empty;

        [DisplayName("رقم الطاولة")]
        public string tableNo { get; set; } = string.Empty;

        public string InvoiceTypeAr { get; set; } = string.Empty;

        public string InvoiceTypeEn { get; set; } = string.Empty;

        [DisplayName("نسبة الضريبة الإضافية للصنف")]
        public string ItemAdditionalTaxPerc { get; set; } = string.Empty;

        [DisplayName("الضريبة الإضافية للصنف")]
        public string ItemAdditionalTax { get; set; } = string.Empty;

        public string CustCurrentBalance { get; set; } = string.Empty;

        [DisplayName("رصيد العميل قبل")]
        public string CustBalancePerv { get; set; } = string.Empty;

        [DisplayName("العملة")]
        public string currency { get; set; } = string.Empty;

        [DisplayName("جوال العميل النقدي")]
        public string CashCustomerMobile { get; set; } = string.Empty;

        [DisplayName("اسم العميل النقدي")]
        public string CashCustomerName { get; set; } = string.Empty;

        [DisplayName("مركز التكلفة للصنف")]
        public string ItemCostCenter { get; set; } = string.Empty;

        [DisplayName("اجمالي الصنف بعد الخصم")]
        public string ItemPriceAfterDiscount { get; set; } = string.Empty;

        [DisplayName("الخصم للصنف")]
        public string ItemPriceDiscount { get; set; } = string.Empty;

        [DisplayName("رقم الفاتورة")]
        public string ID { get; set; } = string.Empty;

        [DisplayName("سيريال كلاود")]
        public string CloudID { get; set; } = string.Empty;

        [DisplayName("الرمز البريدي للعميل")]
        public string CustPostalZone { get; set; } = string.Empty;

        [DisplayName("الحي للعميل")]
        public string CustDistrict { get; set; } = string.Empty;

        [DisplayName(" اسم شارع اخر للعميل")]
        public string CustAdditionalStreetName { get; set; } = string.Empty;

        [DisplayName("اسم الشارع للعميل")]
        public string CustStreetName { get; set; } = string.Empty;

        [DisplayName(" رقم المبنى للعميل")]
        public string CustBuildingNumber { get; set; } = string.Empty;

        [DisplayName("الرقم الفرعي للعميل")]
        public string CustPlotIdentification { get; set; } = string.Empty;

        [DisplayName("المنطقة")]
        public string Area { get; set; } = string.Empty;

        [DisplayName("الموقع الالكتروني")]
        public string website { get; set; } = string.Empty;

        [DisplayName("الايميل")]
        public string Email { get; set; } = string.Empty;

        [DisplayName("المدينة")]
        public string city { get; set; } = string.Empty;

        [DisplayName("الدولة")]
        public string country { get; set; } = string.Empty;

        [DisplayName("السجل التجاري")]
        public string bsn_no { get; set; } = string.Empty;

        [DisplayName("النشاط التجاري")]
        public string FieldE { get; set; } = string.Empty;

        [DisplayName("الاسم انجلش")]
        public string nameE { get; set; } = string.Empty;

        [DisplayName("السجل التجاري للعميل")]
        public string custCrNo { get; set; } = string.Empty;

        [DisplayName(" الرمز البريدي ")]
        public string PostalZone { get; set; } = string.Empty;

        [DisplayName("الحي ")]
        public string District { get; set; } = string.Empty;

        [DisplayName("اسم شارع اخر   ")]
        public string AdditionalStreetName { get; set; } = string.Empty;

        [DisplayName("اسم الشارع")]
        public string StreetName { get; set; } = string.Empty;

        [DisplayName("رقم المبنى")]
        public string BuildingNumber { get; set; } = string.Empty;

        [DisplayName("الرقم الفرعي")]
        public string PlotIdentification { get; set; } = string.Empty;

        [DisplayName("اسم الفرع")]
        public string BranchName { get; set; } = string.Empty;

        [DisplayName("2الكمية")]
        public string Qty2 { get; set; } = string.Empty;

        [DisplayName("الكمية1")]
        public string Qty1 { get; set; } = string.Empty;

        [DisplayName("اجمالي الكمية")]
        public string TotalQty { get; set; } = string.Empty;

        [DisplayName("مركز التكلفة")]
        public string costCenter { get; set; } = string.Empty;

        [DisplayName("الايميل")]
        public string LeIPD { get; set; } = string.Empty;

        public string LeADD { get; set; } = string.Empty;

        public string LeAX { get; set; } = string.Empty;

        public string LeCYL { get; set; } = string.Empty;

        public string LeSPH { get; set; } = string.Empty;

        public string ReIPD { get; set; } = string.Empty;

        public string ReADD { get; set; } = string.Empty;

        public string ReAX { get; set; } = string.Empty;

        public string ReCYL { get; set; } = string.Empty;

        public string ReSPH { get; set; } = string.Empty;

        [DisplayName("رقم السيريال للصنف")]
        public string ItemSerialNo { get; set; } = string.Empty;

        [DisplayName("ملاحظة الصنف")]
        public string ItemNotes { get; set; } = string.Empty;

        [DisplayName("ضريبة اضافية")]
        public string AdditiontalTax { get; set; } = string.Empty;

        [DisplayName("رمز المجموعة")]
        public string ItemGroupCode { get; set; } = string.Empty;

        [DisplayName("مجموعة الصنف انجلش")]
        public string ItemGroupEn { get; set; } = string.Empty;

        [DisplayName("مجموعة الصنف")]
        public string ItemGroup { get; set; } = string.Empty;

        [DisplayName("اسم الموظف")]
        public string EmpName { get; set; } = string.Empty;

        [DisplayName("التفقيط انجلش")]
        public string ToEnWords { get; set; } = string.Empty;

        [DisplayName("رقم الصنف")]
        public string ItemID { get; set; } = string.Empty;

        [DisplayName("السعر الافرادي للصنف بدون ضريبة")]
        public string ItemPriceWithoutVAT { get; set; } = string.Empty;

        [DisplayName("اجمالي الصنف قبل الضريبة")]
        public string ItemTotalWithoutVAT { get; set; } = string.Empty;

        [DisplayName("اسم الصنف انجلش")]
        public string ItemNameEn { get; set; } = string.Empty;

        [DisplayName("تاريخ المرجع")]
        public string RefDate { get; set; } = string.Empty;

        [DisplayName("ملاحظة العميل")]
        public string CustNote { get; set; } = string.Empty;

        [DisplayName("المنطقة للعميل")]
        public string CustArea { get; set; } = string.Empty;

        [DisplayName("الدولة للعميل")]
        public string Custcountry { get; set; } = string.Empty;

        [DisplayName("المدينة للعميل")]
        public string CustCity { get; set; } = string.Empty;

        [DisplayName("المستودع")]
        public string Store { get; set; } = string.Empty;

        [DisplayName("الكمية الحالية للصنف")]
        public string CurrentQty { get; set; } = string.Empty;

        [DisplayName("الصافي للصنف")]
        public string NetTable { get; set; } = string.Empty;

        [DisplayName("نسبة الخصم للصنف")]
        public string DiscPerc { get; set; } = string.Empty;

        [DisplayName("نسبة الضريبة للصنف")]
        public string TaxPerc { get; set; } = string.Empty;

        [DisplayName(" الضريبة للصنف")]
        public string Tax { get; set; } = string.Empty;

        [DisplayName("متوسط التكلفة")]
        public string AvegCost { get; set; } = string.Empty;

        [DisplayName("المجموع")]
        public string sum { get; set; } = string.Empty;

        [DisplayName("التفقيط عربي")]
        public string ArabicLetter { get; set; } = string.Empty;

        [DisplayName("التعادل ")]
        public string UnitEquality { get; set; } = string.Empty;

        [DisplayName("اجمالي الكمية")]
        public string EssitialQnty { get; set; } = string.Empty;

        [DisplayName("اسم الصنف")]
        public string ItemName { get; set; } = string.Empty;

        [DisplayName("الوصف")]
        public string Description { get; set; } = string.Empty;

        [DisplayName("رمز الصنف")]
        public string ItemNo { get; set; } = string.Empty;

        [DisplayName("الكمية")]
        public string Quantity { get; set; } = string.Empty;

        [DisplayName("الوحدة")]
        public string Unit { get; set; } = string.Empty;

        [DisplayName("السعر")]
        public string Price { get; set; } = string.Empty;

        [DisplayName("الباركود")]
        public string Barcode { get; set; } = string.Empty;

        [DisplayName("المجموع")]
        public string Total { get; set; } = string.Empty;

        [DisplayName("الخصم للصنف")]
        public string SubDiscount { get; set; } = string.Empty;

        [DisplayName("الاجمالي للفاتورة")]
        public string TotalWithoutVAT { get; set; } = string.Empty;

        [DisplayName("الصافي للفاتورة")]
        public string Net { get; set; } = string.Empty;

        [DisplayName("المجموع للفاتورة")]
        public string SumPrice { get; set; } = string.Empty;

        [DisplayName("الضريبة للفاتورة")]
        public string VAT { get; set; } = string.Empty;

        [DisplayName("الباقي")]
        public string Remainder { get; set; } = string.Empty;

        [DisplayName("مدفوع شبكة")]
        public string PayNetwork { get; set; } = string.Empty;

        [DisplayName("مدفوع نقدي")]
        public string Paycash { get; set; } = string.Empty;

        [DisplayName("التأمين")]
        public string Insurance { get; set; } = string.Empty;

        [DisplayName("التوصيل")]
        public string Additions { get; set; } = string.Empty;

        [DisplayName("الخصم للفاتورة")]
        public string Discount { get; set; } = string.Empty;

        [DisplayName("رقم الفاتورة ترميز")]
        public string InvoiceNo { get; set; } = string.Empty;

        [DisplayName("نوع الفاتورة")]
        public string InvoiceType { get; set; } = string.Empty;

        [DisplayName("رقم الطلب")]
        public string OrderNo { get; set; } = string.Empty;

        [DisplayName("نوع الطلب")]
        public string OrderType { get; set; } = string.Empty;

        [DisplayName("تاريخ الفاتورة")]
        public string InvDate { get; set; } = string.Empty;

        // تم الإبقاء على خاصية واحدة فقط باسم InvTime لأن تكرارها يمنع الترجمة
        [DisplayName("وقت الفاتورة")]
        public string InvTime { get; set; } = string.Empty;

        [DisplayName("المستخدم")]
        public string User { get; set; } = string.Empty;

        [DisplayName("المندوب")]
        public string Saleman { get; set; } = string.Empty;

        [DisplayName("ملاحظات الفاتورة")]
        public string InvNote { get; set; } = string.Empty;

        [DisplayName("اسم العميل")]
        public string Customer { get; set; } = string.Empty;

        [DisplayName("الرقم الضريبي للعميل")]
        public string CustVATno { get; set; } = string.Empty;

        [DisplayName("عنوان المنشأة ")]
        public string Address { get; set; } = string.Empty;

        public string Stamp { get; set; } = string.Empty;

        [DisplayName("الشعار")]
        public string Logo { get; set; } = string.Empty;

        [DisplayName("جوال المنشأة")]
        public string Mobile { get; set; } = string.Empty;

        [DisplayName("تيلفون المنشأة")]
        public string TelePhone { get; set; } = string.Empty;

        [DisplayName("الرقم الضريبي للمنشأة")]
        public string VatNo { get; set; } = string.Empty;

        [DisplayName("نسبة الضريبة")]
        public string VATPerc { get; set; } = string.Empty;

        [DisplayName("طريقة الدفع")]
        public string PayType { get; set; } = string.Empty;

        [DisplayName("اسم المنشأة")]
        public string Foundation { get; set; } = string.Empty;

        [DisplayName("الترويسة")]
        public string Header { get; set; } = string.Empty;

        [DisplayName("التذييل")]
        public string footer { get; set; } = string.Empty;

        [DisplayName("النشاط التجاري")]
        public string Field { get; set; } = string.Empty;

        [DisplayName("ملاحظات طباعة")]
        public string PrintNote { get; set; } = string.Empty;

        [DisplayName("الطابعة")]
        public string Printer { get; set; } = string.Empty;

        [DisplayName("جوال العميل")]
        public string CustMobile { get; set; } = string.Empty;

        [DisplayName("رقم الهوية للعميل")]
        public string CustNatianalID { get; set; } = string.Empty;

        [DisplayName("رقم حساب العميل")]
        public string CustAccCode { get; set; } = string.Empty;

        [DisplayName("الاجمالي")]
        public string CustCompanions { get; set; } = string.Empty;

        public string Rent { get; set; } = string.Empty;

        [DisplayName("رقم المرجع")]
        public string ReffNo { get; set; } = string.Empty;

        [DisplayName("ارتفاع الصنف")]
        public string ItemHeight { get; set; } = string.Empty;

        [DisplayName("عرض الصنف")]
        public string ItemWidth { get; set; } = string.Empty;

        [DisplayName("تاريخ الانتاج")]
        public string ProDate { get; set; } = string.Empty;

        [DisplayName("تاريخ الانتهاء")]
        public string ExpDate { get; set; } = string.Empty;

        [DisplayName("الرصيد السابق")]
        public string BalancePreviews { get; set; } = string.Empty;

        public decimal TotalNoTax { get; set; }

        public string contract_total_value
        {
            get => _contractTotalValue.ToString();
            set
            {
                double.TryParse(value, out _contractTotalValue);
            }
        }

        public string original_work_amount
        {
            get => _originalWorkAmount.ToString();
            set
            {
                double.TryParse(value, out _originalWorkAmount);
            }
        }

        public string special_discount
        {
            get => _specialDiscount.ToString();
            set
            {
                double.TryParse(value, out _specialDiscount);
            }
        }

        public string amount_after_discount
        {
            get => _amountAfterDiscount.ToString();
            set
            {
                double.TryParse(value, out _amountAfterDiscount);
            }
        }

        public string vat_amount
        {
            get => _vatAmount.ToString();
            set
            {
                double.TryParse(value, out _vatAmount);
            }
        }

        public string total_with_vat
        {
            get => _totalWithVat.ToString();
            set
            {
                double.TryParse(value, out _totalWithVat);
            }
        }

        public string work_guarantee
        {
            get => _workGuarantee.ToString();
            set
            {
                double.TryParse(value, out _workGuarantee);
            }
        }

        public string net_due_this_payment
        {
            get => _netDueThisPayment.ToString();
            set
            {
                double.TryParse(value, out _netDueThisPayment);
            }
        }

        public string previously_paid_amount
        {
            get => _previouslyPaidAmount.ToString();
            set
            {
                double.TryParse(value, out _previouslyPaidAmount);
            }
        }

        public string remaining_contract_balance
        {
            get => _remainingContractBalance.ToString();
            set
            {
                double.TryParse(value, out _remainingContractBalance);
            }
        }

        [DisplayName("الطول خياط")]
        public string Length { get; set; } = string.Empty;

        [DisplayName("الكتف")]
        public string shoulder { get; set; } = string.Empty;

        [DisplayName("طول اليد")]
        public string chest { get; set; } = string.Empty;

        public string Waist { get; set; } = string.Empty;

        public string Neck { get; set; } = string.Empty;

        public string Sleeve { get; set; } = string.Empty;

        public string ArmWidth { get; set; } = string.Empty;

        public string StepWidth { get; set; } = string.Empty;

        public string pageNo { get; set; } = string.Empty;

        public string MeasurementNote { get; set; } = string.Empty;

        public string ItemColor { get; set; } = string.Empty;

        public string ItemSize { get; set; } = string.Empty;

        public string ItemIncrId { get; set; } = string.Empty;

        public string BatchNo { get; set; } = string.Empty;

        public string safeFrom { get; set; } = string.Empty;

        public string safeTo { get; set; } = string.Empty;
    }
}