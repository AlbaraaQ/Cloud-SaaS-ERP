using System;

namespace SmartAuditERP
{
    public class AccountingPeriod
    {
        public int PeriodID { get; set; }
        public string PeriodName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsClosed { get; set; }
        public string ClosedBy { get; set; }
        public DateTime? ClosedDate { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}