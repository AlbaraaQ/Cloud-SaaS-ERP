// ══════════════════════════════════════════════
// Models للـ DataGrids
// ══════════════════════════════════════════════

namespace SmartAuditERP.Form_WPF
{
    /// <summary>صف بيانات الأصناف في التسوية الجردية</summary>
    public class AdjustItemRow : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this,
               new System.ComponentModel.PropertyChangedEventArgs(name));

        public string ItemCode  { get; set; } = "";
        public string ItemName  { get; set; } = "";
        public string Unit      { get; set; } = "";
        public int    ItemId    { get; set; }

        private double _currentStock;
        public double CurrentStock
        {
            get => _currentStock;
            set { _currentStock = value; OnPropertyChanged(nameof(CurrentStock)); }
        }

        private double _realStock;
        public double RealStock
        {
            get => _realStock;
            set
            {
                _realStock = value;
                OnPropertyChanged(nameof(RealStock));
                RecalcQty();
            }
        }

        private double _inQty;
        public double InQty
        {
            get => _inQty;
            set { _inQty = value; OnPropertyChanged(nameof(InQty)); }
        }

        private double _outQty;
        public double OutQty
        {
            get => _outQty;
            set { _outQty = value; OnPropertyChanged(nameof(OutQty)); }
        }

        private double _price;
        public double Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged(nameof(Price));
                RecalcTotal();
            }
        }

        private double _total;
        public double Total
        {
            get => _total;
            set { _total = value; OnPropertyChanged(nameof(Total)); }
        }

        public string Note { get; set; } = "";

        private void RecalcQty()
        {
            if (_realStock == _currentStock)
            {
                InQty  = 0;
                OutQty = 0;
            }
            else if (_realStock > _currentStock)
            {
                InQty  = _realStock - _currentStock;
                OutQty = 0;
            }
            else
            {
                OutQty = _currentStock - _realStock;
                InQty  = 0;
            }

            RecalcTotal();
        }

        private void RecalcTotal()
        {
            double qty = InQty > 0 ? InQty : OutQty;
            Total = System.Math.Round(_price * qty, 4);
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(InQty));
            OnPropertyChanged(nameof(OutQty));
        }
    }

    /// <summary>صف نتائج البحث</summary>
    public class AdjustSearchRow
    {
        public int    AdjId     { get; set; }
        public string AdjDate   { get; set; } = "";
        public string AdjTime   { get; set; } = "";
        public string SafeName  { get; set; } = "";
        public string EmpName   { get; set; } = "";
        public bool   IsApproved { get; set; }
    }
}