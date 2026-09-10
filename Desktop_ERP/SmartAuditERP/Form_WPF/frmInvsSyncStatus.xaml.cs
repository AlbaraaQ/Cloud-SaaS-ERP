using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using DevExpress.XtraReports.UI;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using UtilitiesProj;
using Ws_Auditor;
using Button = System.Windows.Controls.Button;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInvsSyncStatus : DevExpress.Xpf.Core.ThemedWindow
    {
        // ══════════════════════════════════════════════════════════════
        #region Inner Class

        public class MissedInvo
        {
            public int InvoId;
            public int ProcType;
            public int InvType;
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Fields

        private SqlConnection _conn;

        public int Invtype = 0;

        private int _procType = 1;
        private int _invType = 0;
        private bool _printHeader = true;
        private bool _printFooter = true;
        private bool _printStamp = true;
        private int _printType;
        private int _printNo = 1;
        private string _defPrinter = string.Empty;
        private string _rptName = string.Empty;
        private string _rptUrl = string.Empty;

        private double _invSum;
        private double _invDiscount;
        private double _invVAT;
        private double _invAvrgCost;
        private bool _isEvaluated = true;
        private double _defVAT;
        private bool _priceIncVAT;

        // Custom summary accumulators
        private double Total, Total1;

        // DataTable المعروضة حالياً
        private DataTable _currentDataTable;

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Constructor

        public frmInvsSyncStatus()
        {
            InitializeComponent();
            _conn = MainClass.ConnObj();
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Window Loaded

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtFromDate.SelectedDate = DateTime.Today;
            txtToDate.SelectedDate = DateTime.Today;

            LoadPaymentTypes();
            LoadInvTypes();
            LoadEmps();
            LoadStores();
            LoadCustomers();
            LoadPrintSettings();
            LoadBranches();
            ApplyDgvSettings();
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Data Loading

        private void LoadPaymentTypes()
        {
            cmbType.Items.Clear();
            if (IsArabic())
            {
                cmbType.Items.Add("نقدية");
                cmbType.Items.Add("آجلة");
            }
            else
            {
                cmbType.Items.Add("Cash");
                cmbType.Items.Add("Postpone");
            }
        }

        private void LoadInvTypes()
        {
            cmbInvType.Items.Clear();
            if (IsArabic())
            {
                cmbInvType.Items.Add("مبيعات");
                cmbInvType.Items.Add("نقطة بيع");
                cmbInvType.Items.Add("مشتريات");
                cmbInvType.Items.Add("إدخال و إخراج");
                cmbInvType.Items.Add("مناقلة مستلمة");
                cmbInvType.Items.Add("مناقلة مرسلة");
            }
            else
            {
                cmbInvType.Items.Add("Sales Inv");
                cmbInvType.Items.Add("POS");
                cmbInvType.Items.Add("Purchases");
                cmbInvType.Items.Add("Input Output");
                cmbInvType.Items.Add("Move Received");
                cmbInvType.Items.Add("Move Send");
            }
        }

        private void LoadEmps()
        {
            try
            {
                using var da = new SqlDataAdapter(
                    "SELECT id, name FROM Employees " +
                    "WHERE IS_Deleted=0 ORDER BY id", _conn);
                var dt = new DataTable();
                da.Fill(dt);
                // cmbusers مخفي في الواجهة لكن موجود في الأصل
            }
            catch { /* صامت */ }
        }

        private void LoadStores()
        {
            using var da = new SqlDataAdapter(
                "SELECT id, name FROM Safes " +
                "WHERE IS_Deleted=0 AND status<>2 ORDER BY id", _conn);
            var dt = new DataTable();
            da.Fill(dt);
            cmbStore.ItemsSource = dt.DefaultView;
            cmbStore.DisplayMemberPath = "name";
            cmbStore.SelectedValuePath = "id";
            cmbStore.SelectedIndex = -1;
        }

        private void LoadBranches()
        {
            using var da = new SqlDataAdapter(
                "SELECT id, name FROM Branches WHERE IS_Deleted=0", _conn);
            var dt = new DataTable();
            da.Fill(dt);

            cmbBranches.ItemsSource = dt.DefaultView;
            cmbBranches.DisplayMemberPath = "name";
            cmbBranches.SelectedValuePath = "id";
            cmbBranches.SelectedIndex = -1;

            // cmbMainBranch نفس البيانات
            using var da2 = new SqlDataAdapter(
                "SELECT id, name FROM Branches WHERE IS_Deleted=0", _conn);
            var dt2 = new DataTable();
            da2.Fill(dt2);

            cmbMainBranch.ItemsSource = dt2.DefaultView;
            cmbMainBranch.DisplayMemberPath = "name";
            cmbMainBranch.SelectedValuePath = "id";
            cmbMainBranch.SelectedIndex = -1;
        }

        private void LoadCustomers()
        {
            using var da = new SqlDataAdapter(
                "SELECT id, name FROM Customers " +
                "WHERE (type=1 OR type=3) ORDER BY id", _conn);
            var dt = new DataTable();
            da.Fill(dt);
            cmbClients.ItemsSource = dt.DefaultView;
            cmbClients.DisplayMemberPath = "name";
            cmbClients.SelectedValuePath = "id";
            cmbClients.SelectedIndex = -1;
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
                        try
                        {
                            _printType = Convert.ToInt32(dt.Rows[0]["printType"]);
                            _printFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                            _printHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                            _printStamp = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                            _defPrinter = dt.Rows[0]["CasherPrinter"]?.ToString()
                                           ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(_defPrinter))
                                _defPrinter = Common.GetDefaultPrinter();
                            _printNo = Convert.ToInt32(dt.Rows[0]["printNo"]);
                        }
                        catch { /* صامت */ }
                    }
                }

                using (var da = new SqlDataAdapter(
                    $"SELECT * FROM SettingGeneral WHERE Inv_Id={Invtype}", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count == 1)
                    {
                        try
                        {
                            _priceIncVAT = Convert.ToBoolean(dt.Rows[0]["PriceIncVAT"]);
                            _defVAT = Convert.ToDouble(dt.Rows[0]["MainVAT"]);
                        }
                        catch { /* صامت */ }
                    }
                }
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private void ApplyDgvSettings()
        {
            // لا حاجة لإعدادات إضافية في WPF
            // تنسيق التاريخ محدد في XAML
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Show Invoices

        public void ShowInvs(string filterCond)
        {
            try
            {
                var resultTable = new DataTable();
                resultTable.Columns.Add("DgvNo", typeof(int));
                resultTable.Columns.Add("DgvInvType", typeof(string));
                resultTable.Columns.Add("DgvInvGlobalID", typeof(string));
                resultTable.Columns.Add("DgvInvBranch", typeof(string));
                resultTable.Columns.Add("DgvInvNo", typeof(int));
                resultTable.Columns.Add("DgvRefNo", typeof(string));
                resultTable.Columns.Add("DgvDate", typeof(DateTime));
                resultTable.Columns.Add("DgvPayType", typeof(string));
                resultTable.Columns.Add("DgvClient", typeof(string));
                resultTable.Columns.Add("DgvUser", typeof(string));
                resultTable.Columns.Add("DgvNet", typeof(double));
                resultTable.Columns.Add("DgvCloudID", typeof(string));
                resultTable.Columns.Add("DgvOperType", typeof(string));
                resultTable.Columns.Add("DgvProcType", typeof(int));
                resultTable.Columns.Add("DgvStore", typeof(string));
                resultTable.Columns.Add("DgvSyncStatus", typeof(bool));
                resultTable.Columns.Add("DgvISDeleted", typeof(bool));

                GridControl1.ItemsSource = null;

                using var da = new SqlDataAdapter(
                    "SELECT Inv_type, Proc_type, id, Reff_No, cust_id, " +
                    "sales_emp, tot_net, Sync, IS_Deleted, InvGlobalID, " +
                    "safe, branch, CloudID, date, pay_type " +
                    $"FROM inv WHERE {filterCond} id IS NOT NULL " +
                    "ORDER BY id", _conn);

                if (ckTotalPeriod.IsChecked != true)
                {
                    DateTime fromDate =
                        (txtFromDate.SelectedDate ?? DateTime.Today).Date;
                    DateTime toDate =
                        (txtToDate.SelectedDate ?? DateTime.Today).AddDays(1).Date;

                    da.SelectCommand.Parameters
                      .Add("@date1", SqlDbType.DateTime).Value = fromDate;
                    da.SelectCommand.Parameters
                      .Add("@date2", SqlDbType.DateTime).Value = toDate;
                }

                var invoicesTable = new DataTable();
                da.Fill(invoicesTable);

                Dispatcher.Invoke(() =>
                {
                    ProgressBar1.Value = 0;
                    ProgressBar1.Maximum = invoicesTable.Rows.Count;
                });

                bool isAr = IsArabic();

                for (int i = 0; i < invoicesTable.Rows.Count; i++)
                {
                    DataRow row = invoicesTable.Rows[i];

                    int invTypeId = Convert.ToInt32(row["Inv_type"]);
                    int procTypeId = Convert.ToInt32(row["Proc_type"]);
                    int payTypeId = Convert.ToInt32(row["pay_type"]);

                    string invTypeName = InvoiceOper.GetInvoiceType(
                        invTypeId, procTypeId, 0, 1);

                    string payText = ResolvePaymentText(payTypeId, isAr);

                    string globalId =
                        row["InvGlobalID"].ToString();
                    string refNo =
                        row["Reff_No"].ToString();
                    string clientName =
                        Common.GetClientName(Convert.ToInt32(row["cust_id"]));
                    string empName =
                        Common.GetEmpName(Convert.ToInt32(row["sales_emp"]));
                    double netVal =
                        Convert.ToDouble(row["tot_net"]);
                    bool syncStatus =
                        Convert.ToBoolean(row["Sync"]);
                    bool isDeleted =
                        Convert.ToBoolean(row["IS_Deleted"]);
                    string cloudId =
                        row["CloudID"] != DBNull.Value
                            ? row["CloudID"].ToString()
                            : string.Empty;
                    string storeName =
                        Common.GetStoreName(Convert.ToInt32(row["safe"]));
                    string branchName =
                        Common.GetBranchName(Convert.ToInt32(row["branch"]));

                    resultTable.Rows.Add(
                        i + 1,
                        invTypeName,
                        globalId,
                        branchName,
                        Convert.ToInt32(row["id"]),
                        refNo,
                        Convert.ToDateTime(row["date"]),
                        payText,
                        clientName,
                        empName,
                        netVal,
                        cloudId,
                        string.Empty,
                        procTypeId,
                        storeName,
                        syncStatus,
                        isDeleted);

                    Dispatcher.Invoke(() => ProgressBar1.Value++);
                }

                _currentDataTable = resultTable;
                GridControl1.ItemsSource = resultTable.DefaultView;

                RecalculateNetSummary();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private void RecalculateNetSummary()
        {
            if (_currentDataTable == null) return;

            Total = Total1 = 0;

            foreach (DataRow row in _currentDataTable.Rows)
            {
                int procType = Convert.ToInt32(row["DgvProcType"]);
                double net = Convert.ToDouble(row["DgvNet"]);

                if (procType == 1) Total += net;
                else Total1 += net;
            }
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Button Events

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            string whereClause = BuildWhereClause();
            ShowInvs(whereClause);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentDataTable == null
                    || _currentDataTable.Rows.Count == 0)
                {
                    ShowInfo("لا توجد بيانات للتصدير.");
                    return;
                }

                var dlg = new SaveFileDialog
                {
                    Title = "تصدير البيانات",
                    Filter = "CSV File (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = $"{Title}.csv"
                };

                if (dlg.ShowDialog() != true) return;

                ExportToCsv(dlg.FileName);
                Process.Start(new ProcessStartInfo(dlg.FileName)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => PrintReport(1);

        private void btnPreview_Click(object sender, RoutedEventArgs e)
            => PrintReport(2);

        private async void btnSync_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Home Home = new Home();
                string confirmMsg = IsArabic()
                    ? "هل انت متأكد من مزامنة الفواتير المختارة ؟"
                    : "Are you sure to sync selected invoices?";

                if (DXMessageBox.Show(confirmMsg, "تأكيد",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) == MessageBoxResult.No)
                    return;

                int[] handles = GridView1.GetSelectedRowHandles();
                if (handles == null || handles.Length == 0)
                {
                    ShowInfo("لا توجد صفوف محددة.");
                    return;
                }

                var invoiceOper = new InvoiceOper();
                var invoiceList = new List<Invoice>();
                var invoiceCRUD = new InvoiceCRUD(Sync.APIUrl);

                foreach (int handle in handles)
                {
                    object rowObj = GridControl1.GetRow(handle);
                    if (rowObj is not DataRowView drv) continue;

                    string globalId =
                        drv["DgvInvGlobalID"]?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(globalId)) continue;

                    Invoice inv = invoiceOper.BindInvoByID(globalId);
                    if (inv == null) continue;

                    if (cmbMainBranch.SelectedIndex > -1
                        && cmbMainBranch.SelectedValue != null)
                    {
                        inv.DistBranch = Convert.ToInt32(cmbMainBranch.SelectedValue);
                    }

                    invoiceList.Add(inv);

                    // مزامنة فورية عبر Broker
                    if (Home._is_active && ConnectBroker.CheckConnectionAndBroker())
                    {
                        string cond = $"where InvGlobalID=N'{globalId}'";
                        string obj = $"WHERE InvGlobalID=N'{globalId}'";
                        SendData.SendDataa("Inv",
                            globalId, SendData.GetInv(cond));
                        SendData.SendDataa("InvSub",
                            globalId, SendData.GetInvSub(obj));
                    }
                }

                if (invoiceList.Count == 0) return;

                Dispatcher.Invoke(() =>
                {
                    ProgressBar1.Value = 0;
                    ProgressBar1.Maximum = invoiceList.Count;
                });

                // مزامنة Cloud
                if (Sync.ValidAPIUrl && !MainClass.IsTrial)
                {
                    if (Sync.BranchType == 4)
                    {
                        var clsInvoice = new ClsInvoice(Sync.APIUrl, "");
                        var payTypes =
                            InvoiceOper.GetInvoiceCloudPaymentType();

                        int idx = 0;
                        foreach (Invoice inv in invoiceList)
                        {
                            if (inv == null) continue;

                            Result result = await clsInvoice.Save(
                                new List<Invoice> { inv },
                                User.CurrentCloudUser,
                                payTypes);

                            if (result.IsValid)
                            {
                                EnsureConnectionOpen();
                                using var cmd = new SqlCommand(
                                    "UPDATE Inv SET Sync=1 " +
                                    "WHERE InvGlobalID=@InvGlobalID", _conn);
                                cmd.Parameters.Add(
                                    "@InvGlobalID", SqlDbType.NVarChar)
                                   .Value = inv.InvGlobalID;
                                cmd.ExecuteNonQuery();
                                EnsureConnectionClosed();

                                new InvoiceCRUD(Sync.APIUrl).CLearfiles();
                                ShowSuccess("تمت العملية بنجاح ✅");
                            }
                            else
                            {
                                ShowError(
                                    $"فشل مزامنة الفاتورة رقم: {inv.InvoiceNo}\n" +
                                    result.ErrorMessage);
                            }

                            idx++;
                            Dispatcher.Invoke(() => ProgressBar1.Value = idx);
                        }

                        // تحديث آخر مزامنة
                        EnsureConnectionOpen();
                        using var syncCmd = new SqlCommand(
                            "UPDATE SettingSync SET InvoicesLastSync=@dt",
                            _conn);
                        syncCmd.Parameters
                               .Add("@dt", SqlDbType.DateTime)
                               .Value = DateTime.Now;
                        syncCmd.ExecuteNonQuery();
                        EnsureConnectionClosed();
                        return;
                    }

                    // مزامنة بـ API عادي
                    invoiceList = (List<Invoice>)
                        await invoiceCRUD.PostInvoices(invoiceList);
                }

                // تحديث حالة المزامنة في قاعدة البيانات
                EnsureConnectionOpen();
                int progress = 0;
                foreach (Invoice inv in invoiceList)
                {
                    if (inv == null) continue;
                    using var cmd = new SqlCommand(
                        $"UPDATE Inv SET Sync=1 " +
                        $"WHERE InvGlobalID=N'{inv.InvGlobalID}'", _conn);
                    cmd.ExecuteNonQuery();

                    progress++;
                    Dispatcher.Invoke(() => ProgressBar1.Value = progress);
                }
                EnsureConnectionClosed();

                ShowSuccess("تمت المزامنة بنجاح ✅");
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Grid Cell Buttons

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((sender as Button)?.Tag is not DataRowView drv)
                    return;

                string globalId = drv["DgvInvGlobalID"]?.ToString()
                                  ?? string.Empty;
                if (string.IsNullOrWhiteSpace(globalId)) return;

                var detailForm = new frmInvoiceDetails
                {
                    InvGlobalId = globalId
                };
                detailForm.btnRecalculateCost.Visibility = Visibility.Collapsed;
                MainClass.ApplyPermissionToForm(detailForm);
                MainClass.DoApplyUserSett(detailForm);
                detailForm.ShowDialog();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private void BtnChangeStatus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((sender as Button)?.Tag is not DataRowView drv)
                    return;

                string globalId = drv["DgvInvGlobalID"]?.ToString()
                                  ?? string.Empty;

                if (string.IsNullOrWhiteSpace(globalId))
                {
                    ShowError("Cannot update record: No valid InvGlobalID selected.");
                    return;
                }

                EnsureConnectionOpen();

                using var cmd = new SqlCommand(
                    "UPDATE Inv SET Sync=1 WHERE InvGlobalID=@InvGlobalID",
                    _conn);
                cmd.Parameters.AddWithValue("@InvGlobalID", globalId);
                cmd.ExecuteNonQuery();

                DXMessageBox.Show("Record updated", "Update Status",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
            }
            catch (Exception ex) { ShowError(ex.Message); }
            finally { EnsureConnectionClosed(); }
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Search Panel Handlers

        private void ckAllType_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool allChecked = ckAllType.IsChecked == true;
            cmbType.IsEnabled = !allChecked;
            if (allChecked) cmbType.SelectedIndex = -1;
        }

        private void ckAllInvs_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool allChecked = ckAllInvs.IsChecked == true;
            cmbInvType.IsEnabled = !allChecked;
            if (allChecked) cmbInvType.SelectedIndex = -1;
        }

        private void ckAllStore_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool allChecked = ckAllStore.IsChecked == true;
            cmbStore.IsEnabled = !allChecked;
            if (allChecked) cmbStore.SelectedIndex = -1;
        }

        private void ckAllClients_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool allChecked = ckAllClients.IsChecked == true;
            cmbClients.IsEnabled = !allChecked;
            if (allChecked) cmbClients.SelectedIndex = -1;
        }

        private void ckBranches_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool allChecked = ckBranches.IsChecked == true;
            cmbBranches.IsEnabled = !allChecked;
            if (allChecked) cmbBranches.SelectedIndex = -1;
        }

        private void ckTotalPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool allPeriod = ckTotalPeriod.IsChecked == true;
            txtFromDate.IsEnabled = !allPeriod;
            txtToDate.IsEnabled = !allPeriod;
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Build Where Clause

        private string BuildWhereClause()
        {
            string clause = string.Empty;

            // الفرع
            if (cmbBranches.SelectedIndex > 0
                && cmbBranches.SelectedValue != null)
                clause += $"inv.branch={cmbBranches.SelectedValue} AND ";

            // العميل
            if (ckAllClients.IsChecked != true
                && cmbClients.SelectedIndex > -1)
                clause += $"cust_id={cmbClients.SelectedValue} AND ";

            // المستودع
            if (ckAllStore.IsChecked != true
                && cmbStore.SelectedIndex > -1)
                clause += $"inv.safe={cmbStore.SelectedValue} AND ";

            // نوع الإجراء
            clause += "(inv.proc_type=1 OR inv.proc_type=2 " +
                      "OR inv.proc_type=4) AND ";

            // نوع الفاتورة
            if (ckAllInvs.IsChecked != true)
            {
                switch (cmbInvType.SelectedIndex)
                {
                    case 0: clause += "inv.inv_type=2 AND "; break;
                    case 1: clause += "inv.inv_type=3 AND "; break;
                    case 2: clause += "inv.inv_type=1 AND "; break;
                    case 3: clause += "(inv.inv_type=4 OR inv.inv_type=5) AND "; break;
                    case 4: clause += "(inv.inv_type=8 AND inv.proc_type=1) AND "; break;
                    case 5: clause += "(inv.inv_type=8 AND inv.proc_type=2) AND "; break;
                }
            }

            // حالة المزامنة
            if (rbSynced.IsChecked == true)
                clause += "inv.Sync=1 AND ";
            else if (rbNotSynced.IsChecked == true)
                clause += "inv.Sync=0 AND ";

            // نوع الدفع
            if (ckAllType.IsChecked != true)
            {
                if (cmbType.SelectedIndex == 1)
                    clause += "inv.pay_type=-1 AND ";
                else if (cmbType.SelectedIndex == 0)
                    clause += "inv.pay_type>0 AND ";
            }

            // التاريخ
            if (ckTotalPeriod.IsChecked != true)
                clause += "(date>=@date1 AND date<=@date2) AND ";

            // المحذوفات
            if (ckDeleted.IsChecked == true)
                clause += "IS_Deleted=1 AND ";

            return clause;
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Print / Report

        private void PrintReport(int printMode)
        {
            _rptUrl = MainClass.ReportsPath;
            _defPrinter = MainClass.ReportsPrinter;
            _rptName = "rptInvSumByClient.repx";

            if (_currentDataTable == null
                || _currentDataTable.Rows.Count == 0)
            {
                ShowInfo("لا توجد عمليات بالجدول");
                return;
            }

            if (string.IsNullOrWhiteSpace(_rptUrl))
            {
                ShowInfo("يجب تحديد مسار التقرير");
                return;
            }

            string fullPath = Path.Combine(_rptUrl, _rptName);
            if (!Directory.Exists(_rptUrl) || !File.Exists(fullPath))
            {
                ShowError("المسار الحالي للتقارير غير موجود أو تم تعديله");
                return;
            }

            try
            {
                var xtraReport =
                    DevExpress.XtraReports.UI.XtraReport.FromFile(fullPath);
                xtraReport.DataSource = BuildReportDataSet();

                string headerPath = Path.Combine(_rptUrl, "header.repx");
                if (File.Exists(headerPath))
                {
                    var headerReport =
                        DevExpress.XtraReports.UI.XtraReport
                                  .FromFile(headerPath);
                    headerReport.DataSource = Common.FoundationInfoDT;

                    var sub = xtraReport.FindControl("headerRpt", true)
                              as DevExpress.XtraReports.UI.XRSubreport;
                    if (sub != null) sub.ReportSource = headerReport;
                }

                if (string.IsNullOrWhiteSpace(_defPrinter))
                {
                    ShowInfo("يجب تحديد الطابعة من الإعدادات");
                    return;
                }

                xtraReport.PrinterName = _defPrinter;
                if (printMode == 1)
                    for (int i = 0; i < _printNo; i++)
                        xtraReport.Print();
                else
                    xtraReport.ShowPreviewDialog();

                xtraReport.Dispose();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private DataSet BuildReportDataSet()
        {
            var list = new List<InventoryData>();
            if (_currentDataTable == null) return new DataSet("Name");

            string fromDate =
                (txtFromDate.SelectedDate ?? DateTime.Today).ToShortDateString();
            string toDate =
                (txtToDate.SelectedDate ?? DateTime.Today).ToShortDateString();

            foreach (DataRow row in _currentDataTable.Rows)
            {
                list.Add(new InventoryData
                {
                    InvType = row["DgvInvType"]?.ToString(),
                    InvoiceNo = row["DgvInvNo"].ToString(),
                    RefsInvNo = row["DgvRefNo"]?.ToString(),
                    InvDate = row["DgvDate"] is DateTime dt
                                    ? dt.ToString("M/d/yyyy HH:mm:ss")
                                    : string.Empty,
                    Clint = row["DgvClient"]?.ToString(),
                    NetBeforeTax = row["DgvInvBranch"]?.ToString(),
                    Net = row["DgvNet"] != DBNull.Value
                                    ? row["DgvNet"].ToString() : "0",
                    SafeName = row["DgvStore"]?.ToString(),
                    EmpName = row["DgvUser"]?.ToString(),
                    InventoryType = Title,
                    NetTotal = (Total - Total1).ToString("0.##"),
                    FromDate = fromDate,
                    ToDate = toDate,
                    ProcessType = cmbType.Text,
                    User = Common.GetEmpName(MainClass.EmpNo),
                    PrintDate = DateTime.Now.ToShortDateString()
                });
            }

            var ds = new DataSet("Name");
            var table = global::UtilitiesProj.Common.ToDataTable(list);
            ds.Tables.Add(table);
            return ds;
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Export CSV

        private void ExportToCsv(string filePath)
        {
            var lines = new List<string>
            {
                string.Join(",",
                    Csv("م"), Csv("ID"), Csv("الفرع"),
                    Csv("نوع الفاتورة"), Csv("رقم الفاتورة"),
                    Csv("رقم المرجع"), Csv("التاريخ"), Csv("نوع الدفع"),
                    Csv("العميل"), Csv("المستخدم"), Csv("المستودع"),
                    Csv("الصافي"), Csv("CloudID"),
                    Csv("حالة المزامنة"), Csv("محذوف"))
            };

            if (_currentDataTable != null)
            {
                foreach (DataRow row in _currentDataTable.Rows)
                {
                    lines.Add(string.Join(",",
                        Csv(row["DgvNo"].ToString()),
                        Csv(row["DgvInvGlobalID"].ToString()),
                        Csv(row["DgvInvBranch"].ToString()),
                        Csv(row["DgvInvType"].ToString()),
                        Csv(row["DgvInvNo"].ToString()),
                        Csv(row["DgvRefNo"].ToString()),
                        Csv(row["DgvDate"] is DateTime d
                            ? d.ToString("M/d/yyyy HH:mm:ss")
                            : string.Empty),
                        Csv(row["DgvPayType"].ToString()),
                        Csv(row["DgvClient"].ToString()),
                        Csv(row["DgvUser"].ToString()),
                        Csv(row["DgvStore"].ToString()),
                        Csv(Format(row["DgvNet"])),
                        Csv(row["DgvCloudID"].ToString()),
                        Csv(Convert.ToBoolean(row["DgvSyncStatus"])
                            ? "متزامن" : "غير متزامن"),
                        Csv(Convert.ToBoolean(row["DgvISDeleted"])
                            ? "محذوف" : "")));
                }
            }

            File.WriteAllLines(filePath, lines, new UTF8Encoding(true));
        }

        private static string Format(object val)
        {
            if (val == null || val == DBNull.Value) return "0";
            if (double.TryParse(val.ToString(), out double d))
                return d.ToString("0.##", CultureInfo.InvariantCulture);
            return val.ToString();
        }

        private static string Csv(string value)
        {
            if (value == null) return "\"\"";
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Send Stored Invoices

        public void SendStoredInv()
        {
            if (!ConnectBroker.CheckConnectionAndBroker()) return;

            string cond = string.Empty;
            string invGlobalId = string.Empty;
            string cond2 = string.Empty;
            string cond3 = string.Empty;

            if (ckTotalPeriod.IsChecked != true)
            {
                string fromStr =
                    (txtFromDate.SelectedDate ?? DateTime.Today)
                    .ToShortDateString();
                string toStr =
                    (txtToDate.SelectedDate ?? DateTime.Today)
                    .ToShortDateString();

                cond =
                    $"where Date>=N'{fromStr}' and Date<=N'{toStr}'";
                invGlobalId =
                    $"WHERE TRY_CAST(InvGlobalID AS nvarchar) IN (" +
                    $"SELECT InvGlobalID FROM inv " +
                    $"WHERE Date>=N'{fromStr}' and Date<=N'{toStr}')";
                cond2 =
                    $"where Date>=N'{fromStr}' and Date<=N'{toStr}'";
                cond3 =
                    $"where TRY_CAST(EntryGLobalID AS nvarchar) in (" +
                    $"select GlobalID from Entry " +
                    $"where Date>=N'{fromStr}' and Date<=N'{toStr}')";
            }

            string s1 = SendData.GetInv(cond) ?? string.Empty;
            string s2 = SendData.GetInvSub(invGlobalId) ?? string.Empty;
            string s3 = SendData.GetEntryData(cond2) ?? string.Empty;
            string s4 = SendData.GetEntrySubData(cond3) ?? string.Empty;

            ConnectBroker.mqttClient.Publish(
                ConnectBroker.ClientCode + "Inv",
                Encoding.UTF8.GetBytes(s1), 0, true);
            ConnectBroker.mqttClient.Publish(
                ConnectBroker.ClientCode + "InvSub",
                Encoding.UTF8.GetBytes(s2), 0, true);
            ConnectBroker.mqttClient.Publish(
                ConnectBroker.ClientCode + "Entry",
                Encoding.UTF8.GetBytes(s3), 0, true);
            ConnectBroker.mqttClient.Publish(
                ConnectBroker.ClientCode + "EntryGlobalID",
                Encoding.UTF8.GetBytes(s4), 0, true);
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Helpers

        private static string ResolvePaymentText(int payType, bool isArabic)
            => payType switch
            {
                -1 => isArabic ? "آجل" : "Credit",
                1 => isArabic ? "نقدي" : "Cash",
                2 => isArabic ? "شبكة" : "Card",
                _ => string.Empty
            };

        private static bool IsArabic()
            => string.Equals(MainClass.Language, "ar",
                             StringComparison.OrdinalIgnoreCase);

        private void EnsureConnectionOpen()
        {
            if (_conn.State != ConnectionState.Open)
                _conn.Open();
        }

        private void EnsureConnectionClosed()
        {
            if (_conn.State == ConnectionState.Open)
                _conn.Close();
        }

        private static void ShowInfo(string msg)
            => DXMessageBox.Show(msg, "تنبيه",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information);

        private static void ShowSuccess(string msg)
            => DXMessageBox.Show(msg, "نجاح",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information);

        private static void ShowError(string msg)
            => DXMessageBox.Show(msg, "خطأ",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);

        #endregion
    }
}