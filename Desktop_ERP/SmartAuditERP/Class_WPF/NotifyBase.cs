// ═══════════════════════════════════════════════════════════
//                     Model Classes
// ═══════════════════════════════════════════════════════════

using System.ComponentModel;

namespace SmartAuditERP.Form_WPF
{
    // ─── فئة أساسية تدعم INotifyPropertyChanged ───
    public class NotifyBase1 : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// نموذج صف جدول أصناف التحويل (dgvItems)
    /// </summary>
    public class TransferItem : NotifyBase1
    {
        private int    _rowNo;
        private double _transferQty;
        private double _totalCost;
        private double _primaryQty;

        public int    RowNo        { get => _rowNo;      set { _rowNo = value; OnPropertyChanged(nameof(RowNo)); } }
        public string ItemCode     { get; set; }
        public int    ItemId       { get; set; }
        public string ItemName     { get; set; }
        public double StoreBalance { get; set; }
        public double AvgCost      { get; set; }
        public string UnitName     { get; set; }
        public double TransferQty
        {
            get => _transferQty;
            set { _transferQty = value; OnPropertyChanged(nameof(TransferQty)); }
        }
        public double TotalCost
        {
            get => _totalCost;
            set { _totalCost = value; OnPropertyChanged(nameof(TotalCost)); }
        }
        public double ReceivedQty  { get; set; }
        public double Diff         { get; set; }
        public double UnitEquality { get; set; }
        public double PrimaryQty
        {
            get => _primaryQty;
            set { _primaryQty = value; OnPropertyChanged(nameof(PrimaryQty)); }
        }
        public string UserName     { get; set; }
    }

    /// <summary>
    /// نموذج صف جدول الاستلام (dgvReceive)
    /// </summary>
    public class ReceiveItem : NotifyBase1
    {
        private double _receivedQty;
        private double _diff;

        public int    ItemId       { get; set; }
        public string ItemName     { get; set; }
        public string UnitName     { get; set; }
        public double SentQty      { get; set; }
        public double AvgCost      { get; set; }
        public double ReceivedQty
        {
            get => _receivedQty;
            set { _receivedQty = value; OnPropertyChanged(nameof(ReceivedQty)); }
        }
        public double Diff
        {
            get => _diff;
            set { _diff = value; OnPropertyChanged(nameof(Diff)); }
        }
        public string StatusText   { get; set; }
        public bool   IsReceived   { get; set; }
        public double UnitEquality { get; set; }
        public double PrimaryQty   { get; set; }
        public double AvgCost2     { get; set; }
        public string UserName     { get; set; }
    }

    /// <summary>
    /// نموذج صف نتائج البحث (dgvSrch)
    /// </summary>
    public class TransferSrchItem
    {
        public int    TransferId   { get; set; }
        public string TransferDate { get; set; }
        public string SafeFromName { get; set; }
        public string SafeToName   { get; set; }
        public bool   IsReceived   { get; set; }
    }
}