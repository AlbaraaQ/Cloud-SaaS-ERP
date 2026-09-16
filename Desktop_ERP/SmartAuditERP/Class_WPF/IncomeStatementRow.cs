namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// نموذج صف في جدول قائمة الدخل
    /// </summary>
    public class IncomeStatementRow
    {
        /// <summary>المجموعة (الإيرادات / تكلفة البضاعة / المصروفات...)</summary>
        public string Category { get; set; }

        /// <summary>رقم الحساب</summary>
        public string AccountCode { get; set; }

        /// <summary>اسم الحساب</summary>
        public string AccountName { get; set; }

        /// <summary>الرصيد كرقم</summary>
        public decimal Balance { get; set; }

        /// <summary>الرصيد منسّق للعرض</summary>
        public string BalanceFormatted { get; set; }

        /// <summary>ملاحظات</summary>
        public string Notes { get; set; }

        /// <summary>
        /// لون الصف (LightYellow / LightGreen / LightCyan / LightBlue / Normal)
        /// يُستخدم في DataTriggers بـ XAML
        /// </summary>
        public string RowColor { get; set; }
    }
}