using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmItemPrice : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;

        private int Code;
        private int Emp_No;
        private bool _IsUpdateDG;

        private ObservableCollection<PriceRow> _priceRows;
        private ObservableCollection<SrchRow> _srchRows;

        #endregion

        #region Constructor

        public frmItemPrice()
        {
            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            Code = -1;
            Emp_No = -1;
            _IsUpdateDG = false;
            _priceRows = new ObservableCollection<PriceRow>();
            _srchRows = new ObservableCollection<SrchRow>();

            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmCurrencyPrice_Load(object sender, RoutedEventArgs e)
        {
            txtDate.SelectedDate = DateTime.Today;
            txtTime.Text = DateTime.Now.ToString("HH:mm");

            LoadCurrency();
            LoadDG("");
            LoadEmp(MainClass.EmpNo);

            try { LoadData(); }
            catch { }

            WindowState = MainClass.Window_State == WindowState.Maximized
                ? WindowState.Maximized
                : WindowState.Normal;
        }

        #endregion

        #region Clear

        private void CLR()
        {
            txtDate.SelectedDate = DateTime.Today;
            txtTime.Text = DateTime.Now.ToString("HH:mm");
            LoadEmp(MainClass.EmpNo);
            cmbCurrency1.SelectedIndex = -1;
            txtPurch.Text = "";
            txtSale.Text = "";
            txtLowPurch.Text = "";
            txtHighPurch.Text = "";
            txtLowSale.Text = "";
            txtHighSale.Text = "";
            _IsUpdateDG = false;
            cmbCurrency1.Focus();
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            CLR();
        }

        #endregion

        #region Load Data

        public void LoadCurrency()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter("select id,name as currency from Items order by id", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbCurrency1.ItemsSource = dt.DefaultView;
                cmbCurrency1.SelectedIndex = -1;

                SqlDataAdapter adapter2 = new SqlDataAdapter("select id,name as currency from Items order by id", conn);
                DataTable dt2 = new DataTable();
                adapter2.Fill(dt2);

                cmbCurrencySrch.ItemsSource = dt2.DefaultView;
                cmbCurrencySrch.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError("خطأ تحميل الأصناف", ex);
            }
        }

        private void LoadEmp(int emp)
        {
            if (emp == -1) return;

            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select name from Employees where id=" + emp, conn1);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    Emp_No = emp;
                    txtEmp.Text = dt.Rows[0][0].ToString();
                }
            }
            catch { }
        }

        private void LoadData()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter("select id from Currency_Lastprice", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0) { Code = -1; return; }

                int.TryParse(dt.Rows[0][0].ToString(), out Code);
                Navigate("select * from Currency_Lastprice where id=" + Code);
            }
            catch { }
        }

        private void LoadDG(string extraCondition)
        {
            try
            {
                _srchRows.Clear();

                string sql = "select currency1,purch_price,sale_price,Currency_Lastprice_Sub.time," +
                             "Currency_Lastprice_Sub.date,Emp from Currency_Lastprice,Currency_Lastprice_Sub " +
                             "where Currency_Lastprice.id = Currency_Lastprice_Sub.doc_id and IS_Deleted=0 " +
                             extraCondition;

                SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                string currentTime = txtTime.Text;

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow row = dt.Rows[i];

                    string itemName = "";
                    SqlDataAdapter itemAdapter = new SqlDataAdapter(
                        "select name from Items where id=" + row["currency1"], conn);
                    DataTable itemDt = new DataTable();
                    itemAdapter.Fill(itemDt);
                    if (itemDt.Rows.Count > 0)
                        itemName = itemDt.Rows[0][0].ToString();

                    string timeText = NormalizeTime(row["time"].ToString(), currentTime);

                    string dateText = "";
                    try { dateText = Convert.ToDateTime(row["date"]).ToShortDateString(); } catch { }

                    string empName = "";
                    if (row["emp"] != DBNull.Value)
                    {
                        int empId = 0;
                        int.TryParse(row["emp"].ToString(), out empId);
                        empName = GetEmpName(empId);
                    }

                    _srchRows.Add(new SrchRow
                    {
                        Column11 = itemName,
                        Column14 = row["purch_price"].ToString(),
                        Column15 = row["sale_price"].ToString(),
                        Column19 = timeText,
                        Column20 = dateText,
                        Column16 = empName,
                        Column17 = row["currency1"].ToString()
                    });
                }

                dgvSrch.ItemsSource = _srchRows;
                dgvSrch.UnselectAll();
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل البيانات", ex);
            }
        }

        private string NormalizeTime(string timeValue, string referenceTime)
        {
            if (timeValue.Contains("ص") && (referenceTime.Contains("AM") || referenceTime.Contains("PM")))
                timeValue = timeValue.Replace("ص", "AM");

            if (timeValue.Contains("م") && (referenceTime.Contains("AM") || referenceTime.Contains("PM")))
                timeValue = timeValue.Replace("م", "PM");

            if (timeValue.Contains("AM") && (referenceTime.Contains("ص") || referenceTime.Contains("م")))
                timeValue = timeValue.Replace("AM", "ص");

            if (timeValue.Contains("PM") && (referenceTime.Contains("ص") || referenceTime.Contains("م")))
                timeValue = timeValue.Replace("PM", "م");

            return timeValue;
        }

        #endregion

        #region Save

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_priceRows == null || _priceRows.Count == 0)
                {
                    DXMessageBox.Show("لا توجد أصناف مدخلة", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                if (Code != -1)
                {
                    SqlCommand updateCmd = new SqlCommand(
                        "update Currency_Lastprice set date=@date where id=" + Code, conn);
                    updateCmd.Parameters.Add("@date", SqlDbType.DateTime).Value = DateTime.Now;
                    updateCmd.ExecuteNonQuery();

                    new SqlCommand("delete from Currency_Lastprice_Sub where doc_id=" + Code, conn).ExecuteNonQuery();
                }

                if (Code == -1)
                {
                    SqlCommand insertMain = new SqlCommand(
                        "insert into Currency_Lastprice(date,IS_Deleted) values(@date,0)", conn);
                    insertMain.Parameters.Add("@date", SqlDbType.DateTime).Value = DateTime.Now;
                    insertMain.ExecuteNonQuery();

                    object maxId = new SqlCommand("select max(id) from Currency_Lastprice", conn).ExecuteScalar();
                    double.TryParse(maxId?.ToString(), out double newId);
                    Code = (int)newId;
                }

                foreach (PriceRow row in _priceRows)
                {
                    SqlCommand subCmd = new SqlCommand(
                        "insert into Currency_Lastprice_Sub(doc_id,currency1,time,date,purch_price,sale_price," +
                        "low_purch_price,high_purch_price,low_sale_price,high_sale_price,Emp) " +
                        "values(@doc_id,@currency1,@time,@date,@purch_price,@sale_price," +
                        "@low_purch_price,@high_purch_price,@low_sale_price,@high_sale_price,@Emp)", conn);

                    subCmd.Parameters.AddWithValue("@doc_id", Code);
                    subCmd.Parameters.AddWithValue("@currency1", row.ItemId);
                    subCmd.Parameters.AddWithValue("@time", row.Column3 ?? "");

                    DateTime rowDate = DateTime.Today;
                    DateTime.TryParse(row.Column4, out rowDate);
                    subCmd.Parameters.AddWithValue("@date", rowDate);

                    double purch = 0, sale = 0, lowP = 0, highP = 0, lowS = 0, highS = 0;
                    double.TryParse(row.Column5, out purch);
                    double.TryParse(row.Column6, out sale);
                    double.TryParse(row.Column7, out lowP);
                    double.TryParse(row.Column8, out highP);
                    double.TryParse(row.Column9, out lowS);
                    double.TryParse(row.Column10, out highS);

                    subCmd.Parameters.AddWithValue("@purch_price", purch);
                    subCmd.Parameters.AddWithValue("@sale_price", sale);
                    subCmd.Parameters.AddWithValue("@low_purch_price", lowP);
                    subCmd.Parameters.AddWithValue("@high_purch_price", highP);
                    subCmd.Parameters.AddWithValue("@low_sale_price", lowS);
                    subCmd.Parameters.AddWithValue("@high_sale_price", highS);

                    int empNo = GetEmpNo(row.Column12 ?? "");
                    if (empNo != -1)
                        subCmd.Parameters.AddWithValue("@Emp", empNo);
                    else
                        subCmd.Parameters.AddWithValue("@Emp", DBNull.Value);

                    subCmd.ExecuteNonQuery();
                }

                DXMessageBox.Show("تم الحفظ", "", MessageBoxButton.OK, MessageBoxImage.Information);
                CalcAvgCost();
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء الحفظ", ex);
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                    conn.Close();
            }
        }

        #endregion

        #region Add to Grid

        private void Add2DG()
        {
            if (cmbCurrency1.SelectedValue == null)
            {
                DXMessageBox.Show("اختر الصنف", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbCurrency1.Focus();
                return;
            }

            double purchVal = 0;
            double.TryParse(txtPurch.Text, out purchVal);
            if (string.IsNullOrWhiteSpace(txtPurch.Text) || purchVal == 0)
            {
                DXMessageBox.Show("ادخل سعر الشراء", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPurch.Focus();
                return;
            }

            double saleVal = 0;
            double.TryParse(txtSale.Text, out saleVal);
            if (string.IsNullOrWhiteSpace(txtSale.Text) || saleVal == 0)
            {
                DXMessageBox.Show("ادخل سعر البيع", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSale.Focus();
                return;
            }

            string itemId = cmbCurrency1.SelectedValue.ToString();
            string itemName = (cmbCurrency1.SelectedItem as DataRowView)?["currency"]?.ToString() ?? "";

            double lowP = 0, highP = 0, lowS = 0, highS = 0;
            double.TryParse(txtLowPurch.Text, out lowP);
            double.TryParse(txtHighPurch.Text, out highP);
            double.TryParse(txtLowSale.Text, out lowS);
            double.TryParse(txtHighSale.Text, out highS);

            string dateText = txtDate.SelectedDate?.ToShortDateString() ?? DateTime.Today.ToShortDateString();

            if (_IsUpdateDG && dgvPrices.SelectedItem != null)
            {
                PriceRow existing = dgvPrices.SelectedItem as PriceRow;
                if (existing != null)
                {
                    existing.Column1 = itemName;
                    existing.Column3 = txtTime.Text;
                    existing.Column4 = dateText;
                    existing.Column5 = purchVal.ToString();
                    existing.Column6 = saleVal.ToString();
                    existing.Column7 = lowP.ToString();
                    existing.Column8 = highP.ToString();
                    existing.Column9 = lowS.ToString();
                    existing.Column10 = highS.ToString();
                    existing.Column12 = txtEmp.Text;
                    existing.ItemId = itemId;

                    int idx = _priceRows.IndexOf(existing);
                    _priceRows.RemoveAt(idx);
                    _priceRows.Insert(idx, existing);
                    dgvPrices.ItemsSource = _priceRows;
                    return;
                }
            }

            _priceRows.Add(new PriceRow
            {
                ItemId = itemId,
                Column1 = itemName,
                Column3 = txtTime.Text,
                Column4 = dateText,
                Column5 = purchVal.ToString(),
                Column6 = saleVal.ToString(),
                Column7 = lowP.ToString(),
                Column8 = highP.ToString(),
                Column9 = lowS.ToString(),
                Column10 = highS.ToString(),
                Column12 = txtEmp.Text
            });

            dgvPrices.ItemsSource = _priceRows;
            _IsUpdateDG = true;
        }

        private void btnSave2DG_Click(object sender, RoutedEventArgs e)
        {
            Add2DG();
        }

        private void btnNew2DG_Click(object sender, RoutedEventArgs e)
        {
            txtPurch.Text = "";
            txtSale.Text = "";
            txtLowPurch.Text = "";
            txtHighPurch.Text = "";
            txtLowSale.Text = "";
            txtHighSale.Text = "";
            txtTime.Text = DateTime.Now.ToString("HH:mm");
            txtDate.SelectedDate = DateTime.Today;
            _IsUpdateDG = false;
            cmbCurrency1.Focus();
        }

        private void TextBox8_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                Add2DG();
        }

        #endregion

        #region Navigate

        private void Navigate(string sqlStr)
        {
            dgvSrch.UnselectAll();

            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                SqlCommand cmd = new SqlCommand(sqlStr, conn);
                ReadData(cmd.ExecuteReader());
            }
            catch (Exception ex)
            {
                ShowError("خطأ في التنقل", ex);
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                    conn.Close();
            }
        }

        private void ReadData(SqlDataReader dr)
        {
            try
            {
                if (!dr.HasRows) return;

                dr.Read();
                CLR();

                int.TryParse(dr["id"].ToString(), out Code);
                dr.Close();

                _priceRows.Clear();

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select * from Currency_Lastprice_Sub where doc_id=" + Code, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                string currentTime = txtTime.Text;

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow row = dt.Rows[i];

                    string timeText = NormalizeTime(row["time"].ToString(), currentTime);
                    string dateText = "";
                    try { dateText = Convert.ToDateTime(row["date"]).ToShortDateString(); } catch { }

                    string empName = "";
                    if (row["Emp"] != DBNull.Value)
                    {
                        int empId = 0;
                        int.TryParse(row["Emp"].ToString(), out empId);
                        empName = GetEmpName(empId);
                    }

                    _priceRows.Add(new PriceRow
                    {
                        ItemId = row["currency1"].ToString(),
                        Column1 = row["currency1"].ToString(),
                        Column3 = timeText,
                        Column4 = dateText,
                        Column5 = row["purch_price"].ToString(),
                        Column6 = row["sale_price"].ToString(),
                        Column7 = row["low_purch_price"].ToString(),
                        Column8 = row["high_purch_price"].ToString(),
                        Column9 = row["low_sale_price"].ToString(),
                        Column10 = row["high_sale_price"].ToString(),
                        Column12 = empName
                    });
                }

                dgvPrices.ItemsSource = _priceRows;
            }
            catch (Exception ex)
            {
                ShowError("خطأ في قراءة البيانات", ex);
            }
        }

        #endregion

        #region Calc

        private void CalcAvgCost()
        {
            try
            {
                if (cmbCurrency1.SelectedValue == null) return;

                double totalCost = 0.0;
                int totalQty = 0;
                double avgCost = 0.0;

                string branchFilter = MainClass.BranchNo != -1
                    ? "inv.branch=" + MainClass.BranchNo + " and " : "";

                string sql1 = "select sum(val),sum(val1*exchange_price) from inv,inv_sub where " +
                              branchFilter + "ItemId=" + cmbCurrency1.SelectedValue +
                              " and inv.proc_type=1 and inv_sub.proc_type=1 and inv.InvGlobalID=inv_sub.InvGlobalID and IS_Deleted=0";

                SqlDataAdapter adapter1 = new SqlDataAdapter(sql1, conn);
                DataTable dt1 = new DataTable();
                adapter1.Fill(dt1);

                if (dt1.Rows.Count > 0 && !string.IsNullOrEmpty(dt1.Rows[0][1]?.ToString()))
                {
                    double.TryParse(dt1.Rows[0][1].ToString(), out double cost1);
                    int.TryParse(dt1.Rows[0][0].ToString(), out int qty1);
                    totalCost += cost1;
                    totalQty += qty1;
                }

                string sql2 = "select sum(val),sum(val1*exchange_price) from inv,inv_sub where " +
                              branchFilter + "ItemId=" + cmbCurrency1.SelectedValue +
                              " and inv.proc_type=3 and inv.InvGlobalID=inv_sub.InvGlobalID and IS_Deleted=0";

                SqlDataAdapter adapter2 = new SqlDataAdapter(sql2, conn);
                DataTable dt2 = new DataTable();
                adapter2.Fill(dt2);

                if (dt2.Rows.Count > 0 && !string.IsNullOrEmpty(dt2.Rows[0][1]?.ToString()))
                {
                    double.TryParse(dt2.Rows[0][1].ToString(), out double cost2);
                    int.TryParse(dt2.Rows[0][0].ToString(), out int qty2);
                    totalCost += cost2;
                    totalQty += qty2;
                }

                if (totalQty != 0)
                {
                    avgCost = Math.Floor(totalCost / totalQty * 100000000.0) / 100000000.0;
                }
                else
                {
                    SqlDataAdapter lastPriceAdapter = new SqlDataAdapter(
                        "select purch_price from Currency_Lastprice_Sub where currency1=" + cmbCurrency1.SelectedValue, conn);
                    DataTable lastPriceDt = new DataTable();
                    lastPriceAdapter.Fill(lastPriceDt);

                    if (lastPriceDt.Rows.Count > 0)
                        double.TryParse(lastPriceDt.Rows[0][0].ToString(), out avgCost);
                }

                txtAveragePrice.Text = avgCost.ToString("N4");
            }
            catch { }
        }

        #endregion

        #region Grid Events

        private void dgvPrices_CellClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                PriceRow selected = dgvPrices.SelectedItem as PriceRow;
                if (selected == null) return;

                int idx = _priceRows.IndexOf(selected);
                DGPrice_CellClick(idx);
            }
            catch { }
        }

        private void DGPrice_CellClick(int index)
        {
            try
            {
                PriceRow row = _priceRows[index];

                cmbCurrency1.SelectedValue = row.ItemId;
                txtTime.Text = row.Column3;

                DateTime d = DateTime.Today;
                DateTime.TryParse(row.Column4, out d);
                txtDate.SelectedDate = d;

                txtPurch.Text = row.Column5;
                txtSale.Text = row.Column6;
                txtLowPurch.Text = row.Column7;
                txtHighPurch.Text = row.Column8;
                txtLowSale.Text = row.Column9;
                txtHighSale.Text = row.Column10;

                _IsUpdateDG = true;
            }
            catch { }
        }

        private void dgvSrch_CellClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                SrchRow selected = dgvSrch.SelectedItem as SrchRow;
                if (selected == null) return;

                foreach (PriceRow priceRow in _priceRows)
                {
                    if (priceRow.ItemId == selected.Column17)
                    {
                        dgvPrices.SelectedItem = priceRow;
                        int idx = _priceRows.IndexOf(priceRow);
                        DGPrice_CellClick(idx);
                        break;
                    }
                }

                TabControl1.SelectedIndex = 0;
                cmbCurrency1.Focus();
            }
            catch { }
        }

        #endregion

        #region Search

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (!chkSrchAll.IsChecked == true && string.IsNullOrWhiteSpace(txtCurrencyNoSrch.Text))
            {
                DXMessageBox.Show("ادخل الصنف", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCurrencyNoSrch.Focus();
                return;
            }

            Search();
        }

        private void Search()
        {
            if (chkSrchAll.IsChecked != true)
                LoadDG("and currency1=" + txtCurrencyNoSrch.Text);
            else
                LoadDG("");
        }

        private void chkSrchAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool enableSearch = chkSrchAll.IsChecked != true;
            cmbCurrencySrch.IsEnabled = enableSearch;
            txtCurrencyNoSrch.IsEnabled = enableSearch;
        }

        private void txtCurrencyNoSrch_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(txtCurrencyNoSrch.Text))
                    cmbCurrencySrch.SelectedValue = txtCurrencyNoSrch.Text;
            }
            catch { }
        }

        private void cmbCurrencySrch_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbCurrencySrch.SelectedValue != null)
                    txtCurrencyNoSrch.Text = cmbCurrencySrch.SelectedValue.ToString();
            }
            catch { }
        }

        #endregion

        #region Buttons

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ComboBox1_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                txtDate.SelectedDate = DateTime.Today;
                txtTime.Text = DateTime.Now.ToString("HH:mm");
                CalcAvgCost();
            }
            catch { }
        }

        #endregion

        #region KeyDown

        private void frmCurrencyPrice_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Next);
                UIElement element = Keyboard.FocusedElement as UIElement;
                element?.MoveFocus(request);
                e.Handled = true;
            }
        }

        #endregion

        #region Helpers

        private int GetEmpNo(string empName)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id from Employees where name='" + empName + "'", conn1);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : -1;
            }
            catch { return -1; }
        }

        private string GetEmpName(int empId)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select name from Employees where id=" + empId, conn1);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        private void ShowError(string context, Exception ex)
        {
            DXMessageBox.Show(context + Environment.NewLine + "تفاصيل الخطأ: " + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }

    // ─── Models ─────────────────────────────────────────────────────────────

    public class PriceRow
    {
        public string ItemId { get; set; }
        public string Column1 { get; set; }
        public string Column3 { get; set; }
        public string Column4 { get; set; }
        public string Column5 { get; set; }
        public string Column6 { get; set; }
        public string Column7 { get; set; }
        public string Column8 { get; set; }
        public string Column9 { get; set; }
        public string Column10 { get; set; }
        public string Column12 { get; set; }
    }

    public class SrchRow
    {
        public string Column11 { get; set; }
        public string Column14 { get; set; }
        public string Column15 { get; set; }
        public string Column19 { get; set; }
        public string Column20 { get; set; }
        public string Column16 { get; set; }
        public string Column17 { get; set; }
    }
}