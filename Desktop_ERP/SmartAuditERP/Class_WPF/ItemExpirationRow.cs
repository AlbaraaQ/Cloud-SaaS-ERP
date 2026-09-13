using System;

namespace SmartAuditERP.Form_WPF
{
    public class ItemExpirationRow
    {
        public int      AutoIncrementID { get; set; }
        public string   ItemCode        { get; set; } = "";
        public string   ItemName        { get; set; } = "";
        public string   StoreName       { get; set; } = "";
        public double   ItemStock       { get; set; }
        public DateTime? ItemExpire     { get; set; }
        public int      RemaindeYear    { get; set; }
        public int      RemaindeMonths  { get; set; }
        public int      RemaindeDays    { get; set; }
    }
}