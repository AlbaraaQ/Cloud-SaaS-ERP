namespace SmartAuditERP.Form_WPF
{
    // ─── FrmRptSalesChart - بيانات الرسوم البيانية ───
    // يستخدم DataView مباشرة - لا يحتاج Model مخصص

    // ─── frmRptSalesInPeriod ───

    /// <summary>صف في جدول الأصناف (Tab1)</summary>
    public class SaleItemRow
    {
        public int    ItemId   { get; set; }
        public string ItemName { get; set; } = "";
        public double Quantity { get; set; }
        public double Total    { get; set; }
    }

    /// <summary>صف في جدول الفواتير (Tab2)</summary>
    public class SaleInvoiceRow
    {
        public string InvGlobalID  { get; set; } = "";
        public string id           { get; set; } = "";
        public string ProcTypeName { get; set; } = "";
        public string InvDate      { get; set; } = "";
        public string InvTime      { get; set; } = "";
        public bool   IsPostpone   { get; set; }
        public double Cash         { get; set; }
        public double Visa         { get; set; }
        public double InvTotal     { get; set; }
        public double Tax          { get; set; }
        public double Minus        { get; set; }
        public double TotNet       { get; set; }
    }
}