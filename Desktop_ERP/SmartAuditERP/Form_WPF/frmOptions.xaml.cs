using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmOptions : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private string connectionString;
        private int currentCategoryID = 0;

        private ObservableCollection<CategoryRow_frmOptions>  _categoryRows;
        private ObservableCollection<OptionRow>    _optionRows;

        #endregion

        #region Constructor

        public frmOptions()
        {
            connectionString = MainClass.connstr;
            _categoryRows    = new ObservableCollection<CategoryRow_frmOptions>();
            _optionRows      = new ObservableCollection<OptionRow>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmOptions_Load(object sender, RoutedEventArgs e)
        {
            gridCategories.ItemsSource = _categoryRows;
            gridOptions.ItemsSource    = _optionRows;
            LoadCategories();
        }

        private void LoadCategories()
        {
            try
            {
                string sql = @"
                    SELECT CategoryID, CategoryName, DisplayOrder
                    FROM OptionCategories
                    WHERE IsActive = 1
                    ORDER BY DisplayOrder";

                using SqlConnection conn = new SqlConnection(connectionString);
                using SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                _categoryRows.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    _categoryRows.Add(new CategoryRow_frmOptions
                    {
                        CategoryID   = row["CategoryID"].ToString(),
                        CategoryName = row["CategoryName"].ToString(),
                        DisplayOrder = row["DisplayOrder"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void LoadOptions(int categoryID)
        {
            try
            {
                string sql = @"
                    SELECT ValueID, ValueName, DisplayOrder,
                           CASE WHEN IsDefault=1 THEN 'نعم' ELSE 'لا' END AS IsDefaultText
                    FROM OptionValues
                    WHERE CategoryID=@CatID AND IsActive=1
                    ORDER BY DisplayOrder";

                using SqlConnection conn = new SqlConnection(connectionString);
                using SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                adapter.SelectCommand.Parameters.AddWithValue("@CatID", categoryID);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                _optionRows.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    _optionRows.Add(new OptionRow
                    {
                        ValueID       = row["ValueID"].ToString(),
                        ValueName     = row["ValueName"].ToString(),
                        DisplayOrder  = row["DisplayOrder"].ToString(),
                        IsDefaultText = row["IsDefaultText"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void gridCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CategoryRow_frmOptions selected = gridCategories.SelectedItem as CategoryRow_frmOptions;
            if (selected == null) return;

            int.TryParse(selected.CategoryID, out currentCategoryID);
            LoadOptions(currentCategoryID);
        }

        #endregion

        #region Categories

        private void btnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            InputDialog dialog = new InputDialog("إضافة تصنيف جديد", "أدخل اسم التصنيف (مثل: نوع الرقبة)");
            dialog.Owner = this;
            if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.InputResult)) return;

            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();

                int nextOrder = 1;
                using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(MAX(DisplayOrder),0)+1 FROM OptionCategories", conn))
                    nextOrder = Convert.ToInt32(cmd.ExecuteScalar());

                using SqlCommand insertCmd = new SqlCommand(
                    "INSERT INTO OptionCategories (CategoryName, DisplayOrder) VALUES (@Name, @Order)", conn);
                insertCmd.Parameters.AddWithValue("@Name",  dialog.InputResult.Trim());
                insertCmd.Parameters.AddWithValue("@Order", nextOrder);
                insertCmd.ExecuteNonQuery();

                DXMessageBox.Show("تمت الإضافة بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadCategories();
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void btnEditCategory_Click(object sender, RoutedEventArgs e)
        {
            CategoryRow_frmOptions selected = gridCategories.SelectedItem as CategoryRow_frmOptions;
            if (selected == null)
            {
                DXMessageBox.Show("الرجاء اختيار تصنيف للتعديل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            InputDialog dialog = new InputDialog("تعديل اسم التصنيف", "تعديل اسم التصنيف:", selected.CategoryName);
            dialog.Owner = this;
            if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.InputResult)) return;
            if (dialog.InputResult == selected.CategoryName) return;

            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();
                using SqlCommand cmd = new SqlCommand(
                    "UPDATE OptionCategories SET CategoryName=@Name WHERE CategoryID=@ID", conn);
                cmd.Parameters.AddWithValue("@Name", dialog.InputResult.Trim());
                cmd.Parameters.AddWithValue("@ID",   selected.CategoryID);
                cmd.ExecuteNonQuery();

                DXMessageBox.Show("تم التعديل بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadCategories();
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void btnDeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            CategoryRow_frmOptions selected = gridCategories.SelectedItem as CategoryRow_frmOptions;
            if (selected == null)
            {
                DXMessageBox.Show("الرجاء اختيار تصنيف للحذف", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DXMessageBox.Show("هل أنت متأكد من حذف هذا التصنيف وجميع خياراته؟", "تأكيد الحذف",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();
                using SqlCommand cmd = new SqlCommand(
                    "UPDATE OptionCategories SET IsActive=0 WHERE CategoryID=@ID", conn);
                cmd.Parameters.AddWithValue("@ID", selected.CategoryID);
                cmd.ExecuteNonQuery();

                DXMessageBox.Show("تم الحذف بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadCategories();
                _optionRows.Clear();
            }
            catch (Exception ex) { ShowError(ex); }
        }

        #endregion

        #region Options

        private void btnAddOption_Click(object sender, RoutedEventArgs e)
        {
            if (currentCategoryID == 0)
            {
                DXMessageBox.Show("الرجاء اختيار تصنيف أولًا", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            InputDialog dialog = new InputDialog("إضافة خيار جديد", "أدخل اسم الخيار (مثل: عادية، كشمير)");
            dialog.Owner = this;
            if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.InputResult)) return;

            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();

                int nextOrder = 1;
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ISNULL(MAX(DisplayOrder),0)+1 FROM OptionValues WHERE CategoryID=@CatID", conn))
                {
                    cmd.Parameters.AddWithValue("@CatID", currentCategoryID);
                    nextOrder = Convert.ToInt32(cmd.ExecuteScalar());
                }

                using SqlCommand insertCmd = new SqlCommand(
                    "INSERT INTO OptionValues (CategoryID, ValueName, DisplayOrder) VALUES (@CatID, @Name, @Order)", conn);
                insertCmd.Parameters.AddWithValue("@CatID", currentCategoryID);
                insertCmd.Parameters.AddWithValue("@Name",  dialog.InputResult.Trim());
                insertCmd.Parameters.AddWithValue("@Order", nextOrder);
                insertCmd.ExecuteNonQuery();

                DXMessageBox.Show("تمت الإضافة بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadOptions(currentCategoryID);
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void btnEditOption_Click(object sender, RoutedEventArgs e)
        {
            OptionRow selected = gridOptions.SelectedItem as OptionRow;
            if (selected == null)
            {
                DXMessageBox.Show("الرجاء اختيار خيار للتعديل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            InputDialog dialog = new InputDialog("تعديل اسم الخيار", "تعديل اسم الخيار:", selected.ValueName);
            dialog.Owner = this;
            if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.InputResult)) return;
            if (dialog.InputResult == selected.ValueName) return;

            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();
                using SqlCommand cmd = new SqlCommand(
                    "UPDATE OptionValues SET ValueName=@Name WHERE ValueID=@ID", conn);
                cmd.Parameters.AddWithValue("@Name", dialog.InputResult.Trim());
                cmd.Parameters.AddWithValue("@ID",   selected.ValueID);
                cmd.ExecuteNonQuery();

                DXMessageBox.Show("تم التعديل بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadOptions(currentCategoryID);
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void btnDeleteOption_Click(object sender, RoutedEventArgs e)
        {
            OptionRow selected = gridOptions.SelectedItem as OptionRow;
            if (selected == null)
            {
                DXMessageBox.Show("الرجاء اختيار خيار للحذف", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DXMessageBox.Show("هل أنت متأكد من حذف هذا الخيار؟", "تأكيد الحذف",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No) return;

            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();
                using SqlCommand cmd = new SqlCommand(
                    "UPDATE OptionValues SET IsActive=0 WHERE ValueID=@ID", conn);
                cmd.Parameters.AddWithValue("@ID", selected.ValueID);
                cmd.ExecuteNonQuery();

                DXMessageBox.Show("تم الحذف بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadOptions(currentCategoryID);
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void btnSetDefault_Click(object sender, RoutedEventArgs e)
        {
            OptionRow selected = gridOptions.SelectedItem as OptionRow;
            if (selected == null)
            {
                DXMessageBox.Show("الرجاء اختيار خيار", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();

                using (SqlCommand resetCmd = new SqlCommand(
                    "UPDATE OptionValues SET IsDefault=0 WHERE CategoryID=@CatID", conn))
                {
                    resetCmd.Parameters.AddWithValue("@CatID", currentCategoryID);
                    resetCmd.ExecuteNonQuery();
                }

                using SqlCommand setCmd = new SqlCommand(
                    "UPDATE OptionValues SET IsDefault=1 WHERE ValueID=@ID", conn);
                setCmd.Parameters.AddWithValue("@ID", selected.ValueID);
                setCmd.ExecuteNonQuery();

                DXMessageBox.Show("تم التعيين كافتراضي", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadOptions(currentCategoryID);
            }
            catch (Exception ex) { ShowError(ex); }
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

    public class CategoryRow_frmOptions
    {
        public string CategoryID   { get; set; }
        public string CategoryName { get; set; }
        public string DisplayOrder { get; set; }
    }

    public class OptionRow
    {
        public string ValueID       { get; set; }
        public string ValueName     { get; set; }
        public string DisplayOrder  { get; set; }
        public string IsDefaultText { get; set; }
    }
}