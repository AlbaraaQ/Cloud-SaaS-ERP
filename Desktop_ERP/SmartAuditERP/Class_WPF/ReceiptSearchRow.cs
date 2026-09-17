namespace SmartAuditERP.Form_WPF
{
    public class ReceiptSearchRow
    {
        public string GlobalId    { get; set; } = "";
        public int    ReceiptNo   { get; set; }
        public string ReceiptDate { get; set; } = "";
        public double Payment     { get; set; }
        public string UserName    { get; set; } = "";
    }
}