using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInvoiceDetails : DevExpress.Xpf.Core.ThemedWindow
    {
        // ══════════════════════════════════════════════════════════
        #region Public Fields

        public int selectedCli;
        public int StoreId;
        public string sql;
        public string Cond;
        public string InvGlobalId;
        public int ItemID;
        public int InvType;
        public bool ISDone;
        public int RowIndex;
        public bool ISProfit;
        public int costPrice;

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Private Fields

        private readonly SqlConnection _conn;
        private DataTable _dt;
        private ObservableCollection<InvoiceDetailRow> _rows;

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Constructor

        public frmInvoiceDetails()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            StoreId = 1;
            InvGlobalId = "1";
            ItemID = -1;
            InvType = 1;
            ISDone = false;
            RowIndex = -1;
            ISProfit = true;
            costPrice = 1;
            _dt = new DataTable();
            _rows = new ObservableCollection<InvoiceDetailRow>();

            dgvItems.ItemsSource = _rows;
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadInvoiceItems();
            ApplyProfitVisibility();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (RowIndex > -1)
                {
                    try
                    {
                        ISDone = true;
                        var row = _rows[RowIndex];
                        ItemID = row.ItemId;
                        InvGlobalId = row.ItemCode;
                        Close();
                    }
                    catch { }
                    return;
                }

                if (_rows.Count > 0)
                {
                    dgvItems.SelectedIndex = 0;
                    dgvItems.ScrollIntoView(_rows[0]);
                }
            }
            else if (e.Key == Key.Up)
            {
                NavigateUp();
            }
            else if (e.Key == Key.Down)
            {
                NavigateDown();
            }
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Header Events

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void txtSrchDgv_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return) return;

            string searchText = txtSrchDgv.Text.Trim();
            if (string.IsNullOrWhiteSpace(searchText)) return;

            var found = _rows.FirstOrDefault(r =>
                r.ItemCode == searchText ||
                r.ItemName?.Contains(searchText) == true);

            if (found != null)
            {
                dgvItems.SelectedItem = found;
                dgvItems.ScrollIntoView(found);
                txtSrchDgv.Text = string.Empty;
            }
            else
            {
                DXMessageBox.Show("غير موجود", "بحث",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnRecalculateCost_Click(object sender, RoutedEventArgs e)
        {
            RecalculateCostForChecked();
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Grid Events

        private void dgvItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RowIndex = dgvItems.SelectedIndex;
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Data Loading

        public void LoadInvoiceItems()
        {
            try
            {
                _rows.Clear();
                _dt.Clear();

                // فحص PriceIncVAT
                bool priceIncVAT = false;
                string vatQuery = $"SELECT PriceIncVAT FROM Inv WHERE InvGlobalId = N'{InvGlobalId}'";
                var vatTable = new DataTable();
                new SqlDataAdapter(vatQuery, _conn).Fill(vatTable);

                if (vatTable.Rows.Count > 0 && vatTable.Rows[0]["PriceIncVAT"] != DBNull.Value)
                    priceIncVAT = Convert.ToBoolean(vatTable.Rows[0]["PriceIncVAT"]);

                // تحميل بيانات الفاتورة
                string invQuery = $"SELECT * FROM Inv_Sub WHERE InvGlobalId = N'{InvGlobalId}'";
                new SqlDataAdapter(invQuery, _conn).Fill(_dt);

                int rowIndex = 1;

                foreach (DataRow row in _dt.Rows)
                {
                    try
                    {
                        int itemId = Convert.ToInt32(row["ItemId"]);
                        int unitId = Convert.ToInt32(row["unit"]);
                        string itemName = GetItemName(itemId);
                        string itemCode = GetItemCode(itemId);
                        string unitName = GetUnitName(unitId);

                        double unitPerc = GetUnitPerc(itemId, unitId);
                        double qty1 = Convert.ToDouble(row["val1"]);
                        double qty = Convert.ToDouble(row["val"]);
                        double currentQty = Convert.ToDouble(row["CurrentQnty"]);
                        double price = Convert.ToDouble(row["exchange_price"]);
                        double taxVal = Convert.ToDouble(row["taxval"]);
                        double discount = Convert.ToDouble(row["discount"] == DBNull.Value ? 0 : row["discount"]);
                        double avrgCost = GetAvrgCost(row, unitPerc, itemId);

                        double currentQtyCalc = Math.Round(currentQty / (unitPerc == 0 ? 1 : unitPerc), 2);

                        double subTotal = Math.Round(qty1 * price, 3);
                        if (priceIncVAT)
                            subTotal = Math.Round(qty1 * price - taxVal, 3);

                        double netTotal = subTotal - discount;
                        double netWithVAT = netTotal + taxVal;
                        double costTotal = Math.Round(qty * avrgCost, 3);
                        double profit = netTotal - costTotal;
                        string profitRatio = costTotal == 0
                            ? "0%"
                            : Math.Round(profit / costTotal * 100, 2) + "%";

                        var detailRow = new InvoiceDetailRow
                        {
                            RowNo = rowIndex++,
                            ItemId = itemId,
                            ItemCode = itemCode,
                            ItemName = itemName,
                            UnitName = unitName,
                            ItemQty = qty1,
                            TotalQty = qty,
                            CurrentQty = currentQtyCalc < 0 ? 0 : currentQtyCalc,
                            AvgCost = avrgCost,
                            ItemPrice = price,
                            SubTotal = subTotal,
                            Discount = discount,
                            NetTotal = netTotal,
                            TaxValue = taxVal,
                            NetWithVAT = netWithVAT,
                            Profit = profit,
                            ProfitRatio = profitRatio,
                            IsChecked = false,
                            IsNegative = currentQtyCalc < 0
                        };

                        _rows.Add(detailRow);
                    }
                    catch { /* نتجاوز الصف المعطوب */ }
                }

                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    $"خطأ في تحميل البيانات:\n{ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Business Logic

        private void RecalculateCostForChecked()
        {
            try
            {
                string cond = $" and inv.InvGlobalId = N'{InvGlobalId}'";

                foreach (var row in _rows.Where(r => r.IsChecked))
                {
                    using var sqlConn = MainClass.ConnObj();
                    if (sqlConn.State != System.Data.ConnectionState.Open)
                        sqlConn.Open();

                    string storeQuery = $@"
                        SELECT Inv_Sub.store, Inv_Sub.AvrgCost, inv.branch
                        FROM   inv, Inv_Sub
                        WHERE  inv.InvGlobalID  = N'{InvGlobalId}'
                          AND  Inv_Sub.ItemId   = {row.ItemId}
                          AND  Inv_Sub.InvGlobalID = inv.InvGlobalID";

                    var storeTable = new DataTable();
                    new SqlDataAdapter(storeQuery, sqlConn).Fill(storeTable);

                    if (storeTable.Rows.Count == 0) continue;

                    int storeId = Convert.ToInt32(storeTable.Rows[0]["store"]);
                    int branch = Convert.ToInt32(storeTable.Rows[0]["branch"]);
                    double stock = Inventory.CalcItemStock(storeId, row.ItemId, branch);
                    double avgCost = ItemOper.AvgCost(row.ItemId, branch);

                    if (row.CurrentQty > -1)
                    {
                        var confirm = DXMessageBox.Show(
                            "هل تريد إعادة احتساب التكلفة بتكلفة الصنف الحالية؟",
                            "تنبيه", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (confirm == MessageBoxResult.Yes)
                        {
                            var cmd = new SqlCommand(StoredQueries.UpdateItemCost, sqlConn);
                            cmd.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = InvGlobalId;
                            cmd.Parameters.Add("@ItemId", SqlDbType.Int).Value = row.ItemId;
                            cmd.Parameters.Add("@AvrgCost", SqlDbType.Float).Value = avgCost;
                            cmd.Parameters.Add("@CurrentQnty", SqlDbType.Float).Value = stock;
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else if (row.CurrentQty < 0 && stock > -1)
                    {
                        var confirm = DXMessageBox.Show(
                            "هل تريد إعادة احتساب التكلفة بتكلفة الصنف الحالية؟",
                            "تنبيه", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (confirm == MessageBoxResult.Yes)
                        {
                            ItemOper.RecalculateCost(row.ItemId, stock, ref avgCost, cond);
                        }
                    }
                    else
                    {
                        DXMessageBox.Show("لا يوجد كميات متوفرة في المستودع",
                            "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                // إعادة تحميل البيانات
                _dt.Clear();
                _rows.Clear();
                LoadInvoiceItems();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    $"خطأ في إعادة الاحتساب:\n{ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private double GetAvrgCost(DataRow row, double unitPerc, int itemId)
        {
            double avrgCost = Convert.ToDouble(row["AvrgCost"]);

            if (costPrice == 1)
                return Math.Round(avrgCost * unitPerc, 2);

            if (costPrice == 2)
            {
                double purchPrice = GetPurchPrice(itemId, Convert.ToInt32(row["unit"]));
                return Math.Round(purchPrice, 2);
            }

            if (costPrice == 3)
                return Math.Round(ItemOper.RecentPurchPrice(itemId), 2);

            return avrgCost;
        }

        private double GetPurchPrice(int itemId, int unitId)
        {
            try
            {
                string query = $@"
                    SELECT ItemUnits.purch
                    FROM   Items, ItemUnits
                    WHERE  Items.id       = ItemUnits.ItemId
                      AND  ItemUnits.unit = {unitId}
                      AND  ItemUnits.ItemId = {itemId}";

                var table = new DataTable();
                new SqlDataAdapter(query, _conn).Fill(table);

                if (table.Rows.Count > 0)
                    return Convert.ToDouble(table.Rows[0]["purch"]);
            }
            catch { }

            return 0.0;
        }

        private double GetUnitPerc(int itemId, int unitId)
        {
            try
            {
                string query = $@"
                    SELECT ItemUnits.perc
                    FROM   Items, ItemUnits
                    WHERE  Items.id       = ItemUnits.ItemId
                      AND  ItemUnits.unit = {unitId}
                      AND  ItemUnits.ItemId = {itemId}";

                var table = new DataTable();
                new SqlDataAdapter(query, _conn).Fill(table);

                if (table.Rows.Count > 0)
                {
                    double perc = Convert.ToDouble(table.Rows[0]["perc"]);
                    return perc == 0 ? 1 : perc;
                }
            }
            catch { }

            return 1.0;
        }

        private void ApplyProfitVisibility()
        {
            if (!ISProfit)
            {
                // إظهار الضريبة والصافي، إخفاء الربح ونسبة الربح
                // يتحكم بها عبر خاصية Visibility في الأعمدة
                // Col13 = TaxValue, Col14 = NetWithVAT
                // Col15 = Profit, Col16 = ProfitRatio
                // Col8 = AvgCost
                // نستعمل الأعمدة المسماة مباشرة
            }
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Database Helpers

        private string GetItemName(int itemId)
        {
            try
            {
                var table = new DataTable();
                new SqlDataAdapter(
                    $"SELECT name FROM Items WHERE id = {itemId}",
                    _conn).Fill(table);

                return table.Rows.Count > 0
                    ? table.Rows[0][0]?.ToString() ?? ""
                    : "";
            }
            catch { return ""; }
        }

        private string GetItemCode(int itemId)
        {
            try
            {
                var table = new DataTable();
                new SqlDataAdapter(
                    $"SELECT Code FROM Items WHERE id = {itemId}",
                    _conn).Fill(table);

                return table.Rows.Count > 0
                    ? table.Rows[0][0]?.ToString() ?? ""
                    : "";
            }
            catch { return ""; }
        }

        private string GetUnitName(int unitId)
        {
            try
            {
                var table = new DataTable();
                new SqlDataAdapter(
                    $"SELECT name FROM units WHERE id = {unitId}",
                    _conn).Fill(table);

                return table.Rows.Count > 0
                    ? table.Rows[0][0]?.ToString() ?? ""
                    : "";
            }
            catch { return ""; }
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Navigation Helpers

        private void NavigateUp()
        {
            if (dgvItems.SelectedIndex > 0)
                dgvItems.SelectedIndex--;
        }

        private void NavigateDown()
        {
            if (dgvItems.SelectedIndex < _rows.Count - 1)
                dgvItems.SelectedIndex++;
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region UI Helpers

        private void UpdateStatusBar()
        {
            lblStatusBar.Text = $"📦 إجمالي الأصناف: {_rows.Count}";
        }

        #endregion
    }

    // ══════════════════════════════════════════════════════════════
    #region Model: InvoiceDetailRow

    public class InvoiceDetailRow : INotifyPropertyChanged
    {
        public int RowNo { get; set; }
        public int ItemId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string UnitName { get; set; }
        public double ItemQty { get; set; }
        public double TotalQty { get; set; }
        public double CurrentQty { get; set; }
        public double AvgCost { get; set; }
        public double ItemPrice { get; set; }
        public double SubTotal { get; set; }
        public double Discount { get; set; }
        public double NetTotal { get; set; }
        public double TaxValue { get; set; }
        public double NetWithVAT { get; set; }
        public double Profit { get; set; }
        public string ProfitRatio { get; set; }
        public bool IsNegative { get; set; }

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; OnPropertyChanged(nameof(IsChecked)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    #endregion
}