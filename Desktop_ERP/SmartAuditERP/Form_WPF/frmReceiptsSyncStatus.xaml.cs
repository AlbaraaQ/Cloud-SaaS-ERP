using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using Color = System.Windows.Media.Color;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmReceiptsSyncStatus : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;

        public int Invtype = 0;

        private int    _procType     = 1;
        private int    _invType      = 0;
        private bool   _printHeader  = true;
        private bool   _printFooter  = true;
        private bool   _printStamp   = true;
        private int    _printType    = 1;
        private int    _printNo      = 1;
        private string _defPrinter   = "";
        private string _rptName      = "";
        private string _rptUrl       = "";
        private double _defVAT       = 0;
        private bool   _priceIncVAT  = false;

        private ObservableCollection<ReceiptRow> _receiptsSource
            = new ObservableCollection<ReceiptRow>();

        #endregion

        #region Constructor

        public frmReceiptsSyncStatus()
        {
            InitializeComponent();
            _conn = MainClass.ConnObj();
            GridControl1.ItemsSource = _receiptsSource;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtFromDate.DateTime = DateTime.Now;
            txtToDate.DateTime   = DateTime.Now;
            LoadEmps();
            LoadPrintSettings();
            LoadBranches();
        }

        private void Window_Closing(object sender,
            System.ComponentModel.CancelEventArgs e)
        {
            _conn?.Close();
        }

        #endregion

        #region Load Data

        private void LoadEmps()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name FROM Employees WHERE IS_Deleted=0 ORDER BY id",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbusers.ItemsSource       = dt.DefaultView;
                    cmbusers.DisplayMemberPath = "name";
                    cmbusers.SelectedValuePath = "id";
                    cmbusers.SelectedIndex     = -1;
                }
            }
            catch { }
        }

        private void LoadBranches()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name FROM Branches WHERE IS_Deleted=0", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);

                    cmbBranches.ItemsSource       = dt.DefaultView;
                    cmbBranches.DisplayMemberPath = "name";
                    cmbBranches.SelectedValuePath = "id";
                    cmbBranches.SelectedIndex     = -1;

                    var dt2 = new DataTable();
                    da.Fill(dt2);
                    cmbMainBranch.ItemsSource       = dt2.DefaultView;
                    cmbMainBranch.DisplayMemberPath = "name";
                    cmbMainBranch.SelectedValuePath = "id";
                    cmbMainBranch.SelectedIndex     = -1;
                }
            }
            catch { }
        }

        private void LoadPrintSettings()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=12", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count == 1)
                    {
                        try { _printType   = Convert.ToInt32(dt.Rows[0]["printType"]);           } catch { }
                        try { _printFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);       } catch { }
                        try { _printHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);       } catch { }
                        try { _printStamp  = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);        } catch { }
                        try { _defPrinter  = dt.Rows[0]["CasherPrinter"].ToString();             } catch { }
                        try { _printNo     = Convert.ToInt32(dt.Rows[0]["printNo"]);             } catch { }
                        if (string.IsNullOrEmpty(_defPrinter))
                            _defPrinter = Common.GetDefaultPrinter();
                    }
                }

                using (var da2 = new SqlDataAdapter(
                    $"SELECT * FROM SettingGeneral WHERE Inv_Id={Invtype}", _conn))
                {
                    var dt2 = new DataTable();
                    da2.Fill(dt2);
                    if (dt2.Rows.Count == 1)
                    {
                        try { _priceIncVAT = Convert.ToBoolean(dt2.Rows[0]["PriceIncVAT"]); } catch { }
                        try { _defVAT      = Convert.ToDouble(dt2.Rows[0]["MainVAT"]);      } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region Show Invoices

        public void ShowInvs(string filterCond)
        {
            try
            {
                _receiptsSource.Clear();
                lblCount.Text = "0";
                lblTotal.Text = "0.00";
                ProgressBar1.Value = 0;

                string sql = $"SELECT * FROM Receipts WHERE {filterCond} ReceiptNo IS NOT NULL ORDER BY ReceiptNo";

                using (var da = new SqlDataAdapter(sql, _conn))
                {
                    if (!ckTotalPeriod.IsChecked == true)
                    {
                        da.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value
                            = txtFromDate.DateTime.ToShortDateString();
                        da.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value
                            = txtToDate.DateTime.AddHours(24);
                    }

                    var dt = new DataTable();
                    da.Fill(dt);

                    ProgressBar1.Maximum = dt.Rows.Count > 0 ? dt.Rows.Count : 1;

                    double totalSum = 0;
                    int    rowNo    = 1;

                    foreach (DataRow row in dt.Rows)
                    {
                        string receiptType = ReceiptOper.ReceipTypeName(
                            Convert.ToInt32(row["ReceiptType"]));

                        string receiptNo   = row["ReceiptNo"].ToString();
                        string refNo       = row["ReffNo"].ToString();
                        string dateStr     = Convert.ToDateTime(row["Receiptdate"]).ToShortDateString();
                        string empName     = Common.GetEmpName(Convert.ToInt32(row["EmpId"]));
                        string globalId    = row["GlobalID"].ToString();
                        string payTypeStr  = Convert.ToInt32(row["PaymentType"]) == 1 ? "نقدي" : "بنكي";
                        bool   isSynced    = Convert.ToBoolean(row["Sync"]);
                        bool   isDeleted   = false;

                        try { isDeleted = Convert.ToBoolean(row["ISDeleted"]); } catch { }

                        double sumVal = 0;
                        try { sumVal = Convert.ToDouble(row["Payment"]); } catch { }

                        string branchName = "";
                        try
                        {
                            branchName = Common.GetBranchName(
                                Convert.ToInt32(row["branchId"]));
                        }
                        catch { }

                        totalSum += sumVal;

                        _receiptsSource.Add(new ReceiptRow
                        {
                            IsSelected        = false,
                            DgvNo             = rowNo++,
                            DgvType           = receiptType,
                            DgvGlobalID       = globalId,
                            DgvReceiptBranch  = branchName,
                            DgvReceiptNo      = Convert.ToInt32(receiptNo),
                            DgvRefNo          = refNo,
                            DgvDate           = Convert.ToDateTime(row["Receiptdate"]),
                            DgvPayType        = payTypeStr,
                            DgvUser           = empName,
                            DgvSum            = sumVal,
                            DgvSyncStatus     = isSynced,
                            DgvSyncStatusText = isSynced ? "✅ متزامن" : "❌ غير متزامن",
                            DgvISDeleted      = isDeleted,
                            DgvISDeletedText  = isDeleted ? "نعم" : "لا"
                        });

                        ProgressBar1.Value = rowNo - 1;
                    }

                    lblCount.Text = _receiptsSource.Count.ToString();
                    lblTotal.Text = totalSum.ToString("N2");
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region Row Coloring

        private void GridControl1_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (e.Row.Item is ReceiptRow row)
            {
                if (row.DgvISDeleted)
                {
                    e.Row.Background  = new SolidColorBrush(Colors.SlateGray);
                    e.Row.Foreground  = new SolidColorBrush(Colors.White);
                }
                else if (row.DgvSyncStatus)
                {
                    e.Row.Background = new SolidColorBrush(
                        Color.FromRgb(200, 240, 200)); // أخضر فاتح
                    e.Row.Foreground = new SolidColorBrush(Colors.Black);
                }
                else
                {
                    e.Row.Background = new SolidColorBrush(
                        Color.FromRgb(255, 200, 180)); // برتقالي فاتح
                    e.Row.Foreground = new SolidColorBrush(Colors.Black);
                }
            }
        }

        #endregion

        #region Button Events - Show

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            string filter = "";

            // فرع السند
            if (CheckBox1.IsChecked != true && cmbBranches.SelectedIndex > 0)
                filter += $"Receipts.branchID={cmbBranches.SelectedValue} AND ";

            // المستخدم
            if (ckAllUsers.IsChecked != true && cmbusers.SelectedIndex > -1)
                filter += $" Receipts.EmpId={cmbusers.SelectedValue} AND ";

            // نوع السند
            if (ckAllInvs.IsChecked != true && cmbReceiptType.SelectedIndex >= 0)
                filter += $" Receipts.ReceiptType={cmbReceiptType.SelectedIndex + 5} AND ";

            // حالة المزامنة
            if (rbSynced.IsChecked == true)
                filter += " Receipts.Sync=1 AND ";
            else if (rbNotSynced.IsChecked == true)
                filter += " Receipts.Sync=0 AND ";

            // نوع الدفع
            if (ckAllType.IsChecked != true)
            {
                if (cmbType.SelectedIndex == 1)
                    filter += " Receipts.PaymentType=2 AND ";
                else if (cmbType.SelectedIndex == 0)
                    filter += " Receipts.PaymentType=1 AND ";
            }

            // الفترة
            if (ckTotalPeriod.IsChecked != true)
                filter += " Receiptdate>=@date1 AND Receiptdate<=@date2 AND ";

            // المحذوفات
            if (ckDeleted.IsChecked == true)
                filter += " ISDeleted=1 AND ";

            ShowInvs(filter);
        }

        #endregion

        #region Button Events - Footer

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnPrint_Click(object sender, RoutedEventArgs e) => PrintDevexpress(1);

        private void btnPreview_Click(object sender, RoutedEventArgs e) => PrintDevexpress(2);

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_receiptsSource.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للتصدير",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // تصدير بسيط إلى CSV
                var sb = new StringBuilder();
                sb.AppendLine("م,نوع السند,رقم السند,رقم المرجع,التاريخ,المستخدم,قيمة السند,حالة المزامنة,محذوف");

                foreach (var row in _receiptsSource)
                {
                    sb.AppendLine(
                        $"{row.DgvNo}," +
                        $"{row.DgvType}," +
                        $"{row.DgvReceiptNo}," +
                        $"{row.DgvRefNo}," +
                        $"{row.DgvDate:d}," +
                        $"{row.DgvUser}," +
                        $"{row.DgvSum:N2}," +
                        $"{row.DgvSyncStatusText}," +
                        $"{row.DgvISDeletedText}");
                }

                string fileName = "Receipts_Export.csv";
                File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);

                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(fileName)
                    { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في التصدير\nتفاصيل الخطأ: {ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Button Events - Details & Sync

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is string globalId)
                {
                    var frm = new frmInvoiceDetails();
                    MainClass.ApplyPermissionToForm(frm);
                    MainClass.DoApplyUserSett(frm);
                    frm.InvGlobalId = globalId;
                    frm.ShowDialog();
                }
            }
            catch { }
        }

        private async void btnSync_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var answer = DXMessageBox.Show(
                    "هل أنت متأكد من مزامنة السندات المحددة؟",
                    "", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (answer != MessageBoxResult.Yes) return;

                // جمع السندات المحددة
                var selectedRows = new List<ReceiptRow>();
                foreach (var row in _receiptsSource)
                {
                    if (row.IsSelected)
                        selectedRows.Add(row);
                }

                if (selectedRows.Count == 0)
                {
                    DXMessageBox.Show("يرجى تحديد سند واحد على الأقل",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var receiptOper = new ReceiptOper();
                var receiptCRUD = new ReceiptCRUD(Sync.APIUrl);
                var list        = new List<Receipt>();

                foreach (var row in selectedRows)
                {
                    var receipt = receiptOper.BindReceiptByID(row.DgvGlobalID);

                    if (cmbMainBranch.SelectedIndex > -1)
                        receipt.DistBranch = (int?)cmbMainBranch.SelectedValue;

                    list.Add(receipt);
                    Home Home = new Home();
                    if (Home._is_active)
                    {
                        string globalId = row.DgvGlobalID;
                        string cond     = $"WHERE GlobalID=N'{globalId}'";
                        string cond2    = $"WHERE GlobalID=N'{globalId}'";
                        string cond3    = $"WHERE EntryGlobalID=N'{globalId}'";

                        SendData.SendDataa("Receipts",      "0", SendData.GetReceipts(cond));
                        SendData.SendDataa("entry",         "0", SendData.GetEntryData(cond2));
                        SendData.SendDataa("entryDetails",  "0", SendData.GetEntrySubData(cond3));
                    }
                }

                // تحديث حالة المزامنة في قاعدة البيانات
                EnsureOpen(_conn);
                foreach (var receipt in list)
                {
                    using (var cmd = new SqlCommand(
                        $"UPDATE receipts SET Sync=1 WHERE GlobalID=N'{receipt.GlobalID}'",
                        _conn))
                        cmd.ExecuteNonQuery();
                }
                CloseConn(_conn);

                // إرسال عبر API
                if (Sync.ValidAPIUrl)
                    await receiptCRUD.PostReceipts(list);

                DXMessageBox.Show("تمت العملية بنجاح",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);

                // تحديث عرض الجدول
                foreach (var row in selectedRows)
                {
                    row.DgvSyncStatus     = true;
                    row.DgvSyncStatusText = "✅ متزامن";
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء المزامنة: " + ex.Message);
            }
            finally { CloseConn(_conn); }
        }

        #endregion

        #region CheckBox Events

        private void CheckBox1_Changed(object sender, RoutedEventArgs e)
        {
            cmbBranches.IsEnabled = (CheckBox1.IsChecked != true);
            if (CheckBox1.IsChecked == true)
                cmbBranches.SelectedIndex = -1;
        }

        private void ckAllInvs_Changed(object sender, RoutedEventArgs e)
        {
            cmbReceiptType.IsEnabled = (ckAllInvs.IsChecked != true);
            if (ckAllInvs.IsChecked == true)
                cmbReceiptType.SelectedIndex = -1;
        }

        private void ckAllType_Changed(object sender, RoutedEventArgs e)
        {
            cmbType.IsEnabled = (ckAllType.IsChecked != true);
            if (ckAllType.IsChecked == true)
                cmbType.SelectedIndex = -1;
        }

        private void ckAllUsers_Changed(object sender, RoutedEventArgs e)
        {
            cmbusers.IsEnabled = (ckAllUsers.IsChecked != true);
            if (ckAllUsers.IsChecked == true)
                cmbusers.SelectedIndex = -1;
        }

        private void ckTotalPeriod_Changed(object sender, RoutedEventArgs e)
        {
            bool enabled = (ckTotalPeriod.IsChecked != true);
            txtFromDate.IsEnabled = enabled;
            txtToDate.IsEnabled   = enabled;
        }

        #endregion

        #region Select All

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox chk)
            {
                bool selectAll = chk.IsChecked == true;
                foreach (var row in _receiptsSource)
                    row.IsSelected = selectAll;
            }
        }

        #endregion

        #region Print

        private DataSet BindToData()
        {
            var list = new List<InventoryData>();
            var ds   = new DataSet("Name");
            ds.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        private void PrintDevexpress(int printType)
        {
            _rptUrl     = MainClass.ReportsPath;
            _defPrinter = MainClass.ReportsPrinter;
            _rptName    = "rptInvsProfitSales.repx";

            if (_receiptsSource.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات بالجدول",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_rptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير",
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

            try
            {
                var report = XtraReport.FromFile(fullPath);
                report.DataSource = BindToData();

                string headerPath = Path.Combine(_rptUrl, "header.repx");
                if (File.Exists(headerPath))
                {
                    var headerRpt = XtraReport.FromFile(headerPath);
                    headerRpt.DataSource = Common.FoundationInfoDT;
                    var subHeader = report.FindControl("headerRpt", true) as XRSubreport;
                    if (subHeader != null) subHeader.ReportSource = headerRpt;
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

                report.Dispose();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في الطباعة: " + ex.Message);
            }
        }

        #endregion

        #region Send Stored Receipts

        public void SendStoredReceipts()
        {
            if (ConnectBroker.CheckConnectionAndBroker())
            {
                string cond  = "";
                string cond2 = "";
                string cond3 = "";

                if (ckTotalPeriod.IsChecked != true)
                {
                    string from = txtFromDate.DateTime.ToShortDateString();
                    string to   = txtToDate.DateTime.ToShortDateString();

                    cond  = $"WHERE ReceiptDate>=N'{from}' AND ReceiptDate<=N'{to}'";
                    cond2 = $"WHERE Date>=N'{from}' AND Date<=N'{to}'";
                    cond3 = $"WHERE TRY_CAST(EntryGLobalID AS nvarchar) IN " +
                            $"(SELECT GlobalID FROM Entry WHERE Date>=N'{from}' AND Date<=N'{to}')";
                }

                if (ConnectBroker.CheckConnectionAndBroker())
                {
                    ConnectBroker.mqttClient.Publish(
                        ConnectBroker.ClientCode + "Receipts",
                        Encoding.UTF8.GetBytes(SendData.GetReceipts(cond)), 0, true);
                    ConnectBroker.mqttClient.Publish(
                        ConnectBroker.ClientCode + "Entry",
                        Encoding.UTF8.GetBytes(SendData.GetEntryData(cond2)), 0, true);
                    ConnectBroker.mqttClient.Publish(
                        ConnectBroker.ClientCode + "EntryGlobalID",
                        Encoding.UTF8.GetBytes(SendData.GetEntrySubData(cond3)), 0, true);
                }
            }
        }

        #endregion

        #region Helpers

        private void EnsureOpen(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Open) conn.Open();
        }

        private void CloseConn(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Closed) conn.Close();
        }

        #endregion
    }

    #region Model

    public class ReceiptRow : System.ComponentModel.INotifyPropertyChanged
    {
        private bool   _isSelected;
        private bool   _dgvSyncStatus;
        private string _dgvSyncStatusText;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public int      DgvNo            { get; set; }
        public string   DgvType          { get; set; }
        public string   DgvGlobalID      { get; set; }
        public string   DgvReceiptBranch { get; set; }
        public int      DgvReceiptNo     { get; set; }
        public string   DgvRefNo         { get; set; }
        public DateTime DgvDate          { get; set; }
        public string   DgvPayType       { get; set; }
        public string   DgvUser          { get; set; }
        public double   DgvSum           { get; set; }
        public bool     DgvISDeleted     { get; set; }
        public string   DgvISDeletedText { get; set; }

        public bool DgvSyncStatus
        {
            get => _dgvSyncStatus;
            set
            {
                _dgvSyncStatus = value;
                OnPropertyChanged(nameof(DgvSyncStatus));
            }
        }

        public string DgvSyncStatusText
        {
            get => _dgvSyncStatusText;
            set
            {
                _dgvSyncStatusText = value;
                OnPropertyChanged(nameof(DgvSyncStatusText));
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this,
               new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    #endregion
}