// ═══════════════════════════════════════════════════════════
//                     Model Classes
// ═══════════════════════════════════════════════════════════

namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// نموذج صف جدول نتائج البحث (dgvSrch)
    /// </summary>
    public class ReceiptSrchItem
    {
        /// <summary>المعرف العالمي للسند</summary>
        public string GlobalId     { get; set; }
        /// <summary>رقم السند</summary>
        public string ReceiptNo    { get; set; }
        /// <summary>تاريخ السند (نص منسق)</summary>
        public string ReceiptDate  { get; set; }
        /// <summary>المبلغ</summary>
        public string Payment      { get; set; }
        /// <summary>اسم العميل</summary>
        public string ClientName   { get; set; }
    }
}