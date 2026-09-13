using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    #region Addition Model
    public class Addition
    {
        public int AdditionId { get; set; }
        public string AdditionName { get; set; }
        public double SalePrice { get; set; }
    }
    #endregion

    public partial class frmAdditions : Window
    {
        #region Fields
        private SqlConnection _dbConnection;
        private int _selectedRowIndex;
        private int _currentAdditionId;
        private ObservableCollection<Addition> _additions;
        public bool IsDone { get; set; }
        #endregion

        #region Constructor
        public frmAdditions()
        {
            InitializeComponent();
            _dbConnection = MainClass.ConnObj();
            _selectedRowIndex = -1;
            _currentAdditionId = -1;
            IsDone = false;
            _additions = new ObservableCollection<Addition>();
            dgvData.ItemsSource = _additions;

            Loaded += FrmAdditions_Loaded;
        }
        #endregion

        #region Form Events
        private void FrmAdditions_Loaded(object sender, RoutedEventArgs e)
        {
            LoadNextAdditionNumber();
            LoadAdditionsGrid();
        }
        #endregion

        #region Data Loading Methods
        private void LoadNextAdditionNumber()
        {
            txtNo.Text = "";
            int nextNumber = 1;

            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT id FROM Additions WHERE IsDeleted=0", _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        nextNumber = dataTable.Rows.Count + 1;
                    }
                }

                txtNo.Text = nextNumber.ToString();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل الرقم التالي", ex);
            }
        }

        private void LoadAdditionsGrid()
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT * FROM Additions WHERE IsDeleted=0 ORDER BY id", _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    _additions.Clear();

                    if (dataTable.Rows.Count > 0)
                    {
                        foreach (DataRow row in dataTable.Rows)
                        {
                            _additions.Add(new Addition
                            {
                                AdditionId = Convert.ToInt32(row["id"]),
                                AdditionName = row["name"].ToString(),
                                SalePrice = Convert.ToDouble(row["SalePrice"])
                            });
                        }
                    }

                    dgvData.SelectedItem = null;
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل البيانات", ex);
            }
        }
        #endregion

        #region DataGrid Events
        private void dgvData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvData.SelectedItem == null) return;

            Addition selectedAddition = (Addition)dgvData.SelectedItem;
            _selectedRowIndex = dgvData.SelectedIndex;
            _currentAdditionId = selectedAddition.AdditionId;

            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT * FROM Additions WHERE id={_currentAdditionId}", _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        DataRow row = dataTable.Rows[0];
                        txtNo.Text = row["id"].ToString();
                        txtName.Text = row["name"].ToString();
                        txtSalePrice.Text = row["SalePrice"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل البيانات المحددة", ex);
            }
        }
        #endregion

        #region Button Click Events
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveAddition();
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            ClearAllFields();
            LoadAdditionsGrid();
            LoadNextAdditionNumber();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteAddition();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        #endregion

        #region Save Method
        private void SaveAddition()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtNo.Text.Trim()))
                {
                    return;
                }

                bool isNewAddition = true;

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT id FROM Additions WHERE IsDeleted=0 AND id={Convert.ToInt32(txtNo.Text)}",
                    _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        isNewAddition = false;
                    }
                }

                if (_dbConnection.State != ConnectionState.Open)
                {
                    _dbConnection.Open();
                }

                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("يجب إدخال اسم الإضافة ⚠️", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtSalePrice.Text))
                {
                    txtSalePrice.Text = "0";
                }

                double salePrice = Convert.ToDouble(txtSalePrice.Text);

                if (isNewAddition)
                {
                    using (SqlCommand command = new SqlCommand(
                        "INSERT INTO Additions(name, SalePrice, IsDeleted) VALUES(@name, @SalePrice, @IsDeleted)",
                        _dbConnection))
                    {
                        command.Parameters.AddWithValue("@name", txtName.Text);
                        command.Parameters.AddWithValue("@SalePrice", salePrice);
                        command.Parameters.AddWithValue("@IsDeleted", 0);
                        command.ExecuteNonQuery();
                    }

                    IsDone = true;
                }
                else
                {
                    int additionId = Convert.ToInt32(txtNo.Text);
                    using (SqlCommand command = new SqlCommand(
                        $"UPDATE Additions SET name=@name, SalePrice=@SalePrice WHERE id={additionId}",
                        _dbConnection))
                    {
                        command.Parameters.AddWithValue("@name", txtName.Text);
                        command.Parameters.AddWithValue("@SalePrice", salePrice);
                        command.ExecuteNonQuery();
                    }
                }

                _dbConnection.Close();

                LoadAdditionsGrid();

                string successMessage = isNewAddition
                    ? "✅ تم الحفظ بنجاح"
                    : "✅ تم حفظ التعديلات بنجاح";

                MessageBox.Show(successMessage, "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                ClearAllFields();
                LoadNextAdditionNumber();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ أثناء الحفظ", ex);
            }
            finally
            {
                if (_dbConnection.State != ConnectionState.Closed)
                {
                    _dbConnection.Close();
                }
            }
        }
        #endregion

        #region Delete Method
        private void DeleteAddition()
        {
            try
            {
                if (_dbConnection.State != ConnectionState.Open)
                {
                    _dbConnection.Open();
                }

                if (dgvData.SelectedItem == null)
                {
                    MessageBox.Show("يجب تحديد الإضافة المراد حذفها ⚠️", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBoxResult result = MessageBox.Show(
                    "هل أنت متأكد من حذف هذه الإضافة؟ 🗑️",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    int additionId = Convert.ToInt32(txtNo.Text);

                    using (SqlCommand command = new SqlCommand(
                        $"DELETE FROM Additions WHERE id={additionId}", _dbConnection))
                    {
                        command.ExecuteNonQuery();
                    }

                    MessageBox.Show("✅ تم الحذف بنجاح", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    ClearAllFields();
                    LoadAdditionsGrid();
                    LoadNextAdditionNumber();
                }

                _dbConnection.Close();
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ أثناء الحذف", ex);
            }
        }
        #endregion

        #region Helper Methods
        private void ClearAllFields()
        {
            txtName.Text = "";
            txtSalePrice.Text = "";
            dgvData.SelectedItem = null;
        }

        private void ShowErrorMessage(string message, Exception ex)
        {
            MessageBox.Show(
                $"{message} ❌\n\nتفاصيل الخطأ:\n{ex.Message}",
                "خطأ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        #endregion
    }
}