using DevExpress.Charts.Model;
using drHamiedNotify;
using Newtonsoft.Json;
using SmartAuditERP.Form_WPF;
using SmartAuditERP.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Valley;

namespace SmartAuditERP
{
    public partial class Home : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        public SqlConnection conn;
        public SqlConnection conn1;

        private bool IsAppTrial;
        public bool IsCashierClosed;
        public bool IsbackedUp;
        private string Currentlang;
        private bool IsLogin;
        private string LicenseFeatures;

        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(Environment.MachineName);

        public nService ns;
        public RSAEncryptDecrypt RSAkeys;
        public bool IsClosed;

        private int backUptype;
        public string BackupPath;
        private DateTime previousTime;
        private DateTime currentTime;
        private int curTick;
        private int iMachineNumber;
        private bool CloseResult;

        private System.Timers.Timer MoveWaitTimer;
        private DispatcherTimer Timer1;
        private DispatcherTimer tmrAutoBackup;
        private DispatcherTimer Timer3;
        private DispatcherTimer TimerCheckInternet;
        private DispatcherTimer tmrsallaSync;
        private DispatcherTimer colorChangeTimer;

        private BackgroundWorker BackgroundWorker1;

        private System.Windows.Forms.NotifyIcon notifysalla;
        private System.Windows.Forms.NotifyIcon NotifyIcon1;

        private SolidColorBrush originalColor;

        // ══════════════════════════════════════════
        // تعريف جميع الواجهات الفرعية (static instances)
        // ══════════════════════════════════════════
        private static frmAccountsTree _frmAccountsTree;
        public static frmAccountsTree frmAccountsTree => _frmAccountsTree ?? (_frmAccountsTree = new frmAccountsTree());

        private static frmTreasury _frmTreasury;
        public static frmTreasury frmTreasury => _frmTreasury ?? (_frmTreasury = new frmTreasury());

        private static frmBanks _frmBanks;
        public static frmBanks frmBanks => _frmBanks ?? (_frmBanks = new frmBanks());

        private static frmTaxGroups _frmTaxGroups;
        public static frmTaxGroups frmTaxGroups => _frmTaxGroups ?? (_frmTaxGroups = new frmTaxGroups());

        private static frmRptEntries _frmRptEntries;
        public static frmRptEntries frmRptEntries => _frmRptEntries ?? (_frmRptEntries = new frmRptEntries());

        private static FrmIntialRestraiction _FrmIntialRestraiction;
        public static FrmIntialRestraiction FrmIntialRestraiction => _FrmIntialRestraiction ?? (_FrmIntialRestraiction = new FrmIntialRestraiction());

        private static frmSummaryD _frmSummaryD;
        public static frmSummaryD frmSummaryD => _frmSummaryD ?? (_frmSummaryD = new frmSummaryD());

        private static frmRptKhzna _frmRptKhzna;
        public static frmRptKhzna frmRptKhzna => _frmRptKhzna ?? (_frmRptKhzna = new frmRptKhzna());

        private static frmRptDailyProcess _frmRptDailyProcess;
        public static frmRptDailyProcess frmRptDailyProcess => _frmRptDailyProcess ?? (_frmRptDailyProcess = new frmRptDailyProcess());

        private static frmSafes _frmSafes;
        public static frmSafes frmSafes => _frmSafes ?? (_frmSafes = new frmSafes());

        private static frmItemCategory _frmItemCategory;
        public static frmItemCategory frmItemCategory => _frmItemCategory ?? (_frmItemCategory = new frmItemCategory());

        private static frmUnits _frmUnits;
        public static frmUnits frmUnits => _frmUnits ?? (_frmUnits = new frmUnits());

        private static frmItems _frmItems;
        public static frmItems frmItems => _frmItems ?? (_frmItems = new frmItems());

        private static frmSafeGrd _frmSafeGrd;
        public static frmSafeGrd frmSafeGrd => _frmSafeGrd ?? (_frmSafeGrd = new frmSafeGrd());

        private static frmSafeGrdToDate _frmSafeGrdToDate;
        public static frmSafeGrdToDate frmSafeGrdToDate => _frmSafeGrdToDate ?? (_frmSafeGrdToDate = new frmSafeGrdToDate());

        private static frmItemsExpire _frmItemsExpire;
        public static frmItemsExpire frmItemsExpire => _frmItemsExpire ?? (_frmItemsExpire = new frmItemsExpire());

        private static frmDecisionHelp _frmDecisionHelp;
        public static frmDecisionHelp frmDecisionHelp => _frmDecisionHelp ?? (_frmDecisionHelp = new frmDecisionHelp());

        private static frmSummaryQ _frmSummaryQ;
        public static frmSummaryQ frmSummaryQ => _frmSummaryQ ?? (_frmSummaryQ = new frmSummaryQ());

        private static frmInvSumByClient _frmInvSumByClient;
        public static frmInvSumByClient frmInvSumByClient => _frmInvSumByClient ?? (_frmInvSumByClient = new frmInvSumByClient());

        private static frmInvSumByType _frmInvSumByType;
        public static frmInvSumByType frmInvSumByType => _frmInvSumByType ?? (_frmInvSumByType = new frmInvSumByType());

        private static frmRptInvAnalysis _frmRptInvAnalysis;
        public static frmRptInvAnalysis frmRptInvAnalysis => _frmRptInvAnalysis ?? (_frmRptInvAnalysis = new frmRptInvAnalysis());

        private static frmNkdTotSalePurch _frmNkdTotSalePurch;
        public static frmNkdTotSalePurch frmNkdTotSalePurch => _frmNkdTotSalePurch ?? (_frmNkdTotSalePurch = new frmNkdTotSalePurch());

        private static frmNetsales _frmNetsales;
        public static frmNetsales frmNetsales => _frmNetsales ?? (_frmNetsales = new frmNetsales());

        private static frmGroupM _frmGroupM;
        public static frmGroupM frmGroupM => _frmGroupM ?? (_frmGroupM = new frmGroupM());

        private static frmItemM _frmItemM;
        public static frmItemM frmItemM => _frmItemM ?? (_frmItemM = new frmItemM());

        private static frmQueueM _frmQueueM;
        public static frmQueueM frmQueueM => _frmQueueM ?? (_frmQueueM = new frmQueueM());

        private static frmAttendM _frmAttendM;
        public static frmAttendM frmAttendM => _frmAttendM ?? (_frmAttendM = new frmAttendM());

        private static frmViolationM _frmViolationM;
        public static frmViolationM frmViolationM => _frmViolationM ?? (_frmViolationM = new frmViolationM());

        private static frmBookingM _frmBookingM;
        public static frmBookingM frmBookingM => _frmBookingM ?? (_frmBookingM = new frmBookingM());

        private static frmHoldM _frmHoldM;
        public static frmHoldM frmHoldM => _frmHoldM ?? (_frmHoldM = new frmHoldM());

        private static frmFPSSetting _frmFPSSetting;
        public static frmFPSSetting frmFPSSetting => _frmFPSSetting ?? (_frmFPSSetting = new frmFPSSetting());

        private static frmManagement _frmManagement;
        public static frmManagement frmManagement => _frmManagement ?? (_frmManagement = new frmManagement());

        private static frmEmployees _frmEmployees;
        public static frmEmployees frmEmployees => _frmEmployees ?? (_frmEmployees = new frmEmployees());

        private static frmDepartments _frmDepartments;
        public static frmDepartments frmDepartments => _frmDepartments ?? (_frmDepartments = new frmDepartments());

        private static frmRptSalary _frmRptSalary;
        public static frmRptSalary frmRptSalary => _frmRptSalary ?? (_frmRptSalary = new frmRptSalary());

        private static frmEmpSalaryAddSub _frmEmpSalaryAddSub;
        public static frmEmpSalaryAddSub frmEmpSalaryAddSub => _frmEmpSalaryAddSub ?? (_frmEmpSalaryAddSub = new frmEmpSalaryAddSub());

        private static frmBuyApp _frmBuyApp;
        public static frmBuyApp frmBuyApp => _frmBuyApp ?? (_frmBuyApp = new frmBuyApp());

        private static frmSerial _frmSerial;
        public static frmSerial frmSerial => _frmSerial ?? (_frmSerial = new frmSerial());

        private static frmAbout _frmAbout;
        public static frmAbout frmAbout => _frmAbout ?? (_frmAbout = new frmAbout());

        private static frmBackupRstore _frmBackupRstore;
        public static frmBackupRstore frmBackupRstore => _frmBackupRstore ?? (_frmBackupRstore = new frmBackupRstore());

        private static frmInvRptType _frmInvRptType;
        public static frmInvRptType frmInvRptType => _frmInvRptType ?? (_frmInvRptType = new frmInvRptType());

        private static dfh _dfh;
        public static dfh dfh => _dfh ?? (_dfh = new dfh());

        private static frmAddUsers _frmAddUsers;
        public static frmAddUsers frmAddUsers => _frmAddUsers ?? (_frmAddUsers = new frmAddUsers());

        private static frmUsersPermissions _frmUsersPermissions;
        public static frmUsersPermissions frmUsersPermissions => _frmUsersPermissions ?? (_frmUsersPermissions = new frmUsersPermissions());

        private static frmChangePwd _frmChangePwd;
        public static frmChangePwd frmChangePwd => _frmChangePwd ?? (_frmChangePwd = new frmChangePwd());

        private static frmSettings _frmSettings;
        public static frmSettings frmSettings => _frmSettings ?? (_frmSettings = new frmSettings());

        private static frmFonudation _frmFonudation;
        public static frmFonudation frmFonudation => _frmFonudation ?? (_frmFonudation = new frmFonudation());

        private static frmBranches _frmBranches;
        public static frmBranches frmBranches => _frmBranches ?? (_frmBranches = new frmBranches());

        private static frmCostCenter _frmCostCenter;
        public static frmCostCenter frmCostCenter => _frmCostCenter ?? (_frmCostCenter = new frmCostCenter());

        private static frmUserLogin _frmUserLogin;
        public static frmUserLogin frmUserLogin => _frmUserLogin ?? (_frmUserLogin = new frmUserLogin());

        private static frmSecurityPnl _frmSecurityPnl;
        public static frmSecurityPnl frmSecurityPnl => _frmSecurityPnl ?? (_frmSecurityPnl = new frmSecurityPnl());

        private static frmLogOut _frmLogOut;
        public static frmLogOut frmLogOut => _frmLogOut ?? (_frmLogOut = new frmLogOut());

        private static frmSalaryPay _frmSalaryPay;
        public static frmSalaryPay frmSalaryPay => _frmSalaryPay ?? (_frmSalaryPay = new frmSalaryPay());

        private static ClosShiftAndroid _ClosShiftAndroid;
        public static ClosShiftAndroid ClosShiftAndroid => _ClosShiftAndroid ?? (_ClosShiftAndroid = new ClosShiftAndroid());

        private static frmRptInvSalesDetailsPosAndroid _frmRptInvSalesDetailsPosAndroid;
        public static frmRptInvSalesDetailsPosAndroid frmRptInvSalesDetailsPosAndroid => _frmRptInvSalesDetailsPosAndroid ?? (_frmRptInvSalesDetailsPosAndroid = new frmRptInvSalesDetailsPosAndroid());

        private static FrmReseved _FrmReseved;
        public static FrmReseved FrmReseved => _FrmReseved ?? (_FrmReseved = new FrmReseved());

        private static frmRptReseved _frmRptReseved;
        public static frmRptReseved frmRptReseved => _frmRptReseved ?? (_frmRptReseved = new frmRptReseved());

        private static frmEmpAccountGet _frmEmpAccountGet;
        public static frmEmpAccountGet frmEmpAccountGet => _frmEmpAccountGet ?? (_frmEmpAccountGet = new frmEmpAccountGet());

        private static FrmSyncMQtt _FrmSyncMQtt;
        public static FrmSyncMQtt FrmSyncMQtt => _FrmSyncMQtt ?? (_FrmSyncMQtt = new FrmSyncMQtt());

        private static FormCreateTicket _FormCreateTicket;
        public static FormCreateTicket FormCreateTicket => _FormCreateTicket ?? (_FormCreateTicket = new FormCreateTicket());

     //   private static FrmWebhooksetting _FrmWebhooksetting;
     //   public static FrmWebhooksetting FrmWebhooksetting => _FrmWebhooksetting ?? (_FrmWebhooksetting = new FrmWebhooksetting());

     //   private static FrmSallaProducts _FrmSallaProducts;
    //    public static FrmSallaProducts FrmSallaProducts => _FrmSallaProducts ?? (_FrmSallaProducts = new FrmSallaProducts());

    //    private static FrmOrderSalla _FrmOrderSalla;
    //    public static FrmOrderSalla FrmOrderSalla => _FrmOrderSalla ?? (_FrmOrderSalla = new FrmOrderSalla());

        private static FrmAccountingPeriods _FrmAccountingPeriods;
        public static FrmAccountingPeriods FrmAccountingPeriods => _FrmAccountingPeriods ?? (_FrmAccountingPeriods = new FrmAccountingPeriods());

   //     private static FrmRestorData _FrmRestorData;
   //     public static FrmRestorData FrmRestorData => _FrmRestorData ?? (_FrmRestorData = new FrmRestorData());

        private static frmDevicesAndroid _frmDevicesAndroid;
        public static frmDevicesAndroid frmDevicesAndroid => _frmDevicesAndroid ?? (_frmDevicesAndroid = new frmDevicesAndroid());

        private static frmInvSalContactDetails _frmInvSalContactDetails;
        public static frmInvSalContactDetails frmInvSalContactDetails => _frmInvSalContactDetails ?? (_frmInvSalContactDetails = new frmInvSalContactDetails());

        #endregion

        #region Properties

        public string _clientid { get; set; }
        public int _nodeid { get; set; }
        public string _frequency { get; set; }
        public string _sale { get; set; }
        public string _receipt { get; set; }
        public string _income { get; set; }
        public string _finance { get; set; }
        public string _passcode { get; set; }
        public string _servername { get; set; }
        public string _usrname { get; set; }
        public string _pwd { get; set; }
        public string _DataBase { get; set; }
        public string _publickey { get; set; }
        public string _privatekey { get; set; }
        public string _Itemid { get; set; }
        public bool _is_active { get; set; }

        #endregion

        #region Constructor

        public Home()
        {
            this.conn = MainClass.ConnObj();
            this.conn1 = MainClass.ConnObj();
            this.IsAppTrial = true;
            this.IsCashierClosed = true;
            this.IsbackedUp = false;
            this.Currentlang = "ar";
            this.IsLogin = false;
            this.LicenseFeatures = "000000";
            this.ns = new nService();
            this.RSAkeys = new RSAEncryptDecrypt();
            this._clientid = "";
            this._nodeid = 1;
            this._frequency = "";
            this._sale = "";
            this._receipt = "";
            this._income = "";
            this._finance = "";
            this._passcode = "";
            this._servername = "";
            this._usrname = "";
            this._pwd = "";
            this._DataBase = "";
            this._publickey = "";
            this._privatekey = "";
            this._Itemid = "";
            this._is_active = false;
            this.notifysalla = new System.Windows.Forms.NotifyIcon();
            this.IsClosed = false;
            this.backUptype = 1;
            this.BackupPath = "";
            this.currentTime = DateTime.Now;
            this.CloseResult = false;
            this.MoveWaitTimer = new System.Timers.Timer();
            this.NotifyIcon1 = new System.Windows.Forms.NotifyIcon();

            Thread.CurrentThread.CurrentUICulture = new CultureInfo("ar");
            if (MainClass.Language != "ar")
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
            }

