using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Printing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmProductionOrder : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        private SqlConnection _conn1;

        // ═══ حقول الفاتورة (محافظة على الأسماء الأصلية) ═══
        private int _code = -1;
        private int _procCode = -1;
        private string _invGlobalID = "-1";

        private InvoiceObj _invObj;

        private int _defUnit = 0;
        private int _defTreasury = 0;
        private double _defVAT = 0;
        private double _defDelivery = 0;
        private double _defInsurance = 0;
        private bool _priceIncVAT = false;
        private bool _saleByMinus = true;
        private bool _printHeader = true;
        private bool _printFooter = true;
        private int _printType = 1;
        private int _printNo = 1;
        private string _defPrinter = "";
        private bool _isPrinted = false;
        private string _printNote = "";
        private string _rptName = "";
        private string _rptUrl = "";

        private int _rowIndex = -1;
        private int _selectedId = -1;

        public string SrchName = "";
        private int _restSaleCode = -1;

        public int RestrType = 0;
        public int InvType = 2;
        public int ProcType = 0;

        private bool _isLoaded = false;
        private double _invTax = 0;
        private double _invDiscount = 0;
        private double _totAfterDisc = 0;
        private bool _isReturn = false;

        // ═══ مصادر بيانات الجداول ═══
        private ObservableCollection<ProductionItemRow> _itemsSource
            = new ObservableCollection<ProductionItemRow>();

        private ObservableCollection<SearchResultRow> _searchSource
            = new ObservableCollection<SearchResultRow>();

        // ═══ جداول مساعدة للـ ComboBox ═══
        private DataTable _unitsTable = new DataTable();
        private DataTable _storesTable = new DataTable();
        private DataView _storesView;
        private DataView _unitsView;

        #endregion

        #region Constructor

        public frmProductionOrder()
        {
            InitializeComponent();
            _conn = MainClass.ConnObj();
            _conn1 = MainClass.ConnObj();
            _invObj = new InvoiceObj(6, 1);

            dgvItems.ItemsSource = _itemsSource;
            dgvSrch.ItemsSource = _searchSource;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUnits();
            LoadMainSettings();
            LoadSafes();
            PrintSetting();
            LoadInvNo();

            txtDate.DateTime = DateTime.Now;
            txtToDate.DateTime = DateTime.Now;
            txtRefDate.DateTime = DateTime.Now;
            txtFromDate.DateTime = DateTime.Now;

            LoadProductions();
        }

        private void Window_Closing(object sender,
            CancelEventArgs e)
        {
            try { _conn?.Close(); } catch { }
            try { _conn1?.Close(); } catch { }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
                AddNewItem();
        }

        #endregion

        #region Load Settings

        private void LoadMainSettings()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT * FROM SettingGeneral WHERE Inv_Id={InvType}",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count == 1)
                    {
                        try { _priceIncVAT = Convert.ToBoolean(dt.Rows[0]["PriceIncVAT"]); } catch { }
                        try { _defUnit = Convert.ToInt32(dt.Rows[0]["unit"]); } catch { }
                        try { _defDelivery = Convert.ToDouble(dt.Rows[0]["DeliveryVal"]); } catch { }
                        try { _defInsurance = Convert.ToDouble(dt.Rows[0]["InsureVal"]); } catch { }
                        try { _defVAT = Convert.ToDouble(dt.Rows[0]["MainVAT"]); } catch { }
                        try { _defTreasury = Convert.ToInt32(dt.Rows[0]["Treasury"]); } catch { }
                        try { _saleByMinus = Convert.ToBoolean(dt.Rows[0]["SaleByMinus"]); } catch { }
                    }
                }
            }
            catch { }
        }

        private void PrintSetting()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT * FROM SettingPrint WHERE Inv_Id={InvType}",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count == 1)
                    {
                        try { _printType = Convert.ToInt32(dt.Rows[0]["printType"]); } catch { }
                        try { _printFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]); } catch { }
                        try { _printHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]); } catch { }
                        try { _defPrinter = dt.Rows[0]["CasherPrinter"].ToString(); } catch { }
                        try { _printNo = Convert.ToInt32(dt.Rows[0]["printNo"]); } catch { }
                        try { _rptName = dt.Rows[0]["RptName"].ToString(); } catch { }
                        try { _rptUrl = Path.GetDirectoryName(dt.Rows[0]["RptUrl"].ToString()); } catch { }
                        try { _printNote = dt.Rows[0]["note"].ToString(); } catch { }

                        if (string.IsNullOrEmpty(_defPrinter))
                            _defPrinter = GetDefaultPrinter();

                        if (string.IsNullOrEmpty(_rptUrl) || !Directory.Exists(_rptUrl))
                            _rptUrl = MainClass.ReportsPath;

                        return;
                    }
                }

                // القيم الافتراضية
                _rptUrl = MainClass.ReportsPath;
                _rptName = "rptProductionOrder.repx";
                _defPrinter = MainClass.ReportsPrinter;
            }
            catch { }
        }

        #endregion

        #region Load Data Methods

        public void LoadSafes()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name FROM Safes ORDER BY id", _conn))
                {
                    _storesTable = new DataTable();
                    da.Fill(_storesTable);
                    _storesView = _storesTable.DefaultView;

                    cmbProductionStore.ItemsSource = _storesView;
                    cmbProductionStore.DisplayMemberPath = "name";
                    cmbProductionStore.SelectedValuePath = "id";
                    cmbProductionStore.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المستودعات: " + ex.Message);
            }
        }

        private void LoadUnits()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name FROM units ORDER BY id", _conn))
                {
                    _unitsTable = new DataTable();
                    da.Fill(_unitsTable);
                    _unitsView = _unitsTable.DefaultView;
                }
            }
            catch { }
        }

        private void LoadProductions()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, Code, name, nameEN FROM items " +
                    "WHERE Itemtype=2 ORDER BY id", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbItems.ItemsSource = dt.DefaultView;
                    cmbItems.DisplayMemberPath = "name";
                    cmbItems.SelectedValuePath = "id";
                    cmbItems.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
        }

        private void LoadItemUnits(int itemId)
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT unit AS unitId, units.name AS unitName " +
                    $"FROM ItemUnits LEFT JOIN units ON ItemUnits.unit=units.id " +
                    $"WHERE ItemId={itemId} ORDER BY units.id",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbUnits.ItemsSource = dt.DefaultView;
                    cmbUnits.DisplayMemberPath = "unitName";
                    cmbUnits.SelectedValuePath = "unitId";

                    if (dt.Rows.Count > 0)
                        cmbUnits.SelectedIndex = 0;
                    else
                        cmbUnits.SelectedIndex = -1;
                }
            }
            catch { }
        }

        private void LoadInvNo()
        {
            try
            {
                string invNo = InvoiceOper.InvoiceNo(
                    InvType, ProcType, _invObj.Prefixe).ToString();

                txtNo.Text = invNo;
                txtNo.Background = new SolidColorBrush(Colors.Firebrick);
                txtNo.Foreground = new SolidColorBrush(Colors.White);
            }
            catch { }
        }

        private void LoadComponent()
        {
            try
            {
                _itemsSource.Clear();
                if (cmbItems.SelectedValue == null) return;

                string sql = $@"SELECT ItemComponents.Id,
                                       ItemComponents.ComponentId,
                                       ItemComponents.store,
                                       ItemComponents.itemId,
                                       ItemComponents.quantity,
                                       ItemComponents.price,
                                       ItemComponents.total,
                                       ItemComponents.type,
                                       ItemComponents.unit AS unit_id,
                                       items.id            AS item_ID,
                                       items.Code          AS item_Code,
                                       items.name          AS Item_name
                                FROM ItemComponents
                                LEFT JOIN items ON ItemComponents.ComponentId = items.id
                                WHERE itemId = {cmbItems.SelectedValue}";

                using (var da = new SqlDataAdapter(sql, _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);

                    int rowNo = 1;
                    foreach (DataRow row in dt.Rows)
                    {
                        int componentId = Convert.ToInt32(row["ComponentId"]);
                        int storeId = 0;
                        int unitId = 0;

                        try { storeId = Convert.ToInt32(row["store"]); } catch { }
                        try { unitId = Convert.ToInt32(row["unit_id"]); } catch { }

                        double qty = 0;
                        double price = 0;
                        double perc = 1;

                        try { qty = Convert.ToDouble(row["quantity"]); } catch { }
                        try { price = Convert.ToDouble(row["price"]); } catch { }

                        // الحصول على نسبة تعادل الوحدة
                        try
                        {
                            using (var da2 = new SqlDataAdapter(
                                $"SELECT perc FROM ItemUnits " +
                                $"WHERE ItemId={componentId} AND unit={unitId}",
                                _conn))
                            {
                                var dt2 = new DataTable();
                                da2.Fill(dt2);
                                if (dt2.Rows.Count > 0)
                                    double.TryParse(dt2.Rows[0]["perc"].ToString(), out perc);
                            }
                        }
                        catch { }

                        double stockQty = CalcStock(componentId, storeId);
                        double avgCost = 0;
                        try { avgCost = ItemOper.Cost(componentId); } catch { }

                        _itemsSource.Add(new ProductionItemRow
                        {
                            RowNo = rowNo++,
                            ItemId = componentId,
                            ItemCode = row["item_Code"].ToString(),
                            ItemName = row["Item_name"].ToString(),
                            UnitId = unitId,
                            UnitName = GetUnitName(unitId),
                            StoreId = storeId,
                            StoreName = GetStoreName(storeId),
                            BaseQty = qty,
                            Qty = qty,
                            Price = price,
                            Total = Math.Round(qty * price, 2),
                            StockQty = stockQty,
                            UnitEquality = perc,
                            AvgCost = avgCost
                        });
                    }

                    CalcTot();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المكونات: " + ex.Message);
            }
        }

        private void LoadItemInf(int itemId, ProductionItemRow row)
        {
            try
            {
                int storeId = cmbProductionStore.SelectedValue != null
                    ? Convert.ToInt32(cmbProductionStore.SelectedValue) : 0;

                row.AvgCost = 0;
                row.StockQty = CalcStock(itemId, storeId);
                row.StoreId = storeId;
                row.StoreName = GetStoreName(storeId);
                row.Qty = 1;
                row.Total = Math.Round(row.Qty * row.Price, 2);

                try { row.AvgCost = ItemOper.Cost(itemId); } catch { }

                CalcTot();
            }
            catch { }
        }

        private void LoadUnitInf(ProductionItemRow row, int unitId)
        {
            try
            {
                if (unitId == 0) return;

                using (var da = new SqlDataAdapter(
                    $"SELECT purch, sale, barcode, perc, units.name " +
                    $"FROM units, ItemUnits " +
                    $"WHERE ItemUnits.ItemId={row.ItemId} " +
                    $"AND ItemUnits.unit=units.id " +
                    $"AND units.id={unitId}",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        double.TryParse(dt.Rows[0]["perc"].ToString(), out double perc);
                        row.UnitId = unitId;
                        row.UnitName = GetUnitName(unitId);
                        row.UnitEquality = perc;
                        row.Qty = 1;
                        row.BaseQty = 1;
                        row.Price = ItemOper.Cost(row.ItemId);
                        row.Total = Math.Round(row.Qty * row.Price, 2);
                    }
                    else
                    {
                        // الوحدة الأساسية
                        using (var da2 = new SqlDataAdapter(
                            $"SELECT units.name AS unit, purch_price AS purch, " +
                            $"sale_price AS sale, items.barcode AS barcode, " +
                            $"item, items.unit AS UnitId " +
                            $"FROM Items, units " +
                            $"WHERE items.unit=units.id AND items.id={row.ItemId}",
                            _conn))
                        {
                            var dt2 = new DataTable();
                            da2.Fill(dt2);
                            if (dt2.Rows.Count > 0)
                            {
                                row.UnitId = Convert.ToInt32(dt2.Rows[0]["UnitId"]);
                                row.UnitName = GetUnitName(row.UnitId);
                                row.Qty = 1;
                                row.Price = ItemOper.Cost(row.ItemId);
                                row.Total = Math.Round(row.Qty * row.Price, 2);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        #endregion

        #region ComboBox Events

        private void cmbItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_isLoaded) return;
                if (cmbItems.SelectedIndex <= -1) return;

                _itemsSource.Clear();
                txtQty.Text = "1";

                int itemId = Convert.ToInt32(cmbItems.SelectedValue);

                // تحميل المستودع الافتراضي للصنف
                using (var da = new SqlDataAdapter(
                    $"SELECT Store FROM Items WHERE id={itemId}", _conn1))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        try
                        {
                            int defaultStore = Convert.ToInt32(dt.Rows[0]["store"]);
                            cmbProductionStore.SelectedValue = defaultStore;
                            txtCurrentQty.Text = CalcStock(itemId, defaultStore).ToString("N2");
                        }
                        catch { }
                    }
                }

                // تحميل المكونات وأوحدة الإنتاج
                LoadComponent();
                LoadItemUnits(itemId);
            }
            catch { }
        }

        private void cmbUnits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_isLoaded) return;
                if (cmbUnits.SelectedIndex <= -1) return;
                if (cmbItems.SelectedValue == null) return;

                int itemId = Convert.ToInt32(cmbItems.SelectedValue);
                int unitId = Convert.ToInt32(cmbUnits.SelectedValue);

                using (var da = new SqlDataAdapter(
                    $"SELECT perc FROM ItemUnits " +
                    $"WHERE ItemId={itemId} AND unit={unitId}",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        double.TryParse(dt.Rows[0]["perc"].ToString(), out double perc);
                        double.TryParse(txtQty.Text, out double qty);

                        foreach (var row in _itemsSource)
                        {
                            row.Qty = Math.Round(perc * row.BaseQty * qty, 2);
                            row.Total = Math.Round(row.Qty * row.Price, 2);
                            row.ApplyColor();
                        }
                    }
                }

                CalcTot();
            }
            catch { }
        }

        #endregion

        #region TextChanged Events

        private void txtQty_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtQty.Text))
                {
                    txtQty.Text = "1";
                    return;
                }

                if (!double.TryParse(txtQty.Text, out double qty) || qty <= 0)
                    return;

                foreach (var row in _itemsSource)
                {
                    if (!string.IsNullOrEmpty(row.ItemName) && row.BaseQty > 0)
                    {
                        row.Qty = Math.Round(qty * row.BaseQty * row.UnitEquality, 2);
                        row.Total = Math.Round(row.Qty * row.Price, 2);
                        row.ApplyColor();
                    }
                }

                CalcTot();
            }
            catch { }
        }

        #endregion

        #region Calculation

        public void CalcTot()
        {
            try
            {
                double total = 0;
                int count = 0;

                foreach (var row in _itemsSource)
                {
                    if (!string.IsNullOrEmpty(row.ItemName))
                    {
                        total += row.Total;
                        count++;
                    }
                }

                txtSumVal.Text = $"{Math.Round(total, 2):N2}";
                txtTirmsNo.Text = count.ToString();
                txtTotQuan.Text = count.ToString();
            }
            catch { }
        }

        private int CheckItemUnit(int itemId)
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT id FROM units WHERE defaultInv={InvType}", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        int defaultUnit = Convert.ToInt32(dt.Rows[0]["id"]);
                        using (var da2 = new SqlDataAdapter(
                            $"SELECT purch, sale FROM ItemUnits " +
                            $"WHERE ItemId={itemId} AND unit={defaultUnit}",
                            _conn))
                        {
                            var dt2 = new DataTable();
                            da2.Fill(dt2);
                            if (dt2.Rows.Count > 0) return defaultUnit;
                        }
                    }
                }
            }
            catch { }
            return 0;
        }

        #endregion

        #region DataGrid Events

        private void dgvItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (dgvItems.SelectedItem is ProductionItemRow row
                    && row.ItemId > 0)
                {
                    _rowIndex = _itemsSource.IndexOf(row);
                    _selectedId = row.ItemId;
                    LoadItemDetails(row);
                }
            }
            catch { }
        }

        private void dgvItems_CellEditEnding(object sender,
            DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (dgvItems.SelectedItem is ProductionItemRow row)
                    {
                        row.Total = Math.Round(row.Qty * row.Price, 2);
                        row.ApplyColor();

                        // تحذير إذا كانت الكمية تتجاوز المتوفر
                        if (!_isLoaded && row.Qty > row.StockQty && row.StockQty >= 0)
                        {
                            DXMessageBox.Show(
                                $"الكمية المتوفرة من المادة [{row.ItemName}] غير كافية\n" +
                                $"المتوفر: {row.StockQty}  المطلوب: {row.Qty}",
                                "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }

                        CalcTot();
                    }
                }
                catch { }
            }));
        }

        private void DeleteItemRow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is ProductionItemRow row)
                {
                    var answer = DXMessageBox.Show(
                        "هل تريد حذف المادة؟",
                        "", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (answer == MessageBoxResult.Yes)
                    {
                        _itemsSource.Remove(row);

                        // إعادة ترقيم الصفوف
                        int idx = 1;
                        foreach (var r in _itemsSource)
                            r.RowNo = idx++;

                        CalcTot();
                    }
                }
            }
            catch { }
        }

        private void LoadItemDetails(ProductionItemRow row)
        {
            try
            {
                // مسح القيم السابقة
                txtLastSalePrice.Text = "";
                txtCompetitorPrice.Text = "";
                txtCostAvrg.Text = "";
                txtStock.Text = "";
                txtItemBarcode.Text = "";
                txtRecentPurchPrice.Text = "";

                // تحميل التفاصيل
                try { txtCostAvrg.Text = ItemOper.Cost(row.ItemId).ToString("N2"); } catch { }

                try
                {
                    txtRecentPurchPrice.Text = ItemOper.RecentPurchPrice(
                        row.ItemId).ToString("N2");
                }
                catch { }

                // الكمية المتوفرة بالوحدة المحددة
                if (row.UnitEquality != 0)
                {
                    double stockInUnit = Math.Round(
                        CalcStock(row.ItemId, row.StoreId) / row.UnitEquality, 2);

                    txtStock.Text = stockInUnit.ToString("N2");

                    txtStock.Foreground = stockInUnit > -1
                        ? new SolidColorBrush(Colors.Green)
                        : new SolidColorBrush(Colors.Firebrick);
                }

                // باقي التفاصيل
                txtPerUnit.Text = row.Total.ToString("N2");
                txtTotUnitQuan.Text = row.StoreId.ToString();
                txtItemBarcode.Text = row.BaseQty.ToString();
            }
            catch { }
        }

        #endregion

        #region Search

        private void btnSearch_Click(object sender, RoutedEventArgs e) => Search();

        private void txtSrchNo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) Search();
        }

        private void Search()
        {
            try
            {
                string branchCond = (MainClass.BranchNo != -1)
                    ? $"inv.branch={MainClass.BranchNo} AND " : "";

                string condition;

                if (string.IsNullOrWhiteSpace(txtSrchNo.Text))
                    condition = branchCond + " date>=@date1 AND date<=@date2 AND ";
                else
                    condition = branchCond + $" inv.id={txtSrchNo.Text} AND ";

                LoadGrid(condition);
                TabControl1.SelectedIndex = 1;
            }
            catch { }
        }

        private void LoadGrid(string cond)
        {
            _searchSource.Clear();
            try
            {
                string sql = $@"SELECT Inv.InvGlobalID,
                                       Inv.id   AS id,
                                       Inv.date AS date,
                                       Inv.Reff_No,
                                       Inv.cust_id
                                FROM Inv
                                WHERE inv_type  = {InvType}
                                  AND proc_type = {ProcType}
                                  AND {cond}
                                       Inv.IS_Deleted = 0
                                ORDER BY Inv.id";

                using (var da = new SqlDataAdapter(sql, _conn))
                {
                    // إضافة البراميترات فقط عند البحث بالتاريخ
                    if (cond.Contains("@date1"))
                    {
                        da.SelectCommand.Parameters.Add(
                            "@date1", SqlDbType.DateTime).Value =
                            txtFromDate.DateTime.ToShortDateString();

                        da.SelectCommand.Parameters.Add(
                            "@date2", SqlDbType.DateTime).Value =
                            txtToDate.DateTime.AddHours(24);
                    }

                    var dt = new DataTable();
                    da.Fill(dt);

                    foreach (DataRow row in dt.Rows)
                    {
                        _searchSource.Add(new SearchResultRow
                        {
                            InvGlobalID = row["InvGlobalID"].ToString(),
                            id = Convert.ToInt32(row["id"]),
                            vDate = Convert.ToDateTime(
                                row["date"]).ToShortDateString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في البحث: " + ex.Message);
            }
        }

        private void dgvSrch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (dgvSrch.SelectedItem is SearchResultRow row)
                {
                    _invGlobalID = row.InvGlobalID;
                    Navigate($"SELECT * FROM Inv WHERE InvGlobalID=N'{_invGlobalID}'");
                    TabControl1.SelectedIndex = 0;
                }
            }
            catch { }
        }

        #endregion

        #region Navigation

        public void Navigate(string sqlQuery)
        {
            try
            {
                EnsureOpen(_conn);
                using (var cmd = new SqlCommand(sqlQuery, _conn))
                using (var dr = cmd.ExecuteReader())
                    ReadData(dr);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التنقل: " + ex.Message);
            }
            finally
            {
                CloseConn(_conn);
            }
        }

        private void ReadData(SqlDataReader dr)
        {
            if (!dr.HasRows) { CLR(); return; }
            dr.Read();
            CLR();

            _isLoaded = true;

            // ═══ تحميل بيانات الفاتورة ═══
            try { _procCode = Convert.ToInt32(dr["proc_id"]); } catch { }
            try { _invGlobalID = dr["InvGlobalID"].ToString(); } catch { }
            try { _code = Convert.ToInt32(dr["id"]); } catch { }
            txtNo.Text = _code.ToString();

            try
            {
                if (DateTime.TryParse(dr["date"].ToString(), out DateTime vDate))
                    txtDate.DateTime = vDate;
            }
            catch { }

            try { InvType = Convert.ToInt32(dr["inv_type"]); } catch { }
            try { ProcType = Convert.ToInt32(dr["proc_type"]); } catch { }

            try
            {
                cmbProductionStore.SelectedValue = Convert.ToInt32(dr["safe"]);
            }
            catch { }

            try { txtSumVal.Text = dr["InvTotal"].ToString(); } catch { }
            try { _restSaleCode = Convert.ToInt32(dr["EntryID"]); } catch { }
            try { txtNote.Text = dr["notes"].ToString(); } catch { }

            // تاريخ ورقم المرجع
            try
            {
                if (DateTime.TryParse(dr["Reff_date"].ToString(), out DateTime refDate))
                    txtRefDate.DateTime = refDate;
            }
            catch { }

            try { txtRefNo.Text = dr["Reff_No"].ToString(); } catch { }

            dr.Close();

            // ═══ تحميل الصنف المنتج ═══
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT * FROM Inv_Sub " +
                    $"WHERE productId=0 AND InvGlobalID=N'{_invGlobalID}'",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        try
                        {
                            cmbItems.SelectedValue = Convert.ToInt32(dt.Rows[0]["ItemId"]);
                        }
                        catch { }

                        try
                        {
                            if (dt.Rows[0]["store"] != null &&
                                Convert.ToDouble(dt.Rows[0]["store"]) > 0)
                            {
                                cmbProductionStore.SelectedValue =
                                    Convert.ToInt32(dt.Rows[0]["store"]);
                                cmbUnits.SelectedValue =
                                    Convert.ToInt32(dt.Rows[0]["unit"]);
                            }
                        }
                        catch { }

                        try { txtQty.Text = dt.Rows[0]["val1"].ToString(); } catch { }
                    }
                }
            }
            catch { }

            // ═══ تحميل مكونات الإنتاج ═══
            _itemsSource.Clear();

            try
            {
                using (var da2 = new SqlDataAdapter(
                    $"SELECT * FROM Inv_Sub " +
                    $"WHERE productId>0 AND InvGlobalID=N'{_invGlobalID}'",
                    _conn))
                {
                    var dt2 = new DataTable();
                    da2.Fill(dt2);
                    int rowNo = 1;

                    foreach (DataRow row in dt2.Rows)
                    {
                        try
                        {
                            int itemId = Convert.ToInt32(row["ItemId"]);
                            double qty = Math.Round(Convert.ToDouble(row["val1"]), 2);
                            double price = Math.Round(Convert.ToDouble(row["exchange_price"]), 2);
                            double total = Math.Round(qty * price, 2);
                            int unitId = 0;
                            int storeId = 0;

                            try { unitId = Convert.ToInt32(row["unit"]); } catch { }
                            try
                            {
                                if (row["store"] != null &&
                                    Convert.ToDouble(row["store"]) > 0)
                                    storeId = Convert.ToInt32(row["store"]);
                            }
                            catch { }

                            double avgCost = 0;
                            try { avgCost = ItemOper.Cost(itemId); } catch { }

                            _itemsSource.Add(new ProductionItemRow
                            {
                                RowNo = rowNo++,
                                ItemId = itemId,
                                ItemCode = GetItemCode(itemId),
                                ItemName = GetCurrencyName(itemId),
                                UnitId = unitId,
                                UnitName = GetUnitName(unitId),
                                StoreId = storeId,
                                StoreName = GetStoreName(storeId),
                                Qty = qty,
                                Price = price,
                                Total = total,
                                StockQty = CalcStock(itemId, storeId),
                                AvgCost = avgCost
                            });
                        }
                        catch { }
                    }
                }
            }
            catch { }

            CalcTot();
            _isLoaded = false;
        }

        #endregion

        #region Navigation Buttons

        private void btnFirst_Click(object sender, RoutedEventArgs e)
            => Navigate(
                $"SELECT TOP 1 * FROM Inv " +
                $"WHERE inv_type={InvType} AND proc_type={ProcType} " +
                $"AND IS_Deleted=0 ORDER BY id ASC");

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
            => Navigate(
                $"SELECT TOP 1 * FROM Inv " +
                $"WHERE inv_type={InvType} AND proc_type={ProcType} " +
                $"AND IS_Deleted=0 AND id<{_code} ORDER BY id DESC");

        private void btnNext_Click(object sender, RoutedEventArgs e)
            => Navigate(
                $"SELECT TOP 1 * FROM Inv " +
                $"WHERE inv_type={InvType} AND proc_type={ProcType} " +
                $"AND IS_Deleted=0 AND id>{_code} ORDER BY id ASC");

        private void btnLast_Click(object sender, RoutedEventArgs e)
            => Navigate(
                $"SELECT TOP 1 * FROM Inv " +
                $"WHERE inv_type={InvType} AND proc_type={ProcType} " +
                $"AND IS_Deleted=0 ORDER BY id DESC");

        #endregion

        #region Action Buttons

        private void btnNew_Click(object sender, RoutedEventArgs e) => CLR();

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            _isPrinted = false;
            Save();
        }

        private void btnSavePrint_Click(object sender, RoutedEventArgs e)
        {
            _isPrinted = true;
            Save();
            PrintDevexpress(1);
            CLR();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => PrintDevexpress(1);

        private void btnView_Click(object sender, RoutedEventArgs e)
            => PrintDevexpress(2);

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_code == -1)
                {
                    DXMessageBox.Show("اختر أمر إنتاج ليتم حذفه",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var answer = DXMessageBox.Show(
                    "هل أنت متأكد من حذف الفاتورة؟",
                    "", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (answer != MessageBoxResult.Yes) return;

                EnsureOpen(_conn);

                using (var cmd = new SqlCommand(
                    $"UPDATE Inv SET IS_Deleted=1 " +
                    $"WHERE InvGlobalID=N'{_invGlobalID}'", _conn))
                    cmd.ExecuteNonQuery();

                if (_restSaleCode != 0)
                {
                    using (var cmd2 = new SqlCommand(
                        $"UPDATE Entry SET state=2 " +
                        $"WHERE id={_restSaleCode}", _conn))
                        cmd2.ExecuteNonQuery();
                }

                DXMessageBox.Show(
                    MainClass.Language == "ar" ? "تم الحذف" : "Deleted",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);

                _searchSource.Clear();
                CLR();
            }
            catch (Exception ex)
            {
                string msg = MainClass.Language == "ar"
                    ? $"خطأ أثناء الحذف\nتفاصيل الخطأ: {ex.Message}"
                    : $"error in delete\nerror details: {ex.Message}";
                DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { CloseConn(_conn); }
        }

        #endregion

        #region Add New Item

        private void AddNewItem()
        {
            try
            {
                var srch = new frmItemsSrch();
                MainClass.ApplyPermissionToForm(srch);
                MainClass.DoApplyUserSett(srch);
                srch.sql = "SELECT id, name, nameEN, sale_price, unit " +
                                "FROM Items WHERE IS_Deleted=0 ORDER BY id";
                srch.search = "SELECT id, name, nameEN, sale_price, unit FROM Items";
                srch.Itemname = "";
                srch.StoreId = cmbProductionStore.SelectedValue != null
                    ? Convert.ToInt32(cmbProductionStore.SelectedValue) : 0;
                srch.txtSrchNm.Text = SrchName;
                srch.ShowDialog();

                if (srch.ISDone && srch.ItemId > 0)
                {
                    foreach (int itemId in srch.Itemlist)
                    {
                        _selectedId = itemId;
                        SearchByID(itemId);
                    }
                    _rowIndex = _itemsSource.Count - 1;
                }
            }
            catch { }
        }

        private void SearchByID(int itemId)
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT name, id, Code FROM Items " +
                    $"WHERE IS_Deleted=0 AND id={itemId}", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count == 0) { AddNewItem(); return; }

                    // التحقق من وجود الصنف مسبقًا
                    bool exists = false;
                    int existIndex = -1;

                    for (int i = 0; i < _itemsSource.Count; i++)
                    {
                        if (_itemsSource[i].ItemId == itemId)
                        {
                            exists = true;
                            existIndex = i;
                            break;
                        }
                    }

                    if (exists)
                    {
                        // عرض نافذة الاختيار (إضافة أو تعديل الكمية)
                        var msgExist = new MsgExistItem();
                        msgExist.ShowDialog();

                        if (msgExist.Action == 1)
                        {
                            // إضافة للكمية الموجودة
                            _itemsSource[existIndex].Qty += 1;
                            _itemsSource[existIndex].Total =
                                Math.Round(_itemsSource[existIndex].Qty *
                                           _itemsSource[existIndex].Price, 2);
                        }
                        else if (msgExist.Action == 2)
                        {
                            // إضافة صف جديد
                            AppendItemRow(itemId, dt.Rows[0]);
                        }
                    }
                    else
                    {
                        AppendItemRow(itemId, dt.Rows[0]);
                    }

                    CalcTot();
                }
            }
            catch { }
        }

        private void AppendItemRow(int itemId, DataRow dataRow)
        {
            int storeId = cmbProductionStore.SelectedValue != null
                ? Convert.ToInt32(cmbProductionStore.SelectedValue) : 0;

            int unitId = CheckItemUnit(itemId);

            double perc = 1;
            if (unitId > 0)
            {
                try
                {
                    using (var da = new SqlDataAdapter(
                        $"SELECT perc FROM ItemUnits " +
                        $"WHERE ItemId={itemId} AND unit={unitId}", _conn))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        if (dt.Rows.Count > 0)
                            double.TryParse(dt.Rows[0]["perc"].ToString(), out perc);
                    }
                }
                catch { }
            }

            double cost = 0;
            try { cost = ItemOper.Cost(itemId); } catch { }

            var newRow = new ProductionItemRow
            {
                RowNo = _itemsSource.Count + 1,
                ItemId = itemId,
                ItemCode = dataRow["Code"].ToString(),
                ItemName = dataRow["name"].ToString(),
                UnitId = unitId,
                UnitName = GetUnitName(unitId),
                StoreId = storeId,
                StoreName = GetStoreName(storeId),
                BaseQty = 1,
                Qty = 1,
                Price = cost,
                Total = Math.Round(1 * cost, 2),
                StockQty = CalcStock(itemId, storeId),
                UnitEquality = perc,
                AvgCost = cost
            };

            newRow.ApplyColor();
            _itemsSource.Add(newRow);
        }

        #endregion

        #region Save

        private void Save()
        {
            // ═══ التحقق من توفر الكميات ═══
            foreach (var row in _itemsSource)
            {
                if (string.IsNullOrEmpty(row.ItemName)) continue;
                if (row.Qty > row.StockQty && row.StockQty >= 0)
                {
                    DXMessageBox.Show(
                        "لا يمكن إستكمال العملية لعدم توفر كمية كافية من المكونات للإنتاج",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            SqlTransaction transaction = null;
            try
            {
                EnsureOpen(_conn);
                EnsureOpen(_conn1);
                transaction = _conn.BeginTransaction();

                // ═══ التحقق من النسخة التجريبية ═══
                if (MainClass.IsTrial && _procCode == -1)
                {
                    using (var da = new SqlDataAdapter("SELECT id FROM Entry", _conn1))
                    {
                        var trialDt = new DataTable();
                        da.Fill(trialDt);
                        if (trialDt.Rows.Count >= 20)
                        {
                            string trialMsg = MainClass.Language == "ar"
                                ? "نأسف لقد وصلت لأقصى حد إدخال للنسخة التجريبية."
                                : "Sorry, you reached the maximum entries for the trial version.";
                            DXMessageBox.Show(trialMsg, "",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                    }
                }

                if (string.IsNullOrEmpty(txtSumVal.Text))
                    txtSumVal.Text = "0";

                bool isUpdate = false;
                string invGlobalID = "";

                SqlCommand cmd;

                // ═══ تحديد إدراج أو تعديل ═══
                if (_procCode != -1)
                {
                    InvoiceOper.GetInvoiceGlobalID(ref invGlobalID, ref _procCode);

                    new SqlCommand(
                        $"DELETE FROM Inv_Sub WHERE InvGlobalID='{invGlobalID}'",
                        _conn, transaction).ExecuteNonQuery();

                    isUpdate = true;
                    cmd = new SqlCommand(StoredQueries.UpdateInv, _conn, transaction);
                }
                else
                {
                    using (var maxCmd = new SqlCommand(
                        "SELECT MAX(proc_id) FROM Inv", _conn, transaction))
                    {
                        object r = maxCmd.ExecuteScalar();
                        _procCode = (r != DBNull.Value)
                            ? Convert.ToInt32(r) + 1 : 1;
                    }

                    InvoiceOper.GetInvoiceGlobalID(ref invGlobalID, ref _procCode);
                    LoadInvNo();
                    int.TryParse(txtNo.Text, out _code);
                    cmd = new SqlCommand(StoredQueries.InsertInv, _conn, transaction);
                }

                // ═══ بناء رقم الفاتورة المركب ═══
                string invCombinedId =
                    MainClass.BranchCode +
                    _invObj.InvoiceCode +
                    ProcType.ToString() +
                    _code.ToString();

                // ═══ إضافة Parameters الفاتورة ═══
                double.TryParse(txtSumVal.Text, out double sumVal);

                cmd.Parameters.Add("@InvCombinedId", SqlDbType.NVarChar).Value = invCombinedId;
                cmd.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = invGlobalID;
                cmd.Parameters.Add("@CloudID", SqlDbType.NVarChar).Value = "";
                cmd.Parameters.Add("@proc_type", SqlDbType.Int).Value = ProcType;
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = _code;
                cmd.Parameters.Add("@date", SqlDbType.DateTime).Value = txtDate.DateTime;
                cmd.Parameters.Add("@inv_type", SqlDbType.Int).Value = InvType;
                cmd.Parameters.Add("@OrderType", SqlDbType.Int).Value = 0;
                cmd.Parameters.Add("@safe", SqlDbType.Int).Value =
                    cmbProductionStore.SelectedValue ?? (object)DBNull.Value;
                cmd.Parameters.Add("@stock", SqlDbType.Int).Value = -1;
                cmd.Parameters.Add("@cust_id", SqlDbType.Int).Value = -1;
                cmd.Parameters.Add("@sales_emp", SqlDbType.Int).Value = MainClass.EmpNo;
                cmd.Parameters.Add("@InvTotal", SqlDbType.Float).Value = sumVal;
                cmd.Parameters.Add("@AdditionsTot", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@Insurance", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@tot_net", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@InvProfit", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@paid", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@minus", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@tax", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@EntryID", SqlDbType.Int).Value = 0;
                cmd.Parameters.Add("@branch", SqlDbType.Int).Value = MainClass.BranchNo;
                cmd.Parameters.Add("@IS_Buy", SqlDbType.Bit).Value = (InvType == 2) ? 0 : 1;
                cmd.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;
                cmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value =
                    string.IsNullOrWhiteSpace(txtNote.Text) ? " " : txtNote.Text;

                // المرجع
                double.TryParse(txtRefNo.Text, out double refNo);
                if ((InvType == 2 && ProcType == 1) || ProcType == 2)
                {
                    cmd.Parameters.Add("@Reff_No ", SqlDbType.Int).Value = (int)refNo;
                    cmd.Parameters.Add("@Reff_date ", SqlDbType.DateTime).Value = txtRefDate.DateTime;
                }
                else
                {
                    cmd.Parameters.Add("@Reff_No ", SqlDbType.Int).Value = -1;
                    cmd.Parameters.Add("@Reff_date ", SqlDbType.DateTime).Value = txtRefDate.DateTime;
                }

                cmd.Parameters.Add("@salesman", SqlDbType.Int).Value = -1;
                cmd.Parameters.Add("@pay_type", SqlDbType.Int).Value = -1;
                cmd.Parameters.Add("@cash", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@visa", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@bank", SqlDbType.Int).Value = -1;
                cmd.Parameters.Add("@ExtraVAT", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@Sync", SqlDbType.Bit).Value = 0;
                cmd.Parameters.Add("@AdditionalCost", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@InvoiceStatus", SqlDbType.Int).Value = 3;
                cmd.Parameters.Add("@PriceIncVAT", SqlDbType.Bit).Value = 1;
                cmd.Parameters.Add("@PaymentStatus", SqlDbType.Int).Value = -1;
                cmd.Parameters.Add("@CurrencyCode ", SqlDbType.NVarChar).Value = _invObj.Currency.ToString();
                cmd.Parameters.Add("@ItemsDiscount", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@InvCost", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@FreeVATSales", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@InvSum", SqlDbType.Float).Value = sumVal;
                cmd.Parameters.Add("@VATPercent", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@CashCustomerName", SqlDbType.NVarChar).Value = "";
                cmd.Parameters.Add("@CashCustomerMobile", SqlDbType.NVarChar).Value = "";
                cmd.Parameters.Add("@QRCode", SqlDbType.NVarChar).Value = "";
                cmd.Parameters.Add("@InvoiceHash", SqlDbType.NVarChar).Value = "";
                cmd.Parameters.Add("@UUID", SqlDbType.NVarChar).Value = "";
                cmd.Parameters.Add("@ZatcaSent", SqlDbType.Bit).Value = false;
                cmd.Parameters.Add("@TableNo", SqlDbType.NVarChar).Value = "";
                cmd.ExecuteNonQuery();

                // ═══ إدراج الصنف المنتج (proc_type=1) ═══
                double.TryParse(txtQty.Text, out double prodQty);
                double.TryParse(txtCurrentQty.Text, out double currentQty);
                double exchPrice = prodQty > 0 ? sumVal / prodQty : 0;

                var subCmd = new SqlCommand(StoredQueries.InsertInvSub, _conn, transaction);
                subCmd.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = invGlobalID;
                subCmd.Parameters.Add("@proc_id", SqlDbType.Int).Value = _procCode;
                subCmd.Parameters.Add("@proc_type", SqlDbType.Int).Value = 1;
                subCmd.Parameters.Add("@expire_date", SqlDbType.DateTime).Value = DBNull.Value;
                subCmd.Parameters.Add("@Store", SqlDbType.Float).Value =
                    cmbProductionStore.SelectedValue ?? (object)DBNull.Value;
                subCmd.Parameters.Add("@ItemId", SqlDbType.Int).Value =
                    cmbItems.SelectedValue ?? (object)DBNull.Value;
                subCmd.Parameters.Add("@unit", SqlDbType.Int).Value =
                    cmbUnits.SelectedValue ?? (object)DBNull.Value;
                subCmd.Parameters.Add("@UnitEquality", SqlDbType.Float).Value = 1;
                subCmd.Parameters.Add("@val", SqlDbType.Float).Value = prodQty;
                subCmd.Parameters.Add("@val1", SqlDbType.Float).Value = prodQty;
                subCmd.Parameters.Add("@exchange_price", SqlDbType.Float).Value = exchPrice;
                subCmd.Parameters.Add("@discount", SqlDbType.Float).Value = 0;
                subCmd.Parameters.Add("@taxperc", SqlDbType.Float).Value = 0;
                subCmd.Parameters.Add("@taxval", SqlDbType.Float).Value = 0;
                subCmd.Parameters.Add("@Description", SqlDbType.NVarChar).Value =
                    $" منتج من {cmbItems.Text}";
                subCmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value = "";
                subCmd.Parameters.Add("@ProductId", SqlDbType.Int).Value = 0;
                subCmd.Parameters.Add("@AvrgCost", SqlDbType.Float).Value = exchPrice;
                subCmd.Parameters.Add("@CurrentQnty", SqlDbType.Float).Value = currentQty;
                subCmd.Parameters.Add("@BranchID", SqlDbType.Int).Value = MainClass.BranchNo;
                subCmd.Parameters.Add("@ItemAddedCost", SqlDbType.Float).Value = 0;
                subCmd.Parameters.Add("@ItemPriceWithoutVAT", SqlDbType.Float).Value = exchPrice;
                subCmd.Parameters.Add("@ItemCostCenter", SqlDbType.NVarChar).Value = "";
                subCmd.ExecuteNonQuery();

                // ═══ إدراج مكونات الإنتاج (proc_type=2) ═══
                foreach (var row in _itemsSource)
                {
                    if (string.IsNullOrEmpty(row.ItemName)) continue;

                    var compCmd = new SqlCommand(
                        StoredQueries.InsertInvSub, _conn, transaction);

                    compCmd.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = invGlobalID;
                    compCmd.Parameters.Add("@CloudID", SqlDbType.NVarChar).Value = "";
                    compCmd.Parameters.Add("@proc_id", SqlDbType.Int).Value = _procCode;
                    compCmd.Parameters.Add("@proc_type", SqlDbType.Int).Value = 2;
                    compCmd.Parameters.Add("@expire_date", SqlDbType.DateTime).Value = DBNull.Value;
                    compCmd.Parameters.Add("@Store", SqlDbType.Float).Value = row.StoreId;
                    compCmd.Parameters.Add("@ItemId", SqlDbType.Int).Value = row.ItemId;
                    compCmd.Parameters.Add("@unit", SqlDbType.Int).Value = row.UnitId;
                    compCmd.Parameters.Add("@UnitEquality", SqlDbType.Float).Value = 1;
                    compCmd.Parameters.Add("@val", SqlDbType.Float).Value = row.Qty;
                    compCmd.Parameters.Add("@val1", SqlDbType.Float).Value = row.Qty;
                    compCmd.Parameters.Add("@exchange_price", SqlDbType.Float).Value = row.Price;
                    compCmd.Parameters.Add("@discount", SqlDbType.Float).Value = 0;
                    compCmd.Parameters.Add("@taxperc", SqlDbType.Float).Value = 0;
                    compCmd.Parameters.Add("@taxval", SqlDbType.Float).Value = 0;
                    compCmd.Parameters.Add("@Description", SqlDbType.NVarChar).Value = row.UnitId.ToString();
                    compCmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value = "";
                    compCmd.Parameters.Add("@ProductId", SqlDbType.Int).Value =
                        cmbItems.SelectedValue ?? (object)DBNull.Value;
                    compCmd.Parameters.Add("@AvrgCost", SqlDbType.Float).Value = row.AvgCost;
                    compCmd.Parameters.Add("@BranchID", SqlDbType.Int).Value = MainClass.BranchNo;
                    compCmd.Parameters.Add("@CurrentQnty", SqlDbType.Float).Value =
                        row.StockQty - row.BaseQty;
                    compCmd.Parameters.Add("@ItemAddedCost", SqlDbType.Float).Value = 0;
                    compCmd.Parameters.Add("@ItemCostCenter", SqlDbType.NVarChar).Value = "";
                    compCmd.Parameters.Add("@ItemAdditionalTax", SqlDbType.Int).Value = 0;
                    compCmd.Parameters.Add("@ItemAdditionalTaxPerc", SqlDbType.Int).Value = 0;
                    compCmd.ExecuteNonQuery();
                }

                transaction.Commit();

                // ═══ رسالة النجاح ═══
                var savedMsg = new frmSavedMsg();
                if (isUpdate) savedMsg.lblSave.Text = "تم حفظ التعديلات بنجاح...";
                savedMsg.ShowDialog();

                if (savedMsg.Pressed == 1)
                {
                    if (!_isPrinted)
                    {
                        _itemsSource.Clear();
                        txtSumVal.Text = "0";
                        LoadInvNo();
                        _isReturn = false;
                        CLR();
                    }
                }
                else if (savedMsg.Pressed == 3 && !_isPrinted)
                {
                    _itemsSource.Clear();
                    _isReturn = false;
                    CLR();
                }
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                string msg = MainClass.Language == "ar"
                    ? $"خطأ أثناء الحفظ\nتفاصيل الخطأ: {ex.Message}"
                    : $"error in saving\nError details: {ex.Message}";
                DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConn(_conn);
                CloseConn(_conn1);
            }
        }

        #endregion

        #region CLR (Clear Form)

        private void CLR()
        {
            try
            {
                txtNote.Text = "";
                txtSumVal.Text = "0";
                txtQty.Text = "1";
                txtRefNo.Text = "";

                txtDate.DateTime = DateTime.Now;
                txtToDate.DateTime = DateTime.Now;
                txtRefDate.DateTime = DateTime.Now;
                txtFromDate.DateTime = DateTime.Now;

                _code = -1;
                _procCode = -1;
                _selectedId = -1;
                _invGlobalID = "-1";
                _isPrinted = false;
                _isReturn = false;

                _invTax = 0;
                _invDiscount = 0;
                _totAfterDisc = 0;

                cmbItems.SelectedIndex = -1;
                cmbUnits.SelectedIndex = -1;
                cmbProductionStore.SelectedIndex = -1;

                txtStock.Foreground = new SolidColorBrush(Colors.Black);
                txtTirmsNo.Text = "";
                txtTotQuan.Text = "";
                txtCostAvrg.Text = "";
                txtRecentPurchPrice.Text = "";
                txtItemBarcode.Text = "";
                txtStock.Text = "";
                txtPerUnit.Text = "";
                txtTotUnitQuan.Text = "";
                txtCurrentQty.Text = "";

                _itemsSource.Clear();
                _isLoaded = false;

                LoadInvNo();
            }
            catch { }
        }

        #endregion

        #region Print

        private DataSet BindToData()
        {
            string invType = "أمر إنتاج";
            string invDate = txtDate.DateTime.ToShortDateString();
            string invTime = DateTime.Now.ToString("hh:mm tt");
            string user = GetSalesEmpNameByInvNo();

            double.TryParse(txtSumVal.Text, out double sumVal);
            double total = Math.Round(sumVal, 2);

            string address = "", telePhone = "", mobile = "";
            string foundation = "", field = "", vatNo = "";

            try
            {
                using (var da = new SqlDataAdapter("SELECT * FROM Foundation", _conn1))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        address = dt.Rows[0]["Address"].ToString();
                        telePhone = dt.Rows[0]["Tel"].ToString();
                        mobile = dt.Rows[0]["Mobile"].ToString();
                        foundation = dt.Rows[0]["nameA"].ToString();
                        field = dt.Rows[0]["FieldA"].ToString();
                        vatNo = dt.Rows[0]["tax_no"].ToString();
                    }
                }
            }
            catch { }

            string invNote = string.IsNullOrEmpty(txtNote.Text) ? "" : txtNote.Text;

            var list = new List<InvoiceData>();

            foreach (var row in _itemsSource)
            {
                if (string.IsNullOrEmpty(row.ItemName)) continue;

                list.Add(new InvoiceData
                {
                    ItemNo = row.ItemId.ToString(),
                    ItemName = row.ItemName,
                    Description = row.UnitName,
                    Quantity = row.Qty.ToString(),
                    Unit = row.BaseQty.ToString(),
                    Price = row.Price.ToString("N2"),
                    Total = row.Total.ToString("N2"),
                    SumPrice = total.ToString("N2"),
                    Additions = "0",
                    PayNetwork = "0",
                    Insurance = "0",
                    Paycash = "0",
                    Remainder = "0",
                    InvoiceNo = txtNo.Text,
                    InvoiceType = invType,
                    OrderNo = "1",
                    OrderType = " ",
                    InvTime = invTime,
                    InvDate = invDate,
                    User = user,
                    Customer = "",
                    Saleman = "",
                    CustVATno = "",
                    InvNote = invNote,
                    CustAccCode = "",
                    PrintNote = _printNote,
                    Address = address,
                    Mobile = mobile,
                    TelePhone = telePhone,
                    VatNo = vatNo,
                    Foundation = foundation,
                    Field = field,
                    VATPerc = _defVAT.ToString(),
                    Logo = "",
                    Header = "",
                    footer = "",
                    Stamp = ""
                });
            }

            var ds = new DataSet("Name");
            ds.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        private void PrintDevexpress(int printType)
        {
            _rptName = "rptProductionOrder.repx";

            if (_itemsSource.Count == 0)
            {
                DXMessageBox.Show("لا توجد مكونات بالجدول",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_rptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير من الإعدادات",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string fullPath = Path.Combine(_rptUrl, _rptName);

            if (!Directory.Exists(_rptUrl) || !File.Exists(fullPath))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_rptName))
            {
                DXMessageBox.Show("يجب إدخال اسم التقرير من الإعدادات",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var report = XtraReport.FromFile(fullPath);
                report.DataSource = BindToData();

                // Header Sub-report
                string headerPath = Path.Combine(_rptUrl, "header.repx");
                if (File.Exists(headerPath))
                {
                    var headerRpt = XtraReport.FromFile(headerPath);
                    headerRpt.DataSource = Common.FoundationInfoDT;
                    var subHeader = report.FindControl("headerRpt", true) as XRSubreport;
                    if (subHeader != null) subHeader.ReportSource = headerRpt;
                }

                // Footer Sub-report
                string footerPath = Path.Combine(_rptUrl, "footer.repx");
                if (File.Exists(footerPath))
                {
                    var footerRpt = XtraReport.FromFile(footerPath);
                    footerRpt.DataSource = Common.FoundationInfoDT;
                    var subFooter = report.FindControl("footerRpt", true) as XRSubreport;
                    if (subFooter != null) subFooter.ReportSource = footerRpt;
                }

                if (string.IsNullOrEmpty(_defPrinter))
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                report.PrinterName = _defPrinter;

                if (printType == 1)
                {
                    for (int i = 0; i < _printNo; i++)
                        report.Print();
                }
                else
                {
                    report.ShowPreviewDialog();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في الطباعة: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helper Methods

        private double CalcStock(int itemId, int storeId)
        {
            try
            {
                return Inventory.CalcItemStock(storeId, itemId, MainClass.BranchNo);
            }
            catch { return 0; }
        }

        private string GetSalesEmpNameByInvNo()
        {
            try
            {
                if (_procCode != -1)
                {
                    using (var da = new SqlDataAdapter(
                        $"SELECT users.username " +
                        $"FROM Employees, Inv, users " +
                        $"WHERE users.emp=Employees.id " +
                        $"AND Employees.id=Inv.sales_emp " +
                        $"AND InvGlobalID=N'{_invGlobalID}'",
                        _conn1))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
                    }
                }
                return MainClass.UserName;
            }
            catch { return ""; }
        }

        private string GetCurrencyName(int itemId)
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT name FROM Items WHERE id={itemId}", _conn1))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
                }
            }
            catch { return ""; }
        }

        private string GetItemCode(int itemId)
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT Code FROM Items WHERE id={itemId}", _conn1))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
                }
            }
            catch { return ""; }
        }

        private string GetUnitName(int unitId)
        {
            if (unitId <= 0) return "";
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT name FROM units WHERE id={unitId}", _conn1))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
                }
            }
            catch { return ""; }
        }

        private string GetStoreName(int storeId)
        {
            if (storeId <= 0) return "";
            try
            {
                // البحث من الجدول المحمل في الذاكرة أولًا (أسرع)
                if (_storesTable != null)
                {
                    var rows = _storesTable.Select($"id={storeId}");
                    if (rows.Length > 0) return rows[0]["name"].ToString();
                }

                using (var da = new SqlDataAdapter(
                    $"SELECT name FROM Safes WHERE id={storeId}", _conn1))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
                }
            }
            catch { return ""; }
        }

        private string GetDefaultPrinter()
        {
            try
            {
                var ps = new PrinterSettings();
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    ps.PrinterName = printer;
                    if (ps.IsDefaultPrinter) return printer;
                }
            }
            catch { }
            return "";
        }

        private void EnsureOpen(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
        }

        private void CloseConn(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Closed)
                conn.Close();
        }

        #endregion
    }

    #region Models

    /// <summary>
    /// Model صف مكون الإنتاج - مع INotifyPropertyChanged للربط التلقائي
    /// </summary>
    public class ProductionItemRow : INotifyPropertyChanged
    {
        #region Private Fields
        private int _rowNo;
        private int _itemId;
        private string _itemCode = "";
        private string _itemName = "";
        private int _unitId;
        private string _unitName = "";
        private int _storeId;
        private string _storeName = "";
        private double _baseQty;
        private double _qty;
        private double _price;
        private double _total;
        private double _stockQty;
        private double _unitEquality = 1;
        private double _avgCost;
        private System.Windows.Media.Brush _qtyBackground =
            new SolidColorBrush(System.Windows.Media.Colors.White);
        #endregion

        #region Properties

        public int RowNo
        {
            get => _rowNo;
            set { _rowNo = value; OnPropertyChanged(nameof(RowNo)); }
        }

        public int ItemId
        {
            get => _itemId;
            set { _itemId = value; OnPropertyChanged(nameof(ItemId)); }
        }

        public string ItemCode
        {
            get => _itemCode;
            set { _itemCode = value; OnPropertyChanged(nameof(ItemCode)); }
        }

        public string ItemName
        {
            get => _itemName;
            set { _itemName = value; OnPropertyChanged(nameof(ItemName)); }
        }

        public int UnitId
        {
            get => _unitId;
            set { _unitId = value; OnPropertyChanged(nameof(UnitId)); }
        }

        public string UnitName
        {
            get => _unitName;
            set { _unitName = value; OnPropertyChanged(nameof(UnitName)); }
        }

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

        public double BaseQty
        {
            get => _baseQty;
            set { _baseQty = value; OnPropertyChanged(nameof(BaseQty)); }
        }

        public double Qty
        {
            get => _qty;
            set
            {
                _qty = value;
                OnPropertyChanged(nameof(Qty));
                ApplyColor();
            }
        }

        public double Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(nameof(Price)); }
        }

        public double Total
        {
            get => _total;
            set { _total = value; OnPropertyChanged(nameof(Total)); }
        }

        public double StockQty
        {
            get => _stockQty;
            set { _stockQty = value; OnPropertyChanged(nameof(StockQty)); }
        }

        public double UnitEquality
        {
            get => _unitEquality;
            set { _unitEquality = value; OnPropertyChanged(nameof(UnitEquality)); }
        }

        public double AvgCost
        {
            get => _avgCost;
            set { _avgCost = value; OnPropertyChanged(nameof(AvgCost)); }
        }

        public System.Windows.Media.Brush QtyBackground
        {
            get => _qtyBackground;
            set { _qtyBackground = value; OnPropertyChanged(nameof(QtyBackground)); }
        }

        #endregion

        #region Methods

        /// <summary>
        /// يُطبّق اللون حسب المقارنة بين الكمية المطلوبة والمتوفرة
        /// (كما في dgvItems_CellFormatting الأصلية)
        /// </summary>
        public void ApplyColor()
        {
            if (Qty <= StockQty)
                QtyBackground = new SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(144, 238, 144)); // LightGreen
            else
                QtyBackground = new SolidColorBrush(
                    System.Windows.Media.Colors.Red);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this,
               new PropertyChangedEventArgs(propertyName));

        #endregion
    }

    /// <summary>
    /// Model صف نتيجة البحث
    /// </summary>
    public class SearchResultRow
    {
        public string InvGlobalID { get; set; }
        public int id { get; set; }
        public string vDate { get; set; }
    }

    #endregion
}