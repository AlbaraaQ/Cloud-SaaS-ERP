namespace SmartAuditERP.Form_WPF
{
    public class DbRow
    {
        public int    DbId       { get; set; }
        public string AutoName   { get; set; } = "";
        public string DbName     { get; set; } = "";
        public string CreateDate { get; set; } = "";
        public bool   IsActive   { get; set; }
    }
}