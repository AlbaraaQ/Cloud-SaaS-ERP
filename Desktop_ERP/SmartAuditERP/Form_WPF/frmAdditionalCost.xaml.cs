using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AuditorAPI.Models.DGVmodels;
using log4net;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// نافذة التكاليف الإضافية للفاتورة
    /// </summary>
    public partial class frmAdditionalCost : Window
    {
        #region Fields

        private static readonly ILog Logger = LogManager.GetLogger(Environment.MachineName);

        private SqlConnection _connection;
        private ObservableCollection<InvoiceCostWrapper> _costsCollection;

        #endregion

        #region Properties

        /// <summary>
        /// بيانات الفاتورة
        /// </summary>
        public InvoiceDGV Invoice { get; set; }

        /// <summary>
        /// هل تم الحفظ بنجاح
        /// </summary>
        public bool IsDone { get; set; }

        #endregion

        #region Constructor

        public frmAdditionalCost()
        {
            InitializeComponent();
            _connection = MainClass.ConnObj();
            Invoice = new InvoiceDGV();
            IsDone = false;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // إعداد المجموعة
                _costsCollection = new ObservableCollection<InvoiceCostWrapper>();

                // تحويل البيانات الموجودة إلى Wrapper
                if (Invoice.InvoiceCosts != null)
                {
                    foreach (var cost in Invoice.InvoiceCosts)
                    {
                        _costsCollection.Add(new InvoiceCostWrapper(cost));
                    }
                }

                dgvCosts.ItemsSource = _costsCollection;

                // تحميل البيانات
                LoadCosts();
                LoadCostCenters();

                // حساب الإجمالي
                CalculateTotal();
            }
            catch (Exception ex)
            {
                Logger.Error($"Window_Loaded Error: {ex.Message}");
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Load Data Methods

        /// <summary>
        /// تحميل قائمة التكاليف
        /// </summary>
        private void LoadCosts()
        {
            try
            {
                string query = "SELECT id, name FROM costs";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    // ربط البيانات بعمود التكاليف
                    colCostName.ItemsSource = dt.DefaultView;
                    colCostName.DisplayMemberPath = "name";
                    colCostName.SelectedValuePath = "id";
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"LoadCosts Error: {ex.Message}");
            }
        }

        /// <summary>
        /// تحميل مراكز التكلفة
        /// </summary>
        private void LoadCostCenters()
        {
            try
            {
                string query = "SELECT ID, name FROM Cost_Center WHERE Is_Deleted=0 AND type=2";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    // ربط البيانات بعمود مراكز التكلفة
                    colCostCenter.ItemsSource = dt.DefaultView;
                    colCostCenter.DisplayMemberPath = "name";
                    colCostCenter.SelectedValuePath = "ID";
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"LoadCostCenters Error: {ex.Message}");
            }
        }

        #endregion

        #region Calculation Methods

        /// <summary>
        /// حساب الإجمالي
        /// </summary>
        private void CalculateTotal()
        {
            try
            {
                double total = 0;

                if (_costsCollection != null)
                {
                    foreach (var wrapper in _costsCollection)
                    {
                        total += (double)wrapper.CostValue;
                    }
                }

                txtSumVal.Text = total.ToString("N2");

                if (Invoice != null)
                {
                    Invoice.AdditionalCost = (decimal)total;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"CalculateTotal Error: {ex.Message}");
            }
        }

        #endregion

        #region DataGrid Events

        /// <summary>
        /// عند انتهاء تعديل خلية
        /// </summary>
        private void dgvCosts_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                // الانتظار حتى ينتهي التعديل
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    string columnHeader = e.Column.Header?.ToString() ?? "";

                    if (columnHeader == "اسم التكلفة")
                    {
                        HandleCostNameChange(e.Row);
                    }
                    else if (columnHeader == "القيمة")
                    {
                        CalculateTotal();
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                Logger.Error($"dgvCosts_CellEditEnding Error: {ex.Message}");
            }
        }

        /// <summary>
        /// معالجة تغيير اسم التكلفة
        /// </summary>
        private void HandleCostNameChange(DataGridRow row)
        {
            try
            {
                if (row.Item is InvoiceCostWrapper wrapper)
                {
                    int costId = wrapper.CostId;

                    // تحميل مركز التكلفة الافتراضي
                    if (costId > 0)
                    {
                        LoadDefaultCostCenter(wrapper, costId);
                    }

                    // تعيين القيم الأساسية
                    if (wrapper.InvoiceCostId == 0)
                    {
                        wrapper.InvGlobalID = Invoice.InvGlobalID;
                        wrapper.InvoiceCostNo = 1;
                    }

                    dgvCosts.Items.Refresh();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"HandleCostNameChange Error: {ex.Message}");
            }
        }

        /// <summary>
        /// تحميل مركز التكلفة الافتراضي
        /// </summary>
        private void LoadDefaultCostCenter(InvoiceCostWrapper wrapper, int costId)
        {
            try
            {
                string query = "SELECT ISNULL(costCenterId, 0) FROM costs WHERE id=@costId";
                using (SqlCommand cmd = new SqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@costId", costId);

                    if (_connection.State != ConnectionState.Open)
                        _connection.Open();

                    object result = cmd.ExecuteScalar();

                    if (result != null && Convert.ToInt32(result) > 0)
                    {
                        wrapper.CostCenterId = Convert.ToInt32(result);
                    }

                    if (_connection.State == ConnectionState.Open)
                        _connection.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"LoadDefaultCostCenter Error: {ex.Message}");
            }
        }

        /// <summary>
        /// حذف صف
        /// </summary>
        private void btnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // استخدام System.Windows.Controls.Button بشكل صريح
                System.Windows.Controls.Button btn = sender as System.Windows.Controls.Button;

                if (btn?.DataContext is InvoiceCostWrapper wrapper)
                {
                    var result = MessageBox.Show("هل أنت متأكد من الحذف؟", "تأكيد الحذف",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _costsCollection.Remove(wrapper);
                        CalculateTotal();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"btnDeleteRow_Click Error: {ex.Message}");
                MessageBox.Show($"خطأ في الحذف: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Button Events

        /// <summary>
        /// زر جديد
        /// </summary>
        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _costsCollection.Clear();
                txtSumVal.Text = "0.00";

                if (Invoice != null)
                {
                    Invoice.AdditionalCost = 0;
                }

                dgvCosts.Items.Refresh();
            }
            catch (Exception ex)
            {
                Logger.Error($"btnNew_Click Error: {ex.Message}");
            }
        }

        /// <summary>
        /// زر إدراج
        /// </summary>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // التحقق من وجود بيانات
                if (_costsCollection == null || _costsCollection.Count == 0)
                {
                    MessageBox.Show("لا توجد تكاليف للإدراج", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // التحقق من صحة البيانات
                if (!ValidateCosts())
                    return;

                // تحديث القائمة الأصلية
                SyncCostsToInvoice();

                IsDone = true;
                this.Close();
            }
            catch (Exception ex)
            {
                Logger.Error($"btnSave_Click Error: {ex.Message}");
                MessageBox.Show($"خطأ في الحفظ: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// زر إغلاق
        /// </summary>
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_costsCollection != null && _costsCollection.Count > 0)
                {
                    var result = MessageBox.Show("هل تريد الخروج بدون حفظ التكاليف؟", "تأكيد",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.No)
                        return;
                }

                IsDone = false;
                _costsCollection?.Clear();
                this.Close();
            }
            catch (Exception ex)
            {
                Logger.Error($"btnClose_Click Error: {ex.Message}");
            }
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// التحقق من صحة البيانات
        /// </summary>
        private bool ValidateCosts()
        {
            try
            {
                foreach (var wrapper in _costsCollection)
                {
                    if (wrapper.CostId <= 0)
                    {
                        MessageBox.Show("يجب اختيار اسم التكلفة لجميع الصفوف", "تنبيه",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }

                    if (wrapper.CostValue <= 0)
                    {
                        MessageBox.Show("يجب إدخال قيمة أكبر من صفر لجميع التكاليف", "تنبيه",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"ValidateCosts Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// مزامنة التكاليف مع الفاتورة
        /// </summary>
        private void SyncCostsToInvoice()
        {
            try
            {
                // مسح القائمة القديمة
                Invoice.InvoiceCosts.Clear();

                // إضافة العناصر الجديدة
                foreach (var wrapper in _costsCollection)
                {
                    Invoice.InvoiceCosts.Add(wrapper.ToInvoiceCost());
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"SyncCostsToInvoice Error: {ex.Message}");
            }
        }

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// غلاف لـ InvoiceCost لدعم Binding في WPF
    /// </summary>
    public class InvoiceCostWrapper : System.ComponentModel.INotifyPropertyChanged
    {
        private InvoiceCost _originalCost;
        private int _costId;
        private decimal _costValue;
        private string _note;
        private int _costCenterId;

        public InvoiceCostWrapper()
        {
            _originalCost = new InvoiceCost();
        }

        public InvoiceCostWrapper(InvoiceCost cost)
        {
            _originalCost = cost;
            _costId = cost.CostId;
            _costValue = (decimal)cost.Cost;
            _note = cost.Note;
            _costCenterId = cost.costCenter;
        }

        public int InvoiceCostId
        {
            get => _originalCost.InvoiceCostId;
            set
            {
                _originalCost.InvoiceCostId = value;
                OnPropertyChanged(nameof(InvoiceCostId));
            }
        }

        public int InvoiceCostNo
        {
            get => _originalCost.InvoiceCostNo;
            set
            {
                _originalCost.InvoiceCostNo = value;
                OnPropertyChanged(nameof(InvoiceCostNo));
            }
        }

        public string InvGlobalID
        {
            get => _originalCost.InvGlobalID;
            set
            {
                _originalCost.InvGlobalID = value;
                OnPropertyChanged(nameof(InvGlobalID));
            }
        }

        public int CostId
        {
            get => _costId;
            set
            {
                _costId = value;
                _originalCost.CostId = value;
                OnPropertyChanged(nameof(CostId));
            }
        }

        public decimal CostValue
        {
            get => _costValue;
            set
            {
                _costValue = value;
                _originalCost.Cost = (float)value;
                OnPropertyChanged(nameof(CostValue));
            }
        }

        public string Note
        {
            get => _note;
            set
            {
                _note = value;
                _originalCost.Note = value;
                OnPropertyChanged(nameof(Note));
            }
        }

        public int CostCenterId
        {
            get => _costCenterId;
            set
            {
                _costCenterId = value;
                _originalCost.costCenter = value;
                OnPropertyChanged(nameof(CostCenterId));
            }
        }

        public InvoiceCost ToInvoiceCost()
        {
            return _originalCost;
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }

    #endregion
}