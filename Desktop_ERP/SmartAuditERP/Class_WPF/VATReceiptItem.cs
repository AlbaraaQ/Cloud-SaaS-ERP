// ═══════════════════════════════════════════════════════════
//                     Model Classes
// ═══════════════════════════════════════════════════════════

namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// نموذج صف جدول نتائج البحث في frmSandVAT (dgvSrch)
    /// </summary>
    public class VATReceiptItem
    {
        /// <summary>المعرف العالمي للسند</summary>
        public string GlobalId      { get; set; }

        /// <summary>رقم السند</summary>
        public string ReceiptNo     { get; set; }

        /// <summary>تاريخ السند (نص منسق)</summary>
        public string ReceiptDate   { get; set; }

        /// <summary>قيمة السند</summary>
        public string Payment       { get; set; }

        /// <summary>اسم المورد</summary>
        public string SupplierName  { get; set; }
    }
}