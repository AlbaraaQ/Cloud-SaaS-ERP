// ═══════════════════════════════════════════════════════════════
//  ملف منفصل: ItemActivityRow.cs  (أو في نهاية frmRptItemsActivity.xaml.cs)
// ═══════════════════════════════════════════════════════════════

namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// يمثل صف واحد في جدول إجمالي حركات الأصناف
    /// </summary>
    public class ItemActivityRow
    {
        /// <summary>رمز الصنف</summary>
        public string DgvItemCode   { get; set; } = "";

        /// <summary>اسم الصنف</summary>
        public string DgvItemName   { get; set; } = "";

        /// <summary>الرصيد الحالي</summary>
        public double DgvBalnce     { get; set; }

        /// <summary>متوسط سعر التكلفة</summary>
        public double DgvAvgCost    { get; set; }

        /// <summary>إجمالي التكلفة (الرصيد × متوسط التكلفة)</summary>
        public double DgvTotalCost  { get; set; }

        /// <summary>بضاعة أول المدة</summary>
        public double DgvFirstVal   { get; set; }

        /// <summary>إجمالي المشتريات</summary>
        public double DgvPurch      { get; set; }

        /// <summary>إجمالي مرتجع المشتريات</summary>
        public double DgvRePurch    { get; set; }

        /// <summary>إجمالي المبيعات</summary>
        public double DgvSales      { get; set; }

        /// <summary>إجمالي مرتجع المبيعات</summary>
        public double DgvReSales    { get; set; }

        /// <summary>إجمالي نقطة البيع</summary>
        public double DgvPOS        { get; set; }

        /// <summary>إجمالي مرتجع نقطة البيع</summary>
        public double DgvRePOS      { get; set; }

        /// <summary>مناقلة مرسلة (تحويل للخارج)</summary>
        public double DgvTransTo    { get; set; }

        /// <summary>مناقلة مستلمة (تحويل من الخارج)</summary>
        public double DgvTransFrom  { get; set; }

        /// <summary>فاتورة إدخال</summary>
        public double DgvEntryInv   { get; set; }

        /// <summary>فاتورة إخراج</summary>
        public double DgvOutInv     { get; set; }

        /// <summary>تسوية جردية - إدخال</summary>
        public double DgvSafeAdj    { get; set; }

        /// <summary>تسوية جردية - إخراج</summary>
        public double DgvSafeAdjRet { get; set; }
    }
}