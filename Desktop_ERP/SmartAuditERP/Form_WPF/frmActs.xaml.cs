using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using log4net;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// نافذة تعريف مجالات العمل والأنشطة
    /// </summary>
    public partial class frmActs : Window
    {
        #region Fields

        private static readonly ILog Logger = LogManager.GetLogger(Environment.MachineName);

        private SqlConnection _connection;
        private int _currentCode = -1;
        private ObservableCollection<ActItem> _actsList;

        #endregion

        #region Constructor

        public frmActs()
        {
            InitializeComponent();
            _connection = MainClass.ConnObj();
            _actsList = new ObservableCollection<ActItem>();

            // ربط البيانات
            dgvActs.ItemsSource = _actsList;

            // تسجيل اختصارات لوحة المفاتيح
            RegisterKeyboardShortcuts();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadData();
                txtName.Focus();

                // تطبيق حالة النافذة
                this.WindowState = MainClass.Window_State;
            }
            catch (Exception ex)
            {
                Logger.Error($"Window_Loaded Error: {ex.Message}");
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Keyboard Shortcuts

        /// <summary>
        /// تسجيل اختصارات لوحة المفاتيح
        /// </summary>
        private void RegisterKeyboardShortcuts()
        {
            try
            {
                // Ctrl+N - جديد
                this.InputBindings.Add(new KeyBinding(
                    new RelayCommand(param => btnNew_Click(null, null)),
                    Key.N, ModifierKeys.Control));

                // Ctrl+S - حفظ
                this.InputBindings.Add(new KeyBinding(
                    new RelayCommand(param => btnSave_Click(null, null)),
                    Key.S, ModifierKeys.Control));

                // Delete - حذف
                this.InputBindings.Add(new KeyBinding(
                    new RelayCommand(param => btnDelete_Click(null, null)),
                    Key.Delete, ModifierKeys.None));

                // Ctrl+P - طباعة
                this.InputBindings.Add(new KeyBinding(
                    new RelayCommand(param => btnPrint_Click(null, null)),
                    Key.P, ModifierKeys.Control));

                // Esc - إغلاق
                this.InputBindings.Add(new KeyBinding(
                    new RelayCommand(param => btnClose_Click(null, null)),
                    Key.Escape, ModifierKeys.None));
            }
            catch (Exception ex)
            {
                Logger.Error($"RegisterKeyboardShortcuts Error: {ex.Message}");
            }
        }

        #endregion

        #region Data Methods

        /// <summary>
        /// تحميل البيانات من قاعدة البيانات
        /// </summary>
        private void LoadData()
        {
            try
            {
                _actsList.Clear();

                string query = "SELECT * FROM Acts ORDER BY id";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    foreach (DataRow row in dt.Rows)
                    {
                        _actsList.Add(new ActItem
                        {
                            Id = Convert.ToInt32(row["id"]),
                            Name = row["name"].ToString()
                        });
                    }
                }

                dgvActs.Items.Refresh();
            }
            catch (Exception ex)
            {
                Logger.Error($"LoadData Error: {ex.Message}");
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// مسح الحقول
        /// </summary>
        private void Clear()
        {
            txtName.Clear();
            _currentCode = -1;
            dgvActs.SelectedItem = null;
            txtName.Focus();
        }

        #endregion

        #region Save Methods

        /// <summary>
        /// حفظ البيانات
        /// </summary>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // التحقق من البيانات
                if (!ValidateInput())
                    return;

                if (_connection.State != ConnectionState.Open)
                    _connection.Open();

                if (_currentCode == -1)
                {
                    // إضافة جديد
                    InsertNewRecord();
                }
                else
                {
                    // تعديل موجود
                    UpdateRecord();
                }

                // إعادة تحميل البيانات
                LoadData();
                Clear();

                MessageBox.Show("تم الحفظ بنجاح", "حفظ",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.Error($"btnSave_Click Error: {ex.Message}");
                MessageBox.Show($"خطأ أثناء الحفظ{Environment.NewLine}تفاصيل الخطأ: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    _connection.Close();
            }
        }

        /// <summary>
        /// التحقق من صحة البيانات
        /// </summary>
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("ادخل مجال العمل", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return false;
            }

            return true;
        }

        /// <summary>
        /// إدراج سجل جديد
        /// </summary>
        private void InsertNewRecord()
        {
            string query = "INSERT INTO Acts(name) VALUES(N@name)";
            using (SqlCommand cmd = new SqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@name", txtName.Text.Trim());
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// تعديل سجل موجود
        /// </summary>
        private void UpdateRecord()
        {
            string query = "UPDATE Acts SET name=@name WHERE id=@id";
            using (SqlCommand cmd = new SqlCommand(query, _connection))
            {
                cmd.Parameters.AddWithValue("@name", txtName.Text.Trim());
                cmd.Parameters.AddWithValue("@id", _currentCode);
                cmd.ExecuteNonQuery();
            }
        }

        #endregion

        #region Delete Methods

        /// <summary>
        /// حذف السجل
        /// </summary>
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentCode == -1)
                {
                    MessageBox.Show("اختر مجال عمل ليتم حذفه", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // تأكيد الحذف
                var result = MessageBox.Show($"هل أنت متأكد من حذف '{txtName.Text}'؟",
                    "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;

                if (_connection.State != ConnectionState.Open)
                    _connection.Open();

                string query = "DELETE FROM Acts WHERE id=@id";
                using (SqlCommand cmd = new SqlCommand(query, _connection))
                {
                    cmd.Parameters.AddWithValue("@id", _currentCode);
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("تم الحذف بنجاح", "حذف",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadData();
                Clear();
            }
            catch (Exception ex)
            {
                Logger.Error($"btnDelete_Click Error: {ex.Message}");
                MessageBox.Show($"خطأ أثناء الحذف{Environment.NewLine}تفاصيل الخطأ: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    _connection.Close();
            }
        }

        #endregion

        #region Navigation Methods

        /// <summary>
        /// التنقل في السجلات
        /// </summary>
        private void Navigate(string sqlQuery)
        {
            try
            {
                dgvActs.SelectedItem = null;

                if (_connection.State != ConnectionState.Open)
                    _connection.Open();

                using (SqlCommand cmd = new SqlCommand(sqlQuery, _connection))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows && reader.Read())
                    {
                        _currentCode = Convert.ToInt32(reader["id"]);
                        txtName.Text = reader["name"].ToString();

                        // تحديد الصف في الجدول
                        var item = _actsList.FirstOrDefault(x => x.Id == _currentCode);
                        if (item != null)
                        {
                            dgvActs.SelectedItem = item;
                            dgvActs.ScrollIntoView(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Navigate Error: {ex.Message}");
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                    _connection.Close();
            }
        }

        /// <summary>
        /// الانتقال للسجل الأول
        /// </summary>
        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate("SELECT TOP 1 * FROM Acts ORDER BY id ASC");
        }

        /// <summary>
        /// الانتقال للسجل الأخير
        /// </summary>
        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate("SELECT TOP 1 * FROM Acts ORDER BY id DESC");
        }

        /// <summary>
        /// الانتقال للسجل التالي
        /// </summary>
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCode != -1)
            {
                Navigate($"SELECT TOP 1 * FROM Acts WHERE id > {_currentCode} ORDER BY id ASC");
            }
        }

        /// <summary>
        /// الانتقال للسجل السابق
        /// </summary>
        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCode != -1)
            {
                Navigate($"SELECT TOP 1 * FROM Acts WHERE id < {_currentCode} ORDER BY id DESC");
            }
        }

        #endregion

        #region DataGrid Events

        /// <summary>
        /// عند تغيير الصف المحدد
        /// </summary>
        private void dgvActs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (dgvActs.SelectedItem is ActItem selectedItem)
                {
                    _currentCode = selectedItem.Id;
                    txtName.Text = selectedItem.Name;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"dgvActs_SelectionChanged Error: {ex.Message}");
            }
        }

        #endregion

        #region TextBox Events

        /// <summary>
        /// عند الضغط على Enter في حقل الاسم
        /// </summary>
        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSave_Click(null, null);
            }
        }

        #endregion

        #region Button Events

        /// <summary>
        /// زر جديد
        /// </summary>
        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            Clear();
        }

        /// <summary>
        /// زر طباعة
        /// </summary>
        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: تنفيذ وظيفة الطباعة
                MessageBox.Show("وظيفة الطباعة قيد التطوير", "معلومة",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.Error($"btnPrint_Click Error: {ex.Message}");
            }
        }

        /// <summary>
        /// زر إغلاق
        /// </summary>
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// عنصر مجال العمل
    /// </summary>
    public class ActItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// فئة للأوامر (Commands)
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    #endregion
}