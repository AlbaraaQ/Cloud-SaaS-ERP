using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AuditorAPI.Models.DGVmodels;
using DevExpress.Xpf.Core;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmItemSerialNo : DevExpress.Xpf.Core.ThemedWindow
    {
        // ══════════════════════════════════════════════════════════
        #region Public Fields

        public double TotQty;
        public string cond;
        public List<int> ItemSerialNolist;
        public bool ShowSeerialNo;
        public int ItemId;
        public bool ISDone;
        public int operType;
        public InvoiceItem InvItem;
        public InvoiceDGV Invo;

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Private Fields

        private readonly SqlConnection _conn;
        private bool _isEdit;

        /// <summary>
        /// مصدر بيانات الشبكة اليسرى — الأرقام المتاحة
        /// </summary>
        private ObservableCollection<InvoiceItemDetailRow> _availableItems;

        /// <summary>
        /// مصدر بيانات الشبكة اليمنى — الأرقام المباعة / المنقولة
        /// </summary>
        private ObservableCollection<SelledSerialRow> _selledItems;

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Constructor

        public frmItemSerialNo()
        {
            InitializeComponent();

            // تهيئة الحقول العامة
            TotQty = 0.0;
            _conn = MainClass.ConnObj();
            cond = string.Empty;
            ItemSerialNolist = new List<int>();
            ShowSeerialNo = false;
            ItemId = -1;
            ISDone = false;
            operType = 0;
            InvItem = new InvoiceItem();
            Invo = new InvoiceDGV();
            _isEdit = false;

            // تهيئة مصادر البيانات
            _availableItems = new ObservableCollection<InvoiceItemDetailRow>();
            _selledItems = new ObservableCollection<SelledSerialRow>();

            // ربط الشبكتين بمصادر البيانات
            GridControl1.ItemsSource = _availableItems;
            GridControl2.ItemsSource = _selledItems;
        }

        public void LoadDgvEdit(DataTable Dt)
        {
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtQty.Text = "1";
            lblItemName.Text = InvItem?.ItemName ?? "الرقم التسلسلي";

            // تزامن InvItem مع ObservableCollection
            SyncFromInvItemDetails();

            bool isSalesMode = IsSalesMode();

            if (isSalesMode)
            {
                // تحميل بيانات المخزون إذا لم يكن هناك فاتورة مرجعية
                if (operType == 0 && InvItem.InvGlobalID == null)
                {
                    LoadItemDetails();
                }

                // إخفاء لوحة الإدخال في وضع المبيعات
                PanelInput.Visibility = Visibility.Collapsed;

                // تغيير نص زر الجديد ليصبح إغلاق
                btnNew.Content = MainClass.Language == "ar"
                    ? "🚪 إغلاق"
                    : "🚪 Close";

                // إظهار لوحة الأرقام المباعة وأزرار النقل
                SetSelledPanelVisible(true);
            }

            UpdateStatusBar();
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Header Events

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (_availableItems.Count <= 1)
            {
                InvItem.InvoiceItemDetails.Clear();
            }

            Close();
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Input Panel Events

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            GenerateSerialNo();
            txtSerialNo.Text = string.Empty;
            txtSerialNo.Focus();
        }

        private void txtSerialNo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                GenerateSerialNo();
                txtSerialNo.Text = string.Empty;
            }
        }

        private void txtQty_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // قبول الأرقام الصحيحة فقط
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Footer Events

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            if (IsSalesMode())
            {
                Close();
            }
            else
            {
                ClearAll();
            }
        }

        private void BtnInsert_Click(object sender, RoutedEventArgs e)
        {
            if (IsSalesMode())
            {
                if (CheckDuplicates())
                    return;

                InsertSerialNo();
            }

            ISDone = true;
            Close();
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region GridControl1 (Available) Events

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn)
                return;

            if (btn.Tag is not InvoiceItemDetailRow row)
                return;

            // التحقق من وجود مبيعات مرتبطة بهذا الرقم
            if (IsSerialLinkedToSales(row.ItemSerialNo))
            {
                ShowMessage(
                    ar: "لا يمكن حذف صنف تم بيعه مسبقاً",
                    en: "Cannot delete an item that has been sold.",
                    icon: MessageBoxImage.Warning);
                return;
            }

            var confirmResult = DXMessageBox.Show(
                MainClass.Language == "ar"
                    ? "هل أنت متأكد من الحذف؟"
                    : "Are you sure you want to delete?",
                MainClass.Language == "ar"
                    ? "تأكيد الحذف"
                    : "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmResult == MessageBoxResult.Yes)
            {
                InvItem.InvoiceItemDetails.Remove(row.Detail);
                _availableItems.Remove(row);
                RefreshAvailableRowNumbers();
                UpdateStatusBar();
            }
        }

        private void GridControl1_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return)
                return;

            // إذا كان المستخدم يكتب في صف جديد — يُتعامل معه عبر GenerateSerialNo
            // هذا الحدث محجوز للتوسع المستقبلي
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region GridControl2 (Selled) Events

        private void GridControl2_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!IsSalesMode())
                return;

            if (CheckDuplicates())
                return;

            InsertSerialNoFromFocusedRow();
            ISDone = true;
            Close();
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Transfer Arrow Button Events

        private void BtnShift_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (GridControl1.SelectedItem is not InvoiceItemDetailRow selectedRow)
                    return;

                // نقل العنصر المحدد من المتاح إلى المباع
                _selledItems.Add(new SelledSerialRow
                {
                    DgvNo1 = _selledItems.Count + 1,
                    DgvSerialNo1 = selectedRow.ItemSerialNo
                });

                InvItem.InvoiceItemDetails.Remove(selectedRow.Detail);
                _availableItems.Remove(selectedRow);

                RefreshAvailableRowNumbers();
                RefreshSelledRowNumbers();
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }

        private void BtnShiftAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_availableItems.Count == 0)
                    return;

                // نقل جميع العناصر من المتاح إلى المباع
                var allAvailable = _availableItems.ToList();

                foreach (var row in allAvailable)
                {
                    _selledItems.Add(new SelledSerialRow
                    {
                        DgvNo1 = _selledItems.Count + 1,
                        DgvSerialNo1 = row.ItemSerialNo
                    });

                    InvItem.InvoiceItemDetails.Remove(row.Detail);
                }

                _availableItems.Clear();

                RefreshSelledRowNumbers();
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }

        private void BtnReturnt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (GridControl2.SelectedItem is not SelledSerialRow selectedSelled)
                    return;

                // إرجاع العنصر المحدد من المباع إلى المتاح
                var restoredDetail = new InvoiceItemDetail
                {
                    ItemSerialNo = selectedSelled.DgvSerialNo1,
                    ItemQuantity = 1.0,
                    ItemId = InvItem.ItemId
                };

                InvItem.InvoiceItemDetails.Add(restoredDetail);

                _availableItems.Add(
                    new InvoiceItemDetailRow(restoredDetail, _availableItems.Count + 1));

                _selledItems.Remove(selectedSelled);

                RefreshAvailableRowNumbers();
                RefreshSelledRowNumbers();
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }

        private void BtnReturntAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selledItems.Count == 0)
                    return;

                // إرجاع جميع العناصر من المباع إلى المتاح
                var allSelled = _selledItems.ToList();

                foreach (var selled in allSelled)
                {
                    var restoredDetail = new InvoiceItemDetail
                    {
                        ItemSerialNo = selled.DgvSerialNo1,
                        ItemQuantity = 1.0,
                        ItemId = InvItem.ItemId
                    };

                    InvItem.InvoiceItemDetails.Add(restoredDetail);

                    _availableItems.Add(
                        new InvoiceItemDetailRow(restoredDetail, _availableItems.Count + 1));
                }

                _selledItems.Clear();

                RefreshAvailableRowNumbers();
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Data Loading

        /// <summary>
        /// تزامن InvItem.InvoiceItemDetails مع ObservableCollection
        /// </summary>
        private void SyncFromInvItemDetails()
        {
            _availableItems.Clear();

            int rowIndex = 1;
            foreach (var detail in InvItem.InvoiceItemDetails)
            {
                _availableItems.Add(new InvoiceItemDetailRow(detail, rowIndex++));
            }
        }

        /// <summary>
        /// تحميل الأرقام التسلسلية المتاحة في المخزون من قاعدة البيانات
        /// </summary>
        private void LoadItemDetails()
        {
            try
            {
                InvItem.InvoiceItemDetails.Clear();
                _availableItems.Clear();

                // استعلام رصيد المخزون لكل رقم تسلسلي
                string stockQuery = $@"
                    SELECT itemid,
                           ItemSerialNo,
                           SUM(stockIn - stockOut) AS result
                    FROM (
                        SELECT itemid,
                               ItemSerialNo,
                               SUM(ItemQuantity) AS stockIn,
                               0                 AS stockOut
                        FROM   inv
                        LEFT JOIN InvoiceItemDetail
                               ON inv.InvGlobalID = InvoiceItemDetail.InvGlobalID
                        WHERE  inv.IS_Deleted     = 0
                          AND  ItemId             = {InvItem.ItemId}
                          AND  InvertoryImpact    = 1
                        GROUP BY itemid, ItemSerialNo

                        UNION ALL

                        SELECT itemid,
                               ItemSerialNo,
                               0                 AS stockIn,
                               SUM(ItemQuantity) AS stockOut
                        FROM   inv
                        LEFT JOIN InvoiceItemDetail
                               ON inv.InvGlobalID = InvoiceItemDetail.InvGlobalID
                        WHERE  inv.IS_Deleted     = 0
                          AND  ItemId             = {InvItem.ItemId}
                          AND  InvertoryImpact    = 2
                        GROUP BY itemid, ItemSerialNo
                    ) AS stockSummary
                    GROUP BY itemid, ItemSerialNo";

                var stockTable = new DataTable();
                new SqlDataAdapter(stockQuery, _conn).Fill(stockTable);

                if (stockTable.Rows.Count == 0)
                    return;

                int rowIndex = 1;

                foreach (DataRow stockRow in stockTable.Rows)
                {
                    double stockBalance = stockRow["result"] == DBNull.Value
                        ? 0.0
                        : Convert.ToDouble(stockRow["result"]);

                    if (stockBalance <= 0)
                        continue;

                    string serialNo = stockRow["ItemSerialNo"]?.ToString() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(serialNo))
                        continue;

                    // جلب تفاصيل هذا الرقم من فواتير المشتريات / الأذونات
                    string detailQuery = $@"
                        SELECT *
                        FROM   InvoiceItemDetail
                        WHERE  ItemSerialNo = N'{serialNo}'
                          AND  InvGlobalID IN (
                               SELECT InvGlobalID
                               FROM   inv
                               WHERE  (inv_type = 1 OR inv_type = 4 OR inv_type = 9)
                                 AND  proc_type = 1
                          )";

                    var detailTable = new DataTable();
                    new SqlDataAdapter(detailQuery, _conn).Fill(detailTable);

                    foreach (DataRow detailRow in detailTable.Rows)
                    {
                        var detail = MapRowToDetail(detailRow);

                        InvItem.InvoiceItemDetails.Add(detail);
                        _availableItems.Add(new InvoiceItemDetailRow(detail, rowIndex++));
                    }
                }

                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }

        /// <summary>
        /// تحميل الأرقام التسلسلية من استعلام خاص (ShowSeerialNo mode)
        /// </summary>
        public void LoadSerialNo()
        {
            try
            {
                AvailableNo.Visibility = Visibility.Visible;

                string selectQuery = $"SELECT SerialNo AS DgvSerialNo FROM ItemSerialNo, inv {cond}";
                var serialTable = new DataTable();
                new SqlDataAdapter(selectQuery, _conn).Fill(serialTable);

                _availableItems.Clear();
                InvItem.InvoiceItemDetails.Clear();

                int rowIndex = 1;

                foreach (DataRow row in serialTable.Rows)
                {
                    var detail = new InvoiceItemDetail
                    {
                        ItemSerialNo = row["DgvSerialNo"]?.ToString() ?? string.Empty,
                        ItemQuantity = 1.0,
                        ItemId = InvItem.ItemId
                    };

                    InvItem.InvoiceItemDetails.Add(detail);
                    _availableItems.Add(new InvoiceItemDetailRow(detail, rowIndex++));
                }

                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex);
            }
        }

        /// <summary>
        /// تحويل DataRow إلى InvoiceItemDetail بأمان
        /// </summary>
        private InvoiceItemDetail MapRowToDetail(DataRow row)
        {
            return new InvoiceItemDetail
            {
                ItemId = InvItem.ItemId,
                InvGlobalID = row["InvGlobalID"]?.ToString() ?? string.Empty,
                ItemSerialNo = row["ItemSerialNo"]?.ToString() ?? string.Empty,
                BatchNo = row["BatchNo"]?.ToString() ?? string.Empty,
                ItemColor = row["ItemColor"]?.ToString() ?? string.Empty,
                ItemSize = row["ItemSize"]?.ToString() ?? string.Empty,
                ItemBarcode = row["ItemBarcode"]?.ToString() ?? string.Empty,

                ItemQuantity = row["ItemQuantity"] == DBNull.Value ? 0.0 : Convert.ToDouble(row["ItemQuantity"]),
                ItemHeight = row["ItemHeight"] == DBNull.Value ? 0.0 : Convert.ToDouble(row["ItemHeight"]),
                ItemWidth = row["ItemWidth"] == DBNull.Value ? 0.0 : Convert.ToDouble(row["ItemWidth"]),
                FillValue = row["FillValue"] == DBNull.Value ? 0.0 : Convert.ToDouble(row["FillValue"]),
                FillRatio = row["FillRatio"] == DBNull.Value ? 0.0 : Convert.ToDouble(row["FillRatio"]),
                ItemProperty = row["ItemProperty"] == DBNull.Value ? 0 : Convert.ToInt32(row["ItemProperty"]),
                InvertoryImpact = row["InvertoryImpact"] == DBNull.Value ? 0 : Convert.ToInt32(row["InvertoryImpact"]),

                ItemProductionDate = row["ItemProductionDate"] == DBNull.Value
                                        ? DateTime.Now
                                        : Convert.ToDateTime(row["ItemProductionDate"]),

                ItemExpireDate = row["ItemExpireDate"] == DBNull.Value
                                        ? DateTime.Now.AddYears(1)
                                        : Convert.ToDateTime(row["ItemExpireDate"])
            };
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Business Logic

        /// <summary>
        /// توليد أرقام تسلسلية تلقائياً بناءً على الرقم الأول والكمية
        /// </summary>
        private void GenerateSerialNo()
        {
            if (string.IsNullOrWhiteSpace(txtSerialNo.Text) ||
                string.IsNullOrWhiteSpace(txtQty.Text))
                return;

            if (!int.TryParse(txtSerialNo.Text.Trim(), out int startSerial))
            {
                ShowMessage(
                    ar: "الرقم التسلسلي يجب أن يكون رقماً صحيحاً",
                    en: "Serial number must be a valid integer.",
                    icon: MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtQty.Text.Trim(), out int qty) || qty <= 0)
            {
                ShowMessage(
                    ar: "الكمية يجب أن تكون أكبر من صفر",
                    en: "Quantity must be greater than zero.",
                    icon: MessageBoxImage.Warning);
                return;
            }

            int addedCount = 0;

            for (int offset = 0; offset < qty; offset++)
            {
                int currentSerial = startSerial + offset;
                string serialStr = currentSerial.ToString();

                // تخطي الأرقام المكررة
                if (CheckSerialNo(serialStr))
                {
                    ShowMessage(
                        ar: $"الرقم مدخل مسبقاً: {serialStr}",
                        en: $"The number is already entered: {serialStr}",
                        icon: MessageBoxImage.Warning);
                    continue;
                }

                var detail = new InvoiceItemDetail
                {
                    ItemSerialNo = serialStr,
                    ItemQuantity = 1.0,
                    ItemId = InvItem.ItemId
                };

                InvItem.InvoiceItemDetails.Add(detail);

                _availableItems.Add(
                    new InvoiceItemDetailRow(detail, _availableItems.Count + 1));

                addedCount++;
            }

            if (addedCount > 0)
            {
                UpdateStatusBar();
            }
        }

        /// <summary>
        /// مسح جميع البيانات وإعادة التهيئة
        /// </summary>
        private void ClearAll()
        {
            txtQty.Text = "1";
            txtSerialNo.Text = string.Empty;

            _availableItems.Clear();
            InvItem.InvoiceItemDetails.Clear();

            UpdateStatusBar();
        }

        /// <summary>
        /// فحص تكرار الرقم التسلسلي في ObservableCollection والقاعدة
        /// </summary>
        private bool CheckSerialNo(string serialNo)
        {
            if (string.IsNullOrWhiteSpace(serialNo))
                return false;

            // ① فحص في ObservableCollection المعروض
            if (_availableItems.Any(r =>
                    string.Equals(r.ItemSerialNo, serialNo, StringComparison.Ordinal)))
                return true;

            // ② فحص في InvItem مباشرة
            if (InvItem.InvoiceItemDetails.Any(d =>
                    string.Equals(d.ItemSerialNo, serialNo, StringComparison.Ordinal)))
                return true;

            // ③ فحص في قاعدة البيانات
            try
            {
                string checkQuery = $@"
                    SELECT TOP 1 ItemDetailId
                    FROM   InvoiceItemDetail
                    WHERE  ItemSerialNo = N'{serialNo}'";

                var checkTable = new DataTable();
                new SqlDataAdapter(checkQuery, _conn).Fill(checkTable);
                return checkTable.Rows.Count > 0;
            }
            catch
            {
                // لا نوقف التطبيق بسبب خطأ في الفحص
                return false;
            }
        }

        /// <summary>
        /// فحص التكرار مع فواتير InvoiceDGV قبل الإدراج
        /// </summary>
        private bool CheckDuplicates()
        {
            string focusedSerial = GetFocusedSerialFromGrid1();

            if (string.IsNullOrWhiteSpace(focusedSerial))
                return false;

            foreach (var invoiceItem in Invo.InvoiceItems)
            {
                foreach (var detail in invoiceItem.InvoiceItemDetails)
                {
                    if (string.Equals(
                            detail.ItemSerialNo,
                            focusedSerial,
                            StringComparison.Ordinal))
                    {
                        ShowMessage(
                            ar: $"الرقم مدخل مسبقاً: {focusedSerial}",
                            en: $"The number is already entered: {focusedSerial}",
                            icon: MessageBoxImage.Warning);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// إدراج الرقم التسلسلي المحدد في GridControl1 إلى InvItem
        /// </summary>
        private void InsertSerialNo()
        {
            var selectedRow = GridControl1.SelectedItem as InvoiceItemDetailRow;
            if (selectedRow == null)
                return;

            var newDetail = new InvoiceItemDetail
            {
                ItemQuantity = 1.0,
                ItemId = InvItem.ItemId,
                ItemSerialNo = selectedRow.ItemSerialNo,
                ItemColor = selectedRow.ItemColor,
                ItemSize = selectedRow.ItemSize,
                BatchNo = selectedRow.BatchNo
            };

            InvItem.InvoiceItemDetails.Clear();
            InvItem.InvoiceItemDetails.Add(newDetail);
        }

        /// <summary>
        /// إدراج الرقم التسلسلي المحدد عند double-click من GridControl1
        /// </summary>
        private void InsertSerialNoFromFocusedRow()
        {
            InsertSerialNo();
        }

        /// <summary>
        /// التحقق من وجود مبيعات مرتبطة بالرقم التسلسلي
        /// </summary>
        private bool IsSerialLinkedToSales(string serialNo)
        {
            if (string.IsNullOrWhiteSpace(serialNo))
                return false;

            try
            {
                string query = $@"
                    SELECT TOP 1 InvoiceItemDetail.InvGlobalID
                    FROM   InvoiceItemDetail
                    JOIN   inv
                           ON InvoiceItemDetail.InvGlobalID = inv.InvGlobalID
                    WHERE  InvoiceItemDetail.ItemSerialNo = N'{serialNo}'
                      AND  inv.inv_type    = 2
                      AND  inv.IS_Deleted  = 0";

                var table = new DataTable();
                new SqlDataAdapter(query, _conn).Fill(table);
                return table.Rows.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// الحصول على الرقم التسلسلي للعنصر المحدد في GridControl1
        /// </summary>
        private string GetFocusedSerialFromGrid1()
        {
            var selectedRow = GridControl1.SelectedItem as InvoiceItemDetailRow;
            return selectedRow?.ItemSerialNo ?? string.Empty;
        }

        /// <summary>
        /// هل نحن في وضع المبيعات / المرتجعات؟
        /// </summary>
        private bool IsSalesMode()
        {
            return InvItem.InvType == 2
                || InvItem.InvType == 3
                || InvItem.InvType == 5;
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region UI Helper Methods

        /// <summary>
        /// إظهار / إخفاء لوحة الأرقام المباعة وأزرار النقل
        /// </summary>
        private void SetSelledPanelVisible(bool isVisible)
        {
            if (isVisible)
            {
                PnlSelled.Visibility = Visibility.Visible;
                PnlBtn.Visibility = Visibility.Visible;

                // توسيع العمود الأيمن لعرض جدول المباع
                SetContentColumnWidth(2, new GridLength(1, GridUnitType.Star));
                SetContentColumnWidth(1, new GridLength(46));
            }
            else
            {
                PnlSelled.Visibility = Visibility.Collapsed;
                PnlBtn.Visibility = Visibility.Collapsed;

                SetContentColumnWidth(2, new GridLength(0));
                SetContentColumnWidth(1, new GridLength(0));
            }
        }

        /// <summary>
        /// تعديل عرض عمود في Grid المحتوى الرئيسي
        /// </summary>
        private void SetContentColumnWidth(int columnIndex, GridLength width)
        {
            // المحتوى الرئيسي موجود في Grid.Row[2]
            // هيكل: ThemedWindow > Grid > Grid[Row=2]
            if (Content is not Grid rootGrid)
                return;

            // نبحث عن Grid المحتوى (Row 2)
            foreach (var child in rootGrid.Children)
            {
                if (child is Grid contentGrid && Grid.GetRow((UIElement)child) == 2)
                {
                    if (columnIndex < contentGrid.ColumnDefinitions.Count)
                    {
                        contentGrid.ColumnDefinitions[columnIndex].Width = width;
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// تحديث أرقام صفوف جدول الأرقام المتاحة
        /// </summary>
        private void RefreshAvailableRowNumbers()
        {
            for (int i = 0; i < _availableItems.Count; i++)
            {
                _availableItems[i].RowNo = i + 1;
            }

            GridControl1.Items.Refresh();
        }

        /// <summary>
        /// تحديث أرقام صفوف جدول الأرقام المباعة
        /// </summary>
        private void RefreshSelledRowNumbers()
        {
            for (int i = 0; i < _selledItems.Count; i++)
            {
                _selledItems[i].DgvNo1 = i + 1;
            }

            GridControl2.Items.Refresh();
        }

        /// <summary>
        /// تحديث شريط الحالة السفلي
        /// </summary>
        private void UpdateStatusBar()
        {
            int availableCount = _availableItems.Count;
            int selledCount = _selledItems.Count;

            lblStatusInfo.Text = MainClass.Language == "ar"
                ? $"📦 المتاح: {availableCount}   |   📤 المباع: {selledCount}"
                : $"📦 Available: {availableCount}   |   📤 Selled: {selledCount}";

            lblAvailableNo.Text = MainClass.Language == "ar"
                ? $"الأرقام المتاحة: {availableCount}"
                : $"Available Numbers: {availableCount}";

            lblSelledNo.Text = MainClass.Language == "ar"
                ? $"الأرقام المباعة: {selledCount}"
                : $"Selled Numbers: {selledCount}";
        }

        /// <summary>
        /// عرض رسالة حسب اللغة الحالية
        /// </summary>
        private void ShowMessage(string ar, string en,
                                 MessageBoxImage icon = MessageBoxImage.Information)
        {
            string message = MainClass.Language == "ar" ? ar : en;
            string title = MainClass.Language == "ar" ? "رسالة" : "Message";

            DXMessageBox.Show(message, title, MessageBoxButton.OK, icon);
        }

        /// <summary>
        /// عرض رسالة خطأ للمطور
        /// </summary>
        private void ShowErrorMessage(Exception ex)
        {
            DXMessageBox.Show(
                $"حدث خطأ غير متوقع:\n{ex.Message}",
                "خطأ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        #endregion
    }

    // ══════════════════════════════════════════════════════════════
    #region Wrapper: InvoiceItemDetailRow

    /// <summary>
    /// Wrapper لعرض InvoiceItemDetail داخل DataGrid
    /// بدون تعديل الكلاس الأصلي الموجود في المكتبة الخارجية
    /// </summary>
    public class InvoiceItemDetailRow
    {
        // ── الخاصية الإضافية لرقم الصف في الواجهة ──
        public int RowNo { get; set; }

        // ── مرجع للكائن الأصلي ──
        public InvoiceItemDetail Detail { get; }

        // ── خصائص مُعاد تمريرها من Detail ──

        public int ItemDetailId => Detail.ItemDetailId;

        public int ItemId => Detail.ItemId;

        public string ItemSerialNo
        {
            get => Detail.ItemSerialNo;
            set => Detail.ItemSerialNo = value;
        }

        public string BatchNo
        {
            get => Detail.BatchNo;
            set => Detail.BatchNo = value;
        }

        public DateTime ItemProductionDate
        {
            get => Detail.ItemProductionDate;
            set => Detail.ItemProductionDate = value;
        }

        public DateTime ItemExpireDate
        {
            get => Detail.ItemExpireDate;
            set => Detail.ItemExpireDate = value;
        }

        public double ItemHeight
        {
            get => Detail.ItemHeight;
            set => Detail.ItemHeight = value;
        }

        public double ItemWidth
        {
            get => Detail.ItemWidth;
            set => Detail.ItemWidth = value;
        }

        public string ItemColor
        {
            get => Detail.ItemColor;
            set => Detail.ItemColor = value;
        }

        public string ItemSize
        {
            get => Detail.ItemSize;
            set => Detail.ItemSize = value;
        }

        public int ItemProperty
        {
            get => Detail.ItemProperty;
            set => Detail.ItemProperty = value;
        }

        public double FillValue
        {
            get => Detail.FillValue;
            set => Detail.FillValue = value;
        }

        public double FillRatio
        {
            get => Detail.FillRatio;
            set => Detail.FillRatio = value;
        }

        public double ItemQuantity
        {
            get => Detail.ItemQuantity;
            set => Detail.ItemQuantity = value;
        }

        public string ItemBarcode
        {
            get => Detail.ItemBarcode;
            set => Detail.ItemBarcode = value;
        }

        // ── Constructor ──
        public InvoiceItemDetailRow(InvoiceItemDetail detail, int rowNo)
        {
            Detail = detail ?? throw new ArgumentNullException(nameof(detail));
            RowNo = rowNo;
        }
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    #region Helper Model: SelledSerialRow

    /// <summary>
    /// نموذج بيانات بسيط لتمثيل صف في جدول الأرقام المباعة / المنقولة
    /// </summary>
    public class SelledSerialRow
    {
        public int DgvNo1 { get; set; }
        public string DgvSerialNo1 { get; set; }
    }

    #endregion
}