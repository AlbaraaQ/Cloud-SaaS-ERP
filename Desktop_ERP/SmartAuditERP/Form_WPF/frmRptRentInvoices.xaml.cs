using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using UtilitiesProj;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRptRentInvoices : ThemedWindow
    {
        #region Fields

        private bool   PrintHeader;
        private bool   PrintFooter;
        private bool   PrintStamp;
        private int    PrintType;
        private int    PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;

        private int    SelectedId;
        public  string SrchName;
        private SqlConnection conn;

        public  int    selectedCli;
        public  int    StoreId;
        public  string sql;
        public  string Cond;
        public  int    ProcType;
        public  int    InvID;
        public  int    InvType;
        public  bool   ISDone;

        private double ReturnIncome;
        private double Income;
        private string LoadQry;

        // DataTable الكاملة قبل الفلتر
        private DataTable _dtable = new DataTable();

        // DataView للفلتر
        private DataView _dataView;

        private int ClickCount;
        private int RowIndex;

        #endregion

        #region Constructor

        public frmRptRentInvoices()
        {
            InitializeComponent();

            PrintHeader  = true;
            PrintFooter  = true;
            PrintStamp   = true;
            PrintNo      = 1;
            RptName      = "";
            RptUrl       = "";
            SelectedId   = -1;
            SrchName     = "";
            conn         = MainClass.ConnObj();
            StoreId      = 1;
            Cond         = "";
            ProcType     = 1;
            InvID        = -1;
            InvType      = 1;
            ISDone       = false;
            ReturnIncome = 0.0;
            Income       = 0.0;
            LoadQry      = "";
            ClickCount   = 0;
            RowIndex     = -1;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtToDate.DateTime   = DateTime.Now;
            txtFromDate.DateTime = DateTime.Now;

            LoadPrintSettings();
            LoadUsers();
            LoadGroupCode();
            LoadProcesses();
            cmbProcType.SelectedIndex = 1;

            LoadDgvItems();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            conn?.Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (RowIndex > -1 && dgvItems.Items.Count > RowIndex)
                {
                    try
                    {
                        if (dgvItems.Items[RowIndex] is DataRowView drv)
                        {
                            ISDone   = true;
                            InvID    = Convert.ToInt32(drv["id"]);
                            ProcType = Convert.ToInt32(drv["proc_type"]);
                            Close();
                        }
                    }
                    catch { }
                }
                else if (dgvItems.Items.Count > 0)
                {
                    dgvItems.SelectedIndex = 0;
                    dgvItems.ScrollIntoView(dgvItems.Items[0]);
                }
            }
        }

        #endregion

        #region Load Data

        public void LoadUsers()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT emp, username FROM Users " +
                    "WHERE IS_Deleted=0 AND id>0 ORDER BY emp", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                cmbUsers.DisplayMemberPath = "username";
                cmbUsers.SelectedValuePath = "emp";
                cmbUsers.ItemsSource       = dt.DefaultView;
                cmbUsers.SelectedIndex     = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المستخدمين:\n" + ex.Message);
            }
        }

        public void LoadDgvItems()
        {
            try
            {
                if (!string.IsNullOrEmpty(txtInvNo.Text))
                    Cond = "";

                LoadQry =
                    "SELECT RentInvoice.proc_id AS proc_id, " +
                    "RentInvoice.proc_type, " +
                    "RentInvoice.id AS id, " +
                    "RentInvoice.tot_net AS Net, " +
                    "RentInvoice.date AS date1, " +
                    "Marine.Name AS marine, " +
                    "Marine.Groupcode AS Groupcode, " +
                    "Customers.name AS cust, " +
                    "Users.username AS emp, " +
                    "Customers.mobile, " +
                    "Marine.IS_InPlan AS InPlan " +
                    "FROM RentInvoice, Customers, Users, Marine " +
                    "WHERE RentInvoice.cust_id=Customers.id " +
                    "  AND RentInvoice.sales_emp=Users.emp " +
                    "  AND RentInvoice.MarineId=Marine.id " +
                    $"  {Cond} " +
                    "  AND RentInvoice.IS_Deleted=0 " +
                    "ORDER BY RentInvoice.id DESC";

                var adapter = new SqlDataAdapter(LoadQry, conn);

                if (!string.IsNullOrEmpty(Cond))
                {
                    DateTime dateTo2 = txtToDate.DateTime.AddHours(24.0);
                    adapter.SelectCommand.Parameters.Add(
                        "@date1", SqlDbType.DateTime).Value
                        = txtFromDate.DateTime.ToShortDateString();
                    adapter.SelectCommand.Parameters.Add(
                        "@date2", SqlDbType.DateTime).Value = dateTo2;
                }

                _dtable = new DataTable();
                adapter.Fill(_dtable);

                _dataView            = new DataView(_dtable);
                dgvItems.ItemsSource = _dataView;

                CalcIncome();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل البيانات:\n" + ex.Message);
            }
        }

        private void LoadGroupCode()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT id, code FROM GroupMarine WHERE IsDeleted=0 ORDER BY id", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    cmbGroups.DisplayMemberPath = "code";
                    cmbGroups.SelectedValuePath = "id";
                    cmbGroups.ItemsSource       = dt.DefaultView;
                    cmbGroups.SelectedIndex     = -1;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المجموعات:\n" + ex.Message);
            }
        }

        public void LoadProcesses()
        {
            cmbProcType.Items.Clear();
            cmbProcType.Items.Add("الكل");
            cmbProcType.Items.Add("تأجير");
            cmbProcType.Items.Add("مرتجع");
            cmbProcType.Items.Add("معلق");
            cmbProcType.Items.Add("حجوزات");
        }

        #endregion

        #region CalcIncome - حساب المجاميع

        private void CalcIncome()
        {
            try
            {
                Income       = 0.0;
                ReturnIncome = 0.0;

                var source = dgvItems.ItemsSource as DataView;
                if (source == null) return;

                foreach (DataRowView drv in source)
                {
                    int    procType = Convert.ToInt32(drv["proc_type"]);
                    double netVal   = Convert.ToDouble(drv["Net"]);

                    if (procType == 2)
                        ReturnIncome += netVal;
                    else if (procType != 4)
                        Income       += netVal;
                }

                txtTotCredit.Text = Income.ToString("N2");
                txtTotDept.Text   = ReturnIncome.ToString("N2");
                txtNet.Text       = (Income - ReturnIncome).ToString("N2");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في حساب المجاميع:\n" + ex.Message);
            }
        }

        #endregion

        #region GenerateFilter - فلتر DataView

        private void GenerateFilter()
        {
            try
            {
                if (_dataView == null) return;

                string filter = "";

                // نوع العملية
                if (cmbProcType.SelectedIndex <= 0)
                    filter += " proc_type>0";
                else
                    filter += $" proc_type={cmbProcType.SelectedIndex}";

                // ضمن الخطة / خارجها
                if (rdAll.IsChecked == true)
                    filter += " AND (InPlan=1 OR InPlan=0)";
                else if (rdInPlan.IsChecked == true)
                    filter += " AND InPlan=1";
                else if (rdOutPlan.IsChecked == true)
                    filter += " AND InPlan=0";

                // المستخدم
                if (cmbUsers.SelectedIndex > -1)
                    filter += $" AND emp LIKE '%{cmbUsers.Text}%'";

                // الفئة
                if (cmbGroups.SelectedIndex > -1)
                    filter += $" AND Groupcode LIKE '%{cmbGroups.Text}%'";

                // رقم الفاتورة
                if (!string.IsNullOrWhiteSpace(txtInvNo.Text))
                {
                    if (int.TryParse(txtInvNo.Text.Trim(), out int invNo))
                        filter += $" AND id={invNo}";
                }

                // جوال العميل
                if (!string.IsNullOrWhiteSpace(txtClientMobile.Text))
                    filter += $" AND mobile LIKE '%{txtClientMobile.Text.Trim()}%'";

                // اسم العميل
                if (!string.IsNullOrWhiteSpace(txtClientName.Text))
                    filter += $" AND cust LIKE '%{txtClientName.Text.Trim()}%'";

                _dataView.RowFilter  = filter;
                dgvItems.ItemsSource = _dataView;
                CalcIncome();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تطبيق الفلتر:\n" + ex.Message);
            }
        }

        #endregion

        #region Print / Preview / Export

        private DataSet BuildReportDataSet()
        {
            // بيانات المؤسسة
            string address = "", telephone = "", mobile = "",
                   foundation = "", field = "", vATNo = "";
            try
            {
                var adapter = new SqlDataAdapter("SELECT * FROM Foundation", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    address    = dt.Rows[0]["Address"].ToString();
                    telephone  = dt.Rows[0]["Tel"].ToString();
                    mobile     = dt.Rows[0]["Mobile"].ToString();
                    foundation = dt.Rows[0]["nameA"].ToString();
                    field      = dt.Rows[0]["FieldA"].ToString();
                    vATNo      = dt.Rows[0]["tax_no"].ToString();
                }
            }
            catch { }

            var list = new List<RptRentInvData>();

            var source = dgvItems.ItemsSource as DataView;
            if (source == null) return new DataSet("Name");

            foreach (DataRowView drv in source)
            {
                var item = new RptRentInvData
                {
                    Description = drv["cust"].ToString(),
                    InvoiceNet  = drv["Net"].ToString(),
                    Dept        = drv["proc_id"].ToString(),
                    Status      = " ",
                    Balance     = drv["id"].ToString(),
                    ProcessNo   = drv["id"].ToString(),
                    ProcessType = drv["proc_type"].ToString(),
                    Marine      = drv["marine"].ToString(),
                    GroupCode   = drv["Groupcode"].ToString(),
                    Client      = drv["cust"].ToString(),
                    ClientPhone = drv["mobile"].ToString(),
                    InvUser     = drv["emp"].ToString(),
                    RestrNo     = drv["Net"].ToString(),
                    Inplan      = drv["InPlan"].ToString(),
                    RptType     = "تقرير فواتير التأجير",
                    InvDate     = drv["date1"].ToString(),
                    InvTime     = drv["date1"].ToString(),
                    FromDate    = txtFromDate.DateTime.ToShortDateString(),
                    ToDate      = txtToDate.DateTime.ToShortDateString(),
                    PrintDate   = DateTime.Now.ToShortDateString(),
                    Address     = address,
                    Mobile      = mobile,
                    Telephone   = telephone,
                    VATNo       = vATNo,
                    Foundation  = foundation,
                    Field       = field,
                    Logo        = "",
                    Header      = "",
                    Footer      = "",
                    Stamp       = "",
                };
                item.User = Common.GetEmpName(MainClass.EmpNo);
                list.Add(item);
            }

            var ds = new DataSet("Name");
            ds.Tables.Add(UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        private void PrintDevexpress(int printMode)
        {
            var source = dgvItems.ItemsSource as DataView;
            if (source == null || source.Count == 0)
            {
                DXMessageBox.Show("لا توجد عمليات بالجدول",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrEmpty(RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string fullPath = Path.Combine(RptUrl, RptName);
            if (!Directory.Exists(RptUrl) || !File.Exists(fullPath))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(RptName))
            {
                DXMessageBox.Show("يجب إدخال اسم التقرير من الإعدادات",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var report = XtraReport.FromFile(fullPath);
                report.DataSource = BuildReportDataSet();

                if (string.IsNullOrEmpty(defPrinter))
                {
                    DXMessageBox.Show("يجب تحديد الطابعة من الإعدادات",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                report.PrinterName = defPrinter;
                for (int c = 1; c <= PrintNo; c++)
                {
                    if (printMode == 1) report.Print();
                    else                report.ShowPreviewDialog();
                }
                report.Dispose();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في الطباعة:\n" + ex.Message);
            }
        }

        private void ExportToExcel()
        {
            try
            {
                var source = dgvItems.ItemsSource as DataView;
                if (source == null || source.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للتصدير",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // فتح SaveFileDialog
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter      = "CSV Files (*.csv)|*.csv",
                    DefaultExt  = ".csv",
                    FileName    = $"RentInvoices_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dlg.ShowDialog() != true) return;

                string filePath = dlg.FileName;

                using (var sw = new System.IO.StreamWriter(filePath,
                    false, System.Text.Encoding.UTF8))
                {
                    // رأس الأعمدة
                    sw.WriteLine("\"الرقم\",\"المركب\",\"الفئة\",\"الصافي\"," +
                                 "\"التاريخ\",\"العميل\",\"المستخدم\",\"الجوال\",\"ضمن الخطة\"");

                    foreach (DataRowView drv in source)
                    {
                        sw.WriteLine(
                            $"\"{drv["id"]}\"," +
                            $"\"{drv["marine"]}\"," +
                            $"\"{drv["Groupcode"]}\"," +
                            $"\"{drv["Net"]}\"," +
                            $"\"{drv["date1"]}\"," +
                            $"\"{drv["cust"]}\"," +
                            $"\"{drv["emp"]}\"," +
                            $"\"{drv["mobile"]}\"," +
                            $"\"{drv["InPlan"]}\"");
                    }
                }

                DXMessageBox.Show($"تم حفظ الملف في:\n{filePath}",
                    "تم التصدير", MessageBoxButton.OK, MessageBoxImage.Information);

                Process.Start(new ProcessStartInfo(filePath)
                    { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التصدير:\n" + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Print Settings

        private void LoadPrintSettings()
        {
            try
            {
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM SettingPrint WHERE Inv_Id=14", conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 1)
                {
                    try
                    {
                        DataRow row = dt.Rows[0];
                        if (int.TryParse(row["printType"].ToString(), out int pt))
                            PrintType = pt;
                        PrintFooter = row["PrintFooter"] != DBNull.Value &&
                                      Convert.ToBoolean(row["PrintFooter"]);
                        PrintHeader = row["PrintHeader"] != DBNull.Value &&
                                      Convert.ToBoolean(row["PrintHeader"]);
                        PrintStamp  = row["PrintStamp"]  != DBNull.Value &&
                                      Convert.ToBoolean(row["PrintStamp"]);
                        defPrinter  = row["CasherPrinter"].ToString();
                        if (string.IsNullOrEmpty(defPrinter))
                            defPrinter = Common.GetDefaultPrinter();
                        if (int.TryParse(row["printNo"].ToString(), out int pn))
                            PrintNo = pn;
                        RptName = row["RptName"].ToString();
                        RptUrl  = Path.GetDirectoryName(row["RptUrl"].ToString());
                    }
                    catch { }
                }
                else
                {
                    RptUrl     = MainClass.ReportsPath;
                    RptName    = "Statement.repx";
                    defPrinter = MainClass.ReportsPrinter;
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل إعدادات الطباعة:\n" + ex.Message);
            }
        }

        #endregion

        #region Button Events

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _dtable = new DataTable();
                LoadDgvItems();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ:\n" + ex.Message);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
            => Close();

        private void btnPreview_Click(object sender, RoutedEventArgs e)
            => PrintDevexpress(2);

        private void btnPrint_Click(object sender, RoutedEventArgs e)
            => PrintDevexpress(1);

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
            => ExportToExcel();

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            txtClientMobile.Text     = "";
            txtClientName.Text       = "";
            txtInvNo.Text            = "";
            cmbUsers.SelectedIndex   = -1;
            cmbProcType.SelectedIndex= 1;
            cmbGroups.SelectedIndex  = -1;
        }

        #endregion

        #region Filter Events

        private void cmbProcType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GenerateFilter();
        }

        private void cmbGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GenerateFilter();
        }

        private void cmbUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbUsers.SelectedIndex > -1)
                GenerateFilter();
        }

        private void txtClientName_TextChanged(object sender, TextChangedEventArgs e)
        {
            GenerateFilter();
        }

        private void txtClientMobile_TextChanged(object sender, TextChangedEventArgs e)
        {
            GenerateFilter();
        }

        private void txtInvNo_TextChanged(object sender, TextChangedEventArgs e)
        {
            GenerateFilter();
        }

        private void rdAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (rdAll.IsChecked == true) GenerateFilter();
        }

        private void rdInPlan_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (rdInPlan.IsChecked == true) GenerateFilter();
        }

        private void rdOutPlan_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (rdOutPlan.IsChecked == true) GenerateFilter();
        }

        private void txtFromDate_ValueChanged(object sender, EventArgs e)
        {
            Cond    = " AND RentInvoice.date>=@date1 AND RentInvoice.date<=@date2 ";
            _dtable = new DataTable();
            LoadDgvItems();
        }

        private void txtToDate_ValueChanged(object sender, EventArgs e)
        {
            Cond    = " AND RentInvoice.date>=@date1 AND RentInvoice.date<=@date2 ";
            _dtable = new DataTable();
            LoadDgvItems();
        }

        #endregion

        #region DataGrid Events

        private void dgvItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                RowIndex = dgvItems.SelectedIndex;
            }
            catch { RowIndex = -1; }
        }

        private void dgvItems_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (dgvItems.SelectedItem is DataRowView drv)
                {
                    ISDone   = true;
                    InvID    = Convert.ToInt32(drv["id"]);
                    ProcType = Convert.ToInt32(drv["proc_type"]);
                    Hide();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ:\n" + ex.Message);
            }
        }

        #endregion
    }
}