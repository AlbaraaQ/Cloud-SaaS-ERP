using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Core;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmStocksSync : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        public int Invtype { get; set; }
        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int PrintType;
        private int PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;
        private short SelectedId;

        private ObservableCollection<StockSyncItem> _stockItems;

        #endregion

        #region Constructor

        public frmStocksSync()
        {
            InitializeComponent();

            conn = MainClass.ConnObj();
            Invtype = 0;
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp = true;
            PrintNo = 1;
            RptName = "";
            RptUrl = "";
            SelectedId = 0;

            _stockItems = new ObservableCollection<StockSyncItem>();
            GridControl1.ItemsSource = _stockItems;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                txtLastDate.DateTime = DateTime.Now;
                LoadStores();
                LoadPrintSettings();
                LoadGroups();

                // إظهار زر التحديث الحالي إذا كان Sync نشطاً
                if (Sync.ActiveSync && Sync.SyncType > 0)
                    btnItemQuantity.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل النافذة: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Data Loading

        public void LoadGroups()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter("SELECT id, name FROM ItemsCategory ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbGroup.ItemsSource = dt.DefaultView;
                cmbGroup.DisplayMemberPath = "name";
                cmbGroup.SelectedValuePath = "id";
                cmbGroup.SelectedIndex = -1;
            }
            catch (Exception ex) { SetStatus("خطأ في تحميل المجموعات: " + ex.Message); }
            finally { EnsureClose(conn); }
        }

        public void LoadStores()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Safes WHERE status=1 AND IS_Deleted=0 ORDER BY id",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbStores.ItemsSource = dt.DefaultView;
                cmbStores.DisplayMemberPath = "name";
                cmbStores.SelectedValuePath = "id";
                cmbStores.SelectedIndex = -1;
            }
            catch (Exception ex) { SetStatus("خطأ في تحميل المستودعات: " + ex.Message); }
            finally { EnsureClose(conn); }
        }

        private void LoadPrintSettings()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=12", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 1)
                {
                    if (int.TryParse(dt.Rows[0]["printType"].ToString(), out int pt)) PrintType = pt;
                    PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                    PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                    PrintStamp = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                    defPrinter = dt.Rows[0]["CasherPrinter"].ToString();
                    if (string.IsNullOrWhiteSpace(defPrinter)) defPrinter = MainClass.ReportsPrinter;
                    if (int.TryParse(dt.Rows[0]["printNo"].ToString(), out int pn)) PrintNo = pn;
                }
            }
            catch { }
            finally { EnsureClose(conn); }
        }

        #endregion

        #region Show Stocks

        public void ShowStocks(string filterCondition)
        {
            try
            {
                _stockItems.Clear();

                EnsureOpen(conn);

                string sql = $@"SELECT ROW_NUMBER() OVER(ORDER BY ProductId) AS RowNo,
                                       ProductStocks.ProductId,
                                       ProductStocks.Quantity,
                                       ProductStocks.AvrgCost,
                                       ProductStocks.LastUpdate,
                                       items.name   AS ItemName,
                                       items.code   AS ItemCode,
                                       units.name   AS UnitName,
                                       Branches.name AS BranchName,
                                       Safes.name   AS InventoryName
                                FROM ProductStocks
                                LEFT JOIN items    ON ProductStocks.ProductId = items.id
                                LEFT JOIN units    ON ProductStocks.UnitId    = units.Id
                                LEFT JOIN Branches ON ProductStocks.BranchId  = Branches.BranchId
                                LEFT JOIN Safes    ON ProductStocks.InventoryId = Safes.id
                                WHERE ProductId IS NOT NULL
                                {filterCondition}";

                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                int rowNo = 1;
                foreach (DataRow row in dt.Rows)
                {
                    double qty = 0;
                    double.TryParse(row["Quantity"].ToString(), out qty);

                    // تصفية الكمية
                    if (rbPositiveQty.IsChecked == true && qty <= 0) continue;
                    if (rbNegativeQnty.IsChecked == true && qty >= 0) continue;
                    if (rbZeroQnty.IsChecked == true && qty != 0) continue;

                    _stockItems.Add(new StockSyncItem
                    {
                        RowNo = rowNo++,
                        ProductId = row["ProductId"] != DBNull.Value ? Convert.ToInt32(row["ProductId"]) : 0,
                        ItemCode = row["ItemCode"] != DBNull.Value ? row["ItemCode"].ToString() : "",
                        ItemName = row["ItemName"] != DBNull.Value ? row["ItemName"].ToString() : "",
                        UnitName = row["UnitName"] != DBNull.Value ? row["UnitName"].ToString() : "",
                        Quantity = qty,
                        BranchName = row["BranchName"] != DBNull.Value ? row["BranchName"].ToString() : "",
                        InventoryName = row["InventoryName"] != DBNull.Value ? row["InventoryName"].ToString() : "",
                        LastUpdate = row["LastUpdate"] != DBNull.Value
                            ? Convert.ToDateTime(row["LastUpdate"]).ToShortDateString()
                            : ""
                    });
                }

                UpdateSummaries();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء حساب المخزون\nتفاصيل الخطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { EnsureClose(conn); }
        }

        private void UpdateSummaries()
        {
            lblTotalItems.Text = _stockItems.Count.ToString();
            lblTotalQty.Text = _stockItems.Sum(x => x.Quantity).ToString("N2");
        }

        #endregion

        #region Button Events

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            string filter = "";

            if (!string.IsNullOrWhiteSpace(txtItemName.Text) && SelectedId > 0)
                filter += $" AND ProductId={SelectedId}";

            if (cmbStores.SelectedIndex > -1 && cmbStores.SelectedValue != null)
                filter += $" AND InventoryId={cmbStores.SelectedValue}";

            ShowStocks(filter);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnPrint_Click(object sender, RoutedEventArgs e) => PrintReport(1);

        private void btnPreview_Click(object sender, RoutedEventArgs e) => PrintReport(2);

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_stockItems.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للتصدير", "",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string fileName = $"مخزون_{DateTime.Now:yyyyMMdd_HHmm}.csv";
                string filePath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                using (var writer = new System.IO.StreamWriter(filePath,
                       false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("#,رمز الصنف,الصنف,الوحدة,الكمية,الفرع,المستودع,آخر تحديث");
                    foreach (var item in _stockItems)
                        writer.WriteLine($"{item.RowNo},{item.ItemCode},{item.ItemName}," +
                                         $"{item.UnitName},{item.Quantity:N2},{item.BranchName}," +
                                         $"{item.InventoryName},{item.LastUpdate}");
                }

                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                SetStatus("تم التصدير بنجاح");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التصدير\nتفاصيل الخطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnUpdateStocks_Click(object sender, RoutedEventArgs e)
        {
            await UpdateStocksAsync();
        }

        private async Task UpdateStocksAsync()
        {
            SetStatus("جارِ تحديث المستودعات...");
            await new SyncOperation().ReadStockOnline();
            btnShow_Click(null, null);
            DXMessageBox.Show("جارِ تحديث المستودعات الأخرى...", "تحديث",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnItemQuantity_Click(object sender, RoutedEventArgs e)
        {
             if (Sync.ActiveSync && Sync.SyncType == 1 && Sync.BranchType != 4)
                 new Thread(Inventory.PostStocks).Start();
            DXMessageBox.Show("جارِ تحديث مخزون المستودع الحالي...", "تحديث",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            // فتح نافذة البحث عن الأصناف
             frmItemsSrch frm = new frmItemsSrch();
             frm.ShowDialog();
             if (frm.ISDone && frm.ItemId > 0) { SelectedId = (short)frm.ItemId; txtItemName.Text = frm.Itemname; }
             else { SelectedId = 0; txtItemName.Text = ""; }
          //  DXMessageBox.Show("افتح نافذة البحث عن الأصناف هنا", "بحث",
         //                   MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region ComboBox Events

        private void cmbGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbGroup.SelectedIndex >= 0 && cmbGroup.SelectedValue != null)
                txtGroupNo.Text = cmbGroup.SelectedValue.ToString();
        }

        private void txtGroupNo_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(txtGroupNo.Text) && cmbGroup != null)
                    cmbGroup.SelectedValue = txtGroupNo.Text;
            }
            catch { }
        }

        private void ckAllStore_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbStores != null)
            {
                cmbStores.IsEnabled = ckAllStore.IsChecked != true;
                if (ckAllStore.IsChecked == true)
                    cmbStores.SelectedIndex = -1;
            }
        }

        #endregion

        #region Print

        private void PrintReport(int printMode)
        {
            RptUrl = MainClass.ReportsPath;
            RptName = "rptInvsProfitSales.repx";

            if (_stockItems.Count == 0)
            {
                DXMessageBox.Show("لا توجد بيانات بالجدول", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string fullPath = System.IO.Path.Combine(RptUrl, RptName);
            if (!Directory.Exists(RptUrl) || !File.Exists(fullPath))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SetStatus(printMode == 1 ? "جارِ الطباعة..." : "جارِ المعاينة...");
        }

        #endregion

        #region Helpers

        private void EnsureOpen(SqlConnection c)
        { if (c.State != System.Data.ConnectionState.Open) c.Open(); }

        private void EnsureClose(SqlConnection c)
        { if (c.State != System.Data.ConnectionState.Closed) c.Close(); }

        private void SetStatus(string msg) { }

        #endregion

        #region Closing

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            EnsureClose(conn);
        }

        #endregion
    }
}