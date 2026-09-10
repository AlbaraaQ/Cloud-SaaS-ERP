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
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.XtraReports.UI;
using UtilitiesProj;
using Button = System.Windows.Controls.Button;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmTaxInvDetails : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;

        public int  Invtype      = 0;
        private int Proc_Type    = 1;
        private int Inv_Type     = 0;

        private bool   PrintHeader  = true;
        private bool   PrintFooter  = true;
        private bool   PrintStamp   = true;
        private int    PrintType    = 0;
        private int    PrintNo      = 1;
        private string defPrinter   = "";
        private string RptName      = "";
        private string RptUrl       = "";

        private double InvSum       = 0;
        private double InvDiscount  = 0;
        private double InvVAT       = 0;
        private double InvAvrgCost  = 0;
        private bool   IsEvaluated  = true;
        private bool   IsVAT        = true;
        private double defVAT       = 0;
        private bool   PricIncVAT   = false;

        // مجاميع يتم حسابها يدوياً (بديل CustomSummary)
        private double Sum         = 0;
        private double Sum1        = 0;
        private double Discount    = 0;
        private double Discount1   = 0;
        private double NetBeforVat = 0;
        private double NetBeforVat1= 0;
        private double VAT         = 0;
        private double VAT1        = 0;
        private double Net         = 0;
        private double Net1        = 0;

        private ObservableCollection<InvDetailsModel> invoiceRows;

        #endregion

        #region Constructor

        public frmTaxInvDetails()
        {
            InitializeComponent();
            conn        = MainClass.ConnObj();
            invoiceRows = new ObservableCollection<InvDetailsModel>();
            GridControl1.ItemsSource = invoiceRows;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var Home = new Home();
            if (Invtype == 0)
            {
                txtDateFrom.DateTime = DateTime.Now.Date;
                txtDateTo.DateTime   = DateTime.Now.Date;
            }

            LoadBranches();

            if (Invtype == 1)
            {
                bool isAr = string.Equals(MainClass.Language, "ar");
                cmbProcType.Items.Add(isAr ? "مشتريات"       : "Purchases");
                cmbProcType.Items.Add(isAr ? "مرتجع مشتريات" : "Purchase Returns");
            }
            else if (Invtype == 2)
            {
                bool isAr = string.Equals(MainClass.Language, "ar");
                if (isAr)
                {
                    cmbProcType.Items.Add("مبيعات");
                    cmbProcType.Items.Add("نقطة بيع");
                    cmbProcType.Items.Add("مرتجع مبيعات");
                    cmbProcType.Items.Add("مرتجع نقطة بيع");
                    if (Home.StripMarine.Visibility == Visibility.Visible)
                    {
                        cmbProcType.Items.Add("تأجير");
                        cmbProcType.Items.Add("مرتجع تأجير");
                    }
                }
                else
                {
                    cmbProcType.Items.Add("Sales");
                    cmbProcType.Items.Add("POS Sales");
                    cmbProcType.Items.Add("Return Sales");
                    cmbProcType.Items.Add("POS Returns");
                    if (Home.StripMarine.Visibility == Visibility.Visible)
                    {
                        cmbProcType.Items.Add("Rent");
                        cmbProcType.Items.Add("Rent Return");
                    }
                }
            }

            LoadPrintSettings();
        }

        #endregion

        #region Load Branches

        private void LoadBranches()
        {
            try
            {
                string cond = "";
                if (MainClass.BranchNo != -1)
                    cond = !string.Equals(Accounting.BranchCondition, " ")
                        ? $" AND id={MainClass.BranchNo}"
                        : "";

                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Branches WHERE IS_Deleted=0{cond}", conn);
                var table = new DataTable();
                adapter.Fill(table);
                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath = "id";
                cmbBranches.ItemsSource       = table.DefaultView;
                cmbBranches.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء تحميل الفروع\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Load Print Settings

        private void LoadPrintSettings()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=12", conn);
                var table = new DataTable();
                adapter.Fill(table);
                if (table.Rows.Count == 1)
                {
                    try
                    {
                        int.TryParse(table.Rows[0]["printType"].ToString(), out PrintType);
                        PrintFooter = Convert.ToBoolean(table.Rows[0]["PrintFooter"]);
                        PrintHeader = Convert.ToBoolean(table.Rows[0]["PrintHeader"]);
                        PrintStamp  = Convert.ToBoolean(table.Rows[0]["PrintStamp"]);
                        defPrinter  = table.Rows[0]["CasherPrinter"].ToString();
                        if (string.IsNullOrEmpty(defPrinter))
                            defPrinter = Common.GetDefaultPrinter();
                        int.TryParse(table.Rows[0]["printNo"].ToString(), out PrintNo);
                    }
                    catch { }
                }

                adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingGeneral WHERE Inv_Id=2", conn);
                table = new DataTable();
                adapter.Fill(table);
                if (table.Rows.Count == 1)
                {
                    try
                    {
                        PricIncVAT = Convert.ToBoolean(table.Rows[0]["PriceIncVAT"]);
                        double.TryParse(table.Rows[0]["MainVAT"].ToString(), out defVAT);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Show Invoices

        public void ShowInvs()
        {
            try
            {
                invoiceRows.Clear();
                ResetSummaries();

                ProgressBar1.Value   = 0;
                ProgressBar1.Maximum = 100;

                string typeFilter   = "";
                string typeFilter2  = "";
                string dateFilter   = "";
                string branchFilter = "";

                if (chkAll.IsChecked == true)
                {
                    if (Invtype == 1)
                        typeFilter = " (inv_type=1 AND proc_type<=2) AND ";
                    else if (Invtype == 2)
                    {
                        typeFilter  = " (inv_type=2 AND proc_type<=2) AND ";
                        typeFilter2 = " (inv_type=3 AND proc_type<=2 OR inv_type=20 AND proc_type<=2) AND ";
                    }
                }
                else
                {
                    typeFilter = $" inv_type={Inv_Type} AND proc_type={Proc_Type} AND ";
                }

                if (ckTotalPeriod.IsChecked != true)
                    dateFilter = " (date>=@date1 AND date<@date2) AND ";

                if (cmbBranches.SelectedIndex > -1)
                    branchFilter = $"inv.branch={cmbBranches.SelectedValue} AND ";

                string sql = "SELECT InvGlobalID,proc_id,proc_type,id,date,inv_type," +
                             "cust_id,InvTotal,AdditionsTot,tot_net,Reff_No,Reff_date," +
                             "branch,minus,tax,ExtraVAT,pay_type,bank,InvSum,VATPercent,InvCost " +
                             $"FROM inv WHERE {branchFilter}{typeFilter}{dateFilter}IS_Deleted=0 " +
                             "ORDER BY date DESC";

                var fromDate = DateTime.Parse(
                    txtDateFrom.DateTime.ToShortDateString() + " 00:00:00");
                var toDate   = DateTime.Parse(
                    txtDateTo.DateTime.ToShortDateString()   + " 23:59:59");

                var adapter = new SqlDataAdapter(sql, conn);
                adapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = fromDate;
                adapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = toDate;

                var mainTable = new DataTable();
                adapter.Fill(mainTable);

                // دمج فواتير POS إذا لزم
                if (Invtype == 2 && chkAll.IsChecked == true &&
                    !string.IsNullOrEmpty(typeFilter2))
                {
                    string sql2 = "SELECT InvGlobalID,proc_id,proc_type,id,date,inv_type," +
                                  "cust_id,InvTotal,AdditionsTot,tot_net,Reff_No,Reff_date," +
                                  "branch,minus,tax,ExtraVAT,pay_type,bank,InvSum,VATPercent,InvCost " +
                                  $"FROM inv WHERE {branchFilter}{typeFilter2}{dateFilter}IS_Deleted=0 " +
                                  "ORDER BY date DESC";
                    var adapter2 = new SqlDataAdapter(sql2, conn);
                    adapter2.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = fromDate;
                    adapter2.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = toDate.AddHours(24);
                    var table2 = new DataTable();
                    adapter2.Fill(table2);
                    mainTable.Merge(table2);
                }

                ProgressBar1.Maximum = mainTable.Rows.Count > 0 ? mainTable.Rows.Count : 1;
                var invoiceObj = new InvoiceObj(Invtype, 1);

                for (int i = 0; i < mainTable.Rows.Count; i++)
                {
                    try
                    {
                        ProcessInvoiceRow(mainTable, i, invoiceObj);
                    }
                    catch { }
                    ProgressBar1.Value++;
                }

                // فواتير مشتريات بدون تفاصيل صنف
                if (Invtype == 1)
                    ProcessPurchasesWithoutSubs(branchFilter, typeFilter, dateFilter, fromDate, toDate);

                // المصروفات (ReceiptType=9)
                if ((Invtype == 1 && chkAll.IsChecked == true) ||
                    (Inv_Type == 1 && Proc_Type == 1))
                    ProcessExpenseReceipts(fromDate, toDate);

                // المقبوضات (ReceiptType=10)
                if ((Invtype == 2 && chkAll.IsChecked == true) ||
                    (Inv_Type == 2 && Proc_Type == 1))
                    ProcessIncomeReceipts(fromDate, toDate);

                UpdateSummaryCards();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Process Invoice Row

        private void ProcessInvoiceRow(DataTable mainTable, int i, InvoiceObj invoiceObj)
        {
            var subAdapter = new SqlDataAdapter(
                $"SELECT (Inv_Sub.val1 * Inv_Sub.exchange_price) AS Sum," +
                $"ISNULL(Inv_Sub.discount,0) AS ItDiscount," +
                $"Inv_Sub.taxval AS Vat, Inv_sub.ProductId AS ProductId," +
                $"Inv_Sub.val * Inv_Sub.AvrgCost AS AvrgCost " +
                $"FROM Inv_Sub WHERE Inv_Sub.InvGlobalID=N'{mainTable.Rows[i]["InvGlobalID"]}' " +
                $"AND ProductId=0", conn);

            var subTable = new DataTable();
            subAdapter.Fill(subTable);
            if (subTable.Rows.Count <= 0) return;

            InvSum = InvDiscount = InvVAT = InvAvrgCost = 0;
            double discountShare = 0;
            IsVAT = true;

            double invSumCell   = 0;
            if (double.TryParse(mainTable.Rows[i]["InvSum"].ToString(), out double invSumVal))
                invSumCell = invSumVal;
            double minusCell = 0;
            if (double.TryParse(mainTable.Rows[i]["minus"].ToString(), out double minusVal))
                minusCell = minusVal;

            foreach (DataRow subRow in subTable.Rows)
            {
                double rowSum      = ToDouble(subRow["Sum"]);
                double rowVat      = ToDouble(subRow["Vat"]);
                double rowDiscount = ToDouble(subRow["ItDiscount"]);
                double rowAvrg     = ToDouble(subRow["AvrgCost"]);
                double rowProdId   = ToDouble(subRow["ProductId"]);
                double share       = 0;

                if (rbNoVAT.IsChecked == true)
                {
                    if (rowVat == 0 && rowProdId == 0)
                    {
                        IsVAT = false;
                        InvSum += rowSum; InvVAT += rowVat;
                        InvAvrgCost += rowAvrg; InvDiscount += rowDiscount;
                        share = invSumCell > 0
                            ? (minusCell * rowSum / invSumCell) - rowVat
                            : 0;
                    }
                }
                else if (rbWithVAT.IsChecked == true)
                {
                    if (rowVat > 0)
                    {
                        InvSum += rowSum; InvVAT += rowVat;
                        InvAvrgCost += rowAvrg; InvDiscount += rowDiscount;
                        share = invSumCell > 0
                            ? minusCell * rowSum / invSumCell
                            : 0;
                    }
                }
                else
                {
                    if (rowVat == 0) IsVAT = false;
                    InvSum += rowSum; InvVAT += rowVat;
                    InvAvrgCost += rowAvrg; InvDiscount += rowDiscount;
                    share = invSumCell > 0
                        ? minusCell * rowSum / invSumCell
                        : 0;
                }

                if (double.IsNaN(share)) share = 0;
                discountShare += share;
            }

            if (invoiceObj.PriceIncVAT) InvSum -= InvVAT;
            if (rbWithVAT.IsChecked == true && InvVAT == 0) return;
            if (!IsVAT && rbWithVAT.IsChecked == true) return;
            if (IsVAT && rbNoVAT.IsChecked == true) return;

            double invType  = ToDouble(mainTable.Rows[i]["inv_type"]);
            double procType = ToDouble(mainTable.Rows[i]["proc_type"]);

            string invTypeName = GetInvTypeName(invType, procType);
            InvVAT = ToDouble(mainTable.Rows[i]["tax"]);
            if (rbNoVAT.IsChecked == true) InvVAT = 0;

            double totalDiscount = Math.Round(discountShare + InvDiscount, 2);
            double netBeforeVat;
            if (invoiceObj.PriceIncVAT)
            {
                double vatShare = discountShare - discountShare / (1 + invoiceObj.VAT / 100.0);
                netBeforeVat = Math.Round(InvSum - (discountShare - vatShare + InvDiscount), 2);
            }
            else
            {
                netBeforeVat = Math.Round(InvSum - totalDiscount, 2);
            }

            double netTotal = Math.Round(netBeforeVat + InvVAT, 2);

            var row = new InvDetailsModel
            {
                DgvNo          = invoiceRows.Count + 1,
                DgvInvType     = invTypeName,
                DgvInvNo       = mainTable.Rows[i]["id"].ToString(),
                DgvSuplierInvNo= mainTable.Rows[i]["Reff_No"].ToString(),
                DgvDate        = Convert.ToDateTime(mainTable.Rows[i]["date"]).ToShortDateString(),
                DgvClient      = GetCustomerInfo(ToInt(mainTable.Rows[i]["cust_id"]), "name"),
                DgvVATNo       = GetCustomerInfo(ToInt(mainTable.Rows[i]["cust_id"]), "vat"),
                DgvSum2        = Math.Round(InvSum, 2),
                DgvDiscount    = totalDiscount,
                DgvNetBeforVat = netBeforeVat,
                DgvVAT         = InvVAT,
                DgvNet         = netTotal,
                DgvInvType1    = mainTable.Rows[i]["inv_type"].ToString(),
                DgvOperType    = mainTable.Rows[i]["proc_type"].ToString(),
                DgvInvGlobalID = mainTable.Rows[i]["InvGlobalID"].ToString()
            };

            invoiceRows.Add(row);
            AccumulateSummary(row);
        }

        #endregion

        #region Process Purchases Without Subs

        private void ProcessPurchasesWithoutSubs(
            string branchFilter, string typeFilter,
            string dateFilter, DateTime fromDate, DateTime toDate)
        {
            try
            {
                string sql = $"SELECT * FROM inv WHERE {branchFilter}{typeFilter}{dateFilter}" +
                             "IS_Deleted=0 AND inv_type=1 AND " +
                             "Inv.InvGlobalID NOT IN (SELECT InvGlobalID FROM Inv_Sub) " +
                             "ORDER BY date DESC";
                var adapter = new SqlDataAdapter(sql, conn);
                adapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value =
                    fromDate.ToShortDateString();
                adapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value =
                    toDate.AddHours(24).ToShortDateString();
                var table = new DataTable();
                adapter.Fill(table);

                foreach (DataRow dr in table.Rows)
                {
                    var invObj2 = new InvoiceObj(ToInt(dr["inv_type"]), ToInt(dr["proc_type"]));
                    InvSum = InvDiscount = InvVAT = InvAvrgCost = 0;

                    double tax   = ToDouble(dr["tax"]);
                    double total = ToDouble(dr["InvTotal"]);
                    double minus = ToDouble(dr["minus"]);

                    if (rbNoVAT.IsChecked == true)
                    {
                        if (tax == 0) { IsVAT = false; InvSum = total; InvVAT = tax; InvDiscount = minus; }
                    }
                    else if (rbWithVAT.IsChecked == true)
                    {
                        if (tax > 0) { InvSum = total; InvVAT = tax; InvDiscount = minus; }
                    }
                    else
                    {
                        if (tax == 0) IsVAT = false;
                        InvSum = total; InvVAT = tax; InvDiscount = minus;
                    }

                    if (PricIncVAT) InvSum -= InvVAT;
                    if (rbWithVAT.IsChecked == true && InvVAT == 0) continue;
                    if (!IsVAT && rbWithVAT.IsChecked == true) continue;
                    if (IsVAT && rbNoVAT.IsChecked == true) continue;

                    double netBef  = Math.Round(InvSum - minus, 2);
                    double vatVal  = Math.Round(netBef * (invObj2.VAT / 100.0), 3);
                    double netTot  = Math.Round(netBef + vatVal, 2);

                    var row = new InvDetailsModel
                    {
                        DgvNo          = invoiceRows.Count + 1,
                        DgvInvType     = GetInvTypeName(ToDouble(dr["inv_type"]), ToDouble(dr["proc_type"])),
                        DgvInvNo       = dr["id"].ToString(),
                        DgvSuplierInvNo= dr["Reff_No"].ToString(),
                        DgvDate        = Convert.ToDateTime(dr["date"]).ToShortDateString(),
                        DgvClient      = GetCustomerInfo(ToInt(dr["cust_id"]), "name"),
                        DgvVATNo       = GetCustomerInfo(ToInt(dr["cust_id"]), "vat"),
                        DgvSum2        = Math.Round(InvSum, 2),
                        DgvDiscount    = Math.Round(minus, 2),
                        DgvNetBeforVat = netBef,
                        DgvVAT         = vatVal,
                        DgvNet         = netTot,
                        DgvInvType1    = dr["inv_type"].ToString(),
                        DgvOperType    = dr["proc_type"].ToString(),
                        DgvInvGlobalID = dr["InvGlobalID"].ToString()
                    };
                    invoiceRows.Add(row);
                    AccumulateSummary(row);
                }
            }
            catch { }
        }

        #endregion

        #region Process Expense / Income Receipts

        private void ProcessExpenseReceipts(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT * FROM Receipts WHERE ReceiptType=9 " +
                    $"AND branchID={MainClass.BranchNo} " +
                    $"AND ReceiptDate>=@date1 AND ReceiptDate<@date2 " +
                    $"AND Receipts.ISDeleted=0", conn);
                adapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value =
                    fromDate.ToShortDateString();
                adapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value =
                    toDate.AddHours(24).ToShortDateString();
                var table = new DataTable();
                adapter.Fill(table);

                foreach (DataRow dr in table.Rows)
                {
                    double payment = Math.Round(ToDouble(dr["Payment"]), 2);
                    double vatVal  = Math.Round(ToDouble(dr["VAT"]), 2);
                    double netVal  = Math.Round(ToDouble(dr["Netval"]), 2);

                    var row = new InvDetailsModel
                    {
                        DgvNo          = invoiceRows.Count + 1,
                        DgvInvType     = "مصروفات",
                        DgvInvNo       = dr["ReceiptNo"].ToString(),
                        DgvSuplierInvNo= dr["ReffNo"].ToString(),
                        DgvDate        = Convert.ToDateTime(dr["ReceiptDate"]).ToShortDateString(),
                        DgvClient      = GetSupplierName(ToInt(dr["ClientID"])),
                        DgvVATNo       = GetTaxNo2(ToInt(dr["ClientID"])),
                        DgvSum2        = payment,
                        DgvDiscount    = 0,
                        DgvNetBeforVat = payment,
                        DgvVAT         = vatVal,
                        DgvNet         = netVal,
                        DgvInvType1    = "",
                        DgvOperType    = "3",
                        DgvInvGlobalID = ""
                    };
                    invoiceRows.Add(row);
                    AccumulateSummary(row);
                }
            }
            catch { }
        }

        private void ProcessIncomeReceipts(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT * FROM Receipts WHERE ReceiptType=10 " +
                    $"AND branchID={MainClass.BranchNo} " +
                    $"AND ReceiptDate>=@date1 AND Paymenttype=1 " +
                    $"AND ReceiptDate<@date2 AND Receipts.ISDeleted=0", conn);
                adapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value =
                    fromDate.ToShortDateString();
                adapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value =
                    toDate.AddHours(24).ToShortDateString();
                var table = new DataTable();
                adapter.Fill(table);

                foreach (DataRow dr in table.Rows)
                {
                    double payment = Math.Round(ToDouble(dr["Payment"]), 2);
                    double netVal  = Math.Round(ToDouble(dr["Netval"]), 2);
                    double vatVal  = Math.Round(netVal - payment, 2);

                    var row = new InvDetailsModel
                    {
                        DgvNo          = invoiceRows.Count + 1,
                        DgvInvType     = "مقبوضات",
                        DgvInvNo       = dr["ReceiptNo"].ToString(),
                        DgvSuplierInvNo= dr["ReffNo"].ToString(),
                        DgvDate        = Convert.ToDateTime(dr["ReceiptDate"]).ToShortDateString(),
                        DgvClient      = GetSupplierName(ToInt(dr["ClientID"])),
                        DgvVATNo       = GetTaxNo2(ToInt(dr["ClientID"])),
                        DgvSum2        = payment,
                        DgvDiscount    = 0,
                        DgvNetBeforVat = payment,
                        DgvVAT         = vatVal,
                        DgvNet         = netVal,
                        DgvInvType1    = "",
                        DgvOperType    = "4",
                        DgvInvGlobalID = ""
                    };
                    invoiceRows.Add(row);
                    AccumulateSummary(row);
                }
            }
            catch { }
        }

        #endregion

        #region Summary Logic (بديل CustomSummary)

        private void ResetSummaries()
        {
            Sum = Sum1 = Discount = Discount1 =
            NetBeforVat = NetBeforVat1 =
            VAT = VAT1 = Net = Net1 = 0;
        }

        private void AccumulateSummary(InvDetailsModel row)
        {
            bool isReturn = row.DgvOperType == "2";
            if (isReturn)
            {
                Sum1        += row.DgvSum2;
                Discount1   += row.DgvDiscount;
                NetBeforVat1+= row.DgvNetBeforVat;
                VAT1        += row.DgvVAT;
                Net1        += row.DgvNet;
            }
            else
            {
                Sum         += row.DgvSum2;
                Discount    += row.DgvDiscount;
                NetBeforVat += row.DgvNetBeforVat;
                VAT         += row.DgvVAT;
                Net         += row.DgvNet;
            }
        }

        private void UpdateSummaryCards()
        {
            lblSumTotal.Text       = $"{Math.Round(Sum - Sum1, 2):N2}";
            lblSumDiscount.Text    = $"{Math.Round(Discount - Discount1, 2):N2}";
            lblSumNetBeforVat.Text = $"{Math.Round(NetBeforVat - NetBeforVat1, 2):N2}";
            lblSumVAT.Text         = $"{Math.Round(VAT - VAT1, 2):N2}";
            lblSumNet.Text         = $"{Math.Round(Net - Net1, 2):N2}";
        }

        #endregion

        #region Helper: GetInvTypeName

        private string GetInvTypeName(double invType, double procType)
        {
            if      (invType == 1  && procType == 1) return "مشتريات";
            else if (invType == 2  && procType == 1) return "مبيعات";
            else if (invType == 3  && procType == 1) return "نقطة بيع";
            else if (invType == 1  && procType == 2) return "مرتجع مشتريات";
            else if (invType == 2  && procType == 2) return "مرتجع مبيعات";
            else if (invType == 3  && procType == 2) return "مرتجع نقطة بيع";
            else if (invType == 20 && procType == 1) return "مبيع أندرويد";
            else if (invType == 20 && procType == 2) return "مرتجع أندرويد";
            return "";
        }

        #endregion

        #region Helper: Customer Info

        private string GetCustomerInfo(int id, string field)
        {
            try
            {
                string col = field == "vat" ? "tax_no" : "name";
                var adapter = new SqlDataAdapter(
                    $"SELECT {col} FROM Customers WHERE IS_Deleted=0 AND id={id}", conn);
                var table = new DataTable();
                adapter.Fill(table);
                return table.Rows.Count > 0 ? table.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        private string GetSupplierName(int id)
        {
            try
            {
                string tbl = Invtype == 1 ? "Suppliers" : "VATClients";
                var adapter = new SqlDataAdapter(
                    $"SELECT name FROM {tbl} WHERE id={id} AND IS_Deleted=0", conn);
                var table = new DataTable();
                adapter.Fill(table);
                return table.Rows.Count == 1 ? table.Rows[0]["name"].ToString() : "";
            }
            catch { return ""; }
        }

        private string GetTaxNo2(int id)
        {
            try
            {
                string tbl = Invtype == 1 ? "Suppliers" : "VATClients";
                var adapter = new SqlDataAdapter(
                    $"SELECT taxNo FROM {tbl} WHERE id={id} AND IS_Deleted=0", conn);
                var table = new DataTable();
                adapter.Fill(table);
                return table.Rows.Count == 1 ? table.Rows[0]["taxNo"].ToString() : "";
            }
            catch { return ""; }
        }

        #endregion

        #region Helper: Type Conversion

        private static double ToDouble(object obj)
        {
            if (obj == null || obj == DBNull.Value) return 0;
            double.TryParse(obj.ToString(), out double result);
            return result;
        }

        private static int ToInt(object obj)
        {
            if (obj == null || obj == DBNull.Value) return 0;
            int.TryParse(obj.ToString(), out int result);
            return result;
        }

        #endregion

        #region Events

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            ShowInvs();
        }

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            cmbProcType.IsEnabled = chkAll.IsChecked != true;
        }

        private void chkAllBranches_CheckedChanged(object sender, RoutedEventArgs e)
        {
            cmbBranches.IsEnabled = chkAllBranches.IsChecked != true;
        }

        private void ckTotalPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool enabled = ckTotalPeriod.IsChecked != true;
            txtDateFrom.IsEnabled = enabled;
            txtDateTo.IsEnabled   = enabled;
        }

        private void cmbProcType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Inv_Type = Invtype;
            int idx = cmbProcType.SelectedIndex;

            if (idx == 0 && Invtype == 1) { Proc_Type = 1; Inv_Type = 1; }
            if (idx == 1 && Invtype == 1) { Proc_Type = 2; Inv_Type = 1; }
            if (idx == 0 && Invtype == 2) { Proc_Type = 1; Inv_Type = 2; }
            if (idx == 1 && Invtype == 2) { Inv_Type  = 3; Proc_Type = 1; }
            if (idx == 2 && Invtype == 2) { Proc_Type = 2; Inv_Type = 2; }
            if (idx == 3 && Invtype == 2) { Inv_Type  = 3; Proc_Type = 2; }
            if (idx == 4 && Invtype == 2) { Inv_Type  = 4; Proc_Type = 1; }
            if (idx >  5 && Invtype == 2) { Inv_Type  = 4; Proc_Type = 2; }
        }

        private void GridControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // يمكن إضافة منطق عند تغيير التحديد
        }

        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InvDetailsModel row)
                OpenDetailsWindow(row);
        }

        private void OpenDetailsWindow(InvDetailsModel row)
        {
            try
            {
                if (row.DgvInvType == "مصروفات")
                {
                    var frm = new frmSandVAT();
                    frm.Show();
                    frm.Navigate(
                        $"SELECT * FROM Receipts WHERE ReceiptType=9 " +
                        $"AND ISDeleted=0 AND ReceiptNo={row.DgvInvNo}");
                    frm.Activate();
                    return;
                }

                int.TryParse(row.DgvInvType1,  out int invType1);
                int.TryParse(row.DgvOperType,  out int operType);
                string globalId = row.DgvInvGlobalID;

                if (invType1 == 1 && operType == 1)
                {
                    var frm = new frmInvPurch { ProcType = 1, InvType = 1 };
                    frm.Show();
                    frm.WindowState = System.Windows.WindowState.Maximized;
                    frm.Navigate(
                        $"SELECT * FROM Inv WHERE IS_Deleted=0 AND proc_type=1 " +
                        $"AND inv_type=1 AND InvGlobalID=N'{globalId}'");
                    frm.Activate();
                }
                else if (invType1 == 2 && operType == 1)
                {
                    var frm = new frmInvSale { ProcType = 1, InvType = 2 };
                    frm.Show();
                    frm.WindowState = System.Windows.WindowState.Maximized;
                    frm.Navigate(
                        $"SELECT * FROM Inv WHERE IS_Deleted=0 AND proc_type=1 " +
                        $"AND inv_type=2 AND InvGlobalID=N'{globalId}'");
                    frm.Activate();
                }
                else if (invType1 == 3 && operType == 1)
                {
                    var frm = new frmInvPOS();
                    frm.Show();
                    frm.WindowState = System.Windows.WindowState.Maximized;
                    frm.Navigate(
                        $"SELECT * FROM Inv WHERE IS_Deleted=0 AND proc_type=1 " +
                        $"AND inv_type=3 AND InvGlobalID=N'{globalId}'");
                    frm.Activate();
                }
                else if (invType1 == 1 && operType == 2)
                {
                    var frm = new frmInvPurch
                        { Title = "مرتجع مشتريات", ProcType = 2, InvType = 1 };
                    frm.Show();
                    frm.WindowState = System.Windows.WindowState.Maximized;
                    frm.Navigate(
                        $"SELECT * FROM Inv WHERE IS_Deleted=0 AND proc_type=2 " +
                        $"AND inv_type=1 AND InvGlobalID=N'{globalId}'");
                    frm.Activate();
                }
                else if (invType1 == 2 && operType == 2)
                {
                    var frm = new frmInvSale
                        { Title = "مرتجع مبيع", ProcType = 2, InvType = 2 };
                    frm.Show();
                    frm.WindowState = System.Windows.WindowState.Maximized;
                    frm.Navigate(
                        $"SELECT * FROM Inv WHERE IS_Deleted=0 AND proc_type=2 " +
                        $"AND inv_type=2 AND InvGlobalID=N'{globalId}'");
                    frm.Activate();
                }
                else if (invType1 == 3 && operType == 2)
                {
                    var frm = new frmInvPOS { Title = "مرتجع" };
                    frm.Show();
                    frm.WindowState = System.Windows.WindowState.Maximized;
                    frm.Navigate(
                        $"SELECT * FROM Inv WHERE IS_Deleted=0 AND proc_type=2 " +
                        $"AND inv_type=3 AND InvGlobalID=N'{globalId}'");
                    frm.Activate();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (invoiceRows.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للتصدير",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter      = "Excel Files|*.xlsx",
                    DefaultExt  = "xlsx",
                    FileName    = "TaxInvoices_Export"
                };

                if (saveDialog.ShowDialog() != true) return;

                // تصدير البيانات إلى XML مؤقتاً (يمكن استبداله بـ EPPlus)
                var table = new DataTable("TaxInvoices");
                table.Columns.Add("نوع الفاتورة");
                table.Columns.Add("رقم الفاتورة");
                table.Columns.Add("التاريخ");
                table.Columns.Add("العميل");
                table.Columns.Add("المجموع");
                table.Columns.Add("الخصم");
                table.Columns.Add("الصافي قبل الضريبة");
                table.Columns.Add("الضريبة");
                table.Columns.Add("الصافي");

                foreach (var row in invoiceRows)
                {
                    table.Rows.Add(
                        row.DgvInvType, row.DgvInvNo, row.DgvDate,
                        row.DgvClient,  row.DgvSum2,  row.DgvDiscount,
                        row.DgvNetBeforVat, row.DgvVAT, row.DgvNet);
                }

                table.WriteXml(saveDialog.FileName);
                Process.Start(new ProcessStartInfo(saveDialog.FileName)
                    { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء التصدير\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            PrintReport(2);
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintReport(1);
        }

        #endregion

        #region Print

        private DataSet BindToData()
        {
            var list = new List<InventoryData>();
            foreach (var row in invoiceRows)
            {
                list.Add(new InventoryData
                {
                    ProcessType    = cmbProcType.SelectedItem?.ToString() ?? "",
                    InvType        = row.DgvInvType,
                    InvoiceNo      = row.DgvInvNo,
                    RefsInvNo      = row.DgvSuplierInvNo ?? "",
                    InvDate        = row.DgvDate,
                    Clint          = row.DgvClient,
                    ClintVatNo     = row.DgvVATNo ?? "",
                    Total          = row.DgvSum2.ToString("N2"),
                    NetDiscount    = row.DgvDiscount.ToString("N2"),
                    NetBeforeTax   = row.DgvNetBeforVat.ToString("N2"),
                    Tax            = row.DgvVAT.ToString("N2"),
                    Net            = row.DgvNet.ToString("N2"),
                    FromDate       = txtDateFrom.DateTime.ToShortDateString(),
                    ToDate         = txtDateTo.DateTime.ToShortDateString(),
                    InventoryType  = this.Title,
                    Sum            = lblSumTotal.Text,
                    SumNetDiscount = lblSumDiscount.Text,
                    Total1         = lblSumNetBeforVat.Text,
                    SumTax         = lblSumVAT.Text,
                    NetTotal       = lblSumNet.Text,
                    VATFilter      = rbAllVat.IsChecked == true ? rbAllVat.Content.ToString()
                                   : rbWithVAT.IsChecked == true ? rbWithVAT.Content.ToString()
                                   : rbNoVAT.Content.ToString(),
                    User           = Common.GetEmpName(MainClass.EmpNo),
                    PrintDate      = DateTime.Now.ToShortDateString()
                });
            }

            var ds    = new DataSet("Name");
            var table = UtilitiesProj.Common.ToDataTable(list);
            ds.Tables.Add(table);
            return ds;
        }

        private void PrintReport(int type)
        {
            RptUrl = MainClass.ReportsPath;
            defPrinter = MainClass.ReportsPrinter;
            RptName = Invtype == 1
                ? "rptTaxInvDetailsPurchase.repx"
                : "rptTaxInvDetailsSales.repx";

            if (invoiceRows.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات بالجدول",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!Directory.Exists(RptUrl) || !File.Exists(Path.Combine(RptUrl, RptName)))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var report = DevExpress.XtraReports.UI.XtraReport.FromFile(
                    Path.Combine(RptUrl, RptName));
                report.DataSource = BindToData();

                var header = DevExpress.XtraReports.UI.XtraReport.FromFile(
                    Path.Combine(RptUrl, "header.repx"));
                header.DataSource = Common.FoundationInfoDT;

                var headerSub = report.FindControl("headerRpt", ignoreCase: true)
                    as DevExpress.XtraReports.UI.XRSubreport;
                if (headerSub != null) headerSub.ReportSource = header;

                var footer = DevExpress.XtraReports.UI.XtraReport.FromFile(
                    Path.Combine(RptUrl, "footer.repx"));
                footer.DataSource = Common.FoundationInfoDT;

                var footerSub = report.FindControl("footerRpt", ignoreCase: true)
                    as DevExpress.XtraReports.UI.XRSubreport;
                if (footerSub != null) footerSub.ReportSource = footer;

                if (!string.IsNullOrEmpty(defPrinter))
                {
                    report.PrinterName = defPrinter;
                    if (type == 1)
                        for (int k = 1; k <= PrintNo; k++)
                            report.Print();
                    else
                        report.ShowPreviewDialog();
                    report.Dispose();
                }
                else
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الطباعة\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }

    #region Model

    public class InvDetailsModel
    {
        public int    DgvNo           { get; set; }
        public string DgvInvType      { get; set; }
        public string DgvInvNo        { get; set; }
        public string DgvSuplierInvNo { get; set; }
        public string DgvDate         { get; set; }
        public string DgvClient       { get; set; }
        public string DgvVATNo        { get; set; }
        public double DgvSum2         { get; set; }
        public double DgvDiscount     { get; set; }
        public double DgvNetBeforVat  { get; set; }
        public double DgvVAT          { get; set; }
        public double DgvNet          { get; set; }
        public string DgvInvType1     { get; set; }
        public string DgvOperType     { get; set; }
        public string DgvInvGlobalID  { get; set; }
    }

    #endregion
}