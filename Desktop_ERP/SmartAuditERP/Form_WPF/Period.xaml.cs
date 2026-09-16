using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using DevExpress.Xpf.Core;

namespace SmartAuditERP.Form_WPF
{
    public partial class Period : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        public int     ItemId      { get; set; }
        public double  ItemQuan    { get; set; } = 1.0;
        public string  Periodname  { get; set; } = "";
        public decimal PeriodPrice { get; set; }
        public int     offer       { get; set; }

        private ObservableCollection<PeriodItem> _periods;

        #endregion

        #region Constructor

        public Period()
        {
            InitializeComponent();
            conn     = MainClass.ConnObj();
            _periods = new ObservableCollection<PeriodItem>();
            dvgPeriod.ItemsSource = _periods;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e) => LoadPeriod();

        private void Window_Deactivated(object sender, EventArgs e) => this.Close();

        #endregion

        #region Data Loading

        private void LoadPeriod()
        {
            try
            {
                _periods.Clear();
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    $"SELECT * FROM RentPeriodSub WHERE MGroupID={ItemId}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                EnsureClose(conn);

                foreach (DataRow row in dt.Rows)
                {
                    int periodId = Convert.ToInt32(row["periodID"]);
                    _periods.Add(new PeriodItem
                    {
                        PeriodId   = periodId,
                        PeriodName = GetPeriodName(periodId),
                        Offer      = row["offer"] != DBNull.Value ? Convert.ToInt32(row["offer"]) : 0,
                        Rent       = row["rent"]  != DBNull.Value ? Convert.ToDecimal(row["rent"]) : 0m
                    });
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المدد: " + ex.Message, "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetPeriodName(int id)
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    $"SELECT name FROM RentPeriod WHERE id={id}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                EnsureClose(conn);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { EnsureClose(conn); return ""; }
        }

        #endregion

        #region DataGrid Events

        private void dvgPeriod_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dvgPeriod.SelectedItem is PeriodItem item)
            {
                Periodname  = item.PeriodName;
                offer       = item.Offer;
                PeriodPrice = item.Rent;
                this.Close();
            }
        }

        #endregion

        #region Button Events

        private void btnAddPeriod_Click(object sender, RoutedEventArgs e)
        {
            // frmAddPeriod frm = new frmAddPeriod();
            // frm.Topmost = true;
            // frm.ShowDialog();
            DXMessageBox.Show("افتح نافذة إضافة مدة هنا", "إضافة",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

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

    public class PeriodItem
    {
        public int     PeriodId   { get; set; }
        public string  PeriodName { get; set; }
        public int     Offer      { get; set; }
        public decimal Rent       { get; set; }
    }
}