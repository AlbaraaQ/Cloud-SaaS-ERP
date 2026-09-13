using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using UtilitiesProj;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptItemsExpiration : ThemedWindow
    {
        #region Fields

        private int     ItemId;
        private SqlConnection conn;

        private ObservableCollection<ItemExpirationRow> _gridData
            = new ObservableCollection<ItemExpirationRow>();

        #endregion

        #region Constructor

        public frmRptItemsExpiration()
        {
            InitializeComponent();
            ItemId = 0;
            conn   = MainClass.ConnObj();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GridControl2.ItemsSource = _gridData;
            LoadSafes();
            LoadGroups();
            LoadBranches();
        }

        #endregion

        #region Load Lookups

        public void LoadSafes()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Safes " +
                    $"WHERE branch={MainClass.BranchNo} " +
                    $"AND status=1 AND IS_Deleted=0 ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbSafes.DisplayMemberPath = "name";
                cmbSafes.SelectedValuePath = "id";
                cmbSafes.ItemsSource       = dt.DefaultView;
                cmbSafes.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المستودعات:\n" + ex.Message);
            }
        }

        public void LoadGroups()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM ItemsCategory WHERE type=2 ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbGrp.DisplayMemberPath = "name";
                cmbGrp.SelectedValuePath = "id";
                cmbGrp.ItemsSource       = dt.DefaultView;
                cmbGrp.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المجموعات:\n" + ex.Message);
            }
        }

        private void LoadBranches()
        {
            try
            {
                string branchCondition = "";
                if (MainClass.BranchNo != -1)
                {
                    branchCondition = string.Equals(
                        Accounting.BranchCondition, " ",
                        StringComparison.Ordinal)
                        ? ""
                        : $" AND id={MainClass.BranchNo}";
                }

                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Branches WHERE IS_Deleted=0{branchCondition}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath = "id";
                cmbBranches.ItemsSource       = dt.DefaultView;
                cmbBranches.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل الفروع:\n" + ex.Message);
            }
        }

        #endregion

        #region ShowResults

        private void ShowResults()
        {
            try
            {
                _gridData.Clear();

                string whereClause = "";

                if (chlAllSafe.IsChecked != true && cmbSafes.SelectedValue != null)
                    whereClause += $" AND StoreID={cmbSafes.SelectedValue}";

                if (chkAllGrp.IsChecked != true && cmbGrp.SelectedValue != null)
                    whereClause += $" AND GroupID={cmbGrp.SelectedValue}";

                if (chkAll.IsChecked != true && ItemId != 0
                    && !string.IsNullOrWhiteSpace(txtItemName.Text))
                    whereClause += $" AND ItemId={ItemId}";

                if (chkAllBranches.IsChecked != true && cmbBranches.SelectedValue != null)
                    whereClause += $" AND BranchId={cmbBranches.SelectedValue}";

                // استدعاء Inventory.ItemsExpirationStock من UtilitiesProj
                DataTable dt = Inventory.ItemsExpirationStock(whereClause);

                if (dt == null || dt.Rows.Count == 0)
                {
                    UpdateSummary();
                    return;
                }

                ProgressBar1.Maximum = dt.Rows.Count;
                ProgressBar1.Value   = 0;

                int rowNum = 1;
                foreach (DataRow row in dt.Rows)
                {
                    ProgressBar1.Value = rowNum;

                    DateTime? expireDate = null;
                    if (row["ItemExpire"] != DBNull.Value)
                    {
                        if (DateTime.TryParse(row["ItemExpire"].ToString(), out DateTime ed))
                            expireDate = ed;
                    }

                    double.TryParse(row["ItemStock"].ToString(),     out double stock);
                    int.TryParse(row["RemaindeYear"].ToString(),     out int remainYear);
                    int.TryParse(row["RemaindeMonths"].ToString(),   out int remainMonths);
                    int.TryParse(row["RemaindeDays"].ToString(),     out int remainDays);

                    _gridData.Add(new ItemExpirationRow
                    {
                        AutoIncrementID = rowNum++,
                        ItemCode        = row["ItemCode"].ToString(),
                        ItemName        = row["ItemName"].ToString(),
                        StoreName       = row["StoreName"].ToString(),
                        ItemStock       = stock,
                        ItemExpire      = expireDate,
                        RemaindeYear    = remainYear,
                        RemaindeMonths  = remainMonths,
                        RemaindeDays    = remainDays,
                    });
                }

                UpdateSummary();
                ProgressBar1.Value = ProgressBar1.Maximum;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في عرض النتائج:\n" + ex.Message);
            }
        }

        private void UpdateSummary()
        {
            lblCountItems.Text   = _gridData.Count.ToString();
            int expired = _gridData.Count(r =>
                r.ItemExpire.HasValue && r.ItemExpire.Value < DateTime.Today);
            lblExpiredCount.Text = expired.ToString();
        }

        #endregion

        #region Item Search

        private void SearchByName()
        {
            try
            {
                string name = txtItemName.Text.Trim();
                if (string.IsNullOrEmpty(name)) { OpenItemSearch(); return; }

                var adapter = new SqlDataAdapter(
                    $"SELECT name, id FROM Items " +
                    $"WHERE IS_Deleted=0 AND name=N'{name}'", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    ItemId           = Convert.ToInt32(dt.Rows[0]["id"]);
                    txtItemName.Text = dt.Rows[0]["name"].ToString();
                }
                else
                {
                    OpenItemSearch();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في البحث:\n" + ex.Message);
            }
        }

        private void OpenItemSearch()
        {
            var dlg = new frmItemsSrch();
            MainClass.ApplyPermissionToForm(dlg);
            MainClass.DoApplyUserSett(dlg);
            dlg.sql         = "SELECT id, name, nameEN, sale_price, unit FROM Items WHERE IS_Deleted=0 ORDER BY id";
            dlg.search      = "SELECT id, name, nameEN, sale_price, unit FROM Items";
            dlg.Itemname    = "";
            dlg.txtSrchNm.Text = txtItemName.Text;
            dlg.ShowDialog();

            if (dlg.ISDone && dlg.ItemId > 0)
            {
                ItemId           = dlg.ItemId;
                txtItemName.Text = dlg.Itemname;
            }
        }

        #endregion

        #region Print / Preview / Export

        private DataSet BuildReportDataSet()
        {
            var list = new List<InventoryData>();

            foreach (var row in _gridData)
            {
                list.Add(new InventoryData
                {
                    SafeMain      = cmbSafes.Text,
                    SafeMainCode  = cmbSafes.SelectedValue?.ToString() ?? "",
                    MainItemName  = txtItemName.Text,
                    BranchName    = cmbBranches.Text,
                    CategoryName  = cmbGrp.Text,
                    ItemName      = row.ItemName,
                    ItemCode      = row.ItemCode,
                    SafeName      = row.StoreName,
                    Quantity      = row.ItemStock.ToString("N2"),
                    RemaindeDays  = row.RemaindeDays.ToString(),
                    RemaindeMonths= row.RemaindeMonths.ToString(),
                    RemaindeYear  = row.RemaindeYear.ToString(),
                    OperDate      = row.ItemExpire.HasValue
                                    ? row.ItemExpire.Value.ToShortDateString() : "",
                    FromDate      = "",
                    ToDate        = "",
                    Status        = " ",
                    InventoryType = this.Title,
                    Sum           = "",
                    User          = Common.GetEmpName(MainClass.EmpNo),
                    PrintDate     = DateTime.Now.ToShortDateString(),
                });
            }

            var ds = new DataSet("Name");
            ds.Tables.Add(UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        private void PrintDevexpress(int printMode)
        {
            string reportsPath   = MainClass.ReportsPath;
            string reportsPrinter= MainClass.ReportsPrinter;
            string rptName       = "RptItemsExpiration.repx";

            if (_gridData.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات بالجدول",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            new Report().Printing(printMode, BuildReportDataSet(),
                reportsPath, rptName, reportsPrinter, 1);
        }

        private void ExportToExcel()
        {
            try
            {
                if (_gridData.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للتصدير",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"{this.Title}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                using (var sw = new System.IO.StreamWriter(filePath,
                    false, System.Text.Encoding.UTF8))
                {
                    sw.WriteLine("\"م\",\"رمز الصنف\",\"الصنف\",\"المستودع\"," +
                                 "\"الكمية\",\"تاريخ الإنتهاء\"," +
                                 "\"باقي سنوات\",\"باقي أشهر\",\"باقي أيام\"");

                    foreach (var r in _gridData)
                    {
                        sw.WriteLine(
                            $"\"{r.AutoIncrementID}\"," +
                            $"\"{r.ItemCode}\"," +
                            $"\"{r.ItemName}\"," +
                            $"\"{r.StoreName}\"," +
                            $"\"{r.ItemStock:N2}\"," +
                            $"\"{(r.ItemExpire.HasValue ? r.ItemExpire.Value.ToShortDateString() : "")}\"," +
                            $"\"{r.RemaindeYear}\"," +
                            $"\"{r.RemaindeMonths}\"," +
                            $"\"{r.RemaindeDays}\"");
                    }
                }

                Process.Start(new ProcessStartInfo(filePath)
                    { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التصدير:\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Button Events

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            ShowResults();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(2);
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(1);
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            chkAll.IsChecked = false;
            OpenItemSearch();
        }

        #endregion

        #region CheckBox Events

        private void chlAllSafe_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbSafes.IsEnabled = chlAllSafe.IsChecked != true;
        }

        private void chkAllGrp_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbGrp.IsEnabled = chkAllGrp.IsChecked != true;
        }

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool isAll = chkAll.IsChecked == true;
            txtItemName.IsEnabled = !isAll;
            if (isAll)
            {
                txtItemName.Text = "";
                ItemId = 0;
            }
        }

        private void chkAllBranches_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbBranches.IsEnabled = chkAllBranches.IsChecked != true;
        }

        #endregion

        #region TextBox Events

        private void txtItemName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return &&
                !string.IsNullOrWhiteSpace(txtItemName.Text))
                SearchByName();
        }

        #endregion
    }
}