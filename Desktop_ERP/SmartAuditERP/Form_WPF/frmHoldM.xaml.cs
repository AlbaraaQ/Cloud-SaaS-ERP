using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmHoldM : ThemedWindow
    {
        #region ── Fields ──────────────────────────────────────────────────

        private SqlConnection _conn;

        // معرفات السجل الحالي
        private int _invoiceNo;
        private int _recordCode;
        private int _stockId;
        private int _customerId;
        private int _restrictionId;
        private string _entryGlobalId;

        // قيم مالية
        private int _netValue;
        private int _paymentType;
        private int _bankAmount;
        private int _cashAmount;
        private int _visaAmount;

        // بيانات مساعدة
        private string _clientName;

        // للتنقل بين السجلات
        private int _currentRowIndex = -1;

        // علم لمنع التكرار عند تغيير cmbMarine و txtMarineCode
        private bool _isUpdatingMarineSelection = false;

        #endregion

        #region ── Constructor ─────────────────────────────────────────────

        public frmHoldM()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            _invoiceNo = -1;
            _recordCode = -1;
            _stockId = -1;
            _customerId = -1;
            _restrictionId = -1;
            _entryGlobalId = string.Empty;
            _netValue = 0;
            _paymentType = 1;
            _bankAmount = -1;
            _cashAmount = 0;
            _visaAmount = 0;
            _clientName = string.Empty;
        }

        #endregion

        #region ── Window Events ───────────────────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadGroup();
            // ضبط حالة النافذة حسب إعدادات النظام
            if (MainClass.Window_State == WindowState.Maximized)
                this.WindowState = WindowState.Maximized;
        }

        #endregion

        #region ── Data Loading ────────────────────────────────────────────

        /// <summary>
        /// تحميل الفواتير في الجدول بناءً على شرط SQL إضافي.
        /// </summary>
        /// <param name="additionalCondition">
        ///   جزء من WHERE يُضاف قبل شرط العميل، مثال:
        ///   "RentInvoice.GroupId=5 and" أو فارغ للكل.
        /// </param>
        private void LoadInvoicesGrid(string additionalCondition)
        {
            try
            {
                dgvSrch.ItemsSource = null;

                string sql = $@"
                    SELECT RentInvoice.proc_id,
                           RentInvoice.id   AS id,
                           RentInvoice.date AS date,
                           Customers.name   AS cust
                    FROM   RentInvoice, Customers
                    WHERE  (RentInvoice.proc_type = 3 OR RentInvoice.MarineId = 0)
                      AND  RentInvoice.IS_Deleted  = 0
                      AND  {additionalCondition}
                           RentInvoice.cust_id = Customers.id
                    ORDER BY RentInvoice.id";

                var adapter = new SqlDataAdapter(sql, _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                // تحويل التاريخ إلى نص قصير
                foreach (DataRow row in dataTable.Rows)
                {
                    if (row["date"] != DBNull.Value)
                        row["date"] = Convert.ToDateTime(row["date"]).ToShortDateString();
                }

                dgvSrch.ItemsSource = dataTable.DefaultView;
                dgvSrch.UnselectAll();
                _currentRowIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل البيانات", ex);
            }
        }

        /// <summary>
        /// تحميل مجموعات المراكب في cmbGroup.
        /// </summary>
        private void LoadGroup()
        {
            try
            {
                string sql = "SELECT id, name, code FROM GroupMarine WHERE IsDeleted = 0 ORDER BY id";
                var adapter = new SqlDataAdapter(sql, _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    cmbGroup.DisplayMemberPath = "code";
                    cmbGroup.SelectedValuePath = "id";
                    cmbGroup.ItemsSource = dataTable.DefaultView;
                    cmbGroup.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل الفئات", ex);
            }
        }

        /// <summary>
        /// تحميل المراكب في cmbMarine بناءً على الفئة المختارة.
        /// </summary>
        private void LoadMarine()
        {
            try
            {
                // نأخذ النص المعروض من cmbGroup (الكود)
                string groupCode = (cmbGroup.SelectedItem as DataRowView)?["code"]?.ToString()
                                   ?? string.Empty;

                string sql = $@"
            SELECT id, Name, MarineCode
            FROM   Marine
            WHERE  IS_Deleted = 0
              AND  Groupcode  = '{groupCode}'";

                var adapter = new SqlDataAdapter(sql, _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                cmbMarine.DisplayMemberPath = "Name";
                cmbMarine.SelectedValuePath = "id";

                // ✅ الإصلاح: تعيين مباشر بدون ternary مشكل
                if (dataTable.Rows.Count > 0)
                    cmbMarine.ItemsSource = dataTable.DefaultView;
                else
                    cmbMarine.ItemsSource = null;

                cmbMarine.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل المراكب", ex);
            }
        }

        #endregion

        #region ── Record Navigation ────────────────────────────────────────

        /// <summary>
        /// قراءة بيانات سجل من قاعدة البيانات وعرضها في الحقول.
        /// </summary>
        public void Navigate(string sqlQuery)
        {
            try
            {
                dgvSrch.UnselectAll();

                if (_conn.State != ConnectionState.Open)
                    _conn.Open();

                using (var cmd = new SqlCommand(sqlQuery, _conn))
                using (var reader = cmd.ExecuteReader())
                {
                    ReadDataFromReader(reader);
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في التنقل", ex);
            }
            finally
            {
                if (_conn.State != ConnectionState.Closed)
                    _conn.Close();
            }
        }

        private void ReadDataFromReader(SqlDataReader reader)
        {
            if (!reader.HasRows) return;

            try
            {
                reader.Read();

                _invoiceNo = SafeInt(reader["id"]);
                _recordCode = SafeInt(reader["proc_id"]);
                _stockId = SafeInt(reader["stock"]);
                _restrictionId = SafeInt(reader["Restraction_id"]);
                _customerId = SafeInt(reader["cust_id"]);

                double totalRent = SafeDouble(reader["tot_Rent"]);
                double discount = SafeDouble(reader["Discount"]);
                _netValue = (int)Math.Round(totalRent - discount);
                _paymentType = SafeInt(reader["pay_type"]);
                _bankAmount = SafeInt(reader["bank"]);
                _cashAmount = SafeInt(reader["cash"]);
                _visaAmount = SafeInt(reader["visa"]);

                // تعبئة الحقول
                txtNo.Text = _invoiceNo.ToString();

                string rawDate = reader["date"]?.ToString() ?? string.Empty;
                if (DateTime.TryParse(rawDate, out DateTime parsedDate))
                {
                    txtDate.SelectedDate = parsedDate;
                    txtTime.Text = parsedDate.ToString("HH:mm:ss");
                }
                else
                {
                    txtDate.SelectedDate = null;
                    txtTime.Text = string.Empty;
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في قراءة بيانات السجل", ex);
            }
        }

        /// <summary>
        /// تحميل سجل حسب فهرس الصف في الجدول.
        /// </summary>
        private void LoadRowByIndex(int rowIndex)
        {
            try
            {
                var view = dgvSrch.ItemsSource as DataView;
                if (view == null || rowIndex < 0 || rowIndex >= view.Count) return;

                DataRowView row = view[rowIndex];
                _invoiceNo = SafeInt(row["proc_id"]);
                _clientName = row["cust"]?.ToString() ?? string.Empty;
                _currentRowIndex = rowIndex;

                Navigate($"SELECT * FROM RentInvoice WHERE proc_id = {_invoiceNo}");

                // تحديد الصف في الجدول
                dgvSrch.SelectedIndex = rowIndex;
                dgvSrch.ScrollIntoView(dgvSrch.SelectedItem);
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحديد الصف", ex);
            }
        }

        private int GetGridRowCount()
        {
            var view = dgvSrch.ItemsSource as DataView;
            return view?.Count ?? 0;
        }

        #endregion

        #region ── Clear / Reset ────────────────────────────────────────────

        private void ClearFields()
        {
            _invoiceNo = -1;
            _recordCode = -1;
            _stockId = -1;
            _customerId = -1;
            _netValue = 0;
            _paymentType = 1;
            _bankAmount = -1;
            _cashAmount = 0;
            _visaAmount = 0;
            _restrictionId = -1;
            _entryGlobalId = string.Empty;
            _currentRowIndex = -1;

            cmbMarine.SelectedIndex = -1;
            txtMarineCode.Text = string.Empty;
            txtNo.Text = string.Empty;
            txtDate.SelectedDate = null;
            txtTime.Text = string.Empty;

            dgvSrch.ItemsSource = null;
        }

        #endregion

        #region ── Save Logic ───────────────────────────────────────────────

        /// <summary>
        /// حفظ ربط المركب بالفاتورة مع إدخالات دفترية.
        /// </summary>
        public void SaveInvoiceLink()
        {
            SqlTransaction transaction = null;
            try
            {
                int ownerAccountCode = 0;
                int ownerSharePercent = 0;
                int marineId = SafeInt(cmbMarine.SelectedValue);

                // جلب بيانات المالك من المركب
                string marineSql = $@"
                    SELECT OwnerId, OwnerPercent
                    FROM   Marine
                    WHERE  IS_Deleted = 0 AND id = {marineId}";

                var marineAdapter = new SqlDataAdapter(marineSql, _conn);
                var marineTable = new DataTable();
                marineAdapter.Fill(marineTable);

                if (marineTable.Rows.Count > 0)
                {
                    int ownerId = SafeInt(marineTable.Rows[0]["OwnerId"]);
                    ownerSharePercent = SafeInt(marineTable.Rows[0]["OwnerPercent"]);
                    ownerAccountCode = GetOwnerAccountCode(ownerId);
                }

                if (_conn.State != ConnectionState.Open)
                    _conn.Open();

                transaction = _conn.BeginTransaction();

                // تحديث الفاتورة بربطها بالمركب
                string updateSql = $@"
                    UPDATE RentInvoice
                    SET    proc_type = 1,
                           MarineId  = {marineId}
                    WHERE  proc_id   = {_recordCode}
                      AND  IS_Deleted = 0";

                new SqlCommand(updateSql, _conn, transaction).ExecuteNonQuery();

                // حساب نصيب المالك
                double ownerShareAmount = _netValue * (ownerSharePercent / 100.0);

                // إدخال قيود محاسبية إذا لم تكن النسبة 100%
                if (ownerSharePercent < 100 && ownerAccountCode > 0)
                {
                    string entryRef = $"{MainClass.BranchNo}-{_restrictionId}";
                    string entryNote = $"فاتورة تأجير رقم: {_recordCode}";

                    // القيد المدين
                    InsertAccountingEntry(
                        transaction,
                        entryRef,
                        _restrictionId,
                        dept: ownerShareAmount,
                        credit: 0,
                        accNo: 4200003,
                        note: entryNote);

                    // القيد الدائن
                    InsertAccountingEntry(
                        transaction,
                        entryRef,
                        _restrictionId,
                        dept: 0,
                        credit: ownerShareAmount,
                        accNo: ownerAccountCode,
                        note: entryNote);
                }

                transaction.Commit();

                // تسجيل خروج المركب في سجلات الحضور
                if (marineId != 0)
                    RecordMarineExit(marineId);
            }
            catch (Exception ex)
            {
                transaction?.Rollback();

                string errorMessage = MainClass.Language == "en"
                    ? $"Error in saving\nError details: {ex.Message}"
                    : $"خطأ أثناء الحفظ\nتفاصيل الخطأ: {ex.Message}";

                DXMessageBox.Show(errorMessage, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (_conn.State != ConnectionState.Closed)
                    _conn.Close();
            }
        }

        private void InsertAccountingEntry(
            SqlTransaction transaction,
            string entryGlobalId,
            int restrictionId,
            double dept,
            double credit,
            int accNo,
            string note)
        {
            var cmd = new SqlCommand(StoredQueries.InsertEntrySub, _conn, transaction);
            cmd.Parameters.Add("@EntryGlobalID", SqlDbType.NVarChar).Value = entryGlobalId;
            cmd.Parameters.Add("@res_id", SqlDbType.Int).Value = restrictionId;
            cmd.Parameters.Add("@dept", SqlDbType.Float).Value = dept;
            cmd.Parameters.Add("@CCcode", SqlDbType.Int).Value = -1;
            cmd.Parameters.Add("@credit", SqlDbType.Float).Value = credit;
            cmd.Parameters.Add("@acc_no", SqlDbType.Int).Value = accNo;
            cmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value = note;
            cmd.Parameters.Add("@branch", SqlDbType.Int).Value = MainClass.BranchNo;
            cmd.ExecuteNonQuery();
        }

        private int GetOwnerAccountCode(int ownerId)
        {
            try
            {
                string sql = $@"
                    SELECT AccountCode
                    FROM   Owners
                    WHERE  type = 1 AND id = {ownerId}";

                var adapter = new SqlDataAdapter(sql, _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                return dataTable.Rows.Count > 0 ? SafeInt(dataTable.Rows[0][0]) : -1;
            }
            catch
            {
                return -1;
            }
        }

        private void RecordMarineExit(int marineId)
        {
            SqlTransaction transaction = null;
            try
            {
                if (_conn.State != ConnectionState.Open)
                    _conn.Open();

                transaction = _conn.BeginTransaction();

                DateTime selectedDate = txtDate.SelectedDate ?? DateTime.Today;

                string sql = $@"
                    UPDATE Attendance
                    SET    _inOutMode  = 1,
                           takeJoureny = 1
                    WHERE  _enrollNumber = {marineId}
                      AND  _year        = {selectedDate.Year}
                      AND  _month       = {selectedDate.Month}
                      AND  _day         = {selectedDate.Day}";

                new SqlCommand(sql, _conn, transaction).ExecuteNonQuery();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                ShowError("خطأ في تسجيل خروج المركب", ex);
            }
            finally
            {
                if (_conn.State != ConnectionState.Closed)
                    _conn.Close();
            }
        }

        #endregion

        #region ── Button Events ────────────────────────────────────────────

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            ClearFields();
            _clientName = string.Empty;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtNo.Text))
                {
                    DXMessageBox.Show("يجب تحديد فاتورة", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (cmbMarine.SelectedIndex == -1)
                {
                    DXMessageBox.Show("يجب تحديد المركب", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var result = DXMessageBox.Show(
                    "هل أنت متأكد من ربط المركب بالفاتورة؟",
                    "تأكيد",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SaveInvoiceLink();
                    DXMessageBox.Show("✅ تم ربط الفاتورة بنجاح", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadInvoicesGrid(string.Empty);
                    ClearFields();
                    _clientName = string.Empty;
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

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            // يمكن إضافة منطق الحذف هنا حسب متطلبات النظام
            DXMessageBox.Show("⚠️ وظيفة الحذف غير مفعّلة في هذه الشاشة",
                            "تنبيه",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            // يمكن إضافة منطق الطباعة هنا
            DXMessageBox.Show("🖨️ وظيفة الطباعة ستُنفَّذ قريبًا",
                            "طباعة",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region ── Navigation Button Events ─────────────────────────────────

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            int count = GetGridRowCount();
            if (count > 0) LoadRowByIndex(0);
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            int count = GetGridRowCount();
            if (count > 0) LoadRowByIndex(count - 1);
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            int count = GetGridRowCount();
            if (_currentRowIndex < count - 1)
                LoadRowByIndex(_currentRowIndex + 1);
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRowIndex > 0)
                LoadRowByIndex(_currentRowIndex - 1);
        }

        #endregion

        #region ── DataGrid Events ──────────────────────────────────────────

        private void dgvSrch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            int selectedIndex = dgvSrch.SelectedIndex;
            if (selectedIndex >= 0)
            {
                _currentRowIndex = selectedIndex;
                LoadRowByIndex(selectedIndex);
                txtMarineCode.Text = string.Empty;
            }
        }

        #endregion

        #region ── ComboBox & TextBox Events ────────────────────────────────

        private void cmbGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbGroup.SelectedIndex < 0) return;

                ClearFields();

                // جلب id الفئة
                object groupIdValue = (cmbGroup.SelectedItem as DataRowView)?["id"];
                if (groupIdValue == null) return;

                string condition = $"RentInvoice.GroupId = {groupIdValue} AND ";
                LoadInvoicesGrid(condition);
                LoadMarine();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تغيير الفئة", ex);
            }
        }

        private void cmbMarine_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingMarineSelection) return;

            try
            {
                if (cmbMarine.SelectedIndex < 0 || cmbMarine.SelectedValue == null) return;

                string sql = $@"
                    SELECT MarineCode
                    FROM   Marine
                    WHERE  IS_Deleted = 0 AND id = {cmbMarine.SelectedValue}";

                var adapter = new SqlDataAdapter(sql, _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    _isUpdatingMarineSelection = true;
                    txtMarineCode.Text = dataTable.Rows[0][0]?.ToString() ?? string.Empty;
                    _isUpdatingMarineSelection = false;
                }
            }
            catch (Exception ex)
            {
                _isUpdatingMarineSelection = false;
                ShowError("خطأ في اختيار المركب", ex);
            }
        }

        private void txtMarineCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingMarineSelection) return;

            try
            {
                string codeText = txtMarineCode.Text.Trim();
                if (string.IsNullOrEmpty(codeText))
                {
                    cmbMarine.SelectedIndex = -1;
                    return;
                }

                string sql = $@"
                    SELECT id
                    FROM   Marine
                    WHERE  IS_Deleted = 0 AND MarineCode = '{codeText}'";

                var adapter = new SqlDataAdapter(sql, _conn);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    _isUpdatingMarineSelection = true;
                    cmbMarine.SelectedValue = dataTable.Rows[0][0];
                    _isUpdatingMarineSelection = false;
                }
            }
            catch (Exception ex)
            {
                _isUpdatingMarineSelection = false;
                ShowError("خطأ في البحث بكود المركب", ex);
            }
        }

        private void txtNo_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                string noText = txtNo.Text.Trim();
                if (string.IsNullOrEmpty(noText)) return;

                if (double.TryParse(noText, out double invoiceNumber) && invoiceNumber > 0)
                {
                    string condition = $"RentInvoice.id = {(int)invoiceNumber} AND ";
                    LoadInvoicesGrid(condition);
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في البحث برقم الفاتورة", ex);
            }
        }

        #endregion

        #region ── Helpers ──────────────────────────────────────────────────

        private static int SafeInt(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            return (int)Math.Round(Convert.ToDouble(value.ToString()));
        }

        private static double SafeDouble(object value)
        {
            if (value == null || value == DBNull.Value) return 0.0;
            return Convert.ToDouble(value.ToString());
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
    }
}