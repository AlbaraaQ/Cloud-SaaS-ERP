using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using CheckBox = System.Windows.Controls.CheckBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class FrmReseved : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        public SqlConnection conn;
        public SqlConnection conn1;

        public int    id_resSal = 0;
        public string cond      = "";
        public string SrchName  = "";
        public int    resNo     = 0;
        public bool   IsNew     = true;

        private int    _code          = -1;
        private int    _empNo         = -1;
        private bool   _loaded        = false;
        private string _entryGlobalID = "-1";
        private string _globalID      = "-1";
        private int    _receiptType   = 8;
        private Print  _print;

        private ObservableCollection<SalaryRow> _salarySource
            = new ObservableCollection<SalaryRow>();

        #endregion

        #region Constructor

        public FrmReseved()
        {
            InitializeComponent();
            conn   = MainClass.ConnObj();
            conn1  = MainClass.ConnObj();
            _print = new Print(10);
            GridControl1.ItemsSource = _salarySource;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // تعبئة قائمة السنوات
            for (int year = 2000; year <= 2100; year++)
                cmbYear.Items.Add(year.ToString());

            CLR();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            conn?.Close();
            conn1?.Close();
        }

        #endregion

        #region CLR

        private void CLR()
        {
            try
            {
                txtDate.DateTime = DateTime.Now;
                _salarySource.Clear();
                LoadNextNo();
                txtEntryGlobalId.Text = "";
                TxtGlobalID.Text      = "";
                btnviewEntry.Visibility = Visibility.Collapsed;

                // تعيين الشهر الحالي
                int currentMonth = DateTime.Now.Month;
                if (cmbMonth.Items.Count >= currentMonth)
                    cmbMonth.SelectedIndex = currentMonth - 1;

                // تعيين السنة الحالية
                string currentYear = DateTime.Now.ToString("yyyy");
                for (int i = 0; i < cmbYear.Items.Count; i++)
                {
                    if (cmbYear.Items[i].ToString() == currentYear)
                    {
                        cmbYear.SelectedIndex = i;
                        break;
                    }
                }
            }
            catch { }
        }

        #endregion

        #region Load Next No

        public void LoadNextNo()
        {
            try
            {
                if (!IsNew) return;

                int num = 1;
                using (var da = new SqlDataAdapter(
                    "SELECT ISNULL(MAX(id),0) AS id FROM Salary_Res", conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0 && !string.IsNullOrEmpty(dt.Rows[0][0].ToString()))
                        num = Convert.ToInt32(dt.Rows[0][0]) + 1;
                }

                txtNo.Text = num.ToString();
                resNo      = num;
            }
            catch { }
        }

        #endregion

        #region Load Employee Data

        public void LoadEmpData(string condition)
        {
            try
            {
                _salarySource.Clear();

                if (conn.State == ConnectionState.Open) conn.Close();
                conn.Open();

                string noteText = $"إستحقاق راتب شهر {GetSelectedMonth()} لسنة {GetSelectedYear()}";

                int year  = GetSelectedYearInt();
                int month = GetSelectedMonthInt();

                if (year <= 0 || month <= 0) return;

                var startDate = new DateTime(year, month, 1);
                var endDate   = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);

                using (var da = new SqlDataAdapter(
                    $@"SELECT 
                        ISNULL(E.id,0) AS id, ISNULL(E.name,'') AS name,
                        ISNULL(E.AccCode,0) AS AccCode,
                        ISNULL(E.salary_basic,0) AS salary_basic,
                        ISNULL(E.salary_add,0) AS salary_add,
                        ISNULL(E.salary_other,0) AS salary_other,
                        ISNULL(E.food,0) AS food,
                        ISNULL(E.medical,0) AS medical,
                        ISNULL(E.house,0) AS house,
                        ISNULL(E.travel,0) AS travel,
                        ISNULL(SUM(CASE WHEN SAT.ISAdd=1 AND ESA.SubFromSalary=1
                            AND ESA.[date]>=@StartDate AND ESA.[date]<@EndDate
                            AND ESA.IS_Deleted=0 THEN ESA.val ELSE 0 END),0) AS addSal,
                        ISNULL(SUM(CASE WHEN SAT.ISAdd=0 AND ESA.SubFromSalary=1
                            AND ESA.[date]>=@StartDate AND ESA.[date]<@EndDate
                            AND ESA.IS_Deleted=0 THEN ESA.val ELSE 0 END),0) AS SubSal
                    FROM Employees E
                    LEFT JOIN EmpSalaryAddSub ESA ON ESA.emp=E.id
                    LEFT JOIN SalaryAddSubTypes SAT ON ESA.type=SAT.id
                    WHERE E.IS_Deleted=0 {condition}
                    GROUP BY E.id,E.name,E.AccCode,E.salary_basic,E.salary_add,
                             E.salary_other,E.food,E.medical,E.house,E.travel",
                    conn))
                {
                    da.SelectCommand.Parameters.Add("@StartDate", SqlDbType.Date).Value = startDate;
                    da.SelectCommand.Parameters.Add("@EndDate",   SqlDbType.Date).Value = endDate.AddDays(1);

                    var dt = new DataTable();
                    da.Fill(dt);

                    int rowNo = 1;
                    foreach (DataRow row in dt.Rows)
                    {
                        decimal basic   = Convert.ToDecimal(row["salary_basic"]);
                        decimal house   = Convert.ToDecimal(row["house"]);
                        decimal travel  = Convert.ToDecimal(row["travel"]);
                        decimal addSal  = Convert.ToDecimal(row["addSal"]);
                        decimal subSal  = Convert.ToDecimal(row["SubSal"]);
                        decimal total   = basic + house + travel + addSal;
                        decimal net     = total - subSal;

                        _salarySource.Add(new SalaryRow
                        {
                            IsSelected = false,
                            DgvNo      = rowNo++,
                            Empnam     = row["name"].ToString(),
                            AccCode    = Convert.ToDouble(row["AccCode"]),
                            BasicSal   = basic,
                            HousSal    = house,
                            TravelSal  = travel,
                            Sal_add    = addSal,
                            Total      = total,
                            Sal_Sub    = subSal,
                            Net_Sal    = net,
                            Notes      = noteText,
                            Emp        = Convert.ToInt32(row["id"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل البيانات: " + ex.Message);
            }
            finally { CloseConn(conn); }
        }

        #endregion

        #region ComboBox Events

        private void cmbMonth_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (!_loaded) return;
                string note = $"إستحقاق راتب شهر {GetSelectedMonth()} لسنة {GetSelectedYear()}";
                foreach (var row in _salarySource) row.Notes = note;

                int year  = GetSelectedYearInt();
                int month = GetSelectedMonthInt();
                if (year > 0 && month > 0)
                    txtDate.DateTime = new DateTime(year, month, DateTime.Now.Day);

                LoadEmpData(cond);
            }
            catch { }
        }

        private void cmbYear_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (!_loaded) return;
                string note = $"إستحقاق راتب شهر {GetSelectedMonth()} لسنة {GetSelectedYear()}";
                foreach (var row in _salarySource) row.Notes = note;
                LoadEmpData(cond);
            }
            catch { }
        }

        #endregion

        #region DataGrid Events

        private void SelectAllRows_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox chk)
            {
                bool selectAll = chk.IsChecked == true;
                foreach (var row in _salarySource)
                    row.IsSelected = selectAll;
            }
        }

        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            bool allSelected = true;
            foreach (var row in _salarySource)
                if (!row.IsSelected) { allSelected = false; break; }

            foreach (var row in _salarySource)
                row.IsSelected = !allSelected;
        }

        private void GridControl1_CellEditEnding(object sender,
            DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (GridControl1.SelectedItem is SalaryRow row)
                    {
                        row.Total   = row.BasicSal + row.HousSal + row.TravelSal + row.Sal_add;
                        row.Net_Sal = row.Total - row.Sal_Sub;
                    }
                }));
            }
        }

        #endregion

        #region Button Events

        private void BtnAll_Click(object sender, RoutedEventArgs e)
        {
            _loaded = true;
            CLR();
            LoadEmpData(cond);
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            IsNew = true;
            CLR();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnView_Click(object sender, RoutedEventArgs e) => RptPrint(2);

        private void btnPrint_Click(object sender, RoutedEventArgs e) => RptPrint(1);

        private void btnviewEntry_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(txtEntryGlobalId.Text))
                {
                    DXMessageBox.Show("لا توجد بيانات للعرض");
                    return;
                }

                var frm = new frmRptEntries();
                frm.Show();
                frm.Navigate($"SELECT * FROM Entry WHERE IS_Deleted=0 AND GlobalID=N'{txtEntryGlobalId.Text}'");
            }
            catch { }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbMonth.SelectedIndex < 0)
                {
                    DXMessageBox.Show("يرجى اختيار الشهر",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (cmbYear.SelectedIndex < 0)
                {
                    DXMessageBox.Show("يرجى اختيار السنة",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // جمع الصفوف المحددة
                var selectedRows = new ArrayList();
                foreach (var row in _salarySource)
                    if (row.IsSelected) selectedRows.Add(row);

                if (selectedRows.Count == 0)
                {
                    DXMessageBox.Show("لا يوجد بيانات للحفظ",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ref string entryGlobalID = ref _entryGlobalID;
                int entryNo = 27;
                EntryOper.GetEntryGlobalID(ref entryGlobalID, ref entryNo);
                _globalID = $"{MainClass.BranchNo}-{txtNo.Text}";

                if (!string.IsNullOrEmpty(txtEntryGlobalId.Text) &&
                    !string.IsNullOrEmpty(TxtGlobalID.Text))
                {
                    IsNew            = false;
                    _entryGlobalID   = txtEntryGlobalId.Text;
                    _globalID        = TxtGlobalID.Text;
                }

                if (IsNew)
                    resNo = (int)MaX_ID("id", "Salary_Res");
                else
                    int.TryParse(txtNo.Text, out resNo);

                var reseved = new Reseved();

                foreach (SalaryRow row in selectedRows)
                {
                    // التحقق من صلاحية الراتب
                    if (!ValidateWorkDate(row.Emp) || !ValidateNetSalaryRow(row))
                        return;

                    var resvd = new Reseved.resvd
                    {
                        GlobalID      = _globalID,
                        _id           = resNo,
                        BranchID      = MainClass.BranchNo,
                        EntryGlobalID = _entryGlobalID,
                        ReceiptType   = 27,
                        State         = true,
                        _Net          = row.Net_Sal,
                        ReceiptDate   = DateTime.Parse(
                            txtDate.DateTime.ToShortDateString() + " " + txtTime.Text),
                        ISDeleted     = false,
                        Notes         = row.Notes,
                        _name         = row.Empnam,
                        _res_emp      = row.Emp,
                        CreditAcc     = row.AccCode.ToString(),
                        DebitAcc      = "3122001",
                        _Houses       = row.HousSal,
                        _Travel       = row.TravelSal,
                        _Basic        = row.BasicSal,
                        _saladd       = row.Sal_add,
                        _salsub       = row.Sal_Sub,
                        _Month        = GetSelectedMonthInt(),
                        _year         = GetSelectedYearInt(),
                        Cccode        = "-1",
                        Sync          = false.ToString()
                    };

                    reseved.SaveReceipt1(resvd, reseved.BindreservedToEntry(resvd, selectedRows), IsNew);
                }

                var savedMsg = new frmSavedMsg();
                savedMsg.ShowDialog();

                if (savedMsg.Pressed == 1)
                {
                    CLR();
                }
                else if (savedMsg.Pressed == 2)
                {
                    btnviewEntry.Visibility = Visibility.Visible;
                    LoadDataToGridview(_entryGlobalID);
                }
                else if (savedMsg.Pressed == 3)
                {
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"خطأ أثناء الحفظ\nتفاصيل الخطأ: {ex.Message}",
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureOpen(conn);

                int payCount = Convert.ToInt32(new SqlCommand(
                    $"SELECT COUNT(*) FROM SalaryPay " +
                    $"WHERE ResGlobalID IN (SELECT GlobalID FROM Salary_Res WHERE GlobalID=N'{TxtGlobalID.Text}') " +
                    $"AND IS_Deleted=0", conn).ExecuteScalar());

                if (payCount > 0)
                {
                    DXMessageBox.Show("لا يمكنك حذف السند يوجد سند صرف مرتبط به",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var answer = DXMessageBox.Show("هل أنت متأكد من الحذف؟", "حذف",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (answer != MessageBoxResult.Yes) return;

                new SqlCommand(
                    $"UPDATE Salary_Res SET IS_Deleted=1 WHERE GlobalID=N'{TxtGlobalID.Text}'",
                    conn).ExecuteNonQuery();
                new SqlCommand(
                    $"UPDATE Entry SET IS_Deleted=1 WHERE GlobalID=N'{txtEntryGlobalId.Text}'",
                    conn).ExecuteNonQuery();

                DXMessageBox.Show("تم الحذف بنجاح",
                    "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message);
            }
            finally { CloseConn(conn); }
        }

        #endregion

        #region Validate

        private bool ValidateWorkDate(int empId)
        {
            try
            {
                EnsureOpen(conn);
                using (var da = new SqlDataAdapter(
                    "SELECT work_date, name FROM Employees WHERE id=@EmpId", conn))
                {
                    da.SelectCommand.Parameters.AddWithValue("@EmpId", empId);
                    var dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        if (dt.Rows[0]["work_date"] == DBNull.Value)
                        {
                            DXMessageBox.Show($"تاريخ التعيين غير موجود للموظف {dt.Rows[0]["name"]}",
                                "", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return false;
                        }

                        var workDate = Convert.ToDateTime(dt.Rows[0]["work_date"]);
                        int workYM   = workDate.Year * 100 + workDate.Month;
                        int selYM    = GetSelectedYearInt() * 100 + GetSelectedMonthInt();

                        if (workYM > selYM)
                        {
                            DXMessageBox.Show(
                                $"لا يمكن الإستحقاق قبل تاريخ التعيين للموظف {dt.Rows[0]["name"]}",
                                "", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return false;
                        }
                    }
                }
            }
            catch { }
            finally { CloseConn(conn); }
            return true;
        }

        private bool ValidateNetSalaryRow(SalaryRow row)
        {
            if ((double)row.Net_Sal <= 0)
            {
                DXMessageBox.Show(
                    $"لا يمكن حفظ راتب بقيمة صفر أو أقل للموظف: {row.Empnam}\nالصافي: {row.Net_Sal:N2}",
                    "خطأ في البيانات", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        #endregion

        #region Print

        private void RptPrint(int printType)
        {
            if (string.IsNullOrEmpty(TxtGlobalID.Text))
            {
                DXMessageBox.Show("لا يمكنك الطباعة قبل الحفظ",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_print.RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_print.RptName))
                _print.RptName = "RptResevedSalary.repx";

            string path = Path.Combine(_print.RptUrl, _print.RptName);
            if (!Directory.Exists(_print.RptUrl) || !File.Exists(path))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_print.defPrinter))
                _print.defPrinter = MainClass.ReportsPrinter;

            _print.Printing(printType, BindToData(), _print.RptUrl,
                _print.RptName, _print.defPrinter, _print.kitchenprinter, _print.PrintNo);
        }

        private DataSet BindToData()
        {
            var list = new System.Collections.Generic.List<resvdPrint>();

            using (var da = new SqlDataAdapter("SELECT * FROM Foundation", conn))
            {
                var dt = new DataTable();
                da.Fill(dt);

                string address    = dt.Rows.Count > 0 ? dt.Rows[0]["Address"].ToString() : "";
                string mobile     = dt.Rows.Count > 0 ? dt.Rows[0]["Mobile"].ToString()  : "";
                string foundation = dt.Rows.Count > 0 ? dt.Rows[0]["nameA"].ToString()   : "";
                string field      = dt.Rows.Count > 0 ? dt.Rows[0]["FieldA"].ToString()  : "";
                string vatNo      = dt.Rows.Count > 0 ? dt.Rows[0]["tax_no"].ToString()  : "";

                int rowNo = 1;
                foreach (var row in _salarySource)
                {
                    string cardNo = "", bankNo = "";
                    try
                    {
                        EnsureOpen(conn);
                        using (var da2 = new SqlDataAdapter(
                            $"SELECT ISNULL(CardNo,'') AS CardNo, ISNULL(BankNo,'') AS BankNo " +
                            $"FROM Employees WHERE id='{row.Emp}'", conn))
                        {
                            var dt2 = new DataTable();
                            da2.Fill(dt2);
                            if (dt2.Rows.Count > 0)
                            {
                                cardNo = dt2.Rows[0]["CardNo"].ToString();
                                bankNo = dt2.Rows[0]["BankNo"].ToString();
                            }
                        }
                    }
                    catch { }
                    finally { CloseConn(conn); }

                    list.Add(new resvdPrint
                    {
                        BondName   = this.Title,
                        id         = txtNo.Text,
                        Dateresevd = txtDate.DateTime.ToString(),
                        No         = rowNo++.ToString(),
                        Empname    = row.Empnam,
                        AccCode    = row.AccCode.ToString(),
                        Basic      = row.BasicSal.ToString("N2"),
                        Houses     = row.HousSal.ToString("N2"),
                        travel     = row.TravelSal.ToString("N2"),
                        AddSal     = row.Sal_add.ToString("N2"),
                        SubSal     = row.Sal_Sub.ToString("N2"),
                        Total      = row.Total.ToString("N2"),
                        NetSal     = row.Net_Sal.ToString("N2"),
                        CardNo     = cardNo,
                        BankAcc    = bankNo,
                        User       = Common.GetEmpName(MainClass.EmpNo),
                        branch     = MainClass.BranchName,
                        Adress     = address,
                        Mobile     = mobile,
                        VATNo      = vatNo,
                        Foundation = foundation,
                        Field      = field,
                        Logo       = "",
                        PrintDate  = DateTime.Now.ToString()
                    });
                }
            }

            var ds = new DataSet("Name");
            ds.Tables.Add(global::UtilitiesProj.Common.ToDataTable(list));
            return ds;
        }

        #endregion

        #region Load Grid Data

        public void LoadDataToGridview(string entryGlobalId)
        {
            try
            {
                _salarySource.Clear();
                EnsureOpen(conn);

                using (var da = new SqlDataAdapter(
                    $@"SELECT ISNULL(E.id,0) AS id, ISNULL(E.name,'') AS name,
                              ISNULL(E.AccCode,'') AS AccCode,
                              ISNULL(SD.Basic,0) AS Basic,
                              ISNULL(SD.Houses,0) AS Houses,
                              ISNULL(SD.travel,0) AS travel,
                              ISNULL(SD.sal_add,0) AS sal_add,
                              ISNULL(SD.sal_Sub,0) AS sal_Sub,
                              ISNULL(SD.Net,0) AS Net,
                              ISNULL(RS.Notes,0) AS Notes,
                              ISNULL(RS.EntryGlobalID,'') AS EntryGlobalID,
                              ISNULL(Rs.GlobalID,'') AS GlobalID
                       FROM Salary_Res RS
                       LEFT JOIN Salary_Res_Details SD ON SD.GlobalId=Rs.GlobalId
                       LEFT JOIN Employees E ON SD.res_emp=E.id
                       WHERE EntryGlobalID=N'{entryGlobalId}'", conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    int rowNo = 1;

                    foreach (DataRow row in dt.Rows)
                    {
                        decimal basic  = Convert.ToDecimal(row["Basic"]);
                        decimal house  = Convert.ToDecimal(row["Houses"]);
                        decimal travel = Convert.ToDecimal(row["travel"]);
                        decimal addSal = Convert.ToDecimal(row["sal_add"]);
                        decimal subSal = Convert.ToDecimal(row["sal_Sub"]);
                        decimal total  = basic + house + travel + addSal;
                        decimal net    = total - subSal;

                        _salarySource.Add(new SalaryRow
                        {
                            DgvNo     = rowNo++,
                            Empnam    = row["name"].ToString(),
                            AccCode   = Convert.ToDouble(row["AccCode"]),
                            BasicSal  = basic,
                            HousSal   = house,
                            TravelSal = travel,
                            Sal_add   = addSal,
                            Total     = total,
                            Sal_Sub   = subSal,
                            Net_Sal   = net,
                            Notes     = "إستحقاق راتب"
                        });
                    }

                    txtEntryGlobalId.Text = _entryGlobalID;
                    TxtGlobalID.Text      = _globalID;
                    txtNo.Text            = resNo.ToString();
                }
            }
            catch { }
            finally { CloseConn(conn); }
        }

        public void Navigate(string sqlQuery)
        {
            try
            {
                EnsureOpen(conn);
                using (var cmd = new SqlCommand(sqlQuery, conn))
                using (var dr  = cmd.ExecuteReader())
                    ReadData(dr);
            }
            catch { }
            finally { CloseConn(conn); }
        }

        private void ReadData(SqlDataReader dr)
        {
            if (!dr.HasRows) return;
            dr.Read();

            _code = Convert.ToInt32(dr["id"]);
            txtNo.Text            = _code.ToString();
            txtEntryGlobalId.Text = dr["EntryGlobalID"].ToString();
            TxtGlobalID.Text      = dr["GlobalID"].ToString();
            txtDate.DateTime      = Convert.ToDateTime(dr["date"]);
            TabControl1.SelectedIndex = 0;
            btnviewEntry.Visibility   = Visibility.Visible;
            IsNew = false;

            string globalId = dr["GlobalID"].ToString();
            dr.Close();
            LoadDataToGridview2(globalId);
        }

        private void LoadDataToGridview2(string globalId)
        {
            try
            {
                _salarySource.Clear();
                EnsureOpen(conn);

                using (var da = new SqlDataAdapter(
                    $@"SELECT ISNULL(E.id,0) AS id, ISNULL(E.name,'') AS name,
                              ISNULL(E.AccCode,'') AS AccCode,
                              ISNULL(SD.Basic,0) AS Basic,
                              ISNULL(SD.Houses,0) AS Houses,
                              ISNULL(SD.travel,0) AS travel,
                              ISNULL(SD.sal_add,0) AS sal_add,
                              ISNULL(SD.sal_Sub,0) AS sal_Sub,
                              ISNULL(SD.Net,0) AS Net,
                              ISNULL(RS.Notes,0) AS Notes
                       FROM Salary_Res RS
                       LEFT JOIN Salary_Res_Details SD ON SD.GlobalId=Rs.GlobalId
                       LEFT JOIN Employees E ON SD.res_emp=E.id
                       WHERE RS.GlobalId=N'{globalId}'", conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    int rowNo = 1;

                    foreach (DataRow row in dt.Rows)
                    {
                        decimal basic  = Convert.ToDecimal(row["Basic"]);
                        decimal house  = Convert.ToDecimal(row["Houses"]);
                        decimal travel = Convert.ToDecimal(row["travel"]);
                        decimal addSal = Convert.ToDecimal(row["sal_add"]);
                        decimal subSal = Convert.ToDecimal(row["sal_Sub"]);
                        decimal total  = basic + house + travel + addSal;
                        decimal net    = total - subSal;

                        _salarySource.Add(new SalaryRow
                        {
                            DgvNo     = rowNo++,
                            Empnam    = row["name"].ToString(),
                            AccCode   = Convert.ToDouble(row["AccCode"]),
                            BasicSal  = basic,
                            HousSal   = house,
                            TravelSal = travel,
                            Sal_add   = addSal,
                            Total     = total,
                            Sal_Sub   = subSal,
                            Net_Sal   = net,
                            Notes     = row["Notes"].ToString()
                        });
                    }

                    TabControl1.SelectedIndex   = 0;
                    btnviewEntry.Visibility     = Visibility.Visible;
                }
            }
            catch { }
            finally { CloseConn(conn); }
        }

        #endregion

        #region Helper Methods

        public object MaX_ID(string columnName, string tableName)
        {
            try
            {
                EnsureOpen(conn);
                using (var da = new SqlDataAdapter(
                    $"SELECT MAX({columnName}) FROM {tableName}", conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
                        return Convert.ToInt32(dt.Rows[0][0]) + 1;
                }
            }
            catch { }
            finally { CloseConn(conn); }
            return 1;
        }

        private string GetSelectedMonth()
        {
            if (cmbMonth.SelectedItem is ComboBoxItem item) return item.Content.ToString();
            return cmbMonth.SelectedItem?.ToString() ?? DateTime.Now.Month.ToString();
        }

        private string GetSelectedYear()
        {
            return cmbYear.SelectedItem?.ToString() ?? DateTime.Now.Year.ToString();
        }

        private int GetSelectedMonthInt()
        {
            int.TryParse(GetSelectedMonth(), out int m);
            return m;
        }

        private int GetSelectedYearInt()
        {
            int.TryParse(GetSelectedYear(), out int y);
            return y;
        }

        private void EnsureOpen(SqlConnection c)
        {
            if (c.State != ConnectionState.Open) c.Open();
        }

        private void CloseConn(SqlConnection c)
        {
            if (c.State != ConnectionState.Closed) c.Close();
        }

        #endregion

        #region Model

        public class SalaryRow : INotifyPropertyChanged
        {
            private bool    _isSelected;
            private decimal _total;
            private decimal _net;
            private string  _notes;

            public bool IsSelected
            {
                get => _isSelected;
                set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
            }

            public int     DgvNo     { get; set; }
            public string  Empnam    { get; set; }
            public double  AccCode   { get; set; }
            public decimal BasicSal  { get; set; }
            public decimal HousSal   { get; set; }
            public decimal TravelSal { get; set; }
            public decimal Sal_add   { get; set; }
            public decimal Sal_Sub   { get; set; }
            public int     Emp       { get; set; }

            public decimal Total
            {
                get => _total;
                set { _total = value; OnPropertyChanged(nameof(Total)); }
            }

            public decimal Net_Sal
            {
                get => _net;
                set { _net = value; OnPropertyChanged(nameof(Net_Sal)); }
            }

            public string Notes
            {
                get => _notes;
                set { _notes = value; OnPropertyChanged(nameof(Notes)); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }
}