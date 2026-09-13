namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// يمثل صف واحد في تقرير أرباح المواد التجميعي
    /// </summary>
    public class ItemProfitRow
    {
        /// <summary>رقم تسلسلي</summary>
        public int    DgvNo          { get; set; }

        /// <summary>رمز المادة</summary>
        public string DgvItemCode    { get; set; } = "";

        /// <summary>اسم المادة</summary>
        public string DgvItem        { get; set; } = "";

        /// <summary>الكمية المباعة الصافية</summary>
        public double DgvQty         { get; set; }

        /// <summary>متوسط التكلفة / إجمالي التكلفة</summary>
        public double DgvAvgCost     { get; set; }

        /// <summary>صافي المبيعات بعد الخصم</summary>
        public double DgvNetSale     { get; set; }

        /// <summary>الربح = صافي البيع - التكلفة</summary>
        public double DgvProfit      { get; set; }

        /// <summary>نسبة الربح بصيغة "XX.XX%"</summary>
        public string DgvProfitRatio { get; set; } = "0%";

        /// <summary>معرف الصنف في قاعدة البيانات (للتفاصيل)</summary>
        public string DgvItemNo      { get; set; } = "";
    }
}