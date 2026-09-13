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
    public partial class frmRptReseved : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        public SqlConnection conn;
        public SqlConnection conn1;
        public string cond;

        private ObservableCollection<ResevedRow> ResevedList;

        #endregion

        #region Constructor

        public frmRptReseved()
        {
            InitializeComponent();

            conn  = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            cond  = "";

            ResevedList = new ObservableCollection<ResevedRow>();
            GridControl2.ItemsSource = ResevedList;
        }

        #endregion

        #region Window Loaded

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtStartDate.DateTime = DateTime.Today;
            txtEndDate.DateTime   = DateTime.Today;
        }

        #endregion

        #region Load Data

        public void LoadData(string filterCond)
        {
            try
            {
                if (conn.State == ConnectionState.Open) conn.Close();
                conn.Open();

                ResevedList.Clear();

                string sql =
                    "SELECT SR.id, SR.Date, SR.EntryGlobalID, SR.GlobalID, " +
                    "B.name, SR.month, SR.year, SR.Notes " +
                    "FROM Salary_Res SR " +
                    "LEFT JOIN Branches B ON B.code=SR.branch " +
                    $"WHERE SR.IS_Deleted=0 {filterCond}";

                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow row = dt.Rows[i];
                    ResevedList.Add(new ResevedRow
                    {
                        DgvNo        = i + 1,
                        Id           = Convert.ToInt32(row["id"]),
                        DateRes      = Convert.ToDateTime(row["Date"]),
                        EntryGlobalID = row["EntryGlobalID"]?.ToString() ?? "",
                        GlobalID     = row["GlobalID"]?.ToString() ?? "",
                        branch       = row["name"]?.ToString() ?? "",
                        month        = Convert.ToInt32(row["month"]),
                        year         = Convert.ToInt32(row["year"]),
                        Notes        = row["Notes"]?.ToString() ?? "",
                    });
                }

                conn.Close();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في تحميل البيانات:\n{ex.Message}",
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                conn.Close();
            }
        }

        public void LoadDataToGridview(ResevedRow selectedItem)
        {
            try
            {
                if (conn.State == ConnectionState.Open) conn.Close();
                conn.Open();

                var detailDt = new DataTable();
                detailDt.Columns.Add("DgvNo",      typeof(int));
                detailDt.Columns.Add("Empnam",      typeof(string));
                detailDt.Columns.Add("AccCode",     typeof(int));
                detailDt.Columns.Add("BasicSal",    typeof(decimal));
                detailDt.Columns.Add("HousSal",     typeof(decimal));
                detailDt.Columns.Add("TravelSal",   typeof(decimal));
                detailDt.Columns.Add("Sal_add",     typeof(decimal));
                detailDt.Columns.Add("Total",       typeof(decimal));
                detailDt.Columns.Add("Sal_Sub",     typeof(decimal));
                detailDt.Columns.Add("Net_Sal",     typeof(decimal));
                detailDt.Columns.Add("Notes",       typeof(string));
                detailDt.Columns.Add("Emp",         typeof(int));

                string sql =
                    "SELECT ISNULL(E.id,0) AS id, ISNULL(E.name,'') AS name, " +
                    "ISNULL(E.AccCode,'') AS AccCode, ISNULL(SD.Basic,0) AS Basic, " +
                    "ISNULL(SD.Houses,0) AS Houses, ISNULL(SD.travel,0) AS travel, " +
                    "ISNULL(SD.sal_add,0) AS sal_add, ISNULL(SD.sal_Sub,0) AS sal_Sub, " +
                    "ISNULL(SD.Net,0) AS Net, ISNULL(RS.Notes,0) AS Notes " +
                    "FROM Salary_Res RS " +
                    "LEFT JOIN Salary_Res_Details SD ON SD.GlobalId=RS.GlobalId " +
                    "LEFT JOIN Employees E ON SD.res_emp=E.id " +
                    $"WHERE RS.GlobalId=N'{selectedItem.GlobalID}'";

                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                string notes = selectedItem.Notes;

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow row = dt.Rows[i];
                    decimal basic  = Convert.ToDecimal(row["Basic"]);
                    decimal houses = Convert.ToDecimal(row["Houses"]);
                    decimal travel = Convert.ToDecimal(row["travel"]);
                    decimal salAdd = Convert.ToDecimal(row["sal_add"]);
                    decimal salSub = Convert.ToDecimal(row["sal_Sub"]);
                    decimal total  = basic + houses + travel + salAdd;
                    decimal net    = total - salSub;

                    detailDt.Rows.Add(i + 1,
                        row["name"], Convert.ToInt32(row["AccCode"]),
                        basic, houses, travel, salAdd, total, salSub, net,
                        notes, row["id"]);
                }

                if (detailDt.Rows.Count > 0)
                {
                    // فتح نافذة FrmReseved (WPF)
                    var frmReseved = new FrmReseved();
                    frmReseved.IsNew = false;
                    frmReseved.txtNo.Text            = selectedItem.Id.ToString();
                    frmReseved.txtEntryGlobalId.Text = selectedItem.EntryGlobalID;
                    frmReseved.TxtGlobalID.Text      = selectedItem.GlobalID;
                    frmReseved.TabControl1.SelectedIndex = 0;
                    frmReseved.btnviewEntry.Visibility  = Visibility.Visible;
                    frmReseved.GridControl1.ItemsSource = detailDt.DefaultView;
                    frmReseved.Show();
                    frmReseved.Activate();
                    this.Hide();
                }

                conn.Close();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                conn.Close();
            }
        }

        public void PreviewResevd(ResevedRow item)
            => LoadDataToGridview(item);

        #endregion

        #region Button Events

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            cond = "";

            if (chkall.IsChecked != true)
            {
                string d1 = txtStartDate.DateTime.ToShortDateString();
                string d2 = txtEndDate.DateTime.ToShortDateString();
                cond = $"AND Date>=N'{d1}' AND Date<=N'{d2}'";
            }

            LoadData(cond);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnviewres_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ResevedRow item)
            {
                PreviewResevd(item);
            }
        }

        #endregion

        #region CheckBox Events

        private void chkall_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isAll = chkall.IsChecked == true;
            txtStartDate.IsEnabled = !isAll;
            txtEndDate.IsEnabled   = !isAll;
        }

        #endregion
    }
}