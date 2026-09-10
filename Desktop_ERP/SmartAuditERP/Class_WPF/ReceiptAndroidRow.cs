namespace SmartAuditERP.Form_WPF
{
    public class ReceiptAndroidRow
    {
        public string GlobalId    { get; set; } = "";
        public int    ReceiptNo   { get; set; }
        public string ReceiptDate { get; set; } = "";
        public double Payment     { get; set; }
        public string ClientName  { get; set; } = "";
    }
}