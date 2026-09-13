using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmServerSetting : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        public  SqlConnection conn;
        private int    Code      = -1;
        private int    tabNo     = 0;
        private string SelectedDb = "";

        private ObservableCollection<DbRow> DbList;
        //منع الأحداث من العمل قبل اكتمال تحميل النافذة
        private bool _isLoaded;

        #endregion

        #region Constructor

        public frmServerSetting()
        {
            conn = MainClass.ConnObj();
            DbList = new ObservableCollection<DbRow>();
            
            _isLoaded = false;
            InitializeComponent();
            DgvDBs.ItemsSource = DbList;
        }

        #endregion

        #region Window Loaded

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var now  = DateTime.Now;
            txtStartdate.DateTime = new DateTime(now.Year, 1, 1);
            txtEnddate.DateTime   = new DateTime(now.Year, 12, 31);

            lblIsOk.Text = "";
            rbActiveDbs.IsChecked = true;

            if (!string.IsNullOrEmpty(MainClass.Server))
                txtServer.Text = MainClass.Server;

            _isLoaded = true;

            loadDatabases();
            loadSalesmanData();
        }

        #endregion

        #region Build Connection String

        private string BuildConnStr(string database = "master")
        {
            if (rdLocal.IsChecked == true)
            {
                if (chkServerAuth.IsChecked == true)
                    return $"server={txtServer.Text.Trim()};" +
                           $"database={database};" +
                           $"user id={txtUser.Text}; pwd={txtPWD.Password};";
                else
                    return $"server={txtServer.Text.Trim()};" +
                           $"database={database};trusted_connection=true;";
            }
            else
            {
                return $"server={txtServer.Text.Trim()};" +
                       $"database={database};" +
                       $"user id={txtUser.Text}; pwd={txtPWD.Password};";
            }
        }

        #endregion

        #region Load Helpers

        public void loadDatabases()
        {
            try
            {
                using var localConn = new SqlConnection(BuildConnStr());
                localConn.Open();

                var adapter = new SqlDataAdapter(
                    "SELECT sysdatabases.name, DatabasesManagment.Dbname " +
                    "FROM master.dbo.DatabasesManagment, master.dbo.sysdatabases " +
                    "WHERE DatabasesManagment.dbid=sysdatabases.dbid " +
                    "AND DatabasesManagment.IsActive=1 " +
                    "AND DatabasesManagment.IsDeleted=0 " +
                    "ORDER BY DatabasesManagment.ImportanceOrder",
                    localConn);
                var dt = new DataTable();
                adapter.Fill(dt);

                cmbDatabases.ItemsSource        = dt.DefaultView;
                cmbDatabases.DisplayMemberPath  = "Dbname";
                cmbDatabases.SelectedValuePath  = "name";
                cmbDatabases.SelectedIndex      = -1;
            }
            catch { /* تجاهل */ }
        }

        private void loadSalesmanData()
        {
            try
            {
                txtSalesmaneMobile.Text  = Common.AppSalesmaneMobie;
                txtSalemaneEmail.Text    = Common.AppSalesmaneEmail;
                txtSalesmanWebsite.Text  = Common.AppSalesmaneWebsite;
                txtSalesmanAddress.Text  = Common.AppSalesmaneAdress;
            }
            catch { /* تجاهل */ }
        }

        public void loadDBs()
        {
            try
            {
                if (DbList == null)
                    DbList = new ObservableCollection<DbRow>();

                if (conn == null)
                    conn = MainClass.ConnObj();

                DbList.Clear();

                if (conn == null)
                    throw new Exception("تعذر إنشاء اتصال بقاعدة البيانات");

                if (conn.State != ConnectionState.Open)
                    conn.Open();

                string condFilter = "";
                if (rbActiveDbs != null && rbActiveDbs.IsChecked == true)
                    condFilter = " AND IsActive=1";
                else if (rbInactiveDbs != null && rbInactiveDbs.IsChecked == true)
                    condFilter = " AND IsActive=0";

                var adapter = new SqlDataAdapter(
                    $"SELECT * FROM master.dbo.DatabasesManagment " +
                    $"WHERE IsDeleted=0{condFilter} ORDER BY ImportanceOrder",
                    conn);

                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    DbList.Add(new DbRow
                    {
                        DbId = Convert.ToInt32(row["dbid"]),
                        AutoName = row["DbAutoName"]?.ToString() ?? "",
                        DbName = row["Dbname"]?.ToString() ?? "",
                        CreateDate = Convert.ToDateTime(row["CreateTime"]).ToShortDateString(),
                        IsActive = Convert.ToBoolean(row["IsActive"])
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في تحميل قواعد البيانات: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn != null && conn.State == ConnectionState.Open)
                    conn.Close();
            }
        }

        private void UpdateLicenseEndDate()
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                new SqlCommand(
                    "UPDATE License SET LicenseExpire = DATEADD(YEAR, 1, LicenseExpire);",
                    conn).ExecuteNonQuery();
            }
            catch { /* تجاهل */ }
        }

        #endregion

        #region Create Database

        private void CreateDatabase(SqlConnection conn1)
        {
            string sql = $"USE [master] CREATE DATABASE {txtDbName?.Text?.Trim() ?? "NewDB"}";
            ExecuteDbSetup(conn1, sql);
        }

        private void CreateDatabase(SqlConnection conn1, string dbName)
        {
            string sql = $"CREATE DATABASE {dbName.Trim()}";

            if (!string.IsNullOrWhiteSpace(txtPath?.Text))
            {
                sql += $" CONTAINMENT=NONE ON PRIMARY " +
                       $"(NAME=N'{dbName}', FILENAME=N'{txtPath.Text}.mdf', " +
                       $"SIZE=9280KB, MAXSIZE=UNLIMITED, FILEGROWTH=1024KB) " +
                       $"LOG ON (NAME=N'{dbName}_log', " +
                       $"FILENAME=N'{txtPath.Text}.ldf', " +
                       $"SIZE=3904KB, MAXSIZE=2048GB, FILEGROWTH=10%)";
            }

            ExecuteDbSetup(conn1, sql);
        }

        private void ExecuteDbSetup(SqlConnection conn1, string createSql)
        {
            try
            {
                if (conn1.State == ConnectionState.Closed) conn1.Open();

                new SqlCommand(createSql, conn1).ExecuteNonQuery();

                string[] setupCmds =
                {
                    "IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name=N'auditor') " +
                    "CREATE LOGIN [auditor] WITH PASSWORD=N'auditor', DEFAULT_DATABASE=[master]",
                    "ALTER SERVER ROLE [sysadmin] ADD MEMBER [auditor]",
                    "If Not EXISTS(SELECT * FROM sys.database_principals WHERE name=N'auditor') " +
                    "CREATE USER [auditor] FOR LOGIN [auditor] WITH DEFAULT_SCHEMA=[dbo]",
                    "ALTER ROLE [db_owner] ADD MEMBER[auditor]",
                    "ALTER LOGIN sa ENABLE",
                    "ALTER LOGIN sa WITH PASSWORD='auditor'",
                };

                foreach (var cmd in setupCmds)
                {
                    try { new SqlCommand(cmd, conn1).ExecuteNonQuery(); }
                    catch { /* تجاهل أخطاء غير حرجة */ }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.ToString(), "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn1.State == ConnectionState.Open) conn1.Close();
            }
        }

        #endregion

        #region Insert DB Management

        private void InsertInDatabasesManagment(string dbDist, int dbId, string dbName)
        {
            try
            {
                using var localConn = new SqlConnection(BuildConnStr());
                localConn.Open();

                var cmd = new SqlCommand(
                    "INSERT INTO master.dbo.DatabasesManagment(" +
                    "[DbId],[DbAutoName],[Dbname],[CreateTime],UserCreate," +
                    "[AccountingPeriodStart],[AccountingPeriodEnd]," +
                    "[UserName],[UseFullName],[passward],[DefaultCoin]," +
                    "[IsActive],[IsDeleted],[ImportanceOrder]) " +
                    "VALUES(@DbId,@DbAutoName,@Dbname,@CreateTime,@UserCreate," +
                    "@AccountingPeriodStart,@AccountingPeriodEnd," +
                    "@UserName,@UseFullName,@passward,@DefaultCoin," +
                    "@IsActive,@IsDeleted,@ImportanceOrder)",
                    localConn);

                cmd.Parameters.Add("@DbId",                 SqlDbType.Int     ).Value = dbId;
                cmd.Parameters.Add("@DbAutoName",           SqlDbType.NVarChar).Value = dbDist;
                cmd.Parameters.Add("@Dbname",               SqlDbType.NVarChar).Value = dbName;
                cmd.Parameters.Add("@CreateTime",           SqlDbType.DateTime).Value = DateTime.Now;
                cmd.Parameters.Add("@UserCreate",           SqlDbType.Int     ).Value = MainClass.EmpNo;
                cmd.Parameters.Add("@AccountingPeriodStart",SqlDbType.DateTime).Value = txtStartdate.DateTime;
                cmd.Parameters.Add("@AccountingPeriodEnd",  SqlDbType.DateTime).Value = txtEnddate.DateTime;
                cmd.Parameters.Add("@UserName",             SqlDbType.NVarChar).Value = "1";
                cmd.Parameters.Add("@UseFullName",          SqlDbType.NVarChar).Value = "1";
                cmd.Parameters.Add("@passward",             SqlDbType.NVarChar).Value = "-1";
                cmd.Parameters.Add("@DefaultCoin",          SqlDbType.NVarChar).Value = "";
                cmd.Parameters.Add("@IsActive",             SqlDbType.Bit     ).Value = 1;
                cmd.Parameters.Add("@IsDeleted",            SqlDbType.Float   ).Value = 0;
                cmd.Parameters.Add("@ImportanceOrder",      SqlDbType.Int     ).Value = 1;
                cmd.ExecuteNonQuery();

                _isLoaded = true;
                loadDBs();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Restore DB

        private void RestoreDb()
        {
            if (string.IsNullOrWhiteSpace(txtRestore.Text))
            {
                DXMessageBox.Show("يجب تحديد مسار النسخة الاحتياطية.", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var localConn = new SqlConnection(
                    $"server={MainClass.Server};trusted_connection=true;");
                localConn.Open();

                string dbName = "";
                int    id     = 1;
                Common.GetValidDBname(ref dbName, ref id);

                string moveSql =
                    $"Restore Database {dbName} FROM DISK=N'{txtRestore.Text}' " +
                    $"WITH FILE=1, " +
                    $"MOVE N'{txtSrcDb.Text.Trim()}' TO " +
                    $"N'C:\\Program Files\\Microsoft SQL Server\\MSSQL11.SQLEXPRESS2012\\MSSQL\\DATA\\{dbName}.mdf', " +
                    $"MOVE N'{txtSrcDb.Text.Trim()}_log' TO " +
                    $"N'C:\\Program Files\\Microsoft SQL Server\\MSSQL11.SQLEXPRESS2012\\MSSQL\\DATA\\{dbName}_log.ldf', " +
                    $"NOUNLOAD, STATS=5";

                new SqlCommand(moveSql, localConn).ExecuteNonQuery();

                DXMessageBox.Show("تم استرجاع النسخة الاحتياطية بنجاح.", "تأكيد",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                InsertInDatabasesManagment(dbName, id, txtDbNameDis.Text);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"لم تتم العملية:\n{ex.Message}", "تنبيه",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Button Events

        private void Button2_Click(object sender, RoutedEventArgs e)
            => Close();

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog { Filter = "BAK Files (*.bak)|*.bak|All Files|*.*" };
                if (dlg.ShowDialog() == true)
                    txtRestore.Text = dlg.FileName;
            }
            catch { /* تجاهل */ }
        }

        private void Button5_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnConnectServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtServer.Text))
                {
                    DXMessageBox.Show("أدخل عنوان السيرفر.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtServer.Focus(); return;
                }

                if ((rdNetwork.IsChecked == true || chkServerAuth.IsChecked == true))
                {
                    if (string.IsNullOrWhiteSpace(txtUser.Text))
                    {
                        DXMessageBox.Show("أدخل المستخدم.", "تنبيه",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtUser.Focus(); return;
                    }
                    if (string.IsNullOrWhiteSpace(txtPWD.Password))
                    {
                        DXMessageBox.Show("أدخل كلمة المرور.", "تنبيه",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtPWD.Focus(); return;
                    }
                }

                conn = new SqlConnection(BuildConnStr());
                var frmUserLogin = new frmUserLogin();
                if (rdLocal.IsChecked == true)
                {
                    
                    frmUserLogin.conn_type    = 1;
                    MainClass.Conn_type       = 1;
                    if (chkServerAuth.IsChecked == true)
                    {
                        MainClass.NetUserId   = txtUser.Text;
                        MainClass.NetPwd      = txtPWD.Password;
                        MainClass.UseServerAuth = true;
                    }
                }
                else
                {
                    frmUserLogin.conn_type    = 2;
                    MainClass.Conn_type       = 2;
                    MainClass.NetUserId       = txtUser.Text;
                    MainClass.NetPwd          = txtPWD.Password;
                    MainClass.UseServerAuth = true;
                }

                if (conn.State != ConnectionState.Open) conn.Open();

                bool isAr = string.Equals(MainClass.Language, "ar",
                                           StringComparison.OrdinalIgnoreCase);
                lblIsOk.Text = isAr ? "تم الاتصال ✅" : "Connected ✅";

                MainClass.Server = txtServer.Text.Trim();

                Common.AddPreviousDBs();
                Common.AddSalesmanSettings();
                Common.AddSettingNotify();

                _isLoaded = true;
                loadDBs();
                UpdateLicenseEndDate();

                frmUserLogin.conn = conn;
                frmUserLogin.loadDBs();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"حدث خطأ أثناء الاتصال:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var frmUserLogin = new frmUserLogin();
                if (string.IsNullOrWhiteSpace(cmbDatabases.Text))
                {
                    DXMessageBox.Show("اختر قاعدة البيانات.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (DXMessageBox.Show("هل أنت متأكد من الاتصال بقاعدة البيانات المختارة؟",
                    "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.No) return;

                string dbValue = cmbDatabases.SelectedValue?.ToString()
                              ?? cmbDatabases.Text;

                if (rdLocal.IsChecked == true)
                {
                    frmUserLogin.conn = new SqlConnection(
                        $"server={txtServer.Text};database={dbValue};trusted_connection=true;");
                    frmUserLogin.conn_type = 1;
                }
                else
                {
                    frmUserLogin.conn = new SqlConnection(
                        $"server={txtServer.Text};database={dbValue};" +
                        $"user id={txtUser.Text}; pwd={txtPWD.Password};");
                    frmUserLogin.conn_type   = 2;
                    frmUserLogin.NetUserid   = txtUser.Text;
                    frmUserLogin.Netpwd      = txtPWD.Password;
                }

                MainClass.Server = txtServer.Text;
                frmUserLogin.txtDatabase = dbValue;
                lblCurrDatabase.Text     = dbValue;

                frmUserLogin.loadDBs();
                //احتمال
                frmUserLogin.txtdatabaseName.Text = dbValue;
                frmUserLogin.cmbDatabases.Text    = cmbDatabases.Text;
                lblCurrDatabase.Foreground        = System.Windows.Media.Brushes.Green;

                frmUserLogin.LoadBranches();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCreateDatabase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var checkPwd = new frmCheckPwd { CheckType = 100 };
                checkPwd.ShowDialog();
                if (!checkPwd.Iscorrect) return;

                if (string.IsNullOrWhiteSpace(txtDbName.Text))
                {
                    DXMessageBox.Show("يرجى إدخال اسم قاعدة البيانات.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using var localConn = new SqlConnection(BuildConnStr());
                localConn.Open();

                // التحقق من التكرار
                var checkAdp = new SqlDataAdapter(
                    $"SELECT * FROM master.dbo.DatabasesManagment WHERE Dbname='{txtDbName.Text.Trim()}'",
                    localConn);
                var checkDt = new DataTable();
                checkAdp.Fill(checkDt);

                if (checkDt.Rows.Count > 0)
                {
                    DXMessageBox.Show("اسم قاعدة البيانات مستخدم مسبقاً.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // توليد اسم تلقائي
                var maxAdp = new SqlDataAdapter(
                    "SELECT MAX(dbid) FROM master.dbo.sysdatabases", localConn);
                var maxDt = new DataTable();
                maxAdp.Fill(maxDt);
                string newId = (Convert.ToDouble(maxDt.Rows[0][0]) + 1).ToString();
                string autoName = $"Data{newId}";

                // التحقق من عدم وجود اسم مكرر
                bool nameOk = false;
                while (!nameOk)
                {
                    var nameAdp = new SqlDataAdapter(
                        $"SELECT dbid FROM master.dbo.sysdatabases WHERE name=N'{autoName}'",
                        localConn);
                    var nameDt = new DataTable();
                    nameAdp.Fill(nameDt);
                    if (nameDt.Rows.Count == 0) nameOk = true;
                    else { newId = (double.Parse(newId) + 1).ToString(); autoName = $"Data{newId}"; }
                }

                CreateDatabase(localConn, autoName);

                string connStr2 = BuildConnStr(autoName.Trim());
                using var conn2 = new SqlConnection(connStr2);

                var sqlParts = Properties.Resources.CrystalLiteDB.Split(
                    new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);

                if (conn2.State == ConnectionState.Closed) conn2.Open();

                // جلب dbid
                var idAdp = new SqlDataAdapter(
                    $"SELECT dbid FROM master.dbo.sysdatabases WHERE name=N'{autoName}'",
                    localConn);
                var idDt = new DataTable();
                idAdp.Fill(idDt);
                if (idDt.Rows.Count == 1)
                    newId = idDt.Rows[0]["dbid"].ToString();

                foreach (var part in sqlParts)
                    new SqlCommand(part, conn2).ExecuteNonQuery();

                InsertInDatabasesManagment(autoName, int.Parse(newId), txtDbName.Text);

                DXMessageBox.Show("تمت عملية إنشاء قاعدة البيانات بنجاح.", "نجاح",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDoRestore_Click(object sender, RoutedEventArgs e)
            => RestoreDb();

        private void btnRestoreFromScript_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtRestore.Text))
                {
                    DXMessageBox.Show("يجب تحديد مسار النسخة الاحتياطية.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var confirm = new frmAttentionMsg();
                confirm.btnInsure.IsEnabled = true;
                confirm.lblmsg.Text = "هل أنت متأكد من استعادة النسخة الاحتياطية؟";
                confirm.ShowDialog();
                if (!confirm.IsSure) return;

                string dbName = ""; int id = 1;
                Common.GetValidDBname(ref dbName, ref id);

                using var localConn = new SqlConnection(
                    $"server={MainClass.Server};trusted_connection=true;");
                localConn.Open();

                CreateDatabase(localConn, dbName);

                using var conn2 = new SqlConnection(
                    $"server={MainClass.Server};database={dbName.Trim()};trusted_connection=true");
                conn2.Open();

                using var sr = new StreamReader(txtRestore.Text, Encoding.Default);
                string script = sr.ReadToEnd();

                try { new SqlCommand(script, conn2).ExecuteNonQuery(); }
                catch { /* تجاهل أخطاء جزئية */ }

                sr.Close();
                DXMessageBox.Show("تم التنفيذ بنجاح.", "تم",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPath_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog { FileName = txtDbName.Text };
            if (dlg.ShowDialog() == true)
                txtPath.Text = dlg.FileName;
        }

        private void btnSaveSalesmaneSetting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();

                var checkAdp = new SqlDataAdapter(
                    $"SELECT code FROM SalesmanSetting " +
                    $"WHERE IS_Active=1 AND code<>N'{txtSalesmanCode.Text}'",
                    conn);
                var checkDt = new DataTable();
                checkAdp.Fill(checkDt);

                if (checkDt.Rows.Count > 0)
                {
                    DXMessageBox.Show("تم حفظ الإعدادات من قبل.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var existAdp = new SqlDataAdapter(
                    $"SELECT code FROM SalesmanSetting WHERE code=N'{txtSalesmanCode.Text}'",
                    conn);
                var existDt = new DataTable();
                existAdp.Fill(existDt);

                if (existDt.Rows.Count == 0)
                {
                    DXMessageBox.Show("لا يوجد موزع بهذا الرمز.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var cmd = new SqlCommand(
                    $"UPDATE SalesmanSetting SET is_Active=1, " +
                    $"email=@email, mobileNo=@mobileNo, " +
                    $"address=@address, website=@website " +
                    $"WHERE code=N'{txtSalesmanCode.Text}'",
                    conn);
                cmd.Parameters.Add("@mobileNo", SqlDbType.NVarChar).Value = txtSalesmaneMobile.Text;
                cmd.Parameters.Add("@email",    SqlDbType.NVarChar).Value = txtSalemaneEmail.Text;
                cmd.Parameters.Add("@address",  SqlDbType.NVarChar).Value = txtSalesmanAddress.Text;
                cmd.Parameters.Add("@website",  SqlDbType.NVarChar).Value = txtSalesmanWebsite.Text;
                cmd.ExecuteNonQuery();

                DXMessageBox.Show("تم الحفظ بنجاح.", "تم",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"حدث خطأ أثناء الحفظ:\n{ex.Message}", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DbRow item)
            {
                if (rdNetwork.IsChecked == true)
                {
                    DXMessageBox.Show("لا يمكن تحديث البيانات من جهاز طرفي.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    using var localConn = new SqlConnection(BuildConnStr(item.AutoName));
                    var parts = Properties.Resources.AlterDb.Split(
                        new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);
                    localConn.Open();
                    foreach (var p in parts)
                        new SqlCommand(p, localConn).ExecuteNonQuery();

                    DXMessageBox.Show("تمت عملية تحديث قاعدة البيانات.", "تم",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnScript_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DbRow item)
            {
                if (!rdLocal.IsChecked == true)
                {
                    DXMessageBox.Show("لا يمكن التنفيذ من جهاز طرفي.", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dlg = new OpenFileDialog { Filter = "Text Files (*.txt)|*.txt" };
                if (dlg.ShowDialog() != true) return;

                try
                {
                    using var localConn = new SqlConnection(BuildConnStr(item.AutoName));
                    localConn.Open();

                    using var sr = new StreamReader(
                        System.IO.Path.GetDirectoryName(dlg.FileName) + "\\script.txt",
                        Encoding.Default);

                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (!string.IsNullOrEmpty(line))
                            new SqlCommand(line, localConn).ExecuteNonQuery();
                    }

                    DXMessageBox.Show("تم التنفيذ.", "تم",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region RadioButton / CheckBox Events

        private void rdLocal_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            if (rdLocal == null || chkServerAuth == null || pnlAuth == null || txtServer == null) return;
            if (rdLocal.IsChecked != true) return;

            bool useServerAuth = chkServerAuth.IsChecked == true;

            pnlAuth.IsEnabled = useServerAuth;
            pnlAuth.Opacity = useServerAuth ? 1.0 : 0.5;

            txtServer.Text = (!string.IsNullOrWhiteSpace(MainClass.Server) && MainClass.Conn_type == 1)
                ? MainClass.Server
                : @".\sqlexpress";
        }

        private void rdNetwork_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (rdNetwork?.IsChecked != true) return;

            pnlAuth.IsEnabled = true;
            pnlAuth.Opacity   = 1.0;
            var frmUserLogin = new frmUserLogin();
            if (MainClass.Server != "" && frmUserLogin.conn_type == 2)
                txtServer.Text = MainClass.Server;
            else
                txtServer.Text = "192.168.1.100,1433";

            chkServerAuth.IsEnabled = false;
            txtUser.Focus();
        }

        private void chkServerAuth_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool enabled = (chkServerAuth.IsChecked == true) && (rdLocal.IsChecked == true);
            pnlAuth.IsEnabled = enabled;
            pnlAuth.Opacity   = enabled ? 1.0 : 0.5;
        }

        private void rbActiveDbs_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (rbActiveDbs.IsChecked == true) loadDBs();
        }

        private void rbInactiveDbs_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (rbInactiveDbs.IsChecked == true) loadDBs();
        }

        private void rbAllDBs_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (rbAllDBs.IsChecked == true) loadDBs();
        }

        #endregion

        #region DataGrid Events

        private void DgvDBs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DgvDBs.SelectedItem is DbRow row)
            {
                if (row.IsActive) SelectedDb = row.AutoName;
            }
        }

        private void DgvDBs_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DgvDBs.SelectedItem is not DbRow row) return;

            string action = row.IsActive ? "تجميد" : "تنشيط";
            string sql    = row.IsActive
                ? $"UPDATE master.dbo.DatabasesManagment SET [IsActive]=0 WHERE DbId={row.DbId}"
                : $"UPDATE master.dbo.DatabasesManagment SET [IsActive]=1 WHERE DbId={row.DbId}";

            if (DXMessageBox.Show($"هل تريد {action} قاعدة البيانات المحددة؟",
                "تنبيه", MessageBoxButton.YesNo, MessageBoxImage.Question)
                == MessageBoxResult.Yes)
            {
                try
                {
                    using var localConn = new SqlConnection(BuildConnStr());
                    localConn.Open();
                    new SqlCommand(sql, localConn).ExecuteNonQuery();
                    DXMessageBox.Show($"تم {action} قاعدة البيانات بنجاح.", "تم",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                    loadDBs();
                }
                catch (Exception ex)
                {
                    DXMessageBox.Show($"خطأ:\n{ex.Message}", "خطأ",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region TextBox Events

        private void txtPWD_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                btnConnectServer_Click(null, null);
        }

        #endregion

        #region Window KeyDown

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+R في تبويب الموزع → إلغاء الإعدادات
            if (TabControl1.SelectedIndex == 4 &&
                (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
                e.Key == Key.R)
            {
                try
                {
                    if (conn.State != ConnectionState.Open) conn.Open();
                    new SqlCommand(
                        "UPDATE SalesmanSetting SET is_Active=0 WHERE is_Active=1",
                        conn).ExecuteNonQuery();
                    DXMessageBox.Show("تم الحفظ.", "تم",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch { /* تجاهل */ }
            }
        }

        #endregion
    }
}