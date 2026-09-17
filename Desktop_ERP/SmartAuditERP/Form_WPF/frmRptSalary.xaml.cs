using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptSalary : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private ObservableCollection<SalaryRow> SalaryList;

        #endregion

        #region Constructor

        public frmRptSalary()
        {
            InitializeComponent();
            conn        = MainClass.ConnObj();
            SalaryList  = new ObservableCollection<SalaryRow>();
            dgvItems.ItemsSource = SalaryList;
        }

        #endregion

        #region Window Loaded

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtYear.Text  = DateTime.Now.Year.ToString();
            txtMonth.Text = DateTime.Now.Month.ToString();
        }

        #endregion

        #region CheckBox Events

        private void ckWholePeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isAll = ckWholePeriod.IsChecked == true;
            txtMonth.IsEnabled = !isAll;
            txtYear.IsEnabled  = !isAll;
        }

        #endregion

        #region Button Events

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SalaryList.Clear();

                string condWhere = "";

                if (ckWholePeriod.IsChecked != true)
                {
                    if (int.TryParse(txtYear.Text, out int yr) &&
                        int.TryParse(txtMonth.Text, out int mo))
                    {
                        condWhere = $" AND SalaryPay.year=N'{yr}' " +
                                    $"AND SalaryPay.month={mo}";
                    }
                }

                string sql =
                    "SELECT SalaryPay.[id], SalaryPay.[date], " +
                    "SalaryPay.[month], SalaryPay.[year], " +
                    "SalaryPay.[branch], SalaryPay.[tot_salary], " +
                    "SalaryPay.[salary_add], SalaryPay.[salary_sub], " +
                    "SalaryPay.[salary_net], SalaryPay.[emp_resp], " +
                    "SalaryPay.[notes], SalaryPay.[Houses], " +
                    "SalaryPay.[Travel], " +
                    "Employees.name AS EmpName " +
                    "FROM SalaryPay " +
                    "LEFT JOIN Employees ON SalaryPay.Emp=Employees.id " +
                    $"WHERE SalaryPay.IS_Deleted=0 {condWhere}";

                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow row = dt.Rows[i];

                    double totSal  = Convert.ToDouble(row["tot_salary"]);
                    double houses  = Convert.ToDouble(row["Houses"]);
                    double travel  = Convert.ToDouble(row["Travel"]);
                    double salAdd  = Convert.ToDouble(row["salary_add"]);
                    double salSub  = Convert.ToDouble(row["salary_sub"]);
                    double gross   = totSal + houses + travel + salAdd;
                    double net     = gross - salSub;

                    SalaryList.Add(new SalaryRow
                    {
                        RowNo      = i + 1,
                        SalId      = Convert.ToInt32(row["id"]),
                        EmpName    = row["EmpName"]?.ToString() ?? "",
                        TotSalary  = totSal,
                        Houses     = houses,
                        Travel     = travel,
                        SalaryAdd  = salAdd,
                        GrossTotal = gross,
                        SalarySub  = salSub,
                        SalaryNet  = net,
                    });
                }

                // إجمالي الرواتب
                double totalNet = SalaryList.Sum(x => x.SalaryNet);
                txtSum.Text = totalNet.ToString("N2");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في عرض البيانات:\n{ex.Message}",
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            // فتح نافذة سند استلام الراتب (WPF)
            try
            {
                var form = new frmSalaryPay();
                form.Show();
                form.Activate();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnShowSand_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SalaryRow item)
            {
                try
                {
                    var form = new frmSalaryPay();
                    form.Show();
                    form.Navigate($"SELECT * FROM SalaryPay WHERE id={item.SalId}");
                    form.TabControl1.SelectedIndex = 0;
                    form.Activate();
                }
                catch (Exception ex)
                {
                    DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            // يمكن إضافة معاينة لاحقًا
            DXMessageBox.Show("معاينة الطباعة غير مفعّلة في هذا الإصدار.", "تنبيه",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            // يمكن إضافة طباعة لاحقًا
            DXMessageBox.Show("الطباعة غير مفعّلة في هذا الإصدار.", "تنبيه",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void dgvItems_SelectionChanged(object sender,
                                                SelectionChangedEventArgs e)
        {
            // يمكن إضافة منطق عند تغيير الاختيار
        }

        #endregion
    }
}