namespace SmartAuditERP.Form_WPF
{
    public class StockRow
    {
        public int    ItemId   { get; set; }
        public string ItemName { get; set; } = "";
        public double Qty      { get; set; }
        public double AvgCost  { get; set; }
        public double TotCost  { get; set; }
    }
}