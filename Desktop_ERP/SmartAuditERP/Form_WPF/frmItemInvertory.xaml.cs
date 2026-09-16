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
    public partial class frmItemInvertory : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;

        public string InvertoryName { get; set; } = "";
        public short InvertoryId { get; set; } = 0;
        public int ItemId { get; set; } = 0;
        public decimal ItemQty { get; set; } = 0;

        private ObservableCollection<InvertoryRow> _rows;

        #endregion

        #region Constructor

        public frmItemInvertory()
        {
            conn = MainClass.ConnObj();
            _rows = new ObservableCollection<InvertoryRow>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmItemInvertory_Load(object sender, RoutedEventArgs e)
        {
            LoadInvertory();
            GridControl1.Focus();

            if (GridControl1.Items.Count > 0)
                GridControl1.SelectedIndex = 0;
        }

        private void LoadInvertory()
        {
            try
            {
                _rows.Clear();

                DataTable inventoryTable = LoadData.Invertories(MainClass.EmpNo);

                foreach (DataRow row in inventoryTable.Rows)
                {
                    int inventoryId = Convert.ToInt32(row["id"]);
                    string inventoryName = row["name"].ToString();

                    double qty = Inventory.CalcItemStock(inventoryId, ItemId, MainClass.BranchNo);

                    _rows.Add(new InvertoryRow
                    {
                        InvertoryId2 = inventoryId.ToString(),
                        InvertoryName2 = inventoryName,
                        ItemQty2 = qty.ToString("N2")
                    });
                }

                GridControl1.ItemsSource = _rows;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المستودعات" + Environment.NewLine + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Selection

        private void SelectUnit()
        {
            try
            {
                InvertoryRow selected = GridControl1.SelectedItem as InvertoryRow;
                if (selected == null) return;

                short id = 0;
                short.TryParse(selected.InvertoryId2, out id);

                InvertoryId = id;
                InvertoryName = selected.InvertoryName2;
                decimal.TryParse(selected.ItemQty2, out decimal qty);
                ItemQty = qty;

                Close();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ" + Environment.NewLine + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Events

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
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

        private void frmItemInvertory_Deactivated(object sender, EventArgs e)
        {
            Close();
        }

        #endregion
    }

    public class InvertoryRow
    {
        public string InvertoryId2 { get; set; }
        public string InvertoryName2 { get; set; }
        public string ItemQty2 { get; set; }
    }
}