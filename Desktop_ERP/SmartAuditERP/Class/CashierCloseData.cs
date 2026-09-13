using System.ComponentModel;

namespace SmartAuditERP
{
    public class CashierCloseData
    {
        [DisplayName("")]
        public string ExtraTax { get; set; } = string.Empty;

        [DisplayName("اسم التقرير")]
        public string ReportName { get; set; } = string.Empty;

        [DisplayName("تاريخ الطباعة")]
        public string printDate { get; set; } = string.Empty;

        [DisplayName("اسم الموظف")]
        public string EmpName { get; set; } = string.Empty;

        [DisplayName("مجموع الفرق")]
        public string SummaryDiffVal { get; set; } = string.Empty;

        [DisplayName("مجموع الكاشير")]
        public string SummaryCashier { get; set; } = string.Empty;

        [DisplayName("مجموع الصافي")]
        public string SummaryNet { get; set; } = string.Empty;

        [DisplayName("إجمالي الخصومات")]
        public string SummaryDiscount { get; set; } = string.Empty;

        [DisplayName("إجمالي الضريبة")]
        public string SummaryTax { get; set; } = string.Empty;

        public string SummaryHosting { get; set; } = string.Empty;

        [DisplayName("اجمالي التأمينات")]
        public string SummaryInsuranceVal { get; set; } = string.Empty;

        [DisplayName("اجمالي التوصيل")]
        public string SummaryAdditionVal { get; set; } = string.Empty;

        [DisplayName("مجموع المشتريات")]
        public string SummaryPurchases { get; set; } = string.Empty;

        [DisplayName("مجموع المصروفات")]
        public string SummaryExpenses { get; set; } = string.Empty;

        public string SummarySumCashAndCredit { get; set; } = string.Empty;

        [DisplayName("مجموع النقدي")]
        public string SummaryCash { get; set; } = string.Empty;

        public string SummaryCreditCard { get; set; } = string.Empty;

        public string SumCashAndCredit { get; set; } = string.Empty;

        [DisplayName("المشتريات")]
        public string Purchases { get; set; } = string.Empty;

        [DisplayName("المصروفات")]
        public string Expenses { get; set; } = string.Empty;

        [DisplayName("اغلاق إلى")]
        public string CloseDateTo { get; set; } = string.Empty;

        [DisplayName("إغلاق من")]
        public string CloseDateFrome { get; set; } = string.Empty;

        public string Stamp { get; set; } = string.Empty;

        [DisplayName("التذييل")]
        public string Footer { get; set; } = string.Empty;

        [DisplayName("الترويسة")]
        public string Header { get; set; } = string.Empty;

        [DisplayName("الشعار")]
        public string Logo { get; set; } = string.Empty;

        [DisplayName("النشاط ")]
        public string Field { get; set; } = string.Empty;

        [DisplayName("اسم المنشأة")]
        public string Foundation { get; set; } = string.Empty;

        [DisplayName("الرقم الضريبي")]
        public string VATNo { get; set; } = string.Empty;

        [DisplayName("تليفون")]
        public string Telephone { get; set; } = string.Empty;

        [DisplayName("جوال")]
        public string Mobile { get; set; } = string.Empty;

        [DisplayName("العنوان")]
        public string Address { get; set; } = string.Empty;

        [DisplayName("رقم الشفت")]
        public string shiftNo { get; set; } = string.Empty;

        [DisplayName("رقم الاغلاق")]
        public string CloseNo { get; set; } = string.Empty;

        [DisplayName("الى تاريخ")]
        public string dateTo { get; set; } = string.Empty;

        [DisplayName("من تاريخ")]
        public string datefrom { get; set; } = string.Empty;

        [DisplayName("صافي الصندوق")]
        public string SAfeNetVal { get; set; } = string.Empty;

        [DisplayName("القيمة المدخلة")]
        public string CasherValue { get; set; } = string.Empty;

        public string NetWithoutVAT { get; set; } = string.Empty;

        [DisplayName("الفرق")]
        public string DiffVal { get; set; } = string.Empty;

        [DisplayName("الصافي")]
        public string Net { get; set; } = string.Empty;

        [DisplayName("المبيعات النقدية")]
        public string CashVal { get; set; } = string.Empty;

        [DisplayName("الضريبة")]
        public string VAT { get; set; } = string.Empty;

        [DisplayName("مرتجع مبيعات")]
        public string ReturnSales { get; set; } = string.Empty;

        [DisplayName("مبيعات الشبكة")]
        public string NetworkSales { get; set; } = string.Empty;

        [DisplayName("المبيعات الآجلة")]
        public string PostPoneSales { get; set; } = string.Empty;

        [DisplayName("مرتجع المبيعات الاجلة")]
        public string PostPoneRet { get; set; } = string.Empty;

        [DisplayName("اجمالي التوصيل")]
        public string AdditionVal { get; set; } = string.Empty;

        [DisplayName("المستخدم")]
        public string Username { get; set; } = string.Empty;

        [DisplayName("اجمالي التأمين")]
        public string InsurVal { get; set; } = string.Empty;

        [DisplayName("اجمالي الخصومات")]
        public string Discount { get; set; } = string.Empty;

        public string HostingVal { get; set; } = string.Empty;
    }
}