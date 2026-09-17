using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    #region Models
    public class RentPeriodItem : INotifyPropertyChanged
    {
        private int _periodId;
        private double _rent;
        private int _offer;

        public int PeriodId
        {
            get => _periodId;
            set
            {
                _periodId = value;
                OnPropertyChanged(nameof(PeriodId));
            }
        }

        public double Rent
        {
            get => _rent;
            set
            {
                _rent = value;
                OnPropertyChanged(nameof(Rent));
            }
        }

        public int Offer
        {
            get => _offer;
            set
            {
                _offer = value;
                OnPropertyChanged(nameof(Offer));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class GroupItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
    }

    public class PeriodItem_RentPeriodItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    #endregion

    public partial class frmAddPeriod : Window
    {
        #region Fields
        private SqlConnection _dbConnection;
        private int _selectedGroupId;
        private ObservableCollection<RentPeriodItem> _rentPeriods;
        private ObservableCollection<PeriodItem_RentPeriodItem> _periods;

        public bool IsDone { get; set; }
        public int GroupId { get; set; }
        #endregion

        #region Properties
        public ObservableCollection<PeriodItem_RentPeriodItem> Periods
        {
            get => _periods;
            set => _periods = value;
        }
        #endregion

        #region Constructor
        public frmAddPeriod()
        {
            InitializeComponent();

            _dbConnection = MainClass.ConnObj();
            _selectedGroupId = -1;
            IsDone = false;
            GroupId = -1;

            _rentPeriods = new ObservableCollection<RentPeriodItem>();
            _periods = new ObservableCollection<PeriodItem_RentPeriodItem>();

            dgvData.ItemsSource = _rentPeriods;
            DataContext = this;

            Loaded += FrmAddPeriod_Loaded;
        }
        #endregion

        #region Form Events
        private void FrmAddPeriod_Loaded(object sender, RoutedEventArgs e)
        {
            LoadGroups();
            LoadPeriods();

            try
            {
                if (GroupId > 0)
                {
                    cmbGroup.SelectedValue = GroupId;
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل الفئة", ex);
            }
        }
        #endregion

        #region Data Loading Methods
        private void LoadGroups()
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT id, name, code FROM GroupMarine WHERE IsDeleted=0 ORDER BY id", _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count > 0)
                    {
                        cmbGroup.ItemsSource = dataTable.AsEnumerable().Select(row => new GroupItem
                        {
                            Id = Convert.ToInt32(row["id"]),
                            Name = row["name"].ToString(),
                            Code = row["code"].ToString()
                        }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل الفئات", ex);
            }
        }

        private void LoadPeriods()
        {
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT id, name FROM RentPeriod ORDER BY id", _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    _periods.Clear();

                    foreach (DataRow row in dataTable.Rows)
                    {
                        _periods.Add(new PeriodItem_RentPeriodItem
                        {
                            Id = Convert.ToInt32(row["id"]),
                            Name = row["name"].ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل الفترات", ex);
            }
        }

        public void LoadGroupPeriods()
        {
            try
            {
                _rentPeriods.Clear();

                using (SqlDataAdapter adapter = new SqlDataAdapter(
                    $"SELECT * FROM RentPeriodSub WHERE MGroupID={_selectedGroupId}", _dbConnection))
                {
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    foreach (DataRow row in dataTable.Rows)
                    {
                        _rentPeriods.Add(new RentPeriodItem
                        {
                            PeriodId = Convert.ToInt32(row["periodID"]),
                            Rent = Convert.ToDouble(row["rent"]),
                            Offer = Convert.ToInt32(row["offer"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في تحميل فترات الفئة", ex);
            }
        }
        #endregion

        #region ComboBox Events
        private void cmbGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbGroup.SelectedValue != null)
                {
                    _selectedGroupId = Convert.ToInt32(cmbGroup.SelectedValue);
                    GroupId = _selectedGroupId;
                    _rentPeriods.Clear();
                    LoadGroupPeriods();
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ في اختيار الفئة", ex);
            }
        }
        #endregion

        #region DataGrid Events
        private void dgvData_CurrentCellChanged(object sender, EventArgs e)
        {
            try
            {
                dgvData.CommitEdit(DataGridEditingUnit.Row, true);
            }
            catch { }
        }

        private void dgvData_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                dgvData.CommitEdit(DataGridEditingUnit.Row, true);
            }
            catch { }
        }

        private void btnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = sender as Button;
                if (btn?.DataContext is RentPeriodItem item)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "هل أنت متأكد من حذف الفترة المحددة؟ 🗑️",
                        "تأكيد الحذف",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _rentPeriods.Remove(item);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage("خطأ أثناء الحذف", ex);
            }
        }
        #endregion

        #region Button Click Events
        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            ClearAll();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveRentPeriods();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        #endregion

        #region Save Method
        private void SaveRentPeriods()
        {
            try
            {
                // Validation
                var invalidRows = _rentPeriods.Where(r => r.PeriodId <= 0 || r.Rent <= 0).ToList();

                if (invalidRows.Any())
                {
                    MessageBox.Show("يجب إستكمال البيانات ⚠️", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_selectedGroupId <= 0)
                {
                    MessageBox.Show("يجب اختيار الفئة أولاً ⚠️", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Set default offer value
                foreach (var period in _rentPeriods.Where(p => p.Offer == 0))
                {
                    period.Offer = 0;
                }

                MessageBoxResult confirmResult = MessageBox.Show(
                    "هل أنت متأكد من الحفظ؟ 💾",
                    "تأكيد الحفظ",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    if (_dbConnection.State != ConnectionState.Open)
                    {
                        _dbConnection.Open();
                    }

                    // Delete existing records
                    using (SqlCommand deleteCommand = new SqlCommand(
                        $"DELETE FROM RentPeriodSub WHERE MGroupID={_selectedGroupId}", _dbConnection))
                    {
                        deleteCommand.ExecuteNonQuery();
                    }

                    // Insert new records
                    string groupCode = cmbGroup.Text;

                    foreach (var period in _rentPeriods)
                    {
                        using (SqlCommand insertCommand = new SqlCommand(
                            "INSERT INTO RentPeriodSub(MGroupID, code, periodID, rent, offer) " +
                            "VALUES(@MGroupID, @code, @periodID, @rent, @offer)", _dbConnection))
                        {
                            insertCommand.Parameters.AddWithValue("@MGroupID", _selectedGroupId);
                            insertCommand.Parameters.AddWithValue("@code", groupCode);
                            insertCommand.Parameters.AddWithValue("@periodID", period.PeriodId);
                            insertCommand.Parameters.AddWithValue("@rent", period.Rent);
                            insertCommand.Parameters.AddWithValue("@offer", period.Offer);
                            insertCommand.ExecuteNonQuery();
                        }
                    }

                    _dbConnection.Close();

                    MessageBox.Show("✅ تم الحفظ بنجاح", "نجاح",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    IsDone = true;
                    ClearAll();
                }
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

        #region Helper Methods
        private void ClearAll()
        {
            cmbGroup.SelectedIndex = -1;
            _rentPeriods.Clear();
            _selectedGroupId = -1;
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