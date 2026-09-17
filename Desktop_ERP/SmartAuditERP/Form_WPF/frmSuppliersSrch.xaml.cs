using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSuppliersSrch : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private ObservableCollection<SupplierSearchModel> allSuppliers;

        public string SelectedSupplierNo      { get; private set; }
        public string selectedSupplierName    { get; private set; }
        public string selectedSuppliertaxno   { get; private set; }

        #endregion

        #region Constructor

        public frmSuppliersSrch()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
            allSuppliers = new ObservableCollection<SupplierSearchModel>();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loadDgvItems();
        }

        #endregion

        #region Load Data

        public void loadDgvItems()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, Name, taxno FROM Suppliers WHERE Is_Deleted=0", conn);
                var table = new DataTable();
                adapter.Fill(table);

                allSuppliers.Clear();
                foreach (DataRow row in table.Rows)
                {
                    allSuppliers.Add(new SupplierSearchModel
                    {
                        id    = row["id"].ToString(),
                        name  = row["Name"].ToString(),
                        taxno = row["taxno"].ToString()
                    });
                }
                Grd1.ItemsSource = allSuppliers;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Search

        private void txtSrchNm_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string searchText = txtSrchNm.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                Grd1.ItemsSource = allSuppliers;
                return;
            }

            var filtered = new ObservableCollection<SupplierSearchModel>(
                allSuppliers.Where(s =>
                    (s.name  ?? "").ToLower().Contains(searchText) ||
                    (s.taxno ?? "").ToLower().Contains(searchText)));

            Grd1.ItemsSource = filtered;
        }

        #endregion

        #region Grid Double Click

        private void Grd1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Grd1.SelectedItem is SupplierSearchModel selected)
            {
                SelectedSupplierNo    = selected.id;
                selectedSupplierName  = selected.name;
                selectedSuppliertaxno = selected.taxno;
                this.Close();
            }
        }

        #endregion

        #region Buttons

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnAddSupplier_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var suppliersWindow = new frmSuppliers();
                MainClass.ApplyPermissionToForm(suppliersWindow);
                MainClass.DoApplyUserSett(suppliersWindow);
                suppliersWindow.ShowDialog();

                if (suppliersWindow.IsDone)
                    loadDgvItems();
            }
            catch
            {
                // تجاهل الأخطاء غير الحرجة
            }
        }

        #endregion
    }

    #region Model

    public class SupplierSearchModel
    {
        public string id    { get; set; }
        public string name  { get; set; }
        public string taxno { get; set; }
    }

    #endregion
}