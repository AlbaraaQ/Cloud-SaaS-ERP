using AuditorAPI.Models;
using AuditorAPI.Models.DGVmodels;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using ETA_Invoice.Models;
using log4net;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInvSale : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;
        public int EntryType;
        public int InvType;
        public int ProcType;
        private Print print;
        private InvoiceObj InvObj;
        private InvoiceDGV Invoic;
        private InvoiceDGV InvoicCheck;
        private string StyleFile;
        private string StyleFolder;
        private bool EnableE_Invoice;
        private bool allowEditInvoice;
        public int InvType_credit;
        private bool _UpdateQty = false;
        private DispatcherTimer Timer1;
        private static readonly ILog Logger =
            LogManager.GetLogger(Environment.MachineName);

        // Property بديل لـ cmbProcTypeSrch الذي كان في TabPage2
        // يستخدمه النوافذ الخارجية لتحديد نوع البحث
        private int _procTypeSrchIndex = 0;

        #endregion

        #region Public Properties

        /// <summary>
        /// بديل لـ cmbProcTypeSrch في WinForms
        /// يُستخدم من النوافذ الخارجية لتحديد نوع العملية في البحث
        /// </summary>
        public ComboBoxProxy cmbProcTypeSrch { get; private set; }

        #endregion

        #region Constructor

        public frmInvSale()
        {
            InitializeComponent();

            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            InvType = 2;
            print = new Print(InvType);
            InvObj = new InvoiceObj(InvType, ProcType);
            Invoic = new InvoiceDGV();
            InvoicCheck = new InvoiceDGV();
            StyleFile = Path.Combine(MainClass.ReportsPath,
                "Styles\\SaleInvoSaveLayoutToXML.xml");
            StyleFolder = Path.Combine(MainClass.ReportsPath, "Styles");
            EnableE_Invoice = true;
            allowEditInvoice = true;

            // تهيئة الـ Proxy لـ cmbProcTypeSrch
            cmbProcTypeSrch = new ComboBoxProxy();

            // Timer
            Timer1 = new DispatcherTimer();
            Timer1.Interval = TimeSpan.FromSeconds(1);
            Timer1.Tick += Timer1_Tick;

            Loaded += Window_Loaded;
            Closing += Window_Closing;
            KeyDown += Window_KeyDown;
        }

        #endregion

        #region Item Search Button in Grid

        private void ItemSearchButton_Click(object sender, RoutedEventArgs e)
        {
            // عند الضغط على زر البحث 🔍 داخل خلية الصنف
            addNewItem();

            // بعد اختيار الصنف، نقل التركيز إلى عمود الكمية
            Dispatcher.BeginInvoke(new Action(() =>
            {
                MoveFocusToColumn("ItemQuantity");
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                loadSettings();
                LoadData1();
                InvObj = new InvoiceObj(InvType, ProcType);

                txtDate.DateTime = DateTime.Now;
                txtInvTime.Text = DateTime.Now.ToString("HH:mm:ss");
                txtRefDate.DateTime = DateTime.Now;

                // تم إزالة dxe_txtFromDate و dxe_txtToDate 
                // لأنها كانت في TabPage2 (البحث) المنفصل

                if (cmbStore.Items.Count > 0)
                    cmbStore.SelectedIndex = 0;

                txtBarcode.Focus();
                txtRefDate.IsEnabled = true;
                txtRefNo.IsEnabled = true;

                if (InvObj.ProcessFlow)
                {
                    cmbInvoiceStatus.Visibility = Visibility.Visible;
                    cmbInvoiceStatus.SelectedIndex = 0;
                }

                bool etaActive = EtaSetting.Active;
                lblWithholdTax.Visibility =
                    etaActive ? Visibility.Visible : Visibility.Collapsed;
                txtWithholdTax.Visibility =
                    etaActive ? Visibility.Visible : Visibility.Collapsed;
                BtnSendToEta.Visibility =
                    etaActive ? Visibility.Visible : Visibility.Collapsed;
                BtnEgyPrint.Visibility =
                    etaActive ? Visibility.Visible : Visibility.Collapsed;

                Timer1.Start();
                loadFonudtion();

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

                if (ProcType == 2)
                {
                    lblReturn.Visibility = Visibility.Visible;
                    allowEditInvoice = Common.AllowEdit("frmSalePurch5");
                    lblYearPreviews.Visibility = Visibility.Visible;
                    CmbYearPreviews.Visibility = Visibility.Visible;
                    txtBalancePreviews.Visibility = Visibility.Visible;
                }

                if (InvType == 21)
                {
                    lblReturn.Visibility = Visibility.Visible;
                    allowEditInvoice = Common.AllowEdit("frmSalePurch5");
                    lblYearPreviews.Visibility = Visibility.Visible;
                    CmbYearPreviews.Visibility = Visibility.Visible;
                    txtBalancePreviews.Visibility = Visibility.Visible;
                }

                if (ProcType == 4)
                {
                    btnInsertLinkedInv.Visibility = Visibility.Collapsed;
                    txtDate.IsEnabled = true;
                    txtInvTime.IsEnabled = true;
                    btnDelete.Visibility = Visibility.Visible;
                    btnDelete.IsEnabled = true;
                    allowEditInvoice = Common.AllowEdit("frmSalePurch6");
                }

                if (InvType == 2 && ProcType == 1)
                {
                    btnSaveQuotation.Visibility = Visibility.Visible;
                    allowEditInvoice = Common.AllowEdit("frmInvSale");
                }

                if (InvType == 2 && ProcType == 2)
                {
                    OpenReportDesgin.Visibility = Visibility.Collapsed;
                }

                ResetInvoice();
                LoadDefaultpaytyp();
                loadSendEmail();
            }
            catch (Exception ex)
            {
                Logger.Error("Window_Loaded: " + ex.Message);
                MessageBox.Show("خطأ أثناء التحميل: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Closing(object sender,
            System.ComponentModel.CancelEventArgs e)
        {
            bool hasRows = GridControl1.Items.Count > 1;
            if ((hasRows && Invoic.ISNew) || Invoic.IsUpdated)
            {
                var result = MessageBox.Show(
                    "لم يتم حفظ الفاتورة، هل تريد الاستمرار؟",
                    "تنبيه",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                    e.Cancel = true;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Alt+B → Focus Barcode
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.B)
            {
                e.Handled = true;
                txtBarcode.Focus();
            }

            // Alt+G → Glasses
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.G)
            {
                glassesOptions();
            }

            // F1 → Add Item
            if (e.Key == Key.F1)
                addNewItem();

            // Enter on Barcode
            if (e.Key == Key.Return)
            {
                if (txtBarcode.IsFocused)
                {
                    ReadBarcode(txtBarcode.Text, false);
                    txtBarcode.Text = "";
                }
                if (txtSrchSerialNo.IsFocused)
                {
                    SearchBySerialNo(txtSrchSerialNo.Text.Trim());
                    txtSrchSerialNo.Text = "";
                }
            }

            // F5 → New
            if (e.Key == Key.F5)
            {
                btnNew_Click(null, null);
                e.Handled = true;
            }

            // F6 → Save
            if (e.Key == Key.F6)
                ValidateInvoice();

            // F7 → Print
            if (e.Key == Key.F7)
            {
                btnPrint_Click(null, null);
                e.Handled = true;
            }

            // F8 → View
            if (e.Key == Key.F8)
            {
                btnView_Click(null, null);
                e.Handled = true;
            }

            // F9 → Delete
            if (e.Key == Key.F9)
            {
                btnDelete_Click(null, null);
                e.Handled = true;
            }
        }

        #endregion

        #region Load Data

        private void loadSendEmail()
        {
            try
            {
                if (conn.State == ConnectionState.Open) conn.Close();
                conn.Open();
                var cmd = new SqlCommand(
                    "Select isnull(SendInv,0) as SendInv from SettingEmail " +
                    "WHERE Branch_Id=" + MainClass.BranchNo, conn);
                var reader = cmd.ExecuteReader();
                if (reader.Read() && reader.HasRows)
                {
                    bool sendInv = Convert.ToBoolean(reader["SendInv"].ToString());
                    BtnSendEmail.Visibility =
                        sendInv ? Visibility.Visible : Visibility.Collapsed;
                }
                conn.Close();
            }
            catch (Exception ex)
            {
                Logger.Error("loadSendEmail: " + ex.Message);
            }
        }

        public void LoadDefaultpaytyp()
        {
            try
            {
                if (InvObj.PayTypeDefault == -1)
                {
                    cmbPayType.SelectedIndex = 0;
                    cmbTreasury.SelectedIndex = -1;
                    if (Invoic.ISNew)
                    {
                        if (LookUpEdit1.Items.Count > 0)
                            LookUpEdit1.SelectedIndex = 0;
                        Invoic.Customer = -1;
                    }
                    Invoic.PayType = -1;
                    Invoic.Treasury = -1;
                    Invoic.PaymentStatus = PaymentStatus.Paid;
                }
                else if (InvObj.PayTypeDefault == 1)
                {
                    cmbPayType.SelectedIndex = 1;
                    if (cmbTreasury.Items.Count > 0)
                        cmbTreasury.SelectedIndex = 0;
                    cmbBanks.SelectedIndex = -1;
                    Invoic.PayType = 1;
                    Invoic.Treasury = GetSelectedValue<int>(cmbTreasury);
                    Invoic.Customer = GetSelectedValue<int>(LookUpEdit1);
                }
                else if (InvObj.PayTypeDefault == 2)
                {
                    cmbPayType.SelectedIndex = 2;
                    if (cmbBanks.Items.Count > 0)
                        cmbBanks.SelectedIndex = 0;
                    cmbTreasury.SelectedIndex = -1;
                    Invoic.PayType = 2;
                    Invoic.Treasury = -1;
                    Invoic.Bank = 1;
                    Invoic.Customer = GetSelectedValue<int>(LookUpEdit1);
                }
                else
                {
                    cmbPayType.SelectedIndex = 1;
                    if (cmbTreasury.Items.Count > 0)
                        cmbTreasury.SelectedIndex = 0;
                    cmbBanks.SelectedIndex = -1;
                    Invoic.PayType = 1;
                    Invoic.Treasury = GetSelectedValue<int>(cmbTreasury);
                    Invoic.Customer = GetSelectedValue<int>(LookUpEdit1);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("LoadDefaultpaytyp: " + ex.Message);
            }
        }

        private void loadFonudtion()
        {
            try
            {
                var da = new SqlDataAdapter(
                    "select EnableE_Invoice from Foundation", conn);
                var dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    if (dt.Rows[0]["EnableE_Invoice"] != DBNull.Value)
                        EnableE_Invoice = Convert.ToBoolean(
                            dt.Rows[0]["EnableE_Invoice"]);
                    else
                        EnableE_Invoice = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("loadFonudtion: " + ex.Message);
            }
        }

        private void loadSettings()
        {
            LoadDGvSetting();
        }

        private void LoadData1()
        {
            LoadSafes();
            LoadCustomers();
            LoadTreasury();
            LoadSalesMen();
            LoadBanks();
            LoadCostCenters();
        }

        private void LoadCostCenters()
        {
            try
            {
                var dt = LoadData.CostCenters();
                cmbCostCenter.ItemsSource = dt.DefaultView;
                cmbCostCenter.DisplayMemberPath = "name";
                cmbCostCenter.SelectedValuePath = "code";
                cmbCostCenter.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                Logger.Error("LoadCostCenters: " + ex.Message);
            }
        }

        private void LoadBanks()
        {
            try
            {
                var dt = LoadData.Banks();
                cmbBanks.ItemsSource = dt.DefaultView;
                cmbBanks.DisplayMemberPath = "name";
                cmbBanks.SelectedValuePath = "id";
                cmbBanks.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                Logger.Error("LoadBanks: " + ex.Message);
            }
        }

        public void LoadSafes()
        {
            try
            {
                var dt = LoadData.Invertories(MainClass.EmpNo);
                cmbStore.ItemsSource = dt.DefaultView;
                cmbStore.DisplayMemberPath = "name";
                cmbStore.SelectedValuePath = "id";
                cmbStore.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                Logger.Error("LoadSafes: " + ex.Message);
            }
        }

        private void LoadCustomers()
        {
            try
            {
                int type = 1;
                if (InvType == 1)
                {
                    type = 2;
                    lblClient.Text = string.Equals(MainClass.Language, "ar")
                        ? "المورد" : "Supplier";
                }
                var dt = LoadData.Customers(type);
                LookUpEdit1.ItemsSource = dt.DefaultView;
                LookUpEdit1.DisplayMemberPath = "name";
                LookUpEdit1.SelectedValuePath = "id";
                if (LookUpEdit1.Items.Count > 0)
                    LookUpEdit1.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Logger.Error("LoadCustomers: " + ex.Message);
            }
        }

        public void LoadTreasury()
        {
            try
            {
                var dt = LoadData.Treasury();
                cmbTreasury.ItemsSource = dt.DefaultView;
                cmbTreasury.DisplayMemberPath = "name";
                cmbTreasury.SelectedValuePath = "id";
                cmbTreasury.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                Logger.Error("LoadTreasury: " + ex.Message);
            }
        }

        public void LoadSalesMen()
        {
            try
            {
                var dt = LoadData.SalesMen();
                cmbSalesMen.ItemsSource = dt.DefaultView;
                cmbSalesMen.DisplayMemberPath = "name";
                cmbSalesMen.SelectedValuePath = "id";
                cmbSalesMen.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                Logger.Error("LoadSalesMen: " + ex.Message);
            }
        }

        private void LoadDGvSetting()
        {
            try
            {
                if (User.EditPrice)
                    ItemPrice.IsReadOnly = false;
            }
            catch (Exception ex)
            {
                Logger.Error("LoadDGvSetting: " + ex.Message);
            }
        }

        private void LoadInvNo()
        {
            Invoic.InvoiceNo = InvoiceOper.InvoiceNo(InvType, ProcType,
                InvObj.Prefixe);
            txtInvCode.Text = string.Concat(
                MainClass.BranchCode + InvObj.InvoiceCode,
                ProcType.ToString());
            Invoic.InvCombinedId =
                txtInvCode.Text.Trim() + Invoic.InvoiceNo.ToString();
            txtNo.Text = Invoic.InvoiceNo.ToString();
            txtNo.Background = new SolidColorBrush(Colors.Firebrick);
            txtNo.Foreground = new SolidColorBrush(Colors.White);
        }

        public void loadDB_by_DbAutoName()
        {
            try
            {
                var sqlConn = MainClass.ConnObj();
                if (sqlConn.State != ConnectionState.Open)
                    sqlConn.Open();
                var dt = new DataTable();
                new SqlDataAdapter(
                    "SELECT Dbname, DbAutoName FROM Year_Previews " +
                    "WHERE Is_Deleted=0 ORDER BY id", sqlConn).Fill(dt);
                CmbYearPreviews.ItemsSource = dt.DefaultView;
                CmbYearPreviews.DisplayMemberPath = "Dbname";
                CmbYearPreviews.SelectedValuePath = "DbAutoName";
                sqlConn.Close();
            }
            catch (Exception ex)
            {
                Logger.Error("loadDB_by_DbAutoName: " + ex.Message);
            }
        }

        #endregion

        #region Item Search (Quick Entry)

        private bool ISItem(string barcode)
        {
            try
            {
                var da = new SqlDataAdapter(
                    "select name from Items where IS_Deleted=0 and barcode="
                    + barcode, conn);
                var dt = new DataTable();
                da.Fill(dt);
                return dt.Rows.Count > 0;
            }
            catch { return false; }
        }

        private void SearchByName()
        {
            try
            {
                var selectedItem = GridControl1.SelectedItem as InvoiceItem;
                string text = selectedItem?.ItemName ?? "";

                if (!string.IsNullOrEmpty(text))
                {
                    var da = new SqlDataAdapter(
                        "select name,id from Items where IS_Deleted=0 " +
                        "and name=N'" + text + "'", conn);
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                        SearchByID(Convert.ToInt32(dt.Rows[0]["id"]), 0, "", "");
                    else
                        SearchByCode(text, 0);
                }
                else
                {
                    addNewItem();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("SearchByName: " + ex.Message);
            }
        }

        private void SearchByCode(string itemCode, int itemUnit)
        {
            try
            {
                var da = new SqlDataAdapter(
                    "select name,id from Items where IS_Deleted=0 " +
                    "and Code=N'" + itemCode + "'", conn);
                var dt = new DataTable();
                da.Fill(dt);
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
                Logger.Error("SearchByCode: " + ex.Message);
            }
        }

        private void SearchByID(int itemId, int unitId,
            string itemBarcode, string serialNo)
        {
            try
            {
                new ItemOper().GetItemByID(itemId, unitId, ref Invoic,
                    false, itemBarcode, serialNo);
                if (!InvObj.SaleByMinus)
                    ItemOper.ISvalidQuantity(ref Invoic, itemId,
                        Invoic.InvoiceItems.Count, InvObj.SaleByMinus);
                ItemOper.CalcRows(ref Invoic);
                BindingDGV();
            }
            catch (Exception ex)
            {
                Logger.Error("SearchByID: " + ex.Message);
            }
        }

        private void addNewItem()
        {
            var frm = new frmItemsSrch();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.sql = "select id,name,nameEN,sale_price,unit " +
                      "from Items where IS_Deleted=0 order by id";
            frm.search = "select id,name,nameEN,sale_price,unit from Items";
            frm.Itemname = "";
            frm.StoreId = GetSelectedValue<int>(cmbStore);
            frm.ShowDialog();
            if (frm.ISDone && frm.ItemId > 0)
            {
                foreach (var id in frm.Itemlist)
                    SearchByID(id, 0, "", "");
            }
        }

        private void ReadBarcode(string itemBarcode, bool isMultiText)
        {
            try
            {
                int itemID = 0, unitId = 0;
                decimal itemPrice = 0, itemQuantity = 0;
                ItemOper.SearchForBarcode(itemBarcode.Trim(),
                    ref itemID, ref unitId,
                    ref itemPrice, ref itemQuantity, isMultiText);
                if (itemID > 0)
                    SearchByID(itemID, unitId, itemBarcode.Trim(), "");
                else
                    addNewItem();
            }
            catch (Exception ex)
            {
                Logger.Error("ReadBarcode: " + ex.Message);
            }
        }

        private void SearchBySerialNo(string serialNo)
        {
            try
            {
                bool alreadyAdded = Invoic.InvoiceItems.Any(item =>
                    item.InvoiceItemDetails.Any(sub =>
                        string.Equals(sub.ItemSerialNo, serialNo)));
                if (alreadyAdded)
                {
                    MessageBox.Show(
                        string.Equals(MainClass.Language, "ar")
                            ? "تم إدراج هذا الرقم التسلسلي من قبل"
                            : "This serial number has been listed before.",
                        "", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var sql = "select itemid,ItemSerialNo," +
                    "Sum(stockIn-stockOut) as result from (" +
                    "select itemid,ItemSerialNo,Sum(ItemQuantity) as stockIn," +
                    "0 as stockOut from inv left join InvoiceItemDetail " +
                    "on Inv.InvGlobalID=InvoiceItemDetail.InvGlobalID " +
                    "where Inv.IS_Deleted=0 and ItemSerialNo=N'" + serialNo +
                    "' and InvertoryImpact=1 group by itemid,ItemSerialNo " +
                    "union select itemid,ItemSerialNo,0 as stockIn," +
                    "sum(ItemQuantity) as stockOut from inv " +
                    "left join InvoiceItemDetail " +
                    "on Inv.InvGlobalID=InvoiceItemDetail.InvGlobalID " +
                    "where Inv.IS_Deleted=0 and ItemSerialNo=N'" + serialNo +
                    "' and InvertoryImpact=2 group by itemid,ItemSerialNo" +
                    ") as tb group by itemid,ItemSerialNo";

                var da = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        if (Convert.ToDouble(row["result"]) > 0)
                            SearchByID(Convert.ToInt32(row["itemid"]),
                                0, "", serialNo);
                        else
                            MessageBox.Show(
                                string.Equals(MainClass.Language, "ar")
                                    ? "تم بيع أو إخراج هذا الرقم التسلسلي"
                                    : "This serial number has been sold.",
                                "", MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show(
                        string.Equals(MainClass.Language, "ar")
                            ? "لا يوجد صنف بهذا الرقم التسلسلي"
                            : "There is no item with this serial number.",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("SearchBySerialNo: " + ex.Message);
                addNewItem();
            }
        }

        #endregion

        #region Item Units & Invertory

        private void ShowItemUnit(int itemId, int rowIndex)
        {
            try
            {
                for (int i = 0; i < Invoic.InvoiceItems.Count; i++)
                {
                    var currentItem = Invoic.InvoiceItems[i];
                    if (currentItem.ItemId == itemId &&
                        currentItem.ItemRowIndex == rowIndex)
                    {
                        var frm = new frmItemUnits();
                        frm.ItemId = currentItem.ItemId;
                        frm.PreUnit = currentItem.UnitID;
                        frm.ShowDialog();
                        if (!string.IsNullOrEmpty(frm.Unitname))
                        {
                            currentItem.UnitID = frm.UnitId;
                            currentItem.UnitName = frm.Unitname;
                            // استخدام متغير محلي بدل ref على foreach
                            var tempItem = currentItem;
                            ItemOper.LoadUnitInf(ref tempItem,
                                (int)Invoic.InvoiceType, Invoic.Pricing);
                            Invoic.InvoiceItems[i] = tempItem;
                            ItemOper.CalcRows(ref Invoic);
                            BindingDGV();
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل الوحدات\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowItemInvertories(int itemId, int rowIndex)
        {
            try
            {
                foreach (var item in Invoic.InvoiceItems)
                {
                    if (item.ItemId == itemId && item.ItemRowIndex == rowIndex)
                    {
                        var frm = new frmItemInvertory();
                        frm.ItemId = item.ItemId;
                        frm.ShowDialog();
                        if (frm.InvertoryId > 0)
                        {
                            item.InvertoryId = frm.InvertoryId;
                            item.InvertoryName = frm.InvertoryName;
                            item.ValiableInvertory =
                                Convert.ToDouble(frm.ItemQty);
                            ItemOper.ISvalidQuantity(ref Invoic,
                                GetFocusedItemId(), GetFocusedRowIndex(),
                                InvObj.SaleByMinus);
                            ItemOper.CalcRows(ref Invoic);
                            BindingDGV();
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل المستودع\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region DataGrid Events (GridControl1)

        private void GridControl1_BeginningEdit(object sender,
            DataGridBeginningEditEventArgs e)
        {
            // Allow editing
        }

        private void GridControl1_CellEditEnding(object sender,
            DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;

            var item = e.Row.Item as InvoiceItem;
            if (item == null) return;

            string colName = e.Column.SortMemberPath;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                HandleCellChanged(item, colName);
            }), DispatcherPriority.Background);
        }

        private void HandleCellChanged(InvoiceItem item, string fieldName)
        {
            try
            {
                if (fieldName == "ItemQuantity")
                {
                    if (!InvObj.SaleByMinus)
                        ItemOper.ISvalidQuantity(ref Invoic, item.ItemId,
                            item.ItemRowIndex, InvObj.SaleByMinus);
                    ItemOper.CalcRows(ref Invoic);
                    BindingDGV();
                    ItemOper.checkQtyLimi(ref Invoic, item.ItemId);
                }
                else if (fieldName == "ItemPrice")
                {
                    ItemOper.IsValidPrice(ref Invoic,
                        item.ItemId, item.ItemRowIndex);
                    ItemOper.CalcRows(ref Invoic);
                    BindingDGV();
                }
                else if (fieldName == "ItemDiscount")
                {
                    double discPerc = Convert.ToDouble(
                        InvoiceOper.GetDiscountPercentage(
                            item.ItemSumPrice, item.ItemDiscount)
                        .ToString(InvObj.DigitsNo));
                    double priceDisc = item.ItemDiscount / item.ItemQuantity;

                    foreach (var inv in Invoic.InvoiceItems)
                    {
                        if (inv.ItemId == item.ItemId &&
                            inv.ItemRowIndex == item.ItemRowIndex)
                        {
                            inv.ItemDiscountPerc = discPerc;
                            inv.ItemPriceDiscount = priceDisc;
                            ItemOper.IsValidDiscount(ref Invoic,
                                inv.ItemId, inv.ItemRowIndex, false);
                        }
                    }
                    ItemOper.CalcRows(ref Invoic);
                    BindingDGV();
                }
                else if (fieldName == "ItemDiscountPerc")
                {
                    double discVal = item.ItemDiscountPerc / 100.0
                        * item.ItemSumPrice;
                    double priceDisc = discVal / item.ItemQuantity;

                    foreach (var inv in Invoic.InvoiceItems)
                    {
                        if (inv.ItemId == item.ItemId &&
                            inv.ItemRowIndex == item.ItemRowIndex)
                        {
                            inv.ItemDiscount = discVal;
                            inv.ItemPriceDiscount = priceDisc;
                            ItemOper.IsValidDiscount(ref Invoic,
                                inv.ItemId, inv.ItemRowIndex, true);
                        }
                    }
                    ItemOper.CalcRows(ref Invoic);
                    BindingDGV();
                }
                else if (fieldName == "ItemSumPrice")
                {
                    if (item.ItemQuantity != 0)
                        item.ItemPrice = item.ItemSumPrice / item.ItemQuantity;
                    ItemOper.CalcRows(ref Invoic);
                    BindingDGV();
                }
                else if (fieldName == "ItemPriceDiscount")
                {
                    double discVal = item.ItemPriceDiscount * item.ItemQuantity;
                    double discPerc = Convert.ToDouble(
                        InvoiceOper.GetDiscountPercentage(
                            item.ItemSumPrice, discVal)
                        .ToString(InvObj.DigitsNo));
                    foreach (var inv in Invoic.InvoiceItems)
                    {
                        if (inv.ItemId == item.ItemId &&
                            inv.ItemRowIndex == item.ItemRowIndex)
                        {
                            inv.ItemDiscount = discVal;
                            inv.ItemDiscountPerc = discPerc;
                        }
                    }
                    ItemOper.CalcRows(ref Invoic);
                    BindingDGV();
                }
                else if (fieldName == "ItemName")
                {
                    // Quick search by name
                    SearchByName();
                }
                else if (fieldName == "ItemCode")
                {
                    SearchByCode(item.ItemCode, 0);
                }
                else if (fieldName == "UnitName")
                {
                    ShowItemUnit(item.ItemId, item.ItemRowIndex);
                }
                else if (fieldName == "InvertoryName")
                {
                    ShowItemInvertories(item.ItemId, item.ItemRowIndex);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("HandleCellChanged: " + ex.Message);
            }
        }

        private void GridControl1_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            var item = GridControl1.SelectedItem as InvoiceItem;
            if (item != null && item.ItemId > 0)
                ShowItemDetails(item.ItemId);
        }

        private void GridControl1_KeyDown(object sender, KeyEventArgs e)
        {
            if (GridControl1.Items.Count <= 0) return;

            if (e.Key == Key.Delete)
            {
                var result = MessageBox.Show("هل أنت متأكد من الحذف؟",
                    "رسالة تأكيد", MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;

                var item = GridControl1.SelectedItem as InvoiceItem;
                if (item != null)
                {
                    Invoic.InvoiceItems.Remove(item);
                    int idx = 1;
                    foreach (var inv in Invoic.InvoiceItems)
                        inv.ItemRowIndex = idx++;
                    ItemOper.CalcRows(ref Invoic);
                    BindingDGV();
                }
            }

            if (e.Key == Key.Return)
            {
                var item = GridControl1.SelectedItem as InvoiceItem;
                if (item == null) return;

                var col = GridControl1.CurrentColumn;
                if (col == null) return;

                string fieldName = col.SortMemberPath;

                if (fieldName == "ItemCode")
                    SearchByCode(item.ItemCode, 0);
                else if (fieldName == "ItemName")
                    SearchByName();
                else if (fieldName == "UnitName")
                    ShowItemUnit(item.ItemId, item.ItemRowIndex);
                else if (fieldName == "ItemQuantity")
                    MoveFocusToColumn("ItemPrice");
                else if (fieldName == "ItemPrice")
                    MoveFocusToNextRow("ItemName");
                else if (fieldName == "InvertoryName")
                {
                    ShowItemInvertories(item.ItemId, item.ItemRowIndex);
                    MoveFocusToNextRow("ItemName");
                }
            }
        }

        private void GridControl1_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Handled in GridControl1_KeyDown
        }

        private void GridControl1_MouseDoubleClick(object sender,
            MouseButtonEventArgs e)
        {
            var item = GridControl1.SelectedItem as InvoiceItem;
            if (item == null) return;

            var col = GridControl1.CurrentColumn;
            if (col == null) return;

            string fieldName = col.SortMemberPath;

            if (fieldName == "UnitName")
                ShowItemUnit(item.ItemId, item.ItemRowIndex);
            else if (fieldName == "InvertoryName")
                ShowItemInvertories(item.ItemId, item.ItemRowIndex);
            else if (fieldName == "ItemPrice" && !InvObj.PriceIncVAT)
                ShowVATCalculator_Price(item);
            else if (fieldName == "ItemExpireDate")
                loadExpireItem(item.ItemId, item.ItemRowIndex);
            else if (fieldName == "Description")
            {
                ItemOper.LoadItemDetails(Invoic, ref item);
                ItemOper.CalcRows(ref Invoic);
                BindingDGV();
            }
        }

        private void GridControl1_ContextMenuOpening(object sender,
            ContextMenuEventArgs e)
        {
            // Context menu is defined in XAML
        }

        #endregion

        #region Context Menu Events

        private void MenuShowItem_Click(object sender, RoutedEventArgs e)
            => ShowItemCard();
        private void MenuAddNewProduct_Click(object sender, RoutedEventArgs e)
            => AddNewProduct();
        private void MenuShowItemUnits_Click(object sender, RoutedEventArgs e)
            => ShowItemUnitsFromMenu();
        private void MenuItemInvertories_Click(object sender, RoutedEventArgs e)
            => ShowItemInvertoryFromMenu();
        private void MenuDeleteRow_Click(object sender, RoutedEventArgs e)
            => DeleteSelectedRow();
        private void MenuItemProcess_Click(object sender, RoutedEventArgs e)
            => ItemProcess();
        private void MenuItemLastActivity_Click(object sender, RoutedEventArgs e)
            => ItemLastActivity();
        private void MenuClientActivity_Click(object sender, RoutedEventArgs e)
            => ItemClientActivity();
        private void MenuItemSerialNo_Click(object sender, RoutedEventArgs e)
            => temSerialNo();
        private void MenuItemCost_Click(object sender, RoutedEventArgs e)
            => ItemCost1();

        private void ShowItemCard()
        {
            if (!User.EditItemInfo)
            {
                MessageBox.Show("لا يوجد لديك صلاحية لهذه العملية");
                return;
            }
            var item = GridControl1.SelectedItem as InvoiceItem;
            if (item == null) return;
            var frm = new frmItems();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.ItemId = item.ItemId;
            frm.ShowDialog();
        }

        private void AddNewProduct()
        {
            if (!User.EditItemInfo)
            {
                MessageBox.Show("لا يوجد لديك صلاحية لهذه العملية");
                return;
            }
            var frm = new frmItems();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.ShowDialog();
        }

        private void ShowItemUnitsFromMenu()
        {
            var item = GridControl1.SelectedItem as InvoiceItem;
            if (item == null) return;
            ShowItemUnit(item.ItemId, item.ItemRowIndex);
        }

        private void ShowItemInvertoryFromMenu()
        {
            var item = GridControl1.SelectedItem as InvoiceItem;
            if (item == null) return;
            ShowItemInvertories(item.ItemId, item.ItemRowIndex);
        }

        private void DeleteSelectedRow()
        {
            var item = GridControl1.SelectedItem as InvoiceItem;
            if (item == null) return;
            var result = MessageBox.Show("هل أنت متأكد من حذف السجل؟",
                "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            Invoic.InvoiceItems.Remove(item);
            int idx = 1;
            foreach (var inv in Invoic.InvoiceItems)
                inv.ItemRowIndex = idx++;
            ItemOper.CalcRows(ref Invoic);
            BindingDGV();
        }

        private void ItemProcess()
        {
            var item = GridControl1.SelectedItem as InvoiceItem;
            if (item == null) return;
            var frm = new frmRptItemsActivityDetailed();
            frm.SelectedId = item.ItemId;
            frm.txtItemName.Text = item.ItemName;
            frm.txtItemCode.Text = item.ItemCode;
            frm.ShowResult();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.ShowDialog();
        }

        private void ItemLastActivity()
        {
            var item = GridControl1.SelectedItem as InvoiceItem;
            if (item == null) return;
            var frm = new frmRptItemsActivityDetailed();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.txtItemName.Text = item.ItemName;
            frm.txtItemCode.Text = item.ItemCode;
            frm.Activate();
            frm.Show();
            frm.LastProcess(item.ItemId, item.InvertoryId, -1, 1);
        }

        private void ItemClientActivity()
        {
            var item = GridControl1.SelectedItem as InvoiceItem;
            if (item == null) return;
            var frm = new frmRptItemsActivityDetailed();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.txtItemName.Text = item.ItemName;
            frm.txtItemCode.Text = item.ItemCode;
            frm.Activate();
            frm.Show();
            int custId = GetSelectedValue<int>(LookUpEdit1);
            if (custId > -1)
                frm.LastProcess(item.ItemId, item.InvertoryId, custId, 1);
        }

        private void ItemCost1()
        {
            if (!User.ShowCosts)
            {
                MessageBox.Show("لا يوجد لديك صلاحية هذه العملية");
                return;
            }
            var item = GridControl1.SelectedItem as InvoiceItem;
            if (item == null) return;

            // تحويل الناتج إلى string
            var costResult = ItemOper.AvgCost(item.ItemId, MainClass.BranchNo);
            MessageBox.Show(
                costResult.ToString(),
                "تكلفة المادة", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void temSerialNo()
        {
            try
            {
                var currentItem = GridControl1.SelectedItem as InvoiceItem;
                if (currentItem == null) return;
                int itemId = currentItem.ItemId;

                var frm = new frmItemSerialNo();
                foreach (var inv in Invoic.InvoiceItems)
                {
                    if (inv.ItemId != itemId) continue;
                    foreach (var detail in inv.InvoiceItemDetails)
                    {
                        var d = new InvoiceItemDetail
                        {
                            InvGlobalID = detail.InvGlobalID,
                            ItemId = detail.ItemId,
                            ItemSerialNo = detail.ItemSerialNo,
                            BatchNo = detail.BatchNo,
                            ItemProductionDate = detail.ItemProductionDate,
                            ItemExpireDate = detail.ItemExpireDate,
                            ItemHeight = detail.ItemHeight,
                            ItemWidth = detail.ItemWidth,
                            ItemColor = detail.ItemColor,
                            ItemSize = detail.ItemSize,
                            ItemProperty = detail.ItemProperty,
                            FillValue = detail.FillValue,
                            FillRatio = detail.FillRatio,
                            ItemQuantity = detail.ItemQuantity
                        };
                        frm.InvItem.InvoiceItemDetails.Add(d);
                    }
                    frm.InvItem = inv;
                    if (Invoic.ISNew && inv.InvoiceItemDetails.Count > 0)
                        frm.operType = 1;
                }
                frm.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.Error("temSerialNo: " + ex.Message);
            }
        }

        #endregion

        #region Item Details

        private void ShowItemDetails(int itemId)
        {
            try
            {
                txtLastSalePrice.Text = "";
                txtCompetitorPrice.Text = "";
                txtCostAvrg.Text = "";
                txtStock.Text = "";
                txtItemBarcode.Text = "";

                var item = GridControl1.SelectedItem as InvoiceItem;
                if (item == null) return;

                if (User.ShowCosts)
                {
                    txtRecentPurchPrice.Text =
                        ItemOper.RecentPurchPrice(itemId).ToString();
                    txtCostAvrg.Text = Math.Round(
                        item.ItemCost * item.UnitEquality, 2).ToString();
                }
                else
                {
                    txtRecentPurchPrice.Text = "0";
                    txtCostAvrg.Text = "0";
                }

                LoadPrices(itemId);

                if (item.UnitEquality > 0)
                {
                    txtStock.Text = item.ValiableInvertory.ToString();
                    txtStock.Foreground = item.ValiableInvertory > -1
                        ? new SolidColorBrush(Colors.Green)
                        : new SolidColorBrush(Colors.Firebrick);
                }

                txtPerUnit.Text = item.UnitEquality.ToString();
                txtTotUnitQuan.Text = item.ItemPrimaryQnty.ToString();
                txtItemBarcode.Text = item.ItemBarcode;
            }
            catch (Exception ex)
            {
                Logger.Error("ShowItemDetails: " + ex.Message);
            }
        }

        private void LoadPrices(int itemId)
        {
            try
            {
                var da = new SqlDataAdapter(
                    "select sale_price,CompetitorPrice from ItemPrices " +
                    "where Itemid=" + itemId +
                    " order by Proc_id desc", conn);
                var dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    txtLastSalePrice.Text =
                        dt.Rows[0]["sale_price"].ToString();
                    txtCompetitorPrice.Text =
                        dt.Rows[0]["CompetitorPrice"].ToString();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("LoadPrices: " + ex.Message);
            }
        }

        private void loadExpireItem(int itemId, int rowIndex)
        {
            try
            {
                foreach (var item in Invoic.InvoiceItems)
                {
                    if (item.ItemId == itemId &&
                        item.ItemRowIndex == rowIndex)
                    {
                        var frm = new frmItemsExpire();
                        frm.SelectedID = itemId;
                        frm.lblItemName.Text = item.ItemName;
                        frm.ShowDialog();
                        if (frm.SelectedID != 0)
                        {
                            item.ItemExpireDate = frm.ExpireDate;
                            ItemOper.CalcRows(ref Invoic);
                            BindingDGV();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("loadExpireItem: " + ex.Message);
            }
        }

        private void ShowVATCalculator_Price(InvoiceItem item)
        {
            var frm = new FrmCalcVAT();
            frm.ItemVAT = (int)item.ItemVatPerc;
            frm.ShowDialog();
            if (frm.Price > 0)
            {
                item.ItemPrice = InvObj.PriceIncVAT
                    ? Convert.ToDouble(frm.txtPriceWithVAT.Text)
                    : frm.Price;
                ItemOper.CalcRows(ref Invoic);
                BindingDGV();
            }
        }

        #endregion

        #region Binding

        private void BindingDGV()
        {
            GridControl1.ItemsSource = null;
            GridControl1.ItemsSource = Invoic.InvoiceItems;

            txtTirmsNo.Text = Invoic.InvoiceItems.Count.ToString();
            txtTotQuan.Text = Invoic.TotalQty.ToString(InvObj.DigitsNo);
            txtSumVal.Text = Invoic.SumPrice.ToString(InvObj.DigitsNo);
            txtTotDiscount.Text =
                Invoic.TotDiscount.ToString(InvObj.DigitsNo);
            txtInvDiscVal.Text =
                Invoic.InvDiscount.ToString(InvObj.DigitsNo);
            txtInvDiscPerc.Text =
                InvoiceOper.GetDiscountPercentage(
                    Invoic.SumPrice, Invoic.InvDiscount)
                .ToString(InvObj.DigitsNo);
            txtNetWithoutVAT.Text =
                Invoic.Total.ToString(InvObj.DigitsNo);
            txtTotVAT.Text = Invoic.VAT.ToString(InvObj.DigitsNo);
            txtNet.Text = Invoic.Net.ToString(InvObj.DigitsNo);
            txtInvRemainder.Text =
                Invoic.Remainder.ToString(InvObj.DigitsNo);
            txtInvPaid.Text = Invoic.Paid.ToString(InvObj.DigitsNo);
            txtWithholdTax.Text =
                Invoic.TotalWithholdingTax.ToString(InvObj.DigitsNo);
            txtBalancePreviews.Text =
                Invoic.BalancePreviews.ToString();

            // Scroll to last
            if (GridControl1.Items.Count > 0)
                GridControl1.ScrollIntoView(
                    GridControl1.Items[GridControl1.Items.Count - 1]);
        }

        private void BindingControls()
        {
            txtNo.Text = Invoic.InvoiceNo.ToString();
            txtDate.DateTime = Invoic.InvDate;

            try
            {
                var dt = DateTime.Parse(
                    Invoic.InvDate.ToShortDateString() + " " +
                    Invoic.InvTime.ToShortTimeString());
                txtInvTime.Text = dt.ToString("HH:mm:ss");
            }
            catch { }

            SetComboByValue(cmbTreasury, Invoic.Treasury);
            SetComboByValue(cmbStore, Invoic.Store);
            txtNote.Text = Invoic.InvNote;
            txtBalancePreviews.Text = Invoic.BalancePreviews.ToString();
            txtRefDate.DateTime = Invoic.RefDate;
            txtRefNo.Text = Invoic.ReffNo;

            // Payment status
            if (!Invoic.ISNew && InvObj.PaymentStatus && ProcType == 1)
            {
                lblPaidStatus.Visibility = Visibility.Visible;
                if (Invoic.PaymentStatus == PaymentStatus.Unpaid ||
                    Invoic.PaymentStatus == PaymentStatus.PaidPartially)
                {
                    lblPaidStatus.Foreground =
                        new SolidColorBrush(Colors.DarkRed);
                    btnRepaid.Visibility = Visibility.Visible;
                    lblPaidStatus.Text = string.Equals(MainClass.Language, "ar")
                        ? "غير مكتملة الدفع" : "Unpaid Invoice";
                }
                else if (Invoic.PaymentStatus == PaymentStatus.Paid)
                {
                    lblPaidStatus.Text = "مدفوعة";
                    lblPaidStatus.Foreground =
                        new SolidColorBrush(Colors.Green);
                    btnRepaid.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                btnRepaid.Visibility = Visibility.Collapsed;
                lblPaidStatus.Visibility = Visibility.Collapsed;
            }

            // Zatca Status
            try
            {
                if (Common.GetZatcaActive() && ProcType != 4)
                {
                    LBLzatcaStatus.Visibility = Visibility.Visible;
                    if (Invoic.ZatcaSent)
                    {
                        LBLzatcaStatus.Text = "تم الترحيل zatca";
                        LBLzatcaStatus.Foreground =
                            new SolidColorBrush(Colors.Green);
                    }
                    else
                    {
                        LBLzatcaStatus.Text = "فشل الترحيل zatca";
                        LBLzatcaStatus.Foreground =
                            new SolidColorBrush(Colors.Red);
                    }
                }
            }
            catch { }

            // Payment type
            if (Invoic.PayType == -1)
                cmbPayType.SelectedIndex = 0;
            else if (Invoic.PayType == 1)
                cmbPayType.SelectedIndex = 1;
            else if (Invoic.PayType == 2)
            {
                cmbPayType.SelectedIndex = 2;
                SetComboByValue(cmbBanks, Invoic.Bank);
            }

            if (Convert.ToDouble(Invoic.InvCCcode) > -1)
                SetComboByValue(cmbCostCenter, Invoic.InvCCcode);

            SetComboByValue(LookUpEdit1, Invoic.Customer);
            txtCashCust.Text = Invoic.CashCustomerName;
            txtCashCustMobile.Text = Invoic.CashCustomerMobile;

            try { SetComboByValue(cmbSalesMen, Invoic.Saleman); }
            catch { }

            if (cmbInvoiceStatus.IsVisible)
                cmbInvoiceStatus.SelectedIndex = Invoic.InvoiceStatus;
        }

        #endregion

        #region Reset & Navigate

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
            Invoic.InvAccCode = InvObj.InvAcc;
            if (Invoic.InvoiceType == (InvoiceType)21)
                Invoic.InvAccCode = 4100001.ToString();

            Invoic.Store = GetSelectedValue<int>(cmbStore);
            Invoic.InvertoryName = cmbStore.Text;
            Invoic.Treasury = GetSelectedValue<int>(cmbTreasury);

            if (InvObj.InvDefualtCust != 0)
            {
                Invoic.Customer = InvObj.InvDefualtCust;
                SetComboByValue(LookUpEdit1, InvObj.InvDefualtCust);
            }
            else
            {
                Invoic.Customer = GetSelectedValue<int>(LookUpEdit1);
            }

            Invoic.InvCCcode = (-1).ToString();
            Invoic.CreateDate = DateTime.Now;
            Invoic.InvDate = txtDate.DateTime;
            Invoic.InvTime = DateTime.Now;
            Invoic.RefDate = txtRefDate.DateTime;
            Invoic.PayType = 1;
            Invoic.User = MainClass.EmpNo;
            Invoic.ISNew = true;
            Invoic.IsDeleted = false;
            Invoic.IsLoaded = false;
            Invoic.IsPrinted = false;
            Invoic.ReffNo = (-1).ToString();
            Invoic.TotalWithholdingTax = 0.0;
            Invoic.InvoiceStatus = InvObj.ProcessFlow ? 2 : 3;
            Invoic.Currency = InvObj.Currency;
            Invoic.Pricing = InvObj.Pricing;

            GridControl1.ItemsSource = Invoic.InvoiceItems;
            InvoicCheck = new InvoiceDGV();
            InvoiceOper.filePathDocument = null;
            LBLzatcaStatus.Visibility = Visibility.Collapsed;
        }

        private void CLR()
        {
            try
            {
                if (cmbStore.Items.Count > 0)
                    cmbStore.SelectedIndex = 0;
                txtDate.DateTime = DateTime.Now;
                txtRefDate.DateTime = DateTime.Now;
                txtInvTime.Text = DateTime.Now.ToString("HH:mm:ss");
                cmbPayType.SelectedIndex = 1;
                if (cmbInvoiceStatus.IsVisible)
                    cmbInvoiceStatus.SelectedIndex = 0;
                cmbTreasury.IsEnabled = true;
                cmbCostCenter.SelectedIndex = -1;
                if (LookUpEdit1.Items.Count > 0)
                    LookUpEdit1.SelectedIndex = 0;
                if (cmbStore.Items.Count > 0)
                    cmbStore.SelectedIndex = 0;
                if (cmbTreasury.Items.Count > 0)
                    cmbTreasury.SelectedIndex = 0;
                txtBarcode.Focus();
            }
            catch (Exception ex)
            {
                Logger.Error("CLR: " + ex.Message);
            }

            lblPaidStatus.Text = "";
            GridControl1.ItemsSource = null;
            Invoic = new InvoiceDGV();
            ResetInvoice();
            txtRefDate.IsEnabled = true;
            txtRefNo.IsEnabled = true;
            LoadDefaultpaytyp();
        }

        public void Navigate(string sqlStr)
        {
            if (Invoic != null) CLR();
            InvoiceOper.BindingInvoice(ref Invoic, sqlStr);
            if (Invoic != null)
            {
                txtNo.Background =
                    new SolidColorBrush(Color.FromRgb(245, 245, 245));
                txtNo.Foreground = new SolidColorBrush(Colors.Black);
                BindingDGV();
                BindingControls();
            }
            else
            {
                Invoic = new InvoiceDGV();
                CLR();
            }
        }

        private bool CheckBeforeClear()
        {
            if (Invoic == null) return false;
            if ((GridControl1.Items.Count > 1 && Invoic.ISNew) ||
                Invoic.IsUpdated)
            {
                var result = MessageBox.Show(
                    string.Equals(MainClass.Language, "ar")
                        ? "لم يتم حفظ الفاتورة، هل تريد جديد؟"
                        : "You do not save the invoice",
                    "تنبيه",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                return result == MessageBoxResult.No;
            }
            return false;
        }

        #endregion

        #region Validate & Save

        private void ValidateInvoice()
        {
            try
            {
                // Return invoice checks
                if (Invoic.ISNew && Invoic.ProcType == 2)
                {
                    if (Invoic.InvGlobalID != null)
                    {
                        if ((Invoic.InvoiceType == InvoiceType.Sale ||
                             Invoic.InvoiceType == InvoiceType.POS ||
                             Invoic.InvoiceType == InvoiceType.Purchase) &&
                             Invoic.ProcType == 2)
                        {
                            if (InvoiceOper.isReturned(Invoic))
                            {
                                MessageBox.Show(
                                    string.Equals(MainClass.Language, "ar")
                                        ? "الفاتورة تم إرجاعها سابقاً أو بعض الأصناف"
                                        : "The invoice has already been returned.",
                                    "", MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                                return;
                            }
                        }
                    }

                    if (Invoic.ReffNo == null ||
                        DateTime.Compare(Invoic.RefDate, DateTime.Now) >= 0)
                    {
                        MessageBox.Show(
                            string.Equals(MainClass.Language, "ar")
                                ? "يجب ربط الفاتورة برقم فاتورة المبيعات أو الرقم غير صحيح"
                                : "The invoice must be linked to the sales invoice number.",
                            "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (!InvoiceOper.IsHasRefrence(Invoic.ReffNo))
                    {
                        var r = MessageBox.Show(
                            string.Equals(MainClass.Language, "ar")
                                ? "فاتورة المبيعات خارج الفترة المحاسبية، هل تريد استكمال إرجاع الفاتورة؟"
                                : "Sales invoice outside period. Continue?",
                            "", MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                        if (r == MessageBoxResult.No) return;
                    }
                }

                // Empty items
                if (Invoic.InvoiceItems.Count == 0)
                {
                    MessageBox.Show(
                        string.Equals(MainClass.Language, "ar")
                            ? "لا يمكن حفظ الفاتورة بدون أصناف"
                            : "An invoice cannot be saved without items.",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // SalesMen required
                if (InvObj.SaleManIsRequire && Invoic.Saleman <= 0)
                {
                    MessageBox.Show(
                        string.Equals(MainClass.Language, "ar")
                            ? "يجب اختيار المندوب"
                            : "Please select salesman.",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Zero quantities
                foreach (var item in Invoic.InvoiceItems)
                {
                    if (item.ItemQuantity <= 0)
                    {
                        MessageBox.Show(
                            string.Equals(MainClass.Language, "ar")
                                ? "لا يمكن حفظ الفاتورة، يوجد كميات أقل أو تساوي الصفر"
                                : "Quantities less than or equal to zero.",
                            "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Confirm
                var confirm = MessageBox.Show(
                    string.Equals(MainClass.Language, "ar")
                        ? "هل أنت متأكد من الحفظ؟"
                        : "Are you sure to save?",
                    "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm == MessageBoxResult.No) return;

                // Trial check
                if (MainClass.IsTrial && Invoic.ISNew)
                {
                    var da = new SqlDataAdapter("select id from Entry", conn1);
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count >= 20)
                    {
                        MessageBox.Show(
                            "نأسف، وصلت لأقصى حد إدخال للنسخة التجريبية",
                            "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        Invoic.IsPrinted = false;
                        return;
                    }
                }

                if (conn1.State != ConnectionState.Open) conn1.Open();

                // Net > 0
                if (!double.TryParse(txtNet.Text, out double netVal) ||
                    netVal <= 0)
                {
                    MessageBox.Show(
                        string.Equals(MainClass.Language, "ar")
                            ? "يجب أن يكون الصافي أكبر من الصفر"
                            : "The net must be greater than zero.",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Client for credit
                if (cmbPayType.SelectedIndex == 0 &&
                    GetSelectedValue<int>(LookUpEdit1) == 1)
                {
                    MessageBox.Show(
                        string.Equals(MainClass.Language, "ar")
                            ? "يجب اختيار العميل"
                            : "The client must be selected.",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Payment form
                if ((InvObj.ShowPayForm || InvObj.PaymentStatus) &&
                    (Invoic.ProcType == 1 || Invoic.ProcType == 2) &&
                    cmbPayType.SelectedIndex > 0 &&
                    (Invoic.PaymentStatus == PaymentStatus.Unpaid ||
                     Invoic.PaymentStatus == PaymentStatus.PaidPartially ||
                     Invoic.ISNew))
                {
                    PayBill();
                    return;
                }

                // Set payment amounts
                if (Invoic.PayType == -1)
                {
                    Invoic.Paycash = 0; Invoic.PayATM = 0;
                }
                else if (Invoic.PayType == 1)
                {
                    Invoic.Paycash = Invoic.Net;
                    Invoic.PayATM = 0;
                    Invoic.Paid = Invoic.Net;
                }
                else if (Invoic.PayType == 2)
                {
                    Invoic.Paycash = 0;
                    Invoic.PayATM = Invoic.Net;
                    Invoic.Paid = Invoic.Net;
                }

                SaveAndPrint();
            }
            catch (Exception ex)
            {
                Logger.Error("ValidateInvoice: " + ex.Message);
            }
        }

        private async void SaveAndPrint()
        {
            try
            {
                IsEnabled = false;
                Cursor = Cursors.Wait;

                if (!Invoic.ISNew &&
                    (Invoic.ProcType == 1 || Invoic.ProcType == 2) &&
                    !(InvObj.PaymentStatus && !Invoic.IsUpdated))
                {
                    if (EnableE_Invoice)
                    {
                        var msg = new MsgGeneralAlter();
                        msg.txtAlarm.Text =
                            "بناءً على توجيهات هيئة الزكاة والضريبة والجمارك " +
                            "يمنع تعديل أو حذف أي فاتورة ضريبية";
                        msg.ShowDialog();
                        return;
                    }
                    if (!allowEditInvoice)
                    {
                        MessageBox.Show(
                            string.Equals(MainClass.Language, "ar")
                                ? "ليس لديك صلاحية لتعديل الفاتورة"
                                : "You do not have permission to modify.",
                            "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                if (InvObj.SaleManIsRequire && Invoic.Saleman <= 0)
                {
                    MessageBox.Show("يجب اختيار المندوب",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (InvObj.CashCustRequire &&
                    (string.IsNullOrEmpty(Invoic.CashCustomerName) ||
                     Invoic.CashCustomerName.Equals(DBNull.Value)))
                {
                    MessageBox.Show("يجب إدخال عميل نقدي",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var invoiceOper = new InvoiceOper();
                Invoice invoice = invoiceOper.MappingInvoice(ref Invoic);
                Entry entry = (ProcType == 1 || ProcType == 2)
                    ? invoiceOper.BindToEntry(invoice)
                    : null;

                bool saved = invoiceOper.SaveInvoice(invoice, entry, Invoic.ISNew);

                if (saved)
                {
                    ThreadContext.Properties["EmpID"] = MainClass.EmpNo;
                    string action = Invoic.ISNew ? "تم حفظ" : "تم تعديل";
                    Logger.Info(action + " " +
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

                if (saved && Sync.ActiveSync &&
                    InvObj.SyncInv && Sync.SyncType > 0)
                {
                    if (!InvObj.SyncEntry) entry = null;
                    if (Sync.BranchType != 1)
                        await invoiceOper.SyncInvoice(invoice, entry, Invoic.ISNew);
                }

                Inventory.UpdateItemStock(invoice);

                if (saved)
                {
                    var frmMsg = new frmSavedMsg();
                    frmMsg.ShowDialog();
                    if (frmMsg.Pressed == 1)
                        CLR();
                    else if (frmMsg.Pressed == 2)
                    {
                        Invoic.ISNew = false;
                        Invoic.IsPrinted = true;
                        Invoic.IsUpdated = false;
                        txtNo.Background =
                            new SolidColorBrush(
                                Color.FromRgb(245, 245, 245));
                        txtNo.Foreground =
                            new SolidColorBrush(Colors.Black);
                    }
                    else if (frmMsg.Pressed == 3)
                    {
                        CLR();
                        Close();
                    }
                    else CLR();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء الحفظ:\n" + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Error("SaveAndPrint: " + ex.Message);
            }
            finally
            {
                IsEnabled = true;
                Cursor = Cursors.Arrow;
            }
        }

        public void PayBill()
        {
            if (!Invoic.ISNew &&
                (Invoic.ProcType == 1 || Invoic.ProcType == 2) &&
                !(InvObj.PaymentStatus && !Invoic.IsUpdated))
            {
                var msg = new MsgGeneralAlter();
                msg.txtAlarm.Text =
                    "بناءً على توجيهات هيئة الزكاة والضريبة والجمارك " +
                    "يمنع تعديل أو حذف أي فاتورة ضريبية";
                msg.ShowDialog();
            }
            else if (InvoiceOper.PayInvoice(ref Invoic, InvObj))
            {
                SaveAndPrint();
            }
        }

        private async void DeleteInv()
        {
            var invoiceOper = new InvoiceOper();
            Invoice invoice = invoiceOper.MappingInvoice(ref Invoic);
            Entry entry = null;
            if ((ProcType == 1 || ProcType == 2) && InvType == 2)
            {
                entry = invoiceOper.BindToEntry(invoice);
                if (entry == null) return;
            }
            if (Invoic.ISNew) return;

            if ((InvType == 2 && ProcType == 4) ||
                (InvType == 2 && !EnableE_Invoice))
            {
                if (!InvoiceOper.DeleteInvoice(Invoic)) return;
                invoice.IsDeleted = true;
                if (entry != null) entry.ISDeleted = true;
                invoice.Sent = false;
                if (entry != null) entry.Sent = false;

                if (Sync.ActiveSync && InvObj.SyncInv && Sync.SyncType > 0)
                {
                    if (!InvObj.SyncEntry) entry = null;
                    await invoiceOper.SyncInvoice(invoice, entry, Invoic.ISNew);
                }

                ThreadContext.Properties["EmpID"] = MainClass.EmpNo;
                Logger.Info("تم حذف فاتورة رقم " + Invoic.InvoiceNo +
                    " بواسطة: " + MainClass.UserName);
                CLR();
            }
            else
            {
                MessageBox.Show("لا يمكن حذف الفاتورة", "تحذير",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Print & Report

        private void RptPrint(Invoice inv, int type)
        {
            if (GridControl1.Items.Count == 0)
            {
                MessageBox.Show("لا توجد عمليات بالجدول",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(print.RptUrl))
            {
                MessageBox.Show("يجب تحديد مسار التقرير",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(print.RptName))
                print.RptName = "rptPOSA4.repx";
            if (ProcType == 4)
                print.RptName = "rptPricingInv.repx";

            string path;
            if (((inv.InvoiceType == InvoiceType.Sale ||
                  inv.InvoiceType == InvoiceType.POS ||
                  inv.InvoiceType == (InvoiceType)21) &&
                  inv.ProcType == 2) ||
                (inv.InvoiceType == (InvoiceType)21 && inv.ProcType == 1))
            {
                print.RptName = "rptPOSA4CreditNote.repx";
                path = Path.Combine(print.RptUrl, print.RptName);
                if (!Directory.Exists(print.RptUrl) || !File.Exists(path))
                {
                    MessageBox.Show("مسار التقارير غير موجود",
                        "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    print.RptName = "rptPOSA4.repx";
                }
            }

            path = Path.Combine(print.RptUrl, print.RptName);
            if (!Directory.Exists(print.RptUrl) || !File.Exists(path))
            {
                MessageBox.Show("مسار التقارير غير موجود",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(print.defPrinter))
                print.defPrinter = MainClass.ReportsPrinter;

            print.Printing(type, print.BindToData(inv),
                print.RptUrl, print.RptName,
                print.defPrinter, print.kitchenprinter, print.PrintNo);
        }

        private void RptPrintPDF(Invoice inv, int type)
        {
            if (GridControl1.Items.Count == 0)
            {
                MessageBox.Show("لا توجد عمليات بالجدول",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(print.RptUrl))
            {
                MessageBox.Show("يجب تحديد مسار التقرير",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(print.RptName))
                print.RptName = "rptPOSA4.repx";
            if (ProcType == 4)
                print.RptName = "rptPricingInv.repx";

            string path = Path.Combine(print.RptUrl, print.RptName);
            if (!Directory.Exists(print.RptUrl) || !File.Exists(path))
            {
                MessageBox.Show("مسار التقارير غير موجود",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            print.defPrinter = "Microsoft Print to PDF";
            print.Printing(type, print.BindToData(inv),
                print.RptUrl, print.RptName,
                print.defPrinter, print.kitchenprinter, print.PrintNo);
        }

        #endregion

        #region Customer

        private void ShowCustBalance()
        {
            try
            {
                if (string.IsNullOrEmpty(
                    (LookUpEdit1.SelectedItem as DataRowView)?["name"]
                    ?.ToString())) return;

                string custName =
                    (LookUpEdit1.SelectedItem as DataRowView)?["name"]
                    ?.ToString() ?? "";

                var da = new SqlDataAdapter(
                    "select Code from Accounts_Index where AName=N'" +
                    custName + "' " + Accounting.BranchCondition, conn1);
                var dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count == 0) return;

                string accCondition =
                    " and Entry_sub.acc_no=" + dt.Rows[0][0];
                string branchCond = "";
                if (MainClass.BranchNo != -1)
                    branchCond = "Entry.branch=" + MainClass.BranchNo +
                        " and Entry_sub.branch=" + MainClass.BranchNo + " and ";

                var da2 = new SqlDataAdapter(
                    "select sum(Entry_sub.dept) as dept," +
                    "sum(Entry_sub.credit) as credit " +
                    "from Entry,Entry_sub where " + branchCond +
                    "Entry.IS_Deleted=0 and Entry.state=1 " +
                    "and Entry.GlobalId=Entry_sub.EntryGlobalId" +
                    accCondition +
                    " group by Entry_sub.acc_no", conn1);
                var dt2 = new DataTable();
                da2.Fill(dt2);

                double dept = 0, credit = 0;
                foreach (DataRow row in dt2.Rows)
                {
                    dept += Convert.ToDouble(row["dept"]);
                    credit += Convert.ToDouble(row["credit"]);
                }

                if (dept > credit)
                    txtBalance.Text =
                        $"{Math.Round(dept - credit, 3):0.##}";
                else if (credit > dept)
                    txtBalance.Text =
                        $"{Math.Round(credit - dept, 3):0.##} دائن";
                else
                    txtBalance.Text = "0";
            }
            catch (Exception ex)
            {
                Logger.Error("ShowCustBalance: " + ex.Message);
            }
        }

        private void ShowCustBalancePreviews()
        {
            try
            {
                string custName =
                    (LookUpEdit1.SelectedItem as DataRowView)?["name"]
                    ?.ToString() ?? "";
                if (string.IsNullOrEmpty(custName)) return;

                var da = new SqlDataAdapter(
                    "select Code from Accounts_Index where AName=N'" +
                    custName + "' " + Accounting.BranchCondition, conn1);
                var dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count == 0) return;

                string accCond = " and Entry_sub.acc_no=" + dt.Rows[0][0];
                string branchCond = "";
                if (MainClass.BranchNo != -1)
                    branchCond = "Entry.branch=" + MainClass.BranchNo +
                        " and Entry_sub.branch=" + MainClass.BranchNo + " and ";

                var da2 = new SqlDataAdapter(
                    "select sum(Entry_sub.dept) as dept," +
                    "sum(Entry_sub.credit) as credit " +
                    "from Entry,Entry_sub where " + branchCond +
                    "Entry.IS_Deleted=0 and Entry.state=1 " +
                    "and Entry.GlobalId=Entry_sub.EntryGlobalId" +
                    accCond, conn1);
                var dt2 = new DataTable();
                da2.Fill(dt2);

                double dept = 0, credit = 0;
                foreach (DataRow row in dt2.Rows)
                {
                    dept += Convert.ToDouble(row["dept"]);
                    credit += Convert.ToDouble(row["credit"]);
                }

                if (dept > credit)
                    txtBalancePreviews.Text =
                        $"{Math.Round(dept - credit, 3):0.##}";
                else if (credit > dept)
                    txtBalancePreviews.Text =
                        $"{Math.Round(credit - dept, 3):0.##} دائن";
                else
                    txtBalancePreviews.Text = "0";
            }
            catch (Exception ex)
            {
                Logger.Error("ShowCustBalancePreviews: " + ex.Message);
            }
        }

        private void CheckClient()
        {
            string searchText = LookUpEdit1.Text;
            if (string.IsNullOrEmpty(searchText)) { SrchByNameClint(); return; }

            var da = new SqlDataAdapter(
                "select name,id from Customers where IS_Deleted=0 " +
                "and (type=1 or type=3) " +
                "and (name=N'" + searchText +
                "' or mobile=N'" + searchText + "')", conn);
            var dt = new DataTable();
            da.Fill(dt);

            if (dt.Rows.Count > 0)
            {
                Invoic.Customer = Convert.ToInt32(dt.Rows[0]["id"]);
                SetComboByValue(LookUpEdit1, Invoic.Customer);
                txtBarcode.Focus();
            }
            else
            {
                var r = MessageBox.Show(
                    string.Equals(MainClass.Language, "ar")
                        ? "العميل غير موجود، هل تريد إضافة العميل كعميل نقدي؟"
                        : "The customer is not found, Add?",
                    "تنبيه", MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    var frm = new frmCustomers();
                    MainClass.ApplyPermissionToForm(frm);
                    MainClass.DoApplyUserSett(frm);
                    frm.Title = "تعريف عميل";
                    frm.txtName.Text = searchText;
                    frm.Type = 1;
                    frm.ShowDialog();
                    if (frm.isDone && frm.ClientId > -1)
                    {
                        LoadCustomers();
                        Invoic.Customer = frm.ClientId;
                        SetComboByValue(LookUpEdit1, Invoic.Customer);
                        txtBarcode.Focus();
                    }
                }
            }
        }

        private void SrchByNameClint()
        {
            try
            {
                var da = new SqlDataAdapter(
                    "select id,name from Customers where IS_Deleted=0 " +
                    "and name=N'" + LookUpEdit1.Text +
                    "' and (type=1 or type=3)", conn);
                var dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                    SetComboByValue(LookUpEdit1,
                        Convert.ToInt32(dt.Rows[0]["id"]));
                else
                    addNewClint();
            }
            catch (Exception ex)
            {
                Logger.Error("SrchByNameClint: " + ex.Message);
            }
        }

        private void addNewClint()
        {
            try
            {
                var frm = new frmSrchClient();
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.Type = 1;
                frm.txtClientName.Text =
                    string.Equals(LookUpEdit1.Text, "عميل عام")
                        ? "" : LookUpEdit1.Text;
                if (cmbPayType.SelectedIndex == 0)
                    frm.PostponeClient = true;
                frm.ShowDialog();
                if (!string.IsNullOrEmpty(frm.Clientname))
                {
                    LoadCustomers();
                    SetComboByValue(LookUpEdit1, frm.ClientId);
                    Invoic.Customer = GetSelectedValue<int>(LookUpEdit1);
                    ShowCustBalancePreviews();
                    Invoic.BalancePreviews =
                        Convert.ToSingle(txtBalancePreviews.Text);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("addNewClint: " + ex.Message);
            }
        }

        #endregion

        #region Discount

        private void CalcDiscount()
        {
            if (!string.IsNullOrWhiteSpace(txtInvDiscVal.Text))
            {
                if (double.TryParse(txtInvDiscVal.Text,
                    out double discVal))
                {
                    Invoic.InvDiscount = discVal;
                    ItemOper.CalcRows(ref Invoic);
                    BindingDGV();
                }
            }
            else
            {
                Invoic.InvDiscount = 0;
                txtInvDiscPerc.Text = "0";
                ItemOper.CalcRows(ref Invoic);
                BindingDGV();
            }
        }

        private void CalcDiscountPerc()
        {
            if (!string.IsNullOrWhiteSpace(txtInvDiscPerc.Text))
            {
                if (double.TryParse(txtInvDiscPerc.Text,
                    out double percVal))
                {
                    Invoic.InvDiscount = Convert.ToDouble(
                        InvoiceOper.GetInvDiscountByPercentage(
                            Invoic.SumPrice, percVal));
                    ItemOper.CalcRows(ref Invoic);
                    BindingDGV();
                }
            }
            else
            {
                txtInvDiscPerc.Text = "0";
                Invoic.InvDiscount = 0;
                ItemOper.CalcRows(ref Invoic);
                BindingDGV();
            }
        }

        #endregion

        #region Import / Export

        public void ImportInventoryItems(List<InvoiceItem> importedItems)
        {
            if (importedItems.Count <= 0) return;
            foreach (var importedItem in importedItems)
            {
                SearchByID(importedItem.ItemId, 0, "", "");
                var existing = Invoic.InvoiceItems
                    .FirstOrDefault(i => i.ItemId == importedItem.ItemId);
                if (existing != null)
                {
                    existing.ItemPrice = importedItem.ItemPrice;
                    existing.ItemQuantity = importedItem.ItemQuantity;
                    ItemOper.CalcRows(ref Invoic);
                    BindingDGV();
                }
            }
        }

        private void StoreEditHistory()
        {
            try
            {
                int entryType = ProcType == 2 ? 22 : 2;
                if (conn1.State != ConnectionState.Open) conn1.Open();

                int invNo = 0;
                int.TryParse(txtNo.Text, out invNo);

                new SqlCommand(
                    "INSERT His_Entry(id,date,doc_no,type,state,notes," +
                    "branch,IS_Deleted,EmpID) Select id,date,doc_no,type," +
                    "state,notes,branch,IS_Deleted,EmpID From Entry " +
                    "Where type=" + entryType + " and doc_no=" + invNo,
                    conn1).ExecuteNonQuery();

                int procId = Convert.ToInt32(new SqlCommand(
                    "select max(ProcID) From His_Entry " +
                    "Where type=" + entryType + " and doc_no=" + invNo,
                    conn1).ExecuteScalar());

                var da = new SqlDataAdapter(
                    "Select res_id,dept,credit,acc_no,Entry_sub.notes," +
                    "Entry_sub.branch,CCcode From Entry_sub,Entry " +
                    "Where Entry.GlobalId=Entry_sub.EntryGlobalId " +
                    "and Entry.type=" + entryType +
                    " and Entry.doc_no=" + invNo, conn1);
                var dt = new DataTable();
                da.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    var cmd = new SqlCommand(
                        "INSERT into His_Entry_sub(procId,res_id,dept,credit," +
                        "acc_no,notes,branch,CCcode) " +
                        "values(@procId,@res_id,@dept,@credit,@acc_no," +
                        "@notes,@branch,@CCcode)", conn1);
                    cmd.Parameters.AddWithValue("@procId", procId);
                    cmd.Parameters.AddWithValue("@res_id", row["res_id"]);
                    cmd.Parameters.AddWithValue("@dept", row["dept"]);
                    cmd.Parameters.AddWithValue("@credit", row["credit"]);
                    cmd.Parameters.AddWithValue("@acc_no", row["acc_no"]);
                    cmd.Parameters.AddWithValue("@notes", row["notes"]);
                    cmd.Parameters.AddWithValue("@branch", row["branch"]);
                    cmd.Parameters.AddWithValue("@CCcode", row["CCcode"]);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("StoreEditHistory: " + ex.Message);
            }
        }

        private bool isInvExist()
        {
            try
            {
                var da = new SqlDataAdapter(
                    "select id from Inv where branch=" + MainClass.BranchNo +
                    " and inv_type=" + InvType +
                    " and proc_type=" + ProcType +
                    " and id=" + txtNo.Text, conn);
                var dt = new DataTable();
                da.Fill(dt);
                return dt.Rows.Count > 0;
            }
            catch { return false; }
        }

        private void AddInvLocally(InvGLobalIDOff newInv)
        {
            var list = new List<InvGLobalIDOff>();
            string path = Path.Combine(
                System.Windows.Forms.Application.StartupPath,
                "Data", "InvGlobalID.json");
            if (File.Exists(path))
            {
                string content = File.ReadAllText(path);
                if (!string.IsNullOrWhiteSpace(content))
                    list = JsonConvert.DeserializeObject<
                        List<InvGLobalIDOff>>(content);
            }
            list.Add(newInv);
            File.WriteAllText(path,
                JsonConvert.SerializeObject(list, Formatting.Indented));
        }

        #endregion

        #region Glasses

        private void glassesOptions()
        {
            if (GridControl1.Items.Count < 1)
            {
                MessageBox.Show("الرجاء إضافة صنف للفاتورة");
                return;
            }
            var item = GridControl1.SelectedItem as InvoiceItem;
            if (item == null)
            {
                MessageBox.Show("الرجاء وضع المؤشر على الصنف");
                return;
            }
            var frm = new frmGlasses();
            frm.InvGlobalID = Invoic.InvGlobalID;
            frm.ItemId = item.ItemId;
            foreach (var inv in Invoic.InvoiceItems)
            {
                if (inv.ItemId == item.ItemId)
                {
                    frm.Glasses = (List<Glass>)inv.Glasses;
                    frm.ShowDialog();
                    inv.Glasses = frm.Glasses;
                    break;
                }
            }
        }

        #endregion

        #region Button Events

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!Common.CheckProiedAcc(txtDate.DateTime))
            {
                MessageBox.Show(
                    "لا يمكن أن يكون التاريخ خارج الفترة المحاسبية",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Invoic.IsPrinted = false;
            ValidateInvoice();
        }

        private void btnSavePrint_Click(object sender, RoutedEventArgs e)
        {
            if (!Common.CheckProiedAcc(txtDate.DateTime))
            {
                MessageBox.Show(
                    "لا يمكن أن يكون التاريخ خارج الفترة المحاسبية",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Invoic.IsPrinted = true;
            ValidateInvoice();
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
            {
                MainClass.ReturnYearPreviews = false;
                CLR();
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (Invoic.ISNew) return;
            DeleteInv();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (Invoic.IsPrinted && !Invoic.IsUpdated)
                RptPrint(new InvoiceOper().MappingInvoice(ref Invoic), 1);
            else
                MessageBox.Show("لا يمكن طباعة الفاتورة قبل الحفظ");
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            if (Invoic.IsPrinted && !Invoic.IsUpdated)
                RptPrint(new InvoiceOper().MappingInvoice(ref Invoic), 2);
            else
                MessageBox.Show("لا يمكن معاينة الفاتورة قبل الحفظ");
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                Navigate("select top 1 * from Inv where inv_type=" +
                    InvType + " and proc_type=" + ProcType +
                    " and IS_Deleted=0 and branch=" +
                    MainClass.BranchNo + " order by id asc");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                Navigate("select top 1 * from Inv where inv_type=" +
                    InvType + " and proc_type=" + ProcType +
                    " and IS_Deleted=0 and branch=" +
                    MainClass.BranchNo + " order by id desc");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
            {
                int.TryParse(txtNo.Text, out int currentNo);
                Navigate("select top 1 * from Inv where inv_type=" +
                    InvType + " and proc_type=" + ProcType +
                    " and IS_Deleted=0 and id>" + currentNo +
                    " and branch=" + MainClass.BranchNo +
                    " order by id asc");
            }
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
            {
                int.TryParse(txtNo.Text, out int currentNo);
                Navigate("select top 1 * from Inv where inv_type=" +
                    InvType + " and proc_type=" + ProcType +
                    " and IS_Deleted=0 and id<" + currentNo +
                    " and branch=" + MainClass.BranchNo +
                    " order by id desc");
            }
        }

        private void btnInvSrch_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvoiceSrch();
            frm.cmbProcType.IsEnabled = false;
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.ProcType = ProcType;
            frm.InvType = InvType;
            if (InvObj.PaymentStatus)
                frm.ckUnPaid.Visibility    = Visibility.Visible;
            frm.ShowDialog();
            if (frm.ISDone &&
                !string.Equals(frm.InvGlobalID, "-1"))
                Navigate("select * from Inv where branch=" +
                    MainClass.BranchNo +
                    " and InvGlobalID=N'" + frm.InvGlobalID + "'");
        }

        private void btnRepaid_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPayType.SelectedIndex > 0 &&
                (Invoic.PaymentStatus == PaymentStatus.Unpaid ||
                 Invoic.PaymentStatus == PaymentStatus.PaidPartially) &&
                !Invoic.ISNew)
                PayBill();
        }

        private void btnZoom_Click(object sender, RoutedEventArgs e)
            => addNewClint();

        private void btnAddNewItem_Click(object sender, RoutedEventArgs e)
        {
            if (!User.EditItemInfo)
            {
                MessageBox.Show("لا يوجد لديك صلاحية لهذه العملية");
                return;
            }
            var frm = new frmItems();
            frm.Activate();
            frm.ShowDialog();
        }

        private void btnCustAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var frm = new frmCustomers();
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.Title = InvType == 2 ? "تعريف عميل" : "تعريف مورد";
                frm.Type = InvType == 2 ? 1 : 2;
                frm.ShowDialog();
                if (frm.isDone) LoadCustomers();
            }
            catch (Exception ex)
            {
                Logger.Error("btnCustAdd: " + ex.Message);
            }
        }

        private void btnAddCostCenter_Click(object sender, RoutedEventArgs e)
        {
            int prevVal = GetSelectedValue<int>(cmbCostCenter);
            var frm = new frmCostCenter();
            frm.Activate();
            frm.ShowDialog();
            LoadCostCenters();
            try { SetComboByValue(cmbCostCenter, prevVal); } catch { }
        }

        private void cmbAddSalesMen_Click(object sender, RoutedEventArgs e)
        {
            int prevVal = GetSelectedValue<int>(cmbSalesMen);
            var frm = new frmSalesMen();
            frm.Activate();
            frm.ShowDialog();
            LoadSalesMen();
            try { SetComboByValue(cmbSalesMen, prevVal); } catch { }
        }

        private void btnInsertLinkedInv_Click(
            object sender, RoutedEventArgs e)
        {
            if (InvType == 2 && ProcType == 2)
                InsertLinkedReturn(2, 1);
            else if (InvType == 2 && ProcType == 1)
                InsertLinkedFromQuotation();
            else if (InvType == 21 && ProcType == 2)
                InsertLinkedCreditNote(2);
            else if (InvType == 21 && ProcType == 1)
                InsertLinkedCreditNote(1);
        }

        private void InsertLinkedReturn(int procType, int invertoryImpact)
        {
            if (MainClass.ReturnYearPreviews)
            {
                MainClass.originalConnStr = MainClass.connstr;
                MainClass.connstr =
                    "server=" + MainClass.Server +
                    ";database=" +
                    CmbYearPreviews.SelectedValue +
                    ";trusted_connection=true";
                conn = MainClass.ConnObj();
            }
            CLR();
            InvoiceOper.InsertFromInv(ref Invoic, 1);
            Invoic.ProcType = procType;
            Invoic.InvertoryImpact = invertoryImpact;
            Invoic.InvNote = "";
            Invoic.PayATM = 0;
            Invoic.Paycash = 0;
            Invoic.Paid = 0;
            Invoic.Remainder = 0;
            Invoic.ReffNo = Invoic.InvCombinedId;
            Invoic.RefDate = Invoic.InvDate;
            txtRefDate.IsEnabled = false;
            txtRefNo.IsEnabled = false;
            Invoic.InvDate = DateTime.Now;
            Invoic.InvTime = DateTime.Now;
            LoadInvNo();
            CalcItemsDiscountRates();
            Invoic.ISNew = true;
            ItemOper.CalcRows(ref Invoic);
            BindingDGV();
            BindingControls();
            Invoic.User = MainClass.EmpNo;
        }

        private void InsertLinkedFromQuotation()
        {
            if (MainClass.ReturnYearPreviews)
            {
                MainClass.originalConnStr = MainClass.connstr;
                MainClass.connstr =
                    "server=" + MainClass.Server +
                    ";database=" +
                    CmbYearPreviews.SelectedValue +
                    ";trusted_connection=true";
                conn = MainClass.ConnObj();
            }
            CLR();
            InvoiceOper.InsertFromInv(ref Invoic, 4);
            Invoic.ProcType = 1;
            Invoic.InvertoryImpact = 2;
            Invoic.ReffNo = "";
            Invoic.RefDate = DateTime.Now;
            LoadInvNo();
            Invoic.ISNew = true;
            Invoic.InvNote = "";
            Invoic.PayType = 1;
            Invoic.Paycash = 0;
            Invoic.PayATM = 0;
            Invoic.Paid = 0;
            Invoic.InvDate = DateTime.Now;
            Invoic.InvTime = DateTime.Now;
            CalcItemsDiscountRates();
            ItemOper.CalcRows(ref Invoic);
            BindingDGV();
            BindingControls();
            Invoic.User = MainClass.EmpNo;
        }

        private void InsertLinkedCreditNote(int procType)
        {
            if (MainClass.ReturnYearPreviews)
            {
                MainClass.originalConnStr = MainClass.connstr;
                MainClass.connstr =
                    "server=" + MainClass.Server +
                    ";database=" +
                    CmbYearPreviews.SelectedValue +
                    ";trusted_connection=true";
                conn = MainClass.ConnObj();
            }
            InvType_credit = 21;
            CLR();
            InvoiceOper.InsertFromInv(ref Invoic, 1);
            Invoic.InvoiceType = (InvoiceType)21;
            Invoic.ProcType = procType;
            Invoic.InvertoryImpact = 1;
            Invoic.InvNote = "";
            Invoic.PayATM = 0;
            Invoic.Paycash = 0;
            Invoic.Paid = 0;
            Invoic.Remainder = 0;
            Invoic.ReffNo = Invoic.InvCombinedId;
            Invoic.RefDate = Invoic.InvDate;
            txtRefDate.IsEnabled = false;
            txtRefNo.IsEnabled = false;
            Invoic.InvDate = DateTime.Now;
            Invoic.InvTime = DateTime.Now;
            LoadInvNo();
            CalcItemsDiscountRates();
            foreach (var item in Invoic.InvoiceItems)
            {
                if (item.ItemType == 3 || item.ItemType == 5)
                    item.InvertoryImpact = 0;
                else
                    item.InvertoryImpact = Invoic.InvertoryImpact;
            }
            Invoic.ISNew = true;
            ItemOper.CalcRows(ref Invoic);
            BindingDGV();
            BindingControls();
            Invoic.User = MainClass.EmpNo;
        }

        private void CalcItemsDiscountRates()
        {
            double totalDisc = 0;
            foreach (var item in Invoic.InvoiceItems)
            {
                item.InvertoryImpact = Invoic.InvertoryImpact;
                if (Invoic.InvDiscount > 0)
                    totalDisc += ItemOper.ItemDiscountRateFromInvDiscount(
                        new decimal(Invoic.SumPrice),
                        new decimal(Invoic.InvDiscount),
                        item.ItemSumPrice,
                        0 - (Invoic.PriceIncVAT ? 1 : 0),
                        item.ItemVatPerc);
            }
            Invoic.InvDiscount = totalDisc;
        }

        private void btnSaveQuotation_Click(
            object sender, RoutedEventArgs e)
        {
            if (!Invoic.ISNew)
            {
                MessageBox.Show(
                    string.Equals(MainClass.Language, "ar")
                        ? "لا يمكن تعديل فاتورة محفوظة من قبل"
                        : "A previously saved invoice cannot be modified.",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ProcType = 4;
            Invoic.ProcType = 4;
            foreach (var item in Invoic.InvoiceItems)
                item.InvertoryImpact = 0;
            ValidateInvoice();
            ProcType = 1;
            Invoic.ProcType = 1;
            CLR();
        }

        private void btnprint2_Click(object sender, RoutedEventArgs e)
        {
            if (Invoic.IsPrinted && !Invoic.IsUpdated)
            {
                var inv = new InvoiceOper().MappingInvoice(ref Invoic);
                print.RptName = "rptPOSA4Vat.repx";
                RptPrint(inv, 1);
                print.RptName = "";
            }
            else
                MessageBox.Show("لا يمكن طباعة الفاتورة قبل الحفظ");
        }

        private void BtnReceipts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!isInvExist())
                {
                    MessageBox.Show("لم يتم حفظ الفاتورة");
                    return;
                }
                var da = new SqlDataAdapter(
                    "select Forms.name as form_name,Forms.id as form_id," +
                    "Forms.buttonName as btnName,Forms.Parent_id," +
                    "Forms.nameEn from User_Permissions,Forms " +
                    "where User_Permissions.Form_id=Forms.id " +
                    "and buttonName=N'stripSandQclient' and id=52002 " +
                    "and user_id=" + MainClass.UserID, conn1);
                var dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    var frm = new frmSandQ();
                    MainClass.ApplyPermissionToForm(frm);
                    frm.Show();
                    frm.Activate();
                    frm.cmbClients.SelectedValue =
                        LookUpEdit1.SelectedValue;
                    frm.txtVal.Text = txtNet.Text;
                    frm.txtNotes.Text = txtNote.Text +
                        "   خاصة العميل:" + LookUpEdit1.Text;
                }
                else
                    MessageBox.Show("ليس لديك صلاحية",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.Error("BtnReceipts: " + ex.Message);
            }
        }

        private void BtnSendToEta_Click(object sender, RoutedEventArgs e)
        {
            if (Invoic.ISNew)
            {
                MessageBox.Show(
                    string.Equals(MainClass.Language, "ar")
                        ? "الرجاء حفظ الفاتورة قبل إرسالها"
                        : "Please save the invoice before sending it.");
                return;
            }
            if ((Invoic.InvoiceType == InvoiceType.Sale) &&
                (Invoic.ProcType == 1 || Invoic.ProcType == 2))
            {
                var inv = new InvoiceOper().MappingInvoice(ref Invoic);
                if (EtaSetting.Receipt)
                    new EtaReciptService().sendERecitp(inv);
                else
                    new EtaService().sendtoETA(inv);
            }
        }

        private async void BtnEgyPrint_Click(
            object sender, RoutedEventArgs e)
        {
            if (Invoic.ISNew)
            {
                MessageBox.Show(
                    string.Equals(MainClass.Language, "ar")
                        ? "الرجاء حفظ الفاتورة قبل طباعتها"
                        : "Please save the invoice before printing it.");
                return;
            }
            if (string.IsNullOrEmpty(Invoic.UUID) ||
                Invoic.UUID.Equals(DBNull.Value) ||
                Invoic.UUID == null)
            {
                MessageBox.Show(
                    string.Equals(MainClass.Language, "ar")
                        ? "الرجاء إرسال الفاتورة قبل طباعتها"
                        : "Please sent the invoice before printing it.");
                return;
            }
            await new EtaService().PrintEInvoice(Invoic.UUID.Trim());
        }

        private async void BtnSendEmail_Click(
            object sender, RoutedEventArgs e)
        {
            try
            {
                if (Invoic.InvoiceItems.Count == 0)
                {
                    MessageBox.Show(
                        string.Equals(MainClass.Language, "ar")
                            ? "لا يمكن إرسال الفاتورة قبل الحفظ"
                            : "An invoice cannot be sent before saving.",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var inv = new InvoiceOper().MappingInvoice(ref Invoic);
                if (string.IsNullOrEmpty(print.RptName))
                    print.RptName = "rptPOSA4.repx";
                if (ProcType == 4)
                    print.RptName = "rptPricingInv.repx";
                string path = Path.Combine(print.RptUrl, print.RptName);

                if (conn.State == ConnectionState.Open) conn.Close();
                conn.Open();
                int custId = GetSelectedValue<int>(LookUpEdit1);
                var cmd = new SqlCommand(
                    "select name,email from Customers where id=" + custId,
                    conn);
                var reader = cmd.ExecuteReader();
                reader.Read();
                if (!string.IsNullOrEmpty(reader["email"].ToString()))
                {
                    string recEmail = reader["email"].ToString();
                    string custName = reader["name"].ToString();
                    var rpt =
                        DevExpress.XtraReports.UI.XtraReport.FromFile(path);
                    rpt.DataSource = print.BindToData(inv);
                    print.SendInvoiceToEmail(rpt, recEmail, custName);
                }
                else
                    MessageBox.Show("لا يوجد إيميل للعميل");
                reader.Close();
                conn.Close();
            }
            catch (Exception ex)
            {
                Logger.Error("BtnSendEmail: " + ex.Message);
                MessageBox.Show(ex.Message);
            }
        }

        private void BtnImportDocument_Click(
            object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter =
                    "Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png" +
                    "|PDF Files (*.pdf)|*.pdf",
                Title = "تحديد الملفات",
                Multiselect = true
            };
            string path = Path.Combine(
                System.Windows.Forms.Application.StartupPath, "Data");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            if (dlg.ShowDialog() == true)
                InvoiceOper.filePathDocument = dlg.FileNames;
        }

        private void BtnShowDocument_Click2(
            object sender, RoutedEventArgs e)
        {
            if (Invoic.ISNew)
            {
                MessageBox.Show("يجب حفظ الفاتورة أولاً",
                    "المدقق", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            var dtt = new DataTable();
            InvoiceOper.getdocuments(ref dtt, Invoic.InvGlobalID);
            if (dtt.Rows.Count > 0)
            {
                var frm = new Frmshowdocument();
                int i = 1;
                foreach (DataRow row in dtt.Rows)
                  //  frm.DataGridView1.Rows.Add(
                  //      i++, row["FileName"], row["FileUrl"]);
                frm.type = 2;
                frm.GlobalIdDoc = Invoic.InvGlobalID;
                frm.Show();
            }
            else
                MessageBox.Show("لا يوجد مستندات",
                    "المدقق", MessageBoxButton.OK,
                    MessageBoxImage.Information);
        }

        private void BtnShowDocument_Click(object sender, RoutedEventArgs e)
        {
            if (Invoic.ISNew)
            {
                DXMessageBox.Show("يجب حفظ الفاتورة أولاً",
                    "المدقق", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dtt = new DataTable();
            InvoiceOper.getdocuments(ref dtt, Invoic.InvGlobalID);

            if (dtt.Rows.Count > 0)
            {
                var docDlg = new Frmshowdocument();

                var displayTable = new DataTable();
                displayTable.Columns.Add("RowNo", typeof(int));
                displayTable.Columns.Add("FileName", typeof(string));
                displayTable.Columns.Add("FileUrl", typeof(string));

                for (int i = 0; i < dtt.Rows.Count; i++)
                {
                    displayTable.Rows.Add(
                        i + 1,
                        dtt.Rows[i]["FileName"]?.ToString() ?? "",
                        dtt.Rows[i]["FileUrl"]?.ToString() ?? "");
                }

                docDlg.DataGridView1.ItemsSource = displayTable.DefaultView;
                docDlg.type = 2;
                docDlg.GlobalIdDoc = Invoic.InvGlobalID;
                docDlg.Show();
            }
            else
            {
                DXMessageBox.Show("لا يوجد مستندات",
                    "المدقق", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void SendWhatsapp_Click(
            object sender, RoutedEventArgs e)
            => printwhatsapp();

        private async void printwhatsapp()
        {
            try
            {
                if (Invoic.InvoiceItems.Count == 0)
                {
                    MessageBox.Show(
                        string.Equals(MainClass.Language, "ar")
                            ? "لا يمكن إرسال الفاتورة قبل الحفظ"
                            : "An invoice cannot be sent before saving.",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var inv = new InvoiceOper().MappingInvoice(ref Invoic);
                if (string.IsNullOrEmpty(print.RptName))
                    print.RptName = "rptPOSA4.repx";
                if (ProcType == 4)
                    print.RptName = "rptPricingInv.repx";
                string path = Path.Combine(print.RptUrl, print.RptName);

                if (conn.State == ConnectionState.Open) conn.Close();
                conn.Open();
                int custId = GetSelectedValue<int>(LookUpEdit1);
                var cmd = new SqlCommand(
                    "SELECT name, mobile FROM Customers WHERE id=" + custId,
                    conn);
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string mobile = reader["mobile"].ToString().Trim();
                    string custName = reader["name"].ToString().Trim();
                    if (string.IsNullOrEmpty(mobile))
                    {
                        MessageBox.Show("❌ لا يوجد رقم جوال للعميل");
                        reader.Close();
                        conn.Close();
                        return;
                    }
                    string invRef = "INV" + txtNo.Text;
                    string pdfPath = Path.Combine(
                        System.Windows.Forms.Application.StartupPath,
                        $"فاتورة_{invRef}.pdf");
                    if (File.Exists(pdfPath)) File.Delete(pdfPath);

                    var rpt =
                        DevExpress.XtraReports.UI.XtraReport.FromFile(path);
                    rpt.DataSource = print.BindToData(inv);
                    rpt.ExportToPdf(pdfPath);
                    reader.Close();
                    conn.Close();

                    string foundName =
                        Common.FoundationInfoDT.Rows[0]["nameA"].ToString();
                    string message =
                        $"🧾 مرحباً {custName}، هذه فاتورتك رقم " +
                        $"{invRef} من {foundName}";

                    await Session.EnsureWhatsAppSessionAsync();
                    await Session.waSender.SendInvoiceAsync(
                        mobile, pdfPath, message);
                }
                else
                    MessageBox.Show("❌ لم يتم العثور على العميل.");
                if (reader != null && !reader.IsClosed) reader.Close();
                if (conn.State == ConnectionState.Open) conn.Close();
            }
            catch (Exception ex)
            {
                Logger.Error("printwhatsapp: " + ex.Message);
                MessageBox.Show("❌ خطأ أثناء إرسال الفاتورة: " + ex.Message);
            }
        }

        private void ExportPDF_Click(object sender, RoutedEventArgs e)
        {
            if (Invoic.IsPrinted && !Invoic.IsUpdated)
                RptPrintPDF(
                    new InvoiceOper().MappingInvoice(ref Invoic), 1);
            else
                MessageBox.Show("لا يمكن تصدير الفاتورة قبل الحفظ");
        }

        private void BTnShowEntry_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!isInvExist())
                {
                    MessageBox.Show("لم يتم حفظ الفاتورة");
                    return;
                }
                var frm = new frmRptEntries();
                frm.Show();
                frm.Navigate(
                    "select * from Entry where IS_Deleted=0 " +
                    "and GlobalID=N'" + Invoic.EntryGlobalID + "'");
            }
            catch (Exception ex)
            {
                Logger.Error("BTnShowEntry: " + ex.Message);
            }
        }

        private void StripProfit_Click(object sender, RoutedEventArgs e)
        {
            if (User.ShowCosts)
                MessageBox.Show(
                    Invoic.InvProfit.ToString(Common.DigitsNo),
                    "ربح الفاتورة",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
        }

        private void stripImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var r = MessageBox.Show(
                    "هل أنت متأكد من استيراد البيانات؟",
                    "", MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (r == MessageBoxResult.No) return;

                var frm = new frmImportDataGeneral();
                frm.cmbInv.SelectedIndex = 0;
                frm.cmbInv.Visibility    = Visibility.Visible;
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
                        foreach (var item in Invoic.InvoiceItems)
                        {
                            if (string.Equals(item.ItemCode?.ToString(),
                                row["ItemCode"]?.ToString()) &&
                                item.ItemRowIndex ==
                                    Invoic.InvoiceItems.Count)
                            {
                                item.Description =
                                    row["description"].ToString();
                                item.ItemQuantity =
                                    Convert.ToDouble(row["quntity"]);
                                double price =
                                    Convert.ToDouble(row["price"]);
                                if (price > 0)
                                    item.ItemPrice = price;
                                ItemOper.CalcRows(ref Invoic);
                                BindingDGV();
                            }
                        }
                    }
                }
                MessageBox.Show("تم الاستيراد بنجاح");
            }
            catch (Exception ex)
            {
                Logger.Error("stripImport: " + ex.Message);
            }
        }

        private void stripExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!isInvExist())
                {
                    MessageBox.Show("يجب حفظ الفاتورة قبل التصدير");
                    return;
                }
                var r = MessageBox.Show(
                    "هل أنت متأكد من تصدير البيانات؟",
                    "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.No) return;

                var dlg = new SaveFileDialog
                {
                    Filter = "Excel Files(*.xlsx)|*.xlsx"
                };
                if (dlg.ShowDialog() == true)
                {
                    string filePath = dlg.FileName;
                    ExportDataGridToExcel(filePath);
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(filePath)
                        { UseShellExecute = true });
                    MessageBox.Show("تم حفظ الملف في " + filePath);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("stripExport: " + ex.Message);
                MessageBox.Show("خطأ: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSaveDgvSettings_Click(
            object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(StyleFolder))
                Directory.CreateDirectory(StyleFolder);
            // Save column widths to XML
            SaveGridLayout();
            MessageBox.Show("تم حفظ مظهر الجدول");
        }

        private void BtnDefaultSetting_Click(
            object sender, RoutedEventArgs e)
        {
            if (File.Exists(StyleFile))
                File.Delete(StyleFile);
        }

        private void StripImportInv_Click(
            object sender, RoutedEventArgs e)
        {
            var frm = new frmImportInvoice();
            frm.ShowDialog();
            if (frm.isDone &&
                !string.Equals(frm.InvGID, "-1"))
                Navigate("select * from Inv where InvGlobalID=N'" +
                    frm.InvGID + "'");
            Invoic.InvoiceType = (InvoiceType)InvType;
            Invoic.ProcType = ProcType;
            Invoic.InvertoryImpact = InvObj.InvertoryImpact;
            foreach (var item in Invoic.InvoiceItems)
            {
                if (item.ItemType == 3 || item.ItemType == 5)
                    item.InvertoryImpact = 0;
                else
                    item.InvertoryImpact = Invoic.InvertoryImpact;
            }
            Invoic.InvNote = "";
            LoadInvNo();
            Invoic.ISNew = true;
            Invoic.IsPrinted = false;
            Invoic.IsUpdated = false;
            BindingControls();
        }

        private void OpenReportDesgin_Click(
            object sender, RoutedEventArgs e)
        {
            if (InvType == 2 && ProcType == 1)
                Common.OpenReportDesgin("rptPOSA4.repx", 2);
            else if (InvType == 2 && ProcType == 4)
                Common.OpenReportDesgin("rptPricingInv.repx", 4);
            else if (InvType == 2 && ProcType == 2)
                Common.OpenReportDesgin("rptPOSA4CreditNote.repx", 2);
        }

        private void ToolsMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        #endregion

        #region ComboBox / TextBox Events

        private void cmbPayType_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (cmbPayType.SelectedIndex == 2)
            {
                cmbTreasury.IsEnabled = false;
                cmbBanks.IsEnabled = true;
                cmbBanks.Focus();
                BtnReceipts.Visibility = Visibility.Collapsed;
                Invoic.PayType = 2;
                Invoic.Treasury = -1;
                Invoic.Customer = GetSelectedValue<int>(LookUpEdit1);
            }
            else if (cmbPayType.SelectedIndex == 1)
            {
                cmbTreasury.IsEnabled = true;
                cmbBanks.IsEnabled = false;
                BtnReceipts.Visibility = Visibility.Collapsed;
                Invoic.PayType = 1;
                if (cmbTreasury.Items.Count > 0 &&
                    cmbTreasury.SelectedIndex < 0)
                    cmbTreasury.SelectedIndex = 0;
                Invoic.Treasury = GetSelectedValue<int>(cmbTreasury);
                Invoic.Customer = GetSelectedValue<int>(LookUpEdit1);
            }
            else if (cmbPayType.SelectedIndex == 0)
            {
                cmbTreasury.IsEnabled = false;
                cmbBanks.IsEnabled = false;
                if (InvType == 2)
                    BtnReceipts.Visibility = Visibility.Visible;
                if (Invoic.ISNew)
                {
                    if (LookUpEdit1.Items.Count > 0)
                        LookUpEdit1.SelectedIndex = 0;
                    Invoic.Customer = -1;
                }
                Invoic.PayType = -1;
                Invoic.Treasury = -1;
                Invoic.PaymentStatus = PaymentStatus.PostPaid;
            }
        }

        private void cmbTreasury_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (cmbTreasury.SelectedIndex > -1)
                Invoic.Treasury = GetSelectedValue<int>(cmbTreasury);
            else
                Invoic.Treasury = -1;
        }

        private void cmbBanks_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (cmbBanks.SelectedIndex > -1)
                Invoic.Bank = GetSelectedValue<int>(cmbBanks);
            else
                Invoic.Bank = -1;
        }

        private void cmbStore_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (cmbStore.SelectedIndex > -1)
            {
                Invoic.Store = GetSelectedValue<int>(cmbStore);
                Invoic.InvertoryName =
                    Common.GetStoreName(Invoic.Store);
            }
            else Invoic.Store = -1;
        }

        private void cmbSalesMen_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (cmbSalesMen.SelectedIndex > -1)
                Invoic.Saleman = GetSelectedValue<int>(cmbSalesMen);
            else
                Invoic.Saleman = -1;
        }

        private void cmbCostCenter_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (cmbCostCenter.SelectedIndex > -1)
                Invoic.InvCCcode =
                    GetSelectedValue<string>(cmbCostCenter);
            else
                Invoic.InvCCcode = (-1).ToString();
        }

        private void cmbInvoiceStatus_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (cmbInvoiceStatus.SelectedIndex > -1)
                Invoic.InvoiceStatus = cmbInvoiceStatus.SelectedIndex;
            else
                Invoic.InvoiceStatus = 1;
        }

        private void LookUpEdit1_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            try
            {
                int custId = GetSelectedValue<int>(LookUpEdit1);
                if (custId > -1)
                {
                    Invoic.Customer = custId;
                    ShowCustBalance();
                }
                else Invoic.Customer = -1;
            }
            catch (Exception ex)
            {
                Logger.Error("LookUpEdit1_SelectionChanged: " + ex.Message);
            }
        }

        private void LookUpEdit1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                CheckClient();
            }
        }

        private void txtRefNo_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
            => Invoic.ReffNo = txtRefNo.Text;

        private void txtNote_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
            => Invoic.InvNote = txtNote.Text;

        private void txtCashCust_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
            => Invoic.CashCustomerName = txtCashCust.Text;

        private void txtCashCustMobile_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
            => Invoic.CashCustomerMobile = txtCashCustMobile.Text.Trim();

        private void txtInvDiscVal_LostFocus(object sender,
            RoutedEventArgs e) => CalcDiscount();

        private void txtInvDiscVal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) CalcDiscount();
        }

        private void txtInvDiscPerc_LostFocus(object sender,
            RoutedEventArgs e) => CalcDiscountPerc();

        private void txtInvDiscPerc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) CalcDiscountPerc();
        }

        private void txtInvDisc_PreviewTextInput(object sender,
            TextCompositionEventArgs e)
        {
            bool isDigitOrDot = e.Text.All(
                c => char.IsDigit(c) || c == '.');
            if (!isDigitOrDot)
            {
                e.Handled = true;
                MessageBox.Show("الحقل لا يقبل إلا الأرقام فقط");
            }
        }

        private void txtNet_MouseDoubleClick(object sender,
            MouseButtonEventArgs e)
        {
            if (InvObj.PriceIncVAT) return;
            var frm = new FrmCalcVAT();
            frm.ItemVAT = (int)Math.Round(InvObj.VAT);
            frm.ShowDialog();
            if (frm.Price > 0)
            {
                Invoic.Total = frm.PriceWithOutVAT;
                Invoic.TotDiscount = Invoic.SumPrice - Invoic.Total;
                Invoic.InvDiscount =
                    Invoic.TotDiscount - Invoic.ItemsDiscount;
                ItemOper.CalcRows(ref Invoic);
                BindingDGV();
            }
        }

        private void txtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ReadBarcode(txtBarcode.Text, false);
                txtBarcode.Text = "";
            }
        }

        private void txtSrchSerialNo_KeyDown(
            object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                SearchBySerialNo(txtSrchSerialNo.Text.Trim());
                txtSrchSerialNo.Text = "";
            }
        }

        private void CmbYearPreviews_DropDownOpened(
            object sender, EventArgs e) => loadDB_by_DbAutoName();

        private void CmbYearPreviews_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (CmbYearPreviews.SelectedIndex >= 0)
                MainClass.ReturnYearPreviews = true;
        }

        #endregion

        #region Timer

        private void Timer1_Tick(object sender, EventArgs e)
        {
            lblCurrentTime.Text = DateTime.Now.ToString("h:mm:ss tt");
        }

        #endregion

        #region Helper Methods

        private T GetSelectedValue<T>(ComboBox cmb)
        {
            try
            {
                if (cmb.SelectedItem is DataRowView row &&
                    cmb.SelectedValuePath != null)
                {
                    var val = row[cmb.SelectedValuePath];
                    if (val != DBNull.Value)
                        return (T)Convert.ChangeType(val, typeof(T));
                }
            }
            catch { }
            return default(T);
        }

        private void SetComboByValue(ComboBox cmb, object value)
        {
            if (value == null) return;
            cmb.SelectedValue = value;
        }

        private int GetFocusedItemId()
        {
            var item = GridControl1.SelectedItem as InvoiceItem;
            return item?.ItemId ?? 0;
        }

        private int GetFocusedRowIndex()
        {
            var item = GridControl1.SelectedItem as InvoiceItem;
            return item?.ItemRowIndex ?? 0;
        }

        private void MoveFocusToColumn(string fieldName)
        {
            foreach (DataGridColumn col in GridControl1.Columns)
            {
                if (col.SortMemberPath == fieldName)
                {
                    GridControl1.CurrentColumn = col;
                    GridControl1.BeginEdit();
                    break;
                }
            }
        }

        private void MoveFocusToNextRow(string fieldName)
        {
            int idx = GridControl1.SelectedIndex + 1;
            if (idx < GridControl1.Items.Count)
            {
                GridControl1.SelectedIndex = idx;
                GridControl1.ScrollIntoView(GridControl1.SelectedItem);
                MoveFocusToColumn(fieldName);
            }
        }

        private void SaveGridLayout()
        {
            try
            {
                if (!Directory.Exists(StyleFolder))
                    Directory.CreateDirectory(StyleFolder);
                var colSettings = new List<ColSetting>();
                foreach (DataGridColumn col in GridControl1.Columns)
                {
                    colSettings.Add(new ColSetting
                    {
                        Header = col.Header?.ToString(),
                        Width = col.Width.Value,
                        DisplayIndex = col.DisplayIndex,
                        Visibility = col.Visibility.ToString()
                    });
                }
                File.WriteAllText(StyleFile,
                    JsonConvert.SerializeObject(colSettings,
                        Formatting.Indented));
            }
            catch (Exception ex)
            {
                Logger.Error("SaveGridLayout: " + ex.Message);
            }
        }

        private void LoadGridLayout()
        {
            try
            {
                if (!File.Exists(StyleFile)) return;
                var colSettings = JsonConvert.DeserializeObject<
                    List<ColSetting>>(File.ReadAllText(StyleFile));
                if (colSettings == null) return;
                foreach (var setting in colSettings)
                {
                    foreach (DataGridColumn col in GridControl1.Columns)
                    {
                        if (col.Header?.ToString() == setting.Header)
                        {
                            col.Width = new DataGridLength(setting.Width);
                            col.Visibility =
                                setting.Visibility == "Visible"
                                    ? Visibility.Visible
                                    : Visibility.Collapsed;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("LoadGridLayout: " + ex.Message);
            }
        }

        private void ExportDataGridToExcel(string filePath)
        {
            // Basic CSV export as fallback
            var sb = new System.Text.StringBuilder();
            // Headers
            var headers = GridControl1.Columns
                .Where(c => c.Visibility == Visibility.Visible)
                .Select(c => c.Header?.ToString() ?? "");
            sb.AppendLine(string.Join(",", headers));
            // Rows
            foreach (var item in Invoic.InvoiceItems)
            {
                var row = new List<string>
                {
                    item.ItemRowIndex.ToString(),
                    item.ItemCode ?? "",
                    item.ItemName ?? "",
                    item.UnitName ?? "",
                    item.ItemQuantity.ToString(),
                    item.ItemPrice.ToString(),
                    item.ItemSumPrice.ToString(),
                    item.ItemDiscount.ToString(),
                    item.ItemVat.ToString(),
                    item.ItemNetPrice.ToString()
                };
                sb.AppendLine(string.Join(",", row));
            }
            File.WriteAllText(filePath, sb.ToString(),
                System.Text.Encoding.UTF8);
        }

        private string RemoveNonBmpCharacters(string input)
        {
            return new string(input.Where(c => c <= '\uffff').ToArray());
        }

        private string ReplaceUnsupportedSymbols(string input)
        {
            input = input.Replace("\ud83e\uddfe", "[فاتورة]");
            input = input.Replace("\ud83d\udc4b", "[مرحباً]");
            return RemoveNonBmpCharacters(input);
        }

        private bool IsInvoiceChanged(InvoiceDGV inv)
        {
            if (inv.Customer != InvoicCheck.Customer) return true;
            if (!string.Equals(inv.CashCustomerName,
                InvoicCheck.CashCustomerName)) return true;
            if (!string.Equals(inv.CashCustomerMobile,
                InvoicCheck.CashCustomerMobile)) return true;
            if (inv.PayType != InvoicCheck.PayType) return true;
            if (!string.Equals(inv.InvNote,
                InvoicCheck.InvNote)) return true;
            if (inv.Treasury != InvoicCheck.Treasury) return true;
            return false;
        }

        #endregion

        #region Number Pad (GroupBox5)

        private void cmd1_Click(object sender, RoutedEventArgs e)
            => AppendToPaid("1");
        private void cmd2_Click(object sender, RoutedEventArgs e)
            => AppendToPaid("2");
        private void cmd3_Click(object sender, RoutedEventArgs e)
            => AppendToPaid("3");
        private void cmd4_Click(object sender, RoutedEventArgs e)
            => AppendToPaid("4");
        private void cmd5_Click(object sender, RoutedEventArgs e)
            => AppendToPaid("5");
        private void cmd6_Click(object sender, RoutedEventArgs e)
            => AppendToPaid("6");
        private void cmd7_Click(object sender, RoutedEventArgs e)
            => AppendToPaid("7");
        private void cmd8_Click(object sender, RoutedEventArgs e)
            => AppendToPaid("8");
        private void cmd9_Click(object sender, RoutedEventArgs e)
            => AppendToPaid("9");
        private void cmd0_Click(object sender, RoutedEventArgs e)
            => AppendToPaid("0");
        private void cmdDot_Click(object sender, RoutedEventArgs e)
            => AppendToPaid(".");
        private void cmdClearAll_Click(object sender, RoutedEventArgs e)
            => txtInvPaid.Text = "0";

        private void AppendToPaid(string digit)
        {
            if (txtInvPaid.Text == "0" && digit != ".")
                txtInvPaid.Text = digit;
            else
                txtInvPaid.Text += digit;
        }

        #endregion

    }

    #region Model Classes

    public class ColSetting
    {
        public string Header { get; set; }
        public double Width { get; set; }
        public int DisplayIndex { get; set; }
        public string Visibility { get; set; }
    }

    /// <summary>
    /// كلاس وسيط يحاكي سلوك ComboBox لكي لا تنكسر الأكواد الخارجية
    /// التي تستدعي cmbProcTypeSrch.SelectedIndex
    /// </summary>
    public class ComboBoxProxy
    {
        private int _selectedIndex = -1;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set => _selectedIndex = value;
        }

        public ComboBoxProxy()
        {
            _selectedIndex = -1;
        }
    }

    #endregion
}