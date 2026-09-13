using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmItemsDetails : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;

        public int selectedCli { get; set; }
        public int StoreId { get; set; } = 1;
        public string sql { get; set; }
        public string Cond { get; set; }
        public int ProcCode { get; set; } = 1;
        public int ItemID { get; set; } = -1;
        public int InvType { get; set; } = 1;
        public bool ISDone { get; set; } = false;
        public int RowIndex { get; set; } = -1;
        public bool ISProfit { get; set; } = true;
        public int SelectedId { get; set; } = -1;

        private ObservableCollection<ItemDetailRow> _itemRows;

        #endregion

        #region Constructor

        public frmItemsDetails()
        {
            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            _itemRows = new ObservableCollection<ItemDetailRow>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmInvoiceDetails_Load(object sender, RoutedEventArgs e)
        {
            dgvItems.ItemsSource = _itemRows;
            ShowResults();
        }

        #endregion

        #region Show Results

        public void ShowResults()
        {
            try
            {
                _itemRows.Clear();

                double totalQty = 0.0;
                double totalAvgCost = 0.0;
                double totalItemSum = 0.0;
                double totalDiscount = 0.0;
                double totalFinalSum = 0.0;
                double totalProfit = 0.0;
                double totalCostSum = 0.0;
                double totalInvoiceSum = 0.0;

                string itemFilter = "";
                if (SelectedId != -1)
                    itemFilter = $" and Inv_Sub.ItemId={SelectedId}";

                itemFilter += " and (Inv.inv_type=2 or Inv.inv_type=3)";

                string branchFilter = MainClass.BranchNo != -1
                    ? $"inv.branch={MainClass.BranchNo} and "
                    : "";

                string mainSql =
                    "select Inv.safe, Inv.proc_type, Inv_sub.proc_type as ProcTypeSub, date, Inv.id, " +
                    "ItemId, val, val1, exchange_price, (exchange_price/UnitEquality) as price, " +
                    "(val*(exchange_price/UnitEquality)) as sum, Inv.Reff_No, Inv.inv_type, " +
                    "inv_sub.unit, AvrgCost, discount " +
                    "from Inv, Inv_Sub where " + branchFilter +
                    " Inv.InvGlobalID=Inv_Sub.InvGlobalID and Inv.IS_Deleted=0 " +
                    itemFilter + " order by date";

                SqlDataAdapter adapter = new SqlDataAdapter(mainSql, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                int rowNumber = 0;

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow row = dt.Rows[i];

                    int invType = Convert.ToInt32(row["inv_type"]);
                    int procType = Convert.ToInt32(row["proc_type"]);
                    int procTypeSub = Convert.ToInt32(row["ProcTypeSub"]);

                    string procTypeCondition = invType == 7
                        ? $"and ProcType={procTypeSub}"
                        : $"and ProcType={procType}";

                    SqlDataAdapter typeAdapter = new SqlDataAdapter(
                        $"select name, IsInput from InvTypes where InvType={invType} {procTypeCondition}",
                        conn1);
                    DataTable typeDt = new DataTable();
                    typeAdapter.Fill(typeDt);

                    if (typeDt.Rows.Count == 0) continue;

                    string invTypeName = typeDt.Rows[0]["name"]?.ToString() ?? "";

                    double val1 = Convert.ToDouble(row["val1"]);
                    double exchPrice = Convert.ToDouble(row["exchange_price"]);
                    double avrgCost = Math.Round(Convert.ToDouble(row["AvrgCost"]), 2);
                    double val = Convert.ToDouble(row["val"]);
                    double discount = Convert.ToDouble(row["discount"]);

                    double itemSum = Math.Round(val1 * exchPrice, 2);
                    double finalSum = Math.Round(itemSum - discount, 2);
                    double costTotal = Math.Round(val * avrgCost, 3);
                    double profit = Math.Round(finalSum - costTotal, 2);
                    double profitPct = costTotal != 0
                        ? Math.Round(profit / costTotal * 100.0, 2)
                        : 0.0;

                    rowNumber++;

                    ItemDetailRow newRow = new ItemDetailRow
                    {
                        Col0 = rowNumber.ToString(),
                        Col1 = "",
                        Col2 = row["id"].ToString(),
                        Col3 = Common.GetItemName(Convert.ToInt32(row["ItemId"])),
                        Col4 = invTypeName,
                        Col5 = row["id"].ToString(),
                        Col6 = Common.GetUnitName(Convert.ToInt32(row["unit"])),
                        Col7 = val1.ToString("N2"),
                        Col8 = avrgCost.ToString("N2"),
                        Col9 = Math.Round(val1 * avrgCost, 2).ToString("N2"),
                        Col10 = exchPrice.ToString("N2"),
                        Col11 = itemSum.ToString("N2"),
                        Col12 = discount.ToString("0.00"),
                        Col13 = finalSum.ToString("0.00"),
                        Col14 = profit.ToString("0.00"),
                        Col15 = profitPct.ToString("0.00") + "%"
                    };

                    _itemRows.Add(newRow);

                    bool isReturn = invTypeName.Contains("مرتجع");

                    if (isReturn)
                    {
                        totalQty -= val1;
                        totalAvgCost -= avrgCost;
                        totalFinalSum -= finalSum;
                        totalDiscount -= discount;
                        totalProfit -= profit;
                        totalCostSum -= Math.Round(val1 * avrgCost, 2);
                        totalInvoiceSum -= itemSum;
                    }
                    else
                    {
                        totalQty += val1;
                        totalAvgCost += avrgCost;
                        totalFinalSum += finalSum;
                        totalDiscount += discount;
                        totalProfit += profit;
                        totalCostSum += Math.Round(val1 * avrgCost, 2);
                        totalInvoiceSum += itemSum;
                    }
                }

                dgvItems.ItemsSource = _itemRows;

                txtTotalQty.Text = Math.Round(totalQty, 2).ToString();
                txtTotalCost.Text = Math.Round(totalAvgCost, 2).ToString();
                txtTotal.Text = Math.Round(totalInvoiceSum, 2).ToString();
                txtTotalDisc.Text = Math.Round(totalDiscount, 2).ToString();
                txtSum.Text = Math.Round(totalFinalSum, 2).ToString();
                txtTotalProfit.Text = Math.Round(totalProfit, 2).ToString();
                TxtTotCost.Text = Math.Round(totalCostSum, 2).ToString();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region Events

        private void dgvItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                RowIndex = -1;
                if (dgvItems.SelectedItem != null)
                    RowIndex = dgvItems.Items.IndexOf(dgvItems.SelectedItem);
            }
            catch { }
        }

        private void dgvItems_CellClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // للتوسع المستقبلي
        }

        private void frmItemsSrch_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Return)
                {
                    if (RowIndex > -1)
                    {
                        try
                        {
                            ItemDetailRow selected = _itemRows[RowIndex];
                            ISDone = true;
                            int.TryParse(selected.Col2, out int id);
                            ItemID = id;
                            Close();
                            return;
                        }
                        catch { return; }
                    }

                    if (dgvItems.Items.Count > 0)
                    {
                        dgvItems.SelectedItem = dgvItems.Items[0];
                    }
                }
                else if (e.Key == Key.Up)
                {
                    BtnUpFunc();
                }
                else if (e.Key == Key.Down)
                {
                    BtnDownFunc();
                }
            }
            catch { }
        }

        private void BtnUpFunc()
        {
            try
            {
                if (dgvItems.SelectedIndex > 0)
                    dgvItems.SelectedIndex = dgvItems.SelectedIndex - 1;
            }
            catch { }
        }

        private void BtnDownFunc()
        {
            try
            {
                if (dgvItems.SelectedIndex < dgvItems.Items.Count - 1)
                    dgvItems.SelectedIndex = dgvItems.SelectedIndex + 1;
            }
            catch { }
        }

        private void frmItemsSrch_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // للتوسع المستقبلي
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Helpers

        private string GetItemName(int id)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select name from Items where id=" + id, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        private string GetItemCode(int id)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select Code from Items where id=" + id, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        private string GetUnitName(int id)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select name from units where id=" + id, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        private void ShowError(Exception ex)
        {
            DXMessageBox.Show("خطأ" + Environment.NewLine + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }

    public class ItemDetailRow
    {
        public string Col0 { get; set; }
        public string Col1 { get; set; }
        public string Col2 { get; set; }
        public string Col3 { get; set; }
        public string Col4 { get; set; }
        public string Col5 { get; set; }
        public string Col6 { get; set; }
        public string Col7 { get; set; }
        public string Col8 { get; set; }
        public string Col9 { get; set; }
        public string Col10 { get; set; }
        public string Col11 { get; set; }
        public string Col12 { get; set; }
        public string Col13 { get; set; }
        public string Col14 { get; set; }
        public string Col15 { get; set; }
    }
}