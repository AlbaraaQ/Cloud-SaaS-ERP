using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Core;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSummaryQ : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private string Type;
        private string title;
        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int PrintType;
        private int PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;

        private ObservableCollection<ReceiptQItem> _receiptItems;

        #endregion

        #region Constructor

        public frmSummaryQ()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
            Type = "1";
            title = "";
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp = true;
            PrintNo = 1;
            RptName = "";
            RptUrl = "";

            _receiptItems = new ObservableCollection<ReceiptQItem>();
            GridControl2.ItemsSource = _receiptItems;
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
                LoadPrintSettings();
                LoadBranches();
                LoadAccounts();
                LoadSalesMen();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل النافذة: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Data Loading

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
            catch (Exception ex) { SetStatus("خطأ: " + ex.Message); }
            finally { EnsureClose(conn); }
        }

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
            catch (Exception ex) { SetStatus("خطأ: " + ex.Message); }
            finally { EnsureClose(conn); }
        }

        public void LoadSalesMen()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM salesmen WHERE IS_Deleted=0 ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbSaleman.ItemsSource = dt.DefaultView;
                cmbSaleman.DisplayMemberPath = "name";
                cmbSaleman.SelectedValuePath = "id";
                cmbSaleman.SelectedIndex = -1;
            }
            catch (Exception ex) { SetStatus("خطأ: " + ex.Message); }
            finally { EnsureClose(conn); }
        }

        private void LoadPrintSettings()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=0", conn);
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
                    RptName = dt.Rows[0]["RptName"]?.ToString() ?? "";
                    RptUrl = dt.Rows[0]["RptUrl"]?.ToString() ?? "";
                }
            }
            catch { }
            finally { EnsureClose(conn); }
        }

        #endregion

        #region Show Results

        private void ShowResult()
        {
            try
            {
                _receiptItems.Clear();
                ProgressBar1.Value = 0;

                // بناء شروط الاستعلام
                var conditions = new System.Collections.Generic.List<string>();

                if (chkAll.IsChecked == true)
                    conditions.Add("(Receipts.ReceiptType=5 OR Receipts.ReceiptType=6 OR Receipts.ReceiptType=10)");
                else
                {
                    switch (cmbType.SelectedIndex)
                    {
                        case 0: conditions.Add("Receipts.ReceiptType=5"); break;
                        case 1: conditions.Add("Receipts.ReceiptType=6"); break;
                        case 2: conditions.Add("Receipts.ReceiptType=10"); break;
                    }
                }

                if (chkAllBranches.IsChecked != true
                    && cmbBranches.SelectedIndex != -1
                    && cmbBranches.SelectedValue != null)
                    conditions.Add($"Receipts.BranchID={cmbBranches.SelectedValue}");

                if (chkAllAcc.IsChecked != true
                    && cmbAccounts.SelectedIndex != -1
                    && cmbAccounts.SelectedValue != null)
                {
                    string accVal = cmbAccounts.SelectedValue.ToString();
                    conditions.Add($"(Receipts.CreditAcc=N'{accVal}' OR Receipts.DebitAcc=N'{accVal}')");
                }

                if (cbAllSalesmen.IsChecked != true
                    && cmbSaleman.SelectedIndex != -1
                    && cmbSaleman.SelectedValue != null)
                    conditions.Add($"Receipts.SalesManID={cmbSaleman.SelectedValue}");

                bool useDate = ckTotalPeriod.IsChecked != true;
                if (useDate)
                {
                    DateTime from = txtDateFrom.DateTime != DateTime.MinValue
    ? txtDateFrom.DateTime.Date
    : DateTime.Now.Date;

                    DateTime to = txtDateTo.DateTime != DateTime.MinValue
                        ? txtDateTo.DateTime.Date.AddDays(1)
                        : DateTime.Now.Date.AddDays(1);
                    conditions.Add("Receipts.ReceiptDate>=@date1 AND Receipts.ReceiptDate<@date2");
                }

                conditions.Add("Receipts.ISDeleted=0");

                string whereClause = conditions.Count > 0
                    ? "WHERE " + string.Join(" AND ", conditions)
                    : "";

                string sql = $@"SELECT Receipts.ReceiptNo,
                                       Receipts.ReceiptDate,
                                       Receipts.Payment,
                                       Receipts.Notes,
                                       Receipts.ReceiptType,
                                       Receipts.GlobalID,
                                       ISNULL(Receipts.BranchID, 1)   AS BranchID,
                                       Receipts.ClientID,
                                       Receipts.PaymentType,
                                       Receipts.EmpId,
                                       ISNULL(SalesMen.Name, '')       AS SalesmanName,
                                       ISNULL(Customers.Name, '')      AS ClientName,
                                       ISNULL(Users.username, '')      AS UserName
                                FROM Receipts
                                LEFT JOIN SalesMen  ON SalesMen.ID  = Receipts.SalesManID
                                LEFT JOIN Customers ON Customers.ID  = Receipts.ClientID
                                LEFT JOIN Users     ON Users.emp     = Receipts.EmpId
                                {whereClause}
                                ORDER BY Receipts.ReceiptNo";

                EnsureOpen(conn);
                var cmd = new SqlCommand(sql, conn);

                if (useDate)
                {
                    cmd.Parameters.AddWithValue("@date1",
    txtDateFrom.DateTime != DateTime.MinValue
        ? txtDateFrom.DateTime.Date
        : DateTime.Now.Date);

                    cmd.Parameters.AddWithValue("@date2",
                        txtDateTo.DateTime != DateTime.MinValue
                            ? txtDateTo.DateTime.Date.AddDays(1)
                            : DateTime.Now.Date.AddDays(1));
                }

                var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                EnsureClose(conn);

                ProgressBar1.Maximum = dt.Rows.Count > 0 ? dt.Rows.Count : 1;
                int idx = 1;

                foreach (DataRow row in dt.Rows)
                {
                    decimal payment = row["Payment"] != DBNull.Value
                        ? Convert.ToDecimal(row["Payment"]) : 0m;

                    int receiptType = row["ReceiptType"] != DBNull.Value
                        ? Convert.ToInt32(row["ReceiptType"]) : 0;

                    int paymentType = row["PaymentType"] != DBNull.Value
                        ? Convert.ToInt32(row["PaymentType"]) : 0;

                    int branchId = row["BranchID"] != DBNull.Value
                        ? Convert.ToInt32(row["BranchID"]) : 0;

                    _receiptItems.Add(new ReceiptQItem
                    {
                        AutoIncrementID = idx++,
                        Recieptvalue = payment,
                        RecieptDate = row["ReceiptDate"] != DBNull.Value
                            ? Convert.ToDateTime(row["ReceiptDate"]).ToShortDateString()
                            : "",
                        Recieptno = row["ReceiptNo"] != DBNull.Value ? Convert.ToInt32(row["ReceiptNo"]) : 0,
                        ReciptType = GetReceiptTypeText(receiptType),
                        ClientTxt = row["ClientName"] != DBNull.Value ? row["ClientName"].ToString() : "",
                        PayType = GetPayTypeText(paymentType),
                        SalesmanTxt = row["SalesmanName"] != DBNull.Value ? row["SalesmanName"].ToString() : "",
                        Recieptnote = row["Notes"] != DBNull.Value ? row["Notes"].ToString() : "",
                        BranchTxt = GetBranchName(branchId),
                        UserTxt = row["UserName"] != DBNull.Value ? row["UserName"].ToString() : "",
                        GlobalID = row["GlobalID"] != DBNull.Value ? row["GlobalID"].ToString() : "",
                        Typeno = receiptType
                    });

                    ProgressBar1.Value++;
                }

                // تحديث الإجمالي
                lblTotalValue.Text = _receiptItems.Sum(x => x.Recieptvalue).ToString("N2");
                title = (cmbType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("حدث خطأ أثناء عرض السندات:\n" + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                EnsureClose(conn);
            }
        }

        #endregion

        #region Helper Methods

        private string GetReceiptTypeText(int type)
        {
            return type switch
            {
                5 => "سند قبض عميل",
                6 => "سند صرف لمورد",
                10 => "سند قبض أندرويد",
                _ => ""
            };
        }

        public static string GetPayTypeText(int type)
        {
            return type switch
            {
                1 => "نقدي",
                2 => "بنك",
                _ => ""
            };
        }

        private string GetBranchName(int branchId)
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    $"SELECT name FROM Branches WHERE BranchId={branchId}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
            finally { EnsureClose(conn); }
        }

        #endregion

        #region Button Events

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            ShowResult();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnPrint_Click(object sender, RoutedEventArgs e) => PrintReport(1);

        private void btnPreview_Click(object sender, RoutedEventArgs e) => PrintReport(2);

        private void BtnExcportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_receiptItems.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للتصدير", "",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    FileName = $"سندات_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string filePath = saveDialog.FileName;
                    using (var writer = new System.IO.StreamWriter(filePath,
                           false, System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine("م,قيمة السند,تاريخ السند,رقم السند,نوع السند,العميل,طريقة الدفع,المندوب,البيان,الفرع,المستخدم");
                        foreach (var item in _receiptItems)
                            writer.WriteLine($"{item.AutoIncrementID},{item.Recieptvalue:N2}," +
                                             $"{item.RecieptDate},{item.Recieptno}," +
                                             $"{item.ReciptType},{item.ClientTxt}," +
                                             $"{item.PayType},{item.SalesmanTxt}," +
                                             $"{item.Recieptnote},{item.BranchTxt},{item.UserTxt}");
                    }

                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                    DXMessageBox.Show("تم حفظ الملف بنجاح في:\n" + filePath, "تم",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التصدير\nتفاصيل الخطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnShowInv_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ReceiptQItem item)
            {
                try
                {
                    if (item.Typeno == 5)
                    {
                        var frm = new frmSandQ();
                        frm.Show();
                        frm.Navigate($"SELECT * FROM Receipts WHERE receiptType=5 AND GlobalID=N'{item.GlobalID}'");
                        frm.Activate();
                    }
                    else if (item.Typeno == 6)
                    {
                        // frmSandD frm = new frmSandD();
                        // frm.Show();
                        // frm.Navigate($"SELECT * FROM Receipts WHERE receiptType=6 AND GlobalID=N'{item.GlobalID}'");
                        DXMessageBox.Show("افتح نافذة سند الصرف هنا", "عرض",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else if (item.Typeno == 10)
                    {
                        // frmSandQAndroid frm = new frmSandQAndroid();
                        // frm.Show();
                        // frm.Navigate($"SELECT * FROM Receipts WHERE receiptType=10 AND GlobalID=N'{item.GlobalID}'");
                        DXMessageBox.Show("افتح نافذة سند قبض أندرويد هنا", "عرض",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    DXMessageBox.Show("خطأ في فتح السند: " + ex.Message,
                                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void cmbAccounts_Click(object sender, RoutedEventArgs e)
        {
            if (chkAllAcc.IsChecked != true)
            {
                // frmAccountSrch frm = new frmAccountSrch();
                // frm.ShowDialog();
                // if (frm.Code > -1) cmbAccounts.SelectedValue = frm.Code;
                DXMessageBox.Show("افتح نافذة البحث عن الحسابات هنا", "بحث",
                                MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void cbAllSalesmen_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbSaleman != null)
                cmbSaleman.IsEnabled = cbAllSalesmen.IsChecked != true;
        }

        #endregion

        #region Print

        private void PrintReport(int printMode)
        {
            RptUrl = MainClass.ReportsPath;
            RptName = "rptSummaryD.repx";

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