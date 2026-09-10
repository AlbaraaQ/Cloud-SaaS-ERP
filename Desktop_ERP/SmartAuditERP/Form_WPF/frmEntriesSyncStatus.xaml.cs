using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DevExpress.XtraReports.UI;
using SmartAuditERP.Form_WPF;
using UtilitiesProj;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmEntriesSyncStatus : DXWindow
    {
        #region ══════════════════ الحقول العامة والخاصة ══════════════════

        private SqlConnection conn;

        public int Invtype;

        private int Proc_Type;
        private int Inv_Type;

        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int PrintType;
        private int PrintNo;
        private string defPrinter;

        private string RptName;
        private string RptUrl;

        private double InvSum;
        private double InvDiscount;
        private double InvVAT;
        private double InvAvrgCost;
        private bool IsEvaluated;
        private double defVAT;
        private bool PricIncVAT;

        private double Total;
        private double Total1;
        private double Discount;
        private double Discount1;
        private double Cost;
        private double Cost1;
        private double Profit;
        private double Profit1;
        private double sum;
        private double sum1;

        #endregion

        #region ══════════════════ المُنشئ (Constructor) ══════════════════

        public frmEntriesSyncStatus()
        {
            conn = MainClass.ConnObj();
            Invtype = 0;
            Proc_Type = 1;
            Inv_Type = 0;
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp = true;
            PrintNo = 1;
            defPrinter = string.Empty;
            RptName = string.Empty;
            RptUrl = string.Empty;
            InvSum = 0.0;
            InvDiscount = 0.0;
            InvVAT = 0.0;
            InvAvrgCost = 0.0;
            IsEvaluated = true;
            defVAT = 0.0;
            PricIncVAT = false;
            Total = 0.0;
            Total1 = 0.0;
            Discount = 0.0;
            Discount1 = 0.0;
            Cost = 0.0;
            Cost1 = 0.0;
            Profit = 0.0;
            Profit1 = 0.0;
            sum = 0.0;
            sum1 = 0.0;

            InitializeComponent();
            Loaded += frmInvsDetails_Load;
        }

        #endregion

        #region ══════════════════ تحميل الفورم ══════════════════

        private void frmInvsDetails_Load(object sender, RoutedEventArgs e)
        {
            txtFromDate.EditValue = DateTime.Today;
            txtToDate.EditValue = DateTime.Today;
            lblCurrentDate.Text = DateTime.Now.ToString("dddd  dd/MM/yyyy");

            LoadEmps();
            LoadEntryTypes();
            loadPrintSettings();
            LoadBranches();
        }

        #endregion

        #region ══════════════════ تحميل أنواع القيود ══════════════════

        private void LoadEntryTypes()
        {
            try
            {
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter("Select Name, Id from EntryTypes", conn);
                DataTable dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);

                cmbEntryType.ItemsSource = dataTable.DefaultView;
                cmbEntryType.DisplayMember = "Name";
                cmbEntryType.ValueMember = "Id";
                cmbEntryType.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region ══════════════════ جلب اسم الموظف ══════════════════

        private string GetEmpName(int employeeId)
        {
            try
            {
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                    "select name from Employees where id=" + employeeId, conn);
                DataTable dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                    return dataTable.Rows[0][0] != DBNull.Value ? dataTable.Rows[0][0].ToString() : string.Empty;

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        #endregion

        #region ══════════════════ عرض القيود (ShowInvs) ══════════════════

        public void ShowInvs(string FilterCond)
        {
            try
            {
                // بناء جدول العرض
                DataTable displayTable = new DataTable();
                displayTable.Columns.Add("IsSelected", typeof(bool));
                displayTable.Columns.Add("DgvNo", typeof(int));
                displayTable.Columns.Add("DgvEntryType", typeof(string));
                displayTable.Columns.Add("DgvEntryGlobalID", typeof(string));
                displayTable.Columns.Add("DgvEntryBranch", typeof(string));
                displayTable.Columns.Add("DgvEntryNo", typeof(int));
                displayTable.Columns.Add("DgvRefNo", typeof(string));
                displayTable.Columns.Add("DgvDate", typeof(DateTime));
                displayTable.Columns.Add("DgvUser", typeof(string));
                displayTable.Columns.Add("DgvSyncStatus", typeof(bool));
                displayTable.Columns.Add("DgvISDeleted", typeof(bool));
                displayTable.Columns.Add("BtnDetails", typeof(string));

                displayTable.Rows.Clear();
                GridControl1.ItemsSource = null;

                // بناء الاستعلام
                string sql = "select * from Entry where " + FilterCond + " id IS NOT NULL order by id";
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sql, conn);

                // إضافة بارامترات التاريخ إذا لم تكن كل الفترة
                bool isTotalPeriod = ckTotalPeriod.IsChecked == true;
                if (!isTotalPeriod)
                {
                    DateTime fromDateValue = GetDateEditValue(txtFromDate);
                    DateTime toDateValue = GetDateEditValue(txtToDate).AddHours(24);

                    sqlDataAdapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = fromDateValue.ToShortDateString();
                    sqlDataAdapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = toDateValue;
                }

                DataTable sourceTable = new DataTable();
                sqlDataAdapter.Fill(sourceTable);

                // إعداد شريط التقدم
                ProgressBar1.Value = 0;
                ProgressBar1.Maximum = sourceTable.Rows.Count > 0 ? sourceTable.Rows.Count : 1;

                int rowCount = sourceTable.Rows.Count;

                for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                {
                    DataRow sourceRow = sourceTable.Rows[rowIndex];

                    string employeeName = string.Empty;

                    // نوع القيد
                    int entryTypeValue = 0;
                    if (sourceRow["type"] != DBNull.Value)
                        entryTypeValue = Convert.ToInt32(sourceRow["type"]);
                    string entryTypeName = Common.ResrirectionType(entryTypeValue);

                    // معرف القيد
                    string entryId = sourceRow["id"] != DBNull.Value
                        ? sourceRow["id"].ToString()
                        : string.Empty;

                    // رقم المستند
                    string documentNo = sourceRow["doc_no"] != DBNull.Value
                        ? sourceRow["doc_no"].ToString()
                        : string.Empty;

                    // التاريخ
                    DateTime entryDate = DateTime.MinValue;
                    if (sourceRow["date"] != DBNull.Value)
                        entryDate = Convert.ToDateTime(sourceRow["date"]);

                    // اسم الموظف
                    if (sourceRow["EmpID"] != DBNull.Value)
                        employeeName = Common.GetEmpName(Convert.ToInt32(sourceRow["EmpID"]));

                    // حالة المزامنة
                    bool syncStatus = false;
                    if (sourceRow["Sync"] != DBNull.Value)
                        syncStatus = Convert.ToBoolean(sourceRow["Sync"]);

                    // المعرف العام
                    string globalId = sourceRow["GlobalID"] != DBNull.Value
                        ? sourceRow["GlobalID"].ToString()
                        : string.Empty;

                    // اسم الفرع
                    int branchId = 0;
                    if (sourceRow["branch"] != DBNull.Value)
                        branchId = Convert.ToInt32(sourceRow["branch"]);
                    string branchName = Common.GetBranchName(branchId);

                    // هل محذوف
                    bool isDeleted = false;
                    if (sourceRow["IS_Deleted"] != DBNull.Value)
                        isDeleted = Convert.ToBoolean(sourceRow["IS_Deleted"]);

                    // إضافة الصف إلى جدول العرض
                    displayTable.Rows.Add(
                        false,                          // IsSelected
                        displayTable.Rows.Count + 1,    // DgvNo
                        entryTypeName,                  // DgvEntryType
                        globalId,                       // DgvEntryGlobalID
                        branchName,                     // DgvEntryBranch
                        string.IsNullOrEmpty(entryId) ? 0 : Convert.ToInt32(entryId), // DgvEntryNo
                        documentNo,                     // DgvRefNo
                        entryDate,                      // DgvDate
                        employeeName,                   // DgvUser
                        syncStatus,                     // DgvSyncStatus
                        isDeleted,                      // DgvISDeleted
                        string.Empty                    // BtnDetails
                    );

                    ProgressBar1.Value = rowIndex + 1;
                }

                GridControl1.ItemsSource = displayTable.DefaultView;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region ══════════════════ زر عرض (btnShow_Click) ══════════════════

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string filterCondition = string.Empty;

                // فلتر الفرع
                if (CheckBox1.IsChecked != true && cmbBranches.EditValue != null)
                {
                    filterCondition += "Entry.branch=" + Convert.ToInt32(cmbBranches.EditValue) + " and ";
                }

                // فلتر نوع القيد
                if (ckAllInvs.IsChecked != true && cmbEntryType.EditValue != null)
                {
                    filterCondition += " Entry.type=" + Convert.ToInt32(cmbEntryType.EditValue) + " and ";
                }

                // فلتر حالة المزامنة
                if (rbSynced.IsChecked == true)
                {
                    filterCondition += " Entry.Sync=1 and ";
                }
                else if (rbNotSynced.IsChecked == true)
                {
                    filterCondition += " Entry.Sync=0 and ";
                }

                // فلتر التاريخ
                if (ckTotalPeriod.IsChecked != true)
                {
                    filterCondition += " date>=@date1 and date<=@date2 and ";
                }

                // فلتر المحذوفات
                if (ckDeleted.IsChecked == true)
                {
                    filterCondition += " IS_Deleted=1 and ";
                }

                // فلتر المستخدم
                if (ckAllUsers.IsChecked != true && cmbusers.EditValue != null)
                {
                    filterCondition += " Entry.EmpID=" + Convert.ToInt32(cmbusers.EditValue) + " and ";
                }

                ShowInvs(filterCondition);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region ══════════════════ جلب اسم المورد / العميل ══════════════════

        private string GetSupplierName(int supplierId)
        {
            string result = string.Empty;

            if (Invtype == 1)
            {
                try
                {
                    SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                        "select name from Suppliers where id=" + supplierId + " and IS_Deleted=0", conn);
                    DataTable dataTable = new DataTable();
                    sqlDataAdapter.Fill(dataTable);

                    if (dataTable.Rows.Count == 1)
                        result = dataTable.Rows[0]["name"] != DBNull.Value ? dataTable.Rows[0]["name"].ToString() : string.Empty;
                }
                catch
                {
                    result = string.Empty;
                }
            }
            else
            {
                try
                {
                    SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                        "select name from VATClients where id=" + supplierId + " and IS_Deleted=0", conn);
                    DataTable dataTable = new DataTable();
                    sqlDataAdapter.Fill(dataTable);

                    if (dataTable.Rows.Count == 1)
                        result = dataTable.Rows[0]["name"] != DBNull.Value ? dataTable.Rows[0]["name"].ToString() : string.Empty;
                }
                catch
                {
                    result = string.Empty;
                }
            }

            return result;
        }

        #endregion

        #region ══════════════════ جلب الرقم الضريبي ══════════════════

        private string GettaxNo2(int entityId)
        {
            string result = string.Empty;

            if (Invtype == 1)
            {
                try
                {
                    SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                        "select taxNo from Suppliers where id=" + entityId + " and IS_Deleted=0", conn);
                    DataTable dataTable = new DataTable();
                    sqlDataAdapter.Fill(dataTable);

                    if (dataTable.Rows.Count == 1)
                        result = dataTable.Rows[0]["taxNo"] != DBNull.Value ? dataTable.Rows[0]["taxNo"].ToString() : string.Empty;
                }
                catch
                {
                    result = string.Empty;
                }
            }
            else
            {
                try
                {
                    SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                        "select taxNo from VATClients where id=" + entityId + " and IS_Deleted=0", conn);
                    DataTable dataTable = new DataTable();
                    sqlDataAdapter.Fill(dataTable);

                    if (dataTable.Rows.Count == 1)
                        result = dataTable.Rows[0]["taxNo"] != DBNull.Value ? dataTable.Rows[0]["taxNo"].ToString() : string.Empty;
                }
                catch
                {
                    result = string.Empty;
                }
            }

            return result;
        }

        #endregion

        #region ══════════════════ معاينة (btnPreview_Click) ══════════════════

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(2);
        }

        #endregion

        #region ══════════════════ طباعة (btnPrint_Click) ══════════════════

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(1);
        }

        #endregion

        #region ══════════════════ تصدير CSV (بديل آمن لـ Export Excel) ══════════════════

        private void btnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataView dataView = GridControl1.ItemsSource as DataView;
                if (dataView == null || dataView.Count == 0)
                {
                    ShowMessage("لا توجد بيانات للتصدير.");
                    return;
                }

                Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV File (*.csv)|*.csv|All Files (*.*)|*.*",
                    FileName = "EntriesSync_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv"
                };

                if (saveDialog.ShowDialog() != true)
                    return;

                ExportDataViewToCsv(dataView, saveDialog.FileName);

                Process.Start(new ProcessStartInfo(saveDialog.FileName)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowError("خطأ في التصدير" + Environment.NewLine + "تفاصيل الخطأ: " + ex.Message);
            }
        }

        private void ExportDataViewToCsv(DataView dataView, string filePath)
        {
            StringBuilder csvContent = new StringBuilder();

            // أسماء الأعمدة المطلوبة للتصدير
            string[] exportColumnNames = new string[]
            {
                "DgvNo",
                "DgvEntryGlobalID",
                "DgvEntryBranch",
                "DgvEntryType",
                "DgvEntryNo",
                "DgvRefNo",
                "DgvDate",
                "DgvUser",
                "DgvSyncStatus",
                "DgvISDeleted"
            };

            // عناوين الأعمدة بالعربي
            string[] headerTexts = new string[]
            {
                "م",
                "المعرف العام",
                "الفرع",
                "نوع القيد",
                "رقم القيد",
                "رقم المرجع",
                "التاريخ",
                "المستخدم",
                "حالة المزامنة",
                "محذوف"
            };

            // كتابة رأس الملف
            csvContent.AppendLine(string.Join(",", headerTexts));

            // كتابة البيانات
            foreach (DataRowView rowView in dataView)
            {
                List<string> rowValues = new List<string>();

                foreach (string columnName in exportColumnNames)
                {
                    string cellValue = string.Empty;

                    if (rowView[columnName] != DBNull.Value)
                        cellValue = rowView[columnName].ToString();

                    // تحويل القيم المنطقية لنص عربي
                    if (columnName == "DgvSyncStatus")
                        cellValue = cellValue == "True" ? "متزامن" : "غير متزامن";

                    if (columnName == "DgvISDeleted")
                        cellValue = cellValue == "True" ? "محذوف" : "غير محذوف";

                    // حماية القيم التي تحتوي فاصلة
                    cellValue = cellValue.Replace("\"", "\"\"");
                    cellValue = "\"" + cellValue + "\"";

                    rowValues.Add(cellValue);
                }

                csvContent.AppendLine(string.Join(",", rowValues));
            }

            File.WriteAllText(filePath, csvContent.ToString(), Encoding.UTF8);
        }

        #endregion

        #region ══════════════════ ربط البيانات للتقرير (BindToData) ══════════════════

        private DataSet BindToData()
        {
            List<InventoryData> inventoryList = new List<InventoryData>();

            DataView dataView = GridControl1.ItemsSource as DataView;
            if (dataView != null)
            {
                for (int rowIndex = 0; rowIndex < dataView.Count; rowIndex++)
                {
                    // يمكن إضافة تعبئة InventoryData هنا حسب الحاجة
                }
            }

            DataSet dataSet = new DataSet("Name");
            DataTable reportTable = UtilitiesProj.Common.ToDataTable(inventoryList);
            dataSet.Tables.Add(reportTable);
            inventoryList.Clear();

            return dataSet;
        }

        #endregion

        #region ══════════════════ تحميل الموظفين (LoadEmps) ══════════════════

        private void LoadEmps()
        {
            try
            {
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                    "select id, name from Employees where IS_Deleted=0 order by id", conn);
                DataTable dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);

                cmbusers.ItemsSource = dataTable.DefaultView;
                cmbusers.DisplayMember = "name";
                cmbusers.ValueMember = "id";
                cmbusers.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region ══════════════════ تحميل إعدادات الطباعة (loadPrintSettings) ══════════════════

        private void loadPrintSettings()
        {
            try
            {
                // إعدادات الطباعة
                SqlDataAdapter printAdapter = new SqlDataAdapter(
                    "select * from SettingPrint where Inv_Id=12", conn);
                DataTable printTable = new DataTable();
                printAdapter.Fill(printTable);

                if (printTable.Rows.Count == 1)
                {
                    try
                    {
                        PrintType = Convert.ToInt32(printTable.Rows[0]["printType"]);
                        PrintFooter = Convert.ToBoolean(printTable.Rows[0]["PrintFooter"]);
                        PrintHeader = Convert.ToBoolean(printTable.Rows[0]["PrintHeader"]);
                        PrintStamp = Convert.ToBoolean(printTable.Rows[0]["PrintStamp"]);

                        defPrinter = printTable.Rows[0]["CasherPrinter"] != DBNull.Value
                            ? printTable.Rows[0]["CasherPrinter"].ToString()
                            : string.Empty;

                        if (string.IsNullOrEmpty(defPrinter))
                            defPrinter = Common.GetDefaultPrinter();

                        PrintNo = Convert.ToInt32(printTable.Rows[0]["printNo"]);
                    }
                    catch
                    {
                        // تجاهل أخطاء الحقول الفردية
                    }
                }

                // إعدادات عامة
                SqlDataAdapter generalAdapter = new SqlDataAdapter(
                    "select * from SettingGeneral where Inv_Id=" + Invtype, conn);
                DataTable generalTable = new DataTable();
                generalAdapter.Fill(generalTable);

                if (generalTable.Rows.Count == 1)
                {
                    try
                    {
                        PricIncVAT = Convert.ToBoolean(generalTable.Rows[0]["PriceIncVAT"]);
                        defVAT = Convert.ToDouble(generalTable.Rows[0]["MainVAT"]);
                    }
                    catch
                    {
                        // تجاهل أخطاء الحقول الفردية
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region ══════════════════ الطباعة عبر DevExpress (PrintDevexpress) ══════════════════

        private void PrintDevexpress(int printMode)
        {
            RptUrl = MainClass.ReportsPath;
            defPrinter = MainClass.ReportsPrinter;
            RptName = "rptInvsProfitSales.repx";

            // التحقق من وجود بيانات
            DataView dataView = GridControl1.ItemsSource as DataView;
            if (dataView == null || dataView.Count == 0)
            {
                ShowMessage("لا توجد عمليات بالجدول");
                return;
            }

            // التحقق من مسار التقرير
            if (string.IsNullOrEmpty(RptUrl))
            {
                ShowMessage("يجب تحديد مسار التقرير");
                return;
            }

            string fullReportPath = RptUrl + "\\" + RptName;

            if (!Directory.Exists(RptUrl) || !File.Exists(fullReportPath))
            {
                ShowMessage("المسار الحالي للتقارير غير موجود او تم تعديله", "خطأ");
                return;
            }

            if (string.IsNullOrEmpty(RptName))
            {
                ShowMessage("يجب إدخال اسم التقرير من الإعدادات");
                return;
            }

            try
            {
                // تحميل التقرير الرئيسي
                XtraReport mainReport = XtraReport.FromFile(RptUrl + "/" + RptName);
                mainReport.DataSource = BindToData();

                // تحميل تقرير الترويسة
                string headerReportPath = RptUrl + "/header.repx";
                if (File.Exists(headerReportPath))
                {
                    XtraReport headerReport = XtraReport.FromFile(headerReportPath);
                    headerReport.DataSource = Common.FoundationInfoDT;

                    XRSubreport headerSubreport = (XRSubreport)mainReport.FindControl("headerRpt", true);
                    if (headerSubreport != null)
                    {
                        headerSubreport.ReportSource = headerReport;
                    }
                }

                // الطباعة أو المعاينة
                if (!string.IsNullOrEmpty(defPrinter))
                {
                    mainReport.PrinterName = defPrinter;

                    if (printMode == 1)
                    {
                        for (int copyIndex = 1; copyIndex <= PrintNo; copyIndex++)
                        {
                            mainReport.Print();
                        }
                    }
                    else
                    {
                        mainReport.ShowPreviewDialog();
                    }

                    mainReport.Dispose();
                }
                else
                {
                    ShowMessage("يجب تحديد الطابعة من الإعدادات");
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region ══════════════════ CheckBox: كل المستخدمين ══════════════════

        private void ckAllUsers_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (ckAllUsers.IsChecked == true)
            {
                cmbusers.SelectedIndex = -1;
                cmbusers.IsEnabled = false;
            }
            else
            {
                cmbusers.IsEnabled = true;
            }
        }

        #endregion

        #region ══════════════════ CheckBox: كل الفترة ══════════════════

        private void ckTotalPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (ckTotalPeriod.IsChecked == true)
            {
                txtFromDate.IsEnabled = false;
                txtToDate.IsEnabled = false;
            }
            else
            {
                txtFromDate.IsEnabled = true;
                txtToDate.IsEnabled = true;
            }
        }

        #endregion

        #region ══════════════════ CheckBox: كل أنواع القيود ══════════════════

        private void ckAllInvs_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (ckAllInvs.IsChecked == true)
            {
                cmbEntryType.SelectedIndex = -1;
                cmbEntryType.IsEnabled = false;
            }
            else
            {
                cmbEntryType.IsEnabled = true;
            }
        }

        #endregion

        #region ══════════════════ CheckBox: كل الفروع ══════════════════

        private void CheckBox1_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (CheckBox1.IsChecked == true)
            {
                cmbBranches.SelectedIndex = -1;
                cmbBranches.IsEnabled = false;
            }
            else
            {
                cmbBranches.IsEnabled = true;
            }
        }

        #endregion

        #region ══════════════════ زر التفاصيل (Btn_Details) ══════════════════

        private void BtnDetailsCell_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button clickedButton = sender as Button;
                if (clickedButton == null)
                    return;

                DataRowView rowData = clickedButton.Tag as DataRowView;
                if (rowData == null)
                    return;

                string entryGlobalId = string.Empty;
                if (rowData["DgvEntryGlobalID"] != DBNull.Value)
                    entryGlobalId = rowData["DgvEntryGlobalID"].ToString();

                frmInvoiceDetails invoiceDetailsForm = new frmInvoiceDetails();
                MainClass.ApplyPermissionToForm(invoiceDetailsForm);
                MainClass.DoApplyUserSett(invoiceDetailsForm);
                invoiceDetailsForm.InvGlobalId = entryGlobalId;
                invoiceDetailsForm.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region ══════════════════ تحميل الفروع (LoadBranches) ══════════════════

        private void LoadBranches()
        {
            try
            {
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                    "select id, name from Branches where IS_Deleted=0", conn);

                // جدول الفروع الأول (فرع القيود)
                DataTable branchesTable = new DataTable();
                sqlDataAdapter.Fill(branchesTable);

                cmbBranches.ItemsSource = branchesTable.DefaultView;
                cmbBranches.DisplayMember = "name";
                cmbBranches.ValueMember = "id";
                cmbBranches.SelectedIndex = -1;

                // جدول الفروع الثاني (إرسال إلى)
                DataTable mainBranchTable = new DataTable();
                sqlDataAdapter.Fill(mainBranchTable);

                cmbMainBranch.ItemsSource = mainBranchTable.DefaultView;
                cmbMainBranch.DisplayMember = "name";
                cmbMainBranch.ValueMember = "id";
                cmbMainBranch.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region ══════════════════ مزامنة القيود المحددة (btnSync_Click) ══════════════════

        private async void btnSync_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Home Home = new Home();
                // التحقق من إمكانية المزامنة
                bool canUseApi = Sync.ValidAPIUrl;
                bool canUseBroker = Home._is_active;

                if (!canUseApi && !canUseBroker)
                {
                    ShowMessage("تعذر الاتصال بخدمة المزامنة. تحقق من إعدادات الاتصال.");
                    return;
                }

                // تأكيد المستخدم
                MessageBoxResult confirmResult = MessageBox.Show(
                    "هل انت متأكد من مزامنة القيود المختارة ؟",
                    "تأكيد",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult != MessageBoxResult.Yes)
                    return;

                // جلب الصفوف المحددة
                List<DataRowView> checkedRows = GetCheckedRows();

                if (checkedRows.Count == 0)
                {
                    ShowMessage("يرجى تحديد القيود المطلوب مزامنتها أولاً.");
                    return;
                }

                // إعداد كائنات المزامنة
                EntryOper entryOper = new EntryOper();
                EntryCRUD entryCRUD = new EntryCRUD(Sync.APIUrl);
                List<Entry> entriesToSync = new List<Entry>();

                // إعداد شريط التقدم
                ProgressBar1.Value = 0;
                ProgressBar1.Maximum = checkedRows.Count;

                // فتح الاتصال
                EnsureConnectionOpen();

                // معالجة كل صف محدد
                for (int rowIndex = 0; rowIndex < checkedRows.Count; rowIndex++)
                {
                    DataRowView currentRow = checkedRows[rowIndex];

                    string entryGlobalId = string.Empty;
                    if (currentRow["DgvEntryGlobalID"] != DBNull.Value)
                        entryGlobalId = currentRow["DgvEntryGlobalID"].ToString();

                    if (string.IsNullOrEmpty(entryGlobalId))
                    {
                        ProgressBar1.Value = rowIndex + 1;
                        continue;
                    }

                    // ربط القيد
                    Entry entry = entryOper.BindEntryByID(entryGlobalId);

                    if (entry != null)
                    {
                        // تعيين فرع الإرسال
                        if (cmbMainBranch.EditValue != null)
                        {
                            entry.DistBranch = Convert.ToInt32(cmbMainBranch.EditValue);
                        }

                        entriesToSync.Add(entry);
                    }

                    // الإرسال عبر Broker
                    if (canUseBroker)
                    {
                        if (ConnectBroker.CheckConnectionAndBroker())
                        {
                            string entryCond = "where GlobalID=N'" + entryGlobalId + "'";
                            string entrySubCond = "WHERE EntryGlobalID=N'" + entryGlobalId + "'";

                            SendData.SendDataa("entry", entryGlobalId,
                                SendData.GetEntryData(entryCond));

                            SendData.SendDataa("entryDetails", entryGlobalId,
                                SendData.GetEntrySubData(entrySubCond));
                        }
                    }

                    // تحديث حالة المزامنة محلياً
                    UpdateEntrySyncStatus(entryGlobalId, true);

                    ProgressBar1.Value = rowIndex + 1;
                }

                // الإرسال عبر API
                if (canUseApi && entriesToSync.Count > 0)
                {
                    try
                    {
                        object apiResult = await entryCRUD.PostEntries(entriesToSync);
                        List<Entry> postedEntries = apiResult as List<Entry>;

                        if (postedEntries != null && postedEntries.Count > 0)
                        {
                            EnsureConnectionOpen();

                            foreach (Entry postedEntry in postedEntries)
                            {
                                if (postedEntry != null && !string.IsNullOrEmpty(postedEntry.EntryGlobalID))
                                {
                                    UpdateEntrySyncStatus(postedEntry.EntryGlobalID, true);
                                }
                            }
                        }
                    }
                    catch (Exception apiEx)
                    {
                        ShowError("خطأ في المزامنة عبر API: " + apiEx.Message);
                    }
                }

                ShowMessage("تمت العملية بنجاح");

                // إعادة تحميل البيانات
                btnShow_Click(null, null);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region ══════════════════ إرسال القيود المخزنة (SendStoredEntry) ══════════════════

        public void SendStoredEntry()
        {
            try
            {
                if (!ConnectBroker.CheckConnectionAndBroker())
                    return;

                if (ckTotalPeriod.IsChecked == true)
                    return;

                DateTime fromDate = GetDateEditValue(txtFromDate);
                DateTime toDate = GetDateEditValue(txtToDate);

                string fromDateStr = fromDate.ToShortDateString();
                string toDateStr = toDate.ToShortDateString();

                string entryCond = "where Date>=N'" + fromDateStr + "' and Date<=N'" + toDateStr + "'";

                string entrySubCond = "WHERE TRY_CAST(EntryGlobalID AS nvarchar) IN (\r\n" +
                    "    SELECT GlobalID \r\n" +
                    "    FROM Entry \r\n" +
                    "    WHERE Date>=N'" + fromDateStr + "' and Date<=N'" + toDateStr + "'";

                ConnectBroker.mqttClient.Publish(
                    ConnectBroker.ClientCode + "Entry",
                    Encoding.UTF8.GetBytes(SendData.GetEntryData(entryCond)),
                    0,
                    true);

                ConnectBroker.mqttClient.Publish(
                    ConnectBroker.ClientCode + "EntrySub",
                    Encoding.UTF8.GetBytes(SendData.GetEntrySubData(entrySubCond)),
                    0,
                    true);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region ══════════════════ زر الخروج (btnClose_Click) ══════════════════

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region ══════════════════ دوال مساعدة ══════════════════

        /// <summary>
        /// جلب الصفوف المحددة (IsSelected = true) من الجدول
        /// </summary>
        private List<DataRowView> GetCheckedRows()
        {
            List<DataRowView> selectedRows = new List<DataRowView>();

            DataView dataView = GridControl1.ItemsSource as DataView;
            if (dataView == null)
                return selectedRows;

            foreach (DataRowView rowView in dataView)
            {
                bool isSelected = false;

                if (rowView["IsSelected"] != DBNull.Value)
                    isSelected = Convert.ToBoolean(rowView["IsSelected"]);

                if (isSelected)
                    selectedRows.Add(rowView);
            }

            return selectedRows;
        }

        /// <summary>
        /// تحديث حالة المزامنة في قاعدة البيانات
        /// </summary>
        private void UpdateEntrySyncStatus(string globalId, bool syncValue)
        {
            try
            {
                if (string.IsNullOrEmpty(globalId))
                    return;

                EnsureConnectionOpen();

                using (SqlCommand sqlCommand = new SqlCommand(
                    "update Entry set Sync=@Sync where GlobalID=N'" + globalId + "'", conn))
                {
                    sqlCommand.Parameters.AddWithValue("@Sync", syncValue ? 1 : 0);
                    sqlCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحديث حالة المزامنة: " + ex.Message);
            }
        }

        /// <summary>
        /// قراءة القيمة من DateEdit بشكل آمن
        /// </summary>
        private DateTime GetDateEditValue(DateEdit dateEdit)
        {
            if (dateEdit == null || dateEdit.EditValue == null)
                return DateTime.Today;

            DateTime result;
            if (dateEdit.EditValue is DateTime dateValue)
            {
                result = dateValue;
            }
            else
            {
                bool parsed = DateTime.TryParse(dateEdit.EditValue.ToString(), out result);
                if (!parsed)
                    result = DateTime.Today;
            }

            return result;
        }

        /// <summary>
        /// فتح الاتصال بقاعدة البيانات إذا كان مغلقاً
        /// </summary>
        private void EnsureConnectionOpen()
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
        }

        /// <summary>
        /// عرض رسالة معلومات
        /// </summary>
        private void ShowMessage(string message, string title = "")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// عرض رسالة خطأ
        /// </summary>
        private void ShowError(string message, string title = "خطأ")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}