using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmOrderDetails : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private string connectionString;
        private int    selectedCustID = 0;
        private Dictionary<int, int> selectedOptions;

        public int  OrderID     { get; set; } = 0;
        public bool IsNewRecord { get; set; } = true;
        public bool IsOK        { get; private set; } = false;

        #endregion

        #region Constructor

        public frmOrderDetails()
        {
            connectionString  = MainClass.connstr;
            selectedOptions   = new Dictionary<int, int>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmOrderDetails_Load(object sender, RoutedEventArgs e)
        {
            headerTitle.Text = IsNewRecord ? "👔 إضافة طلب جديد" : "✏️ تعديل الطلب";
            dtDelivery.SelectedDate = DateTime.Now.AddDays(7);
            LoadTypes();
            LoadOptionCategories();

            if (!IsNewRecord)
                LoadOrderData();
        }

        private void LoadTypes()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT TypeID, TypeName, DefaultPrice FROM TailoringTypes WHERE IsActive=1 ORDER BY TypeName",
                    connectionString);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbType.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void LoadOptionCategories()
        {
            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT CategoryID, CategoryName FROM OptionCategories WHERE IsActive=1 ORDER BY DisplayOrder", conn);
                using SqlDataReader reader = cmd.ExecuteReader();

                int yOffset = 0;

                while (reader.Read())
                {
                    int    catId   = Convert.ToInt32(reader["CategoryID"]);
                    string catName = reader["CategoryName"].ToString();

                    // أضف Label للفئة
                    TextBlock catLabel = new TextBlock
                    {
                        Text       = catName + ":",
                        FontWeight = FontWeights.Bold,
                        FontSize   = 12,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#213A7A")),
                        Margin     = new Thickness(0, yOffset == 0 ? 0 : 8, 12, 4)
                    };
                    panelOptions.Children.Add(catLabel);

                    LoadCategoryOptions(catId, conn);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void LoadCategoryOptions(int categoryID, SqlConnection conn)
        {
            try
            {
                SqlCommand cmd = new SqlCommand(
                    "SELECT ValueID, ValueName, IsDefault FROM OptionValues WHERE CategoryID=@CatID AND IsActive=1 ORDER BY DisplayOrder",
                    conn);
                cmd.Parameters.AddWithValue("@CatID", categoryID);
                using SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    int    valueId   = Convert.ToInt32(reader["ValueID"]);
                    string valueName = reader["ValueName"].ToString();
                    bool   isDefault = Convert.ToBoolean(reader["IsDefault"]);

                    Button optBtn = new Button
                    {
                        Content         = valueName,
                        Tag             = new { CategoryID = categoryID, ValueID = valueId },
                        Height          = 32,
                        Margin          = new Thickness(0, 0, 6, 6),
                        Padding         = new Thickness(12, 0, 12, 0),
                        FontSize        = 12,
                        FontWeight      = FontWeights.SemiBold,
                        Foreground      = new SolidColorBrush(Colors.White),
                        Background      = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5A6A99")),
                        BorderThickness = new Thickness(0),
                        Cursor          = System.Windows.Input.Cursors.Hand
                    };

                    optBtn.Template = CreateButtonTemplate();
                    optBtn.Click   += OptionButton_Click;

                    if (isDefault)
                    {
                        selectedOptions[categoryID] = valueId;
                        optBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2CC36B"));
                    }

                    panelOptions.Children.Add(optBtn);
                }
            }
            catch { }
        }

        private ControlTemplate CreateButtonTemplate()
        {
            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            border.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });

            FrameworkElementFactory content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);
            template.VisualTree = border;
            return template;
        }

        private void OptionButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedBtn = sender as Button;
            if (clickedBtn == null) return;

            dynamic tag = clickedBtn.Tag;
            int catId   = (int)tag.CategoryID;
            int valId   = (int)tag.ValueID;

            selectedOptions[catId] = valId;

            // إعادة تلوين الأزرار في نفس الفئة
            foreach (var child in panelOptions.Children)
            {
                if (child is Button btn && btn.Tag != null)
                {
                    dynamic btnTag = btn.Tag;
                    if ((int)btnTag.CategoryID == catId)
                        btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5A6A99"));
                }
            }

            clickedBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2CC36B"));
        }

        #endregion

        #region Customer Search

        private void txtCustomerSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                SearchCustomer();
        }

        private void btnSearchCustomer_Click(object sender, RoutedEventArgs e)
        {
            SearchCustomer();
        }

        private void SearchCustomer()
        {
            if (string.IsNullOrWhiteSpace(txtCustomerSearch.Text))
            {
                DXMessageBox.Show("الرجاء إدخال اسم أو جوال العميل", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string keyword = txtCustomerSearch.Text.Trim();
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "SELECT TOP 10 id, name, mobile FROM Customers WHERE mobile LIKE @Search OR name LIKE @Search ORDER BY name",
                    connectionString);
                adapter.SelectCommand.Parameters.AddWithValue("@Search", "%" + keyword + "%");
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    DXMessageBox.Show("لم يتم العثور على عميل", "نتيجة البحث",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                SelectCustomer(dt.Rows[0]);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void SelectCustomer(DataRow row)
        {
            selectedCustID        = Convert.ToInt32(row["id"]);
            lblCustomerName.Text  = "العميل: " + row["name"].ToString();
            lblCustomerPhone.Text = "الجوال: " + row["mobile"].ToString();
            LoadCustomerMeasurements();
        }

        private void LoadCustomerMeasurements()
        {
            try
            {
                string sql = @"
                    SELECT MeasurementID,
                           ISNULL(MeasurementName, 'قياس بتاريخ ' + CONVERT(VARCHAR, MeasurementDate, 103)) AS DisplayName
                    FROM CustomerMeasurements
                    WHERE Cust_ID=@CustID AND IsActive=1
                    ORDER BY MeasurementDate DESC";

                SqlDataAdapter adapter = new SqlDataAdapter(sql, connectionString);
                adapter.SelectCommand.Parameters.AddWithValue("@CustID", selectedCustID);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbMeasurement.ItemsSource = dt.DefaultView;
            }
            catch { }
        }

        #endregion

        #region Calculate Remaining

        private void txtPrice_TextChanged(object sender, TextChangedEventArgs e) => CalculateRemaining();
        private void txtPaidAmount_TextChanged(object sender, TextChangedEventArgs e) => CalculateRemaining();

        private void CalculateRemaining()
        {
            try
            {
                decimal price  = 0;
                decimal paid   = 0;
                decimal.TryParse(txtPrice.Text, out price);
                decimal.TryParse(txtPaidAmount.Text, out paid);
                decimal remaining = price - paid;

                lblRemaining.Text       = remaining.ToString("N2");
                lblRemaining.Foreground = remaining >= 0
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E35656"))
                    : new SolidColorBrush(Colors.Green);
            }
            catch
            {
                lblRemaining.Text = "0.00";
            }
        }

        private void cmbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbType.SelectedItem == null || !string.IsNullOrWhiteSpace(txtPrice.Text)) return;

                DataRowView row = cmbType.SelectedItem as DataRowView;
                if (row != null)
                    txtPrice.Text = row["DefaultPrice"]?.ToString() ?? "";
            }
            catch { }
        }

        #endregion

        #region Save

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCustID == 0)
            {
                DXMessageBox.Show("الرجاء اختيار عميل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbType.SelectedValue == null)
            {
                DXMessageBox.Show("الرجاء اختيار نوع التفصيل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal price = 0;
            decimal.TryParse(txtPrice.Text, out price);

            if (price <= 0)
            {
                DXMessageBox.Show("الرجاء إدخال السعر", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();
                using SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    int    orderId     = 0;
                    string orderNumber = "";

                    decimal quantity   = 1;
                    decimal.TryParse(txtQuantity.Text, out quantity);
                    decimal paidAmount = 0;
                    decimal.TryParse(txtPaidAmount.Text, out paidAmount);

                    using (SqlCommand cmd = new SqlCommand("TailoringTypes", conn, transaction))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Cust_ID",       selectedCustID);
                        cmd.Parameters.AddWithValue("@MeasurementID", (object)cmbMeasurement.SelectedValue ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TypeID",        cmbType.SelectedValue);
                        cmd.Parameters.AddWithValue("@DeliveryDate",  dtDelivery.SelectedDate ?? DateTime.Now.AddDays(7));
                        cmd.Parameters.AddWithValue("@Quantity",      quantity);
                        cmd.Parameters.AddWithValue("@Price",         price);
                        cmd.Parameters.AddWithValue("@PaidAmount",    paidAmount);
                        cmd.Parameters.AddWithValue("@FabricType",    string.IsNullOrWhiteSpace(txtFabricType.Text) ? (object)DBNull.Value : txtFabricType.Text);
                        cmd.Parameters.AddWithValue("@FabricColor",   string.IsNullOrWhiteSpace(txtFabricColor.Text) ? (object)DBNull.Value : txtFabricColor.Text);
                        cmd.Parameters.AddWithValue("@DesignNotes",   string.IsNullOrWhiteSpace(txtDesignNotes.Text) ? (object)DBNull.Value : txtDesignNotes.Text);
                        cmd.Parameters.AddWithValue("@GeneralNotes",  DBNull.Value);
                        cmd.Parameters.AddWithValue("@CreatedBy",     Environment.UserName);

                        SqlParameter outId = new SqlParameter("@OrderID", SqlDbType.Int) { Direction = ParameterDirection.Output };
                        SqlParameter outNo = new SqlParameter("@OrderNumber", SqlDbType.NVarChar, 50) { Direction = ParameterDirection.Output };
                        cmd.Parameters.Add(outId);
                        cmd.Parameters.Add(outNo);
                        cmd.ExecuteNonQuery();

                        orderId     = Convert.ToInt32(outId.Value);
                        orderNumber = outNo.Value.ToString();
                    }

                    foreach (var option in selectedOptions)
                    {
                        using SqlCommand optCmd = new SqlCommand(
                            "INSERT INTO OrderOptions (OrderID, CategoryID, ValueID) VALUES (@OrderID, @CatID, @ValID)",
                            conn, transaction);
                        optCmd.Parameters.AddWithValue("@OrderID", orderId);
                        optCmd.Parameters.AddWithValue("@CatID",   option.Key);
                        optCmd.Parameters.AddWithValue("@ValID",   option.Value);
                        optCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    DXMessageBox.Show($"تم حفظ الطلب بنجاح\nرقم الطلب: {orderNumber}", "نجح",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    IsOK = true;
                    DialogResult = true;
                    Close();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void LoadOrderData()
        {
            // تحميل بيانات الطلب للتعديل - يمكن تطويرها لاحقًا
        }

        #endregion

        #region Cancel & Helpers

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowError(Exception ex)
        {
            DXMessageBox.Show("خطأ في الحفظ: " + ex.Message, "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}