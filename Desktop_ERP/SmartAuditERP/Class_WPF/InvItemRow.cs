// ملف: InvItemRow.cs
using System;
using System.ComponentModel;

namespace SmartAuditERP.Form_WPF
{
    public class InvItemRow : INotifyPropertyChanged
    {
        private int _rowNo;
        private string _itemCode = string.Empty;
        private int _itemId;
        private string _itemName = string.Empty;
        private string _description = string.Empty;
        private string _itemBarcode = string.Empty;
        private string _unitName = string.Empty;
        private double _itemQuantity;
        private double _unitEquality = 1.0;
        private double _primaryQnty;
        private double _itemPrice;
        private double _itemSumPrice;
        private double _itemCost;
        private double _itemDiscount;
        private double _discountPerc;
        private double _itemTotalPrice;
        private double _vatPerc;
        private double _itemVat;
        private double _itemNetPrice;
        private double _valiableStock;
        private string _expireDate = string.Empty;
        private object _storeId;

        public int RowNo { get => _rowNo; set { _rowNo = value; Notify(); } }
        public string ItemCode { get => _itemCode; set { _itemCode = value; Notify(); } }
        public int ItemId { get => _itemId; set { _itemId = value; Notify(); } }
        public string ItemName { get => _itemName; set { _itemName = value; Notify(); } }
        public string Description { get => _description; set { _description = value; Notify(); } }
        public string ItemBarcode { get => _itemBarcode; set { _itemBarcode = value; Notify(); } }
        public string UnitName { get => _unitName; set { _unitName = value; Notify(); } }
        public double ItemQuantity { get => _itemQuantity; set { _itemQuantity = value; Notify(); RecalcRow(); } }
        public double UnitEquality { get => _unitEquality; set { _unitEquality = value; Notify(); RecalcRow(); } }
        public double PrimaryQnty { get => _primaryQnty; set { _primaryQnty = value; Notify(); } }
        public double ItemPrice { get => _itemPrice; set { _itemPrice = value; Notify(); RecalcRow(); } }
        public double ItemSumPrice { get => _itemSumPrice; set { _itemSumPrice = value; Notify(); } }
        public double ItemCost { get => _itemCost; set { _itemCost = value; Notify(); } }
        public double ItemDiscount { get => _itemDiscount; set { _itemDiscount = value; Notify(); RecalcRow(); } }
        public double DiscountPerc { get => _discountPerc; set { _discountPerc = value; Notify(); } }
        public double ItemTotalPrice { get => _itemTotalPrice; set { _itemTotalPrice = value; Notify(); } }
        public double VatPerc { get => _vatPerc; set { _vatPerc = value; Notify(); RecalcRow(); } }
        public double ItemVat { get => _itemVat; set { _itemVat = value; Notify(); } }
        public double ItemNetPrice { get => _itemNetPrice; set { _itemNetPrice = value; Notify(); } }
        public double ValiableStock { get => _valiableStock; set { _valiableStock = value; Notify(); } }
        public string ExpireDate { get => _expireDate; set { _expireDate = value; Notify(); } }
        public object StoreId { get => _storeId; set { _storeId = value; Notify(); } }

        private void RecalcRow()
        {
            _primaryQnty = Math.Round(_itemQuantity * _unitEquality, 2);
            _itemSumPrice = Math.Round(_itemQuantity * _itemPrice, 2);
            _itemTotalPrice = Math.Round(_itemSumPrice - _itemDiscount, 2);

            bool priceIncVAT = false; // يُضبط من InvObj
            if (priceIncVAT)
                _itemVat = Math.Round(_itemTotalPrice - Math.Round(_itemTotalPrice / (1.0 + _vatPerc / 100.0), 3), 3);
            else
                _itemVat = Math.Round(_itemTotalPrice * (_vatPerc / 100.0), 3);

            _itemNetPrice = Math.Round(_itemTotalPrice + _itemVat, 2);
            _discountPerc = _itemSumPrice != 0
                ? Math.Round(_itemDiscount / _itemSumPrice * 100.0, 2) : 0.0;

            Notify(nameof(PrimaryQnty));
            Notify(nameof(ItemSumPrice));
            Notify(nameof(ItemTotalPrice));
            Notify(nameof(ItemVat));
            Notify(nameof(ItemNetPrice));
            Notify(nameof(DiscountPerc));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([System.Runtime.CompilerServices.CallerMemberName] string p = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}

// ملف: SearchInvRow.cs
namespace SmartAuditERP.Form_WPF
{
    public class SearchInvRow
    {
        public string GlobalID { get; set; } = string.Empty;
        public int InvNo { get; set; }
        public string InvDate { get; set; } = string.Empty;
    }
}