            InitializeComponent();
            InitializeTimers();
            InitializeBackgroundWorker();
        }

        #endregion

        #region Initialization

        private void InitializeTimers()
        {
            Timer1 = new DispatcherTimer();
            Timer1.Interval = TimeSpan.FromSeconds(1);
            Timer1.Tick += Timer1_Tick;
            Timer1.Start();

            tmrAutoBackup = new DispatcherTimer();
            tmrAutoBackup.Interval = TimeSpan.FromMilliseconds(300);
            tmrAutoBackup.Tick += tmrAutoBackup_Tick;

            Timer3 = new DispatcherTimer();

            TimerCheckInternet = new DispatcherTimer();
            TimerCheckInternet.Interval = TimeSpan.FromMilliseconds(10000);
            TimerCheckInternet.Tick += TimerCheckInternet_Tick;

            tmrsallaSync = new DispatcherTimer();
            tmrsallaSync.Interval = TimeSpan.FromMilliseconds(60000);
            tmrsallaSync.Tick += tmrAutoSync_Tick;

            colorChangeTimer = new DispatcherTimer();
            colorChangeTimer.Tick += colorChangeTimer_Tick;

            MoveWaitTimer.Elapsed += (s, e) =>
            {
                Dispatcher.Invoke(() => MoveWaitTimer_Tick(s, e));
            };
        }

        private void InitializeBackgroundWorker()
        {
            BackgroundWorker1 = new BackgroundWorker();
            BackgroundWorker1.WorkerReportsProgress = true;
            BackgroundWorker1.WorkerSupportsCancellation = true;
            BackgroundWorker1.DoWork += BackgroundWorker1_DoWork;
        }

        #endregion

        #region Window Events

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                try
                {
                    this.checkConn();
                    Common.SalesmanSetting();
                }
                catch { }

                if (!this.IsLogin)
                {
                    this.ISSecureLogin();
                }

                this.conn = MainClass.ConnObj();
                this.conn1 = MainClass.ConnObj();

                if (!string.IsNullOrEmpty(MainClass.Server))
                {
                    string connectionString = "Data Source=" + MainClass.Server + ";Initial Catalog=" + MainClass.Database + ";Integrated Security=True";

                    if ((MainClass.Conn_type == 1) && MainClass.UseServerAuth)
                    {
                        connectionString = "server=" + MainClass.Server + ";database=" + MainClass.Database + ";user id=" + MainClass.NetUserId + ";pwd=" + MainClass.NetPwd;
                    }
                    if (MainClass.Conn_type == 2)
                    {
                        connectionString = "server=" + MainClass.Server + ";database=" + MainClass.Database + ";user id=" + MainClass.Database + ";pwd=" + MainClass.NetPwd;
                    }
                    Common.SetUpLogDbConnection(connectionString, "log4net.config");
                }

                if (MainClass.Language != this.Currentlang)
                {
                    this.ChangeLanguage(MainClass.Language);
                }

                if (MainClass.Language == "ar")
                    this.Title = " برنامج المدقق المحاسبي للمحاسبة والمستودعات";
                else
                    this.Title = "Auditor program for accounting and warehouses";

                this.conn.Open();
                await this.CheckLicense();

                Dispatcher.Invoke(() =>
                {
                    this.ApplyFeatures(MainClass.LicenseFeature.Trim());
                });
                UpdateHomeStatusBar();
                System.Diagnostics.Debug.WriteLine("User: " + txtLoggedUser1.Text);
                System.Diagnostics.Debug.WriteLine("Branch: " + lblBranch1.Text);
                System.Diagnostics.Debug.WriteLine("Database: " + lbldbName.Text);
                System.Diagnostics.Debug.WriteLine("Version: " + lblVersion.Text);
                // ✅ التحقق من التحديثات في Background Thread
                Task.Run(() => CheckForUpdatesInBackground());
                new Thread(CheckUpdateDB).Start();
                this.ApplyPermissionsOnTabs();

                int type = 1;
                if (!MainClass.IsTrial && this.StripMarine.Visibility == Visibility.Visible)
                    type = 2;

                Common.CheckCashierClose(type);

                new Thread(CheckBackup).Start();
                new Thread(CheckSyncAsync).Start();

                if (Common.GetZatcaActive())
                {
                    this.LblZatca.Visibility = Visibility.Visible;
                    object noZatcaResult = this.shownozatca();
                    if (noZatcaResult != null)
                    {
                        int noZatcaCount = 0;
                        if (int.TryParse(noZatcaResult.ToString(), out noZatcaCount) && noZatcaCount > 0)
                        {
                            MessageBox.Show(
                                "لديك " + noZatcaCount + " فاتورة  لم يتم إرسالها للهيئة ، \r\n \r\nيرجى المزامنة الآن من خلال تقرير مزامنة الفواتير  ",
                                "تنبيه الفوترة الإلكترونية",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    }
                }

                if (this._is_active)
                {
                    this.ConnectMQTList();
                    this.TimerCheckInternet.Start();
                }

                Common.LoadDefualtInfo();
                MainClass.BranchCode = Common.GetBranchCode(MainClass.BranchNo);
                new Thread(Common.GetOnlineVersionInfo).Start();
                this.tmrsallaSync.Start();
                this.notifysalla.Icon = System.Drawing.SystemIcons.Information;
                this.notifysalla.Visible = true;
                this.notifysalla.BalloonTipTitle = "Salla Sync";

               // var frmWebhook = new FrmWebhooksetting();

            }
            catch { }

            frmBackupRstore.MainForm();
        }

        public void UpdateHomeStatusBar()
        {
            if (MainClass.Language == "en-us")
            {
                txtLoggedUser1.Text = "User: " + MainClass.UserName;
                lblBranch1.Text = "Branch: " + frmUserLogin.SelectedBranch;
                lbldbName.Text = "Database: " + frmUserLogin.SelectedDatabase;
                lblVersion.Text = "Version: " + frmUserLogin.VersionText;
            }
            else
            {
                txtLoggedUser1.Text = "المستخدم: " + MainClass.UserName;
                lblBranch1.Text = "الفرع: " + frmUserLogin.SelectedBranch;
                lbldbName.Text = "قاعدة البيانات: " + frmUserLogin.SelectedDatabase;
                lblVersion.Text = "النسخة: " + frmUserLogin.VersionText;

                if (frmUserLogin.ConnectionType == 2)
                {
                    lblDeviceType.Text = "نوع الجهاز: طرفي";
                    lblDeviceType.Visibility = Visibility.Visible;
                }
                else
                {
                    lblDeviceType.Text = "";
                    lblDeviceType.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (!this.IsCashierClosed)
                {
                    MessageBox.Show("لم يتم إغلاق اليومية", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                if (!this.IsClosed)
                {
                    this.IsClosed = true;
                    this.CheckBackup();
                }
                System.Windows.Application.Current.Shutdown();
            }
            catch { }
        }

        private async void Window_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.F5)
                {
                    await ManagerOnline.CheckLicenseStatus();
                }
            }
            catch { }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Layout handled by WPF HorizontalAlignment/VerticalAlignment
        }

        #endregion

        #region Timer Ticks

        private void Timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                lblTime2.Text = DateTime.Now.ToString();
            }
            catch { }
        }

        private void tmrAutoBackup_Tick(object sender, EventArgs e)
        {
            this.currentTime = DateTime.Now;
            double daysDiff = (this.currentTime - this.previousTime).TotalDays;

            if (this.backUptype == 2 && daysDiff >= 1)
            {
                this.tmrAutoBackup.Stop();
                this.backUpDb(this.BackupPath, this.backUptype);
                this.previousTime = this.currentTime;
                this.tmrAutoBackup.Start();
            }
            else if (this.backUptype == 3 && daysDiff >= 7)
            {
                this.tmrAutoBackup.Stop();
                this.backUpDb(this.BackupPath, this.backUptype);
                this.previousTime = this.currentTime;
                this.tmrAutoBackup.Start();
            }
            else if (this.backUptype == 4 && daysDiff >= 30)
            {
                this.tmrAutoBackup.Stop();
                this.backUpDb(this.BackupPath, this.backUptype);
                this.previousTime = this.currentTime;
                this.tmrAutoBackup.Start();
            }
        }

        private void Timer3_Tick(object obj, EventArgs e)
        {
            FingerPrintDevice.loadAttendData();
        }

        private void TimerCheckInternet_Tick(object sender, EventArgs e)
        {
            if (this._is_active)
            {
                this.TimerCheckInternet.Interval = TimeSpan.FromMilliseconds(Convert.ToDouble(MainClass.BranchNo.ToString() + "00000"));
                this.TimerCheckInternet.IsEnabled = true;
                SendData.SendStoredItems();
            }
        }

        private void colorChangeTimer_Tick(object sender, EventArgs e)
        {
            if (originalColor != null)
                this.lblnotify.Foreground = originalColor;
            this.colorChangeTimer.Stop();
        }

        #endregion

        #region Connection Check

        private void checkConn()
        {
            string serverUser = "";
            string serverPass = "";
            try
            {
                string startupPath = AppDomain.CurrentDomain.BaseDirectory;
                string jsonPath = Path.Combine(startupPath, "StartupData.json");
                if (!File.Exists(jsonPath)) return;

                var startupData = JsonConvert.DeserializeObject<StartupData>(File.ReadAllText(jsonPath));
                if (startupData == null) return;

                int connType = 0;
                if (double.TryParse(startupData.ConnType, out double connTypeVal))
                    connType = (int)Math.Round(connTypeVal);

                MainClass.Conn_type = connType;
                MainClass.Server = startupData.Server;
                string dbName = startupData.DatabaseName;
                MainClass.Database = dbName;
                MainClass.UseServerAuth = !string.IsNullOrEmpty(startupData.ServerUserName);

                if (connType == 2)
                {
                    serverUser = startupData.ServerUserName;
                    serverPass = startupData.ServerPassword;
                    MainClass.NetUserId = serverUser;
                    MainClass.NetPwd = serverPass;
                }
                if (connType == 1 && MainClass.UseServerAuth)
                {
                    serverUser = startupData.ServerUserName;
                    serverPass = startupData.ServerPassword;
                    MainClass.NetUserId = serverUser;
                    MainClass.NetPwd = serverPass;
                }

                switch (connType)
                {
                    case 2:
                        this.conn = new SqlConnection("server=" + MainClass.Server + ";database=" + dbName + ";user id=" + serverUser + ";pwd=" + serverPass);
                        break;
                    case 1:
                        if (MainClass.UseServerAuth)
                            this.conn = new SqlConnection("server=" + MainClass.Server + ";database=" + dbName + ";user id=" + serverUser + ";pwd=" + serverPass);
                        else
                            this.conn = new SqlConnection("server=" + MainClass.Server + ";database=" + dbName + ";trusted_connection=true;");
                        break;
                }

                if (this.conn.State != ConnectionState.Open)
                    this.conn.Open();
            }
            catch { }
        }

        #endregion

        #region License

        public async Task CheckLicense()
        {
            var frmLicenseManagment = new frmLicenseManagment();
            string clientName = "";
            string ClientEmail = "";
            frmLicenseManagment.GetLicenseClient(ref clientName, ref ClientEmail);
            await ManagerOnline.StartDailyCheck();
            this.ApplyFeatures(MainClass.LicenseFeature);
        }

        public long MUL(long no, int x)
        {
            return no * x;
        }

        public long GetHasCode(string strObj)
        {
            long result = 0;
            try
            {
                if (string.IsNullOrEmpty(strObj.Trim()))
                    return 0;

                long num = strObj.Length;
                for (int i = 0; i < strObj.Length; i++)
                {
                    num = num * 3 - num + (int)strObj[i];
                }
                result = num;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return result;
        }

        #endregion

        #region MQTT

        public void ConnectMQTList()
        {
            string connstr = MainClass.connstr;
            if (ConnectBroker.CheckConnectionAndBroker())
            {
                ReceivedData.LoadReceived(connstr);
            }
        }

        #endregion

        #region Backup

        public void backUpDb(string path, int type)
        {
            try
            {
                string folderPath = path;
                if (type == 0)
                    folderPath = Path.GetDirectoryName(path);

                if (!Directory.Exists(folderPath))
                {
                    MessageBox.Show("المسار الحالي للنسخ الإحتياطي غير موجود او تم تعديله", "خطأ", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string localBackupDir = "C:\\SQLBackups\\";
                if (!Directory.Exists(localBackupDir))
                    Directory.CreateDirectory(localBackupDir);

                string dateStr = DateTime.Now.ToString("dd-MM-yyyy");
                string timeStr = DateTime.Now.Hour.ToString("00") + "_" + DateTime.Now.Minute.ToString("00");
                string fileName = "BackUp_" + MainClass.Database + "_" + dateStr + "_" + timeStr + ".back";
                string localFilePath = localBackupDir + fileName;
                string destFilePath = folderPath + "\\" + fileName;

                var cmd = new SqlCommand();
                cmd.Connection = this.conn;
                cmd.CommandText = "backup database " + MainClass.Database + " To Disk='" + localFilePath + "'";

                try
                {
                    if (this.conn.State != ConnectionState.Open)
                        this.conn.Open();

                    cmd.ExecuteNonQuery();

                    try
                    {
                        File.Copy(localFilePath, destFilePath, true);
                        File.Delete(localFilePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("تم إنشاء النسخة الاحتياطية محلياً بنجاح، لكن فشل نقلها:\r\n" + ex.Message);
                    }

                    var da = new SqlDataAdapter("select * from BackupSetting where id=1", this.conn);
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count >= 1 && Convert.ToBoolean(dt.Rows[0]["DeleteOld"]))
                    {
                        this.DeleteOldBackups(folderPath, 10);
                    }

                    DateTime nextBackup = DateTime.Now;
                    switch (type)
                    {
                        case 2: nextBackup = this.currentTime.AddHours(24); break;
                        case 3: nextBackup = this.currentTime.AddDays(7); break;
                        case 4: nextBackup = this.currentTime.AddMonths(1); break;
                    }

                    var insertCmd = new SqlCommand("Insert into BackupHistory(type,user_id,time,NextBackupTime) values(@type,@user_id,@time,@NextBackupTime)", this.conn);
                    insertCmd.Parameters.Add("@type", SqlDbType.Int).Value = type;
                    insertCmd.Parameters.Add("@user_id", SqlDbType.Int).Value = MainClass.EmpNo;
                    insertCmd.Parameters.Add("@time", SqlDbType.DateTime).Value = DateTime.Now;
                    insertCmd.Parameters.Add("@NextBackupTime", SqlDbType.DateTime).Value = nextBackup;
                    insertCmd.ExecuteNonQuery();

                    this.IsbackedUp = true;
                }
                catch (Exception ex)
                {
                    this.IsbackedUp = false;
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    this.conn.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DeleteOldBackups(string folderPath, int keepDays)
        {
            try
            {
                string[] files = Directory.GetFiles(folderPath, "BackUp_" + MainClass.Database + "_*.back");
                if (files.Length == 0) return;

                var sortedFiles = files.OrderBy(f => File.GetCreationTime(f)).ToArray();
                DateTime threshold = DateTime.Now.AddDays(-keepDays);

                foreach (string file in sortedFiles)
                {
                    if (File.GetCreationTime(file) < threshold)
                    {
                        try { File.Delete(file); }
                        catch (Exception ex)
                        {
                            Console.WriteLine("لم يتمكن من حذف: " + file + " - " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء حذف النسخ القديمة: " + ex.Message);
            }
        }

        public void CheckBackup()
        {
            try
            {
                if (this.conn.State != ConnectionState.Open) return;

                var da = new SqlDataAdapter("select * from BackupSetting where id=1 and Activated=1", this.conn);
                var dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count == 0) return;

                bool isTypeOne = Convert.ToInt32(dt.Rows[0]["type"]) == 1;

                if (this.IsClosed && isTypeOne)
                {
                    Dispatcher.Invoke(() =>
                    {
                        var frmAttention = new frmAttentionMsg();
                        frmAttention.btnInsure.IsEnabled = true;
                        if (MainClass.Language == "ar")
                            frmAttention.lblmsg.Text = " سيتم عمل نسخة إحتياطية من البيانات قبل الإغلاق ";
                        else
                            frmAttention.lblmsg.Text = "A backup copy of the data will be made before closing ";
                        frmAttention.ShowDialog();

                        bool optionIsTwo = Convert.ToInt32(dt.Rows[0]["options"]) == 2;
                        if (!((!frmAttention.IsSure) && optionIsTwo))
                        {
                            this.backUpDb(dt.Rows[0]["path"].ToString(), Convert.ToInt32(dt.Rows[0]["type"]));
                        }
                    });
                }
                else if (Convert.ToInt32(dt.Rows[0]["type"]) > 1)
                {
                    this.BackupPath = dt.Rows[0]["path"].ToString();
                    this.backUptype = Convert.ToInt32(dt.Rows[0]["type"]);

                    if (this.conn1.State != ConnectionState.Open)
                        this.conn1.Open();

                    var cmdMax = new SqlCommand("select MAX(Time) from BackupHistory where type=" + this.backUptype, this.conn1);
                    object result = cmdMax.ExecuteScalar();
                    if (result == DBNull.Value || result == null)
                        this.previousTime = DateTime.Today;
                    else
                        this.previousTime = Convert.ToDateTime(result);

                    Dispatcher.Invoke(() => { this.tmrAutoBackup.Start(); });
                }
            }
            catch { }
        }

        #endregion

        #region Fingerprint

        private void CheckFPScanner()
        {
            try
            {
                string deviceIP = "";
                string devicePort = "";
                var da = new SqlDataAdapter("Select * from SettingFingurePrint where Branch_Id=" + MainClass.BranchNo, this.conn);
                var dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count <= 0) return;

                if (!string.IsNullOrEmpty(dt.Rows[0]["DeviceIP"].ToString()))
                    deviceIP = dt.Rows[0]["DeviceIP"].ToString();
                if (!string.IsNullOrEmpty(dt.Rows[0]["port"].ToString()))
                    devicePort = dt.Rows[0]["Port"].ToString();

                if (!Convert.ToBoolean(dt.Rows[0]["ConnectAuto"])) return;

                if (string.IsNullOrEmpty(deviceIP) || string.IsNullOrEmpty(devicePort))
                {
                    if (MainClass.Language == "ar")
                        MessageBox.Show("يجب إدخال بيانات الإتصال من الإعدادات", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    else
                        MessageBox.Show("IP and Port cannot be null", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                else if (this.StripMarine.Visibility == Visibility.Visible && !MainClass.IsTrial)
                {
                    FingerPrintDevice.ConnectFPscanner(deviceIP, devicePort);
                    if (FingerPrintDevice.ISconnectedBar)
                    {
                        this.lblConnectBar.Text = "جهاز البصمة متصل";
                        this.lblConnectBar.Foreground = new SolidColorBrush(Colors.Green);
                        this.AttendTimer();
                    }
                    else
                    {
                        if (MainClass.Language == "en")
                            MessageBox.Show("Unable to connect the device", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        else
                            MessageBox.Show(" جهاز البصمة غير متصل", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
            }
            catch { }
        }

        private void AttendTimer()
        {
            this.Timer3.Tick += Timer3_Tick;
            this.Timer3.Interval = TimeSpan.FromMilliseconds(60000);
            this.Timer3.Start();
        }

        #endregion

        #region Login / Logout / Lock

        private bool ISSecureLogin()
        {
            frmUserLogin.ISExit = false;
            if (this._is_active)
                this._is_active = false;

            frmUserLogin.ShowDialog();

            if (frmUserLogin.ISExit)
            {
                frmSecurityPnl.ShowDialog();
                frmUserLogin.ISExit = false;

                if (!frmSecurityPnl.IsExit)
                {
                    this.IsLogin = true;
                    return true;
                }
                this.IsLogin = false;
                this.ISSecureLogin();
            }
            else
            {
                if (MainClass.EmpNo != -1)
                {
                    this.IsLogin = true;
                    return true;
                }
                var processes = Process.GetProcessesByName("Auditor E-Invoicing");
                foreach (var p in processes)
                    p.Kill();
            }
            return false;
        }

        public async void logOut()
        {
            this.Currentlang = MainClass.Language;
            frmLogOut.ShowDialog();

            if (frmLogOut.Action == 0)
            {
                this.@lock();
            }
            else if (frmLogOut.Action == 2)
            {
                int type = 1;
                if (!MainClass.IsTrial && this.StripMarine.Visibility == Visibility.Visible)
                    type = 2;

                Common.CheckCashierClose(type);

                if (this.ISSecureLogin())
                {
                    if (MainClass.Language != this.Currentlang)
                        this.ChangeLanguage(MainClass.Language);

                    await this.CheckLicense();
                    new Thread(CheckUpdateDB).Start();
                    this.ApplyPermissionsOnTabs();
                    Common.LoadDefualtInfo();
                    new Thread(CheckSyncAsync).Start();
                    Common.CheckCashierClose(1);
                    MainClass.BranchCode = Common.GetBranchCode(MainClass.BranchNo);
                }
            }
        }

        public void @lock()
        {
            try
            {
                var frmLock = new frmlock();
                MainClass.DoApplyUserSett(frmLock);
                frmLock.ShowDialog();
            }
            catch { }
        }

        private void btnLogOut_Click(object sender, RoutedEventArgs e)
        {
            this.logOut();
        }

        #endregion

        #region Language

        public void ChangeLanguage(string lang)
        {
            try
            {
                this.Currentlang = lang;
                if (MainClass.Language == "ar")
                {
                    this.txtLoggedUser1.Text = "المستخدم: " + MainClass.UserName;
                    this.lblBranch1.Text = "الفرع:" + MainClass.BranchName;
                    this.lbldbName.Text = "قاعدة البيانات:" + MainClass.Database;
                    this.FlowDirection = System.Windows.FlowDirection.RightToLeft;
                }
                else
                {
                    this.txtLoggedUser1.Text = "User: " + MainClass.UserName;
                    this.lblBranch1.Text = "Branch: " + MainClass.BranchName;
                    this.lbldbName.Text = "Database :" + MainClass.Database;
                    this.lblIsTrial1.Text = "Trial";
                    this.FlowDirection = System.Windows.FlowDirection.LeftToRight;
                }

                try { Common.SalesmanSetting(); }
                catch { }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        private void btnArabic_Click(object sender, RoutedEventArgs e)
        {
            this.Currentlang = MainClass.Language;
            MainClass.Language = "ar";
            this.ChangeLanguage("ar");
        }

        private void btnEnglish_Click(object sender, RoutedEventArgs e)
        {
            this.Currentlang = MainClass.Language;
            MainClass.Language = "en-us";
            this.ChangeLanguage("en-us");
        }

        #endregion

        #region Permissions Helper Methods

        private MenuItem FindMenuItemByName(string name)
        {
            return FindMenuItemByNameRecursive(MainMenuStrip.Items, name);
        }

        private MenuItem FindMenuItemByNameRecursive(ItemCollection items, string name)
        {
            foreach (var item in items)
            {
                if (item is MenuItem mi)
                {
                    if (mi.Name == name) return mi;
                    var found = FindMenuItemByNameRecursive(mi.Items, name);
                    if (found != null) return found;
                }
            }
            return null;
        }

        private void SetAllMenuItemsVisible(Menu menu, bool visible)
        {
            foreach (var item in menu.Items)
            {
                if (item is MenuItem mi)
                {
                    mi.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                    SetChildMenuItemsVisible(mi, visible);
                }
            }
        }

        private void SetChildMenuItemsVisible(MenuItem parent, bool visible)
        {
            foreach (var item in parent.Items)
            {
                if (item is MenuItem mi)
                {
                    mi.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                    mi.IsEnabled = true;
                    SetChildMenuItemsVisible(mi, visible);
                }
            }
        }

        #endregion

        #region ApplyPermissionsOnTabs

        public void ApplyPermissionsOnTabs()
        {
            try
            {
                int userID = MainClass.UserID;
                if (userID == 0)
                {
                    SetAllMenuItemsVisible(MainMenuStrip, true);

                    var da = new SqlDataAdapter("SELECT EgyEInvoice FROM Foundation WHERE EgyEInvoice=1", this.conn1);
                    var dt = new DataTable();
                    da.Fill(dt);
                    this.StripEgyptEInvoice.Visibility = dt.Rows.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    SetAllMenuItemsVisible(MainMenuStrip, false);
                    this.StripSupport.Visibility = Visibility.Visible;
                    SetChildMenuItemsVisible(this.StripSupport, true);

                    this.btnPOS2.Visibility = Visibility.Collapsed;
                    this.StripRptRentInvoices.Visibility = Visibility.Visible;
                    this.stripInvertoryOrderInv.Visibility = Visibility.Visible;
                    this.stripSyncStocks.Visibility = Visibility.Visible;
                    this.btnSyncData.Visibility = Visibility.Visible;
                    this.StripInvProfit.Visibility = Visibility.Visible;
                    this.StripRptInvPurchaseDetails.Visibility = Visibility.Visible;
                    this.StripRptReturnPurchaseDetails.Visibility = Visibility.Visible;
                    this.StripRptReturnSalesDetails.Visibility = Visibility.Visible;
                    this.StripRptInvSalesDetails.Visibility = Visibility.Visible;
                    this.StripLicense.Visibility = Visibility.Visible;
                    this.StripRptSalesmen.Visibility = Visibility.Visible;
                    this.stripAccountsDirectory.Visibility = Visibility.Visible;
                    this.StripZatcaSyncInv.Visibility = Visibility.Visible;
                    this.StripCostCenter.Visibility = Visibility.Visible;
                    this.StripEtaSetting.Visibility = Visibility.Visible;
                    this.closeshiftandroid.Visibility = Visibility.Collapsed;
                    this.StripEgySetting.Visibility = Visibility.Visible;
                    this.StripEgyRepots.Visibility = Visibility.Visible;
                    this.StripEgyUploadedInvoice.Visibility = Visibility.Visible;
                    this.StripPaymentMethodCustomr.Visibility = Visibility.Visible;
                    this.StripSaleByCategry.Visibility = Visibility.Visible;
                    this.StripItemsRptSerialNo.Visibility = Visibility.Visible;
                    this.StripItemsRptSerialNoSummary.Visibility = Visibility.Visible;
                    this.StripExpenses.Visibility = Visibility.Visible;
                    this.StripCategorySaleByDay.Visibility = Visibility.Visible;
                    this.StripPrintBarcode.Visibility = Visibility.Visible;
                    this.StripPriceSetting.Visibility = Visibility.Visible;
                    this.StripItemsSalesDetails.Visibility = Visibility.Visible;
                    this.StripItemsPurchDetails.Visibility = Visibility.Visible;
                    this.StripInvertoryDeliveries.Visibility = Visibility.Visible;
                    this.stripMainbadget1.Visibility = Visibility.Collapsed;

                    if (MainClass.EmpNo == 0)
                        this.stripMaintainInvs.Visibility = Visibility.Visible;

                    this.stripIInvoicesByType.Visibility = Visibility.Visible;
                    this.StripRptNetSalesDetails.Visibility = Visibility.Visible;
                    this.StripItemsProfitDetails.Visibility = Visibility.Visible;
                    this.StripRptNetPurchase.Visibility = Visibility.Visible;
                    this.StripUsersRecords.Visibility = Visibility.Visible;
                    this.stripAccountStatement.Visibility = Visibility.Visible;
                    this.ToolStripSandSalary.Visibility = Visibility.Collapsed;
                    this.StripItemsExpiration.Visibility = Visibility.Visible;
                    this.stripCostCenterCard.Visibility = Visibility.Visible;
                    this.strIpinventoryAdjustment.Visibility = Visibility.Visible;
                    this.StripGraphSale.Visibility = Visibility.Visible;
                    this.StripItemProfit.Visibility = Visibility.Visible;

                    var da2 = new SqlDataAdapter(
                        "select Forms.name as form_name,Forms.id as form_id,Forms.buttonName as btnName, Forms.Parent_id, Forms.nameEn " +
                        "from User_Permissions,Forms where User_Permissions.Form_id=Forms.id and user_id=" + userID, this.conn1);
                    var dt2 = new DataTable();
                    da2.Fill(dt2);

                    if (dt2.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt2.Rows.Count; i++)
                        {
                            string btnName = dt2.Rows[i]["btnName"].ToString();
                            if (string.IsNullOrEmpty(btnName)) continue;
                            int parentId = Convert.ToInt32(dt2.Rows[i]["Parent_id"]);
                            try { ApplyPermissionByParent(parentId, btnName); }
                            catch { }
                        }
                    }

                    this.stripCreateDb.Visibility = Visibility.Visible;
                    this.stripMonitorItem.Visibility = Visibility.Collapsed;
                    this.stripDefaultPrint.Visibility = Visibility.Collapsed;
                    this.StripSafeGrdDate.Visibility = Visibility.Collapsed;
                    this.stripExpire.Visibility = Visibility.Collapsed;
                    this.stripSalesAanalysis.Visibility = Visibility.Collapsed;
                    this.StripRptProducedItems.Visibility = Visibility.Collapsed;
                    this.StripLanguages.Visibility = Visibility.Collapsed;
                    this.stripBenefits.Visibility = Visibility.Collapsed;

                    var daEgy = new SqlDataAdapter("SELECT EgyEInvoice FROM Foundation WHERE EgyEInvoice=1", this.conn1);
                    var dtEgy = new DataTable();
                    daEgy.Fill(dtEgy);
                    this.StripEgyptEInvoice.Visibility = dtEgy.Rows.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                }

                User.LoadUserOperPermission();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ApplyPermissionByParent(int parentId, string btnName)
        {
            var targetItem = FindMenuItemByName(btnName);

            switch (parentId)
            {
                case 21:
                    StripAccount.Visibility = Visibility.Visible;
                    AccountDefinintionMenu.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 22:
                    StripAccount.Visibility = Visibility.Visible;
                    AccountOper.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 23:
                case 24:
                    StripAccount.Visibility = Visibility.Visible;
                    AccountRpt.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 31:
                    StripStores.Visibility = Visibility.Visible;
                    StoreDefinition.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    if (btnName == "stripItem") BtnAddItems.Visibility = Visibility.Visible;
                    break;
                case 32:
                    StripStores.Visibility = Visibility.Visible;
                    StoreOper.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 33:
                    StripStores.Visibility = Visibility.Visible;
                    StoreRpt.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 41:
                    StripPurches.Visibility = Visibility.Visible;
                    PurchOper.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    if (btnName == "stripPurchInv") BtnPurches.Visibility = Visibility.Visible;
                    BtnaddSupplier.Visibility = Visibility.Visible;
                    break;
                case 42:
                    StripPurches.Visibility = Visibility.Visible;
                    PurchSand.Visibility = Visibility.Visible;
                    stripAddSupplier.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 43:
                    StripPurches.Visibility = Visibility.Visible;
                    PurchRpt.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 44:
                    stripAddSupplier.Visibility = Visibility.Visible;
                    break;
                case 51:
                    StripSales.Visibility = Visibility.Visible;
                    SaleOper.Visibility = Visibility.Visible;
                    if (btnName == "stripPOS")
                    {
                        btnPOS2.IsEnabled = true;
                        stripPOS.Visibility = Visibility.Visible;
                        btnPOS2.Visibility = Visibility.Visible;
                    }
                    else if (btnName == "stripAddclient" || btnName == "stripAddSaleman")
                    {
                        stripAddclient.Visibility = Visibility.Visible;
                        stripAddSaleman.Visibility = Visibility.Visible;
                        BtnAddcustomer.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                        if (btnName == "stripSaleInv") BtnSales.Visibility = Visibility.Visible;
                        if (btnName == "stripShowPrice") BtnQutation.Visibility = Visibility.Visible;
                    }
                    break;
                case 52:
                    StripSales.Visibility = Visibility.Visible;
                    SaleSand.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 53:
                    StripSales.Visibility = Visibility.Visible;
                    saleRpt.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 85006:
                    StripSales.Visibility = Visibility.Visible;
                    POSRpt.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 71:
                    StripHR.Visibility = Visibility.Visible;
                    HRdefinition.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 72:
                    StripHR.Visibility = Visibility.Visible;
                    HRoper.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 85018:
                    StripHR.Visibility = Visibility.Visible;
                    HRRpt.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 81:
                    StripSettings.Visibility = Visibility.Visible;
                    foundationDef.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 82:
                    StripSettings.Visibility = Visibility.Visible;
                    stripGnrlSet.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 83:
                    StripSettings.Visibility = Visibility.Visible;
                    usersettings.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 84:
                    StripSettings.Visibility = Visibility.Visible;
                    managSetting.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    StripOffers.Visibility = Visibility.Visible;
                    break;
                case 85:
                    StripSettings.Visibility = Visibility.Visible;
                    StripSyn.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 85023:
                    StripSales.Visibility = Visibility.Visible;
                    NotficReport.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 85030:
                    StripPurches.Visibility = Visibility.Visible;
                    ToolStripNotficPurch.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 85036:
                    ToolStripsallastore.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 85039:
                    ToolStripsallastore.Visibility = Visibility.Visible;
                    break;
            }
        }

        #endregion

        #region ApplyFeatures

        private void ApplyFeatures(string featureList)
        {
            try
            {
                this.LicenseFeatures = featureList;
                if (string.IsNullOrEmpty(featureList) || featureList.Length < 6) return;

                int userId = MainClass.UserID;
                if (userId == 0) userId = 1;

                if (featureList[0] == '0') StripAccount.Visibility = Visibility.Collapsed;

                if (featureList[1] == '0')
                {
                    StripStores.Visibility = Visibility.Collapsed;
                    StripSales.Visibility = Visibility.Collapsed;
                    StripPurches.Visibility = Visibility.Collapsed;
                }

                if (featureList[2] == '0')
                    StripMarine.Visibility = Visibility.Collapsed;
                else
                    LoadMarinePermissions(userId);

                if (featureList[3] == '0') StripHR.Visibility = Visibility.Collapsed;
                if (featureList[4] == '0') StripProjectMang.Visibility = Visibility.Collapsed;

                if (featureList[5] == '1')
                    ApplyPOSOnlyFeatures(userId);

                var daEgy = new SqlDataAdapter("SELECT EgyEInvoice FROM Foundation WHERE EgyEInvoice=1", this.conn1);
                var dtEgy = new DataTable();
                daEgy.Fill(dtEgy);
                StripEgyptEInvoice.Visibility = dtEgy.Rows.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("تفاصيل الخطأ: " + ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMarinePermissions(int userId)
        {
            var da = new SqlDataAdapter(
                "select Forms.buttonName as btnName, Forms.Parent_id from User_Permissions,Forms " +
                "where User_Permissions.Form_id=Forms.id and user_id=" + userId, this.conn1);
            var dt = new DataTable();
            da.Fill(dt);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string btnName = dt.Rows[i]["btnName"].ToString();
                if (string.IsNullOrEmpty(btnName)) continue;
                int parentId = Convert.ToInt32(dt.Rows[i]["Parent_id"]);
                var targetItem = FindMenuItemByName(btnName);

                if (parentId == 61)
                {
                    StripMarine.Visibility = Visibility.Visible;
                    stripMarineDefin.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                }
                else if (parentId == 62)
                {
                    StripMarine.Visibility = Visibility.Visible;
                    stripMarineOper.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    btnPOS2.Visibility = Visibility.Visible;
                }
                else if (parentId == 63)
                {
                    StripMarine.Visibility = Visibility.Visible;
                    stripMarineRpt.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                }
                else if (parentId == 64)
                {
                    StripMarine.Visibility = Visibility.Visible;
                    stripMarineManag.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                }
            }
        }

        private void ApplyPOSOnlyFeatures(int userId)
        {
            SetAllMenuItemsVisible(MainMenuStrip, false);
            StripSupport.Visibility = Visibility.Visible;
            StripZatcaSetting.Visibility = Visibility.Visible;
            StripZatcaSetting.IsEnabled = true;
            SetChildMenuItemsVisible(StripSupport, true);

            var da = new SqlDataAdapter(
                "select Forms.buttonName as btnName, Forms.Parent_id from User_Permissions,Forms " +
                "where User_Permissions.Form_id=Forms.id and user_id=" + userId, this.conn1);
            var dt = new DataTable();
            da.Fill(dt);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string btnName = dt.Rows[i]["btnName"].ToString();
                if (string.IsNullOrEmpty(btnName)) continue;
                int parentId = Convert.ToInt32(dt.Rows[i]["Parent_id"]);
                try
                {
                    var targetItem = FindMenuItemByName(btnName);
                    ApplyPOSPermission(parentId, btnName, targetItem);
                }
                catch { }
            }
        }

        private void ApplyPOSPermission(int parentId, string btnName, MenuItem targetItem)
        {
            switch (parentId)
            {
                case 21:
                    StripAccount.Visibility = Visibility.Visible;
                    AccountDefinintionMenu.Visibility = Visibility.Visible;
                    stripBox.Visibility = Visibility.Visible;
                    stripBank.Visibility = Visibility.Visible;
                    StripPaymentMethodCustomr.Visibility = Visibility.Visible;
                    break;
                case 23:
                    StripAccount.Visibility = Visibility.Visible;
                    AccountRpt.Visibility = Visibility.Visible;
                    stripMenuTaxReport.Visibility = Visibility.Visible;
                    break;
                case 31:
                    StripStores.Visibility = Visibility.Visible;
                    StoreDefinition.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 51:
                    StripSales.Visibility = Visibility.Visible;
                    SaleOper.Visibility = Visibility.Visible;
                    if (btnName == "stripPOS")
                    {
                        btnPOS2.IsEnabled = true;
                        stripPOS.Visibility = Visibility.Visible;
                        btnPOS2.Visibility = Visibility.Visible;
                    }
                    else if (btnName == "stripAddclient" || btnName == "stripAddSaleman")
                    {
                        stripAddclient.Visibility = Visibility.Visible;
                        stripAddSaleman.Visibility = Visibility.Visible;
                    }
                    else if (targetItem != null)
                        targetItem.Visibility = Visibility.Visible;
                    break;
                case 52:
                    StripSales.Visibility = Visibility.Visible;
                    SaleSand.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 53:
                    StripSales.Visibility = Visibility.Visible;
                    saleRpt.Visibility = Visibility.Visible;
                    StripRptInvSalesDetails.Visibility = Visibility.Visible;
                    StripRptReturnSalesDetails.Visibility = Visibility.Visible;
                    StripRptNetSalesDetails.Visibility = Visibility.Visible;
                    stripDetailSales.Visibility = Visibility.Visible;
                    StripItemsSalesDetails.Visibility = Visibility.Visible;
                    stripShiftCloses.Visibility = Visibility.Visible;
                    StripInvProfit.Visibility = Visibility.Visible;
                    stripCleintBala.Visibility = Visibility.Visible;
                    break;
                case 41:
                    StripPurches.Visibility = Visibility.Visible;
                    PurchOper.Visibility = Visibility.Visible;
                    stripPurchInv.Visibility = Visibility.Visible;
                    stripPurchReturn.Visibility = Visibility.Visible;
                    stripPaySupl.Visibility = Visibility.Visible;
                    stripAddSupplier.Visibility = Visibility.Visible;
                    break;
                case 42:
                    StripPurches.Visibility = Visibility.Visible;
                    PurchSand.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 43:
                    StripPurches.Visibility = Visibility.Visible;
                    PurchRpt.Visibility = Visibility.Visible;
                    StripRptInvPurchaseDetails.Visibility = Visibility.Visible;
                    StripRptReturnPurchaseDetails.Visibility = Visibility.Visible;
                    StripRptNetPurchase.Visibility = Visibility.Visible;
                    stripPurchDetails.Visibility = Visibility.Visible;
                    StripItemsPurchDetails.Visibility = Visibility.Visible;
                    stripSupplAccou.Visibility = Visibility.Visible;
                    break;
                case 85006:
                    StripSales.Visibility = Visibility.Visible;
                    POSRpt.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 71:
                    StripHR.Visibility = Visibility.Visible;
                    HRdefinition.Visibility = Visibility.Visible;
                    stripDefineEmp.Visibility = Visibility.Visible;
                    break;
                case 81:
                    StripSettings.Visibility = Visibility.Visible;
                    foundationDef.Visibility = Visibility.Visible;
                    stripFoundation.Visibility = Visibility.Visible;
                    break;
                case 82:
                    StripSettings.Visibility = Visibility.Visible;
                    stripGnrlSet.Visibility = Visibility.Visible;
                    stripGnrlSetting.Visibility = Visibility.Visible;
                    stripBackup.Visibility = Visibility.Visible;
                    StripImportData.Visibility = Visibility.Visible;
                    break;
                case 83:
                    StripSettings.Visibility = Visibility.Visible;
                    usersettings.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
                case 84:
                    StripSettings.Visibility = Visibility.Visible;
                    managSetting.Visibility = Visibility.Visible;
                    StripOffers.Visibility = Visibility.Visible;
                    break;
                case 44:
                    stripAddSupplier.Visibility = Visibility.Visible;
                    break;
                case 85:
                    StripSettings.Visibility = Visibility.Visible;
                    StripSyn.Visibility = Visibility.Visible;
                    StripZatcaSyncInv.Visibility = Visibility.Visible;
                    if (targetItem != null) targetItem.Visibility = Visibility.Visible;
                    break;
            }
        }

        #endregion

        // ══════════════════════════════════════════════════════════════
        // نهاية القسم الأول - اطلب القسم الثاني للحصول على بقية الكود
        // ══════════════════════════════════════════════════════════════
        #region Sync

        public async void CheckSyncAsync()
        {
            try
            {
                if (Sync.CheckSync(MainClass.BranchNo) && Sync.SyncType > 0)
                {
                    Sync.ValidAPIUrl = await Sync.ISValidAPIUrlAsync();
                    Dispatcher.Invoke(() =>
                    {
                        btnRefreshSync.Visibility = Sync.BranchType == 4 ? Visibility.Visible : Visibility.Collapsed;
                    });
                    Dispatcher.Invoke(() => PerformSync(false));
                    SyncTimer();
                }
            }
            catch { }
        }

        private void SyncTimer()
        {
            try
            {
                MoveWaitTimer.Interval = Sync.PostInterval;
                MoveWaitTimer.Start();
            }
            catch { }
        }

        private void MoveWaitTimer_Tick(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => PerformSync(false));
        }

        public async void PerformSync(bool loadCardData)
        {
            try
            {
                if (Sync.SyncType > 0)
                {
                    if (Sync.BranchType == 4)
                    {
                        if (await Sync.ISValidAPIUrlAsync())
                        {
                            btnSyncIng.Text = "حالة المزامنة: نشطة";
                            btnSyncIng.Foreground = new SolidColorBrush(Colors.Green);

                            if (loadCardData)
                            {
                                btnSyncIng.Text = "حالة المزامنة: جاري المزامنة";
                                btnSyncIng.Foreground = new SolidColorBrush(Colors.Orange);
                                if (await Sync.ReadCloudDataManully())
                                {
                                    btnSyncIng.Text = "حالة المزامنة: نشطة";
                                    btnSyncIng.Foreground = new SolidColorBrush(Colors.Green);
                                }
                            }

                            if (!BackgroundWorker1.IsBusy)
                                BackgroundWorker1.RunWorkerAsync();
                        }
                        else
                        {
                            btnSyncIng.Text = "حالة المزامنة: لا يوجد إتصال";
                            btnSyncIng.Foreground = new SolidColorBrush(Colors.Red);
                        }
                    }
                    else if (await Sync.ISValidAPIUrlAsync())
                    {
                        btnSyncIng.Text = "حالة المزامنة: جاري المزامنة";
                        btnSyncIng.Foreground = new SolidColorBrush(Colors.Orange);

                        if (MainClass.Conn_type == 1)
                        {
                            await Sync.ReadDataPeriodically();
                            await Sync.PostDataPeriodically();
                            btnSyncIng.Text = "حالة المزامنة: نشطة";
                            btnSyncIng.Foreground = new SolidColorBrush(Colors.Green);
                        }
                        else
                        {
                            btnSyncIng.Text = "حالة المزامنة: نشطة";
                            btnSyncIng.Foreground = new SolidColorBrush(Colors.Green);
                        }
                    }
                    else
                    {
                        btnSyncIng.Text = "حالة المزامنة: لا يوجد إتصال";
                        btnSyncIng.Foreground = new SolidColorBrush(Colors.Red);
                    }
                }
                else
                {
                    btnSyncIng.Text = "";
                }
            }
            catch { }
        }

        private async void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                btnSyncIng.Text = "حالة المزامنة: جاري المزامنة";
                btnSyncIng.Foreground = new SolidColorBrush(Colors.Orange);
            });

            await Sync.PostDataPeriodicallyCloud();

            if (Sync.SyncQuantity)
            {
                var syncOp = new SyncOperation();
                await syncOp.ReadStockOnline();
            }

            Dispatcher.Invoke(() =>
            {
                btnSyncIng.Text = "حالة المزامنة: نشطة";
                btnSyncIng.Foreground = new SolidColorBrush(Colors.Green);
            });
        }

        #endregion

        #region DB Update

        /// <summary>
        /// التحقق من تحديث قاعدة البيانات وتطبيق التعديلات
        /// </summary>
        private void CheckUpdateDB()
        {
            SqlConnection sqlConn = null;

            try
            {
                sqlConn = MainClass.ConnObj();
                if (sqlConn.State != ConnectionState.Open)
                    sqlConn.Open();

                // ═══ إنشاء جدول الإصدارات إن لم يكن موجوداً ═══
                new SqlCommand(StoredQueries.CreateVersionTb, sqlConn)
                    .ExecuteNonQuery();

                // ═══ جلب آخر إصدار مسجل في قاعدة البيانات ═══
                var versionCmd = new SqlCommand(
                    "SELECT ISNULL(MAX(VersionNo), 0) FROM DbVersions",
                    sqlConn);

                object versionResult = versionCmd.ExecuteScalar();
                double currentDbVersion = 0;
                if (versionResult != null && versionResult != DBNull.Value)
                    double.TryParse(versionResult.ToString(), out currentDbVersion);

                // ═══ جلب الإصدار المطلوب من Version.txt ═══
                var versionInfo = Common.CurrentVersion();
                if (versionInfo == null)
                {
                    Dispatcher.Invoke(() =>
                        ProgressBarC.Visibility = Visibility.Collapsed);
                    return;
                }

                double requiredVersion = 0;
                double.TryParse(versionInfo.Version, out requiredVersion);

                // ═══ المقارنة: هل يحتاج تحديث؟ ═══
                if (currentDbVersion < requiredVersion)
                {
                    if (MainClass.Conn_type == 1)
                    {
                        // ═══ تنفيذ سكريبتات التحديث ═══
                        ExecuteDbUpdateScripts(sqlConn);

                        // ═══ تسجيل الإصدار الجديد ═══
                        RegisterNewVersion(sqlConn, versionInfo);

                        // إغلاق الاتصال
                        if (sqlConn.State == ConnectionState.Open)
                            sqlConn.Close();

                        // إعدادات إضافية
                        Common.AddGediaSettins();
                    }
                    else
                    {
                        // ✅ نسخة عميل: لا يمكنه تحديث القاعدة
                        Dispatcher.Invoke(() =>
                        {
                            ProgressBarC.Visibility = Visibility.Collapsed;
                            MessageBox.Show(
                                "تحذير: نسخة البيانات غير متوافقة مع نسخة البرنامج الحالية\n" +
                                "يرجى التواصل مع مسؤول النظام لتحديث قاعدة البيانات",
                                "تحذير",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        });
                    }
                }
                else
                {
                    // ✅ لا يوجد تحديث مطلوب
                    try
                    {
                        Dispatcher.Invoke(() =>
                            ProgressBarC.Visibility = Visibility.Collapsed);
                    }
                    catch { }
                }
            }
            catch (SqlException ex)
            {
                Dispatcher.Invoke(() =>
                    MessageBox.Show(
                        $"خطأ في تحديث قاعدة البيانات:\n{ex.Message}",
                        "خطأ",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    MessageBox.Show(
                        $"خطأ غير متوقع:\n{ex.Message}",
                        "خطأ",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error));
            }
            finally
            {
                try
                {
                    if (sqlConn?.State == ConnectionState.Open)
                        sqlConn.Close();
                }
                catch { }
            }
        }

        /// <summary>
        /// تنفيذ سكريبتات التحديث من AlterDb
        /// </summary>
        private void ExecuteDbUpdateScripts(SqlConnection sqlConn)
        {
            // ✅ تقسيم السكريبتات على GO
            string[] sqlScripts = Properties.Resources.AlterDb.Split(
                new[] { "GO" },
                StringSplitOptions.RemoveEmptyEntries);

            if (sqlConn.State == ConnectionState.Closed)
                sqlConn.Open();

            // ✅ إظهار ProgressBar
            Dispatcher.Invoke(() =>
            {
                ProgressBarC.Visibility = Visibility.Visible;
                ProgressBarC.Maximum = sqlScripts.Length;
                ProgressBarC.Minimum = 0;
                ProgressBarC.Value = 0;
            });

            int successCount = 0;
            int errorCount = 0;

            foreach (string script in sqlScripts)
            {
                string trimmedScript = script.Trim();
                if (string.IsNullOrWhiteSpace(trimmedScript))
                {
                    Dispatcher.Invoke(() => ProgressBarC.Value++);
                    continue;
                }

                SqlTransaction trans = null;
                try
                {
                    trans = sqlConn.BeginTransaction();

                    var cmd = new SqlCommand(trimmedScript, sqlConn, trans)
                    {
                        CommandTimeout = 120 // ✅ Timeout مناسب للتحديثات الكبيرة
                    };
                    cmd.ExecuteNonQuery();

                    trans.Commit();
                    successCount++;

                    Dispatcher.Invoke(() => ProgressBarC.Value++);
                }
                catch (SqlException ex)
                {
                    try { trans?.Rollback(); } catch { }
                    errorCount++;

                    // تسجيل الخطأ بدون إيقاف باقي السكريبتات
                    System.Diagnostics.Debug.WriteLine(
                        $"DB Script Error: {ex.Message}\n" +
                        $"Script: {trimmedScript.Substring(0, Math.Min(100, trimmedScript.Length))}...");

                    Dispatcher.Invoke(() => ProgressBarC.Value++);
                }
            }

            // ✅ إخفاء ProgressBar
            Dispatcher.Invoke(() =>
            {
                ProgressBarC.Value = 0;
                ProgressBarC.Visibility = Visibility.Collapsed;
            });

            // تقرير ملخص
            if (errorCount > 0)
            {
                Dispatcher.Invoke(() =>
                    MessageBox.Show(
                        $"تم تنفيذ {successCount} أمر بنجاح.\n" +
                        $"فشل {errorCount} أمر.\n" +
                        $"يرجى مراجعة سجل الأخطاء.",
                        "تحديث قاعدة البيانات",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning));
            }
        }

        /// <summary>
        /// تسجيل الإصدار الجديد في جدول DbVersions
        /// </summary>
        private void RegisterNewVersion(
            SqlConnection sqlConn, Updates.VersionInfo versionInfo)
        {
            try
            {
                if (sqlConn.State != ConnectionState.Open)
                    sqlConn.Open();

                var cmd = new SqlCommand(StoredQueries.InsertVersionInfo, sqlConn);

                cmd.Parameters.Add("@VersionNo", SqlDbType.Int)
                    .Value = Convert.ToInt32(versionInfo.Version);

                cmd.Parameters.Add("@Versiontxt", SqlDbType.NVarChar)
                    .Value = versionInfo.VersionText ?? "";

                cmd.Parameters.Add("@Description", SqlDbType.NVarChar)
                    .Value = versionInfo.Description ?? "";

                cmd.Parameters.Add("@VersionType", SqlDbType.NVarChar)
                    .Value = versionInfo.TypeUpdates.ToString();

                cmd.Parameters.Add("@PublishDate", SqlDbType.NVarChar)
                    .Value = versionInfo.PublishDate ?? "";

                cmd.Parameters.Add("@UpdateDate", SqlDbType.DateTime)
                    .Value = DateTime.Now;

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"RegisterNewVersion Error: {ex.Message}");
            }
        }

        #endregion

        #region ══════════════════ التحقق من التحديثات ══════════════════

        /// <summary>
        /// التحقق في الخلفية (يُستدعى عند بدء التشغيل)
        /// </summary>
        private void CheckForUpdatesInBackground()
        {
            try
            {
                
                AppUpdateInfo updateInfo = GetLatestReleaseFromGitHub();

                if (updateInfo == null)
                {
                    System.Diagnostics.Debug.WriteLine("updateInfo = NULL");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("updateInfo != NULL");
                System.Diagnostics.Debug.WriteLine("NewVersion   = " + updateInfo.NewVersion);
                System.Diagnostics.Debug.WriteLine("ReleaseTitle = " + updateInfo.ReleaseTitle);
                System.Diagnostics.Debug.WriteLine("ReleaseNotes = " + updateInfo.ReleaseNotes);
                System.Diagnostics.Debug.WriteLine("DownloadUrl  = " + updateInfo.DownloadUrl);
                System.Diagnostics.Debug.WriteLine("FileName     = " + updateInfo.FileName);
                System.Diagnostics.Debug.WriteLine("FileSize     = " + updateInfo.FileSize);
                System.Diagnostics.Debug.WriteLine("ReleaseDate  = " + updateInfo.ReleaseDate);

                Dispatcher.Invoke(() => ShowUpdateDialog2(updateInfo));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "CheckForUpdatesInBackground Error: " + ex.ToString());
            }
        }
        /// <summary>
        /// جلب آخر إصدار من GitHub Releases
        /// </summary>
        private AppUpdateInfo GetLatestReleaseFromGitHub()
        {
            try
            {
                string apiUrl =
                    $"https://api.github.com/repos/" +
                    $"{MainClass.GITHUB_USER}/{MainClass.GITHUB_REPO}/releases/latest";

                System.Diagnostics.Debug.WriteLine("API URL = " + apiUrl);

                var request = (System.Net.HttpWebRequest)
                    System.Net.WebRequest.Create(apiUrl);

                request.UserAgent = "SmartAuditERP-UpdateChecker";
                request.Method = "GET";
                request.Timeout = 15000;
                request.Accept = "application/vnd.github.v3+json";

                string json;
                using (var response = (System.Net.HttpWebResponse)request.GetResponse())
                using (var reader = new System.IO.StreamReader(
                    response.GetResponseStream(),
                    System.Text.Encoding.UTF8))
                {
                    json = reader.ReadToEnd();
                }

                System.Diagnostics.Debug.WriteLine("JSON Response = ");
                System.Diagnostics.Debug.WriteLine(json);

                if (string.IsNullOrWhiteSpace(json))
                {
                    System.Diagnostics.Debug.WriteLine("JSON is empty");
                    return null;
                }

                string tagName = ExtractJsonValue(json, "tag_name").TrimStart('v', 'V');
                System.Diagnostics.Debug.WriteLine("tagName = " + tagName);

                if (string.IsNullOrWhiteSpace(tagName))
                {
                    System.Diagnostics.Debug.WriteLine("tag_name not found");
                    return null;
                }

                if (!Version.TryParse(tagName, out Version serverVersion))
                {
                    System.Diagnostics.Debug.WriteLine("serverVersion parse failed");
                    return null;
                }

                if (!Version.TryParse(MainClass.CurrentVersion, out Version currentVersion))
                {
                    System.Diagnostics.Debug.WriteLine("currentVersion parse failed");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine("serverVersion  = " + serverVersion);
                System.Diagnostics.Debug.WriteLine("currentVersion = " + currentVersion);

                if (serverVersion <= currentVersion)
                {
                    System.Diagnostics.Debug.WriteLine("No update available");
                    return null;
                }

                var info = new AppUpdateInfo
                {
                    NewVersion = tagName,
                    ReleaseTitle = ExtractJsonValue(json, "name"),
                    ReleaseNotes = ExtractJsonValue(json, "body"),
                    DownloadUrl = ExtractDownloadUrl(json),
                    FileName = ExtractAssetName(json),
                    FileSize = ExtractFileSize(json)
                };

                string publishDate = ExtractJsonValue(json, "published_at");
                if (!string.IsNullOrWhiteSpace(publishDate) &&
                    DateTime.TryParse(publishDate, out DateTime dt))
                {
                    info.ReleaseDate = dt.ToString("yyyy/MM/dd hh:mm tt");
                }

                System.Diagnostics.Debug.WriteLine("Update info created successfully");
                return info;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "GetLatestReleaseFromGitHub Error: " + ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// عرض نافذة التحديث (WPF)
        /// </summary>
        private void ShowUpdateDialog2(AppUpdateInfo info)
        {
            var frm = new UpdateNotification(info);
            frm.ShowDialog();

            if (frm.UserAccepted)
            {
                string updateExePath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "update.exe");

                if (System.IO.File.Exists(updateExePath))
                {
                    string args =
                        $"\"{info.DownloadUrl}\" " +
                        $"\"{info.NewVersion}\" " +
                        $"\"{MainClass.CurrentVersion}\"";

                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(
                            updateExePath, args)
                        {
                            UseShellExecute = true
                        });

                    System.Windows.Application.Current.Shutdown();
                }
                else
                {
                    MessageBox.Show(
                        "ملف التحديث update.exe غير موجود!",
                        "خطأ",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region ══════════════════ JSON Helpers ══════════════════

        private string ExtractJsonValue(string json, string key)
        {
            try
            {
                string searchKey = $"\"{key}\"";
                int keyIndex = json.IndexOf(searchKey);
                if (keyIndex == -1) return "";

                int colonIndex = json.IndexOf(':', keyIndex + searchKey.Length);
                if (colonIndex == -1) return "";

                int valueStart = colonIndex + 1;
                while (valueStart < json.Length && json[valueStart] == ' ')
                    valueStart++;

                if (valueStart >= json.Length) return "";

                if (json[valueStart] == '"')
                {
                    int strStart = valueStart + 1;
                    int strEnd = strStart;
                    while (strEnd < json.Length)
                    {
                        if (json[strEnd] == '"' && json[strEnd - 1] != '\\')
                            break;
                        strEnd++;
                    }
                    return json.Substring(strStart, strEnd - strStart)
                        .Replace("\\n", "\n")
                        .Replace("\\r", "")
                        .Replace("\\\"", "\"");
                }
                else if (json[valueStart] == 'n')
                {
                    return "";
                }
                else
                {
                    int numEnd = valueStart;
                    while (numEnd < json.Length &&
                           json[numEnd] != ',' &&
                           json[numEnd] != '}' &&
                           json[numEnd] != ']')
                        numEnd++;
                    return json.Substring(valueStart, numEnd - valueStart).Trim();
                }
            }
            catch { return ""; }
        }

        private string ExtractDownloadUrl(string json)
        {
            try
            {
                string key = "\"browser_download_url\"";
                int keyIdx = json.IndexOf(key);
                if (keyIdx == -1) return "";
                int colonIdx = json.IndexOf(':', keyIdx + key.Length);
                int urlStart = json.IndexOf('"', colonIdx + 1) + 1;
                int urlEnd = json.IndexOf('"', urlStart);
                return json.Substring(urlStart, urlEnd - urlStart);
            }
            catch { return ""; }
        }

        private string ExtractAssetName(string json)
        {
            try
            {
                int assetsIdx = json.IndexOf("\"assets\"");
                if (assetsIdx == -1) return "update_SmartAuditERP.zip";
                string nk = "\"name\"";
                int nIdx = json.IndexOf(nk, assetsIdx);
                if (nIdx == -1) return "update_SmartAuditERP.zip";
                int cIdx = json.IndexOf(':', nIdx + nk.Length);
                int nStart = json.IndexOf('"', cIdx + 1) + 1;
                int nEnd = json.IndexOf('"', nStart);
                return json.Substring(nStart, nEnd - nStart);
            }
            catch { return "update_SmartAuditERP.zip"; }
        }

        private long ExtractFileSize(string json)
        {
            try
            {
                int assetsIdx = json.IndexOf("\"assets\"");
                if (assetsIdx == -1) return 0;
                string sk = "\"size\"";
                int sIdx = json.IndexOf(sk, assetsIdx);
                if (sIdx == -1) return 0;
                int cIdx = json.IndexOf(':', sIdx + sk.Length);
                int nStart = cIdx + 1;
                while (nStart < json.Length && json[nStart] == ' ') nStart++;
                int nEnd = nStart;
                while (nEnd < json.Length && char.IsDigit(json[nEnd])) nEnd++;
                return long.Parse(json.Substring(nStart, nEnd - nStart));
            }
            catch { return 0; }
        }

        #endregion

        #region Zatca

        public object GetNoSendZatca()
        {
            try
            {
                var da = new SqlDataAdapter("select * from SettingZatca", this.conn);
                var dt = new DataTable();
                da.Fill(dt);
                DateTime startDate = DateTime.MinValue;
                if (dt.Rows.Count > 0)
                    startDate = Convert.ToDateTime(dt.Rows[0]["startDate"]).Date;

                var sqlConn = MainClass.ConnObj();
                if (sqlConn.State != ConnectionState.Open)
                    sqlConn.Open();

                var cmd = new SqlCommand(
                    "select count(*) as count from Inv where branch=" + MainClass.BranchNo +
                    " and inv_type in(2,3,20) and proc_type in (1,2) and ZatcaSent=0 and date>='" +
                    startDate.ToString("yyyy-MM-dd") + "'", sqlConn);

                int count = Convert.ToInt32(cmd.ExecuteScalar());

                if (sqlConn.State == ConnectionState.Open)
                    sqlConn.Close();

                return count;
            }
            catch { }
            return 0;
        }

        public object shownozatca()
        {
            try
            {
                int count = Convert.ToInt32(GetNoSendZatca());
                if (count > 0)
                {
                    lblNosendzatca.Visibility = Visibility.Visible;
                    lblNosendzatca.Text = "      يوجد لديك : " + count + " فاتورة غير مرحلة للهيئة";
                }
                return count;
            }
            catch { }
            return 0;
        }

        #endregion

        #region Salla Sync

        private async void tmrAutoSync_Tick(object sender, EventArgs e)
        {
           
        }

        private async Task<int> GetPendingOrdersCount(int merchantId)
        {
            try
            {
                string url = "https://auditor-salla.auditor.sa:443/api/orders/pending-for-desktop?merchantId=" + merchantId;
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<List<object>>(json).Count;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error checking pending orders: " + ex.Message);
            }
            return 0;
        }

        private void ShowNotification(int count)
        {
            if (NotifyIcon1 == null)
            {
                NotifyIcon1 = new System.Windows.Forms.NotifyIcon();
                NotifyIcon1.Icon = System.Drawing.SystemIcons.Information;
                NotifyIcon1.Visible = true;
            }
            NotifyIcon1.BalloonTipTitle = "طلبات جديدة من سلة";
            NotifyIcon1.BalloonTipText = "وصلك " + count + " طلبات جديدة جاهزة للفوترة!";
            NotifyIcon1.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            NotifyIcon1.ShowBalloonTip(5000);
            SystemSounds.Beep.Play();
        }

        #endregion

        #region Status Bar Click Events

        private void btnSyncIng_Click(object sender, MouseButtonEventArgs e)
        {
            PerformSync(false);
        }

        private void btnRefreshSync_Click(object sender, RoutedEventArgs e)
        {
            if (Sync.ActiveSync && Sync.SyncType == 1 && MainClass.Conn_type == 1)
            {
                if (Sync.BranchType != 4)
                {
                    var syncOp = new SyncOperation();
                    new Thread(() => syncOp.ReadCardsDataOnline()).Start();
                }
                else
                {
                    PerformSync(true);
                }
            }
        }

        private void lblnotify_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_is_active)
                {
                    originalColor = lblnotify.Foreground as SolidColorBrush;
                    lblnotify.Foreground = new SolidColorBrush(Colors.Orange);
                    ReceivedData.LoadReceived(ConnectBroker.connString);
                    colorChangeTimer.Interval = TimeSpan.FromMilliseconds(2000);
                    colorChangeTimer.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء تحميل البيانات: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void lblNosendzatca_Click(object sender, MouseButtonEventArgs e)
        {
            var frm = new frmInvsSyncStatusZatca();
            frm.Show();
            frm.Activate();
        }

        private async void LblLicenseExpire_Click(object sender, MouseButtonEventArgs e)
        {
            var currentForeground = LblLicenseExpire.Foreground;
            LblLicenseExpire.Foreground = new SolidColorBrush(Colors.Orange);
            await Task.Delay(3000);
            LblLicenseExpire.Foreground = currentForeground;
            await ManagerOnline.CheckLicenseStatus();
        }

        #endregion

        #region Menu Click Events - Accounting Definitions

        private void stripAccountsDirectory_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmAccountsDirectory();
            frm.Show();
            frm.Activate();
        }

        private void stripTreeAcc_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmAccountsTree);
            MainClass.DoApplyUserSett(frmAccountsTree);
            frmAccountsTree.Show();
            frmAccountsTree.Activate();
        }

        private void stripBox_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmTreasury);
            MainClass.DoApplyUserSett(frmTreasury);
            frmTreasury.Show();
            frmTreasury.Activate();
        }

        private void stripBank_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmBanks);
            MainClass.DoApplyUserSett(frmBanks);
            frmBanks.Show();
            frmBanks.Activate();
        }

        private void stripCostCenterCard_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmCostCenter);
            MainClass.DoApplyUserSett(frmCostCenter);
            frmCostCenter.Show();
            frmCostCenter.Activate();
        }

        private void StripPaymentMethodCustomr_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmCustomers();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Title = MainClass.Language == "en" ? "Define A Customer" : "تعريف عملاء الدفع تقسيط";
            frm.Type = 5;
            frm.Show();
            frm.Activate();
        }

        private void StripExpenses_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmExpensesAccount();
            frm.Show();
        }

        #endregion

        #region Menu Click Events - Accounting Operations

        private void stripIntialBalance_Click(object sender, RoutedEventArgs e)
        {
            FrmIntialRestraiction.Name = "FrmIntialRestraiction";
            MainClass.ApplyPermissionToForm(FrmIntialRestraiction);
            MainClass.DoApplyUserSett(FrmIntialRestraiction);
            FrmIntialRestraiction.Show();
            FrmIntialRestraiction.Activate();
        }

        private void stripAddRestriction_Click(object sender, RoutedEventArgs e)
        {
            var frm = new FrmNewEntry();
            frm.Name = "FrmNewEntry";
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Show();
            frm.Activate();
        }

        private void stripSandQabth_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmSandQD();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Show();
            frm.Activate();
        }

        private void stripSandSarf_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmSandSD();
            MainClass.ApplyPermissionToForm(frm);
            frm.Show();
            frm.Activate();
        }

        private void stripSandVAT_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmSandVAT();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Show();
            frm.Activate();
        }

        #endregion

        #region Menu Click Events - Accounting Reports

        private void stripRestraction_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmRptEntries);
            MainClass.DoApplyUserSett(frmRptEntries);
            frmRptEntries.Show();
            frmRptEntries.Activate();
        }

        private void stripSandat_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmSummaryD);
            MainClass.DoApplyUserSett(frmSummaryD);
            frmSummaryD.Show();
            frmSummaryD.Activate();
        }

        private void stripAccountBala_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmAccountBalance();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Show();
            frm.Activate();
        }

        private void stripAccountStatement_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmAccountsStatement();
            frm.Show();
            frm.Activate();
        }

        private void stripShowKhzna_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmRptKhzna);
            MainClass.DoApplyUserSett(frmRptKhzna);
            frmRptKhzna.Show();
            frmRptKhzna.Activate();
        }

        private void stripRptBalances_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptBalances();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Show();
            frm.Activate();
        }

        private void StripRptCostCenter_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptCostCenter();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Activate();
            frm.Show();
        }

        private void StripCostCenter_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmCostCenterBalance();
            frm.Show();
            frm.Activate();
        }

        private void stripDailyProcess_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmRptDailyProcess);
            MainClass.DoApplyUserSett(frmRptDailyProcess);
            frmRptDailyProcess.Show();
            frmRptDailyProcess.Activate();
        }

        private void stripMezan_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmMezanMorg3a();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Title = MainClass.Language == "en" ? "main audit balance" : "ميزان المراجعة";
            frm.lblHeader.Text = MainClass.Language == "en" ? "main audit balance" : "ميزان المراجعة";
            frm._Type = 1;
            frm.Show();
            frm.Activate();
        }

        private void stripIncomeAnalysis_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptIncomeStatement();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Title = MainClass.Language == "en-us" ? "Profit and loss of analytical accounts" : "قائمة الدخل الفرعية";
           // frm.lblHeader.Text = frm.Title;
            frm._Type = 2;
            frm._FormType = 1;
            frm.Show();
            frm.Activate();
        }

        private void stripIncomeAnalysis2_Click(object sender, RoutedEventArgs e)
        {
            var frm = new IncomeStatementForm();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Title = "قائمة الدخل";
            frm.Show();
            frm.Activate();
        }

        private void stripBadgetAnalys_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmMezanyaArba7();
            frm.Tag = "frmMezanyaArba74";
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Title = MainClass.Language == "en" ? "Balance Sheet analytical Accounts" : "ميزانية تحليلية";
            frm.lblHeader.Text = frm.Title;
            frm._Type = 2;
            frm._FormType = 2;
            frm.Show();
            frm.Activate();
        }

        private void stripMenuTaxReport_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmVATReturn();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Show();
            frm.Activate();
        }

        #endregion

        #region Menu Click Events - Store Definitions

        private void StripItemDirctory_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmItemsDir();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Activate();
            frm.Show();
        }

        private void stripStore_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmSafes);
            MainClass.DoApplyUserSett(frmSafes);
            frmSafes.Show();
            frmSafes.Activate();
        }

        private void stripGroup_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmItemCategory);
            MainClass.DoApplyUserSett(frmItemCategory);
            frmItemCategory.Show();
            frmItemCategory.Activate();
        }

        private void stripUnit_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmUnits);
            MainClass.DoApplyUserSett(frmUnits);
            frmUnits.Show();
            frmUnits.Activate();
        }

        private void stripItem_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmItems);
            MainClass.DoApplyUserSett(frmItems);
            frmItems.Show();
            frmItems.Activate();
        }

        #endregion

        #region Menu Click Events - Store Operations

        private void stripTransfer_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInventoryTransfer();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.InvType = 8;
            if (MainClass.Language == "ar")
                frm.Title = "مناقلة(تحويل مخزني)";
            frm.ProcType = 2;
            frm.Show();
            frm.Activate();
        }

        private void stripFirstStock_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvPurch();
            frm.Tag = "SalePurch3";
            frm.InvType = 9;
            frm.ProcType = 1;
            frm.EntryType = 11;
            frm.Title = MainClass.Language == "ar" ? "بضاعة أول المدة" : "Beginning inventory";
            MainClass.ApplyPermissionToForm(frm);
            frm.cmbProcType.SelectedIndex = 2;
            frm.cmbProcTypeSrch.SelectedIndex = 2;
            frm.Show();
            frm.WindowState = WindowState.Maximized;
            frm.Activate();
        }

        private void stripInputInv_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvInputOutput();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.InvType = 4;
            frm.ProcType = 1;
            frm.Title = MainClass.Language == "ar" ? "امر توريد مخزني (فاتورة إدخال)" : "Input invoice";
            frm.WindowState = WindowState.Maximized;
            frm.Show();
            frm.Activate();
        }

        private void stripOutputInv_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvInputOutput();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.InvType = 5;
            frm.ProcType = 1;
            frm.Title = MainClass.Language == "ar" ? "فاتورة اخراج(امر صرف مخزني)" : "Output invoice";
            frm.WindowState = WindowState.Maximized;
            frm.Show();
            frm.Activate();
        }

        private void strIpinventoryAdjustment_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmSafeAdjust();
            frm.Show();
            frm.Activate();
        }

        private void StripInvertoryDeliveries_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvoiceDeliveries();
            frm.Show();
            frm.Activate();
        }

        private void stripInvertoryOrderInv_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvInputOutput();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.InvType = 14;
            frm.ProcType = 1;
            frm.BtnInvertoryOrder.Visibility = Visibility.Visible;
            frm.Title = MainClass.Language == "ar" ? "طلب بضاعة" : "Inventory order";
            frm.Show();
            frm.Activate();
        }

        private void StripPrintBarcode_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmPrintBarcode();
            frm.Show();
        }

        #endregion

        #region Menu Click Events - Store Reports

        private void stripSafeGrd_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmSafeGrd);
            MainClass.DoApplyUserSett(frmSafeGrd);
            frmSafeGrd.Show();
            frmSafeGrd.Activate();
        }

        private void StripSafeGrdDate_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmSafeGrdToDate);
            MainClass.DoApplyUserSett(frmSafeGrdToDate);
            frmSafeGrdToDate.Show();
            frmSafeGrdToDate.Activate();
        }

        private void stripItemMov_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptItemsActivityDetailed();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Show();
            frm.Activate();
        }

        private void stripItemsMovs_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptItemsActivity();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Show();
            frm.Activate();
        }

        private void stripExpire_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmItemsExpire);
            MainClass.DoApplyUserSett(frmItemsExpire);
            frmItemsExpire.Show();
            frmItemsExpire.Activate();
        }

        private void stripSalesAanalysis_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmRptInvAnalysis);
            MainClass.DoApplyUserSett(frmRptInvAnalysis);
            frmRptInvAnalysis.Show();
            frmRptInvAnalysis.Activate();
        }

        private void stripTotalSalePurch_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmNkdTotSalePurch);
            MainClass.DoApplyUserSett(frmNkdTotSalePurch);
            frmNkdTotSalePurch.Show();
            frmNkdTotSalePurch.Activate();
        }

        private void stripBenefits_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmNetsales);
            MainClass.DoApplyUserSett(frmNetsales);
            frmNetsales.Show();
            frmNetsales.Activate();
        }

        private void stripDecision_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmDecisionHelp);
            MainClass.DoApplyUserSett(frmDecisionHelp);
            frmDecisionHelp.Show();
            frmDecisionHelp.Activate();
        }

        private void StripRptProducedItems_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptProducedItems();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Show();
            frm.Activate();
        }

        private void stripInvbyTypes_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmInvSumByType);
            MainClass.DoApplyUserSett(frmInvSumByType);
            frmInvSumByType.invtype = 2;
            frmInvSumByType.Title = MainClass.Language == "ar" ? "الفواتير بحسب النوع" : "Invoice by type";
            frmInvSumByType.Show();
            frmInvSumByType.Activate();
        }

        private void stripIInvoicesByType_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptInventory();
            frm.Show();
            frm.Activate();
        }

        private void StripItemsExpiration_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptItemsExpiration();
            frm.Show();
            frm.Activate();
        }

        private void StripItemsRptSerialNo_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptSerialNo();
            frm.Show();
            frm.Activate();
        }

        private void StripItemsRptSerialNoSummary_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptSerialNoSummary();
            frm.Show();
            frm.Activate();
        }

        #endregion

        #region Menu Click Events - Purchases

        private void stripPurchInv_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvPurch();
            frm.InvType = 1;
            frm.ProcType = 1;
            frm.EntryType = 1;
            frm.Tag = "SalePurch";
            frm.Name = "frmSalePurch";
            frm.txtRefNo.IsReadOnly = false;
            frm.txtRefDate.IsEnabled = true;
            if (LicenseFeatures == "000001")
            {
                frm.lblCostCenter.Visibility = Visibility.Collapsed;
                frm.cmbCostCenter.Visibility = Visibility.Collapsed;
                frm.btnAddCostCenter.Visibility = Visibility.Collapsed;
            }
            MainClass.ApplyPermissionToForm(frm);
            frm.cmbProcType.SelectedIndex = 0;
            frm.cmbProcTypeSrch.SelectedIndex = 0;
            frm.Show();
            frm.WindowState = WindowState.Maximized;
            frm.Activate();
        }

        private void stripPurchReturn_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvPurch();
            frm.InvType = 1;
            frm.ProcType = 2;
            frm.EntryType = 21;
            frm.Tag = "SalePurch4";
            frm.Title = "مرتجع مشتريات";
            frm.Name = "frmSalePurch4";
            if (LicenseFeatures == "000001")
            {
                frm.lblCostCenter.Visibility = Visibility.Collapsed;
                frm.cmbCostCenter.Visibility = Visibility.Collapsed;
                frm.btnAddCostCenter.Visibility = Visibility.Collapsed;
            }
            MainClass.ApplyPermissionToForm(frm);
            frm.cmbProcType.SelectedIndex = 1;
            frm.cmbProcTypeSrch.SelectedIndex = 1;
            frm.Show();
            frm.WindowState = WindowState.Maximized;
            frm.Activate();
        }

        private void stripPaySupl_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmSandD();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Show();
            frm.Activate();
        }

        private void stripShowSand_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmSummaryQ);
            MainClass.DoApplyUserSett(frmSummaryQ);
            frmSummaryQ.Show();
            frmSummaryQ.Activate();
        }

        private void stripCreditNote_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmCreditNote();
            frm.Show();
            frm.Activate();
        }

        private void ToolStripNotficDeptPurch_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvPurch();
            frm.InvType = 22;
            frm.ProcType = 1;
            frm.EntryType = 33;
            frm.Tag = "FrmNoticCreditPurch";
            frm.Title = "إشعار دائن";
            frm.Name = "FrmNoticCreditPurch";
            frm.LblReturn.Text = "إشعـــار دائن";
            frm.LblReturn.Visibility = Visibility.Visible;
            frm.CkUpdateQty.Visibility = Visibility.Visible;
            if (LicenseFeatures == "000001")
            {
                frm.lblCostCenter.Visibility = Visibility.Collapsed;
                frm.cmbCostCenter.Visibility = Visibility.Collapsed;
                frm.btnAddCostCenter.Visibility = Visibility.Collapsed;
            }
            MainClass.ApplyPermissionToForm(frm);
            frm.cmbProcType.SelectedIndex = 1;
            frm.cmbProcTypeSrch.SelectedIndex = 1;
            frm.Show();
            frm.WindowState = WindowState.Maximized;
            frm.Activate();
        }

        private void ToolStripNotficCreditPurch_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvPurch();
            frm.InvType = 22;
            frm.ProcType = 2;
            frm.EntryType = 32;
            frm.Tag = "FrmNoticDeptPurch";
            frm.Title = "إشعار مدين";
            frm.Name = "FrmNoticDeptPurch";
            frm.LblReturn.Text = "إشعـــار مديــــن";
            frm.LblReturn.Visibility = Visibility.Visible;
            frm.CkUpdateQty.Visibility = Visibility.Visible;
            if (LicenseFeatures == "000001")
            {
                frm.lblCostCenter.Visibility = Visibility.Collapsed;
                frm.cmbCostCenter.Visibility = Visibility.Collapsed;
                frm.btnAddCostCenter.Visibility = Visibility.Collapsed;
            }
            MainClass.ApplyPermissionToForm(frm);
            frm.cmbProcType.SelectedIndex = 1;
            frm.cmbProcTypeSrch.SelectedIndex = 1;
            frm.Show();
            frm.WindowState = WindowState.Maximized;
            frm.Activate();
        }

        private void RptNotficPurch_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptInvNotfic();
            frm.invType = 22;
            frm.Show();
            frm.lblGridTitle.Text = "إشعـــــارات المشتريات";
            frm.Activate();
        }

        private void stripSupplAccou_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmCustAccountGet();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Title = MainClass.Language == "ar" ? "كشف حساب مورد" : "Supplier Balance";
            frm.LblSupplierName.Text = MainClass.Language == "ar" ? "اسم المورد" : "Supplier name";
            frm.GroupBox1.Header = MainClass.Language == "ar" ? "  بيانات حساب مورد" : "  Filters";
            frm.rdAllSuppliers.IsChecked = true;
            frm.Show();
            frm.Activate();
        }

        private void StripRptInvPurchaseDetails_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptInvPurchaseDetails();
            frm.Invtype = 1;
            frm.rbSales.IsChecked = true;
            frm.Show();
            frm.Activate();
        }

        private void StripRptReturnPurchaseDetails_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptInvPurchaseDetails();
            frm.Invtype = 1;
            frm.Show();
            frm.rbReturn.IsChecked = true;
            frm.Title = MainClass.Language == "en-us" ? "Purchase return report" : "تقرير مردود فواتير المشتريات";
            frm.Activate();
        }

        private void StripRptNetPurchase_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptInvPurchaseDetails();
            frm.Show();
            frm.Title = "صافي المشتريات";
            frm.rbAllSales.IsChecked = true;
            frm.Activate();
        }

        private void stripPurchDetails_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvDetails();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.cmbProcType.SelectedIndex = 0;
            frm.cmbProcType.IsEnabled = false;
            frm.Show();
            frm.Activate();
        }

        private void StripItemsPurchDetails_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptItemsSalesDetails();
            frm.Title = MainClass.Language == "ar" ? "مشتريات الأصناف تجميعي" : "Item purchase report is aggregate";
            frm.OperType = 2;
            frm.Show();
            frm.Activate();
        }

        private void stripSupplAccBala_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmCustAccount();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.clientType = 2;
            frm.Title = MainClass.Language == "ar" ? "ارصدة الموردين" : "Supplier Balance";
            frm.LblAcc.Text = MainClass.Language == "ar" ? "مورد" : "Supplier";
            frm.Show();
            frm.Activate();
        }

        private void stripSadadSuppl_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmCustLastPay();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.invtype = 1;
            frm.Show();
            frm.Title = MainClass.Language == "ar" ? "سداد الموردين" : "Supplier Payment";
            frm.LblAccName.Text = MainClass.Language == "ar" ? "المورد" : "Supplier";
            frm.Activate();
        }

        private void stripEmpPurchess_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmEmpInvs();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.InvType = 1;
            frm.Title = MainClass.Language == "ar" ? "مشتريات موظف" : "Purchase invoice by employee";
            frm.Show();
            frm.Activate();
        }

        private void stripSupplInv_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmInvSumByClient);
            MainClass.DoApplyUserSett(frmInvSumByClient);
            frmInvSumByClient.Clienttype = 2;
            frmInvSumByClient.Title = MainClass.Language == "ar" ? "الفواتير بحسب الموردين" : "Invoices by supplier";
            frmInvSumByClient.lblClintType.Text = MainClass.Language == "ar" ? "اسم المورد" : "Supplier Name";
            frmInvSumByClient.Show();
            frmInvSumByClient.Activate();
        }

        private void stripAddSupplier_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmCustomers();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Title = MainClass.Language == "ar" ? "تعريف مورد" : "New Supplier";
            frm.Type = 2;
            frm.Show();
            frm.Activate();
        }

        #endregion

        #region Menu Click Events - Sales Operations

        private void stripPOS_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvPOS();
            frm.InvType = 3;
            frm.ProcType = 1;
            MainClass.ApplyPermissionToForm(frm);
            frm.ProcType = 1;
            frm.Show();
            frm.WindowState = WindowState.Maximized;
            frm.Activate();
        }

        private void btnPOS2_Click(object sender, RoutedEventArgs e)
        {
            if (StripMarine.Visibility == Visibility.Visible && !MainClass.IsTrial && stripMarineInv.IsEnabled)
            {
                var frm = new frminvoice();
                MainClass.ApplyPermissionToForm(frm);
                MainClass.DoApplyUserSett(frm);
                frm.Show();
                frm.WindowState = WindowState.Maximized;
                frm.Activate();
            }
            else if (stripPOS.IsEnabled)
            {
                var frm = new frmInvPOS();
                frm.InvType = 3;
                frm.ProcType = 1;
                MainClass.ApplyPermissionToForm(frm);
                frm.ProcType = 1;
                frm.Show();
                frm.WindowState = WindowState.Maximized;
                frm.Activate();
            }
        }

        private void stripSaleInv_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvSale();
            frm.InvType = 2;
            frm.ProcType = 1;
            frm.EntryType = 2;
            frm.Tag = "InvSale";
            MainClass.ApplyPermissionToForm(frm);
            frm.cmbProcTypeSrch.SelectedIndex = 0;
            frm.Show();
            frm.WindowState = WindowState.Maximized;
            frm.Activate();
        }

        private void stripSaleRetu_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvSale();
            MainClass.ApplyPermissionToForm(frm);
            frm.InvType = 2;
            frm.ProcType = 2;
            frm.EntryType = 22;
            frm.Tag = "SalePurch5";
            frm.Title = "مرتجع مبيعات";
            frm.cmbProcTypeSrch.SelectedIndex = 1;
            frm.Show();
            frm.WindowState = WindowState.Maximized;
            frm.Activate();
        }

        private void stripShowPrice_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvSale();
            frm.InvType = 2;
            frm.ProcType = 4;
            frm.Tag = "Quotation";
            MainClass.ApplyPermissionToForm(frm);
            frm.cmbProcTypeSrch.SelectedIndex = 2;
            frm.Title = MainClass.Language == "ar" ? "عرض سعر" : "Quotation";
            frm.Show();
            frm.WindowState = WindowState.Maximized;
            frm.Activate();
        }

        private void stripCloseCasher_Click(object sender, RoutedEventArgs e)
        {
            var frmPwd = new frmCheckPwd();
            var frmPOS = new frmPOS();
            frmPwd.operNo = 1;
            frmPwd.ShowDialog();
            if (!frmPwd.Iscorrect) return;

            frmPOS.CheckHold();
            for (int idx = 0; idx <= 4; idx++)
            {
                if (frmPOS.HoldList[idx] != 0)
                {
                    MessageBox.Show(" لا تستطيع إغلاق اليومية !! يوجد فواتير معلقة ");
                    return;
                }
            }

            var da = new SqlDataAdapter("select ActiveAuto from SettingEmail where Branch_Id=" + MainClass.BranchNo, this.conn);
            var dt = new DataTable();
            da.Fill(dt);

            if (dt.Rows.Count > 0 && Convert.ToBoolean(dt.Rows[0]["ActiveAuto"]))
            {
                if (!MainClass.CheckForInternetConnection())
                {
                    if (MessageBox.Show("   الإنترنت غير متصل بالجهاز, للإستمرار في الإغلاق اضغط نعم .    ", "تحذير",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                        return;
                }
            }

            var frmClose = new frmCloseShift();
            frmClose.Activate();
            if (frmClose.NoInvsFound(1))
            {
                MessageBox.Show("لا يوجد فواتير جديدة يمكن إغلاقها  ");
                this.IsCashierClosed = true;
            }
            else
            {
                frmClose.closeShift(1);
            }
        }

        private void stripSandQclient_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmSandQ();
            frm.Name = "frmSandQ";
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Show();
            frm.Activate();
        }

        private void stripShowSanadat_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmSummaryQ);
            MainClass.DoApplyUserSett(frmSummaryQ);
            frmSummaryQ.Show();
            frmSummaryQ.Activate();
        }

        private void stripDeptNote_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmDebtNote();
            frm.Show();
            frm.Activate();
        }

        private void ToolstripNotficCreditSale_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvSale();
            frm.InvType = 21;
            frm.ProcType = 2;
            frm.EntryType = 30;
            frm.Tag = "FrmNoticCreditSale";
            frm.Name = "FrmNoticCreditSale";
            frm.Title = "إشعار دائن";
            MainClass.ApplyPermissionToForm(frm);
            frm.cmbProcTypeSrch.SelectedIndex = 1;
            frm.Show();
            frm.WindowState = WindowState.Maximized;
            frm.Activate();
            frm.CkUpdateQty.Visibility = Visibility.Visible;
            frm.CkUpdateQty.IsChecked = false;
            frm.lblReturn.Text = "إشعــــار دائــن";
            frm.lblYearPreviews.Visibility = Visibility.Collapsed;
            frm.CmbYearPreviews.Visibility = Visibility.Collapsed;
        }

        private void ToolstripNotficDeptsal_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvSale();
            frm.InvType = 21;
            frm.ProcType = 1;
            frm.EntryType = 31;
            frm.Tag = "FrmNoticDeptSale";
            frm.Name = "FrmNoticDeptSale";
            frm.Title = "إشعار مدين";
            MainClass.ApplyPermissionToForm(frm);
            frm.cmbProcTypeSrch.SelectedIndex = 1;
            frm.Show();
            frm.WindowState = WindowState.Maximized;
            frm.Activate();
            frm.CkUpdateQty.Visibility = Visibility.Visible;
            frm.CkUpdateQty.IsChecked = false;
            frm.lblReturn.Text = "إشعــــار مديـن";
            frm.lblYearPreviews.Visibility = Visibility.Collapsed;
            frm.CmbYearPreviews.Visibility = Visibility.Collapsed;
        }

        private void RptNotfic_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptInvNotfic();
            frm.Show();
            frm.invType = 21;
            frm.Activate();
        }

        private void stripinvcontract_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmcontract();
            frm.InvType = 23;
            frm.ProcType = 1;
            frm.EntryType = 2;
            frm.Tag = "frmcontract";
            frm.Title = "فاتورة مقاولات";
            frm.Name = "frmcontract";
            if (LicenseFeatures == "000001")
            {
                frm.lblCostCenter.Visibility = Visibility.Collapsed;
                frm.cmbCostCenter.Visibility = Visibility.Collapsed;
                frm.btnAddCostCenter.Visibility = Visibility.Collapsed;
            }
            MainClass.ApplyPermissionToForm(frm);
            frm.Show();
            frm.WindowState = WindowState.Maximized;
            frm.Activate();
        }

        private void stripinvcontractreturn_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmcontract();
            frm.InvType = 23;
            frm.ProcType = 2;
            frm.EntryType = 22;
            frm.Tag = "frmcontract";
            frm.Title = "مرتجع مقاولات";
            frm.lblReturn.Text = "  فاتورة مرتجع مقاولات ";
            frm.lblReturn.Foreground = new SolidColorBrush(Colors.Red);
            frm.Name = "frmcontractReturn";
            if (LicenseFeatures == "000001")
            {
                frm.lblCostCenter.Visibility = Visibility.Collapsed;
                frm.cmbCostCenter.Visibility = Visibility.Collapsed;
                frm.btnAddCostCenter.Visibility = Visibility.Collapsed;
            }
            MainClass.ApplyPermissionToForm(frm);
            frm.Show();
            frm.WindowState = WindowState.Maximized;
            frm.Activate();
        }

        private void closeshiftandroid_Click(object sender, RoutedEventArgs e)
        {
            ClosShiftAndroid.Show();
        }

        #endregion

        #region Menu Click Events - Sales Reports

        private void stripShiftCloses_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmCloseShift();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Show();
            frm.Activate();
        }

        private void StripRptInvSalesDetails_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptInvSalesDetails();
            frm.rbSales.IsChecked = true;
            frm.Show();
            frm.Activate();
        }

        private void StripRptReturnSalesDetails_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptInvSalesDetails();
            frm.Show();
            frm.Title = MainClass.Language == "en-us" ? "Sales return report" : "تقرير مردود فواتير المبيعات";
            frm.rbReturn.IsChecked = true;
            frm.Activate();
        }

        private void StripRptNetSalesDetails_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptInvSalesDetails();
            frm.Show();
            frm.Title = MainClass.Language == "en-us" ? "Net sales report" : "صافي المبيعات";
            frm.rbAllSales.IsChecked = true;
            frm.Activate();
        }

        private void stripDetailSales_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvDetails();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Show();
            frm.cmbProcType.SelectedIndex = 1;
            frm.Activate();
        }

        private void StripItemsSalesDetails_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptItemsSalesDetails();
            frm.Title = MainClass.Language == "ar" ? "مبيعات الأصناف تجميعي" : "Item sales report is aggregate";
            frm.Show();
            frm.Activate();
        }

        private void StripSaleByCategry_Click(object sender, RoutedEventArgs e) { var frm = new frmRptSalesByCategory(); frm.Show(); frm.Activate(); }
        private void StripInvProfit_Click(object sender, RoutedEventArgs e) { var frm = new frmInvsProfit(); frm.Show(); frm.Activate(); }
        private void StripItemProfit_Click(object sender, RoutedEventArgs e) { var frm = new frmRptItemsProfit(); frm.Show(); frm.Activate(); }
        private void StripItemsProfitDetails_Click(object sender, RoutedEventArgs e) { var frm = new frmRptItemsProfitDetails(); frm.Show(); frm.Activate(); }
        private void StripRptSalesmen_Click(object sender, RoutedEventArgs e) { var frm = new frmInvBySalesMen(); frm.Show(); frm.Activate(); }

        private void stripEmpSales_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmEmpInvs();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Title = MainClass.Language == "ar" ? "مبيعات موظف" : "Sale by employee";
            frm.InvType = 2;
            frm.Show();
            frm.Activate();
        }

        private void stripCleintBala_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmCustAccountGet();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Title = MainClass.Language == "ar" ? "كشف حساب عميل" : "Customer Balance";
            frm.LblSupplierName.Text = MainClass.Language == "ar" ? "اسم العميل" : "Customer name";
            frm.GroupBox1.Header = MainClass.Language == "ar" ? "  بيانات حساب العميل" : "  Filters";
            frm.rdAllClients.IsChecked = true;
            frm.Show();
            frm.Activate();
        }

        private void stripClientsBalans_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmCustAccount();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.clientType = 1;
            frm.Title = MainClass.Language == "ar" ? "ارصدة العملاء" : "Customer Balance";
            frm.LblAcc.Text = MainClass.Language == "ar" ? "عميل" : "Customer";
            frm.Show();
            frm.Activate();
        }

        private void stripClientPay_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmCustLastPay();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.invtype = 2;
            frm.Show();
            frm.Title = MainClass.Language == "ar" ? "سداد العملاء" : "Customer payment";
            frm.LblAccName.Text = MainClass.Language == "ar" ? "العميل" : "Customer";
            frm.Activate();
        }

        private void stripInvbyclient_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmInvSumByClient);
            MainClass.DoApplyUserSett(frmInvSumByClient);
            frmInvSumByClient.Clienttype = 1;
            frmInvSumByClient.Title = MainClass.Language == "ar" ? "الفواتير بحسب العملاء" : "Invoice by customers";
            frmInvSumByClient.Show();
            frmInvSumByClient.Activate();
        }

        private void StripSalesInPeriod_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmRptSalesInPeriod();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Show();
            frm.Activate();
        }

        private void StripGraphSale_Click(object sender, RoutedEventArgs e) { new FrmRptSalesChart().Show(); }
        private void stripDetailSalescontract_Click(object sender, RoutedEventArgs e) { frmInvSalContactDetails.Show(); }
        private void stripAddclient_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmCustomers();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Title = MainClass.Language == "ar" ? "تعريف عميل" : "New Customer";
            frm.Type = 1;
            frm.Show();
            frm.Activate();
        }
        private void stripAddSaleman_Click(object sender, RoutedEventArgs e) { var frm = new frmSalesMen(); MainClass.ApplyPermissionToForm(frm); MainClass.DoApplyUserSett(frm); frm.Show(); frm.Activate(); }

        // POS Reports
        private void RptPOSSales_Click(object sender, RoutedEventArgs e) { var frm = new frmRptInvSalesDetailsPos(); frm.Show(); frm.Activate(); }
        private void RptPOSItemSalesDetailed_Click(object sender, RoutedEventArgs e) { var frm = new frmItemsalesdetailsPos(); frm.Show(); frm.Activate(); }
        private void RptPOSItemSales_Click(object sender, RoutedEventArgs e) { var frm = new frmRptItemsSalesDetailsPOS(); frm.Show(); frm.Activate(); }
        private void StripRptDailySales_Click(object sender, RoutedEventArgs e) { var frm = new frmRptDailySales(); MainClass.ApplyPermissionToForm(frm); MainClass.DoApplyUserSett(frm); frm.Show(); frm.Activate(); }
        private void StripCategorySaleByDay_Click(object sender, RoutedEventArgs e) { var frm = new frmRptCategorySaleByDay(); frm.Show(); }
        private void ToolItemAndroidSal_Click(object sender, RoutedEventArgs e) { frmRptInvSalesDetailsPosAndroid.Show(); }

        #endregion

        #region Menu Click Events - HR

        private void stripDefineManag_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmManagement); MainClass.DoApplyUserSett(frmManagement); frmManagement.Show(); frmManagement.Activate(); }
        private void stripDefineClass_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmDepartments); MainClass.DoApplyUserSett(frmDepartments); frmDepartments.Show(); frmDepartments.Activate(); }
        private void stripDefineEmp_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmEmployees); MainClass.DoApplyUserSett(frmEmployees); frmEmployees.Show(); frmEmployees.Activate(); }
        private void stripWard_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmEmpSalaryAddSub); MainClass.DoApplyUserSett(frmEmpSalaryAddSub); frmEmpSalaryAddSub.Show(); frmEmpSalaryAddSub.Activate(); }
        private void ToolStripResevedSalary_Click(object sender, RoutedEventArgs e) { FrmReseved.Show(); }
        private void ToolStripSandSalary_Click(object sender, RoutedEventArgs e) { frmSalaryPay.Show(); frmSalaryPay.Activate(); }
        private void ToolStripMenuItemreseved_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmRptReseved); MainClass.DoApplyUserSett(frmRptReseved); frmRptReseved.Show(); frmRptReseved.Activate(); }
        private void stripPaySalary_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmRptSalary); MainClass.DoApplyUserSett(frmRptSalary); frmRptSalary.Show(); frmRptSalary.Activate(); }
        private void ToolStripMenuItemAccountEmp_Click(object sender, RoutedEventArgs e) { frmEmpAccountGet.Show(); }
        private void StripUsersRecords_Click(object sender, RoutedEventArgs e) { var frm = new frmrptUsersRecords(); frm.Show(); frm.Activate(); }

        #endregion

        #region Menu Click Events - Marine

        private void stripMarineGrp_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmGroupM); MainClass.DoApplyUserSett(frmGroupM); frmGroupM.Show(); frmGroupM.Activate(); }
        private void stripAddMarine_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmItemM); MainClass.DoApplyUserSett(frmItemM); frmItemM.Show(); frmItemM.Activate(); }
        private void stripMarineAdds_Click(object sender, RoutedEventArgs e) { var frm = new frmAdditions(); MainClass.ApplyPermissionToForm(frm); MainClass.DoApplyUserSett(frm); frm.Activate(); frm.ShowDialog(); }

        private void stripMarineOwner_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmOwners();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Title = MainClass.Language == "en-us" ? "Define an Owner" : "تعريف مالك";
            frm.Activate();
            frm.ShowDialog();
        }

        private void stripMarineAttend_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmAttendM); MainClass.DoApplyUserSett(frmAttendM); frmAttendM.Show(); frmAttendM.Activate(); }
        private void stripMarineViolation_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmViolationM); MainClass.DoApplyUserSett(frmViolationM); frmViolationM.Show(); frmViolationM.Activate(); }
        private void stripMarineQueue_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmQueueM); MainClass.DoApplyUserSett(frmQueueM); frmQueueM.Show(); frmQueueM.Activate(); }

        private void stripMarineInv_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frminvoice();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Show();
            frm.WindowState = WindowState.Maximized;
            frm.Activate();
        }

        private void stripJoinInvs_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmHoldM); MainClass.DoApplyUserSett(frmHoldM); frmHoldM.Show(); frmHoldM.Activate(); }
        private void stripMarineBook_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmBookingM); MainClass.DoApplyUserSett(frmBookingM); frmBookingM.Show(); frmBookingM.Activate(); }

        private void stripMarineCloseCasher_Click(object sender, RoutedEventArgs e)
        {
            var frmPwd = new frmCheckPwd();
            frmPwd.operNo = 1;
            frmPwd.ShowDialog();
            if (!frmPwd.Iscorrect) return;

            var da = new SqlDataAdapter("Select ActiveAuto from SettingEmail where Branch_Id=" + MainClass.BranchNo, this.conn);
            var dt = new DataTable();
            da.Fill(dt);
            if (dt.Rows.Count > 0 && Convert.ToBoolean(dt.Rows[0]["ActiveAuto"]) && !MainClass.CheckForInternetConnection())
            {
                if (MessageBox.Show("   الإنترنت غير متصل بالجهاز, للإستمرار في الإغلاق اضغط نعم .    ", "تحذير",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;
            }

            var frmClose = new frmCloseShift();
            frmClose.Activate();
            if (frmClose.NoInvsFound(2))
            {
                MessageBox.Show("لا يوجد فواتير جديدة يمكن إغلاقها  ");
                this.IsCashierClosed = true;
            }
            else
            {
                frmClose.closeShift(2);
                if (this.IsCashierClosed) this.Close();
            }
        }

        private void stripClosesRpt_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmCloseShift();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Activate();
            frm.ShowDialog();
        }

        private void StripRptRentInvoices_Click(object sender, RoutedEventArgs e) { var frm = new frmRptRentInvoices(); frm.Show(); frm.Activate(); }

        #endregion

        #region Menu Click Events - Projects

        private void stripPMTerm_Click(object sender, RoutedEventArgs e) { var frm = new frmTermsPM(); frm.Show(); frm.Activate(); }
        private void stripPMContractor_Click(object sender, RoutedEventArgs e) { var frm = new frmContractorPM(); frm.Show(); frm.Activate(); }

        private void stripPMClient_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmClientPM();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Title = MainClass.Language == "en-us" ? "New Customer" : "تعريف عميل";
            frm.rdClient.IsChecked = true;
            frm.rdSupplier.IsChecked = false;
            frm.rbOwner.IsChecked = false;
            frm.rdClient.Visibility = Visibility.Collapsed;
            frm.rdSupplier.Visibility = Visibility.Collapsed;
            frm.rbOwner.Visibility = Visibility.Collapsed;
            frm.Show();
            frm.Activate();
        }

        private void stripStagePM_Click(object sender, RoutedEventArgs e) { var frm = new frmProjectStagesPM(); frm.Show(); frm.Activate(); }
        private void stripPMContract_Click(object sender, RoutedEventArgs e) { var frm = new frmContractPM(); frm.Show(); frm.Activate(); }
        private void stripExecutePM_Click(object sender, RoutedEventArgs e) { var frm = new frmAssignProjectPM(); frm.Show(); frm.Activate(); }
        private void StripProjCyclePM_Click(object sender, RoutedEventArgs e) { var frm = new frmProjCyclePM(); frm.Show(); frm.Activate(); }
        private void stripRequirements_Click(object sender, RoutedEventArgs e) { var frm = new frmRequirementPM(); frm.Show(); frm.Activate(); }
        private void stripCashVoucherPM_Click(object sender, RoutedEventArgs e) { var frm = new frmCashVoucherPM(); frm.Show(); frm.Activate(); }
        private void stripPaymentVoucherPM_Click(object sender, RoutedEventArgs e) { var frm = new frmPaymentVoucher(); frm.Show(); frm.Activate(); }

        #endregion

        #region Menu Click Events - Settings

        private void stripFoundation_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmFonudation); MainClass.DoApplyUserSett(frmFonudation); frmFonudation.Show(); frmFonudation.Activate(); }
        private void stripBranch_Click(object sender, RoutedEventArgs e) { var frm = new frmBranches(); MainClass.ApplyPermissionToForm(frm); MainClass.DoApplyUserSett(frm); frm.Show(); frm.Activate(); }
        private void StripZatcaSetting_Click(object sender, RoutedEventArgs e) { var frm = new frmZatcaSetting(); frm.Show(); frm.Activate(); }
        private void stripBackup_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmBackupRstore); MainClass.DoApplyUserSett(frmBackupRstore); frmBackupRstore.Show(); frmBackupRstore.Activate(); }
        private void stripDefaultPrint_Click(object sender, RoutedEventArgs e) { frmInvRptType.ShowDialog(); }
        private void stripMonitorItem_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(dfh); MainClass.DoApplyUserSett(dfh); dfh.Show(); dfh.Activate(); }
        private void stripDataRecycling_Click(object sender, RoutedEventArgs e) { var frm = new frmRecyclingData(); MainClass.ApplyPermissionToForm(frm); MainClass.DoApplyUserSett(frm); frm.Show(); frm.Activate(); }
        private void stripCreateDb_Click(object sender, RoutedEventArgs e) { var frm = new frmDBManagment(); frm.Show(); frm.Activate(); }
        private void mainStripEmailMang_Click(object sender, RoutedEventArgs e) { var frm = new frmEmailMang(); MainClass.ApplyPermissionToForm(frm); MainClass.DoApplyUserSett(frm); frm.Show(); frm.Activate(); }
        private void StripImportData_Click(object sender, RoutedEventArgs e) { var frm = new frmImportDataGeneral(); frm.Show(); frm.Activate(); }
        private void stripMaintainInvs_Click(object sender, RoutedEventArgs e) { var frm = new frmReGenerateEntries(); frm.Show(); frm.Activate(); }
        private void StripOffers_Click(object sender, RoutedEventArgs e) { var frm = new frmOffers(); frm.Show(); frm.Activate(); }
        private void striptoolsendData_Click(object sender, RoutedEventArgs e) { FrmSyncMQtt.Show(); }
        private void AccountingPeriods_Click(object sender, RoutedEventArgs e) { FrmAccountingPeriods.Show(); }
        private void ToolStripMenuItemRestorData_Click(object sender, RoutedEventArgs e)
        {
        }
     //   private void ToolStripMenuItemRestorData_Click(object sender, RoutedEventArgs e) { FrmRestorData.Show(); }
        private void stripAddUser_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmAddUsers); MainClass.DoApplyUserSett(frmAddUsers); frmAddUsers.Show(); frmAddUsers.Activate(); }
        private void stripUserPerm_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmUsersPermissions); MainClass.DoApplyUserSett(frmUsersPermissions); frmUsersPermissions.Show(); frmUsersPermissions.Activate(); }
        private void stripChangePwd_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmChangePwd); MainClass.DoApplyUserSett(frmChangePwd); frmChangePwd.Show(); frmChangePwd.Activate(); }
        private void stripGnrlSetting_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmSettings); MainClass.DoApplyUserSett(frmSettings); frmSettings.Show(); frmSettings.Activate(); }
        private void stripfingDevice_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmFPSSetting); MainClass.DoApplyUserSett(frmFPSSetting); frmFPSSetting.Show(); frmFPSSetting.Activate(); }
        private void ToolStripMenuItemsallahSetting_Click(object sender, RoutedEventArgs e)
        {
        }
        // private void ToolStripMenuItemsallahSetting_Click(object sender, RoutedEventArgs e) { FrmWebhooksetting.Show(); }

            // Sync
        private void StripSynInv_Click(object sender, RoutedEventArgs e) { var frm = new frmInvsSyncStatus(); frm.Show(); frm.Activate(); }
        private void StripSynEntry_Click(object sender, RoutedEventArgs e) { var frm = new frmEntriesSyncStatus(); frm.Show(); frm.Activate(); }
        private void StripSynReceipt_Click(object sender, RoutedEventArgs e) { var frm = new frmReceiptsSyncStatus(); frm.Show(); frm.Activate(); }
        private void stripSyncStocks_Click(object sender, RoutedEventArgs e) { var frm = new frmStocksSync(); MainClass.ApplyPermissionToForm(frm); MainClass.DoApplyUserSett(frm); frm.Show(); frm.Activate(); }
        private void btnSyncData_Click(object sender, RoutedEventArgs e) { var frm = new frmmanagementSync(); MainClass.ApplyPermissionToForm(frm); MainClass.DoApplyUserSett(frm); frm.Show(); frm.Activate(); }
        private void StripPaymentTypeSetting_Click(object sender, RoutedEventArgs e) { var frm = new frmCloudPaymentSetting(); frm.Show(); frm.Activate(); }
        private void StripPriceSetting_Click(object sender, RoutedEventArgs e) { var frm = new frmCloudPriceSetting(); frm.Show(); }
        private void StripZatcaSyncInv_Click(object sender, RoutedEventArgs e) { var frm = new frmInvsSyncStatusZatca(); frm.Show(); frm.Activate(); }
        private void ToolstripDevicesAndroid_Click(object sender, RoutedEventArgs e) { frmDevicesAndroid.Show(); }

        #endregion

        #region Menu Click Events - Egypt EInvoice

        private void StripEgyUploadedInvoice_Click(object sender, RoutedEventArgs e)
        {
        }
      //  private void StripEgyUploadedInvoice_Click(object sender, RoutedEventArgs e) { var frm = new SentEinvoice(); frm.Show(); frm.Activate(); }
        private void StripEtaSetting_Click(object sender, RoutedEventArgs e) { var frm = new frmEtaSetting(); frm.Show(); frm.Activate(); }

        #endregion

        #region Menu Click Events - Support

        private void StripLicense_Click(object sender, RoutedEventArgs e) { var frm = new frmLicenseManagment(); frm.Show(); frm.Activate(); }
        private void stripAbout_Click(object sender, RoutedEventArgs e) { MainClass.ApplyPermissionToForm(frmAbout); MainClass.DoApplyUserSett(frmAbout); frmAbout.Show(); frmAbout.Activate(); }
        private void StripUpdateProgram_Click(object sender, RoutedEventArgs e) { var frm = new FrmUpdate(); frm.Show(); frm.Activate(); }

        private void StripDownLoadTeamViewer_Click(object sender, RoutedEventArgs e)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TeamViewer_Setup_x64.exe");
            if (File.Exists(filePath))
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            else
                MessageBox.Show("ملف التثبيت غير موجود!", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void StripDownLoadAnydesk_Click(object sender, RoutedEventArgs e)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AnyDesk.exe");
            if (File.Exists(filePath))
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            else
                MessageBox.Show("ملف التثبيت غير موجود!", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void OpenTeamViwewr_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string tvPath = "C:\\Program Files\\TeamViewer\\TeamViewer.exe";
                if (File.Exists(tvPath))
                    Process.Start(new ProcessStartInfo(tvPath) { UseShellExecute = true });
                else
                    MessageBox.Show(MainClass.Language == "ar" ? "الرجاء تثبيت البرنامج" : "Please install Teamviewer");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void toolstripdesign_Click(object sender, RoutedEventArgs e)
        {
            if (MainClass.EmpNo == 0)
                new frm_ReportDesign().Show();
            else
                MessageBox.Show("ليس لديك صلاحية");
        }

        private void ToolStripMenuItemYoutube_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.youtube.com/watch?v=MNKA27ZOEcU&list=PLXvZ74gOFEBCcn51FS1oHTLSqCeD_aCue",
                UseShellExecute = true
            });
        }

        private void BtnSupportTicket_Click(object sender, RoutedEventArgs e)
        {
            FormCreateTicket.Show();
        }

        #endregion

        #region Menu Click Events - Salla
        private void ToolStripListProductsalla_Click(object sender, RoutedEventArgs e)
        { }
        private void ToolStripMenuItemGetOrderSallah_Click(object sender, RoutedEventArgs e)
        { }
        private void ToolStripMenuItemSallaSafes_Click(object sender, RoutedEventArgs e)
        { }
        //    private void ToolStripListProductsalla_Click(object sender, RoutedEventArgs e) { FrmSallaProducts.Show(); }
        //     private void ToolStripMenuItemGetOrderSallah_Click(object sender, RoutedEventArgs e) { FrmOrderSalla.Show(); }
        //     private void ToolStripMenuItemSallaSafes_Click(object sender, RoutedEventArgs e) { var frm = new FrmSallaBranchMapping(); frm.Show(); frm.Activate(); }

            #endregion

            #region Quick Access Button Clicks

        private void BtnSales_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvSale();
            frm.InvType = 2;
            frm.ProcType = 1;
            frm.EntryType = 2;
            frm.Tag = "InvSale";
            MainClass.ApplyPermissionToForm(frm);
            frm.cmbProcTypeSrch.SelectedIndex = 0;
            frm.Show();
            frm.WindowState = WindowState.Maximized;
            frm.Activate();
        }

        private void BtnPurches_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvPurch();
            frm.InvType = 1;
            frm.ProcType = 1;
            frm.EntryType = 1;
            frm.Tag = "SalePurch";
            frm.Name = "frmSalePurch";
            frm.txtRefNo.IsReadOnly = false;
            frm.txtRefDate.IsEnabled = true;
            if (LicenseFeatures == "000001")
            {
                frm.lblCostCenter.Visibility = Visibility.Collapsed;
                frm.cmbCostCenter.Visibility = Visibility.Collapsed;
                frm.btnAddCostCenter.Visibility = Visibility.Collapsed;
            }
            MainClass.ApplyPermissionToForm(frm);
            frm.cmbProcType.SelectedIndex = 0;
            frm.cmbProcTypeSrch.SelectedIndex = 0;
            frm.Show();
            frm.WindowState = WindowState.Maximized;
            frm.Activate();
        }

        private void BtnQutation_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmInvSale();
            frm.InvType = 2;
            frm.ProcType = 4;
            frm.Tag = "Quotation";
            MainClass.ApplyPermissionToForm(frm);
            frm.cmbProcTypeSrch.SelectedIndex = 2;
            frm.Title = MainClass.Language == "ar" ? "عرض سعر" : "Quotation";
            frm.Show();
            frm.WindowState = WindowState.Maximized;
            frm.Activate();
        }

        private void BtnaddSupplier_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmCustomers();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Title = MainClass.Language == "ar" ? "تعريف مورد" : "New Supplier";
            frm.Type = 2;
            frm.Show();
            frm.Activate();
        }

        private void BtnAddcustomer_Click(object sender, RoutedEventArgs e)
        {
            var frm = new frmCustomers();
            MainClass.ApplyPermissionToForm(frm);
            MainClass.DoApplyUserSett(frm);
            frm.Title = MainClass.Language == "ar" ? "تعريف عميل" : "New Customer";
            frm.Type = 1;
            frm.Show();
            frm.Activate();
        }

        private void BtnAddItems_Click(object sender, RoutedEventArgs e)
        {
            MainClass.ApplyPermissionToForm(frmItems);
            MainClass.DoApplyUserSett(frmItems);
            frmItems.Show();
            frmItems.Activate();
        }

        #endregion
    }
}