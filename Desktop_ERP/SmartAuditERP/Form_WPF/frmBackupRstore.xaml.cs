using DevExpress.Xpf.Core.Native;
using Microsoft.Win32;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmBackupRstore : Window
    {
        #region Fields

        private SqlDataReader dread;
        private SqlConnection conn;
        private SqlConnection conn1;
        private SqlCommand cmd;
        public string Path;
        private DispatcherTimer Timer1;
        private DispatcherTimer Timer2;
        private Microsoft.Win32.SaveFileDialog saveFileDialog1;
        private Microsoft.Win32.OpenFileDialog openFileDialog1;
        //منع الأحداث من العمل قبل اكتمال تحميل النافذة
        private bool _isLoaded;

        Home Home = new Home();

        #endregion

        #region Constructor

        public frmBackupRstore()
        {
            InitializeComponent();
            _isLoaded = false;
            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            cmd = new SqlCommand();

            saveFileDialog1 = new Microsoft.Win32.SaveFileDialog { DefaultExt = "*.BAK" };
            openFileDialog1 = new Microsoft.Win32.OpenFileDialog { DefaultExt = "*.BAK" };

            Timer1 = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            Timer1.Tick += Timer1_Tick;

            Timer2 = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            Timer2.Tick += Timer2_Tick;

            Loaded += frmBackupRstore_Load;
        }

        #endregion

        #region Event Handlers - Window Load

        private void frmBackupRstore_Load(object sender, RoutedEventArgs e)
        {
            DtbTest.SelectedDate = DateTime.Now;
            chkActiveAutBackUp.IsChecked = false;
            _isLoaded = true;
            try
            {
                using (var adapter = new SqlDataAdapter("SELECT * FROM BackupSetting WHERE id=1", conn))
                {
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count == 1)
                    {
                        cmbBackupPeriods.SelectedIndex = Convert.ToInt32(dataTable.Rows[0]["type"]);
                        bkuppath.Text = dataTable.Rows[0]["path"].ToString() + "\\";

                        if (Convert.ToBoolean(dataTable.Rows[0]["Activated"]))
                        {
                            chkActiveAutBackUp.IsChecked = true;
                        }

                        if (Convert.ToDouble(dataTable.Rows[0]["options"]) == 2.0 &&
                            cmbBackupPeriods.SelectedIndex == 1)
                        {
                            rbOptional.IsChecked = true;
                        }
                    }
                }

                if (conn1.State != ConnectionState.Open)
                    conn1.Open();

                int maxId = Convert.ToInt32(new SqlCommand(
                    "SELECT MAX(id) FROM BackupHistory", conn1).ExecuteScalar());

                if (maxId > 0)
                {
                    using (var adapter = new SqlDataAdapter(
                        $"SELECT Time FROM BackupHistory WHERE id={maxId}", conn))
                    {
                        var dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        txtlastBackup.Text += dataTable.Rows[0][0].ToString();
                    }
                }

                TimBackup.SelectedDate = Properties.Settings.Default.TimBakup;
                MainForm();
            }
            catch { }
        }

        #endregion

        #region Event Handlers - Backup Tab

        private void backup_button_Click(object sender, RoutedEventArgs e)
        {
            var frmAttention = new frmAttentionMsg
            {
                lblmsg = { Text = MainClass.Conn_type == 2
                    ? "سيتم أخذ نسخة احتياطية من قاعدة البيانات في المسار المحدد في السيرفر"
                    : "سيتم أخذ نسخة احتياطية من قاعدة البيانات ,يرجى إختيار مسار القرص" }
            };
            frmAttention.btnInsure.IsEnabled = true;
            frmAttention.ShowDialog();

            if (!frmAttention.IsSure)
                return;

            string dateString = DtbTest.SelectedDate?.ToString("dd-MM-yyyy") ?? DateTime.Now.ToString("dd-MM-yyyy");
            string backupPath;

            if (MainClass.Conn_type == 2)
            {
                using (var adapter = new SqlDataAdapter(
                    "SELECT * FROM BackupSetting WHERE id=1 AND Activated=1", conn))
                {
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (dataTable.Rows.Count <= 0)
                    {
                        MessageBox.Show("⚠️ يجب تحديد مسار النسخة الإحتياطية من السيرفر");
                        return;
                    }

                    backupPath = dataTable.Rows[0]["path"].ToString();
                }
            }
            else
            {
                saveFileDialog1.Filter = "BackUp Files (*.Back) | *.back";
                string timeString = $"{DateTime.Now.Hour:00}_{DateTime.Now.Minute:00}";
                saveFileDialog1.FileName = $"BackUp_{MainClass.Database}_{dateString}_{timeString}.back";

                if (saveFileDialog1.ShowDialog() != true)
                    return;

                backupPath = saveFileDialog1.FileName;
            }

            Home.backUpDb(backupPath, 0);

            if (Home.IsbackedUp)
            {
                UploadBackup(backupPath);
                MessageBox.Show("✅ تم اخذ النسخة الاحتياطية بنجاح", "تاكيد",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("❌ لم تتم العملية", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Event Handlers - Restore Tab

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                openFileDialog1.Filter = "BackUp Files (*.BACK)|*.BACK";
                if (openFileDialog1.ShowDialog() == true)
                {
                    txtRestore.Text = openFileDialog1.FileName;
                }
            }
            catch { }
        }

        private void restore_button_Click(object sender, RoutedEventArgs e)
        {
            RestoreDb();
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Event Handlers - Auto Backup Tab

        private void btnSelectPath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                saveFileDialog1.Filter = "BackUp Files (*.BAK)|*.BAK";
                string dateString = DtbTest.SelectedDate?.ToString("dd-MM-yyyy") ?? DateTime.Now.ToString("dd-MM-yyyy");
                string timeString = $"{DateTime.Now.Hour:00}_{DateTime.Now.Minute:00}";
                saveFileDialog1.FileName = $"BackUp_{MainClass.Database}_{dateString}_{timeString}";

                if (saveFileDialog1.ShowDialog() == true)
                {
                    bkuppath.Text = saveFileDialog1.FileName;
                }
            }
            catch { }
        }

        private void btnSure_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainClass.EmpNo > 1)
                {
                    MessageBox.Show("⚠️ نأسف ليس لديك الصلاحية لتغيير إعدادات النسخ الإحتياطي");
                    return;
                }

                if (cmbBackupPeriods.SelectedIndex < 1)
                {
                    MessageBox.Show("⚠️ يجب تحديد الفترة لنسخة الإحتياطية التلقائية");
                    cmbBackupPeriods.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(bkuppath.Text))
                {
                    MessageBox.Show("⚠️ يجب تحديد المسار الإفتراضي النسخة الإحتياطية");
                    bkuppath.Focus();
                    return;
                }

                int activated = chkActiveAutBackUp.IsChecked == true ? 1 : 0;
                int options = (cmbBackupPeriods.SelectedIndex == 1 && rbOptional.IsChecked == true) ? 2 : 1;

                var frmAttention = new frmAttentionMsg
                {
                    lblmsg = { Text = "سيتم أخذ نسخة احتياطية من قاعدة البيانات في المسار الإفتراضي و بشكل دوري حسب الفترة المحددة" }
                };
                frmAttention.btnInsure.IsEnabled = true;
                frmAttention.ShowDialog();

                if (!frmAttention.IsSure)
                    return;

                using (var adapter = new SqlDataAdapter("SELECT * FROM BackupSetting WHERE id=1", conn))
                {
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    if (conn1.State != ConnectionState.Open)
                        conn1.Open();

                    if (dataTable.Rows.Count == 0)
                    {
                        new SqlCommand(
                            $@"INSERT INTO BackupSetting(type, Activated, path, options) 
                               VALUES({cmbBackupPeriods.SelectedIndex}, {activated}, 
                                      N'{System.IO.Path.GetDirectoryName(bkuppath.Text)}', {options})",
                            conn1).ExecuteNonQuery();
                    }
                    else
                    {
                        new SqlCommand(
                            $@"UPDATE BackupSetting SET type={cmbBackupPeriods.SelectedIndex}, 
                               Activated={activated}, 
                               path=N'{System.IO.Path.GetDirectoryName(bkuppath.Text)}' 
                               WHERE id=1",
                            conn1).ExecuteNonQuery();
                    }
                }

                Properties.Settings.Default._PathBackup = bkuppath.Text;
                Properties.Settings.Default.TimBakup = TimBackup.SelectedDate ?? DateTime.Now;
                Properties.Settings.Default.Save();

                MessageBox.Show("✅ تم الحفظ بنجاح", "تاكيد",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                MainForm();
                Home.backUpDb(System.IO.Path.GetDirectoryName(bkuppath.Text), cmbBackupPeriods.SelectedIndex);

                if (Home.IsbackedUp)
                {
                    MessageBox.Show("✅ تم اخذ النسخة الاحتياطية بنجاح", "تاكيد",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("❌ لم تتم العملية", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void cmbBackupPeriods_SelectedIndexChanged(
    object sender,
    SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (cmbBackupPeriods == null || pnlOptions1 == null || TimBackup == null) return;

            int selectedIndex = cmbBackupPeriods.SelectedIndex;

            pnlOptions1.Visibility = selectedIndex == 1
                ? Visibility.Visible
                : Visibility.Collapsed;

            TimBackup.Visibility = selectedIndex == 2
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        #endregion

        #region Timer Events

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (ProgressBar1.Value >= 100)
            {
                Timer1.Stop();
                ProgressBar1.Value = 0;
            }
            else
            {
                ProgressBar1.Value += 5;
            }
        }

        private void Timer2_Tick(object sender, EventArgs e)
        {
            if (ProgressBar2.Value >= 100)
            {
                Timer2.Stop();
                ProgressBar2.Value = 0;
            }
            else
            {
                ProgressBar2.Value += 5;
            }
        }

        #endregion

        #region Database Operations

        private void RestoreDb()
        {
            if (string.IsNullOrWhiteSpace(txtRestore.Text))
            {
                MessageBox.Show("⚠️ يجب تحديد المسار النسخة الإحتياطية");
                txtRestore.Focus();
                return;
            }

            if (!File.Exists(txtRestore.Text))
            {
                MessageBox.Show("⚠️ ملف النسخة الاحتياطية غير موجود");
                return;
            }

            var frmAttention = new frmAttentionMsg
            {
                lblmsg = { Text = "هل انت متأكد من إستعادة النسخة الإحتياطية" }
            };
            frmAttention.btnInsure.IsEnabled = true;
            frmAttention.ShowDialog();

            if (!frmAttention.IsSure)
                return;

            string connectionString = MainClass.UseServerAuth
                ? $"server={MainClass.Server}; user id={MainClass.NetUserId}; pwd={MainClass.NetPwd};"
                : $"server={MainClass.Server};trusted_connection=true";

            try
            {
                // 1) قراءة أسماء الملفات المنطقية (Logical Names) من ملف النسخة الاحتياطية
                string logicalDataName = null;
                string logicalLogName = null;

                using (var conCheck = new SqlConnection(connectionString))
                {
                    conCheck.Open();
                    using (var cmdList = new SqlCommand(
                        $"RESTORE FILELISTONLY FROM DISK = N'{txtRestore.Text}'", conCheck))
                    using (var reader = cmdList.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string type = reader["Type"].ToString();
                            string logicalName = reader["LogicalName"].ToString();

                            if (type == "D" && logicalDataName == null)
                                logicalDataName = logicalName;
                            else if (type == "L" && logicalLogName == null)
                                logicalLogName = logicalName;
                        }
                    }
                }

                if (string.IsNullOrEmpty(logicalDataName) || string.IsNullOrEmpty(logicalLogName))
                {
                    MessageBox.Show("❌ لم يتم العثور على معلومات الملفات في النسخة الاحتياطية");
                    return;
                }

                // 2) الحصول على المسار الافتراضي لملفات SQL Server على هذا الجهاز
                string defaultDataPath = null;
                string defaultLogPath = null;

                using (var conPath = new SqlConnection(connectionString))
                {
                    conPath.Open();
                    using (var cmdPath = new SqlCommand(@"
                SELECT 
                    CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS NVARCHAR(500)) AS DataPath,
                    CAST(SERVERPROPERTY('InstanceDefaultLogPath')  AS NVARCHAR(500)) AS LogPath", conPath))
                    using (var reader = cmdPath.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            defaultDataPath = reader["DataPath"] as string;
                            defaultLogPath = reader["LogPath"] as string;
                        }
                    }
                }

                // في حال لم يرجع SQL Server المسار الافتراضي، نستخدم مسار قاعدة البيانات الحالية
                if (string.IsNullOrEmpty(defaultDataPath) || string.IsNullOrEmpty(defaultLogPath))
                {
                    using (var conPath2 = new SqlConnection(connectionString))
                    {
                        conPath2.Open();
                        using (var cmdPath2 = new SqlCommand($@"
                    SELECT physical_name 
                    FROM sys.master_files 
                    WHERE database_id = DB_ID(N'{MainClass.Database}')", conPath2))
                        using (var reader = cmdPath2.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string physical = reader["physical_name"].ToString();
                                string dir = System.IO.Path.GetDirectoryName(physical);
                                if (physical.EndsWith(".mdf", StringComparison.OrdinalIgnoreCase))
                                    defaultDataPath = dir + "\\";
                                else if (physical.EndsWith(".ldf", StringComparison.OrdinalIgnoreCase))
                                    defaultLogPath = dir + "\\";
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(defaultDataPath)) defaultDataPath = @"C:\Program Files\Microsoft SQL Server\MSSQL11.SQLEXPRESS\MSSQL\DATA\";
                if (string.IsNullOrEmpty(defaultLogPath)) defaultLogPath = defaultDataPath;

                string newDataFile = System.IO.Path.Combine(defaultDataPath, MainClass.Database + ".mdf");
                string newLogFile = System.IO.Path.Combine(defaultLogPath, MainClass.Database + "_log.ldf");

                // 3) إغلاق جميع الاتصالات الأخرى
                CloseAllOtherConnections(connectionString, MainClass.Database);

                // 4) تنفيذ الاستعادة مع MOVE
                string restoreSql = $@"
            RESTORE DATABASE [{MainClass.Database}] 
            FROM DISK = N'{txtRestore.Text}' 
            WITH REPLACE,
                 MOVE N'{logicalDataName}' TO N'{newDataFile}',
                 MOVE N'{logicalLogName}'  TO N'{newLogFile}',
                 STATS = 10";

                using (var connection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(restoreSql, connection))
                {
                    command.CommandTimeout = 0; // بدون timeout
                    connection.Open();

                    Timer2.Start();
                    ProgressBar2.Visibility = Visibility.Visible;

                    command.ExecuteNonQuery();

                    MessageBox.Show("✅ تم استرجاع النسخة الاحتياطية بنجاح", "تاكيد",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ لم تتم العملية\n{ex.Message}", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                try
                {
                    using (var conFix = new SqlConnection(connectionString))
                    {
                        conFix.Open();
                        new SqlCommand(
                            $"IF DB_ID(N'{MainClass.Database}') IS NOT NULL ALTER DATABASE [{MainClass.Database}] SET MULTI_USER",
                            conFix).ExecuteNonQuery();
                    }
                }
                catch { }

                Timer2.Stop();
                ProgressBar2.Visibility = Visibility.Collapsed;
            }
        }

        private void RestoreDb1()
        {
            if (string.IsNullOrWhiteSpace(txtRestore.Text))
            {
                MessageBox.Show("⚠️ يجب تحديد المسار النسخة الإحتياطية");
                txtRestore.Focus();
                return;
            }

            var frmAttention = new frmAttentionMsg
            {
                lblmsg = { Text = "هل انت متأكد من إستعادة النسخة الإحتياطية" }
            };
            frmAttention.btnInsure.IsEnabled = true;
            frmAttention.ShowDialog();

            if (!frmAttention.IsSure)
                return;

            string Dbname = "";
            int Id = 1;
            Common.GetValidDBname(ref Dbname, ref Id);

            if (string.IsNullOrEmpty(Dbname))
                return;

            string connectionString = MainClass.UseServerAuth
                ? $"server={MainClass.Server}; user id={MainClass.NetUserId}; pwd={MainClass.NetPwd};"
                : $"server={MainClass.Server};trusted_connection=true";

            CloseAllOtherConnections(connectionString, MainClass.Database);

            try
            {
                using (var connection = new SqlConnection(connectionString))
                using (var command = new SqlCommand(
                    $"RESTORE DATABASE {MainClass.Database} FROM DISK = N'{txtRestore.Text}' WITH REPLACE",
                    connection))
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    Timer2.Start();
                    ProgressBar2.Visibility = Visibility.Visible;

                    command.ExecuteNonQuery();

                    MessageBox.Show("✅ تم استرجاع النسخة الاحتياطية بنجاح", "تاكيد",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ لم تتم العملية\n{ex.Message}", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                new SqlCommand($"ALTER DATABASE {MainClass.Database} SET Multi_User", conn).ExecuteNonQuery();
            }
        }

        private void CloseAllOtherConnections(string conStr, string databaseName)
        {
            using (var connection = new SqlConnection(conStr))
            {
                SqlConnection.ClearAllPools();
                connection.Open();
                new SqlCommand(
                    $"ALTER DATABASE {MainClass.Database} SET Single_User WITH Rollback Immediate",
                    connection).ExecuteNonQuery();
                connection.Close();
            }
        }

        public void query(string que)
        {
            try
            {
                if (conn1.State != ConnectionState.Open)
                    conn1.Open();

                cmd = new SqlCommand(que, conn1);
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        public void blank()
        {
            try
            {
                if (openFileDialog1.ShowDialog() != true)
                    return;

                Timer1.Start();
                ProgressBar1.Visibility = Visibility.Visible;

                query($"RESTORE DATABASE {MainClass.Database} FROM disk=N'{openFileDialog1.FileName}'");

                MessageBox.Show("✅ تم استرجاع النسخة الاحتياطية بنجاح", "تاكيد",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ خطأ: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helper Methods

        public void MainForm()
        {
            DateTime timBakup = Properties.Settings.Default.TimBakup;
            TimeSpan targetTime = new TimeSpan(timBakup.Hour, timBakup.Minute, timBakup.Second);
            TimeSpan currentTime = DateTime.Now.TimeOfDay;

            TimeSpan timeUntilTarget = currentTime < targetTime
                ? targetTime - currentTime
                : targetTime + TimeSpan.FromDays(1) - currentTime;

            System.Timers.Timer timer = new System.Timers.Timer(timeUntilTarget.TotalMilliseconds);
            timer.Elapsed += TimerElapsed;
            timer.Start();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                Path = Properties.Settings.Default._PathBackup;
                Home.backUpDb(System.IO.Path.GetDirectoryName(Path), 2);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("✅ تم اخذ النسخة الاحتياطية بنجاح", "تاكيد",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                });

                ((System.Timers.Timer)sender).Interval = TimeSpan.FromDays(1).TotalMilliseconds;
                ((System.Timers.Timer)sender).Start();
            }
            catch { }
        }

        public void UploadBackup(string localFile)
        {
            if (!File.Exists(localFile))
                return;

            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(
                    $"ftp://173.212.208.114/Client_{MainClass.Database}/{System.IO.Path.GetFileName(localFile)}");

                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential("backup_user", "PASSWORD");
                request.UseBinary = true;
                request.UsePassive = true;
                request.KeepAlive = false;

                byte[] fileContents = File.ReadAllBytes(localFile);
                request.ContentLength = fileContents.Length;

                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }
            }
            catch { }
        }

        #endregion
    }
}