using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Windows;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using UtilitiesProj;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmDBManagment : ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;

        #endregion

        #region Constructor

        public frmDBManagment()
        {
            InitializeComponent();
            _conn = MainClass.ConnObj();
            Loaded += FrmDBManagment_Loaded;
        }

        #endregion

        #region Window Events

        private void FrmDBManagment_Loaded(object sender, RoutedEventArgs e)
        {
            txtStartdate.DateTime = DateTime.Now;
            txtEnddate.DateTime = DateTime.Now.AddYears(1);
            StartDateProid.DateTime = DateTime.Now;
            EndDateProid.DateTime = DateTime.Now.AddYears(1);

            rbActiveDbs.IsChecked = true;

            WireUpEvents();
            LoadDatabases();
        }

        private void WireUpEvents()
        {
            rbAllDBs.Checked += RadioFilter_Changed;
            rbActiveDbs.Checked += RadioFilter_Changed;
            rbInactiveDbs.Checked += RadioFilter_Changed;

            btnCreateDatabase.Click += BtnCreateDatabase_Click;
            btnSaveChange.Click += BtnSaveChange_Click;
            BtnSave.Click += BtnSavePeriod_Click;

            GridView1.MouseRightButtonDown += GridView1_MouseRightButtonDown;
        }

        #endregion

        #region Load Databases

        public void LoadDatabases()
        {
            try
            {
                if (_conn.State != ConnectionState.Open)
                    _conn.Open();

                string activeFilter = string.Empty;

                if (rbActiveDbs.IsChecked == true)
                    activeFilter = " AND IsActive=1 ";
                else if (rbInactiveDbs.IsChecked == true)
                    activeFilter = " AND IsActive=0 ";

                var adapter =
                    new SqlDataAdapter(
                        "SELECT * FROM master.dbo.DatabasesManagment " +
                        $"WHERE IsDeleted=0 {activeFilter} " +
                        "ORDER BY ImportanceOrder",
                        _conn);

                var sourceTable = new DataTable();
                adapter.Fill(sourceTable);

                // بناء جدول العرض
                var displayTable = new DataTable();
                displayTable.Columns.Add("DbId", typeof(int));
                displayTable.Columns.Add("DbName", typeof(string));
                displayTable.Columns.Add("CreateDate", typeof(string));
                displayTable.Columns.Add("IsActive", typeof(bool));

                foreach (DataRow row in sourceTable.Rows)
                {
                    displayTable.Rows.Add(
                        row["dbid"],
                        row["Dbname"].ToString(),
                        Convert.ToDateTime(row["CreateTime"]).ToShortDateString(),
                        Convert.ToBoolean(row["IsActive"])
                    );
                }

                DgvDBs.ItemsSource = displayTable.DefaultView;

                // تعبئة ComboBoxes
                cmbDB.ItemsSource = sourceTable.DefaultView;
                cmbDB.DisplayMember = "Dbname";
                cmbDB.ValueMember = "dbid";
                cmbDB.EditValue = null;

                CmbDbName.ItemsSource = sourceTable.DefaultView;
                CmbDbName.DisplayMember = "Dbname";
                CmbDbName.ValueMember = "dbid";
                CmbDbName.EditValue = null;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                if (_conn.State != ConnectionState.Closed)
                    _conn.Close();
            }
        }

        #endregion

        #region Radio Button Events

        private void RadioFilter_Changed(object sender, RoutedEventArgs e)
        {
            LoadDatabases();
        }

        #endregion

        #region Create Database

        private void BtnCreateDatabase_Click(object sender, RoutedEventArgs e)
        {
            var errorDetails = new StringBuilder();

            try
            {
                string dbDisplayName =
                    txtDbName.EditValue?.ToString().Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(dbDisplayName))
                {
                    DXMessageBox.Show("يرجى إدخال اسم قاعدة البيانات",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtDbName.Focus();
                    return;
                }

                string connString = BuildConnectionString();
                var masterConn = new SqlConnection(connString);

                if (masterConn.State != ConnectionState.Open)
                    masterConn.Open();

                if (masterConn.State != ConnectionState.Open)
                {
                    DXMessageBox.Show("السيرفر غير متصل", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // التحقق من تكرار الاسم
                var dupAdapter = new SqlDataAdapter(
                    $"SELECT * FROM master.dbo.DatabasesManagment " +
                    $"WHERE Dbname='{dbDisplayName}'",
                    masterConn);
                var dupTable = new DataTable();
                dupAdapter.Fill(dupTable);

                if (dupTable.Rows.Count > 0)
                {
                    DXMessageBox.Show(
                        "اسم قاعدة البيانات مستخدم من قبل، يرجى إدخال اسم آخر",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtDbName.Focus();
                    return;
                }

                // الحصول على رقم تلقائي فريد
                string autoName = GenerateUniqueDbName(masterConn);

                // إنشاء قاعدة البيانات
                CreateDatabaseOnServer(masterConn, autoName);

                // الاتصال بالقاعدة الجديدة وإنشاء الجداول
                string newConnStr = BuildConnectionString(autoName);
                var newConn = new SqlConnection(newConnStr);

                if (newConn.State == ConnectionState.Closed)
                    newConn.Open();

                // تشغيل سكريبت الجداول
                string[] scripts = Properties.Resources.CrystalLiteDB
                    .Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string script in scripts)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(script))
                            new SqlCommand(script, newConn).ExecuteNonQuery();
                    }
                    catch { /* ignore individual script errors */ }
                }

                // الحصول على dbid
                var dbIdAdapter = new SqlDataAdapter(
                    $"SELECT dbid FROM master.dbo.sysdatabases " +
                    $"WHERE name=N'{autoName}'",
                    masterConn);
                var dbIdTable = new DataTable();

                if (masterConn.State == ConnectionState.Closed)
                    masterConn.Open();

                dbIdAdapter.Fill(dbIdTable);

                string dbId = dbIdTable.Rows.Count > 0
                    ? dbIdTable.Rows[0]["dbid"].ToString()
                    : "1";

                // إدراج المعلومات
                InsertDatabaseManagement(newConn, dbId, autoName, dbDisplayName);

                newConn.Close();
                masterConn.Close();

                DXMessageBox.Show(
                    $"✅ تمت عملية إنشاء قاعدة البيانات '{dbDisplayName}' بنجاح",
                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadDatabases();
            }
            catch (SqlException ex)
            {
                for (int i = 0; i < ex.Errors.Count; i++)
                    errorDetails.AppendLine(
                        $"[{i}] {ex.Errors[i].Message} " +
                        $"(Line: {ex.Errors[i].LineNumber})");

                ShowError(errorDetails.ToString());
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private string GenerateUniqueDbName(SqlConnection conn)
        {
            var maxAdapter = new SqlDataAdapter(
                "SELECT MAX(dbid) FROM master.dbo.sysdatabases", conn);
            var maxTable = new DataTable();
            maxAdapter.Fill(maxTable);

            int nextId = Convert.ToInt32(
                Convert.ToDouble(maxTable.Rows[0][0].ToString())) + 1;

            string name = $"Data{nextId}";

            while (true)
            {
                var checkAdapter = new SqlDataAdapter(
                    $"SELECT dbid FROM master.dbo.sysdatabases WHERE name=N'{name}'",
                    conn);
                var checkTable = new DataTable();
                checkAdapter.Fill(checkTable);

                if (checkTable.Rows.Count == 0)
                    return name;

                nextId++;
                name = $"Data{nextId}";
            }
        }

        private void CreateDatabaseOnServer(SqlConnection conn, string dbName)
        {
            var cmd = new SqlCommand($"CREATE DATABASE {dbName.Trim()}", conn);

            try
            {
                if (conn.State == ConnectionState.Closed)
                    conn.Open();

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                ShowError(ex.ToString());
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();
            }
        }

        private void InsertDatabaseManagement(
            SqlConnection conn, string dbId,
            string autoName, string displayName)
        {
            var cmd = new SqlCommand(
                @"INSERT INTO master.dbo.DatabasesManagment
                  (DbId,DbAutoName,Dbname,CreateTime,UserCreate,
                   AccountingPeriodStart,AccountingPeriodEnd,
                   UserName,UseFullName,passward,DefaultCoin,
                   IsActive,IsDeleted,ImportanceOrder)
                  VALUES
                  (@DbId,@DbAutoName,@Dbname,@CreateTime,@UserCreate,
                   @AccountingPeriodStart,@AccountingPeriodEnd,
                   @UserName,@UseFullName,@passward,@DefaultCoin,
                   @IsActive,@IsDeleted,@ImportanceOrder)",
                conn);

            cmd.Parameters.AddWithValue("@DbId", dbId);
            cmd.Parameters.AddWithValue("@DbAutoName", autoName);
            cmd.Parameters.AddWithValue("@Dbname", displayName);
            cmd.Parameters.AddWithValue("@CreateTime", DateTime.Now);
            cmd.Parameters.AddWithValue("@UserCreate", MainClass.EmpNo);
            cmd.Parameters.AddWithValue("@AccountingPeriodStart", txtStartdate.DateTime);
            cmd.Parameters.AddWithValue("@AccountingPeriodEnd", txtEnddate.DateTime);
            cmd.Parameters.AddWithValue("@UserName", "1");
            cmd.Parameters.AddWithValue("@UseFullName", "1");
            cmd.Parameters.AddWithValue("@passward", "-1");
            cmd.Parameters.AddWithValue("@DefaultCoin", string.Empty);
            cmd.Parameters.AddWithValue("@IsActive", 1);
            cmd.Parameters.AddWithValue("@IsDeleted", 0);
            cmd.Parameters.AddWithValue("@ImportanceOrder", 1);
            cmd.ExecuteNonQuery();
        }

        #endregion

        #region Rename Database

        private void BtnSaveChange_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string connString = BuildConnectionString();
                var conn = new SqlConnection(connString);

                if (conn.State != ConnectionState.Open)
                    conn.Open();

                if (conn.State != ConnectionState.Open)
                {
                    DXMessageBox.Show(
                        MainClass.Language == "ar"
                            ? "السيرفر غير متصل"
                            : "Server is not connected",
                        "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (cmbDB.EditValue == null)
                {
                    DXMessageBox.Show(
                        MainClass.Language == "ar"
                            ? "يجب اختيار ملف"
                            : "Please select a database",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string newName = txtDBNewName.EditValue?.ToString() ?? string.Empty;

                if (string.IsNullOrEmpty(newName))
                {
                    DXMessageBox.Show(
                        MainClass.Language == "ar"
                            ? "يرجى إدخال اسم قاعدة البيانات"
                            : "Please enter database name",
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtDBNewName.Focus();
                    return;
                }

                var cmd = new SqlCommand(
                    $"UPDATE master.dbo.DatabasesManagment " +
                    $"SET [Dbname]=@Dbname " +
                    $"WHERE DbId={Convert.ToDouble(cmbDB.EditValue)}",
                    conn);

                cmd.Parameters.Add("@Dbname", SqlDbType.NVarChar).Value = newName;
                cmd.ExecuteNonQuery();

                DXMessageBox.Show(
                    MainClass.Language == "ar"
                        ? "✅ تم تغيير اسم الملف بنجاح"
                        : "✅ Database name changed successfully",
                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadDatabases();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region Edit Accounting Period

        private void BtnSavePeriod_Click(object sender, RoutedEventArgs e)
        {
            string dbName = CmbDbName.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(dbName))
            {
                DXMessageBox.Show("يرجى اختيار قاعدة البيانات",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DateTime.Compare(
                    StartDateProid.DateTime.Date,
                    EndDateProid.DateTime.Date) == 0)
            {
                DXMessageBox.Show(
                    "لا يمكن أن يكون تاريخ البداية نفس تاريخ النهاية",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            UpdateAccountingPeriod();
        }

        public void UpdateAccountingPeriod()
        {
            var conn = new SqlConnection(
                $"server={MainClass.Server};database=master;trusted_connection=yes;");

            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                var cmd = new SqlCommand(
                    "UPDATE master.dbo.DatabasesManagment " +
                    "SET AccountingPeriodStart=@AccountingPeriodStart, " +
                    "    AccountingPeriodEnd=@AccountingPeriodEnd " +
                    $"WHERE Dbname=N'{CmbDbName.Text}'",
                    conn);

                cmd.Parameters.AddWithValue(
                    "@AccountingPeriodStart",
                    StartDateProid.DateTime);
                cmd.Parameters.AddWithValue(
                    "@AccountingPeriodEnd",
                    EndDateProid.DateTime);
                cmd.ExecuteNonQuery();

                DXMessageBox.Show("✅ تم تعديل الفترة المحاسبية بنجاح",
                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                if (conn.State != ConnectionState.Closed)
                    conn.Close();
            }
        }

        #endregion

        #region Grid Right-Click (Activate / Freeze)

        private void GridView1_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DgvDBs.CurrentItem is not DataRowView rowView)
                return;

            bool isActive = Convert.ToBoolean(rowView["IsActive"]);
            int dbId = Convert.ToInt32(rowView["DbId"]);

            string message = isActive
                ? "هل تريد تجميد قاعدة البيانات المحددة؟"
                : "هل تريد تنشيط قاعدة البيانات المحددة؟";

            var result = DXMessageBox.Show(
                message, "تنبيه",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                string connStr = BuildConnectionString();
                var conn = new SqlConnection(connStr);

                if (conn.State != ConnectionState.Open)
                    conn.Open();

                int newStatus = isActive ? 0 : 1;

                new SqlCommand(
                    $"UPDATE master.dbo.DatabasesManagment " +
                    $"SET [IsActive]={newStatus} WHERE DbId={dbId}",
                    conn).ExecuteNonQuery();

                string successMsg = isActive
                    ? "✅ تم تجميد قاعدة البيانات بنجاح"
                    : "✅ تم تنشيط قاعدة البيانات بنجاح";

                DXMessageBox.Show(successMsg, "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                LoadDatabases();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region Helpers

        private string BuildConnectionString(string database = null)
        {
            if (MainClass.UseServerAuth)
            {
                return string.IsNullOrEmpty(database)
                    ? $"server={MainClass.Server};user id={MainClass.NetUserId};pwd={MainClass.NetPwd}"
                    : $"server={MainClass.Server};database={database};user id={MainClass.NetUserId};pwd={MainClass.NetPwd}";
            }

            return string.IsNullOrEmpty(database)
                ? $"server={MainClass.Server};trusted_connection=true;"
                : $"server={MainClass.Server};database={database};trusted_connection=true;";
        }

        private void ShowError(string message)
        {
            DXMessageBox.Show(message, "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}