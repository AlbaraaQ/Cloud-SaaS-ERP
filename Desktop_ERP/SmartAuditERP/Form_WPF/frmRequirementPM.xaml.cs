using DevExpress.Xpf.Core;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Button = System.Windows.Controls.Button;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmRequirementPM : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection _conn;
        private SqlConnection _conn1;

        private bool   _priceIncVAT = false;
        private double _defVAT      = 0;
        private int    _code        = -1;
        private int    _procType    = 1;
        private int    _restrNo     = -1;

        private ObservableCollection<TermRow>   _termsSource  = new ObservableCollection<TermRow>();
        private ObservableCollection<SearchRow> _searchSource = new ObservableCollection<SearchRow>();

        private DataTable _termsTable = new DataTable();

        #endregion

        #region Constructor

        public frmRequirementPM()
        {
            InitializeComponent();
            _conn  = MainClass.ConnObj();
            _conn1 = MainClass.ConnObj();
            dgvterms.ItemsSource = _termsSource;
            dgvSrch.ItemsSource  = _searchSource;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadRequirementNo();
                LoadCountries();
                LoadCites();
                LoadTerms();
                txtToDate.DateTime   = DateTime.Now;
                txtFromDate.DateTime = DateTime.Now;
                txtDate.DateTime     = DateTime.Now;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _conn?.Close();
            _conn1?.Close();
        }

        #endregion

        #region CLR

        private void CLR()
        {
            _termsSource.Clear();
            _searchSource.Clear();
            _code = -1;

            LoadRequirementNo();

            cmbCities.SelectedIndex  = -1;
            cmbCountry.SelectedIndex = -1;
            txtLocation.Text   = "";
            txtPopulation.Text = "";
            txtTotPrice.Text   = "0";

            txtToDate.DateTime   = DateTime.Now;
            txtFromDate.DateTime = DateTime.Now;
            txtDate.DateTime     = DateTime.Now;
        }

        #endregion

        #region Load Data

        private void LoadRequirementNo()
        {
            try
            {
                EnsureOpen(_conn);
                using (var cmd = new SqlCommand(
                    "SELECT MAX(RequirNo) FROM PM_Requirement", _conn))
                {
                    object r = cmd.ExecuteScalar();
                    int next = (r != DBNull.Value) ? Convert.ToInt32(r) + 1 : 1;
                    txtNo.Text = next.ToString();
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
            finally { CloseConn(_conn); }
        }

        public void LoadCountries()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name FROM Countries ORDER BY id", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbCountry.ItemsSource       = dt.DefaultView;
                    cmbCountry.DisplayMemberPath = "name";
                    cmbCountry.SelectedValuePath = "id";
                    cmbCountry.SelectedIndex     = -1;
                }
            }
            catch { }
        }

        public void LoadCites()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT id, name FROM Cities ORDER BY id", _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    cmbCities.ItemsSource       = dt.DefaultView;
                    cmbCities.DisplayMemberPath = "name";
                    cmbCities.SelectedValuePath = "id";
                    cmbCities.SelectedIndex     = -1;
                }
            }
            catch { }
        }

        public void LoadTerms()
        {
            try
            {
                using (var da = new SqlDataAdapter(
                    "SELECT Code, name FROM PM_Terms " +
                    "WHERE IS_Deleted=0 AND Type=2 ORDER BY Code", _conn))
                {
                    _termsTable = new DataTable();
                    da.Fill(_termsTable);

                    // ربط ComboBox في عمود البند
                    var termCol = dgvterms.Columns[0] as DataGridComboBoxColumn;
                    if (termCol != null)
                    {
                        termCol.ItemsSource       = _termsTable.DefaultView;
                        termCol.DisplayMemberPath = "name";
                        termCol.SelectedValuePath = "Code";
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region Search

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void Search()
        {
            try
            {
                _searchSource.Clear();
                string cond = "";

                if (!string.IsNullOrWhiteSpace(txtSrchNo.Text))
                    cond += $"PM_Requirement.RequirNo={txtSrchNo.Text} AND ";
                else
                    cond += " RequirNo IS NOT NULL AND ";

                using (var da = new SqlDataAdapter(
                    $"SELECT PM_Requirement.RequirNo, Countries.name AS country, " +
                    $"PM_Requirement.RegsDate AS vDate " +
                    $"FROM PM_Requirement LEFT JOIN Countries ON PM_Requirement.Country=Countries.id " +
                    $"WHERE IS_Deleted=0 AND {cond} 1=1 ORDER BY RequirNo DESC",
                    _conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);

                    foreach (DataRow row in dt.Rows)
                    {
                        _searchSource.Add(new SearchRow
                        {
                            id         = Convert.ToInt32(row["RequirNo"]),
                            requireNo  = row["RequirNo"].ToString(),
                            country    = row["country"].ToString(),
                            vDate      = Convert.ToDateTime(row["vDate"]).ToShortDateString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
        }

        private void dgvSrch_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvSrch.SelectedItem is SearchRow row)
            {
                _code = row.id;
                Navigate($"SELECT * FROM PM_Requirement WHERE RequirNo={_code}");
                TabControl1.SelectedIndex = 0;
            }
        }

        #endregion

        #region Navigation

        private void Navigate(string sqlQuery)
        {
            try
            {
                EnsureOpen(_conn);
                using (var cmd = new SqlCommand(sqlQuery, _conn))
                using (var dr  = cmd.ExecuteReader())
                    ReadData(dr);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(ex.Message);
            }
            finally { CloseConn(_conn); }
        }

        private void ReadData(SqlDataReader dr)
        {
            if (!dr.HasRows) { CLR(); return; }
            dr.Read();
            CLR();

            _code          = Convert.ToInt32(dr["RequirNo"]);
            txtNo.Text     = _code.ToString();

            if (DateTime.TryParse(dr["Regsdate"].ToString(), out DateTime d))
                txtDate.DateTime = d;

            try { cmbCities.SelectedValue  = Convert.ToDouble(dr["City"]);    } catch { }
            try { cmbCountry.SelectedValue = dr["Country"];                    } catch { }
            txtLocation.Text   = dr["Address"].ToString();
            txtPopulation.Text = dr["Population"].ToString();

            dr.Close();

            // تحميل البنود
            _termsSource.Clear();
            using (var da = new SqlDataAdapter(
                $"SELECT * FROM PM_RequirementSub " +
                $"WHERE IsDeleted=0 AND RequirNo={_code}",
                _conn))
            {
                var dt = new DataTable();
                da.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    double qty   = Convert.ToDouble(row["Quntity"]);
                    double price = Convert.ToDouble(row["price"]);

                    _termsSource.Add(new TermRow
                    {
                        TermCode = Convert.ToInt32(row["TermPk"]),
                        Qty      = qty,
                        Price    = price,
                        Total    = qty * price,
                        Status   = Convert.ToBoolean(row["status"]),
                        RefNo    = Convert.ToDouble(row["Reff_No"])
                    });
                }
            }

            CalcTotal();
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
            => Navigate("SELECT TOP 1 * FROM PM_Requirement WHERE IS_Deleted=0 ORDER BY RequirNo ASC");
        private void btnPrevious_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM PM_Requirement WHERE IS_Deleted=0 AND RequirNo<{_code} ORDER BY RequirNo DESC");
        private void btnNext_Click(object sender, RoutedEventArgs e)
            => Navigate($"SELECT TOP 1 * FROM PM_Requirement WHERE IS_Deleted=0 AND RequirNo>{_code} ORDER BY RequirNo ASC");
        private void btnLast_Click(object sender, RoutedEventArgs e)
            => Navigate("SELECT TOP 1 * FROM PM_Requirement WHERE IS_Deleted=0 ORDER BY RequirNo DESC");

        #endregion

        #region Calculation

        private void CalcTotal()
        {
            try
            {
                double total = 0;
                foreach (var row in _termsSource)
                    total += row.Qty * row.Price;
                txtTotPrice.Text = $"{total:N2}";
            }
            catch { }
        }

        #endregion

        #region DataGrid Events

        private void DeleteTermRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TermRow row)
            {
                var answer = DXMessageBox.Show("هل تريد حذف البند؟",
                    "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (answer == MessageBoxResult.Yes)
                {
                    _termsSource.Remove(row);
                    CalcTotal();
                }
            }
        }

        #endregion

        #region Action Buttons

        private void btnNew_Click(object sender, RoutedEventArgs e) => CLR();
        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();
        private void btnPrint_Click(object sender, RoutedEventArgs e) { }

        private void btnDelete_Click(object sender, RoutedEventArgs e) { }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SqlTransaction transaction = null;
            try
            {
                if (_termsSource.Count == 0)
                {
                    DXMessageBox.Show("يجب استكمال بيانات البند",
                        "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                EnsureOpen(_conn);
                EnsureOpen(_conn1);
                transaction = _conn.BeginTransaction();

                if (MainClass.IsTrial && _code == -1)
                {
                    using (var da = new SqlDataAdapter("SELECT id FROM Entry", _conn1))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        if (dt.Rows.Count >= 20)
                        {
                            DXMessageBox.Show("نأسف لقد وصلت لأقصى حد إدخال للنسخة التجريبية.",
                                "", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                    }
                }

                if (_code == -1)
                {
                    using (var cmd = new SqlCommand(
                        "INSERT INTO PM_Requirement(RequirNo,Country,City,Address,Population,RegsDate,IS_Deleted) " +
                        "VALUES(@RequirNo,@Country,@City,@Address,@Population,@RegsDate,@IS_Deleted)",
                        _conn, transaction))
                    {
                        double.TryParse(txtNo.Text, out double reqNo);
                        cmd.Parameters.Add("@RequirNo",   SqlDbType.Int).Value      = (int)reqNo;
                        cmd.Parameters.Add("@Country",    SqlDbType.Int).Value      = cmbCountry.SelectedValue ?? DBNull.Value;
                        cmd.Parameters.Add("@City",       SqlDbType.Int).Value      = cmbCities.SelectedValue  ?? DBNull.Value;
                        cmd.Parameters.Add("@Address",    SqlDbType.NVarChar).Value = txtLocation.Text;
                        cmd.Parameters.Add("@Population", SqlDbType.Decimal).Value  = double.TryParse(txtPopulation.Text, out double pop) ? pop : 0;
                        cmd.Parameters.Add("@RegsDate",   SqlDbType.DateTime).Value = txtDate.DateTime;
                        cmd.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value      = 0;
                        cmd.ExecuteNonQuery();
                    }

                    // إدراج البنود
                    double.TryParse(txtNo.Text, out double reqNoSub);
                    foreach (var row in _termsSource)
                    {
                        using (var subCmd = new SqlCommand(
                            "INSERT INTO PM_RequirementSub(RequirNo,TermPk,Quntity,price,status,Reff_No) " +
                            "VALUES(@RequirNo,@TermPk,@Quntity,@price,@status,@Reff_No)",
                            _conn, transaction))
                        {
                            subCmd.Parameters.Add("@RequirNo", SqlDbType.Int).Value      = (int)reqNoSub;
                            subCmd.Parameters.Add("@TermPK",   SqlDbType.Int).Value      = row.TermCode;
                            subCmd.Parameters.Add("@Quntity",  SqlDbType.Int).Value      = row.Qty;
                            subCmd.Parameters.Add("@price",    SqlDbType.Float).Value    = row.Price;
                            subCmd.Parameters.Add("@Reff_No",  SqlDbType.Int).Value      = row.RefNo;
                            subCmd.Parameters.Add("@status",   SqlDbType.Bit).Value      = row.Status;
                            subCmd.ExecuteNonQuery();
                        }
                    }
                }

                transaction.Commit();
                _termsSource.Clear();

                var savedMsg = new frmSavedMsg();
                savedMsg.ShowDialog();

                if (savedMsg.Pressed == 1)
                    CLR();
                else if (savedMsg.Pressed == 3)
                {
                    CLR();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                string msg = MainClass.Language == "ar"
                    ? $"خطأ أثناء الحفظ\nتفاصيل الخطأ: {ex.Message}"
                    : $"error in saving\nError details: {ex.Message}";
                DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CloseConn(_conn);
                CloseConn(_conn1);
            }
        }

        private void btnAddCountry_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new frmCountries().ShowDialog();
                LoadCountries();
            }
            catch { }
        }

        private void btnAddCity_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new frmCities().ShowDialog();
                LoadCites();
            }
            catch { }
        }

        private void btnAddContract_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_termsSource.Count > 0)
                {
                    double.TryParse(txtNo.Text, out double reqNo);
                    var frm = new frmContractPM();
                    frm.Show();
                    frm.Activate();
                    frm.LoadRequirements((int)reqNo);
                }
            }
            catch { }
        }

        #endregion

        #region Helpers

        private void EnsureOpen(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Open) conn.Open();
        }

        private void CloseConn(SqlConnection conn)
        {
            if (conn.State != ConnectionState.Closed) conn.Close();
        }

        #endregion

        #region Models

        public class TermRow : INotifyPropertyChanged
        {
            private double _qty;
            private double _price;
            private double _total;

            public int    TermCode { get; set; }
            public double Qty
            {
                get => _qty;
                set { _qty = value; _total = _qty * _price; OnPropertyChanged(nameof(Qty)); OnPropertyChanged(nameof(Total)); }
            }
            public double Price
            {
                get => _price;
                set { _price = value; _total = _qty * _price; OnPropertyChanged(nameof(Price)); OnPropertyChanged(nameof(Total)); }
            }
            public double Total
            {
                get => _total;
                set { _total = value; OnPropertyChanged(nameof(Total)); }
            }
            public double RefNo    { get; set; }
            public bool   Status   { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public class SearchRow
        {
            public int    id        { get; set; }
            public string requireNo { get; set; }
            public string country   { get; set; }
            public string vDate     { get; set; }
        }

        #endregion
    }
}