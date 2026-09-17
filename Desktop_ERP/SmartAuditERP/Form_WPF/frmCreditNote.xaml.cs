using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using log4net;
using UtilitiesProj;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using MessageBoxOptions = System.Windows.MessageBoxOptions;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmCreditNote : ThemedWindow
    {
        #region ── Private Fields ──

        private SqlConnection conn;
        private SqlConnection conn1;
        private int Code = -1;
        private double InvDiscount = 0.0;
        private int Bond_id;
        private string EntryGlobalId;
        private double defVAT = 0.0;
        private string SrchName = "";
        private InvoiceObj InvObj;
        private bool PrintHeader = true;
        private bool PrintFooter = true;
        private bool PrintStamp = true;
        private int PrintType;
        private int PrintNo = 1;
        private string defPrinter = "";
        private string RptName = "";
        private string RptUrl = "";
        private string InvCombinedId = "";

        private static readonly ILog Logger =
            LogManager.GetLogger(Environment.MachineName);

        #endregion

        #region ── Constructor ──

        public frmCreditNote()
        {
            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            Code = -1;
            InvDiscount = 0.0;
            EntryGlobalId = (-1).ToString();
            defVAT = 0.0;
            SrchName = "";
            InvObj = new InvoiceObj(1, 1);
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp = true;
            PrintNo = 1;
            RptName = "";
            RptUrl = "";
            InvCombinedId = "";

            InitializeComponent();
        }

        #endregion

        #region ── Window Events ──

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDate.SelectedDate = DateTime.Now.Date;
            txtDate.DisplayDateEnd = DateTime.Now.Date;
            LoadClients();
            LoadNxtNo();
            LoadMainSettings();
        }

        #endregion

        #region ── Data Loading ──

        public void LoadClients()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, name FROM Customers " +
                    "WHERE (type=2 OR type=3) " +
                    "AND IS_Deleted=0 ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbSupplies.ItemsSource = dt.DefaultView;
                cmbSupplies.DisplayMemberPath = "name";
                cmbSupplies.SelectedValuePath = "id";
                cmbSupplies.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCreditNote {ex.Message} {MainClass.UserName}");
            }
        }

        private void LoadNxtNo()
        {
            try
            {
                int nextNo = 1;
                var adapter = new SqlDataAdapter(
                    "SELECT MAX(Doc_No) FROM CreditDeptNotes " +
                    "WHERE Doc_Type=1", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0 &&
                    !string.IsNullOrEmpty(dt.Rows[0][0].ToString()))
                {
                    nextNo = Convert.ToInt32(dt.Rows[0][0]) + 1;
                }
                txtNo.Text = nextNo.ToString();
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCreditNote {ex.Message} {MainClass.UserName}");
            }
        }

        private void LoadMainSettings()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT MainVAT FROM SettingGeneral WHERE Inv_Id=1", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 1)
                    defVAT = Convert.ToDouble(dt.Rows[0]["MainVAT"]);
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCreditNote {ex.Message} {MainClass.UserName}");
            }
        }

        private void LoadPrintSettings()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=11", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count != 1) return;

                PrintType = Convert.ToInt32(dt.Rows[0]["printType"]);
                PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                PrintStamp = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                defPrinter = dt.Rows[0]["CasherPrinter"].ToString();

                if (string.IsNullOrEmpty(defPrinter))
                    defPrinter = Common.GetDefaultPrinter();

                PrintNo = Convert.ToInt32(dt.Rows[0]["printNo"]);
            }
            catch (Exception ex)
            {
                ShowMsg(ex.Message, "خطأ", MessageBoxImage.Error);
            }
        }

        private void LoadInvData(int invId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT tot_net FROM Inv " +
                    $"WHERE inv_type=1 AND IS_Deleted=0 " +
                    $"AND id={invId} AND proc_type=1", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                    txtInvVal.Text = Convert.ToDouble(
                        dt.Rows[0]["tot_net"]).ToString();
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCreditNote {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region ── CLR ──

        private void CLR()
        {
            txtInvNo.Text = "";
            txtInvVal.Text = "";
            txtDiscountVal.Text = "0";
            txtDiscountPerc.Text = "0";
            txtTax.Text = "0";
            txtNet.Text = "0";
            txtNotes.Text = "";
            txtPreviousDiscVal.Text = "0";
            cmbSupplies.SelectedIndex = -1;
            Code = -1;
            LoadNxtNo();
        }

        #endregion

        #region ── Calculations ──

        public void CalcDiscount()
        {
            try
            {
                double invVal = SafeParseDouble(txtInvVal.Text);
                double prevDisc = SafeParseDouble(txtPreviousDiscVal.Text);
                double discVal = SafeParseDouble(txtDiscountVal.Text);
                double base_ = invVal - prevDisc;

                if (base_ > 0)
                {
                    txtDiscountPerc.Text =
                        (discVal / base_ * 100.0).ToString(InvObj.DigitsNo);
                }

                CalcTot();
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCreditNote {ex.Message} {MainClass.UserName}");
            }
        }

        private void CalcTot()
        {
            try
            {
                double invVal = SafeParseDouble(txtInvVal.Text);
                double prevDisc = SafeParseDouble(txtPreviousDiscVal.Text);
                double discVal = SafeParseDouble(txtDiscountVal.Text);
                double available = invVal - prevDisc;

                if (string.IsNullOrWhiteSpace(txtDiscountVal.Text))
                {
                    txtDiscountPerc.Text = "0";
                    txtDiscountVal.Text = "0";
                    InvDiscount = 0.0;
                }

                if (discVal > 0)
                {
                    if (discVal > available)
                    {
                        ShowMsg("قيمة الخصم أكبر من قيمة الفاتورة",
                                "", MessageBoxImage.Warning);
                        return;
                    }

                    txtNet.Text = (available - discVal).ToString(InvObj.DigitsNo);
                    txtTax.Text = CalVAT(discVal).ToString(InvObj.DigitsNo);
                }
                else
                {
                    txtNet.Text = "0";
                    txtTax.Text = "0";
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCreditNote {ex.Message} {MainClass.UserName}");
            }
        }

        private double CalVAT(double val)
        {
            return val - Math.Round(val / (1.0 + defVAT / 100.0), 3);
        }

        #endregion

        #region ── Navigate ──

        public void Navigate(string sqlQuery)
        {
            try
            {
                if (conn.State != ConnectionState.Open) conn.Open();
                var cmd = new SqlCommand(sqlQuery, conn);
                using var reader = cmd.ExecuteReader();
                ReadData(reader);
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCreditNote {ex.Message} {MainClass.UserName}");
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        private void ReadData(SqlDataReader reader)
        {
            try
            {
                if (!reader.HasRows) return;
                reader.Read();

                CLR();

                Code = Convert.ToInt32(reader["Doc_No"]);
                txtDate.SelectedDate = Convert.ToDateTime(reader["Date"]);

                EntryGlobalId = EntryOper.ReadEntryGlobalID(
                    Code.ToString(), 16, MainClass.BranchNo);

                double invNo = SafeParseDouble(reader["Inv_No"].ToString());

                if (invNo != -1.0)
                {
                    rdMainNote.IsChecked = false;
                    rdInvNote.IsChecked = true;
                    txtInvNo.Text = reader["Inv_No"].ToString();
                    LoadInvData(Convert.ToInt32(invNo));

                    var adapter = new SqlDataAdapter(
                        $"SELECT SUM(Discount) AS sum " +
                        $"FROM CreditDeptNotes " +
                        $"WHERE Doc_Type=1 AND IS_Deleted=0 " +
                        $"AND Inv_No={invNo} AND Doc_No<{Code}", conn1);
                    var dt2 = new DataTable();
                    adapter.Fill(dt2);

                    txtPreviousDiscVal.Text =
                        (dt2.Rows.Count > 0 &&
                         dt2.Rows[0]["sum"] != DBNull.Value)
                        ? SafeParseDouble(dt2.Rows[0]["sum"].ToString()).ToString()
                        : "0";
                }
                else
                {
                    rdMainNote.IsChecked = true;
                    rdInvNote.IsChecked = false;
                }

                txtNo.Text = reader["Doc_No"].ToString();
                cmbSupplies.SelectedValue =
                    SafeParseDouble(reader["cust_id"].ToString());
                txtDiscountVal.Text = reader["Discount"].ToString();
                txtNotes.Text = reader["notes"].ToString();

                CalcTot();

                double invValD = SafeParseDouble(txtInvVal.Text);
                double prevDiscD = SafeParseDouble(txtPreviousDiscVal.Text);
                double discValD = SafeParseDouble(txtDiscountVal.Text);
                double baseD = invValD - prevDiscD;

                if (baseD > 0)
                    txtDiscountPerc.Text =
                        (discValD / baseD * 100.0).ToString(InvObj.DigitsNo);
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCreditNote {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region ── Invoice Search ──

        private void InvoiceSrch()
        {
            try
            {
                var form = new frmInvoiceSrch();
                form.cmbProcType.IsEnabled = false;
                MainClass.ApplyPermissionToForm(form);
                MainClass.DoApplyUserSett(form);
                form.ProcType = 1;
                form.InvType = 1;
                form.btnPostPone.Visibility = Visibility.Collapsed;
                form.btnPostPone.IsChecked = true;
                form.ShowDialog();

                if (form.ISDone && form.InvGlobalID != "-1")
                {
                    var adapter = new SqlDataAdapter(
                        $"SELECT cust_id, id, tot_net FROM Inv " +
                        $"WHERE inv_type=1 AND IS_Deleted=0 " +
                        $"AND InvGlobalID=N'{form.InvGlobalID}' " +
                        $"AND proc_type=1", conn);
                    var dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        cmbSupplies.SelectedValue =
                            SafeParseDouble(dt.Rows[0]["cust_id"].ToString());
                        txtInvNo.Text = dt.Rows[0]["id"].ToString();
                        txtInvVal.Text = dt.Rows[0]["tot_net"].ToString();
                        IsDiscounted(Convert.ToInt32(dt.Rows[0]["id"]));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCreditNote {ex.Message} {MainClass.UserName}");
            }
        }

        private void SearchByInvNo(int invNo)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT cust_id, tot_net FROM Inv " +
                    $"WHERE pay_type=-1 AND inv_type=1 " +
                    $"AND IS_Deleted=0 AND id={invNo} AND proc_type=1",
                    conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    cmbSupplies.SelectedValue =
                        SafeParseDouble(dt.Rows[0]["cust_id"].ToString());
                    txtInvVal.Text = dt.Rows[0]["tot_net"].ToString();
                    IsDiscounted(invNo);
                }
                else
                {
                    CLR();
                    InvoiceSrch();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCreditNote {ex.Message} {MainClass.UserName}");
            }
        }

        private void IsDiscounted(int invNo)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT Doc_No FROM CreditDeptNotes " +
                    $"WHERE Doc_Type=1 AND IS_Deleted=0 " +
                    $"AND Inv_No={invNo}", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    var result = MessageBox.Show(
                        "تم الخصم من هذه الفاتورة مسبقًا. هل تريد الخصم مرة أخرى؟",
                        "تنبيه",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question,
                        MessageBoxResult.No,
                        MessageBoxOptions.RightAlign |
                        MessageBoxOptions.RtlReading);

                    if (result == MessageBoxResult.Yes)
                    {
                        var adapter2 = new SqlDataAdapter(
                            $"SELECT SUM(Discount) AS sum " +
                            $"FROM CreditDeptNotes " +
                            $"WHERE Doc_Type=1 AND IS_Deleted=0 " +
                            $"AND Inv_No={invNo}", conn1);
                        var dt2 = new DataTable();
                        adapter2.Fill(dt2);

                        if (dt2.Rows.Count > 0)
                            txtPreviousDiscVal.Text =
                                SafeParseDouble(dt2.Rows[0]["sum"].ToString())
                                .ToString();
                    }
                    else
                    {
                        CLR();
                    }
                }
                else
                {
                    txtPreviousDiscVal.Text = "0";
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCreditNote {ex.Message} {MainClass.UserName}");
            }
        }

        #endregion

        #region ── Save ──

        private void Save()
        {
            SqlTransaction sqlTransaction = null;
            try
            {
                if (cmbSupplies.SelectedIndex == -1)
                {
                    ShowMsg("يجب اختيار المورد", "", MessageBoxImage.Warning);
                    return;
                }

                if (rdInvNote.IsChecked == true &&
                    string.IsNullOrWhiteSpace(txtInvNo.Text))
                {
                    ShowMsg("أدخل رقم الفاتورة", "", MessageBoxImage.Warning);
                    txtInvNo.Focus();
                    return;
                }

                var confirm = MessageBox.Show(
                    "هل أنت متأكد من حفظ السند؟",
                    "تأكيد",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No,
                    MessageBoxOptions.RightAlign |
                    MessageBoxOptions.RtlReading);

                if (confirm != MessageBoxResult.Yes) return;

                if (conn.State != ConnectionState.Open) conn.Open();
                sqlTransaction = conn.BeginTransaction();

                SqlCommand cmd;
                if (Code == -1)
                {
                    LoadNxtNo();
                    cmd = new SqlCommand(
                        "INSERT INTO CreditDeptNotes(" +
                        "Doc_No, Doc_Type, Inv_No, Discount, Net, Date, " +
                        "IS_Deleted, Edit_Date, emp_id, notes, cust_id, Inv_Val) " +
                        $"VALUES({txtNo.Text}, 1, @Inv_No, @Discount, @Net, " +
                        "@Date, 0, @Edit_Date, @emp_id, @notes, @cust_id, @Inv_Val)",
                        conn, sqlTransaction);
                }
                else
                {
                    cmd = new SqlCommand(
                        "UPDATE CreditDeptNotes SET " +
                        "Inv_No=@Inv_No, Discount=@Discount, Net=@Net, " +
                        "Edit_Date=@Edit_Date, emp_id=@emp_id, notes=@notes, " +
                        "cust_id=@cust_id, Inv_Val=@Inv_Val " +
                        $"WHERE Doc_Type=1 AND Doc_No={Code}",
                        conn, sqlTransaction);
                }

                cmd.Parameters.Add("@Inv_No", SqlDbType.Int).Value =
                    (rdMainNote.IsChecked == true)
                    ? -1
                    : (object)SafeParseDouble(txtInvNo.Text);

                cmd.Parameters.Add("@Discount", SqlDbType.Float).Value =
                    SafeParseDouble(txtDiscountVal.Text);
                cmd.Parameters.Add("@Net", SqlDbType.Float).Value =
                    SafeParseDouble(txtNet.Text);
                cmd.Parameters.Add("@Date", SqlDbType.DateTime).Value =
                    txtDate.SelectedDate ?? DateTime.Now;
                cmd.Parameters.Add("@Edit_Date", SqlDbType.DateTime).Value =
                    DateTime.Now.ToShortDateString();
                cmd.Parameters.Add("@emp_id", SqlDbType.Int).Value =
                    MainClass.UserID;
                cmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value =
                    txtNotes.Text;
                cmd.Parameters.Add("@cust_id", SqlDbType.Int).Value =
                    cmbSupplies.SelectedValue;
                cmd.Parameters.Add("@Inv_Val", SqlDbType.Float).Value =
                    SafeParseDouble(txtInvVal.Text);
                cmd.Parameters.Add("@BranchId", SqlDbType.NVarChar).Value =
                    MainClass.BranchNo;

                cmd.ExecuteNonQuery();

                if (Code == -1)
                    EntryOper.GetEntryGlobalID(ref EntryGlobalId, ref Bond_id);

                // تحضير القيد المحاسبي
                var entry = new Entry();
                var accounts = new List<Account>();

                entry.EntryGlobalID = EntryGlobalId;
                entry.ClientCode = Sync.ClientCode;
                entry.EntryNo = Bond_id;
                entry.EntryDate = txtDate.SelectedDate ?? DateTime.Now;
                entry.ReffNo = txtNo.Text;
                entry.RefDate = txtDate.SelectedDate ?? DateTime.Now;
                entry.Type = EntryType.CreditNote;
                entry.State = 1;
                entry.Note = $"اشعار دائن رقم:{txtNo.Text} خاصة مورد:{cmbSupplies.Text}";
                entry.Branch = MainClass.BranchNo;
                entry.EmpID = MainClass.EmpNo;
                entry.BranchType = Sync.BranchType;
                entry.DistBranch = Sync.DistBranch;

                double discVal = SafeParseDouble(txtDiscountVal.Text);

                if (discVal > 0.0)
                {
                    // حساب المورد
                    accounts.Add(new Account
                    {
                        EntryGlobalID = entry.EntryGlobalID,
                        EntryNo = entry.EntryNo,
                        Name = cmbSupplies.Text,
                        Code = GetCustAccCode(
                            Convert.ToInt32(cmbSupplies.SelectedValue))
                            .ToString(),
                        Debt = discVal,
                        Credit = 0.0,
                        Note = $"إشعار دائن رقم:{txtNo.Text} خاصة مورد:{cmbSupplies.Text}",
                        CCcode = (-1).ToString()
                    });

                    // خصم مكتسب
                    accounts.Add(new Account
                    {
                        EntryGlobalID = entry.EntryGlobalID,
                        EntryNo = entry.EntryNo,
                        Name = "خصم مكتسب",
                        Code = "3200003",
                        Debt = 0.0,
                        Credit = discVal - CalVAT(discVal),
                        Note = "خصومات مكتسبة",
                        CCcode = (-1).ToString()
                    });

                    // ضريبة
                    accounts.Add(new Account
                    {
                        EntryGlobalID = entry.EntryGlobalID,
                        EntryNo = entry.EntryNo,
                        Name = "الضريبة المضافة",
                        Code = "2222001",
                        Debt = 0.0,
                        Credit = CalVAT(discVal),
                        Note = $"ض.القيمة المضافة إشعار دائن رقم:{Code} خاصة مورد:{cmbSupplies.Text}",
                        CCcode = (-1).ToString()
                    });
                }

                entry.Accounts = accounts;

                if (!new EntryOper().SaveEnty(entry))
                {
                    sqlTransaction.Rollback();
                    ShowMsg("خطأ أثناء الحفظ", "خطأ", MessageBoxImage.Error);
                    return;
                }

                sqlTransaction.Commit();

                if (Code == -1)
                    Code = Convert.ToInt32(SafeParseDouble(txtNo.Text));

                var msgForm = new frmSavedMsg();
                msgForm.ShowDialog();

                switch (msgForm.Pressed)
                {
                    case 1: CLR(); break;
                    case 3: this.Close(); break;
                }
            }
            catch (Exception ex)
            {
                sqlTransaction?.Rollback();
                ShowMsg($"خطأ أثناء الحفظ\nتفاصيل الخطأ: {ex.Message}",
                        "خطأ", MessageBoxImage.Error);
                Logger.Error($"frmCreditNote {ex.Message} {MainClass.UserName}");
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region ── Delete ──

        private void Delete()
        {
            try
            {
                if (Code == -1)
                {
                    ShowMsg("اختر سنداً ليتم حذفه", "", MessageBoxImage.Warning);
                    return;
                }

                var confirm = MessageBox.Show(
                    "هل أنت متأكد من حذف السند؟",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No,
                    MessageBoxOptions.RightAlign |
                    MessageBoxOptions.RtlReading);

                if (confirm != MessageBoxResult.Yes) return;

                if (new ReceiptOper().DeleteCreditNote(Code, 1, EntryGlobalId))
                {
                    ShowMsg("تم الحذف");
                    CLR();
                }
                else
                {
                    ShowMsg("خطأ أثناء الحذف", "خطأ", MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                ShowMsg($"خطأ أثناء الحذف\nتفاصيل الخطأ: {ex.Message}",
                        "خطأ", MessageBoxImage.Error);
                Logger.Error($"frmCreditNote {ex.Message} {MainClass.UserName}");
            }
            finally
            {
                if (conn.State != ConnectionState.Closed) conn.Close();
            }
        }

        #endregion

        #region ── Print ──

        private void PrintDevexpress(int printType)
        {
            RptUrl = MainClass.ReportsPath;
            RptName = "DeptNote.repx";
            defPrinter = MainClass.ReportsPrinter;

            if (string.IsNullOrWhiteSpace(txtDiscountVal.Text))
            {
                ShowMsg("لا توجد قيمة بالسند", "", MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(RptUrl))
            {
                ShowMsg("يجب تحديد مسار التقرير", "", MessageBoxImage.Warning);
                return;
            }

            string fullPath = Path.Combine(RptUrl, RptName);
            if (!Directory.Exists(RptUrl) || !File.Exists(fullPath))
            {
                ShowMsg("المسار الحالي للتقارير غير موجود أو تم تعديله",
                        "خطأ", MessageBoxImage.Warning);
                return;
            }

            var report = XtraReport.FromFile(fullPath);
            report.DataSource = BindToData();

            string headerPath = Path.Combine(RptUrl, "header.repx");
            if (File.Exists(headerPath))
            {
                var headerReport = XtraReport.FromFile(headerPath);
                headerReport.DataSource = Common.FoundationInfoDT;
                var headerCtrl =
                    report.FindControl("headerRpt", true) as XRSubreport;
                if (headerCtrl != null) headerCtrl.ReportSource = headerReport;
            }

            string footerPath = Path.Combine(RptUrl, "footer.repx");
            if (File.Exists(footerPath))
            {
                var footerReport = XtraReport.FromFile(footerPath);
                footerReport.DataSource = Common.FoundationInfoDT;
                var footerCtrl =
                    report.FindControl("footerRpt", true) as XRSubreport;
                if (footerCtrl != null) footerCtrl.ReportSource = footerReport;
            }

            if (!string.IsNullOrEmpty(defPrinter))
            {
                report.PrinterName = defPrinter;
                if (printType == 1)
                    for (int i = 0; i < PrintNo; i++) report.Print();
                else
                    report.ShowPreviewDialog();

                report.Dispose();
            }
            else
            {
                ShowMsg("يجب تحديد الطابعة من الإعدادات",
                        "", MessageBoxImage.Warning);
            }
        }

        private DataSet BindToData()
        {
            string custVATno = "";
            string custAccCode = "";
            string custMobile = "";

            if (cmbSupplies.SelectedIndex > -1)
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT tax_no, name, AccountCode, mobile " +
                    $"FROM customers WHERE id={cmbSupplies.SelectedValue}",
                    conn1);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    custVATno = dt.Rows[0][0].ToString();
                    custAccCode = dt.Rows[0]["AccountCode"].ToString();
                    custMobile = dt.Rows[0]["mobile"].ToString();
                }
            }

            var list = new List<InvoiceData>
            {
                new InvoiceData
                {
                    InvoiceType  = this.Title,
                    InvoiceNo    = txtInvNo.Text,
                    Total        = txtInvVal.Text,
                    Discount     = txtDiscountVal.Text,
                    SubDiscount  = txtDiscountPerc.Text,
                    Net          = txtNet.Text,
                    InvDate      = txtDate.Text,
                    OrderNo      = txtNo.Text,
                    Customer     = cmbSupplies.Text,
                    InvNote      = txtNotes.Text,
                    CustVATno    = custVATno,
                    CustAccCode  = custAccCode,
                    CustMobile   = custMobile,
                    Tax          = txtTax.Text,
                    User         = Common.GetEmpName(MainClass.EmpNo)
                }
            };

            var dataSet = new DataSet("Name");
            dataSet.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            list.Clear();
            return dataSet;
        }

        #endregion

        #region ── Customer Balance ──

        private void GetCustBalance()
        {
            try
            {
                int accCode = -1;
                var adapter1 = new SqlDataAdapter(
                    $"SELECT Code FROM Accounts_Index " +
                    $"WHERE Type=2 AND AName=N'{cmbSupplies.Text}'", conn);
                var dt1 = new DataTable();
                adapter1.Fill(dt1);
                if (dt1.Rows.Count > 0)
                    accCode = Convert.ToInt32(dt1.Rows[0][0]);

                string accFilter = $" AND Entry_sub.acc_no={accCode}";
                string branchFilter = MainClass.BranchNo != -1
                    ? $"Entry.branch={MainClass.BranchNo} " +
                      $"AND Entry_sub.branch={MainClass.BranchNo} AND "
                    : "";

                var adapter2 = new SqlDataAdapter(
                    $"SELECT SUM(Entry_sub.dept) AS dept, " +
                    $"SUM(Entry_sub.credit) AS credit " +
                    $"FROM Entry, Entry_sub " +
                    $"WHERE {branchFilter} Entry.IS_Deleted=0 " +
                    $"AND Entry.state=1 " +
                    $"AND Entry.date<=@date2 " +
                    $"AND Entry.GlobalId=Entry_sub.EntryGlobalId {accFilter}",
                    conn);
                adapter2.SelectCommand.Parameters.Add(
                    "@date2", SqlDbType.DateTime).Value =
                    (txtDate.SelectedDate ?? DateTime.Now).AddHours(24);

                var dt2 = new DataTable();
                adapter2.Fill(dt2);

                double dept = 0.0;
                double credit = 0.0;

                if (dt2.Rows.Count > 0 &&
                    !string.IsNullOrEmpty(dt2.Rows[0][0].ToString()))
                {
                    dept = SafeParseDouble(dt2.Rows[0][0].ToString());
                    credit = SafeParseDouble(dt2.Rows[0][1].ToString());
                }

                txtInvVal.Text = dept >= credit
                    ? Math.Round(dept - credit, 3).ToString()
                    : Math.Round(credit - dept, 3).ToString();
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCreditNote {ex.Message} {MainClass.UserName}");
            }
        }

        private int GetCustAccCode(int customerId)
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT AccountCode FROM Customers " +
                    $"WHERE (type=2 OR type=3) " +
                    $"AND IS_Deleted=0 AND id={customerId}", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0
                    ? Convert.ToInt32(dt.Rows[0][0]) : 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCreditNote {ex.Message} {MainClass.UserName}");
                return 0;
            }
        }

        #endregion

        #region ── Search ──

        private void SearchByName()
        {
            try
            {
                SrchName = cmbSupplies.Text;
                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Customers " +
                    $"WHERE IS_Deleted=0 AND name=N'{SrchName}' " +
                    $"AND (type=2 OR type=3)", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                    cmbSupplies.SelectedValue = dt.Rows[0]["id"];
                else
                    AddNewItem();
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCreditNote {ex.Message} {MainClass.UserName}");
            }
        }

        private void AddNewItem()
        {
            var form = new frmAccountSrch
            {
                cond = " AND ParentCode=2211 "
            };
            SrchName = cmbSupplies.Text;
            form.txtSrchNm.Text = SrchName;
            form.ShowDialog();

            if (form.Code > -1)
            {
                var adapter = new SqlDataAdapter(
                    $"SELECT id, name FROM Customers " +
                    $"WHERE (type=2 OR type=3) " +
                    $"AND AccountCode={form.Code} " +
                    $"AND IS_Deleted=0 ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                    cmbSupplies.SelectedValue = dt.Rows[0]["id"];
            }
        }

        #endregion

        #region ── UI Events ──

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (rdMainNote.IsChecked == true)
                AddNewItem();
            else
                InvoiceSrch();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
            => Save();

        private void btnNew_Click(object sender, RoutedEventArgs e)
            => CLR();

        private void btnDelete_Click(object sender, RoutedEventArgs e)
            => Delete();

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => this.Close();

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => PrintDevexpress(1);

        private void btnView_Click(object sender, RoutedEventArgs e)
            => PrintDevexpress(2);

        private void btnFirst_Click(object sender, RoutedEventArgs e)
            => Navigate("SELECT TOP 1 * FROM CreditDeptNotes " +
                        "WHERE Doc_Type=1 AND IS_Deleted=0 " +
                        "ORDER BY Doc_No ASC");

        private void btnLast_Click(object sender, RoutedEventArgs e)
            => Navigate("SELECT TOP 1 * FROM CreditDeptNotes " +
                        "WHERE Doc_Type=1 AND IS_Deleted=0 " +
                        "ORDER BY Doc_No DESC");

        private void btnNext_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM CreditDeptNotes " +
                        $"WHERE Doc_Type=1 AND IS_Deleted=0 " +
                        $"AND Doc_No>{Code} ORDER BY id ASC");

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM CreditDeptNotes " +
                        $"WHERE Doc_Type=1 AND IS_Deleted=0 " +
                        $"AND Doc_No<{Code} ORDER BY id DESC");

        private void txtInvNo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                SearchByInvNo((int)SafeParseDouble(txtInvNo.Text));
        }

        private void txtDiscountVal_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return) CalcDiscount();
        }

        private void txtDiscountVal_Leave(object sender, RoutedEventArgs e)
        {
            try { CalcDiscount(); }
            catch (Exception ex)
            { Logger.Error($"frmCreditNote {ex.Message} {MainClass.UserName}"); }
        }

        private void txtDiscountPerc_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return) return;
            if (string.IsNullOrWhiteSpace(txtDiscountPerc.Text)) return;

            double invVal = SafeParseDouble(txtInvVal.Text);
            double prevDisc = SafeParseDouble(txtPreviousDiscVal.Text);
            double perc = SafeParseDouble(txtDiscountPerc.Text);
            double available = invVal - prevDisc;

            txtDiscountVal.Text = (perc / 100.0 * available).ToString();
            CalcTot();
        }

        private void txtDiscountPerc_Leave(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDiscountPerc.Text)) return;

            double invVal = SafeParseDouble(txtInvVal.Text);
            double prevDisc = SafeParseDouble(txtPreviousDiscVal.Text);
            double perc = SafeParseDouble(txtDiscountPerc.Text);
            double available = invVal - prevDisc;

            txtDiscountVal.Text =
                (perc / 100.0 * available).ToString(InvObj.DigitsNo);
            CalcTot();
        }

        private void cmbSupplies_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            try
            {
                if (rdMainNote.IsChecked == true)
                    GetCustBalance();
            }
            catch (Exception ex)
            {
                Logger.Error($"frmCreditNote {ex.Message} {MainClass.UserName}");
            }
        }

        private void cmbSupplies_KeyDown(object sender, KeyEventArgs e)
        {
            if (rdMainNote.IsChecked == true && e.Key == Key.Return)
                SearchByName();
        }

        private void rdMainNote_CheckedChanged(object sender,
            RoutedEventArgs e)
        {
            CLR();
            cmbSupplies.SelectedIndex = -1;
            txtInvVal.Text = "";
            txtTax.Text = "0";
            Code = -1;

            bool isMainNote = rdMainNote.IsChecked == true;
            pnlInvNo.Visibility = isMainNote
                ? Visibility.Collapsed : Visibility.Visible;

            bool isArabic = MainClass.Language == "ar";
            if (isMainNote)
            {
                lblInvVal.Text = isArabic ? "رصيد الحساب" : "Balance";
            }
            else
            {
                lblInvVal.Text = isArabic ? "قيمة الفاتورة" : "Invoice Value";
            }

            if (Code == -1) LoadNxtNo();
        }

        // ✅ منع إدخال غير الأرقام
        private void NumericOnly_PreviewTextInput(object sender,
            TextCompositionEventArgs e)
        {
            bool isDigitOrDot = char.IsDigit(e.Text, 0) || e.Text == ".";
            if (!isDigitOrDot)
            {
                e.Handled = true;
                ShowMsg("الحقل لا يقبل إلا الأرقام فقط");
            }
        }

        #endregion

        #region ── Helper Methods ──

        private static double SafeParseDouble(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0.0;
            return double.TryParse(value, out double result) ? result : 0.0;
        }

        private static void ShowMsg(
            string message,
            string title = "SmartAudit ERP",
            MessageBoxImage icon = MessageBoxImage.Information)
        {
            MessageBox.Show(message, title,
                MessageBoxButton.OK, icon,
                MessageBoxResult.OK,
                MessageBoxOptions.RightAlign |
                MessageBoxOptions.RtlReading);
        }

        #endregion
    }
}