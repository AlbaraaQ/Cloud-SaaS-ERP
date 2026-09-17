using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using CheckBox = System.Windows.Controls.CheckBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmReGenerateEntries : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        private SqlConnection _conn1;

        public int Invtype   = 0;
        private int _procType = 1;
        private int _invType  = 0;
        private int _restType = 1;
        private int _invAcc   = 3200001;

        private ObservableCollection<InvoiceRow> _invoicesSource
            = new ObservableCollection<InvoiceRow>();

        private double _totalSum    = 0;
        private double _totalSum2   = 0;
        private double _totalDisc   = 0;
        private double _totalDisc2  = 0;
        private double _totalTax    = 0;
        private double _totalTax2   = 0;
        private double _totalNet    = 0;
        private double _totalNet2   = 0;
        private double _totalNetOT  = 0;
        private double _totalNetOT2 = 0;

        #endregion

        #region Constructor

        public frmReGenerateEntries()
        {
            InitializeComponent();
            _conn  = MainClass.ConnObj();
            _conn1 = MainClass.ConnObj();
            GridControl1.ItemsSource = _invoicesSource;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtDateFrom.DateTime = DateTime.Now;
            txtDateTo.DateTime   = DateTime.Now;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _conn?.Close();
            _conn1?.Close();
        }

        #endregion

        #region ComboBox Selection

        private void cmbProcType_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            switch (cmbProcType.SelectedIndex)
            {
                case 0: _procType = 1; _invType = 1; _restType = 1;  _invAcc = 3200001; break;
                case 1: _procType = 2; _invType = 1; _restType = 21; _invAcc = 3200002; break;
                case 2: _procType = 1; _invType = 2; _restType = 2;  _invAcc = 4200001; break;
                case 3: _procType = 2; _invType = 2; _restType = 22; _invAcc = 4200002; break;
                case 4: _procType = 1; _invType = 4; _restType = 3;  _invAcc = 4200003; break;
                case 5: _procType = 2; _invType = 4; _restType = 23; _invAcc = 4200003; break;
                case 6: _procType = 1; _invType = 3; _restType = 3;  _invAcc = 4200001; break;
                case 7: _procType = 2; _invType = 3; _restType = 32; _invAcc = 4200002; break;
            }
        }

        #endregion

        #region Show Invoices

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProcType.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار نوع الفاتورة",
                    "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ShowInvs();
        }

        public void ShowInvs()
        {
            try
            {
                _invoicesSource.Clear();
                ResetTotals();

                string branchCond = (MainClass.BranchNo != -1)
                    ? $"inv.branch={MainClass.BranchNo} AND " : "";

                // فواتير عادية (مشتريات/مبيعات/نقاط بيع)
                if (cmbProcType.SelectedIndex < 8 && _invType != 4)
                {
                    string typeCond = $" inv_type={_invType} AND proc_type={_procType} AND ";
                    if (_invType == 3) typeCond += " pay_type=-1 AND ";

                    using (var da = new SqlDataAdapter(
                        $"SELECT * FROM inv WHERE {branchCond}{typeCond}" +
                        $"date>=@date1 AND date<=@date2 AND IS_Deleted=0 ORDER BY proc_id",
                        _conn))
                    {
                        da.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value
                            = txtDateFrom.DateTime.ToShortDateString();
                        da.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value
                            = txtDateTo.DateTime.AddHours(24);

                        var dt = new DataTable();
                        da.Fill(dt);
                        ProgressBar1.Maximum = dt.Rows.Count;
                        ProgressBar1.Value   = 0;

                        int rowNo = 1;
                        foreach (DataRow row in dt.Rows)
                        {
                            try
                            {
                                string globalId  = row["InvGlobalID"].ToString();
                                int    invTypeV  = Convert.ToInt32(row["inv_type"]);
                                int    procTypeV = Convert.ToInt32(row["proc_type"]);
                                int    entryNo   = 0;
                                string invName   = "";
                                bool   showClient = false;
                                string clientLabel = "المورد";

                                // تحديد اسم الفاتورة
                                if      (invTypeV == 1 && procTypeV == 1) { invName = "مشتريات";          showClient = true;  clientLabel = "المورد"; }
                                else if (invTypeV == 1 && procTypeV == 2) { invName = "مرتجع مشتريات";    showClient = true;  clientLabel = "المورد"; }
                                else if (invTypeV == 2 && procTypeV == 1) { invName = "مبيعات";           showClient = false; clientLabel = "العميل"; }
                                else if (invTypeV == 2 && procTypeV == 2) { invName = "مرتجع مبيعات";    showClient = false; clientLabel = "العميل"; }
                                else if (invTypeV == 3 && procTypeV == 1) { invName = "نقطة بيع";        showClient = false; clientLabel = "العميل"; }
                                else if (invTypeV == 3 && procTypeV == 2) { invName = "مرتجع نقطة بيع"; showClient = false; clientLabel = "العميل"; }

                                try { entryNo = Convert.ToInt32(row["EntryID"]); } catch { }

                                // تحديث عنوان العمود
                                Dispatcher.Invoke(() => colDgvClient.Header = clientLabel);

                                // الحصول على مجاميع بنود الفاتورة
                                double total     = 0, itemDisc = 0, taxVal = 0;
                                using (var da2 = new SqlDataAdapter(
                                    $"SELECT SUM(Inv_Sub.val1*Inv_Sub.exchange_price) AS total," +
                                    $"SUM(ISNULL(Inv_Sub.discount,0)) AS ItDiscount," +
                                    $"SUM(Inv_Sub.taxval) AS TaxVal " +
                                    $"FROM Inv_Sub WHERE Inv_Sub.InvGlobalID=N'{globalId}'",
                                    _conn))
                                {
                                    var dt2 = new DataTable();
                                    da2.Fill(dt2);
                                    if (dt2.Rows.Count > 0)
                                    {
                                        double.TryParse(dt2.Rows[0]["total"].ToString(),      out total);
                                        double.TryParse(dt2.Rows[0]["ItDiscount"].ToString(), out itemDisc);
                                        double.TryParse(dt2.Rows[0]["TaxVal"].ToString(),     out taxVal);
                                    }
                                }

                                double invDisc = Convert.ToDouble(row["minus"]);
                                double totDisc = invDisc + itemDisc;
                                double netBeforeVat = total - totDisc;
                                double net = netBeforeVat + taxVal;

                                int custId = 0;
                                try { custId = Convert.ToInt32(row["cust_id"]); } catch { }

                                _invoicesSource.Add(new InvoiceRow
                                {
                                    IsSelected    = false,
                                    DgvNo         = rowNo++,
                                    DgvInvName    = invName,
                                    DgvInvNo      = Convert.ToInt32(row["id"]),
                                    DgvSuplierInvNo = row["Reff_No"].ToString(),
                                    DgvDate       = Convert.ToDateTime(row["date"]),
                                    DgvClient     = GetClientName(custId),
                                    DgvVATNo      = GetTaxNo(custId),
                                    DgvSum2       = total,
                                    DgvDiscount   = totDisc,
                                    DgvNetBeforVat = netBeforeVat,
                                    DgvVAT        = taxVal,
                                    DgvNet        = net,
                                    DgvInvType    = invTypeV.ToString(),
                                    DgvOperType   = procTypeV.ToString(),
                                    DgvInvGlobalID = globalId,
                                    DgvEntryNo    = entryNo
                                });

                                // تحديث المجاميع
                                if (procTypeV == 1) { _totalSum += total; _totalDisc += totDisc; _totalTax += taxVal; _totalNetOT += netBeforeVat; _totalNet += net; }
                                else                { _totalSum2+= total; _totalDisc2+= totDisc; _totalTax2+= taxVal; _totalNetOT2+= netBeforeVat; _totalNet2+= net; }

                                ProgressBar1.Value = rowNo - 1;
                            }
                            catch { }
                        }
                    }
                }

                // فواتير التأجير
                else if (_invType == 4)
                {
                    string rentProc = (_procType == 2) ? "Proc_Type=2 AND" : "(Proc_Type=3 OR Proc_Type=1) AND";

                    using (var da3 = new SqlDataAdapter(
                        $"SELECT id, (tot_Rent+tot_Additions) AS total, Discount AS ItDiscount, " +
                        $"tax AS TaxVal, Reff_No, cust_id, date, proc_type, Restraction_id " +
                        $"FROM RentInvoice WHERE {branchCond}{rentProc} " +
                        $"date>=@date1 AND date<=@date2 AND IS_Deleted=0 ORDER BY proc_id",
                        _conn))
                    {
                        da3.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value
                            = txtDateFrom.DateTime.ToShortDateString();
                        da3.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value
                            = txtDateTo.DateTime.AddHours(24);

                        var dt4 = new DataTable();
                        da3.Fill(dt4);

                        int rowNo = _invoicesSource.Count + 1;
                        foreach (DataRow row in dt4.Rows)
                        {
                            int    procV = Convert.ToInt32(row["proc_type"]);
                            string name  = procV switch { 1 => "تأجير", 3 => "تأجير معلق", 2 => "مرتجع تأجير", _ => "" };

                            double total    = Convert.ToDouble(row["total"]);
                            double disc     = Convert.ToDouble(row["ItDiscount"]);
                            double taxV     = Convert.ToDouble(row["TaxVal"]);
                            double netBV    = total - disc;
                            double net      = netBV + taxV;
                            int    custId   = 0;
                            try { custId = Convert.ToInt32(row["cust_id"]); } catch { }

                            _invoicesSource.Add(new InvoiceRow
                            {
                                IsSelected     = false,
                                DgvNo          = rowNo++,
                                DgvInvName     = name,
                                DgvInvNo       = Convert.ToInt32(row["id"]),
                                DgvSuplierInvNo= row["Reff_No"].ToString(),
                                DgvDate        = Convert.ToDateTime(row["date"]),
                                DgvClient      = GetClientName(custId),
                                DgvVATNo       = GetTaxNo(custId),
                                DgvSum2        = total,
                                DgvDiscount    = disc,
                                DgvNetBeforVat = netBV,
                                DgvVAT         = taxV,
                                DgvNet         = net,
                                DgvInvType     = _invType.ToString(),
                                DgvOperType    = procV.ToString(),
                                DgvInvGlobalID = row["InvGlobalID"]?.ToString() ?? "",
                                DgvEntryNo     = Convert.ToInt32(row["Restraction_id"])
                            });

                            if (procV == 1 || procV == 3) { _totalSum += total; _totalDisc += disc; _totalTax += taxV; _totalNetOT += netBV; _totalNet += net; }
                            else                           { _totalSum2+= total; _totalDisc2+= disc; _totalTax2+= taxV; _totalNetOT2+= netBV; _totalNet2+= net; }
                        }
                    }
                }

                UpdateSummaryLabels();
                lblCountsNo.Text = $"عدد السجلات: {_invoicesSource.Count}";
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
        }

        private void UpdateSummaryLabels()
        {
            txtTot.Text       = $"{_totalSum - _totalSum2:N2}";
            txtTotDiscount.Text = $"{_totalDisc - _totalDisc2:N2}";
            txtNetOutTax.Text = $"{_totalNetOT - _totalNetOT2:N2}";
            txtTotTax.Text    = $"{_totalTax - _totalTax2:N2}";
            txtNet.Text       = $"{_totalNet - _totalNet2:N2}";
        }

        private void ResetTotals()
        {
            _totalSum = _totalSum2 = _totalDisc = _totalDisc2 = 0;
            _totalTax = _totalTax2 = _totalNet = _totalNet2  = 0;
            _totalNetOT = _totalNetOT2 = 0;
        }

        #endregion

        #region Regenerate Entries

        private void btnRegenerate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var answer = DXMessageBox.Show(
                    "هل أنت متأكد من إعادة توليد قيود الفواتير المختارة؟",
                    "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (answer != MessageBoxResult.Yes) return;

                var selected = new List<InvoiceRow>();
                foreach (var row in _invoicesSource)
                    if (row.IsSelected) selected.Add(row);

                if (selected.Count == 0)
                {
                    DXMessageBox.Show("يرجى تحديد فاتورة واحدة على الأقل",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int errors = 0;
                foreach (var row in selected)
                {
                    try
                    {
                        var invoiceOper = new InvoiceOper();
                        var inv         = invoiceOper.BindInvoByID(row.DgvInvGlobalID);
                        var entryOper   = new EntryOper();

                        if (!entryOper.SaveEnty(entryOper.BindInvoiceToEntry(inv)))
                            errors++;
                    }
                    catch { errors++; }
                }

                if (errors > 0)
                    DXMessageBox.Show($"تمت العملية مع وجود {errors} خطأ",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                else
                    DXMessageBox.Show("تمت العملية بنجاح",
                        "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message,
                    "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Select All

        private void SelectAllRows_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox chk)
            {
                bool selectAll = chk.IsChecked == true;
                foreach (var row in _invoicesSource)
                    row.IsSelected = selectAll;
            }
        }

        #endregion

        #region Helper Methods

        private string GetClientName(int id)
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT name FROM Customers WHERE IS_Deleted=0 AND id={id}", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
                }
            }
            catch { return ""; }
        }

        private string GetTaxNo(int id)
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    $"SELECT tax_no FROM Customers WHERE IS_Deleted=0 AND id={id}", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
                }
            }
            catch { return ""; }
        }

        private void EnsureOpen(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Open) conn.Open();
        }

        private void CloseConn(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Closed) conn.Close();
        }

        #endregion
    }

    #region Model

    public class InvoiceRow : INotifyPropertyChanged
    {
        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public int      DgvNo          { get; set; }
        public string   DgvInvName     { get; set; }
        public int      DgvInvNo       { get; set; }
        public string   DgvSuplierInvNo{ get; set; }
        public DateTime DgvDate        { get; set; }
        public string   DgvClient      { get; set; }
        public string   DgvVATNo       { get; set; }
        public double   DgvSum2        { get; set; }
        public double   DgvDiscount    { get; set; }
        public double   DgvNetBeforVat { get; set; }
        public double   DgvVAT         { get; set; }
        public double   DgvNet         { get; set; }
        public string   DgvInvType     { get; set; }
        public string   DgvOperType    { get; set; }
        public string   DgvInvGlobalID { get; set; }
        public int      DgvEntryNo     { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this,
               new PropertyChangedEventArgs(name));
    }

    #endregion
}