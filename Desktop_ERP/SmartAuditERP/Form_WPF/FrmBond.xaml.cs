using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class FrmBond : Window
    {
        #region Fields

        private ObservableCollection<BondSearchItem> searchData;

        #endregion

        #region Constructor

        public FrmBond()
        {
            InitializeComponent();

            searchData = new ObservableCollection<BondSearchItem>();
            dgvSrch.ItemsSource = searchData;
        }

        #endregion

        #region Event Handlers - Window

        private void FrmBond_Load(object sender, RoutedEventArgs e)
        {
            txtDate.SelectedDate = DateTime.Now;
            txtFromDate.SelectedDate = DateTime.Now;
            txtToDate.SelectedDate = DateTime.Now;
            LoadEmps();
        }

        #endregion

        #region Event Handlers - Buttons

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                searchData.Clear();

                string query = BuildSearchQuery();

                using (var adapter = new SqlDataAdapter(query, MainClass.conn))
                {
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    foreach (DataRow row in dataTable.Rows)
                    {
                        var item = new BondSearchItem
                        {
                            DataGridViewTextBoxColumn1 = row[0].ToString(),
                            Column11 = row[1].ToString(),
                            Column1 = row[2].ToString(),
                            Column2 = row[3].ToString(),
                            Column4 = row[4].ToString(),
                            Column3 = row[5].ToString()
                        };
                        searchData.Add(item);
                    }
                }

                if (searchData.Count == 0)
                {
                    MessageBox.Show("ℹ️ لا توجد نتائج للبحث", "نتائج البحث",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في البحث\nتفاصيل الخطأ: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helper Methods

        public void LoadEmps()
        {
            try
            {
                using (var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Employees WHERE is_deleted=0 ORDER BY id", MainClass.conn))
                {
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    cmbEmp.ItemsSource = dataTable.DefaultView;
                    cmbEmp.SelectedIndex = -1;

                    var dataTable2 = new DataTable();
                    adapter.Fill(dataTable2);
                    cmbEmpSrch.ItemsSource = dataTable2.DefaultView;
                    cmbEmpSrch.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ في تحميل الموظفين\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string BuildSearchQuery()
        {
            string query = "SELECT id, EmpId, EmpName, Amount, BondType, BondDate FROM Bonds WHERE 1=1";

            // Filter by employee
            if (!chkAllEmp.IsChecked == true && cmbEmpSrch.SelectedValue != null)
            {
                query += $" AND EmpId = {cmbEmpSrch.SelectedValue}";
            }

            // Filter by date range
            if (txtFromDate.SelectedDate.HasValue)
            {
                query += $" AND BondDate >= '{txtFromDate.SelectedDate.Value:yyyy-MM-dd}'";
            }

            if (txtToDate.SelectedDate.HasValue)
            {
                query += $" AND BondDate <= '{txtToDate.SelectedDate.Value:yyyy-MM-dd}'";
            }

            query += " ORDER BY BondDate DESC";

            return query;
        }

        #endregion
    }

    #region Helper Class

    public class BondSearchItem
    {
        public string DataGridViewTextBoxColumn1 { get; set; }
        public string Column11 { get; set; }
        public string Column1 { get; set; }
        public string Column2 { get; set; }
        public string Column4 { get; set; }
        public string Column3 { get; set; }
    }

    #endregion
}