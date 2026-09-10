using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using log4net;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// تقرير إجمالي مشتريات ومبيعات جهات العمل
    /// </summary>
    public partial class frmActsInvTotal : Window
    {
        #region Fields

        private static readonly ILog Logger = LogManager.GetLogger(Environment.MachineName);

        private SqlConnection _connection;
        private ObservableCollection<ActTotalItem> _reportData;

        #endregion

        #region Constructor

        public frmActsInvTotal()
        {
            InitializeComponent();
            _connection = MainClass.ConnObj();
            _reportData = new ObservableCollection<ActTotalItem>();
            dgvItems.ItemsSource = _reportData;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                txtDateFrom.SelectedDate = DateTime.Now;
                txtDateTo.SelectedDate = DateTime.Now;
                LoadActs();
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
        /// تحميل جهات العمل
        /// </summary>
        private void LoadActs()
        {
            try
            {
                string query = "SELECT id, name FROM Acts ORDER BY id";
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, _connection))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    cmbActs.DisplayMemberPath = "name";
                    cmbActs.SelectedValuePath = "id";
                    cmbActs.ItemsSource = dt.DefaultView;
                    cmbActs.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"LoadActs Error: {ex.Message}");
            }
        }

        #endregion

        #region Calculate Report Methods

        /// <summary>
        /// حساب التقرير
        /// </summary>
        private async void btnShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // التحقق من البيانات
                if (!ValidateInput())
                    return;

                // مسح البيانات السابقة
                _reportData.Clear();
                txtSumPurch.Content = "0.00";
                txtSumSale.Content = "0.00";

                // إظهار شريط التقدم
                ProgressBar1.Visibility = Visibility.Visible;
                ProgressBar1.IsIndeterminate = true;

                // تعطيل الأزرار
                SetButtonsEnabled(false);

                // حساب التقرير في خلفية
                await Task.Run(() => CalculateReport());

                // إخفاء شريط التقدم
                ProgressBar1.Visibility = Visibility.Collapsed;
                ProgressBar1.IsIndeterminate = false;

                // تمكين الأزرار
                SetButtonsEnabled(true);
            }
            catch (Exception ex)
            {
                Logger.Error($"btnShow_Click Error: {ex.Message}");
                MessageBox.Show($"خطأ في إنشاء التقرير: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                ProgressBar1.Visibility = Visibility.Collapsed;
                SetButtonsEnabled(true);
            }
        }

        /// <summary>
        /// التحقق من البيانات
        /// </summary>
        private bool ValidateInput()
        {
            if (chkAllActs.IsChecked == false && cmbActs.SelectedIndex == -1)
            {
                MessageBox.Show("يجب اختيار جهة العمل", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbActs.Focus();
                return false;
            }

            if (txtDateFrom.SelectedDate == null || txtDateTo.SelectedDate == null)
            {
                MessageBox.Show("يجب تحديد الفترة الزمنية", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// حساب التقرير الفعلي
        /// </summary>
        private void CalculateReport()
        {
            try
            {
                // بناء استعلام جهات العمل
                string actsQuery = "SELECT id, name FROM Acts";

                if (Dispatcher.Invoke(() => chkAllActs.IsChecked == false))
                {
                    int actId = Dispatcher.Invoke(() => Convert.ToInt32(cmbActs.SelectedValue));
                    actsQuery += $" WHERE id={actId}";
                }

                actsQuery += " ORDER BY id";

                // الحصول على التواريخ
                DateTime dateFrom = Dispatcher.Invoke(() => txtDateFrom.SelectedDate.Value);
                DateTime dateTo = Dispatcher.Invoke(() => txtDateTo.SelectedDate.Value.AddHours(24));

                // تحميل جهات العمل
                DataTable actsTable = new DataTable();
                using (SqlDataAdapter adapter = new SqlDataAdapter(actsQuery, _connection))
                {
                    adapter.Fill(actsTable);
                }

                // شرط الفرع
                string branchCondition = "";
                if (MainClass.BranchNo != -1)
                {
                    branchCondition = $"inv.branch={MainClass.BranchNo} AND ";
                }

                double totalPurchases = 0;
                double totalSales = 0;

                // معالجة كل جهة عمل
                for (int i = 0; i < actsTable.Rows.Count; i++)
                {
                    int actId = Convert.ToInt32(actsTable.Rows[i]["id"]);
                    string actName = actsTable.Rows[i]["name"].ToString();

                    // حساب المشتريات والمبيعات
                    double purchaseTotal = CalculateActTotal(actId, dateFrom, dateTo, branchCondition, true);
                    double saleTotal = CalculateActTotal(actId, dateFrom, dateTo, branchCondition, false);

                    totalPurchases += purchaseTotal;
                    totalSales += saleTotal;

                    // إضافة الصف للجدول
                    Dispatcher.Invoke(() =>
                    {
                        _reportData.Add(new ActTotalItem
                        {
                            RowNumber = i + 1,
                            PurchaseName = actName,
                            PurchaseTotal = purchaseTotal,
                            SaleName = actName,
                            SaleTotal = saleTotal
                        });
                    });
                }

                // تحديث الإجماليات
                Dispatcher.Invoke(() =>
                {
                    txtSumPurch.Content = totalPurchases.ToString("N2");
                    txtSumSale.Content = totalSales.ToString("N2");
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"CalculateReport Error: {ex.Message}");
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"خطأ في الحساب: {ex.Message}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        /// <summary>
        /// حساب إجمالي جهة عمل واحدة
        /// </summary>
        private double CalculateActTotal(int actId, DateTime dateFrom, DateTime dateTo,
                                        string branchCondition, bool isPurchase)
        {
            try
            {
                // نوع العملية (1=مشتريات، 2=مبيعات)
                int procType = isPurchase ? 1 : 2;

                // حساب صافي المبلغ (مشتريات - مرتجعات)
                double total = 0;
                double returns = 0;

                // الاستعلام الأساسي
                string baseQuery = $@"
                    SELECT SUM(val * exchange_price) 
                    FROM inv 
                    INNER JOIN inv_sub ON inv.InvGlobalID = inv_sub.InvGlobalID
                    INNER JOIN Customers ON inv.cust_id = Customers.id
                    WHERE {branchCondition}
                          Customers.act = @actId 
                          AND inv.date >= @dateFrom 
                          AND inv.date <= @dateTo 
                          AND inv.proc_type = @procType
                          AND inv_sub.proc_type = @subProcType
                          AND inv.IS_Deleted = 0";

                // حساب الإجمالي الأساسي (proc_type=1 للعمليات العادية)
                using (SqlCommand cmd = new SqlCommand(baseQuery, _connection))
                {
                    cmd.Parameters.AddWithValue("@actId", actId);
                    cmd.Parameters.AddWithValue("@dateFrom", dateFrom);
                    cmd.Parameters.AddWithValue("@dateTo", dateTo);
                    cmd.Parameters.AddWithValue("@procType", procType);
                    cmd.Parameters.AddWithValue("@subProcType", 1);

                    if (_connection.State != ConnectionState.Open)
                        _connection.Open();

                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        total = Convert.ToDouble(result);
                    }
                }

                // حساب المرتجعات (proc_type=2 للمرتجعات)
                using (SqlCommand cmd = new SqlCommand(baseQuery, _connection))
                {
                    cmd.Parameters.AddWithValue("@actId", actId);
                    cmd.Parameters.AddWithValue("@dateFrom", dateFrom);
                    cmd.Parameters.AddWithValue("@dateTo", dateTo);
                    cmd.Parameters.AddWithValue("@procType", procType);
                    cmd.Parameters.AddWithValue("@subProcType", 2);

                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        returns = Convert.ToDouble(result);
                    }
                }

                if (_connection.State == ConnectionState.Open)
                    _connection.Close();

                // الصافي = الإجمالي - المرتجعات
                return Math.Round(total - returns, 4);
            }
            catch (Exception ex)
            {
                Logger.Error($"CalculateActTotal Error: {ex.Message}");
                return 0;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// تفعيل/تعطيل الأزرار
        /// </summary>
        private void SetButtonsEnabled(bool enabled)
        {
            btnShow.IsEnabled = enabled;
            btnPreview.IsEnabled = enabled;
            btnPrint.IsEnabled = enabled;
            cmbActs.IsEnabled = enabled && (chkAllActs.IsChecked == false);
            chkAllActs.IsEnabled = enabled;
            txtDateFrom.IsEnabled = enabled;
            txtDateTo.IsEnabled = enabled;
        }

        #endregion

        #region CheckBox Events

        /// <summary>
        /// عند تغيير حالة "الكل"
        /// </summary>
        private void chkAllActs_Toggle(object sender, RoutedEventArgs e)
        {
            cmbActs.IsEnabled = chkAllActs.IsChecked == false;
            if (chkAllActs.IsChecked == true)
            {
                cmbActs.SelectedIndex = -1;
            }
        }

        #endregion

        #region Button Events

        /// <summary>
        /// زر معاينة
        /// </summary>
        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_reportData.Count == 0)
                {
                    MessageBox.Show("لا توجد بيانات للمعاينة", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // TODO: تنفيذ المعاينة
                MessageBox.Show("وظيفة المعاينة قيد التطوير", "معلومة",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.Error($"btnPreview_Click Error: {ex.Message}");
            }
        }

        /// <summary>
        /// زر طباعة
        /// </summary>
        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_reportData.Count == 0)
                {
                    MessageBox.Show("لا توجد بيانات للطباعة", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // TODO: تنفيذ الطباعة
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
    /// عنصر بيانات التقرير
    /// </summary>
    public class ActTotalItem
    {
        public int RowNumber { get; set; }
        public string PurchaseName { get; set; }
        public double PurchaseTotal { get; set; }
        public string SaleName { get; set; }
        public double SaleTotal { get; set; }
    }

    #endregion
}