using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using UtilitiesProj;

namespace SmartAuditERP.Form_WPF
{
    public partial class FrmRptSalesChart : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        public  int Invtype;
        //منع الأحداث من العمل قبل اكتمال تحميل النافذة
        private bool _isLoaded;

        #endregion

        #region Constructor

        public FrmRptSalesChart()
        {
            InitializeComponent();
            conn    = MainClass.ConnObj();
            Invtype = 0;
            _isLoaded = false;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtFromDate.DateTime = DateTime.Now;
            txtToDate.DateTime   = DateTime.Now;
            cmbInvType.SelectedIndex = 0;

            _isLoaded = true;

            LoadBranches();
            BranchPermissions();
            BindData();

            LoadSalesByPayment(0, null, null);
            LoadSalesByOrderType(0, null, null);
            LoadSalesByItems(0, null, null);
            LoadSalesByCategory(0, null, null);
        }

        #endregion

        #region Load Branches

        private void LoadBranches()
        {
            try
            {
                string branchCond = "";
                if (MainClass.BranchNo != -1)
                {
                    branchCond = string.Equals(Accounting.BranchCondition, " ",
                        StringComparison.Ordinal)
                        ? ""
                        : $" AND id={MainClass.BranchNo}";
                }

                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Branches WHERE IS_Deleted=0{branchCond}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbBranches.DisplayMemberPath = "name";
                cmbBranches.SelectedValuePath = "id";
                cmbBranches.ItemsSource       = dt.DefaultView;
                cmbBranches.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل الفروع:\n" + ex.Message);
            }
        }

        private void BranchPermissions()
        {
            if (!string.Equals(Accounting.BranchCondition, " ", StringComparison.Ordinal))
            {
                chkAllBranches.IsChecked  = false;
                chkAllBranches.Visibility = Visibility.Collapsed;
                cmbBranches.SelectedIndex = 0;
            }
        }

        #endregion

        #region BindData - المبيعات الشهرية

        private void BindData()
        {
            try
            {
                int invType  = cmbInvType.SelectedIndex == 1 ? 2 : 3;
                int branchId = 0;
                if (chkAllBranches.IsChecked != true && cmbBranches.SelectedValue != null)
                    branchId = Convert.ToInt32(cmbBranches.SelectedValue);

                var dt = GetMonthlySales(branchId, invType, 1, 1);
                ManthlySales.ItemsSource = dt?.DefaultView;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل البيانات الشهرية:\n" + ex.Message);
            }
        }

        #endregion

        #region Load Chart Data

        private void LoadSalesByPayment(int branchId, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                int invType = cmbInvType.SelectedIndex == 1 ? 2 : 3;
                var dt = GetSalesByPaymentMethods(branchId, startDate, endDate, invType, 0);

                // ترجمة أنواع الدفع
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string payTypeId = row["PayTypeID"].ToString();
                        row["PayMethod"] = payTypeId switch
                        {
                            "-1" => "آجل",
                            "1"  => "نقدي",
                            "2"  => "شبكة",
                            "4"  => "متعدد",
                            _    => row["PayMethod"].ToString()
                        };
                    }

