namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// نموذج صف في جدول عملاء ضريبة القيمة المضافة
    /// </summary>
    public class VatClientRow
    {
        /// <summary>معرّف العميل</summary>
        public int id { get; set; }

        /// <summary>اسم العميل</summary>
        public string name { get; set; }

        /// <summary>الرقم الضريبي</summary>
        public string taxNo { get; set; }
    }
}