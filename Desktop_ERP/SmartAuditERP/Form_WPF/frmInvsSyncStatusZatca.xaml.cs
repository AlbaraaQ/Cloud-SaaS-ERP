using AuditorAPI.Models;
using AuditorAPI.Models.DGVmodels;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using DevExpress.XtraReports.UI;
using ETA_Invoice.Models;
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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using UtilitiesProj;
using ZatcaIntegrationSDK.HelperContracts;
using Button = System.Windows.Controls.Button;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInvsSyncStatusZatca : DevExpress.Xpf.Core.ThemedWindow
    {
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

        private double _defVAT;
        private bool _priceIncVAT;

        // Custom summary
        private double sum, sum1;

        private DataTable _currentDataTable;

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Constructor

        public frmInvsSyncStatusZatca()
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

            LoadInvTypes();
            LoadPrintSettings();
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Data Loading

        private void LoadInvTypes()
        {
            cmbInvType.Items.Clear();
            if (IsArabic())
            {
                cmbInvType.Items.Add("مبيعات");
                cmbInvType.Items.Add("نقطة بيع");
                cmbInvType.Items.Add("إشعار");
                cmbInvType.Items.Add("مقاولات");
                cmbInvType.Items.Add("أندرويد");
            }
            else
            {
                cmbInvType.Items.Add("Sales Inv");
                cmbInvType.Items.Add("POS");
                cmbInvType.Items.Add("Notice");
                cmbInvType.Items.Add("Contract");
                cmbInvType.Items.Add("Android");
            }
        }

        private void LoadPrintSettings()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=0", _conn))
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

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Show Invoices

        public void ShowInvs(string filterCond, string filterCondContract)
        {
            try
            {
                var resultTable = new DataTable();
                resultTable.Columns.Add("DgvNo", typeof(int));
                resultTable.Columns.Add("DgvInvType", typeof(string));
                resultTable.Columns.Add("DgvInvGlobalID", typeof(string));
                resultTable.Columns.Add("DgvInvBranch", typeof(string));
                resultTable.Columns.Add("DgvInvNo", typeof(int));
                resultTable.Columns.Add("DgvDate", typeof(DateTime));
                resultTable.Columns.Add("DgvClient", typeof(string));
                resultTable.Columns.Add("DgvUser", typeof(string));
                resultTable.Columns.Add("DgvNet", typeof(double));
                resultTable.Columns.Add("DgvMsg", typeof(string));
                resultTable.Columns.Add("DgvProcType", typeof(int));
                resultTable.Columns.Add("DgvStore", typeof(string));
                resultTable.Columns.Add("DgvSyncStatus", typeof(bool));

                GridControl1.ItemsSource = null;

                EnsureConnectionOpen();

                string sql = $@"
                    SELECT InvGlobalID, id, proc_type, Inv_type,
                           safe, branch, tot_net, cust_id,
                           sales_emp, date, ZatcaSent, pay_type
                    FROM Inv
                    WHERE {filterCond} id IS NOT NULL

                    UNION ALL

                    SELECT InvGlobalID, id, proc_type, Inv_type,
                           safe, branch, tot_net, cust_id,
                           sales_emp, date, ZatcaSent, pay_type
                    FROM InvContratct
                    WHERE {filterCondContract} id IS NOT NULL

                    ORDER BY date DESC";

                using var da = new SqlDataAdapter(sql, _conn);

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

                for (int i = 0; i < invoicesTable.Rows.Count; i++)
                {
                    DataRow row = invoicesTable.Rows[i];

                    int invTypeId = Convert.ToInt32(row["Inv_type"]);
                    int procTypeId = Convert.ToInt32(row["proc_type"]);
                    int payTypeId = Convert.ToInt32(row["pay_type"]);

                    // تحديد نوع الضريبة
                    short taxType = GetCustomerTaxType(row["cust_id"]);

                    string invTypeName = InvoiceOper.GetInvoiceType(
                        invTypeId, procTypeId, payTypeId, taxType);

                    string globalId =
                        row["InvGlobalID"].ToString();
                    string clientName =
                        Common.GetClientName(Convert.ToInt32(row["cust_id"]));
                    string empName =
                        Common.GetEmpName(Convert.ToInt32(row["sales_emp"]));
                    bool zatcaSent =
                        Convert.ToBoolean(row["ZatcaSent"]);
                    string branchName =
                        Common.GetBranchName(Convert.ToInt32(row["branch"]));
                    int procType =
                        Convert.ToInt32(row["proc_type"]);
                    string storeName =
                        Common.GetStoreName(Convert.ToInt32(row["safe"]));
                    double netVal =
                        Convert.ToDouble(row["tot_net"]);

                    // رسالة ZATCA
                    string zatcaMsg = GetZatcaMessage(globalId);

                    resultTable.Rows.Add(
                        i + 1,
                        invTypeName,
                        globalId,
                        branchName,
                        Convert.ToInt32(row["id"]),
                        Convert.ToDateTime(row["date"]),
                        clientName,
                        empName,
                        netVal,
                        zatcaMsg,
                        procType,
                        storeName,
                        zatcaSent);

                    Dispatcher.Invoke(() => ProgressBar1.Value++);
                }

                EnsureConnectionClosed();

                _currentDataTable = resultTable;
                GridControl1.ItemsSource = resultTable.DefaultView;

                RecalculateNetSummary();
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private short GetCustomerTaxType(object custId)
        {
            try
            {
                using var da = new SqlDataAdapter(
                    $"SELECT ISNULL(tax_no,'') AS tax_no " +
                    $"FROM customers WHERE id={custId}", _conn);
                var dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    string taxNo = dt.Rows[0]["tax_no"].ToString();
                    if (!string.IsNullOrWhiteSpace(taxNo))
                    {
                        if (double.TryParse(taxNo, out double val) && val > 0)
                            return 2;
                        if (!double.TryParse(taxNo, out _))
                            return 2;
                    }
                }
            }
            catch { }
            return 1;
        }

        private string GetZatcaMessage(string invGlobalId)
        {
            try
            {
                using var da = new SqlDataAdapter(
                    "SELECT ISNULL(Message,'') Message " +
                    "FROM zatcaresponse WHERE InvGlobalID=@InvGlobalID",
                    _conn);
                da.SelectCommand.Parameters.AddWithValue(
                    "@InvGlobalID", invGlobalId);
                var dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                    return dt.Rows[0]["Message"].ToString();
            }
            catch { }
            return string.Empty;
        }

        private void RecalculateNetSummary()
        {
            if (_currentDataTable == null) return;
            sum = sum1 = 0;
            foreach (DataRow row in _currentDataTable.Rows)
            {
                int procType = Convert.ToInt32(row["DgvProcType"]);
                double net = Convert.ToDouble(row["DgvNet"]);
                if (procType == 1) sum += net;
                else sum1 += net;
            }
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Button Events

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            var (filterInv, filterContract) = BuildWhereClause();
            ShowInvs(filterInv, filterContract);
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
                var Home = new Home();
                if (MainSetting.ZatcaIntegerationActive)
                {
                    await SendZatcaAsync();
                    Home.shownozatca();
                }

                if (EtaSetting.Active)
                    SendEta();
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

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region ZATCA Sync

        private async Task SendZatcaAsync()
        {
            try
            {
                string confirmMsg = IsArabic()
                    ? "هل انت متأكد من مزامنة الفواتير المختارة ؟"
                    : "Are you sure to sync selected invoices?";

                if (DXMessageBox.Show(confirmMsg, "تأكيد",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) == MessageBoxResult.No)
                    return;

                var selectedInvoices = GetSelectedInvoicesFromGrid();
                if (selectedInvoices.Count == 0)
                {
                    ShowInfo("لا توجد صفوف محددة.");
                    return;
                }

                if (MainClass.IsTrial || !MainSetting.ZatcaIntegerationActive)
                    return;

                var zatcaService = new ZatcaService();
                var invoiceCRUD = new InvoiceCRUD(Sync.APIUrl);
                var connection = MainClass.ConnObj();

                Dispatcher.Invoke(() =>
                {
                    ProgressBar1.Value = 0;
                    ProgressBar1.Maximum = selectedInvoices.Count;
                });

                using (connection)
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    int progressIdx = 0;
                    foreach (Invoice invoice in selectedInvoices)
                    {
                        Invoice invoiceRef = invoice;
                        var response = zatcaService.IntegrateInvoice(
                            ref invoiceRef,
                            MainSetting.IsProductionZatca,
                            MainSetting.IsSimulationZatca,
                            isDebit: false);

                        var zatcaResponse = BuildZatcaResponse(
                            invoiceRef, response);

                        if (invoiceRef.ZatcaSent)
                        {
                            using var cmd = new SqlCommand(
                                "UPDATE Inv SET ZatcaSent=1, " +
                                "InvoiceHash=@InvoiceHash " +
                                "WHERE InvGlobalID=@InvGlobalID",
                                connection);
                            cmd.Parameters
                               .Add("@InvGlobalID", SqlDbType.NVarChar)
                               .Value = invoiceRef.InvGlobalID;
                            cmd.Parameters
                               .Add("@InvoiceHash", SqlDbType.NVarChar)
                               .Value = invoiceRef.InvoiceHash;
                            cmd.ExecuteNonQuery();
                        }

                        InvoiceOper.InsertZatcaResponse(zatcaResponse);

                        if (invoiceRef.InvoiceType == (InvoiceType)20
                            && invoiceRef.ZatcaSent)
                        {
                            await invoiceCRUD.SendQrCode(
                                invoiceRef.InvGlobalID,
                                invoiceRef.ClientCode,
                                invoiceRef.QRCode,
                                invoiceRef.ZatcaSent);
                        }

                        progressIdx++;
                        Dispatcher.Invoke(() => ProgressBar1.Value = progressIdx);
                    }
                }

                ShowSuccess("تمت العملية بنجاح ✅");
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        private static ZatcaResponse BuildZatcaResponse(
            Invoice invoice,
            InvoiceReportingResponse response)
        {
            var zatcaResponse = new ZatcaResponse
            {
                InvGlobalID = invoice.InvGlobalID,
                Status = !string.IsNullOrEmpty(response.ReportingStatus)
                         ? response.ReportingStatus
                         : response.ClearanceStatus,
                Message = string.Empty
            };

            if (response.validationResults != null)
            {
                foreach (var err in response.validationResults.ErrorMessages)
                    zatcaResponse.Message +=
                        $"Status: {err.Status}\r\nMessage: {err.Message}\r\n";

                foreach (var warn in response.validationResults.WarningMessages)
                    zatcaResponse.Message +=
                        $"Status: {warn.Status}\r\nMessage: {warn.Message}\r\n";
            }

            zatcaResponse.Message ??= string.Empty;
            zatcaResponse.Status ??= string.Empty;

            return zatcaResponse;
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region ETA Sync

        private async void SendEta()
        {
            try
            {
                string confirmMsg = IsArabic()
                    ? "هل انت متأكد من مزامنة الفواتير المختارة ؟"
                    : "Are you sure to sync selected invoices?";

                if (DXMessageBox.Show(confirmMsg, "تأكيد",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) == MessageBoxResult.No)
                    return;

                var selectedInvoices = GetSelectedInvoicesFromGrid();
                if (selectedInvoices.Count == 0) return;

                var connection = MainClass.ConnObj();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                Dispatcher.Invoke(() =>
                {
                    ProgressBar1.Value = 0;
                    ProgressBar1.Maximum = selectedInvoices.Count;
                });

                int idx = 0;
                foreach (Invoice inv in selectedInvoices)
                {
                    bool sent;
                    if (EtaSetting.Receipt)
                        sent = await new EtaReciptService().sendERecitp(inv);
                    else
                        sent = new EtaService().sendtoETA(inv);

                    if (!sent) break;

                    using var cmd = new SqlCommand(
                        $"UPDATE Inv SET ZatcaSent=1 " +
                        $"WHERE InvGlobalID=N'{inv.InvGlobalID}'",
                        connection);
                    cmd.ExecuteNonQuery();

                    idx++;
                    Dispatcher.Invoke(() => ProgressBar1.Value = idx);
                }

                connection.Close();
                ShowSuccess("تمت العملية بنجاح ✅");
            }
            catch (Exception ex) { ShowError(ex.Message); }
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region BindInvoByID

        public Invoice BindInvoByID(string globalId)
        {
            var conn = MainClass.ConnObj();
            if (conn.State != ConnectionState.Open)
                conn.Open();

            using var reader = new SqlCommand(
                $"SELECT * FROM Inv WHERE InvGlobalID=N'{globalId}'",
                conn).ExecuteReader();

            if (!reader.HasRows) return null;
            reader.Read();

            var invoice = new Invoice();
            var invoiceObj =
                new InvoiceObj(
                    Convert.ToInt32(reader["Inv_type"]),
                    Convert.ToInt32(reader["proc_type"]));

            invoice.AutoIncrementID = Convert.ToInt32(reader["proc_id"].ToString());
            invoice.InvGlobalID = reader["InvGlobalID"].ToString();
            invoice.UUID = reader["UUID"].ToString().Trim();
            invoice.InvoiceHash = reader["InvoiceHash"].ToString().Trim();
            invoice.InvCombinedId = reader["InvCombinedId"] != DBNull.Value
                                      ? reader["InvCombinedId"].ToString()
                                      : string.Empty;
            invoice.InvoiceNo = Convert.ToInt32(reader["id"]);
            invoice.InvoiceType =
                (InvoiceType)Convert.ToInt32(reader["Inv_type"]);
            invoice.Bank = Convert.ToInt32(reader["bank"]);
            invoice.ProcType = Convert.ToInt32(reader["proc_type"]);
            invoice.OrderNo = -1;
            invoice.OrderType = Convert.ToInt32(reader["OrderType"]);
            invoice.InvDate = Convert.ToDateTime(reader["date"]);
            invoice.User = Convert.ToInt32(reader["sales_emp"]);
            invoice.Branch = Convert.ToInt32(reader["branch"]);
            invoice.DistBranch = Sync.DistBranch;
            invoice.ClientCode = Sync.ClientCode;
            invoice.BranchType = Sync.BranchType;
            invoice.Received = false;
            invoice.PriceIncVAT = Convert.ToBoolean(reader["PriceIncVAT"]);
            invoice.Total = Convert.ToDouble(reader["InvTotal"]);
            invoice.Delivery = Convert.ToDouble(reader["AdditionsTot"]);
            invoice.SumPrice = Convert.ToDouble(reader["InvSum"]);
            invoice.VAT = Convert.ToDouble(reader["tax"]);
            invoice.TotalWithholdingTax =
                Convert.ToDouble(reader["TotalWithholdingTax"]);
            invoice.Net = Convert.ToDouble(reader["tot_net"]);
            invoice.Discount = Convert.ToDouble(reader["minus"]);
            invoice.Additions = Convert.ToDouble(reader["AdditionsTot"]);
            invoice.Insurance = Convert.ToDouble(reader["Insurance"]);
            invoice.Paycash = Convert.ToDouble(reader["cash"]);
            invoice.PayATM = Convert.ToDouble(reader["visa"]);
            invoice.PayType = Convert.ToInt32(reader["pay_type"].ToString());
            invoice.PIH = InvoiceOper.GetPIH(
                invoice.InvoiceNo,
                (int)invoice.InvoiceType,
                invoice.ProcType,
                invoice.Branch);

            if (invoice.PayType == 1)
            {
                invoice.Paycash = invoice.Net;
                invoice.PayATM = 0;
                invoice.Paid = invoice.Net;
            }
            else if (invoice.PayType == 2)
            {
                invoice.Paycash = 0;
                invoice.PayATM = invoice.Net;
                invoice.Paid = invoice.Net;
            }

            invoice.Remainder = Convert.ToDouble(reader["paid"])
                                 - Convert.ToDouble(reader["tot_net"]);
            invoice.Customer = Convert.ToInt32(reader["cust_id"].ToString());
            invoice.InvNote = reader["notes"].ToString();
            invoice.IsDeleted = Convert.ToBoolean(reader["IS_Deleted"]);
            invoice.VATperc = Convert.ToDouble(reader["VATPercent"]);
            invoice.ReffNo = reader["Reff_No"].ToString();
            invoice.RefDate = Convert.ToDateTime(reader["Reff_date"]);
            invoice.Store = Convert.ToInt32(reader["safe"].ToString());
            invoice.TotDiscount = invoice.Discount;

            invoice.InvAccCode = invoice.ProcType == 1
                                 ? invoiceObj.InvAcc
                                 : invoiceObj.InvReturnAcc;

            reader.Close();

            // بنود الفاتورة
            var itemsList = new List<InvoiceItem>();
            using var da = new SqlDataAdapter(
                $@"SELECT Inv.id, Inv.proc_type, Inv.ItemId,
                          Inv.Description, Inv.notes, Inv.unit,
                          Inv.val, Inv.val1, Inv.exchange_price,
                          Inv.discount, Inv.ItemPriceWithoutVAT,
                          Inv.AvrgCost, Inv.taxval, Inv.taxperc,
                          Inv.CurrentQnty, Inv.store, Inv.expire_date,
                          Inv.WithholdingTaxPerc, Inv.WithholdingTax,
                          I.name AS ItemName, I.nameEN AS ItemNameEn,
                          I.code AS ItemCode, I.ItemProperty,
                          U.name AS UnitName,
                          ISNULL(U.UnitCode,'') AS UnitCode,
                          IU.barcode AS UnitBarcode,
                          I.EgyCodeType, I.EgyItemCode
                   FROM Inv_Sub AS Inv
                   LEFT JOIN Items AS I ON Inv.ItemId = I.id
                   LEFT JOIN Units AS U ON Inv.unit = U.id
                   LEFT JOIN ItemUnits AS IU ON (inv.ItemId=IU.ItemId
                                                 AND inv.unit=IU.unit)
                   WHERE inv.InvGlobalID=N'{invoice.InvGlobalID}'
                     AND Inv.ProductId=0
                   ORDER BY Inv.id", conn);

            var dtItems = new DataTable();
            da.Fill(dtItems);

            for (int i = 0; i < dtItems.Rows.Count; i++)
            {
                DataRow r = dtItems.Rows[i];
                var item = new InvoiceItem
                {
                    ItemRowIndex = i + 1,
                    InvGlobalID = invoice.InvGlobalID,
                    InvertoryImpact = Convert.ToInt32(r["proc_type"]),
                    ItemCode = r["ItemCode"].ToString(),
                    ItemId = Convert.ToInt32(r["ItemId"].ToString()),
                    ItemName = r["ItemName"].ToString(),
                    Description = r["Description"].ToString(),
                    ItemNotes = r["notes"].ToString(),
                    UnitName = r["UnitName"].ToString(),
                    UnitCode = r["UnitCode"].ToString(),
                    UnitID = Convert.ToInt32(r["unit"]),
                    ItemBarcode = r["UnitBarcode"].ToString(),
                    ItemPrimaryQnty = Convert.ToDouble(r["val"].ToString()),
                    ItemQuantity = Convert.ToDouble(r["val1"].ToString()),
                    ItemPrice = Convert.ToDouble(r["exchange_price"]),
                    ItemDiscount = Convert.ToDouble(r["discount"].ToString()),
                    WithholdingTax = Convert.ToDouble(r["WithholdingTax"].ToString()),
                    WithholdingTaxPerc =
                        Convert.ToDouble(r["WithholdingTaxPerc"].ToString()),
                    EgyCodeType = r["EgyCodeType"].ToString(),
                    EgyItemCode = r["EgyItemCode"].ToString()
                };

                item.ItemPriceWithoutVAT =
                    !string.IsNullOrEmpty(r["ItemPriceWithoutVAT"].ToString())
                    ? Convert.ToDouble(r["ItemPriceWithoutVAT"])
                    : item.ItemPrice;

                item.UnitEquality =
                    Common.DivisionOperation(
                        new decimal(item.ItemPrimaryQnty),
                        new decimal(item.ItemQuantity));

                item.ItemSumPrice = item.ItemQuantity * item.ItemPrice;
                item.ItemDiscountPerc =
                    Common.DivisionOperation(
                        new decimal(item.ItemDiscount),
                        new decimal(item.ItemSumPrice)) * 100.0;
                item.ItemCost =
                    r["AvrgCost"] == null || r["AvrgCost"] == DBNull.Value
                    ? 0.0 : Convert.ToDouble(r["AvrgCost"]);

                item.ItemTotalPrice = item.ItemSumPrice - item.ItemDiscount;
                item.ItemPriceDiscount =
                    item.ItemDiscount / item.ItemQuantity;
                item.ItemPriceAfterDiscount =
                    item.ItemTotalPrice / item.ItemQuantity;
                item.ItemVat = Convert.ToDouble(r["taxval"]);
                item.ItemVatPerc = Convert.ToDouble(r["taxperc"]);

                if (invoice.PriceIncVAT)
                    item.ItemTotalPrice -= item.ItemVat;

                item.ItemNetPrice =
                    item.ItemTotalPrice + item.ItemVat;
                item.ValiableInvertory =
                    Convert.ToDouble(r["CurrentQnty"]);

                invoice.TotDiscount += item.ItemDiscount;
                itemsList.Add(item);
            }

            invoice.InvoiceItems = itemsList;

            conn.Close();
            return invoice;
        }

        private List<Invoice> GetSelectedInvoicesFromGrid()
        {
            var result = new List<Invoice>();
            int[] handles = GridView1.GetSelectedRowHandles();

            if (handles == null || handles.Length == 0)
                return result;

            foreach (int handle in handles)
            {
                object rowObj = GridControl1.GetRow(handle);
                if (rowObj is not DataRowView drv) continue;

                string globalId = drv["DgvInvGlobalID"]?.ToString()
                                  ?? string.Empty;
                if (string.IsNullOrWhiteSpace(globalId)) continue;

                Invoice inv = BindInvoByID(globalId);
                if (inv != null) result.Add(inv);
            }

            return result;
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Search Panel Handlers

        private void ckAllInvs_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool allChecked = ckAllInvs.IsChecked == true;
            cmbInvType.IsEnabled = !allChecked;
            if (allChecked) cmbInvType.SelectedIndex = -1;
        }

        private void ckTotalPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool allPeriod = ckTotalPeriod.IsChecked == true;
            txtFromDate.IsEnabled = !allPeriod;
            txtToDate.IsEnabled = !allPeriod;
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        #region Build Where Clause

        private (string filterInv, string filterContract) BuildWhereClause()
        {
            // فلتر الفواتير العادية
            string inv =
                $"inv.branch={MainClass.BranchNo} AND " +
                "(inv.proc_type=1 OR inv.proc_type=2) AND ";

            // نوع الفاتورة
            if (ckAllInvs.IsChecked != true)
            {
                inv += cmbInvType.SelectedIndex switch
                {
                    0 => "inv.inv_type=2 AND ",
                    1 => "inv.inv_type=3 AND ",
                    2 => "inv.inv_type=21 AND ",
                    3 => "inv.inv_type=20 AND ",
                    _ => string.Empty
                };
            }
            else
            {
                inv += "(inv.inv_type=2 OR inv.inv_type=3 " +
                       "OR inv.inv_type=21 OR inv.inv_type=20) AND ";
            }

            // حالة ZATCA
            if (rbSynced.IsChecked == true)
                inv += "inv.ZatcaSent=1 AND ";
            else if (rbNotSynced.IsChecked == true)
                inv += "inv.ZatcaSent=0 AND ";

            // التاريخ
            if (ckTotalPeriod.IsChecked != true)
            {
                DateTime zatcaStart = GetZatcaStartDate();
                DateTime fromDate =
                    txtFromDate.SelectedDate ?? DateTime.Today;

                if (fromDate < zatcaStart)
                {
                    if (DXMessageBox.Show(
                            "تاريخ البدء المحدد قبل تاريخ تفعيل المزامنة مع الزكاة\nهل تريد الاستمرار ؟",
                            "تأكيد",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.No)
                    {
                        txtFromDate.SelectedDate = zatcaStart;
                    }
                }

                inv += "(date>=@date1 AND date<=@date2) AND ";
            }

            inv += "IS_Deleted=0 AND ";

            // فلتر عقود
            string contract =
                $"InvContratct.branch={MainClass.BranchNo} AND " +
                "(InvContratct.proc_type=1 OR InvContratct.proc_type=2) AND ";

            if (ckAllInvs.IsChecked != true)
            {
                if (cmbInvType.SelectedIndex == 3)
                    contract += "InvContratct.inv_type=23 AND ";
            }
            else
            {
                contract += "(InvContratct.inv_type=23) AND ";
            }

            if (rbSynced.IsChecked == true)
                contract += "InvContratct.ZatcaSent=1 AND ";
            else if (rbNotSynced.IsChecked == true)
                contract += "InvContratct.ZatcaSent=0 AND ";

            if (ckTotalPeriod.IsChecked != true)
                contract += "(date>=@date1 AND date<=@date2) AND ";

            contract += "IS_Deleted=0 AND ";

            return (inv, contract);
        }

        private DateTime GetZatcaStartDate()
        {
            try
            {
                EnsureConnectionOpen();
                using var da = new SqlDataAdapter(
                    "SELECT StartDate FROM settingzatca WHERE IsActive=1",
                    _conn);
                var dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                    return Convert.ToDateTime(dt.Rows[0]["StartDate"]);
            }
            catch { }
            return DateTime.Now.AddYears(-1);
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
                    NetTotal = (sum - sum1).ToString("0.##"),
                    FromDate = fromDate,
                    ToDate = toDate,
                    ProcessType = Title,
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
                    Csv("التاريخ"), Csv("العميل"), Csv("المستخدم"),
                    Csv("الصافي"), Csv("الرسالة"), Csv("حالة المزامنة"))
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
                        Csv(row["DgvDate"] is DateTime d
                            ? d.ToString("M/d/yyyy HH:mm:ss")
                            : string.Empty),
                        Csv(row["DgvClient"].ToString()),
                        Csv(row["DgvUser"].ToString()),
                        Csv(Format(row["DgvNet"])),
                        Csv(row["DgvMsg"].ToString()),
                        Csv(Convert.ToBoolean(row["DgvSyncStatus"])
                            ? "مرسل" : "لم يُرسل")));
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
        #region Helpers

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