                    if (dt.Rows.Count == 0)
                    {
                        dt.Rows.Add("نقدي",  1,  0, 0, 0);
                        dt.Rows.Add("شبكة",  2,  0, 0, 0);
                        dt.Rows.Add("متعدد", 4,  0, 0, 0);
                        dt.Rows.Add("آجل",  -1,  0, 0, 0);
                    }
                }

                ChartSalesByPayments.ItemsSource = dt?.DefaultView;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل بيانات الدفع:\n" + ex.Message);
            }
        }

        private void LoadSalesByOrderType(int branchId, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                int invType = cmbInvType.SelectedIndex == 1 ? 2 : 3;
                var dt = GetSalesByOrderType(branchId, startDate, endDate, invType, 0);

                if (dt?.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string orderTypeId = row["OrderTypeID"].ToString();
                        row["OrderTypeName"] = orderTypeId switch
                        {
                            "0" => "محلي",
                            "1" => "خارجي",
                            "2" => "معلق",
                            _   => row["OrderTypeName"].ToString()
                        };
                    }
                }

                SalesByOrderType.ItemsSource = dt?.DefaultView;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل بيانات نوع الطلب:\n" + ex.Message);
            }
        }

        private void LoadSalesByItems(int branchId, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                int invType = cmbInvType.SelectedIndex == 1 ? 2 : 3;
                var dt = GetSalesByItems(branchId, startDate, endDate, invType, 0);
                SalesByItems.ItemsSource = dt?.DefaultView;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل بيانات الأصناف:\n" + ex.Message);
            }
        }

        private void LoadSalesByCategory(int branchId, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                int invType = cmbInvType.SelectedIndex == 1 ? 2 : 3;
                var dt = GetSalesByCategory(branchId, startDate, endDate, invType, 0);
                SalesByCategory.ItemsSource = dt?.DefaultView;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل بيانات الأقسام:\n" + ex.Message);
            }
        }

        #endregion

        #region Stored Procedures Helpers

        private int GetCurrentBranchId()
        {
            if (chkAllBranches.IsChecked != true && cmbBranches.SelectedValue != null)
                return Convert.ToInt32(cmbBranches.SelectedValue);
            return 0;
        }

        private DataTable GetMonthlySales(int branchId, int invType, int payType, int seller)
        {
            return ExecSP("MonthlySales", branchId, invType, payType, seller, null, null);
        }

        private DataTable GetSalesByPaymentMethods(int branchId, DateTime? dateFrom,
            DateTime? dateTo, int invType, int seller)
        {
            return ExecSP("GetSalesByPaymentMethods", branchId, invType, 0, seller, dateFrom, dateTo);
        }

        private DataTable GetSalesByOrderType(int branchId, DateTime? dateFrom,
            DateTime? dateTo, int invType, int seller)
        {
            return ExecSP("GetSalesByOrderType", branchId, invType, 0, seller, dateFrom, dateTo);
        }

        private DataTable GetSalesByItems(int branchId, DateTime? dateFrom,
            DateTime? dateTo, int invType, int seller)
        {
            return ExecSP("GetSalesByItems", branchId, invType, 0, seller, dateFrom, dateTo);
        }

        private DataTable GetSalesByCategory(int branchId, DateTime? dateFrom,
            DateTime? dateTo, int invType, int seller)
        {
            return ExecSP("GetSalesByCategory", branchId, invType, 0, seller, dateFrom, dateTo);
        }

        /// <summary>تنفيذ Stored Procedure موحد</summary>
        private DataTable ExecSP(string spName, int branchId, int invType,
    int payType, int seller, DateTime? dateFrom, DateTime? dateTo)
        {
            try
            {
                using var connection = MainClass.ConnObj();
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                using var cmd = new SqlCommand(spName, connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 300
                };

                // branch
                if (StoredProcedureHasParameter(connection, spName, "@branch"))
                {
                    cmd.Parameters.Add("@branch", SqlDbType.Int).Value =
                        branchId == 0 ? (object)DBNull.Value : branchId;
                }

                // inv_type
                if (StoredProcedureHasParameter(connection, spName, "@inv_type"))
                {
                    cmd.Parameters.Add("@inv_type", SqlDbType.Int).Value = invType;
                }

                // pay_type
                if (StoredProcedureHasParameter(connection, spName, "@pay_type"))
                {
                    cmd.Parameters.Add("@pay_type", SqlDbType.Int).Value = payType;
                }

                // Seller
                if (StoredProcedureHasParameter(connection, spName, "@Seller"))
                {
                    cmd.Parameters.Add("@Seller", SqlDbType.Int).Value = seller;
                }

                // StartDate
                if (StoredProcedureHasParameter(connection, spName, "@StartDate"))
                {
                    cmd.Parameters.Add("@StartDate", SqlDbType.DateTime).Value =
                        dateFrom.HasValue ? (object)dateFrom.Value : DBNull.Value;
                }

                // EndDate
                if (StoredProcedureHasParameter(connection, spName, "@EndDate"))
                {
                    cmd.Parameters.Add("@EndDate", SqlDbType.DateTime).Value =
                        dateTo.HasValue ? (object)dateTo.Value : DBNull.Value;
                }

                var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ في {spName}:\n{ex.Message}");
                return new DataTable();
            }
        }

        private bool StoredProcedureHasParameter(SqlConnection connection, string procedureName, string parameterName)
        {
            const string sql = @"
        SELECT COUNT(*)
        FROM sys.parameters p
        INNER JOIN sys.objects o ON p.object_id = o.object_id
        WHERE o.type = 'P'
          AND o.name = @ProcedureName
          AND p.name = @ParameterName";

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@ProcedureName", procedureName);
            cmd.Parameters.AddWithValue("@ParameterName", parameterName);

            int count = Convert.ToInt32(cmd.ExecuteScalar());
            return count > 0;
        }

        #endregion

        #region Button Events

        private void btnShowData_Click(object sender, RoutedEventArgs e)
        {
            int branchId = GetCurrentBranchId();

            DateTime? startDate = null;
            DateTime? endDate   = null;

            if (chkAllPeriod.IsChecked != true)
            {
                startDate = BuildDateTime(txtFromDate.DateTime, txtStartTime.Text, "00:00");
                endDate   = BuildDateTime(txtToDate.DateTime,   txtEndTime.Text,   "23:59");
            }

            BindData();
            LoadSalesByPayment(branchId,   startDate, endDate);
            LoadSalesByOrderType(branchId, startDate, endDate);
            LoadSalesByItems(branchId,     startDate, endDate);
            LoadSalesByCategory(branchId,  startDate, endDate);
        }

        private void btnToday_Click(object sender, RoutedEventArgs e)
        {
            int branchId = GetCurrentBranchId();
            var start    = DateTime.Today;
            var end      = DateTime.Now;

            LoadSalesByPayment(branchId,   start, end);
            LoadSalesByOrderType(branchId, start, end);
            LoadSalesByItems(branchId,     start, end);
            LoadSalesByCategory(branchId,  start, end);
        }

        private void btnYest_Click(object sender, RoutedEventArgs e)
        {
            int branchId = GetCurrentBranchId();
            var start    = DateTime.Now.Date.AddDays(-1);
            var end      = start.AddHours(23).AddMinutes(59);

            LoadSalesByPayment(branchId,   start, end);
            LoadSalesByOrderType(branchId, start, end);
            LoadSalesByItems(branchId,     start, end);
            LoadSalesByCategory(branchId,  start, end);
        }

        private void btnWeek_Click(object sender, RoutedEventArgs e)
        {
            int branchId  = GetCurrentBranchId();
            int dayOfWeek = (int)DateTime.Today.DayOfWeek;
            var start     = DateTime.Today.AddDays(-dayOfWeek);
            var end       = DateTime.Today.AddDays(7 - dayOfWeek).AddSeconds(-1);

            LoadSalesByPayment(branchId,   start, end);
            LoadSalesByOrderType(branchId, start, end);
            LoadSalesByItems(branchId,     start, end);
            LoadSalesByCategory(branchId,  start, end);
        }

        private void btnMonth_Click(object sender, RoutedEventArgs e)
        {
            int branchId = GetCurrentBranchId();
            var start    = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var end      = start.AddMonths(1).AddDays(-1);

            LoadSalesByPayment(branchId,   start, end);
            LoadSalesByOrderType(branchId, start, end);
            LoadSalesByItems(branchId,     start, end);
            LoadSalesByCategory(branchId,  start, end);
        }

        private void btnItemsDetails_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new frmRptInvSalesDetails();
            dlg.rbSales.IsChecked = true;

            if (chkAllPeriod.IsChecked != true)
            {
                dlg.ckTotalPeriod.IsChecked   = false;
                dlg.txtFromDate.DateTime       = txtFromDate.DateTime;
                dlg.txtToDate.DateTime         = txtToDate.DateTime;
                dlg.txtStartTime.Text          = txtStartTime.Text;
                dlg.txtEndTime.Text            = txtEndTime.Text;
            }

            if (chkAllBranches.IsChecked != true)
            {
                dlg.cmbBranches.SelectedValue   = cmbBranches.SelectedValue;
                dlg.chkAllBranches.IsChecked    = false;
            }

            dlg.showInvoice();
            dlg.Show();
            dlg.Activate();
        }

        private void BtnSalesByPayType_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new frmRptItemsSalesDetails();
            dlg.Title = "مبيعات الأصناف تجميعي";
            dlg.Show();
            dlg.Activate();
        }

        #endregion

        #region CheckBox Events

        private void chkAllBranches_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            cmbBranches.IsEnabled = chkAllBranches.IsChecked != true;
        }

        private void chkAllPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            if (chkAllPeriod == null || txtFromDate == null || txtToDate == null ||
                txtStartTime == null || txtEndTime == null) return;

            bool isAll = chkAllPeriod.IsChecked == true;
            txtFromDate.IsEnabled = !isAll;
            txtToDate.IsEnabled = !isAll;
            txtStartTime.IsEnabled = !isAll;
            txtEndTime.IsEnabled = !isAll;
        }

        #endregion

        #region Helper

        private DateTime BuildDateTime(DateTime date, string timeText, string defaultTime)
        {
            try
            {
                string time = string.IsNullOrWhiteSpace(timeText)
                    ? defaultTime : timeText.Trim();
                return DateTime.Parse($"{date.ToShortDateString()} {time}");
            }
            catch { return date; }
        }

        #endregion
    }
}