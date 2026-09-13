using AuditorAPI.Models.DGVmodels;
using DevExpress.Xpf.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using FontFamily = System.Windows.Media.FontFamily;
using PrintDialog = System.Windows.Controls.PrintDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInvSumByClient : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;

        private string CustName;
        public int Clienttype;
        public int ClientID;

        private string invTye;
        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int PrintType;
        private int PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;

        private double InvSum;
        private bool PricIncVAT;
        private double InvDiscount;
        private double InvVAT;
        private double InvAvrgCost;
        private bool IsVAT;
        private InvoiceObj InvObj;
        private List<InvoiceTxt> InvoicesList;

        private string StyleFile;
        private string StyleFolder;

        private double nTotal;
        private double nTotal1;
        private double nVAT;
        private double nVAT1;
        private double nNet;
        private double nNet1;
        private double sum;
        private double sum1;
        private double Discount;
        private double Discount1;
        private double Cash;
        private double Cash1;
        private double PayNetwork;
        private double PayNetwork1;
        private double _ExtraTax;
        private double _ExtraTax1;
        private double _TotalTax1;
        private double _TotalTax2;
        private double _Paid1;
        private double _Paid2;
        private double _Remainder1;
        private double _Remainder2;

        #endregion

        #region Constructor

        public frmInvSumByClient()
        {
            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            CustName = "";
            Clienttype = 1;
            ClientID = -1;
            invTye = "";
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp = true;
            PrintNo = 1;
            RptName = "";
            RptUrl = "";
            InvSum = 0.0;
            InvDiscount = 0.0;
            InvVAT = 0.0;
            InvAvrgCost = 0.0;
            IsVAT = true;
            InvObj = new InvoiceObj(2, 1);
            StyleFile = Path.Combine(MainClass.ReportsPath, "Styles\\InvSumByClientLayout.xml");
            StyleFolder = Path.Combine(MainClass.ReportsPath, "Styles");
            nTotal = 0.0;
            nTotal1 = 0.0;
            nVAT = 0.0;
            nVAT1 = 0.0;
            nNet = 0.0;
            nNet1 = 0.0;
            sum = 0.0;
            sum1 = 0.0;
            Discount = 0.0;
            Discount1 = 0.0;
            Cash = 0.0;
            Cash1 = 0.0;
            PayNetwork = 0.0;
            PayNetwork1 = 0.0;
            _ExtraTax = 0.0;
            _ExtraTax1 = 0.0;
            _TotalTax1 = 0.0;
            _TotalTax2 = 0.0;
            _Paid1 = 0.0;
            _Paid2 = 0.0;
            _Remainder1 = 0.0;
            _Remainder2 = 0.0;

            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmInvSumByClient_Load(object sender, RoutedEventArgs e)
        {
            txtDateFrom.SelectedDate = DateTime.Today;
            txtDateTo.SelectedDate = DateTime.Today;
            txtStartTime.Text = "00:00";
            txtEndTime.Text = "23:59";

            LoadClients();
            loadPrintSettings();
            LoadBranches();
            BranchPermissionsi();
            LoadDGvSetting();

            if (Clienttype == 2)
            {
                Title = "إجمالي فواتير بحسب الموردين";
                lblClintType.Text = "اسم المورد";
            }
            else
            {
                Title = "إجمالي فواتير بحسب العملاء";
                lblClintType.Text = "اسم العميل";
            }
        }

        #endregion

        #region Grid Init

        private void LoadDGvSetting()
        {
            GridView2.ItemsSource = null;
            UpdateRowCount();
            ClearSummaryCards();
        }

        #endregion

        #region Check Events

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbClients.IsEnabled = chkAll.IsChecked != true;
        }

        private void chkAllPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool enablePeriod = chkAllPeriod.IsChecked != true;

            txtDateFrom.IsEnabled = enablePeriod;
            txtDateTo.IsEnabled = enablePeriod;
            txtStartTime.IsEnabled = enablePeriod;
            txtEndTime.IsEnabled = enablePeriod;
        }

        private void chkAllBranches_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbBranches.IsEnabled = chkAllBranches.IsChecked != true;
        }

        #endregion

        #region Data Loading

        public void LoadClients()
        {
            try
            {
                string sql = "select id,name from Customers where IS_Deleted=0 and (type=" + Clienttype + " or type=3) order by id";
                SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbClients.ItemsSource = dt.DefaultView;
                cmbClients.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void LoadBranches()
        {
            try
            {
                string branchCondition = "";

                if (MainClass.BranchNo != -1)
                {
                    if (!string.IsNullOrWhiteSpace(Accounting.BranchCondition))
                    {
                        branchCondition = " and id=" + MainClass.BranchNo;
                    }
                }

                string sql = "select id,name from Branches where IS_Deleted=0 " + branchCondition;
                SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbBranches.ItemsSource = dt.DefaultView;
                cmbBranches.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void BranchPermissionsi()
        {
            if (!string.IsNullOrWhiteSpace(Accounting.BranchCondition))
            {
                chkAllBranches.IsChecked = false;
                chkAllBranches.Visibility = Visibility.Collapsed;
                cmbBranches.IsEnabled = true;

                if (cmbBranches.Items.Count > 0)
                {
                    cmbBranches.SelectedIndex = 0;
                }
            }
        }

        #endregion

        #region Print Settings

        private void loadPrintSettings()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter("select * from SettingPrint where Inv_Id=12", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count != 1)
                {
                    return;
                }

                try
                {
                    PrintType = Convert.ToInt32(dt.Rows[0]["printType"]);
                    PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                    PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                    PrintStamp = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                    defPrinter = dt.Rows[0]["CasherPrinter"] == DBNull.Value ? "" : dt.Rows[0]["CasherPrinter"].ToString();

                    if (string.IsNullOrWhiteSpace(defPrinter))
                    {
                        defPrinter = Common.GetDefaultPrinter();
                    }

                    PrintNo = Convert.ToInt32(dt.Rows[0]["printNo"]);
                }
                catch
                {
                    // نفس سلوك الكود الأصلي تقريبًا: تجاهل الخطأ الداخلي
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region Buttons

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            showInvoice();
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
            try
            {
                if (InvoicesList == null || InvoicesList.Count == 0)
                {
                    System.Windows.MessageBox.Show("لا توجد بيانات للتصدير.", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    FileName = Title.Replace(" ", "_"),
                    Filter = "Excel File (*.xls)|*.xls|CSV File (*.csv)|*.csv",
                    DefaultExt = ".xls"
                };

                if (saveFileDialog.ShowDialog() != true)
                {
                    return;
                }

                if (saveFileDialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    ExportToCsv(saveFileDialog.FileName);
                }
                else
                {
                    ExportToExcelHtml(saveFileDialog.FileName);
                }

                Process.Start(new ProcessStartInfo(saveFileDialog.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Main Query

        private void showInvoice()
        {
            try
            {
                ProgressBar1.Visibility = Visibility.Visible;
                ProgressBar1.IsIndeterminate = true;

                GridView2.ItemsSource = null;

                if (InvoicesList != null)
                {
                    InvoicesList.Clear();
                }

                string whereCondition = BuildWhereCondition();

                DateTime dateFrom = BuildDateTime(txtDateFrom.SelectedDate ?? DateTime.Today, txtStartTime.Text, new TimeSpan(0, 0, 0));
                DateTime dateTo = BuildDateTime(txtDateTo.SelectedDate ?? DateTime.Today, txtEndTime.Text, new TimeSpan(23, 59, 0));

                if (chkAllPeriod.IsChecked != true)
                {
                    whereCondition += " date between '" + dateFrom.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dateTo.ToString("yyyy-MM-dd HH:mm:ss") + "' and ";
                }

                if (rbNoVAT.IsChecked == true)
                {
                    whereCondition += " inv.tax=0 and ";
                }
                else if (rbWithVat.IsChecked == true)
                {
                    whereCondition += " inv.tax<>0 and ";
                }

                whereCondition = NormalizeWhereCondition(whereCondition);

                string sqlstr =
                    "select Inv.* ," +
                    " (SELECT ISNULL(sum(Inv_Sub.val1 * Inv_Sub.exchange_price),0) from Inv_Sub where InvGlobalID=Inv.InvGlobalID) as sumPrice," +
                    " (SELECT ISNULL(sum(discount),0) from Inv_Sub where InvGlobalID=Inv.InvGlobalID) as ItemDiscount," +
                    " (SELECT ISNULL(Sum(CASE WHEN taxval =0 THEN (val1*exchange_price) ELSE 0 END),0) from Inv_Sub where InvGlobalID=Inv.InvGlobalID) as FreeVATSales," +
                    " Customers.name as CustName," +
                    " Users.username as username," +
                    " Branches.name as BranchName" +
                    " from Inv" +
                    " left join Customers on inv.cust_id=Customers.id" +
                    " left join Users on inv.sales_emp=Users.emp" +
                    " left join Branches on inv.branch=Branches.id" +
                    " where " + whereCondition + " Inv.IS_Deleted=0 order by date desc";

                InvoicesList = new InvoiceOper().BindingListOfInvoices1(sqlstr, false, dateFrom, dateTo);

                if (InvoicesList == null)
                {
                    InvoicesList = new List<InvoiceTxt>();
                }

                GridView2.ItemsSource = InvoicesList;
                UpdateRowCount();
                GridView2_CustomSummaryCalculate();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            finally
            {
                ProgressBar1.IsIndeterminate = false;
                ProgressBar1.Visibility = Visibility.Collapsed;
            }
        }

        private string BuildWhereCondition()
        {
            string whereCondition = "";

            if (Clienttype == 1)
            {
                if (chkAllTypes.IsChecked == true)
                {
                    whereCondition += " (inv_type=2 or inv_type=3) and ";
                }
                else if (chkSale.IsChecked == true)
                {
                    whereCondition += " inv_type=2 and proc_type=1 and ";
                }
                else if (ChkReturn.IsChecked == true)
                {
                    whereCondition += " inv_type=2 and proc_type=2 and ";
                }
                else if (chkPOS.IsChecked == true)
                {
                    whereCondition += " inv_type=3 and proc_type=1 and ";
                }
                else if (ChkPOSRet.IsChecked == true)
                {
                    whereCondition += " inv_type=3 and proc_type=2 and ";
                }
            }
            else if (Clienttype == 2)
            {
                if (chkAllTypes.IsChecked == true)
                {
                    whereCondition += " inv_type=1 and ";
                }
                else if (chkSale.IsChecked == true)
                {
                    whereCondition += " inv_type=1 and proc_type=1 and ";
                }
                else if (ChkReturn.IsChecked == true)
                {
                    whereCondition += " inv_type=1 and proc_type=2 and ";
                }
            }

            if (chkAllBranches.IsChecked != true && cmbBranches.SelectedValue != null)
            {
                whereCondition += " inv.branch=" + cmbBranches.SelectedValue + " and ";
            }

            if (chkAll.IsChecked != true && cmbClients.SelectedValue != null)
            {
                whereCondition += " cust_id=" + cmbClients.SelectedValue + " and ";
            }

            return whereCondition;
        }

        private string NormalizeWhereCondition(string whereCondition)
        {
            string result = whereCondition.Trim();

            while (result.EndsWith("and", StringComparison.OrdinalIgnoreCase))
            {
                result = result.Substring(0, result.Length - 3).Trim();
            }

            if (string.IsNullOrWhiteSpace(result))
            {
                result = "1=1 and ";
            }
            else
            {
                result += " and ";
            }

            return result;
        }

        private DateTime BuildDateTime(DateTime datePart, string timeText, TimeSpan defaultTime)
        {
            if (TimeSpan.TryParseExact(timeText?.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan parsedTime))
            {
                return datePart.Date.Add(parsedTime);
            }

            if (TimeSpan.TryParse(timeText?.Trim(), out parsedTime))
            {
                return datePart.Date.Add(parsedTime);
            }

            return datePart.Date.Add(defaultTime);
        }

        #endregion

        #region Summary

        private void GridView2_CustomSummaryCalculate()
        {
            nTotal = 0.0;
            nTotal1 = 0.0;
            nVAT = 0.0;
            nVAT1 = 0.0;
            nNet = 0.0;
            nNet1 = 0.0;
            sum = 0.0;
            sum1 = 0.0;
            Discount = 0.0;
            Discount1 = 0.0;
            Cash = 0.0;
            Cash1 = 0.0;
            PayNetwork = 0.0;
            PayNetwork1 = 0.0;
            _ExtraTax = 0.0;
            _ExtraTax1 = 0.0;
            _TotalTax1 = 0.0;
            _TotalTax2 = 0.0;
            _Paid1 = 0.0;
            _Paid2 = 0.0;
            _Remainder1 = 0.0;
            _Remainder2 = 0.0;

            if (InvoicesList == null || InvoicesList.Count == 0)
            {
                ClearSummaryCards();
                return;
            }

            foreach (InvoiceTxt invoice in InvoicesList)
            {
                int procType = ToIntSafe(invoice.ProcType);

                if (procType == 1)
                {
                    sum += ToDoubleSafe(invoice.SumPrice);
                    Discount += ToDoubleSafe(invoice.TotDiscount);
                    nTotal += ToDoubleSafe(invoice.Total);
                    nVAT += ToDoubleSafe(invoice.VAT);
                    _ExtraTax += ToDoubleSafe(invoice.ExtraVAT);
                    _TotalTax1 += ToDoubleSafe(invoice.TotalTax);
                    nNet += ToDoubleSafe(invoice.Net);
                    Cash += ToDoubleSafe(invoice.Paycash);
                    PayNetwork += ToDoubleSafe(invoice.PayATM);
                    _Paid1 += ToDoubleSafe(invoice.Paid);
                    _Remainder1 += ToDoubleSafe(invoice.Remainder);
                }
                else
                {
                    sum1 += ToDoubleSafe(invoice.SumPrice);
                    Discount1 += ToDoubleSafe(invoice.TotDiscount);
                    nTotal1 += ToDoubleSafe(invoice.Total);
                    nVAT1 += ToDoubleSafe(invoice.VAT);
                    _ExtraTax1 += ToDoubleSafe(invoice.ExtraVAT);
                    _TotalTax2 += ToDoubleSafe(invoice.TotalTax);
                    nNet1 += ToDoubleSafe(invoice.Net);
                    Cash1 += ToDoubleSafe(invoice.Paycash);
                    PayNetwork1 += ToDoubleSafe(invoice.PayATM);
                    _Paid2 += ToDoubleSafe(invoice.Paid);
                    _Remainder2 += ToDoubleSafe(invoice.Remainder);
                }
            }

            txtSummarySumPrice.Text = FormatNumber(sum - sum1);
            txtSummaryDiscount.Text = FormatNumber(Discount - Discount1);
            txtSummaryTotal.Text = FormatNumber(nTotal - nTotal1);
            txtSummaryVAT.Text = FormatNumber(nVAT - nVAT1);
            txtSummaryExtraVAT.Text = FormatNumber(_ExtraTax - _ExtraTax1);
            txtSummaryTotalTax.Text = FormatNumber(_TotalTax1 - _TotalTax2);
            txtSummaryNet.Text = FormatNumber(nNet - nNet1);
            txtSummaryCash.Text = FormatNumber(Cash - Cash1);
            txtSummaryATM.Text = FormatNumber(PayNetwork - PayNetwork1);
            txtSummaryPaid.Text = FormatNumber(_Paid1 - _Paid2);
            txtSummaryRemainder.Text = FormatNumber(_Remainder1 - _Remainder2);
        }

        private void ClearSummaryCards()
        {
            txtSummarySumPrice.Text = FormatNumber(0);
            txtSummaryDiscount.Text = FormatNumber(0);
            txtSummaryTotal.Text = FormatNumber(0);
            txtSummaryVAT.Text = FormatNumber(0);
            txtSummaryExtraVAT.Text = FormatNumber(0);
            txtSummaryTotalTax.Text = FormatNumber(0);
            txtSummaryNet.Text = FormatNumber(0);
            txtSummaryCash.Text = FormatNumber(0);
            txtSummaryATM.Text = FormatNumber(0);
            txtSummaryPaid.Text = FormatNumber(0);
            txtSummaryRemainder.Text = FormatNumber(0);
        }

        private void UpdateRowCount()
        {
            int count = InvoicesList == null ? 0 : InvoicesList.Count;
            lblRowCount.Text = "عدد السجلات: " + count.ToString("N0");
        }

        #endregion

        #region Grid Buttons

        private void RepositoryItemBtnDetails_ButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Button clickedButton = sender as Button;
                if (clickedButton == null)
                {
                    return;
                }

                InvoiceTxt currentInvoice = clickedButton.Tag as InvoiceTxt;
                if (currentInvoice == null)
                {
                    return;
                }

                frmInvoiceDetails frmInvoiceDetails2 = new frmInvoiceDetails();
                MainClass.ApplyPermissionToForm(frmInvoiceDetails2);
                MainClass.DoApplyUserSett(frmInvoiceDetails2);
                frmInvoiceDetails2.btnRecalculateCost.Visibility = Visibility.Collapsed;
                frmInvoiceDetails2.ISProfit = false;
                frmInvoiceDetails2.InvGlobalId = currentInvoice.InvGlobalID == null ? "" : currentInvoice.InvGlobalID.ToString();
                frmInvoiceDetails2.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void RepositoryItemBtnShowInv_ButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Button clickedButton = sender as Button;
                if (clickedButton == null)
                {
                    return;
                }

                InvoiceTxt currentInvoice = clickedButton.Tag as InvoiceTxt;
                if (currentInvoice == null)
                {
                    return;
                }

                string invGlobalId = currentInvoice.InvGlobalID == null ? "" : currentInvoice.InvGlobalID.ToString();
                int procType = ToIntSafe(currentInvoice.ProcType);
                int invoiceType = ToIntSafe(currentInvoice.InvoiceType);

                if (invoiceType == 2)
                {
                    frmInvSale frmInvSale2 = new frmInvSale();
                    if (procType == 2)
                    {
                        frmInvSale2.Title = "مرتجع مبيعات";
                    }

                    frmInvSale2.Show();
                    frmInvSale2.InvType = 2;
                    frmInvSale2.ProcType = procType;
                    frmInvSale2.WindowState = WindowState.Maximized;
                    frmInvSale2.Navigate("select * from Inv where IS_Deleted=0 And inv_type=2 And proc_type=" + procType + " And InvGlobalID=N'" + invGlobalId + "'");
                    frmInvSale2.Activate();
                }
                else if (invoiceType == 3 || invoiceType == 20)
                {
                    frmInvPOS frmInvPOS2 = new frmInvPOS();
                    if (procType == 2)
                    {
                        frmInvPOS2.Title = "مرتجع";
                    }

                    frmInvPOS2.Show();
                    frmInvPOS2.ProcType = procType;
                    frmInvPOS2.WindowState = WindowState.Maximized;
                    frmInvPOS2.Navigate("select * from Inv where IS_Deleted=0 and (inv_type=3 or inv_type=20) and proc_type=" + procType + " and InvGlobalID=N'" + invGlobalId + "'");
                    frmInvPOS2.Activate();
                }
                else if (invoiceType == 1)
                {
                    frmInvPurch frmInvPurch2 = new frmInvPurch();
                    if (procType == 2)
                    {
                        frmInvPurch2.Title = "مرتجع مشتريات";
                    }

                    frmInvPurch2.Show();
                    frmInvPurch2.InvType = 1;
                    frmInvPurch2.ProcType = procType;
                    frmInvPurch2.WindowState = WindowState.Maximized;
                    frmInvPurch2.Navigate("select * from Inv where IS_Deleted=0 and inv_type=1 and proc_type=" + procType + " and InvGlobalID=N'" + invGlobalId + "'");
                    frmInvPurch2.Activate();
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void RepositoryItemBtnDetails_ButtonClick_1(object sender, RoutedEventArgs e)
        {
            RepositoryItemBtnDetails_ButtonClick(sender, e);
        }

        private void RepositoryItemBtnShowInv_ButtonClick_1(object sender, RoutedEventArgs e)
        {
            RepositoryItemBtnShowInv_ButtonClick(sender, e);
        }

        #endregion

        #region Print / Preview

        private void PrintDevexpress(int type)
        {
            try
            {
                RptUrl = MainClass.ReportsPath;

                if (Clienttype == 1)
                {
                    RptName = "rptInvSumByClient.repx";
                }
                else if (Clienttype == 2)
                {
                    RptName = "rptInvSumBySupplier.repx";
                }

                defPrinter = MainClass.ReportsPrinter;

                if (InvoicesList == null || InvoicesList.Count == 0)
                {
                    System.Windows.MessageBox.Show("لا توجد عمليات بالجدول", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                FlowDocument document = BuildPrintDocument();

                if (type == 2)
                {
                    ShowPreviewWindow(document);
                }
                else
                {
                    PrintDialog printDialog = new PrintDialog();
                    bool? result = printDialog.ShowDialog();

                    if (result == true)
                    {
                        document.PageHeight = printDialog.PrintableAreaHeight;
                        document.PageWidth = printDialog.PrintableAreaWidth;
                        document.PagePadding = new Thickness(25);
                        document.ColumnGap = 0;
                        document.ColumnWidth = printDialog.PrintableAreaWidth;

                        IDocumentPaginatorSource idpSource = document;
                        printDialog.PrintDocument(idpSource.DocumentPaginator, Title);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private FlowDocument BuildPrintDocument()
        {
            FlowDocument document = new FlowDocument
            {
                FlowDirection = System.Windows.FlowDirection.RightToLeft,
                FontFamily = new FontFamily("Tahoma"),
                FontSize = 11,
                PagePadding = new Thickness(20),
                ColumnWidth = 1200
            };

            Paragraph titleParagraph = new Paragraph(new Run(Title))
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#213A7A"))
            };
            document.Blocks.Add(titleParagraph);

            string periodText = chkAllPeriod.IsChecked == true
                ? "كل الفترة"
                : "من " + (txtDateFrom.SelectedDate.HasValue ? txtDateFrom.SelectedDate.Value.ToShortDateString() : "")
                  + " " + txtStartTime.Text
                  + " إلى " + (txtDateTo.SelectedDate.HasValue ? txtDateTo.SelectedDate.Value.ToShortDateString() : "")
                  + " " + txtEndTime.Text;

            Paragraph infoParagraph = new Paragraph(new Run("📅 الفترة: " + periodText))
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            document.Blocks.Add(infoParagraph);

            Table table = new Table
            {
                CellSpacing = 0
            };

            for (int i = 0; i < 12; i++)
            {
                table.Columns.Add(new TableColumn());
            }

            TableRowGroup rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            TableRow headerRow = new TableRow();
            rowGroup.Rows.Add(headerRow);

            AddHeaderCell(headerRow, "م");
            AddHeaderCell(headerRow, "نوع الفاتورة");
            AddHeaderCell(headerRow, "رقم الفاتورة");
            AddHeaderCell(headerRow, "المرجع");
            AddHeaderCell(headerRow, "التاريخ");
            AddHeaderCell(headerRow, "الوقت");
            AddHeaderCell(headerRow, "العميل");
            AddHeaderCell(headerRow, "نوع الدفع");
            AddHeaderCell(headerRow, "الإجمالي");
            AddHeaderCell(headerRow, "الضريبة");
            AddHeaderCell(headerRow, "الصافي");
            AddHeaderCell(headerRow, "الفرع");

            foreach (InvoiceTxt item in InvoicesList)
            {
                TableRow row = new TableRow();
                rowGroup.Rows.Add(row);

                AddBodyCell(row, SafeText(item.AutoIncrementID));
                AddBodyCell(row, SafeText(item.InvoiceTypeTxt));
                AddBodyCell(row, SafeText(item.InvoiceNo));
                AddBodyCell(row, SafeText(item.ReffNo));
                AddBodyCell(row, SafeText(item.InvDate));
                AddBodyCell(row, SafeText(item.InvoiceTime));
                AddBodyCell(row, SafeText(item.ClientTxt));
                AddBodyCell(row, SafeText(item.PaymentTxt));
                AddBodyCell(row, FormatNumber(ToDoubleSafe(item.Total)));
                AddBodyCell(row, FormatNumber(ToDoubleSafe(item.VAT)));
                AddBodyCell(row, FormatNumber(ToDoubleSafe(item.Net)));
                AddBodyCell(row, SafeText(item.BranchTxt));
            }

            document.Blocks.Add(table);

            Paragraph totalsParagraph = new Paragraph
            {
                Margin = new Thickness(0, 12, 0, 0),
                FontWeight = FontWeights.Bold
            };

            totalsParagraph.Inlines.Add(new Run("💵 المجموع: " + txtSummarySumPrice.Text + "    "));
            totalsParagraph.Inlines.Add(new Run("🏷️ الخصم: " + txtSummaryDiscount.Text + "    "));
            totalsParagraph.Inlines.Add(new Run("🧾 الإجمالي: " + txtSummaryTotal.Text + "    "));
            totalsParagraph.Inlines.Add(new Run("💰 الضريبة: " + txtSummaryVAT.Text + "    "));
            totalsParagraph.Inlines.Add(new Run("✅ الصافي: " + txtSummaryNet.Text));

            document.Blocks.Add(totalsParagraph);

            return document;
        }

        private void ShowPreviewWindow(FlowDocument document)
        {
            Window previewWindow = new Window
            {
                Title = "معاينة قبل الطباعة",
                Width = 1100,
                Height = 750,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                FlowDirection = System.Windows.FlowDirection.RightToLeft,
                Background = Brushes.White
            };

            DocumentViewer documentViewer = new DocumentViewer
            {
                Document = document
            };

            previewWindow.Content = documentViewer;
            previewWindow.ShowDialog();
        }

        private void AddHeaderCell(TableRow row, string text)
        {
            TableCell cell = new TableCell(new Paragraph(new Run(text)))
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDE7FF")),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(5)
            };

            cell.Blocks.FirstBlock.SetValue(TextElement.FontWeightProperty, FontWeights.Bold);
            cell.Blocks.FirstBlock.SetValue(Block.TextAlignmentProperty, TextAlignment.Center);

            row.Cells.Add(cell);
        }

        private void AddBodyCell(TableRow row, string text)
        {
            TableCell cell = new TableCell(new Paragraph(new Run(text)))
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(4)
            };

            cell.Blocks.FirstBlock.SetValue(Block.TextAlignmentProperty, TextAlignment.Center);

            row.Cells.Add(cell);
        }

        #endregion

        #region Export

        private void ExportToCsv(string fileName)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("م,نوع الفاتورة,رقم الفاتورة,رقم المرجع,تاريخ الفاتورة,الوقت,العميل,نوع الدفع,المدفوع,نقدي,شبكة,المجموع,الخصم,الإجمالي,الضريبة,الضريبة الإضافية,إجمالي الضريبة,الصافي,الباقي,المستودع,الفرع,المندوب,المستخدم");

            foreach (InvoiceTxt item in InvoicesList)
            {
                builder.AppendLine(
                    CsvValue(SafeText(item.AutoIncrementID)) + "," +
                    CsvValue(SafeText(item.InvoiceTypeTxt)) + "," +
                    CsvValue(SafeText(item.InvoiceNo)) + "," +
                    CsvValue(SafeText(item.ReffNo)) + "," +
                    CsvValue(SafeText(item.InvDate)) + "," +
                    CsvValue(SafeText(item.InvoiceTime)) + "," +
                    CsvValue(SafeText(item.ClientTxt)) + "," +
                    CsvValue(SafeText(item.PaymentTxt)) + "," +
                    CsvValue(FormatNumber(ToDoubleSafe(item.Paid))) + "," +
                    CsvValue(FormatNumber(ToDoubleSafe(item.Paycash))) + "," +
                    CsvValue(FormatNumber(ToDoubleSafe(item.PayATM))) + "," +
                    CsvValue(FormatNumber(ToDoubleSafe(item.SumPrice))) + "," +
                    CsvValue(FormatNumber(ToDoubleSafe(item.TotDiscount))) + "," +
                    CsvValue(FormatNumber(ToDoubleSafe(item.Total))) + "," +
                    CsvValue(FormatNumber(ToDoubleSafe(item.VAT))) + "," +
                    CsvValue(FormatNumber(ToDoubleSafe(item.ExtraVAT))) + "," +
                    CsvValue(FormatNumber(ToDoubleSafe(item.TotalTax))) + "," +
                    CsvValue(FormatNumber(ToDoubleSafe(item.Net))) + "," +
                    CsvValue(FormatNumber(ToDoubleSafe(item.Remainder))) + "," +
                    CsvValue(SafeText(item.InvertoryName)) + "," +
                    CsvValue(SafeText(item.BranchTxt)) + "," +
                    CsvValue(SafeText(item.SalesmanTxt)) + "," +
                    CsvValue(SafeText(item.UserTxt))
                );
            }

            File.WriteAllText(fileName, builder.ToString(), Encoding.UTF8);
        }

        private void ExportToExcelHtml(string fileName)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("<html>");
            builder.AppendLine("<head>");
            builder.AppendLine("<meta http-equiv='Content-Type' content='text/html; charset=utf-8'/>");
            builder.AppendLine("<style>");
            builder.AppendLine("table{border-collapse:collapse;font-family:Tahoma;font-size:12px;}");
            builder.AppendLine("th,td{border:1px solid #999;padding:5px;text-align:center;}");
            builder.AppendLine("th{background:#DDE7FF;font-weight:bold;}");
            builder.AppendLine("</style>");
            builder.AppendLine("</head>");
            builder.AppendLine("<body dir='rtl'>");
            builder.AppendLine("<h2 style='text-align:center;'>" + Title + "</h2>");
            builder.AppendLine("<table>");
            builder.AppendLine("<tr>");
            builder.AppendLine("<th>م</th>");
            builder.AppendLine("<th>نوع الفاتورة</th>");
            builder.AppendLine("<th>رقم الفاتورة</th>");
            builder.AppendLine("<th>رقم المرجع</th>");
            builder.AppendLine("<th>تاريخ الفاتورة</th>");
            builder.AppendLine("<th>الوقت</th>");
            builder.AppendLine("<th>العميل</th>");
            builder.AppendLine("<th>نوع الدفع</th>");
            builder.AppendLine("<th>المدفوع</th>");
            builder.AppendLine("<th>نقدي</th>");
            builder.AppendLine("<th>شبكة</th>");
            builder.AppendLine("<th>المجموع</th>");
            builder.AppendLine("<th>الخصم</th>");
            builder.AppendLine("<th>الإجمالي</th>");
            builder.AppendLine("<th>الضريبة</th>");
            builder.AppendLine("<th>الضريبة الإضافية</th>");
            builder.AppendLine("<th>إجمالي الضريبة</th>");
            builder.AppendLine("<th>الصافي</th>");
            builder.AppendLine("<th>الباقي</th>");
            builder.AppendLine("<th>المستودع</th>");
            builder.AppendLine("<th>الفرع</th>");
            builder.AppendLine("<th>المندوب</th>");
            builder.AppendLine("<th>المستخدم</th>");
            builder.AppendLine("</tr>");

            foreach (InvoiceTxt item in InvoicesList)
            {
                builder.AppendLine("<tr>");
                builder.AppendLine("<td>" + HtmlValue(SafeText(item.AutoIncrementID)) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(SafeText(item.InvoiceTypeTxt)) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(SafeText(item.InvoiceNo)) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(SafeText(item.ReffNo)) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(SafeText(item.InvDate)) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(SafeText(item.InvoiceTime)) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(SafeText(item.ClientTxt)) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(SafeText(item.PaymentTxt)) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(FormatNumber(ToDoubleSafe(item.Paid))) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(FormatNumber(ToDoubleSafe(item.Paycash))) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(FormatNumber(ToDoubleSafe(item.PayATM))) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(FormatNumber(ToDoubleSafe(item.SumPrice))) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(FormatNumber(ToDoubleSafe(item.TotDiscount))) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(FormatNumber(ToDoubleSafe(item.Total))) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(FormatNumber(ToDoubleSafe(item.VAT))) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(FormatNumber(ToDoubleSafe(item.ExtraVAT))) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(FormatNumber(ToDoubleSafe(item.TotalTax))) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(FormatNumber(ToDoubleSafe(item.Net))) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(FormatNumber(ToDoubleSafe(item.Remainder))) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(SafeText(item.InvertoryName)) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(SafeText(item.BranchTxt)) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(SafeText(item.SalesmanTxt)) + "</td>");
                builder.AppendLine("<td>" + HtmlValue(SafeText(item.UserTxt)) + "</td>");
                builder.AppendLine("</tr>");
            }

            builder.AppendLine("</table>");
            builder.AppendLine("<br/>");
            builder.AppendLine("<table>");
            builder.AppendLine("<tr><th>المجموع</th><td>" + HtmlValue(txtSummarySumPrice.Text) + "</td></tr>");
            builder.AppendLine("<tr><th>الخصم</th><td>" + HtmlValue(txtSummaryDiscount.Text) + "</td></tr>");
            builder.AppendLine("<tr><th>الإجمالي</th><td>" + HtmlValue(txtSummaryTotal.Text) + "</td></tr>");
            builder.AppendLine("<tr><th>الضريبة</th><td>" + HtmlValue(txtSummaryVAT.Text) + "</td></tr>");
            builder.AppendLine("<tr><th>الضريبة الإضافية</th><td>" + HtmlValue(txtSummaryExtraVAT.Text) + "</td></tr>");
            builder.AppendLine("<tr><th>إجمالي الضريبة</th><td>" + HtmlValue(txtSummaryTotalTax.Text) + "</td></tr>");
            builder.AppendLine("<tr><th>الصافي</th><td>" + HtmlValue(txtSummaryNet.Text) + "</td></tr>");
            builder.AppendLine("<tr><th>نقدي</th><td>" + HtmlValue(txtSummaryCash.Text) + "</td></tr>");
            builder.AppendLine("<tr><th>شبكة</th><td>" + HtmlValue(txtSummaryATM.Text) + "</td></tr>");
            builder.AppendLine("<tr><th>المدفوع</th><td>" + HtmlValue(txtSummaryPaid.Text) + "</td></tr>");
            builder.AppendLine("<tr><th>الباقي</th><td>" + HtmlValue(txtSummaryRemainder.Text) + "</td></tr>");
            builder.AppendLine("</table>");
            builder.AppendLine("</body>");
            builder.AppendLine("</html>");

            File.WriteAllText(fileName, builder.ToString(), Encoding.UTF8);
        }

        #endregion

        #region Helpers

        private string GetEmpName(int emp)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter("select name from Employees where id=" + emp, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    return dt.Rows[0][0].ToString();
                }

                return "";
            }
            catch
            {
                return "";
            }
        }

        private int ToIntSafe(object value)
        {
            try
            {
                if (value == null || value == DBNull.Value)
                    return 0;

                int result;
                if (int.TryParse(value.ToString(), out result))
                    return result;

                return Convert.ToInt32(value);
            }
            catch
            {
                return 0;
            }
        }

        private double ToDoubleSafe(object value)
        {
            try
            {
                if (value == null || value == DBNull.Value)
                    return 0.0;

                double result;
                if (double.TryParse(value.ToString(), out result))
                    return result;

                return Convert.ToDouble(value);
            }
            catch
            {
                return 0.0;
            }
        }

        private string FormatNumber(double value)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(Common.DigitsNo))
                {
                    return value.ToString(Common.DigitsNo);
                }
            }
            catch
            {
            }

            return value.ToString("N2");
        }

        private string SafeText(object value)
        {
            return value == null || value == DBNull.Value ? "" : value.ToString();
        }

        private string CsvValue(string text)
        {
            if (text == null)
            {
                return "\"\"";
            }

            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        private string HtmlValue(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        private void ShowError(Exception ex)
        {
            System.Windows.MessageBox.Show(
                "خطأ" + Environment.NewLine + "تفاصيل الخطأ: " + ex.Message,
                "خطأ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        #endregion
    }
}