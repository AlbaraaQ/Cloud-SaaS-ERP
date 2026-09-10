namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// نموذج صف حركة الصندوق
    /// </summary>
    public class KhznaRow
    {
        public int    RowNo       { get; set; }
        public string ProcessType { get; set; } = "";
        public string ProcessNo   { get; set; } = "";
        public string RestDate    { get; set; } = "";
        public double Income      { get; set; }
        public double Outcome     { get; set; }
        public double Balance     { get; set; }
        public string Note        { get; set; } = "";
    }
}