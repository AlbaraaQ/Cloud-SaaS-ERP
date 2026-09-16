namespace SmartAuditERP.Form_WPF
{
    /// <summary>صف في جدول المواد المنتجة</summary>
    public class ProducedItemRow
    {
        public int    RowNum   { get; set; }
        public int    ItemId   { get; set; }
        public string ItemName { get; set; } = "";
        public string UnitName { get; set; } = "";
        public double Quantity { get; set; }
        public double Price    { get; set; }
        public double Total    { get; set; }
    }

    /// <summary>صف في جدول مكونات المنتج</summary>
    public class ComponentItemRow
    {
        public int    RowNum   { get; set; }
        public int    ItemId   { get; set; }
        public string ItemName { get; set; } = "";
        public string UnitName { get; set; } = "";
        public double Quantity { get; set; }
        public double Price    { get; set; }
        public double Total    { get; set; }
    }
}