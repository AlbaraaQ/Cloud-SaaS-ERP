namespace SmartAuditERP.Form_WPF
{
    public class SalaryRow
    {
        public int    RowNo      { get; set; }
        public int    SalId      { get; set; }
        public string EmpName    { get; set; } = "";
        public double TotSalary  { get; set; }
        public double Houses     { get; set; }
        public double Travel     { get; set; }
        public double SalaryAdd  { get; set; }
        public double GrossTotal { get; set; }
        public double SalarySub  { get; set; }
        public double SalaryNet  { get; set; }
    }
}