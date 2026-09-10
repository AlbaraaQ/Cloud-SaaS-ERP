using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmNkdTotSalePurchActCust : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private ObservableCollection<SalePurchActRow> _rows;

        #endregion

        #region Constructor

        public frmNkdTotSalePurchActCust()
        {
            conn  = MainClass.ConnObj();
            _rows = new ObservableCollection<SalePurchActRow>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmNkdTotSalePurchActCust_Load(object sender, RoutedEventArgs e)
        {
            txtDateFrom.SelectedDate = DateTime.Today;
            txtDateTo.SelectedDate   = DateTime.Today;
            dgvItems.ItemsSource     = _rows;
            LoadActs();
        }

        public void LoadActs()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter("select id,name from Acts order by id", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbActs.ItemsSource = dt.DefaultView;
                cmbActs.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region CheckBox Events

        private void chkAllActs_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbActs == null) return;

            if (chkAllActs.IsChecked == true)
            {
                cmbActs.IsEnabled  = false;
                cmbCust.IsEnabled  = false;
                if (chkAllCust != null)
                {
                    chkAllCust.IsChecked = true;
                    chkAllCust.IsEnabled = false;
                }
            }
            else
            {
                cmbActs.IsEnabled  = true;
                cmbCust.IsEnabled  = true;
                if (chkAllCust != null)
                {
                    chkAllCust.IsChecked = false;
                    chkAllCust.IsEnabled = true;
                }
            }
        }

        private void chkAllCust_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbCust == null) return;
            cmbCust.IsEnabled = chkAllCust.IsChecked != true;
        }

        private void cmbActs_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbActs.SelectedValue == null) return;

                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select id,name from customers where act={cmbActs.SelectedValue} order by id", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbCust.ItemsSource = dt.DefaultView;
                cmbCust.SelectedIndex = -1;
            }
            catch { }
        }

        #endregion

        #region Show Result

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            if (chkAllActs.IsChecked != true && cmbActs.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار جهة العمل", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbActs.Focus();
                return;
            }

            if (chkAllCust.IsEnabled && chkAllCust.IsChecked != true && cmbCust.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار العميل", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbCust.Focus();
                return;
            }

            _rows.Clear();
            txtSumPurch.Text = "0.00";
            txtSumSale.Text  = "0.00";

            Thread workerThread = new Thread(CalcStock);
            workerThread.SetApartmentState(ApartmentState.STA);
            workerThread.IsBackground = true;
            workerThread.Start();
        }

        private void CalcStock()
        {
            try
            {
                SqlDataAdapter itemAdapter = new SqlDataAdapter(
                    "select id,name,nameEN from Items where IS_Deleted=0 order by id", conn);
                DataTable itemsDt = new DataTable();
                itemAdapter.Fill(itemsDt);

                double totalPurch = 0.0, totalSale = 0.0;

                string actFilter = "";
                string custFilter = "";
                string branchFilter = MainClass.BranchNo != -1
                    ? $"inv.branch={MainClass.BranchNo} and "
                    : "";

                Dispatcher.Invoke(() =>
                {
                    if (chkAllActs.IsChecked != true && cmbActs.SelectedValue != null)
                        actFilter = $"acts.id={cmbActs.SelectedValue} and ";

                    if (chkAllCust.IsChecked != true && cmbCust.SelectedValue != null)
                        custFilter = $"customers.id={cmbCust.SelectedValue} and ";

                    ProgressBar1.Visibility = Visibility.Visible;
                    ProgressBar1.Maximum    = itemsDt.Rows.Count;
                    ProgressBar1.Value      = 0;
                });

                DateTime dtFrom = DateTime.Today, dtTo = DateTime.Today;
                Dispatcher.Invoke(() =>
                {
                    dtFrom = txtDateFrom.SelectedDate ?? DateTime.Today;
                    dtTo   = (txtDateTo.SelectedDate ?? DateTime.Today).AddHours(24);
                });

                for (int i = 0; i < itemsDt.Rows.Count; i++)
                {
                    object itemId   = itemsDt.Rows[i]["id"];
                    string itemName = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                        ? itemsDt.Rows[i]["name"].ToString()
                        : itemsDt.Rows[i]["nameEN"].ToString();

                    string baseWhere = $"inv,inv_sub,customers,acts where {branchFilter} Inv_Sub.ItemId={itemId} and inv.cust_id=customers.id and customers.act=acts.id and {actFilter}{custFilter} date>=@date1 and date<=@date2";

                    double pQty = 0, pTotal = 0, pRetQty = 0, pRetTotal = 0;
                    double sQty = 0, sTotal = 0, sRetQty = 0, sRetTotal = 0;

                    ExecuteItemQuery($"select sum(val),sum(val*exchange_price) from {baseWhere} and inv.proc_type=1 and inv_sub.proc_type=1 and inv.InvGlobalID=inv_sub.InvGlobalID and inv.IS_Deleted=0",
                        conn, dtFrom.ToShortDateString(), dtTo.ToString(), ref pQty, ref pTotal);

                    ExecuteItemQuery($"select sum(val),sum(val*exchange_price) from {baseWhere} and inv.proc_type=2 and inv_sub.proc_type=1 and inv.InvGlobalID=inv_sub.InvGlobalID and inv.IS_Deleted=0",
                        conn, dtFrom.ToShortDateString(), dtTo.ToString(), ref pRetQty, ref pRetTotal);

                    ExecuteItemQuery($"select sum(val),sum(val*exchange_price) from {baseWhere} and inv.proc_type=1 and inv_sub.proc_type=2 and inv.InvGlobalID=inv_sub.InvGlobalID and inv.IS_Deleted=0",
                        conn, dtFrom.ToShortDateString(), dtTo.ToString(), ref sQty, ref sTotal);

                    ExecuteItemQuery($"select sum(val),sum(val*exchange_price) from {baseWhere} and inv.proc_type=2 and inv_sub.proc_type=2 and inv.InvGlobalID=inv_sub.InvGlobalID and inv.IS_Deleted=0",
                        conn, dtFrom.ToShortDateString(), dtTo.ToString(), ref sRetQty, ref sRetTotal);

                    double netPQty   = pQty   - pRetQty;
                    double netPTotal = pTotal  - pRetTotal;
                    double netSQty   = sQty    - sRetQty;
                    double netSTotal = sTotal  - sRetTotal;

                    if (netPQty != 0.0 || netSQty != 0.0)
                    {
                        totalPurch += netPTotal;
                        totalSale  += netSTotal;

                        Dispatcher.Invoke(() =>
                        {
                            _rows.Add(new SalePurchActRow
                            {
                                Column1 = itemsDt.Rows[i]["id"].ToString(),
                                Column2 = itemName,
                                Column3 = netPQty.ToString("N4"),
                                Column4 = netPTotal.ToString("N4"),
                                Column6 = netSQty.ToString("N4"),
                                Column5 = netSTotal.ToString("N4")
                            });
                        });
                    }

                    Dispatcher.Invoke(() => ProgressBar1.Value++);
                }

                Dispatcher.Invoke(() =>
                {
                    txtSumPurch.Text        = totalPurch.ToString("N2");
                    txtSumSale.Text         = totalSale.ToString("N2");
                    ProgressBar1.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowError(ex));
            }
        }

        private void ExecuteItemQuery(string sql, SqlConnection conn, string date1, string date2,
            ref double qty, ref double total)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                adapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = date1;
                adapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = date2;
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0 && !string.IsNullOrEmpty(dt.Rows[0][0]?.ToString()))
                {
                    double.TryParse(dt.Rows[0][0].ToString(), out qty);
                    double.TryParse(dt.Rows[0][1].ToString(), out total);
                    total = Math.Round(total, 4);
                }
            }
            catch { }
        }

        #endregion

        #region Print (Stubs)

        private void btnPreview_Click(object sender, RoutedEventArgs e) { }
        private void btnPrint_Click(object sender, RoutedEventArgs e)   { }

        #endregion

        #region Close & Helpers

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void ShowError(Exception ex)
        {
            DXMessageBox.Show("خطأ" + Environment.NewLine + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }

    public class SalePurchActRow
    {
        public string Column1 { get; set; }
        public string Column2 { get; set; }
        public string Column3 { get; set; }
        public string Column4 { get; set; }
        public string Column6 { get; set; }
        public string Column5 { get; set; }
    }
}