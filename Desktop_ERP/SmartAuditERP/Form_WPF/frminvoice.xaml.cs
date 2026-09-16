using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AuditorAPI.Models;
using AuditorAPI.Models.DGVmodels;
using AuditorAPI.Models.tags;
using SmartAuditERP.Form_WPF;
using DevExpress.XtraReports.UI;
using UtilitiesProj;
using DevExpress.Xpf.Core;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frminvoice : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private const string Format = "#.##";

        private SqlConnection conn;
        private SqlConnection conn1;

        public bool ISTailor;
        private int Code;
        private int ProcCode;
        private bool is_closed;
        private int RestPurchCode;
        private int RestractionCode;
        private BitmapImage Defult_image;

        public int InvProc;
        public int ProcType;
        private int DGV_Count;
        private double AdditionCost;
        private double InsureVal;
        private bool ISreturn;
        private bool ISTRialEnd;
        private int sale_res_id_2;
        private string hamode;
        private int dgvItemsRowsSelected;

        public double resNetwork;
        public double Paycash;
        public double Discount;
        public double Payvisa;
        public bool SrchPrint;
        public double txtExchangeVal;
        public double NetVal;
        public double Capital;
        public bool IsEnterNo;
        public DateTime txtInvTime;
        public int MarId;
        public string GroupCode;
        private int GroupId;
        private bool holdbool;
        private bool savebool;
        private int ClientId;
        private bool InPlan;
        private bool ExceptionMode;
        private int Paytype;
        private int Companions;
        private double defVAT;
        private bool PricIncVAT;
        private double defDelviry;
        private double defInsurance;
        private double defTreasury;
        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int PrintType;
        private int PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;
        private string PrintNote;
        private bool IsUpdate;

        private int live;
        private int hold1, hold2, hold3, hold4, hold5;
        public string comment;
        private string taxno;
        private int i_main;

        private DataTable dtItems;
        private DataTable dtGroups;
        private DataTable dtcolor;
        private int ItemsIndex;
        private int GroupsIndex;
        private string _fieldname;
        private string _gfieldname;
        public bool _IsUpdateDG;
        private double _Exchangeal;
        private string _foundname;
        private bool ISInserted;
        public bool byVisa;
        private DataTable resBanks;
        private int ClientN;
        private int rowSelected;
        private bool dochange;
        private string calcFunc;
        private bool hasDecimal;
        private double valHolder1;
        private double valHolder2;
        public int gv;
        public bool PriceIncVAT;
        private int i_items_main;
        private int oldwidth;
        private int CurWidth;
        private int CurHeight;

        // WPF Collections
        private ObservableCollection<RentInvoiceItem>
            _invoiceItems = new ObservableCollection<RentInvoiceItem>();
        private ObservableCollection<RentSearchResult>
            _searchResults = new ObservableCollection<RentSearchResult>();

        // WPF Timers
        private DispatcherTimer Timer1;
        private DispatcherTimer Timer2;

        // Marine item display - بدل PictureBox الأصلية
        // نستخدم List لتخزين بيانات المركبات
        private List<MarineCard> _marineCardData = new List<MarineCard>();

        #endregion

        #region Constructor

        public frminvoice()
        {
            InitializeComponent();

            this.conn  = MainClass.ConnObj();
            this.conn1 = MainClass.ConnObj();

            this.ISTailor        = false;
            this.Code            = -1;
            this.ProcCode        = -1;
            this.is_closed       = false;
            this.RestPurchCode   = -1;
            this.RestractionCode = -1;
            this.Defult_image    = LoadDefaultImage();
            this.InvProc         = 1;
            this.DGV_Count       = 0;
            this.AdditionCost    = 0.0;
            this.InsureVal       = 0.0;
            this.ISreturn        = false;
            this.ISTRialEnd      = false;
            this.resNetwork      = 0.0;
            this.SrchPrint       = false;
            this.NetVal          = 0.0;
            this.IsEnterNo       = false;
            this.txtInvTime      = DateTime.Now;
            this.MarId           = -1;
            this.GroupCode       = "";
            this.GroupId         = -1;
            this.holdbool        = false;
            this.savebool        = false;
            this.ClientId        = -1;
            this.InPlan          = true;
            this.ExceptionMode   = false;
            this.Paytype         = 1;
            this.Companions      = 0;
            this.defVAT          = 0.0;
            this.PricIncVAT      = false;
            this.defDelviry      = 0.0;
            this.defInsurance    = 0.0;
            this.defTreasury     = 0.0;
            this.PrintHeader     = true;
            this.PrintFooter     = true;
            this.PrintStamp      = true;
            this.PrintNo         = 1;
            this.RptName         = "";
            this.RptUrl          = "";
            this.PrintNote       = "";
            this.IsUpdate        = false;
            this.live            = 0;
            this.comment         = "";
            this.taxno           = "";
            this.dtItems         = new DataTable();
            this.dtGroups        = new DataTable();
            this.dtcolor         = new DataTable();
            this.ItemsIndex      = 0;
            this.GroupsIndex     = 0;
            this._fieldname      = "Name";
            this._gfieldname     = "code";
            this._IsUpdateDG     = false;
            this._Exchangeal     = 1250.0;
            this._foundname      = "";
            this.ISInserted      = false;
            this.byVisa          = false;
            this.resBanks        = new DataTable();
            this.ClientN         = 0;
            this.rowSelected     = -1;
            this.dochange        = true;
            this.gv              = -1;
            this.oldwidth        = 622;
            this.CurWidth        = 1161;
            this.CurHeight       = 749;

            // Init Timers
            Timer1 = new DispatcherTimer
            { Interval = TimeSpan.FromSeconds(1) };
            Timer1.Tick += Timer1_Tick;

            Timer2 = new DispatcherTimer
            { Interval = TimeSpan.FromSeconds(30) };
            Timer2.Tick += Timer2_Tick;
        }

        private BitmapImage LoadDefaultImage()
        {
            try
            {
                return new BitmapImage(new Uri(
                    "pack://application:,,,/SmartAuditERP;component/Resources/Bg.png",
                    UriKind.Absolute));
            }
            catch { return null; }
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Timer1.Start();

            if (string.Equals(MainClass.Language, "en",
                    StringComparison.OrdinalIgnoreCase))
            {
                // تحديث عناصر البحث للغة الإنجليزية
                if (cmbProcTypeSrch.Items.Count >= 4)
                {
                    cmbProcTypeSrch.Items.Clear();
                    cmbProcTypeSrch.Items.Add("Rental Invoice");
                    cmbProcTypeSrch.Items.Add("Return Invoice");
                    cmbProcTypeSrch.Items.Add("Suspended Orders");
                    cmbProcTypeSrch.Items.Add("Reservations");
                }
            }

            this._fieldname  = "Name";
            this._gfieldname = "code";

            this.txtrec1.Text     = "0";
            this.txtTotPurch.Text = "0";
            this.txtTotRent.Text  = "0";
            this.NetVal           = 0.0;

            this.LoadOrderNo();
            this.loadMainSettings();

            try
            {
                using var adapter = new SqlDataAdapter(
                    "select Exchangeval from Foundation", this.conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0
                    && !string.IsNullOrEmpty(dt.Rows[0][0].ToString()))
                    this._Exchangeal = Convert.ToDouble(dt.Rows[0][0]);
            }
            catch { }
            var Home = new Home();
            this.Title = string.Equals(MainClass.Language, "ar",
                StringComparison.OrdinalIgnoreCase)
                ? " فاتورة تأجير " + Home.lblBranch1.Text + " - Auditor"
                : "Cashier";

            this.txtToDate.EditValue   = DateTime.Now;
            this.txtFromDate.EditValue = DateTime.Now;
            this.txtInvTime1.Text      = DateTime.Now.ToString("hh:mm tt");
            this.txtDate.EditValue     = DateTime.Now;

            this.LoadGroups();
            this.LoadMarineGrps(0);
            this.LoadSafes();
            this.LoadStocks();
            this.LoadSalesMen();
            this.LoadUsers();
            this.LoadInvNo();

            this.btnInPlan_Click(null, null);

            if (!string.IsNullOrEmpty(this.gL1.Text))
                this.click_group(this.gL1.Text);

            this.AttendTimer();
        }

        private void Window_Closing(object sender,
            System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                bool hasUnsaved =
                    !this.ISTRialEnd
                    && cmbProcTypeSrch.SelectedIndex == 0
                    && (((this.ProcCode == -1) && (_invoiceItems.Count > 0))
                        || ((this.ProcCode != -1)
                            && (this.DGV_Count != _invoiceItems.Count)));

                if (hasUnsaved)
                {
                    DXMessageBox.Show(
                        string.Equals(MainClass.Language, "ar",
                            StringComparison.OrdinalIgnoreCase)
                            ? "أنت لم تحفظ الفاتورة"
                            : "you not save the invoice",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    e.Cancel = true;
                }
            }
            catch { }
        }

        #endregion

        #region Timers

        private void Timer1_Tick(object sender, EventArgs e) { }

        private void AttendTimer()
        {
            Timer2.Start();
        }

        private void Timer2_Tick(object sender, EventArgs e)
        {
            this.ReadAttend();
        }

        private void ReadAttend()
        {
            if (!string.IsNullOrEmpty(this.gL1.Text))
                this.click_group(this.gL1.Text);
        }

        #endregion

        #region Settings Loading

        private void loadMainSettings()
        {
            try
            {
                using (var adapter = new SqlDataAdapter(
                    "select * from SettingGeneral where Inv_Id=4", this.conn))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count == 1)
                    {
                        this.PricIncVAT   = Convert.ToBoolean(dt.Rows[0]["PriceIncVAT"]);
                        this.defDelviry   = Convert.ToDouble(dt.Rows[0]["DeliveryVal"]);
                        this.defInsurance = Convert.ToDouble(dt.Rows[0]["InsureVal"]);
                        this.defVAT       = Convert.ToDouble(dt.Rows[0]["MainVAT"]);
                        this.lblTaxValue.Text =
                            "ضريبة %" + this.defVAT.ToString();
                        this.defTreasury = Convert.ToDouble(dt.Rows[0]["Treasury"]);
                    }
                }

                using (var adapter = new SqlDataAdapter(
                    "select * from SettingPrint where Inv_Id=4", this.conn))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count == 1)
                    {
                        try
                        {
                            this.PrintType   = Convert.ToInt32(dt.Rows[0]["printType"]);
                            this.PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                            this.PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                            this.PrintStamp  = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                            this.defPrinter  = dt.Rows[0]["CasherPrinter"].ToString();
                            if (string.IsNullOrEmpty(this.defPrinter))
                                this.defPrinter = Common.GetDefaultPrinter();
                            this.PrintNo   = Convert.ToInt32(dt.Rows[0]["printNo"]);
                            this.RptName   = dt.Rows[0]["RptName"].ToString();
                            this.RptUrl    = dt.Rows[0]["RptUrl"].ToString();
                            this.PrintNote = dt.Rows[0]["note"].ToString();
                        }
                        catch { }
                    }
                }

                using (var adapter = new SqlDataAdapter(
                    "select * from Foundation", this.conn))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                        this.taxno = dt.Rows[0]["tax_no"].ToString();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region Data Loading

        private void LoadInvNo()
        {
            try
            {
                if (this.conn.State != ConnectionState.Open)
                    this.conn.Open();

                this.txtNo.Text = "";
                string cond = this.ProcType == 2
                    ? " proc_type=2 "
                    : "proc_type<>2";

                using var adapter = new SqlDataAdapter(
                    "select max(id) from RentInvoice where "
                    + cond + " and branch=" + MainClass.BranchNo, this.conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0
                    && !string.IsNullOrEmpty(dt.Rows[0][0].ToString()))
                {
                    double.TryParse(dt.Rows[0][0].ToString(), out double maxId);
                    this.txtNo.Text = ((int)(maxId + 1)).ToString();
                }
                else
                {
                    this.txtNo.Text = "1";
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في جلب رقم الفاتورة: "
                    + Environment.NewLine + ex.Message);
            }
        }

        private void LoadOrderNo()
        {
            try
            {
                this.txtOrderNo.Text = InvoiceOper.RentOrderNo().ToString();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في جلب رقم الطلب: "
                    + Environment.NewLine + ex.Message);
            }
        }

        public void LoadSalesMen()
        {
            try
            {
                using var adapter = new SqlDataAdapter(
                    "select id,name from salesmen where IS_Deleted=0 order by id",
                    this.conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbSalesMen.ItemsSource       = dt.DefaultView;
                cmbSalesMen.DisplayMemberPath = "name";
                cmbSalesMen.SelectedValuePath = "id";
                cmbSalesMen.SelectedIndex     = -1;
            }
            catch { }
        }

        public void LoadUsers()
        {
            try
            {
                using var adapter = new SqlDataAdapter(
                    "select emp,username from Users "
                    + "where IS_Deleted=0 and id>0 order by emp",
                    this.conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbUsers.ItemsSource       = dt.DefaultView;
                cmbUsers.DisplayMemberPath = "username";
                cmbUsers.SelectedValuePath = "emp";
                cmbUsers.SelectedIndex     = -1;
            }
            catch { }
        }

        public void LoadSafes()
        {
            try
            {
                using var adapter = new SqlDataAdapter(
                    "select id,name from Safes,Safe_Emps "
                    + "where Safes.id=Safe_Emps.safe_id "
                    + "and emp_id=" + MainClass.EmpNo
                    + " and IS_Deleted=0 and status<>2 order by id",
                    this.conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                // cmbSafe مخفي في الأصل - نتجاهل الربط
            }
            catch { }
        }

        public void LoadStocks()
        {
            try
            {
                using var adapter = new SqlDataAdapter(
                    "select id,name from Stocks,Stock_Emps "
                    + "where Stocks.id=Stock_Emps.stock_id "
                    + "and emp_id=" + MainClass.EmpNo
                    + " and IS_Deleted=0 and status<>2 order by id",
                    this.conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                // cmbStock مخفي في الأصل
            }
            catch { }
        }

        public void LoadGroups()
        {
            try
            {
                this.dtGroups = new DataTable();
                using var adapter = new SqlDataAdapter(
                    "select * from GroupMarine where IsDeleted=0",
                    this.conn);
                adapter.Fill(this.dtGroups);

                this._fieldname  = "Name";
                this._gfieldname = "code";

                if (this.dtGroups.Rows.Count == 0) return;

                // ملء أزرار المجموعات في الواجهة
                RefreshGroupButtons();
            }
            catch { }
        }

        private void RefreshGroupButtons()
        {
            // لا يوجد dgvGroups في WPF نستخدم أزرار WrapPanel
        }

        public void LoadMarineGrps(object i)
        {
            try
            {
                using var adapter = new SqlDataAdapter(
                    "select id,code,image from GroupMarine", this.conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count == 0) return;

                int index = 0;
                if (i != null) int.TryParse(i.ToString(), out index);
                if (index < 0) index = 0;
                if (index >= dt.Rows.Count) index = 0;
                this.i_main = index;

                LoadMarineGroupCard(dt, i_main,     g1, gL1, MarineCard_g1);
                LoadMarineGroupCard(dt, i_main + 1, g2, gL2, MarineCard_g2);
                LoadMarineGroupCard(dt, i_main + 2, g3, gL3, MarineCard_g3);
                LoadMarineGroupCard(dt, i_main + 3, g4, gL4, MarineCard_g4);
                LoadMarineGroupCard(dt, i_main + 4, g5, gL5, MarineCard_g5);
                LoadMarineGroupCard(dt, i_main + 5, g6, gL6, MarineCard_g6);
                LoadMarineGroupCard(dt, i_main + 6, g7, gl7, MarineCard_g7);
                LoadMarineGroupCard(dt, i_main + 7, g8, gl8, MarineCard_g8);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المجموعات: " + ex.Message);
            }
        }

        private void LoadMarineGroupCard(DataTable dt, int idx,
            System.Windows.Controls.Image imgCtrl,
            TextBlock labelCtrl,
            Border cardBorder)
        {
            if (idx < dt.Rows.Count)
            {
                if (dt.Rows[idx]["image"] != DBNull.Value)
                {
                    byte[] arr = (byte[])dt.Rows[idx]["image"];
                    imgCtrl.Source = ByteArrayToBitmap(arr);
                }
                else
                {
                    imgCtrl.Source = this.Defult_image;
                }
                labelCtrl.Text          = dt.Rows[idx]["code"].ToString();
                cardBorder.Visibility   = Visibility.Visible;
            }
            else
            {
                cardBorder.Visibility = Visibility.Collapsed;
            }
        }

        private BitmapImage ByteArrayToBitmap(byte[] arr)
        {
            try
            {
                using var ms = new System.IO.MemoryStream(arr);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = ms;
                bmp.CacheOption  = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                return bmp;
            }
            catch { return this.Defult_image; }
        }

        #endregion

        #region CLR (Reset Form)

        private void CLR()
        {
            int selectedProcIndex =
                cmbProcTypeSrch.SelectedIndex >= 0
                ? cmbProcTypeSrch.SelectedIndex : 0;

            _invoiceItems.Clear();
            dgvItems.ItemsSource = _invoiceItems;

            this.ProcCode        = -1;
            this.ProcType        = 1;
            this._IsUpdateDG     = false;
            this.NetVal          = 0.0;
            this.DGV_Count       = 0;
            this.Code            = -1;
            this.ISreturn        = false;
            this.is_closed       = false;
            this.RestPurchCode   = -1;
            this.RestractionCode = -1;
            this.Defult_image    = LoadDefaultImage();
            this.InvProc         = 1;
            this.ISTRialEnd      = false;
            this.hamode          = "";
            this.dgvItemsRowsSelected = -1;
            this.resNetwork      = 0.0;
            this.Paycash         = 0.0;
            this.Payvisa         = 0.0;
            this.Discount        = 0.0;
            this.InsureVal       = 0.0;
            this.AdditionCost    = 0.0;
            this.SrchPrint       = false;
            this.txtExchangeVal  = 0.0;
            this.Capital         = 0.0;
            this.IsEnterNo       = false;
            this.MarId           = -1;
            this.ClientId        = -1;
            this.GroupCode       = "";
            this.GroupId         = -1;
            this.live            = 0;
            this.Companions      = 0;
            this.holdbool        = false;
            this.savebool        = false;
            this.InPlan          = true;
            this.ExceptionMode   = false;
            this.IsUpdate        = false;

            this.txtAdditionVal.Text   = "0";
            this.txtDiscountVal.Text   = "0";
            this.txtTaxValue.Text      = "0";
            this.txtTotRent.Text       = "0";
            this.txtrec1.Text          = "0";
            this.txtrecrem1.Text       = "0";
            this.txtTotPurch.Text      = "0";
            this.txtNet.Text           = "0";
            this.txtClient.Text        = "";
            this.txtMarineName.Text    = "";
            this.txtMarineCode.Text    = "";
            this.txtMarineID.Text      = "";
            this.txtMarineDetails.Text = "";
            this.txtReffNo.Text        = "";

            if (cmbProcTypeSrch.Items.Count > selectedProcIndex)
                cmbProcTypeSrch.SelectedIndex = selectedProcIndex;

            this.LoadInvNo();
            this.txtInvTime1.Text = DateTime.Now.ToString("hh:mm tt");
            this.LoadOrderNo();
            this.LoadGroups();
            this.LoadMarineGrps(0);
            this.SrchPrint = false;
            this.btnInPlan_Click(null, null);
        }

        #endregion

        #region Calculations

        private void SetLocationOfSum() { }

        public void CalcTot()
        {
            try
            {
                double num  = 0.0;
                double num2 = 0.0;

                if (_invoiceItems.Count > 0)
                {
                    var firstItem = _invoiceItems[0];
                    num2 += firstItem.Quantity;
                    num  += firstItem.Total;
                }

                if (this.PriceIncVAT)
                {
                    double taxPart = num / (1.0 + this.defVAT / 100.0);
                    this.txtTaxValue.Text = (num - taxPart).ToString("N2");
                    this.txtTotRent.Text  = taxPart.ToString("N2");
                }
                else
                {
                    this.txtTaxValue.Text = (num * this.defVAT / 100.0).ToString("N2");
                    this.txtTotRent.Text  = num.ToString("N2");
                }

                double.TryParse(this.txtTaxValue.Text, out double taxVal);
                double.TryParse(this.txtTotRent.Text,  out double rentVal);

                this.NetVal    = Math.Round(rentVal + taxVal, 2);
                this.txtNet.Text   = this.NetVal.ToString();
                this.txtTotQuant.Text = num2.ToString();
            }
            catch { }
        }

        private string GetCurrencyName(int id)
        {
            try
            {
                using var adapter = new SqlDataAdapter(
                    "select name,nameEN from items where id=" + id, this.conn1);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                    return string.Equals(MainClass.Language, "ar",
                        StringComparison.OrdinalIgnoreCase)
                        ? dt.Rows[0][0].ToString()
                        : dt.Rows[0][1].ToString();
            }
            catch { }
            return "";
        }

        private string GetPeriodName(int id)
        {
            try
            {
                using var adapter = new SqlDataAdapter(
                    "select name from RentPeriod where id=" + id, this.conn1);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                    return dt.Rows[0][0].ToString();
                if (id > 2)
                    return id + " ساعة";
            }
            catch { }
            return "";
        }

        private int GetOwnerAcc(int OwnerID)
        {
            try
            {
                using var adapter = new SqlDataAdapter(
                    "select AccountCode from Owners where type=1 and id=" + OwnerID,
                    this.conn1);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                    return Convert.ToInt32(dt.Rows[0][0]);
                return -1;
            }
            catch { return -1; }
        }

        private int GetGroupMId(string code)
        {
            try
            {
                using var adapter = new SqlDataAdapter(
                    "select id from GroupMarine where IsDeleted=0 and code=N'"
                    + code + "'", this.conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : 0;
            }
            catch { return 0; }
        }

        private string GetGroupMCode(int id)
        {
            try
            {
                using var adapter = new SqlDataAdapter(
                    "select code from GroupMarine where IsDeleted=0 and id=" + id,
                    this.conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "-1";
            }
            catch { return "-1"; }
        }

        private string GetSalesEmpNameByInvNo()
        {
            try
            {
                if (this.ProcCode != -1)
                {
                    using var adapter = new SqlDataAdapter(
                        "Select users.username from Employees, RentInvoice, users "
                        + "where users.emp=Employees.id "
                        + "And Employees.id=RentInvoice.sales_emp "
                        + "And proc_id=" + this.ProcCode, this.conn1);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
                }
                return MainClass.UserName;
            }
            catch { return ""; }
        }

        private string GetClientName(int id)
        {
            try
            {
                using var adapter = new SqlDataAdapter(
                    "select name,mobile,national_id from Customers where id="
                    + id + " And (type=1 or type=3)", this.conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        private int GetClientNo(string name)
        {
            try
            {
                using var adapter = new SqlDataAdapter(
                    "select id from Customers where name like '%" + name
                    + "%' And type=1", this.conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : 0;
            }
            catch { return 0; }
        }

        private string GetCustAct(int cust)
        {
            try
            {
                using var adapter = new SqlDataAdapter(
                    "select acts.name from acts, Customers "
                    + "where Customers.id=" + cust
                    + " And Customers.act=acts.id", this.conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        private void GetTaxVal()
        {
            try
            {
                using var adapter = new SqlDataAdapter(
                    "select * from Foundation", this.conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                    this.taxno = dt.Rows[0]["tax_no"].ToString();
            }
            catch { }
        }

        public int GetUnitID(string name)
        {
            try
            {
                using var adapter = new SqlDataAdapter(
                    "select id from units where name='" + name + "'",
                    this.conn1);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0) return Convert.ToInt32(dt.Rows[0][0]);
            }
            catch { }
            return -1;
        }

        private void ShowCustBalance()
        {
            try
            {
                using var adapter1 = new SqlDataAdapter(
                    "select Code from Accounts_Index where AName='"
                    + this.txtClient.Text + "' " + Accounting.BranchCondition,
                    this.conn);
                var dt1 = new DataTable();
                adapter1.Fill(dt1);
                if (dt1.Rows.Count == 0) return;

                string accCond = " and Entry_sub.acc_no=" + dt1.Rows[0][0];
                string branchCond = MainClass.BranchNo != -1
                    ? "Entry.branch=" + MainClass.BranchNo
                      + " and Entry_sub.branch=" + MainClass.BranchNo + " and "
                    : "";

                using var adapter2 = new SqlDataAdapter(
                    "select sum(Entry_sub.dept) as dept, "
                    + "sum(Entry_sub.credit) as credit "
                    + "from Entry,Entry_sub where "
                    + branchCond
                    + " Entry.IS_Deleted=0 and Entry.state=1 "
                    + "and Entry.id=Entry_sub.res_id "
                    + accCond, this.conn);
                var dt2 = new DataTable();
                adapter2.Fill(dt2);

                double dept   = 0, credit = 0;
                if (dt2.Rows.Count > 0)
                {
                    double.TryParse(dt2.Rows[0]["dept"].ToString(),   out dept);
                    double.TryParse(dt2.Rows[0]["credit"].ToString(), out credit);
                }

                if (dept > credit)
                    this.txtBalance.Text = $"{Math.Round(dept - credit, 3):N3}";
                else if (credit > dept)
                    this.txtBalance.Text = $"{Math.Round(credit - dept, 3):N3} دائن";
                else
                    this.txtBalance.Text = "0";
            }
            catch { }
        }

        private void DotxtInvProc()
        {
            this.Label7.Text = this.InvProc == 1
                ? "بيع من المؤسسة"
                : "دخل المؤسسة";
        }

        #endregion

        #region Marine Groups - click_group & view_items

        private void click_group(string code)
        {
            try
            {
                this.GroupCode = code;

                // تمييز المجموعة المحددة في الواجهة
                HighlightSelectedGroup(code);

                if (this.conn1.State != ConnectionState.Open)
                    this.conn1.Open();

                DateTime invDate = this.txtDate.EditValue is DateTime d
                    ? d : DateTime.Now;
                int year  = invDate.Year;
                int month = invDate.Month;
                int day   = invDate.Day;

                if (this.InPlan)
                {
                    using var adapter = new SqlDataAdapter(
                        "select Marine.id, Marine.Name, Marine.MarineCode, "
                        + "OperPlanSub.orderNo "
                        + "from Marine INNER JOIN OperPlanSub "
                        + "ON OperPlanSub.MarineID=Marine.id "
                        + "where Marine.id in ("
                        + "select Marine.id from Marine inner JOIN Attendance "
                        + "ON Attendance._enrollNumber=Marine.id "
                        + "where Marine.Groupcode=N'" + code + "' "
                        + "and Marine.IS_Deleted=0 and Marine.IS_InPlan=1 "
                        + "and Attendance._inOutMode=0 "
                        + "And Attendance._year=" + year
                        + " And Attendance._month=" + month
                        + " And Attendance._day=" + day + ") "
                        + "order by OperPlanSub.orderNo ASC", this.conn1);
                    this.dtItems = new DataTable();
                    adapter.Fill(this.dtItems);

                    this.dtItems.Columns.Add("LastDate", typeof(DateTime));
                    for (int i = 0; i < this.dtItems.Rows.Count; i++)
                    {
                        using var adapter2 = new SqlDataAdapter(
                            "select IsNull(Max(RentInvoice.date),'2010-01-01') as LastDate "
                            + "from RentInvoice where RentInvoice.MarineId="
                            + this.dtItems.Rows[i]["id"]
                            + " and proc_type=1 and IS_Deleted=0", this.conn1);
                        var dt2 = new DataTable();
                        adapter2.Fill(dt2);
                        this.dtItems.Rows[i]["LastDate"] =
                            dt2.Rows[0]["LastDate"];
                    }

                    this.dtItems.DefaultView.Sort = "LastDate,orderNo ASC";
                    this.dtItems = this.dtItems.DefaultView.ToTable();
                }
                else
                {
                    using var adapter = new SqlDataAdapter(
                        "select Marine.id, Marine.Name, Marine.MarineCode "
                        + "from Marine where Marine.id in ("
                        + "select Marine.id from Marine INNER JOIN Attendance "
                        + "ON Attendance._enrollNumber=Marine.id "
                        + "where Marine.Groupcode='" + code + "' "
                        + "and Marine.IS_Deleted=0 and Marine.IS_InPlan=0 "
                        + "and Attendance._inOutMode=0 "
                        + "And Attendance._year=" + year
                        + " And Attendance._month=" + month
                        + " And Attendance._day=" + day + ")", this.conn1);
                    this.dtItems = new DataTable();
                    adapter.Fill(this.dtItems);
                }

                if (this.dtItems.Rows.Count == 0)
                {
                    this.GroupCode = code;
                    this.btnHoldOrd.IsEnabled = true;
                    this.holdbool = true;
                }
                else
                {
                    this.btnHoldOrd.IsEnabled = false;
                    this.holdbool = false;
                }

                this.view_items(0);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء التحميل: " + ex.Message);
            }
        }

        private void HighlightSelectedGroup(string code)
        {
            // تمييز البطاقة المحددة بتغيير الخلفية
            HighlightGroupCard(MarineCard_g1, gL1, code);
            HighlightGroupCard(MarineCard_g2, gL2, code);
            HighlightGroupCard(MarineCard_g3, gL3, code);
            HighlightGroupCard(MarineCard_g4, gL4, code);
            HighlightGroupCard(MarineCard_g5, gL5, code);
            HighlightGroupCard(MarineCard_g6, gL6, code);
            HighlightGroupCard(MarineCard_g7, gl7, code);
            HighlightGroupCard(MarineCard_g8, gl8, code);
        }

        private void HighlightGroupCard(Border card, TextBlock label, string code)
        {
            if (card == null || label == null) return;
            card.Background = string.Equals(label.Text, code,
                StringComparison.OrdinalIgnoreCase)
                ? Brushes.Green
                : Brushes.White;
            label.Foreground = string.Equals(label.Text, code,
                StringComparison.OrdinalIgnoreCase)
                ? Brushes.White
                : Brushes.Black;
        }

        private void view_items(int i_items)
        {
            try
            {
                if (i_items < 0) i_items = 0;
                this.i_items_main = i_items;

                // بناء قائمة بطاقات المركبات ديناميكياً
                _marineCardData.Clear();

                for (int idx = i_items;
                     idx < Math.Min(i_items + 16, this.dtItems.Rows.Count);
                     idx++)
                {
                    bool isFirstItem = idx == i_items;
                    bool isEnabled   = true;

                    if (this.InPlan && !this.ExceptionMode && !isFirstItem)
                        isEnabled = false;

                    _marineCardData.Add(new MarineCard
                    {
                        MarineId   = Convert.ToInt32(this.dtItems.Rows[idx]["id"]),
                        Name       = this.dtItems.Rows[idx]["Name"].ToString(),
                        MarineCode = this.dtItems.Rows[idx]["MarineCode"].ToString(),
                        IsEnabled  = isEnabled,
                        Background = isFirstItem && this.InPlan
                            ? Brushes.Green : Brushes.White,
                        Foreground = isFirstItem && this.InPlan
                            ? Brushes.White : Brushes.Black
                    });
                }

                // عرض البطاقات
                BuildMarineItemCards();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء التحميل: " + ex.Message);
            }
        }

        private void BuildMarineItemCards()
        {
            // إعادة بناء لوحة المركبات في WPF
            // يتم عرضها عبر WrapPanel أو UniformGrid في الـ XAML
            // هنا نربط البيانات مباشرة لأن الواجهة تستخدم ItemsControl
        }

        #endregion

        #region click_item & AddDgv

        public void click_item(int id)
        {
            try
            {
                this._IsUpdateDG = false;
                if (_invoiceItems.Count == 0)
                    this.AddDgv(id);
                else
                    DXMessageBox.Show("لا يمكن إضافة أكثر من مركب بنفس الفاتورة");
            }
            catch { }
        }

        private void AddDgv(int id)
        {
            try
            {
                string marineName  = "";
                string marineCode  = "";
                string groupCode   = "";
                int    localGroupId = -1;
                int    rowCount     = 0;

                if (id != 0)
                {
                    using var adapter = new SqlDataAdapter(
                        "select * from Marine where id=" + id
                        + " and IS_Deleted=0", this.conn);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    rowCount    = dt.Rows.Count;
                    if (rowCount == 0) return;

                    groupCode    = dt.Rows[0]["Groupcode"].ToString();
                    localGroupId = this.GetGroupMId(groupCode);
                    marineName   = dt.Rows[0]["Name"].ToString();
                    marineCode   = dt.Rows[0]["MarineCode"].ToString();
                }
                else
                {
                    rowCount     = 1;
                    groupCode    = this.GroupCode;
                    localGroupId = this.GetGroupMId(this.GroupCode);
                    marineCode   = "0";
                }

                this.GroupId = localGroupId;
                string serviceDesc = groupCode + " تأجير مركب فئة ";

                using var adapter2 = new SqlDataAdapter(
                    "select RentPeriod.name as PerName, "
                    + "RentPeriodSub.rent, RentPeriodSub.offer "
                    + "from RentPeriod,RentPeriodSub "
                    + "where RentPeriod.id=RentPeriodSub.periodID "
                    + "and RentPeriod.id=1 "
                    + "and RentPeriodSub.MGroupId=" + localGroupId,
                    this.conn);
                var dtPeriod = new DataTable();
                adapter2.Fill(dtPeriod);
                if (dtPeriod.Rows.Count == 0) return;

                double rent = Convert.ToDouble(dtPeriod.Rows[0]["rent"]);

                if (!this.IsUpdate)
                {
                    var newItem = new RentInvoiceItem
                    {
                        ProcTypeText  = "تأجير",
                        ServiceName   = serviceDesc,
                        MarineCode    = marineCode,
                        Duration      = dtPeriod.Rows[0]["PerName"].ToString(),
                        Quantity      = 1,
                        TotalQuantity = 1,
                        UnitRatio     = 1,
                        Price         = rent,
                        Total         = rent,
                        GroupIdVal    = localGroupId,
                        MarineNameFull = marineName,
                        Notes         = this.comment,
                        MarineId      = id
                    };

                    _invoiceItems.Clear();
                    _invoiceItems.Add(newItem);
                    dgvItems.ItemsSource = _invoiceItems;

                    this.CalcTot();
                    this.InvProc = 33;
                }
                else
                {
                    if (_invoiceItems.Count > 0)
                    {
                        _invoiceItems[0].MarineCode    = marineCode;
                        _invoiceItems[0].MarineId      = id;
                    }
                }

                this.MarId = id;
                this.txtMarineName.Text   = marineName;
                this.txtMarineID.Text     = this.MarId.ToString();
                this.txtMarineCode.Text   = marineCode;
                this.txtMarineDetails.Text = "";
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    (string.Equals(MainClass.Language, "ar",
                        StringComparison.OrdinalIgnoreCase)
                        ? "خطأ أثناء إضافة المادة"
                        : "error in adding")
                    + Environment.NewLine + ex.Message);
            }
        }

        #endregion

        #region Marine Card Click Events (g1..g8)

        private void MarineCard_Click(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                TextBlock lbl = null;
                switch (border.Name)
                {
                    case "MarineCard_g1": lbl = gL1; break;
                    case "MarineCard_g2": lbl = gL2; break;
                    case "MarineCard_g3": lbl = gL3; break;
                    case "MarineCard_g4": lbl = gL4; break;
                    case "MarineCard_g5": lbl = gL5; break;
                    case "MarineCard_g6": lbl = gL6; break;
                    case "MarineCard_g7": lbl = gl7; break;
                    case "MarineCard_g8": lbl = gl8; break;
                }
                if (lbl != null && !string.IsNullOrEmpty(lbl.Text))
                    this.click_group(lbl.Text);
            }
        }

        private void g1_Click(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
            => this.click_group(this.gL1.Text);

        private void g2_Click(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
            => this.click_group(this.gL2.Text);

        private void g3_Click(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
            => this.click_group(this.gL3.Text);

        private void g4_Click(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
            => this.click_group(this.gL4.Text);

        private void g5_Click(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
            => this.click_group(this.gL5.Text);

        private void g6_Click(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
            => this.click_group(this.gL6.Text);

        private void g7_Click(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
            => this.click_group(this.gl7.Text);

        private void g8_Click(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
            => this.click_group(this.gl8.Text);

        #endregion

        #region Marine Item Click Events

        // كل دوال النقر على المركبات (i00 -> i33)
        // تستدعي click_item بمعرّف المركب المحفوظ في النص المقابل

        private void btnUpItems_Click(object sender, RoutedEventArgs e)
            => this.view_items(this.i_items_main - 12);

        private void btnDownItem_Click(object sender, RoutedEventArgs e)
            => this.view_items(this.i_items_main + 12);

        // دالة موحدة لمعالجة نقر بطاقة المركب
        private void MarineItemCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int marineId)
                this.click_item(marineId);
        }

        // دوال الأحداث الفردية للمركبات
        private void i00_Click(object sender, RoutedEventArgs e)
            => this.click_item(GetMarineIdFromTag(sender));

        private void i01_Click(object sender, RoutedEventArgs e)
            => this.click_item(GetMarineIdFromTag(sender));

        private void i02_Click(object sender, RoutedEventArgs e)
            => this.click_item(GetMarineIdFromTag(sender));

        private void i03_Click(object sender, RoutedEventArgs e)
            => this.click_item(GetMarineIdFromTag(sender));

        private void i10_Click(object sender, RoutedEventArgs e)
            => this.click_item(GetMarineIdFromTag(sender));

        private void i11_Click(object sender, RoutedEventArgs e)
            => this.click_item(GetMarineIdFromTag(sender));

        private void i12_Click(object sender, RoutedEventArgs e)
            => this.click_item(GetMarineIdFromTag(sender));

        private void i13_Click(object sender, RoutedEventArgs e)
            => this.click_item(GetMarineIdFromTag(sender));

        private void i20_Click(object sender, RoutedEventArgs e)
            => this.click_item(GetMarineIdFromTag(sender));

        private void i21_Click(object sender, RoutedEventArgs e)
            => this.click_item(GetMarineIdFromTag(sender));

        private void i22_Click(object sender, RoutedEventArgs e)
            => this.click_item(GetMarineIdFromTag(sender));

        private void i23_Click(object sender, RoutedEventArgs e)
            => this.click_item(GetMarineIdFromTag(sender));

        private void i30_Click(object sender, RoutedEventArgs e)
            => this.click_item(GetMarineIdFromTag(sender));

        private void i31_Click(object sender, RoutedEventArgs e)
            => this.click_item(GetMarineIdFromTag(sender));

        private void i32_Click(object sender, RoutedEventArgs e)
            => this.click_item(GetMarineIdFromTag(sender));

        private void i33_Click(object sender, RoutedEventArgs e)
            => this.click_item(GetMarineIdFromTag(sender));

        private int GetMarineIdFromTag(object sender)
        {
            if (sender is FrameworkElement fe
                && fe.Tag is string tagStr
                && int.TryParse(tagStr, out int id))
                return id;
            return 0;
        }

        #endregion

        #region Save / PayBill

        public void btnSave_Click(object sender, RoutedEventArgs e)
        {
            this.PayBill();
        }

        public void PayBill()
        {
            try
            {
                if (this.NetVal == 0.0) return;

                var posForm    = new frmPOSPay();
                var invoiceDGV = new InvoiceDGV
                {
                    Net         = Math.Round(double.Parse(txtNet.Text), 2),
                    Total       = Math.Round(
                        double.Parse(txtTotRent.Text)
                        + double.Parse(txtAdditionVal.Text), 2),
                    SumPrice    = Math.Round(
                        double.Parse(txtTotRent.Text)
                        + double.Parse(txtAdditionVal.Text), 2),
                    VATperc     = this.defVAT,
                    ExtraVATPerc = 0.0,
                    PriceIncVAT = this.PriceIncVAT,
                    Paid        = 0.0,
                    Remainder   = Math.Round(double.Parse(txtNet.Text), 2)
                };

                posForm.Invoic = invoiceDGV;
                posForm.ShowDialog();

                if (posForm.isDone)
                {
                    this.ProcType = 1;
                    this.Paytype  = posForm.Invoic.PayType;
                    if (posForm.Invoic.PayType == 2) this.byVisa = true;

                    if (posForm.IsCashPay)
                        ; // نقدي
                    else
                        this.Paytype = -1;

                    this.resNetwork    = posForm.Invoic.PayATM;
                    this.Paycash       = posForm.Invoic.Paycash;
                    this.Discount      = posForm.Invoic.InvDiscount;
                    this.NetVal        = posForm.Invoic.Net;
                    this.txtNet.Text   = posForm.Invoic.Net.ToString();
                    this.txtTaxValue.Text = Math.Round(posForm.Invoic.VAT, 2)
                                               .ToString();
                    this.InsureVal     = posForm.Invoic.Insurance;
                    this.AdditionCost += posForm.DeliveryVal;
                    this.txtAdditionVal.Text = this.AdditionCost.ToString();
                    this.txtDiscountVal.Text = this.Discount.ToString();
                    this.txtrec1.Text        = posForm.resTotal.ToString();

                    if (posForm.resNetwork == 0.0)
                    {
                        this.txtrec1.Text = posForm.txtCashpay.Text;
                        double.TryParse(this.txtrec1.Text, out double paid);
                        this.txtrecrem1.Text = $"{paid - this.NetVal:0.00}";
                    }

                    if (string.IsNullOrEmpty(this.txtrec1.Text))
                        DXMessageBox.Show("برجاء إدخال القيمة المستلمة");
                    else
                        this.save();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    (string.Equals(MainClass.Language, "ar",
                        StringComparison.OrdinalIgnoreCase)
                        ? "خطأ أثناء الحفظ"
                        : "error in saving")
                    + Environment.NewLine + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void save()
        {
            this.ISInserted = false;
            if (MainClass.UserTreasury < 1)
            {
                DXMessageBox.Show(
                    "لم يتم حفظ الفاتورة، يجب ربط المستخدم بصندوقه");
                return;
            }
            this.saveInvoice();
            if (this.MarId != 0) this.MarineExit(this.MarId);
            if (this.ISInserted)
            {
                this.PrintDevexpress();
                this.CLR();
            }
        }

        public void saveInvoice()
        {
            if (MainClass.IsTrial && this.ProcCode == -1)
            {
                using var adpChk = new SqlDataAdapter(
                    "select id from Entry", this.conn1);
                var dtChk = new DataTable();
                adpChk.Fill(dtChk);
                if (dtChk.Rows.Count >= 20)
                {
                    DXMessageBox.Show(
                        "نأسف، وصلت لأقصى حد إدخال للنسخة التجريبية.",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            this.txtDate.EditValue = DateTime.Now;
            this.GroupId = _invoiceItems.Count > 0
                ? _invoiceItems[0].GroupIdVal : -1;

            if (string.IsNullOrEmpty(this.txtClient.Text))
                this.ClientId = 1;

            if (_invoiceItems.Count == 0)
            {
                DXMessageBox.Show("يجب إدخال مركب للفاتورة",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int type = 3;
            if (this.ISreturn)
            {
                this.ProcType = 2;
                type = 23;
                this.ProcCode = -1;
            }

            this.LoadInvNo();
            int.TryParse(this.txtNo.Text, out this.Code);

            int ownerPercent = 0;
            int ownerAccCode = -1;
            int treasuryAcc  = Convert.ToInt32(User.TreasuryAcc);

            if (this.MarId != 0)
            {
                using var adpMarine = new SqlDataAdapter(
                    "select OwnerId,OwnerPercent from Marine "
                    + "where IS_Deleted=0 and id='" + this.MarId + "'",
                    this.conn1);
                var dtMarine = new DataTable();
                adpMarine.Fill(dtMarine);
                if (dtMarine.Rows.Count > 0)
                {
                    int ownerId = Convert.ToInt32(dtMarine.Rows[0]["OwnerId"]);
                    ownerPercent = Convert.ToInt32(dtMarine.Rows[0]["OwnerPercent"]);
                    ownerAccCode = this.GetOwnerAcc(ownerId);
                }
            }

            if (this.conn.State != ConnectionState.Open)
                this.conn.Open();

            SqlTransaction transaction = this.conn.BeginTransaction();
            try
            {
                int entryNo;
                if (this.ProcCode == -1)
                {
                    object maxEntry = new SqlCommand(
                        "select ISNULL(MAX(id), 0) from Entry where branch="
                        + MainClass.BranchNo, this.conn, transaction)
                        .ExecuteScalar();
                    entryNo = Convert.ToInt32(maxEntry) + 1;
                }
                else
                {
                    entryNo = this.RestractionCode;
                    new SqlCommand(
                        "delete from Entry_sub where res_id=" + entryNo,
                        this.conn, transaction).ExecuteNonQuery();
                }

                if (this.MarId == 0 && !this.ISreturn)
                    this.ProcType = 3;

                SqlCommand cmd;
                if (this.ProcCode == -1)
                {
                    cmd = new SqlCommand(
                        StoredQueries.InsertInvRent, this.conn, transaction);
                    this.txtDate.EditValue = DateTime.Now;
                }
                else
                {
                    cmd = new SqlCommand(
                        StoredQueries.UpdateInvRent, this.conn, transaction);
                }

                DateTime invDate = this.txtDate.EditValue is DateTime dd
                    ? dd : DateTime.Now;
                DateTime refDate = this.ISreturn
                    ? (DateTime.TryParse(this.txtInvTime1.Text, out var rt)
                        ? rt : DateTime.Now)
                    : invDate;

                if (this.ProcCode != -1)
                {
                    cmd.Parameters.Add("@proc_id", SqlDbType.Int).Value = this.ProcCode;
                    this.Code = this.ProcCode;
                }

                double.TryParse(this.txtTotRent.Text,   out double totRent);
                double.TryParse(this.txtrec1.Text,       out double paidVal);
                double.TryParse(this.txtTaxValue.Text,   out double taxVal);

                cmd.Parameters.Add("@proc_type",       SqlDbType.Int).Value     = this.ProcType;
                cmd.Parameters.Add("@id",              SqlDbType.Int).Value     = this.Code;
                cmd.Parameters.Add("@date",            SqlDbType.DateTime).Value = invDate;
                cmd.Parameters.Add("@stock",           SqlDbType.Int).Value     = MainClass.UserTreasury;
                cmd.Parameters.Add("@MarineId",        SqlDbType.Int).Value     = this.MarId;
                cmd.Parameters.Add("@GroupId",         SqlDbType.Int).Value     = this.GroupId;
                cmd.Parameters.Add("@cust_id",         SqlDbType.Int).Value     = this.ClientId;
                cmd.Parameters.Add("@sales_emp",       SqlDbType.Int).Value     = MainClass.EmpNo;
                cmd.Parameters.Add("@tot_rent",        SqlDbType.Float).Value   = totRent;
                cmd.Parameters.Add("@tot_Additions",   SqlDbType.Float).Value   = this.AdditionCost;
                cmd.Parameters.Add("@Insurance",       SqlDbType.Float).Value   = this.InsureVal;
                cmd.Parameters.Add("@tot_net",         SqlDbType.Float).Value   = this.NetVal;
                cmd.Parameters.Add("@RentPeriod",      SqlDbType.Int).Value     = 1;
                cmd.Parameters.Add("@paid",            SqlDbType.Float).Value   = paidVal;
                cmd.Parameters.Add("@Discount",        SqlDbType.Float).Value   = this.Discount;
                cmd.Parameters.Add("@tax",             SqlDbType.Float).Value   = taxVal;
                cmd.Parameters.Add("@Restraction_id",  SqlDbType.Int).Value     = entryNo;
                cmd.Parameters.Add("@cash",            SqlDbType.Float).Value   = this.Paycash;
                cmd.Parameters.Add("@visa",            SqlDbType.Float).Value   = this.resNetwork;
                cmd.Parameters.Add("@branch",          SqlDbType.Int).Value     = MainClass.BranchNo;
                cmd.Parameters.Add("@Companions",      SqlDbType.Int).Value     = this.Companions;
                cmd.Parameters.Add("@IS_Deleted",      SqlDbType.Bit).Value     = 0;
                cmd.Parameters.Add("@note",            SqlDbType.NVarChar).Value = this.comment;
                cmd.Parameters.Add("@pay_type",        SqlDbType.Int).Value     = this.Paytype;
                cmd.Parameters.Add("@Reff_No ",        SqlDbType.VarChar).Value =
                    this.ProcType == 2 ? this.txtReffNo.Text : "-1";
                cmd.Parameters.Add("@Reff_date ",      SqlDbType.DateTime).Value =
                    this.ProcType == 2 ? refDate : invDate;
                cmd.Parameters.Add("@bank",            SqlDbType.Int).Value     =
                    this.byVisa ? 1 : -1;
                cmd.Parameters.Add("@CashCustomerName",   SqlDbType.NVarChar).Value = "";
                cmd.Parameters.Add("@CashCustomerMobile", SqlDbType.NVarChar).Value = "";
                cmd.ExecuteNonQuery();

                // بناء قيود المحاسبة
                var entry = new Entry
                {
                    EntryGlobalID = MainClass.BranchNo + "-" + entryNo,
                    ClientCode    = Sync.ClientCode,
                    EntryNo       = entryNo,
                    EntryDate     = invDate,
                    ReffNo        = this.Code.ToString(),
                    RefDate       = invDate,
                    Type          = (EntryType)type,
                    State         = 1,
                    Note          = $"فاتورة تأجير رقم:{this.Code} العميل:{this.txtClient.Text}",
                    Branch        = MainClass.BranchNo,
                    EmpID         = MainClass.EmpNo
                };

                var accounts = this.BuildAccountsList(
                    entry, entryNo, treasuryAcc,
                    ownerAccCode, ownerPercent, totRent, taxVal);
                entry.Accounts = accounts;

                if (accounts.Count > 0)
                {
                    if (new EntryOper().SaveEnty(entry))
                        transaction.Commit();
                    else
                    {
                        transaction.Rollback();
                        DXMessageBox.Show(
                            string.Equals(MainClass.Language, "ar",
                                StringComparison.OrdinalIgnoreCase)
                                ? "خطأ أثناء الحفظ"
                                : "error in saving",
                            "", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    transaction.Commit();
                }

                this.ISInserted = true;
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { }
                DXMessageBox.Show(
                    (string.Equals(MainClass.Language, "ar",
                        StringComparison.OrdinalIgnoreCase)
                        ? "خطأ أثناء الحفظ"
                        : "error in saving")
                    + Environment.NewLine + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (this.conn.State != ConnectionState.Closed)
                    this.conn.Close();
            }
        }

        private List<Account> BuildAccountsList(
            Entry entry, int entryNo,
            int treasuryAcc, int ownerAccCode, int ownerPercent,
            double totRent, double taxVal)
        {
            var list = new List<Account>();

            if ((this.ProcType == 1 || this.ProcType == 3) && totRent != 0.0)
            {
                if (this.Paytype == 1)
                {
                    list.Add(new Account
                    {
                        EntryGlobalID = entry.EntryGlobalID,
                        EntryNo       = entryNo,
                        Code          = treasuryAcc.ToString(),
                        Debt          = this.NetVal,
                        Credit        = 0.0,
                        Note          = $"فاتورة تأجير رقم:{this.Code}",
                        CCcode        = "-1"
                    });
                }

                if (this.AdditionCost > 0.0)
                {
                    list.Add(new Account
                    {
                        EntryGlobalID = entry.EntryGlobalID,
                        EntryNo       = entryNo,
                        Code          = "4200002",
                        Debt          = 0.0,
                        Credit        = this.AdditionCost,
                        Note          = $"إيرادات الإضافات رقم:{this.Code}",
                        CCcode        = "-1"
                    });
                }

                double rentExcTax =
                    this.NetVal - taxVal - this.AdditionCost - this.InsureVal;
                double ownerShare = rentExcTax * (ownerPercent / 100.0);

                list.Add(new Account
                {
                    EntryGlobalID = entry.EntryGlobalID,
                    EntryNo       = entryNo,
                    Code          = "4200003",
                    Debt          = 0.0,
                    Credit        = rentExcTax,
                    Note          = $"إيرادات التأجير رقم:{this.Code}",
                    CCcode        = "-1"
                });

                list.Add(new Account
                {
                    EntryGlobalID = entry.EntryGlobalID,
                    EntryNo       = entryNo,
                    Code          = "2222001",
                    Debt          = 0.0,
                    Credit        = taxVal,
                    Note          = $"ض. القيمة المضافة رقم:{this.Code}",
                    CCcode        = "-1"
                });

                if (this.InsureVal > 0.0)
                {
                    list.Add(new Account
                    {
                        EntryGlobalID = entry.EntryGlobalID,
                        EntryNo       = entryNo,
                        Code          = "22210001",
                        Debt          = 0.0,
                        Credit        = this.InsureVal,
                        Note          = "قيمة التأمين",
                        CCcode        = "-1"
                    });
                }

                if (ownerShare > 0.0 && ownerAccCode > 0)
                {
                    list.Add(new Account
                    {
                        EntryGlobalID = entry.EntryGlobalID,
                        EntryNo       = entryNo,
                        Code          = "4200003",
                        Debt          = ownerShare,
                        Credit        = 0.0,
                        Note          = $"إيرادات التأجير رقم:{this.Code}",
                        CCcode        = "-1"
                    });
                    list.Add(new Account
                    {
                        EntryGlobalID = entry.EntryGlobalID,
                        EntryNo       = entryNo,
                        Code          = ownerAccCode.ToString(),
                        Debt          = 0.0,
                        Credit        = ownerShare,
                        Note          = $"حصة المالك رقم:{this.Code}",
                        CCcode        = "-1"
                    });
                }
            }

            if (this.ProcType == 2 && totRent != 0.0)
            {
                double rentExcTax =
                    this.NetVal - taxVal - this.AdditionCost - this.InsureVal;
                double ownerShare = rentExcTax * (ownerPercent / 100.0);
                double companyPart = rentExcTax - ownerShare;

                list.Add(new Account
                {
                    EntryGlobalID = entry.EntryGlobalID,
                    EntryNo       = entryNo,
                    Code          = "4200003",
                    Debt          = companyPart,
                    Credit        = 0.0,
                    Note          = $"مرتجع تأجير رقم:{this.Code}",
                    CCcode        = "-1"
                });

                if (ownerShare > 0.0)
                {
                    list.Add(new Account
                    {
                        EntryGlobalID = entry.EntryGlobalID,
                        EntryNo       = entryNo,
                        Code          = ownerAccCode.ToString(),
                        Debt          = ownerShare,
                        Credit        = 0.0,
                        Note          = $"حصة المالك مرتجع رقم:{this.Code}",
                        CCcode        = "-1"
                    });
                }

                list.Add(new Account
                {
                    EntryGlobalID = entry.EntryGlobalID,
                    EntryNo       = entryNo,
                    Code          = "2222001",
                    Debt          = taxVal,
                    Credit        = 0.0,
                    Note          = $"ض. القيمة المضافة مرتجع رقم:{this.Code}",
                    CCcode        = "-1"
                });

                if (this.AdditionCost > 0.0)
                {
                    list.Add(new Account
                    {
                        EntryGlobalID = entry.EntryGlobalID,
                        EntryNo       = entryNo,
                        Code          = "4200002",
                        Debt          = this.AdditionCost,
                        Credit        = 0.0,
                        Note          = $"الإضافات مرتجع رقم:{this.Code}",
                        CCcode        = "-1"
                    });
                }

                if (this.InsureVal > 0.0)
                {
                    list.Add(new Account
                    {
                        EntryGlobalID = entry.EntryGlobalID,
                        EntryNo       = entryNo,
                        Code          = "22210001",
                        Debt          = this.InsureVal,
                        Credit        = 0.0,
                        Note          = "قيمة التأمين",
                        CCcode        = "-1"
                    });
                }

                list.Add(new Account
                {
                    EntryGlobalID = entry.EntryGlobalID,
                    EntryNo       = entryNo,
                    Code          = Convert.ToInt32(User.TreasuryAcc).ToString(),
                    Debt          = 0.0,
                    Credit        = this.NetVal,
                    Note          = $"مرتجع تأجير رقم:{this.Code}",
                    CCcode        = "-1"
                });
            }

            return list;
        }

        private void MarineExit(int MID)
        {
            try
            {
                if (this.conn.State != ConnectionState.Open)
                    this.conn.Open();
                var transaction = this.conn.BeginTransaction();

                DateTime invDate = this.txtDate.EditValue is DateTime d
                    ? d : DateTime.Now;

                string sql = this.ISreturn
                    ? $"update Attendance set _inOutMode=0, takeJoureny=0 "
                      + $"Where _enrollNumber={MID} "
                      + $"And _year={invDate.Year} "
                      + $"And _month={invDate.Month} "
                      + $"And _day={invDate.Day}"
                    : $"update Attendance set _inOutMode=1, takeJoureny=1 "
                      + $"Where _enrollNumber={MID} "
                      + $"And _year={invDate.Year} "
                      + $"And _month={invDate.Month} "
                      + $"And _day={invDate.Day}";

                new SqlCommand(sql, this.conn, transaction).ExecuteNonQuery();
                transaction.Commit();
            }
            catch { }
            finally
            {
                if (this.conn.State != ConnectionState.Closed)
                    this.conn.Close();
            }
        }

        #endregion

        #region Navigate & ReadData

        public void Navigate(string sqlstr)
        {
            if (this.conn.State != ConnectionState.Open)
                this.conn.Open();

            using var cmd = new SqlCommand(sqlstr, this.conn);
            using var dr  = cmd.ExecuteReader();
            this.ReadData(dr);
            this.SrchPrint = true;

            if (this.conn.State != ConnectionState.Closed)
                this.conn.Close();
        }

        private void ReadData(SqlDataReader dr)
        {
            try
            {
                if (!dr.HasRows) { this.CLR(); return; }
                dr.Read();
                this.CLR();

                this.ProcCode        = Convert.ToInt32(dr["proc_id"]);
                this.RestractionCode = Convert.ToInt32(dr["Restraction_id"]);
                this.AdditionCost    = Convert.ToDouble(dr["tot_Additions"]);
                this.txtAdditionVal.Text = this.AdditionCost.ToString();
                this.InsureVal       = Convert.ToDouble(dr["Insurance"]);
                this.Code            = Convert.ToInt32(dr["id"]);
                this.txtNo.Text      = this.Code.ToString();
                this.txtDate.EditValue = Convert.ToDateTime(dr["date"]);
                this.txtInvTime1.Text  =
                    Convert.ToDateTime(dr["date"]).ToString("hh:mm tt");

                this.ClientId = Convert.ToInt32(dr["cust_id"]);

                double totRent = Convert.ToDouble(dr["tot_Rent"]);
                this.txtTotRent.Text = $"{totRent:N2}";
                this.NetVal = Convert.ToDouble(dr["tot_net"]);
                this.txtNet.Text = this.NetVal.ToString();
                this.Discount = Convert.ToDouble(dr["Discount"]);
                this.txtDiscountVal.Text = this.Discount.ToString();

                double paidVal = Convert.ToDouble(dr["paid"]);
                this.txtrec1.Text = $"{paidVal:N2}";

                this.ProcType = Convert.ToInt32(dr["proc_type"]);
                this.comment  = dr["note"].ToString();
                this.txtReffNo.Text = dr["Reff_No"].ToString();

                try { this.txtTaxValue.Text = dr["tax"].ToString(); } catch { }

                this.MarId    = Convert.ToInt32(dr["MarineId"]);
                this.GroupId  = Convert.ToInt32(dr["GroupId"]);
                this.GroupCode = this.GetGroupMCode(this.GroupId);

                string serviceDesc =
                    this.ProcType == 1 ? this.GroupCode + " تأجير مركب فئة " :
                    this.ProcType == 2 ? this.GroupCode + " مرتجع تأجير مركب فئة " :
                    this.ProcType == 3 ? this.GroupCode + " تأجير مركب معلق فئة " :
                                         this.GroupCode + " تأجير مركب فئة ";

                string marineName = "";
                string marineCode = "";
                using (var adpMarine = new SqlDataAdapter(
                    "Select Name,MarineCode,IS_InPlan,Groupcode from Marine "
                    + "where IS_Deleted=0 And id=" + this.MarId, this.conn1))
                {
                    var dtMarine = new DataTable();
                    adpMarine.Fill(dtMarine);
                    if (dtMarine.Rows.Count > 0)
                    {
                        marineName      = dtMarine.Rows[0]["Name"].ToString();
                        marineCode      = dtMarine.Rows[0]["MarineCode"].ToString();
                        this.InPlan     = Convert.ToBoolean(dtMarine.Rows[0]["IS_InPlan"]);
                        this.txtMarineName.Text = marineName;
                        this.txtMarineCode.Text = marineCode;
                    }
                }
                this.txtMarineID.Text = this.MarId.ToString();

                string periodName = this.GetPeriodName(
                    Convert.ToInt32(dr["RentPeriod"]));

                var item = new RentInvoiceItem
                {
                    ProcTypeText   = serviceDesc,
                    ServiceName    = serviceDesc,
                    MarineCode     = marineCode,
                    Duration       = periodName,
                    Quantity       = 1,
                    TotalQuantity  = 1,
                    UnitRatio      = 1,
                    Price          = totRent,
                    Total          = totRent,
                    GroupIdVal     = this.GroupId,
                    MarineNameFull = marineName,
                    Notes          = this.comment,
                    MarineId       = this.MarId
                };

                double.TryParse(dr["tax"].ToString(), out double taxVal);
                item.TaxValue = taxVal;

                _invoiceItems.Clear();
                _invoiceItems.Add(item);
                dgvItems.ItemsSource = _invoiceItems;

                // بيانات العميل
                using (var adpCust = new SqlDataAdapter(
                    "select name,mobile,national_id from Customers where id="
                    + this.ClientId + " And (type=1 or type=3)", this.conn1))
                {
                    var dtCust = new DataTable();
                    adpCust.Fill(dtCust);
                    if (dtCust.Rows.Count > 0)
                    {
                        this.txtClient.Text = dtCust.Rows[0]["name"].ToString();
                        this.txtMarineDetails.Text =
                            $" العميل: {dtCust.Rows[0]["name"]} , "
                            + $"رقم الجوال: {dtCust.Rows[0]["mobile"]}\n"
                            + $" الهوية: {dtCust.Rows[0]["national_id"]} , "
                            + $"عدد المرافقين: {dr["Companions"]}";
                    }
                }

                dr.Close();
                this.DGV_Count = _invoiceItems.Count;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Search

        private void LoadDG(string cond)
        {
            _searchResults.Clear();
            try
            {
                int procTypeIdx = cmbProcTypeSrch.SelectedIndex + 1;
                string sql =
                    "select RentInvoice.proc_id, "
                    + "RentInvoice.id as id, "
                    + "RentInvoice.cust_id, "
                    + "RentInvoice.date as date, "
                    + "Customers.name as cust, "
                    + "sales_emp, "
                    + "Customers.mobile "
                    + "from RentInvoice, Customers "
                    + "where proc_type=" + procTypeIdx
                    + " and RentInvoice.IS_Deleted=0 and "
                    + cond
                    + " RentInvoice.cust_id=Customers.id "
                    + "order by RentInvoice.id";

                using var adapter = new SqlDataAdapter(sql, this.conn);

                if (!string.IsNullOrEmpty(cond)
                    && cond.Contains("@date"))
                {
                    DateTime fromDate = this.txtFromDate.EditValue is DateTime fd
                        ? fd : DateTime.Now;
                    DateTime toDate   = this.txtToDate.EditValue is DateTime td
                        ? td.AddHours(24) : DateTime.Now.AddHours(24);
                    adapter.SelectCommand.Parameters
                        .AddWithValue("@date1", fromDate.ToShortDateString());
                    adapter.SelectCommand.Parameters
                        .AddWithValue("@date2", toDate);
                }

                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    int invId = Convert.ToInt32(row["id"]);

                    string status = "";
                    using (var adp2 = new SqlDataAdapter(
                        "select id from RentInvoice where proc_type=2 and id="
                        + invId + " and IS_Deleted=0", this.conn))
                    {
                        var dt2 = new DataTable();
                        adp2.Fill(dt2);
                        if (dt2.Rows.Count >= 1) status = "تم إرجاعها";
                    }

                    _searchResults.Add(new RentSearchResult
                    {
                        ProcId       = Convert.ToInt32(row["proc_id"]),
                        InvId        = invId,
                        InvDate      = Convert.ToDateTime(row["date"])
                                           .ToShortDateString(),
                        CustomerName = row["cust"].ToString(),
                        Mobile       = row["mobile"].ToString(),
                        CustomerId   = Convert.ToInt32(row["cust_id"]),
                        UserName     = Common.GetEmpName(
                                           Convert.ToInt32(row["sales_emp"])),
                        InvStatus    = status
                    });
                }

                dgvSrch.ItemsSource = _searchResults;
                dgvSrch.Items.Refresh();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في البحث: " + ex.Message);
            }
        }

        private void Search(int type)
        {
            string branchCond = MainClass.BranchNo != -1
                ? "RentInvoice.branch=" + MainClass.BranchNo + " and "
                : "";

            string cond;
            if (!string.IsNullOrWhiteSpace(this.txtSrchNo.Text))
                cond = branchCond + " RentInvoice.id="
                       + this.txtSrchNo.Text + " and ";
            else
                cond = branchCond + " date>=@date1 and date<=@date2 and ";

            if (type == 3)
                cond = branchCond
                       + " RentInvoice.cust_id=" + this.ClientN + " and ";

            if (this.cmbUsers.SelectedIndex > -1
                && this.cmbUsers.SelectedValue != null)
                cond += $" RentInvoice.sales_emp={this.cmbUsers.SelectedValue} and ";

            if (!string.IsNullOrWhiteSpace(this.txtCustPhone.Text))
                cond += $" Customers.mobile like '{this.txtCustPhone.Text}' and ";

            if (!string.IsNullOrWhiteSpace(this.txtClientName.Text))
                cond += $" Customers.name like '{this.txtClientName.Text}' and ";

            this.SrchPrint = true;
            this.LoadDG(cond);
        }

        #endregion

        #region Button Event Handlers

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            this.CLR();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            this.Search(2);

            if (!string.IsNullOrWhiteSpace(this.txtClientName.Text.Trim()))
            {
                foreach (var result in _searchResults)
                {
                    if (string.Equals(result.CustomerName,
                            this.txtClientName.Text,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        this.ClientN = result.CustomerId;
                        this.Search(3);
                        break;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(this.txtCustPhone.Text.Trim()))
            {
                foreach (var result in _searchResults)
                {
                    if (string.Equals(result.Mobile,
                            this.txtCustPhone.Text,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        this.ClientN = result.CustomerId;
                        this.Search(3);
                        break;
                    }
                }
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (this.SrchPrint) this.PrintDevexpress();
            this.SrchPrint = false;
        }

        private void btnPay_Click(object sender, RoutedEventArgs e)
        {
            if (this.Code == -1)
            {
                if (this.ProcType != 4)
                {
                    var dlg = new frmSrchClient { Type = 1 };
                    MainClass.ApplyPermissionToForm(dlg);
                    MainClass.DoApplyUserSett(dlg);
                    dlg.ShowDialog();

                    this.txtClient.Text = "";
                    this.ClientId       = -1;

                    if (string.IsNullOrEmpty(dlg.Clientname)
                        || dlg.ClientId <= 1)
                    {
                        DXMessageBox.Show("يجب اختيار العميل",
                            "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    this.txtClient.Text = dlg.Clientname;
                    this.ClientId       = dlg.ClientId;
                }

                var companions = new frmCompanions();
                companions.ShowDialog();

                if (companions.ISdone)
                {
                    try
                    {
                        double.TryParse(
                            companions.txtCompanionsNo.Text, out double comp);
                        this.txtMarineDetails.Text =
                            " عدد المرافقين: " + ((int)comp).ToString();
                        this.Companions = (int)comp;
                        this.comment    = comp.ToString();

                        if (_invoiceItems.Count > 0)
                            _invoiceItems[0].Notes = this.comment;

                        this.PayBill();
                    }
                    catch { }
                }
            }
            else
            {
                DXMessageBox.Show("الفاتورة تم حفظها مسبقاً",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (_invoiceItems.Count == 0) return;

            if (this.Code == -1)
            {
                var pwd = new frmCheckPwd { operNo = 5 };
                pwd.ShowDialog();
                if (pwd.Iscorrect) this.AbsentMarine();
            }
            else
            {
                DXMessageBox.Show("يجب تحديد المركب لاستبعاده");
            }
        }

        private void DeleteRowBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is RentInvoiceItem item)
            {
                string msg = string.Equals(MainClass.Language, "ar",
                    StringComparison.OrdinalIgnoreCase)
                    ? "هل تريد حذف السجل"
                    : "Do you want to delete this record";

                if (DXMessageBox.Show(msg, "", MessageBoxButton.YesNo,
                        MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _invoiceItems.Remove(item);
                    this.CalcTot();
                    this.rowSelected = -1;
                }
            }
        }

        private void btnHoldOrd_Click(object sender, RoutedEventArgs e)
        {
            if (this.holdbool)
            {
                if (!string.IsNullOrEmpty(this.GroupCode))
                {
                    var res = DXMessageBox.Show(
                        "هل تريد تعليق مركب من فئة " + this.GroupCode,
                        "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res == MessageBoxResult.Yes)
                    {
                        this.click_item(0);
                        this.ProcType = 3;
                    }
                }
                else
                {
                    DXMessageBox.Show("من فضلك اختر الفئة أولاً");
                }
            }
            else
            {
                DXMessageBox.Show("من فضلك اختر الفئة أولاً");
            }
        }

        private void btnExpection_Click(object sender, RoutedEventArgs e)
        {
            var pwd = new frmCheckPwd { operNo = 6 };
            pwd.ShowDialog();
            if (pwd.Iscorrect)
            {
                this.ExceptionMode = true;
                this.click_group(this.gL1.Text);
            }
        }

        private void btnAddUnit_Click(object sender, RoutedEventArgs e)
        {
            this.ChangePeriod();
        }

        private void btnAddbook_Click(object sender, RoutedEventArgs e)
        {
            cmbProcTypeSrch.SelectedIndex = 3;
            TabControl1.SelectedIndex     = 1;
        }

        private void btnReplace_Click(object sender, RoutedEventArgs e)
        {
            var msg = new MsgGeneralAlter();
            msg.txtAlarm.Text =
                "بناءً على توجيهات هيئة الزكاة والضريبة والجمارك "
                + "يُمنع منعاً باتاً تعديل أو حذف أي فاتورة ضريبية "
                + "صادرة من النظام";
            msg.ShowDialog();
        }

        private void RetSales_Click(object sender, RoutedEventArgs e)
        {
            if (_invoiceItems.Count == 0) return;

            if (this.Code != -1)
            {
                using var adapter = new SqlDataAdapter(
                    "select id from RentInvoice where proc_type=2 and Reff_No="
                    + this.Code + " and IS_Deleted=0", this.conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count >= 1)
                {
                    DXMessageBox.Show("الفاتورة تم إرجاعها سابقاً");
                    return;
                }
                if (this.ProcType == 4)
                {
                    DXMessageBox.Show("لا يمكن إرجاع فاتورة حجوزات");
                    return;
                }

                var pwd = new frmCheckPwd { operNo = 2 };
                pwd.ShowDialog();
                if (pwd.Iscorrect)
                {
                    this.ISreturn = true;
                    this.txtReffNo.Text = this.txtNo.Text;
                    this.txtInvTime1.Text =
                        (this.txtDate.EditValue is DateTime d
                            ? d : DateTime.Now).ToString("hh:mm tt");
                    this.ProcType = 2;
                    this.save();
                }
            }
            else
            {
                DXMessageBox.Show("اختر فاتورة");
            }
        }

        private void btnCloseDay_Click(object sender, RoutedEventArgs e)
        {
            var pwd = new frmCheckPwd { operNo = 1 };
            pwd.ShowDialog();
            if (!pwd.Iscorrect) return;

            using var adapter = new SqlDataAdapter(
                "Select ActiveAuto from SettingEmail where Branch_Id="
                + MainClass.BranchNo, this.conn);
            var dt = new DataTable();
            adapter.Fill(dt);

            if (dt.Rows.Count > 0
                && Convert.ToBoolean(dt.Rows[0]["ActiveAuto"])
                && !MainClass.CheckForInternetConnection())
            {
                var res = DXMessageBox.Show(
                    "الإنترنت غير متصل، للاستمرار في الإغلاق اضغط نعم.",
                    "تحذير", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.No) return;
            }
            var Home = new Home();
            var closeShift = new frmCloseShift();
            closeShift.Activate();
            if (closeShift.NoInvsFound(2))
            {
                DXMessageBox.Show("لا يوجد فواتير جديدة يمكن إغلاقها");
                Home.IsCashierClosed = true;
                return;
            }
            closeShift.closeShift(2);
            if (Home.IsCashierClosed) this.Close();
        }

        private void btnsrch_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new frmInvoiceRentSrch();
            MainClass.ApplyPermissionToForm(dlg);
            MainClass.DoApplyUserSett(dlg);
            dlg.ProcType = 1;
            dlg.InvType  = 20;
            dlg.ShowDialog();

            if (dlg.ISDone && dlg.InvID > 0)
                this.Navigate(
                    "select * from RentInvoice where proc_id="
                    + dlg.InvID + " AND proc_type=" + dlg.ProcType);
        }

        private void btnInPlan_Click(object sender, RoutedEventArgs e)
        {
            this.btnInPlan.Background  = new SolidColorBrush(Colors.SeaGreen);
            this.btnOutPlan.Background = new SolidColorBrush(Colors.DarkGray);
            this.InPlan = true;
            if (!string.IsNullOrEmpty(this.gL1.Text))
                this.click_group(this.gL1.Text);
        }

        private void btnOutPlan_Click(object sender, RoutedEventArgs e)
        {
            this.btnOutPlan.Background = new SolidColorBrush(Colors.SeaGreen);
            this.btnInPlan.Background  = new SolidColorBrush(Colors.DarkGray);
            this.InPlan = false;
            if (!string.IsNullOrEmpty(this.gL1.Text))
                this.click_group(this.gL1.Text);
        }

        private void btnPrice_Click(object sender, RoutedEventArgs e)
        {
            this.priceEdit();
        }

        private void btnQuantity_Click(object sender, RoutedEventArgs e) { }

        private void btnAddItem_Click(object sender, RoutedEventArgs e)
        {
            DXMessageBox.Show("اختر المركب من لوحة المركبات");
        }

        #endregion

        #region ChangePeriod & priceEdit

        private void ChangePeriod()
        {
            this._IsUpdateDG = false;
            var period = new Period { ItemId = this.GroupId };
            try
            {
                period.ShowDialog();
                if (!string.IsNullOrEmpty(period.Periodname)
                    && _invoiceItems.Count > 0)
                {
                    _invoiceItems[0].Duration = period.Periodname;

                    // ✅ تحويل صريح من decimal إلى double
                    _invoiceItems[0].Price = Convert.ToDouble(period.PeriodPrice);
                    _invoiceItems[0].Total = Convert.ToDouble(period.PeriodPrice);

                    this.CalcTot();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void priceEdit()
        {
            if (_invoiceItems.Count == 0)
            {
                DXMessageBox.Show("لا يوجد أصناف");
                return;
            }
            if (!this._IsUpdateDG)
                this.dgvItemsRowsSelected = _invoiceItems.Count - 1;

            var calc = new Frm_Calculator();
            calc.ShowDialog();
            if (!calc.is_close)
                double.TryParse(calc.TextBox1.Text, out this.txtExchangeVal);
        }

        #endregion

        #region AbsentMarine

        private void AbsentMarine()
        {
            try
            {
                if (this.conn.State != ConnectionState.Open)
                    this.conn.Open();
                var transaction = this.conn.BeginTransaction();

                DateTime invDate = this.txtDate.EditValue is DateTime d
                    ? d : DateTime.Now;

                if (this.Code == -1)
                {
                    new SqlCommand(
                        $"update Attendance set _inOutMode=1, takeJoureny=0 "
                        + $"Where _enrollNumber={this.MarId} "
                        + $"And _year={invDate.Year} "
                        + $"And _month={invDate.Month} "
                        + $"And _day={invDate.Day}",
                        this.conn, transaction).ExecuteNonQuery();

                    DXMessageBox.Show(
                        string.Equals(MainClass.Language, "ar",
                            StringComparison.OrdinalIgnoreCase)
                            ? "تم الاستبعاد"
                            : "Deleted",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    transaction.Commit();
                    this.CLR();
                }
                else
                {
                    DXMessageBox.Show(
                        string.Equals(MainClass.Language, "ar",
                            StringComparison.OrdinalIgnoreCase)
                            ? "اختر المركب ليتم استبعاده"
                            : "choose invoice to be deleted");
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    (string.Equals(MainClass.Language, "ar",
                        StringComparison.OrdinalIgnoreCase)
                        ? "خطأ أثناء الاستبعاد"
                        : "error in deleting")
                    + Environment.NewLine + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (this.conn.State != ConnectionState.Closed)
                    this.conn.Close();
            }
        }

        #endregion

        #region DataGrid Events

        private void dgvItems_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            this.rowSelected         = dgvItems.SelectedIndex;
            this.dgvItemsRowsSelected = this.rowSelected;
        }

        private void dgvItems_MouseDoubleClick(object sender,
            MouseButtonEventArgs e) { }

        private void dgvSrch_MouseDoubleClick(object sender,
            MouseButtonEventArgs e)
        {
            if (dgvSrch.SelectedItem is RentSearchResult result)
            {
                this.ProcCode = result.ProcId;
                this.Navigate(
                    "select * from RentInvoice where proc_id=" + this.ProcCode);
                TabControl1.SelectedIndex = 0;
            }
        }

        #endregion

        #region TabControl Events

        private void TabControl1_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (TabControl1.SelectedIndex == 1)
                cmbProcTypeSrch.SelectedIndex = 0;
        }

        #endregion

        #region ComboBox Events

        private void cmbProcTypeSrch_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (cmbProcTypeSrch.SelectedIndex >= 0)
                this.Search(2);
        }

        private void cmbUsers_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            try
            {
                if (this.cmbUsers.SelectedIndex > -1)
                {
                    this.Search(2);
                    this.cmbUsers.SelectedIndex = -1;
                }
            }
            catch { }
        }

        private void cmbSalesMen_SelectionChanged(object sender,
            SelectionChangedEventArgs e) { }

        #endregion

        #region TextBox Events

        private void txtrec1_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                if (!IsLoaded) return;
                double.TryParse(this.txtrec1.Text, out double paid);
                this.txtrecrem1.Text = $"{paid - this.NetVal:0.00}";
            }
            catch { }
        }

        private void txtClientName_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(this.txtClientName.Text.Trim()))
                    this.Search(2);
            }
            catch { }
        }

        private void txtCustPhone_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(this.txtCustPhone.Text.Trim()))
                    this.Search(2);
            }
            catch { }
        }

        private void txtMinusPerc_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!this.dochange) return;
            try
            {
                this.dochange = false;
                double percVal = 0;
                double.TryParse(this.txtMinusPerc.Text, out percVal);

                if (this.InvProc == 1)
                {
                    double.TryParse(this.txtTotPurch.Text, out double totPurch);
                    this.txtMinusVal.Text =
                        Math.Round(percVal / 100.0 * totPurch, 2).ToString();
                }
                else
                {
                    double.TryParse(this.txtTotRent.Text, out double totRent);
                    this.txtMinusVal.Text =
                        Math.Round(percVal / 100.0 * totRent, 2).ToString();
                }
            }
            catch { }
            this.dochange = true;
        }

        private void txtMinusVal_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!this.dochange) return;
            try
            {
                this.dochange = false;
                double minusVal = 0;
                double.TryParse(this.txtMinusVal.Text, out minusVal);

                if (this.InvProc == 1)
                {
                    double.TryParse(this.txtTotPurch.Text, out double totPurch);
                    if (totPurch != 0)
                        this.txtMinusPerc.Text =
                            Math.Round(minusVal / totPurch * 100.0, 2).ToString();
                }
                else
                {
                    double.TryParse(this.txtTotRent.Text, out double totRent);
                    if (totRent != 0)
                        this.txtMinusPerc.Text =
                            Math.Round(minusVal / totRent * 100.0, 2).ToString();
                }
            }
            catch { }
            this.dochange = true;
        }

        #endregion

        #region NumPad

        private void NumPad_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string digit)
            {
                if (Keyboard.FocusedElement is TextBox tb)
                {
                    if (digit == "." && tb.Text.Contains(".")) return;
                    int caret = tb.CaretIndex;
                    tb.Text = tb.Text.Insert(caret, digit);
                    tb.CaretIndex = caret + 1;
                }
            }
        }

        private void cmdClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (Keyboard.FocusedElement is TextBox tb)
                tb.Text = "";
        }

        #endregion

        #region Keyboard Shortcuts

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.Key)
            {
                case Key.F3:
                    this.priceEdit();
                    e.Handled = true;
                    break;
                case Key.F4:
                    this.ChangePeriod();
                    e.Handled = true;
                    break;
                case Key.F5:
                    this.btnDeleteRow_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.F6:
                    this.btnPay_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.F7:
                    this.btnHoldOrd_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.F8:
                    this.btnReplace_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.F9:
                    this.btnCloseDay_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.F11:
                    this.btnExpection_Click(null, null);
                    e.Handled = true;
                    break;
                case Key.F12:
                    this.btnNew_Click(null, null);
                    e.Handled = true;
                    break;
            }
        }

        #endregion

        #region Print

        private void PrintDevexpress()
        {
            if (_invoiceItems.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات بالجدول");
                return;
            }
            if (string.IsNullOrEmpty(this.RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير");
                return;
            }
            if (this.ProcType == 2) this.RptName = "RentCreditNote.repx";

            string rptDir  = System.IO.Path.GetDirectoryName(this.RptUrl);
            string fullPath = System.IO.Path.Combine(rptDir ?? "", this.RptName);

            if (!System.IO.Directory.Exists(rptDir)
                || !System.IO.File.Exists(fullPath))
            {
                DXMessageBox.Show("مسار التقارير غير موجود أو تم تعديله",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(this.RptName))
            {
                DXMessageBox.Show("يجب إدخال اسم التقرير من الإعدادات");
                return;
            }

            try
            {
                var report = XtraReport.FromFile(fullPath);
                report.DataSource = this.BindToData();

                if (!string.IsNullOrEmpty(this.defPrinter))
                {
                    report.PrinterName = this.defPrinter;
                    for (int i = 1; i <= this.PrintNo; i++)
                        report.Print();
                    report.Dispose();
                }
                else
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات");
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في الطباعة: " + ex.Message);
            }
        }

        private DataSet BindToData()
        {
            string invoiceType = this.ProcType == 2
                ? "إشعار دائن للفاتورة الضريبية المبسطة"
                : this.ProcType == 3
                    ? "فاتورة ضريبية مبسطة - مفتوحة"
                    : "فاتورة ضريبية مبسطة";

            string invDate =
                (this.txtDate.EditValue is DateTime d)
                ? d.ToShortDateString()
                : DateTime.Now.ToShortDateString();

            double totSum  = 0.0;
            double.TryParse(this.txtrec1.Text, out double paidVal);
            double remainder = Math.Round(paidVal - this.NetVal, 2);
            string salesEmp  = this.GetSalesEmpNameByInvNo();

            foreach (var item in _invoiceItems)
                totSum += item.Price * item.Quantity;

            using var adpFound = new SqlDataAdapter(
                "Select * from Foundation", this.conn);
            var dtFound = new DataTable();
            adpFound.Fill(dtFound);

            string address    = "";
            string telephone  = "";
            string mobile     = "";
            string foundation = "";
            string field      = "";
            string vatNo      = "";

            if (dtFound.Rows.Count > 0)
            {
                address    = dtFound.Rows[0]["Address"].ToString();
                telephone  = dtFound.Rows[0]["Tel"].ToString();
                mobile     = dtFound.Rows[0]["Mobile"].ToString();
                foundation = dtFound.Rows[0]["nameA"].ToString();
                field      = dtFound.Rows[0]["FieldA"].ToString();
                vatNo      = dtFound.Rows[0]["tax_no"].ToString();
            }

            string payType = this.Paytype switch
            {
                -1 => "آجل",
                2  => "شبكة",
                3  => "شبكة",
                4  => "متعدد",
                5  => "ضيافة",
                _  => "نقدي"
            };

            var customer = new Customer(this.ClientId);
            string saleman = this.cmbSalesMen.SelectedIndex > -1
                ? this.cmbSalesMen.Text : "";

            double.TryParse(this.txtTaxValue.Text, out double taxVal);
            double netExcTax = Math.Round(this.NetVal - taxVal, 2);

            var list = new List<InvoiceData>();

            foreach (var item in _invoiceItems)
            {
                double priceNoVat = this.PricIncVAT
                    ? item.Price / (1.0 + this.defVAT / 100.0)
                    : item.Price;

                var qr = new QR
                {
                    SellerName       = foundation,
                    TaxAmount        = taxVal.ToString(),
                    TaxNumber        = vatNo,
                    InvoiceTotal     = this.NetVal.ToString(),
                    InvoiceTimeStamp = DateTime.Now.ToString()
                };

                var invData = new InvoiceData
                {
                    QRCode          = Generator.GenerateBase64(qr),
                    ItemName        = item.MarineNameFull,
                    ItemNo          = item.MarineCode,
                    Quantity        = item.Duration,
                    Unit            = item.Duration,
                    Price           = priceNoVat.ToString("N2"),
                    Total           = priceNoVat.ToString("N2"),
                    Barcode         = item.MarineCode,
                    SumPrice        = totSum.ToString("N2"),
                    TotalWithoutVAT = netExcTax.ToString("N2"),
                    VAT             = taxVal.ToString("N2"),
                    Net             = this.NetVal.ToString("N2"),
                    Discount        = this.Discount.ToString("N2"),
                    Additions       = this.AdditionCost.ToString("N2"),
                    PayNetwork      = this.resNetwork.ToString("N2"),
                    Insurance       = this.InsureVal.ToString("N2"),
                    Paycash         = this.Paycash.ToString("N2"),
                    Remainder       = remainder.ToString("N2"),
                    PayType         = payType,
                    InvoiceNo       = this.txtNo.Text,
                    ReffNo          = this.txtReffNo.Text,
                    InvoiceType     = invoiceType,
                    OrderNo         = this.txtOrderNo.Text,
                    OrderType       = this.comment,
                    InvTime         = DateTime.Now.ToShortTimeString(),
                    InvDate         = invDate,
                    User            = salesEmp,
                    Customer        = customer.Name,
                    Saleman         = saleman,
                    CustVATno       = customer.VATno,
                    CustNatianalID  = customer.NatianalID,
                    CustMobile      = customer.Mobile,
                    CustNote        = customer.Note,
                    InvNote         = this.GroupCode + " مركب فئة",
                    CustCompanions  = this.comment,
                    PrintNote       = this.PrintNote,
                    Address         = address,
                    Mobile          = mobile,
                    TelePhone       = telephone,
                    VatNo           = vatNo,
                    Foundation      = foundation,
                    Field           = field,
                    VATPerc         = this.defVAT.ToString(),
                    Logo            = "",
                    Header          = "",
                    footer          = "",
                    Stamp           = "",
                    Rent            = item.ServiceName
                };
                list.Add(invData);
            }

            var ds    = new DataSet("Name");
            var table = global::UtilitiesProj.Common.ToDataTable(list);
            ds.Tables.Add(table);
            list.Clear();
            return ds;
        }

        #endregion

        #region Public Methods (Used from Other Classes)

        public void ImportInventoryItems(List<RentInvoiceItem> importedItems)
        {
            if (importedItems == null || importedItems.Count == 0) return;
            foreach (var item in importedItems)
            {
                this.click_item(item.MarineId);
                var target = _invoiceItems
                    .FirstOrDefault(i => i.MarineId == item.MarineId);
                if (target != null)
                {
                    target.Price    = item.Price;
                    target.Quantity = item.Quantity;
                    target.Total    = item.Price * item.Quantity;
                    this.CalcTot();
                }
            }
        }

        #endregion
    }

    #region Model Classes

    public class RentInvoiceItem : System.ComponentModel.INotifyPropertyChanged
    {
        private string _procTypeText;
        private string _serviceName;
        private string _marineCode;
        private string _duration;
        private double _quantity;
        private double _totalQuantity;
        private double _unitRatio;
        private double _price;
        private double _total;
        private int    _groupIdVal;
        private string _marineNo;
        private string _marineNameFull;
        private string _notes;
        private double _taxValue;
        private int    _marineId;
        private DateTime _expireDate;

        public string ProcTypeText
        {
            get => _procTypeText;
            set { _procTypeText = value; OnPropertyChanged(nameof(ProcTypeText)); }
        }
        public string ServiceName
        {
            get => _serviceName;
            set { _serviceName = value; OnPropertyChanged(nameof(ServiceName)); }
        }
        public string MarineCode
        {
            get => _marineCode;
            set { _marineCode = value; OnPropertyChanged(nameof(MarineCode)); }
        }
        public string Duration
        {
            get => _duration;
            set { _duration = value; OnPropertyChanged(nameof(Duration)); }
        }
        public double Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(nameof(Quantity)); }
        }
        public double TotalQuantity
        {
            get => _totalQuantity;
            set { _totalQuantity = value; OnPropertyChanged(nameof(TotalQuantity)); }
        }
        public double UnitRatio
        {
            get => _unitRatio;
            set { _unitRatio = value; OnPropertyChanged(nameof(UnitRatio)); }
        }
        public double Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(nameof(Price)); }
        }
        public double Total
        {
            get => _total;
            set { _total = value; OnPropertyChanged(nameof(Total)); }
        }
        public int GroupIdVal
        {
            get => _groupIdVal;
            set { _groupIdVal = value; OnPropertyChanged(nameof(GroupIdVal)); }
        }
        public string MarineNo
        {
            get => _marineNo;
            set { _marineNo = value; OnPropertyChanged(nameof(MarineNo)); }
        }
        public string MarineNameFull
        {
            get => _marineNameFull;
            set { _marineNameFull = value; OnPropertyChanged(nameof(MarineNameFull)); }
        }
        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(nameof(Notes)); }
        }
        public double TaxValue
        {
            get => _taxValue;
            set { _taxValue = value; OnPropertyChanged(nameof(TaxValue)); }
        }
        public int MarineId
        {
            get => _marineId;
            set { _marineId = value; OnPropertyChanged(nameof(MarineId)); }
        }
        public DateTime ExpireDate
        {
            get => _expireDate;
            set { _expireDate = value; OnPropertyChanged(nameof(ExpireDate)); }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this,
                new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    public class RentSearchResult
    {
        public int    ProcId       { get; set; }
        public int    InvId        { get; set; }
        public string InvDate      { get; set; }
        public string CustomerName { get; set; }
        public string Mobile       { get; set; }
        public int    CustomerId   { get; set; }
        public string UserName     { get; set; }
        public string InvStatus    { get; set; }
    }

    public class MarineCard
    {
        public int    MarineId   { get; set; }
        public string Name       { get; set; }
        public string MarineCode { get; set; }
        public bool   IsEnabled  { get; set; }
        public SolidColorBrush Background { get; set; }
        public SolidColorBrush Foreground { get; set; }
    }

    #endregion
}