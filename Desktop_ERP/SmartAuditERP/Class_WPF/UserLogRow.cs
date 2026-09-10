namespace SmartAuditERP.Form_WPF
{
    public class UserLogRow
    {
        public int      Id      { get; set; }
        public System.DateTime LogDate { get; set; }
        public string   Logger  { get; set; } = "";
        public string   Message { get; set; } = "";
        public string   EmpName { get; set; } = "";
    }
}