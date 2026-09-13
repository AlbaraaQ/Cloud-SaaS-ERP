namespace SmartAuditERP.Form_WPF
{
    public class ItemSalesPosRow
    {
        public int    DgvNo               { get; set; }
        public string DgvItemNo           { get; set; } = "";
        public string DgvItemCode         { get; set; } = "";
        public string DgvItem             { get; set; } = "";
        public double DgvQty              { get; set; }
        public double DgvNetSales         { get; set; }
        public double DgvSaleRatio        { get; set; }
        public string DgvCategory         { get; set; } = "";
        public double DgvItemAdditionalTax{ get; set; }
    }
}