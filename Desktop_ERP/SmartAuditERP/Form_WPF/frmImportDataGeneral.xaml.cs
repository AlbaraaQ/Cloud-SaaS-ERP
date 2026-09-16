using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using ExcelDataReader;
using Microsoft.Win32;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmImportDataGeneral : ThemedWindow
    {
        #region ── Fields ──────────────────────────────────────────────────

        private SqlConnection _conn;
        private SqlConnection _conn1;
        private SqlCommand _cmd;
        private SqlTransaction _transaction;

        // رقم التبويب الحالي (0=تعليمات, 1=اختر ملف, 2=عرض, 3=تطابق, 4=انتهاء)
        private int _currentTabIndex;

        // بيانات الاستيراد من Excel
        private DataSet _excelDataSet;

        // مجموعة صفوف تطابق الأعمدة
        private ObservableCollection<ColumnMappingRow> _mappingRows;

        // Public fields (محافظ على أسمائها الأصلية)
        public int ClientId = -1;
        public bool ISDone = false;
        public DataTable dtInvSel;
        public DataTable dtIniRes;

        #endregion

        #region ── Constructor ─────────────────────────────────────────────

        public frmImportDataGeneral()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            _conn1 = MainClass.ConnObj();
            _cmd = new SqlCommand();
            _currentTabIndex = 0;
            _excelDataSet = new DataSet();
            _mappingRows = new ObservableCollection<ColumnMappingRow>();
            dtInvSel = new DataTable();
            dtIniRes = new DataTable();

            dgvImportedData.ItemsSource = _mappingRows;
        }

        #endregion

        #region ── Window Events ───────────────────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // ابدأ من أول تبويب
            _currentTabIndex = 0;
            TabControl1.SelectedIndex = 0;
            btnPrevious.IsEnabled = false;
            btnImport.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region ── TabControl Events ────────────────────────────────────────

        private void TabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source != TabControl1) return;

            try
            {
                int selectedIndex = TabControl1.SelectedIndex;

                // منع المستخدم من التنقل يدويًا، التنقل فقط عبر الأزرار
                if (selectedIndex != _currentTabIndex)
                {
                    TabControl1.SelectedIndex = _currentTabIndex;
                    return;
                }

                // ضبط ظهور الأزرار
                if (selectedIndex == 3)
                {
                    btnImport.Visibility = Visibility.Visible;
                    btnNext.Visibility = Visibility.Collapsed;
                }
                else if (selectedIndex > 3)
                {
                    btnNext.Visibility = Visibility.Collapsed;
                    btnImport.Visibility = Visibility.Collapsed;
                }
                else
                {
                    btnNext.Visibility = Visibility.Visible;
                    btnImport.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تبديل التبويب", ex);
            }
        }

        #endregion

        #region ── Navigation Buttons ──────────────────────────────────────

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // تحقق: يجب اختيار ملف أولًا
                if (_currentTabIndex == 1 && string.IsNullOrWhiteSpace(txtFileName.Text))
                {
                    DXMessageBox.Show("الرجاء اختيار ملف", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _currentTabIndex++;

                if (_currentTabIndex == 2)
                    btnPrevious.IsEnabled = true;

                // عند الوصول لتبويب تطابق الأعمدة: تحميل رؤوس الأعمدة
                if (_currentTabIndex == 3 && dgvData.Items.Count > 0)
                    LoadExcelHeadersIntoMappingCombo();

                if (_currentTabIndex == 3)
                {
                    btnImport.Visibility = Visibility.Visible;
                    btnNext.Visibility = Visibility.Collapsed;
                }

                if (_currentTabIndex == 4)
                {
                    btnPrevious.Visibility = Visibility.Collapsed;
                    btnImport.Visibility = Visibility.Collapsed;
                }

                if (_currentTabIndex <= 4)
                    TabControl1.SelectedIndex = _currentTabIndex;

                btnNext.IsEnabled = true;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في التنقل للأمام", ex);
            }
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentTabIndex--;

                if (_currentTabIndex >= 0)
                    TabControl1.SelectedIndex = _currentTabIndex;

                if (_currentTabIndex == 1)
                    btnPrevious.IsEnabled = false;

                // إعادة تحميل رؤوس عند العودة لتبويب التطابق
                if (_currentTabIndex == 3 && dgvData.Items.Count > 0)
                    LoadExcelHeadersIntoMappingCombo();

                if (_currentTabIndex == 3)
                    btnImport.Visibility = Visibility.Visible;
                else
                {
                    btnImport.Visibility = Visibility.Collapsed;
                    btnNext.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في التنقل للخلف", ex);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region ── File Selection ───────────────────────────────────────────

        private void btnChooseFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "اختر ملف Excel",
                    Filter = "Excel Files (*.xls;*.xlsx)|*.xls;*.xlsx",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                if (dialog.ShowDialog() != true) return;

                string filePath = dialog.FileName;
                txtFileName.Text = filePath;

                // فتح الملف وقراءته
                FileStream fileStream;
                try
                {
                    fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                }
                catch
                {
                    DXMessageBox.Show("الرجاء إغلاق الملف المراد استيراده أولًا.",
                                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // قراءة بيانات Excel
                IExcelDataReader excelReader;
                string ext = Path.GetExtension(filePath).ToLower();

                if (ext == ".xls")
                    excelReader = ExcelReaderFactory.CreateBinaryReader(fileStream);
                else
                    excelReader = ExcelReaderFactory.CreateOpenXmlReader(fileStream);

                _excelDataSet = excelReader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = true
                    }
                });

                excelReader.Close();
                fileStream.Close();

                // تعبئة قائمة الصفحات
                cboSheet.Items.Clear();
                foreach (DataTable table in _excelDataSet.Tables)
                    cboSheet.Items.Add(table.TableName);

                if (cboSheet.Items.Count > 0)
                    cboSheet.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في فتح الملف", ex);
            }
        }

        #endregion

        #region ── Sheet Selection ──────────────────────────────────────────

        private void cboSheet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cboSheet.SelectedIndex < 0 || _excelDataSet == null) return;
                if (cboSheet.SelectedIndex >= _excelDataSet.Tables.Count) return;

                dgvData.AutoGenerateColumns = true;
                dgvData.ItemsSource = _excelDataSet.Tables[cboSheet.SelectedIndex].DefaultView;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل الصفحة", ex);
            }
        }

        #endregion

        #region ── Column Mapping ───────────────────────────────────────────

        /// <summary>
        /// تحميل أسماء أعمدة Excel في قائمة ComboBox داخل جدول التطابق.
        /// </summary>
        private void LoadExcelHeadersIntoMappingCombo()
        {
            try
            {
                if (_excelDataSet == null || cboSheet.SelectedIndex < 0) return;

                int sheetIndex = cboSheet.SelectedIndex;
                if (sheetIndex >= _excelDataSet.Tables.Count) return;

                DataTable sheetTable = _excelDataSet.Tables[sheetIndex];

                // 🟢 الإصلاح: بناء قائمة الأعمدة كـ (فهرس، اسم) لتتناسب مع SelectedValuePath
                var columnList = new List<KeyValuePair<int, string>>();

                // الخيار الافتراضي (فهرس -1 يعني غير مرتبط)
                columnList.Add(new KeyValuePair<int, string>(-1, "-- لا شيء --"));

                // إضافة أعمدة الإكسل مع فهارسها الحقيقية
                for (int i = 0; i < sheetTable.Columns.Count; i++)
                {
                    columnList.Add(new KeyValuePair<int, string>(i, sheetTable.Columns[i].ColumnName));
                }

                // تعيين مصدر بيانات عمود ComboBox في الجدول
                colMappedColumn.ItemsSource = columnList;

                // إعادة ضبط الفهارس المختارة
                foreach (var row in _mappingRows)
                    row.MappedColumnIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل رؤوس الأعمدة", ex);
            }
        }

        private void cmbDataTable_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _mappingRows.Clear();

            int idx = cmbDataTable.SelectedIndex;

            if (idx == 2 || idx == 3) // عملاء أو موردون
            {
                AddMappingRow("الاسم");
                AddMappingRow("الجوال");
                AddMappingRow("رقم الهوية");
                AddMappingRow("الرقم الضريبي");
            }
            else if (idx == 1) // مجموعات المواد
            {
                AddMappingRow("اسم المجموعة");
                AddMappingRow("مجموعة (EN)");
                AddMappingRow("الرمز");
                AddMappingRow("الرئيسية");
            }
            else if (idx == 0) // المواد
            {
                string[] materialFields =
                {
                    "رقم المجموعة", "اسم المادة", "اسم المادة (EN)",
                    "الباركود", "رقم الوحدة الافتراضية",
                    "سعر الشراء", "سعر البيع", "رمز المادة",
                    "الوحدة الثانية", "تعادل الوحدة الثانية",
                    "سعر الشراء للوحدة الثانية", "سعر البيع للوحدة الثانية",
                    "باركود الوحدة الثانية", "الوحدة الثالثة",
                    "تعادل الوحدة الثالثة", "سعر الشراء للوحدة الثالثة",
                    "سعر البيع للوحدة الثالثة", "باركود الوحدة الثالثة",
                    "اسم المجموعة", "اسم الوحدة"
                };
                foreach (var field in materialFields)
                    AddMappingRow(field);
            }
        }

        private void cmbInv_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _mappingRows.Clear();

            int idx = cmbInv.SelectedIndex;

            if (idx == 0 || idx == 1) // مبيعات / مشتريات
            {
                AddMappingRow("رمز الصنف");
                AddMappingRow("الوصف");
                AddMappingRow("الوحدة");
                AddMappingRow("الكمية");
                AddMappingRow("السعر");
            }
            else if (idx == 2 || idx == 3) // قيد افتتاحي / سند قيد
            {
                AddMappingRow("رمز الحساب");
                AddMappingRow("اسم الحساب");
                AddMappingRow("مدين");
                AddMappingRow("دائن");
                AddMappingRow("الشرح");
            }
            else if (idx == 4) // تسوية جردية
            {
                AddMappingRow("رمز الصنف");
                AddMappingRow("الوحدة");
                AddMappingRow("الكمية الفعلية");
                AddMappingRow("ملاحظة");
            }
        }

        private void AddMappingRow(string fieldName)
        {
            _mappingRows.Add(new ColumnMappingRow
            {
                IsSelected = false,
                FieldName = fieldName,
                MappedColumnIndex = -1,
                DefaultValue = string.Empty
            });
        }

        private void dgvImportedData_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                // إذا أُلغي الاختيار (IsSelected = false) → امسح الحقل البديل
                if (e.Column.DisplayIndex == 0 &&
                    e.Row.Item is ColumnMappingRow row)
                {
                    if (!row.IsSelected)
                        row.MappedColumnIndex = -1;
                }
            }
            catch { /* تجاهل */ }
        }

        #endregion

        #region ── Import Button ────────────────────────────────────────────

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            int tableIdx = cmbDataTable.SelectedIndex;
            int invIdx = cmbInv.SelectedIndex;

            if (tableIdx == 0) InsertItems();
            else if (tableIdx == 1) InsertItemCategory();
            else if (tableIdx == 2) InsertClients();
            else if (tableIdx == 3) InsertSuppliers();

            if (invIdx == 0 || invIdx == 1) InsertSalesInvoice();
            else if (invIdx == 2 || invIdx == 3) InsertInitialRestriction();
            else if (invIdx == 4) InsertSafeAdjust();
        }

        #endregion

        #region ── Insert: Clients ──────────────────────────────────────────

        private void InsertClients()
        {
            if (cmbDataTable.SelectedIndex != 2) return;

            EnsureConnectionOpen(_conn);
            _transaction = _conn.BeginTransaction();

            try
            {
                if (ConfirmImport() == MessageBoxResult.No) return;

                string mobile = string.Empty;
                string nationalId = string.Empty;
                string taxNo = string.Empty;

                SetProgressMax(dgvData.Items.Count - 1);

                int rowCount = GetDataGridRowCount(dgvData);

                for (int i = 0; i < rowCount; i++)
                {
                    if (!GetRowIsSelected(0))
                    {
                        DXMessageBox.Show("يرجى اختيار عمود الأسماء");
                        return;
                    }

                    string nameValue = GetCellValueFromExcel(i, 0, out bool nameOk);
                    if (!nameOk)
                    {
                        DXMessageBox.Show($"يجب إدخال اسم عميل في الصف {i + 1}");
                        return;
                    }

                    mobile = GetMappedOrDefault(i, 1);
                    nationalId = GetMappedOrDefault(i, 2);
                    taxNo = GetMappedOrDefault(i, 3);

                    _cmd = new SqlCommand(@"
                        INSERT INTO Customers
                            (name, national_id, tax_no, type, mobile,
                             IS_Deleted, IdScan, Branch, ISCredit)
                        VALUES
                            (@name, @national_id, @tax_no, @type, @mobile,
                             @IS_Deleted, @IdScan, @Branch, 0)",
                        _conn, _transaction);

                    _cmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = nameValue;
                    _cmd.Parameters.Add("@national_id", SqlDbType.NVarChar).Value = nationalId;
                    _cmd.Parameters.Add("@mobile", SqlDbType.NVarChar).Value = mobile;
                    _cmd.Parameters.Add("@tax_no", SqlDbType.NVarChar).Value = taxNo;
                    _cmd.Parameters.Add("@type", SqlDbType.Int).Value = 1;
                    _cmd.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;
                    _cmd.Parameters.Add("@IdScan", SqlDbType.Image).Value = DBNull.Value;
                    _cmd.Parameters.Add("@Branch", SqlDbType.Int).Value = MainClass.BranchNo;
                    _cmd.ExecuteNonQuery();

                    IncrementProgress();
                }

                _transaction.Commit();
                GoToFinalTab();
            }
            catch (Exception ex)
            {
                _transaction?.Rollback();
                ShowError("خطأ أثناء استيراد العملاء", ex);
            }
            finally
            {
                CloseConnection(_conn);
            }
        }

        #endregion

        #region ── Insert: Suppliers ────────────────────────────────────────

        private void InsertSuppliers()
        {
            if (cmbDataTable.SelectedIndex != 3) return;

            EnsureConnectionOpen(_conn);
            _transaction = _conn.BeginTransaction();

            try
            {
                if (ConfirmImport() == MessageBoxResult.No) return;

                SetProgressMax(dgvData.Items.Count - 1);

                int rowCount = GetDataGridRowCount(dgvData);

                for (int i = 0; i < rowCount; i++)
                {
                    if (!GetRowIsSelected(0))
                    {
                        DXMessageBox.Show("يرجى اختيار عمود الأسماء");
                        return;
                    }

                    string nameValue = GetCellValueFromExcel(i, 0, out bool nameOk);
                    if (!nameOk)
                    {
                        DXMessageBox.Show($"يجب إدخال اسم مورد في الصف {i + 1}");
                        return;
                    }

                    string mobile = GetMappedOrDefault(i, 1);
                    string nationalId = GetMappedOrDefault(i, 2);
                    string taxNo = GetMappedOrDefault(i, 3);

                    _cmd = new SqlCommand(@"
                        INSERT INTO Customers
                            (name, national_id, tax_no, type, mobile, IS_Deleted, IdScan)
                        VALUES
                            (@name, @national_id, @tax_no, @type, @mobile, @IS_Deleted, @IdScan)",
                        _conn, _transaction);

                    _cmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = nameValue;
                    _cmd.Parameters.Add("@national_id", SqlDbType.NVarChar).Value = nationalId;
                    _cmd.Parameters.Add("@mobile", SqlDbType.NVarChar).Value = mobile;
                    _cmd.Parameters.Add("@tax_no", SqlDbType.NVarChar).Value = taxNo;
                    _cmd.Parameters.Add("@type", SqlDbType.Int).Value = 2;
                    _cmd.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = 0;
                    _cmd.Parameters.Add("@IdScan", SqlDbType.Image).Value = DBNull.Value;
                    _cmd.ExecuteNonQuery();

                    IncrementProgress();
                }

                _transaction.Commit();
                GoToFinalTab();
            }
            catch (Exception ex)
            {
                _transaction?.Rollback();
                ShowError("خطأ أثناء استيراد الموردين", ex);
            }
            finally
            {
                CloseConnection(_conn);
            }
        }

        #endregion

        #region ── Insert: Item Category ────────────────────────────────────

        private void InsertItemCategory()
        {
            if (cmbDataTable.SelectedIndex != 1) return;

            EnsureConnectionOpen(_conn);
            _transaction = _conn.BeginTransaction();

            try
            {
                if (ConfirmImport() == MessageBoxResult.No) return;

                SetProgressMax(dgvData.Items.Count - 1);

                int rowCount = GetDataGridRowCount(dgvData);

                for (int i = 0; i < rowCount; i++)
                {
                    if (!GetRowIsSelected(0))
                    {
                        DXMessageBox.Show("يرجى اختيار عمود اسم المجموعة");
                        return;
                    }

                    string categoryName = GetCellValueFromExcel(i, 0, out bool nameOk);
                    if (!nameOk)
                    {
                        DXMessageBox.Show($"يجب إدخال اسم المجموعة في الصف {i + 1}");
                        return;
                    }

                    if (IsCategoryNameExist(categoryName))
                    {
                        DXMessageBox.Show($"يوجد مجموعة بنفس الاسم في الصف {i + 1}");
                        return;
                    }

                    string nameEn = GetMappedOrDefault(i, 1);
                    string categoryCode = GetMappedOrDefault(i, 2);

                    if (string.IsNullOrEmpty(categoryCode))
                    {
                        categoryCode = new SqlCommand(
                            "SELECT ISNULL(MAX(id), 0) + 1 FROM itemsCategory",
                            _conn, _transaction).ExecuteScalar()?.ToString() ?? "1";
                    }

                    // حساب الكود الأصلي
                    string parentCode = string.Empty;
                    string parentName = GetMappedOrDefault(i, 3);
                    if (!string.IsNullOrEmpty(parentName))
                    {
                        var parentCmd = new SqlCommand(
                            "SELECT code FROM itemsCategory WHERE name = @name AND IS_Deleted = 0",
                            _conn, _transaction);
                        parentCmd.Parameters.AddWithValue("@name", parentName);
                        object parentResult = parentCmd.ExecuteScalar();
                        if (parentResult != null)
                            parentCode = parentResult.ToString() ?? string.Empty;
                    }

                    _cmd = new SqlCommand(@"
                        INSERT INTO itemsCategory
                            (CategoryId, name, nameEN, color, printer,
                             ShowInPOS, DispalyOrder, IS_Deleted,
                             code, parentCode, type, id)
                        VALUES
                            (@CategoryId, @name, @nameEN, -1, @Printers,
                             0, 0, 0, @code, @parentCode, 2, @Id)",
                        _conn, _transaction);

                    _cmd.Parameters.AddWithValue("@CategoryId", categoryCode);
                    _cmd.Parameters.AddWithValue("@name", categoryName);
                    _cmd.Parameters.AddWithValue("@nameEN", nameEn);
                    _cmd.Parameters.AddWithValue("@Printers", string.Empty);
                    _cmd.Parameters.AddWithValue("@code", categoryCode);
                    _cmd.Parameters.AddWithValue("@parentCode",
                        string.IsNullOrEmpty(parentCode) ? (object)DBNull.Value : parentCode);
                    _cmd.Parameters.AddWithValue("@Id", categoryCode);
                    _cmd.ExecuteNonQuery();

                    IncrementProgress();
                }

                _transaction.Commit();
                GoToFinalTab();
            }
            catch (Exception ex)
            {
                _transaction?.Rollback();
                ShowError("خطأ أثناء استيراد مجموعات المواد", ex);
            }
            finally
            {
                CloseConnection(_conn);
            }
        }

        #endregion

        #region ── Insert: Items ────────────────────────────────────────────

        private void InsertItems()
        {
            if (cmbDataTable.SelectedIndex != 0) return;

            EnsureConnectionOpen(_conn);

            try
            {
                if (ConfirmImport() == MessageBoxResult.No) return;

                SetProgressMax(dgvData.Items.Count - 1);

                // تحميل مجموعات الأسماء والوحدات مسبقًا
                var categoryNamesTable = LoadTableData("SELECT ISNULL(name,'') AS name FROM itemsCategory", _conn);
                var unitNamesTable = LoadTableData("SELECT ISNULL(name,'') AS name FROM units", _conn);

                int rowCount = GetDataGridRowCount(dgvData);

                for (int i = 0; i < rowCount; i++)
                {
                    string groupCode = string.Empty;
                    string itemNameAr = string.Empty;
                    string itemNameEn = string.Empty;
                    string barcode = string.Empty;
                    string itemCode = string.Empty;
                    int unitId = 1;
                    double purchasePrice = 0;
                    double salePrice = 0;
                    double taxRate = 15.0;
                    int unit2Id = 0;
                    int unit2Equiv = 0;
                    double unit2PurchPrice = 0;
                    double unit2SalePrice = 0;
                    string unit2Barcode = string.Empty;
                    int unit3Id = 0;
                    int unit3Equiv = 0;
                    double unit3PurchPrice = 0;
                    double unit3SalePrice = 0;
                    string unit3Barcode = string.Empty;

                    // ── تحديد المجموعة ──
                    bool useGroupByCode = GetRowIsSelected(0);
                    bool useGroupByName = GetRowIsSelected(18);

                    if (useGroupByCode && useGroupByName)
                    {
                        DXMessageBox.Show("يرجى تحديد الاستيراد للمجموعة بالرقم أو الاسم، ليس كليهما");
                        return;
                    }
                    if (!useGroupByCode && !useGroupByName)
                    {
                        DXMessageBox.Show("يرجى اختيار المجموعة");
                        return;
                    }

                    if (useGroupByName)
                    {
                        string groupNameFromExcel = GetCellValueFromExcel(i, 18, out _);
                        bool found = false;
                        foreach (DataRow row in categoryNamesTable.Rows)
                        {
                            if (row["name"]?.ToString() == groupNameFromExcel)
                            { found = true; break; }
                        }
                        groupCode = found
                            ? GetGroupIdByName(groupNameFromExcel).ToString()
                            : CreateNewCategory(groupNameFromExcel);

                        categoryNamesTable = LoadTableData(
                            "SELECT ISNULL(name,'') AS name FROM itemsCategory", _conn);
                    }
                    else // useGroupByCode
                    {
                        groupCode = GetCellValueFromExcel(i, 0, out bool gcOk);
                        if (!gcOk)
                        {
                            DXMessageBox.Show($"يجب إدخال رمز المجموعة في الصف {i + 1}");
                            return;
                        }
                    }

                    // ── تحديد الوحدة ──
                    bool useUnitById = GetRowIsSelected(4);
                    bool useUnitByName = GetRowIsSelected(19);

                    if (useUnitById && useUnitByName)
                    {
                        DXMessageBox.Show("يرجى تحديد الاستيراد للوحدة بالرقم أو الاسم، ليس كليهما");
                        return;
                    }

                    if (useUnitByName)
                    {
                        string unitNameFromExcel = GetCellValueFromExcel(i, 19, out _);
                        bool found = false;
                        foreach (DataRow row in unitNamesTable.Rows)
                        {
                            if (row["name"]?.ToString() == unitNameFromExcel)
                            { found = true; break; }
                        }
                        if (found)
                            unitId = GetUnitIdByName(unitNameFromExcel);
                        else
                        {
                            int newUnitId = GetUnitId();
                            InsertNewUnit(newUnitId, unitNameFromExcel);
                            unitNamesTable = LoadTableData(
                                "SELECT ISNULL(name,'') AS name FROM units", _conn);
                            unitId = GetUnitIdByName(unitNameFromExcel);
                        }
                    }
                    else if (useUnitById)
                    {
                        string unitVal = GetCellValueFromExcel(i, 4, out _);
                        unitId = int.TryParse(unitVal, out int ui) ? ui : 1;
                    }

                    // ── الحقول الأخرى ──
                    itemNameAr = GetMappedOrDefault(i, 1);
                    if (string.IsNullOrWhiteSpace(itemNameAr))
                    {
                        DXMessageBox.Show("يرجى اختيار اسم المادة");
                        return;
                    }

                    itemNameEn = GetMappedOrDefault(i, 2);
                    barcode = GetMappedOrDefault(i, 3);
                    itemCode = GetMappedOrDefault(i, 7);

                    string purchStr = GetMappedOrDefault(i, 5);
                    if (string.IsNullOrWhiteSpace(purchStr))
                    {
                        DXMessageBox.Show("يرجى اختيار سعر الشراء");
                        return;
                    }
                    purchasePrice = SafeDouble(purchStr);
                    salePrice = SafeDouble(GetMappedOrDefault(i, 6));

                    // وحدة ثانية وثالثة
                    unit2Id = SafeInt(GetMappedOrDefault(i, 8));
                    unit2Equiv = SafeInt(GetMappedOrDefault(i, 9));
                    unit2PurchPrice = SafeDouble(GetMappedOrDefault(i, 10));
                    unit2SalePrice = SafeDouble(GetMappedOrDefault(i, 11));
                    unit2Barcode = GetMappedOrDefault(i, 12);
                    unit3Id = SafeInt(GetMappedOrDefault(i, 13));
                    unit3Equiv = SafeInt(GetMappedOrDefault(i, 14));
                    unit3PurchPrice = SafeDouble(GetMappedOrDefault(i, 15));
                    unit3SalePrice = SafeDouble(GetMappedOrDefault(i, 16));
                    unit3Barcode = GetMappedOrDefault(i, 17);

                    // الحصول على ID جديد
                    int newItemId = SafeInt(
                        new SqlCommand("SELECT ISNULL(MAX(id),0)+1 FROM Items WHERE is_deleted=0",
                                       _conn).ExecuteScalar());

                    int groupDbId = GetGroupId(groupCode);

                    // إدراج المادة
                    _cmd = new SqlCommand(@"
                        INSERT INTO Items
                            (id, Code, GrpCode, name, nameEN, barcode,
                             group_id, unit, purch_price, sale_price,
                             limit, discount, tax_group, tax, IS_Deleted,
                             ShowInPOS, Wscale, store, ItemType,
                             WithholdingTax, EgyItemCode, EgyCodeType,
                             MaxDicountParcent, is_extra_tax_applied)
                        VALUES
                            (@id, @Code, @GrpCode, @name, @nameEN, @barcode,
                             @group_id, @unit, @purch_price, @sale_price,
                             0, 0, 1, @tax, 0, 1, 0, 1, 1,
                             0, '', '', 0, 0)", _conn);

                    _cmd.Parameters.Add("@id", SqlDbType.Int).Value = newItemId;
                    _cmd.Parameters.Add("@GrpCode", SqlDbType.NVarChar).Value = groupCode;
                    _cmd.Parameters.Add("@group_id", SqlDbType.Int).Value = groupDbId;
                    _cmd.Parameters.Add("@Code", SqlDbType.NVarChar).Value = itemCode;
                    _cmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = itemNameAr;
                    _cmd.Parameters.Add("@nameEN", SqlDbType.NVarChar).Value = itemNameEn;
                    _cmd.Parameters.Add("@barcode", SqlDbType.NVarChar).Value = barcode;
                    _cmd.Parameters.Add("@unit", SqlDbType.Int).Value = unitId;
                    _cmd.Parameters.Add("@purch_price", SqlDbType.Float).Value = purchasePrice;
                    _cmd.Parameters.Add("@sale_price", SqlDbType.Float).Value = salePrice;
                    _cmd.Parameters.Add("@tax", SqlDbType.Float).Value = taxRate;
                    _cmd.ExecuteNonQuery();

                    // إدراج وحدة أولى
                    InsertItemUnit(newItemId, unitId, 1, purchasePrice, salePrice, barcode);

                    // إدراج وحدة ثانية
                    if (GetRowIsSelected(8) && GetRowIsSelected(9) && unit2Id != 0)
                        InsertItemUnit(newItemId, unit2Id, unit2Equiv,
                                       unit2PurchPrice, unit2SalePrice, unit2Barcode);

                    // إدراج وحدة ثالثة
                    if (GetRowIsSelected(13) && GetRowIsSelected(14) && unit3Id != 0)
                        InsertItemUnit(newItemId, unit3Id, unit3Equiv,
                                       unit3PurchPrice, unit3SalePrice, unit3Barcode);

                    // سجل الأسعار
                    InsertItemPrice(newItemId, unitId, purchasePrice, salePrice);

                    IncrementProgress();
                }

                GoToFinalTab();
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء استيراد المواد", ex);
            }
            finally
            {
                CloseConnection(_conn);
            }
        }

        private void InsertItemUnit(int itemId, int unitId, int equiv,
                                    double purchPrice, double salePrice, string barcode)
        {
            var cmd = new SqlCommand(@"
                INSERT INTO ItemUnits (ItemId, unit, perc, purch, sale, barcode)
                VALUES (@ItemId, @unit, @perc, @purch, @sale, @barcode)", _conn);

            cmd.Parameters.Add("@ItemId", SqlDbType.Int).Value = itemId;
            cmd.Parameters.Add("@unit", SqlDbType.Int).Value = unitId;
            cmd.Parameters.Add("@perc", SqlDbType.Int).Value = equiv;
            cmd.Parameters.Add("@purch", SqlDbType.Float).Value = purchPrice;
            cmd.Parameters.Add("@sale", SqlDbType.Float).Value = salePrice;
            cmd.Parameters.Add("@barcode", SqlDbType.NVarChar).Value = barcode;
            cmd.ExecuteNonQuery();
        }

        private void InsertItemPrice(int itemId, int unitId,
                                     double purchPrice, double salePrice)
        {
            var cmd = new SqlCommand(@"
                INSERT INTO ItemPrices
                    (ItemID, UnitID, time, date, purch_price, sale_price,
                     low_purch_price, high_purch_price, low_sale_price,
                     high_sale_price, CompetitorPrice, Emp, IS_Deleted)
                VALUES
                    (@ItemId, @unit, @time, @date, @purch, @sale,
                     0, 0, 0, 0, 0, @Emp, 0)", _conn);

            cmd.Parameters.Add("@ItemId", SqlDbType.Int).Value = itemId;
            cmd.Parameters.Add("@unit", SqlDbType.Int).Value = unitId;
            cmd.Parameters.Add("@time", SqlDbType.NVarChar).Value = DateTime.Now.ToShortTimeString();
            cmd.Parameters.Add("@date", SqlDbType.DateTime).Value = DateTime.Now;
            cmd.Parameters.Add("@purch", SqlDbType.Float).Value = purchPrice;
            cmd.Parameters.Add("@sale", SqlDbType.Float).Value = salePrice;
            cmd.Parameters.Add("@Emp", SqlDbType.Int).Value = MainClass.EmpNo;
            cmd.ExecuteNonQuery();
        }

        private string CreateNewCategory(string categoryName)
        {
            var maxCmd = new SqlCommand(
                "SELECT ISNULL(MAX(CategoryId), 0) + 1 FROM itemsCategory", _conn);
            double nextId = Convert.ToDouble(maxCmd.ExecuteScalar() ?? 1);

            var insertCmd = new SqlCommand(@"
                INSERT INTO itemsCategory
                    (CategoryId, name, nameEN, color, printer, ShowInPOS,
                     DispalyOrder, IS_Deleted, code, parentCode, type, id,
                     AllBranch, BranchId)
                VALUES
                    (@CategoyId, @name, @nameEN, -1, @Printers,
                     0, 0, 0, @code, 0, 2, @Id, 1, 1)", _conn);

            insertCmd.Parameters.Add("@CategoyId", SqlDbType.Int).Value = (int)nextId;
            insertCmd.Parameters.Add("@Printers", SqlDbType.NVarChar).Value = string.Empty;
            insertCmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = categoryName;
            insertCmd.Parameters.Add("@nameEN", SqlDbType.NVarChar).Value = string.Empty;
            insertCmd.Parameters.Add("@code", SqlDbType.NVarChar).Value = nextId.ToString();
            insertCmd.Parameters.Add("@Id", SqlDbType.NVarChar).Value = nextId.ToString();
            insertCmd.ExecuteNonQuery();

            return nextId.ToString();
        }

        private void InsertNewUnit(int unitId, string unitName)
        {
            var cmd = new SqlCommand(@"
                INSERT INTO units (id, name, defaultInv, UnitId, IS_Deleted, UnitCode)
                VALUES (@id, @name, @defaultInv, @UnitId, 0, @UnitCode)", _conn);

            cmd.Parameters.Add("@id", SqlDbType.Int).Value = unitId;
            cmd.Parameters.Add("@UnitId", SqlDbType.Int).Value = unitId;
            cmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = unitName;
            cmd.Parameters.Add("@UnitCode", SqlDbType.NVarChar).Value = unitId.ToString();
            cmd.Parameters.Add("@defaultInv", SqlDbType.NVarChar).Value = string.Empty;
            cmd.ExecuteNonQuery();
        }

        #endregion

        #region ── Insert: Sales Invoice ────────────────────────────────────

        public void InsertSalesInvoice()
        {
            if (cmbInv.SelectedIndex != 0 && cmbInv.SelectedIndex != 1) return;

            try
            {
                if (ConfirmImport() == MessageBoxResult.No) return;

                dtInvSel.Columns.Clear();
                dtInvSel.Columns.Add("ItemCode");
                dtInvSel.Columns.Add("description");
                dtInvSel.Columns.Add("unit");
                dtInvSel.Columns.Add("quntity");
                dtInvSel.Columns.Add("price");

                SetProgressMax(dgvData.Items.Count - 1);

                int rowCount = GetDataGridRowCount(dgvData);

                for (int i = 0; i < rowCount; i++)
                {
                    if (!GetRowIsSelected(0))
                    {
                        DXMessageBox.Show("يرجى اختيار عمود رمز الصنف");
                        return;
                    }

                    string itemCode = GetCellValueFromExcel(i, 0, out bool codeOk);
                    if (!codeOk)
                    {
                        DXMessageBox.Show($"يجب إدخال رمز الصنف في الصف {i + 1}");
                        return;
                    }

                    if (!IsItemExist(itemCode))
                    {
                        DXMessageBox.Show($"لا يوجد مادة بهذا الرمز في الصف {i + 1} - الرمز: {itemCode}");
                        return;
                    }

                    string description = GetMappedOrDefault(i, 1);
                    int unitId = SafeInt(GetMappedOrDefault(i, 2), 1);
                    double quantity = SafeDouble(GetMappedOrDefault(i, 3), 1.0);
                    double price = SafeDouble(GetMappedOrDefault(i, 4));

                    dtInvSel.Rows.Add(itemCode, description, unitId,
                                      quantity, Math.Round(price, 2));
                    IncrementProgress();
                }

                ISDone = true;
                GoToFinalTab();
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء استيراد الفاتورة", ex);
            }
        }

        #endregion

        #region ── Insert: Initial Restriction ──────────────────────────────

        private void InsertInitialRestriction()
        {
            if (cmbInv.SelectedIndex != 2 && cmbInv.SelectedIndex != 3) return;

            try
            {
                if (ConfirmImport() == MessageBoxResult.No) return;

                dtIniRes.Columns.Clear();
                dtIniRes.Columns.Add("AccCode");
                dtIniRes.Columns.Add("AccName");
                dtIniRes.Columns.Add("Dept");
                dtIniRes.Columns.Add("Credit");
                dtIniRes.Columns.Add("Note");

                SetProgressMax(dgvData.Items.Count - 1);

                int rowCount = GetDataGridRowCount(dgvData);

                for (int i = 0; i < rowCount; i++)
                {
                    string accCode = GetMappedOrDefault(i, 0);
                    if (GetRowIsSelected(0) && !IsAccountExist(accCode))
                    {
                        DXMessageBox.Show($"لا يوجد حساب بهذا الرمز في الصف {i + 1}");
                        return;
                    }

                    string accName = GetMappedOrDefault(i, 1);
                    double dept = SafeDouble(GetMappedOrDefault(i, 2));
                    double credit = SafeDouble(GetMappedOrDefault(i, 3));
                    string note = GetMappedOrDefault(i, 4);

                    dtIniRes.Rows.Add(accCode, accName,
                                      Math.Round(dept, 2), Math.Round(credit, 2), note);
                    IncrementProgress();
                }

                ISDone = true;
                GoToFinalTab();
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء استيراد القيد", ex);
            }
        }

        #endregion

        #region ── Insert: Safe Adjust ──────────────────────────────────────

        private void InsertSafeAdjust()
        {
            if (cmbInv.SelectedIndex != 4) return;

            try
            {
                if (ConfirmImport() == MessageBoxResult.No) return;

                dtIniRes.Columns.Clear();
                dtIniRes.Columns.Add("ItemCode");
                dtIniRes.Columns.Add("Unit");
                dtIniRes.Columns.Add("CurrentQty");
                dtIniRes.Columns.Add("Note");

                SetProgressMax(dgvData.Items.Count - 1);

                int rowCount = GetDataGridRowCount(dgvData);

                for (int i = 0; i < rowCount; i++)
                {
                    string itemCode = GetMappedOrDefault(i, 0);
                    if (GetRowIsSelected(0) && !IsItemExist(itemCode))
                    {
                        DXMessageBox.Show($"لا يوجد مادة بهذا الرمز في الصف {i + 1}");
                        return;
                    }

                    int unitId = SafeInt(GetMappedOrDefault(i, 1), 1);
                    double qty = SafeDouble(GetMappedOrDefault(i, 2));
                    string note = GetMappedOrDefault(i, 3);

                    dtIniRes.Rows.Add(itemCode, unitId, qty, note);
                    IncrementProgress();
                }

                ISDone = true;
                GoToFinalTab();
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء استيراد التسوية", ex);
            }
        }

        #endregion

        #region ── Database Helpers ─────────────────────────────────────────

        private int GetGroupIdByName(string name)
        {
            var adapter = new SqlDataAdapter(
                $"SELECT id FROM ItemsCategory WHERE name LIKE N'%{name}%'", _conn1);
            var table = new DataTable();
            adapter.Fill(table);
            return table.Rows.Count > 0 ? SafeInt(table.Rows[0][0]) : 0;
        }

        private int GetUnitIdByName(string name)
        {
            var adapter = new SqlDataAdapter(
                $"SELECT id FROM units WHERE name LIKE N'%{name}%'", _conn1);
            var table = new DataTable();
            adapter.Fill(table);
            return table.Rows.Count > 0 ? SafeInt(table.Rows[0][0]) : 0;
        }

        private int GetGroupId(string code)
        {
            var adapter = new SqlDataAdapter(
                $"SELECT id FROM ItemsCategory WHERE Code = '{code}'", _conn1);
            var table = new DataTable();
            adapter.Fill(table);
            return table.Rows.Count > 0 ? SafeInt(table.Rows[0][0]) : 0;
        }

        private bool IsItemExist(string code)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT id FROM Items WHERE IS_Deleted = 0 AND Code = N'{code}'", _conn);
                var table = new DataTable();
                adapter.Fill(table);
                return table.Rows.Count > 0;
            }
            catch { return false; }
        }

        private bool IsAccountExist(string code)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index WHERE type = 2 AND Code = {code}", _conn);
                var table = new DataTable();
                adapter.Fill(table);
                return table.Rows.Count > 0;
            }
            catch { return false; }
        }

        private bool IsCategoryNameExist(string name)
        {
            // يمكن تفعيلها لاحقًا
            return false;
        }

        private DataTable LoadTableData(string sql, SqlConnection conn)
        {
            var adapter = new SqlDataAdapter(sql, conn);
            var table = new DataTable();
            adapter.Fill(table);
            return table;
        }

        public int GetUnitId()
        {
            int unitId = 0;
            try
            {
                using var conn = MainClass.ConnObj();
                conn.Open();

                unitId = Convert.ToInt32(
                    new SqlCommand("SELECT COUNT(*) FROM units", conn).ExecuteScalar())
                    + MainClass.BranchNo * 100 + 1;

                bool isUnique;
                do
                {
                    double count = Convert.ToDouble(
                        new SqlCommand(
                            $"SELECT COUNT(*) FROM units WHERE id = {unitId}",
                            conn).ExecuteScalar());

                    isUnique = (count == 0.0);
                    if (!isUnique) unitId++;
                }
                while (!isUnique);
            }
            catch (Exception ex)
            {
                ShowError("خطأ في الحصول على معرف الوحدة", ex);
            }
            return unitId;
        }

        #endregion

        #region ── Mapping Helpers ──────────────────────────────────────────

        /// <summary>
        /// يُرجع القيمة من Excel للصف i بناءً على الفهرس المحدد في الـ mapping.
        /// </summary>
        private string GetCellValueFromExcel(int rowIndex, int mappingRowIndex, out bool hasValue)
        {
            hasValue = false;

            if (mappingRowIndex >= _mappingRows.Count) return string.Empty;

            var mapRow = _mappingRows[mappingRowIndex];

            // إذا كان المستخدم أدخل قيمة افتراضية
            if (mapRow.MappedColumnIndex < 0)
            {
                if (!string.IsNullOrEmpty(mapRow.DefaultValue))
                {
                    hasValue = true;
                    return mapRow.DefaultValue;
                }
                return string.Empty;
            }

            int colIndex = mapRow.MappedColumnIndex;
            if (_excelDataSet == null || cboSheet.SelectedIndex < 0) return string.Empty;

            int sheetIndex = cboSheet.SelectedIndex;
            if (sheetIndex >= _excelDataSet.Tables.Count) return string.Empty;

            DataTable sheet = _excelDataSet.Tables[sheetIndex];
            if (rowIndex >= sheet.Rows.Count || colIndex >= sheet.Columns.Count)
                return string.Empty;

            object cellVal = sheet.Rows[rowIndex][colIndex];
            if (cellVal == null || cellVal == DBNull.Value) return string.Empty;

            string result = cellVal.ToString() ?? string.Empty;
            hasValue = !string.IsNullOrEmpty(result);
            return result;
        }

        /// <summary>
        /// يُرجع القيمة المعيَّنة أو القيمة الافتراضية للصف mappingRowIndex.
        /// </summary>
        private string GetMappedOrDefault(int excelRowIndex, int mappingRowIndex)
        {
            if (!GetRowIsSelected(mappingRowIndex)) return string.Empty;
            return GetCellValueFromExcel(excelRowIndex, mappingRowIndex, out _);
        }

        private bool GetRowIsSelected(int mappingRowIndex)
        {
            if (mappingRowIndex >= _mappingRows.Count) return false;
            return _mappingRows[mappingRowIndex].IsSelected;
        }

        private int GetDataGridRowCount(DataGrid grid)
        {
            if (grid.ItemsSource is DataView dv) return dv.Count;
            return 0;
        }

        #endregion

        #region ── UI Helpers ───────────────────────────────────────────────

        private void GoToFinalTab()
        {
            _currentTabIndex = 4;
            TabControl1.SelectedIndex = _currentTabIndex;
            lblSuccess.Visibility = Visibility.Visible;
            btnImport.Visibility = Visibility.Collapsed;
            btnNext.Visibility = Visibility.Collapsed;
            btnPrevious.Visibility = Visibility.Collapsed;
        }

        private void SetProgressMax(int max)
        {
            ProgressBar1.Maximum = max > 0 ? max : 1;
            ProgressBar1.Value = 0;
        }

        private void IncrementProgress()
        {
            if (ProgressBar1.Value < ProgressBar1.Maximum)
                ProgressBar1.Value++;
        }

        private MessageBoxResult ConfirmImport()
        {
            return DXMessageBox.Show(
                "هل أنت متأكد من استيراد البيانات؟",
                "تأكيد الاستيراد",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
        }

        private void EnsureConnectionOpen(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
        }

        private void CloseConnection(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Closed)
                conn.Close();
        }

        private void ShowError(string title, Exception ex)
        {
            DXMessageBox.Show(
                $"{title}\nتفاصيل الخطأ: {ex.Message}",
                "خطأ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        #endregion

        #region ── Safe Converters ──────────────────────────────────────────

        private static int SafeInt(object value, int defaultVal = 0)
        {
            if (value == null || value == DBNull.Value) return defaultVal;
            return int.TryParse(value.ToString(), out int result) ? result : defaultVal;
        }

        private static double SafeDouble(object value, double defaultVal = 0.0)
        {
            if (value == null || value == DBNull.Value) return defaultVal;
            return double.TryParse(value.ToString(), out double result) ? result : defaultVal;
        }

        private static double SafeDouble(string value, double defaultVal = 0.0)
        {
            return double.TryParse(value, out double result) ? result : defaultVal;
        }

        private static int SafeInt(string value, int defaultVal = 0)
        {
            return int.TryParse(value, out int result) ? result : defaultVal;
        }

        #endregion
    }
}