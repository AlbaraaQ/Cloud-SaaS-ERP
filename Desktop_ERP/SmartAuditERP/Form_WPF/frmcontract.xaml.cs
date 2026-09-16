using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AuditorAPI.Models;
using AuditorAPI.Models.DGVmodels;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;
using log4net;
using Brushes = System.Windows.Media.Brushes;
using ComboBox = System.Windows.Controls.ComboBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using MessageBoxOptions = System.Windows.MessageBoxOptions;
using TextBox = System.Windows.Controls.TextBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmcontract : ThemedWindow
    {
        #region ── Inner Classes ──

        public class RowInfo
        {
            public int RowHandle;

            public RowInfo(int rowHandle)
            {
                RowHandle = rowHandle;
            }
        }

        #endregion

        #region ── Private Fields ──

        private SqlConnection conn;
        private SqlConnection conn1;
        private Print print;
        private InvoiceObj InvObj;
        private Invoicecontract Invoic;
        private InvoiceDGV InvoicCheck;
        private string StyleFile;
        private string StyleFolder;
        private bool EnableE_Invoice;
        private bool allowEditInvoice;
        private bool _UpdateQty;
        private DispatcherTimer _clockTimer;
        private bool _isLoading;

        private static readonly ILog Logger =
            LogManager.GetLogger(Environment.MachineName);

        #endregion

        #region ── Public Fields ──

        public int EntryType;
        public int InvType;
        public int ProcType;
        public int InvType_credit;

        #endregion

        #region ── Constructor ──

        public frmcontract()
        {
            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            InvType = 23;
            print = new Print(InvType);
            InvObj = new InvoiceObj(InvType, ProcType);
            Invoic = new Invoicecontract();
            InvoicCheck = new InvoiceDGV();
            StyleFile = Path.Combine(MainClass.ReportsPath,
                               "Styles\\contractInvoSaveLayoutToXML.xml");
            StyleFolder = Path.Combine(MainClass.ReportsPath, "Styles");
            EnableE_Invoice = true;
            allowEditInvoice = true;
            _UpdateQty = false;

            InitializeComponent();

            this.Loaded += frmcontract_Load;
            this.Closing += frmcontract_Closing;
            this.KeyDown += frmcontract_KeyDown;
        }

        #endregion

        #region ── Window Events ──

        private void frmcontract_Load(object sender, RoutedEventArgs e)
        {
            if (CheckBeforeClear()) return;

            CLR();
            LoadSettings();
            LoadData1();

            InvObj = new InvoiceObj(InvType, ProcType);

            txtDate.SelectedDate = DateTime.Now;
            txtInvTime.Text = DateTime.Now.ToString("hh:mm:ss tt");
            txtRefDate.SelectedDate = DateTime.Now;
            txtRefDate.DisplayDateEnd = DateTime.Now;

            if (cmbStore.Items.Count > 0)
                cmbStore.SelectedIndex = 0;

            txtRefDate.IsEnabled = true;
            txtRefNo.IsEnabled = true;

            if (InvObj.ProcessFlow)
            {
                cmbInvoiceStatus.Visibility = Visibility.Visible;
                txtInvoStatus.Visibility = Visibility.Visible;
                cmbInvoiceStatus.SelectedIndex = 0;
            }

            LoadFoundation();
            ConfigurePermissions();
            ResetInvoice();
            LoadDefaultPayType();
            StartClock();

            SetupGridEvents();
            SetupButtonEvents();
            SetupComboBoxEvents();
            SetupTextBoxEvents();
        }

        private void frmcontract_Closing(object sender,
            System.ComponentModel.CancelEventArgs e)
        {
            _clockTimer?.Stop();
        }

        private void frmcontract_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5: btnNew_Click(null, null); break;
                case Key.F6: btnSave_Click(null, null); break;
                case Key.F7: btnPrint_Click(null, null); break;
                case Key.F8: btnView_Click(null, null); break;
                case Key.F9: btnDelete_Click(null, null); break;
                case Key.Escape: this.Close(); break;
            }
        }

        #endregion

        #region ── Event Wiring ──

        private void SetupGridEvents()
        {
            var tableView = (TableView)GridControl1.View;
            tableView.CellValueChanged += GridView1_CellValueChanged;
            tableView.ShowingEditor += GridView1_ShowingEditor;
            tableView.ShowGridMenu += GridView1_ShowGridMenu;
        }

        private void SetupButtonEvents()
        {
            btnNew.Click += btnNew_Click;
            btnSave.Click += btnSave_Click;
            btnSavePrint.Click += btnSavePrint_Click;
            btnPrint.Click += btnPrint_Click;
            btnView.Click += btnView_Click;
            btnDelete.Click += btnDelete_Click;
            btnClose.Click += btnClose_Click;
            btnFirst.Click += btnFirst_Click;
            btnPrevious.Click += btnPrevious_Click;
            btnNext.Click += btnNext_Click;
            btnLast.Click += btnLast_Click;
            btnInvSrch.Click += btnInvSrch_Click;
            btnCustAdd.Click += btnCustAdd_Click;
            btnZoom.Click += btnZoom_Click;
            cmbAddSalesMen.Click += cmbAddSalesMen_Click;
            btnAddCostCenter.Click += btnAddCostCenter_Click;
            btnInsertLinkedInv.Click += btnInsertLinkedInv_Click;
            BtnReceipts.Click += BtnReceipts_Click;

            stripImport.Click += stripImport_Click;
            BTnShowEntry.Click += BTnShowEntry_Click;
            BtnSaveDgvSettings.Click += BtnSaveDgvSettings_Click;
            BtnDefaultSetting.Click += BtnDefaultSetting_Click;
            OpenReportDesgin.Click += OpenReportDesgin_Click;
            ExportPDF.Click += ExportPDF_Click;

            ToolStripDropDownButton1.Click += (s, ev) =>
            {
                ToolStrip2.IsOpen = true;
            };
        }

        private void SetupComboBoxEvents()
        {
            cmbPayType.SelectionChanged += cmbPayType_SelectionChanged;
            cmbClient.SelectionChanged += cmbClient_SelectionChanged;
            cmbTreasury.SelectionChanged += cmbTreasury_SelectionChanged;
            cmbBanks.SelectionChanged += cmbBanks_SelectionChanged;
            cmbSalesMen.SelectionChanged += cmbSalesMen_SelectionChanged;
            cmbCostCenter.SelectionChanged += cmbCostCenter_SelectionChanged;
            cmbStore.SelectionChanged += cmbStore_SelectionChanged;
        }

        private void SetupTextBoxEvents()
        {
            txtNote.TextChanged += txtNote_TextChanged;
            txtCashCust.TextChanged += txtCashCust_TextChanged;
            txtCashCustMobile.TextChanged += txtCashCustMobile_TextChanged;
            txtInvDiscVal.KeyDown += txtInvDiscVal_KeyDown;
            txtInvDiscPerc.KeyDown += txtInvDiscPerc_KeyDown;
            txtInvDiscPerc.LostFocus += txtInvDiscPerc_LostFocus;
            txtWorkdiscPerc.KeyDown += txtWorkdiscPerc_KeyDown;
            txtWorkdiscPerc.LostFocus += txtWorkdiscPerc_LostFocus;
            txtcontract_total_value.KeyDown += txtcontract_total_value_KeyDown;
            txtpreviously_paid_amount.KeyDown += txtpreviously_paid_amount_KeyDown;
        }

        #endregion

        #region ── Clock ──

        private void StartClock()
        {
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += (s, e) =>
                lblCurrentTime.Text = $"🕐 {DateTime.Now:hh:mm:ss tt}";
            _clockTimer.Start();
        }

        #endregion

        #region ── Initialization & Settings ──

        private void LoadSettings()
        {
            LoadDGvSetting();
        }

        private void ConfigurePermissions()
        {
            if (!EnableE_Invoice)
            {
                btnDelete.Visibility = Visibility.Visible;
                btnDelete.IsEnabled = true;
                txtDate.IsEnabled = true;
                txtInvTime.IsEnabled = true;
            }
            else
            {
                btnDelete.Visibility = Visibility.Collapsed;
                btnDelete.IsEnabled = false;

                if (MainSetting.ZatcaIntegerationActive)
                {
                    txtDate.IsEnabled = false;
                    txtInvTime.IsEnabled = false;
                }
                else if (User.EditInvDate)
                {
                    txtDate.IsEnabled = true;
                    txtInvTime.IsEnabled = true;
                }
                else
                {
                    txtDate.IsEnabled = false;
                    txtInvTime.IsEnabled = false;
                }
            }
        }

        private void LoadFoundation()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT EnableE_Invoice FROM Foundation", conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0 &&
                    dataTable.Rows[0]["EnableE_Invoice"] != DBNull.Value)
                    EnableE_Invoice = Convert.ToBoolean(
                        dataTable.Rows[0]["EnableE_Invoice"]);
                else
                    EnableE_Invoice = true;
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        private void LoadDGvSetting()
        {
            try
            {
                if (User.EditPrice)
                {
                    var col = GridControl1.Columns["ItemPrice"];
                    if (col != null)
                        col.AllowEditing = DevExpress.Utils.DefaultBoolean.True;
                }

                if (File.Exists(StyleFile))
                    GridControl1.RestoreLayoutFromXml(StyleFile);

                SetColumnFormats();
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        private void SetColumnFormats()
        {
            string digitsFormat = InvObj.DigitsNo ?? "N2";
            string formatStr = "{0:" + digitsFormat + "}";

            string[] numericColumns =
            {
                "ItemVat", "ItemVatPerc", "ItemQuantity", "ItemPrice",
                "ItemDiscount", "ItemDiscountPerc", "ItemSumPrice",
                "ItemCost", "ItemNetPrice", "ItemTotalPrice",
                "ItemPriceWithoutVAT", "WithholdingTax", "WithholdingTaxPerc",
                "ItemPriceDiscount", "ItemPriceAfterDiscount", "Additionsitem"
            };

            foreach (string fieldName in numericColumns)
            {
                var column = GridControl1.Columns[fieldName];
                if (column != null)
                {
                    column.EditSettings =
                        new DevExpress.Xpf.Editors.Settings.TextEditSettings
                        {
                            DisplayFormat = formatStr,
                            MaskType = DevExpress.Xpf.Editors.MaskType.Numeric,
                            Mask = digitsFormat
                        };
                }
            }
        }

        #endregion

        #region ── Data Loading ──

        private void LoadData1()
        {
            LoadSafes();
            LoadCustomers();
            LoadTreasury();
            LoadSalesMen();
            LoadBanks();
            LoadCostCenters();
        }

        public void LoadSafes()
        {
            try
            {
                var dataSource = LoadData.Invertories(MainClass.EmpNo);
                cmbStore.ItemsSource = dataSource.DefaultView;
                cmbStore.DisplayMemberPath = "name";
                cmbStore.SelectedValuePath = "id";
                cmbStore.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        private void LoadCostCenters()
        {
            try
            {
                var dataTable = LoadData.CostCenters();
                cmbCostCenter.ItemsSource = dataTable.DefaultView;
                cmbCostCenter.DisplayMemberPath = "name";
                cmbCostCenter.SelectedValuePath = "code";
                cmbCostCenter.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        private void LoadBanks()
        {
            try
            {
                var dataTable = LoadData.Banks();
                cmbBanks.ItemsSource = dataTable.DefaultView;
                cmbBanks.DisplayMemberPath = "name";
                cmbBanks.SelectedValuePath = "id";
                cmbBanks.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        private void LoadCustomers()
        {
            try
            {
                int customerType = 1;
                if (InvType == 1)
                {
                    customerType = 2;
                    lblClient.Text = (MainClass.Language == "ar") ? "المورد" : "Supplier";
                }

                var dataSource = LoadData.Customers(customerType);
                cmbClient.ItemsSource = dataSource.DefaultView;
                cmbClient.DisplayMemberPath = "name";
                cmbClient.SelectedValuePath = "id";
                cmbClient.SelectedIndex = -1;

                if (cmbClient.Items.Count > 0)
                    cmbClient.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        public void LoadTreasury()
        {
            try
            {
                var dataSource = LoadData.Treasury();
                cmbTreasury.ItemsSource = dataSource.DefaultView;
                cmbTreasury.DisplayMemberPath = "name";
                cmbTreasury.SelectedValuePath = "id";
                cmbTreasury.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        public void LoadSalesMen()
        {
            try
            {
                var dataSource = LoadData.SalesMen();
                cmbSalesMen.ItemsSource = dataSource.DefaultView;
                cmbSalesMen.DisplayMemberPath = "name";
                cmbSalesMen.SelectedValuePath = "id";
                cmbSalesMen.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region ── Clear & Reset ──

        public static void CLRForm(DependencyObject parent)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(parent))
            {
                if (child is TextBox tb)
                    tb.Text = "0";
                else if (child is ComboBox cb)
                    cb.SelectedIndex = -1;
                else if (child is DatePicker dp)
                    dp.SelectedDate = DateTime.Now;

                if (child is DependencyObject dep)
                    CLRForm(dep);
            }
        }

        private void CLR()
        {
            _isLoading = true;
            try
            {
                CLRForm(this);

                if (cmbStore.Items.Count > 0) cmbStore.SelectedIndex = 0;

                txtDate.SelectedDate = DateTime.Now;
                txtRefDate.SelectedDate = DateTime.Now;
                txtInvTime.Text = DateTime.Now.ToString("hh:mm:ss tt");

                txtcontract_total_value.Text = "0";
                txtoriginal_work_amount.Text = "0";
                txtspecial_discount.Text = "0";
                txtamount_after_discount.Text = "0";
                txtvat_amount.Text = "0";
                txttotal_with_vat.Text = "0";
                txtwork_guarantee.Text = "0";
                txtnet_due_this_payment.Text = "0";
                txtpreviously_paid_amount.Text = "0";
                txtremaining_contract_balance.Text = "0";

                cmbPayType.SelectedIndex = 1;
                cmbInvoiceStatus.SelectedIndex = 0;
                cmbTreasury.IsEnabled = true;
                cmbCostCenter.SelectedIndex = -1;

                if (cmbClient.Items.Count > 0) cmbClient.SelectedIndex = 0;
                if (cmbTreasury.Items.Count > 0) cmbTreasury.SelectedIndex = 0;

                lblPaidStatus.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
            finally
            {
                _isLoading = false;
            }

            Invoic = null;
            GridControl1.ItemsSource = null;
            Invoic = new Invoicecontract();

            ResetInvoice();
            txtRefDate.IsEnabled = true;
            txtRefNo.IsEnabled = true;
            LoadDefaultPayType();
        }

        private void ResetInvoice()
        {
            LoadInvNo();

            Invoic.InvoiceType = InvObj.InvType;
            Invoic.ProcType = ProcType;
            Invoic.Currency = InvObj.Currency;
            Invoic.InvoiceCode = InvObj.InvoiceCode;
            Invoic.VATperc = InvObj.VAT;
            Invoic.AdditionalCost = 0m;
            Invoic.PaymentStatus = PaymentStatus.Paid;
            Invoic.Branch = MainClass.BranchNo;
            Invoic.InvertoryImpact = InvObj.InvertoryImpact;

            if (Invoic.InvoiceType == (InvoiceType)21 && Invoic.ProcType == 2)
                Invoic.InvertoryImpact = 1;
            else if (Invoic.InvoiceType == (InvoiceType)21 && Invoic.ProcType == 1)
                Invoic.InvertoryImpact = 2;

            Invoic.ExtraVATPerc = InvObj.AdditionalTax;
            Invoic.PriceIncVAT = InvObj.PriceIncVAT;
            Invoic.InvAccCode = "4100001";
            Invoic.Store = SafeGetSelectedValue<int>(cmbStore);
            Invoic.InvertoryName = cmbStore.Text;
            Invoic.Treasury = SafeGetSelectedValue<int>(cmbTreasury);

            if (InvObj.InvDefualtCust != 0)
            {
                Invoic.Customer = InvObj.InvDefualtCust;
                cmbClient.SelectedValue = InvObj.InvDefualtCust;
            }
            else
            {
                Invoic.Customer = SafeGetSelectedValue<int>(cmbClient);
            }

            Invoic.InvCCcode = "-1";
            Invoic.CreateDate = DateTime.Now;
            Invoic.InvDate = txtDate.SelectedDate ?? DateTime.Now;
            Invoic.InvTime = DateTime.Now;
            Invoic.RefDate = txtRefDate.SelectedDate ?? DateTime.Now;
            Invoic.PayType = 1;
            Invoic.User = MainClass.EmpNo;
            Invoic.ISNew = true;
            Invoic.IsDeleted = false;
            Invoic.IsLoaded = false;
            Invoic.IsPrinted = false;
            Invoic.ReffNo = "-1";
            Invoic.TotalWithholdingTax = 0.0;
            Invoic.InvoiceStatus = InvObj.ProcessFlow ? 2 : 3;
            Invoic.Currency = InvObj.Currency;
            Invoic.Pricing = InvObj.Pricing;

            Invoic.contract_total_value = 0.0;
            Invoic.original_work_amount = 0.0;
            Invoic.special_discount = 0.0;
            Invoic.amount_after_discount = 0.0;
            Invoic.vat_amount = 0.0;
            Invoic.total_with_vat = 0.0;
            Invoic.work_guarantee = 0.0;
            Invoic.net_due_this_payment = 0.0;
            Invoic.previously_paid_amount = 0.0;
            Invoic.remaining_contract_balance = 0.0;

            GridControl1.ItemsSource = Invoic.InvoiceItems;
            InvoicCheck = new InvoiceDGV();
            InvoiceOper.filePathDocument = null;
        }

        private void LoadInvNo()
        {
            Invoic.InvoiceNo = GetInvoiceNo(InvType, ProcType, InvObj.Prefixe);
            txtInvCode.Text = $"{MainClass.BranchCode}C{ProcType}";
            Invoic.InvCombinedId = txtInvCode.Text.Trim() + Invoic.InvoiceNo;
            txtNo.Text = Invoic.InvoiceNo.ToString();
            txtNo.Foreground = Brushes.Firebrick;
        }

        public static int GetInvoiceNo(int invType, int procType, int prefixe)
        {
            try
            {
                using var sqlConn = MainClass.ConnObj();
                if (sqlConn.State != ConnectionState.Open) sqlConn.Open();

                var cmd = new SqlCommand(
                    $"SELECT ISNULL(MAX(id), 0) FROM InvContratct " +
                    $"WHERE branch={MainClass.BranchNo} " +
                    $"AND inv_type={invType} AND proc_type={procType}",
                    sqlConn);

                int invoiceNumber = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
                if (invoiceNumber == 1) invoiceNumber = prefixe;
                return invoiceNumber;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return prefixe;
            }
        }

        #endregion

        #region ── Default Pay Type ──

        public void LoadDefaultPayType()
        {
            try
            {
                switch (InvObj.PayTypeDefault)
                {
                    case -1:
                        cmbPayType.SelectedIndex = 0;
                        cmbTreasury.SelectedIndex = -1;
                        if (Invoic.ISNew)
                        {
                            cmbClient.SelectedIndex = -1;
                            cmbClient.Focus();
                            Invoic.Customer = -1;
                        }
                        Invoic.PayType = -1;
                        Invoic.Treasury = -1;
                        Invoic.PaymentStatus = PaymentStatus.Paid;
                        break;

                    case 1:
                        cmbPayType.SelectedIndex = 1;
                        if (cmbTreasury.Items.Count > 0)
                            cmbTreasury.SelectedIndex = 0;
                        cmbBanks.SelectedIndex = -1;
                        Invoic.PayType = 1;
                        Invoic.Treasury = SafeGetSelectedValue<int>(cmbTreasury);
                        Invoic.Customer = SafeGetSelectedValue<int>(cmbClient);
                        break;

                    case 2:
                        cmbPayType.SelectedIndex = 2;
                        if (cmbBanks.Items.Count > 0)
                            cmbBanks.SelectedIndex = 0;
                        cmbTreasury.SelectedIndex = -1;
                        Invoic.PayType = 2;
                        Invoic.Treasury = -1;
                        Invoic.Bank = 1;
                        Invoic.Customer = SafeGetSelectedValue<int>(cmbClient);
                        break;

                    default:
                        cmbPayType.SelectedIndex = 1;
                        if (cmbTreasury.Items.Count > 0)
                            cmbTreasury.SelectedIndex = 0;
                        cmbBanks.SelectedIndex = -1;
                        Invoic.PayType = 1;
                        Invoic.Treasury = SafeGetSelectedValue<int>(cmbTreasury);
                        Invoic.Customer = SafeGetSelectedValue<int>(cmbClient);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region ── ComboBox Events ──

        private void cmbPayType_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (_isLoading || Invoic == null) return;

            switch (cmbPayType.SelectedIndex)
            {
                case 0:
                    cmbTreasury.SelectedIndex = -1;
                    if (Invoic.ISNew)
                    {
                        cmbClient.SelectedIndex = -1;
                        cmbClient.Focus();
                        Invoic.Customer = -1;
                    }
                    Invoic.PayType = -1;
                    Invoic.Treasury = -1;
                    Invoic.PaymentStatus = PaymentStatus.PostPaid;
                    break;

                case 1:
                    try
                    {
                        if (cmbTreasury.Items.Count > 0)
                            cmbTreasury.SelectedIndex = 0;
                        Invoic.PayType = 1;
                        Invoic.Treasury = SafeGetSelectedValue<int>(cmbTreasury);
                        Invoic.Customer = SafeGetSelectedValue<int>(cmbClient);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
                    }
                    break;

                case 2:
                    cmbBanks.SelectedIndex = -1;
                    cmbTreasury.SelectedIndex = -1;
                    Invoic.PayType = 2;
                    Invoic.Treasury = -1;
                    Invoic.Customer = SafeGetSelectedValue<int>(cmbClient);
                    break;
            }
        }

        private void cmbClient_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (_isLoading || Invoic == null) return;
            try
            {
                Invoic.Customer = cmbClient.SelectedIndex > -1
                    ? SafeGetSelectedValue<int>(cmbClient) : -1;
                ShowCustBalance();
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        private void cmbTreasury_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (Invoic == null) return;
            Invoic.Treasury = cmbTreasury.SelectedIndex > -1
                ? SafeGetSelectedValue<int>(cmbTreasury) : -1;
        }

        private void cmbBanks_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (Invoic == null) return;
            Invoic.Bank = cmbBanks.SelectedIndex > -1
                ? SafeGetSelectedValue<int>(cmbBanks) : -1;
        }

        private void cmbSalesMen_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (Invoic == null) return;
            Invoic.Saleman = cmbSalesMen.SelectedIndex > -1
                ? SafeGetSelectedValue<int>(cmbSalesMen) : -1;
        }

        private void cmbCostCenter_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (Invoic == null) return;
            Invoic.InvCCcode = cmbCostCenter.SelectedIndex > -1
                ? cmbCostCenter.SelectedValue?.ToString() ?? "-1" : "-1";
        }

        private void cmbStore_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (Invoic == null) return;
            if (cmbStore.SelectedIndex > -1)
            {
                Invoic.Store = SafeGetSelectedValue<int>(cmbStore);
                Invoic.InvertoryName = Common.GetStoreName(Invoic.Store);
            }
            else
            {
                Invoic.Store = -1;
            }
        }

        #endregion

        #region ── Customer Balance ──

        private void ShowCustBalance()
        {
            try
            {
                if (string.IsNullOrEmpty(cmbClient.Text)) return;
                var (debitTotal, creditTotal) =
                    CalculateAccountBalance(cmbClient.Text);
                txtBalance.Text = FormatBalanceText(debitTotal, creditTotal);
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        private void ShowCustBalancePreviews()
        {
            try
            {
                var (debitTotal, creditTotal) =
                    CalculateAccountBalance(cmbClient.Text);
                txtBalancePreviews.Text = FormatBalanceText(debitTotal, creditTotal);
                Invoic.BalancePreviews =
                    (float)SafeParseDouble(txtBalancePreviews.Text);
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        private (double debitTotal, double creditTotal)
            CalculateAccountBalance(string clientName)
        {
            double debitTotal = 0.0;
            double creditTotal = 0.0;

            var adapterAcc = new SqlDataAdapter(
                $"SELECT Code FROM Accounts_Index " +
                $"WHERE AName=N'{clientName}' {Accounting.BranchCondition}",
                conn1);
            var dtAcc = new DataTable();
            adapterAcc.Fill(dtAcc);
            if (dtAcc.Rows.Count == 0) return (0, 0);

            string accountFilter = $" AND Entry_sub.acc_no={dtAcc.Rows[0][0]}";
            string branchFilter = MainClass.BranchNo != -1
                ? $"Entry.branch={MainClass.BranchNo} " +
                  $"AND Entry_sub.branch={MainClass.BranchNo} AND "
                : string.Empty;

            var adapterEntries = new SqlDataAdapter(
                $"SELECT Entry.id, Entry.date, " +
                $"SUM(Entry_sub.dept) AS dept, " +
                $"SUM(Entry_sub.credit) AS credit, Entry.notes " +
                $"FROM Entry, Entry_sub " +
                $"WHERE {branchFilter} Entry.IS_Deleted=0 " +
                $"AND Entry.state=1 " +
                $"AND Entry.GlobalId=Entry_sub.EntryGlobalId {accountFilter} " +
                $"GROUP BY Entry.id, Entry.date, Entry.notes, Entry_sub.acc_no",
                conn1);

            var dtEntries = new DataTable();
            adapterEntries.Fill(dtEntries);

            for (int i = 0; i < dtEntries.Rows.Count; i++)
            {
                debitTotal += Convert.ToDouble(dtEntries.Rows[i]["dept"]);
                creditTotal += Convert.ToDouble(dtEntries.Rows[i]["credit"]);
            }

            return (debitTotal, creditTotal);
        }

        private static string FormatBalanceText(double debitTotal,
            double creditTotal)
        {
            if (debitTotal > creditTotal)
                return $"{Math.Round(debitTotal - creditTotal, 3):0.#,##.##}";
            if (creditTotal > debitTotal)
                return $"{Math.Round(creditTotal - debitTotal, 3):0.#,##.##}  دائن";
            return "0";
        }

        #endregion

        #region ── TextBox Events ──

        private void txtNote_TextChanged(object sender,
            TextChangedEventArgs e)
        {
            if (Invoic != null) Invoic.InvNote = txtNote.Text;
        }

        private void txtCashCust_TextChanged(object sender,
            TextChangedEventArgs e)
        {
            if (Invoic != null) Invoic.CashCustomerName = txtCashCust.Text;
        }

        private void txtCashCustMobile_TextChanged(object sender,
            TextChangedEventArgs e)
        {
            if (Invoic != null)
                Invoic.CashCustomerMobile = txtCashCustMobile.Text.Trim();
        }

        private void txtInvDiscVal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) CalcDiscount();
        }

        private void txtInvDiscPerc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) CalcDiscountPerc();
        }

        private void txtInvDiscPerc_LostFocus(object sender,
            RoutedEventArgs e)
        {
            try { CalcDiscountPerc(); }
            catch (Exception ex)
            { Logger.Error($"{Title} {ex.Message} {MainClass.UserName}"); }
        }

        private void txtWorkdiscPerc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) CalcDiscountPercWork();
        }

        private void txtWorkdiscPerc_LostFocus(object sender,
            RoutedEventArgs e)
        {
            try { CalcDiscountPercWork(); }
            catch (Exception ex)
            { Logger.Error($"{Title} {ex.Message} {MainClass.UserName}"); }
        }

        private void txtcontract_total_value_KeyDown(object sender,
            KeyEventArgs e)
        {
            if (e.Key != Key.Return) return;
            try
            {
                Invoic.contract_total_value =
                    SafeParseDouble(txtcontract_total_value.Text);
                CalcDiscountPercWork();
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, "خطأ");
            }
        }

        private void txtpreviously_paid_amount_KeyDown(object sender,
            KeyEventArgs e)
        {
            if (e.Key != Key.Return) return;
            Invoic.previously_paid_amount =
                SafeParseDouble(txtpreviously_paid_amount.Text);
            CalcDiscountPercWork();
        }

        #endregion

        #region ── Calculation Logic ──

        private void CalcDiscount()
        {
            string discText = txtInvDiscVal.Text.Trim();
            if (!string.IsNullOrEmpty(discText))
            {
                double discountValue = SafeParseDouble(discText);
                Invoic.InvDiscount = discountValue;
                Invoic.special_discount = discountValue;
            }
            else
            {
                Invoic.InvDiscount = 0.0;
                Invoic.special_discount = 0.0;
                txtInvDiscPerc.Text = "0";
            }

            ItemOpercontract.CalcRowscontratct(ref Invoic);
            BindingDGV();
        }

        private void CalcDiscountPerc()
        {
            string percText = txtInvDiscPerc.Text.Trim();
            if (!string.IsNullOrEmpty(percText))
            {
                double percValue = SafeParseDouble(percText);
                double discountValue = Convert.ToDouble(
                    InvoiceOper.GetInvDiscountByPercentage(
                        Invoic.SumPrice, percValue));
                Invoic.InvDiscount = discountValue;
                Invoic.special_discount = discountValue;
            }
            else
            {
                txtInvDiscPerc.Text = "0";
                Invoic.InvDiscount = 0.0;
                Invoic.special_discount = 0.0;
            }

            ItemOpercontract.CalcRowscontratct(ref Invoic);
            BindingDGV();
        }

        private void CalcDiscountPercWork()
        {
            try
            {
                if (string.IsNullOrEmpty(txtWorkdiscPerc.Text.Trim()) ||
                    string.IsNullOrEmpty(txtcontract_total_value.Text.Trim()))
                    return;

                decimal contractTotal = new decimal(Invoic.contract_total_value);
                decimal workDiscPercVal =
                    (decimal)SafeParseDouble(txtWorkdiscPerc.Text);
                decimal guarantee = contractTotal * workDiscPercVal / 100m;

                Invoic.work_guarantee =
                    Convert.ToDouble(guarantee.ToString("0.00"));

                decimal netDue = (decimal)Invoic.total_with_vat - guarantee;
                Invoic.net_due_this_payment =
                    Convert.ToDouble(netDue.ToString("0.00"));

                Invoic.remaining_contract_balance =
                    Convert.ToDouble(contractTotal) -
                    Invoic.previously_paid_amount -
                    Invoic.net_due_this_payment;

                BindingDGV();
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region ── Grid Binding ──

        private void BindingDGV()
        {
            if (_isLoading) return;
            _isLoading = true;
            try
            {
                string fmt = InvObj.DigitsNo ?? "N2";

                txtTirmsNo.Text = (Invoic.InvoiceItems?.Count ?? 0).ToString();
                txtTotQuan.Text = Invoic.TotalQty.ToString(fmt);
                txtSumVal.Text = Invoic.SumPrice.ToString(fmt);
                txtInvDiscVal.Text = Invoic.InvDiscount.ToString(fmt);
                txtInvDiscPerc.Text = InvoiceOper
                    .GetDiscountPercentage(Invoic.SumPrice, Invoic.InvDiscount)
                    .ToString(fmt);
                txtNetWithoutVAT.Text = Invoic.Total.ToString(fmt);
                txtTotVAT.Text = Invoic.VAT.ToString(fmt);
                txtNet.Text = Invoic.Net.ToString(fmt);
                txtInvRemainder.Text = Invoic.Remainder.ToString(fmt);
                txtInvPaid.Text = Invoic.Paid.ToString(fmt);
                txtBalancePreviews.Text = Invoic.BalancePreviews.ToString(fmt);

                txtnet_due_this_payment.Text =
                    Invoic.net_due_this_payment.ToString(fmt);
                txtoriginal_work_amount.Text =
                    Invoic.original_work_amount.ToString(fmt);
                txtTotDiscount.Text = Invoic.TotDiscount.ToString(fmt);
                txtspecial_discount.Text =
                    Invoic.special_discount.ToString(fmt);
                txtamount_after_discount.Text =
                    Invoic.amount_after_discount.ToString(fmt);
                txtvat_amount.Text = Invoic.VAT.ToString(fmt);
                txttotal_with_vat.Text = Invoic.Net.ToString(fmt);
                txtremaining_contract_balance.Text =
                    Invoic.remaining_contract_balance.ToString(fmt);
                txtpreviously_paid_amount.Text =
                    Invoic.previously_paid_amount.ToString(fmt);
                txtwork_guarantee.Text =
                    Invoic.work_guarantee.ToString(fmt);
                txtcontract_total_value.Text =
                    Invoic.contract_total_value.ToString(fmt);

                GridControl1.RefreshData();
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void BindingControls()
        {
            _isLoading = true;
            try
            {
                txtNo.Text = Invoic.InvoiceNo.ToString();
                txtDate.SelectedDate = Invoic.InvDate;
                txtInvTime.Text =
                    Invoic.InvTime.ToString("hh:mm:ss tt");
                txtRefDate.SelectedDate = Invoic.RefDate;
                txtRefNo.Text = Invoic.ReffNo;
                txtNote.Text = Invoic.InvNote;
                txtBalancePreviews.Text =
                    Invoic.BalancePreviews.ToString();
                txtCashCust.Text = Invoic.CashCustomerName;
                txtCashCustMobile.Text = Invoic.CashCustomerMobile;

                // حالة الدفع
                if (!Invoic.ISNew && InvObj.PaymentStatus && ProcType == 1)
                {
                    lblPaidStatus.Visibility = Visibility.Visible;
                    if (Invoic.PaymentStatus == PaymentStatus.Unpaid ||
                        Invoic.PaymentStatus == PaymentStatus.PaidPartially)
                    {
                        lblPaidStatus.Foreground = Brushes.Red;
                        lblPaidStatus.Text = (MainClass.Language == "ar")
                            ? "⚠️ غير مكتملة الدفع" : "Unpaid Invoice";
                    }
                    else if (Invoic.PaymentStatus == PaymentStatus.Paid)
                    {
                        lblPaidStatus.Text = "✅ مدفوعة";
                        lblPaidStatus.Foreground = Brushes.LightGreen;
                    }
                }
                else
                {
                    lblPaidStatus.Visibility = Visibility.Collapsed;
                }

                // طريقة الدفع
                if (Invoic.PayType == -1) cmbPayType.SelectedIndex = 0;
                else if (Invoic.PayType == 1) cmbPayType.SelectedIndex = 1;
                else if (Invoic.PayType == 2)
                {
                    cmbPayType.SelectedIndex = 2;
                    cmbBanks.SelectedValue = Invoic.Bank;
                }

                // مركز التكلفة
                if (double.TryParse(Invoic.InvCCcode, out double ccVal) &&
                    ccVal > -1)
                    cmbCostCenter.SelectedValue = Invoic.InvCCcode;

                cmbTreasury.SelectedValue = Invoic.Treasury;
                cmbStore.SelectedValue = Invoic.Store;
                cmbClient.SelectedValue = Invoic.Customer;

                try { cmbSalesMen.SelectedValue = Invoic.Saleman; }
                catch (Exception ex)
                { Logger.Error($"{Title} {ex.Message} {MainClass.UserName}"); }

                cmbInvoiceStatus.SelectedIndex = Invoic.InvoiceStatus;
                txtNo.Background = Brushes.WhiteSmoke;
            }
            finally
            {
                _isLoading = false;
            }
        }

        #endregion

        #region ── Grid Cell Value Changed ──

        private void GridView1_CellValueChanged(object sender,
            CellValueChangedEventArgs e)
        {
            if (e.RowHandle < 0 || Invoic == null) return;

            string fieldName = e.Column.FieldName;
            int currentRowIndex = e.RowHandle + 1;

            if (fieldName == "ItemQuantity" ||
                fieldName == "ItemPrice" ||
                fieldName == "Additionsitem")
            {
                ItemOpercontract.CalcRowscontratct(ref Invoic);
                BindingDGV();
            }
            else if (fieldName == "ItemDiscount")
            {
                int itemId = GetGridCellInt(e.RowHandle, "ItemId");
                double discountVal = GetGridCellDouble(e.RowHandle, "ItemDiscount");
                double sumPrice = GetGridCellDouble(e.RowHandle, "ItemSumPrice");
                double quantity = GetGridCellDouble(e.RowHandle, "ItemQuantity");
                double discountPerc = Convert.ToDouble(
                    InvoiceOper.GetDiscountPercentage(sumPrice, discountVal)
                    .ToString(InvObj.DigitsNo));
                double priceDiscount = quantity > 0 ? discountVal / quantity : 0;

                foreach (var item in Invoic.InvoiceItems)
                {
                    if (item.ItemId == itemId && item.ItemRowIndex == currentRowIndex)
                    {
                        item.ItemDiscountPerc = discountPerc;
                        item.ItemPriceDiscount = priceDiscount;
                    }
                }
                ItemOpercontract.CalcRowscontratct(ref Invoic);
                BindingDGV();
            }
            else if (fieldName == "ItemDiscountPerc")
            {
                int itemId = GetGridCellInt(e.RowHandle, "ItemId");
                double percValue = GetGridCellDouble(e.RowHandle, "ItemDiscountPerc");
                double sumPrice = GetGridCellDouble(e.RowHandle, "ItemSumPrice");
                double quantity = GetGridCellDouble(e.RowHandle, "ItemQuantity");
                double discountCalc = percValue / 100.0 * sumPrice;
                double priceDiscount = quantity > 0 ? discountCalc / quantity : 0;

                foreach (var item in Invoic.InvoiceItems)
                {
                    if (item.ItemId == itemId && item.ItemRowIndex == currentRowIndex)
                    {
                        item.ItemDiscount = discountCalc;
                        item.ItemPriceDiscount = priceDiscount;
                    }
                }
                ItemOpercontract.CalcRowscontratct(ref Invoic);
                BindingDGV();
            }
            else if (fieldName == "ItemSumPrice")
            {
                int itemId = GetGridCellInt(e.RowHandle, "ItemId");
                double totalPrice = GetGridCellDouble(e.RowHandle, "ItemSumPrice");
                double quantity = GetGridCellDouble(e.RowHandle, "ItemQuantity");
                double unitPrice = quantity > 0 ? totalPrice / quantity : 0;

                foreach (var item in Invoic.InvoiceItems)
                {
                    if (item.ItemId == itemId && item.ItemRowIndex == currentRowIndex)
                        item.ItemPrice = unitPrice;
                }
                ItemOpercontract.CalcRowscontratct(ref Invoic);
                BindingDGV();
            }
            else if (fieldName == "ItemPriceDiscount")
            {
                int itemId = GetGridCellInt(e.RowHandle, "ItemId");
                double priceDisc = GetGridCellDouble(e.RowHandle, "ItemPriceDiscount");
                double quantity = GetGridCellDouble(e.RowHandle, "ItemQuantity");
                double totalDisc = priceDisc * quantity;
                double sumPrice = GetGridCellDouble(e.RowHandle, "ItemSumPrice");
                double discPerc = Convert.ToDouble(
                    InvoiceOper.GetDiscountPercentage(sumPrice, totalDisc)
                    .ToString(InvObj.DigitsNo));

                foreach (var item in Invoic.InvoiceItems)
                {
                    if (item.ItemId == itemId && item.ItemRowIndex == currentRowIndex)
                    {
                        item.ItemDiscount = totalDisc;
                        item.ItemDiscountPerc = discPerc;
                    }
                }
                ItemOpercontract.CalcRowscontratct(ref Invoic);
                BindingDGV();
            }
            else if (fieldName == "ItemExpireDate")
            {
                int itemId = GetGridCellInt(e.RowHandle, "ItemId");
                var expVal = GridControl1.GetCellValue(e.RowHandle, "ItemExpireDate");

                if (expVal != null && itemId > 0)
                {
                    foreach (var item in Invoic.InvoiceItems)
                    {
                        if (item.ItemId == itemId &&
                            item.ItemRowIndex == currentRowIndex)
                        {
                            if (DateTime.TryParse(expVal.ToString(),
                                out DateTime expDate))
                                item.ItemExpireDate = expDate;
                        }
                    }
                }
            }
        }

        private void GridView1_ShowingEditor(object sender,
            ShowingEditorEventArgs e)
        {
            // يمكن إضافة منطق إضافي عند بدء التحرير
        }

        #endregion

        #region ── Grid Context Menu ──

        private void GridView1_ShowGridMenu(object sender,
            GridMenuEventArgs e)
        {
            if (e.MenuType != GridMenuType.RowCell) return;

            var contextMenu = new ContextMenu();
            contextMenu.FlowDirection = System.Windows.FlowDirection.RightToLeft;

            AddMenuItem(contextMenu, "🔍 استعراض المادة",
                (s, ev) => ShowItemCard());
            AddMenuItem(contextMenu, "➕ إضافة مادة",
                (s, ev) => AddNewProduct());

            contextMenu.Items.Add(new Separator());

            AddMenuItem(contextMenu, "📏 الوحدات",
                (s, ev) => ShowItemUnits());
            AddMenuItem(contextMenu, "🏭 مخزون الصنف",
                (s, ev) => ShowItemInvertory());

            contextMenu.Items.Add(new Separator());

            AddMenuItem(contextMenu, "🗑️ حذف سجل",
                (s, ev) => DeleteGridRow());

            contextMenu.Items.Add(new Separator());

            AddMenuItem(contextMenu, "🔄 حركة المادة",
                (s, ev) => ItemProcess());
            AddMenuItem(contextMenu, "📋 آخر حركة للمادة",
                (s, ev) => ItemLastActivity());
            AddMenuItem(contextMenu, "👤 آخر حركة للعميل",
                (s, ev) => ItemClientActivity());
            AddMenuItem(contextMenu, "➕ إضافة مجموعة",
                (s, ev) => AddCategory());

            contextMenu.Items.Add(new Separator());

            AddMenuItem(contextMenu, "🔢 الرقم التسلسلي",
                (s, ev) => TemSerialNo());
            AddMenuItem(contextMenu, "💰 تكلفة المادة",
                (s, ev) => ItemCost1());

            contextMenu.IsOpen = true;
            e.Handled = true;
        }

        private static void AddMenuItem(ContextMenu menu, string header,
            RoutedEventHandler handler)
        {
            var item = new MenuItem { Header = header };
            item.Click += handler;
            menu.Items.Add(item);
        }

        #endregion

        #region ── Item Card & Product Actions ──

        private void ShowItemCard()
        {
            if (!User.EditItemInfo)
            {
                ShowMessage("لا يوجد لديك صلاحية لهذه العملية");
                return;
            }
            int focusedRow =
                ((TableView)GridControl1.View).FocusedRowHandle;
            if (focusedRow < 0) return;

            var form = new frmItems();
            MainClass.ApplyPermissionToForm(form);
            MainClass.DoApplyUserSett(form);
            form.ItemId = GetGridCellInt(focusedRow, "ItemId");
            form.ShowDialog();
        }

        private void AddNewProduct()
        {
            if (!User.EditItemInfo)
            {
                ShowMessage("لا يوجد لديك صلاحية لهذه العملية");
                return;
            }
            var form = new frmItems();
            MainClass.ApplyPermissionToForm(form);
            MainClass.DoApplyUserSett(form);
            form.ShowDialog();
        }

        private void AddCategory()
        {
            var form = new frmItemCategory();
            MainClass.ApplyPermissionToForm(form);
            MainClass.DoApplyUserSett(form);
            form.ShowDialog();
        }

        #endregion

        #region ── Item Units & Inventory ──

        private void ShowItemUnit(int itemId, int rowIndex)
        {
            try
            {
                foreach (InvoiceItem invoiceItem in Invoic.InvoiceItems)
                {
                    if (invoiceItem.ItemId != itemId ||
                        invoiceItem.ItemRowIndex != rowIndex)
                        continue;

                    var form = new frmItemUnits
                    {
                        ItemId = invoiceItem.ItemId,
                        PreUnit = invoiceItem.UnitID
                    };
                    form.ShowDialog();

                    if (!string.IsNullOrEmpty(form.Unitname))
                    {
                        invoiceItem.UnitID = form.UnitId;
                        invoiceItem.UnitName = form.Unitname;

                        InvoiceItem tempItem = invoiceItem;
                        ItemOpercontract.LoadUnitInf(ref tempItem,
                            (int)Invoic.InvoiceType, Invoic.Pricing);

                        ItemOpercontract.CalcRowscontratct(ref Invoic);
                        BindingDGV();
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                ShowMessage(
                    $"خطأ أثناء تحميل الوحدات\nتفاصيل الخطأ: {ex.Message}",
                    "خطأ", MessageBoxImage.Error);
            }
        }

        private void ShowItemInvertories(int itemId, int rowIndex)
        {
            try
            {
                foreach (InvoiceItem invoiceItem in Invoic.InvoiceItems)
                {
                    if (invoiceItem.ItemId != itemId ||
                        invoiceItem.ItemRowIndex != rowIndex)
                        continue;

                    var form = new frmItemInvertory
                    {
                        ItemId = invoiceItem.ItemId
                    };
                    form.ShowDialog();

                    if (form.InvertoryId > 0)
                    {
                        invoiceItem.InvertoryId = form.InvertoryId;
                        invoiceItem.InvertoryName = form.InvertoryName;
                        invoiceItem.ValiableInvertory =
                            Convert.ToDouble(form.ItemQty);

                        ItemOpercontract.CalcRowscontratct(ref Invoic);
                        BindingDGV();
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                ShowMessage(
                    $"خطأ أثناء تحميل المستودع\nتفاصيل الخطأ: {ex.Message}",
                    "خطأ", MessageBoxImage.Error);
            }
        }

        private void ShowItemUnits()
        {
            int focusedRow =
                ((TableView)GridControl1.View).FocusedRowHandle;
            if (focusedRow < 0) return;
            ShowItemUnit(GetGridCellInt(focusedRow, "ItemId"),
                         focusedRow + 1);
        }

        private void ShowItemInvertory()
        {
            int focusedRow =
                ((TableView)GridControl1.View).FocusedRowHandle;
            if (focusedRow < 0) return;
            ShowItemInvertories(GetGridCellInt(focusedRow, "ItemId"),
                                focusedRow + 1);
        }

        #endregion

        #region ── Item Activity & Reports ──

        private void ItemProcess()
        {
            int focusedRow =
                ((TableView)GridControl1.View).FocusedRowHandle;
            if (focusedRow < 0) return;

            var form = new frmRptItemsActivityDetailed
            {
                SelectedId = GetGridCellInt(focusedRow, "ItemId")
            };
            form.txtItemName.Text =
                GridControl1.GetCellValue(focusedRow, "ItemName")?.ToString();
            form.txtItemCode.Text =
                GridControl1.GetCellValue(focusedRow, "ItemCode")?.ToString();
            form.ShowResult();
            MainClass.ApplyPermissionToForm(form);
            MainClass.DoApplyUserSett(form);
            form.ShowDialog();
        }

        private void ItemLastActivity()
        {
            int focusedRow =
                ((TableView)GridControl1.View).FocusedRowHandle;
            if (focusedRow < 0) return;

            var form = new frmRptItemsActivityDetailed();
            MainClass.ApplyPermissionToForm(form);
            MainClass.DoApplyUserSett(form);
            form.txtItemName.Text =
                GridControl1.GetCellValue(focusedRow, "ItemName")?.ToString();
            form.txtItemCode.Text =
                GridControl1.GetCellValue(focusedRow, "ItemCode")?.ToString();
            form.Activate();
            form.Show();
            form.LastProcess(
                GetGridCellInt(focusedRow, "ItemId"),
                GetGridCellInt(focusedRow, "InvertoryId"),
                -1, 1);
        }

        private void ItemClientActivity()
        {
            int focusedRow =
                ((TableView)GridControl1.View).FocusedRowHandle;
            if (focusedRow < 0) return;

            var form = new frmRptItemsActivityDetailed();
            MainClass.ApplyPermissionToForm(form);
            MainClass.DoApplyUserSett(form);
            form.txtItemName.Text =
                GridControl1.GetCellValue(focusedRow, "ItemName")?.ToString();
            form.txtItemCode.Text =
                GridControl1.GetCellValue(focusedRow, "ItemCode")?.ToString();
            form.Activate();
            form.Show();

            if (cmbClient.SelectedIndex > -1)
                form.LastProcess(
                    GetGridCellInt(focusedRow, "ItemId"),
                    GetGridCellInt(focusedRow, "InvertoryId"),
                    SafeGetSelectedValue<int>(cmbClient), 1);
        }

        private void ItemCost1()
        {
            int focusedRow =
                ((TableView)GridControl1.View).FocusedRowHandle;
            if (focusedRow < 0) return;

            if (User.ShowCosts)
            {
                var avgCost = ItemOpercontract.AvgCost(
                    GetGridCellInt(focusedRow, "ItemId"),
                    MainClass.BranchNo);
                ShowMessage(avgCost.ToString(), "تكلفة المادة");
            }
            else
            {
                ShowMessage("لا يوجد لديك صلاحية هذه العملية");
            }
        }

        #endregion

        #region ── Serial Number ──

        private void TemSerialNo()
        {
            int focusedRow =
                ((TableView)GridControl1.View).FocusedRowHandle;
            if (focusedRow < 0) return;

            try
            {
                var form = new frmItemSerialNo();
                int targetItemId = GetGridCellInt(focusedRow, "ItemId");

                foreach (InvoiceItem invoiceItem in Invoic.InvoiceItems)
                {
                    if (invoiceItem.ItemId != targetItemId) continue;

                    foreach (InvoiceItemDetail src in
                             invoiceItem.InvoiceItemDetails)
                    {
                        form.InvItem.InvoiceItemDetails.Add(
                            new InvoiceItemDetail
                            {
                                InvGlobalID = src.InvGlobalID,
                                ItemId = src.ItemId,
                                ItemSerialNo = src.ItemSerialNo,
                                BatchNo = src.BatchNo,
                                ItemProductionDate = src.ItemProductionDate,
                                ItemExpireDate = src.ItemExpireDate,
                                ItemHeight = src.ItemHeight,
                                ItemWidth = src.ItemWidth,
                                ItemColor = src.ItemColor,
                                ItemSize = src.ItemSize,
                                ItemProperty = src.ItemProperty,
                                FillValue = src.FillValue,
                                FillRatio = src.FillRatio,
                                ItemQuantity = src.ItemQuantity
                            });
                    }

                    form.InvItem = invoiceItem;

                    if (Invoic.ISNew && invoiceItem.InvoiceItemDetails.Count > 0)
                        form.operType = 1;

                    break;
                }

                form.PanelInput.Visibility = Visibility.Collapsed;
                form.ShowDialog();

                if (!form.ISDone && Invoic.ISNew)
                    form.InvItem.InvoiceItemDetails.Clear();
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region ── Grid Row Delete ──

        private void DeleteGridRow()
        {
            int focusedRow =
                ((TableView)GridControl1.View).FocusedRowHandle;
            if (focusedRow < 0) return;

            var result = MessageBox.Show(
                "هل تريد حذف هذا السجل؟",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No,
                MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading);

            if (result != MessageBoxResult.Yes) return;

            if (focusedRow < Invoic.InvoiceItems.Count)
            {
                Invoic.InvoiceItems.RemoveAt(focusedRow);

                int rowNumber = 1;
                foreach (var item in Invoic.InvoiceItems)
                    item.ItemRowIndex = rowNumber++;

                ItemOpercontract.CalcRowscontratct(ref Invoic);
                BindingDGV();
            }
        }

        #endregion

        #region ── Item Search ──

        private void addNewItem()
        {
            var searchForm = new frmItemsSrch
            {
                sql = "SELECT id, name, nameEN, sale_price, unit " +
                          "FROM Items WHERE IS_Deleted=0 ORDER BY id",
                search = "SELECT id, name, nameEN, sale_price, unit FROM Items",
                Itemname = "",
                StoreId = SafeGetSelectedValue<int>(cmbStore)
            };

            var tableView = (TableView)GridControl1.View;
            string cellValue = tableView.ActiveEditor?.EditValue?.ToString();
            if (!string.IsNullOrEmpty(cellValue))
            {
                string focusedField = tableView.FocusedColumn?.FieldName ?? "";
                if (focusedField == "ItemCode")
                    searchForm.txtSrchCode.Text = cellValue;
                else
                    searchForm.txtSrchNm.Text = cellValue;
            }

            MainClass.ApplyPermissionToForm(searchForm);
            MainClass.DoApplyUserSett(searchForm);
            searchForm.ShowDialog();

            if (searchForm.ISDone && searchForm.ItemId > 0)
            {
                foreach (int itemId in searchForm.Itemlist)
                    SearchByID(itemId, 0, "", "");
            }
        }

        private void SearchByID(int itemId, int unitId,
            string barcode, string serialNo)
        {
            try
            {
                new ItemOpercontract().GetItemByIDcontract(
                    itemId, unitId, ref Invoic, false, barcode, serialNo);

                ItemOpercontract.CalcRowscontratct(ref Invoic);
                BindingDGV();
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        private void SearchByCode(string itemCode, int itemUnit)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT name, id FROM Items " +
                    $"WHERE IS_Deleted=0 AND Code=N'{itemCode}'", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                    SearchByID(Convert.ToInt32(dt.Rows[0]["id"]),
                               itemUnit, "", "");
                else if (!string.IsNullOrWhiteSpace(itemCode))
                    ReadBarcode(itemCode, true);
                else
                    addNewItem();
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        private void ReadBarcode(string barcode, bool isMultiText)
        {
            try
            {
                int itemId = 0, unitId = 0;
                decimal price = 0m, qty = 0m;

                ItemOpercontract.SearchForBarcode(barcode.Trim(),
                    ref itemId, ref unitId, ref price, ref qty, isMultiText);

                if (itemId > 0)
                    SearchByID(itemId, unitId, barcode.Trim(), "");
                else
                    addNewItem();
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region ── Add Buttons ──

        private void btnZoom_Click(object sender, RoutedEventArgs e)
        {
            AddNewClient();
        }

        private void btnCustAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var form = new frmCustomers();
                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);
                form.Type = (InvType == 2) ? 1 : 2;
                form.Title = (InvType == 2) ? "تعريف عميل" : "تعريف مورد";
                form.ShowDialog();
                if (form.isDone) LoadCustomers();
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        private void AddNewClient()
        {
            try
            {
                var form = new frmSrchClient();
                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);
                form.Background = Brushes.WhiteSmoke;
                form.lblName.Foreground = Brushes.Black;
                form.lblMobile.Foreground = Brushes.Black;
                form.Type = 1;

                form.txtClientName.Text =
                    (cmbClient.Text == "عميل عام") ? "" : cmbClient.Text;

                if (cmbPayType.SelectedIndex == 0)
                    form.PostponeClient = true;

                form.ShowDialog();

                if (!string.IsNullOrEmpty(form.Clientname))
                {
                    LoadCustomers();
                    cmbClient.SelectedValue = form.ClientId;
                    Invoic.Customer =
                        SafeGetSelectedValue<int>(cmbClient);
                    ShowCustBalancePreviews();
                    Invoic.BalancePreviews =
                        (float)SafeParseDouble(txtBalancePreviews.Text);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        private void cmbAddSalesMen_Click(object sender, RoutedEventArgs e)
        {
            int previousValue = SafeGetSelectedValue<int>(cmbSalesMen);
            var form = new frmSalesMen();
            form.Activate();
            form.ShowDialog();
            LoadSalesMen();
            try { cmbSalesMen.SelectedValue = previousValue; }
            catch (Exception ex)
            { Logger.Error($"{Title} {ex.Message} {MainClass.UserName}"); }
        }

        private void btnAddCostCenter_Click(object sender, RoutedEventArgs e)
        {
            object previousValue = cmbCostCenter.SelectedValue;
            var form = new frmCostCenter();
            form.Activate();
            form.ShowDialog();
            LoadCostCenters();
            try { cmbCostCenter.SelectedValue = previousValue; }
            catch (Exception ex)
            { Logger.Error($"{Title} {ex.Message} {MainClass.UserName}"); }
        }

        #endregion

        #region ── Navigation ──

        public void Navigate(string sqlQuery)
        {
            if (Invoic != null) CLR();

            InvoiceOperContract.BindingInvoice(ref Invoic, sqlQuery);

            if (Invoic != null)
            {
                GridControl1.ItemsSource = Invoic.InvoiceItems;
                GridControl1.RefreshData();
                BindingDGV();
                BindingControls();
            }
            else
            {
                Invoic = new Invoicecontract();
                CLR();
            }
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                Navigate(
                    $"SELECT TOP 1 * FROM InvContratct " +
                    $"WHERE inv_type={InvType} AND proc_type={ProcType} " +
                    $"AND IS_Deleted=0 AND branch={MainClass.BranchNo} " +
                    $"ORDER BY id ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                Navigate(
                    $"SELECT TOP 1 * FROM InvContratct " +
                    $"WHERE inv_type={InvType} AND proc_type={ProcType} " +
                    $"AND IS_Deleted=0 " +
                    $"AND id<{SafeParseDouble(txtNo.Text)} " +
                    $"AND branch={MainClass.BranchNo} ORDER BY id DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                Navigate(
                    $"SELECT TOP 1 * FROM InvContratct " +
                    $"WHERE inv_type={InvType} AND proc_type={ProcType} " +
                    $"AND IS_Deleted=0 " +
                    $"AND id>{SafeParseDouble(txtNo.Text)} " +
                    $"AND branch={MainClass.BranchNo} ORDER BY id ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                Navigate(
                    $"SELECT TOP 1 * FROM InvContratct " +
                    $"WHERE inv_type={InvType} AND proc_type={ProcType} " +
                    $"AND IS_Deleted=0 AND branch={MainClass.BranchNo} " +
                    $"ORDER BY id DESC");
        }

        private void btnInvSrch_Click(object sender, RoutedEventArgs e)
        {
            SearchOverlay.Visibility =
                SearchOverlay.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;
        }

        #endregion

        #region ── Action Buttons ──

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear()) CLR();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!Common.CheckProiedAcc(
                    txtDate.SelectedDate ?? DateTime.Now))
            {
                ShowMessage(
                    "لا يمكن أن يكون التاريخ خارج الفترة المحاسبية",
                    "خطأ", MessageBoxImage.Error);
                return;
            }
            Invoic.IsPrinted = false;
            ValidateInvoice();
        }

        private void btnSavePrint_Click(object sender, RoutedEventArgs e)
        {
            Invoic.IsPrinted = true;
            ValidateInvoice();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (Invoic.IsPrinted && !Invoic.IsUpdated)
                RptPrint(new InvoiceOperContract()
                    .MappingInvoicecontract(ref Invoic), 1);
            else
                ShowMessage("لا يمكن طباعة الفاتورة قبل الحفظ ..");
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            if (Invoic.IsPrinted && !Invoic.IsUpdated)
                RptPrint(new InvoiceOperContract()
                    .MappingInvoicecontract(ref Invoic), 2);
            else
                ShowMessage("لا يمكن معاينة الفاتورة قبل الحفظ ..");
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (Invoic.ISNew) return;

            bool canDelete = (InvType == 2 && ProcType == 4) ||
                             (InvType == 2 && !EnableE_Invoice);

            if (canDelete)
            {
                if (InvoiceOperContract.DeleteInvoice(Invoic))
                {
                    ThreadContext.Properties["EmpID"] = MainClass.EmpNo;
                    Logger.Info(
                        $"تم حذف " +
                        $"{InvoiceOper.GetInvoiceType((int)Invoic.InvoiceType, Invoic.ProcType, Invoic.PayType, 2)} " +
                        $"برقم {Invoic.InvoiceNo} " +
                        $"بواسطة المستخدم: {MainClass.UserName}");
                    CLR();
                }
            }
            else
            {
                ShowMessage("لا يمكن حذف الفاتورة",
                            "تحذير", MessageBoxImage.Warning);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnReceipts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsInvExist())
                {
                    ShowMessage("لم يتم حفظ الفاتورة");
                    return;
                }
                var form = new frmSandQ();
                form.Show();
                form.cmbClients.SelectedValue = cmbClient.SelectedValue;
                form.txtVal.Text = txtNet.Text;
                form.txtNotes.Text = txtNote.Text +
                                     "   خاصة العميل:" + cmbClient.Text;
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        private void btnInsertLinkedInv_Click(object sender, RoutedEventArgs e)
        {
            if (!(InvType == 23 && ProcType == 2)) return;

            if (MainClass.ReturnYearPreviews)
            {
                MainClass.originalConnStr = MainClass.connstr;
                MainClass.connstr =
                    $"server={MainClass.Server};" +
                    $"database={CmbYearPreviews.SelectedValue};" +
                    $"trusted_connection=true";
                conn = MainClass.ConnObj();
            }

            CLR();
            InvoiceOperContract.InsertFromInv(ref Invoic, 1);
            Invoic.ProcType = 2;
            Invoic.InvertoryImpact = 1;
            Invoic.InvNote = "";
            Invoic.PayATM = 0.0;
            Invoic.Paycash = 0.0;
            Invoic.Paid = 0.0;
            Invoic.Remainder = 0.0;
            Invoic.ReffNo = Invoic.InvCombinedId;
            Invoic.RefDate = Invoic.InvDate;
            txtRefDate.IsEnabled = false;
            txtRefNo.IsEnabled = false;
            Invoic.InvDate = DateTime.Now;
            Invoic.InvTime = DateTime.Now;
            LoadInvNo();

            double totalRowDiscount = 0.0;
            foreach (var invItem in Invoic.InvoiceItems)
            {
                invItem.InvertoryImpact = Invoic.InvertoryImpact;
                if (Invoic.InvDiscount > 0.0)
                    totalRowDiscount += ItemOper.ItemDiscountRateFromInvDiscount(
                        new decimal(Invoic.SumPrice),
                        new decimal(Invoic.InvDiscount),
                        invItem.ItemSumPrice,
                        0 - (Invoic.PriceIncVAT ? 1 : 0),
                        invItem.ItemVatPerc);
            }

            Invoic.InvDiscount = totalRowDiscount;
            Invoic.ISNew = true;

            ItemOpercontract.CalcRowscontratct(ref Invoic);
            BindingDGV();
            BindingControls();
            Invoic.User = MainClass.EmpNo;
        }

        #endregion

        #region ── Validation & Save ──

        private bool CheckBeforeClear()
        {
            if (Invoic == null) return false;

            int gridRowCount = Invoic.InvoiceItems?.Count ?? 0;
            bool hasUnsaved =
                (gridRowCount > 0 && Invoic.ISNew) || Invoic.IsUpdated;

            if (!hasUnsaved) return false;

            string msg = (MainClass.Language == "ar")
                ? "لم يتم حفظ الفاتورة, هل تريد جديد؟"
                : "You do not save the invoice";

            return MessageBox.Show(msg, "تنبيه",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes;
        }

        private bool IsInvExist()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT id FROM InvContratct " +
                    $"WHERE branch={MainClass.BranchNo} " +
                    $"AND inv_type={InvType} AND proc_type={ProcType} " +
                    $"AND id={txtNo.Text}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0;
            }
            catch { return false; }
        }

        private void ValidateInvoice()
        {
            try
            {
                // === فحص فاتورة المرتجع المرتبطة ===
                if (Invoic.ISNew && Invoic.ProcType == 2)
                {
                    if (string.IsNullOrEmpty(Invoic.ReffNo) ||
                        Invoic.ReffNo == "-1")
                    {
                        ShowMessage(
                            (MainClass.Language == "ar")
                                ? "يجب ربط الفاتورة برقم فاتورة المبيعات " +
                                  "أو أن الرقم المدخل غير صحيح"
                                : "The invoice must be linked to the sales " +
                                  "invoice number, or the number entered is incorrect",
                            "", MessageBoxImage.Warning);
                        return;
                    }

                    if (!InvoiceOper.IsHasRefrence(Invoic.ReffNo))
                    {
                        if (MessageBox.Show(
                            (MainClass.Language == "ar")
                                ? "فاتورة المبيعات خارج الفترة المحاسبية، " +
                                  "هل تريد استكمال إرجاع الفاتورة؟"
                                : "Sales invoice outside the accounting period. " +
                                  "Do you want to complete the invoice return?",
                            "", MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.No)
                            return;
                    }
                }

                // === فحص وجود أصناف ===
                if (Invoic.InvoiceItems.Count == 0)
                {
                    ShowMessage(
                        (MainClass.Language == "ar")
                            ? "لا يمكن حفظ الفاتورة بدون أصناف"
                            : "An invoice cannot be saved without items",
                        "", MessageBoxImage.Warning);
                    return;
                }

                // === فحص المندوب ===
                if (InvObj.SaleManIsRequire && Invoic.Saleman <= 0)
                {
                    ShowMessage(
                        (MainClass.Language == "ar")
                            ? "يجب اختيار المندوب"
                            : "Please select salesman",
                        "", MessageBoxImage.Warning);
                    return;
                }

                // === فحص الكميات ===
                if (Invoic.InvoiceItems.Any(item => item.ItemQuantity <= 0.0))
                {
                    ShowMessage(
                        (MainClass.Language == "ar")
                            ? "لا يمكن حفظ الفاتورة يوجد كميات " +
                              "أقل أو تساوي الصفر"
                            : "The invoice cannot be saved. " +
                              "There are quantities less than or equal to zero",
                        "", MessageBoxImage.Warning);
                    return;
                }

                // === تأكيد الحفظ ===
                if (MessageBox.Show(
                    (MainClass.Language == "ar")
                        ? "هل أنت متأكد من حفظ؟"
                        : "Are you sure to save?",
                    "", MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.No)
                    return;

                // === فحص النسخة التجريبية ===
                if (MainClass.IsTrial && Invoic.ISNew)
                {
                    var trialAdapter = new SqlDataAdapter(
                        "SELECT id FROM Entry", conn1);
                    var trialDt = new DataTable();
                    trialAdapter.Fill(trialDt);

                    if (trialDt.Rows.Count >= 20)
                    {
                        ShowMessage(
                            (MainClass.Language == "en-us")
                                ? "Sorry, You reach the maximum of entries, " +
                                  "you can purchase the app and activate it"
                                : "نأسف لقد وصلت لأقصى حد إدخال للنسخة " +
                                  "التجريبية، يمكنك شراء البرنامج وتفعيله",
                            "", MessageBoxImage.Warning);
                        Invoic.IsPrinted = false;
                        return;
                    }
                }

                if (conn1.State != ConnectionState.Open) conn1.Open();

                // === فحص الصافي ===
                if (SafeParseDouble(txtNet.Text) <= 0.0)
                {
                    ShowMessage(
                        (MainClass.Language == "ar")
                            ? "يجب أن يكون الصافي أكبر من الصفر"
                            : "The net must be greater than zero",
                        "", MessageBoxImage.Warning);
                    return;
                }

                // === فحص العميل في حالة الآجل ===
                if (cmbPayType.SelectedIndex == 0 &&
                    cmbClient.SelectedIndex == -1)
                {
                    ShowMessage(
                        (MainClass.Language == "ar")
                            ? "يجب اختيار العميل"
                            : "The client must be selected",
                        "", MessageBoxImage.Warning);
                    return;
                }

                // === تعيين قيم الدفع ===
                if (Invoic.PayType == -1)
                {
                    Invoic.Paycash = 0.0;
                    Invoic.PayATM = 0.0;
                }
                else if (Invoic.PayType == 1)
                {
                    Invoic.Paycash = Invoic.Net;
                    Invoic.PayATM = 0.0;
                    Invoic.Paid = Invoic.Net;
                }
                else if (Invoic.PayType == 2)
                {
                    Invoic.Paycash = 0.0;
                    Invoic.PayATM = Invoic.Net;
                    Invoic.Paid = Invoic.Net;
                }

                SaveAndPrint();
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        private async void SaveAndPrint()
        {
            // === فحص صلاحية التعديل ===
            if (!Invoic.ISNew && (ProcType == 1 || ProcType == 2))
            {
                if (EnableE_Invoice)
                {
                    ShowMessage(
                        "بناءً على توجيهات هيئة الزكاة والضريبة والجمارك " +
                        "يمنع تعديل أي فاتورة ضريبية صادرة من النظام");
                    return;
                }
                if (!allowEditInvoice)
                {
                    ShowMessage(
                        (MainClass.Language == "ar")
                            ? "ليس لديك صلاحية لتعديل الفاتورة"
                            : "You do not have the permission to modify the invoice");
                    return;
                }
            }

            // === فحص المندوب ===
            if (InvObj.SaleManIsRequire && Invoic.Saleman <= 0)
            {
                ShowMessage("يجب اختيار المندوب", "", MessageBoxImage.Warning);
                return;
            }

            // === فحص العميل النقدي ===
            if (InvObj.CashCustRequire &&
                string.IsNullOrWhiteSpace(Invoic.CashCustomerName))
            {
                ShowMessage("يجب إدخال عميل نقدي", "", MessageBoxImage.Warning);
                return;
            }

            // === ربط قيم العقد ===
            Invoic.contract_total_value =
                SafeParseDouble(txtcontract_total_value.Text);
            Invoic.original_work_amount =
                SafeParseDouble(txtoriginal_work_amount.Text);
            Invoic.special_discount =
                SafeParseDouble(txtspecial_discount.Text);
            Invoic.amount_after_discount =
                SafeParseDouble(txtamount_after_discount.Text);
            Invoic.vat_amount =
                SafeParseDouble(txtvat_amount.Text);
            Invoic.total_with_vat =
                SafeParseDouble(txttotal_with_vat.Text);
            Invoic.work_guarantee =
                SafeParseDouble(txtwork_guarantee.Text);
            Invoic.net_due_this_payment =
                SafeParseDouble(txtnet_due_this_payment.Text);
            Invoic.previously_paid_amount =
                SafeParseDouble(txtpreviously_paid_amount.Text);
            Invoic.remaining_contract_balance =
                SafeParseDouble(txtremaining_contract_balance.Text);

            var operContract = new InvoiceOperContract();
            var invoice = operContract.MappingInvoicecontract(ref Invoic);
            Entry entry = (ProcType == 1 || ProcType == 2)
                ? operContract.BindToEntry(invoice) : null;

            bool saved = operContract.SaveInvoice(invoice, entry, Invoic.ISNew);

            if (saved)
            {
                ThreadContext.Properties["EmpID"] = MainClass.EmpNo;
                string actionType = Invoic.ISNew ? "حفظ" : "تعديل";
                Logger.Info(
                    $"تم {actionType} " +
                    $"{InvoiceOper.GetInvoiceType((int)invoice.InvoiceType, invoice.ProcType, invoice.PayType, 2)} " +
                    $"برقم {invoice.InvoiceNo} " +
                    $"بواسطة المستخدم: {MainClass.UserName}");
            }

            if (saved && Invoic.IsPrinted && print.PrintNo > 0)
                Invoic.ISNew = false;

            // === مزامنة ===
            if (saved && Sync.ActiveSync && InvObj.SyncInv &&
                Sync.SyncType > 0)
            {
                if (!InvObj.SyncEntry) entry = null;
                await operContract.SyncInvoice(invoice, entry, Invoic.ISNew);
            }

            Inventory.UpdateItemStock(invoice);

            if (saved)
            {
                var msgForm = new frmSavedMsg();
                msgForm.ShowDialog();

                switch (msgForm.Pressed)
                {
                    case 1:
                        CLR();
                        break;
                    case 2:
                        Invoic.ISNew = false;
                        Invoic.IsPrinted = true;
                        Invoic.IsUpdated = false;
                        txtNo.Background = Brushes.WhiteSmoke;
                        break;
                    case 3:
                        CLR();
                        this.Close();
                        break;
                    default:
                        CLR();
                        break;
                }
            }
        }

        #endregion

        #region ── Print & Export ──

        private void RptPrint(Invoice inv, int printType)
        {
            if (Invoic.InvoiceItems.Count == 0)
            {
                ShowMessage("لا توجد عمليات بالجدول",
                            "", MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(print.RptUrl))
            {
                ShowMessage("يجب تحديد مسار التقرير",
                            "", MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(print.RptName))
                print.RptName = "rptcontract.repx";

            if (!Directory.Exists(print.RptUrl) || !File.Exists(""))
                print.RptName = "rptcontract.repx";

            string path = Path.Combine(print.RptUrl, print.RptName);
            if (!Directory.Exists(print.RptUrl) || !File.Exists(path))
            {
                ShowMessage("المسار الحالي للتقارير غير موجود أو تم تعديله",
                            "خطأ", MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(print.defPrinter))
                print.defPrinter = MainClass.ReportsPrinter;

            print.Printing(printType, print.BindToData(inv),
                print.RptUrl, print.RptName,
                print.defPrinter, print.kitchenprinter, print.PrintNo);
        }

        private void RptPrintPDF(Invoice inv, int printType)
        {
            if (string.IsNullOrEmpty(print.RptName))
                print.RptName = "rptcontract.repx";

            if (!Directory.Exists(print.RptUrl) || !File.Exists(""))
                print.RptName = "rptcontract.repx";

            string path = Path.Combine(print.RptUrl, print.RptName);
            if (!Directory.Exists(print.RptUrl) || !File.Exists(path))
            {
                ShowMessage("المسار الحالي للتقارير غير موجود أو تم تعديله",
                            "خطأ", MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(print.defPrinter))
                print.defPrinter = MainClass.ReportsPrinter;

            print.defPrinter = "Microsoft Print to PDF";
            print.Printing(printType, print.BindToData(inv),
                print.RptUrl, print.RptName,
                print.defPrinter, print.kitchenprinter, print.PrintNo);
        }

        #endregion

        #region ── Context Menu Events ──

        private void stripImport_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("هل أنت متأكد من استيراد البيانات؟",
                "", MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            try
            {
                var importForm = new frmImportDataGeneral();
                importForm.cmbInv.SelectedIndex = 0;
                importForm.cmbInv.Visibility = Visibility.Visible;
                importForm.cmbInv.IsEnabled = false;
                importForm.cmbDataTable.Visibility = Visibility.Collapsed;
                importForm.ShowDialog();

                if (!importForm.ISDone ||
                    importForm.dtInvSel.Rows.Count == 0) return;

                foreach (DataRow row in importForm.dtInvSel.Rows)
                {
                    SearchByCode(row["ItemCode"].ToString(),
                        Convert.ToInt32(row["unit"]));

                    foreach (var invItem in Invoic.InvoiceItems)
                    {
                        if (invItem.ItemCode?.ToString() !=
                                row["ItemCode"]?.ToString() ||
                            invItem.ItemRowIndex !=
                                Invoic.InvoiceItems.Count)
                            continue;

                        invItem.Description = row["description"].ToString();
                        invItem.ItemQuantity =
                            Convert.ToDouble(row["quntity"]);
                        if (Convert.ToDouble(row["price"]) > 0.0)
                            invItem.ItemPrice =
                                Convert.ToDouble(row["price"]);

                        ItemOpercontract.CalcRowscontratct(ref Invoic);
                        BindingDGV();
                    }
                }
                ShowMessage("تم الاستيراد بنجاح");
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        private void BTnShowEntry_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsInvExist())
                {
                    ShowMessage("لم يتم حفظ الفاتورة");
                    return;
                }
                var form = new frmRptEntries();
                form.Show();
                form.Navigate(
                    $"SELECT * FROM Entry " +
                    $"WHERE IS_Deleted=0 " +
                    $"AND GlobalID=N'{Invoic.EntryGlobalID}'");
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        private void BtnSaveDgvSettings_Click(object sender,
            RoutedEventArgs e)
        {
            if (!Directory.Exists(StyleFolder))
                Directory.CreateDirectory(StyleFolder);
            GridControl1.SaveLayoutToXml(StyleFile);
            ShowMessage("تم حفظ مظهر الجدول");
        }

        private void BtnDefaultSetting_Click(object sender,
            RoutedEventArgs e)
        {
            if (File.Exists(StyleFile))
                File.Delete(StyleFile);
            ShowMessage("تم إعادة الجدول للإعدادات الافتراضية");
        }

        private void OpenReportDesgin_Click(object sender,
            RoutedEventArgs e)
        {
            try
            {
                if (InvType == 23 && ProcType == 1)
                    Common.OpenReportDesgin("rptcontract.repx", 23);
            }
            catch (Exception ex)
            {
                Logger.Error($"{Title} {ex.Message} {MainClass.UserName}");
            }
        }

        private void ExportPDF_Click(object sender, RoutedEventArgs e)
        {
            if (Invoic.IsPrinted && !Invoic.IsUpdated)
                RptPrintPDF(new InvoiceOperContract()
                    .MappingInvoicecontract(ref Invoic), 1);
            else
                ShowMessage("لا يمكن طباعة الفاتورة قبل الحفظ ..");
        }

        #endregion

        #region ── Helper Methods ──

        private static double SafeParseDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0.0;
            string cleaned = value.Replace("دائن", "").Trim();
            return double.TryParse(cleaned,
                out double result) ? result : 0.0;
        }

        private T SafeGetSelectedValue<T>(ComboBox combo)
        {
            try
            {
                if (combo.SelectedValue == null) return default;
                return (T)Convert.ChangeType(
                    combo.SelectedValue, typeof(T));
            }
            catch { return default; }
        }

        private int GetGridCellInt(int rowHandle, string fieldName)
        {
            var value = GridControl1.GetCellValue(rowHandle, fieldName);
            return value == null ? 0 : Convert.ToInt32(value);
        }

        private double GetGridCellDouble(int rowHandle, string fieldName)
        {
            var value = GridControl1.GetCellValue(rowHandle, fieldName);
            return value == null ? 0.0 : Convert.ToDouble(value);
        }

        private static void ShowMessage(
            string message,
            string title = "SmartAudit ERP",
            MessageBoxImage icon = MessageBoxImage.Information)
        {
            MessageBox.Show(message, title,
                MessageBoxButton.OK, icon,
                MessageBoxResult.OK,
                MessageBoxOptions.RightAlign |
                MessageBoxOptions.RtlReading);
        }

        #endregion
    }
}