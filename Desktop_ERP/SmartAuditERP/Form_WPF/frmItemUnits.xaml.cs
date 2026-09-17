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
    public partial class frmItemUnits : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;

        public int selectedCli { get; set; }
        public string sql { get; set; }
        public string search { get; set; }
        public int ItemId { get; set; }
        public int PreUnit { get; set; }
        public double ItemQuan { get; set; } = 1.0;
        public bool ISDone { get; set; } = false;
        public string Unitname { get; set; }
        public int UnitId { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quan { get; set; }

        private ObservableCollection<UnitRow> _unitRows;

        #endregion

        #region Constructor

        public frmItemUnits()
        {
            conn = MainClass.ConnObj();
            _unitRows = new ObservableCollection<UnitRow>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void ShowItemUnits_Load(object sender, RoutedEventArgs e)
        {
            LoadUnits();
            GridControl1.Focus();

            if (GridControl1.Items.Count > 0)
                GridControl1.SelectedIndex = 0;
        }

        private void LoadUnits()
        {
            try
            {
                _unitRows.Clear();

                string sql = $"select unit,perc,sale from ItemUnits where ItemId={ItemId} and unit<>{PreUnit}";
                SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    int unitId = Convert.ToInt32(row["unit"]);

                    _unitRows.Add(new UnitRow
                    {
                        Unit = GetUnitName(unitId),
                        UnitEql = row["perc"].ToString(),
                        unitsPrice = ToDoubleSafe(row["sale"]),
                        UnitNo = unitId.ToString()
                    });
                }

                GridControl1.ItemsSource = _unitRows;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region Selection

        private void SelectUnit()
        {
            try
            {
                UnitRow selected = GridControl1.SelectedItem as UnitRow;
                if (selected == null) return;

                int unitNo = 0;
                int.TryParse(selected.UnitNo, out unitNo);

                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select ItemUnits.perc from Items,ItemUnits " +
                    $"where Items.id=ItemUnits.ItemId and ItemUnits.unit={unitNo} and ItemUnits.ItemId={ItemId}",
                    conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    double.TryParse(dt.Rows[0][0]?.ToString(), out double quan);
                    ItemQuan = quan;
                }

                Unitname = selected.Unit;
                UnitId = unitNo;
                UnitPrice = (decimal)selected.unitsPrice;
                Quan = 1;

                Close();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region Events

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (GridControl1.SelectedIndex > -1)
                SelectUnit();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GridControl1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectUnit();
        }

        private void GridControl1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                SelectUnit();
        }

        private void frmItemsSrch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void ShowItemUnits_Leave(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Helpers

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

        private int GetUnitID(string name)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id from units where name='" + name + "'", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : 0;
            }
            catch { return 0; }
        }

        private double ToDoubleSafe(object value)
        {
            if (value == null || value == DBNull.Value) return 0.0;
            double.TryParse(value.ToString(), out double result);
            return result;
        }

        private void ShowError(Exception ex)
        {
            DXMessageBox.Show("خطأ" + Environment.NewLine + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }

    public class UnitRow
    {
        public string Unit { get; set; }
        public string UnitEql { get; set; }
        public double unitsPrice { get; set; }
        public string UnitNo { get; set; }
    }
}