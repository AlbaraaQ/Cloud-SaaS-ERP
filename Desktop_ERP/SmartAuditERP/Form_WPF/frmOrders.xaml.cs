using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using ComboBox = System.Windows.Controls.ComboBox;
using FontFamily = System.Windows.Media.FontFamily;
using Orientation = System.Windows.Controls.Orientation;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmOrders : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private string connectionString;
        private ObservableCollection<OrderRow_frmOrders> _orderRows;

        #endregion

        #region Constructor

        public frmOrders()
        {
            connectionString = MainClass.connstr;
            _orderRows       = new ObservableCollection<OrderRow_frmOrders>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmOrders_Load(object sender, RoutedEventArgs e)
        {
            gridOrders.ItemsSource = _orderRows;
            LoadStatusFilter();
            LoadOrders();
        }

        private void LoadStatusFilter()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT StatusID, StatusName FROM OrderStatus WHERE IsActive=1 ORDER BY DisplayOrder",
                    connectionString);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                DataRow allRow = dt.NewRow();
                allRow["StatusID"]   = 0;
                allRow["StatusName"] = "الكل";
                dt.Rows.InsertAt(allRow, 0);

                cmbStatus.ItemsSource = dt.DefaultView;
                cmbStatus.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void LoadOrders()
        {
            try
            {
                string sql  = "SELECT * FROM vw_OrdersComplete WHERE 1=1";
                var   parms = new List<SqlParameter>();

                if (cmbStatus.SelectedValue != null && Convert.ToInt32(cmbStatus.SelectedValue) > 0)
                {
                    sql += " AND StatusID=@StatusID";
                    parms.Add(new SqlParameter("@StatusID", cmbStatus.SelectedValue));
                }

                if (dtFrom.SelectedDate.HasValue)
                {
                    sql += " AND OrderDate>=@DateFrom";
                    parms.Add(new SqlParameter("@DateFrom", dtFrom.SelectedDate.Value));
                }

                if (dtTo.SelectedDate.HasValue)
                {
                    sql += " AND OrderDate<=@DateTo";
                    parms.Add(new SqlParameter("@DateTo", dtTo.SelectedDate.Value));
                }

                if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    sql += " AND (OrderNumber LIKE @Search OR CustomerName LIKE @Search)";
                    parms.Add(new SqlParameter("@Search", "%" + txtSearch.Text.Trim() + "%"));
                }

                sql += " ORDER BY OrderDate DESC";

                SqlDataAdapter adapter = new SqlDataAdapter(sql, connectionString);
                adapter.SelectCommand.Parameters.AddRange(parms.ToArray());
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                _orderRows.Clear();

                foreach (DataRow row in dt.Rows)
                {
                    bool isDelayed = row["IsDelayed"] != DBNull.Value && Convert.ToBoolean(row["IsDelayed"]);

                    _orderRows.Add(new OrderRow_frmOrders
                    {
                        OrderID         = row["OrderID"].ToString(),
                        OrderNumber     = row["OrderNumber"].ToString(),
                        CustomerName    = row["CustomerName"].ToString(),
                        CustomerPhone   = row["CustomerPhone"]?.ToString() ?? "",
                        MeasurementName = row["MeasurementName"]?.ToString() ?? "",
                        TypeName        = row["TypeName"]?.ToString() ?? "",
                        StatusName      = row["StatusName"]?.ToString() ?? "",
                        OrderDate       = row["OrderDate"] != DBNull.Value
                                          ? Convert.ToDateTime(row["OrderDate"]).ToString("dd/MM/yyyy") : "",
                        DeliveryDate    = row["DeliveryDate"] != DBNull.Value
                                          ? Convert.ToDateTime(row["DeliveryDate"]).ToString("dd/MM/yyyy") : "",
                        Price           = row["Price"] != DBNull.Value ? Convert.ToDecimal(row["Price"]) : 0,
                        PaidAmount      = row["PaidAmount"] != DBNull.Value ? Convert.ToDecimal(row["PaidAmount"]) : 0,
                        RemainingAmount = row["RemainingAmount"] != DBNull.Value ? Convert.ToDecimal(row["RemainingAmount"]) : 0,
                        IsDelayed       = isDelayed
                    });
                }

                lblRowCount.Text = $"عدد السجلات: {_orderRows.Count:N0}";

                // تلوين الصفوف المتأخرة
                gridOrders.LoadingRow += (s, e2) =>
                {
                    OrderRow_frmOrders row2 = e2.Row.Item as OrderRow_frmOrders;
                    if (row2?.IsDelayed == true)
                    {
                        e2.Row.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE4E4"));
                        e2.Row.Foreground = new SolidColorBrush(Colors.DarkRed);
                    }
                    else
                    {
                        e2.Row.Background = null;
                        e2.Row.Foreground = null;
                    }
                };
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region Filter Events

        private void cmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadOrders();
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            LoadOrders();
        }

        #endregion

        #region Buttons

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            frmOrderDetails form = new frmOrderDetails { IsNewRecord = true };
            if (form.ShowDialog() == true)
                LoadOrders();
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            OrderRow_frmOrders selected = gridOrders.SelectedItem as OrderRow_frmOrders;
            if (selected == null)
            {
                DXMessageBox.Show("الرجاء اختيار طلب للتعديل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int.TryParse(selected.OrderID, out int orderId);
            frmOrderDetails form = new frmOrderDetails { OrderID = orderId, IsNewRecord = false };
            if (form.ShowDialog() == true)
                LoadOrders();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            OrderRow_frmOrders selected = gridOrders.SelectedItem as OrderRow_frmOrders;
            if (selected == null)
            {
                DXMessageBox.Show("الرجاء اختيار طلب للحذف", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DXMessageBox.Show("هل أنت متأكد من حذف هذا الطلب؟\nسيتم حذف جميع البيانات المرتبطة به", "تأكيد الحذف",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();
                using SqlCommand cmd = new SqlCommand(
                    "DELETE FROM TailoringOrders WHERE OrderID=@ID", conn);
                cmd.Parameters.AddWithValue("@ID", selected.OrderID);
                cmd.ExecuteNonQuery();

                DXMessageBox.Show("تم الحذف بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadOrders();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void btnChangeStatus_Click(object sender, RoutedEventArgs e)
        {
            OrderRow_frmOrders selected = gridOrders.SelectedItem as OrderRow_frmOrders;
            if (selected == null)
            {
                DXMessageBox.Show("الرجاء اختيار طلب", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int.TryParse(selected.OrderID, out int orderId);
            string currentStatus = selected.StatusName;

            // نافذة تغيير الحالة
            System.Windows.Window statusWindow = new System.Windows.Window
            {
                Title                 = "تغيير حالة الطلب",
                Width                 = 380,
                Height                = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner                 = this,
                FlowDirection         = System.Windows.FlowDirection.RightToLeft,
                ResizeMode            = ResizeMode.NoResize,
                Background            = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F5F9")),
                FontFamily            = new FontFamily("Cairo, Tahoma, Arial")
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(20) };

            panel.Children.Add(new TextBlock
            {
                Text       = $"الحالة الحالية: {currentStatus}",
                FontWeight = FontWeights.Bold,
                FontSize   = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B2642")),
                Margin     = new Thickness(0, 0, 0, 12)
            });

            StackPanel row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
            row.Children.Add(new TextBlock { Text = "الحالة الجديدة:", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B2642")), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });

            ComboBox cmbNewStatus = new ComboBox { Width = 220, Height = 32, FontSize = 12 };

            try
            {
                SqlDataAdapter statusAdapter = new SqlDataAdapter(
                    "SELECT StatusID, StatusName FROM OrderStatus WHERE IsActive=1 ORDER BY DisplayOrder",
                    connectionString);
                DataTable statusDt = new DataTable();
                statusAdapter.Fill(statusDt);
                cmbNewStatus.ItemsSource   = statusDt.DefaultView;
                cmbNewStatus.DisplayMemberPath = "StatusName";
                cmbNewStatus.SelectedValuePath  = "StatusID";
            }
            catch { }

            row.Children.Add(cmbNewStatus);
            panel.Children.Add(row);

            Button changeBtn = new Button
            {
                Content    = "✔ تغيير",
                Width      = 110,
                Height     = 38,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2CC36B")),
                Foreground = new SolidColorBrush(Colors.White),
                FontSize   = 13,
                FontWeight = FontWeights.Bold,
                Cursor     = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(0)
            };

            changeBtn.Click += (s, args) =>
            {
                if (cmbNewStatus.SelectedValue == null)
                {
                    DXMessageBox.Show("الرجاء اختيار حالة جديدة", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    int newStatusId = Convert.ToInt32(cmbNewStatus.SelectedValue);
                    using SqlConnection conn2 = new SqlConnection(connectionString);
                    conn2.Open();
                    using SqlCommand cmd2 = new SqlCommand("sp_UpdateOrderStatus", conn2)
                        { CommandType = CommandType.StoredProcedure };
                    cmd2.Parameters.AddWithValue("@OrderID",    orderId);
                    cmd2.Parameters.AddWithValue("@NewStatusID", newStatusId);
                    cmd2.Parameters.AddWithValue("@ChangedBy",  Environment.UserName);
                    cmd2.ExecuteNonQuery();

                    DXMessageBox.Show("تم تغيير الحالة بنجاح", "نجح",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    statusWindow.Close();
                    LoadOrders();
                }
                catch (Exception ex2)
                {
                    ShowError(ex2);
                }
            };

            panel.Children.Add(changeBtn);
            statusWindow.Content = panel;
            statusWindow.ShowDialog();
        }

        private void gridOrders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            btnEdit_Click(null, null);
        }

        #endregion

        #region Close & Helpers

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void ShowError(Exception ex)
        {
            DXMessageBox.Show("خطأ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }

    public class OrderRow_frmOrders
    {
        public string  OrderID         { get; set; }
        public string  OrderNumber     { get; set; }
        public string  CustomerName    { get; set; }
        public string  CustomerPhone   { get; set; }
        public string  MeasurementName { get; set; }
        public string  TypeName        { get; set; }
        public string  StatusName      { get; set; }
        public string  OrderDate       { get; set; }
        public string  DeliveryDate    { get; set; }
        public decimal Price           { get; set; }
        public decimal PaidAmount      { get; set; }
        public decimal RemainingAmount { get; set; }
        public bool    IsDelayed       { get; set; }
    }
}