namespace SmartAuditERP.Form_WPF
{
    public class SerialSummaryRow
    {
        public int    DgvNo        { get; set; }
        public string ItemName     { get; set; } = "";
        public string ItemCode     { get; set; } = "";
        public string ItemSerialNo { get; set; } = "";
        public double SerialQty    { get; set; }
    }
}