using System.ComponentModel;

namespace SmartAuditERP
{
    public class Bond
    {
        public string SalarySub { get; set; } = string.Empty;

        public string SalaryAdd { get; set; } = string.Empty;

        public string TransportationAllowance { get; set; } = string.Empty;

        public string HousingAllowance { get; set; } = string.Empty;

        public string TotSalary { get; set; } = string.Empty;

        [DisplayName("الفرع")]
        public string Branch { get; set; } = string.Empty;

        [DisplayName("تاريخ المرجع")]
        public string reffDate { get; set; } = string.Empty;

        [DisplayName("رقم المرجع")]
        public string reffNo { get; set; } = string.Empty;

        [DisplayName("العميل")]
        public string client { get; set; } = string.Empty;

        [DisplayName("المورد")]
        public string supplier { get; set; } = string.Empty;

        [DisplayName("صافي القيمة")]
        public string netVal { get; set; } = string.Empty;

        [DisplayName("نسبة الضريبة")]
        public string vatPerc { get; set; } = string.Empty;

        [DisplayName("قيمة الضريبة")]
        public string taxVal { get; set; } = string.Empty;

        [DisplayName("تفقيط عربي")]
        public string ArabicLetter { get; set; } = string.Empty;

        [DisplayName("تاريخ الطباعة")]
        public string PrintDate { get; set; } = string.Empty;

        [DisplayName("تاريخ الشيك")]
        public string CheckDate { get; set; } = string.Empty;

        [DisplayName("رقم الشيك")]
        public string CheckNo { get; set; } = string.Empty;

        [DisplayName("ملاحظات")]
        public string Notes { get; set; } = string.Empty;

        [DisplayName("المندوب")]
        public string SalesMan { get; set; } = string.Empty;

        [DisplayName("الرصيد الحالي")]
        public string CurrentBalance { get; set; } = string.Empty;

        [DisplayName("قيمة السند")]
        public string BondVal { get; set; } = string.Empty;

        [DisplayName("الوصف")]
        public string Description { get; set; } = string.Empty;

        [DisplayName("حالة الحساب")]
        public string AccountStatus { get; set; } = string.Empty;

        [DisplayName("رقم السند")]
        public string BondNo { get; set; } = string.Empty;

        [DisplayName("نوع السند")]
        public string BondType { get; set; } = string.Empty;

        [DisplayName("اسم السند")]
        public string BondName { get; set; } = string.Empty;

        [DisplayName("تاريخ السند")]
        public string BondDate { get; set; } = string.Empty;

        [DisplayName("وقت السند")]
        public string BondTime { get; set; } = string.Empty;

        [DisplayName("المسنخدم")]
        public string User { get; set; } = string.Empty;

        [DisplayName("دفعة لحساب")]
        public string PayTo { get; set; } = string.Empty;

        [DisplayName("دفعة من حساب")]
        public string Payfrom { get; set; } = string.Empty;

        [DisplayName("من حساب رقم")]
        public string PayFromCode { get; set; } = string.Empty;

        [DisplayName("الى حساب رقم")]
        public string PayToCode { get; set; } = string.Empty;

        [DisplayName("مركز التكلفة")]
        public string CostCenter { get; set; } = string.Empty;

        [DisplayName("من تاريخ")]
        public string FromDate { get; set; } = string.Empty;

        [DisplayName("الى تاريخ")]
        public string ToDate { get; set; } = string.Empty;

        [DisplayName("المجموع")]
        public string Sum { get; set; } = string.Empty;

        [DisplayName("العنوان")]
        public string Address { get; set; } = string.Empty;

        [DisplayName("موبايل")]
        public string Mobile { get; set; } = string.Empty;

        [DisplayName("اسم المنشأة")]
        public string Foundation { get; set; } = string.Empty;

        [DisplayName("تليفون")]
        public string Telephone { get; set; } = string.Empty;

        [DisplayName("النشاط التجاري")]
        public string Field { get; set; } = string.Empty;

        [DisplayName("الرقم الضريبي")]
        public string VATNo { get; set; } = string.Empty;

        [DisplayName("الشعار")]
        public string Logo { get; set; } = string.Empty;

        [DisplayName("الملاحظات")]
        public string Note { get; set; } = string.Empty;

        [DisplayName("الترويسة")]
        public string Header { get; set; } = string.Empty;

        [DisplayName("التذييل")]
        public string Footer { get; set; } = string.Empty;

        public string Stamp { get; set; } = string.Empty;

        public string Month { get; set; } = string.Empty;

        public string Year { get; set; } = string.Empty;
    }
}