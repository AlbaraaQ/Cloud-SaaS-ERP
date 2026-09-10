using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;
using Orientation = System.Windows.Controls.Orientation;
using TextBox = System.Windows.Controls.TextBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmMeasurementAttributes : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private string connectionString;
        private ObservableCollection<AttributeRow> _rows;

        #endregion

        #region Constructor

        public frmMeasurementAttributes()
        {
            connectionString = MainClass.connstr;
            _rows            = new ObservableCollection<AttributeRow>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmMeasurementAttributes_Load(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                string sql = @"
                    SELECT AttributeID, AttributeName, DisplayOrder,
                           CASE WHEN IsActive = 1 THEN 'نشط' ELSE 'معطل' END AS StatusText
                    FROM MeasurementAttributes
                    ORDER BY DisplayOrder";

                using SqlConnection conn = new SqlConnection(connectionString);
                using SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);

                DataTable dt = new DataTable();
                adapter.Fill(dt);

                _rows.Clear();

                foreach (DataRow row in dt.Rows)
                {
                    _rows.Add(new AttributeRow
                    {
                        AttributeID   = row["AttributeID"].ToString(),
                        AttributeName = row["AttributeName"].ToString(),
                        DisplayOrder  = row["DisplayOrder"].ToString(),
                        StatusText    = row["StatusText"].ToString()
                    });
                }

                gridControl.ItemsSource = _rows;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل البيانات", ex);
            }
        }

        #endregion

        #region Add

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            string newName = PromptInput("أدخل اسم الخاصية (مثل: الطول، العرض، الكم)", "إضافة خاصية جديدة");
            if (string.IsNullOrWhiteSpace(newName)) return;

            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();

                int nextOrder = 1;
                using (SqlCommand orderCmd = new SqlCommand(
                    "SELECT ISNULL(MAX(DisplayOrder), 0) + 1 FROM MeasurementAttributes", conn))
                {
                    nextOrder = Convert.ToInt32(orderCmd.ExecuteScalar());
                }

                using SqlCommand insertCmd = new SqlCommand(
                    "INSERT INTO MeasurementAttributes (AttributeName, DisplayOrder) VALUES (@Name, @Order)", conn);
                insertCmd.Parameters.AddWithValue("@Name",  newName.Trim());
                insertCmd.Parameters.AddWithValue("@Order", nextOrder);
                insertCmd.ExecuteNonQuery();

                DXMessageBox.Show("تمت الإضافة بنجاح", "نجح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في الإضافة", ex);
            }
        }

        #endregion

        #region Edit

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            AttributeRow selected = gridControl.SelectedItem as AttributeRow;
            if (selected == null)
            {
                DXMessageBox.Show("الرجاء اختيار خاصية للتعديل", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string newName = PromptInput("تعديل اسم الخاصية:", "تعديل", selected.AttributeName);

            if (string.IsNullOrWhiteSpace(newName) ||
                string.Equals(newName, selected.AttributeName, StringComparison.Ordinal)) return;

            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();

                using SqlCommand cmd = new SqlCommand(
                    "UPDATE MeasurementAttributes SET AttributeName=@Name WHERE AttributeID=@ID", conn);
                cmd.Parameters.AddWithValue("@Name", newName.Trim());
                cmd.Parameters.AddWithValue("@ID",   selected.AttributeID);
                cmd.ExecuteNonQuery();

                DXMessageBox.Show("تم التعديل بنجاح", "نجح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في التعديل", ex);
            }
        }

        #endregion

        #region Delete (Deactivate)

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            AttributeRow selected = gridControl.SelectedItem as AttributeRow;
            if (selected == null)
            {
                DXMessageBox.Show("الرجاء اختيار خاصية للتعطيل", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DXMessageBox.Show(
                "هل أنت متأكد من تعطيل هذه الخاصية؟\nسيتم إخفاؤها من القياسات الجديدة",
                "تأكيد التعطيل",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.No) return;

            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();

                using SqlCommand cmd = new SqlCommand(
                    "UPDATE MeasurementAttributes SET IsActive=0 WHERE AttributeID=@ID", conn);
                cmd.Parameters.AddWithValue("@ID", selected.AttributeID);
                cmd.ExecuteNonQuery();

                DXMessageBox.Show("تم التعطيل بنجاح", "نجح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في التعطيل", ex);
            }
        }

        #endregion

        #region Move Up / Down

        private void btnMoveUp_Click(object sender, RoutedEventArgs e)
        {
            AttributeRow selected = gridControl.SelectedItem as AttributeRow;
            if (selected == null || gridControl.SelectedIndex <= 0) return;

            try
            {
                int currentOrder = int.TryParse(selected.DisplayOrder, out int co) ? co : 0;
                int selectedId   = int.TryParse(selected.AttributeID, out int sid) ? sid : 0;

                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();

                int prevOrder = 0;
                using (SqlCommand prevCmd = new SqlCommand(
                    "SELECT TOP 1 DisplayOrder FROM MeasurementAttributes WHERE DisplayOrder < @Current ORDER BY DisplayOrder DESC",
                    conn))
                {
                    prevCmd.Parameters.AddWithValue("@Current", currentOrder);
                    object result = prevCmd.ExecuteScalar();
                    if (result == null) return;
                    prevOrder = Convert.ToInt32(result);
                }

                using SqlCommand swapCmd = new SqlCommand(@"
                    UPDATE MeasurementAttributes SET DisplayOrder = CASE
                        WHEN DisplayOrder = @Current THEN @Prev
                        WHEN DisplayOrder = @Prev THEN @Current
                    END
                    WHERE DisplayOrder IN (@Current, @Prev)", conn);
                swapCmd.Parameters.AddWithValue("@Current", currentOrder);
                swapCmd.Parameters.AddWithValue("@Prev",    prevOrder);
                swapCmd.ExecuteNonQuery();

                LoadData();
                SelectById(selectedId);
            }
            catch (Exception ex)
            {
                ShowError("خطأ في التحريك للأعلى", ex);
            }
        }

        private void btnMoveDown_Click(object sender, RoutedEventArgs e)
        {
            AttributeRow selected = gridControl.SelectedItem as AttributeRow;
            if (selected == null || gridControl.SelectedIndex >= _rows.Count - 1) return;

            try
            {
                int currentOrder = int.TryParse(selected.DisplayOrder, out int co) ? co : 0;
                int selectedId   = int.TryParse(selected.AttributeID, out int sid) ? sid : 0;

                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();

                int nextOrder = 0;
                using (SqlCommand nextCmd = new SqlCommand(
                    "SELECT TOP 1 DisplayOrder FROM MeasurementAttributes WHERE DisplayOrder > @Current ORDER BY DisplayOrder ASC",
                    conn))
                {
                    nextCmd.Parameters.AddWithValue("@Current", currentOrder);
                    object result = nextCmd.ExecuteScalar();
                    if (result == null) return;
                    nextOrder = Convert.ToInt32(result);
                }

                using SqlCommand swapCmd = new SqlCommand(@"
                    UPDATE MeasurementAttributes SET DisplayOrder = CASE
                        WHEN DisplayOrder = @Current THEN @Next
                        WHEN DisplayOrder = @Next THEN @Current
                    END
                    WHERE DisplayOrder IN (@Current, @Next)", conn);
                swapCmd.Parameters.AddWithValue("@Current", currentOrder);
                swapCmd.Parameters.AddWithValue("@Next",    nextOrder);
                swapCmd.ExecuteNonQuery();

                LoadData();
                SelectById(selectedId);
            }
            catch (Exception ex)
            {
                ShowError("خطأ في التحريك للأسفل", ex);
            }
        }

        private void SelectById(int attributeId)
        {
            foreach (AttributeRow row in _rows)
            {
                if (row.AttributeID == attributeId.ToString())
                {
                    gridControl.SelectedItem = row;
                    break;
                }
            }
        }

        #endregion

        #region Close

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Helpers

        private string PromptInput(string prompt, string title, string defaultValue = "")
        {
            InputDialog inputDialog = new InputDialog(title, prompt, defaultValue);
            inputDialog.Owner = this;

            if (inputDialog.ShowDialog() == true)
                return inputDialog.InputResult;

            return null;
        }

        private void ShowError(string context, Exception ex)
        {
            DXMessageBox.Show(context + Environment.NewLine + "خطأ: " + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }

    public class AttributeRow
    {
        public string AttributeID   { get; set; }
        public string AttributeName { get; set; }
        public string DisplayOrder  { get; set; }
        public string StatusText    { get; set; }
    }

    // ─── InputDialog بسيط ───────────────────────────────────────────────────
    public class InputDialog : Window
    {
        public string InputResult { get; private set; } = "";

        private TextBox _inputTextBox;

        public InputDialog(string title, string prompt, string defaultValue = "")
        {
            Title                = title;
            Width                = 420;
            Height               = 160;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FlowDirection        = System.Windows.FlowDirection.RightToLeft;
            ResizeMode           = ResizeMode.NoResize;
            FontFamily           = new System.Windows.Media.FontFamily("Cairo, Tahoma, Arial");

            StackPanel panel = new StackPanel { Margin = new Thickness(14) };

            panel.Children.Add(new TextBlock
            {
                Text       = prompt,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1B2642")),
                Margin     = new Thickness(0, 0, 0, 8)
            });

            _inputTextBox = new TextBox
            {
                Text   = defaultValue,
                Height = 34,
                FontSize = 13,
                Padding  = new Thickness(6, 0, 6, 0),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            panel.Children.Add(_inputTextBox);

            StackPanel btns = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Margin = new Thickness(0, 12, 0, 0)
            };

            Button okBtn = new Button
            {
                Content = "✔ موافق",
                Width   = 90,
                Height  = 34,
                Margin  = new Thickness(0, 0, 8, 0),
                IsDefault = true
            };
            okBtn.Click += (s, e) => { InputResult = _inputTextBox.Text; DialogResult = true; };

            Button cancelBtn = new Button
            {
                Content   = "✖ إلغاء",
                Width     = 90,
                Height    = 34,
                IsCancel  = true
            };

            btns.Children.Add(okBtn);
            btns.Children.Add(cancelBtn);
            panel.Children.Add(btns);

            Content = panel;

            Loaded += (s, e) => _inputTextBox.Focus();
        }
    }
}