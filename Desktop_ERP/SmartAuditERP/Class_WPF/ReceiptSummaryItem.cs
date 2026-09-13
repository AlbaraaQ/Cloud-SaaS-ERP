// ═══════════════════════════════════════════════════════════
//                     Model Classes
// ═══════════════════════════════════════════════════════════

namespace SmartAuditERP.Form_WPF
{
    /// <summary>نموذج صف جدول frmSummaryD</summary>
    public class ReceiptSummaryItem
    {
        public int    Rank           { get; set; }
        public int    ReceiptNo      { get; set; }
        public string OperDate       { get; set; }
        public double Payment        { get; set; }
        public double VAT            { get; set; }
        public double DgvNet         { get; set; }
        public string ReceiptNote    { get; set; }
        public string DgvreceiptType { get; set; }
        public string GlobalID       { get; set; }
        public string BranchName     { get; set; }
        public int    ReceiptType    { get; set; }
    }
}