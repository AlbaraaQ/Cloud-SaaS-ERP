namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// نموذج صف في جدول طلبات الخياطة
    /// </summary>
    public class OrderRow
    {
        /// <summary>رقم الفاتورة</summary>
        public int code { get; set; }

        /// <summary>اسم العميل</summary>
        public string name { get; set; }

        /// <summary>رقم الهاتف</summary>
        public string phone { get; set; }

        /// <summary>التاريخ (نص منسّق)</summary>
        public string Datee { get; set; }

        /// <summary>الإجمالي مع الضريبة (5%)</summary>
        public double Sale_price { get; set; }

        /// <summary>المبلغ المدفوع</summary>
        public double paid { get; set; }

        /// <summary>المبلغ الباقي</summary>
        public double rest { get; set; }

        /// <summary>الحالة (مستلم / في الخياطة / جاهز / تم التسليم)</summary>
        public string state { get; set; }
    }
}