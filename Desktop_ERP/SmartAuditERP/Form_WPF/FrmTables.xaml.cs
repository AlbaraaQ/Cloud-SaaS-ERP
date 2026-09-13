using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;

namespace SmartAuditERP.Form_WPF
{
    public partial class FrmTables : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;
        private int  Code  = -1;
        private bool ISNew = true;
        private bool Order = false;

        #endregion

        #region Constructor

        public FrmTables()
        {
            InitializeComponent();
            conn  = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Clear();
            LoadTables();
            FillCategoryCombo();
        }

        #endregion

        #region Fill Combo

        private void FillCategoryCombo()
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                var adapter = new SqlDataAdapter(
                    "SELECT Cat_ID, Cat_Name FROM Cat_Table", conn);
                var table = new DataTable();
                adapter.Fill(table);
                cmbCat.DisplayMemberPath = "Cat_Name";
                cmbCat.SelectedValuePath = "Cat_ID";
                cmbCat.ItemsSource = table.DefaultView;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء تحميل الأقسام\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region Clear

        public void Clear()
        {
            Code = -1;
            txtTablename.Text = "";
            txtTableid.Text   = Common.GetMaX_ID("id", "Tables").ToString();
            txtTableid.Background = System.Windows.Media.Brushes.LightCoral;
            cmbCat.SelectedIndex = -1;
        }

        #endregion

        #region Load Tables

        public void LoadTables()
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                var adapter = new SqlDataAdapter(
                    @"SELECT T.id, T.TableName, C.Cat_Name, T.Status, T.Balance
                      FROM Tables T
                      JOIN Cat_Table C ON C.Cat_ID = T.Cat_ID", conn);
                var table = new DataTable();
                adapter.Fill(table);

                var list = new ObservableCollection<TableModel>();
                foreach (DataRow row in table.Rows)
                {
                    list.Add(new TableModel
                    {
                        id        = row["id"].ToString(),
                        TableName = row["TableName"].ToString(),
                        Cat_Name  = row["Cat_Name"].ToString(),
                        Status    = row["Status"] != DBNull.Value && (bool)row["Status"],
                        Balance   = row["Balance"].ToString()
                    });
                }
                dgv_Table.ItemsSource = list;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء تحميل الطاولات\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region Grid Selection

        private void dgv_Table_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgv_Table.SelectedItem is TableModel selected)
            {
                try
                {
                    if (conn.State != ConnectionState.Open) conn.Open();

                    var cmd = new SqlCommand(
                        "SELECT * FROM Tables WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("@id", selected.id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (int.TryParse(reader["id"].ToString(), out int parsedId))
                                Code = parsedId;
                            txtTablename.Text = reader["TableName"].ToString();
                            txtTableid.Text   = reader["id"].ToString();

                            // تحديد القسم
                            var catIdObj = reader["Cat_ID"];
                            if (catIdObj != DBNull.Value)
                                cmbCat.SelectedValue = catIdObj;

                            bool statusVal = reader["Status"] != DBNull.Value &&
                                            (bool)reader["Status"];
                        }
                    }
                }
                catch (Exception ex)
                {
                    DXMessageBox.Show("خطأ أثناء تحميل بيانات الطاولة\n" + ex.Message,
                        "", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    if (conn.State != ConnectionState.Closed) conn.Close();
                }
            }
        }

        #endregion

        #region Save

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtTablename.Text))
                {
                    DXMessageBox.Show("أدخل اسم الطاولة", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Code == -1)
                {
                    if (cmbCat.SelectedValue == null ||
                        cmbCat.SelectedValue.ToString() == "-1")
                    {
                        DXMessageBox.Show("الرجاء اختيار القسم", "",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    InsertTable();
                }
                else
                {
                    UpdateTable();
                }

                Clear();
                LoadTables();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحفظ\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void InsertTable()
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                var cmd = new SqlCommand(
                    "INSERT INTO Tables(TableName,Cat_ID,Status,Balance,InvGlobalID) " +
                    "VALUES(@TableName,@Cat_ID,@Status,@Balance,N'')", conn);

                cmd.Parameters.AddWithValue("@TableName", txtTablename.Text);
                cmd.Parameters.AddWithValue("@Cat_ID",    cmbCat.SelectedValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Status",    0);
                cmd.Parameters.AddWithValue("@Balance",   0.0);
                cmd.ExecuteNonQuery();

                DXMessageBox.Show("تمت الإضافة بنجاح", "إضافة",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        public void UpdateTable()
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                var cmd = new SqlCommand(
                    "UPDATE Tables SET TableName=@TableName, Cat_ID=@Cat_ID, " +
                    "Status=@Status, Balance=@Balance, InvGlobalID=N'' " +
                    "WHERE id=@id", conn);

                cmd.Parameters.AddWithValue("@TableName", txtTablename.Text);
                cmd.Parameters.AddWithValue("@id",        txtTableid.Text);
                cmd.Parameters.AddWithValue("@Cat_ID",    cmbCat.SelectedValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Status",    0);
                cmd.Parameters.AddWithValue("@Balance",   0.0);
                cmd.ExecuteNonQuery();

                DXMessageBox.Show("تم التعديل بنجاح", "تعديل",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region Delete

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (Code == -1)
            {
                DXMessageBox.Show("لا يمكن الحذف - لم يتم اختيار طاولة", "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DeleteTable();
            LoadTables();
        }

        public void DeleteTable()
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                var cmd = new SqlCommand(
                    "DELETE FROM Tables WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", txtTableid.Text);
                cmd.ExecuteNonQuery();

                DXMessageBox.Show("تم الحذف بنجاح", "حذف",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Clear();
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region Navigation

        public void Navigate(string sqlQuery)
        {
            try
            {
                dgv_Table.UnselectAll();
                var cmd = new SqlCommand(sqlQuery, conn);
                if (conn.State != ConnectionState.Open) conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    ReadData(reader);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء التنقل\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void ReadData(SqlDataReader reader)
        {
            if (reader.HasRows)
            {
                reader.Read();
                Clear();
                if (int.TryParse(reader["id"].ToString(), out int parsedId))
                    Code = parsedId;
                txtTablename.Text = reader["TableName"].ToString();
                txtTableid.Text   = reader["id"].ToString();

                var catIdObj = reader["Cat_ID"];
                if (catIdObj != DBNull.Value)
                    cmbCat.SelectedValue = catIdObj;
            }
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate("SELECT TOP 1 * FROM Tables ORDER BY id ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate("SELECT TOP 1 * FROM Tables ORDER BY id DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM Tables WHERE id>{Code} ORDER BY id ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate($"SELECT TOP 1 * FROM Tables WHERE id<{Code} ORDER BY id DESC");
        }

        #endregion

        #region Other Events

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            Clear();
            LoadTables();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnAddGrp_Click(object sender, RoutedEventArgs e)
        {
            var FrmDept = new FrmDept();
            FrmDept.Show();
        }

        #endregion
    }

    #region Model

    public class TableModel
    {
        public string id        { get; set; }
        public string TableName { get; set; }
        public string Cat_Name  { get; set; }
        public bool   Status    { get; set; }
        public string Balance   { get; set; }
    }

    #endregion
}