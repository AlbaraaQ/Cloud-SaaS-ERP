using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AuditorAPI.Models;
using DevExpress.XtraReports.UI;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using UtilitiesProj;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class ClosShiftAndroid : Window
    {
        #region Private Fields

        private SqlConnection conn;
        private SqlConnection conn1;
        public DateTime datefrom;
        public DateTime dateTo;
        public DateTime DateFromclose;
        public DateTime Datetoclose;
        private DateTime _entryDate;
        private double CasherValue;
        private int shiftNo;
        private int CloseNo;
        private int selectedCloseNo;
        private string Username;
        private bool IsGroupPrinted;
        private bool IsItemsPrinted;
        private int UTreasuryAc;
        private bool PrintHeader;
        private bool PrintFooter;
        private int PrintType;
        private int PrintNo;
        private string defPrinter;
        private bool isPrintd;
        private string PrintNote;
        private string RptName;
        private string RptUrl;
        private bool PrintStamp;
        private CloseShiftSetting CloseShiftSetting;
        private SqlDataAdapter da;
        private DataTable dt;
        private DataTable dtGroup;
        private DataTable dt2;
        private double TotSale;
        private double TotQun;
        Home Home = new Home();

        #endregion

        #region Public Properties



        #endregion

        #region Constructor

        public ClosShiftAndroid()
        {
            InitializeComponent();

            this.conn = MainClass.ConnObj();
            this.conn1 = MainClass.ConnObj();
            this.CasherValue = 0.0;
            this.shiftNo = 1;
            this.CloseNo = 1;
            this.selectedCloseNo = -1;
            this.Username = "";
            this.IsGroupPrinted = true;
            this.IsItemsPrinted = true;
            this.PrintHeader = true;
            this.PrintFooter = true;
            this.PrintType = 1;
            this.PrintNo = 1;
            this.isPrintd = true;
            this.PrintNote = "";
            this.RptName = "";
            this.RptUrl = "";
            this.PrintStamp = true;
            this.CloseShiftSetting = new CloseShiftSetting();
            this.da = new SqlDataAdapter();
            this.dt = new DataTable();
            this.dtGroup = new DataTable();
            this.dt2 = new DataTable();
            this.TotSale = 0.0;
            this.TotQun = 0.0;

            this.Loaded += ClosShiftAndroid_Load;
        }

        #endregion

        #region Window Events

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnclose_Click(object sender, RoutedEventArgs e)
        {
            this.closeShift(1);
            if (Home.ns != null)
                Home.ns.closeofday = "";
        }

        private void ClosShiftAndroid_Load(object sender, RoutedEventArgs e)
        {
            this.LoadEmps();
            this.StartDate.SelectedDate = DateTime.Now.Date;
            this.EndDate.SelectedDate = DateTime.Now.Date;
            this.txtStartTime.Text = "00:00";
            this.txtEndTime.Text = "23:59";
        }

        #endregion

        #region Data Loading Methods

        private void LoadEmps()
        {
            try
            {
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                    "select id,name from Employees where IS_Deleted=0 order by id",
                    this.conn);
                DataTable dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);
                this.cmbUser.ItemsSource = dataTable.DefaultView;
                this.cmbUser.DisplayMemberPath = "name";
                this.cmbUser.SelectedValuePath = "id";
                this.cmbUser.SelectedIndex = -1;
            }
            catch (Exception)
            {
            }
        }

        private void LoadCloseShiftSetting()
        {
            try
            {
                this.CloseShiftSetting = Common.CloseDaySetting();
            }
            catch
            {
            }
        }

        #endregion

        #region Close Shift Main Method

        public void closeShift(int type)
        {
            this.LoadCloseShiftSetting();
            if (new InvoiceObj(20, 1).PaymentStatus)
            {
                type = 3;
            }

            DateTime startDateTime;
            DateTime endDateTime;

            try
            {
                startDateTime = DateTime.Parse(this.txtStartTime.Text);
                endDateTime = DateTime.Parse(this.txtEndTime.Text);
            }
            catch
            {
                MessageBox.Show("صيغة الوقت غير صحيحة. يرجى إدخال الوقت بصيغة HH:mm",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TimeSpan timeOfDay = startDateTime.TimeOfDay;
            TimeSpan timeOfDay2 = endDateTime.TimeOfDay;

            this.datefrom = DateTime.Parse(this.StartDate.SelectedDate.Value.ToShortDateString() +
                " " + this.txtStartTime.Text);
            this.dateTo = DateTime.Parse(this.EndDate.SelectedDate.Value.ToShortDateString() +
                " " + this.txtEndTime.Text);

            if (this.conn1.State != ConnectionState.Open)
            {
                this.conn1.Open();
            }

            this.Username = Conversions.ToString(this.cmbUser.SelectedValue);

            if (this.CloseShiftSetting.BalanceRequired)
            {
                Frm_Calculator frm_Calculator = new Frm_Calculator();
                frm_Calculator.txtMsg.Text = "إدخل المبلغ الموجود في الصندوق";
                frm_Calculator.ShowDialog();
                if (frm_Calculator.is_close)
                {
                    return;
                }
                this.CasherValue = Conversion.Val(frm_Calculator.TextBox1.Text);
            }

            this.UTreasuryAc = this.GetUserTreasuryAccemp();
            if (this.UTreasuryAc == -1)
            {
                MessageBox.Show("الموظف غير مرتبط بصندوق", "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (this.conn1.State != ConnectionState.Open)
            {
                this.conn1.Open();
            }

            string EntryGlobalId = "";
            int EntryNo = default(int);
            EntryOper.GetEntryGlobalID(ref EntryGlobalId, ref EntryNo);

            checked
            {
                this.CloseNo = (int)Math.Round(Conversion.Val(Operators.ConcatenateObject("",
                    new SqlCommand("select ISNULL(MAX(id), 0) from CasherClosed",
                    this.conn1).ExecuteScalar())) + 1.0);

                SqlCommand sqlCommand = new SqlCommand(
                    "select ISNULL(MAX(id), 0) from CasherClosed where endTime >= @date1 and endTime <= @date2  and type=" +
                    Conversions.ToString(type), this.conn1);
                sqlCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = this.datefrom;
                sqlCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = this.dateTo;
                this.shiftNo = (int)Math.Round(Conversion.Val(Operators.ConcatenateObject("",
                    sqlCommand.ExecuteScalar())) + 1.0);

                CloseShift closeData = this.GetCloseData((short)type,
                    Conversions.ToDate(this.datefrom.ToString("yyyy-MM-dd'T'HH:mm:ss zzz")),
                    Conversions.ToDate(this.dateTo.ToString("yyyy-MM-dd'T'HH:mm:ss zzz")),
                    Conversions.ToShort(this.Username));

                if (this.dt.Rows.Count <= 0)
                {
                    MessageBox.Show("لا يوجد فواتير للاغلاق", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                closeData.ClosedId = this.CloseNo;
                closeData.ShiftNo = this.shiftNo;
                closeData.Bank = 1;
                closeData.EmpId = Conversions.ToInteger(this.Username);
                closeData.CloseTime = this.datefrom;
                closeData.ShiftStart = Conversions.ToDate(this.DateFromclose.ToString("yyyy-MM-dd'T'HH:mm:ss zzz"));
                this.dateTo = Conversions.ToDate(DateTime.Now.Date.ToString("yyyy-MM-dd'T'HH:mm:ss zzz"));
                closeData.ShiftEnd = Conversions.ToDate(this.Datetoclose.ToString("yyyy-MM-dd'T'HH:mm:ss zzz"));
                closeData.Treasury = User.TreasuryID;
                closeData.EntryGlobalID = EntryGlobalId;
                closeData.CashierBalance = new decimal(this.CasherValue);

                Entry entry = this.BindCloseShiftToEntry1(closeData, this.CloseShiftSetting.BalanceRequired);
                CashierCloseData obj = new CashierCloseData();
                this.BindingCloseShift(ref closeData, ref obj);

                frmCloseShiftDetails frmCloseShiftDetails2 = new frmCloseShiftDetails();
                frmCloseShiftDetails2.obj = obj;
                frmCloseShiftDetails2.txtFrom.Text = Conversions.ToString(closeData.ShiftStart);
                frmCloseShiftDetails2.txtTo.Text = Conversions.ToString(closeData.ShiftEnd);
                frmCloseShiftDetails2.ShowDialog();

                if (!frmCloseShiftDetails2.isDone || entry == null)
                {
                    return;
                }

                EntryOper entryOper = new EntryOper();
                if (!entryOper.SaveEnty(entry))
                {
                    string text = "خطأ أثناء الحفظ";
                    if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
                    {
                        text = "error in saving";
                    }
                    MessageBox.Show(text, "", MessageBoxButton.OK, MessageBoxImage.Hand);
                    return;
                }

                if (Sync.ActiveSync & (Sync.SyncType > 0))
                {
                    entryOper.SyncEntry(entry, isNew: true);
                }

                if (this.conn.State != ConnectionState.Open)
                {
                    this.conn.Open();
                }

                SqlTransaction sqlTransaction = this.conn.BeginTransaction();

                try
                {
                    double cashDifference = double.Parse(obj.CashVal) - double.Parse(obj.ReturnSales);
                    if (cashDifference - this.CasherValue > 0.0)
                    {
                        sqlCommand = new SqlCommand(
                            "insert into EmpSalaryAddSub (emp,type,date,val,notes,user_id,IS_Deleted) values (@emp,3,@date,@val,@notes,1,0)",
                            this.conn, sqlTransaction);
                        sqlCommand.Parameters.AddWithValue("@emp", MainClass.EmpNo);
                        sqlCommand.Parameters.AddWithValue("@date", DateTime.Now);
                        sqlCommand.Parameters.AddWithValue("@val", $"{cashDifference - this.CasherValue:0.#,##.##}");
                        sqlCommand.Parameters.AddWithValue("@notes", "عجز اغلاق الوردية ");
                        sqlCommand.ExecuteNonQuery();
                    }

                    SqlCommand sqlCommand2 = new SqlCommand(
                        " Insert  into dbo.CasherClosed (type,user_id,startTime,endTime,ToT,CasherValue,diff)values(@type,@user_id,@startTime,@endTime,@ToT,@CasherValue,@diff) ",
                        this.conn, sqlTransaction);
                    sqlCommand2.Parameters.Add("@type", SqlDbType.Int).Value = type;
                    sqlCommand2.Parameters.Add("@user_id", SqlDbType.Int).Value = this.Username;
                    sqlCommand2.Parameters.Add("@startTime", SqlDbType.DateTime).Value = this.GetIso8601Date(this.StartDate.SelectedDate.Value);
                    sqlCommand2.Parameters.Add("@endTime", SqlDbType.DateTime).Value = this.GetIso8601Date(this.EndDate.SelectedDate.Value);
                    sqlCommand2.Parameters.Add("@ToT", SqlDbType.NVarChar).Value = obj.Net;
                    sqlCommand2.Parameters.Add("@CasherValue", SqlDbType.NVarChar).Value = Math.Round(this.CasherValue, 2);
                    sqlCommand2.Parameters.Add("@diff", SqlDbType.NVarChar).Value = obj.DiffVal;
                    sqlCommand2.ExecuteNonQuery();

                    sqlCommand = new SqlCommand(
                        "insert into CasherClosed_Sub(ClosedId,CashTotal,SAfeNetVal,ReturnSum,NetworkSum,AdditionVal,PostPoneSales,PostPoneRet,InsurVal,Discount,AllVAT,IsDeleted,HostingVal,Expenses,Purchases,ExtraTax)values(@ClosedId,@CashTotal,@SAfeNetVal,@ReturnSum,@NetworkSum,@AdditionVal,@PostPoneSales,@PostPoneRet,@InsurVal,@Discount,@AllVAT,@IsDeleted,@HostingVal,@Expenses,@Purchases,@ExtraTax)",
                        this.conn, sqlTransaction);
                    sqlCommand.Parameters.Add("@ClosedId", SqlDbType.Int).Value = this.CloseNo;
                    sqlCommand.Parameters.Add("@CashTotal", SqlDbType.Float).Value = $"{double.Parse(obj.CashVal):0.#,##.##}";
                    sqlCommand.Parameters.Add("@SAfeNetVal", SqlDbType.Float).Value = $"{double.Parse(obj.SAfeNetVal):0.#,##.##}";
                    sqlCommand.Parameters.Add("@ReturnSum", SqlDbType.Float).Value = $"{double.Parse(obj.ReturnSales):0.#,##.##}";
                    sqlCommand.Parameters.Add("@NetworkSum", SqlDbType.Float).Value = $"{double.Parse(obj.NetworkSales):0.#,##.##}";
                    sqlCommand.Parameters.Add("@AdditionVal", SqlDbType.Float).Value = $"{double.Parse(obj.AdditionVal):0.#,##.##}";
                    sqlCommand.Parameters.Add("@PostPoneSales", SqlDbType.Float).Value = $"{double.Parse(obj.PostPoneSales):0.#,##.##}";
                    sqlCommand.Parameters.Add("@PostPoneRet", SqlDbType.Float).Value = $"{double.Parse(obj.PostPoneRet):0.#,##.##}";
                    sqlCommand.Parameters.Add("@InsurVal", SqlDbType.Float).Value = $"{double.Parse(obj.InsurVal):0.#,##.##}";
                    sqlCommand.Parameters.Add("@Discount", SqlDbType.Float).Value = $"{double.Parse(obj.Discount):0.#,##.##}";
                    sqlCommand.Parameters.Add("@AllVAT", SqlDbType.Float).Value = $"{double.Parse(obj.VAT):0.#,##.##}";
                    sqlCommand.Parameters.Add("@ExtraTax", SqlDbType.Float).Value = RuntimeHelpers.GetObjectValue((!string.IsNullOrEmpty(obj.ExtraTax)) ? $"{double.Parse(obj.ExtraTax):0.#,##.##}" : ((object)0));
                    sqlCommand.Parameters.Add("@Expenses", SqlDbType.Float).Value = $"{double.Parse(obj.Expenses):0.#,##.##}";
                    sqlCommand.Parameters.Add("@Purchases", SqlDbType.Float).Value = $"{double.Parse(obj.Purchases):0.#,##.##}";
                    sqlCommand.Parameters.Add("@HostingVal", SqlDbType.Float).Value = $"{double.Parse(obj.HostingVal):0.#,##.##}";
                    sqlCommand.Parameters.Add("@IsDeleted", SqlDbType.Bit).Value = 0;
                    sqlCommand.ExecuteNonQuery();

                    this.insertCheckclose();
                    sqlTransaction.Commit();
                    Home.IsCashierClosed = true;

                    this.PrintDevexpress(1, obj, (List<CloseShiftCustomer>)closeData.CloseShiftCustomers);

                    if (type == 2)
                    {
                    }

                    if (!this.CloseShiftSetting.PrintGroups & !this.CloseShiftSetting.QuantPrint)
                    {
                        this.Close();
                        MainClass.lastCashercloseDate = DateTime.Now;
                        Home.stripPOS.IsEnabled = false;
                        Home.btnPOS2.IsEnabled = false;
                        return;
                    }

                    if (this.CloseShiftSetting.PrintGroups & this.CloseShiftSetting.QuantPrint)
                    {
                        this.SalesByGroups(type, obj);
                        this.SalesReport(type, obj);
                    }
                    else if (!this.CloseShiftSetting.PrintGroups & this.CloseShiftSetting.QuantPrint)
                    {
                        this.SalesReport(type, obj);
                    }
                    else if (this.CloseShiftSetting.PrintGroups & !this.CloseShiftSetting.QuantPrint)
                    {
                        this.SalesByGroups(type, obj);
                    }

                    this.PrintDevexpresDetails(1);
                    this.Close();
                    MainClass.lastCashercloseDate = DateTime.Now;
                    Home.stripPOS.IsEnabled = false;
                    Home.btnPOS2.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    sqlTransaction.Rollback();
                    MessageBox.Show("خطأ أثناء إغلاق اليومية" + Environment.NewLine +
                        "تفاصيل الخطأ: " + ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Hand);
                }
                finally
                {
                    if (this.conn.State == ConnectionState.Open)
                    {
                        this.conn.Close();
                    }
                }
            }
        }

        #endregion

        #region Check Close Methods

        public void insertCheckclose()
        {
            SqlCommand sqlCommand = new SqlCommand(Conversions.ToString(
                Operators.ConcatenateObject(Operators.ConcatenateObject(
                "select isnull(InvGlobalId,'')as invglobal from inv where date>=@date1 and date<=@date2 and sales_emp=",
                this.cmbUser.SelectedValue), " and branch=@branch ")), this.conn1);
            sqlCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = this.datefrom;
            sqlCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = this.dateTo;
            sqlCommand.Parameters.Add("@Branch", SqlDbType.Int).Value = MainClass.BranchNo;
            sqlCommand.ExecuteNonQuery();
            SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand);
            DataTable dataTable = new DataTable();
            sqlDataAdapter.Fill(dataTable);

            checked
            {
                int num = dataTable.Rows.Count - 1;
                for (int i = 0; i <= num; i++)
                {
                    SqlCommand sqlCommand2 = new SqlCommand(
                        "insert into Check_Close (InvGlobalId,ClosedId,Empid)values(@InvGlobalID,@ClosedId,@Empid)",
                        this.conn1);
                    sqlCommand2.Parameters.AddWithValue("@InvGlobalId", SqlDbType.NVarChar).Value =
                        RuntimeHelpers.GetObjectValue(dataTable.AsEnumerable().ElementAtOrDefault(i)["invglobal"]);
                    sqlCommand2.Parameters.AddWithValue("@ClosedId", SqlDbType.NVarChar).Value = this.CloseNo;
                    sqlCommand2.Parameters.AddWithValue("@Empid", SqlDbType.Int).Value = this.Username;
                    sqlCommand2.ExecuteNonQuery();
                }
            }
        }

        #endregion

        #region Get Close Data Methods

        private CloseShift GetCloseData(short Closetype, DateTime FromDate, DateTime ToDate, short EmpId)
        {
            CloseShift closeShift = new CloseShift();
            CloseShift result2;
            try
            {
                closeShift.EmpId = EmpId;
                string selectCommandText = "";

                switch (Closetype)
                {
                    case 3:
                        selectCommandText = "Select inv.InvGlobalID as invg,inv.Date as date Inv.inv_type As InvType ,Inv.bank as BankId, Inv.proc_type As ProcType , P.paytype As PayType,\r\n                        P.PaymentStatus As PaymentStatus,CASE WHEN P.PaymentStatus =3 THEN 0  ELSE sum(tot_net) end as InvNet,\r\n                        CASE WHEN P.PaymentStatus =3 THEN 0  ELSE sum(InvTotal) end as InvTotal , \r\n                        sum(CashPayment)as Cash,sum(MadaPayment)as Mada, 0 as Paid ,\r\n                        sum(Remainder)as Remainder,CASE WHEN P.PaymentStatus =3 THEN 0  ELSE sum(Inv.tax) end as VAT ,\r\n                        CASE WHEN P.PaymentStatus =3 THEN 0  ELSE sum(inv.ExtraVat) end as  ExtraVAT , \r\n                        CASE WHEN P.PaymentStatus =3 THEN 0  ELSE sum(Inv.minus) end as Discount,\r\n                        CASE WHEN P.PaymentStatus =3 THEN 0  ELSE sum(Inv.ItemsDiscount) end as ItemsDiscount,\r\n                        CASE WHEN P.PaymentStatus =3 THEN 0  ELSE sum(Inv.AdditionsTot) end as Additions ,\r\n                        CASE WHEN P.PaymentStatus =3 THEN 0  ELSE sum(Inv.Insurance) end as Insurance \r\n                        from Inv left join InvoicePayments as p on inv.InvGlobalID=p.InvGlobalID  \r\n                        where branch=@Branch and p.EmpId=@EmpId and p.paymentDate>=@date1 \r\n                        and p.paymentDate<=@date2 and  inv.Inv_Type=20 and Inv.IS_Deleted=0   \r\n                        group by Inv_Type,Proc_Type,payType,P.PaymentStatus,Inv.bank,inv.InvGlobalID,inv.date";
                        break;
                    case 1:
                        if (this.conn1.State != ConnectionState.Open)
                        {
                            this.conn1.Open();
                        }
                        new SqlCommand("update [Inv] set cash=tot_net where cash>tot_net and Inv_type=20 and sales_emp =" + Conversions.ToString((int)EmpId), this.conn1).ExecuteNonQuery();
                        new SqlCommand("  Update  [Inv] Set cash=tot_net, visa = 0 where visa >0 And pay_type=1 and  Inv_type=20 and sales_emp =" + Conversions.ToString((int)EmpId), this.conn1).ExecuteNonQuery();
                        new SqlCommand(" Update  [Inv] Set visa=tot_net, cash = 0 where cash >0 And pay_type=2 and  Inv_type=20 and sales_emp =" + Conversions.ToString((int)EmpId), this.conn1).ExecuteNonQuery();
                        selectCommandText = " select inv.date as date,inv.InvGlobalID as invg, Inv.inv_type As InvType , Inv.proc_type As ProcType ,pay_type as Paytype,inv.VATPercent ,1 as PaymentStatus,0 as Remainder ,sum(tot_net) as InvNet,sum(InvTotal) as InvTotal ,bank as BankId,\r\n               sum(cash)as cash,sum(visa)as Mada ,sum(tax) as VAT ,sum(ExtraVAT) as  ExtraVAT, sum(minus) as Discount, isnull(SUM(ItemsDiscount),0) as ItemsDiscount, sum(AdditionsTot) as Additions, sum(Insurance)as Insurance, sum(paid) as Paid  from Inv  where  branch=@Branch and sales_emp=@EmpId and (inv.proc_Type=1 or inv.proc_type=2)  and inv.Inv_Type=20 and Inv.date>=@date1 and Inv.date<=@date2  and Inv.IS_Deleted=0 and inv.InvGlobalID not in(select InvGlobalID from  Check_Close) group by Inv_Type,Proc_Type,pay_type,bank,inv.VATPercent,inv.InvGlobalID,inv.date";
                        break;
                }

                this.da = new SqlDataAdapter(selectCommandText, this.conn1);
                this.da.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = FromDate;
                this.da.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = ToDate;
                this.da.SelectCommand.Parameters.Add("@Branch", SqlDbType.Int).Value = MainClass.BranchNo;
                this.da.SelectCommand.Parameters.Add("@EmpId", SqlDbType.Int).Value = EmpId;
                this.dt = new DataTable();
                this.da.Fill(this.dt);

                closeShift.CashNet = 0m;
                closeShift.Net = 0m;
                closeShift.VATNet = 0m;

                this._entryDate = Conversions.ToDate(this.dt.AsEnumerable().ElementAtOrDefault(0)["Date"].ToString());
                this.DateFromclose = Conversions.ToDate(this.dt.AsEnumerable().ElementAtOrDefault(0)["Date"].ToString());

                checked
                {
                    int num = this.dt.Rows.Count - 1;
                    for (int i = 0; i <= num; i++)
                    {
                        if (Conversions.ToBoolean(Operators.AndObject(!this.CloseShiftSetting.IncSaleInv,
                            Operators.CompareObjectEqual(this.dt.Rows[i]["InvType"], 2, TextCompare: false))))
                        {
                            continue;
                        }

                        CloseShiftDetail closeShiftDetail = new CloseShiftDetail();
                        closeShiftDetail.InvoiceType = (int)Math.Round(Math.Round(Conversion.Val(
                            Operators.ConcatenateObject("", this.dt.Rows[i]["InvType"])), 2));
                        closeShiftDetail.ProcType = (int)Math.Round(Math.Round(Conversion.Val(
                            Operators.ConcatenateObject("", this.dt.Rows[i]["ProcType"])), 2));
                        closeShiftDetail.PayType = (int)Math.Round(Conversion.Val(
                            Operators.ConcatenateObject("", this.dt.Rows[i]["PayType"])));
                        closeShiftDetail.PaymentStatus = (int)Math.Round(Conversion.Val(
                            Operators.ConcatenateObject("", this.dt.Rows[i]["PaymentStatus"])));
                        closeShiftDetail.InvNet = new decimal(Math.Round(Conversion.Val(
                            Operators.ConcatenateObject("", this.dt.Rows[i]["InvNet"])), 2));
                        closeShiftDetail.InvGlobalId = Conversions.ToString(this.dt.Rows[i]["invg"]);

                        string text = this.dt.Rows[i]["InvTotal"].ToString();
                        try
                        {
                            if (double.TryParse(text, out var result))
                            {
                                text = result.ToString("0.00");
                            }
                        }
                        catch
                        {
                        }

                        closeShiftDetail.InvTotal = new decimal(Convert.ToDouble(text));

                        if (Conversions.ToBoolean(Operators.AndObject(Operators.AndObject(
                            Conversion.Val(Operators.ConcatenateObject("", this.dt.Rows[i]["ProcType"])) == 2.0,
                            Operators.CompareObjectEqual(this.dt.Rows[i]["InvType"], 20, TextCompare: false)),
                            closeShiftDetail.PayType != -1)))
                        {
                            closeShiftDetail.Cash = new decimal(Math.Round(Conversion.Val(
                                Operators.ConcatenateObject("", this.dt.Rows[i]["InvNet"])), 2));
                        }
                        else
                        {
                            closeShiftDetail.Cash = new decimal(Math.Round(Conversion.Val(
                                Operators.ConcatenateObject("", this.dt.Rows[i]["Cash"])), 2));
                        }

                        closeShiftDetail.Mada = new decimal(Math.Round(Conversion.Val(
                            Operators.ConcatenateObject("", this.dt.Rows[i]["Mada"])), 2));
                        closeShiftDetail.Additions = new decimal(Math.Round(Conversion.Val(
                            Operators.ConcatenateObject("", this.dt.Rows[i]["Additions"])), 2));
                        closeShiftDetail.Insurance = new decimal(Math.Round(Conversion.Val(
                            Operators.ConcatenateObject("", this.dt.Rows[i]["Insurance"])), 2));
                        closeShiftDetail.Discount = new decimal(Math.Round(Conversion.Val(
                            Operators.ConcatenateObject("", this.dt.Rows[i]["Discount"])), 2) +
                            Math.Round(Conversion.Val(Operators.ConcatenateObject("",
                            this.dt.Rows[i]["ItemsDiscount"])), 2));
                        closeShiftDetail.ExtraVAT = new decimal(Math.Round(Conversion.Val(
                            Operators.ConcatenateObject("", this.dt.Rows[i]["ExtraVAT"])), 2));

                        if (closeShiftDetail.PayType == 7)
                        {
                            closeShiftDetail.VAT = new decimal(Convert.ToDouble(closeShiftDetail.InvNet) -
                                Convert.ToDouble(closeShiftDetail.InvNet) / (1.0 +
                                Convert.ToDouble(RuntimeHelpers.GetObjectValue(this.dt.Rows[i]["VATPercent"])) / 100.0));
                        }
                        else
                        {
                            closeShiftDetail.VAT = new decimal(Math.Round(Conversion.Val(
                                Operators.ConcatenateObject("", this.dt.Rows[i]["VAT"])), 2));
                        }

                        if (closeShiftDetail.PayType == 7)
                        {
                            closeShiftDetail.Remainder = 0m;
                        }
                        else
                        {
                            closeShiftDetail.Remainder = new decimal(Math.Round(Conversion.Val(
                                Operators.ConcatenateObject("", this.dt.Rows[i]["Remainder"])), 2));
                        }

                        closeShiftDetail.BankId = (int)Math.Round(Conversion.Val(
                            Operators.ConcatenateObject("", this.dt.Rows[i]["BankId"])));
                        closeShift.CloseShiftDetails.Add(closeShiftDetail);
                        this.Datetoclose = Conversions.ToDate(this.dt.Rows[i]["Date"]);
                    }

                    if (this.CloseShiftSetting.IncExpenses)
                    {
                        this.da = new SqlDataAdapter("select sum(NetVal) as Net,PaymentType as PayType   from Receipts where BranchID=" +
                            Conversions.ToString(MainClass.BranchNo) + "  and EmpId =" +
                            Conversions.ToString(unchecked((int)EmpId)) +
                            "  and  ReceiptDate>=@date1  and ReceiptDate<=@date2 and ISDeleted=0 and ReceiptType=8 group by PaymentType ",
                            this.conn1);
                        this.da.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = FromDate;
                        this.da.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = ToDate;
                        this.dt = new DataTable();
                        this.da.Fill(this.dt);

                        if (this.dt.Rows.Count > 0)
                        {
                            CloseShiftDetail closeShiftDetail2 = new CloseShiftDetail();
                            closeShiftDetail2.InvoiceType = 8;
                            closeShiftDetail2.ProcType = 2;
                            closeShiftDetail2.PayType = (int)Math.Round(Conversion.Val(
                                Operators.ConcatenateObject("", this.dt.Rows[0]["PayType"])));
                            closeShiftDetail2.PaymentStatus = 1;
                            closeShiftDetail2.InvNet = new decimal(Conversion.Val(
                                Operators.ConcatenateObject("", this.dt.Rows[0]["Net"])));
                            closeShiftDetail2.InvTotal = new decimal(Conversion.Val(
                                Operators.ConcatenateObject("", this.dt.Rows[0]["Net"])));

                            if (closeShiftDetail2.PayType == 1)
                            {
                                closeShiftDetail2.Cash = new decimal(Conversion.Val(
                                    Operators.ConcatenateObject("", this.dt.Rows[0]["Net"])));
                                closeShiftDetail2.Mada = 0m;
                            }
                            else if (closeShiftDetail2.PayType == 2)
                            {
                                closeShiftDetail2.Mada = new decimal(Conversion.Val(
                                    Operators.ConcatenateObject("", this.dt.Rows[0]["Net"])));
                                closeShiftDetail2.Cash = 0m;
                            }

                            closeShiftDetail2.Additions = 0m;
                            closeShiftDetail2.Insurance = 0m;
                            closeShiftDetail2.Discount = 0m;
                            closeShiftDetail2.VAT = 0m;
                            closeShiftDetail2.ExtraVAT = 0m;
                            closeShift.CloseShiftDetails.Add(closeShiftDetail2);
                        }
                    }

                    if (Closetype == 2)
                    {
                        this.da = new SqlDataAdapter("select proc_type as ProcType, pay_type as PayType ,sum(tot_net) as InvNet ,\r\n               sum(tot_Rent) as InvTotal, sum(cash)as Cash,sum(visa)as Mada ,sum(tax) as VAT , sum(Discount) as Discount, sum(tot_Additions) as Additions, sum(Insurance)as Insurance  from RentInvoice  where RentInvoice.sales_emp=@EmpId  and date>=@date1 and date<=@date2  and IS_Deleted=0 group by proc_type,pay_type ",
                            this.conn1);
                        this.da.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = FromDate;
                        this.da.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = ToDate;
                        this.da.SelectCommand.Parameters.Add("@EmpId", SqlDbType.Int).Value = EmpId;
                        this.dt = new DataTable();
                        this.da.Fill(this.dt);

                        int num2 = this.dt.Rows.Count - 1;
                        for (int j = 0; j <= num2; j++)
                        {
                            CloseShiftDetail closeShiftDetail3 = new CloseShiftDetail();
                            closeShiftDetail3.InvoiceType = 11;
                            closeShiftDetail3.ProcType = (int)Math.Round(Math.Round(Conversion.Val(
                                Operators.ConcatenateObject("", this.dt.Rows[j]["ProcType"])), 2));
                            closeShiftDetail3.PayType = (int)Math.Round(Math.Round(Conversion.Val(
                                Operators.ConcatenateObject("", this.dt.Rows[j]["PayType"])), 2));
                            closeShiftDetail3.PaymentStatus = 1;
                            closeShiftDetail3.InvNet = new decimal(Math.Round(Conversion.Val(
                                Operators.ConcatenateObject("", this.dt.Rows[j]["InvNet"])), 2));
                            closeShiftDetail3.InvTotal = new decimal(Math.Round(Conversion.Val(
                                Operators.ConcatenateObject("", this.dt.Rows[j]["InvTotal"])), 2));
                            closeShiftDetail3.Cash = new decimal(Math.Round(Conversion.Val(
                                Operators.ConcatenateObject("", this.dt.Rows[j]["Cash"])), 2));
                            closeShiftDetail3.Mada = new decimal(Math.Round(Conversion.Val(
                                Operators.ConcatenateObject("", this.dt.Rows[j]["Mada"])), 2));
                            closeShiftDetail3.Additions = new decimal(Math.Round(Conversion.Val(
                                Operators.ConcatenateObject("", this.dt.Rows[j]["Additions"])), 2));
                            closeShiftDetail3.Insurance = new decimal(Math.Round(Conversion.Val(
                                Operators.ConcatenateObject("", this.dt.Rows[j]["Insurance"])), 2));
                            closeShiftDetail3.Discount = new decimal(Math.Round(Conversion.Val(
                                Operators.ConcatenateObject("", this.dt.Rows[j]["Discount"])), 2));
                            closeShiftDetail3.VAT = new decimal(Math.Round(Conversion.Val(
                                Operators.ConcatenateObject("", this.dt.Rows[j]["VAT"])), 2));
                            closeShiftDetail3.ExtraVAT = 0m;
                            closeShiftDetail3.Remainder = 0m;
                            closeShift.CloseShiftDetails.Add(closeShiftDetail3);
                        }
                    }

                    result2 = closeShift;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء إحتساب إغلاق اليومية" + Environment.NewLine +
                    "تفاصيل الخطأ: " + ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Hand);
                result2 = null;
            }
            finally
            {
                if (this.conn.State == ConnectionState.Open)
                {
                    this.conn.Close();
                }
            }

            return result2;
        }

        #endregion

        #region Helper Methods

        public string GetIso8601Date(DateTime time)
        {
            string text = time.ToString("yyyy-MM-ddTHH:mm:ss.fff");
            TimeSpan utcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(time);
            if (utcOffset > TimeSpan.Zero)
            {
                text += "+";
            }
            return text + $"{utcOffset.Hours:00}:{utcOffset.Minutes:00}";
        }

        public int GetUserTreasuryAccemp()
        {
            SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                selectConnection: MainClass.ConnObj(),
                selectCommandText: "select Stocks.Acc_Code from Stocks,Stock_Emps where Stocks.id=Stock_Emps.stock_id and  Stock_Emps.emp_id=" +
                this.Username + " and Stocks.branch=" + Conversions.ToString(MainClass.BranchNo));
            DataTable dataTable = new DataTable();
            sqlDataAdapter.Fill(dataTable);
            if (dataTable.Rows.Count > 0)
            {
                return Conversions.ToInteger(dataTable.Rows[0]["Acc_Code"]);
            }
            return -1;
        }

        private string GetGroupID(int Currency_id)
        {
            SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                "select group_id from Items where id=" + Conversions.ToString(Currency_id),
                this.conn);
            DataTable dataTable = new DataTable();
            sqlDataAdapter.Fill(dataTable);
            if (dataTable.Rows.Count > 0)
            {
                return dataTable.Rows[0][0].ToString();
            }
            return "";
        }

        private string GetGroupName(int Group_id)
        {
            SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                "select name from ItemsCategory where CategoryId=" + Conversions.ToString(Group_id),
                this.conn);
            DataTable dataTable = new DataTable();
            sqlDataAdapter.Fill(dataTable);
            if (dataTable.Rows.Count > 0)
            {
                return dataTable.Rows[0][0].ToString();
            }
            return "";
        }

        private string GetCurrencyName(int Currency_id)
        {
            SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                "select name from Items where id=" + Conversions.ToString(Currency_id),
                this.conn);
            DataTable dataTable = new DataTable();
            sqlDataAdapter.Fill(dataTable);
            if (dataTable.Rows.Count > 0)
            {
                return dataTable.Rows[0][0].ToString();
            }
            return "";
        }

        #endregion

        #region Binding Methods

        private void BindingCloseShift(ref CloseShift closeShift, ref CashierCloseData obj)
        {
            obj.shiftNo = Conversions.ToString(closeShift.ShiftNo);
            obj.CloseNo = Conversions.ToString(closeShift.ClosedId);
            obj.dateTo = Conversions.ToString(closeShift.ShiftEnd);
            obj.datefrom = Conversions.ToString(closeShift.ShiftStart);
            obj.Username = this.Username;
            obj.CashVal = Conversions.ToString(0);
            obj.ReturnSales = Conversions.ToString(0);
            obj.NetworkSales = Conversions.ToString(0);
            obj.PostPoneSales = Conversions.ToString(0);
            obj.PostPoneRet = Conversions.ToString(0);
            obj.AdditionVal = Conversions.ToString(0);
            obj.InsurVal = Conversions.ToString(0);
            obj.Discount = Conversions.ToString(0);
            obj.HostingVal = Conversions.ToString(0);
            obj.Expenses = Conversions.ToString(0);
            obj.Purchases = Conversions.ToString(0);

            foreach (CloseShiftDetail closeShiftDetail in closeShift.CloseShiftDetails)
            {
                InvoiceObj invoiceObj = new InvoiceObj(closeShiftDetail.InvoiceType, 1);
                double calculatedVAT;
                double calculatedTotal;

                if (closeShiftDetail.PayType == 7)
                {
                    double totalAmount = Convert.ToDouble(decimal.Add(decimal.Add(
                        closeShiftDetail.Cash, closeShiftDetail.Mada), closeShiftDetail.Visa));
                    calculatedVAT = totalAmount - Math.Round(totalAmount / (1.0 + invoiceObj.VAT / 100.0), 3);
                    calculatedTotal = totalAmount - calculatedVAT;
                }
                else
                {
                    Convert.ToDouble(closeShiftDetail.InvNet);
                    calculatedVAT = Convert.ToDouble(closeShiftDetail.VAT);
                    calculatedTotal = Convert.ToDouble(closeShiftDetail.InvTotal);
                }

                if ((closeShiftDetail.InvoiceType == 20) & (closeShiftDetail.ProcType == 1) &
                    (closeShiftDetail.PayType != -1) & (closeShiftDetail.PayType != 5))
                {
                    closeShift.CashNet = decimal.Add(closeShift.CashNet, closeShiftDetail.Cash);
                    obj.CashVal = Conversions.ToString(double.Parse(obj.CashVal) +
                        Convert.ToDouble(closeShiftDetail.Cash));
                    obj.NetworkSales = Conversions.ToString(double.Parse(obj.NetworkSales) +
                        Convert.ToDouble(closeShiftDetail.Mada));
                }
                else if ((closeShiftDetail.InvoiceType == 20) & (closeShiftDetail.ProcType == 2) &
                    (closeShiftDetail.PayType != -1) & (closeShiftDetail.PayType != 5))
                {
                    closeShift.CashNet = decimal.Subtract(closeShift.CashNet, closeShiftDetail.Cash);
                }

                if ((closeShiftDetail.PayType == -1) & (closeShiftDetail.InvoiceType == 20))
                {
                    closeShift.Net = decimal.Add(closeShift.Net, new decimal(calculatedTotal));
                }

                if (closeShiftDetail.InvoiceType != 20 || !((closeShiftDetail.PayType != -1) &
                    (closeShiftDetail.PayType != 5)))
                {
                    continue;
                }

                if (closeShiftDetail.ProcType == 1)
                {
                    closeShift.Net = decimal.Add(closeShift.Net, closeShiftDetail.InvNet);
                    closeShift.VATNet = decimal.Add(closeShift.VATNet, closeShiftDetail.VAT);
                    closeShift.ExtaTax = decimal.Add(closeShift.ExtaTax, closeShiftDetail.ExtraVAT);
                    obj.AdditionVal = Conversions.ToString(double.Parse(obj.AdditionVal) +
                        Convert.ToDouble(closeShiftDetail.Additions));
                    obj.InsurVal = Conversions.ToString(double.Parse(obj.InsurVal) +
                        Convert.ToDouble(closeShiftDetail.Insurance));
                    obj.Discount = Conversions.ToString(double.Parse(obj.Discount) +
                        Convert.ToDouble(closeShiftDetail.Discount));

                    if (decimal.Compare(closeShiftDetail.Additions, 0m) != 0)
                    {
                        if (invoiceObj.PriceIncVAT)
                        {
                            string text = (Convert.ToDouble(closeShiftDetail.Additions) /
                                (1.0 + invoiceObj.VAT / 100.0)).ToString();
                            if (text.IndexOf('.') > -1)
                            {
                                text = text.Substring(0, text.IndexOf('.') + 3);
                            }
                        }
                    }

                    if (closeShiftDetail.PayType == -1)
                    {
                        obj.PostPoneSales = Conversions.ToString(double.Parse(obj.PostPoneSales) +
                            Convert.ToDouble(closeShiftDetail.InvNet));
                    }
                    else if (closeShiftDetail.PayType == 5)
                    {
                        obj.HostingVal = Conversions.ToString(double.Parse(obj.HostingVal) +
                            Convert.ToDouble(closeShiftDetail.InvNet));
                    }
                }
                else if (closeShiftDetail.ProcType == 2)
                {
                    closeShift.Net = decimal.Subtract(closeShift.Net, closeShiftDetail.InvNet);
                    closeShift.VATNet = decimal.Subtract(closeShift.VATNet, closeShiftDetail.VAT);
                    closeShift.ExtaTax = decimal.Subtract(closeShift.ExtaTax, closeShiftDetail.ExtraVAT);

                    if (closeShiftDetail.PayType != -1)
                    {
                        obj.ReturnSales = Conversions.ToString(double.Parse(obj.ReturnSales) +
                            Convert.ToDouble(decimal.Add(closeShiftDetail.Cash, closeShiftDetail.Mada)));
                    }

                    obj.Discount = Conversions.ToString(double.Parse(obj.Discount) -
                        Convert.ToDouble(closeShiftDetail.Discount));
                    obj.AdditionVal = Conversions.ToString(double.Parse(obj.AdditionVal) -
                        Convert.ToDouble(closeShiftDetail.Additions));
                    obj.InsurVal = Conversions.ToString(double.Parse(obj.InsurVal) -
                        Convert.ToDouble(closeShiftDetail.Insurance));

                    if (closeShiftDetail.PayType == -1)
                    {
                        obj.PostPoneRet = Conversions.ToString(double.Parse(obj.PostPoneRet) -
                            Convert.ToDouble(closeShiftDetail.InvNet));
                    }
                    else if (closeShiftDetail.PayType == 5)
                    {
                        obj.HostingVal = Conversions.ToString(double.Parse(obj.HostingVal) -
                            Convert.ToDouble(closeShiftDetail.InvNet));
                    }
                }
            }

            obj.SAfeNetVal = Conversions.ToString(Convert.ToDouble(closeShift.CashNet) -
                Conversions.ToDouble(obj.Expenses));
            obj.CasherValue = Conversions.ToString(closeShift.CashierBalance);
            obj.NetWithoutVAT = Conversions.ToString(decimal.Subtract(closeShift.CashNet, closeShift.VATNet));
            obj.ExtraTax = Conversions.ToString(closeShift.ExtaTax);
            obj.DiffVal = Conversions.ToString(Convert.ToDouble(closeShift.CashierBalance) -
                Conversions.ToDouble(obj.SAfeNetVal));
            obj.Net = Conversions.ToString(closeShift.Net);
            obj.VAT = Conversions.ToString(closeShift.VATNet);
        }

        #endregion

        #region Entry Binding Method

        public Entry BindCloseShiftToEntry1(CloseShift inv, bool EnterBalanceRequired)
        {
            checked
            {
                Entry result;
                try
                {
                    #region Initialize Variables

                    Treasury treasury = new Treasury(inv.Treasury);
                    Entry entry = new Entry();
                    List<Account> accountsList = new List<Account>();
                    List<CloseShiftBanks> banksList = new List<CloseShiftBanks>();
                    string branchCostCenter = Common.GetBranchCostCenter(MainClass.BranchNo);

                    // المتغيرات المالية مع أسماء واضحة
                    decimal totalCashInDrawer = 0m;          // إجمالي النقدي في الصندوق
                    decimal totalCashSales = 0m;             // إجمالي المبيعات النقدية
                    decimal totalNetworkSales = 0m;          // إجمالي مبيعات الشبكة
                    decimal totalVAT = 0m;                   // إجمالي ضريبة القيمة المضافة
                    decimal totalSalesAmount = 0m;           // إجمالي المبيعات
                    decimal totalReturnAmount = 0m;          // إجمالي المرتجعات
                    decimal totalAdditions = 0m;             // إجمالي الإضافات
                    decimal totalInsurance = 0m;             // إجمالي التأمين
                    decimal totalDiscount = 0m;              // إجمالي الخصومات
                    decimal totalExtraVAT = 0m;              // الضريبة الانتقائية
                    decimal totalRemainder = 0m;             // المبالغ المؤجلة
                    decimal totalPaidInvoices = 0m;          // الفواتير المسددة
                    decimal totalPostponedAmount = 0m;       // المبالغ المؤجلة الكلية

                    #endregion

                    #region Setup Entry Header

                    entry.EntryGlobalID = inv.EntryGlobalID;
                    entry.ClientCode = Sync.ClientCode;
                    entry.EntryNo = Conversions.ToInteger(inv.EntryGlobalID.Substring(
                        inv.EntryGlobalID.LastIndexOf("-") + 1));
                    entry.EntryDate = this._entryDate;
                    entry.ReffNo = Conversions.ToString(inv.ClosedId);
                    entry.RefDate = this._entryDate;
                    entry.Type = EntryType.EntryReceipt;
                    entry.State = 1;
                    string employeeName = Common.GetEmployeeName(inv.EmpId);
                    entry.Note = "اغلاق اليومية خاصة الموظف " + employeeName + " رقم" +
                        Conversions.ToString(inv.ClosedId);
                    entry.Branch = MainClass.BranchNo;
                    entry.EmpID = inv.EmpId;
                    entry.DistBranch = Sync.DistBranch;
                    entry.BranchType = Sync.BranchType;
                    entry.ISDeleted = false;

                    #endregion

                    #region Process Close Shift Details

                    foreach (CloseShiftDetail closeShiftDetail in inv.CloseShiftDetails)
                    {
                        InvoiceObj invoiceObj = new InvoiceObj(closeShiftDetail.InvoiceType, 1);
                        double calculatedVAT;
                        double calculatedTotal;

                        if (closeShiftDetail.PayType == 7)
                        {
                            double totalPayment = Convert.ToDouble(decimal.Add(decimal.Add(
                                closeShiftDetail.Cash, closeShiftDetail.Mada), closeShiftDetail.Visa));
                            calculatedVAT = totalPayment - Math.Round(totalPayment /
                                (1.0 + invoiceObj.VAT / 100.0), 3);
                            calculatedTotal = totalPayment - calculatedVAT;
                        }
                        else
                        {
                            Convert.ToDouble(closeShiftDetail.InvNet);
                            calculatedVAT = Convert.ToDouble(closeShiftDetail.VAT);
                            calculatedTotal = Convert.ToDouble(closeShiftDetail.InvTotal);
                        }

                        // معالجة المبيعات النقدية
                        if ((closeShiftDetail.InvoiceType == 20) & (closeShiftDetail.ProcType == 1) &
                            (closeShiftDetail.PayType != -1) & (closeShiftDetail.PayType != 5))
                        {
                            totalCashInDrawer = decimal.Add(totalCashInDrawer, closeShiftDetail.Cash);
                        }
                        else if ((closeShiftDetail.InvoiceType == 20) & (closeShiftDetail.ProcType == 2) &
                            (closeShiftDetail.PayType != -1) & (closeShiftDetail.PayType != 5))
                        {
                            totalCashInDrawer = decimal.Subtract(totalCashInDrawer, closeShiftDetail.Cash);
                        }

                        if ((closeShiftDetail.PayType == -1) & (closeShiftDetail.InvoiceType == 20))
                        {
                            totalPostponedAmount = new decimal(Convert.ToDouble(totalPostponedAmount) + calculatedTotal);
                        }

                        if (closeShiftDetail.InvoiceType != 20 || !((closeShiftDetail.PayType != -1) &
                            (closeShiftDetail.PayType != 5)))
                        {
                            continue;
                        }

                        if (closeShiftDetail.ProcType == 1)
                        {
                            totalCashSales = decimal.Add(totalCashSales, closeShiftDetail.Cash);

                            if ((closeShiftDetail.PayType == 2) & (closeShiftDetail.BankId > 2))
                            {
                                CloseShiftBanks closeShiftBanks = new CloseShiftBanks();
                                closeShiftBanks.BankId = closeShiftDetail.BankId;
                                closeShiftBanks.Amount = closeShiftDetail.Mada;
                                banksList.Add(closeShiftBanks);
                            }
                            else
                            {
                                totalNetworkSales = decimal.Add(totalNetworkSales, closeShiftDetail.Mada);
                            }

                            totalVAT = new decimal(Convert.ToDouble(totalVAT) + calculatedVAT);
                            totalExtraVAT = decimal.Add(totalExtraVAT, closeShiftDetail.ExtraVAT);
                            totalSalesAmount = new decimal(Convert.ToDouble(totalSalesAmount) + calculatedTotal);

                            if (decimal.Compare(closeShiftDetail.Additions, 0m) != 0)
                            {
                                totalAdditions = decimal.Add(totalAdditions, closeShiftDetail.Additions);
                                if (invoiceObj.PriceIncVAT)
                                {
                                    string additionText = (Convert.ToDouble(closeShiftDetail.Additions) /
                                        (1.0 + invoiceObj.VAT / 100.0)).ToString();
                                    if (additionText.IndexOf('.') > -1)
                                    {
                                        additionText = additionText.Substring(0, additionText.IndexOf('.') + 3);
                                    }
                                    string additionValue = (string.IsNullOrEmpty(additionText) ? "0" : additionText);
                                    totalAdditions = new decimal(Convert.ToDouble(totalAdditions) -
                                        (Convert.ToDouble(closeShiftDetail.Additions) - Convert.ToDouble(additionValue)));
                                }
                            }
                            else
                            {
                                totalAdditions = 0m;
                            }

                            totalInsurance = decimal.Add(totalInsurance, closeShiftDetail.Insurance);
                            totalDiscount = decimal.Add(totalDiscount, closeShiftDetail.Discount);
                            totalRemainder = decimal.Add(totalRemainder, closeShiftDetail.Remainder);

                            if (closeShiftDetail.PaymentStatus == 3)
                            {
                                totalPaidInvoices = decimal.Add(totalPaidInvoices, closeShiftDetail.Cash);
                                totalPaidInvoices = decimal.Add(totalPaidInvoices, closeShiftDetail.Mada);
                            }
                        }
                        else
                        {
                            if (closeShiftDetail.ProcType != 2)
                            {
                                continue;
                            }

                            totalCashSales = decimal.Subtract(totalCashSales, closeShiftDetail.Cash);

                            if ((closeShiftDetail.PayType == 2) & (closeShiftDetail.BankId > 2))
                            {
                                CloseShiftBanks closeShiftBanks2 = new CloseShiftBanks();
                                closeShiftBanks2.BankId = closeShiftDetail.BankId;
                                closeShiftBanks2.Amount = decimal.Subtract(closeShiftBanks2.Amount, closeShiftDetail.Mada);
                                banksList.Add(closeShiftBanks2);
                            }
                            else
                            {
                                totalNetworkSales = decimal.Add(totalNetworkSales, closeShiftDetail.Mada);
                            }

                            totalVAT = new decimal(Convert.ToDouble(totalVAT) - calculatedVAT);
                            totalExtraVAT = decimal.Subtract(totalExtraVAT, closeShiftDetail.ExtraVAT);
                            totalReturnAmount = new decimal(Convert.ToDouble(totalReturnAmount) + calculatedTotal);

                            if (decimal.Compare(closeShiftDetail.Additions, 0m) != 0)
                            {
                                totalAdditions = decimal.Subtract(totalAdditions, closeShiftDetail.Additions);
                                if (invoiceObj.PriceIncVAT)
                                {
                                    string additionText2 = (Convert.ToDouble(closeShiftDetail.Additions) /
                                        (1.0 + invoiceObj.VAT / 100.0)).ToString();
                                    if (additionText2.IndexOf('.') > -1)
                                    {
                                        additionText2 = additionText2.Substring(0, additionText2.IndexOf('.') + 3);
                                    }
                                    string additionValue2 = (string.IsNullOrEmpty(additionText2) ? "0" : additionText2);
                                    totalAdditions = new decimal(Convert.ToDouble(totalAdditions) +
                                        (Convert.ToDouble(closeShiftDetail.Additions) - Convert.ToDouble(additionValue2)));
                                }
                            }
                            else
                            {
                                totalAdditions = 0m;
                            }

                            totalInsurance = decimal.Subtract(totalInsurance, closeShiftDetail.Insurance);
                            totalDiscount = decimal.Subtract(totalDiscount, closeShiftDetail.Discount);
                        }
                    }

                    #endregion

                    #region Setup Cashier Balance

                    if (!EnterBalanceRequired & (decimal.Compare(inv.CashierBalance, 0m) == 0))
                    {
                        inv.CashierBalance = totalCashSales;
                    }

                    #endregion

                    #region Create Account Entries - Cash

                    if (decimal.Compare(totalCashSales, 0m) > 0)
                    {
                        double cashAmount = Convert.ToDouble(totalCashSales);
                        Account account = new Account();
                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;

                        if (MainClass.EmpNo == 0)
                        {
                            account.Name = treasury.Name;
                            account.Code = treasury.AccCode;
                        }
                        else
                        {
                            account.Name = Common.GetAccountName(Conversions.ToInteger(User.TreasuryAcc));
                            account.Code = User.TreasuryAcc;
                        }

                        if (cashAmount > 0.0)
                        {
                            account.Debt = Conversions.ToDouble($"{cashAmount:0.#,##.##}");
                            account.Credit = 0.0;
                        }
                        else if (cashAmount < 0.0)
                        {
                            account.Debt = 0.0;
                            account.Credit = Conversions.ToDouble($"{-1.0 * cashAmount:0.#,##.##}");
                        }

                        account.Note = " نقدي في الصندوق " + employeeName + " : خاصة الموظف: رقم" +
                            Conversions.ToString(inv.ClosedId);
                        account.CCcode = Conversions.ToString(-1);
                        accountsList.Add(account);
                    }
                    else if (decimal.Compare(totalCashSales, 0m) < 0)
                    {
                        double cashAmount = Convert.ToDouble(totalCashSales);
                        Account account = new Account();
                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;

                        if (MainClass.EmpNo == 0)
                        {
                            account.Name = treasury.Name;
                            account.Code = treasury.AccCode;
                        }
                        else
                        {
                            account.Name = Common.GetAccountName(Conversions.ToInteger(User.TreasuryAcc));
                            account.Code = User.TreasuryAcc;
                        }

                        if (cashAmount > 0.0)
                        {
                            account.Debt = Conversions.ToDouble($"{cashAmount:0.#,##.##}");
                            account.Credit = 0.0;
                        }
                        else if (cashAmount < 0.0)
                        {
                            account.Debt = 0.0;
                            account.Credit = Conversions.ToDouble($"{-1.0 * cashAmount:0.#,##.##}");
                        }

                        account.Note = " نقدي في الصندوق " + employeeName + " : خاصة الموظف: رقم" +
                            Conversions.ToString(inv.ClosedId);
                        account.CCcode = Conversions.ToString(-1);
                        accountsList.Add(account);
                    }

                    #endregion

                    #region Create Account Entries - Network

                    if (decimal.Compare(totalNetworkSales, 0m) > 0)
                    {
                        double networkAmount = Convert.ToDouble(totalNetworkSales);
                        Account account = new Account();
                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;
                        account.Name = Common.GetAccountName(Conversions.ToInteger("1221001"));
                        account.Code = "1221001";

                        if (networkAmount > 0.0)
                        {
                            account.Debt = Conversions.ToDouble($"{networkAmount:0.#,##.##}");
                            account.Credit = 0.0;
                        }
                        else if (networkAmount < 0.0)
                        {
                            account.Debt = 0.0;
                            account.Credit = Conversions.ToDouble($"{-1.0 * networkAmount:0.#,##.##}");
                        }

                        account.Note = " شبكة " + employeeName + " : خاصة الموظف: رقم" +
                            Conversions.ToString(inv.ClosedId);
                        account.CCcode = Conversions.ToString(-1);
                        accountsList.Add(account);
                    }

                    #endregion

                    #region Create Account Entries - Banks

                    if (banksList.Count > 0)
                    {
                        var bankGroups = (from x in banksList
                                          group x by x.BankId into y
                                          select new
                                          {
                                              Total = y.Sum(z => z.Amount),
                                              BankID = y.Key
                                          }).ToList();

                        if (bankGroups.Count > 0)
                        {
                            foreach (var bankItem in bankGroups)
                            {
                                double bankAmount = Convert.ToDouble(bankItem.Total);
                                Account account = new Account();
                                Bank bank = new Bank(bankItem.BankID);
                                account.EntryGlobalID = entry.EntryGlobalID;
                                account.EntryNo = entry.EntryNo;
                                account.Name = bank.Name;
                                account.Code = bank.AccCode;

                                if (bankAmount > 0.0)
                                {
                                    account.Debt = Conversions.ToDouble($"{bankAmount:0.#,##.##}");
                                    account.Credit = 0.0;
                                }
                                else if (bankAmount < 0.0)
                                {
                                    account.Debt = 0.0;
                                    account.Credit = Conversions.ToDouble($"{-1.0 * bankAmount:0.#,##.##}");
                                }

                                account.Note = " " + bank.Name + " " + employeeName + " : خاصة الموظف: رقم" +
                                    Conversions.ToString(inv.ClosedId);
                                account.CCcode = Conversions.ToString(-1);
                                accountsList.Add(account);
                            }
                        }
                    }

                    #endregion

                    #region Create Account Entries - Sales

                    totalSalesAmount = decimal.Add(totalSalesAmount, totalDiscount);

                    if (decimal.Compare(totalSalesAmount, 0m) > 0)
                    {
                        double salesAmount = Convert.ToDouble(totalSalesAmount);
                        Account account = new Account();
                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;
                        account.Name = Common.GetAccountName(Conversions.ToInteger("4100001"));
                        account.Code = Conversions.ToString(4100001);

                        if (salesAmount < 0.0)
                        {
                            account.Debt = Conversions.ToDouble($"{-1.0 * salesAmount:0.#,##.##}");
                            account.Credit = 0.0;
                        }
                        else if (salesAmount > 0.0)
                        {
                            account.Debt = 0.0;
                            account.Credit = Conversions.ToDouble($"{salesAmount:0.#,##.##}");
                        }

                        account.Note = " مبيعات " + employeeName + " : خاصة الموظف: رقم" +
                            Conversions.ToString(inv.ClosedId);
                        account.CCcode = branchCostCenter;
                        accountsList.Add(account);
                    }

                    #endregion

                    #region Create Account Entries - Returns

                    if (decimal.Compare(totalReturnAmount, 0m) > 0)
                    {
                        double returnAmount = Convert.ToDouble(totalReturnAmount);
                        Account account = new Account();
                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;
                        account.Name = Common.GetAccountName(Conversions.ToInteger("4100002"));
                        account.Code = Conversions.ToString(4100002);

                        if (returnAmount < 0.0)
                        {
                            account.Debt = 0.0;
                            account.Credit = Conversions.ToDouble($"{-1.0 * returnAmount:0.#,##.##}");
                        }
                        else if (returnAmount > 0.0)
                        {
                            account.Debt = Conversions.ToDouble($"{returnAmount:0.#,##.##}");
                            account.Credit = 0.0;
                        }

                        account.Note = " مرتجع مبيعات " + employeeName + " : خاصة الموظف: رقم" +
                            Conversions.ToString(inv.ClosedId);
                        account.CCcode = branchCostCenter;
                        accountsList.Add(account);
                    }

                    #endregion

                    #region Create Account Entries - Extra VAT

                    if (decimal.Compare(totalExtraVAT, 0m) != 0)
                    {
                        double extraVATAmount = Convert.ToDouble(totalExtraVAT);
                        Account account = new Account();
                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;
                        account.Name = Common.GetAccountName(Conversions.ToInteger("2222002"));
                        account.Code = Conversions.ToString(2222002);

                        if (extraVATAmount < 0.0)
                        {
                            account.Debt = Conversions.ToDouble($"{-1.0 * extraVATAmount:0.#,##.##}");
                            account.Credit = 0.0;
                        }
                        else if (extraVATAmount > 0.0)
                        {
                            account.Debt = 0.0;
                            account.Credit = Conversions.ToDouble($"{extraVATAmount:0.#,##.##}");
                        }

                        account.Note = "الضريبة الإنتقائية" + employeeName + " : خاصة الموظف: رقم" +
                            Conversions.ToString(inv.ClosedId);
                        account.CCcode = Conversions.ToString(-1);
                        accountsList.Add(account);
                    }

                    #endregion

                    #region Create Account Entries - VAT

                    if (decimal.Compare(totalVAT, 0m) != 0)
                    {
                        double vatAmount = Convert.ToDouble(decimal.Subtract(totalVAT, totalExtraVAT));
                        Account account = new Account();
                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;
                        account.Name = Common.GetAccountName(Conversions.ToInteger("2222001"));
                        account.Code = Conversions.ToString(2222001);

                        if (vatAmount < 0.0)
                        {
                            account.Debt = Conversions.ToDouble($"{-1.0 * vatAmount:0.#,##.##}");
                            account.Credit = 0.0;
                        }
                        else if (vatAmount > 0.0)
                        {
                            account.Debt = 0.0;
                            account.Credit = Conversions.ToDouble($"{vatAmount:0.#,##.##}");
                        }

                        account.Note = " الضريبة المضافة " + employeeName + " : خاصة الموظف: رقم" +
                            Conversions.ToString(inv.ClosedId);
                        account.CCcode = Conversions.ToString(-1);
                        accountsList.Add(account);
                    }

                    #endregion

                    #region Create Account Entries - Additions

                    if (decimal.Compare(totalAdditions, 0m) != 0)
                    {
                        double additionsAmount = Convert.ToDouble(totalAdditions);
                        Account account = new Account();
                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;
                        account.Name = Common.GetAccountName(Conversions.ToInteger("4200002"));
                        account.Code = Conversions.ToString(4200002);

                        if (additionsAmount < 0.0)
                        {
                            account.Debt = Conversions.ToDouble($"{-1.0 * additionsAmount:0.#,##.##}");
                            account.Credit = 0.0;
                        }
                        else if (additionsAmount > 0.0)
                        {
                            account.Debt = 0.0;
                            account.Credit = Conversions.ToDouble($"{additionsAmount:0.#,##.##}");
                        }

                        account.Note = " إضافات " + employeeName + " : خاصة الموظف: رقم" +
                            Conversions.ToString(inv.ClosedId);
                        account.CCcode = Conversions.ToString(-1);
                        accountsList.Add(account);
                    }

                    #endregion

                    #region Create Account Entries - Insurance

                    if (decimal.Compare(totalInsurance, 0m) != 0)
                    {
                        double insuranceAmount = Convert.ToDouble(totalInsurance);
                        Account account = new Account();
                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;
                        account.Name = Common.GetAccountName(Conversions.ToInteger("22210001"));
                        account.Code = Conversions.ToString(22210001);

                        if (insuranceAmount < 0.0)
                        {
                            account.Debt = Conversions.ToDouble($"{-1.0 * insuranceAmount:0.#,##.##}");
                            account.Credit = 0.0;
                        }
                        else if (insuranceAmount > 0.0)
                        {
                            account.Debt = 0.0;
                            account.Credit = Conversions.ToDouble($"{insuranceAmount:0.#,##.##}");
                        }

                        account.Note = " تأمين " + employeeName + " : خاصة الموظف: رقم" +
                            Conversions.ToString(inv.ClosedId);
                        account.CCcode = Conversions.ToString(-1);
                        accountsList.Add(account);
                    }

                    #endregion

                    #region Create Account Entries - Discount

                    if (decimal.Compare(totalDiscount, 0m) != 0)
                    {
                        double discountAmount = Convert.ToDouble(totalDiscount);
                        Account account = new Account();
                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;
                        account.Name = Common.GetAccountName(Conversions.ToInteger("4100003"));
                        account.Code = Conversions.ToString(4100003);

                        if (discountAmount > 0.0)
                        {
                            account.Debt = Conversions.ToDouble($"{discountAmount:0.#,##.##}");
                            account.Credit = 0.0;
                        }
                        else if (discountAmount < 0.0)
                        {
                            account.Debt = 0.0;
                            account.Credit = Conversions.ToDouble($"{-1.0 * discountAmount:0.#,##.##}");
                        }

                        account.Note = " خصومات ممنوحة " + employeeName + " : خاصة الموظف: رقم" +
                            Conversions.ToString(inv.ClosedId);
                        account.CCcode = Conversions.ToString(-1);
                        accountsList.Add(account);
                    }

                    #endregion

                    #region Create Account Entries - Remainder

                    if (decimal.Compare(totalRemainder, 0m) != 0)
                    {
                        double remainderAmount = Convert.ToDouble(totalRemainder);
                        Account account = new Account();
                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;
                        account.Name = Common.GetAccountName(Conversions.ToInteger("12310001"));
                        account.Code = Conversions.ToString(12310001);

                        if (remainderAmount > 0.0)
                        {
                            account.Debt = Conversions.ToDouble($"{remainderAmount:0.#,##.##}");
                            account.Credit = 0.0;
                        }
                        else if (remainderAmount < 0.0)
                        {
                            account.Debt = 0.0;
                            account.Credit = Conversions.ToDouble($"{-1.0 * remainderAmount:0.#,##.##}");
                        }

                        account.Note = " مبالغ مستحقة  " + employeeName + " : خاصة الموظف: رقم" +
                            Conversions.ToString(inv.ClosedId);
                        account.CCcode = Conversions.ToString(-1);
                        accountsList.Add(account);
                    }

                    #endregion

                    #region Create Account Entries - Paid Invoices

                    if (decimal.Compare(totalPaidInvoices, 0m) > 0)
                    {
                        double paidAmount = Convert.ToDouble(totalPaidInvoices);
                        Account account = new Account();
                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;
                        account.Name = Common.GetAccountName(Conversions.ToInteger("12310001"));
                        account.Code = Conversions.ToString(12310001);
                        account.Debt = 0.0;
                        account.Credit = Conversions.ToDouble($"{paidAmount:0.#,##.##}");
                        account.Note = " مبالغ تم تسديدة  " + employeeName + " : خاصة الموظف: رقم" +
                            Conversions.ToString(inv.ClosedId);
                        account.CCcode = Conversions.ToString(-1);
                        accountsList.Add(account);
                    }

                    #endregion

                    #region Create Account Entries - Cash Adjustment

                    if (decimal.Compare(totalCashInDrawer, 0m) != 0)
                    {
                        Account account = new Account();
                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;

                        if (MainClass.EmpNo == 0)
                        {
                            account.Name = treasury.Name;
                            account.Code = treasury.AccCode;
                        }
                        else
                        {
                            account.Name = Common.GetAccountName(Conversions.ToInteger(User.TreasuryAcc));
                            account.Code = User.TreasuryAcc;
                        }

                        if (decimal.Compare(totalCashInDrawer, 0m) > 0)
                        {
                            account.Debt = 0.0;
                            account.Credit = Convert.ToDouble(Math.Abs(totalCashInDrawer));
                        }
                        else if (decimal.Compare(totalCashInDrawer, 0m) < 0)
                        {
                            account.Debt = Convert.ToDouble(Math.Abs(totalCashInDrawer));
                            account.Credit = 0.0;
                        }

                        account.Note = " نقدي في الصندوق " + employeeName + " : خاصة الموظف: رقم" +
                            Conversions.ToString(inv.ClosedId);
                        account.CCcode = Conversions.ToString(-1);
                        accountsList.Add(account);
                    }

                    #endregion

                    #region Create Account Entries - Cashier Balance

                    if (decimal.Compare(inv.CashierBalance, 0m) != 0)
                    {
                        Account account = new Account();
                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;
                        account.Name = Common.GetAccountName(1211002);
                        account.Code = Conversions.ToString(1211002);

                        if (decimal.Compare(inv.CashierBalance, 0m) > 0)
                        {
                            account.Debt = Convert.ToDouble(inv.CashierBalance);
                            account.Credit = 0.0;
                        }
                        else
                        {
                            account.Debt = 0.0;
                            account.Credit = Conversions.ToDouble($"{decimal.Multiply(-1m, inv.CashierBalance):0.#,##.##}");
                        }

                        account.Note = " عهدة الإغلاق " + employeeName + " : خاصة الموظف: رقم" +
                            Conversions.ToString(inv.ClosedId);
                        account.CCcode = Conversions.ToString(-1);
                        accountsList.Add(account);
                    }

                    #endregion

                    #region Create Account Entries - Cash Difference

                    if (decimal.Compare(totalCashInDrawer, inv.CashierBalance) > 0)
                    {
                        Account account = new Account();
                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;
                        account.Name = Common.GetAccountName(3110004);
                        account.Code = Conversions.ToString(3110004);
                        account.Debt = Conversions.ToDouble($"{decimal.Subtract(totalCashInDrawer, inv.CashierBalance):0.#,##.##}");
                        account.Credit = 0.0;
                        account.Note = " فرق بالصندوق " + employeeName + " : خاصة الموظف: رقم" +
                            Conversions.ToString(inv.ClosedId);
                        account.CCcode = Conversions.ToString(-1);
                        accountsList.Add(account);
                    }
                    else if (decimal.Compare(totalCashInDrawer, inv.CashierBalance) < 0)
                    {
                        Account account = new Account();
                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;
                        account.Name = Common.GetAccountName(3110004);
                        account.Code = Conversions.ToString(3110004);
                        account.Debt = 0.0;
                        account.Credit = Conversions.ToDouble($"{decimal.Subtract(inv.CashierBalance, totalCashInDrawer):0.#,##.##}");
                        account.Note = " فرق بالصندوق " + employeeName + " : خاصة الموظف: رقم" +
                            Conversions.ToString(inv.ClosedId);
                        account.CCcode = Conversions.ToString(-1);
                        accountsList.Add(account);
                    }

                    #endregion

                    entry.Accounts = accountsList;
                    result = entry;
                }
                catch (Exception ex)
                {
                    string errorText = "error in Binding data";
                    if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
                    {
                        errorText = "error in Binding data";
                    }
                    MessageBox.Show((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) != 0) ?
                        (errorText + Environment.NewLine + "Error details: " + ex.Message) :
                        (errorText + Environment.NewLine + "تفاصيل الخطأ: " + ex.Message), "",
                        MessageBoxButton.OK, MessageBoxImage.Hand);
                    result = null;
                }

                return result;
            }
        }

        #endregion

        #region Print Methods

        private void PrintDevexpress(int Type, CashierCloseData obj, List<CloseShiftCustomer> closeCustomersList)
        {
            try
            {
                this.PrintSetting(6);
                if (Operators.CompareString(this.RptUrl, "", TextCompare: false) == 0)
                {
                    this.RptUrl = MainClass.ReportsPath;
                    this.RptName = "rptCloseday.repx";
                }
                this.defPrinter = MainClass.ReportsPrinter;
                string path = this.RptUrl + "/" + this.RptName;

                if (!Directory.Exists(this.RptUrl) | !File.Exists(path))
                {
                    MessageBox.Show("المسار الحالي للتقارير  غير موجود او تم تعديله", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return;
                }

                if (Operators.CompareString(this.RptName, "", TextCompare: false) == 0)
                {
                    MessageBox.Show("يجب  إدخال اسم التقرير من الإعدادات", "",
                        MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return;
                }

                XtraReport xtraReport = XtraReport.FromFile(path);
                List<CashierCloseData> list = new List<CashierCloseData>();
                list.Add(obj);
                DataSet dataSet = new DataSet("Name");
                DataTable table = global::UtilitiesProj.Common.ToDataTable(list);
                dataSet.Tables.Add(table);
                xtraReport.DataSource = dataSet;

                XtraReport xtraReport2 = XtraReport.FromFile(this.RptUrl + "/rptClosedayCust.repx");
                xtraReport2.DataSource = closeCustomersList;
                XRSubreport xRSubreport = (XRSubreport)xtraReport.FindControl("subreportCust", ignoreCase: true);
                if (xRSubreport != null)
                {
                    xRSubreport.ReportSource = xtraReport2;
                }

                XtraReport xtraReport3 = XtraReport.FromFile(this.RptUrl + "/header.repx");
                xtraReport3.DataSource = Common.FoundationInfoDT;
                XRSubreport xRSubreport2 = (XRSubreport)xtraReport.FindControl("headerRpt", ignoreCase: true);
                if (xRSubreport2 != null)
                {
                    xRSubreport2.ReportSource = xtraReport3;
                }

                XtraReport xtraReport4 = XtraReport.FromFile(this.RptUrl + "/footer.repx");
                xtraReport4.DataSource = Common.FoundationInfoDT;
                XRSubreport xRSubreport3 = (XRSubreport)xtraReport.FindControl("footerRpt", ignoreCase: true);
                if (xRSubreport3 != null)
                {
                    xRSubreport3.ReportSource = xtraReport4;
                }

                if (Operators.CompareString(this.defPrinter, "", TextCompare: false) != 0)
                {
                    xtraReport.PrinterName = this.defPrinter;
                    switch (Type)
                    {
                        case 1:
                            {
                                int printNo = this.PrintNo;
                                for (int i = 1; i <= printNo; i = checked(i + 1))
                                {
                                    xtraReport.Print();
                                }
                                break;
                            }
                        case 2:
                            xtraReport.ShowPreviewDialog();
                            break;
                        case 3:
                            this.SendEmail(xtraReport);
                            break;
                    }
                    this.SendEmail(xtraReport);
                }
                else
                {
                    MessageBox.Show("يجب  تحديد الطابعة من الإعدادات", "",
                        MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }
        }

        private void PrintSetting(int type)
        {
            SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                "select * from SettingPrint where Inv_Id=" + Conversions.ToString(type),
                this.conn1);
            DataTable dataTable = new DataTable();
            sqlDataAdapter.Fill(dataTable);

            if (dataTable.Rows.Count == 1)
            {
                try
                {
                    this.IsGroupPrinted = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["PrintTotGroup"]));
                    this.IsItemsPrinted = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["PrintTotItem"]));
                    this.PrintFooter = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["PrintFooter"]));
                    this.PrintHeader = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["PrintHeader"]));
                    this.defPrinter = Conversions.ToString(dataTable.Rows[0]["CasherPrinter"]);

                    if (Operators.CompareString(this.defPrinter, "", TextCompare: false) == 0)
                    {
                        this.defPrinter = MainClass.ReportsPrinter;
                    }

                    this.PrintNo = Conversions.ToInteger(dataTable.Rows[0]["printNo"]);
                    this.RptName = Conversions.ToString(dataTable.Rows[0]["RptName"]);

                    try
                    {
                        this.RptUrl = Path.GetDirectoryName(Conversions.ToString(dataTable.Rows[0]["RptUrl"]));
                    }
                    catch
                    {
                    }

                    if ((Operators.CompareString(this.RptUrl, "", TextCompare: false) == 0) | !Directory.Exists(this.RptUrl))
                    {
                        this.RptUrl = MainClass.ReportsPath;
                    }

                    this.PrintNote = Conversions.ToString(dataTable.Rows[0]["note"]);
                    return;
                }
                catch
                {
                    return;
                }
            }

            this.RptUrl = MainClass.ReportsPath;
            this.RptName = "rptCloseday.repx";
            this.defPrinter = MainClass.ReportsPrinter;
        }

        private void SendEmail(XtraReport report)
        {
            SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                "Select  * from  SettingEmail where Branch_Id=" + Conversions.ToString(MainClass.BranchNo),
                this.conn);
            DataTable dataTable = new DataTable();
            sqlDataAdapter.Fill(dataTable);

            if (dataTable.Rows.Count <= 0 || !Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["ActiveAuto"])))
            {
                return;
            }

            if (File.Exists(System.Windows.Forms.Application.StartupPath + "\\\\إغلاق_الوردية.pdf"))
            {
                File.Delete(System.Windows.Forms.Application.StartupPath + "\\\\إغلاق_الوردية.pdf");
            }

            report.ExportToPdf(System.Windows.Forms.Application.StartupPath + "\\\\إغلاق_الوردية.pdf");
            string pdfPath = System.Windows.Forms.Application.StartupPath + "\\\\إغلاق_الوردية.pdf";
            string sendEmail = Conversions.ToString(dataTable.Rows[0]["SendEmail"]);
            string recEmail = Conversions.ToString(dataTable.Rows[0]["RecEmail"]);
            string serverName = Conversions.ToString(dataTable.Rows[0]["ServerName"]);
            string sendPwd = Conversions.ToString(dataTable.Rows[0]["SendPWd"]);
            int port = checked((int)Math.Round(Conversion.Val(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["port"]))));

            string body = string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(
                "" + "السلام عليكم و رحمة الله و بركاته:", Environment.NewLine,
                "إغلاق الوردية للموظف ", this.Username), Environment.NewLine,
                Home.lblBranch1.Text), Environment.NewLine, "التاريخ:",
                Conversions.ToString(this.StartDate.SelectedDate.Value)), Environment.NewLine,
                "المرسل:", this.Username), Environment.NewLine, "مع خالص التحايا …");

            using MailMessage mailMessage = new MailMessage(sendEmail.Trim(), recEmail.Trim());
            mailMessage.Subject = "  إغلاق الوردية للموظف " + this.Username + " - " + Home.lblBranch1.Text;
            mailMessage.Body = body;

            if (File.Exists(pdfPath))
            {
                Path.GetFileName(pdfPath);
                mailMessage.Attachments.Add(new Attachment(pdfPath));
            }

            mailMessage.IsBodyHtml = false;
            SmtpClient smtpClient = new SmtpClient();
            smtpClient.Host = serverName.Trim();
            smtpClient.EnableSsl = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["SSL"]));
            NetworkCredential credentials = new NetworkCredential(sendEmail.Trim(), sendPwd.Trim());
            smtpClient.UseDefaultCredentials = true;
            smtpClient.Credentials = credentials;
            smtpClient.Port = port;

            if (MainClass.CheckForInternetConnection())
            {
                smtpClient.Send(mailMessage);
                if (Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0)
                {
                    MessageBox.Show("تم ارسال البريد", "بريد", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Email sent.", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show(" لم يتم إرسال البريد", "بريد", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void PrintDevexpresDetails(object type)
        {
            try
            {
                this.RptUrl = MainClass.ReportsPath;
                this.defPrinter = MainClass.ReportsPrinter;
                this.RptName = "rptSalesByGroups.repx";

                if (this.dtGroup.Rows.Count == 0)
                {
                }

                if (Operators.CompareString(this.RptUrl, "", TextCompare: false) == 0)
                {
                    MessageBox.Show("يجب  تحديد مسار التقرير ", "",
                        MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return;
                }

                string path = this.RptUrl + "/" + this.RptName;
                if (!Directory.Exists(Path.GetDirectoryName(this.RptUrl)) | !File.Exists(path))
                {
                    MessageBox.Show("المسار الحالي للتقارير  غير موجود او تم تعديله", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return;
                }

                if (Operators.CompareString(this.RptName, "", TextCompare: false) == 0)
                {
                    MessageBox.Show("يجب  إدخال اسم التقرير من الإعدادات", "",
                        MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return;
                }

                XtraReport xtraReport = XtraReport.FromFile(path);
                XtraReport xtraReport2 = XtraReport.FromFile(this.RptUrl + "/ItemDetail.repx");
                xtraReport.DataSource = this.dtGroup;
                xtraReport2.DataSource = this.dt2;

                XRSubreport xRSubreport = (XRSubreport)xtraReport.FindControl("subreport", ignoreCase: true);
                if (xRSubreport != null)
                {
                    xRSubreport.ReportSource = xtraReport2;
                }

                XtraReport xtraReport3 = XtraReport.FromFile(this.RptUrl + "/header.repx");
                xtraReport3.DataSource = Common.FoundationInfoDT;
                XRSubreport xRSubreport2 = (XRSubreport)xtraReport.FindControl("headerRpt", ignoreCase: true);
                if (xRSubreport2 != null)
                {
                    xRSubreport2.ReportSource = xtraReport3;
                }

                if (this.CloseShiftSetting.PrintGroups)
                {
                    xtraReport.FindControl("SubBandGrp", ignoreCase: true).Visible = true;
                }
                else
                {
                    xtraReport.FindControl("SubBandGrp", ignoreCase: true).Visible = false;
                }

                if (this.CloseShiftSetting.QuantPrint)
                {
                    xtraReport.FindControl("SubBandItems", ignoreCase: true).Visible = true;
                }
                else
                {
                    xtraReport.FindControl("SubBandItems", ignoreCase: true).Visible = false;
                }

                if (Operators.CompareString(this.defPrinter, "", TextCompare: false) != 0)
                {
                    xtraReport.PrinterName = this.defPrinter;
                    if (Operators.ConditionalCompareObjectEqual(type, 1, TextCompare: false))
                    {
                        int printNo = this.PrintNo;
                        for (int i = 1; i <= printNo; i = checked(i + 1))
                        {
                            xtraReport.Print();
                        }
                    }
                    else
                    {
                        xtraReport.ShowPreviewDialog();
                    }
                    xtraReport.Dispose();
                }
                else
                {
                    MessageBox.Show("يجب  تحديد الطابعة من الإعدادات", "",
                        MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
            }
            catch
            {
            }
        }

        #endregion

        #region Sales Report Methods

        private void SalesReport(int type, CashierCloseData obj)
        {
            checked
            {
                try
                {
                    this.TotSale = 0.0;
                    this.TotQun = 0.0;
                    string branchFilter = "";

                    if (MainClass.BranchNo != -1)
                    {
                        branchFilter = "inv.branch=" + Conversions.ToString(MainClass.BranchNo) +
                            " and inv.sales_emp=" + this.Username + " and ";
                    }

                    DataTable tempDataTable = new DataTable();
                    tempDataTable.Columns.Add("currency");
                    tempDataTable.Columns.Add("sum");
                    tempDataTable.Columns.Add("type");
                    tempDataTable.Columns.Add("TotPrice");

                    if (type != 1)
                    {
                        return;
                    }

                    // معالجة المبيعات
                    SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                        "select ItemId,sum(val) as Val ,sum(val*exchange_price) as TotPrice from inv,inv_sub,Items where " +
                        branchFilter + " inv.date>=@date1 and inv.date<=@date2 and  inv_sub.ItemId=Items.id and inv.inv_type=20 and inv.proc_type=1  and inv.InvGlobalID=inv_sub.InvGlobalID  and inv.IS_Deleted=0 group by ItemId order by max(exchange_price) desc",
                        this.conn);
                    sqlDataAdapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = this.datefrom;
                    sqlDataAdapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = this.dateTo;
                    DataTable salesDataTable = new DataTable();
                    sqlDataAdapter.Fill(salesDataTable);

                    if (salesDataTable.Rows.Count > 0)
                    {
                        int salesCount = salesDataTable.Rows.Count - 1;
                        for (int i = 0; i <= salesCount; i++)
                        {
                            tempDataTable.Rows.Add(salesDataTable.Rows[i][0],
                                Conversion.Val(Operators.ConcatenateObject("", salesDataTable.Rows[i][1])),
                                1, salesDataTable.Rows[i]["TotPrice"]);
                        }
                    }

                    // معالجة المرتجعات
                    sqlDataAdapter = new SqlDataAdapter(
                        "select ItemId,sum(val) as Val,sum(val*exchange_price) as TotPrice from inv,inv_sub,Items where " +
                        branchFilter + " inv.date>=@date1 and inv.date<=@date2 and  inv_sub.ItemId=Items.id and inv.inv_type=20 and inv.proc_type=2 and  inv.InvGlobalID=inv_sub.InvGlobalID  and inv.IS_Deleted=0 group by ItemId order by max(exchange_price) desc",
                        this.conn);
                    sqlDataAdapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = this.datefrom;
                    sqlDataAdapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = this.dateTo;
                    salesDataTable = new DataTable();
                    sqlDataAdapter.Fill(salesDataTable);

                    if (salesDataTable.Rows.Count > 0)
                    {
                        int returnsCount = salesDataTable.Rows.Count - 1;
                        for (int j = 0; j <= returnsCount; j++)
                        {
                            tempDataTable.Rows.Add(salesDataTable.Rows[j][0],
                                Conversion.Val(Operators.ConcatenateObject("", salesDataTable.Rows[j][1])),
                                2, salesDataTable.Rows[j]["TotPrice"]);
                        }
                    }

                    int totalRows = tempDataTable.Rows.Count - 1;
                    int maxRows = totalRows;
                    for (int k = 0; k <= maxRows; k++)
                    {
                        if (k <= totalRows && Operators.ConditionalCompareObjectEqual(tempDataTable.Rows[k][2], 2, TextCompare: false))
                        {
                            tempDataTable.Rows[k][1] = 0.0 - Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[k][1]));
                            tempDataTable.Rows[k]["TotPrice"] = 0.0 - Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[k]["TotPrice"]));
                        }

                        int nextRow = k + 1;
                        int lastRow = totalRows;
                        for (int l = nextRow; l <= lastRow; l++)
                        {
                            if (l <= totalRows && Operators.ConditionalCompareObjectEqual(tempDataTable.Rows[k][0], tempDataTable.Rows[l][0], TextCompare: false))
                            {
                                if (Operators.ConditionalCompareObjectEqual(tempDataTable.Rows[l][2], 2, TextCompare: false))
                                {
                                    tempDataTable.Rows[l][1] = 0.0 - Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[l][1]));
                                    tempDataTable.Rows[l]["TotPrice"] = 0.0 - Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[l]["TotPrice"]));
                                }

                                tempDataTable.Rows[k][1] = Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[k][1])) +
                                    Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[l][1]));
                                tempDataTable.Rows[k]["TotPrice"] = Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[k]["TotPrice"])) +
                                    Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[l]["TotPrice"]));
                                tempDataTable.Rows.RemoveAt(l);
                                l--;
                                totalRows--;
                            }
                        }
                    }

                    if (this.dt2.Columns.Count > 0)
                    {
                        this.dt2.Columns.Clear();
                    }

                    this.dt2.Columns.Add("Items");
                    this.dt2.Columns.Add("Value1");
                    this.dt2.Columns.Add("Process");
                    this.dt2.Columns.Add("Price");
                    this.dt2.Columns.Add("Value2");

                    int finalRowsCount = tempDataTable.Rows.Count - 1;
                    for (int m = 0; m <= finalRowsCount; m++)
                    {
                        SqlDataAdapter itemAdapter = new SqlDataAdapter(
                            Conversions.ToString(Operators.AddObject("select is_deleted,sale_price from Items where id=",
                            tempDataTable.Rows[m][0])), this.conn);
                        DataTable itemDataTable = new DataTable();
                        itemAdapter.Fill(itemDataTable);

                        if (!Convert.ToBoolean(RuntimeHelpers.GetObjectValue(itemDataTable.Rows[0][0])))
                        {
                            try
                            {
                                double salePrice = Conversions.ToDouble(string.Format("{0:0.00}",
                                    RuntimeHelpers.GetObjectValue(itemDataTable.Rows[0]["sale_price"])));
                                double totalPrice = Conversions.ToDouble(string.Format("{0:0.00}",
                                    Math.Round(Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[m]["TotPrice"])), 2)));
                                this.dt2.Rows.Add(this.GetCurrencyName(Conversions.ToInteger(tempDataTable.Rows[m][0])),
                                    tempDataTable.Rows[m][1], "", salePrice, $"{totalPrice:0.#,##.##}");
                                this.TotSale += totalPrice;
                                ref double totQun = ref this.TotQun;
                                totQun = Conversions.ToDouble(Operators.AddObject(totQun, tempDataTable.Rows[m][1]));
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خطأ اثناء حساب المبيعات" + Environment.NewLine +
                        "تفاصيل الخطأ: " + ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Hand);
                }
                finally
                {
                }
            }
        }

        private void SalesByGroups(int type, CashierCloseData obj)
        {
            checked
            {
                try
                {
                    this.TotSale = 0.0;
                    this.TotQun = 0.0;
                    string branchFilter = "";

                    if (MainClass.BranchNo != -1)
                    {
                        branchFilter = "inv.branch=" + Conversions.ToString(MainClass.BranchNo) +
                            " and inv.sales_emp=" + Conversions.ToString(MainClass.EmpNo) + " and ";
                    }

                    DataTable tempDataTable = new DataTable();
                    tempDataTable.Columns.Add("currency");
                    tempDataTable.Columns.Add("Val");
                    tempDataTable.Columns.Add("TotPrice");
                    tempDataTable.Columns.Add("type");
                    tempDataTable.Columns.Add("GrpID");

                    if (this.dtGroup.Columns.Count > 0)
                    {
                        this.dtGroup.Columns.Clear();
                    }

                    this.dtGroup.Columns.Add("GroupName");
                    this.dtGroup.Columns.Add("Quatity");
                    this.dtGroup.Columns.Add("TotalSales");
                    this.dtGroup.Columns.Add("CloseNo");
                    this.dtGroup.Columns.Add("datefrom");
                    this.dtGroup.Columns.Add("dateTo");
                    this.dtGroup.Columns.Add("shiftNo");
                    this.dtGroup.Columns.Add("Username");

                    if (type != 1)
                    {
                        return;
                    }

                    SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                        "select ItemId,sum(val) as Val,sum(val*exchange_price) as TotPrice from inv,inv_sub,Items where " +
                        branchFilter + " inv.date>=@date1 and inv.date<=@date2 and  inv_sub.ItemId=Items.id and inv.inv_type=20 and inv.proc_type=1  and inv.InvGlobalID=inv_sub.InvGlobalID  and inv.IS_Deleted=0 group by ItemId order by max(exchange_price) desc",
                        this.conn);
                    sqlDataAdapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = this.datefrom;
                    sqlDataAdapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = this.dateTo;
                    DataTable salesDataTable = new DataTable();
                    sqlDataAdapter.Fill(salesDataTable);

                    int salesCount = salesDataTable.Rows.Count - 1;
                    for (int i = 0; i <= salesCount; i++)
                    {
                        tempDataTable.Rows.Add(salesDataTable.Rows[i][0],
                            Conversion.Val(Operators.ConcatenateObject("", salesDataTable.Rows[i]["Val"])),
                            Conversion.Val(Operators.ConcatenateObject("", salesDataTable.Rows[i]["TotPrice"])),
                            1, this.GetGroupID(Conversions.ToInteger(salesDataTable.Rows[i][0])));
                    }

                    sqlDataAdapter = new SqlDataAdapter(
                        "select ItemId,sum(val) as Val, sum(val*exchange_price) as TotPrice  from inv,inv_sub,Items where " +
                        branchFilter + " inv.date>=@date1 and inv.date<=@date2 and  inv_sub.ItemId=Items.id and inv.inv_type=20 and inv.proc_type=2  and inv.InvGlobalID=inv_sub.InvGlobalID  and inv.IS_Deleted=0 group by ItemId order by max(exchange_price) desc",
                        this.conn);
                    sqlDataAdapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = this.datefrom;
                    sqlDataAdapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = this.dateTo;
                    salesDataTable = new DataTable();
                    sqlDataAdapter.Fill(salesDataTable);

                    int returnsCount = salesDataTable.Rows.Count - 1;
                    for (int j = 0; j <= returnsCount; j++)
                    {
                        tempDataTable.Rows.Add(salesDataTable.Rows[j][0],
                            Conversion.Val(Operators.ConcatenateObject("", salesDataTable.Rows[j]["Val"])),
                            Conversion.Val(Operators.ConcatenateObject("", salesDataTable.Rows[j]["TotPrice"])),
                            2, this.GetGroupID(Conversions.ToInteger(salesDataTable.Rows[j][0])));
                    }

                    int totalRows = tempDataTable.Rows.Count - 1;
                    int maxRows = totalRows;
                    for (int k = 0; k <= maxRows; k++)
                    {
                        if (k <= totalRows && Operators.ConditionalCompareObjectEqual(tempDataTable.Rows[k][3], 2, TextCompare: false))
                        {
                            tempDataTable.Rows[k]["Val"] = 0.0 - Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[k]["Val"]));
                            tempDataTable.Rows[k]["TotPrice"] = 0.0 - Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[k]["TotPrice"]));
                        }

                        int nextRow = k + 1;
                        int lastRow = totalRows;
                        for (int l = nextRow; l <= lastRow; l++)
                        {
                            if (l <= totalRows && Operators.ConditionalCompareObjectEqual(tempDataTable.Rows[k][0], tempDataTable.Rows[l][0], TextCompare: false))
                            {
                                if (Operators.ConditionalCompareObjectEqual(tempDataTable.Rows[l][3], 2, TextCompare: false))
                                {
                                    tempDataTable.Rows[l]["Val"] = 0.0 - Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[l]["Val"]));
                                    tempDataTable.Rows[l]["TotPrice"] = 0.0 - Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[l]["TotPrice"]));
                                }

                                tempDataTable.Rows[k]["Val"] = Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[k]["Val"])) +
                                    Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[l]["Val"]));
                                tempDataTable.Rows[k]["TotPrice"] = Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[k]["TotPrice"])) +
                                    Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[l]["TotPrice"]));
                                tempDataTable.Rows.RemoveAt(l);
                                l--;
                                totalRows--;
                            }
                        }
                    }

                    totalRows = tempDataTable.Rows.Count - 1;
                    int groupMaxRows = totalRows;
                    for (int m = 0; m <= groupMaxRows; m++)
                    {
                        int groupNextRow = m + 1;
                        int groupLastRow = totalRows;
                        for (int n = groupNextRow; n <= groupLastRow; n++)
                        {
                            if (n <= totalRows && Operators.ConditionalCompareObjectEqual(tempDataTable.Rows[m]["GrpID"], tempDataTable.Rows[n]["GrpID"], TextCompare: false))
                            {
                                tempDataTable.Rows[m]["Val"] = Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[m]["Val"])) +
                                    Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[n]["Val"]));
                                tempDataTable.Rows[m]["TotPrice"] = Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[m]["TotPrice"])) +
                                    Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[n]["TotPrice"]));
                                tempDataTable.Rows.RemoveAt(n);
                                n--;
                                totalRows--;
                            }
                        }
                    }

                    int finalRowsCount = tempDataTable.Rows.Count - 1;
                    for (int num11 = 0; num11 <= finalRowsCount; num11++)
                    {
                        double totalPrice = Conversions.ToDouble(string.Format("{0:0.00}",
                            Math.Round(Conversion.Val(RuntimeHelpers.GetObjectValue(tempDataTable.Rows[num11]["TotPrice"]))), 2));
                        this.dtGroup.Rows.Add(this.GetGroupName(Conversions.ToInteger(tempDataTable.Rows[num11]["GrpID"])),
                            tempDataTable.Rows[num11]["Val"].ToString(), $"{totalPrice:0.#,##.##}",
                            this.CloseNo, this.datefrom, this.dateTo, this.shiftNo, this.Username);
                        ref double totSale = ref this.TotSale;
                        totSale = Conversions.ToDouble(Operators.AddObject(totSale, tempDataTable.Rows[num11]["TotPrice"]));
                        ref double totQun = ref this.TotQun;
                        totQun = Conversions.ToDouble(Operators.AddObject(totQun, tempDataTable.Rows[num11]["Val"]));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خطأ اثناء حساب المبيعات" + Environment.NewLine +
                        "تفاصيل الخطأ: " + ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Hand);
                }
                finally
                {
                }
            }
        }

        #endregion
    }
}