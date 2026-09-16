using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using DevExpress.Xpf.Core;

namespace SmartAuditERP.Form_WPF
{
    public partial class recPay : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private DataTable CustTable;

        #endregion

        #region Constructor

        public recPay()
        {
            InitializeComponent();
            conn      = MainClass.ConnObj();
            CustTable = new DataTable();
        }

        #endregion

        #region Public Methods

        public void FillCustCombo()
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter("SELECT name FROM Customers", conn);
                adapter.Fill(CustTable);
                EnsureClose(conn);

                CustCB.Items.Clear();
                foreach (DataRow row in CustTable.Rows)
                    CustCB.Items.Add(row[0].ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل العملاء: " + ex.Message, "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowMessage(string message)
        {
            MessageLB.Text = message;
        }

        #endregion

        #region Button Events

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            // Visa - تنفيذ المنطق الخاص
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            FillCustCombo();
            pnlCustomer.Visibility = Visibility.Visible;
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            pnlTable.Visibility = Visibility.Visible;
        }

        private void Button4_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Helpers

        private void EnsureOpen(SqlConnection c)
        { if (c.State != System.Data.ConnectionState.Open) c.Open(); }

        private void EnsureClose(SqlConnection c)
        { if (c.State != System.Data.ConnectionState.Closed) c.Close(); }

        #endregion

        #region Closing

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            EnsureClose(conn);
        }

        #endregion
    }
}