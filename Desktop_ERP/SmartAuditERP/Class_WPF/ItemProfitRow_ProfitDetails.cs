namespace SmartAuditERP.Form_WPF
{
    public class ItemProfitRow_ProfitDetails
    {
        public int    DgvNo         { get; set; }
        public string DgvStore      { get; set; } = "";
        public string DgvInvType    { get; set; } = "";
        public string DgvDate       { get; set; } = "";
        public int    DgvInvNo      { get; set; }
        public string DgvItemCode   { get; set; } = "";
        public string DgvItem       { get; set; } = "";
        public string DgvUnit       { get; set; } = "";
        public double DgvQty        { get; set; }
        public double DgvAvgCost    { get; set; }
        public double DgvTotAvg     { get; set; }
        public double DgvPrice      { get; set; }
        public double DgvTotal      { get; set; }
        public double DgvDiscount   { get; set; }
        public double DgvSum        { get; set; }
        public double DgvProfit     { get; set; }
        public string DgvProfitPerc { get; set; } = "";
        public string DgvInvGlobalID { get; set; } = "";
        public int    DgvIsPlus     { get; set; }
    }
}