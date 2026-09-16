// ═══════════════════════════════════════════════════════════
//                     Model Classes
// ═══════════════════════════════════════════════════════════

using System.Data.SqlClient;

namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// نموذج بيانات نتائج البحث في جدول dgvSrch
    /// </summary>
    public class ReceiptSearchItem
    {
        /// <summary>المعرف العالمي للسند</summary>
        public string GlobalId { get; set; }

        /// <summary>رقم السند</summary>
        public string ReceiptNo { get; set; }

        /// <summary>تاريخ السند (نص منسق)</summary>
        public string ReceiptDate { get; set; }

        /// <summary>قيمة السند</summary>
        public string Payment { get; set; }

        /// <summary>اسم مندوب المبيعات</summary>
        public string SalesManName { get; set; }
    }

    /// <summary>
    /// نموذج بيانات طباعة السند
    /// (يُستخدم لتمرير البيانات لمحرك التقارير)
    /// </summary>
    public class BondPrintModel
    {
        public string BondName        { get; set; }
        public string PayTo           { get; set; }
        public string PayToCode       { get; set; }
        public string Payfrom         { get; set; }
        public string PayFromCode     { get; set; }
        public string CostCenter      { get; set; }
        public string BondVal         { get; set; }
        public string BondDate        { get; set; }
        public string BondNo          { get; set; }
        public string PrintDate       { get; set; }
        public string Note            { get; set; }
        public string BondType        { get; set; }
        public string SalesMan        { get; set; }
        public string User            { get; set; }
        public string ArabicLetter    { get; set; }
        public string ReffNo          { get; set; }
        public string ReffDate        { get; set; }
        public string Branch          { get; set; }
        public string Address         { get; set; }
        public string Mobile          { get; set; }
        public string Telephone       { get; set; }
        public string VATNo           { get; set; }
        public string Foundation      { get; set; }
        public string Field           { get; set; }
        public string Logo            { get; set; }
        public string Header          { get; set; }
        public string Footer          { get; set; }
        public string Stamp           { get; set; }
    }

    /// <summary>
    /// محاكاة لـ MainClass (يجب تعديلها لتتوافق مع MainClass الفعلي في المشروع)
    /// </summary>
    internal static class MainClass1
    {
        public static SqlConnection ConnObj()
            => new SqlConnection("يجب وضع connection string هنا");

        public static int    BranchNo        { get; set; } = -1;
        public static int    EmpNo           { get; set; } = -1;
        public static string UserName        { get; set; } = "";
        public static string BranchName      { get; set; } = "";
        public static string ReportsPath     { get; set; } = "";
        public static string ReportsPrinter  { get; set; } = "";
        public static string DefaultPrinter  { get; set; } = "";
        public static string Language        { get; set; } = "ar";
    }
}