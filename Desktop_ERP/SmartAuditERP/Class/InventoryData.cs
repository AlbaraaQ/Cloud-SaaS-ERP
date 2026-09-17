using System.ComponentModel;

namespace SmartAuditERP
{
    public class InventoryData
    {
        public string SummaryNetwork { get; set; } = string.Empty;
        public string SummaryCash { get; set; } = string.Empty;
        public string ItemAdditionalTax { get; set; } = string.Empty;
        public string SummaryItemAdditionalTax { get; set; } = string.Empty;
        public string SummaryRemainder { get; set; } = string.Empty;
        public string SummaryPaid { get; set; } = string.Empty;
        public string Remainder { get; set; } = string.Empty;
        public string Paid { get; set; } = string.Empty;

        [DisplayName("الاجمالي قبل الضريبة")]
        public string SummaryTotalBeforeTax { get; set; } = string.Empty;

        [DisplayName(" مجموع الخصم للصنف")]
        public string SummaryItemDiscount { get; set; } = string.Empty;

        [DisplayName("المجموع للصنف")]
        public string SummaryItemSum { get; set; } = string.Empty;

        [DisplayName("خصم الصنف")]
        public string ItemDiscount { get; set; } = string.Empty;

        [DisplayName("مجموع الصنف")]
        public string ItemSum { get; set; } = string.Empty;

        [DisplayName("اجمالي الضريبة")]
        public string SummaryTotalTax { get; set; } = string.Empty;

        [DisplayName("")]
        public string SummaryExtraTax { get; set; } = string.Empty;

        [DisplayName("إجمالي الضريبة")]
        public string TotalTax { get; set; } = string.Empty;

        [DisplayName("")]
        public string ExtraTax { get; set; } = string.Empty;

        [DisplayName("رقم السيريال")]
        public string ItemSerialNo { get; set; } = string.Empty;

        [DisplayName("طريقة الدفع")]
        public string Paytype { get; set; } = string.Empty;

        [DisplayName("")]
        public string InvDay { get; set; } = string.Empty;

        [DisplayName("اسم الصنف انجلش")]
        public string ItemNameEn { get; set; } = string.Empty;

        [DisplayName("")]
        public string RemaindeDays { get; set; } = string.Empty;

        public string RemaindeMonths { get; set; } = string.Empty;

        [DisplayName("")]
        public string RemaindeYear { get; set; } = string.Empty;

        [DisplayName("اسم الفرع")]
        public string BranchName { get; set; } = string.Empty;

        [DisplayName("الصافي")]
        public string Net { get; set; } = string.Empty;

        [DisplayName("الفرق")]
        public string Diff { get; set; } = string.Empty;

        [DisplayName("النسبة")]
        public string Ratio { get; set; } = string.Empty;

        [DisplayName("")]
        public string VATFilter { get; set; } = string.Empty;

        [DisplayName("العميل")]
        public string ClientFilter { get; set; } = string.Empty;

        [DisplayName("المندوب")]
        public string SalemanFilter { get; set; } = string.Empty;

        [DisplayName("نوع الدفع")]
        public string PayTypeFilter { get; set; } = string.Empty;

        [DisplayName("المستودع")]
        public string StoreFilter { get; set; } = string.Empty;

        [DisplayName("نوع الفاتورة")]
        public string InvTypeFilter { get; set; } = string.Empty;

        public string FilterText { get; set; } = string.Empty;
        public string AdjustOut { get; set; } = string.Empty;
        public string AdjustIn { get; set; } = string.Empty;

        [DisplayName("فاتورة إخراج")]
        public string OutputInv { get; set; } = string.Empty;

        [DisplayName("فاتورة ادخال")]
        public string InputInv { get; set; } = string.Empty;

