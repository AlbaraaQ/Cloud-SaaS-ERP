using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using DevExpress.Xpf.Core;
using UtilitiesProj;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmCustomizeShow : ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        private SqlConnection _conn1;
        private SqlCommand _cmd;
        private int _tableNo;

        public int ClientId;
        public bool ISDone;

        #endregion

        #region Constructor

        public frmCustomizeShow()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            _conn1 = MainClass.ConnObj();
            _cmd = new SqlCommand();
            _tableNo = 0;
            ClientId = -1;
            ISDone = false;

            Loaded += FrmCustomizeShow_Loaded;
        }

        #endregion

        #region Window Events

        private void FrmCustomizeShow_Loaded(object sender, RoutedEventArgs e)
        {
            cmbInv.EditValueChanged += CmbInv_EditValueChanged;
            btnSave.Click += BtnSave_Click;
            btnDefault.Click += BtnDefault_Click;
        }

        #endregion

        #region ComboBox Event

        private void CmbInv_EditValueChanged(
            object sender,
            DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            int selectedIndex = cmbInv.SelectedIndex;

            if (selectedIndex < 0)
                return;

            ClearGrid();

            // حساب رقم الجدول
            int tableId = selectedIndex + 1;
            if (selectedIndex == 3)
                tableId = 9;

            // إظهار خيار إخفاء الأصناف فقط لنقطة البيع
            GpItemsGp.Visibility = tableId == 3
                ? Visibility.Visible
                : Visibility.Collapsed;

            LoadGridColumns(tableId);

            // إذا لم توجد بيانات، حمّل من نوع 2 كافتراضي
            if (GetGridRowCount() < 1)
                LoadGridColumns(2);
        }

        #endregion

        #region Data Loading

        private void LoadGridColumns(int tableId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT * FROM CustomizedDGV WHERE Form_id={tableId}",
                    _conn);

                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count == 0)
                    return;

                var resultTable = BuildResultTable();

                foreach (DataRow row in dataTable.Rows)
                {
                    // الصف الخاص بإخفاء لوحة الأصناف (Column_id = 100)
                    if (Convert.ToInt32(row["Column_id"]) == 100)
                    {
                        if (!Convert.ToBoolean(row["IS_Visible"]))
                            ckHideItemsPanel.IsChecked = true;
                        continue;
                    }

                    resultTable.Rows.Add(
                        Convert.ToBoolean(row["IS_Visible"]),
                        row["col_name"].ToString(),
                        row["col_nameEn"].ToString(),
                        row["Width"],
                        Convert.ToBoolean(row["IS_filled"])
                    );
                }

                dgvColumns.ItemsSource = resultTable.DefaultView;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        /// <summary>
        /// ينشئ جدول نتائج بالأعمدة المطلوبة
        /// </summary>
        private DataTable BuildResultTable()
        {
            var table = new DataTable();
            table.Columns.Add("IsVisible", typeof(bool));
            table.Columns.Add("ColNameAr", typeof(string));
            table.Columns.Add("ColNameEn", typeof(string));
            table.Columns.Add("ColWidth", typeof(object));
            table.Columns.Add("IsFilled", typeof(bool));
            return table;
        }

        private void ClearGrid()
        {
            dgvColumns.ItemsSource = null;
            ckHideItemsPanel.IsChecked = false;
        }

        private int GetGridRowCount()
        {
            if (dgvColumns.ItemsSource is DataView dv)
                return dv.Count;
            return 0;
        }

        #endregion

        #region Save

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var confirmResult = DXMessageBox.Show(
                "هل أنت متأكد من الحفظ؟",
                "تأكيد",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmResult != MessageBoxResult.Yes)
                return;

            if (cmbInv.SelectedIndex < 0)
            {
                DXMessageBox.Show("يجب اختيار نوع الشاشة",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_conn1.State != System.Data.ConnectionState.Open)
                _conn1.Open();

            var transaction = _conn1.BeginTransaction();

            try
            {
                int tableId = cmbInv.SelectedIndex + 1;
                if (cmbInv.SelectedIndex == 3)
                    tableId = 9;

                // حذف البيانات القديمة
                _cmd = new SqlCommand(
                    $"DELETE FROM CustomizedDGV " +
                    $"WHERE form_id={tableId} AND user_id={MainClass.EmpNo}",
                    _conn1, transaction);
                _cmd.ExecuteNonQuery();

                // إدراج البيانات الجديدة
                var dataView = dgvColumns.ItemsSource as DataView;
                if (dataView != null)
                {
                    for (int i = 0; i < dataView.Count; i++)
                    {
                        DataRowView row = dataView[i];

                        bool isVisible = Convert.ToBoolean(row["IsVisible"]);
                        bool isFilled = Convert.ToBoolean(row["IsFilled"]);
                        string nameAr = row["ColNameAr"]?.ToString() ?? string.Empty;
                        string nameEn = row["ColNameEn"]?.ToString() ?? string.Empty;
                        object width = row["ColWidth"];

                        _cmd = new SqlCommand(
                            $"INSERT INTO CustomizedDGV" +
                            $"(user_id,Form_id,Column_id,IS_Visible,Width," +
                            $"col_name,col_nameEn,IS_filled) " +
                            $"VALUES({MainClass.EmpNo},{tableId},{i}," +
                            $"{Convert.ToInt16(isVisible)},{width}," +
                            $"N'{nameAr}',N'{nameEn}',{Convert.ToInt16(isFilled)})",
                            _conn1, transaction);
                        _cmd.ExecuteNonQuery();
                    }
                }

                // إدراج صف إخفاء لوحة الأصناف إذا لزم
                if (tableId == 3 && ckHideItemsPanel.IsChecked == true)
                {
                    _cmd = new SqlCommand(
                        $"INSERT INTO CustomizedDGV" +
                        $"(user_id,Form_id,Column_id,IS_Visible,Width," +
                        $"col_name,col_nameEn,IS_filled) " +
                        $"VALUES({MainClass.EmpNo},{tableId},100,0,0,0,0,0)",
                        _conn1, transaction);
                    _cmd.ExecuteNonQuery();
                }

                transaction.Commit();

                var applyResult = DXMessageBox.Show(
                    "✅ تم حفظ المظهر، هل تريد تنفيذه؟",
                    "نجاح",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (applyResult == MessageBoxResult.Yes)
                {
                    ISDone = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                string errorMsg = MainClass.Language == "ar"
                    ? $"خطأ أثناء الحفظ\nتفاصيل الخطأ: {ex.Message}"
                    : $"Error in saving\nError details: {ex.Message}";

                ShowError(errorMsg);
            }
            finally
            {
                if (_conn.State != System.Data.ConnectionState.Closed)
                    _conn.Close();
            }
        }

        #endregion

        #region Reset Default

        private void BtnDefault_Click(object sender, RoutedEventArgs e)
        {
            var confirmResult = DXMessageBox.Show(
                "هل أنت متأكد من إعادة المظهر الافتراضي؟",
                "تأكيد",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmResult != MessageBoxResult.Yes)
                return;

            if (cmbInv.SelectedIndex < 0)
            {
                DXMessageBox.Show("يجب اختيار نوع الشاشة",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int tableId = cmbInv.SelectedIndex + 1;

                if (_conn1.State != System.Data.ConnectionState.Open)
                    _conn1.Open();

                _cmd = new SqlCommand(
                    $"DELETE FROM CustomizedDGV " +
                    $"WHERE form_id={tableId} AND user_id={MainClass.EmpNo}",
                    _conn1);
                _cmd.ExecuteNonQuery();

                // إعادة تحميل الجدول
                ClearGrid();
                LoadGridColumns(tableId);

                DXMessageBox.Show("✅ تم إعادة المظهر الافتراضي بنجاح",
                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                if (_conn1.State != System.Data.ConnectionState.Closed)
                    _conn1.Close();
            }
        }

        #endregion

        #region Utility

        private void ShowError(string message)
        {
            DXMessageBox.Show(message, "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}