using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using TextBox = System.Windows.Controls.TextBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmMeasurementDetails : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private string connectionString;
        private Dictionary<int, TextBox> measurementControls;

        public int  Cust_ID       { get; set; }
        public int  MeasurementID { get; set; } = 0;
        public bool IsNewRecord   { get; set; } = true;

        public bool IsOK { get; private set; } = false;

        #endregion

        #region Constructor

        public frmMeasurementDetails()
        {
            connectionString    = MainClass.connstr;
            measurementControls = new Dictionary<int, TextBox>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmMeasurementDetails_Load(object sender, RoutedEventArgs e)
        {
            LoadMeasurementAttributes();

            if (!IsNewRecord)
                LoadMeasurementData();
        }

        #endregion

        #region Load Attributes (Dynamic UI)

        private void LoadMeasurementAttributes()
        {
            try
            {
                string sql = @"
                    SELECT AttributeID, AttributeName
                    FROM MeasurementAttributes
                    WHERE IsActive = 1
                    ORDER BY DisplayOrder";

                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();
                using SqlCommand cmd = new SqlCommand(sql, conn);
                using SqlDataReader reader = cmd.ExecuteReader();

                panelMeasurements.Children.Clear();
                measurementControls.Clear();

                while (reader.Read())
                {
                    int    attrId   = Convert.ToInt32(reader["AttributeID"]);
                    string attrName = reader["AttributeName"].ToString();

                    Grid rowGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

                    TextBlock label = new TextBlock
                    {
                        Text              = attrName + ":",
                        FontSize          = 12,
                        FontWeight        = FontWeights.Bold,
                        Foreground        = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B2642")),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(label, 0);

                    TextBox inputBox = new TextBox
                    {
                        Height                  = 32,
                        FontSize                = 13,
                        Background              = System.Windows.Media.Brushes.White,
                        BorderBrush             = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D7E8")),
                        BorderThickness         = new Thickness(1),
                        Padding                 = new Thickness(6, 0, 6, 0),
                        VerticalContentAlignment = VerticalAlignment.Center,
                        TextAlignment           = TextAlignment.Center
                    };
                    Grid.SetColumn(inputBox, 1);

                    TextBlock unitLabel = new TextBlock
                    {
                        Text              = "سم",
                        FontSize          = 11,
                        Foreground        = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5A6A8A")),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin            = new Thickness(6, 0, 0, 0)
                    };
                    Grid.SetColumn(unitLabel, 2);

                    rowGrid.Children.Add(label);
                    rowGrid.Children.Add(inputBox);
                    rowGrid.Children.Add(unitLabel);

                    panelMeasurements.Children.Add(rowGrid);
                    measurementControls[attrId] = inputBox;
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل الخصائص", ex);
            }
        }

        #endregion

        #region Load Measurement Data

        private void LoadMeasurementData()
        {
            try
            {
                string sql = @"
                    SELECT MeasurementName, Notes
                    FROM CustomerMeasurements
                    WHERE MeasurementID = @ID";

                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", MeasurementID);
                    using SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        txtMeasurementName.Text = reader["MeasurementName"].ToString();
                        txtNotes.Text = reader["Notes"] == DBNull.Value ? "" : reader["Notes"].ToString();
                    }
                }

                using SqlCommand valCmd = new SqlCommand(
                    "SELECT AttributeID, AttributeValue FROM MeasurementValues WHERE MeasurementID = @ID", conn);
                valCmd.Parameters.AddWithValue("@ID", MeasurementID);
                using SqlDataReader valReader = valCmd.ExecuteReader();

                while (valReader.Read())
                {
                    int     attrId = Convert.ToInt32(valReader["AttributeID"]);
                    decimal value  = Convert.ToDecimal(valReader["AttributeValue"]);

                    if (measurementControls.ContainsKey(attrId))
                        measurementControls[attrId].Text = value.ToString("N2");
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل البيانات", ex);
            }
        }

        #endregion

        #region Save

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMeasurementName.Text))
            {
                DXMessageBox.Show("الرجاء إدخال اسم صاحب القياس", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMeasurementName.Focus();
                return;
            }

            bool hasAtLeastOne = false;
            foreach (var pair in measurementControls)
            {
                if (!string.IsNullOrWhiteSpace(pair.Value.Text))
                {
                    decimal val = 0;
                    decimal.TryParse(pair.Value.Text, out val);
                    if (val > 0) { hasAtLeastOne = true; break; }
                }
            }

            if (!hasAtLeastOne)
            {
                DXMessageBox.Show("الرجاء إدخال قياس واحد على الأقل", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();
                using SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    if (IsNewRecord)
                    {
                        using SqlCommand insertCmd = new SqlCommand(
                            @"INSERT INTO CustomerMeasurements (Cust_ID, MeasurementName, Notes)
                              VALUES (@CustID, @Name, @Notes);
                              SELECT SCOPE_IDENTITY();",
                            conn, transaction);

                        insertCmd.Parameters.AddWithValue("@CustID", Cust_ID);
                        insertCmd.Parameters.AddWithValue("@Name",   txtMeasurementName.Text.Trim());
                        insertCmd.Parameters.AddWithValue("@Notes",
                            string.IsNullOrWhiteSpace(txtNotes.Text)
                            ? (object)DBNull.Value
                            : (object)txtNotes.Text.Trim());

                        MeasurementID = Convert.ToInt32(insertCmd.ExecuteScalar());
                    }
                    else
                    {
                        using (SqlCommand updateCmd = new SqlCommand(
                            @"UPDATE CustomerMeasurements
                              SET MeasurementName=@Name, Notes=@Notes
                              WHERE MeasurementID=@ID",
                            conn, transaction))
                        {
                            updateCmd.Parameters.AddWithValue("@Name",
                                txtMeasurementName.Text.Trim());
                            updateCmd.Parameters.AddWithValue("@Notes",
                                string.IsNullOrWhiteSpace(txtNotes.Text)
                                ? (object)DBNull.Value
                                : (object)txtNotes.Text.Trim());
                            updateCmd.Parameters.AddWithValue("@ID", MeasurementID);
                            updateCmd.ExecuteNonQuery();
                        }

                        using SqlCommand deleteCmd = new SqlCommand(
                            "DELETE FROM MeasurementValues WHERE MeasurementID=@ID",
                            conn, transaction);
                        deleteCmd.Parameters.AddWithValue("@ID", MeasurementID);
                        deleteCmd.ExecuteNonQuery();
                    }

                    string insertValSql =
                        "INSERT INTO MeasurementValues (MeasurementID, AttributeID, AttributeValue) VALUES (@MeasID, @AttrID, @Value)";

                    foreach (var pair in measurementControls)
                    {
                        if (string.IsNullOrWhiteSpace(pair.Value.Text)) continue;

                        decimal val = 0;
                        decimal.TryParse(pair.Value.Text, out val);
                        if (val <= 0) continue;

                        using SqlCommand valCmd = new SqlCommand(insertValSql, conn, transaction);
                        valCmd.Parameters.AddWithValue("@MeasID",  MeasurementID);
                        valCmd.Parameters.AddWithValue("@AttrID",  pair.Key);
                        valCmd.Parameters.AddWithValue("@Value",   val);
                        valCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    DXMessageBox.Show("تم الحفظ بنجاح", "نجح",
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
                ShowError("خطأ في الحفظ", ex);
            }
        }

        #endregion

        #region Cancel

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

        #region Helpers

        private void ShowError(string context, Exception ex)
        {
            DXMessageBox.Show(context + Environment.NewLine + "خطأ: " + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}