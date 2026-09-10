using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
using Microsoft.Win32;
using Newtonsoft.Json;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInventoryTransfer : ThemedWindow
    {
        #region ══════════════════════════════════════════════════════════════════
        //                              FIELDS & PROPERTIES
        #endregion

        private SqlConnection _conn;
        private SqlConnection _conn1;

        public int InvType = 8;
        public int ProcType = 2;
        public int EntryType;

        private Print _print;
        private InvoiceObj _invObj;
        private InvoiceDGV _invoic;
        private string _styleFile;
        private DispatcherTimer _timer;

        public FormPermission FrmPermission;

        private static readonly ILog Logger = LogManager.GetLogger(Environment.MachineName);

        private string _focusedFieldName = string.Empty;
        private bool _isLoading = false;

        #region ══════════════════════════════════════════════════════════════════
        //                           GRID HELPERS (WPF)
        #endregion

        private int GC1_RowCount => GridControl1.VisibleRowCount;
        private int GC2_RowCount => GridControl2.VisibleRowCount;

        private int GC1_FocusedRow
        {
            get => GridView1.FocusedRowHandle;
            set => GridView1.FocusedRowHandle = value;
        }

        private object GC1_GetFocused(string fieldName)
            => GridControl1.GetCellValue(GC1_FocusedRow, fieldName);

        private object GC1_GetCell(int rowHandle, string fieldName)
            => GridControl1.GetCellValue(rowHandle, fieldName);

        private void GC1_SetFocused(string fieldName, object value)
            => GridControl1.SetCellValue(GC1_FocusedRow, fieldName, value);

        private void GC1_SetCell(int rowHandle, string fieldName, object value)
            => GridControl1.SetCellValue(rowHandle, fieldName, value);

        private void GC1_Refresh() => GridControl1.RefreshData();

        private void GC1_DeleteRow(int rowHandle)
        {
            // في WPF DevExpress نحذف من الـ ItemsSource مباشرة
            if (_invoic?.InvoiceItems != null && rowHandle >= 0 && rowHandle < _invoic.InvoiceItems.Count)
            {
                _invoic.InvoiceItems.RemoveAt(rowHandle);
                GC1_Refresh();
            }
        }

        private void GC1_MoveLast()
        {
            if (GC1_RowCount > 0)
                GC1_FocusedRow = GC1_RowCount - 1;
        }

        private object GC2_GetFocused(string fieldName)
            => GridControl2.GetCellValue(GridView2.FocusedRowHandle, fieldName);

        private string GC1_FocusedColumnName => GridView1.FocusedColumn?.FieldName ?? string.Empty;

        #region ══════════════════════════════════════════════════════════════════
        //                              CONSTRUCTOR
        #endregion

        public frmInventoryTransfer()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            _conn1 = MainClass.ConnObj();
            _print = new Print(InvType);
            _invObj = new InvoiceObj(InvType, ProcType);
            _invoic = new InvoiceDGV();
            _styleFile = Path.Combine(MainClass.ReportsPath,
                                 "Styles\\InventoryTransferSaveLayout.xml");
            FrmPermission = new FormPermission();
        }

        #region ══════════════════════════════════════════════════════════════════
        //                           WINDOW EVENTS
        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _isLoading = true;

                LoadSettings();
                LoadSafes();

                _invObj = new InvoiceObj(InvType, ProcType);

                txtDate.SelectedDate = DateTime.Today;
                txtRefDate.SelectedDate = DateTime.Today;
                txtDateFrom.SelectedDate = DateTime.Today;
                txtDateTo.SelectedDate = DateTime.Today;
                txtInvTime.Text = DateTime.Now.ToString("h:mm:ss tt");
                txtInvTaxPer.Text = _invObj.VAT.ToString("F2");

                cmbSafeFrom.SelectedIndex = -1;
                cmbSafeTo.SelectedIndex = -1;

                FrmPermission.ApplyFrmPermission(this);
                Common.ApplyEditAddPermission(this, IsNew: true, FrmPermission);

                ResetInvoice();
                LoadSearchSafes();
                SetupContextMenu();

                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromSeconds(1);
                _timer.Tick += (s, ev) => lblCurrentTime.Text = DateTime.Now.ToString("h:mm:ss tt");
                _timer.Start();

                _isLoading = false;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل النافذة", ex);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.B)
                {
                    e.Handled = true;
                    return;
                }

                switch (e.Key)
                {
                    case Key.F1:
                        AddNewItem();
                        e.Handled = true;
                        break;
                    case Key.F5:
                        btnNew_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.F6:
                        btnSave_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.F7:
                        btnPrint_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.F8:
                        btnView_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.F9:
                        btnDelete_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.P:
                        if (Keyboard.Modifiers == ModifierKeys.None)
                        {
                            AddNewItem();
                            e.Handled = true;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في معالجة الاختصارات", ex);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                if ((_invoic.ISNew && GC1_RowCount > 0) || _invoic.IsUpdated)
                {
                    string msg = MainClass.Language == "en-us"
                        ? "The invoice was not saved. Continue?"
                        : "لم يتم حفظ الفاتورة، هل تريد الاستمرار؟";

                    if (DXMessageBox.Show(msg, "تنبيه",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                        e.Cancel = true;
                }

                _timer?.Stop();
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء الإغلاق", ex);
            }
        }

        #region ══════════════════════════════════════════════════════════════════
        //                        SETTINGS & INITIALIZATION
        #endregion

        private void LoadSettings()
        {
            try
            {
                LoadDGvSetting();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل الإعدادات", ex);
            }
        }

        private void LoadDGvSetting()
        {
            try
            {
                if (User.EditPrice)
                {
                    var col = GridControl1.Columns.FirstOrDefault(c => c.FieldName == "ItemPrice");
                    if (col != null)
                        col.ReadOnly = false;
                }

                if (File.Exists(_styleFile))
                {
                    try
                    {
                        GridControl1.RestoreLayoutFromXml(_styleFile);
                    }
                    catch
                    {
                        // تجاهل أخطاء التحميل
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في إعدادات الجدول", ex);
            }
        }

        private void ResetInvoice()
        {
            try
            {
                _invoic.InvoiceType = (InvoiceType)InvType;
                _invoic.ProcType = ProcType;
                LoadInvoiceNumber();
                _invoic.Currency = _invObj.Currency;
                _invoic.InvoiceCode = _invObj.InvoiceCode;
                _invoic.VATperc = _invObj.VAT;
                _invoic.AdditionalCost = 0m;
                _invoic.Branch = MainClass.BranchNo;
                _invoic.InvoiceStatus = 3;
                _invoic.InvertoryImpact = _invObj.InvertoryImpact;
                _invoic.ExtraVATPerc = _invObj.AdditionalTax;
                _invoic.PriceIncVAT = _invObj.PriceIncVAT;
                _invoic.InvAccCode = _invObj.InvAcc;
                _invoic.Store = SafeInt(cmbSafeFrom.SelectedValue);
                _invoic.InvertoryName = cmbSafeFrom.Text;
                _invoic.Treasury = 0;
                _invoic.Customer = 1;
                _invoic.User = MainClass.EmpNo;
                _invoic.InvCCcode = "-1";
                _invoic.CreateDate = DateTime.Now;
                _invoic.InvDate = DateTime.Today;
                _invoic.InvTime = DateTime.Now;
                _invoic.RefDate = DateTime.Today;
                _invoic.PayType = 1;
                _invoic.ISNew = true;
                _invoic.IsDeleted = false;
                _invoic.IsLoaded = false;
                _invoic.IsPrinted = false;
                _invoic.ReffNo = "-1";
                _invoic.Pricing = _invObj.Pricing;

                GridControl1.ItemsSource = _invoic.InvoiceItems;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في إعادة تعيين الفاتورة", ex);
            }
        }

        private void LoadInvoiceNumber()
        {
            try
            {
                _invoic.InvoiceNo = InvoiceOper.InvoiceNo(InvType, ProcType, _invObj.Prefixe);
                txtNo.Text = _invoic.InvoiceNo.ToString();
                _invoic.InvCombinedId = string.Concat(
                    MainClass.BranchCode + _invObj.InvoiceCode,
                    ProcType.ToString(),
                    _invoic.InvoiceNo.ToString());
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل رقم الفاتورة", ex);
            }
        }

        #region ══════════════════════════════════════════════════════════════════
        //                            LOAD DATA
        #endregion

        public void LoadSafes()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Safes WHERE status=1 AND IS_Deleted=0 ORDER BY id",
                    _conn);
                var table = new DataTable();
                adapter.Fill(table);

                cmbSafeFrom.DisplayMemberPath = "name";
                cmbSafeFrom.SelectedValuePath = "id";
                cmbSafeFrom.ItemsSource = table.DefaultView;
                cmbSafeFrom.SelectedIndex = -1;

                var table2 = table.Copy();
                cmbSafeTo.DisplayMemberPath = "name";
                cmbSafeTo.SelectedValuePath = "id";
                cmbSafeTo.ItemsSource = table2.DefaultView;
                cmbSafeTo.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل المستودعات", ex);
            }
        }

        private void LoadSearchSafes()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Safes WHERE status=1 AND IS_Deleted=0 ORDER BY id",
                    _conn);
                var table = new DataTable();
                adapter.Fill(table);

                cmbSearchSafe.DisplayMemberPath = "name";
                cmbSearchSafe.SelectedValuePath = "id";
                cmbSearchSafe.ItemsSource = table.DefaultView;
                cmbSearchSafe.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل مستودعات البحث", ex);
            }
        }

        #region ══════════════════════════════════════════════════════════════════
        //                      NAVIGATE & BINDING
        #endregion

        public void Navigate(string sqlStr)
        {
            try
            {
                if (_invoic != null && !_isLoading)
                    CLR();

                InvoiceOper.BindingInvoice(ref _invoic, sqlStr);

                if (_invoic == null)
                {
                    _invoic = new InvoiceDGV();
                    return;
                }

                if (_invoic.InvoiceType != InvoiceType.SafesTransferTo)
                    _invoic.InvNote = string.Empty;

                GC1_Refresh();
                BindingDGV();
                BindingControls();

                if (_invoic.InvoiceType == InvoiceType.SafesTransferTo &&
                    _invoic.ProcType == 2)
                {
                    var ad = new SqlDataAdapter(
                        $"SELECT safe, branch FROM Inv " +
                        $"WHERE Reff_No=N'{_invoic.InvoiceNo}' " +
                        $"AND inv_type=8 AND proc_type=1", _conn);
                    var dt = new DataTable();
                    ad.Fill(dt);
                    if (dt.Rows.Count > 0)
                        cmbSafeTo.SelectedValue = dt.Rows[0]["safe"];
                    return;
                }

                var ad2 = new SqlDataAdapter(
                    $"SELECT safe, branch FROM Inv " +
                    $"WHERE InvGlobalID=N'{_invoic.ReffNo}' " +
                    $"AND inv_type=8 AND proc_type=2", _conn);
                var dt2 = new DataTable();
                ad2.Fill(dt2);
                if (dt2.Rows.Count > 0)
                    cmbSafeFrom.SelectedValue = dt2.Rows[0]["safe"];
                cmbSafeTo.SelectedValue = _invoic.Store;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في التنقل", ex);
            }
        }

        private void BindingDGV()
        {
            try
            {
                GC1_Refresh();
                txtTirmsNo.Text = Math.Max(0, GC1_RowCount - 1).ToString();
                txtSumVal.Text = _invoic.SumPrice.ToString(_invObj.DigitsNo);
                txtTotDiscount.Text = _invoic.TotDiscount.ToString(_invObj.DigitsNo);
                txtInvDiscVal.Text = _invoic.InvDiscount.ToString(_invObj.DigitsNo);
                txtInvDiscPerc.Text = InvoiceOper
                    .GetDiscountPercentage(_invoic.SumPrice, _invoic.InvDiscount)
                    .ToString(_invObj.DigitsNo);
                txtNetWithoutVAT.Text = _invoic.Total.ToString(_invObj.DigitsNo);
                txtTotVAT.Text = _invoic.VAT.ToString(_invObj.DigitsNo);
                lblRowsCount.Text = Math.Max(0, GC1_RowCount - 1).ToString();
                GC1_MoveLast();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحديث العرض", ex);
            }
        }

        private void BindingControls()
        {
            try
            {
                txtNo.Text = _invoic.InvoiceNo.ToString();
                txtDate.SelectedDate = _invoic.InvDate;
                txtRefDate.SelectedDate = _invoic.RefDate;
                txtInvTime.Text = _invoic.InvDate.ToShortDateString() +
                                          " " + _invoic.InvTime.ToShortTimeString();
                LoadSafes();
                cmbSafeFrom.SelectedValue = _invoic.Store;
                txtNote.Text = _invoic.InvNote;
                txtRefNo.Text = _invoic.ReffNo;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في ربط البيانات", ex);
            }
        }

        private void CLR()
        {
            try
            {
                MainClass.CLRForm(this);

                txtDate.SelectedDate = DateTime.Today;
                txtRefDate.SelectedDate = DateTime.Today;
                txtDateFrom.SelectedDate = DateTime.Today;
                txtDateTo.SelectedDate = DateTime.Today;
                txtInvTime.Text = DateTime.Now.ToString("h:mm:ss tt");

                LoadInvoiceNumber();
                Common.ApplyEditAddPermission(this, IsNew: true, FrmPermission);

                GridControl1.ItemsSource = null;
                _invoic = new InvoiceDGV();
                lblReceiveTransfer.Visibility = Visibility.Collapsed;
                ResetInvoice();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في التفريغ", ex);
            }
        }

        private bool CheckBeforeClear()
        {
            try
            {
                if ((_invoic.ISNew && GC1_RowCount > 0) || _invoic.IsUpdated)
                {
                    string msg = MainClass.Language == "ar"
                        ? "لم يتم حفظ الفاتورة، هل تريد جديد؟"
                        : "Invoice not saved. New?";

                    return DXMessageBox.Show(msg, "تنبيه",
                        MessageBoxButton.YesNo, MessageBoxImage.Question)
                        == MessageBoxResult.No;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        #region ══════════════════════════════════════════════════════════════════
        //                          ITEM SEARCH
        #endregion

        private void SearchByName()
        {
            try
            {
                string name = GC1_GetFocused("ItemName")?.ToString() ?? "";
                if (string.IsNullOrEmpty(name))
                {
                    AddNewItem();
                    return;
                }

                var ad = new SqlDataAdapter(
                    $"SELECT name, id FROM Items WHERE IS_Deleted=0 AND name=N'{name}'",
                    _conn);
                var dt = new DataTable();
                ad.Fill(dt);

                if (dt.Rows.Count > 0)
                    SearchByID(SafeInt(dt.Rows[0]["id"]), 0);
                else if (!string.IsNullOrEmpty(name))
                    SearchByCode(name, 0);
                else
                    AddNewItem();
            }
            catch
            {
                AddNewItem();
            }
        }

        private void SearchByCode(string itemCode, int itemUnit)
        {
            try
            {
                var ad = new SqlDataAdapter(
                    $"SELECT name, id FROM Items WHERE IS_Deleted=0 AND Code=N'{itemCode}'",
                    _conn);
                var dt = new DataTable();
                ad.Fill(dt);

                if (dt.Rows.Count > 0)
                    SearchByID(SafeInt(dt.Rows[0]["id"]), itemUnit);
                else if (!string.IsNullOrWhiteSpace(itemCode))
                    ReadBarcode(itemCode, true);
                else
                    AddNewItem();
            }
            catch
            {
                AddNewItem();
            }
        }

        private void SearchByID(int itemId, int unitId)
        {
            try
            {
                new ItemOper().GetItemByID(itemId, unitId, ref _invoic, false, "", "");
                ItemOper.CalcRows(ref _invoic);
                BindingDGV();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل الصنف", ex);
            }
        }

        private void AddNewItem()
        {
            try
            {
                var frm = new frmItemsSrch
                {
                    sql = "SELECT id, name, nameEN, sale_price, unit FROM Items WHERE IS_Deleted=0 ORDER BY id",
                    search = "SELECT id, name, nameEN, sale_price, unit FROM Items",
                    Itemname = "",
                    StoreId = SafeInt(cmbSafeFrom.SelectedValue)
                };

                string editVal = GC1_GetFocused("ItemName")?.ToString() ?? "";
                frm.txtSrchNm.Text = editVal;
                frm.txtSrchCode.Text = editVal;

                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.ShowDialog();

                if (frm.ISDone && frm.ItemId > 0)
                {
                    foreach (int id in frm.Itemlist)
                        SearchByID(id, 0);
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في إضافة صنف", ex);
            }
        }

        private void ReadBarcode(string barcode, bool isMulti)
        {
            try
            {
                int itemId = 0, unitId = 0;
                decimal price = 0, qty = 0;
                ItemOper.SearchForBarcode(barcode.Trim(), ref itemId,
                    ref unitId, ref price, ref qty, isMulti);

                if (itemId > 0)
                    SearchByID(itemId, unitId);
                else
                    AddNewItem();
            }
            catch
            {
                AddNewItem();
            }
        }

        #region ══════════════════════════════════════════════════════════════════
        //                      ITEM DETAILS DISPLAY
        #endregion

        private void ShowItemDetails(int itemId)
        {
            try
            {
                txtLastSalePrice.Text = "";
                txtCompetitorPrice.Text = "";
                txtCostAvrg.Text = "";
                txtStock.Text = "";
                txtItemBarcode.Text = "";

                txtRecentPurchPrice.Text = ItemOper.RecentPurchPrice(itemId).ToString("F2");

                object costV = GC1_GetFocused("ItemCost");
                object eqlV = GC1_GetFocused("UnitEquality");
                if (costV != null && eqlV != null)
                    txtCostAvrg.Text = Math.Round(
                        SafeDouble(costV) * SafeDouble(eqlV), 2).ToString("F2");

                LoadPrices(itemId);

                object stockV = GC1_GetFocused("ValiableInvertory");
                if (stockV != null)
                {
                    txtStock.Text = SafeDouble(stockV).ToString("F2");
                    txtStock.Foreground = SafeDouble(stockV) >= 0
                        ? Brushes.Green
                        : Brushes.Firebrick;
                }

                txtPerUnit.Text = GC1_GetFocused("UnitEquality")?.ToString() ?? "1";
                txtTotUnitQuan.Text = GC1_GetFocused("ItemPrimaryQnty")?.ToString() ?? "0";
                txtItemBarcode.Text = GC1_GetFocused("ItemBarcode")?.ToString() ?? "";
            }
            catch (Exception ex)
            {
                ShowError("خطأ في عرض تفاصيل الصنف", ex);
            }
        }

        private void LoadPrices(int itemId)
        {
            try
            {
                var ad = new SqlDataAdapter(
                    $"SELECT * FROM ItemPrices WHERE Itemid={itemId} ORDER BY Proc_id DESC",
                    _conn);
                var dt = new DataTable();
                ad.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    txtLastSalePrice.Text = dt.Rows[0]["low_sale_price"]?.ToString() ?? "0";
                    txtCompetitorPrice.Text = dt.Rows[0]["CompetitorPrice"]?.ToString() ?? "0";
                }
            }
            catch
            {
                // تجاهل
            }
        }

        private void ShowItemUnit(int itemId, int rowIndex)
        {
            try
            {
                // البحث عن العنصر في القائمة
                var targetItem = _invoic.InvoiceItems
                    .FirstOrDefault(it => it.ItemId == itemId && it.ItemRowIndex == rowIndex);

                if (targetItem != null)
                {
                    var frm = new frmItemUnits
                    {
                        ItemId = targetItem.ItemId,
                        PreUnit = targetItem.UnitID
                    };
                    frm.ShowDialog();
                    if (!string.IsNullOrEmpty(frm.Unitname))
                    {
                        targetItem.UnitID = frm.UnitId;
                        targetItem.UnitName = frm.Unitname;
                        ItemOper.LoadUnitInf(ref targetItem, InvType, _invoic.Pricing);
                        ItemOper.CalcRows(ref _invoic);
                        BindingDGV();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل الوحدات", ex);
            }
        }

        private void ShowItemInvertories(int itemId, int rowIndex)
        {
            try
            {
                var targetItem = _invoic.InvoiceItems
                    .FirstOrDefault(it => it.ItemId == itemId && it.ItemRowIndex == rowIndex);

                if (targetItem != null)
                {
                    var frm = new frmItemInvertory { ItemId = targetItem.ItemId };
                    frm.ShowDialog();
                    if (frm.InvertoryId > 0)
                    {
                        targetItem.InvertoryId = frm.InvertoryId;
                        targetItem.InvertoryName = frm.InvertoryName;
                        targetItem.ValiableInvertory = Convert.ToDouble(frm.ItemQty);
                        ItemOper.ISvalidQuantity(ref _invoic, targetItem.ItemId,
                            GC1_FocusedRow, true);
                        ItemOper.CalcRows(ref _invoic);
                        BindingDGV();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل المستودع", ex);
            }
        }

        #region ══════════════════════════════════════════════════════════════════
        //                        SAVE OPERATIONS
        #endregion

        private void SaveReceive()
        {
            try
            {
                if (_invoic.InvoiceStatus == 3)
                {
                    ShowMsg("تم اعتماد المناقلة من قبل");
                    return;
                }

                foreach (var it in _invoic.InvoiceItems)
                    it.InvertoryImpact = 0;

                if (GC1_RowCount == 0)
                {
                    ShowMsg("لا يوجد مناقلة لاستلامها");
                    return;
                }

                if (!Confirm("هل أنت متأكد من استلام كمية المناقلة؟"))
                    return;

                _invoic.IsPrinted = false;
                SaveAndPrint();

                if (_conn.State != ConnectionState.Open)
                    _conn.Open();

                if (Confirm("تم استلام المناقلة كمسودة، هل تريد اعتماد الاستلام؟"))
                {
                    foreach (var it in _invoic.InvoiceItems)
                        it.InvertoryImpact = 1;
                    _invoic.IsPrinted = false;
                    _invoic.InvoiceStatus = 3;
                }

                SaveAndPrint();
                CLR();
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء الاستلام", ex);
            }
            finally
            {
                if (_conn.State != ConnectionState.Closed)
                    _conn.Close();
            }
        }

        private void SaveSend()
        {
            try
            {
                if (_invoic.InvoiceStatus == 3)
                {
                    ShowMsg("تم اعتماد المناقلة من قبل");
                    return;
                }

                foreach (var it in _invoic.InvoiceItems)
                    it.InvertoryImpact = 0;

                if (cmbSafeFrom.SelectedIndex == -1)
                {
                    ShowMsg("يجب اختيار المستودع");
                    cmbSafeFrom.Focus();
                    return;
                }
                if (cmbSafeTo.SelectedIndex == -1)
                {
                    ShowMsg("يجب اختيار المستودع");
                    cmbSafeTo.Focus();
                    return;
                }
                if (cmbSafeTo.SelectedValue?.Equals(cmbSafeFrom.SelectedValue) == true)
                {
                    ShowMsg("لا يمكن التحويل لنفس المستودع");
                    cmbSafeTo.Focus();
                    return;
                }
                if (GC1_RowCount == 0)
                {
                    ShowMsg("لم تدخل أصناف للتحويل");
                    return;
                }

                if (_invoic.InvNote == null)
                    _invoic.InvNote = $"مناقلة من المستودع {cmbSafeFrom.Text} إلى {cmbSafeTo.Text} برقم {txtNo.Text}";

                if (_invoic.ISNew)
                {
                    if (!Confirm("هل أنت متأكد من حفظ المناقلة كمسودة؟"))
                        return;
                    _invoic.InvoiceStatus = 1;
                }

                var approveResult = DXMessageBox.Show(
                    "تم حفظ المناقلة كمسودة، هل تريد اعتماد المناقلة؟", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (approveResult == MessageBoxResult.Yes)
                {
                    _invoic.Branch = Common.GetInventoryBranch(SafeInt(cmbSafeFrom.SelectedValue));
                    _invoic.Store = SafeInt(cmbSafeFrom.SelectedValue);
                    foreach (var it in _invoic.InvoiceItems)
                    {
                        it.InvertoryId = SafeInt(cmbSafeFrom.SelectedValue);
                        it.InvertoryImpact = 2;
                    }
                    _invoic.InvoiceStatus = 3;
                }

                SaveAndPrint();

                if (approveResult == MessageBoxResult.Yes)
                {
                    foreach (var it in _invoic.InvoiceItems)
                    {
                        it.InvertoryImpact = 0;
                        it.InvertoryId = SafeInt(cmbSafeTo.SelectedValue);
                        it.InvertoryName = cmbSafeTo.Text;
                    }
                    _invoic.ProcType = 1;
                    _invoic.ISNew = true;
                    _invoic.Branch = Common.GetInventoryBranch(SafeInt(cmbSafeTo.SelectedValue));
                    _invoic.Store = SafeInt(cmbSafeTo.SelectedValue);
                    _invoic.ReffNo = _invoic.InvGlobalID;
                    _invoic.InvoiceStatus = 1;
                    _invoic.InvNote = $"استلام مناقلة من {cmbSafeFrom.Text} إلى {cmbSafeTo.Text} برقم {txtNo.Text}";
                    SaveAndPrint();
                }

                RecalculateCost();

                var savedMsg = new frmSavedMsg();
                if (!_invoic.ISNew)
                    savedMsg.lblSave.Text = "تم حفظ التعديلات بنجاح...";
                savedMsg.ShowDialog();

                switch (savedMsg.Pressed)
                {
                    case 1:
                        CLR();
                        break;
                    case 2:
                        _invoic.ISNew = false;
                        _invoic.IsUpdated = false;
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
            catch (Exception ex)
            {
                ShowError("خطأ أثناء الحفظ", ex);
            }
            finally
            {
                if (_conn.State != ConnectionState.Closed)
                    _conn.Close();
            }
        }

        private async void SaveAndPrint()
        {
            try
            {
                var invoiceOper = new InvoiceOper();
                var invoice = invoiceOper.MappingInvoice(ref _invoic);
                Entry entry = null;

                Inventory.UpdateItemStock(invoice);

                if (invoice.InvoiceType == InvoiceType.SafesTransferTo && invoice.ProcType == 1)
                {
                    if (invoice.InvoiceStatus != 3)
                        invoice.DistBranch = Common.GetInventoryBranch(SafeInt(cmbSafeTo.SelectedValue));
                    invoice.Branch = Common.GetInventoryBranch(SafeInt(cmbSafeTo.SelectedValue));
                }
                else if (invoice.ProcType == 2)
                    invoice.Branch = Common.GetInventoryBranch(SafeInt(cmbSafeFrom.SelectedValue));

                bool saved = invoiceOper.SaveInvoice(invoice, entry, _invoic.ISNew);

                if (saved)
                {
                    ThreadContext.Properties["EmpID"] = MainClass.EmpNo;
                    string action = _invoic.ISNew ? "تم حفظ" : "تم تعديل";
                    Logger.Info($"{action} مناقلة برقم {invoice.InvoiceNo} بواسطة: {MainClass.UserName}");
                }

                if (saved && _invoic.IsPrinted && _print.PrintNo > 0)
                {
                    invoice.safeFrom = cmbSafeFrom.Text;
                    invoice.safeTo = cmbSafeTo.Text;
                    RptPrint(invoice, 1);
                }

                if (saved && Sync.ActiveSync && _invObj.SyncInv && Sync.SyncType > 0)
                {
                    if (!_invObj.SyncEntry)
                        entry = null;
                    await invoiceOper.SyncInvoice(invoice, entry, _invoic.ISNew);
                }
                var Home = new Home();
                if (Home._is_active)
                {
                    if (ConnectBroker.IsConnectedToInternet() && ConnectBroker.CheckConnectionAndBroker())
                    {
                        string c1 = $"WHERE InvGlobalID=N'{invoice.InvGlobalID}'";
                        string c2 = $"WHERE GlobalID=N'{invoice.EntryGlobalID}'";
                        string c3 = $"WHERE EntryGlobalID=N'{invoice.EntryGlobalID}'";
                        SendData.SendDataa("Inv", invoice.InvGlobalID, SendData.GetInv(c1));
                        SendData.SendDataa("InvSub", invoice.InvGlobalID, SendData.GetInvSub(c1));
                        SendData.SendDataa("entry", invoice.EntryGlobalID, SendData.GetEntryData(c2));
                        SendData.SendDataa("entryDetails", invoice.EntryGlobalID, SendData.GetEntrySubData(c3));
                    }
                    else
                        SaveGlobalIDOffLine(_invoic.InvGlobalID, _invoic.EntryGlobalID);
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء الحفظ والطباعة", ex);
            }
        }

        private void RecalculateCost()
        {
            try
            {
                foreach (var it in _invoic.InvoiceItems.ToList())
                {
                    double avg = it.ItemCost;
                    ItemOper.RecalculateCost(it.ItemId, it.ItemPrimaryQnty, ref avg, "");
                    it.ItemCost = avg;
                }
            }
            catch
            {
                // تجاهل
            }
        }

        private void CalcDiscount()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(txtInvDiscVal.Text))
                {
                    double.TryParse(txtInvDiscVal.Text, out double disc);
                    _invoic.InvDiscount = disc;
                }
                else
                {
                    _invoic.InvDiscount = 0.0;
                    txtInvDiscPerc.Text = "0";
                }
                ItemOper.CalcRows(ref _invoic);
                BindingDGV();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في حساب الخصم", ex);
            }
        }

        private void CalcDiscountPerc()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(txtInvDiscPerc.Text))
                {
                    double.TryParse(txtInvDiscPerc.Text, out double perc);
                    _invoic.InvDiscount = Convert.ToDouble(
                        InvoiceOper.GetInvDiscountByPercentage(_invoic.SumPrice, perc));
                }
                else
                {
                    txtInvDiscPerc.Text = "0";
                    _invoic.InvDiscount = 0.0;
                }
                ItemOper.CalcRows(ref _invoic);
                BindingDGV();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في حساب نسبة الخصم", ex);
            }
        }

        private bool IsInvExist()
        {
            try
            {
                var ad = new SqlDataAdapter(
                    $"SELECT id FROM Inv WHERE branch={MainClass.BranchNo} " +
                    $"AND inv_type={InvType} AND proc_type={ProcType} AND id={txtNo.Text}",
                    _conn);
                var dt = new DataTable();
                ad.Fill(dt);
                return dt.Rows.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private void SaveGlobalIDOffLine(string invGID, string entryGID)
        {
            try
            {
                var list = new List<InvGLobalIDOff>();
                string path = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Data", "InvGlobalID.json");

                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    if (!string.IsNullOrWhiteSpace(json))
                        list = JsonConvert.DeserializeObject<List<InvGLobalIDOff>>(json)
                               ?? new List<InvGLobalIDOff>();
                }

                list.Add(new InvGLobalIDOff
                {
                    InvGlobalID = invGID,
                    GlobalID = entryGID,
                    EntryGLobalID = entryGID
                });

                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(path,
                    JsonConvert.SerializeObject(list, Formatting.Indented));
            }
            catch
            {
                // تجاهل
            }
        }

        #region ══════════════════════════════════════════════════════════════════
        //                          GRID EVENTS
        #endregion

        private void GridView1_CellValueChanged(object sender, CellValueChangedEventArgs e)
        {
            try
            {
                if (e.RowHandle < 0 || _isLoading)
                    return;

                _focusedFieldName = e.Column?.FieldName ?? "";
                string f = _focusedFieldName;

                if (f == "ItemQuantity" || f == "ItemPrice" || f == "ItemDiscount")
                {
                    if (!double.TryParse(e.Value?.ToString(), out _))
                    {
                        ShowMsg("يجب إدخال أرقام فقط");
                        GC1_SetCell(e.RowHandle, f, 0);
                        return;
                    }

                    ItemOper.CalcRows(ref _invoic);
                    BindingDGV();

                    if (f == "ItemQuantity")
                        ItemOper.checkQtyLimi(ref _invoic, SafeInt(GC1_GetFocused("ItemId")));
                }

                if (f == "ItemDiscountPerc")
                {
                    object percV = GC1_GetCell(e.RowHandle, "ItemDiscountPerc");
                    object sumV = GC1_GetCell(e.RowHandle, "ItemSumPrice");
                    if (percV != null && sumV != null)
                    {
                        double disc = SafeDouble(percV) / 100.0 * SafeDouble(sumV);
                        GC1_SetCell(e.RowHandle, "ItemDiscount", disc);
                        ItemOper.CalcRows(ref _invoic);
                        BindingDGV();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحديث الخلية", ex);
            }
        }

        private void GridView1_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                _focusedFieldName = GC1_FocusedColumnName;

                if (GC1_RowCount <= 0)
                    return;

                if (e.Key == Key.Delete)
                {
                    if (!Confirm("هل أنت متأكد من الحذف؟"))
                        return;
                    GC1_DeleteRow(GC1_FocusedRow);
                    int idx = 1;
                    foreach (var it in _invoic.InvoiceItems)
                        it.ItemRowIndex = idx++;
                    ItemOper.CalcRows(ref _invoic);
                    BindingDGV();
                    return;
                }

                if (e.Key == Key.Return)
                {
                    switch (_focusedFieldName)
                    {
                        case "ItemCode":
                            SearchByCode(GC1_GetFocused("ItemCode")?.ToString() ?? "", 0);
                            e.Handled = true;
                            break;
                        case "ItemName":
                            SearchByName();
                            e.Handled = true;
                            break;
                        case "UnitName":
                            ShowItemUnit(SafeInt(GC1_GetFocused("ItemId")), GC1_FocusedRow + 1);
                            e.Handled = true;
                            break;
                        case "InvertoryName":
                            ShowItemInvertories(SafeInt(GC1_GetFocused("ItemId")), GC1_FocusedRow + 1);
                            e.Handled = true;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في معالجة المفاتيح", ex);
            }
        }

        private void GridView1_FocusedRowChanged(object sender, FocusedRowChangedEventArgs e)
        {
            try
            {
                // تحقق من وجود صفوف
                if (GridView1 == null || GridView1.FocusedRowHandle < 0)
                    return;

                // الحصول على قيمة ItemId من الصف المركز
                var itemIdValue = GridControl1.GetCellValue(GridView1.FocusedRowHandle, "ItemId");

                if (itemIdValue != null)
                {
                    int itemId = SafeInt(itemIdValue);
                    if (itemId > 0)
                        ShowItemDetails(itemId);
                }
            }
            catch (Exception ex)
            {
                // للتشخيص فقط (يمكن حذفها في الإنتاج)
                System.Diagnostics.Debug.WriteLine($"GridView1_FocusedRowChanged Error: {ex.Message}");
            }
        }

        private void ItemSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            AddNewItem();
        }

        private void UnitChangeBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is InvoiceItem row)
                    ShowItemUnit(row.ItemId, row.ItemRowIndex);
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تغيير الوحدة", ex);
            }
        }

        private void InvertoryChangeBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is InvoiceItem row)
                    ShowItemInvertories(row.ItemId, row.ItemRowIndex);
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تغيير المستودع", ex);
            }
        }

        private void ItemNameEdit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                SearchByName();
        }

        #region ══════════════════════════════════════════════════════════════════
        //                              NUMPAD
        #endregion

        private void NumPad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button btn)
                    return;
                string digit = btn.Tag?.ToString() ?? "";
                if (string.IsNullOrEmpty(_focusedFieldName) || GC1_FocusedRow < 0)
                    return;

                string cur = (GC1_GetFocused(_focusedFieldName) ?? "0").ToString();
                cur = (cur == "0" && digit != ".") ? digit : cur + digit;
                if (!double.TryParse(cur, out double val))
                    return;

                GC1_SetFocused(_focusedFieldName, val);
                ItemOper.CalcRows(ref _invoic);
                BindingDGV();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في NumPad", ex);
            }
        }

        private void cmdClearAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_focusedFieldName) || GC1_FocusedRow < 0)
                    return;
                GC1_SetFocused(_focusedFieldName, 0.0);
                ItemOper.CalcRows(ref _invoic);
                BindingDGV();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في المسح", ex);
            }
        }

        #region ══════════════════════════════════════════════════════════════════
        //                          CONTEXT MENU
        #endregion

        private void SetupContextMenu()
        {
            try
            {
                var menu = new ContextMenu();
                menu.Items.Add(MkMenu("📋 استعراض المادة", ShowItemCard));
                menu.Items.Add(MkMenu("➕ إضافة مادة", AddNewProduct));
                menu.Items.Add(new Separator());
                menu.Items.Add(MkMenu("🔄 الوحدات", () => ShowItemUnit(SafeInt(GC1_GetFocused("ItemId")), GC1_FocusedRow + 1)));
                menu.Items.Add(MkMenu("🏪 مخزون الصنف", () => ShowItemInvertories(SafeInt(GC1_GetFocused("ItemId")), GC1_FocusedRow + 1)));
                menu.Items.Add(new Separator());
                menu.Items.Add(MkMenu("🗑️ حذف سجل", DeleteCurrentRow));
                menu.Items.Add(new Separator());
                menu.Items.Add(MkMenu("📊 حركة المادة", ItemProcess));
                menu.Items.Add(MkMenu("📈 آخر حركة", ItemLastActivity));
                menu.Items.Add(MkMenu("💰 تكلفة المادة", ItemCost1));
                menu.Items.Add(MkMenu("🔢 رقم تسلسلي", ItemSerialNo));
                GridControl1.ContextMenu = menu;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في إنشاء القائمة", ex);
            }
        }

        private MenuItem MkMenu(string header, Action act)
        {
            var mi = new MenuItem { Header = header };
            mi.Click += (s, e) => act();
            return mi;
        }

        private void ShowItemCard()
        {
            try
            {
                var frm = new frmItems();
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.ItemId = SafeInt(GC1_GetFocused("ItemId"));
                frm.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في عرض المادة", ex);
            }
        }

        private void AddNewProduct()
        {
            try
            {
                var frm = new frmItems();
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في إضافة مادة", ex);
            }
        }

        private void DeleteCurrentRow()
        {
            try
            {
                if (GC1_RowCount <= 0)
                    return;
                if (!Confirm("هل أنت متأكد من الحذف؟"))
                    return;
                GC1_DeleteRow(GC1_FocusedRow);
                int idx = 1;
                foreach (var it in _invoic.InvoiceItems)
                    it.ItemRowIndex = idx++;
                ItemOper.CalcRows(ref _invoic);
                BindingDGV();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في الحذف", ex);
            }
        }

        private void ItemProcess()
        {
            try
            {
                var frm = new frmRptItemsActivityDetailed
                {
                    SelectedId = SafeInt(GC1_GetFocused("ItemId"))
                };
                frm.txtItemName.Text = GC1_GetFocused("ItemName")?.ToString() ?? "";
                frm.txtItemCode.Text = GC1_GetFocused("ItemCode")?.ToString() ?? "";
                frm.ShowResult();
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في حركة المادة", ex);
            }
        }

        private void ItemLastActivity()
        {
            try
            {
                var frm = new frmRptItemsActivityDetailed();
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.txtItemName.Text = GC1_GetFocused("ItemName")?.ToString() ?? "";
                frm.txtItemCode.Text = GC1_GetFocused("ItemCode")?.ToString() ?? "";
                frm.Show();
                frm.LastProcess(SafeInt(GC1_GetFocused("ItemId")),
                    SafeInt(GC1_GetFocused("InvertoryId")), -1, 1);
            }
            catch (Exception ex)
            {
                ShowError("خطأ في آخر حركة", ex);
            }
        }

        private void ItemCost1()
        {
            try
            {
                DXMessageBox.Show(
                    ItemOper.AvgCost(SafeInt(GC1_GetFocused("ItemId")),
                        MainClass.BranchNo).ToString("F2"),
                    "تكلفة المادة", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError("خطأ في التكلفة", ex);
            }
        }

        private void ItemSerialNo()
        {
            try
            {
                var frm = new frmItemSerialNo();
                int itemId = SafeInt(GC1_GetFocused("ItemId"));
                frm.lblItemName.Text = GC1_GetFocused("ItemName")?.ToString() ?? "";
                frm.PanelInput.Visibility = Visibility.Collapsed;
                frm.PnlSelled.Visibility = Visibility.Visible;
                frm.AvailableNo.Visibility = Visibility.Visible;
                frm.operType = 3;
                frm.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في الرقم التسلسلي", ex);
            }
        }

        #region ══════════════════════════════════════════════════════════════════
        //                          BUTTON EVENTS
        #endregion

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                CLR();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Common.CheckProiedAcc(txtDate.SelectedDate ?? DateTime.Today))
                {
                    ShowMsg("لا يمكن أن يكون التاريخ خارج الفترة المحاسبية");
                    return;
                }

                if (_invoic.ProcType == 1)
                    SaveReceive();
                else if (_invoic.ProcType == 2)
                    SaveSend();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في الحفظ", ex);
            }
        }

        private void btnSavePrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_invoic.ISNew && _invoic.ProcType == 2)
                    SaveReceive();
                else if (TabControl1.SelectedIndex == 0)
                {
                    _invoic.IsPrinted = true;
                    SaveAndPrint();
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في الحفظ والطباعة", ex);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_invoic.ISNew && InvoiceOper.DeleteInvoice(_invoic))
                {
                    Logger.Info($"تم حذف مناقلة برقم {_invoic.InvoiceNo} بواسطة {MainClass.UserName}");
                    CLR();
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في الحذف", ex);
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_invoic.IsPrinted)
                {
                    var inv = new InvoiceOper().MappingInvoice(ref _invoic);
                    inv.safeFrom = cmbSafeFrom.Text;
                    inv.safeTo = cmbSafeTo.Text;
                    RptPrint(inv, 1);
                }
                else
                    ShowMsg("لا يمكن طباعة الفاتورة قبل الحفظ");
            }
            catch (Exception ex)
            {
                ShowError("خطأ في الطباعة", ex);
            }
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var inv = new InvoiceOper().MappingInvoice(ref _invoic);
                inv.safeFrom = cmbSafeFrom.Text;
                inv.safeTo = cmbSafeTo.Text;
                RptPrint(inv, 2);
            }
            catch (Exception ex)
            {
                ShowError("خطأ في المعاينة", ex);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                Navigate($"SELECT TOP 1 * FROM Inv WHERE inv_type={InvType} AND proc_type={ProcType} AND IS_Deleted=0 AND branch={MainClass.BranchNo} ORDER BY id ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                Navigate($"SELECT TOP 1 * FROM Inv WHERE inv_type={InvType} AND proc_type={ProcType} AND IS_Deleted=0 AND branch={MainClass.BranchNo} ORDER BY id DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                Navigate($"SELECT TOP 1 * FROM Inv WHERE inv_type={InvType} AND proc_type={ProcType} AND IS_Deleted=0 AND id>{txtNo.Text} AND branch={MainClass.BranchNo} ORDER BY id ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckBeforeClear())
                Navigate($"SELECT TOP 1 * FROM Inv WHERE inv_type={InvType} AND proc_type={ProcType} AND IS_Deleted=0 AND id<{txtNo.Text} AND branch={MainClass.BranchNo} ORDER BY id DESC");
        }

        private void btnInvSrch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var frm = new frmInvoiceSrch { ProcType = 2, InvType = 8 };
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.ShowDialog();
                if (frm.ISDone && frm.InvGlobalID != "-1")
                    Navigate($"SELECT * FROM Inv WHERE branch={MainClass.BranchNo} AND InvGlobalID=N'{frm.InvGlobalID}'");
            }
            catch (Exception ex)
            {
                ShowError("خطأ في البحث", ex);
            }
        }

        private void btnReceiveQuantity_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var frm = new frmInvoiceSrch();
                frm.cmbProcType.IsEnabled = false;
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.ProcType = 1;
                frm.InvType = 8;
                frm.Cond += " AND InvoiceStatus = 1";
                frm.ShowDialog();

                if (frm.ISDone && frm.InvGlobalID != "-1")
                    Navigate($"SELECT * FROM Inv WHERE branch={MainClass.BranchNo} AND InvGlobalID=N'{frm.InvGlobalID}'");

                _invoic.ProcType = 1;
                ProcType = 1;
                LoadInvoiceNumber();
                if (ProcType == 1)
                    lblReceiveTransfer.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في استلام المناقلة", ex);
            }
        }

        private void BtnInvertoryOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var frm = new frmInvoiceSrch();
                frm.cmbProcType.IsEnabled = false;
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.ProcType = 1;
                frm.InvType = 14;
                frm.ShowDialog();

                if (frm.ISDone && frm.InvGlobalID != "-1")
                    Navigate($"SELECT * FROM Inv WHERE InvGlobalID=N'{frm.InvGlobalID}'");

                _invoic.InvoiceType = InvoiceType.SafesTransferTo;
                _invoic.ProcType = 2;
                LoadInvoiceNumber();
                _invoic.ISNew = true;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في إدراج طلب بضاعة", ex);
            }
        }

        private void btnInsertPurch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var frm = new frmInvoiceSrch();
                frm.cmbProcType.IsEnabled = false;
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.ProcType = 1;
                frm.InvType = 1;
                frm.ShowDialog();

                if (frm.ISDone && frm.InvGlobalID != "-1")
                    Navigate($"SELECT * FROM Inv WHERE InvGlobalID=N'{frm.InvGlobalID}'");

                _invoic.InvoiceType = InvoiceType.SafesTransferTo;
                _invoic.ProcType = 2;
                LoadInvoiceNumber();
                _invoic.ISNew = true;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في إدراج مشتريات", ex);
            }
        }

        private void stripImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Confirm("هل أنت متأكد من الاستيراد؟"))
                    return;

                var frm = new frmImportDataGeneral();
                frm.cmbInv.SelectedIndex = 1;
                frm.cmbInv.Visibility = Visibility.Visible;
                frm.cmbInv.IsEnabled = false;
                frm.cmbDataTable.Visibility = Visibility.Collapsed;
                frm.ShowDialog();

                if (frm.ISDone && frm.dtInvSel.Rows.Count > 0)
                {
                    foreach (DataRow row in frm.dtInvSel.Rows)
                    {
                        SearchByCode(row["ItemCode"]?.ToString() ?? "", SafeInt(row["unit"]));
                        foreach (var it in _invoic.InvoiceItems.ToList())
                        {
                            if (it.ItemCode == row["ItemCode"]?.ToString() &&
                                it.ItemRowIndex == _invoic.InvoiceItems.Count)
                            {
                                it.Description = row["description"]?.ToString() ?? "";
                                it.ItemQuantity = SafeDouble(row["quntity"]);
                                if (SafeDouble(row["price"]) > 0)
                                    it.ItemPrice = SafeDouble(row["price"]);
                                ItemOper.CalcRows(ref _invoic);
                                BindingDGV();
                            }
                        }
                    }
                }
                ShowMsg("تم الاستيراد بنجاح");
            }
            catch (Exception ex)
            {
                ShowError("خطأ في الاستيراد", ex);
            }
        }

        private void stripExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsInvExist())
                {
                    ShowMsg("يجب حفظ الفاتورة قبل التصدير");
                    return;
                }
                if (!Confirm("هل أنت متأكد من التصدير؟"))
                    return;

                var dlg = new SaveFileDialog { Filter = "Excel Files (*.xlsx)|*.xlsx" };
                if (dlg.ShowDialog() != true)
                    return;

                (GridControl1.View as TableView)?.ExportToXlsx(dlg.FileName);
                Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                ShowMsg($"تم حفظ الملف في {dlg.FileName}");
            }
            catch (Exception ex)
            {
                ShowError("خطأ في التصدير", ex);
            }
        }

        private void StripImportInv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var frm = new frmImportInvoice();
                frm.ShowDialog();
                if (frm.isDone && frm.InvGID != "-1")
                    Navigate($"SELECT * FROM Inv WHERE InvGlobalID=N'{frm.InvGID}'");
                _invoic.InvoiceType = (InvoiceType)InvType;
                _invoic.ProcType = ProcType;
                _invoic.InvNote = "";
                LoadInvoiceNumber();
                _invoic.ISNew = true;
                BindingControls();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في إدراج الفاتورة", ex);
            }
        }

        #region ══════════════════════════════════════════════════════════════════
        //                        SEARCH TAB EVENTS
        #endregion

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SearchInvoices();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في البحث", ex);
            }
        }

        private void SearchInvoices()
        {
            try
            {
                string conds = "";
                if (!string.IsNullOrWhiteSpace(txtSrchNo.Text))
                    conds += $" AND Inv.id={SafeInt(txtSrchNo.Text.Trim())}";
                if (!string.IsNullOrWhiteSpace(txtSrchreffNo.Text))
                    conds += $" AND Inv.Reff_No=N'{txtSrchreffNo.Text.Trim()}'";

                var ad = new SqlDataAdapter(
                    $"SELECT Inv.InvGlobalID AS GlobalID, Inv.id AS InvNo, Inv.Reff_No AS RefNo, " +
                    $"CONVERT(VARCHAR,Inv.date,103) AS InvDate, Customers.name AS Client " +
                    $"FROM Inv LEFT JOIN Customers ON Inv.cust_id=Customers.id " +
                    $"WHERE Inv.inv_type={InvType} AND Inv.proc_type={ProcType} AND Inv.IS_Deleted=0 " +
                    $"AND Inv.branch={MainClass.BranchNo}{conds} ORDER BY Inv.id DESC", _conn);
                var dt = new DataTable();
                ad.Fill(dt);
                dgvSrch.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في البحث", ex);
            }
        }

        private void dgvSrch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (dgvSrch.SelectedItem is not DataRowView row)
                    return;
                string gid = row["GlobalID"]?.ToString() ?? "";
                if (string.IsNullOrEmpty(gid))
                    return;
                Navigate($"SELECT * FROM Inv WHERE branch={MainClass.BranchNo} AND InvGlobalID=N'{gid}'");
                TabControl1.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في التحديد", ex);
            }
        }

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbClientSrch.IsEnabled = chkAll.IsChecked != true;
            if (chkAll.IsChecked == true)
                cmbClientSrch.SelectedIndex = -1;
        }

        #region ══════════════════════════════════════════════════════════════════
        //                    RECEIVE TRANSFER TAB
        #endregion

        private void LoadMoneyTransferData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadTransferData();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل المناقلات", ex);
            }
        }

        private void LoadTransferData()
        {
            try
            {
                var sb = new StringBuilder();
                var pars = new List<SqlParameter>();

                sb.AppendLine("SELECT i.InvGlobalID, i.id, i.Reff_No, i.date AS InvDate,");
                sb.AppendLine("fromBranch.name AS branchFromName, toBranch.name AS toBranchName,");
                sb.AppendLine("fromSafe.name AS safeFromName, toSafe.name AS safeName,");
                sb.AppendLine("i.invoiceStatus, i.notes, sourceInv.branch AS branchFrom,");
                sb.AppendLine(MainClass.Language == "en-us"
                    ? "CASE WHEN i.InvoiceStatus=3 THEN 'Received' ELSE 'Draft' END AS InvStatus"
                    : "CASE WHEN i.InvoiceStatus=3 THEN N'مستلم' ELSE N'مسودة' END AS InvStatus");
                sb.AppendLine("FROM Inv AS i");
                sb.AppendLine("LEFT JOIN Inv AS sourceInv ON sourceInv.InvGlobalID=i.Reff_No AND sourceInv.inv_type=8 AND sourceInv.proc_type=2");
                sb.AppendLine("LEFT JOIN Branches AS fromBranch ON fromBranch.id=sourceInv.branch");
                sb.AppendLine("LEFT JOIN Branches AS toBranch ON toBranch.id=i.branch");
                sb.AppendLine("LEFT JOIN Safes AS fromSafe ON fromSafe.id=sourceInv.safe");
                sb.AppendLine("LEFT JOIN Safes AS toSafe ON toSafe.id=i.safe");
                sb.AppendLine("WHERE i.IS_Deleted=0 AND i.inv_type=8 AND i.proc_type=1 AND i.branch=@branchNo");
                pars.Add(new SqlParameter("@branchNo", MainClass.BranchNo));

                if (chkAllSafes.IsChecked != true && cmbSearchSafe.SelectedValue != null)
                {
                    sb.AppendLine("AND sourceInv.safe=@safeId");
                    pars.Add(new SqlParameter("@safeId", cmbSearchSafe.SelectedValue));
                }
                if (ckTotalPeriod.IsChecked != true)
                {
                    sb.AppendLine("AND (i.date>=@d1 AND i.date<@d2)");
                    pars.Add(new SqlParameter("@d1", (txtDateFrom.SelectedDate ?? DateTime.Today).Date));
                    pars.Add(new SqlParameter("@d2", (txtDateTo.SelectedDate ?? DateTime.Today).Date.AddDays(1)));
                }

                using var conn = new SqlConnection(MainClass.connstr);
                conn.Open();
                using var cmd = new SqlCommand(sb.ToString(), conn);
                foreach (var p in pars)
                    cmd.Parameters.Add(p);
                using var ad = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                ad.Fill(dt);

                if (dt.Rows.Count > 0)
                    GridControl2.ItemsSource = dt.DefaultView;
                else
                    ShowMsg("لا يوجد نتائج");
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل البيانات", ex);
            }
        }

        private void RItemBtnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string gid = GC2_GetFocused("InvGlobalID")?.ToString() ?? "";
                if (string.IsNullOrEmpty(gid))
                    return;
                Navigate($"SELECT * FROM Inv WHERE branch={MainClass.BranchNo} AND InvGlobalID=N'{gid}'");
                TabControl2.SelectedIndex = 0;
                if (GC2_GetFocused("invoiceStatus")?.ToString() == "1")
                {
                    ProcType = _invoic.ProcType;
                    LoadInvoiceNumber();
                }
                lblReceiveTransfer.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في العرض", ex);
            }
        }

        private void ckTotalPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            bool isAll = ckTotalPeriod.IsChecked == true;
            txtDateFrom.IsEnabled = !isAll;
            txtDateTo.IsEnabled = !isAll;
        }

        private void chkAllSafes_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            cmbSearchSafe.IsEnabled = chkAllSafes.IsChecked != true;
            if (!IsLoaded) return;
        }


        private void chkAllTrans_CheckedChanged(object sender, RoutedEventArgs e)
            => txtTransferNo.IsEnabled = chkAllTrans.IsChecked != true;

        #region ══════════════════════════════════════════════════════════════════
        //                      TABCONTROL EVENTS
        #endregion

        private void TabControl2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // يمكن إضافة أكواد عند تغيير التبويب الرئيسي
        }

        private void TabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // يمكن إضافة أكواد عند تغيير التبويب الداخلي
        }

        #region ══════════════════════════════════════════════════════════════════
        //                        FIELD EVENTS
        #endregion

        private void txtDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_invoic != null && !_isLoading)
                _invoic.InvDate = txtDate.SelectedDate ?? DateTime.Today;
        }

        private void txtRefDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_invoic != null && !_isLoading)
                _invoic.RefDate = txtRefDate.SelectedDate ?? DateTime.Today;
        }

        private void txtRefNo_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_invoic != null && !_isLoading)
                _invoic.ReffNo = txtRefNo.Text;
        }

        private void txtNote_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_invoic != null && !_isLoading)
                _invoic.InvNote = txtNote.Text;
        }

        private void txtInvDiscVal_LostFocus(object sender, RoutedEventArgs e) => CalcDiscount();

        private void txtInvDiscVal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                CalcDiscount();
        }

        private void txtInvDiscPerc_LostFocus(object sender, RoutedEventArgs e) => CalcDiscountPerc();

        private void txtInvDiscPerc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                CalcDiscountPerc();
        }

        private void cmbSafeFrom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_invoic == null || _isLoading)
                    return;
                if (cmbSafeFrom.SelectedIndex > -1)
                {
                    _invoic.Store = SafeInt(cmbSafeFrom.SelectedValue);
                    _invoic.InvertoryName = Common.GetStoreName(SafeInt(cmbSafeFrom.SelectedValue));
                }
                else
                    _invoic.Store = -1;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في اختيار المستودع", ex);
            }
        }

        #region ══════════════════════════════════════════════════════════════════
        //                              PRINT
        #endregion

        private void RptPrint(Invoice inv, int type)
        {
            try
            {
                if (GC1_RowCount == 0)
                {
                    ShowMsg("لا توجد عمليات بالجدول");
                    return;
                }
                if (string.IsNullOrEmpty(_print.RptUrl))
                {
                    ShowMsg("يجب تحديد مسار التقرير");
                    return;
                }

                _print.RptName = inv.ProcType == 1
                    ? "rptInventoryTransferReceive.repx"
                    : "rptInventoryTransfer.repx";

                string path = Path.Combine(_print.RptUrl, _print.RptName);
                if (!File.Exists(path))
                {
                    ShowMsg("مسار التقارير غير موجود");
                    return;
                }

                if (string.IsNullOrEmpty(_print.defPrinter))
                    _print.defPrinter = MainClass.ReportsPrinter;

                _print.Printing(type, _print.BindToData(inv), _print.RptUrl,
                    _print.RptName, _print.defPrinter, _print.kitchenprinter, _print.PrintNo);
            }
            catch (Exception ex)
            {
                ShowError("خطأ في الطباعة", ex);
            }
        }

        #region ══════════════════════════════════════════════════════════════════
        //                    MISSING METHODS - الدوال الناقصة
        #endregion

        /// <summary>
        /// اختصار إدخال الفاتورة بسرعة - بديل btnShortCutInv_Click
        /// الكود الأصلي: يفتح FrmShortCutInv لإدخال صافي الفاتورة مباشرة
        /// </summary>
        private void btnShortCutInv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var frm = new FrmShortCutInv
                {
                    InvVAT = _invObj.VAT,
                    InvIncluVAT = _invObj.PriceIncVAT
                };
                frm.ShowDialog();

                double.TryParse(frm.txtNet.Text, out double netVal);
                if (netVal > 0.0)
                {
                    double.TryParse(frm.txtSumVal.Text, out double sumVal);
                    double.TryParse(frm.txtTotDiscount.Text, out double totDisc);
                    double.TryParse(frm.txtVAT.Text, out double vat);
                    double.TryParse(frm.txtTotal.Text, out double total);

                    _invoic.SumPrice = sumVal;
                    _invoic.TotDiscount = totDisc;
                    _invoic.VAT = vat;
                    _invoic.Total = total;
                    _invoic.Net = netVal;

                    BindingDGV();
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في الاختصار السريع", ex);
            }
        }

        /// <summary>
        /// تفعيل خيار الضريبة صفر - بديل ckZeroVAT_CheckedChanged (Checked)
        /// الكود الأصلي: يضع الـ VAT = 0 ويعيد الحساب
        /// </summary>
        private void ckZeroVAT_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_invoic == null || _isLoading)
                    return;

                _invoic.VAT = 0.0;
                ItemOper.CalcRows(ref _invoic);
                BindingDGV();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تطبيق ضريبة الصفر", ex);
            }
        }

        /// <summary>
        /// إلغاء خيار الضريبة صفر - بديل ckZeroVAT_CheckedChanged (Unchecked)
        /// الكود الأصلي: يعيد الـ VAT للقيمة الافتراضية من _invObj
        /// </summary>
        private void ckZeroVAT_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_invoic == null || _isLoading)
                    return;

                _invoic.VAT = _invObj.VAT;
                ItemOper.CalcRows(ref _invoic);
                BindingDGV();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في إلغاء ضريبة الصفر", ex);
            }
        }

        /// <summary>
        /// حفظ إعدادات عرض الجدول - بديل BtnSaveDgvSettings_Click
        /// الكود الأصلي: يحفظ Layout الجدول في XML ويُظهر رسالة نجاح
        /// </summary>
        private void BtnSaveDgvSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string dir = Path.GetDirectoryName(_styleFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                GridControl1.SaveLayoutToXml(_styleFile);
                ShowMsg("تم حفظ مظهر الجدول");
            }
            catch (Exception ex)
            {
                ShowError("خطأ في حفظ إعدادات الجدول", ex);
            }
        }

        #region ══════════════════════════════════════════════════════════════════
        //                            HELPERS
        #endregion

        private static int SafeInt(object v, int d = 0)
        {
            if (v == null || v == DBNull.Value)
                return d;
            return int.TryParse(v.ToString(), out int r) ? r : d;
        }

        private static double SafeDouble(object v, double d = 0.0)
        {
            if (v == null || v == DBNull.Value)
                return d;
            return double.TryParse(v.ToString(), out double r) ? r : d;
        }

        private void ShowError(string title, Exception ex)
            => DXMessageBox.Show($"{title}\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);

        private void ShowMsg(string msg)
            => DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Information);

        private bool Confirm(string msg)
            => DXMessageBox.Show(msg, "", MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes;
    }
}