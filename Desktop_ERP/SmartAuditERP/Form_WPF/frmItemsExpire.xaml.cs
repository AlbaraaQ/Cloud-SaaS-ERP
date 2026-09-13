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
    public partial class frmItemsExpire : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;

        public DataTable dt { get; set; }
        public int SelectedID { get; set; } = 0;
        public DateTime ExpireDate { get; set; }

        private ObservableCollection<ExpireRow> _expireRows;

        #endregion

        #region Constructor

        public frmItemsExpire()
        {
            conn = MainClass.ConnObj();
            dt = new DataTable();
            _expireRows = new ObservableCollection<ExpireRow>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmSafeGrd_Load(object sender, RoutedEventArgs e)
        {
            LoadExpireItems();
        }

        private void LoadExpireItems()
        {
            try
            {
                _expireRows.Clear();

                string extraFilter = "";
                if (SelectedID != 0)
                    extraFilter = $" and ItemId={SelectedID}";

                DataTable dataTable = Inventory.ItemsExpirationStock(
                    extraFilter + " and BranchId=" + MainClass.BranchNo)?.Copy();

                if (dataTable == null || dataTable.Rows.Count == 0)
                {
                    GridControl1.ItemsSource = _expireRows;
                    return;
                }

                foreach (DataRow row in dataTable.Rows)
                {
                    _expireRows.Add(new ExpireRow
                    {
                        ItemCode = row["ItemCode"].ToString(),
                        ItemName = row["ItemName"].ToString(),
                        ItemStock = row["ItemStock"].ToString(),
                        ItemExpire = row["ItemExpire"] == DBNull.Value
                            ? ""
                            : Convert.ToDateTime(row["ItemExpire"]).ToShortDateString(),
                        ItemId = row["ItemId"].ToString()
                    });
                }

                GridControl1.ItemsSource = _expireRows;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region Selection

        private void SelectedDate()
        {
            try
            {
                ExpireRow selected = GridControl1.SelectedItem as ExpireRow;
                if (selected == null) return;

                DateTime.TryParse(selected.ItemExpire, out DateTime expDate);
                ExpireDate = expDate;
                Close();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region Events

        private void GridControl1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectedDate();
        }

        private void GridView1_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Return)
                    SelectedDate();
            }
            catch { }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Helpers

        private void ShowError(Exception ex)
        {
            DXMessageBox.Show("خطأ" + Environment.NewLine + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }

    public class ExpireRow
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string ItemStock { get; set; }
        public string ItemExpire { get; set; }
        public string ItemId { get; set; }
    }
}