        public string TransTo { get; set; } = string.Empty;
        public string TransFrom { get; set; } = string.Empty;
        public string RePos { get; set; } = string.Empty;
        public string Pos { get; set; } = string.Empty;
        public string OperDate { get; set; } = string.Empty;
        public string ReceivedQnty { get; set; } = string.Empty;
        public string SentQnty { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string SafeTo { get; set; } = string.Empty;
        public string SafeFrom { get; set; } = string.Empty;
        public string Profit_Comm { get; set; } = string.Empty;
        public string Colle_Comm { get; set; } = string.Empty;
        public string comm { get; set; } = string.Empty;
        public string NetBeforeTax { get; set; } = string.Empty;

        [DisplayName("مجموع الضريبة")]
        public string SumTax { get; set; } = string.Empty;

        [DisplayName("الرقم الضريبي للعميل")]
        public string ClintVatNo { get; set; } = string.Empty;

        [DisplayName("رقم فاتورة المورد")]
        public string RefsInvNo { get; set; } = string.Empty;

        [DisplayName("وقت الفاتورة")]
        public string InvTime { get; set; } = string.Empty;

        [DisplayName("الضريبة")]
        public string Tax { get; set; } = string.Empty;

        [DisplayName("شبكة")]
        public string Network { get; set; } = string.Empty;

        [DisplayName("كاش")]
        public string Cash { get; set; } = string.Empty;

        [DisplayName("نوع الفاتورة")]
        public string InvType { get; set; } = string.Empty;

        [DisplayName("صافي الشراء")]
        public string TotPurch { get; set; } = string.Empty;

        [DisplayName("مجموع الاجمالي")]
        public string Sum { get; set; } = string.Empty;

        [DisplayName("المورد")]
        public string Supplier { get; set; } = string.Empty;

        public string Process { get; set; } = string.Empty;

        [DisplayName("اسم الموظف")]
        public string EmpName { get; set; } = string.Empty;

        public string TurnoverRate { get; set; } = string.Empty;
        public string RecessionRate { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public string Income { get; set; } = string.Empty;

        [DisplayName("الصافي قبل الضريبة")]
        public string Total1 { get; set; } = string.Empty;

        public string Quantity2 { get; set; } = string.Empty;
        public string Quantity1 { get; set; } = string.Empty;
        public string SumNetProfit { get; set; } = string.Empty;

        [DisplayName("مجموع الخصم")]
        public string SumNetDiscount { get; set; } = string.Empty;

        public string SumNetQuantity { get; set; } = string.Empty;
        public string SafeMainCode { get; set; } = string.Empty;
        public string SafeMain { get; set; } = string.Empty;
        public string StockCode { get; set; } = string.Empty;
        public string StockName { get; set; } = string.Empty;

        [DisplayName("العميل")]
        public string Clint { get; set; } = string.Empty;

        public string ProfitToGrossProfitRatio { get; set; } = string.Empty;
        public string PercTotSale { get; set; } = string.Empty;
        public string ProfitCostRatio { get; set; } = string.Empty;
        public string NetProfit { get; set; } = string.Empty;
        public string NetCost { get; set; } = string.Empty;

        [DisplayName("الخصم")]
        public string NetDiscount { get; set; } = string.Empty;

        [DisplayName("الصافي")]
        public string NetTotal { get; set; } = string.Empty;

        public string NetQuantity { get; set; } = string.Empty;
        public string Sale { get; set; } = string.Empty;
        public string Purch { get; set; } = string.Empty;
        public string ReturnPurch { get; set; } = string.Empty;
        public string ReturnSell { get; set; } = string.Empty;
        public string FirstStock { get; set; } = string.Empty;

        [DisplayName("الاجمالي")]
        public string Total { get; set; } = string.Empty;

        [DisplayName("السعر")]
        public string Price { get; set; } = string.Empty;

        [DisplayName("تاريخ الفاتورة")]
        public string InvDate { get; set; } = string.Empty;

        [DisplayName("رقم الفاتورة")]
        public string InvoiceNo { get; set; } = string.Empty;

        [DisplayName("رقم العملية")]
        public string ProcessNo { get; set; } = string.Empty;

        [DisplayName("نوع العملية")]
        public string ProcessType { get; set; } = string.Empty;

        [DisplayName("الحالة")]
        public string Status { get; set; } = string.Empty;

        public string User { get; set; } = string.Empty;

        [DisplayName("تاريخ الطباعة")]
        public string PrintDate { get; set; } = string.Empty;

        [DisplayName("الى تاريخ")]
        public string ToDate { get; set; } = string.Empty;

        [DisplayName("من تاريخ")]
        public string FromDate { get; set; } = string.Empty;

        public string MainItemName { get; set; } = string.Empty;
        public string MainItemCode { get; set; } = string.Empty;

        [DisplayName("اسم المجموعة")]
        public string CategoryName { get; set; } = string.Empty;

        [DisplayName("رمز المجموعة")]
        public string CategoryCode { get; set; } = string.Empty;

        public string SafeCode { get; set; } = string.Empty;

        [DisplayName("اسم المستوع")]
        public string SafeName { get; set; } = string.Empty;

        [DisplayName("نوع التقرير")]
        public string InventoryType { get; set; } = string.Empty;

        public string SumSell { get; set; } = string.Empty;
        public string SumCost { get; set; } = string.Empty;

        [DisplayName("مجموع الرصيد")]
        public string SumBalance { get; set; } = string.Empty;

        public string TotalItems { get; set; } = string.Empty;
        public string Equalization { get; set; } = string.Empty;
        public string TotSellPrice { get; set; } = string.Empty;
        public string SellPrice { get; set; } = string.Empty;
        public string TotCost { get; set; } = string.Empty;
        public string AvgCostPrice { get; set; } = string.Empty;

        [DisplayName("الرصيد")]
        public string Balance { get; set; } = string.Empty;

        [DisplayName("الكمية")]
        public string Quantity { get; set; } = string.Empty;

        [DisplayName("الباركود")]
        public string Barcode { get; set; } = string.Empty;

        [DisplayName("اسم الصنف")]
        public string ItemName { get; set; } = string.Empty;

        [DisplayName("رقم الصنف")]
        public string ItemId { get; set; } = string.Empty;

        public string ItemCode { get; set; } = string.Empty;
        public string Stamp { get; set; } = string.Empty;
        public string Footer { get; set; } = string.Empty;
        public string Header { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;

        [DisplayName("الشعار")]
        public string Logo { get; set; } = string.Empty;

        [DisplayName("الرقم الضريبي للمنشأة")]
        public string VATNo { get; set; } = string.Empty;

        [DisplayName("النشاط التجاري")]
        public string Field { get; set; } = string.Empty;

        [DisplayName("تليفون")]
        public string Telephone { get; set; } = string.Empty;

        [DisplayName("اسم المؤسسة")]
        public string Foundation { get; set; } = string.Empty;

        [DisplayName("جوال المنشأة")]
        public string Mobile { get; set; } = string.Empty;

        [DisplayName("العنوان")]
        public string Address { get; set; } = string.Empty;
    }
}