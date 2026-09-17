using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmUnits : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private int Code;
        private bool IsNew;

        private ObservableCollection<UnitItem> _units;

        // حقل مخفي لرقم الوحدة
        private TextBox txtID = new TextBox { Visibility = System.Windows.Visibility.Collapsed };

        #endregion

        #region Constructor

        public frmUnits()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
            Code = -1;
            IsNew = true;
            _units = new ObservableCollection<UnitItem>();
            dgvUnits.ItemsSource = _units;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDG();
            cmbInvoice.SelectedIndex = 0;
            CLR();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // F11: تحديث الوحدات من الإنترنت
            if (e.Key == Key.F11)
            {
                var confirm = DXMessageBox.Show("هل تريد تحديث بيانات الوحدات؟", "",
                                              MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm == MessageBoxResult.Yes && Sync.ValidAPIUrl)
                    new ItemOper().ReadUnitsOnline();
            }
        }

        #endregion

        #region Clear

        private void CLR()
        {
            txtName.Text = "";
            txtUnitCode.Text = "";
            Code = -1;
            IsNew = true;
            cmbInvoice.SelectedIndex = 0;
            LoadCode();
        }

        private void LoadCode()
        {
            try
            {
                int nextId = GetUnitId();
                txtID.Text = nextId.ToString();
            }
            catch { }
        }

        private int GetUnitId()
        {
            try
            {
                EnsureOpen(conn);
                int count = Convert.ToInt32(
                    new SqlCommand("SELECT COUNT(*) FROM units", conn).ExecuteScalar());
                int id = count + MainClass.BranchNo * 100 + 1;

                while (true)
                {
                    var checkCmd = new SqlCommand(
                        $"SELECT COUNT(*) FROM units WHERE id={id}", conn);
                    if (Convert.ToInt32(checkCmd.ExecuteScalar()) == 0)
                        break;
                    id++;
                }
                return id;
            }
            catch { return 1; }
            finally { EnsureClose(conn); }
        }

        #endregion

        #region Data Loading

        private void LoadDG()
        {
            try
            {
                _units.Clear();
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    "SELECT * FROM units WHERE IS_Deleted=0", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                EnsureClose(conn);

                foreach (DataRow row in dt.Rows)
                {
                    _units.Add(new UnitItem
                    {
                        Id = Convert.ToInt32(row["id"]),
                        Name = row["name"].ToString(),
                        UnitCode = row["UnitCode"] != DBNull.Value ? row["UnitCode"].ToString() : "",
                        DefaultInv = row["defaultInv"] != DBNull.Value ? Convert.ToInt32(row["defaultInv"]) : 0
                    });
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل البيانات: " + ex.Message, "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Button Events

        private void btnNew_Click(object sender, RoutedEventArgs e) => CLR();

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                DXMessageBox.Show("ادخل الوحدة", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus(); return;
            }

            try
            {
                EnsureOpen(conn);

                // التحقق من التكرار
                if (Code == -1)
                {
                    var chkAdapter = new SqlDataAdapter(
                        $"SELECT id FROM units WHERE name=N'{txtName.Text}' AND IS_Deleted=0", conn);
                    var chkDt = new DataTable();
                    chkAdapter.Fill(chkDt);
                    if (chkDt.Rows.Count > 0)
                    {
                        DXMessageBox.Show("الوحدة تم إدخالها مسبقاً", "",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtName.Focus();
                        EnsureClose(conn);
                        return;
                    }
                }

                int selectedIndex = cmbInvoice.SelectedIndex;

                // التحقق من الوحدة الافتراضية للفاتورة
                if (selectedIndex > 0)
                {
                    var defAdapter = new SqlDataAdapter(
                        $"SELECT id FROM units WHERE defaultInv={selectedIndex} AND IS_Deleted=0",
                        conn);
                    var defDt = new DataTable();
                    defAdapter.Fill(defDt);

                    if (defDt.Rows.Count > 0)
                    {
                        var change = DXMessageBox.Show(
                            $"هل تريد تغيير الوحدة الافتراضية لفاتورة {(cmbInvoice.SelectedItem as ComboBoxItem)?.Content}؟",
                            "", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (change == MessageBoxResult.Yes)
                            new SqlCommand($"UPDATE units SET defaultInv=0 WHERE id={defDt.Rows[0]["id"]}", conn)
                                .ExecuteNonQuery();
                        else
                            cmbInvoice.SelectedIndex = 0;
                    }
                }

                if (Code == -1)
                {
                    // إدراج
                    int newId = GetUnitId();
                    var cmd = new SqlCommand(
                        "INSERT INTO units(id,name,defaultInv,UnitId,IS_Deleted,UnitCode) VALUES(@id,@name,@defaultInv,@UnitId,0,@UnitCode)",
                        conn);
                    cmd.Parameters.AddWithValue("@id", newId);
                    cmd.Parameters.AddWithValue("@UnitId", newId);
                    cmd.Parameters.AddWithValue("@name", txtName.Text);
                    cmd.Parameters.AddWithValue("@UnitCode", txtUnitCode.Text.Trim());
                    cmd.Parameters.AddWithValue("@defaultInv", selectedIndex);
                    cmd.ExecuteNonQuery();
                    txtID.Text = newId.ToString();
                }
                else
                {
                    // تحديث
                    var cmd = new SqlCommand(
                        $"UPDATE units SET name=@name, defaultInv=@defaultInv, UnitCode=@UnitCode WHERE id={Code}",
                        conn);
                    cmd.Parameters.AddWithValue("@name", txtName.Text);
                    cmd.Parameters.AddWithValue("@UnitCode", txtUnitCode.Text.Trim());
                    cmd.Parameters.AddWithValue("@defaultInv", selectedIndex);
                    cmd.ExecuteNonQuery();
                }

                LoadDG();

                var savedMsg = new frmSavedMsg();
                if (Code != -1) savedMsg.lblSave.Text = "تم حفظ التعديلات بنجاح...";
                savedMsg.ShowDialog();

                if (savedMsg.Pressed == 1) CLR();
                else if (savedMsg.Pressed == 2)
                {
                    IsNew = false;
                    if (int.TryParse(txtID.Text, out int currentId))
                        Code = currentId;
                }
                else if (savedMsg.Pressed == 3) this.Close();

                txtName.Focus();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحفظ\nتفاصيل الخطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { EnsureClose(conn); }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (Code == -1)
            {
                DXMessageBox.Show("اختر وحدة ليتم حذفها", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning); return;
            }

            try
            {
                // التحقق من الارتباطات
                EnsureOpen(conn);
                var chkAdapter = new SqlDataAdapter(
                    $"SELECT ItemId FROM ItemUnits WHERE unit='{Code}'", conn);
                var chkDt = new DataTable();
                chkAdapter.Fill(chkDt);
                EnsureClose(conn);

                if (chkDt.Rows.Count > 0)
                {
                    DXMessageBox.Show("هذه الوحدة لها ارتباطات فرعية لا يمكن حذفها", "",
                                    MessageBoxButton.OK, MessageBoxImage.Warning); return;
                }

                var confirm = DXMessageBox.Show("هل تريد حذف هذه الوحدة؟", "تأكيد الحذف",
                                              MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                EnsureOpen(conn);
                new SqlCommand($"DELETE FROM units WHERE id={Code}", conn).ExecuteNonQuery();
                DXMessageBox.Show("تم الحذف", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadDG(); CLR();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحذف\nتفاصيل الخطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { EnsureClose(conn); }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void btnPrint_Click(object sender, RoutedEventArgs e) { }

        #endregion

        #region Navigation

        private void btnFirst_Click(object sender, RoutedEventArgs e) =>
            Navigate("SELECT TOP 1 * FROM units WHERE IS_Deleted=0 ORDER BY id ASC");

        private void btnPrevious_Click(object sender, RoutedEventArgs e) =>
            Navigate($"SELECT TOP 1 * FROM units WHERE IS_Deleted=0 AND id<{Code} ORDER BY id DESC");

        private void btnNext_Click(object sender, RoutedEventArgs e) =>
            Navigate($"SELECT TOP 1 * FROM units WHERE IS_Deleted=0 AND id>{Code} ORDER BY id ASC");

        private void btnLast_Click(object sender, RoutedEventArgs e) =>
            Navigate("SELECT TOP 1 * FROM units WHERE IS_Deleted=0 ORDER BY id DESC");

        public void Navigate(string sqlQuery)
        {
            try
            {
                EnsureOpen(conn);
                var reader = new SqlCommand(sqlQuery, conn).ExecuteReader();
                ReadData(reader);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التنقل: " + ex.Message, "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { EnsureClose(conn); }
        }

        private void ReadData(SqlDataReader reader)
        {
            if (!reader.HasRows) { reader.Close(); return; }

            reader.Read();
            IsNew = false;
            Code = Convert.ToInt32(reader["id"]);
            txtID.Text = Code.ToString();
            txtName.Text = reader["name"].ToString();
            cmbInvoice.SelectedIndex = Convert.ToInt32(reader["defaultInv"]);

            if (reader["UnitCode"] != DBNull.Value
                && !string.IsNullOrWhiteSpace(reader["UnitCode"].ToString()))
                txtUnitCode.Text = reader["UnitCode"].ToString();

            reader.Close();
        }

        #endregion

        #region DataGrid Events

        private void dgvUnits_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvUnits.SelectedItem is UnitItem item)
            {
                txtName.Text = item.Name;
                txtUnitCode.Text = item.UnitCode;
                cmbInvoice.SelectedIndex = item.DefaultInv;
                Code = item.Id;
                txtID.Text = Code.ToString();
                IsNew = false;
            }
        }

        #endregion

        #region TextBox Events

        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                btnSave_Click(null, null);
            }
        }

        #endregion

        #region Helpers

        private void EnsureOpen(SqlConnection c)
        { if (c.State != System.Data.ConnectionState.Open) c.Open(); }

        private void EnsureClose(SqlConnection c)
        { if (c.State != System.Data.ConnectionState.Closed) c.Close(); }

        #endregion

        #region Closing

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            EnsureClose(conn);
        }

        #endregion
    }

    public class UnitItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string UnitCode { get; set; }
        public int DefaultInv { get; set; }
    }
}