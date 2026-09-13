// ═══════════════════════════════════════════════════════════
//                     Model Classes
// ═══════════════════════════════════════════════════════════

namespace SmartAuditERP.Form_WPF
{
    /// <summary>نموذج صف جدول frmStocksSync</summary>
    public class StockSyncItem
    {
        public int    RowNo         { get; set; }
        public int    ProductId     { get; set; }
        public string ItemCode      { get; set; }
        public string ItemName      { get; set; }
        public string UnitName      { get; set; }
        public double Quantity      { get; set; }
        public string BranchName    { get; set; }
        public string InventoryName { get; set; }
        public string LastUpdate    { get; set; }
    }

    /// <summary>نموذج صف جدول frmSummaryQ</summary>
    public class ReceiptQItem
    {
        public int     AutoIncrementID { get; set; }
        public decimal Recieptvalue    { get; set; }
        public string  RecieptDate     { get; set; }
        public int     Recieptno       { get; set; }
        public string  ReciptType      { get; set; }
        public string  ClientTxt       { get; set; }
        public string  PayType         { get; set; }
        public string  SalesmanTxt     { get; set; }
        public string  Recieptnote     { get; set; }
        public string  BranchTxt       { get; set; }
        public string  UserTxt         { get; set; }
        public string  GlobalID        { get; set; }
        public int     Typeno          { get; set; }
    }
}