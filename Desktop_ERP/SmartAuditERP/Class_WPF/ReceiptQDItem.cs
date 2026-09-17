// ═══════════════════════════════════════════════════════════
//                     Model Classes
// ═══════════════════════════════════════════════════════════

namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// نموذج صف جدول نتائج البحث في frmSandQD (dgvSrch)
    /// </summary>
    public class ReceiptQDItem
    {
        /// <summary>المعرف العالمي</summary>
        public string GlobalId     { get; set; }

        /// <summary>رقم السند</summary>
        public string ReceiptNo    { get; set; }

        /// <summary>تاريخ السند (نص منسق)</summary>
        public string ReceiptDate  { get; set; }

        /// <summary>قيمة السند</summary>
        public string Payment      { get; set; }

        /// <summary>اسم الموظف</summary>
        public string EmployeeName { get; set; }
    }
}