// ملف: InvDetailRow.cs
using System.ComponentModel;

namespace SmartAuditERP.Form_WPF
{
    public class InvDetailRow : INotifyPropertyChanged
    {
        public int DgvNo { get; set; }
        public string DgvStore { get; set; } = string.Empty;
        public string DgvInvType { get; set; } = string.Empty;
        public string DgvClient { get; set; } = string.Empty;
        public string DgvDate { get; set; } = string.Empty;
        public string DgvInvNo { get; set; } = string.Empty;
        public string DgvItem { get; set; } = string.Empty;
        public string DgvUnit { get; set; } = string.Empty;
        public double DgvQty { get; set; }
        public double DgvPrimaryQnty { get; set; }
        public double DgvPrice { get; set; }
        public double DgvItemSum { get; set; }
        public double DgvItemDiscount { get; set; }
        public double DgvTotalBeforeTax { get; set; }
        public double DgvVAT { get; set; }
        public double DgvItemAdditionalTax { get; set; }
        public double DgvTotal { get; set; }
        public string DgvUser { get; set; } = string.Empty;
        public string DgvInvGlobalID { get; set; } = string.Empty;

        /// <summary>
        /// 0 = يُضاف للإجمالي، 1 = يُطرح (مرتجع).
        /// </summary>
        public int DgvIsPlus { get; set; } = 0;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}