namespace SmartAuditERP.Form_WPF
{
    public class SerialNoRow
    {
        public int    DgvNo        { get; set; }
        public string ItemName     { get; set; } = "";
        public string ItemCode     { get; set; } = "";
        public string ItemSerialNo { get; set; } = "";
        public string InvType      { get; set; } = "";
        public string InvNo        { get; set; } = "";
        public string InvDate      { get; set; } = "";
        public string BranchName   { get; set; } = "";
        public string InvGlobalID  { get; set; } = "";
        public int    ProcType     { get; set; }
        public int    InvTypeNo    { get; set; }
    }
}