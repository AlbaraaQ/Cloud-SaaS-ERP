// ملف: SalesManRowItem.cs
using System.ComponentModel;

namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// يمثل صفًا واحدًا في جدول مبيعات المندوبين.
    /// </summary>
    public class SalesManRowItem : INotifyPropertyChanged
    {
        public string ProcessType { get; set; } = string.Empty;
        public string InvDate { get; set; } = string.Empty;
        public int InvoiceNo { get; set; }
        public int RefNo { get; set; }
        public string SalesManName { get; set; } = string.Empty;
        public double Value { get; set; }
        public double SalesComm { get; set; }
        public double CollectionComm { get; set; }
        public double ProfitComm { get; set; }

        /// <summary>
        /// 1 = يُضاف للإجمالي، -1 = يُطرح من الإجمالي.
        /// </summary>
        public int IsPlus { get; set; } = 1;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}