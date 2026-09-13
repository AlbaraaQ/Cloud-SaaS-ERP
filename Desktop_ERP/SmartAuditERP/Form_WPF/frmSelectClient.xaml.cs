using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Button = System.Windows.Controls.Button;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSelectClient : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private ObservableCollection<ClientRow> ClientList;

        #endregion

        #region Constructor

        public frmSelectClient()
        {
            InitializeComponent();
            conn       = MainClass.ConnObj();
            ClientList = new ObservableCollection<ClientRow>();
            dgvData.ItemsSource = ClientList;
        }

        #endregion

        #region Window Loaded

        private void Window_Loaded(object sender, RoutedEventArgs e)
            => LoadData();

        #endregion

        #region Load Data

        private string GetAccountNo(string name)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index WHERE AName='{name}'",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0]["Code"]?.ToString() ?? "" : "";
            }
            catch { return ""; }
        }

        private void LoadData()
        {
            ClientList.Clear();
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name, type FROM customers WHERE is_deleted=0",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    int type = Convert.ToInt32(row["type"]);
                    string typeName = "";
                    bool include = false;

                    if (rdAll.IsChecked == true)
                    {
                        include  = true;
                        typeName = type == 1 ? "عميل"
                                 : type == 2 ? "مورد"
                                 : type == 3 ? "مالك" : "";
                    }
                    else if (rdClient.IsChecked == true && type == 1)
                    {
                        include = true; typeName = "عميل";
                    }
                    else if (rdSupplier.IsChecked == true && type == 2)
                    {
                        include = true; typeName = "مورد";
                    }
                    else if (rdOwner.IsChecked == true && type == 3)
                    {
                        include = true; typeName = "مالك";
                    }

                    if (include)
                    {
                        string name = row["name"]?.ToString() ?? "";
                        ClientList.Add(new ClientRow
                        {
                            CustomerId   = Convert.ToInt32(row["id"]),
                            TypeName     = typeName,
                            AccountNo    = GetAccountNo(name),
                            CustomerName = name,
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Events

        private void TypeFilter_Changed(object sender, RoutedEventArgs e)
            => LoadData();

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ClientRow item)
            {
                frmCustomers._selectedacc = item.AccountNo;
                Close();
            }
        }

        private void dgvData_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvData.SelectedItem is ClientRow item)
            {
                frmCustomers._selectedacc = item.AccountNo;
                Close();
            }
        }

        #endregion
    }

    public class ClientRow
    {
        public int    CustomerId   { get; set; }
        public string TypeName     { get; set; } = "";
        public string AccountNo    { get; set; } = "";
        public string CustomerName { get; set; } = "";
    }
}