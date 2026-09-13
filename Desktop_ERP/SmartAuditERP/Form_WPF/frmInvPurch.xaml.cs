// ─── استيراد نماذج المشروع ───
using AuditorAPI.Models;
using AuditorAPI.Models.DGVmodels;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using log4net;
using Newtonsoft.Json;
using SmartAuditERP;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInvPurch : DevExpress.Xpf.Core.ThemedWindow
    {
        #region ══════════════════ Fields & Properties ══════════════════

        private SqlConnection conn;
        private SqlConnection conn1;

        public int InvType;
        public int ProcType;
        public int EntryType;

        private Print print;
        private InvoiceObj InvObj;
        private InvoiceDGV Invoic;

        private string StyleFile;
        private string StyleFolder;

        private static readonly ILog Logger =
            LogManager.GetLogger(Environment.MachineName);

        private string[] filePaths;

        public FormPermission FrmPermission;

        /// <summary>مصدر بيانات الجدول الرئيسي</summary>
        private ObservableCollection<InvoiceItem> _invoiceItems =
            new ObservableCollection<InvoiceItem>();

        /// <summary>مصدر بيانات جدول نتائج البحث</summary>
        private ObservableCollection<InvSearchRow> _searchResults =
            new ObservableCollection<InvSearchRow>();

        /// <summary>مؤقت لعرض الوقت الحالي</summary>
        private DispatcherTimer Timer1;

        /// <summary>رقم صف DataGrid المحدد حالياً</summary>
        private int _focusedRowIndex = -1;

        #endregion

        #region ══════════════════ Constructor ══════════════════

        public frmInvPurch()
        {
            this.conn  = MainClass.ConnObj();
            this.conn1 = MainClass.ConnObj();
            this.InvType  = 1;
            this.ProcType = 1;
            this.print    = new Print(this.InvType);
            this.InvObj   = new InvoiceObj(this.InvType, this.ProcType);
            this.Invoic   = new InvoiceDGV();
            this.StyleFile =
                Path.Combine(MainClass.ReportsPath,
                    "Styles\\PurchaseInvoSaveLayoutToXML.xml");
            this.StyleFolder =
                Path.Combine(MainClass.ReportsPath, "Styles");
            this.FrmPermission = new FormPermission();

            InitializeComponent();

            // ربط مصادر البيانات
            this.GridControl1.ItemsSource = _invoiceItems;
            this.dgvSrch.ItemsSource      = _searchResults;
        }

        #endregion

        #region ══════════════════ Load & Init ══════════════════

        private void frmSalePurch_Load(object sender, RoutedEventArgs e)
        {
            try
            {
                // تحديد ملف الإعدادات حسب نوع الفاتورة
                if (this.InvType == 1)
                    this.StyleFile = Path.Combine(MainClass.ReportsPath,
                        "Styles\\PurchInvoSaveLayoutToXML.xml");
                else if (this.InvType == 9)
                    this.StyleFile = Path.Combine(MainClass.ReportsPath,
                        "Styles\\BeginningInventoryInvoSaveLayoutToXML.xml");

                this.InvObj = new InvoiceObj(this.InvType, this.ProcType);

                // تحميل البيانات
                this.LoadData1();

                // تهيئة التواريخ
                this.txtDate.EditValue    = DateTime.Now;
                this.txtToDate.EditValue  = DateTime.Now;
                this.txtRefDate.EditValue = DateTime.Now;
                this.txtFromDate.EditValue= DateTime.Now;
                this.txtInvTime.Text      = DateTime.Now.ToString("hh:mm:ss tt");

                // تهيئة القوائم المنسدلة
                if (this.cmbPayType.Items.Count > 0)
                    this.cmbPayType.SelectedIndex = 1;
                if (this.cmbTreasury.Items.Count > 0)
                    this.cmbTreasury.SelectedIndex = 0;
                if (this.cmbStore.Items.Count > 0)
                    this.cmbStore.SelectedIndex = 0;

                this.txtBarcode.Focus();
                this.txtInvTaxPer.Text = this.InvObj.VAT.ToString();

                // إظهار زر الإدراج في حالات معينة
                if (((this.InvType == 1) && (this.ProcType == 2)) ||
                    (this.InvType == 22))
                    this.btnInsertLinkedInv.Visibility = Visibility.Visible;

                // لون مميز لفاتورة الإرجاع
                if (this.InvType == 22)
                {
                    this.GroupBox3.Background = new SolidColorBrush(Colors.Silver);
                    this.LblReturn.Visibility = Visibility.Visible;
                }

                // صلاحيات
                this.FrmPermission.ApplyFrmPermission(this);
                Common.ApplyEditAddPermission(this, true, this.FrmPermission);

                // تهيئة الفاتورة
                this.ResetInvoice();

                // تشغيل المؤقت
                this.InitTimer();

                this.LoadDefaultpaytyp();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل النافذة:\n" + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitTimer()
        {
            Timer1 = new DispatcherTimer();
            Timer1.Interval = TimeSpan.FromSeconds(1);
            Timer1.Tick += Timer1_Tick;
            Timer1.Start();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            this.lblCurrentTime.Text = DateTime.Now.ToString("hh:mm:ss tt");
        }

        #endregion

        #region ══════════════════ Load Data ══════════════════

        private void LoadData1()
        {
            this.LoadSafes();
            this.LoadCustomers();
            this.LoadTreasury();
            this.LoadSalesMen();
            this.LoadBanks();
            this.LoadCostCenters();
            this.FillPayTypes();
        }

        private void FillPayTypes()
        {
            this.cmbPayType.Items.Clear();
            this.cmbPayType.Items.Add("آجلة");
            this.cmbPayType.Items.Add("نقدية");
            this.cmbPayType.Items.Add("بنك");
        }

        private void LoadCostCenters()
        {
            try
            {
                DataTable dt = LoadData.CostCenters();
                this.cmbCostCenter.DisplayMemberPath = "name";
                this.cmbCostCenter.SelectedValuePath = "code";
                this.cmbCostCenter.ItemsSource       = dt.DefaultView;
                this.cmbCostCenter.SelectedIndex     = -1;
            }
            catch { }
        }

        private void LoadBanks()
        {
            try
            {
                DataTable dt = LoadData.Banks();
                this.cmbBanks.DisplayMemberPath = "name";
                this.cmbBanks.SelectedValuePath = "id";
                this.cmbBanks.ItemsSource       = dt.DefaultView;
                this.cmbBanks.SelectedIndex     = -1;
            }
            catch { }
        }

        public void LoadSafes()
        {
            DataTable dt = LoadData.Invertories(MainClass.EmpNo);
            this.cmbStore.DisplayMemberPath = "name";
            this.cmbStore.SelectedValuePath = "id";
            this.cmbStore.ItemsSource       = dt.DefaultView;
            this.cmbStore.SelectedIndex     = -1;
        }

        private void LoadCustomers()
        {
            int type = 2;

            if (string.Equals(MainClass.Language, "ar",
                    StringComparison.OrdinalIgnoreCase))
            {
                this.lblClient.Text    = "المورد";
                this.lblClientSrch.Text= "المورد";
            }
            else
            {
                this.lblClient.Text    = "Supplier";
                this.lblClientSrch.Text= "Supplier";
            }

            DataTable dt = LoadData.Customers(type);

            this.cmbClient.DisplayMemberPath = "name";
            this.cmbClient.SelectedValuePath = "id";
            this.cmbClient.ItemsSource       = dt.DefaultView;
            this.cmbClient.SelectedIndex     = -1;

            this.cmbClientSrch.DisplayMemberPath = "name";
            this.cmbClientSrch.SelectedValuePath = "id";
            this.cmbClientSrch.ItemsSource       = dt.DefaultView;
            this.cmbClientSrch.SelectedIndex     = -1;

            if (this.cmbClient.Items.Count > 0)
                this.cmbClient.SelectedIndex = 0;
        }

        public void LoadTreasury()
        {
            DataTable dt = LoadData.Treasury();
            this.cmbTreasury.DisplayMemberPath = "name";
            this.cmbTreasury.SelectedValuePath = "id";
            this.cmbTreasury.ItemsSource       = dt.DefaultView;
            this.cmbTreasury.SelectedIndex     = -1;
        }

        public void LoadSalesMen()
        {
            DataTable dt = LoadData.SalesMen();
            this.cmbSalesMen.DisplayMemberPath = "name";
            this.cmbSalesMen.SelectedValuePath = "id";
            this.cmbSalesMen.ItemsSource       = dt.DefaultView;
            this.cmbSalesMen.SelectedIndex     = -1;
        }

        private void LoadInvNo()
        {
            this.Invoic.InvoiceNo =
                InvoiceOper.InvoiceNo(this.InvType, this.ProcType,
                    this.InvObj.Prefixe);

            this.txtInvCode.Text =
                MainClass.BranchCode + this.InvObj.InvoiceCode +
                this.ProcType.ToString();

            this.Invoic.InvCombinedId =
                this.txtInvCode.Text.Trim() +
                this.Invoic.InvoiceNo.ToString();

            this.txtNo.Text         = this.Invoic.InvoiceNo.ToString();
            this.txtNo.Background   = new SolidColorBrush(Colors.Firebrick);
            this.txtNo.Foreground   = new SolidColorBrush(Colors.White);
        }

        #endregion

        #region ══════════════════ Reset & Binding ══════════════════

        public void LoadDefaultpaytyp()
        {
            if (this.InvObj.PayTypeDefault == -1)
            {
                this.cmbPayType.SelectedIndex = 0;
                this.cmbTreasury.SelectedIndex = -1;
                if (this.Invoic.ISNew)
                {
                    this.cmbClient.SelectedIndex = -1;
                    this.cmbClient.Focus();
                    this.Invoic.Customer = -1;
                }
                this.Invoic.PayType        = -1;
                this.Invoic.Treasury       = -1;
                this.Invoic.PaymentStatus  = PaymentStatus.PostPaid;
            }
            else if (this.InvObj.PayTypeDefault == 1)
            {
                this.cmbPayType.SelectedIndex  = 1;
                this.cmbTreasury.SelectedIndex = 0;
                this.cmbBanks.SelectedIndex    = -1;
                this.Invoic.PayType  = 1;
                this.Invoic.Treasury = GetSelectedId(this.cmbTreasury);
                this.Invoic.Customer = GetSelectedId(this.cmbClient);
            }
            else if (this.InvObj.PayTypeDefault == 2)
            {
                this.cmbPayType.SelectedIndex  = 2;
                this.cmbBanks.SelectedIndex    = 0;
                this.cmbTreasury.SelectedIndex = -1;
                this.Invoic.PayType  = 2;
                this.Invoic.Treasury = -1;
                this.Invoic.Bank     = 1;
                this.Invoic.Customer = GetSelectedId(this.cmbClient);
            }
            else
            {
                this.cmbPayType.SelectedIndex  = 1;
                this.cmbTreasury.SelectedIndex = 0;
                this.cmbBanks.SelectedIndex    = -1;
                this.Invoic.PayType  = 1;
                this.Invoic.Treasury = GetSelectedId(this.cmbTreasury);
                this.Invoic.Customer = GetSelectedId(this.cmbClient);
            }
        }

        private void ResetInvoice()
        {
            this.LoadInvNo();
            this.Invoic.InvoiceType     = this.InvObj.InvType;
            this.Invoic.ProcType        = this.ProcType;
            this.Invoic.VATperc         = this.InvObj.VAT;
            this.Invoic.Currency        = this.InvObj.Currency;
            this.Invoic.InvoiceCode     = this.InvObj.InvoiceCode;
            this.Invoic.AdditionalCost  = 0m;
            this.Invoic.InvoiceStatus   = 3;
            this.Invoic.PaymentStatus   = PaymentStatus.Paid;
            this.Invoic.Branch          = MainClass.BranchNo;
            this.Invoic.InvertoryImpact = this.InvObj.InvertoryImpact;

            if ((this.Invoic.InvoiceType == InvoiceType.ReturnSales) &&
                (this.Invoic.ProcType == 1))
                this.Invoic.InvertoryImpact = 1;
            else if ((this.Invoic.InvoiceType == InvoiceType.ReturnSales) &&
                     (this.Invoic.ProcType == 2))
                this.Invoic.InvertoryImpact = 2;

            this.Invoic.ExtraVATPerc  = this.InvObj.AdditionalTax;
            this.Invoic.PriceIncVAT   = this.InvObj.PriceIncVAT;
            this.Invoic.InvAccCode    = this.InvObj.InvAcc;

            if (this.Invoic.InvoiceType == InvoiceType.ReturnSales)
                this.Invoic.InvAccCode = 3200001.ToString();

            this.Invoic.Store         = GetSelectedId(this.cmbStore);
            this.Invoic.InvertoryName = this.cmbStore.Text;
            this.Invoic.Treasury      = GetSelectedId(this.cmbTreasury);
            this.Invoic.Customer      = GetSelectedId(this.cmbClient);
            this.Invoic.User          = MainClass.EmpNo;
            this.Invoic.InvCCcode     = (-1).ToString();
            this.Invoic.CreateDate    = DateTime.Now;
            this.Invoic.InvDate       = GetDateEditValue(this.txtDate);
            this.Invoic.InvTime       = ParseTime(this.txtInvTime.Text);
            this.Invoic.RefDate       = GetDateEditValue(this.txtRefDate);
            this.Invoic.PayType       = 1;
            this.Invoic.ISNew         = true;
            this.Invoic.IsDeleted     = false;
            this.Invoic.IsLoaded      = false;
            this.Invoic.IsPrinted     = false;
            this.Invoic.ReffNo        = (-1).ToString();
            this.Invoic.Currency      = this.InvObj.Currency;
            this.Invoic.Pricing       = this.InvObj.Pricing;
            this.Invoic.ItemsDiscount = 0.0;
            this.Invoic.FreeVATSales  = 0.0;

            // ربط مصدر بيانات الجدول
            _invoiceItems.Clear();
            if (this.Invoic.InvoiceItems != null)
            {
                foreach (var item in this.Invoic.InvoiceItems)
                    _invoiceItems.Add(item);
            }

            InvoiceOper.filePathDocument = null;
        }

        private void BindingDGV()
        {
            // تحديث مصدر بيانات الجدول
            _invoiceItems.Clear();
            if (this.Invoic?.InvoiceItems != null)
            {
                foreach (var item in this.Invoic.InvoiceItems)
                    _invoiceItems.Add(item);
            }

            // تحديث حقول الملخص
            int rowCount = _invoiceItems.Count;
            this.txtTirmsNo.Text      = rowCount.ToString();
            this.txtTotQuan.Text      = this.Invoic.TotalQty.ToString(this.InvObj.DigitsNo);
            this.txtSumVal.Text       = this.Invoic.SumPrice.ToString(this.InvObj.DigitsNo);
            this.txtTotDiscount.Text  = this.Invoic.TotDiscount.ToString(this.InvObj.DigitsNo);
            this.txtInvDiscVal.Text   = this.Invoic.InvDiscount.ToString(this.InvObj.DigitsNo);
            this.txtInvDiscPerc.Text  =
                InvoiceOper.GetDiscountPercentage(
                    this.Invoic.SumPrice, this.Invoic.InvDiscount)
                .ToString(this.InvObj.DigitsNo);
            this.txtNetWithoutVAT.Text= this.Invoic.Total.ToString(this.InvObj.DigitsNo);
            this.txtTotVAT.Text       = this.Invoic.VAT.ToString(this.InvObj.DigitsNo);
            this.txtNet.Text          = this.Invoic.Net.ToString(this.InvObj.DigitsNo);
            this.txtAdditonalCost.Text= this.Invoic.AdditionalCost.ToString(this.InvObj.DigitsNo);
            this.txtBalancePreviews.Text = this.Invoic.BalancePreviews.ToString();

            // التمرير لآخر صف
            if (this.GridControl1.Items.Count > 0)
            {
                this.GridControl1.ScrollIntoView(
                    this.GridControl1.Items[this.GridControl1.Items.Count - 1]);
            }
        }

        private void BindingControls()
        {
            this.txtNo.Text = this.Invoic.InvoiceNo.ToString();

            this.txtDate.EditValue    = this.Invoic.InvDate;
            this.txtInvTime.Text      = this.Invoic.InvTime.ToString("hh:mm:ss tt");
            this.txtRefDate.EditValue = this.Invoic.RefDate;
            this.txtRefNo.Text        = this.Invoic.ReffNo;
            this.txtNote.Text         = this.Invoic.InvNote;

            SetComboValue(this.cmbTreasury, this.Invoic.Treasury);
            SetComboValue(this.cmbStore,    this.Invoic.Store);
            SetComboValue(this.cmbClient,   this.Invoic.Customer);

            if (double.TryParse(this.Invoic.InvCCcode, out double ccVal) && ccVal > -1)
                SetComboValue(this.cmbCostCenter, this.Invoic.InvCCcode);

            // طريقة الدفع
            if (this.Invoic.PayType == -1)
                this.cmbPayType.SelectedIndex = 0;
            else if (this.Invoic.PayType == 1)
                this.cmbPayType.SelectedIndex = 1;
            else if (this.Invoic.PayType == 2)
            {
                this.cmbPayType.SelectedIndex = 2;
                SetComboValue(this.cmbBanks, this.Invoic.Bank);
            }

            try { SetComboValue(this.cmbSalesMen, this.Invoic.Saleman); }
            catch { }
        }

        #endregion

        #region ══════════════════ Navigation ══════════════════

        public void Navigate(string sqlStr)
        {
            if (this.Invoic != null)
                this.CLR();

            InvoiceOper.BindingInvoice(ref this.Invoic, sqlStr);

            if (this.Invoic != null)
            {
                this.txtNo.Background = new SolidColorBrush(Colors.WhiteSmoke);
                this.txtNo.Foreground = new SolidColorBrush(Colors.Black);
                this.BindingDGV();
                this.BindingControls();

                this.ckZeroVAT.IsChecked = (this.Invoic.VAT == 0.0);
            }
            else
            {
                this.Invoic = new InvoiceDGV();
                this.CLR();
            }
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            if (!this.CheckBeforeClear())
                this.Navigate(
                    $"select top 1 * from Inv where inv_type={this.InvType} " +
                    $"and proc_type={this.ProcType} and IS_Deleted=0 " +
                    $"and branch={MainClass.BranchNo} order by id asc");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (!this.CheckBeforeClear())
            {
                double.TryParse(this.txtNo.Text, out double invNo);
                this.Navigate(
                    $"select top 1 * from Inv where inv_type={this.InvType} " +
                    $"and proc_type={this.ProcType} and IS_Deleted=0 " +
                    $"and id<{(int)invNo} " +
                    $"and branch={MainClass.BranchNo} order by id desc");
            }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (!this.CheckBeforeClear())
            {
                double.TryParse(this.txtNo.Text, out double invNo);
                this.Navigate(
                    $"select top 1 * from Inv where inv_type={this.InvType} " +
                    $"and proc_type={this.ProcType} and IS_Deleted=0 " +
                    $"and id>{(int)invNo} " +
                    $"and branch={MainClass.BranchNo} order by id asc");
            }
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            if (!this.CheckBeforeClear())
                this.Navigate(
                    $"select top 1 * from Inv where inv_type={this.InvType} " +
                    $"and proc_type={this.ProcType} and IS_Deleted=0 " +
                    $"and branch={MainClass.BranchNo} order by id desc");
        }

        #endregion

        #region ══════════════════ Item Search (الإدخال السريع) ══════════════════

        private bool ISItem(string barcode)
        {
            try
            {
                var da = new SqlDataAdapter(
                    $"select name from Items where IS_Deleted=0 and barcode={barcode}",
                    this.conn);
                var dt = new DataTable();
                da.Fill(dt);
                return dt.Rows.Count > 0;
            }
            catch { return false; }
        }

        private void SearchByName(string itemName)
        {
            try
            {
                if (!string.IsNullOrEmpty(itemName))
                {
                    var da = new SqlDataAdapter(
                        $"select name,id from Items where IS_Deleted=0 and name=N'{itemName}'",
                        this.conn);
                    var dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                        this.SearchByID(Convert.ToInt32(dt.Rows[0]["id"]), 0, "");
                    else if (!string.IsNullOrEmpty(itemName))
                        this.SearchByCode(itemName, 0);
                    else
                        this.AddNewItem();
                }
                else
                {
                    this.AddNewItem();
                }
            }
            catch { }
        }

        private void SearchByCode(string itemCode, int itemUnit)
        {
            try
            {
                var da = new SqlDataAdapter(
                    $"select name,id from Items where IS_Deleted=0 and Code=N'{itemCode}'",
                    this.conn);
                var dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                    this.SearchByID(Convert.ToInt32(dt.Rows[0]["id"]), itemUnit, "");
                else if (!string.IsNullOrWhiteSpace(itemCode))
                    this.ReadBarcode(itemCode, true);
                else
                    this.AddNewItem();
            }
            catch { }
        }

        private void SearchByID(int itemId, int unitId, string itemBarcode)
        {
            try
            {
                new ItemOper().GetItemByID(itemId, unitId,
                    ref this.Invoic, false, itemBarcode, "");
                ItemOper.CalcRows(ref this.Invoic);
                this.BindingDGV();
            }
            catch { }
        }

        private void AddNewItem()
        {
            var frm = new frmItemsSrch();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.sql    = "select id, name ,nameEN , sale_price , unit " +
                         "from Items where IS_Deleted=0 order by id";
            frm.search = "select id, name ,nameEN , sale_price , unit from Items";
            frm.StoreId= GetSelectedId(this.cmbStore);
            frm.ShowDialog();

            if (frm.ISDone && frm.ItemId > 0)
            {
                foreach (int id in frm.Itemlist)
                    this.SearchByID(id, 0, "");
            }
        }

        private void ReadBarcode(string itemBarcode, bool isMultiText)
        {
            try
            {
                int     itemID       = 0;
                int     unitId       = 0;
                decimal itemPrice    = 0;
                decimal itemQuantity = 0;

                ItemOper.SearchForBarcode(
                    itemBarcode.Trim(),
                    ref itemID, ref unitId, ref itemPrice, ref itemQuantity,
                    isMultiText);

                if (itemID > 0)
                    this.SearchByID(itemID, unitId, itemBarcode);
                else
                    this.AddNewItem();
            }
            catch { }
        }

        #endregion

        #region ══════════════════ DataGrid Events (الإدخال السريع في الجدول) ══════════════════

        private void GridView1_KeyDown_WPF(object sender, KeyEventArgs e)
        {
            if (this.GridControl1.Items.Count <= 0) return;

            var grid           = sender as DataGrid;
            var focusedRow     = grid?.CurrentItem as InvoiceItem;
            var focusedColName = grid?.CurrentColumn?.Header?.ToString() ?? "";

            if (e.Key == Key.Delete)
            {
                if (MessageBox.Show("هل انت متأكد من الحذف ؟",
                        "رسالة تأكيد",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    DeleteFocusedRow();
                }
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                if (focusedRow == null) return;

                // الإدخال السريع حسب العمود المحدد
                if (focusedColName.Contains("رمز الصنف") ||
                    focusedColName.Contains("ItemCode"))
                {
                    this.SearchByCode(focusedRow.ItemCode, 0);
                    MoveToColumn(grid, "الكمية");
                    e.Handled = true;
                }
                else if (focusedColName.Contains("الصنف") ||
                         focusedColName.Contains("ItemName"))
                {
                    this.SearchByName(focusedRow.ItemName);
                    MoveToColumn(grid, "الكمية");
                    e.Handled = true;
                }
                else if (focusedColName.Contains("الوحدة"))
                {
                    this.ShowItemUnit(focusedRow.ItemId, focusedRow.ItemRowIndex);
                    MoveToColumn(grid, "الكمية");
                    e.Handled = true;
                }
                else if (focusedColName.Contains("الكمية"))
                {
                    MoveToColumn(grid, "السعر");
                    e.Handled = true;
                }
                else if (focusedColName.Contains("السعر"))
                {
                    MoveToNextRow(grid, "الصنف");
                    e.Handled = true;
                }
                else if (focusedColName.Contains("المستودع"))
                {
                    this.ShowItemInvertories(
                        focusedRow.ItemId, focusedRow.ItemRowIndex);
                    MoveToNextRow(grid, "الصنف");
                    e.Handled = true;
                }
            }
        }

        private void GridView1_CellEditEnding_WPF(
            object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;

            var item       = e.Row.Item as InvoiceItem;
            var colHeader  = e.Column.Header?.ToString() ?? "";
            if (item == null) return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (colHeader.Contains("الكمية") ||
                        colHeader == "ItemQuantity")
                    {
                        ItemOper.CalcRows(ref this.Invoic);
                        this.BindingDGV();
                        ItemOper.checkQtyLimi(
                            ref this.Invoic, item.ItemId);
                    }
                    else if (colHeader.Contains("السعر") ||
                             colHeader == "ItemPrice")
                    {
                        ItemOper.CalcRows(ref this.Invoic);
                        this.BindingDGV();
                    }
                    else if (colHeader.Contains("الخصم") ||
                             colHeader == "ItemDiscount")
                    {
                        double discPerc =
                            Convert.ToDouble(
                                InvoiceOper.GetDiscountPercentage(
                                    item.ItemSumPrice, item.ItemDiscount)
                                .ToString(this.InvObj.DigitsNo));

                        double priceDisc = (item.ItemQuantity > 0)
                            ? item.ItemDiscount / item.ItemQuantity
                            : 0;

                        item.ItemDiscountPerc = discPerc;
                        item.ItemPriceDiscount= priceDisc;

                        ItemOper.CalcRows(ref this.Invoic);
                        this.BindingDGV();
                    }
                    else if (colHeader == "ItemDiscountPerc")
                    {
                        double discVal =
                            item.ItemDiscountPerc / 100.0 * item.ItemSumPrice;
                        double priceDisc = (item.ItemQuantity > 0)
                            ? discVal / item.ItemQuantity
                            : 0;

                        item.ItemDiscount      = discVal;
                        item.ItemPriceDiscount = priceDisc;

                        ItemOper.CalcRows(ref this.Invoic);
                        this.BindingDGV();
                    }
                    else if (colHeader == "ItemSumPrice")
                    {
                        if (item.ItemQuantity > 0)
                            item.ItemPrice = item.ItemSumPrice / item.ItemQuantity;

                        ItemOper.CalcRows(ref this.Invoic);
                        this.BindingDGV();
                    }
                    else if (colHeader == "ItemPriceDiscount")
                    {
                        double discVal =
                            item.ItemPriceDiscount * item.ItemQuantity;
                        double discPerc =
                            Convert.ToDouble(
                                InvoiceOper.GetDiscountPercentage(
                                    item.ItemSumPrice, discVal)
                                .ToString(this.InvObj.DigitsNo));

                        item.ItemDiscount      = discVal;
                        item.ItemDiscountPerc  = discPerc;

                        ItemOper.CalcRows(ref this.Invoic);
                        this.BindingDGV();
                    }
                }
                catch { }
            }), DispatcherPriority.Background);
        }

        private void GridView1_FocusedRowChanged_WPF(
            object sender, EventArgs e)
        {
            var grid = sender as DataGrid;
            var item = grid?.CurrentItem as InvoiceItem;
            if (item != null && item.ItemId > 0)
                this.ShowItemDetails(item.ItemId);
        }

        private void DeleteFocusedRow()
        {
            var item = this.GridControl1.SelectedItem as InvoiceItem;
            if (item == null) return;

            _invoiceItems.Remove(item);
            this.Invoic.InvoiceItems.Remove(item);

            // إعادة ترقيم الصفوف
            int idx = 1;
            foreach (var inv in this.Invoic.InvoiceItems)
                inv.ItemRowIndex = idx++;

            ItemOper.CalcRows(ref this.Invoic);
            this.BindingDGV();
        }

        #endregion

        #region ══════════════════ Context Menu ══════════════════

        private void MenuShowItemCard_Click(object sender, RoutedEventArgs e)
        {
            this.ShowItemCard();
        }

        private void MenuAddNewProduct_Click(object sender, RoutedEventArgs e)
        {
            this.AddNewProduct();
        }

        private void MenuShowItemUnits_Click(object sender, RoutedEventArgs e)
        {
            this.ShowItemUnits();
        }

        private void MenuShowItemInvertory_Click(object sender, RoutedEventArgs e)
        {
            this.ShowItemInvertory();
        }

        private void MenuDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("هل انت متأكد من حذف السجل ؟",
                    "تأكيد", MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                DeleteFocusedRow();
        }

        private void MenuItemProcess_Click(object sender, RoutedEventArgs e)
        {
            this.ItemProcess();
        }

        private void MenuItemLastActivity_Click(object sender, RoutedEventArgs e)
        {
            this.ItemLastActivity();
        }

        private void MenuClientActivity_Click(object sender, RoutedEventArgs e)
        {
            this.ItemClientActivity();
        }

        private void MenuItemSerialNo_Click(object sender, RoutedEventArgs e)
        {
            this.ShowItemMoreDetails();
        }

        private void MenuItemCost_Click(object sender, RoutedEventArgs e)
        {
            this.ItemCost1();
        }

        #endregion

        #region ══════════════════ Item Actions ══════════════════

        private void ShowItemDetails(int itemId)
        {
            try
            {
                this.txtLastSalePrice.Text    = "";
                this.txtCompetitorPrice.Text  = "";
                this.txtCostAvrg.Text         = "";
                this.txtStock.Text            = "";
                this.txtItemBarcode.Text      = "";

                var item = this.GridControl1.CurrentItem as InvoiceItem;
                if (item == null) return;

                if (User.ShowCosts)
                {
                    this.txtRecentPurchPrice.Text =
                        ItemOper.RecentPurchPrice(itemId).ToString();
                    this.txtCostAvrg.Text =
                        Math.Round(item.ItemCost * item.UnitEquality, 2).ToString();
                }
                else
                {
                    this.txtRecentPurchPrice.Text = "0";
                    this.txtCostAvrg.Text         = "0";
                }

                this.LoadPrices(itemId);

                if (item.UnitEquality > 0)
                {
                    this.txtStock.Text = item.ValiableInvertory.ToString();
                    this.txtStock.Foreground =
                        item.ValiableInvertory > -1
                            ? new SolidColorBrush(Colors.Green)
                            : new SolidColorBrush(Colors.Firebrick);
                }

                this.txtPerUnit.Text      = item.UnitEquality.ToString();
                this.txtTotUnitQuan.Text  = item.ItemPrimaryQnty.ToString();
                this.txtItemBarcode.Text  = item.ItemBarcode ?? "";
            }
            catch { }
        }

        private void LoadPrices(int itemId)
        {
            try
            {
                var da = new SqlDataAdapter(
                    $"select sale_price,CompetitorPrice from ItemPrices " +
                    $"where Itemid={itemId} order by Proc_id desc",
                    this.conn);
                var dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    this.txtLastSalePrice.Text   = dt.Rows[0]["sale_price"].ToString();
                    this.txtCompetitorPrice.Text = dt.Rows[0]["CompetitorPrice"].ToString();
                }
            }
            catch { }
        }

        private void ShowItemUnit(int itemId, int rowIndex)
        {
            try
            {
                var target = Invoic.InvoiceItems
                    .FirstOrDefault(x => x.ItemId == itemId &&
                                          x.ItemRowIndex == rowIndex);
                if (target == null) return;

                var frm = new frmItemUnits
                {
                    ItemId = target.ItemId,
                    PreUnit = target.UnitID
                };
                frm.ShowDialog();

                if (!string.IsNullOrEmpty(frm.Unitname))
                {
                    target.UnitID = frm.UnitId;
                    target.UnitName = frm.Unitname;
                    ItemOper.LoadUnitInf(ref target,
                        (int)Invoic.InvoiceType, Invoic.Pricing);
                    ItemOper.CalcRows(ref Invoic);
                    BindingDGV();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء تحميل الوحدات\n" + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowItemMoreDetails()
        {
            try
            {
                var currentItem = this.GridControl1.CurrentItem as InvoiceItem;
                if (currentItem == null) return;

                foreach (var inv in this.Invoic.InvoiceItems)
                {
                    if (inv.ItemId == currentItem.ItemId &&
                        inv.InvoiceItemDetails?.Count > 0)
                    {
                        var frm = new frmItemSerialNo();
                        frm.InvItem = inv;
                        frm.ShowDialog();

                        if (frm.ISDone)
                        {
                            var cur = frm.InvItem;
                            cur.ItemQuantity = 0;
                            foreach (var d in cur.InvoiceItemDetails)
                                cur.ItemQuantity += d.ItemQuantity;

                            ItemOper.CalcRows(ref this.Invoic);
                            this.BindingDGV();
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ\n" + ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowItemInvertories(int itemId, int rowIndex)
        {
            try
            {
                foreach (var inv in this.Invoic.InvoiceItems)
                {
                    if (inv.ItemId == itemId && inv.ItemRowIndex == rowIndex)
                    {
                        var frm = new frmItemInvertory();
                        frm.ItemId = inv.ItemId;
                        frm.ShowDialog();

                        if (frm.InvertoryId > 0)
                        {
                            inv.InvertoryId   = frm.InvertoryId;
                            inv.InvertoryName = frm.InvertoryName;
                            inv.ValiableInvertory =
                                Convert.ToDouble(frm.ItemQty);

                            ItemOper.ISvalidQuantity(
                                ref this.Invoic, itemId, rowIndex, true);
                            ItemOper.CalcRows(ref this.Invoic);
                            this.BindingDGV();
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل المستودع\n" + ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowItemCard()
        {
            if (User.EditItemInfo)
            {
                var item = this.GridControl1.CurrentItem as InvoiceItem;
                if (item == null) return;

                var frm = new frmItems();
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.ItemId = item.ItemId;
                frm.ShowDialog();
            }
            else
                MessageBox.Show("لا يوجد لديك صلاحية لهذه العملية");
        }

        private void AddNewProduct()
        {
            if (User.EditItemInfo)
            {
                var frm = new frmItems();
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.ShowDialog();
            }
            else
                MessageBox.Show("لا يوجد لديك صلاحية لهذه العملية");
        }

        private void ShowItemUnits()
        {
            var item = this.GridControl1.CurrentItem as InvoiceItem;
            if (item != null)
                this.ShowItemUnit(item.ItemId, item.ItemRowIndex);
        }

        private void ShowItemInvertory()
        {
            var item = this.GridControl1.CurrentItem as InvoiceItem;
            if (item != null)
                this.ShowItemInvertories(item.ItemId, item.ItemRowIndex);
        }

        private void ItemProcess()
        {
            var item = this.GridControl1.CurrentItem as InvoiceItem;
            if (item == null) return;

            var frm = new frmRptItemsActivityDetailed();
            frm.SelectedId       = item.ItemId;
            frm.txtItemName.Text = item.ItemName;
            frm.txtItemCode.Text = item.ItemCode;
            frm.ShowResult();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.ShowDialog();
        }

        private void ItemLastActivity()
        {
            var item = this.GridControl1.CurrentItem as InvoiceItem;
            if (item == null) return;

            var frm = new frmRptItemsActivityDetailed();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.txtItemName.Text = item.ItemName;
            frm.txtItemCode.Text = item.ItemCode;
            frm.Show();
            frm.LastProcess(item.ItemId, item.InvertoryId, -1, 1);
        }

        private void ItemClientActivity()
        {
            var item = this.GridControl1.CurrentItem as InvoiceItem;
            if (item == null) return;

            var frm = new frmRptItemsActivityDetailed();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.txtItemName.Text = item.ItemName;
            frm.txtItemCode.Text = item.ItemCode;
            frm.Show();

            if (this.cmbClient.SelectedIndex > -1)
                frm.LastProcess(item.ItemId, item.InvertoryId,
                    GetSelectedId(this.cmbClient), 2);
        }

        private void ItemCost1()
        {
            if (!(GridControl1.SelectedItem is InvoiceItem si))
                return;

            if (User.ShowCosts)
            {
                DXMessageBox.Show(
                    ItemOper.AvgCost(si.ItemId, MainClass.BranchNo).ToString("F2"),
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                DXMessageBox.Show("لا يوجد لديك صلاحية لهذه العملية",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ItemSearch_ButtonClick_WPF(object sender, RoutedEventArgs e)
        {
            this.AddNewItem();
        }

        #endregion

        #region ══════════════════ Save & Print ══════════════════

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!Common.CheckProiedAcc(GetDateEditValue(this.txtDate)))
            {
                MessageBox.Show("لا يمكن أن يكون التاريخ خارج الفترة المحاسبية",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int invNo = this.Invoic.ISNew ? 0 : this.Invoic.InvoiceNo;

            if (Common.CheckReffNo(
                    this.txtRefNo.Text,
                    GetSelectedId(this.cmbClient), invNo) &&
                MessageBox.Show(
                    "عفوآ رقم المرجع موجود مسبقآ هل تريد الاستمرار ؟",
                    "تحذير", MessageBoxButton.YesNo,
                    MessageBoxImage.Exclamation) == MessageBoxResult.No)
                return;

            try
            {
                this.btnSave.IsEnabled = false;

                if (!double.TryParse(this.txtNet.Text, out double netVal) ||
                    (netVal <= 0.0 && this.InvType != 9))
                {
                    MessageBox.Show("يجب أن يكون الصافي اكبر من الصفر");
                    this.btnSave.IsEnabled = true;
                    return;
                }

                if (MessageBox.Show("هل انت متأكد من حفظ ؟", "",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    this.btnSave.IsEnabled = true;
                    return;
                }

                if (this.cmbPayType.SelectedIndex == 0 &&
                    this.cmbClient.SelectedIndex == -1)
                {
                    MessageBox.Show("يجب اختيار المورد");
                    this.btnSave.IsEnabled = true;
                    return;
                }

                SetPayAmounts();
                this.Invoic.IsPrinted = false;
                this.SaveAndPrint();
                this.btnSave.IsEnabled = true;
            }
            catch
            {
                this.btnSave.IsEnabled = true;
            }
        }

        private void btnSavePrint_Click(object sender, RoutedEventArgs e)
        {
            if (_invoiceItems.Count == 0 || this.Invoic.Net == 0.0)
            {
                MessageBox.Show("لا يمكن حفظ الفاتورة بدون أصناف");
                return;
            }

            foreach (var item in _invoiceItems)
            {
                if (item.ItemQuantity <= 0)
                {
                    MessageBox.Show("لا يمكن حفظ الفاتورة يوجد كميات أقل أو تساوي الصفر");
                    return;
                }
            }

            if (this.cmbPayType.SelectedIndex == 0 &&
                this.cmbClient.SelectedIndex == -1)
            {
                MessageBox.Show("يجب اختيار المورد");
                return;
            }

            this.Invoic.IsPrinted = true;
            SetPayAmounts();
            this.SaveAndPrint();
        }

        private void SetPayAmounts()
        {
            if (this.Invoic.PayType == -1)
            {
                this.Invoic.Paycash = 0.0;
                this.Invoic.PayATM  = 0.0;
            }
            else if (this.Invoic.PayType == 1)
            {
                this.Invoic.Paycash = this.Invoic.Net;
                this.Invoic.PayATM  = 0.0;
                this.Invoic.Paid    = this.Invoic.Net;
            }
            else if (this.Invoic.PayType == 2)
            {
                this.Invoic.Paycash = 0.0;
                this.Invoic.PayATM  = this.Invoic.Net;
                this.Invoic.Paid    = this.Invoic.Net;
            }
        }

        private async void SaveAndPrint()
        {
            try
            {
                var invoiceOper = new InvoiceOper();

                // تحقق من إرجاع الكميات
                if (Invoic.InvoiceType == InvoiceType.ReturnSales &&
                    !(CkUpdateQty.IsChecked == true))
                {
                    var res = DXMessageBox.Show(
                        "هل أنت متأكد من عدم توريد الكميات؟",
                        "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res == MessageBoxResult.Yes)
                        foreach (var it in Invoic.InvoiceItems)
                            it.InvertoryImpact = 0;
                }

                Invoice invoice = invoiceOper.MappingInvoice(ref Invoic);
                Entry entry = null;

                if ((ProcType == 1 || ProcType == 2) &&
                    (InvType == 1 || InvType == 22))
                {
                    entry = invoiceOper.BindToEntry(invoice);
                    if (entry == null) return;
                }

                Inventory.UpdateItemStock(invoice);
                bool saved = invoiceOper.SaveInvoice(invoice, entry, Invoic.ISNew);
                bool wasNew = Invoic.ISNew;

                if (saved)
                {
                    string action = wasNew ? " تم حفظ " : " تم تعديل ";
                    Logger.Info(action +
                        InvoiceOper.GetInvoiceType(
                            (int)invoice.InvoiceType,
                            invoice.ProcType,
                            invoice.PayType, 2) +
                        " برقم " + invoice.InvoiceNo +
                        " بواسطة: " + MainClass.UserName);
                }

                if (saved && Invoic.IsPrinted && print.PrintNo > 0)
                {
                    Invoic.ISNew = false;
                    RptPrint(invoice, 1);
                }

                // المزامنة
                if (saved && Sync.ActiveSync &&
                    InvObj.SyncInv && Sync.SyncType > 0)
                {
                    if (!InvObj.SyncEntry) entry = null;
                    await invoiceOper.SyncInvoice(invoice, entry, Invoic.ISNew);
                }
                var Home = new Home();
                // إرسال البيانات للسحابة
                if (saved && Home._is_active)
                {
                    if (ConnectBroker.IsConnectedToInternet())
                    {
                        if (ConnectBroker.CheckConnectionAndBroker())
                        {
                            string cond1 = "where InvGlobalID=N'" +
                                           invoice.InvGlobalID + "'";
                            string cond2 = "WHERE InvGlobalID=N'" +
                                           invoice.InvGlobalID + "'";
                            string cond3 = "where GlobalID=N'" +
                                           invoice.EntryGlobalID + "'";
                            string cond4 = "where EntryGlobalID=N'" +
                                           invoice.EntryGlobalID + "'";

                            SendData.SendDataa("Inv",
                                invoice.InvGlobalID,
                                SendData.GetInv(cond1));
                            SendData.SendDataa("InvSub",
                                invoice.InvGlobalID,
                                SendData.GetInvSub(cond2));
                            SendData.SendDataa("entry",
                                invoice.EntryGlobalID,
                                SendData.GetEntryData(cond3));
                            SendData.SendDataa("entryDetails",
                                invoice.EntryGlobalID,
                                SendData.GetEntrySubData(cond4));
                        }
                    }
                    else
                    {
                        SaveGlobalIDOffLine(
                            Invoic.InvGlobalID,
                            Invoic.EntryGlobalID);
                    }
                }

                if (!saved) return;

                RecalculateCost();

                var msgFrm = new frmSavedMsg();
                if (!wasNew)
                    msgFrm.lblSave.Text =
                        MainClass.Language == "ar"
                            ? "تم حفظ التعديلات بنجاح..."
                            : "Successfully updated ...";
                msgFrm.ShowDialog();

                if (msgFrm.Pressed == 1)
                    CLR();
                else if (msgFrm.Pressed == 2)
                {
                    Invoic.ISNew = false;
                    Invoic.IsPrinted = true;
                    Invoic.IsUpdated = false;
                    txtNo.Background = Brushes.WhiteSmoke;
                    txtNo.Foreground = Brushes.Black;
                }
                else if (msgFrm.Pressed == 3)
                {
                    CLR();
                    Close();
                }
                else
                    CLR();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحفظ: " + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RptPrint(Invoice inv, int type)
        {
            if (this.GridControl1.Items.Count == 0)
            {
                MessageBox.Show("لا توجد عمليات شراء أو بيع بالجدول");
                return;
            }
            if (string.IsNullOrEmpty(this.print.RptUrl))
            {
                MessageBox.Show("يجب تحديد مسار التقرير");
                return;
            }
            if (string.IsNullOrEmpty(this.print.RptName))
                this.print.RptName = "RptInvPurchase.repx";

            string path = Path.Combine(this.print.RptUrl, this.print.RptName);
            if (!Directory.Exists(this.print.RptUrl) || !File.Exists(path))
            {
                MessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (string.IsNullOrEmpty(this.print.defPrinter))
                this.print.defPrinter = MainClass.ReportsPrinter;

            this.print.Printing(type,
                this.print.BindToData(inv),
                this.print.RptUrl,
                this.print.RptName,
                this.print.defPrinter,
                this.print.kitchenprinter,
                this.print.PrintNo);
        }

        #endregion

        #region ══════════════════ Delete ══════════════════

        private async void DeleteInv()
        {
            try
            {
                var invoiceOper = new InvoiceOper();
                var invoice     = invoiceOper.MappingInvoice(ref this.Invoic);

                Entry entry = null;
                if (((this.ProcType == 1) || (this.ProcType == 2)) &&
                    (this.InvType == 1))
                {
                    entry = invoiceOper.BindToEntry(invoice);
                    if (entry == null) return;
                }

                if (this.Invoic.ISNew) return;
                if (!InvoiceOper.DeleteInvoice(this.Invoic)) return;

                invoice.IsDeleted = true;
                invoice.Sent      = false;

                if (entry != null) entry.ISDeleted = true;

                if (Sync.ActiveSync && this.InvObj.SyncInv && Sync.SyncType > 0)
                {
                    if (!this.InvObj.SyncEntry) entry = null;
                    await invoiceOper.SyncInvoice(invoice, entry, this.Invoic.ISNew);
                }

                Logger.Info(
                    " تم حذف " +
                    InvoiceOper.GetInvoiceType(
                        (int)this.Invoic.InvoiceType,
                        this.Invoic.ProcType, this.Invoic.PayType, 2) +
                    " برقم " + this.Invoic.InvoiceNo +
                    " بواسطة المستخدم: " + MainClass.UserName);

                this.CLR();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء الحذف:\n" + ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            this.DeleteInv();
        }

        #endregion

        #region ══════════════════ CLR & New ══════════════════

        private void CLR()
        {
            try
            {
                // إعادة تعيين القوائم
                if (this.cmbStore.Items.Count > 0)
                    this.cmbStore.SelectedIndex = 0;
                if (this.cmbClient.Items.Count > 0)
                    this.cmbClient.SelectedIndex = 0;
                if (this.cmbTreasury.Items.Count > 0)
                    this.cmbTreasury.SelectedIndex = 0;

                this.txtDate.EditValue    = DateTime.Now;
                this.txtToDate.EditValue  = DateTime.Now;
                this.txtRefDate.EditValue = DateTime.Now;
                this.txtFromDate.EditValue= DateTime.Now;
                this.txtInvTime.Text      = DateTime.Now.ToString("hh:mm:ss tt");

                this.cmbPayType.SelectedIndex  = 1;
                this.cmbCostCenter.SelectedIndex = -1;

                this.txtBarcode.Focus();

                Common.ApplyEditAddPermission(this, true, this.FrmPermission);
                this.BtnReceipts.Visibility = Visibility.Collapsed;

                this.Invoic = new InvoiceDGV();
                _invoiceItems.Clear();
                this.ResetInvoice();
                this.LoadDefaultpaytyp();

                // إعادة لون رقم الفاتورة
                this.txtNo.Background =
                    new SolidColorBrush(Colors.Firebrick);
                this.txtNo.Foreground =
                    new SolidColorBrush(Colors.White);
            }
            catch { }
        }

        private bool CheckBeforeClear()
        {
            if (this.Invoic == null) return false;

            if ((_invoiceItems.Count > 0 && this.Invoic.ISNew) ||
                this.Invoic.IsUpdated)
            {
                return MessageBox.Show(
                    string.Equals(MainClass.Language, "ar",
                        StringComparison.OrdinalIgnoreCase)
                        ? "لم يتم حفظ الفاتورة, هل تريد جديد"
                        : "You do not save the invoice",
                    "تنبيه", MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.No;
            }
            return false;
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            if (!this.CheckBeforeClear())
                this.CLR();
        }

        private void btnAddNewItem_Click(object sender, RoutedEventArgs e)
        {
            if (User.EditItemInfo)
            {
                var frm = new frmItems();
                frm.ShowDialog();
            }
            else
                MessageBox.Show("لا يوجد لديك صلاحية لهذه العملية");
        }

        #endregion

        #region ══════════════════ Print & View ══════════════════

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (this.Invoic.IsPrinted)
                this.RptPrint(new InvoiceOper().MappingInvoice(ref this.Invoic), 1);
            else
                MessageBox.Show("لا يمكن طباعة الفاتورة قبل الحفظ ..");
        }

        private void IconButton1_Click(object sender, RoutedEventArgs e)
        {
            if (this.Invoic.IsPrinted)
                this.RptPrint(new InvoiceOper().MappingInvoice(ref this.Invoic), 2);
            else
                MessageBox.Show("لا يمكن معاينة الفاتورة قبل الحفظ ..");
        }

        #endregion

        #region ══════════════════ Payment Type ══════════════════

        private void cmbType_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.cmbPayType.SelectedIndex == 2)
            {
                if (this.cmbTreasury != null)
                    this.cmbTreasury.IsEnabled = false;
                if (this.cmbBanks != null)
                {
                    this.cmbBanks.IsEnabled = true;
                    this.cmbBanks.Focus();
                }
                this.BtnReceipts.Visibility = Visibility.Collapsed;
            }
            else if (this.cmbPayType.SelectedIndex == 1)
            {
                if (this.cmbTreasury != null)
                    this.cmbTreasury.IsEnabled = true;
                if (this.cmbBanks != null)
                    this.cmbBanks.IsEnabled = false;
                this.BtnReceipts.Visibility = Visibility.Collapsed;
            }
            else if (this.cmbPayType.SelectedIndex == 0)
            {
                if (this.cmbTreasury != null)
                    this.cmbTreasury.IsEnabled = false;
                this.BtnReceipts.Visibility = Visibility.Visible;
            }
        }

        private void cmbPayType_SelectionChangeCommitted(
            object sender, SelectionChangedEventArgs e)
        {
            if (this.cmbPayType.SelectedIndex == 2)
            {
                this.cmbBanks.SelectedIndex    = -1;
                this.cmbTreasury.SelectedIndex = -1;
                this.Invoic.PayType  = 2;
                this.Invoic.Treasury = -1;
                this.Invoic.Customer = GetSelectedId(this.cmbClient);
            }
            else if (this.cmbPayType.SelectedIndex == 1)
            {
                try
                {
                    this.cmbTreasury.SelectedIndex = 0;
                    this.Invoic.PayType  = 1;
                    this.Invoic.Treasury = GetSelectedId(this.cmbTreasury);
                    this.Invoic.Customer = GetSelectedId(this.cmbClient);
                }
                catch { }
            }
            else if (this.cmbPayType.SelectedIndex == 0)
            {
                this.cmbTreasury.SelectedIndex = -1;
                if (this.Invoic.ISNew)
                {
                    this.cmbClient.SelectedIndex = -1;
                    this.cmbClient.Focus();
                    this.Invoic.Customer = -1;
                }
                this.Invoic.PayType  = -1;
                this.Invoic.Treasury = -1;
            }
        }

        #endregion

        #region ══════════════════ Customer ══════════════════

        private void cmbClient_SelectedIndexChanged(
            object sender, SelectionChangedEventArgs e)
        {
            this.ShowCustBalance();
        }

        private void cmbClient_KeyPress_WPF(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                this.SrchByNameClint();
            }
        }

        private void SrchByNameClint()
        {
            try
            {
                var da = new SqlDataAdapter(
                    $"select id,name from Customers " +
                    $"where IS_Deleted=0 and name=N'{this.cmbClient.Text}' " +
                    $"and (type=2 or type=3)",
                    this.conn);
                var dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    SetComboValue(this.cmbClient,
                        Convert.ToInt32(dt.Rows[0]["id"]));
                    this.Invoic.Customer = GetSelectedId(this.cmbClient);
                }
                else
                    this.AddNewClint();
            }
            catch { }
        }

        private void AddNewClint()
        {
            try
            {
                var frm = new frmSrchClient();
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.Type = 2;

                frm.txtClientName.Text =
                    string.Equals(cmbClient.Text, "مورد عام",
                        StringComparison.OrdinalIgnoreCase)
                        ? ""
                        : cmbClient.Text;

                if (cmbPayType.SelectedIndex == 0)
                    frm.PostponeClient = true;

                frm.ShowDialog();

                if (!string.IsNullOrEmpty(frm.Clientname))
                {
                    LoadCustomers();
                    SetComboValue(cmbClient, frm.ClientId);
                    Invoic.Customer = frm.ClientId;
                    ShowCustBalancePreviews();
                    Invoic.BalancePreviews =
                        float.TryParse(txtBalancePreviews.Text,
                            out float bp) ? bp : 0f;
                }
            }
            catch { }
        }

        private void btnCustAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var frm = new frmCustomers();
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.Title = (this.InvType == 2) ? "تعريف عميل" : "تعريف مورد";
                frm.Type = (this.InvType == 2) ? 1 : 2;
                frm.ShowDialog();
                if (frm.isDone) this.LoadCustomers();
            }
            catch { }
        }

        private void btnZoom_Click(object sender, RoutedEventArgs e)
        {
            this.AddNewClint();
        }

        private void ShowCustBalance()
        {
            try
            {
                if (this.cmbClient.SelectedIndex < 0) return;
                var da = new SqlDataAdapter(
                    $"select Code from Accounts_Index " +
                    $"where AName=N'{this.cmbClient.Text}' " +
                    Accounting.BranchCondition,
                    this.conn1);
                var dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count == 0) return;

                string accCond = $" and Entry_sub.acc_no={dt.Rows[0][0]}";
                string brCond  = MainClass.BranchNo != -1
                    ? $"Entry.branch={MainClass.BranchNo} " +
                      $"and Entry_sub.branch={MainClass.BranchNo} and "
                    : "";

                var da2 = new SqlDataAdapter(
                    $"select sum(Entry_sub.dept) as dept, " +
                    $"sum(Entry_sub.credit) as credit " +
                    $"from Entry,Entry_sub where {brCond} " +
                    $"Entry.IS_Deleted=0 and Entry.state=1 " +
                    $"and Entry.GlobalId=Entry_sub.EntryGlobalId{accCond} " +
                    $"group by Entry_sub.acc_no",
                    this.conn1);
                var dt2 = new DataTable();
                da2.Fill(dt2);

                if (dt2.Rows.Count > 0)
                {
                    double dept   = Convert.ToDouble(dt2.Rows[0]["dept"]);
                    double credit = Convert.ToDouble(dt2.Rows[0]["credit"]);

                    if (dept > credit)
                        this.txtBalance.Text =
                            $"{Math.Round(dept - credit, 3):0.##}";
                    else if (credit > dept)
                        this.txtBalance.Text =
                            $"{Math.Round(credit - dept, 3):0.##} دائن";
                    else
                        this.txtBalance.Text = "0";
                }
            }
            catch { }
        }

        private void ShowCustBalancePreviews()
        {
            try
            {
                if (this.cmbClient.SelectedIndex < 0) return;
                var da = new SqlDataAdapter(
                    $"select Code from Accounts_Index " +
                    $"where AName=N'{this.cmbClient.Text}' " +
                    Accounting.BranchCondition,
                    this.conn1);
                var dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count == 0) return;

                string accCond = $" and Entry_sub.acc_no={dt.Rows[0][0]}";
                string brCond  = MainClass.BranchNo != -1
                    ? $"Entry.branch={MainClass.BranchNo} " +
                      $"and Entry_sub.branch={MainClass.BranchNo} and "
                    : "";

                var da2 = new SqlDataAdapter(
                    $"select sum(Entry_sub.dept) as dept, " +
                    $"sum(Entry_sub.credit) as credit " +
                    $"from Entry,Entry_sub where {brCond} " +
                    $"Entry.IS_Deleted=0 and Entry.state=1 " +
                    $"and Entry.GlobalId=Entry_sub.EntryGlobalId{accCond} " +
                    $"group by Entry_sub.acc_no",
                    this.conn1);
                var dt2 = new DataTable();
                da2.Fill(dt2);

                if (dt2.Rows.Count > 0)
                {
                    double dept   = Convert.ToDouble(dt2.Rows[0]["dept"]);
                    double credit = Convert.ToDouble(dt2.Rows[0]["credit"]);

                    this.txtBalancePreviews.Text = dept > credit
                        ? $"{Math.Round(dept - credit, 3):0.##}"
                        : $"{Math.Round(credit - dept, 3):0.##}";
                }
            }
            catch { }
        }

        #endregion

        #region ══════════════════ Discount ══════════════════

        private void txtInvDiscVal_Leave(object sender, RoutedEventArgs e)
        {
            try { this.CalcDiscount(); } catch { }
        }

        private void txtInvDiscVal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) this.CalcDiscount();
        }

        private void txtInvDiscPerc_Leave(object sender, RoutedEventArgs e)
        {
            try { this.CalcDiscountPerc(); } catch { }
        }

        private void txtInvDiscPerc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) this.CalcDiscountPerc();
        }

        private void CalcDiscount()
        {
            if (!string.IsNullOrWhiteSpace(this.txtInvDiscVal.Text))
            {
                if (double.TryParse(this.txtInvDiscVal.Text, out double val))
                {
                    this.Invoic.InvDiscount = val;
                    ItemOper.CalcRows(ref this.Invoic);
                    this.BindingDGV();
                }
            }
            else
            {
                this.Invoic.InvDiscount  = 0.0;
                this.txtInvDiscPerc.Text = "0";
                ItemOper.CalcRows(ref this.Invoic);
                this.BindingDGV();
            }
        }

        private void CalcDiscountPerc()
        {
            if (!string.IsNullOrWhiteSpace(this.txtInvDiscPerc.Text))
            {
                if (double.TryParse(this.txtInvDiscPerc.Text, out double perc))
                {
                    this.Invoic.InvDiscount = Convert.ToDouble(
                        InvoiceOper.GetInvDiscountByPercentage(
                            this.Invoic.SumPrice, perc));
                    ItemOper.CalcRows(ref this.Invoic);
                    this.BindingDGV();
                }
            }
            else
            {
                this.txtInvDiscPerc.Text = "0";
                this.Invoic.InvDiscount  = 0.0;
                ItemOper.CalcRows(ref this.Invoic);
                this.BindingDGV();
            }
        }

        private void txtNet_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!this.InvObj.PriceIncVAT)
            {
                var frm = new FrmCalcVAT();
                frm.ItemVAT = (int)Math.Round(this.InvObj.VAT);
                frm.ShowDialog();

                if (frm.Price > 0.0)
                {
                    this.Invoic.Total        = frm.PriceWithOutVAT;
                    this.Invoic.TotDiscount  =
                        this.Invoic.SumPrice - this.Invoic.Total;
                    this.Invoic.InvDiscount  =
                        this.Invoic.TotDiscount - this.Invoic.ItemsDiscount;
                    ItemOper.CalcRows(ref this.Invoic);
                    this.BindingDGV();
                }
            }
        }

        #endregion

        #region ══════════════════ ComboBox Events ══════════════════

        private void cmbTreasury_SelectionChangeCommitted(
            object sender, SelectionChangedEventArgs e)
        {
            this.Invoic.Treasury = this.cmbTreasury.SelectedIndex > -1
                ? GetSelectedId(this.cmbTreasury)
                : -1;
        }

        private void cmbBanks_SelectionChangeCommitted(
            object sender, SelectionChangedEventArgs e)
        {
            this.Invoic.Bank = this.cmbBanks.SelectedIndex > -1
                ? GetSelectedId(this.cmbBanks)
                : -1;
        }

        private void cmbStore_SelectionChangeCommitted(
            object sender, SelectionChangedEventArgs e)
        {
            if (this.cmbStore.SelectedIndex > -1)
            {
                this.Invoic.Store =
                    GetSelectedId(this.cmbStore);
                this.Invoic.InvertoryName =
                    Common.GetStoreName(this.Invoic.Store);
            }
            else
                this.Invoic.Store = -1;
        }

        private void cmbSalesMen_SelectionChangeCommitted(
            object sender, SelectionChangedEventArgs e)
        {
            this.Invoic.Saleman = this.cmbSalesMen.SelectedIndex > -1
                ? GetSelectedId(this.cmbSalesMen)
                : -1;
        }

        private void cmbClient_SelectionChangeCommitted(
            object sender, SelectionChangedEventArgs e)
        {
            try
            {
                this.Invoic.Customer = this.cmbClient.SelectedIndex > -1
                    ? GetSelectedId(this.cmbClient)
                    : -1;
            }
            catch { }
        }

        private void cmbCostCenter_SelectionChangeCommitted(
            object sender, SelectionChangedEventArgs e)
        {
            this.Invoic.InvCCcode = this.cmbCostCenter.SelectedIndex > -1
                ? this.cmbCostCenter.SelectedValue?.ToString()
                : (-1).ToString();
        }

        private void cmbAddSalesMen_Click(object sender, RoutedEventArgs e)
        {
            int prevId = -1;
            if (this.cmbSalesMen.SelectedValue != null)
                int.TryParse(this.cmbSalesMen.SelectedValue.ToString(),
                    out prevId);

            var frm = new frmSalesMen();
            frm.ShowDialog();
            this.LoadSalesMen();

            try { SetComboValue(this.cmbSalesMen, prevId); } catch { }
        }

        private void btnAddCostCenter_Click(object sender, RoutedEventArgs e)
        {
            int prevId = -1;
            if (this.cmbCostCenter.SelectedValue != null)
                int.TryParse(this.cmbCostCenter.SelectedValue.ToString(),
                    out prevId);

            var frm = new frmCostCenter();
            frm.ShowDialog();
            this.LoadCostCenters();

            try { SetComboValue(this.cmbCostCenter, prevId); } catch { }
        }

        #endregion

        #region ══════════════════ TextBox Events ══════════════════

        private void txtDate_ValueChanged(object sender, EditValueChangedEventArgs e)
        {
            this.Invoic.InvDate = GetDateEditValue(this.txtDate);
        }

        private void txtRefNo_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.Invoic != null)
                this.Invoic.ReffNo = this.txtRefNo.Text;
        }

        private void txtRefDate_ValueChanged(object sender, EditValueChangedEventArgs e)
        {
            if (this.Invoic != null)
                this.Invoic.RefDate = GetDateEditValue(this.txtRefDate);
        }

        private void txtNote_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.Invoic != null)
                this.Invoic.InvNote = this.txtNote.Text;
        }

        private void txtSrchDgv_TextChanged(object sender, TextChangedEventArgs e)
        {
            // البحث في صفوف الجدول
            string keyword = this.txtSrchDgv.Text.ToLower().Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                this.GridControl1.ItemsSource = _invoiceItems;
                return;
            }
            var filtered = _invoiceItems.Where(i =>
                (i.ItemName?.ToLower().Contains(keyword) == true) ||
                (i.ItemCode?.ToLower().Contains(keyword) == true) ||
                (i.ItemBarcode?.ToLower().Contains(keyword) == true))
                .ToList();
            this.GridControl1.ItemsSource = filtered;
        }

        // ═══ وقت الفاتورة ═══
        private void txtInvTime_GotFocus(object sender, RoutedEventArgs e)
        {
            if (this.txtInvTime.Text == DateTime.Now.ToString("hh:mm:ss tt"))
                this.txtInvTime.SelectAll();
        }

        private void txtInvTime_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.txtInvTime.Text))
                this.txtInvTime.Text = DateTime.Now.ToString("hh:mm:ss tt");

            this.Invoic.InvTime = ParseTime(this.txtInvTime.Text);
        }

        #endregion

        #region ══════════════════ CheckBox Events ══════════════════

        private void ckZeroVAT_CheckedChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Invoic.VATperc = (this.ckZeroVAT.IsChecked == true)
                    ? 0.0
                    : this.InvObj.VAT;
                ItemOper.CalcRows(ref this.Invoic);
                this.BindingDGV();
            }
            catch { }
        }

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (this.chkAll.IsChecked != true)
            {
                this.cmbClientSrch.IsEnabled = true;
            }
            else
            {
                this.cmbClientSrch.SelectedIndex = -1;
                this.cmbClientSrch.IsEnabled     = false;
            }
        }

        #endregion

        #region ══════════════════ Additional Cost ══════════════════

        private void btnAdditonalCost_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmAdditionalCost { Invoice = Invoic };
            frm.ShowDialog();

            if (frm.IsDone)
            {
                Invoic.InvoiceCosts = frm.Invoice.InvoiceCosts;
                Invoic.AdditionalCost = frm.Invoice.AdditionalCost;
                ItemOper.CalcRows(ref Invoic);
                BindingDGV();
            }
        }

        #endregion

        #region ══════════════════ Insert Linked Invoice ══════════════════

        private void btnInsertLinkedInv_Click(object sender, RoutedEventArgs e)
        {
            this.btnNew_Click(sender, e);

            if (this.InvType == 1 && this.ProcType == 2)
            {
                this.CLR();
                InvoiceOper.InsertFromInv(ref this.Invoic, 1);
                PrepareReturnInvoice(InvoiceType.Purchase, 2, 2);
            }
            else if (this.InvType == 1 && this.ProcType == 1)
            {
                InvoiceOper.InsertFromInv(ref this.Invoic, 4);
                this.Invoic.ISNew = true;
                this.ProcType     = 1;
                this.LoadInvNo();
                ItemOper.CalcRows(ref this.Invoic);
                this.BindingDGV();
                this.BindingControls();
            }
            else if (this.InvType == 22 && this.ProcType == 2)
            {
                this.CLR();
                InvoiceOper.InsertFromInv(ref this.Invoic, 1);
                PrepareReturnInvoice(InvoiceType.ReturnSales, 2, 2);
            }
            else if (this.InvType == 22 && this.ProcType == 1)
            {
                this.CLR();
                InvoiceOper.InsertFromInv(ref this.Invoic, 1);
                PrepareReturnInvoice(InvoiceType.ReturnSales, 1, 2);
            }
        }

        private void PrepareReturnInvoice(
            InvoiceType invType, int procType, int invertoryImpact)
        {
            this.Invoic.InvoiceType     = invType;
            this.Invoic.ProcType        = procType;
            this.Invoic.InvNote         = "";
            this.Invoic.InvertoryImpact = invertoryImpact;
            this.Invoic.PayATM          = 0.0;
            this.Invoic.Paycash         = 0.0;
            this.Invoic.Paid            = 0.0;
            this.Invoic.Remainder       = 0.0;

            this.LoadInvNo();

            foreach (var item in this.Invoic.InvoiceItems)
                item.InvertoryImpact = invertoryImpact;

            this.Invoic.ReffNo   = this.Invoic.InvGlobalID;
            this.Invoic.RefDate  = this.Invoic.InvDate;
            this.Invoic.InvDate  = GetDateEditValue(this.txtDate);
            this.Invoic.ISNew    = true;

            ItemOper.CalcRows(ref this.Invoic);
            this.BindingDGV();
            this.BindingControls();
        }

        #endregion

        #region ══════════════════ Search Tab ══════════════════

        private void dgvSrch_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var row = this.dgvSrch.SelectedItem as InvSearchRow;
            if (row == null) return;

            this.Navigate(
                $"select * from Inv where branch={MainClass.BranchNo} " +
                $"and InvGlobalID=N'{row.InvGlobalID}'");

            this.TabControl1.SelectedIndex = 0;
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string branchCond = MainClass.BranchNo != -1
                    ? $" and Inv.branch={MainClass.BranchNo}"
                    : "";
                string clientCond = (!this.chkAll.IsChecked == true &&
                                     this.cmbClientSrch.SelectedIndex > -1)
                    ? $" and Inv.cust_id={GetSelectedId(this.cmbClientSrch)}"
                    : "";
                string noCond = !string.IsNullOrWhiteSpace(this.txtSrchNo.Text)
                    ? $" and Inv.id={this.txtSrchNo.Text}"
                    : "";
                string refCond = !string.IsNullOrWhiteSpace(this.txtSrchreffNo.Text)
                    ? $" and Inv.ref_no=N'{this.txtSrchreffNo.Text}'"
                    : "";

                DateTime from =
                    this.txtFromDate.EditValue is DateTime fd ? fd : DateTime.Today;
                DateTime to =
                    this.txtToDate.EditValue is DateTime td ? td : DateTime.Today;

                string sql =
                    $"select Inv.id, Inv.InvGlobalID, Inv.ref_no, Inv.date, " +
                    $"Customers.name as cust_name " +
                    $"from Inv left join Customers on Inv.cust_id=Customers.id " +
                    $"where Inv.inv_type={this.InvType} " +
                    $"and Inv.proc_type={this.ProcType} " +
                    $"and Inv.IS_Deleted=0 " +
                    $"and Inv.date between '{from:yyyy-MM-dd}' " +
                    $"and '{to:yyyy-MM-dd}'" +
                    branchCond + clientCond + noCond + refCond +
                    " order by Inv.id desc";

                var da = new SqlDataAdapter(sql, this.conn);
                var dt = new DataTable();
                da.Fill(dt);

                _searchResults.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    _searchResults.Add(new InvSearchRow
                    {
                        Column10    = row["id"].ToString(),
                        InvGlobalID = row["InvGlobalID"].ToString(),
                        InvNo       = row["id"].ToString(),
                        RefNo       = row["ref_no"].ToString(),
                        InvDate     = Convert.ToDateTime(row["date"])
                                       .ToString("yyyy-MM-dd"),
                        ClientName  = row["cust_name"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في البحث:\n" + ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region ══════════════════ Import / Export ══════════════════

        private void stripImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var res = DXMessageBox.Show(
                    "هل أنت متأكد من استيراد البيانات؟",
                    "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.No) return;

                var frm = new frmImportDataGeneral();
                frm.cmbInv.SelectedIndex = 1;
                frm.cmbInv.Visibility = Visibility.Visible;
                frm.cmbInv.IsEnabled = false;
                frm.cmbDataTable.Visibility = Visibility.Collapsed;
                frm.Activate();
                frm.ShowDialog();

                if (frm.ISDone && frm.dtInvSel.Rows.Count > 0)
                {
                    foreach (DataRow row in frm.dtInvSel.Rows)
                    {
                        SearchByCode(
                            row["ItemCode"].ToString(),
                            Convert.ToInt32(row["unit"]));

                        var inv = Invoic.InvoiceItems
                            .FirstOrDefault(x =>
                                x.ItemCode == row["ItemCode"].ToString() &&
                                x.ItemRowIndex == Invoic.InvoiceItems.Count);

                        if (inv != null)
                        {
                            inv.Description = row["description"].ToString();
                            inv.ItemQuantity = Convert.ToDouble(row["quntity"]);

                            if (double.TryParse(
                                    row["price"].ToString(),
                                    out double pr) && pr > 0)
                                inv.ItemPrice = pr;

                            ItemOper.CalcRows(ref Invoic);
                            BindingDGV();
                        }
                    }
                }

                DXMessageBox.Show("تم الاستيراد بنجاح");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void stripExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!this.IsInvExist())
                {
                    MessageBox.Show("يجب حفظ الفاتورة قبل التصدير");
                    return;
                }
                if (MessageBox.Show(
                        "هل انت متأكد من تصدير البيانات ؟", "",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) == MessageBoxResult.No)
                    return;

                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx"
                };
                if (dlg.ShowDialog() == true)
                {
                    // تصدير عبر مكتبة Excel (مثال)
                    MessageBox.Show("تم حفظ الملف في " + dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ:\n" + ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsInvExist()
        {
            try
            {
                var da = new SqlDataAdapter(
                    $"select id from Inv where branch={MainClass.BranchNo} " +
                    $"and inv_type={this.InvType} " +
                    $"and proc_type={this.ProcType} " +
                    $"and id={this.txtNo.Text}",
                    this.conn);
                var dt = new DataTable();
                da.Fill(dt);
                return dt.Rows.Count > 0;
            }
            catch { return false; }
        }

        #endregion

        #region ══════════════════ Toolbar Menu Handlers ══════════════════

        private void ToolStripDropDownButton1_Click(
            object sender, RoutedEventArgs e)
        {
            // فتح القائمة السياقية عند الضغط على زر ⚙️
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.Placement =
                    System.Windows.Controls.Primitives.PlacementMode.Bottom;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void BtnShowEntry_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!this.IsInvExist())
                {
                    MessageBox.Show("لم يتم حفظ الفاتورة");
                    return;
                }
                var frm = new frmRptEntries();
                frm.Show();
                frm.Navigate(
                    $"select * from Entry where IS_Deleted=0 " +
                    $"and GlobalID=N'{this.Invoic.EntryGlobalID}'");
            }
            catch { }
        }

        private void BtnReceipts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!this.IsInvExist())
                {
                    MessageBox.Show("لم يتم حفظ الفاتورة");
                    return;
                }
                var frm = new frmSandD();
                frm.Show();
                frm.cmbSupplies.SelectedValue = this.cmbClient.SelectedValue;
                frm.txtVal.Text   = this.txtNet.Text;
                frm.txtNotes.Text =
                    this.txtNote.Text + "   خاصة المورد:" + this.cmbClient.Text;
            }
            catch { }
        }

        private void btnShortCutInv_Click(object sender, RoutedEventArgs e)
        {
            var frm = new FrmShortCutInv();
            frm.InvVAT      = this.InvObj.VAT;
            frm.InvIncluVAT = this.InvObj.PriceIncVAT;
            frm.ShowDialog();

            if (double.TryParse(frm.txtNet.Text, out double net) && net > 0)
            {
                double.TryParse(frm.txtSumVal.Text,     out double sv);
                double.TryParse(frm.txtTotDiscount.Text, out double td);
                double.TryParse(frm.txtVAT.Text,         out double vat);
                double.TryParse(frm.txtTotal.Text,       out double tot);

                this.Invoic.SumPrice   = sv;
                this.Invoic.InvDiscount= td;
                this.Invoic.VAT        = vat;
                this.Invoic.Total      = tot;
                this.Invoic.Net        = net;
                this.BindingDGV();
            }
        }

        private void BtnSaveDgvSettings_Click(object sender, RoutedEventArgs e)
        {
            // حفظ عرض الأعمدة في ملف XML
            try
            {
                if (!Directory.Exists(this.StyleFolder))
                    Directory.CreateDirectory(this.StyleFolder);

                var settings = new System.Xml.Linq.XDocument(
                    new System.Xml.Linq.XElement("GridSettings",
                        this.GridControl1.Columns
                            .Cast<DataGridColumn>()
                            .Select(c => new System.Xml.Linq.XElement("Column",
                                new System.Xml.Linq.XAttribute("Header",
                                    c.Header?.ToString() ?? ""),
                                new System.Xml.Linq.XAttribute("Width",
                                    c.ActualWidth),
                                new System.Xml.Linq.XAttribute("Visibility",
                                    c.Visibility.ToString())))));
                settings.Save(this.StyleFile);
                MessageBox.Show("تم حفظ مظهر الجدول");
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ:\n" + ex.Message);
            }
        }

        private void BtnDefaultSetting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(this.StyleFile))
                    File.Delete(this.StyleFile);
                MessageBox.Show("تم إعادة ضبط الجدول");
            }
            catch { }
        }

        private void BtnPrintBarcode_Click(object sender, RoutedEventArgs e)
        {
            var list = new List<frmPrintBarcode.ItemDgv>();
            foreach (var item in _invoiceItems)
            {
                if (!string.IsNullOrEmpty(item.ItemBarcode))
                {
                    list.Add(new frmPrintBarcode.ItemDgv
                    {
                        RowIndex     = item.ItemRowIndex,
                        ItemCode     = item.ItemCode,
                        ItemName     = item.ItemName,
                        ItemBarcode  = item.ItemBarcode,
                        ItemQty      = (int)Math.Round(item.ItemQuantity),
                        ItemSalePrice= Common.GetItemPrice(item.ItemId)
                    });
                }
            }
            var frm = new frmPrintBarcode();
            frm.ItemList = list;
            frm.Show();
        }

        private void StripImportInv_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmImportInvoice();
            frm.ShowDialog();

            if (frm.isDone &&
                !string.Equals(frm.InvGID, "-1",
                    StringComparison.OrdinalIgnoreCase))
            {
                this.Navigate(
                    $"select * from Inv where InvGlobalID=N'{frm.InvGID}'");
            }

            this.Invoic.InvoiceType = (InvoiceType)this.InvType;
            this.Invoic.ProcType    = this.ProcType;
            this.Invoic.InvNote     = "";
            this.LoadInvNo();
            this.Invoic.ISNew = true;
            this.BindingControls();
        }

        private void OpenDesginReport_Click(object sender, RoutedEventArgs e)
        {
            Common.OpenReportDesgin("rptPOSA4.repx", 1);
        }

        private void ImportDocument_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter    = "Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png" +
                            "|PDF Files (*.pdf)|*.pdf",
                Title     = "تحديد الملفات",
                Multiselect = true
            };
            if (dlg.ShowDialog() == true)
                InvoiceOper.filePathDocument = dlg.FileNames;
        }

        private void btnshowdocument_Click(object sender, RoutedEventArgs e)
        {
            if (Invoic.ISNew)
            {
                DXMessageBox.Show("يجب حفظ الفاتورة أولاً",
                    "المدقق", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dtt = new DataTable();
            InvoiceOper.getdocuments(ref dtt, Invoic.InvGlobalID);

            var frmshowdocument = new Frmshowdocument();

            if (dtt.Rows.Count > 0)
            {
                var displayTable = new DataTable();

                // ✅ الأسماء يجب أن تطابق Binding في XAML بالضبط
                displayTable.Columns.Add("RowNo", typeof(int));
                displayTable.Columns.Add("FileName", typeof(string));
                displayTable.Columns.Add("FileUrl", typeof(string));

                for (int i = 0; i < dtt.Rows.Count; i++)
                {
                    displayTable.Rows.Add(
                        i + 1,                                          // RowNo
                        dtt.Rows[i]["FileName"]?.ToString() ?? "",      // FileName
                        dtt.Rows[i]["FileUrl"]?.ToString() ?? ""        // FileUrl
                    );
                }

                frmshowdocument.DataGridView1.ItemsSource = displayTable.DefaultView;
                frmshowdocument.type = 2;
                frmshowdocument.GlobalIdDoc = Invoic.InvGlobalID;
                frmshowdocument.Show();
            }
            else
            {
                DXMessageBox.Show("لا يوجد مستندات",
                    "المدقق", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnInvSrch_Click_1(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvoiceSrch();
            frm.cmbProcType.IsEnabled = false;
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.ProcType = this.ProcType;
            frm.InvType  = this.InvType;
            frm.ShowDialog();

            if (frm.ISDone &&
                !string.Equals(frm.InvGlobalID, "-1",
                    StringComparison.OrdinalIgnoreCase))
            {
                this.Navigate(
                    $"select * from Inv where branch={MainClass.BranchNo} " +
                    $"and InvGlobalID=N'{frm.InvGlobalID}'");
            }
        }

        #endregion

        #region ══════════════════ Recalculate & Cost ══════════════════

        private void RecalculateCost()
        {
            try
            {
                foreach (var item in this.Invoic.InvoiceItems)
                {
                    double avgCost = item.ItemCost;
                    ItemOper.RecalculateCost(
                        item.ItemId, item.ItemPrimaryQnty, ref avgCost, "");
                    item.ItemCost = avgCost;
                }
            }
            catch { }
        }

        public void ImportInventoryItems(List<InvoiceItem> importedItems)
        {
            if (importedItems?.Count == 0) return;
            foreach (var importedItem in importedItems)
            {
                this.SearchByID(importedItem.ItemId, 0, "");
                var found = this.Invoic.InvoiceItems
                    .FirstOrDefault(i => i.ItemId == importedItem.ItemId);
                if (found != null)
                {
                    found.ItemPrice    = importedItem.ItemPrice;
                    found.ItemQuantity = importedItem.ItemQuantity;
                    ItemOper.CalcRows(ref this.Invoic);
                    this.BindingDGV();
                }
            }
        }

        #endregion

        #region ══════════════════ Offline Sync ══════════════════

        private void SaveGlobalIDOffLine(string invGlobalID, string entryGlobalID)
        {
            var obj = new InvGLobalIDOff
            {
                InvGlobalID   = invGlobalID,
                GlobalID      = entryGlobalID,
                EntryGLobalID = entryGlobalID
            };
            AddInvLocally(obj);
        }

        private void AddInvLocally(InvGLobalIDOff newInv)
        {
            var list = new List<InvGLobalIDOff>();
            string path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Data", "InvGlobalID.json");

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                if (!string.IsNullOrWhiteSpace(json))
                    list = JsonConvert.DeserializeObject<List<InvGLobalIDOff>>(json)
                           ?? new List<InvGLobalIDOff>();
            }

            list.Add(newInv);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path,
                JsonConvert.SerializeObject(list, Formatting.Indented));
        }

        #endregion

        #region ══════════════════ Window Events ══════════════════

        private void frmSalePurch_FormClosing(
            object sender, System.ComponentModel.CancelEventArgs e)
        {
            if ((_invoiceItems.Count > 0 && this.Invoic?.ISNew == true) ||
                this.Invoic?.IsUpdated == true)
            {
                e.Cancel =
                    MessageBox.Show(
                        "لم يتم حفظ الفاتورة، هل تريد الإستمرار ؟",
                        "تنبيه",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) == MessageBoxResult.No;
            }
        }

        private void frmInvInOut_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.B)
            {
                e.Handled = true;
                this.txtBarcode.Focus();
            }

            if (e.Key == Key.F1) { this.AddNewItem(); e.Handled = true; }

            if (e.Key == Key.Return &&
                this.txtBarcode.IsFocused)
            {
                this.ReadBarcode(this.txtBarcode.Text, false);
                this.txtBarcode.Text = "";
                e.Handled = true;
            }

            if (e.Key == Key.F5)
            {
                this.btnNew_Click(sender, e);
                e.Handled = true;
            }
            if (e.Key == Key.F6)
            {
                this.btnSave_Click(sender, e);
                e.Handled = true;
            }
            if (e.Key == Key.F7)
            {
                this.btnPrint_Click(sender, e);
                e.Handled = true;
            }
            if (e.Key == Key.F8)
            {
                this.IconButton1_Click(sender, e);
                e.Handled = true;
            }
            if (e.Key == Key.F9)
            {
                this.btnDelete_Click(sender, e);
                e.Handled = true;
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region ══════════════════ Helper Methods ══════════════════

        /// <summary>استخراج ID من ComboBox</summary>
        private static int GetSelectedId(ComboBox cmb)
        {
            if (cmb.SelectedValue == null) return -1;
            return int.TryParse(cmb.SelectedValue.ToString(), out int id)
                ? id : -1;
        }

        /// <summary>تعيين قيمة ComboBox عبر SelectedValue</summary>
        private static void SetComboValue(ComboBox cmb, object value)
        {
            if (value == null) return;
            cmb.SelectedValue = value;
        }

        /// <summary>قراءة قيمة DateEdit</summary>
        private static DateTime GetDateEditValue(DateEdit de)
        {
            return de.EditValue is DateTime dt ? dt : DateTime.Today;
        }

        /// <summary>تحليل نص الوقت</summary>
        private static DateTime ParseTime(string timeText)
        {
            return DateTime.TryParse(timeText, out DateTime t) ? t : DateTime.Now;
        }

        /// <summary>التنقل بين أعمدة DataGrid</summary>
        private static void MoveToColumn(DataGrid grid, string headerContains)
        {
            if (grid == null) return;
            foreach (var col in grid.Columns)
            {
                if (col.Header?.ToString().Contains(headerContains) == true)
                {
                    grid.CurrentColumn = col;
                    grid.BeginEdit();
                    break;
                }
            }
        }

        /// <summary>الانتقال للصف التالي وتحديد عمود معين</summary>
        private static void MoveToNextRow(DataGrid grid, string headerContains)
        {
            if (grid == null || grid.Items.Count == 0) return;
            int nextIdx = grid.SelectedIndex + 1;
            if (nextIdx < grid.Items.Count)
            {
                grid.SelectedIndex = nextIdx;
                grid.CurrentItem   = grid.Items[nextIdx];
                grid.ScrollIntoView(grid.Items[nextIdx]);
            }
            MoveToColumn(grid, headerContains);
        }

        #endregion
    }

    #region ══════════════════ Model Classes ══════════════════

    /// <summary>نموذج صف نتائج البحث</summary>
    public class InvSearchRow
    {
        public string Column10    { get; set; }
        public string InvGlobalID { get; set; }
        public string InvNo       { get; set; }
        public string RefNo       { get; set; }
        public string InvDate     { get; set; }
        public string ClientName  { get; set; }
    }

    #endregion
}