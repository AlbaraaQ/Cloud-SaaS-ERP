using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmItemsBalances : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private ObservableCollection<BalanceRow> _balanceRows;

        #endregion

        #region Constructor

        public frmItemsBalances()
        {
            conn = MainClass.ConnObj();
            _balanceRows = new ObservableCollection<BalanceRow>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmCurrenciesBalances_Load(object sender, RoutedEventArgs e)
        {
            dgvItems.ItemsSource = _balanceRows;
        }

        #endregion

        #region CalcStock

        public void CalcStock(int safeId)
        {
            try
            {
                _balanceRows.Clear();

                DataTable balanceTable = new DataTable();
                balanceTable.Columns.Add("currency");
                balanceTable.Columns.Add("sum", typeof(double));
                balanceTable.Columns.Add("type", typeof(int));

                string branchFilter = MainClass.BranchNo != -1
                    ? $"inv.branch={MainClass.BranchNo} and "
                    : "";

                string safeFilter = $" inv.safe={safeId} and ";

                // proc_type=3 (تحويل)
                FillBalanceData(balanceTable, branchFilter, safeFilter,
                    $"select ItemId,sum(val) from inv,inv_sub where {branchFilter}{safeFilter} inv.proc_type=3 and inv.InvGlobalID=inv_sub.InvGlobalID and IS_Deleted=0 group by ItemId",
                    1);

                // proc_type=1 (شراء)
                FillBalanceData(balanceTable, branchFilter, safeFilter,
                    $"select ItemId,sum(val) from inv,inv_sub where {branchFilter}{safeFilter} inv.proc_type=1 and inv_sub.proc_type=1 and inv.InvGlobalID=inv_sub.InvGlobalID and IS_Deleted=0 group by ItemId",
                    1);

                // proc_type=2 (بيع - خصم)
                FillBalanceData(balanceTable, branchFilter, safeFilter,
                    $"select ItemId,sum(val) from inv,inv_sub where {branchFilter}{safeFilter} inv.proc_type=2 and inv_sub.proc_type=1 and inv.InvGlobalID=inv_sub.InvGlobalID and IS_Deleted=0 group by ItemId",
                    2);

                // proc_type=1+2 مرتجع شراء
                FillBalanceData(balanceTable, branchFilter, safeFilter,
                    $"select ItemId,sum(val) from inv,inv_sub where {branchFilter}{safeFilter} inv.proc_type=1 and inv_sub.proc_type=2 and inv.InvGlobalID=inv_sub.InvGlobalID and IS_Deleted=0 group by ItemId",
                    2);

                // proc_type=2+2 مرتجع بيع
                FillBalanceData(balanceTable, branchFilter, safeFilter,
                    $"select ItemId,sum(val) from inv,inv_sub where {branchFilter}{safeFilter} inv.proc_type=2 and inv_sub.proc_type=2 and inv.InvGlobalID=inv_sub.InvGlobalID and IS_Deleted=0 group by ItemId",
                    1);

                // تحويلات مستودع - وارد
                FillTransferData(balanceTable, branchFilter,
                    $"select currency,sum(ReceivedValue) from SafesTransfer,SafesTransfer_Sub where {branchFilter} SafesTransfer.safe_to={safeId} and SafesTransfer.id=SafesTransfer_Sub.transfer_id and IS_Deleted=0 group by currency",
                    1);

                // تحويلات مستودع - صادر
                FillTransferData(balanceTable, branchFilter,
                    $"select currency,sum(value) from SafesTransfer,SafesTransfer_Sub where {branchFilter} SafesTransfer.safe_from={safeId} and SafesTransfer.id=SafesTransfer_Sub.transfer_id and IS_Deleted=0 group by currency",
                    2);

                // دمج الصفوف بنفس الصنف
                MergeRows(balanceTable);

                // تعبئة الجدول النهائي
                foreach (DataRow row in balanceTable.Rows)
                {
                    try
                    {
                        int currencyId = Convert.ToInt32(row[0]);
                        double balance = Convert.ToDouble(row[1]);
                        string name = GetCurrencyName(currencyId);

                        _balanceRows.Add(new BalanceRow
                        {
                            Column1 = name,
                            Column2 = balance.ToString("N2")
                        });
                    }
                    catch { }
                }

                dgvItems.ItemsSource = _balanceRows;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في حساب الأرصدة" + Environment.NewLine + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FillBalanceData(DataTable table, string branchFilter,
            string safeFilter, string sql, int type)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                DataTable result = new DataTable();
                adapter.Fill(result);

                foreach (DataRow row in result.Rows)
                {
                    double val = 0;
                    double.TryParse(row[1]?.ToString(), out val);
                    table.Rows.Add(row[0], val, type);
                }
            }
            catch { }
        }

        private void FillTransferData(DataTable table, string branchFilter,
            string sql, int type)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                DataTable result = new DataTable();
                adapter.Fill(result);

                foreach (DataRow row in result.Rows)
                {
                    double val = 0;
                    double.TryParse(row[1]?.ToString(), out val);
                    table.Rows.Add(row[0], val, type);
                }
            }
            catch { }
        }

        private void MergeRows(DataTable table)
        {
            int count = table.Rows.Count;

            for (int i = 0; i < count; i++)
            {
                if (i >= table.Rows.Count) break;

                // عكس الخصم
                if (Convert.ToInt32(table.Rows[i][2]) == 2)
                {
                    double val = Convert.ToDouble(table.Rows[i][1]);
                    table.Rows[i][1] = -val;
                }

                int j = i + 1;
                while (j < table.Rows.Count)
                {
                    if (table.Rows[i][0].ToString() == table.Rows[j][0].ToString())
                    {
                        if (Convert.ToInt32(table.Rows[j][2]) == 2)
                        {
                            double val = Convert.ToDouble(table.Rows[j][1]);
                            table.Rows[j][1] = -val;
                        }

                        double sumI = Convert.ToDouble(table.Rows[i][1]);
                        double sumJ = Convert.ToDouble(table.Rows[j][1]);
                        table.Rows[i][1] = sumI + sumJ;
                        table.Rows.RemoveAt(j);
                    }
                    else
                    {
                        j++;
                    }
                }
            }
        }

        private string GetCurrencyName(int currencyId)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select name from Items where id=" + currencyId, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        #endregion

        #region Events

        private void ListView1_SelectedIndexChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // للتوسع المستقبلي
        }

        #endregion
    }

    public class BalanceRow
    {
        public string Column1 { get; set; }
        public string Column2 { get; set; }
    }
}