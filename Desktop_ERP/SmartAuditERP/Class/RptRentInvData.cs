using System.ComponentModel;

namespace SmartAuditERP
{
    public class RptRentInvData
    {
        private string _marine = string.Empty;

        [DisplayName("نوع التقرير")]
        public string RptType { get; set; } = string.Empty;

        [DisplayName("من تاريخ")]
        public string FromDate { get; set; } = string.Empty;

        [DisplayName("الى تاريخ")]
        public string ToDate { get; set; } = string.Empty;

        [DisplayName("اسم الحساب")]
        public string AccountName { get; set; } = string.Empty;

        [DisplayName("الوصف")]
        public string Description { get; set; } = string.Empty;

        [DisplayName("صافي الفاتورة")]
        public string InvoiceNet { get; set; } = string.Empty;

        public string Marine
        {
            get => _marine;
            set => _marine = value ?? string.Empty;
        }

        public string Marine1
        {
            get => _marine;
            set => _marine = value ?? string.Empty;
        }

        [DisplayName("العميل")]
        public string Client { get; set; } = string.Empty;

        public string Inplan { get; set; } = string.Empty;

        [DisplayName("جوال العميل")]
        public string ClientPhone { get; set; } = string.Empty;

        public string RestrNo { get; set; } = string.Empty;

        [DisplayName("الحالة")]
        public string Status { get; set; } = string.Empty;

        [DisplayName("مدين")]
        public string Dept { get; set; } = string.Empty;

        [DisplayName("تاريخ الطباعة")]
        public string PrintDate { get; set; } = string.Empty;

        [DisplayName("تاريخ الفاتورة")]
        public string InvDate { get; set; } = string.Empty;

        [DisplayName("وقت الفاتورة")]
        public string InvTime { get; set; } = string.Empty;

        [DisplayName("المستخدم")]
        public string User { get; set; } = string.Empty;

        [DisplayName("البائع")]
        public string InvUser { get; set; } = string.Empty;

        [DisplayName("مجموع المدي")]
        public string SumDept { get; set; } = string.Empty;

        [DisplayName("العنوان للمنشاة")]
        public string Address { get; set; } = string.Empty;

        [DisplayName(" جوال المنشاة")]
        public string Mobile { get; set; } = string.Empty;

        [DisplayName("مجموع الدائن")]
        public string SumCredit { get; set; } = string.Empty;

        [DisplayName("تليفون المنشاة")]
        public string Telephone { get; set; } = string.Empty;

        [DisplayName("النشاط التجاري")]
        public string Field { get; set; } = string.Empty;

        [DisplayName("اسم المنشأة")]
        public string Foundation { get; set; } = string.Empty;

        [DisplayName("الرقم الضريبي للمنشاة")]
        public string VATNo { get; set; } = string.Empty;

        [DisplayName("ملاحظات")]
        public string Note { get; set; } = string.Empty;

        [DisplayName("الشعار")]
        public string Logo { get; set; } = string.Empty;

        [DisplayName("الترويسة")]
        public string Header { get; set; } = string.Empty;

        public string Stamp { get; set; } = string.Empty;

        [DisplayName("التذييل")]
        public string Footer { get; set; } = string.Empty;

        [DisplayName("الرصيد")]
        public string Balance { get; set; } = string.Empty;

        [DisplayName("رقم العملية")]
        public string ProcessNo { get; set; } = string.Empty;

        [DisplayName("الرصيد خلال الفترة")]
        public string BalanceInperiod { get; set; } = string.Empty;

        [DisplayName("نوع الرصيد")]
        public string BalanceType { get; set; } = string.Empty;

        [DisplayName("نوع العملية")]
        public string ProcessType { get; set; } = string.Empty;

        public string GroupCode { get; set; } = string.Empty;
    }
}