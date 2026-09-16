using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmMeasurements : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private string connectionString;
        private int    currentCustID = 0;

        private ObservableCollection<MeasurementRow> _measurementRows;

        #endregion

        #region Constructor

        public frmMeasurements()
        {
            connectionString  = MainClass.connstr;
            _measurementRows  = new ObservableCollection<MeasurementRow>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmMeasurements_Load(object sender, RoutedEventArgs e)
        {
            gridMeasurements.ItemsSource = _measurementRows;
            UpdateSearchWatermark();
            LoadAllMeasurements();
        }

        private void LoadAllMeasurements()
        {
            try
            {
                string sql = @"
                    SELECT
                        cm.MeasurementID,
                        c.name          AS CustomerName,
                        cm.MeasurementName,
                        CONVERT(VARCHAR, cm.MeasurementDate, 103) AS MeasurementDate,
                        cm.Notes,
                        (SELECT COUNT(*) FROM MeasurementValues WHERE MeasurementID = cm.MeasurementID) AS MeasurementCount
                    FROM CustomerMeasurements cm
                    INNER JOIN Customers c ON c.id = cm.Cust_ID
                    WHERE cm.IsActive = 1
                    ORDER BY cm.MeasurementDate DESC";

                using SqlConnection conn = new SqlConnection(connectionString);
                using SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                FillMeasurementRows(dt);
            }
            catch (Exception ex)
            {
                ShowError("خطأ في التحميل", ex);
            }
        }

        private void LoadCustomerMeasurements()
        {
            try
            {
                string sql = @"
                    SELECT
                        MeasurementID,
                        NULL AS CustomerName,
                        MeasurementName,
                        CONVERT(VARCHAR, MeasurementDate, 103) AS MeasurementDate,
                        Notes,
                        (SELECT COUNT(*) FROM MeasurementValues WHERE MeasurementID = cm.MeasurementID) AS MeasurementCount
                    FROM CustomerMeasurements cm
                    WHERE Cust_ID = @CustID AND IsActive = 1
                    ORDER BY MeasurementDate DESC";

                using SqlConnection conn = new SqlConnection(connectionString);
                using SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                adapter.SelectCommand.Parameters.AddWithValue("@CustID", currentCustID);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                FillMeasurementRows(dt);
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل القياسات", ex);
            }
        }

        private void FillMeasurementRows(DataTable dt)
        {
            _measurementRows.Clear();

            foreach (DataRow row in dt.Rows)
            {
                _measurementRows.Add(new MeasurementRow
                {
                    MeasurementID   = row["MeasurementID"].ToString(),
                    CustomerName    = row["CustomerName"] == DBNull.Value ? "" : row["CustomerName"].ToString(),
                    MeasurementName = row["MeasurementName"].ToString(),
                    MeasurementDate = row["MeasurementDate"].ToString(),
                    Notes           = row["Notes"] == DBNull.Value ? "" : row["Notes"].ToString(),
                    MeasurementCount = row["MeasurementCount"].ToString()
                });
            }
        }

        #endregion

        #region Search

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchCustomer();
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                SearchCustomer();
        }

        private void SearchCustomer()
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                DXMessageBox.Show("الرجاء إدخال رقم الجوال أو اسم العميل", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string keyword = txtSearch.Text.Trim();

                string sql = @"
                    SELECT TOP 10 id, name, mobile
                    FROM Customers
                    WHERE mobile LIKE @Search OR name LIKE @Search
                    ORDER BY name";

                using SqlConnection conn = new SqlConnection(connectionString);
                using SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                adapter.SelectCommand.Parameters.AddWithValue("@Search", "%" + keyword + "%");

                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    DXMessageBox.Show("لم يتم العثور على عميل", "نتيجة البحث",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearCustomerInfo();
                }
                else
                {
                    SelectCustomer(dt.Rows[0]);
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في البحث", ex);
            }
        }

        private void SelectCustomer(DataRow row)
        {
            currentCustID = Convert.ToInt32(row["id"]);
            lblCustomerName.Text  = "العميل: " + row["name"].ToString();
            lblCustomerPhone.Text = "الجوال: " + row["mobile"].ToString();
            pnlCustomerInfo.Visibility = Visibility.Visible;
            LoadCustomerMeasurements();
        }

        private void ClearCustomerInfo()
        {
            currentCustID = 0;
            lblCustomerName.Text   = "";
            lblCustomerPhone.Text  = "";
            pnlCustomerInfo.Visibility = Visibility.Collapsed;
            _measurementRows.Clear();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSearchWatermark();
        }

        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdateSearchWatermark();
        }

        private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateSearchWatermark();
        }

        private void UpdateSearchWatermark()
        {
            if (txtSearchWatermark == null || txtSearch == null) return;

            txtSearchWatermark.Visibility =
                string.IsNullOrWhiteSpace(txtSearch.Text) && !txtSearch.IsKeyboardFocused
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        #endregion

        #region Grid Buttons

        private void btnAddMeasurement_Click(object sender, RoutedEventArgs e)
        {
            if (currentCustID == 0)
            {
                DXMessageBox.Show("الرجاء البحث عن عميل أولًا", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            frmMeasurementDetails detailsForm = new frmMeasurementDetails
            {
                Cust_ID    = currentCustID,
                IsNewRecord = true
            };

            if (detailsForm.ShowDialog() == true)
                LoadCustomerMeasurements();
        }

        private void btnEditMeasurement_Click(object sender, RoutedEventArgs e)
        {
            MeasurementRow selected = gridMeasurements.SelectedItem as MeasurementRow;
            if (selected == null)
            {
                DXMessageBox.Show("الرجاء اختيار قياس للتعديل", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int measurementId = 0;
            int.TryParse(selected.MeasurementID, out measurementId);

            frmMeasurementDetails detailsForm = new frmMeasurementDetails
            {
                Cust_ID       = currentCustID,
                MeasurementID = measurementId,
                IsNewRecord   = false
            };

            if (detailsForm.ShowDialog() == true)
                LoadCustomerMeasurements();
        }

        private void btnDeleteMeasurement_Click(object sender, RoutedEventArgs e)
        {
            MeasurementRow selected = gridMeasurements.SelectedItem as MeasurementRow;
            if (selected == null)
            {
                DXMessageBox.Show("الرجاء اختيار قياس للحذف", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DXMessageBox.Show("هل أنت متأكد من حذف هذا القياس؟",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.No) return;

            try
            {
                int measurementId = 0;
                int.TryParse(selected.MeasurementID, out measurementId);

                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();

                using SqlCommand cmd = new SqlCommand("sp_DeleteMeasurement", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@MeasurementID", measurementId);
                cmd.ExecuteNonQuery();

                DXMessageBox.Show("تم الحذف بنجاح", "نجح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadCustomerMeasurements();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في الحذف", ex);
            }
        }

        private void gridViewMeasurements_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            btnEditMeasurement_Click(null, null);
        }

        #endregion

        #region Close

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Helpers

        private void ShowError(string context, Exception ex)
        {
            DXMessageBox.Show(context + Environment.NewLine + "خطأ: " + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }

    public class MeasurementRow
    {
        public string MeasurementID    { get; set; }
        public string CustomerName     { get; set; }
        public string MeasurementName  { get; set; }
        public string MeasurementDate  { get; set; }
        public string Notes            { get; set; }
        public string MeasurementCount { get; set; }
    }
}