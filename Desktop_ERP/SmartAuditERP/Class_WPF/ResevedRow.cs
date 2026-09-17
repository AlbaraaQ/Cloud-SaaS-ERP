namespace SmartAuditERP.Form_WPF
{
    public class ResevedRow
    {
        public int      DgvNo         { get; set; }
        public int      Id            { get; set; }
        public System.DateTime DateRes { get; set; }
        public string   EntryGlobalID { get; set; } = "";
        public string   GlobalID      { get; set; } = "";
        public string   branch        { get; set; } = "";
        public int      month         { get; set; }
        public int      year          { get; set; }
        public string   Notes         { get; set; } = "";
    }
}