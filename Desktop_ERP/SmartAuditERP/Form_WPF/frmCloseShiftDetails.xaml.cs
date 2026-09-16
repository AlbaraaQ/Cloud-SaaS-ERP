using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using AuditorAPI.Models;
using SmartAuditERP.Form_WPF;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmCloseShiftDetails : Window
    {
        #region ── Public Fields ──────────────────────────────

        public CashierCloseData obj { get; set; }
        public bool isDone { get; set; } = false;

        #endregion

        #region ── Private Fields ─────────────────────────────

        private SqlConnection conn;
        Home Home = new Home();
        #endregion

        #region ── Constructor ────────────────────────────────

        public frmCloseShiftDetails()
        {
            InitializeComponent();
            obj = new CashierCloseData();
            conn = MainClass.ConnObj();        
        }

        #endregion

        #region ── Window Events ──────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCloseData();
            SetupAndroidPanel();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        #endregion

        #region ── Load Data ──────────────────────────────────

        private void LoadCloseData()
        {
            if (obj == null)
                return;

            txtCloseNo.Text = obj.CloseNo ?? string.Empty;
            txtUser.Text = obj.Username ?? string.Empty;
            txtFrom.Text = obj.CloseDateFrome ?? string.Empty;
            txtTo.Text = obj.CloseDateTo ?? string.Empty;
            txtNetTreasury.Text = obj.SAfeNetVal ?? string.Empty;
            txtCasherValue.Text = obj.CasherValue ?? string.Empty;
            txtDiffVal.Text = obj.DiffVal ?? string.Empty;
            txtExpenses.Text = obj.Expenses ?? string.Empty;
            txtPurch.Text = obj.Purchases ?? string.Empty;
            txtCashSales.Text = obj.CashVal ?? string.Empty;
            txtCashReSales.Text = obj.ReturnSales ?? string.Empty;
            txtVisaSales.Text = obj.NetworkSales ?? string.Empty;
            txtCreditSales.Text = obj.PostPoneSales ?? string.Empty;
            txtCreditReSales.Text = obj.PostPoneRet ?? string.Empty;
            txtNetSales.Text = obj.Net ?? string.Empty;
            txtDisc.Text = obj.Discount ?? string.Empty;
            txtNetTreasuryWithoutTax.Text = obj.NetWithoutVAT ?? string.Empty;
            txtNetTax.Text = obj.VAT ?? string.Empty;

            // تلوين فرق الصندوق حسب القيمة
            if (double.TryParse(obj.DiffVal, out double diffValue))
            {
                if (diffValue < 0)
                    txtDiffVal.Foreground =
                        new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(183, 28, 28));
                else if (diffValue > 0)
                    txtDiffVal.Foreground =
                        new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(27, 94, 32));
            }
        }

        private void SetupAndroidPanel()
        {
            if (Home._is_active)
            {
                Button1.Visibility = Visibility.Visible;
                androidPanel.Visibility = Visibility.Visible;
            }
            else
            {
                androidPanel.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region ── Button Events ──────────────────────────────

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            isDone = false;
            Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            isDone = true;

            try
            {
                if (Home._is_active)
                {
                    Home.ns.Publish(
                        $"{Home.ns.cid}/{Home.ns.frqid}/codcnfrm/{txtUser.Text}",
                        "Done",
                        retained: false);
                }
            }
            catch { }

            Close();
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            GridControl2.ItemsSource = null;
            LoadDataToGridview();
        }

        #endregion

        #region ── Load Android Grid Data ─────────────────────

        public void LoadDataToGridview()
        {
            try
            {
                EnsureConnectionOpen(conn);

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT " +
                    "Ca.closeid, " +
                    "Ca.user_id, " +
                    "Ca.status, " +
                    "Employees.name, " +
                    "Ca.StartDate, " +
                    "Ca.EndDate, " +
                    "Ca.Cash, " +
                    "Ca.Network, " +
                    "Ca.Returncash, " +
                    "Ca.TotalNet, " +
                    "Ca.CountInv, " +
                    "Ca.CountinvR, " +
                    "Ca.CashValue, " +
                    "Ca.Diff, " +
                    "Ca.html " +
                    "FROM CahierClosedAndroid CA " +
                    "LEFT JOIN Employees ON Ca.user_id = Employees.id " +
                    "WHERE Ca.user_id = @user_id AND Ca.status = 0 " +
                    "GROUP BY Ca.closeid, Ca.user_id, Ca.status, " +
                    "Employees.name, Ca.StartDate, Ca.EndDate, " +
                    "Ca.Cash, Ca.Network, Ca.Returncash, Ca.TotalNet, " +
                    "Ca.CountInv, Ca.CountinvR, Ca.CashValue, Ca.Diff, Ca.html",
                    conn);

                adapter.SelectCommand.Parameters.Add(
                    "@user_id", SqlDbType.Int).Value = txtUser.Text;

                DataTable rawTable = new DataTable();
                adapter.Fill(rawTable);

                var gridRows = new List<AndroidCloseRow>();
                int rowNum = 1;

                foreach (DataRow row in rawTable.Rows)
                {
                    gridRows.Add(new AndroidCloseRow
                    {
                        RowNum = rowNum++,
                        CloseId = Convert.ToInt32(row["closeid"]),
                        EmployeeName = row["name"]?.ToString() ?? string.Empty,
                        StartDate = SafeToDateTime(row["StartDate"]),
                        EndDate = SafeToDateTime(row["EndDate"]),
                        Cash = SafeToDecimal(row["Cash"]),
                        Visa = SafeToDecimal(row["Network"]),
                        ReturnCash = SafeToDecimal(row["Returncash"]),
                        NetTotal = SafeToDecimal(row["TotalNet"]),
                        CountInv = SafeToInt(row["CountInv"]),
                        CountInvReturn = SafeToInt(row["CountinvR"]),
                        CashValue = SafeToDecimal(row["CashValue"]),
                        Diff = SafeToDecimal(row["Diff"])
                    });
                }

                GridControl2.ItemsSource = gridRows;
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء تحميل بيانات الأندرويد", ex);
            }
            finally
            {
                EnsureConnectionClosed(conn);
            }
        }

        private void GridControl2_SelectionChanged(
            object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
        }

        #endregion

        #region ── Done Button (تمت المطابقة) ─────────────────

        private void BtnDon_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button clickedButton &&
                clickedButton.Tag is AndroidCloseRow selectedRow)
            {
                ConfirmAndMatch(selectedRow);
            }
        }

        private void ConfirmAndMatch(AndroidCloseRow selectedRow)
        {
            try
            {
                MessageBoxResult confirm = MessageBox.Show(
                    "هل أنت متأكد من مطابقة بيانات الإغلاق؟",
                    "تأكيد المطابقة",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes)
                    return;

                EnsureConnectionOpen(conn);

                SqlCommand command = new SqlCommand(
                    "UPDATE CahierClosedAndroid " +
                    "SET status = 1 " +
                    "WHERE closeid = @closeid",
                    conn);

                command.Parameters.AddWithValue("@closeid", selectedRow.CloseId);
                command.ExecuteNonQuery();

                ShowSuccess("تمت المطابقة بنجاح ✅");

                if (Home._is_active)
                {
                    Home.ns.Publish(
                        $"{Home.ns.cid}/{Home.ns.frqid}/codcnfrm/{txtUser.Text}",
                        $"Done{selectedRow.CloseId}",
                        retained: false);
                }

                // إعادة تحميل البيانات
                LoadDataToGridview();
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء المطابقة", ex);
            }
            finally
            {
                EnsureConnectionClosed(conn);
            }
        }

        #endregion

        #region ── Safe Converters ────────────────────────────

        private static DateTime SafeToDateTime(object value)
        {
            try { return Convert.ToDateTime(value); }
            catch { return DateTime.MinValue; }
        }

        private static decimal SafeToDecimal(object value)
        {
            try { return Convert.ToDecimal(value?.ToString() ?? "0"); }
            catch { return 0m; }
        }

        private static int SafeToInt(object value)
        {
            try { return Convert.ToInt32(value?.ToString() ?? "0"); }
            catch { return 0; }
        }

        #endregion

        #region ── Connection Helpers ─────────────────────────

        private static void EnsureConnectionOpen(SqlConnection connection)
        {
            if (connection.State != ConnectionState.Open)
                connection.Open();
        }

        private static void EnsureConnectionClosed(SqlConnection connection)
        {
            if (connection.State != ConnectionState.Closed)
                connection.Close();
        }

        #endregion

        #region ── Message Helpers ────────────────────────────

        private static void ShowError(string message, Exception ex = null)
        {
            string detail = ex != null
                ? $"{Environment.NewLine}تفاصيل: {ex.Message}"
                : string.Empty;

            MessageBox.Show(
                message + detail,
                "❌ خطأ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private static void ShowSuccess(string message)
        {
            MessageBox.Show(
                message,
                "✅ نجاح",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        #endregion
    }

    #region ── Android Close Row Model ────────────────────────

    public class AndroidCloseRow
    {
        public int RowNum { get; set; }
        public int CloseId { get; set; }
        public string EmployeeName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Cash { get; set; }
        public decimal Visa { get; set; }
        public decimal ReturnCash { get; set; }
        public decimal NetTotal { get; set; }
        public int CountInv { get; set; }
        public int CountInvReturn { get; set; }
        public decimal CashValue { get; set; }
        public decimal Diff { get; set; }
    }

    #endregion
}