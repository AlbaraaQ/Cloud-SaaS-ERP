using System;
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
using Button = System.Windows.Controls.Button;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSummaryD : ThemedWindow
    {
        #region Fields

        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int PrintType;
        private int PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;
        private SqlConnection conn;
        private string Type;
        private string title;

        private ObservableCollection<ReceiptSummaryItem> _items;

        #endregion

        #region Constructor

        public frmSummaryD()
        {
            InitializeComponent();

            conn = MainClass.ConnObj();
            Type = "7";
            title = "";
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp = true;
            PrintNo = 1;
            RptName = "";
            RptUrl = "";

            _items = new ObservableCollection<ReceiptSummaryItem>();
            GridControl1.ItemsSource = _items;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                txtDateFrom.DateTime = DateTime.Now;
                txtDateTo.DateTime = DateTime.Now;
                cmbType.SelectedIndex = 0;
                LoadBranches();
                LoadAccounts();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل النافذة: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Data Loading

        private void LoadAccounts()
        {
            try
            {
                EnsureOpen(conn);
                string branchCond = MainClass.BranchNo != -1
                    ? $" AND Branch={MainClass.BranchNo}" : "";
                var adapter = new SqlDataAdapter(
                    $"SELECT Code, AName FROM Accounts_Index WHERE Type=2 {branchCond}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbAccounts.ItemsSource = dt.DefaultView;
                cmbAccounts.DisplayMemberPath = "AName";
                cmbAccounts.SelectedValuePath = "Code";
                cmbAccounts.SelectedIndex = -1;
            }
            catch (Exception ex) { SetStatus("خطأ في تحميل الحسابات: " + ex.Message); }
            finally { EnsureClose(conn); }
        }

        private void LoadBranches()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Branches WHERE IS_Deleted=0", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbBranches.ItemsSource = dt.DefaultView;
                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath = "id";

                if (MainClass.BranchNo != -1)
                    cmbBranches.SelectedValue = MainClass.BranchNo;
                else
                    cmbBranches.SelectedIndex = -1;
            }
            catch (Exception ex) { SetStatus("خطأ في تحميل الفروع: " + ex.Message); }
            finally { EnsureClose(conn); }
        }

        #endregion

        #region Show Result

        private void ShowResult()
        {
            try
            {
                _items.Clear();

                // بناء شرط نوع السند
                string typeCondition;
                if (chkAll.IsChecked == true)
                    typeCondition = "(receiptType=7 OR receiptType=8 OR receiptType=9) AND ";
                else
                {
                    switch (cmbType.SelectedIndex)
                    {
                        case 0: typeCondition = "receiptType=7 AND "; break;
                        case 1: typeCondition = "receiptType=8 AND "; break;
                        case 2: typeCondition = "receiptType=9 AND "; break;
                        default: typeCondition = "receiptType=7 AND "; break;
                    }
                }

                // شرط الحساب
                string accCondition = "";
                if (chkAllAcc.IsChecked != true
                    && cmbAccounts.SelectedIndex != -1
                    && cmbAccounts.SelectedValue != null)
                {
                    accCondition = $"(CreditAcc=N'{cmbAccounts.SelectedValue}' OR DebitAcc=N'{cmbAccounts.SelectedValue}') AND ";
                }

                // شرط الفرع
                string branchCondition = "";
                if (chkAllBranches.IsChecked != true
                    && cmbBranches.SelectedIndex != -1
                    && cmbBranches.SelectedValue != null)
                {
                    branchCondition = $"Receipts.BranchID={cmbBranches.SelectedValue} AND ";
                }

                // شرط التاريخ
                string dateCondition = "";
                if (ckTotalPeriod.IsChecked != true)
                    dateCondition = "ReceiptDate>=@date1 AND ReceiptDate<=@date2 AND ";

                string sql = $@"SELECT ReceiptNo,
                                       CAST(ReceiptDate AS DATE) AS OperDate,
                                       Payment,
                                       VAT,
                                       (Payment + VAT) AS DgvNet,
                                       Receipts.notes AS ReceiptNote,
                                       CASE
                                           WHEN receiptType=7 THEN N'سند قبض'
                                           WHEN receiptType=8 THEN N'سند صرف'
                                           WHEN receiptType=9 THEN N'سند صرف ضريبي'
                                           ELSE N'غير معروف'
                                       END AS DgvreceiptType,
                                       GlobalID,
                                       Branches.name AS BranchName,
                                       receiptType,
                                       ROW_NUMBER() OVER(ORDER BY ReceiptDate DESC) AS Rank
                                FROM Receipts
                                LEFT JOIN Branches ON Receipts.BranchID=Branches.BranchId
                                WHERE {typeCondition} {branchCondition} {dateCondition} {accCondition}
                                      ISDeleted=0
                                ORDER BY ReceiptDate DESC";

                EnsureOpen(conn);
                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@date1",
    txtDateFrom.DateTime != DateTime.MinValue
        ? txtDateFrom.DateTime.Date
        : DateTime.Now.Date);

                cmd.Parameters.AddWithValue("@date2",
                    txtDateTo.DateTime != DateTime.MinValue
                        ? txtDateTo.DateTime.Date.AddDays(1)
                        : DateTime.Now.Date.AddDays(1));

                var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);

                int rank = 1;
                foreach (DataRow row in dt.Rows)
                {
                    _items.Add(new ReceiptSummaryItem
                    {
                        Rank = rank++,
                        ReceiptNo = row["ReceiptNo"] != DBNull.Value ? Convert.ToInt32(row["ReceiptNo"]) : 0,
                        OperDate = row["OperDate"] != DBNull.Value ? Convert.ToDateTime(row["OperDate"]).ToShortDateString() : "",
                        Payment = row["Payment"] != DBNull.Value ? Convert.ToDouble(row["Payment"]) : 0,
                        VAT = row["VAT"] != DBNull.Value ? Convert.ToDouble(row["VAT"]) : 0,
                        DgvNet = row["DgvNet"] != DBNull.Value ? Convert.ToDouble(row["DgvNet"]) : 0,
                        ReceiptNote = row["ReceiptNote"] != DBNull.Value ? row["ReceiptNote"].ToString() : "",
                        DgvreceiptType = row["DgvreceiptType"].ToString(),
                        GlobalID = row["GlobalID"].ToString(),
                        BranchName = row["BranchName"] != DBNull.Value ? row["BranchName"].ToString() : "",
                        ReceiptType = row["receiptType"] != DBNull.Value ? Convert.ToInt32(row["receiptType"]) : 0
                    });
                }

                // تحديث المجاميع
                UpdateSummaries();

                title = (cmbType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                SetStatus($"تم العثور على {_items.Count} سند");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في عرض البيانات\nتفاصيل الخطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { EnsureClose(conn); }
        }

        private void UpdateSummaries()
        {
            lblTotalPayment.Text = _items.Sum(x => x.Payment).ToString("N2");
            lblTotalVAT.Text = _items.Sum(x => x.VAT).ToString("N2");
            lblTotalNet.Text = _items.Sum(x => x.DgvNet).ToString("N2");
        }

        #endregion

        #region Button Events

        private void btnShow_Click(object sender, RoutedEventArgs e) => ShowResult();

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnPrint_Click(object sender, RoutedEventArgs e) => PrintReport(1);

        private void btnPreview_Click(object sender, RoutedEventArgs e) => PrintReport(2);

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_items.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للتصدير", "",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string fileName = $"سندات_{DateTime.Now:yyyyMMdd_HHmm}.csv";
                string filePath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                using (var writer = new System.IO.StreamWriter(filePath,
                       false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("#,الرقم,التاريخ,قيمة السند,الضريبة,الصافي,الفرع,البيان,النوع");
                    foreach (var item in _items)
                    {
                        writer.WriteLine($"{item.Rank},{item.ReceiptNo},{item.OperDate}," +
                                         $"{item.Payment:N2},{item.VAT:N2},{item.DgvNet:N2}," +
                                         $"{item.BranchName},{item.ReceiptNote},{item.DgvreceiptType}");
                    }
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

        #endregion

        #region DataGrid Events

        private void GridControl1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (GridControl1.SelectedItem is ReceiptSummaryItem item)
                OpenDetail(item);
        }

        private void BtnDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ReceiptSummaryItem item)
                OpenDetail(item);
        }

        private void OpenDetail(ReceiptSummaryItem item)
        {
            try
            {
                if (item.ReceiptType == 7)
                {
                    var frm = new frmSandQD();
                    frm.Show();
                    frm.Navigate($"SELECT * FROM Receipts WHERE receiptType=7 AND GlobalID=N'{item.GlobalID}'");
                    frm.Activate();
                }
                else if (item.ReceiptType == 8)
                {
                    var frm = new frmSandSD();
                    frm.Show();
                    frm.Navigate($"SELECT * FROM Receipts WHERE receiptType=8 AND GlobalID=N'{item.GlobalID}'");
                    frm.Activate();
                }
                else if (item.ReceiptType == 9)
                {
                    var frm = new frmSandVAT();
                    frm.Show();
                    frm.Navigate($"SELECT * FROM Receipts WHERE receiptType=9 AND GlobalID=N'{item.GlobalID}'");
                    frm.Activate();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في فتح التفاصيل: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region CheckBox Events

        private void ckTotalPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool allChecked = ckTotalPeriod.IsChecked == true;
            txtDateFrom.IsEnabled = !allChecked;
            txtDateTo.IsEnabled = !allChecked;
        }

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbType != null)
                cmbType.IsEnabled = chkAll.IsChecked != true;
        }

        private void chkAllBranches_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbBranches != null)
                cmbBranches.IsEnabled = chkAllBranches.IsChecked != true;
        }

        private void chkAllAcc_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbAccounts != null)
                cmbAccounts.IsEnabled = chkAllAcc.IsChecked != true;
        }

        private void cmbAccounts_Click(object sender, RoutedEventArgs e)
        {
            if (chkAllAcc.IsChecked != true)
            {
                // فتح نافذة البحث عن الحسابات
                // frmAccountSrch frm = new frmAccountSrch();
                // frm.ShowDialog();
                // if (frm.Code > -1) cmbAccounts.SelectedValue = frm.Code;
                DXMessageBox.Show("افتح نافذة البحث عن الحسابات هنا", "بحث",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region Print

        private void PrintReport(int printMode)
        {
            RptUrl = MainClass.ReportsPath;
            RptName = "rptSummaryD.repx";

            if (_items.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات بالجدول", "",
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