// ═══════════════════════════════════════════════════
//  Model Classes - في نهاية الملف أو ملف منفصل
// ═══════════════════════════════════════════════════

namespace SmartAuditERP.Form_WPF
{
    // ── موجود في المشروع الأصلي ──
    public class UnitDgv
    {
        public int    DgvNo               { get; set; }
        public int    DgvUnit             { get; set; }
        public double DgvUnitEquality     { get; set; }
        public double DgvUnitPurchasePrice { get; set; }
        public double DgvUnitSalePrice    { get; set; }
        public string DgvUnitBarcode      { get; set; }
    }

    // ── WPF Row: جدول الوحدات ──
    public class UnitDgvRow : System.ComponentModel.INotifyPropertyChanged
    {
        private int    _dgvNo;
        private int    _dgvUnit;
        private string _dgvUnitName;
        private double _dgvUnitEquality;
        private double _dgvUnitPurchasePrice;
        private double _dgvUnitSalePrice;
        private string _dgvUnitBarcode;

        public int DgvNo
        {
            get => _dgvNo;
            set { _dgvNo = value; OnPropertyChanged(nameof(DgvNo)); }
        }
        public int DgvUnit
        {
            get => _dgvUnit;
            set { _dgvUnit = value; OnPropertyChanged(nameof(DgvUnit)); }
        }
        public string DgvUnitName
        {
            get => _dgvUnitName;
            set { _dgvUnitName = value; OnPropertyChanged(nameof(DgvUnitName)); }
        }
        public double DgvUnitEquality
        {
            get => _dgvUnitEquality;
            set { _dgvUnitEquality = value; OnPropertyChanged(nameof(DgvUnitEquality)); }
        }
        public double DgvUnitPurchasePrice
        {
            get => _dgvUnitPurchasePrice;
            set { _dgvUnitPurchasePrice = value; OnPropertyChanged(nameof(DgvUnitPurchasePrice)); }
        }
        public double DgvUnitSalePrice
        {
            get => _dgvUnitSalePrice;
            set { _dgvUnitSalePrice = value; OnPropertyChanged(nameof(DgvUnitSalePrice)); }
        }
        public string DgvUnitBarcode
        {
            get => _dgvUnitBarcode;
            set { _dgvUnitBarcode = value; OnPropertyChanged(nameof(DgvUnitBarcode)); }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    // ── WPF Row: جدول الرصيد ──
    public class SafeBalanceRow
    {
        public string StoreName { get; set; }
        public double Quantity  { get; set; }
    }

    // ── WPF Row: بضاعة أول المدة ──
    public class DgvFirstRow : System.ComponentModel.INotifyPropertyChanged
    {
        private int    _storeId;
        private string _storeName;
        private double _quantity;
        private double _totalCost;

        public int StoreId
        {
            get => _storeId;
            set { _storeId = value; OnPropertyChanged(nameof(StoreId)); }
        }
        public string StoreName
        {
            get => _storeName;
            set { _storeName = value; OnPropertyChanged(nameof(StoreName)); }
        }
        public double Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(nameof(Quantity)); }
        }
        public double TotalCost
        {
            get => _totalCost;
            set { _totalCost = value; OnPropertyChanged(nameof(TotalCost)); }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this,
                new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    // ── WPF Row: جدول المكونات ──
    public class ComponentRow : System.ComponentModel.INotifyPropertyChanged
    {
        private int    _rowNo;
        private string _itemCode;
        private int    _itemId;
        private string _itemName;
        private int    _unitId;
        private string _unitName;
        private double _quantity;
        private double _price;
        private double _total;
        private int    _storeCompId;
        private string _storeCompName;
        private bool   _isAdded;

        public int    RowNo         { get => _rowNo;       set { _rowNo = value;       OnPropertyChanged(nameof(RowNo)); } }
        public string ItemCode      { get => _itemCode;    set { _itemCode = value;    OnPropertyChanged(nameof(ItemCode)); } }
        public int    ItemId        { get => _itemId;      set { _itemId = value;      OnPropertyChanged(nameof(ItemId)); } }
        public string ItemName      { get => _itemName;    set { _itemName = value;    OnPropertyChanged(nameof(ItemName)); } }
        public int    UnitId        { get => _unitId;      set { _unitId = value;      OnPropertyChanged(nameof(UnitId)); } }
        public string UnitName      { get => _unitName;    set { _unitName = value;    OnPropertyChanged(nameof(UnitName)); } }
        public double Quantity      { get => _quantity;    set { _quantity = value;    OnPropertyChanged(nameof(Quantity)); } }
        public double Price         { get => _price;       set { _price = value;       OnPropertyChanged(nameof(Price)); } }
        public double Total         { get => _total;       set { _total = value;       OnPropertyChanged(nameof(Total)); } }
        public int    StoreCompId   { get => _storeCompId; set { _storeCompId = value; OnPropertyChanged(nameof(StoreCompId)); } }
        public string StoreCompName { get => _storeCompName; set { _storeCompName = value; OnPropertyChanged(nameof(StoreCompName)); } }
        public bool   IsAdded       { get => _isAdded;    set { _isAdded = value;    OnPropertyChanged(nameof(IsAdded)); } }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this,
                new System.ComponentModel.PropertyChangedEventArgs(name));
    }
}