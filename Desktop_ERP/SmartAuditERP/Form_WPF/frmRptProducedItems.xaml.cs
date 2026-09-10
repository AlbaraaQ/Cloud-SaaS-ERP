using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using DevExpress.Xpf.Core;
using Microsoft.VisualBasic.CompilerServices;
using UtilitiesProj;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptProducedItems : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private bool _Finished;

        private ObservableCollection<ProducedItemRow>   _itemsData
            = new ObservableCollection<ProducedItemRow>();
        private ObservableCollection<ComponentItemRow>  _componentData
            = new ObservableCollection<ComponentItemRow>();

        // مجاميع
        private double _grandTotalCost  = 0.0;
        private double _grandTotalSale  = 0.0;
        private double _grandTotalStock = 0.0;

        #endregion

        #region Constructor

        public frmRptProducedItems()
        {
            InitializeComponent();
            conn      = MainClass.ConnObj();
            _Finished = false;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDateFrom.DateTime = DateTime.Now;
            txtDateTo.DateTime   = DateTime.Now;

            dgvItems.ItemsSource     = _itemsData;
            dgvComponent.ItemsSource = _componentData;
        }

        #endregion

        #region CalcStock - حساب مواد الإنتاج

        public void CalcStock()
        {
            try
            {
                _itemsData.Clear();
                _grandTotalCost  = 0.0;
                _grandTotalSale  = 0.0;
                _grandTotalStock = 0.0;

                string branchFilter = "";
                if (MainClass.BranchNo != -1)
                    branchFilter = $"inv.branch={MainClass.BranchNo} AND ";

                string sql =
                    $"SELECT ItemId, SUM(val) AS Quantity, " +
                    $"SUM(val1 * exchange_price) AS total " +
                    $"FROM inv, inv_sub, Items, ItemsCategory " +
                    $"WHERE {branchFilter}" +
                    $" inv_sub.ItemId=Items.id " +
                    $" AND Items.group_id=ItemsCategory.id " +
                    $" AND inv.inv_type=6 AND inv.proc_type=1 " +
                    $" AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                    $" AND inv_Sub.proc_type=1 " +
                    $" AND date>=@date1 AND date<=@date2 " +
                    $" AND inv.IS_Deleted=0 " +
                    $"GROUP BY ItemId";

                var adapter = new SqlDataAdapter(sql, conn);
                adapter.SelectCommand.Parameters.Add(
                    "@date1", SqlDbType.DateTime).Value
                    = txtDateFrom.DateTime.ToShortDateString();
                DateTime dateTo2 = txtDateTo.DateTime.AddHours(24.0);
                adapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value = dateTo2;

                var dt = new DataTable();
                adapter.Fill(dt);

                ProgressBar1.Maximum = Math.Max(dt.Rows.Count, 1);
                ProgressBar1.Value   = 0;

                int rowNum = 1;

                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        int    itemId  = Convert.ToInt32(row["ItemId"]);
                        double qty     = ParseDouble(row["Quantity"]);
                        double total   = ParseDouble(row["total"]);
                        double price   = qty > 0 ? total / qty : 0.0;

                        // جلب اسم الصنف والوحدة
                        var adItem = new SqlDataAdapter(
                            $"SELECT Items.name, Items.nameEN, units.name AS Uname " +
                            $"FROM Items, units " +
                            $"WHERE Units.id=Items.unit AND items.IS_Deleted=0 " +
                            $"AND Items.id={itemId}", conn);
                        var dtItem = new DataTable();
                        adItem.Fill(dtItem);

                        if (dtItem.Rows.Count == 0) continue;

                        string itemName = string.Equals(MainClass.Language, "ar",
                            StringComparison.OrdinalIgnoreCase)
                            ? dtItem.Rows[0]["name"].ToString()
                            : dtItem.Rows[0]["nameEN"].ToString();
                        string unitName = dtItem.Rows[0]["Uname"].ToString();

                        _itemsData.Add(new ProducedItemRow
                        {
                            RowNum   = rowNum++,
                            ItemId   = itemId,
                            ItemName = itemName,
                            UnitName = unitName,
                            Quantity = qty,
                            Price    = Math.Round(price, 4),
                            Total    = Math.Round(total, 4),
                        });

                        _grandTotalStock += qty;
                        _grandTotalCost  += total;

                        ProgressBar1.Value++;
                        System.Windows.Application.Current.Dispatcher.Invoke(
                            System.Windows.Threading.DispatcherPriority.Background,
                            new Action(() => { }));
                    }
                    catch { /* تجاهل أخطاء الصفوف الفردية */ }
                }

                UpdateSummary();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء حساب المخزون:\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _Finished = true;
                ProgressBar1.Value = ProgressBar1.Maximum;
            }
        }

        #endregion

        #region Load Components لصنف محدد

        private void LoadComponents(int itemId)
        {
            try
            {
                _componentData.Clear();

                string branchFilter = "";
                if (MainClass.BranchNo != -1)
                    branchFilter = $"inv.branch={MainClass.BranchNo} AND ";

                string sql =
                    $"SELECT ComponentId, SUM(val) AS Quantity, " +
                    $"SUM(val1 * exchange_price) AS total " +
                    $"FROM inv, inv_sub " +
                    $"WHERE {branchFilter}" +
                    $" inv.inv_type=6 AND inv.proc_type=1 " +
                    $" AND inv.InvGlobalID=inv_sub.InvGlobalID " +
                    $" AND inv_Sub.proc_type=2 " +
                    $" AND ProductID={itemId} " +
                    $" AND date>=@date1 AND date<=@date2 " +
                    $" AND inv.IS_Deleted=0 " +
                    $"GROUP BY ComponentId";

                var adapter = new SqlDataAdapter(sql, conn);
                adapter.SelectCommand.Parameters.Add(
                    "@date1", SqlDbType.DateTime).Value
                    = txtDateFrom.DateTime.ToShortDateString();
                DateTime dateTo2 = txtDateTo.DateTime.AddHours(24.0);
                adapter.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value = dateTo2;

                var dt = new DataTable();
                adapter.Fill(dt);

                int rowNum = 1;
                foreach (DataRow row in dt.Rows)
                {
                    int    compId  = Convert.ToInt32(row["ComponentId"]);
                    double qty     = ParseDouble(row["Quantity"]);
                    double total   = ParseDouble(row["total"]);
                    double price   = qty > 0 ? total / qty : 0.0;

                    string compName = GetItemName(compId);
                    string unitName = GetUnitNameForItem(compId);

                    _componentData.Add(new ComponentItemRow
                    {
                        RowNum   = rowNum++,
                        ItemId   = compId,
                        ItemName = compName,
                        UnitName = unitName,
                        Quantity = qty,
                        Price    = Math.Round(price, 4),
                        Total    = Math.Round(total, 4),
                    });
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المكونات:\n" + ex.Message);
            }
        }

        #endregion

        #region Helper Methods

        private string GetItemName(int itemId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT name, nameEN FROM Items " +
                    $"WHERE IS_Deleted=0 AND id={itemId}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                    return string.Equals(MainClass.Language, "ar",
                        StringComparison.OrdinalIgnoreCase)
                        ? dt.Rows[0]["name"].ToString()
                        : dt.Rows[0]["nameEN"].ToString();
            }
            catch { }
            return "";
        }

        private string GetUnitNameForItem(int itemId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT units.name FROM Items " +
                    $"INNER JOIN units ON units.id=Items.unit " +
                    $"WHERE Items.IS_Deleted=0 AND Items.id={itemId}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                    return dt.Rows[0][0].ToString();
            }
            catch { }
            return "";
        }

        private string GetUnitName(int unitId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT name FROM units WHERE IS_Deleted=0 AND id={unitId}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                    return string.Equals(MainClass.Language, "ar",
                        StringComparison.OrdinalIgnoreCase)
                        ? dt.Rows[0][0].ToString()
                        : dt.Rows[0][0].ToString();
            }
            catch { }
            return "";
        }

        private double ParseDouble(object val)
        {
            if (val == null || val == DBNull.Value) return 0.0;
            return double.TryParse(val.ToString(), out double d) ? d : 0.0;
        }

        private void UpdateSummary()
        {
            txtItemQuan.Text = _itemsData.Count.ToString();
            txtTotStock.Text = _grandTotalStock.ToString("N2");
            txtSum.Text      = _grandTotalCost.ToString("N4");
            txtSum2.Text     = _grandTotalSale.ToString("N4");
        }

        private void Clr()
        {
            _itemsData.Clear();
            _componentData.Clear();
            _grandTotalCost  = 0.0;
            _grandTotalSale  = 0.0;
            _grandTotalStock = 0.0;
            UpdateSummary();
            ProgressBar1.Value = 0;
        }

        #endregion

        #region Button Events

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            Clr();
            CalcStock();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            // الطباعة غير مُنفَّذة في الكود الأصلي أيضًا
            DXMessageBox.Show("المعاينة غير مُعدّة بعد",
                "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            // الطباعة غير مُنفَّذة في الكود الأصلي أيضًا
            DXMessageBox.Show("الطباعة غير مُعدّة بعد",
                "", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void clear_Click(object sender, RoutedEventArgs e)
            => Clr();

        #endregion

        #region CheckBox & Grid Events

        private void CheckBox1_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool isDetailed = CheckBox1.IsChecked == true;

            if (isDetailed)
            {
                PnlComponents.Visibility = Visibility.Visible;
                colComponents.Width      = new GridLength(1, GridUnitType.Star);
            }
            else
            {
                PnlComponents.Visibility = Visibility.Collapsed;
                colComponents.Width      = new GridLength(0);
                _componentData.Clear();
            }
        }

        private void dgvItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CheckBox1.IsChecked == true &&
                dgvItems.SelectedItem is ProducedItemRow row)
            {
                LoadComponents(row.ItemId);
            }
        }

        #endregion
    }
}