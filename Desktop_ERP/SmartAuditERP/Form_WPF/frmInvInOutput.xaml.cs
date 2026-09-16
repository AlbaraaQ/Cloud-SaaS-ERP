using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInvInOutput : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;

        private int Code;
        private int ProcCode;

        public int InvType;
        public int ProcType;

        private InvoiceObj InvObj;
        private Print print;
        private int CostType;

        private bool isPrintd;
        private bool ISNew;
        private bool IsPrinted;

        private string InvGlobalID;
        private int RowIndex;
        private int SelectedId;

        public string SrchName;

        private bool ISreturn;
        private bool flag;

        private ObservableCollection<InvoiceItemRowModel> invoiceItems;
        private ObservableCollection<InvoiceSearchRowModel> searchRows;

        #endregion

        #region Constructor

        public frmInvInOutput()
        {
            InitializeComponent();

            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();

            Code = -1;
            ProcCode = -1;
            InvType = 4;
            ProcType = 1;

            InvObj = new InvoiceObj(InvType, ProcType);
            print = new Print(InvType);
            CostType = 1;
            isPrintd = false;
            ISNew = true;
            IsPrinted = false;
            InvGlobalID = string.Empty;
            RowIndex = -1;
            SelectedId = -1;
            SrchName = string.Empty;
            ISreturn = false;
            flag = false;

            invoiceItems = new ObservableCollection<InvoiceItemRowModel>();
            searchRows = new ObservableCollection<InvoiceSearchRowModel>();

            invoiceItems.CollectionChanged += InvoiceItems_CollectionChanged;

            dgvItems.ItemsSource = invoiceItems;
            dgvSrch.ItemsSource = searchRows;

            cmbStore.SelectionChanged += cmbStore_SelectionChanged;
            cmbClient.SelectionChanged += cmbClient_SelectedIndexChanged;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadSafes();
                LoadInvNo();
                LoadCustomers();
                LoadAllCurrency();
                LoadCurrency(0);

                txtDate.EditValue = DateTime.Now;
                txtToDate.EditValue = DateTime.Now;
                txtRefDate.EditValue = DateTime.Now;
                txtFromDate.EditValue = DateTime.Now;

                if (cmbProcTypeSrch.Items.Count > 0)
                {
                    cmbProcTypeSrch.SelectedIndex = 0;
                }

                if (cmbProcType.Items.Count > 0)
                {
                    cmbProcType.SelectedIndex = 0;
                }

                if (cmbStore.Items.Count > 0 && cmbStore.SelectedIndex < 0)
                {
                    cmbStore.SelectedIndex = 0;
                }

                if (cmbClient.Items.Count > 0 && cmbClient.SelectedIndex < 0)
                {
                    cmbClient.SelectedIndex = 0;
                }

                if (InvType == 5)
                {
                    btnInsertLinkedInv.Visibility = Visibility.Collapsed;
                }

                txtBarcode.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.B)
                {
                    e.Handled = true;
                    txtBarcode.Focus();
                    txtBarcode.SelectAll();
                    return;
                }

                if (e.Key == Key.F1)
                {
                    addNewItem();
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.P)
                {
                    addNewItem();
                }

                if (e.Key == Key.Enter && txtBarcode.IsKeyboardFocusWithin)
                {
                    ReadBarcode();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Collection Events

        private void InvoiceItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RenumberRows();

            if (e.NewItems != null)
            {
                foreach (InvoiceItemRowModel item in e.NewItems)
                {
                    item.RecalculateRequested = CalcTot;
                }
            }

            CalcTot();
        }

        #endregion

        #region Helpers

        private double ToDouble(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0;

            double result;
            return double.TryParse(value.ToString(), out result) ? result : 0;
        }

        private int ToInt(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0;

            int result;
            return int.TryParse(value.ToString(), out result) ? result : 0;
        }

        private string ToSafeString(object value)
        {
            return value == null || value == DBNull.Value ? string.Empty : value.ToString();
        }

        private DateTime GetDateValue(DevExpress.Xpf.Editors.DateEdit editor)
        {
            try
            {
                if (editor.EditValue == null)
                    return DateTime.Now;

                return Convert.ToDateTime(editor.EditValue);
            }
            catch
            {
                return DateTime.Now;
            }
        }

        private int GetSelectedStoreId()
        {
            return ToInt(cmbStore.SelectedValue);
        }

        private string GetSelectedStoreName()
        {
            if (cmbStore.SelectedItem is DataRowView rowView)
            {
                return ToSafeString(rowView["name"]);
            }

            if (cmbStore.SelectedItem != null)
            {
                return cmbStore.Text;
            }

            return string.Empty;
        }

        private int GetSelectedClientId()
        {
            return ToInt(cmbClient.SelectedValue);
        }

        private string GetBindingPath(DataGridColumn column)
        {
            if (column is DataGridBoundColumn boundColumn)
            {
                if (boundColumn.Binding is Binding binding && binding.Path != null)
                {
                    return binding.Path.Path;
                }
            }

            return string.Empty;
        }

        private void RenumberRows()
        {
            for (int index = 0; index < invoiceItems.Count; index++)
            {
                invoiceItems[index].RowNo = index + 1;
            }
        }

        private void MoveToNextControl()
        {
            TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Next)
            {
                Wrapped = true
            };

            if (Keyboard.FocusedElement is UIElement element)
            {
                element.MoveFocus(request);
            }
        }

        private void ClearItemDetailsPanel()
        {
            txtStock.Text = string.Empty;
            txtPerUnit.Text = string.Empty;
            txtRecentPurchPrice.Text = string.Empty;
            txtLastSalePrice.Text = string.Empty;
            txtTotUnitQuan.Text = string.Empty;
            txtCostAvrg.Text = string.Empty;
            txtCompetitorPrice.Text = string.Empty;
            txtItemBarcode.Text = string.Empty;
            lblPerUnit.Text = string.Empty;
        }

        private void UpdateCurrentStoreForRows()
        {
            int storeId = GetSelectedStoreId();
            string storeName = GetSelectedStoreName();

            foreach (InvoiceItemRowModel item in invoiceItems)
            {
                item.StoreId = storeId;
                item.StoreName = storeName;

                if (item.ItemId > 0 && storeId > 0)
                {
                    item.Stock = CalcStock(item.ItemId, storeId);
                }
            }

            LoadItemDetails();
        }

        #endregion

        #region Load Data

        public void LoadSafes()
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id,name from Safes,Safe_Emps where Safes.id=Safe_Emps.safe_id and emp_id=" + MainClass.EmpNo + " and IS_Deleted=0 and status<>2 order by id",
                    conn))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    cmbStore.ItemsSource = dataTable.DefaultView;
                    cmbStore.DisplayMemberPath = "name";
                    cmbStore.SelectedValuePath = "id";
                    cmbStore.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ تحميل المستودعات", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadCurrency(int group)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id, name from Items where group_id=" + group + " and IS_Deleted=0 order by id",
                    conn))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                }
            }
            catch
            {
            }
        }

        public void LoadAllCurrency()
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id, name from Items where IS_Deleted=0 order by id",
                    conn))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                }
            }
            catch
            {
            }
        }

        private void LoadInvNo()
        {
            try
            {
                txtNo.Text = InvoiceOper.InvoiceNo(InvType, ProcType, InvObj.Prefixe).ToString();
                txtNo.Background = new SolidColorBrush(Color.FromRgb(240, 244, 255));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCustomers()
        {
            try
            {
                int customerType = 2;
                if (InvType != 4 && InvType == 5)
                {
                    customerType = 1;
                }

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id,name from Customers where (type=" + customerType + " or type=3) order by id",
                    conn))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        cmbClient.ItemsSource = dataTable.DefaultView;
                        cmbClient.DisplayMemberPath = "name";
                        cmbClient.SelectedValuePath = "id";
                        cmbClient.SelectedIndex = -1;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ تحميل العملاء", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Unit Handling

        private int CheckItemUnit(int itemID)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id,name from units where defaultInv=" + InvType,
                    conn))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        int unitId = ToInt(dataTable.Rows[0]["id"]);

                        using (SqlDataAdapter adapter2 = new SqlDataAdapter(
                            "select purch,sale from ItemUnits where ItemId=" + itemID + " and unit=" + unitId,
                            conn))
                        {
                            DataTable dataTable2 = new DataTable();
                            adapter2.Fill(dataTable2);

                            if (dataTable2.Rows.Count > 0)
                            {
                                return unitId;
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return 0;
        }

        private void LoadUnitInf(int index, int unitId)
        {
            try
            {
                if (index < 0 || index >= invoiceItems.Count)
                    return;

                InvoiceItemRowModel row = invoiceItems[index];
                int itemId = row.ItemId;

                if (itemId <= 0)
                    return;

                if (unitId == 0 && string.IsNullOrWhiteSpace(row.UnitName))
                {
                    unitId = CheckItemUnit(itemId);
                }

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select purch,sale,barcode,perc, units.name from units,ItemUnits where ItemUnits.ItemId=" + itemId + " and ItemUnits.unit=units.id and units.id=" + unitId,
                    conn))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        row.SuppressRecalculate = true;
                        row.UnitName = ToSafeString(dataTable.Rows[0]["name"]);
                        row.Barcode = ToSafeString(dataTable.Rows[0]["barcode"]);
                        row.UnitEquality = ToDouble(dataTable.Rows[0]["perc"]);
                        row.Quantity = row.Quantity <= 0 ? 1 : row.Quantity;
                        row.TotalQuantity = row.Quantity * row.UnitEquality;

                        double calculatedPrice = ItemOper.Cost(itemId) * row.UnitEquality;

                        if (InvObj.Pricing == Pricing.Default)
                        {
                            calculatedPrice = ToDouble(dataTable.Rows[0]["sale"]);
                        }
                        else if (InvObj.Pricing == Pricing.SalePrice)
                        {
                            calculatedPrice = ToDouble(dataTable.Rows[0]["purch"]);
                        }
                        else if (InvObj.Pricing == Pricing.PurchasePrice)
                        {
                            calculatedPrice = ItemOper.RecentPurchPrice(itemId) * row.UnitEquality;
                        }
                        else if (InvObj.Pricing == Pricing.LastPurchasePrice)
                        {
                            calculatedPrice = ItemOper.Cost(itemId) * row.UnitEquality;
                        }

                        row.Price = calculatedPrice;
                        row.SuppressRecalculate = false;
                        row.Recalculate();
                        return;
                    }
                }

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select units.name as unit ,purch_price as purch,sale_price as sale,Items.barcode as barcode from Items, units where items.unit=units.id and items.id=" + itemId,
                    conn))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        row.SuppressRecalculate = true;
                        row.UnitName = ToSafeString(dataTable.Rows[0]["unit"]);
                        row.Barcode = ToSafeString(dataTable.Rows[0]["barcode"]);
                        row.UnitEquality = 1;
                        row.Quantity = row.Quantity <= 0 ? 1 : row.Quantity;
                        row.TotalQuantity = row.Quantity * row.UnitEquality;

                        double calculatedPrice = ItemOper.Cost(itemId);

                        if (InvObj.Pricing == Pricing.Default)
                        {
                            calculatedPrice = ToDouble(dataTable.Rows[0]["sale"]);
                        }
                        else if (InvObj.Pricing == Pricing.SalePrice)
                        {
                            calculatedPrice = ToDouble(dataTable.Rows[0]["purch"]);
                        }
                        else if (InvObj.Pricing == Pricing.PurchasePrice)
                        {
                            calculatedPrice = ItemOper.RecentPurchPrice(itemId);
                        }
                        else if (InvObj.Pricing == Pricing.LastPurchasePrice)
                        {
                            calculatedPrice = ItemOper.Cost(itemId);
                        }

                        row.Price = calculatedPrice;
                        row.SuppressRecalculate = false;
                        row.Recalculate();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل بيانات الوحدة" + Environment.NewLine + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowItemUnit(int index)
        {
            try
            {
                if (index < 0 || index >= invoiceItems.Count)
                    return;

                InvoiceItemRowModel row = invoiceItems[index];
                if (row.ItemId <= 0)
                    return;

                frmItemUnits unitWindow = new frmItemUnits
                {
                    ItemId = row.ItemId,
                    PreUnit = GetUnitID(row.UnitName)
                };

                unitWindow.ShowDialog();

                if (!string.IsNullOrWhiteSpace(unitWindow.Unitname))
                {
                    LoadUnitInf(index, unitWindow.UnitId);
                    LoadItemDetails();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل الوحدات" + Environment.NewLine + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public int GetUnitID(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return -1;

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id from units where name=N'" + name.Replace("'", "''") + "'",
                    conn1))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        return ToInt(dataTable.Rows[0][0]);
                    }
                }
            }
            catch
            {
            }

            return -1;
        }

        private string GetUnitName(int id)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select name from units where id=" + id,
                    conn1))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        return ToSafeString(dataTable.Rows[0][0]);
                    }
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        #endregion

        #region Item Search / Add

        private void LoadItemInf(int id, int index, int unitID)
        {
            try
            {
                // التأكد من أن السطر موجود فعلياً في الجدول
                if (index < 0 || index >= invoiceItems.Count)
                    return;

                // جلب السطر الحالي للتعامل معه ككائن
                InvoiceItemRowModel row = invoiceItems[index];

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select Items.id, Items.name, Items.barcode, units.name as unit, purch_price, sale_price, tax, discount " +
                    "from Items, units where Items.unit = units.id and items.id = " + id, conn))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        // استدعاء دالة الوحدات كما في الكود الأصلي
                        LoadUnitInf(index, unitID);

                        // إيقاف الحساب التلقائي مؤقتاً لتجنب تكرار الحساب مع كل قيمة نضعها
                        row.SuppressRecalculate = true;

                        // تعيين القيم من الداتابيز أو الدوال المساعدة
                        row.AvgCost = ItemOper.Cost(id);

                        int currentStoreId = GetSelectedStoreId();
                        row.StoreId = currentStoreId;
                        row.StoreName = GetSelectedStoreName();
                        row.Stock = CalcStock(id, currentStoreId);

                        row.Quantity = 1;
                        row.VatPer = ToDouble(dataTable.Rows[0]["tax"]);
                        row.DiscountVal = ToDouble(dataTable.Rows[0]["discount"]);

                        // التحقق من خيار "تلقائي" (في WPF نستخدم IsChecked == true)
                        if (rdAuto.IsChecked == true)
                        {
                            Frm_Calculator frm_Calculator = new Frm_Calculator();

                            // إذا كنت تستخدم TextBlock بدلاً من Label في نافذة الآلة الحاسبة WPF
                            // تأكد أن أسماء العناصر بداخلها متطابقة مع هذا الكود
                            frm_Calculator.txtMsg.Text = "إدخل الكمية ";
                            frm_Calculator.TextBox1.Text = "1";
                            frm_Calculator.ShowDialog();

                            // جلب القيمة من إعدادات البرنامج كما في الكود الأصلي
                            row.Quantity = (double)Properties.Settings.Default.QTY;
                        }

                        // إعادة تشغيل الحساب التلقائي وتحديث المجاميع
                        row.SuppressRecalculate = false;
                        row.Recalculate();

                        CalcTot();
                    }
                }
            }
            catch (Exception ex)
            {
                // استبدلنا Interaction.MsgBox و ProjectData بـ MessageBox الخاصة بـ WPF
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ISItem(string barcode)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select name from Items where IS_Deleted=0 and barcode=" + barcode,
                    conn))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    return dataTable.Rows.Count > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private void txtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ReadBarcode();
                e.Handled = true;
            }
        }

        private void ReadBarcode()
        {
            try
            {
                string barcodeText = txtBarcode.Text.Trim();
                if (string.IsNullOrWhiteSpace(barcodeText))
                    return;

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select Items.id,ItemUnits.unit from Items,ItemUnits where Items.IS_Deleted=0 and ItemUnits.ItemId=items.id and (ItemUnits.barcode=N'" + barcodeText.Replace("'", "''") + "' or Items.barcode=N'" + barcodeText.Replace("'", "''") + "')",
                    conn1))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        SelectedId = ToInt(dataTable.Rows[0]["id"]);
                        txtBarcode.Text = string.Empty;
                        SearchByID(ToInt(dataTable.Rows[0]["unit"]));
                        return;
                    }
                }

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select items.unit ,Itembarcodes.itemId from Items,Itembarcodes where Items.IS_Deleted=0 and Itembarcodes.ItemId=items.id and (Itembarcodes.barcode=N'" + barcodeText.Replace("'", "''") + "' or Items.barcode=N'" + barcodeText.Replace("'", "''") + "')",
                    conn1))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        SelectedId = ToInt(dataTable.Rows[0]["itemId"]);
                        SearchByID(ToInt(dataTable.Rows[0]["unit"]));

                        InvoiceItemRowModel lastRow = invoiceItems.LastOrDefault();
                        if (lastRow != null)
                        {
                            lastRow.Barcode = barcodeText;
                        }

                        txtBarcode.Text = string.Empty;
                        return;
                    }
                }

                MessageBox.Show("هذا الباركود غير موجود ضمن بيانات البرنامج",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchByName(InvoiceItemRowModel editingRow = null)
        {
            try
            {
                string itemName = editingRow != null ? editingRow.ItemName : SrchName;

                if (string.IsNullOrWhiteSpace(itemName))
                    return;

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select name,id from Items where IS_Deleted=0 and name=N'" + itemName.Replace("'", "''") + "'",
                    conn))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        SelectedId = ToInt(dataTable.Rows[0]["id"]);

                        if (editingRow == null)
                        {
                            SearchByID(0);
                        }
                        else
                        {
                            LoadSelectedItemIntoExistingRow(editingRow, 0);
                        }
                    }
                    else
                    {
                        addNewItem();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchByCode(InvoiceItemRowModel editingRow = null)
        {
            try
            {
                string codeText = editingRow != null ? editingRow.ItemCode : string.Empty;
                if (string.IsNullOrWhiteSpace(codeText))
                    return;

                SrchName = string.Empty;

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select name,id from Items where IS_Deleted=0 and Code=N'" + codeText.Replace("'", "''") + "'",
                    conn))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        SelectedId = ToInt(dataTable.Rows[0]["id"]);

                        if (editingRow == null)
                        {
                            SearchByID(0);
                        }
                        else
                        {
                            LoadSelectedItemIntoExistingRow(editingRow, 0);
                        }
                    }
                    else
                    {
                        addNewItem();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchByID(int unitId)
        {
            try
            {
                if (SelectedId <= 0)
                    return;

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select name,id,code,nameEN from Items where IS_Deleted=0 and id=" + SelectedId,
                    conn))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count == 0)
                    {
                        addNewItem();
                        return;
                    }

                    int itemId = ToInt(dataTable.Rows[0]["id"]);
                    InvoiceItemRowModel existingRow = invoiceItems.FirstOrDefault(x => x.ItemId == itemId);

                    if (existingRow != null)
                    {
                        MessageBoxResult duplicateResult = MessageBox.Show(
                            "الصنف موجود مسبقًا." + Environment.NewLine +
                            "نعم = زيادة الكمية" + Environment.NewLine +
                            "لا = إضافة كسطر جديد" + Environment.NewLine +
                            "إلغاء = عدم الإضافة",
                            "تنبيه", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                        if (duplicateResult == MessageBoxResult.Yes)
                        {
                            existingRow.Quantity += 1;
                            existingRow.Recalculate();
                            dgvItems.SelectedItem = existingRow;
                            LoadItemDetails();
                            return;
                        }

                        if (duplicateResult == MessageBoxResult.Cancel)
                        {
                            return;
                        }
                    }

                    InvoiceItemRowModel row = new InvoiceItemRowModel
                    {
                        ItemId = itemId,
                        ItemCode = ToSafeString(dataTable.Rows[0]["code"]),
                        ItemName = MainClass.Language == "ar"
                            ? ToSafeString(dataTable.Rows[0]["name"])
                            : string.IsNullOrWhiteSpace(ToSafeString(dataTable.Rows[0]["nameEN"]))
                                ? ToSafeString(dataTable.Rows[0]["name"])
                                : ToSafeString(dataTable.Rows[0]["nameEN"]),
                        StoreId = GetSelectedStoreId(),
                        StoreName = GetSelectedStoreName(),
                        Quantity = 1,
                        UnitEquality = 1,
                        DiscountVal = 0,
                        VatPer = 0
                    };

                    invoiceItems.Add(row);
                    RowIndex = invoiceItems.Count - 1;

                    LoadItemInf(row.ItemId, RowIndex, unitId);

                    dgvItems.SelectedItem = row;
                    dgvItems.ScrollIntoView(row);
                    LoadItemDetails();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSelectedItemIntoExistingRow(InvoiceItemRowModel row, int unitId)
        {
            try
            {
                if (row == null || SelectedId <= 0)
                    return;

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select name,id,code,nameEN from Items where IS_Deleted=0 and id=" + SelectedId,
                    conn))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count == 0)
                    {
                        return;
                    }

                    row.SuppressRecalculate = true;
                    row.ItemId = ToInt(dataTable.Rows[0]["id"]);
                    row.ItemCode = ToSafeString(dataTable.Rows[0]["code"]);
                    row.ItemName = MainClass.Language == "ar"
                        ? ToSafeString(dataTable.Rows[0]["name"])
                        : string.IsNullOrWhiteSpace(ToSafeString(dataTable.Rows[0]["nameEN"]))
                            ? ToSafeString(dataTable.Rows[0]["name"])
                            : ToSafeString(dataTable.Rows[0]["nameEN"]);
                    row.StoreId = GetSelectedStoreId();
                    row.StoreName = GetSelectedStoreName();
                    row.SuppressRecalculate = false;

                    int rowIndex = invoiceItems.IndexOf(row);
                    LoadItemInf(row.ItemId, rowIndex, unitId);

                    dgvItems.SelectedItem = row;
                    LoadItemDetails();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void addNewItem()
        {
            try
            {
                frmItemsSrch searchWindow = new frmItemsSrch();
                MainClass.ApplyPermissionToForm(searchWindow);
                MainClass.DoApplyUserSett(searchWindow);

                searchWindow.sql = "select id, name ,nameEN , sale_price , unit from Items where IS_Deleted=0 order by id";
                searchWindow.search = "select id, name ,nameEN , sale_price , unit from Items";
                searchWindow.Itemname = string.Empty;
                searchWindow.StoreId = GetSelectedStoreId();
                searchWindow.txtSrchNm.Text = SrchName;
                searchWindow.txtSrchCode.Text = string.Empty;

                searchWindow.ShowDialog();

                if (searchWindow.ISDone && searchWindow.ItemId > 0)
                {
                    if (searchWindow.Itemlist != null && searchWindow.Itemlist.Count > 0)
                    {
                        foreach (int itemId in searchWindow.Itemlist)
                        {
                            SelectedId = itemId;
                            SearchByID(0);
                        }
                    }
                    else
                    {
                        SelectedId = searchWindow.ItemId;
                        SearchByID(0);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ فتح شاشة الأصناف", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddNewItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                frmItems itemWindow = new frmItems();
                itemWindow.ShowDialog();
                LoadAllCurrency();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Grid Item Events

        private void dgvItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (dgvItems.SelectedItem is InvoiceItemRowModel)
                {
                    RowIndex = dgvItems.SelectedIndex;
                    LoadItemDetails();
                }
                else
                {
                    ClearItemDetailsPanel();
                }
            }
            catch
            {
            }
        }

        private void dgvItems_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                if (e.EditAction != DataGridEditAction.Commit)
                    return;

                if (!(e.Row.Item is InvoiceItemRowModel row))
                    return;

                string bindingPath = GetBindingPath(e.Column);

                if (e.EditingElement is TextBox textBox)
                {
                    string enteredText = textBox.Text?.Trim() ?? string.Empty;

                    if (bindingPath == "ItemCode")
                    {
                        row.ItemCode = enteredText;
                        SearchByCode(row);
                    }
                    else if (bindingPath == "ItemName")
                    {
                        row.ItemName = enteredText;
                        SearchByName(row);
                    }
                    else if (bindingPath == "UnitName")
                    {
                        row.UnitName = enteredText;
                        int unitId = GetUnitID(enteredText);
                        if (unitId == -1)
                        {
                            MessageBox.Show("اسم الوحدة غير صحيح", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                            int rowIndex = invoiceItems.IndexOf(row);
                            ShowItemUnit(rowIndex);
                        }
                        else
                        {
                            int rowIndex = invoiceItems.IndexOf(row);
                            LoadUnitInf(rowIndex, unitId);
                        }
                    }
                    else if (bindingPath == "Quantity")
                    {
                        row.Quantity = ToDouble(enteredText);
                        row.Recalculate();
                    }
                    else if (bindingPath == "Price")
                    {
                        row.Price = ToDouble(enteredText);
                        row.Recalculate();
                    }
                    else if (bindingPath == "DiscountVal")
                    {
                        row.DiscountVal = ToDouble(enteredText);
                        row.Recalculate();
                    }
                    else if (bindingPath == "DiscountPer")
                    {
                        row.DiscountPer = ToDouble(enteredText);
                    }
                }

                LoadItemDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ تعديل", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgvItems_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    MoveToNextControl();
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.F1)
                {
                    addNewItem();
                    e.Handled = true;
                    return;
                }

                if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Delete)
                {
                    deleteRow();
                    e.Handled = true;
                }
            }
            catch
            {
            }
        }

        private void DeleteRowBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is InvoiceItemRowModel row)
                {
                    MessageBoxResult result = MessageBox.Show("هل تريد حذف البند؟",
                        "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        invoiceItems.Remove(row);
                        CalcTot();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ حذف بند", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Item Details Panel

        private void LoadItemDetails()
        {
            try
            {
                if (!(dgvItems.SelectedItem is InvoiceItemRowModel row))
                {
                    ClearItemDetailsPanel();
                    return;
                }

                SelectedId = row.ItemId;

                txtLastSalePrice.Text = string.Empty;
                txtCompetitorPrice.Text = string.Empty;
                txtCostAvrg.Text = string.Empty;
                txtStock.Text = string.Empty;
                txtItemBarcode.Text = string.Empty;

                txtRecentPurchPrice.Text = ItemOper.RecentPurchPrice(SelectedId).ToString();
                txtCostAvrg.Text = (row.AvgCost * row.UnitEquality).ToString("0.##");

                LoadPrices(SelectedId);

                txtStock.Text = row.Stock.ToString("0.##");
                txtStock.Foreground = row.Stock >= 0 ? Brushes.Green : Brushes.Firebrick;

                txtPerUnit.Text = row.UnitEquality.ToString("0.##");
                txtTotUnitQuan.Text = row.TotalQuantity.ToString("0.##");
                txtItemBarcode.Text = row.Barcode;
                lblPerUnit.Text = row.UnitName;
            }
            catch
            {
            }
        }

        public void RecentPurchPrice(int itemId)
        {
            try
            {
                string branchCondition = string.Empty;
                if (MainClass.BranchNo != -1)
                {
                    branchCondition = " branch=" + MainClass.BranchNo + " and ";
                }

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select val,val1,exchange_price from inv,inv_sub where " + branchCondition +
                    " Inv_Sub.ItemId=" + itemId +
                    " and inv.proc_type=1 and inv.inv_type=1 and inv_sub.proc_type=1 and inv.InvGlobalID=inv_sub.InvGlobalID and IS_Deleted=0 order by inv.id desc",
                    conn))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        double totalPrimary = ToDouble(dataTable.Rows[0]["val"]);
                        double quantity = ToDouble(dataTable.Rows[0]["val1"]);
                        double price = ToDouble(dataTable.Rows[0]["exchange_price"]);

                        if (totalPrimary != 0)
                        {
                            double recentPrice = quantity * price / totalPrimary;
                            txtRecentPurchPrice.Text = recentPrice.ToString("0.00");
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void LoadPrices(int itemId)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select * from ItemPrices where Itemid=" + itemId + " order by Proc_id desc",
                    conn))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        txtLastSalePrice.Text = ToSafeString(dataTable.Rows[0]["low_sale_price"]);
                        txtCompetitorPrice.Text = ToSafeString(dataTable.Rows[0]["CompetitorPrice"]);
                    }
                }
            }
            catch
            {
            }
        }

        #endregion

        #region Totals

        private void CalcRowValues(int index)
        {
            if (index < 0 || index >= invoiceItems.Count)
                return;

            invoiceItems[index].Recalculate();
        }

        private void CalcInRow(int index)
        {
            try
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CalcTot()
        {
            try
            {
                double sumValue = 0;
                double totalAfterDiscount = 0;
                double netValue = 0;
                double totalVat = 0;
                double totalDiscount = 0;
                double totalQuantity = 0;

                foreach (InvoiceItemRowModel row in invoiceItems)
                {
                    if (row.ItemId > 0)
                    {
                        sumValue += row.SumPrice;
                        totalQuantity += row.TotalQuantity;
                        totalDiscount += row.DiscountVal;
                        netValue += row.NetVal;
                        totalVat += row.VatVal;
                        totalAfterDiscount += row.TotalPrice;
                    }
                }

                if (string.IsNullOrWhiteSpace(txtInvDiscVal.Text))
                {
                    txtInvDiscVal.Text = "0";
                }

                double invoiceDiscountValue = ToDouble(txtInvDiscVal.Text);

                if (invoiceDiscountValue > 0)
                {
                    totalAfterDiscount -= invoiceDiscountValue;
                    totalVat = !InvObj.PriceIncVAT
                        ? Math.Round(totalAfterDiscount * (InvObj.VAT / 100.0), 3)
                        : totalAfterDiscount - Math.Round(totalAfterDiscount / (1.0 + InvObj.VAT / 100.0), 3);

                    netValue = totalAfterDiscount + totalVat;
                }

                txtTirmsNo.Text = invoiceItems.Count.ToString();
                txtTotQuan.Text = totalQuantity.ToString("0.##");
                txtSumVal.Text = Math.Round(sumValue, 2).ToString("0.##");
                txtTotDiscount.Text = Math.Round(totalDiscount + invoiceDiscountValue, 2).ToString("0.##");
                txtTotVAT.Text = Math.Round(totalVat, 2).ToString("0.##");
                txtNet.Text = Math.Round(netValue, 2).ToString("0.##");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ حساب الإجماليات", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Save / Print

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            isPrintd = false;
            IsPrinted = false;
            SaveAndPrint();
        }

        private void btnSavePrint_Click(object sender, RoutedEventArgs e)
        {
            IsPrinted = true;
            SaveAndPrint();
        }

        private void Save()
        {
            IsPrinted = false;
            SaveAndPrint();
        }

        private async void SaveAndPrint()
        {
            try
            {
                if (invoiceItems.Count == 0)
                {
                    MessageBox.Show("لا يوجد بنود في الفاتورة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (cmbStore.SelectedIndex == -1)
                {
                    MessageBox.Show("يجب اختيار المخزن", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbStore.Focus();
                    return;
                }

                InvoiceOper invoiceOper = new InvoiceOper();
                AuditorAPI.Models.Invoice invoice = BindToInvoice();
                Entry entry = null;

                bool saved = invoiceOper.SaveInvoice(invoice, entry, ISNew);

                if (!saved)
                    return;

                if (IsPrinted && print.PrintNo > 0)
                {
                    RptPrint(invoice, 1);
                }

                if (Sync.ActiveSync && InvObj.SyncInv && Sync.SyncType > 0)
                {
                    if (!InvObj.SyncEntry)
                    {
                        entry = null;
                    }

                    await invoiceOper.SyncInvoice(invoice, entry, ISNew);
                }

                Inventory.UpdateItemStock(invoice);

                Code = invoice.InvoiceNo;
                ProcCode = invoice.AutoIncrementID;
                InvGlobalID = invoice.InvGlobalID;
                ISNew = false;
                txtNo.Background = Brushes.White;

                MessageBoxResult nextAction = MessageBox.Show(
                    "تم الحفظ بنجاح." + Environment.NewLine +
                    "نعم = جديد" + Environment.NewLine +
                    "لا = البقاء في الفاتورة" + Environment.NewLine +
                    "إلغاء = إغلاق",
                    "نجاح",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Information);

                if (nextAction == MessageBoxResult.Yes)
                {
                    CLR();
                }
                else if (nextAction == MessageBoxResult.Cancel)
                {
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "خطأ أثناء الحفظ" + Environment.NewLine + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AuditorAPI.Models.Invoice invoice = BindToInvoice();
                RptPrint(invoice, 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ طباعة", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AuditorAPI.Models.Invoice invoice = BindToInvoice();
                RptPrint(invoice, 2);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ معاينة", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RptPrint(AuditorAPI.Models.Invoice invoice, int type)
        {
            try
            {
                if (invoiceItems.Count == 0)
                {
                    MessageBox.Show("لا توجد عمليات بالجدول", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(print.RptUrl))
                {
                    MessageBox.Show("يجب تحديد مسار التقرير", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(print.RptName))
                {
                    if (InvType == 4 || InvType == 5)
                    {
                        print.RptName = "RptInvInOutput.repx";
                    }
                }

                string reportPath = Path.Combine(print.RptUrl, print.RptName);

                if (!Directory.Exists(print.RptUrl) || !File.Exists(reportPath))
                {
                    MessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(print.defPrinter))
                {
                    print.defPrinter = MainClass.ReportsPrinter;
                }

                print.Printing(
                    type,
                    print.BindToData(invoice),
                    print.RptUrl,
                    print.RptName,
                    print.defPrinter,
                    print.kitchenprinter,
                    print.PrintNo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ الطباعة", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Invoice Binding

        private void GetInvoiceIDs()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtSumVal.Text))
                {
                    txtSumVal.Text = "0";
                }

                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }

                if (ProcCode == -1)
                {
                    using (SqlCommand command = new SqlCommand("select ISNULL(MAX(proc_id), 0) from Inv", conn))
                    {
                        ProcCode = ToInt(command.ExecuteScalar()) + 1;
                    }

                    LoadInvNo();
                    Code = ToInt(txtNo.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ ترقيم الفاتورة", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        private AuditorAPI.Models.Invoice BindToInvoice()
        {
            AuditorAPI.Models.Invoice invoice = new AuditorAPI.Models.Invoice();

            if (ProcCode == -1)
            {
                GetInvoiceIDs();
                InvGlobalID = MainClass.BranchNo + "-" + ProcCode;
            }

            invoice.AutoIncrementID = ProcCode;
            invoice.ClientCode = Sync.ClientCode;
            invoice.Total = ToDouble(txtSumVal.Text);
            invoice.Delivery = 0;
            invoice.SumPrice = ToDouble(txtSumVal.Text);
            invoice.VAT = Math.Round(ToDouble(txtTotVAT.Text), 2);
            invoice.Net = Math.Round(ToDouble(txtNet.Text), 2);
            invoice.Discount = Math.Round(ToDouble(txtTotDiscount.Text), 2);
            invoice.Additions = 0;
            invoice.Insurance = 0;
            invoice.Paycash = 0;
            invoice.PayATM = 0;
            invoice.PayType = -1;
            invoice.Remainder = 0;
            invoice.InvGlobalID = InvGlobalID;
            invoice.EntryGlobalID = "-1";
            invoice.InvoiceNo = ToInt(txtNo.Text);
            invoice.InvoiceType = (AuditorAPI.Models.InvoiceType)InvType;
            invoice.Bank = -1;
            invoice.ProcType = ProcType;
            invoice.OrderNo = -1;
            invoice.OrderType = -1;
            invoice.InvDate = GetDateValue(txtDate);
            invoice.User = MainClass.EmpNo;
            invoice.Branch = MainClass.BranchNo;
            invoice.DistBranch = Sync.DistBranch;
            invoice.BranchType = Sync.BranchType;
            invoice.Treasury = -1;
            invoice.Saleman = -1;
            invoice.Customer = 1;

            if (cmbClient.SelectedIndex > -1)
            {
                invoice.Customer = GetSelectedClientId();
            }

            if (!string.IsNullOrWhiteSpace(txtNote.Text))
            {
                invoice.InvNote = txtNote.Text.Trim();
            }
            else
            {
                invoice.InvNote = InvoiceOper.GetInvoiceType((int)invoice.InvoiceType, invoice.ProcType, invoice.PayType, 1) + " برقم " + invoice.InvoiceNo;
            }

            invoice.IsDeleted = false;
            invoice.VATperc = InvObj.VAT;
            invoice.ReffNo = "-1";

            if (!string.IsNullOrWhiteSpace(txtRefNo.Text))
            {
                invoice.ReffNo = txtRefNo.Text.Trim();
            }

            invoice.RefDate = GetDateValue(txtRefDate);
            invoice.Store = GetSelectedStoreId() > 0 ? GetSelectedStoreId() : 1;

            List<AuditorAPI.Models.Item> items = new List<AuditorAPI.Models.Item>();

            foreach (InvoiceItemRowModel row in invoiceItems)
            {
                AuditorAPI.Models.Item item = new AuditorAPI.Models.Item
                {
                    InvGlobalID = InvGlobalID,
                    ClientCode = Sync.ClientCode,
                    Name = row.ItemName,
                    ItemNo = row.ItemId,
                    ProcType = InvType == 4 ? 1 : InvType == 5 ? 2 : 0,
                    Description = row.Description ?? string.Empty,
                    Quantity = row.Quantity,
                    UnitEquality = row.UnitEquality,
                    PrimaryQnty = row.TotalQuantity,
                    Unit = Common.GetUnitID(row.UnitName),
                    Price = row.Price,
                    AvegCost = row.AvgCost,
                    Vat = row.VatVal,
                    VatPerc = row.VatPer,
                    Barcode = row.Barcode,
                    ItemDiscount = row.DiscountVal,
                    ValiableStock = 0,
                    Store = row.StoreId > 0 ? row.StoreId : invoice.Store,
                    ExpireDate = DateTime.Now.AddYears(1),
                    Note = string.Empty,
                    ProductId = 0
                };

                items.Add(item);
            }

            invoice.Items = items;
            return invoice;
        }

        private async void SyncInvoice(AuditorAPI.Models.Invoice inv, Entry entry)
        {
            try
            {
                InvoiceOper invoiceOper = new InvoiceOper();

                if (Sync.ActiveSync && InvObj.SyncInv)
                {
                    if (!InvObj.SyncEntry)
                    {
                        entry = null;
                    }

                    await invoiceOper.SyncInvoice(inv, entry, ISNew);
                }
            }
            catch
            {
            }
        }

        #endregion

        #region Search / Load Grid

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void Search()
        {
            try
            {
                string branchCondition = string.Empty;
                if (MainClass.BranchNo != -1)
                {
                    branchCondition = "inv.branch=" + MainClass.BranchNo + " and ";
                }

                if (string.IsNullOrWhiteSpace(txtSrchNo.Text))
                {
                    LoadDG(branchCondition + " date>=@date1 and date<=@date2 and ");
                }
                else
                {
                    int invoiceNumber;
                    if (!int.TryParse(txtSrchNo.Text.Trim(), out invoiceNumber))
                    {
                        MessageBox.Show("رقم الفاتورة غير صحيح", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    LoadDG(branchCondition + " inv.id=" + invoiceNumber + " and ");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ بحث", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDG(string condition)
        {
            try
            {
                searchRows.Clear();

                string sql =
                    "select Inv.InvGlobalID,Inv.id as id,Inv.date as date " +
                    "from Inv where inv_type=" + InvType +
                    " and proc_type=" + (cmbProcTypeSrch.SelectedIndex + 1) +
                    " and " + condition +
                    " Inv.IS_Deleted=0 order by Inv.id";

                using (SqlCommand command = new SqlCommand(sql, conn))
                {
                    if (condition.Contains("@date1"))
                    {
                        DateTime fromDate = GetDateValue(txtFromDate).Date;
                        DateTime toDate = GetDateValue(txtToDate).Date.AddDays(1);

                        command.Parameters.Add("@date1", SqlDbType.DateTime).Value = fromDate;
                        command.Parameters.Add("@date2", SqlDbType.DateTime).Value = toDate;
                    }

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        foreach (DataRow row in dataTable.Rows)
                        {
                            searchRows.Add(new InvoiceSearchRowModel
                            {
                                InvGlobalID = ToSafeString(row["InvGlobalID"]),
                                InvoiceNo = ToSafeString(row["id"]),
                                InvoiceDate = Convert.ToDateTime(row["date"]).ToShortDateString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ تحميل البحث", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgvSrch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!(dgvSrch.SelectedItem is InvoiceSearchRowModel selectedRow))
                    return;

                if (ISreturn)
                {
                    int selectedProcId = GetProcIdFromInvGlobalID(selectedRow.InvGlobalID);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(
                        "select id from Inv where proc_type=2 and inv_type=" + InvType +
                        " and Reff_No=" + selectedProcId + " and IS_Deleted=0",
                        conn))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        if (dataTable.Rows.Count >= 1)
                        {
                            MessageBox.Show("الفاتورة تم إرجاعها سابقًا", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                }

                LoadInvoiceByGlobalId(selectedRow.InvGlobalID);
                TabControl1.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ تحميل الفاتورة", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetProcIdFromInvGlobalID(string invGlobalId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(invGlobalId))
                    return -1;

                string[] parts = invGlobalId.Split('-');
                if (parts.Length >= 2)
                {
                    int procId;
                    if (int.TryParse(parts[1], out procId))
                    {
                        return procId;
                    }
                }
            }
            catch
            {
            }

            return -1;
        }

        #endregion

        #region Navigation / Read

        public void Navigate(string sqlstr)
        {
            try
            {
                using (SqlCommand command = new SqlCommand(sqlstr, conn))
                {
                    if (conn.State != ConnectionState.Open)
                    {
                        conn.Open();
                    }

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        ReadData(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ تنقل", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        private void LoadInvoiceByGlobalId(string invGlobalId)
        {
            if (string.IsNullOrWhiteSpace(invGlobalId))
                return;

            Navigate("select * from Inv where InvGlobalID=N'" + invGlobalId.Replace("'", "''") + "'");
        }

        private void ReadData(SqlDataReader dr)
        {
            try
            {
                if (!dr.HasRows)
                {
                    CLR();
                    return;
                }

                dr.Read();

                CLR();
                isPrintd = true;
                ISNew = false;

                InvGlobalID = ToSafeString(dr["InvGlobalID"]);
                ProcCode = ToInt(dr["proc_id"]);
                Code = ToInt(dr["id"]);

                txtNo.Text = Code.ToString();
                txtDate.EditValue = Convert.ToDateTime(dr["date"]);
                InvType = ToInt(dr["inv_type"]);
                txtSumVal.Text = ToSafeString(dr["InvTotal"]);
                txtNet.Text = ToSafeString(dr["tot_net"]);
                txtInvDiscVal.Text = ToSafeString(dr["minus"]);
                txtInvTax.Text = ToSafeString(dr["tax"]);
                txtNote.Text = ToSafeString(dr["notes"]);

                try
                {
                    if (dr["Reff_date"] != DBNull.Value)
                    {
                        txtRefDate.EditValue = Convert.ToDateTime(dr["Reff_date"]);
                    }

                    txtRefNo.Text = ToSafeString(dr["Reff_No"]);
                }
                catch
                {
                }

                int procTypeFromDb = ToInt(dr["proc_type"]);
                if (procTypeFromDb > 0 && cmbProcType.Items.Count >= procTypeFromDb)
                {
                    cmbProcType.SelectedIndex = procTypeFromDb - 1;
                }

                try
                {
                    cmbStore.SelectedValue = ToInt(dr["safe"]);
                }
                catch
                {
                }

                try
                {
                    if (ToInt(dr["cust_id"]) != -1)
                    {
                        cmbClient.SelectedValue = ToInt(dr["cust_id"]);
                    }
                }
                catch
                {
                }

                dr.Close();

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select * from Inv_Sub where InvGlobalID=N'" + InvGlobalID.Replace("'", "''") + "'",
                    conn))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        int itemId = ToInt(dataRow["ItemId"]);
                        int unitId = ToInt(dataRow["unit"]);
                        int storeId = ToInt(dataRow["store"]);
                        double quantity = ToDouble(dataRow["val1"]);
                        double primaryQuantity = ToDouble(dataRow["val"]);
                        double exchangePrice = ToDouble(dataRow["exchange_price"]);
                        double avgCost = dataRow["AvrgCost"] == DBNull.Value ? 0 : ToDouble(dataRow["AvrgCost"]);
                        double discount = ToDouble(dataRow["discount"]);
                        double taxPerc = ToDouble(dataRow["taxperc"]);
                        double taxVal = ToDouble(dataRow["taxval"]);

                        double unitEquality = 1;
                        using (SqlDataAdapter unitAdapter = new SqlDataAdapter(
                            "select ItemUnits.perc from Items,ItemUnits where Items.id=ItemUnits.ItemId and ItemUnits.unit=" + unitId + " and ItemUnits.ItemId=" + itemId,
                            conn))
                        {
                            DataTable unitTable = new DataTable();
                            unitAdapter.Fill(unitTable);

                            if (unitTable.Rows.Count > 0)
                            {
                                unitEquality = ToDouble(unitTable.Rows[0][0]);
                            }
                        }

                        InvoiceItemRowModel row = new InvoiceItemRowModel
                        {
                            SuppressRecalculate = true,
                            ItemId = itemId,
                            ItemCode = GetItemCode(itemId),
                            ItemName = GetItemName(itemId),
                            UnitName = GetUnitName(unitId),
                            Quantity = quantity,
                            UnitEquality = unitEquality,
                            TotalQuantity = primaryQuantity,
                            Price = exchangePrice,
                            SumPrice = Math.Round(quantity * exchangePrice, 2),
                            AvgCost = avgCost,
                            DiscountVal = discount,
                            DiscountPer = (Math.Round(quantity * exchangePrice, 2) == 0) ? 0 : Math.Round((discount / Math.Round(quantity * exchangePrice, 2)) * 100, 2),
                            TotalPrice = Math.Round(Math.Round(quantity * exchangePrice, 2) - discount, 2),
                            VatPer = taxPerc,
                            VatVal = taxVal,
                            NetVal = Math.Round(Math.Round(quantity * exchangePrice, 2) - discount + taxVal, 2),
                            Stock = CalcStock(itemId, storeId),
                            StoreId = storeId,
                            StoreName = GetStoreName(storeId),
                            Barcode = string.Empty
                        };

                        row.SuppressRecalculate = false;
                        invoiceItems.Add(row);
                    }
                }

                CalcTot();
                LoadItemDetails();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ قراءة البيانات", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetItemName(int id)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select name from Items where id=" + id,
                    conn1))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        return ToSafeString(dataTable.Rows[0][0]);
                    }
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private string GetItemCode(int id)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select Code from Items where id=" + id,
                    conn1))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        return ToSafeString(dataTable.Rows[0][0]);
                    }
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private string GetStoreName(int storeId)
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select name from Safes where id=" + storeId,
                    conn1))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        return ToSafeString(dataTable.Rows[0][0]);
                    }
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from Inv where inv_type=" + InvType + " and proc_type=" + ProcType + " and IS_Deleted=0 order by id asc");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from Inv where inv_type=" + InvType + " and proc_type=" + ProcType + " and IS_Deleted=0 and id<" + ToInt(txtNo.Text) + " order by id desc");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from Inv where inv_type=" + InvType + " and proc_type=" + ProcType + " and IS_Deleted=0 and id>" + ToInt(txtNo.Text) + " order by id asc");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from Inv where inv_type=" + InvType + " and proc_type=" + ProcType + " and IS_Deleted=0 order by id desc");
        }

        #endregion

        #region Clear / New / Delete

        private void CLR()
        {
            try
            {
                invoiceItems.Clear();
                searchRows.Clear();

                Code = -1;
                ProcCode = -1;
                SelectedId = -1;
                RowIndex = -1;
                InvGlobalID = string.Empty;

                txtSumVal.Text = "0";
                txtNet.Text = "0";
                txtTotDiscount.Text = "0";
                txtTotVAT.Text = "0";
                txtTotQuan.Text = "0";
                txtTirmsNo.Text = "0";
                txtInvDisc.Text = "0";
                txtInvDiscVal.Text = "0";
                txtInvTaxPer.Text = "0";
                txtInvTax.Text = "0";

                txtBarcode.Text = string.Empty;
                txtRefNo.Text = string.Empty;
                txtNote.Text = string.Empty;
                txtBalance.Text = string.Empty;
                txtSrchNo.Text = string.Empty;

                txtDate.EditValue = DateTime.Now;
                txtToDate.EditValue = DateTime.Now;
                txtRefDate.EditValue = DateTime.Now;
                txtFromDate.EditValue = DateTime.Now;

                isPrintd = false;
                ISNew = true;
                IsPrinted = false;
                ISreturn = false;

                if (cmbStore.Items.Count > 0)
                {
                    cmbStore.SelectedIndex = 0;
                }

                if (cmbProcTypeSrch.Items.Count > 0)
                {
                    cmbProcTypeSrch.SelectedIndex = 0;
                }

                if (cmbClient.Items.Count > 0)
                {
                    cmbClient.SelectedIndex = 0;
                }

                ClearItemDetailsPanel();
                LoadInvNo();

                txtNo.Background = new SolidColorBrush(Color.FromRgb(240, 244, 255));
                txtBarcode.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ تنظيف", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            CLR();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Code == -1 || string.IsNullOrWhiteSpace(InvGlobalID))
                {
                    MessageBox.Show("اختر فاتورة ليتم حذفها", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBoxResult result = MessageBox.Show(
                    "هل أنت متأكد من حذف الفاتورة؟",
                    "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }

                using (SqlCommand command = new SqlCommand(
                    "update Inv set IS_Deleted=1 where InvGlobalID=N'" + InvGlobalID.Replace("'", "''") + "'",
                    conn))
                {
                    command.ExecuteNonQuery();
                }

                MessageBox.Show(
                    MainClass.Language == "ar" ? "تم الحذف" : "Deleted",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);

                CLR();
            }
            catch (Exception ex)
            {
                string errorText = MainClass.Language == "ar"
                    ? "خطأ أثناء الحذف" + Environment.NewLine + "تفاصيل الخطأ: " + ex.Message
                    : "Error in delete" + Environment.NewLine + "Error details: " + ex.Message;

                MessageBox.Show(errorText, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }

        private void deleteRow()
        {
            try
            {
                if (!(dgvItems.SelectedItem is InvoiceItemRowModel row))
                {
                    MessageBox.Show("برجى اختيار السجل الذى تريد حذفه", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBoxResult result = MessageBox.Show(
                    "هل تريد حذف السجل؟",
                    "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    invoiceItems.Remove(row);
                    CalcTot();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ حذف سطر", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Customer Handling

        private void btnCustAdd_Click(object sender, RoutedEventArgs e)
        {
            addNewClint();
        }

        private void cmbClient_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                SrchByNameClint();
            }
        }

        private void cmbClient_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ShowCustBalance();
            }
            catch
            {
            }
        }

        private void cmbStore_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                UpdateCurrentStoreForRows();
            }
            catch
            {
            }
        }

        private void SrchByNameClint()
        {
            try
            {
                string clientName = cmbClient.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(clientName))
                    return;

                int customerType = InvType == 5 ? 1 : 2;

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id,name from Customers where IS_Deleted=0 and name=N'" + clientName.Replace("'", "''") + "' and (type=" + customerType + " or type=3)",
                    conn))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        cmbClient.SelectedValue = ToInt(dataTable.Rows[0]["id"]);
                    }
                    else
                    {
                        addNewClint();
                    }
                }
            }
            catch
            {
            }
        }

        private void addNewClint()
        {
            try
            {
                frmSrchClient clientWindow = new frmSrchClient();
                MainClass.ApplyPermissionToForm(clientWindow);
                MainClass.DoApplyUserSett(clientWindow);

                if (InvType == 4)
                {
                    clientWindow.Type = 2;
                }
                else if (InvType == 5)
                {
                    clientWindow.Type = 1;
                }

                clientWindow.txtClientName.Text = cmbClient.Text;
                clientWindow.ShowDialog();

                if (!string.IsNullOrWhiteSpace(clientWindow.Clientname))
                {
                    LoadCustomers();
                    cmbClient.SelectedValue = clientWindow.ClientId;
                }
            }
            catch
            {
            }
        }

        private void ShowCustBalance()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cmbClient.Text))
                {
                    txtBalance.Text = string.Empty;
                    return;
                }

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select Code from Accounts_Index where AName=N'" + cmbClient.Text.Replace("'", "''") + "' " + Accounting.BranchCondition,
                    conn1))
                {
                    DataTable accountTable = new DataTable();
                    adapter.Fill(accountTable);

                    if (accountTable.Rows.Count == 0)
                    {
                        txtBalance.Text = "0";
                        return;
                    }

                    string accountCondition = " and Entry_sub.acc_no=" + ToSafeString(accountTable.Rows[0][0]);
                    string branchCondition = string.Empty;

                    if (MainClass.BranchNo != -1)
                    {
                        branchCondition = "Entry.branch=" + MainClass.BranchNo + " and Entry_sub.branch=" + MainClass.BranchNo + " and ";
                    }

                    double debitTotal = 0;
                    double creditTotal = 0;

                    using (SqlDataAdapter entryAdapter = new SqlDataAdapter(
                        "select Entry.id,Entry.date,sum(Entry_sub.dept) as dept ,sum(Entry_sub.credit) as credit,Entry.notes " +
                        "from Entry,Entry_sub where " + branchCondition +
                        "Entry.IS_Deleted=0 and Entry.state=1 and Entry.GlobalId=Entry_sub.EntryGlobalId " +
                        accountCondition +
                        " group by Entry.id,Entry.date,Entry.notes,Entry_sub.acc_no",
                        conn1))
                    {
                        DataTable entryTable = new DataTable();
                        entryAdapter.Fill(entryTable);

                        foreach (DataRow row in entryTable.Rows)
                        {
                            debitTotal += ToDouble(row["dept"]);
                            creditTotal += ToDouble(row["credit"]);
                        }
                    }

                    if (debitTotal > creditTotal)
                    {
                        txtBalance.Text = Math.Round(debitTotal - creditTotal, 3).ToString("0.###");
                    }
                    else if (creditTotal > debitTotal)
                    {
                        txtBalance.Text = Math.Round(creditTotal - debitTotal, 3).ToString("0.###") + " دائن";
                    }
                    else
                    {
                        txtBalance.Text = "0";
                    }
                }
            }
            catch
            {
            }
        }

        #endregion

        #region Context Menu Events

        private void StripItemSearch_Click(object sender, RoutedEventArgs e)
        {
            addNewItem();
        }

        private void StripAddNewItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                frmItems itemWindow = new frmItems();
                MainClass.ApplyPermissionToForm(itemWindow);
                MainClass.DoApplyUserSett(itemWindow);
                itemWindow.ShowDialog();
            }
            catch
            {
            }
        }

        private void StripAddGroup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                frmItemCategory categoryWindow = new frmItemCategory();
                MainClass.ApplyPermissionToForm(categoryWindow);
                MainClass.DoApplyUserSett(categoryWindow);
                categoryWindow.ShowDialog();
            }
            catch
            {
            }
        }

        private void StripItemUnits_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgvItems.SelectedItem is InvoiceItemRowModel row)
                {
                    int index = invoiceItems.IndexOf(row);
                    ShowItemUnit(index);
                }
            }
            catch
            {
            }
        }

        private void StripdeleteRow_Click(object sender, RoutedEventArgs e)
        {
            deleteRow();
        }

        private void StripItemProcess_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(dgvItems.SelectedItem is InvoiceItemRowModel row))
                    return;

                frmRptItemsActivity reportWindow = new frmRptItemsActivity();
                reportWindow.ItemId = row.ItemId;
                MainClass.ApplyPermissionToForm(reportWindow);
                MainClass.DoApplyUserSett(reportWindow);
                reportWindow.ShowDialog();
            }
            catch
            {
            }
        }

        private void StripClientLastItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(dgvItems.SelectedItem is InvoiceItemRowModel row))
                    return;

                frmRptItemsActivityDetailed reportWindow = new frmRptItemsActivityDetailed();
                MainClass.ApplyPermissionToForm(reportWindow);
                MainClass.DoApplyUserSett(reportWindow);
                reportWindow.Show();
                reportWindow.LastProcess(row.ItemId, row.StoreId, -1, 1);
            }
            catch
            {
            }
        }

        private void StripLastItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(dgvItems.SelectedItem is InvoiceItemRowModel row))
                    return;

                frmRptItemsActivityDetailed reportWindow = new frmRptItemsActivityDetailed();
                MainClass.ApplyPermissionToForm(reportWindow);
                MainClass.DoApplyUserSett(reportWindow);
                reportWindow.Show();
                reportWindow.LastProcess(row.ItemId, row.StoreId, -1, 1);
            }
            catch
            {
            }
        }

        private void StripItemAvrgCost_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(dgvItems.SelectedItem is InvoiceItemRowModel row))
                    return;

                MessageBox.Show(
                    ItemOper.AvgCost(row.ItemId, MainClass.BranchNo).ToString(),
                    "متوسط التكلفة",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch
            {
            }
        }

        private void StripItemDetail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(dgvItems.SelectedItem is InvoiceItemRowModel row))
                    return;

                frmItems itemWindow = new frmItems();
                MainClass.ApplyPermissionToForm(itemWindow);
                MainClass.DoApplyUserSett(itemWindow);
                itemWindow.ItemId = row.ItemId;
                itemWindow.ShowDialog();

                int rowIndex = invoiceItems.IndexOf(row);
                LoadItemInf(row.ItemId, rowIndex, 1);
            }
            catch
            {
            }
        }

        #endregion

        #region Extra Legacy-Compatible Methods

        private string GetSalesEmpNameByInvNo()
        {
            try
            {
                if (ProcCode != -1 && !string.IsNullOrWhiteSpace(InvGlobalID))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(
                        "select users.username from Employees,Inv,users where users.emp=Employees.id and Employees.id=Inv.sales_emp and Inv.InvGlobalID=N'" + InvGlobalID.Replace("'", "''") + "'",
                        conn1))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        if (dataTable.Rows.Count > 0)
                        {
                            return ToSafeString(dataTable.Rows[0][0]);
                        }
                    }
                }
                else
                {
                    return MainClass.UserName;
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private double CalcAvgCost(int itemId)
        {
            try
            {
                double totalCost = 0;
                int totalQty = 0;
                double averageCost = 0;
                string branchCondition = string.Empty;

                if (MainClass.BranchNo != -1)
                {
                    branchCondition = "inv.branch=" + MainClass.BranchNo + " and ";
                }

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select sum(val),sum(val1*exchange_price) from inv,inv_sub where " + branchCondition +
                    " ItemId=" + itemId +
                    " and inv.inv_type=1 and inv.proc_type=1 and inv_sub.proc_type=1 and inv.InvGlobalID=inv_sub.InvGlobalID and IS_Deleted=0",
                    conn))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0 && !string.IsNullOrWhiteSpace(ToSafeString(dataTable.Rows[0][1])))
                    {
                        totalCost += ToDouble(dataTable.Rows[0][1]);
                        totalQty += (int)Math.Round(ToDouble(dataTable.Rows[0][0]));
                    }
                }

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "select sum(val),sum(val1*exchange_price) from inv,inv_sub where " + branchCondition +
                    " ItemId=" + itemId +
                    " and inv.inv_type=1 and inv.proc_type=3 and inv.InvGlobalID=inv_sub.InvGlobalID and IS_Deleted=0",
                    conn))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0 && !string.IsNullOrWhiteSpace(ToSafeString(dataTable.Rows[0][1])))
                    {
                        totalCost += ToDouble(dataTable.Rows[0][1]);
                        totalQty += (int)Math.Round(ToDouble(dataTable.Rows[0][0]));
                    }
                }

                if (totalQty != 0)
                {
                    averageCost = Math.Floor(totalCost / totalQty * 100000000.0) / 100000000.0;
                }
                else
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(
                        "select purch_price from Currency_Lastprice_Sub where currency1=" + itemId,
                        conn))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);

                        if (dataTable.Rows.Count > 0)
                        {
                            averageCost = ToDouble(dataTable.Rows[0][0]);
                        }
                    }
                }

                return Math.Round(averageCost, 2);
            }
            catch
            {
                return 0;
            }
        }

        private int CalcStock(int itemID, int storeId)
        {
            try
            {
                return (int)Math.Round(Inventory.CalcItemStock(storeId, itemID, MainClass.BranchNo));
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region Linked Invoice

        private void btnInsertLinkedInv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnNew_Click(null, null);
                InsertFromInv(1);
                ProcCode = -1;
                ISNew = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "خطأ الإدراج", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InsertFromInv(int Ptype)
        {
        }

        #endregion
    }

    #region Models

    public class InvoiceItemRowModel : INotifyPropertyChanged
    {
        private int rowNo;
        private string itemCode;
        private int itemId;
        private string itemName;
        private string description;
        private string barcode;
        private string unitName;
        private double quantity = 1;
        private double unitEquality = 1;
        private double totalQuantity;
        private double price;
        private double sumPrice;
        private double avgCost;
        private double discountVal;
        private double discountPer;
        private double totalPrice;
        private double vatPer;
        private double vatVal;
        private double netVal;
        private double stock;
        private int storeId;
        private string storeName;

        public bool SuppressRecalculate { get; set; }

        public Action RecalculateRequested { get; set; }

        public int RowNo
        {
            get => rowNo;
            set
            {
                rowNo = value;
                OnPropertyChanged(nameof(RowNo));
            }
        }

        public string ItemCode
        {
            get => itemCode;
            set
            {
                itemCode = value;
                OnPropertyChanged(nameof(ItemCode));
            }
        }

        public int ItemId
        {
            get => itemId;
            set
            {
                itemId = value;
                OnPropertyChanged(nameof(ItemId));
            }
        }

        public string ItemName
        {
            get => itemName;
            set
            {
                itemName = value;
                OnPropertyChanged(nameof(ItemName));
            }
        }

        public string Description
        {
            get => description;
            set
            {
                description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        public string Barcode
        {
            get => barcode;
            set
            {
                barcode = value;
                OnPropertyChanged(nameof(Barcode));
            }
        }

        public string UnitName
        {
            get => unitName;
            set
            {
                unitName = value;
                OnPropertyChanged(nameof(UnitName));
            }
        }

        public double Quantity
        {
            get => quantity;
            set
            {
                quantity = value;
                OnPropertyChanged(nameof(Quantity));

                if (!SuppressRecalculate)
                {
                    Recalculate();
                }
            }
        }

        public double UnitEquality
        {
            get => unitEquality;
            set
            {
                unitEquality = value;
                OnPropertyChanged(nameof(UnitEquality));

                if (!SuppressRecalculate)
                {
                    Recalculate();
                }
            }
        }

        public double TotalQuantity
        {
            get => totalQuantity;
            set
            {
                totalQuantity = value;
                OnPropertyChanged(nameof(TotalQuantity));
            }
        }

        public double Price
        {
            get => price;
            set
            {
                price = value;
                OnPropertyChanged(nameof(Price));

                if (!SuppressRecalculate)
                {
                    Recalculate();
                }
            }
        }

        public double SumPrice
        {
            get => sumPrice;
            set
            {
                sumPrice = value;
                OnPropertyChanged(nameof(SumPrice));
            }
        }

        public double AvgCost
        {
            get => avgCost;
            set
            {
                avgCost = value;
                OnPropertyChanged(nameof(AvgCost));
            }
        }

        public double DiscountVal
        {
            get => discountVal;
            set
            {
                discountVal = value;
                OnPropertyChanged(nameof(DiscountVal));

                if (!SuppressRecalculate)
                {
                    Recalculate();
                }
            }
        }

        public double DiscountPer
        {
            get => discountPer;
            set
            {
                discountPer = value;
                OnPropertyChanged(nameof(DiscountPer));
            }
        }

        public double TotalPrice
        {
            get => totalPrice;
            set
            {
                totalPrice = value;
                OnPropertyChanged(nameof(TotalPrice));
            }
        }

        public double VatPer
        {
            get => vatPer;
            set
            {
                vatPer = value;
                OnPropertyChanged(nameof(VatPer));

                if (!SuppressRecalculate)
                {
                    Recalculate();
                }
            }
        }

        public double VatVal
        {
            get => vatVal;
            set
            {
                vatVal = value;
                OnPropertyChanged(nameof(VatVal));
            }
        }

        public double NetVal
        {
            get => netVal;
            set
            {
                netVal = value;
                OnPropertyChanged(nameof(NetVal));
            }
        }

        public double Stock
        {
            get => stock;
            set
            {
                stock = value;
                OnPropertyChanged(nameof(Stock));
            }
        }

        public int StoreId
        {
            get => storeId;
            set
            {
                storeId = value;
                OnPropertyChanged(nameof(StoreId));
            }
        }

        public string StoreName
        {
            get => storeName;
            set
            {
                storeName = value;
                OnPropertyChanged(nameof(StoreName));
            }
        }

        public void Recalculate()
        {
            TotalQuantity = Quantity * UnitEquality;
            SumPrice = Math.Round(Quantity * Price, 2);

            double lineTotalAfterDiscount = Math.Round(SumPrice - DiscountVal, 2);
            if (lineTotalAfterDiscount < 0)
            {
                lineTotalAfterDiscount = 0;
            }

            TotalPrice = lineTotalAfterDiscount;
            VatVal = Math.Round(lineTotalAfterDiscount * (VatPer / 100.0), 2);
            NetVal = Math.Round(TotalPrice + VatVal, 2);

            RecalculateRequested?.Invoke();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class InvoiceSearchRowModel
    {
        public string InvGlobalID { get; set; }
        public string InvoiceNo { get; set; }
        public string InvoiceDate { get; set; }
    }

    #endregion
}