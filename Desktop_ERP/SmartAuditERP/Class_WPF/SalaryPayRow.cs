namespace SmartAuditERP.Form_WPF
{
    public class SalaryPayRow
    {
        public int    PayId          { get; set; }
        public string EmpName        { get; set; } = "";
        public double NetAmount      { get; set; }
        public string Month          { get; set; } = "";
        public string Year           { get; set; } = "";
        public string PayDate        { get; set; } = "";
        public string ResponsibleEmp { get; set; } = "";
    }
}