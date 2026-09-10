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
using System.Windows.Threading;
using AuditorAPI.Models;
using AuditorAPI.Models.DGVmodels;
using SmartAuditERP.Form_WPF;
using Newtonsoft.Json;
using log4net;
using DevExpress.Xpf.Core;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;
using Button = System.Windows.Controls.Button;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInvInputOutput : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;

        public int InvType;
        public int ProcType;
        public int EntryType;

        private Print print;
        private InvoiceObj InvObj;
        private InvoiceDGV Invoic;

        private string StyleFile;
        private string StyleFile1;
        private string StyleFile2;
        private string StyleFolder;

        private static readonly ILog Logger =
            LogManager.GetLogger(Environment.MachineName);

        public FormPermission FrmPermission;

        private DispatcherTimer _timer;

        // مصدر بيانات الجدول الرئيسي
        private ObservableCollection<InvoiceItem> _invoiceItems =
            new ObservableCollection<InvoiceItem>();

        // مصدر بيانات جدول البحث
        private ObservableCollection<InvoiceSearchResult> _searchResults =
            new ObservableCollection<InvoiceSearchResult>();

        // الصنف المحدد حالياً في الجدول
        private InvoiceItem _selectedItem;

        #endregion

        #region Constructor

        public frmInvInputOutput()
        {
            InitializeComponent();

            conn  = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();

            InvType   = 4;
            ProcType  = 1;
            print     = new Print(InvType);
            InvObj    = new InvoiceObj(InvType, ProcType);
            Invoic    = new InvoiceDGV();

            StyleFile  = Path.Combine(MainClass.ReportsPath,
                             "Styles\\InvInputSaveLayout.xml");
            StyleFile1 = Path.Combine(MainClass.ReportsPath,
                             "Styles\\InvOutputSaveLayout.xml");
            StyleFile2 = Path.Combine(MainClass.ReportsPath,
                             "Styles\\InvOrderItemsSaveLayout.xml");
            StyleFolder = Path.Combine(MainClass.ReportsPath, "Styles");

            FrmPermission = new FormPermission();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData1();

            InvObj = new InvoiceObj(InvType, ProcType);

            DateTime now = DateTime.Now;
            txtDate.EditValue    = now;
            txtToDate.EditValue  = now;
            txtRefDate.EditValue = now;
            txtFromDate.EditValue = now;
            txtInvTime.Text      = now.ToString("hh:mm:ss tt");

            if (cmbStore.Items.Count > 0)
                cmbStore.SelectedIndex = 0;

            txtInvTaxPer.Text = InvObj.VAT.ToString();

            if (ProcType == 2)
                btnInsertLinkedInv.Visibility = Visibility.Visible;

            FrmPermission.ApplyFrmPermission(this);
            Common.ApplyEditAddPermission(this, true, FrmPermission);

            ResetInvoice();

            // Timer للوقت الحالي
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer1_Tick;
            _timer.Start();

            txtBarcode.Focus();
        }

        private void Window_Closing(object sender,
            System.ComponentModel.CancelEventArgs e)
        {
            bool hasUnsaved =
                ((GridControl1.Items.Count > 0) && Invoic.ISNew)
                || Invoic.IsUpdated;

            if (hasUnsaved)
            {
                var result = DXMessageBox.Show(
                    "لم يتم حفظ الفاتورة، هل تريد الاستمرار؟",
                    "تنبيه",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                e.Cancel = result == MessageBoxResult.No;
            }
        }

        #endregion

        #region Timer

        private void Timer1_Tick(object sender, EventArgs e)
        {
            lblCurrentTime.Text = DateTime.Now.ToString("hh:mm:ss tt");
        }

        #endregion

        #region Reset / Load

        private void ResetInvoice()
        {
            LoadInvNo();

            Invoic.InvoiceType     = InvObj.InvType;
            Invoic.ProcType        = ProcType;
            Invoic.VATperc         = InvObj.VAT;
            Invoic.Currency        = InvObj.Currency;
            Invoic.InvoiceCode     = InvObj.InvoiceCode;
            Invoic.AdditionalCost  = 0m;
            Invoic.InvoiceStatus   = 3;
            Invoic.Branch          = MainClass.BranchNo;
            Invoic.InvertoryImpact = InvObj.InvertoryImpact;
            Invoic.ExtraVATPerc    = InvObj.AdditionalTax;
            Invoic.PriceIncVAT     = InvObj.PriceIncVAT;
            Invoic.InvAccCode      = InvObj.InvAcc;
            Invoic.InvCCcode       = "-1";
            Invoic.CreateDate      = DateTime.Now;

            if (txtDate.EditValue is DateTime dt)
                Invoic.InvDate = dt;

            Invoic.RefDate  = DateTime.Now;
            Invoic.PayType  = 1;
            Invoic.ISNew    = true;
            Invoic.IsDeleted = false;
            Invoic.IsLoaded  = false;
            Invoic.IsPrinted = false;
            Invoic.ReffNo   = "-1";
            Invoic.Pricing  = InvObj.Pricing;
            Invoic.User     = MainClass.EmpNo;

            if (cmbStore.SelectedValue != null &&
                int.TryParse(cmbStore.SelectedValue.ToString(), out int storeId))
            {
                Invoic.Store = storeId;
                Invoic.InvertoryName = cmbStore.Text;
            }

            if (cmbClient.SelectedValue != null &&
                int.TryParse(cmbClient.SelectedValue.ToString(), out int custId))
                Invoic.Customer = custId;

            RefreshGrid();
        }

        private void LoadData1()
        {
            LoadSafes();
            LoadCustomers();
            LoadInvNo();
            LoadSalesMen();
        }

        public void LoadSafes()
        {
            try
            {
                DataTable dt = LoadData.Invertories(MainClass.EmpNo);
                cmbStore.ItemsSource       = dt.DefaultView;
                cmbStore.DisplayMemberPath = "name";
                cmbStore.SelectedValuePath = "id";
                cmbStore.SelectedIndex     = -1;

                // للبحث
                cmbClientSrch.ItemsSource       = dt.DefaultView;
                cmbClientSrch.DisplayMemberPath  = "name";
                cmbClientSrch.SelectedValuePath  = "id";
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المستودعات: " + ex.Message);
            }
        }

        public void LoadSalesMen()
        {
            try
            {
                DataTable dt = LoadData.SalesMen();
                cmbSalesMen.ItemsSource       = dt.DefaultView;
                cmbSalesMen.DisplayMemberPath = "name";
                cmbSalesMen.SelectedValuePath = "id";
                cmbSalesMen.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المندوبين: " + ex.Message);
            }
        }

        private void LoadCustomers()
        {
            try
            {
                int type = 1;
                if (InvType == 4 || InvType == 14)
                {
                    type = 2;
                    lblClient.Text     = "اسم المورد";
                    lblClientSrch.Text = "المورد";
                    if (Column1 != null)
                        Column1.Header = "المورد";
                }

                DataTable dt = LoadData.Customers(type);

                cmbClient.ItemsSource       = dt.DefaultView;
                cmbClient.DisplayMemberPath = "name";
                cmbClient.SelectedValuePath = "id";
                cmbClient.SelectedIndex     = -1;

                cmbClientSrch.ItemsSource       = dt.DefaultView;
                cmbClientSrch.DisplayMemberPath = "name";
                cmbClientSrch.SelectedValuePath = "id";
                cmbClientSrch.SelectedIndex     = -1;

                if (cmbClient.Items.Count > 0)
                    cmbClient.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل العملاء: " + ex.Message);
            }
        }

        private void LoadInvNo()
        {
            Invoic.InvoiceNo =
                InvoiceOper.InvoiceNo(InvType, ProcType, InvObj.Prefixe);
            txtNo.Text = Invoic.InvoiceNo.ToString();

            Invoic.InvCombinedId =
                MainClass.BranchCode
                + InvObj.InvoiceCode
                + ProcType.ToString()
                + Invoic.InvoiceNo.ToString();

            txtNo.Foreground = System.Windows.Media.Brushes.Firebrick;
        }

        #endregion

        #region Grid Refresh (BindingDGV)

        private void RefreshGrid()
        {
            _invoiceItems = new ObservableCollection<InvoiceItem>(
                Invoic.InvoiceItems ?? new List<InvoiceItem>());
            GridControl1.ItemsSource = _invoiceItems;

            int rowCount = _invoiceItems.Count;

            txtTirmsNo.Text      = rowCount.ToString();
            txtTotQuan.Text      = Invoic.TotalQty.ToString(InvObj.DigitsNo);
            txtSumVal.Text       = Invoic.SumPrice.ToString(InvObj.DigitsNo);
            txtTotDiscount.Text  = Invoic.TotDiscount.ToString(InvObj.DigitsNo);
            txtInvDiscVal.Text   = Invoic.InvDiscount.ToString(InvObj.DigitsNo);
            txtInvDiscPerc.Text  = InvoiceOper
                .GetDiscountPercentage(Invoic.SumPrice, Invoic.InvDiscount)
                .ToString(InvObj.DigitsNo);
            txtNetWithoutVAT.Text = Invoic.Total.ToString(InvObj.DigitsNo);
            txtTotVAT.Text       = Invoic.VAT.ToString(InvObj.DigitsNo);
            txtNet.Text          = Invoic.Net.ToString(InvObj.DigitsNo);

            // تحديد آخر صف
            if (rowCount > 0)
                GridControl1.ScrollIntoView(_invoiceItems.Last());
        }

        private void BindingControls()
        {
            try
            {
                txtNo.Text = Invoic.InvoiceNo.ToString();

                txtDate.EditValue    = Invoic.InvDate;
                txtRefDate.EditValue = Invoic.RefDate;

                txtInvTime.Text = Invoic.InvTime.ToString("hh:mm:ss tt");

                if (cmbStore.ItemsSource != null)
                    cmbStore.SelectedValue = Invoic.Store;

                txtNote.Text   = Invoic.InvNote;
                txtRefNo.Text  = Invoic.ReffNo;

                if (cmbClient.ItemsSource != null)
                    cmbClient.SelectedValue = Invoic.Customer;

                if (Invoic.Saleman != 0 && cmbSalesMen.ItemsSource != null)
                    cmbSalesMen.SelectedValue = Invoic.Saleman;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في ربط البيانات: " + ex.Message);
            }
        }

        #endregion

        #region Item Search

        private void SearchByName()
        {
            try
            {
                var selectedRow = GridControl1.SelectedItem as InvoiceItem;
                string itemName = selectedRow?.ItemName ?? "";

                if (!string.IsNullOrEmpty(itemName))
                {
                    using (var adapter = new SqlDataAdapter(
                        "SELECT name, id FROM Items WHERE IS_Deleted=0 AND name=N'"
                        + itemName + "'", conn))
                    {
                        var dt = new DataTable();
                        adapter.Fill(dt);
                        if (dt.Rows.Count > 0)
                        {
                            int id = Convert.ToInt32(dt.Rows[0]["id"]);
                            SearchByID(id, 0, "");
                        }
                        else
                        {
                            SearchByCode(itemName, 0);
                        }
                    }
                }
                else
                {
                    AddNewItem();
                }
            }
            catch { }
        }

        private void SearchByCode(string itemCode, int itemUnit)
        {
            try
            {
                using (var adapter = new SqlDataAdapter(
                    "SELECT name, id FROM Items WHERE IS_Deleted=0 AND Code=N'"
                    + itemCode + "'", conn))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        int id = Convert.ToInt32(dt.Rows[0]["id"]);
                        SearchByID(id, itemUnit, "");
                    }
                    else if (!string.IsNullOrWhiteSpace(itemCode))
                    {
                        ReadBarcode(itemCode, true);
                    }
                    else
                    {
                        AddNewItem();
                    }
                }
            }
            catch { }
        }

        private void SearchByID(int itemId, int unitId, string itemBarcode)
        {
            try
            {
                new ItemOper().GetItemByID(itemId, unitId,
                    ref Invoic, false, itemBarcode, "");
                ItemOper.CalcRows(ref Invoic);
                RefreshGrid();
            }
            catch { }
        }

        private void ReadBarcode(string itemBarcode, bool isMultiText)
        {
            try
            {
                int    foundItemId  = 0;
                int    foundUnitId  = 0;
                decimal foundPrice  = 0;
                decimal foundQty    = 0;

                ItemOper.SearchForBarcode(itemBarcode.Trim(),
                    ref foundItemId, ref foundUnitId,
                    ref foundPrice, ref foundQty, isMultiText);

                if (foundItemId > 0)
                    SearchByID(foundItemId, foundUnitId, itemBarcode);
                else
                    AddNewItem();
            }
            catch { }
        }

        private void AddNewItem()
        {
            var dlg = new frmItemsSrch();
            MainClass.ApplyPermissionToForm(dlg);
            MainClass.DoApplyUserSett(dlg);
            dlg.sql    = "SELECT id,name,nameEN,sale_price,unit FROM Items WHERE IS_Deleted=0 ORDER BY id";
            dlg.search = "SELECT id,name,nameEN,sale_price,unit FROM Items";
            dlg.StoreId = cmbStore.SelectedValue != null
                ? Convert.ToInt32(cmbStore.SelectedValue) : 0;

            dlg.ShowDialog();

            if (dlg.ISDone && dlg.ItemId > 0)
            {
                foreach (int id in dlg.Itemlist)
                    SearchByID(id, 0, "");
            }
        }

        #endregion

        #region Navigation

        public void Navigate(string sqlStr)
        {
            CLR();
            InvoiceOper.BindingInvoice(ref Invoic, sqlStr);
            if (Invoic != null)
            {
                txtNo.Background =
                    System.Windows.Media.Brushes.WhiteSmoke;
                RefreshGrid();
                BindingControls();
            }
            else
            {
                Invoic = new InvoiceDGV();
            }
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                Navigate("SELECT TOP 1 * FROM Inv WHERE inv_type="
                    + InvType + " AND proc_type=" + ProcType
                    + " AND IS_Deleted=0 AND branch="
                    + MainClass.BranchNo + " ORDER BY id ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
            {
                int.TryParse(txtNo.Text, out int currentNo);
                Navigate("SELECT TOP 1 * FROM Inv WHERE inv_type="
                    + InvType + " AND proc_type=" + ProcType
                    + " AND IS_Deleted=0 AND id<" + currentNo
                    + " AND branch=" + MainClass.BranchNo
                    + " ORDER BY id DESC");
            }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
            {
                int.TryParse(txtNo.Text, out int currentNo);
                Navigate("SELECT TOP 1 * FROM Inv WHERE inv_type="
                    + InvType + " AND proc_type=" + ProcType
                    + " AND IS_Deleted=0 AND id>" + currentNo
                    + " AND branch=" + MainClass.BranchNo
                    + " ORDER BY id ASC");
            }
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                Navigate("SELECT TOP 1 * FROM Inv WHERE inv_type="
                    + InvType + " AND proc_type=" + ProcType
                    + " AND IS_Deleted=0 AND branch="
                    + MainClass.BranchNo + " ORDER BY id DESC");
        }

        #endregion

        #region Save / Delete / New

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                CLR();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!Common.CheckProiedAcc(
                    txtDate.EditValue is DateTime d ? d : DateTime.Now))
            {
                DXMessageBox.Show(
                    "لا يمكن أن يكون التاريخ خارج الفترة المحاسبية",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Invoic.IsPrinted = false;

            double netVal = 0;
            double.TryParse(txtNet.Text, out netVal);

            if (netVal <= 0 && InvType != 9)
            {
                DXMessageBox.Show("يجب أن يكون الصافي أكبر من الصفر",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveAndPrint();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!Invoic.ISNew && InvoiceOper.DeleteInvoice(Invoic))
            {
                Logger.Info("تم حذف الفاتورة رقم " + Invoic.InvoiceNo
                    + " بواسطة: " + MainClass.UserName);
                CLR();
            }
        }

        private void btnSavePrint_Click(object sender, RoutedEventArgs e)
        {
            if (Invoic.ISNew && Invoic.ProcType == 2
                && InvoiceOper.IsPreviousReturned(
                    Invoic.InvGlobalID, Invoic.InvGlobalID))
            {
                DXMessageBox.Show("الفاتورة تم إرجاعها سابقاً");
                return;
            }
            Invoic.IsPrinted = true;
            SaveAndPrint();
        }

        private async void SaveAndPrint()
        {
            var invoiceOper = new InvoiceOper();
            Invoice invoice = invoiceOper.MappingInvoice(ref Invoic);
            Entry   entry   = null;

            Inventory.UpdateItemStock(invoice);
            bool saved = invoiceOper.SaveInvoice(invoice, entry, Invoic.ISNew);

            if (saved)
            {
                Logger.Info((Invoic.ISNew ? "تم حفظ " : "تم تعديل ")
                    + InvoiceOper.GetInvoiceType(
                        (int)invoice.InvoiceType,
                        invoice.ProcType, invoice.PayType, 2)
                    + " برقم " + invoice.InvoiceNo
                    + " بواسطة: " + MainClass.UserName);
            }

            if (saved && Invoic.IsPrinted && print.PrintNo > 0)
                RptPrint(invoice, 1);

            if (saved && Sync.ActiveSync && InvObj.SyncInv && Sync.SyncType > 0)
            {
                if (!InvObj.SyncEntry) entry = null;
                await invoiceOper.SyncInvoice(invoice, entry, Invoic.ISNew);
            }

            if (!saved) return;

            RecalculateCost();

            var msgForm = new frmSavedMsg();
            if (!Invoic.ISNew)
                msgForm.lblSave.Text = "تم حفظ التعديلات بنجاح...";

            msgForm.ShowDialog();

            if (msgForm.Pressed == 1)
            {
                CLR();
            }
            else if (msgForm.Pressed == 2)
            {
                Invoic.ISNew      = false;
                Invoic.IsUpdated  = false;
                txtNo.Background  = System.Windows.Media.Brushes.WhiteSmoke;
            }
            else if (msgForm.Pressed == 3)
            {
                CLR();
                Close();
            }
            else
            {
                CLR();
            }
        }

        private void CLR()
        {
            MainClass.CLRForm(this);

            if (cmbStore.Items.Count > 0)
                cmbStore.SelectedIndex = 0;

            DateTime now = DateTime.Now;
            txtDate.EditValue     = now;
            txtToDate.EditValue   = now;
            txtRefDate.EditValue  = now;
            txtFromDate.EditValue = now;
            txtInvTime.Text       = now.ToString("hh:mm:ss tt");

            if (cmbClient.Items.Count > 0)
                cmbClient.SelectedIndex = 0;

            LoadInvNo();
            txtBarcode.Focus();

            Common.ApplyEditAddPermission(this, true, FrmPermission);

            Invoic = null;
            GridControl1.ItemsSource = null;
            Invoic = new InvoiceDGV();
            ResetInvoice();
        }

        private bool CheckBeforeClear()
        {
            bool hasUnsaved =
                ((_invoiceItems.Count > 0) && Invoic.ISNew)
                || Invoic.IsUpdated;

            if (hasUnsaved)
            {
                var res = DXMessageBox.Show(
                    "لم يتم حفظ الفاتورة، هل تريد جديد؟",
                    "تنبيه",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                return res == MessageBoxResult.No;
            }
            return false;
        }

        #endregion

        #region Print / View

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (Invoic.IsPrinted)
                RptPrint(new InvoiceOper().MappingInvoice(ref Invoic), 1);
            else
                DXMessageBox.Show("لا يمكن طباعة الفاتورة قبل الحفظ..");
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            RptPrint(new InvoiceOper().MappingInvoice(ref Invoic), 2);
        }

        private void RptPrint(Invoice inv, int type)
        {
            if (_invoiceItems.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات بالجدول");
                return;
            }
            if (string.IsNullOrEmpty(print.RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير");
                return;
            }
            if (string.IsNullOrEmpty(print.RptName))
            {
                if (InvType == 4 || InvType == 5)
                    print.RptName = "RptInvInOutput.repx";
                else if (InvType == 14)
                    print.RptName = "RptInvOrderItems.repx";
            }

            string fullPath = Path.Combine(print.RptUrl, print.RptName);
            if (!Directory.Exists(print.RptUrl) || !File.Exists(fullPath))
            {
                DXMessageBox.Show("مسار التقارير غير موجود أو تم تعديله",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(print.defPrinter))
                print.defPrinter = MainClass.ReportsPrinter;

            print.Printing(type, print.BindToData(inv),
                print.RptUrl, print.RptName,
                print.defPrinter, print.kitchenprinter, print.PrintNo);
        }

        #endregion

        #region Discount

        private void txtInvDiscVal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) CalcDiscount();
        }

        private void txtInvDiscVal_LostFocus(object sender, RoutedEventArgs e)
        {
            try { CalcDiscount(); } catch { }
        }

        private void txtInvDiscPerc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) CalcDiscountPerc();
        }

        private void txtInvDiscPerc_LostFocus(object sender, RoutedEventArgs e)
        {
            try { CalcDiscountPerc(); } catch { }
        }

        private void CalcDiscount()
        {
            if (!string.IsNullOrWhiteSpace(txtInvDiscVal.Text))
            {
                double.TryParse(txtInvDiscVal.Text, out double val);
                Invoic.InvDiscount = val;
            }
            else
            {
                Invoic.InvDiscount = 0;
                txtInvDiscPerc.Text = "0";
            }
            ItemOper.CalcRows(ref Invoic);
            RefreshGrid();
        }

        private void CalcDiscountPerc()
        {
            if (!string.IsNullOrWhiteSpace(txtInvDiscPerc.Text))
            {
                double.TryParse(txtInvDiscPerc.Text, out double perc);
                Invoic.InvDiscount = Convert.ToDouble(
                    InvoiceOper.GetInvDiscountByPercentage(
                        Invoic.SumPrice, perc));
            }
            else
            {
                txtInvDiscPerc.Text = "0";
                Invoic.InvDiscount  = 0;
            }
            ItemOper.CalcRows(ref Invoic);
            RefreshGrid();
        }

        private void NumericOnly_PreviewTextInput(object sender,
            TextCompositionEventArgs e)
        {
            e.Handled = !IsNumericChar(e.Text);
        }

        private bool IsNumericChar(string text)
        {
            foreach (char c in text)
                if (!char.IsDigit(c) && c != '.') return false;
            return true;
        }

        #endregion

        #region Client / Store / SalesMen

        private void cmbClient_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbClient.SelectedValue != null)
                {
                    int.TryParse(
                        cmbClient.SelectedValue.ToString(), out int custId);
                    Invoic.Customer = custId;
                    ShowCustBalance();
                }
                else
                {
                    Invoic.Customer = -1;
                }
            }
            catch { }
        }

        private void cmbClient_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                SrchByNameClient();
            }
        }

        private void cmbStore_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (cmbStore.SelectedValue != null)
            {
                int.TryParse(cmbStore.SelectedValue.ToString(),
                    out int storeId);
                Invoic.Store = storeId;
                Invoic.InvertoryName =
                    Common.GetStoreName(storeId);
            }
            else
            {
                Invoic.Store = -1;
            }
        }

        private void cmbSalesMen_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (cmbSalesMen.SelectedValue != null)
            {
                int.TryParse(cmbSalesMen.SelectedValue.ToString(),
                    out int salesId);
                Invoic.Saleman = salesId;
            }
        }

        private void ShowCustBalance()
        {
            try
            {
                using var adapter1 = new SqlDataAdapter(
                    "SELECT Code FROM Accounts_Index WHERE AName=N'"
                    + cmbClient.Text + "' " + Accounting.BranchCondition,
                    conn1);
                var dt1 = new DataTable();
                adapter1.Fill(dt1);
                if (dt1.Rows.Count == 0) return;

                string accCode = dt1.Rows[0][0].ToString();
                string branchCond = MainClass.BranchNo != -1
                    ? "Entry.branch=" + MainClass.BranchNo
                      + " AND Entry_sub.branch=" + MainClass.BranchNo + " AND "
                    : "";

                using var adapter2 = new SqlDataAdapter(
                    "SELECT SUM(Entry_sub.dept) AS dept,"
                    + "SUM(Entry_sub.credit) AS credit FROM Entry,Entry_sub WHERE "
                    + branchCond
                    + "Entry.IS_Deleted=0 AND Entry.state=1"
                    + " AND Entry.GlobalId=Entry_sub.EntryGlobalId"
                    + " AND Entry_sub.acc_no=" + accCode, conn1);
                var dt2 = new DataTable();
                adapter2.Fill(dt2);

                if (dt2.Rows.Count > 0)
                {
                    double dept   = Convert.ToDouble(dt2.Rows[0]["dept"]);
                    double credit = Convert.ToDouble(dt2.Rows[0]["credit"]);

                    if (dept > credit)
                        txtBalance.Text = $"{Math.Round(dept - credit, 3):N3}";
                    else if (credit > dept)
                        txtBalance.Text = $"{Math.Round(credit - dept, 3):N3} دائن";
                    else
                        txtBalance.Text = "0";
                }
            }
            catch { }
        }

        private void SrchByNameClient()
        {
            try
            {
                int type = (InvType == 4 || InvType == 14) ? 2 : 1;
                using var adapter = new SqlDataAdapter(
                    "SELECT id,name FROM Customers WHERE IS_Deleted=0"
                    + " AND name=N'" + cmbClient.Text + "'"
                    + " AND (type=" + type + " OR type=3)", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    cmbClient.SelectedValue = Convert.ToInt32(dt.Rows[0]["id"]);
                    Invoic.Customer = Convert.ToInt32(cmbClient.SelectedValue);
                }
                else
                {
                    AddNewClient();
                }
            }
            catch { }
        }

        private void AddNewClient()
        {
            var dlg = new frmSrchClient();
            MainClass.ApplyPermissionToForm(dlg);
            MainClass.DoApplyUserSett(dlg);

            dlg.Type = (InvType == 4 || InvType == 14) ? 2 : 1;

            if (cmbClient.Text != "عميل عام" && cmbClient.Text != "مورد عام")
                dlg.txtClientName.Text = cmbClient.Text;

            dlg.ShowDialog();

            if (!string.IsNullOrEmpty(dlg.Clientname))
            {
                LoadCustomers();
                cmbClient.SelectedValue = dlg.ClientId;
                Invoic.Customer =
                    Convert.ToInt32(cmbClient.SelectedValue);
            }
        }

        #endregion

        #region Barcode / AddItem Buttons

        private void txtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ReadBarcode(txtBarcode.Text, false);
                txtBarcode.Text = "";
                e.Handled = true;
            }
        }

        private void btnAddNewItem_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new frmItems();
            dlg.Activate();
            dlg.ShowDialog();
        }

        private void btnCustAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new frmCustomers();
                MainClass.ApplyPermissionToForm(dlg);
                MainClass.DoApplyUserSett(dlg);

                if (InvType == 2) { dlg.Title = "تعريف عميل"; dlg.Type = 1; }
                else if (InvType == 1 || InvType == 4 || InvType == 14)
                { dlg.Title = "تعريف مورد"; dlg.Type = 2; }

                dlg.ShowDialog();
                if (dlg.isDone) LoadCustomers();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void cmbAddSalesMen_Click(object sender, RoutedEventArgs e)
        {
            object prevVal = cmbSalesMen.SelectedValue;
            var dlg = new frmSalesMen();
            dlg.Activate();
            dlg.ShowDialog();
            LoadSalesMen();
            try
            {
                if (prevVal != null)
                    cmbSalesMen.SelectedValue = prevVal;
            }
            catch { }
        }

        private void btnZoom_Click(object sender, RoutedEventArgs e)
        {
            AddNewClient();
        }

        #endregion

        #region GridControl1 Events

        private void GridControl1_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            _selectedItem = GridControl1.SelectedItem as InvoiceItem;
            if (_selectedItem != null)
                ShowItemDetails(_selectedItem.ItemId);
        }

        private void GridControl1_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_invoiceItems.Count == 0) return;

            if (e.Key == Key.Delete)
            {
                var res = DXMessageBox.Show("هل أنت متأكد من الحذف؟",
                    "تأكيد", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.Yes && _selectedItem != null)
                {
                    Invoic.InvoiceItems.Remove(_selectedItem);
                    int idx = 1;
                    foreach (var item in Invoic.InvoiceItems)
                        item.ItemRowIndex = idx++;
                    ItemOper.CalcRows(ref Invoic);
                    RefreshGrid();
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Return)
            {
                // الانتقال للعمود التالي
                var col = GetFocusedColumnName();
                if (col == "ItemCode")
                    SearchByCode(
                        GetFocusedCellValue("ItemCode")?.ToString() ?? "", 0);
                else if (col == "ItemName")
                    SearchByName();
                else if (col == "UnitName" && _selectedItem != null)
                    ShowItemUnit(_selectedItem.ItemId,
                        _selectedItem.ItemRowIndex);
                e.Handled = true;
            }
            else if (e.Key == Key.F1)
            {
                AddNewItem();
                e.Handled = true;
            }
        }

        private void GridControl1_CellEditEnding(object sender,
    DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (e.Row.Item is not InvoiceItem editedItem) return;

            if (e.EditingElement is TextBox tb)
            {
                double.TryParse(tb.Text, out double val);

                // ✅ إصلاح خطأ 1: استخدام Binding مباشرة بدل .Path.Path
                string binding = "";
                if (e.Column is DataGridTextColumn col &&
                    col.Binding is System.Windows.Data.Binding b)
                {
                    binding = b.Path.Path;
                }

                switch (binding)
                {
                    case "ItemQuantity":
                        editedItem.ItemQuantity = val;
                        break;
                    case "ItemPrice":
                        editedItem.ItemPrice = val;
                        break;
                    case "ItemDiscount":
                        editedItem.ItemDiscount = val;
                        break;
                    case "ItemDiscountPerc":
                        editedItem.ItemDiscountPerc = val;
                        editedItem.ItemDiscount =
                            val / 100.0 * editedItem.ItemSumPrice;
                        break;
                    case "ItemCode":
                        SearchByCode(tb.Text, 0);
                        return;
                    case "ItemName":
                        SearchByName();
                        return;
                }
            }

            ItemOper.CalcRows(ref Invoic);
            RefreshGrid();
        }


        private void GridControl1_MouseDoubleClick(object sender,
            MouseButtonEventArgs e)
        {
            if (_selectedItem == null) return;
            var col = GetFocusedColumnName();

            if (col == "ItemPrice")
            {
                var dlg = new FrmCalcVAT
                {
                    ItemVAT = (int)Math.Round(_selectedItem.ItemVatPerc)
                };
                dlg.ShowDialog();
                if (dlg.Price > 0)
                {
                    _selectedItem.ItemPrice = dlg.Price;
                    ItemOper.CalcRows(ref Invoic);
                    RefreshGrid();
                }
            }
            else if (col == "UnitName")
            {
                ShowItemUnit(_selectedItem.ItemId,
                    _selectedItem.ItemRowIndex);
            }
            else if (col == "InvertoryName")
            {
                ShowItemInvertories(_selectedItem.ItemId,
                    _selectedItem.ItemRowIndex);
            }
            else if (col == "Description")
            {
                ItemOper.LoadItemDetails(Invoic, ref _selectedItem);
                ItemOper.CalcRows(ref Invoic);
                RefreshGrid();
            }
        }

        private void GridControl1_ContextMenuOpening(object sender,
            ContextMenuEventArgs e)
        {
            // القائمة السياقية معرّفة في XAML
        }

        private string GetFocusedColumnName()
        {
            // مساعد بسيط
            return "";
        }

        private object GetFocusedCellValue(string property)
        {
            return _selectedItem == null ? null
                : typeof(InvoiceItem).GetProperty(property)
                    ?.GetValue(_selectedItem);
        }

        #endregion

        #region Context Menu Handlers

        private void CtxShowItemCard_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            var dlg = new frmItems();
            MainClass.ApplyPermissionToForm(dlg);
            MainClass.DoApplyUserSett(dlg);
            dlg.ItemId = _selectedItem.ItemId;
            dlg.ShowDialog();
        }

        private void CtxAddNewProduct_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new frmItems();
            MainClass.ApplyPermissionToForm(dlg);
            MainClass.DoApplyUserSett(dlg);
            dlg.ShowDialog();
        }

        private void CtxShowItemUnits_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem != null)
                ShowItemUnit(_selectedItem.ItemId,
                    _selectedItem.ItemRowIndex);
        }

        private void CtxShowItemInvertory_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem != null)
                ShowItemInvertories(_selectedItem.ItemId,
                    _selectedItem.ItemRowIndex);
        }

        private void CtxAddCategory_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new frmItemCategory();
            MainClass.ApplyPermissionToForm(dlg);
            MainClass.DoApplyUserSett(dlg);
            dlg.ShowDialog();
        }

        private void CtxDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            var res = DXMessageBox.Show("هل أنت متأكد من حذف السجل؟",
                "تأكيد", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.Yes)
            {
                Invoic.InvoiceItems.Remove(_selectedItem);
                int idx = 1;
                foreach (var item in Invoic.InvoiceItems)
                    item.ItemRowIndex = idx++;
                ItemOper.CalcRows(ref Invoic);
                RefreshGrid();
            }
        }

        private void CtxItemProcess_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            var dlg = new frmRptItemsActivityDetailed
            {
                SelectedId = _selectedItem.ItemId
            };
            dlg.txtItemName.Text = _selectedItem.ItemName;
            dlg.txtItemCode.Text = _selectedItem.ItemCode;
            dlg.ShowResult();
            MainClass.ApplyPermissionToForm(dlg);
            MainClass.DoApplyUserSett(dlg);
            dlg.ShowDialog();
        }

        private void CtxItemLastActivity_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            var dlg = new frmRptItemsActivityDetailed();
            MainClass.ApplyPermissionToForm(dlg);
            MainClass.DoApplyUserSett(dlg);
            dlg.txtItemName.Text = _selectedItem.ItemName;
            dlg.txtItemCode.Text = _selectedItem.ItemCode;
            dlg.Show();
            dlg.LastProcess(_selectedItem.ItemId,
                _selectedItem.InvertoryId, -1, 1);
        }

        private void CtxClientActivity_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            var dlg = new frmRptItemsActivityDetailed();
            MainClass.ApplyPermissionToForm(dlg);
            MainClass.DoApplyUserSett(dlg);
            dlg.txtItemName.Text = _selectedItem.ItemName;
            dlg.txtItemCode.Text = _selectedItem.ItemCode;
            dlg.Show();
            if (cmbClient.SelectedIndex > -1)
            {
                int.TryParse(cmbClient.SelectedValue?.ToString(),
                    out int custId);
                dlg.LastProcess(_selectedItem.ItemId,
                    _selectedItem.InvertoryId, custId, 2);
            }
        }

        private void CtxItemCost_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;

            // ✅ إصلاح خطأ 2: AvgCost يُرجع double → تحويل لـ string
            double cost = ItemOper.AvgCost(_selectedItem.ItemId, MainClass.BranchNo);
            DXMessageBox.Show(cost.ToString("F2"), "تكلفة المادة",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CtxItemSerialNo_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            TemSerialNo();
        }

        #endregion

        #region Item Details

        private void ShowItemDetails(int itemId)
        {
            try
            {
                txtLastSalePrice.Text    = "";
                txtCompetitorPrice.Text  = "";
                txtCostAvrg.Text         = "";
                txtStock.Text            = "";
                txtItemBarcode.Text      = "";
                txtRecentPurchPrice.Text =
                    ItemOper.RecentPurchPrice(itemId).ToString();

                if (_selectedItem != null)
                {
                    txtCostAvrg.Text = Math.Round(
                        _selectedItem.ItemCost * _selectedItem.UnitEquality,
                        2).ToString();

                    if (_selectedItem.UnitEquality > 0)
                    {
                        txtStock.Text = _selectedItem.ValiableInvertory
                            .ToString();
                        txtStock.Foreground =
                            _selectedItem.ValiableInvertory >= 0
                            ? System.Windows.Media.Brushes.Green
                            : System.Windows.Media.Brushes.Firebrick;
                    }

                    txtPerUnit.Text      = _selectedItem.UnitEquality.ToString();
                    txtTotUnitQuan.Text  = _selectedItem.ItemPrimaryQnty.ToString();
                    txtItemBarcode.Text  = _selectedItem.ItemBarcode;
                }

                LoadPrices(itemId);
            }
            catch { }
        }

        private void LoadPrices(int itemId)
        {
            try
            {
                using var adapter = new SqlDataAdapter(
                    "SELECT * FROM ItemPrices WHERE Itemid=" + itemId
                    + " ORDER BY Proc_id DESC", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    txtLastSalePrice.Text   =
                        dt.Rows[0]["low_sale_price"].ToString();
                    txtCompetitorPrice.Text =
                        dt.Rows[0]["CompetitorPrice"].ToString();
                }
            }
            catch { }
        }

        private void ShowItemUnit(int itemId, int rowIndex)
        {
            try
            {
                var target = Invoic.InvoiceItems
                    .FirstOrDefault(i => i.ItemId == itemId
                                     && i.ItemRowIndex == rowIndex);
                if (target == null) return;

                var dlg = new frmItemUnits
                {
                    ItemId  = target.ItemId,
                    PreUnit = target.UnitID
                };
                dlg.ShowDialog();

                if (!string.IsNullOrEmpty(dlg.Unitname))
                {
                    target.UnitID   = dlg.UnitId;
                    target.UnitName = dlg.Unitname;
                    ItemOper.LoadUnitInf(ref target,
                        (int)Invoic.InvoiceType, Invoic.Pricing);
                    ItemOper.CalcRows(ref Invoic);
                    RefreshGrid();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء تحميل الوحدات: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowItemInvertories(int itemId, int rowIndex)
        {
            try
            {
                var target = Invoic.InvoiceItems
                    .FirstOrDefault(i => i.ItemId == itemId
                                     && i.ItemRowIndex == rowIndex);
                if (target == null) return;

                var dlg = new frmItemInvertory { ItemId = target.ItemId };
                dlg.ShowDialog();

                if (dlg.InvertoryId > 0)
                {
                    target.InvertoryId   = dlg.InvertoryId;
                    target.InvertoryName = dlg.InvertoryName;
                    target.ValiableInvertory =
                        Convert.ToDouble(dlg.ItemQty);
                    ItemOper.ISvalidQuantity(ref Invoic,
                        target.ItemId, target.ItemRowIndex, true);
                    ItemOper.CalcRows(ref Invoic);
                    RefreshGrid();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء تحميل المستودع: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TemSerialNo()
        {
            try
            {
                if (_selectedItem == null) return;
                var dlg = new frmItemSerialNo();
                dlg.lblItemName.Text = _selectedItem.ItemName;
                dlg.PanelInput.Visibility = Visibility.Collapsed;

                var dtSelected  = new DataTable();
                dtSelected.Columns.Add("DgvNo");
                dtSelected.Columns.Add("DgvSerialNo");

                var dtAvailable = new DataTable();
                dtAvailable.Columns.Add("DgvNo1");
                dtAvailable.Columns.Add("DgvSerialNo1");

                if (!Invoic.ISNew)
                {
                    using var a1 = new SqlDataAdapter(
                        "SELECT SerialNo FROM ItemSerialNo WHERE ItemId="
                        + _selectedItem.ItemId
                        + " AND InvGlobalID=N'" + Invoic.InvGlobalID + "'",
                        conn);
                    var dt1 = new DataTable();
                    a1.Fill(dt1);
                    for (int i = 0; i < dt1.Rows.Count; i++)
                        dtAvailable.Rows.Add(i + 1, dt1.Rows[i]["SerialNo"]);
                }

                // ✅ إصلاح خطأ 3: DataSource → ItemsSource في WPF
                dlg.GridControl2.ItemsSource = dtAvailable.DefaultView;
                dlg.operType = 3;
                dlg.LoadDgvEdit(dtSelected);
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message);
            }
        }

        #endregion

        #region RecalculateCost

        private void RecalculateCost()
        {
            try
            {
                foreach (var item in Invoic.InvoiceItems)
                {
                    double avgCost = item.ItemCost;
                    ItemOper.RecalculateCost(item.ItemId,
                        item.ItemPrimaryQnty, ref avgCost, "");
                    item.ItemCost = avgCost;
                }
            }
            catch { }
        }

        #endregion

        #region Search (Tab2)

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string fromDate = txtFromDate.EditValue is DateTime fd
                    ? fd.ToString("yyyy-MM-dd") : DateTime.Now.ToString("yyyy-MM-dd");
                string toDate = txtToDate.EditValue is DateTime td
                    ? td.ToString("yyyy-MM-dd") : DateTime.Now.ToString("yyyy-MM-dd");

                string custCond = "";
                if (!chkAll.IsChecked == true && cmbClientSrch.SelectedValue != null)
                    custCond = " AND c.id=" + cmbClientSrch.SelectedValue;

                string refCond = !string.IsNullOrWhiteSpace(txtSrchreffNo.Text)
                    ? " AND i.ref_no=N'" + txtSrchreffNo.Text + "'" : "";

                string noCond = !string.IsNullOrWhiteSpace(txtSrchNo.Text)
                    ? " AND i.id=" + txtSrchNo.Text : "";

                string sql =
                    "SELECT i.InvGlobalID, i.id AS InvoiceNo,"
                    + " i.ref_no AS ReffNo, i.date AS InvDate,"
                    + " c.name AS CustomerName"
                    + " FROM Inv i LEFT JOIN Customers c ON i.cust_id=c.id"
                    + " WHERE i.inv_type=" + InvType
                    + " AND i.proc_type=" + ProcType
                    + " AND i.IS_Deleted=0"
                    + " AND i.branch=" + MainClass.BranchNo
                    + " AND i.date BETWEEN '" + fromDate + "' AND '" + toDate + "'"
                    + custCond + refCond + noCond
                    + " ORDER BY i.id DESC";

                using var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                _searchResults.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    _searchResults.Add(new InvoiceSearchResult
                    {
                        InvGlobalID   = row["InvGlobalID"].ToString(),
                        InvoiceNo     = Convert.ToInt32(row["InvoiceNo"]),
                        ReffNo        = row["ReffNo"].ToString(),
                        InvDate       = row["InvDate"] != DBNull.Value
                                        ? Convert.ToDateTime(row["InvDate"])
                                        : DateTime.MinValue,
                        CustomerName  = row["CustomerName"].ToString()
                    });
                }

                dgvSrch.ItemsSource = _searchResults;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في البحث: " + ex.Message);
            }
        }

        private void dgvSrch_MouseDoubleClick(object sender,
            MouseButtonEventArgs e)
        {
            if (dgvSrch.SelectedItem is InvoiceSearchResult result
                && result.InvGlobalID != "-1")
            {
                TabControl1.SelectedIndex = 0;
                Navigate("SELECT * FROM Inv WHERE branch="
                    + MainClass.BranchNo
                    + " AND InvGlobalID=N'" + result.InvGlobalID + "'");
            }
        }

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbClientSrch.IsEnabled = !(chkAll.IsChecked == true);
            if (chkAll.IsChecked == true)
                cmbClientSrch.SelectedIndex = -1;
        }

        #endregion

        #region Invoice Search Button

        private void btnInvSrch_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new frmInvoiceSrch();
            dlg.cmbProcType.IsEnabled = false;
            MainClass.ApplyPermissionToForm(dlg);
            MainClass.DoApplyUserSett(dlg);
            dlg.ProcType = ProcType;
            dlg.InvType  = InvType;
            dlg.ShowDialog();

            if (dlg.ISDone && dlg.InvGlobalID != "-1")
                Navigate("SELECT * FROM Inv WHERE branch="
                    + MainClass.BranchNo
                    + " AND InvGlobalID=N'" + dlg.InvGlobalID + "'");
        }

        #endregion

        #region Import / Export / DGV Settings

        private void stripImport_Click(object sender, RoutedEventArgs e)
        {
            popupMenu.IsOpen = false;
            try
            {
                var res = DXMessageBox.Show(
                    "هل أنت متأكد من استيراد البيانات؟",
                    "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.No) return;

                var dlg = new frmImportDataGeneral();
                dlg.cmbInv.SelectedIndex = 1;
                dlg.cmbInv.Visibility = Visibility.Visible;
                dlg.cmbInv.IsEnabled = false;
                dlg.cmbDataTable.Visibility = Visibility.Collapsed;
                dlg.ShowDialog();

                if (dlg.ISDone && dlg.dtInvSel.Rows.Count > 0)
                {
                    foreach (DataRow row in dlg.dtInvSel.Rows)
                    {
                        SearchByCode(row["ItemCode"].ToString(),
                            Convert.ToInt32(row["unit"]));

                        var target = Invoic.InvoiceItems
                            .FirstOrDefault(i =>
                                i.ItemCode == row["ItemCode"].ToString()
                                && i.ItemRowIndex == Invoic.InvoiceItems.Count);

                        if (target != null)
                        {
                            target.Description  = row["description"].ToString();
                            target.ItemQuantity  =
                                Convert.ToDouble(row["quntity"]);

                            double price = 0;
                            double.TryParse(row["price"].ToString(),
                                out price);
                            if (price > 0)
                                target.ItemPrice = price;

                            ItemOper.CalcRows(ref Invoic);
                            RefreshGrid();
                        }
                    }
                    DXMessageBox.Show("تم الاستيراد بنجاح");
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message);
            }
        }

        private void stripExport_Click(object sender, RoutedEventArgs e)
        {
            popupMenu.IsOpen = false;
            try
            {
                if (!isInvExist())
                {
                    DXMessageBox.Show("يجب حفظ الفاتورة قبل التصدير");
                    return;
                }

                var res = DXMessageBox.Show(
                    "هل أنت متأكد من تصدير البيانات؟",
                    "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.No) return;

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx"
                };
                if (saveDialog.ShowDialog() != true) return;

                string filePath = saveDialog.FileName;

                // تصدير بسيط من DataGrid
                ExportDataGridToExcel(GridControl1, filePath);

                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(filePath)
                    { UseShellExecute = true });

                DXMessageBox.Show("تم حفظ الملف في: " + filePath);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التصدير: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportDataGridToExcel(DataGrid grid, string filePath)
        {
            // تصدير بسيط بصيغة CSV يمكن فتحها في Excel
            using var writer = new StreamWriter(filePath, false,
                System.Text.Encoding.UTF8);

            // ترويسات
            var headers = grid.Columns
                .Where(c => c.Visibility == Visibility.Visible)
                .Select(c => c.Header?.ToString() ?? "");
            writer.WriteLine(string.Join(",", headers));

            // بيانات
            foreach (InvoiceItem item in grid.Items)
            {
                var values = new List<string>
                {
                    item.ItemRowIndex.ToString(),
                    item.ItemCode,
                    item.ItemName,
                    item.Description,
                    item.UnitName,
                    item.ItemQuantity.ToString(),
                    item.ItemPrice.ToString(),
                    item.ItemSumPrice.ToString(),
                    item.ItemDiscount.ToString(),
                    item.ItemVat.ToString(),
                    item.ItemNetPrice.ToString(),
                    item.InvertoryName
                };
                writer.WriteLine(string.Join(",", values));
            }
        }

        private bool isInvExist()
        {
            try
            {
                int.TryParse(txtNo.Text, out int invNo);
                using var adapter = new SqlDataAdapter(
                    "SELECT id FROM Inv WHERE branch="
                    + MainClass.BranchNo + " AND inv_type=" + InvType
                    + " AND proc_type=" + ProcType + " AND id=" + invNo,
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0;
            }
            catch { return false; }
        }

        private void BtnSaveDgvSettings_Click(object sender, RoutedEventArgs e)
        {
            popupMenu.IsOpen = false;
            // في WPF لا نستخدم RestoreLayoutFromXml
            // يمكن حفظ عرض الأعمدة يدوياً إن لزم
            DXMessageBox.Show("تم حفظ مظهر الجدول");
        }

        private void BtnDefaultSetting_Click(object sender, RoutedEventArgs e)
        {
            popupMenu.IsOpen = false;
            try
            {
                if (InvType == 4 && File.Exists(StyleFile))
                    File.Delete(StyleFile);
                else if (InvType == 5 && File.Exists(StyleFile1))
                    File.Delete(StyleFile1);
                else if (InvType == 14 && File.Exists(StyleFile2))
                    File.Delete(StyleFile2);
            }
            catch { }
        }

        private void StripImportInv_Click(object sender, RoutedEventArgs e)
        {
            popupMenu.IsOpen = false;
            var dlg = new frmImportInvoice();
            dlg.ShowDialog();

            if (dlg.isDone && dlg.InvGID != "-1")
            {
                Navigate("SELECT * FROM Inv WHERE InvGlobalID=N'"
                    + dlg.InvGID + "'");
                Invoic.InvoiceType = (InvoiceType)InvType;
                Invoic.ProcType    = ProcType;
                Invoic.InvNote     = "";
                LoadInvNo();
                Invoic.ISNew = true;
                BindingControls();
            }
        }

        private void btnMenuOptions_Click(object sender, RoutedEventArgs e)
        {
            popupMenu.IsOpen = !popupMenu.IsOpen;
        }

        #endregion

        #region Insert Linked Invoice

        private void btnInsertLinkedInv_Click(object sender, RoutedEventArgs e)
        {
            if (ProcType == 2)
            {
                CLR();
                InvoiceOper.InsertFromInv(ref Invoic, 1);
                Invoic.ProcType = 2;
                Invoic.ReffNo   = Invoic.InvGlobalID;
                Invoic.RefDate  = Invoic.InvDate;
                Invoic.InvDate  = txtDate.EditValue is DateTime d
                    ? d : DateTime.Now;
                Invoic.ISNew    = true;
                ItemOper.CalcRows(ref Invoic);
                RefreshGrid();
                BindingControls();
            }
            else if (ProcType == 1)
            {
                InvoiceOper.InsertFromInv(ref Invoic, 1);
                Invoic.ISNew = true;
                ItemOper.CalcRows(ref Invoic);
                RefreshGrid();
                BindingControls();
            }
        }

        #endregion

        #region Inventory Order

        private void BtnInvertoryOrder_Click(object sender, RoutedEventArgs e)
        {
            new ItemOper().GetInventoryOrder(ref Invoic);
            RefreshGrid();
            BindingControls();
        }

        #endregion

        #region NumPad

        private void NumPad_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string digit)
            {
                // إرسال الرقم للحقل النشط
                if (Keyboard.FocusedElement is TextBox tb)
                {
                    if (digit == "." && tb.Text.Contains(".")) return;
                    tb.Text += digit;
                    tb.CaretIndex = tb.Text.Length;
                }
            }
        }

        private void cmdClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBox tb)
                tb.Text = "";
        }

        #endregion

        #region TextBox Events

        private void txtNote_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Invoic != null)
                Invoic.InvNote = txtNote.Text;
        }

        private void txtRefNo_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Invoic != null)
                Invoic.ReffNo = txtRefNo.Text;
        }

        private void txtDate_EditValueChanged(object sender, EventArgs e)
        {
            if (Invoic != null && txtDate.EditValue is DateTime dt)
                Invoic.InvDate = dt;
        }

        private void txtInvTime_GotFocus(object sender, RoutedEventArgs e)
        {
            txtInvTime.SelectAll();
        }

        private void txtInvTime_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DateTime.TryParse(txtInvTime.Text, out DateTime t))
            {
                if (Invoic != null)
                    Invoic.InvTime = t;
            }
        }

        private void txtSrchDgv_TextChanged(object sender,
            TextChangedEventArgs e)
        {
            // بحث في الجدول
            string keyword = txtSrchDgv.Text.ToLower();
            if (string.IsNullOrWhiteSpace(keyword))
            {
                GridControl1.ItemsSource = _invoiceItems;
                return;
            }
            var filtered = _invoiceItems
                .Where(i => (i.ItemName?.ToLower().Contains(keyword) == true)
                         || (i.ItemCode?.ToLower().Contains(keyword) == true))
                .ToList();
            GridControl1.ItemsSource = filtered;
        }

        private void ckZeroVAT_CheckedChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ckZeroVAT.IsChecked == true)
                    Invoic.VAT = 0;
                else
                    Invoic.VAT = InvObj.VAT;
                ItemOper.CalcRows(ref Invoic);
                RefreshGrid();
            }
            catch { }
        }

        #endregion

        #region Close

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Keyboard Shortcuts

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.B)
            {
                txtBarcode.Focus();
                e.Handled = true;
            }
            if (e.Key == Key.F1) { AddNewItem(); e.Handled = true; }
            if (e.Key == Key.F5) { btnNew_Click(null, null); e.Handled = true; }
            if (e.Key == Key.F6) { btnSave_Click(null, null); e.Handled = true; }
            if (e.Key == Key.F7) { btnPrint_Click(null, null); e.Handled = true; }
            if (e.Key == Key.F8) { btnView_Click(null, null); e.Handled = true; }
            if (e.Key == Key.F9) { btnDelete_Click(null, null); e.Handled = true; }
        }

        #endregion

        #region Public Methods

        public void ImportInventoryItems(List<InvoiceItem> importedItems)
        {
            if (importedItems == null || importedItems.Count == 0) return;
            foreach (var importedItem in importedItems)
            {
                SearchByID(importedItem.ItemId, 0, "");
                var target = Invoic.InvoiceItems
                    .FirstOrDefault(i => i.ItemId == importedItem.ItemId);
                if (target != null)
                {
                    target.ItemPrice    = importedItem.ItemPrice;
                    target.ItemQuantity = importedItem.ItemQuantity;
                    ItemOper.CalcRows(ref Invoic);
                    RefreshGrid();
                }
            }
        }

        public void PayBill()
        {
            if (InvoiceOper.PayInvoice(ref Invoic, InvObj))
                SaveAndPrint();
        }

        #endregion
    }

    #region Model Classes

    /// <summary>نتيجة بحث الفاتورة لجدول dgvSrch</summary>
    public class InvoiceSearchResult
    {
        public string   InvGlobalID  { get; set; }
        public int      InvoiceNo    { get; set; }
        public string   ReffNo       { get; set; }
        public DateTime InvDate      { get; set; }
        public string   CustomerName { get; set; }
    }

    #endregion
}