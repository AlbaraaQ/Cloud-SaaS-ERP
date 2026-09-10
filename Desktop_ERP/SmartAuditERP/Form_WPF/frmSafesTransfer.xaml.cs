using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Core;
using Button = System.Windows.Controls.Button;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmSafesTransfer : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;
        private int Code;
        private int RowIndex;
        private int SelectedId;
        public string SrchName;
        public int InvType;
        public int ProcType;
        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int PrintType;
        private int PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;
        private string InvGlobalID;
        private bool _isLoaded;
        private int ProcCode;

        // مصادر البيانات
        private ObservableCollection<TransferItem> _transferItems;
        private ObservableCollection<ReceiveItem> _receiveItems;
        private ObservableCollection<TransferSrchItem> _searchItems;

        #endregion

        #region Constructor

        public frmSafesTransfer()
        {
            InitializeComponent();

            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();


            Code = -1;
            RowIndex = -1;
            SelectedId = -1;
            SrchName = "";
            InvType = 8;
            ProcType = 1;
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp = true;
            PrintNo = 1;
            RptName = "";
            RptUrl = "";
            InvGlobalID = "";
            _isLoaded = false;
            ProcCode = -1;

            _transferItems = new ObservableCollection<TransferItem>();
            _receiveItems = new ObservableCollection<ReceiveItem>();
            _searchItems = new ObservableCollection<TransferSrchItem>();

            dgvItems.ItemsSource = _transferItems;
            dgvReceive.ItemsSource = _receiveItems;
            dgvSrch.ItemsSource = _searchItems;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                txtDate.DateTime = DateTime.Now;
                txtFromDate.DateTime = DateTime.Now;
                txtToDate.DateTime = DateTime.Now;

                LoadSafes();
                LoadNextNo();
                _isLoaded = true;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل النافذة: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Clear / Reset

        private void CLR()
        {
            _transferItems.Clear();
            LoadNextNo();
            Code = -1;

            cmbSafeFrom.SelectedIndex = -1;
            cmbSafeTo.SelectedIndex = -1;
            cmbStoreFrom2.SelectedIndex = -1;
            cmbStoreTo2.SelectedIndex = -1;
            txtNo2.Text = "";
            txtNetWithoutVAT.Text = "0";
            _receiveItems.Clear();
            ckReceiveAll.IsChecked = false;
            lblstatus.Text = "";
            InvGlobalID = "";
            ProcCode = -1;

            if (txtDate.DateTime == DateTime.MinValue || txtDate.DateTime == default(DateTime))
                txtDate.DateTime = DateTime.Now;
        }

        #endregion

        #region Data Loading

        private void LoadNextNo()
        {
            try
            {
                EnsureOpen(conn1);
                var cmd = new SqlCommand(
                    "SELECT ISNULL(MAX(id), 0) + 1 FROM SafesTransfer", conn1);
                var result = cmd.ExecuteScalar();
                txtNo.Text = result?.ToString() ?? "1";
            }
            catch
            {
                txtNo.Text = "1";
            }
            finally
            {
                EnsureClose(conn1);
            }
        }

        public void LoadSafes()
        {
            try
            {
                EnsureOpen(conn);
                string sql = "SELECT id, name FROM Safes WHERE status=1 AND IS_Deleted=0 ORDER BY id";
                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);

                // تعيين لكل الكومبو
                var dv1 = dt.DefaultView;
                cmbSafeFrom.ItemsSource = dv1;
                cmbSafeFrom.DisplayMemberPath = "name";
                cmbSafeFrom.SelectedValuePath = "id";
                cmbSafeFrom.SelectedIndex = -1;

                var dt2 = dt.Copy();
                cmbSafeTo.ItemsSource = dt2.DefaultView;
                cmbSafeTo.DisplayMemberPath = "name";
                cmbSafeTo.SelectedValuePath = "id";
                cmbSafeTo.SelectedIndex = -1;

                var dt3 = dt.Copy();
                cmbStoreFrom2.ItemsSource = dt3.DefaultView;
                cmbStoreFrom2.DisplayMemberPath = "name";
                cmbStoreFrom2.SelectedValuePath = "id";
                cmbStoreFrom2.SelectedIndex = -1;

                var dt4 = dt.Copy();
                cmbStoreTo2.ItemsSource = dt4.DefaultView;
                cmbStoreTo2.DisplayMemberPath = "name";
                cmbStoreTo2.SelectedValuePath = "id";
                cmbStoreTo2.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل المستودعات: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        private void LoadSearchGrid(string condition)
        {
            try
            {
                _searchItems.Clear();

                EnsureOpen(conn);
                string sql = $@"SELECT SafesTransfer.id, SafesTransfer.date,
                                       safe_from, safe_to, IsReceived
                                FROM SafesTransfer
                                WHERE branch = {MainClass.BranchNo}
                                  AND SafesTransfer.IS_Deleted = 0
                                  {condition}";

                var cmd = new SqlCommand(sql, conn);

                if (!string.IsNullOrWhiteSpace(condition)
                    && condition.Contains("@date"))
                {
                    cmd.Parameters.AddWithValue("@date1",
    txtFromDate.DateTime != default(DateTime)
        ? txtFromDate.DateTime.ToShortDateString()
        : DateTime.Now.ToShortDateString());
                    cmd.Parameters.AddWithValue("@date2",
                        (txtToDate.DateTime != default(DateTime)
                            ? txtToDate.DateTime
                            : DateTime.Now).AddHours(24).ToString());
                }

                var adapter = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);

                foreach (DataRow row in dt.Rows)
                {
                    _searchItems.Add(new TransferSrchItem
                    {
                        TransferId = Convert.ToInt32(row["id"]),
                        TransferDate = Convert.ToDateTime(row["date"]).ToShortDateString(),
                        SafeFromName = GetSafeName(Convert.ToInt32(row["safe_from"])),
                        SafeToName = GetSafeName(Convert.ToInt32(row["safe_to"])),
                        IsReceived = row["IsReceived"] != DBNull.Value && Convert.ToBoolean(row["IsReceived"])
                    });
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في البحث: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        #endregion

        #region Helpers

        private string GetSafeName(int safeId)
        {
            try
            {
                EnsureOpen(conn1);
                var adapter = new SqlDataAdapter(
                    $"SELECT name FROM safes WHERE id={safeId}", conn1);
                var dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch
            {
                return "";
            }
            finally
            {
                EnsureClose(conn1);
            }
        }

        private string GetItemName(int itemId)
        {
            try
            {
                EnsureOpen(conn);
                var adapter = new SqlDataAdapter(
                    $"SELECT name, nameEN FROM Items WHERE id={itemId}", conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    return MainClass.Language == "ar"
                        ? dt.Rows[0]["name"].ToString()
                        : dt.Rows[0]["nameEN"].ToString();
                }
                return "";
            }
            catch
            {
                return "";
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        private string GetUnitName(int unitId)
        {
            try
            {
                EnsureOpen(conn);
                var cmd = new SqlCommand($"SELECT name FROM units WHERE id={unitId}", conn);
                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        private int CalcStock(int itemId)
        {
            try
            {
                if (cmbSafeFrom.SelectedValue == null) return 0;
                int safeId = Convert.ToInt32(cmbSafeFrom.SelectedValue);

                EnsureOpen(conn);
                string sql = $@"SELECT ISNULL(SUM(CASE WHEN type IN (1,3,5) THEN qty ELSE -qty END), 0)
                                FROM Inv_Sub
                                WHERE safe_id = {safeId} AND ItemId = {itemId}";
                var cmd = new SqlCommand(sql, conn);
                var result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value
                    ? Convert.ToInt32(result)
                    : 0;
            }
            catch
            {
                return 0;
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        private double GetAvgCost(int itemId)
        {
            try
            {
                EnsureOpen(conn);
                string sql = $@"SELECT ISNULL(SUM(cost * qty) / NULLIF(SUM(qty), 0), 0)
                                FROM Inv_Sub
                                WHERE ItemId = {itemId} AND type IN (1,3,5)";
                var cmd = new SqlCommand(sql, conn);
                var result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value
                    ? Convert.ToDouble(result)
                    : 0.0;
            }
            catch
            {
                return 0.0;
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        private void CalcTotal()
        {
            double total = _transferItems.Sum(x => x.TotalCost);
            txtNetWithoutVAT.Text = Math.Round(total, 2).ToString("N2");
        }

        private void EnsureOpen(SqlConnection connection)
        {
            if (connection.State != ConnectionState.Open)
                connection.Open();
        }

        private void EnsureClose(SqlConnection connection)
        {
            if (connection.State != ConnectionState.Closed)
                connection.Close();
        }

        #endregion

        #region Button Events - Navigation

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            CLR();
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate($@"SELECT TOP 1 * FROM SafesTransfer
                        WHERE branch={MainClass.BranchNo} AND IS_Deleted=0
                        ORDER BY id ASC");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate($@"SELECT TOP 1 * FROM SafesTransfer
                        WHERE branch={MainClass.BranchNo} AND IS_Deleted=0
                          AND id < {Code}
                        ORDER BY id DESC");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate($@"SELECT TOP 1 * FROM SafesTransfer
                        WHERE branch={MainClass.BranchNo} AND IS_Deleted=0
                          AND id > {Code}
                        ORDER BY id ASC");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate($@"SELECT TOP 1 * FROM SafesTransfer
                        WHERE branch={MainClass.BranchNo} AND IS_Deleted=0
                        ORDER BY id DESC");
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Navigate / ReadData

        public void Navigate(string sqlQuery)
        {
            try
            {
                EnsureOpen(conn);
                var cmd = new SqlCommand(sqlQuery, conn);
                var reader = cmd.ExecuteReader();
                ReadData(reader);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في التنقل: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        private void ReadData(SqlDataReader reader)
        {
            if (!reader.HasRows)
            {
                reader.Close();
                return;
            }

            reader.Read();
            CLR();

            Code = Convert.ToInt32(reader["id"]);
            txtNo.Text = Code.ToString();

            if (reader["date"] != DBNull.Value)
                txtDate.DateTime = Convert.ToDateTime(reader["date"]);

            if (reader["safe_from"] != DBNull.Value)
                cmbSafeFrom.SelectedValue = reader["safe_from"];

            if (reader["safe_to"] != DBNull.Value)
                cmbSafeTo.SelectedValue = reader["safe_to"];

            reader.Close();

            // تحميل الأصناف
            LoadTransferItems();

            TabControl1.SelectedIndex = 0;
        }

        private void LoadTransferItems()
        {
            try
            {
                _transferItems.Clear();

                EnsureOpen(conn);
                string sql = $"SELECT * FROM SafesTransfer_Sub WHERE transfer_id={Code}";
                var adapter = new SqlDataAdapter(sql, conn);
                var dt = new DataTable();
                adapter.Fill(dt);
                EnsureClose(conn);

                int rowNo = 1;
                foreach (DataRow row in dt.Rows)
                {
                    int itemId = Convert.ToInt32(row["ItemId"]);
                    double unitEquality = 1.0;
                    string unitName = "";

                    if (row["unit"] != DBNull.Value)
                    {
                        unitName = GetUnitName(Convert.ToInt32(row["unit"]));
                        if (row["unitPercnt"] != DBNull.Value)
                            unitEquality = Convert.ToDouble(row["unitPercnt"]);
                    }

                    double primaryQty = row["value"] != DBNull.Value ? Convert.ToDouble(row["value"]) : 0;
                    double transferQty = unitEquality > 0 ? primaryQty / unitEquality : primaryQty;
                    double avgCost = row["AvrgCost"] != DBNull.Value ? Convert.ToDouble(row["AvrgCost"]) : 0;

                    _transferItems.Add(new TransferItem
                    {
                        RowNo = rowNo++,
                        ItemCode = GetItemCode(itemId),
                        ItemId = itemId,
                        ItemName = GetItemName(itemId),
                        StoreBalance = CalcStock(itemId),
                        AvgCost = avgCost,
                        UnitName = unitName,
                        TransferQty = transferQty,
                        TotalCost = transferQty * avgCost,
                        ReceivedQty = row["ReceivedValue"] != DBNull.Value ? Convert.ToDouble(row["ReceivedValue"]) : 0,
                        Diff = row["Diff"] != DBNull.Value ? Convert.ToDouble(row["Diff"]) : 0,
                        UnitEquality = unitEquality,
                        PrimaryQty = primaryQty,
                        UserName = GetEmpName(row["user_emp"] != DBNull.Value ? Convert.ToInt32(row["user_emp"]) : 0)
                    });
                }

                CalcTotal();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في تحميل الأصناف: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetItemCode(int itemId)
        {
            try
            {
                EnsureOpen(conn);
                var cmd = new SqlCommand($"SELECT Code FROM Items WHERE id={itemId}", conn);
                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        private string GetEmpName(int empId)
        {
            try
            {
                EnsureOpen(conn);
                var cmd = new SqlCommand($"SELECT name FROM Employees WHERE id={empId}", conn);
                var result = cmd.ExecuteScalar();
                return result?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        #endregion

        #region Save Operations

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (TabControl1.SelectedIndex == 1)
                SaveReceive();
            else
                SaveSend();
        }

        private void SaveSend()
        {
            // ── التحقق ──
            if (cmbSafeFrom.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار المستودع (من)", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbSafeFrom.Focus();
                return;
            }

            if (cmbSafeTo.SelectedIndex == -1)
            {
                DXMessageBox.Show("يجب اختيار المستودع (إلى)", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbSafeTo.Focus();
                return;
            }

            if (cmbSafeFrom.SelectedValue?.ToString() == cmbSafeTo.SelectedValue?.ToString())
            {
                DXMessageBox.Show("لا يمكن التحويل لنفس المستودع", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbSafeTo.Focus();
                return;
            }

            var validItems = _transferItems.Where(x => x.ItemId > 0).ToList();
            if (validItems.Count == 0)
            {
                DXMessageBox.Show("لم تدخل أصناف للتحويل", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = DXMessageBox.Show(
                "هل أنت متأكد من حفظ المناقلة كمسودة؟\nلاعتماد المناقلة يجب اعتمادها بعد الحفظ.",
                "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.No) return;

            try
            {
                EnsureOpen(conn);
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    int safeFromId = Convert.ToInt32(cmbSafeFrom.SelectedValue);
                    int safeToId = Convert.ToInt32(cmbSafeTo.SelectedValue);
                    DateTime transferDate = txtDate.DateTime != default(DateTime)
    ? txtDate.DateTime
    : DateTime.Now;

                    if (Code != -1)
                    {
                        // تحقق من الإرسال السابق
                        var chkCmd = new SqlCommand(
                            $"SELECT IsSent FROM SafesTransfer WHERE id={Code}", conn, transaction);
                        var chkResult = chkCmd.ExecuteScalar();
                        if (chkResult != null && Convert.ToBoolean(chkResult))
                        {
                            DXMessageBox.Show("المناقلة تم إرسالها سابقاً", "",
                                            MessageBoxButton.OK, MessageBoxImage.Warning);
                            transaction.Rollback();
                            return;
                        }

                        // تحديث
                        var updateCmd = new SqlCommand(
                            $@"UPDATE SafesTransfer SET date=@date, safe_from={safeFromId},
                               safe_to={safeToId} WHERE id={Code}",
                            conn, transaction);
                        updateCmd.Parameters.AddWithValue("@date", transferDate.ToShortDateString());
                        updateCmd.ExecuteNonQuery();

                        new SqlCommand(
                            $"DELETE FROM SafesTransfer_Sub WHERE transfer_id={Code}",
                            conn, transaction).ExecuteNonQuery();
                    }
                    else
                    {
                        // إدراج جديد
                        if (!int.TryParse(txtNo.Text, out int newNo))
                            newNo = 1;

                        var insertCmd = new SqlCommand(
                            $@"INSERT INTO SafesTransfer(id,date,safe_from,safe_to,branch,IsSent,IsReceived,IS_Deleted)
                               VALUES({newNo},@date,{safeFromId},{safeToId},{MainClass.BranchNo},0,0,0)",
                            conn, transaction);
                        insertCmd.Parameters.AddWithValue("@date", transferDate);
                        insertCmd.ExecuteNonQuery();
                        Code = newNo;
                    }

                    // إدراج الأصناف
                    foreach (var item in validItems)
                    {
                        var subCmd = new SqlCommand(
                            @"INSERT INTO SafesTransfer_Sub
                              (transfer_id, ItemId, unit, unitPercnt, value, user_emp,
                               ReceivedValue, Diff, IsReceived, AvrgCost)
                              VALUES
                              (@transfer_id,@ItemId,@unit,@unitPercnt,@value,@user_emp,
                               0,@Diff,0,@AvrgCost)",
                            conn, transaction);

                        subCmd.Parameters.AddWithValue("@transfer_id", Code);
                        subCmd.Parameters.AddWithValue("@ItemId", item.ItemId);
                        subCmd.Parameters.AddWithValue("@value", item.PrimaryQty);
                        subCmd.Parameters.AddWithValue("@user_emp", MainClass.EmpNo);
                        subCmd.Parameters.AddWithValue("@unit", DBNull.Value);
                        subCmd.Parameters.AddWithValue("@unitPercnt", item.UnitEquality);
                        subCmd.Parameters.AddWithValue("@Diff", item.Diff);
                        subCmd.Parameters.AddWithValue("@AvrgCost", item.AvgCost);
                        subCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    var approve = DXMessageBox.Show(
                        "تم حفظ المناقلة كمسودة. هل تريد اعتماد الإرسال؟",
                        "اعتماد", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (approve == MessageBoxResult.Yes)
                        SaveAndApprove(2);

                    CLR();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    DXMessageBox.Show("خطأ أثناء الحفظ\nتفاصيل الخطأ: " + ex.Message,
                                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في الاتصال: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        private void SaveReceive()
        {
            try
            {
                EnsureOpen(conn1);
                var chkAdapter = new SqlDataAdapter(
                    $"SELECT IsSent, IsReceived FROM SafesTransfer WHERE id={Code}", conn1);
                var chkDt = new DataTable();
                chkAdapter.Fill(chkDt);
                EnsureClose(conn1);

                if (chkDt.Rows.Count == 0)
                {
                    DXMessageBox.Show("لا يوجد مناقلة مرسلة بهذا الرقم", "",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Convert.ToBoolean(chkDt.Rows[0]["IsReceived"]))
                {
                    DXMessageBox.Show("المناقلة تم اعتماد استلامها سابقاً", "",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!Convert.ToBoolean(chkDt.Rows[0]["IsSent"]))
                {
                    DXMessageBox.Show("المناقلة غير جاهزة للاستلام", "",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_receiveItems.Count == 0)
                {
                    DXMessageBox.Show("لا يوجد مناقلة للاستلام", "",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // التحقق من الكميات
                foreach (var item in _receiveItems)
                {
                    if (item.Diff < 0)
                    {
                        DXMessageBox.Show("لا يمكن استلام كمية أكبر من الكمية المرسلة", "",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                var confirm = DXMessageBox.Show("هل أنت متأكد من استلام كمية المناقلة؟",
                                              "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm == MessageBoxResult.No) return;

                EnsureOpen(conn);
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    foreach (var item in _receiveItems)
                    {
                        if (!item.IsReceived) continue;

                        var updateCmd = new SqlCommand(
                            $@"UPDATE SafesTransfer_Sub
                               SET ReceivedValue=@ReceivedValue, Diff=@Diff
                               WHERE transfer_id={Code} AND ItemId={item.ItemId}",
                            conn, transaction);
                        updateCmd.Parameters.AddWithValue("@ReceivedValue", item.ReceivedQty);
                        updateCmd.Parameters.AddWithValue("@Diff", item.Diff);
                        updateCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    var approve = DXMessageBox.Show(
                        "تم الاستلام كمسودة. هل تريد اعتماد الاستلام؟",
                        "اعتماد", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (approve == MessageBoxResult.Yes)
                        SaveAndApprove(1);

                    _receiveItems.Clear();
                    CLR();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    DXMessageBox.Show("خطأ أثناء الاستلام\nتفاصيل الخطأ: " + ex.Message,
                                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        private void SaveAndApprove(int proc)
        {
            try
            {
                EnsureOpen(conn);

                if (proc == 2)
                {
                    // اعتماد الإرسال
                    new SqlCommand(
                        $"UPDATE SafesTransfer SET IsSent=1 WHERE id={Code}",
                        conn).ExecuteNonQuery();
                    DXMessageBox.Show("تم إرسال الكمية بنجاح", "",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // اعتماد الاستلام
                    new SqlCommand(
                        $"UPDATE SafesTransfer SET IsReceived=1 WHERE id={Code}",
                        conn).ExecuteNonQuery();
                    DXMessageBox.Show("تم استلام الكمية بنجاح", "",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في الاعتماد: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureClose(conn);
            }
        }

        #endregion

        #region Delete

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (Code == -1)
            {
                DXMessageBox.Show("اختر إذناً ليتم حذفه", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // التحقق من الاستلام
                EnsureOpen(conn1);
                var chkAdapter = new SqlDataAdapter(
                    $"SELECT id FROM SafesTransfer WHERE IsReceived=1 AND id={Code}", conn1);
                var chkDt = new DataTable();
                chkAdapter.Fill(chkDt);
                EnsureClose(conn1);

                if (chkDt.Rows.Count > 0)
                {
                    DXMessageBox.Show("لا يمكن حذف مناقلة تم استلامها", "",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var confirm = DXMessageBox.Show("هل أنت متأكد من حذف المناقلة؟",
                                              "تأكيد الحذف", MessageBoxButton.YesNo,
                                              MessageBoxImage.Question);
                if (confirm == MessageBoxResult.No) return;

                EnsureOpen(conn);
                new SqlCommand(
                    $"UPDATE SafesTransfer SET IS_Deleted=1 WHERE id={Code}",
                    conn).ExecuteNonQuery();

                DXMessageBox.Show("تم الحذف بنجاح", "",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                _searchItems.Clear();
                CLR();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ أثناء الحذف\nتفاصيل الخطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                EnsureClose(conn);
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
            string condition = "";

            if (!string.IsNullOrWhiteSpace(txtSrchNo.Text))
                condition += $" AND SafesTransfer.id = {txtSrchNo.Text}";

            if (chkAllPeriod.IsChecked != true)
                condition += " AND date >= @date1 AND date <= @date2";

            LoadSearchGrid(condition);
        }

        #endregion

        #region Show Transfer

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNo2.Text))
            {
                DXMessageBox.Show("أدخل رقم المناقلة", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtNo2.Text, out int transferNo)) return;

            _receiveItems.Clear();
            Code = transferNo;
            ShowTransfer(0);
        }

        private void ShowTransfer(int all)
        {
            try
            {
                EnsureOpen(conn);
                string sql = $@"SELECT * FROM SafesTransfer
                                WHERE branch={MainClass.BranchNo} AND id={Code}";
                var headerAdapter = new SqlDataAdapter(sql, conn);
                var headerDt = new DataTable();
                headerAdapter.Fill(headerDt);

                if (headerDt.Rows.Count != 1)
                {
                    EnsureClose(conn);
                    return;
                }

                var headerRow = headerDt.Rows[0];
                txtDate3.Text = headerRow["date"] != DBNull.Value
                    ? Convert.ToDateTime(headerRow["date"]).ToShortDateString()
                    : "";

                if (headerRow["safe_from"] != DBNull.Value)
                    cmbStoreFrom2.SelectedValue = headerRow["safe_from"];

                if (headerRow["safe_to"] != DBNull.Value)
                    cmbStoreTo2.SelectedValue = headerRow["safe_to"];

                lblstatus.Text = headerRow["IsReceived"] != DBNull.Value
                                 && Convert.ToBoolean(headerRow["IsReceived"])
                    ? "معتمد"
                    : "غير معتمد";

                // تحميل الأصناف
                string subSql = $"SELECT * FROM SafesTransfer_Sub WHERE transfer_id={Code}";
                var subAdapter = new SqlDataAdapter(subSql, conn);
                var subDt = new DataTable();
                subAdapter.Fill(subDt);
                EnsureClose(conn);

                _receiveItems.Clear();

                string statusText = "غير معتمد";

                foreach (DataRow row in subDt.Rows)
                {
                    int itemId = Convert.ToInt32(row["ItemId"]);
                    double unitEquality = 1.0;
                    string unitName = "";

                    if (row["unit"] != DBNull.Value)
                    {
                        unitName = GetUnitName(Convert.ToInt32(row["unit"]));
                        if (row["unitPercnt"] != DBNull.Value)
                            unitEquality = Convert.ToDouble(row["unitPercnt"]);
                    }

                    double primaryQty = row["value"] != DBNull.Value ? Convert.ToDouble(row["value"]) : 0;
                    double sentQty = unitEquality > 0 ? primaryQty / unitEquality : primaryQty;

                    bool isReceived = row["IsReceived"] != DBNull.Value && Convert.ToBoolean(row["IsReceived"]);
                    if (isReceived) statusText = "معتمد";

                    double receivedQty;
                    double diff;

                    switch (all)
                    {
                        case 1:
                            receivedQty = sentQty;
                            diff = 0;
                            break;
                        default:
                            receivedQty = row["ReceivedValue"] != DBNull.Value
                                ? Convert.ToDouble(row["ReceivedValue"])
                                : 0;
                            diff = row["Diff"] != DBNull.Value
                                ? Convert.ToDouble(row["Diff"])
                                : 0;
                            break;
                    }

                    double avgCost = row["AvrgCost"] != DBNull.Value
                        ? Convert.ToDouble(row["AvrgCost"])
                        : 0;

                    _receiveItems.Add(new ReceiveItem
                    {
                        ItemId = itemId,
                        ItemName = GetItemName(itemId),
                        UnitName = unitName,
                        SentQty = sentQty,
                        AvgCost = avgCost,
                        ReceivedQty = receivedQty,
                        Diff = diff,
                        StatusText = statusText,
                        IsReceived = isReceived,
                        UnitEquality = unitEquality,
                        PrimaryQty = primaryQty,
                        AvgCost2 = avgCost,
                        UserName = row["user_emp"] != DBNull.Value
                            ? GetEmpName(Convert.ToInt32(row["user_emp"]))
                            : ""
                    });
                }

                if (subDt.Rows.Count > 0 && subDt.Rows[0]["user_emp"] != DBNull.Value)
                    lblEmp1.Text = GetEmpName(Convert.ToInt32(subDt.Rows[0]["user_emp"]));
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ في عرض المناقلة\nتفاصيل الخطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                EnsureClose(conn);
            }
        }

        #endregion

        #region DataGrid Events

        private void dgvSrch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dgvSrch.SelectedItem is TransferSrchItem selected)
            {
                CLR();
                Code = selected.TransferId;
                Navigate($@"SELECT * FROM SafesTransfer
                            WHERE branch={MainClass.BranchNo} AND id={Code}");
                TabControl1.SelectedIndex = 0;
            }
        }

        private void BtnReceiveFromGrid_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TransferSrchItem item)
            {
                CLR();
                TabControl1.SelectedIndex = 1;
                Code = item.TransferId;
                txtNo2.Text = Code.ToString();
                ShowTransfer(0);
            }
        }

        private void BtnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TransferItem item)
            {
                var confirm = DXMessageBox.Show("هل تريد حذف البند؟", "",
                                              MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm == MessageBoxResult.Yes)
                {
                    _transferItems.Remove(item);
                    // إعادة ترقيم الصفوف
                    int n = 1;
                    foreach (var t in _transferItems)
                        t.RowNo = n++;
                    CalcTotal();
                }
            }
        }

        private void dgvItems_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Row.Item is TransferItem item && e.EditAction == DataGridEditAction.Commit)
            {
                // إعادة حساب الإجمالي بعد التعديل
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    item.TotalCost = item.TransferQty * item.AvgCost;
                    item.PrimaryQty = item.TransferQty * item.UnitEquality;
                    CalcTotal();
                }));
            }
        }

        private void dgvItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvItems.SelectedIndex >= 0)
                RowIndex = dgvItems.SelectedIndex;
        }

        private void dgvItems_CurrentCellChanged(object sender, EventArgs e)
        {
            if (dgvItems.CurrentCell.Item is TransferItem)
                RowIndex = dgvItems.Items.IndexOf(dgvItems.CurrentCell.Item);
        }

        private void dgvReceive_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Row.Item is ReceiveItem item && e.EditAction == DataGridEditAction.Commit)
            {
                // إعادة حساب الفرق بعد تغيير الكمية المستلمة
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (item.ReceivedQty > item.SentQty || item.ReceivedQty < 0)
                    {
                        DXMessageBox.Show("أدخل الكمية المستلمة بشكل صحيح", "",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        item.ReceivedQty = item.SentQty;
                    }
                    item.Diff = item.SentQty - item.ReceivedQty;
                }));
            }
        }

        #endregion

        #region CheckBox / Other Events

        private void chkAllPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool allChecked = chkAllPeriod.IsChecked == true;
            txtFromDate.IsEnabled = !allChecked;
            txtToDate.IsEnabled = !allChecked;
        }

        private void ckReceiveAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtNo2.Text)
                && int.TryParse(txtNo2.Text, out int transferNo))
            {
                _receiveItems.Clear();
                Code = transferNo;
                ShowTransfer(ckReceiveAll.IsChecked == true ? 1 : 0);
            }
        }

        private void txtSrchNo_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        #endregion

        #region Execute / Receive Approve

        private void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            if (_transferItems.Count == 0)
            {
                DXMessageBox.Show("لا يوجد مناقلة للإرسال", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                EnsureOpen(conn1);
                var chkAdapter = new SqlDataAdapter(
                    $"SELECT IsSent, IsReceived FROM SafesTransfer WHERE id={Code}", conn1);
                var chkDt = new DataTable();
                chkAdapter.Fill(chkDt);
                EnsureClose(conn1);

                if (chkDt.Rows.Count > 0)
                {
                    if (Convert.ToBoolean(chkDt.Rows[0]["IsSent"]))
                    {
                        DXMessageBox.Show("المناقلة تم إرسالها سابقاً", "",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var confirm = DXMessageBox.Show("هل أنت متأكد من اعتماد إرسال كمية المناقلة؟",
                                                  "تأكيد", MessageBoxButton.YesNo,
                                                  MessageBoxImage.Question);
                    if (confirm == MessageBoxResult.Yes)
                        SaveAndApprove(2);
                }
                else
                {
                    DXMessageBox.Show("يجب حفظ المناقلة أولاً", "",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnReceiveQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (_receiveItems.Count == 0)
            {
                DXMessageBox.Show("لا يوجد مناقلة للاستلام", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                EnsureOpen(conn1);
                var chkAdapter = new SqlDataAdapter(
                    $"SELECT IsSent, IsReceived FROM SafesTransfer WHERE id={Code}", conn1);
                var chkDt = new DataTable();
                chkAdapter.Fill(chkDt);
                EnsureClose(conn1);

                if (chkDt.Rows.Count > 0)
                {
                    if (Convert.ToBoolean(chkDt.Rows[0]["IsReceived"]))
                    {
                        DXMessageBox.Show("المناقلة تم اعتماد استلامها سابقاً", "",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var confirm = DXMessageBox.Show("هل أنت متأكد من اعتماد استلام كمية المناقلة؟",
                                                  "تأكيد", MessageBoxButton.YesNo,
                                                  MessageBoxImage.Question);
                    if (confirm == MessageBoxResult.Yes)
                        SaveAndApprove(1);
                }
                else
                {
                    DXMessageBox.Show("لا يوجد مناقلة مرسلة بهذا الرقم", "",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show("خطأ: " + ex.Message,
                                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Print

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintReport(1);
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            PrintReport(2);
        }

        private void PrintReport(int printMode)
        {
            RptUrl = MainClass.ReportsPath;
            RptName = TabControl1.SelectedIndex == 0
                ? "rptSafeTransfer.repx"
                : "rptReceiveTransfer.repx";

            var itemsCount = TabControl1.SelectedIndex == 0
                ? _transferItems.Count
                : _receiveItems.Count;

            if (itemsCount == 0)
            {
                DXMessageBox.Show("لا توجد بيانات للطباعة", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(RptUrl))
            {
                DXMessageBox.Show("يجب تحديد مسار التقرير", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string fullPath = System.IO.Path.Combine(RptUrl, RptName);
            if (!Directory.Exists(RptUrl) || !File.Exists(fullPath))
            {
                DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            //احتمال
            // تنفيذ الطباعة عبر XtraReport
            // XtraReport report = XtraReport.FromFile(fullPath);
            // report.DataSource = BuildReportData();
            // if (printMode == 1) report.Print();
            // else report.ShowPreviewDialog();
        }

        #endregion

        #region Closing

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            EnsureClose(conn);
            EnsureClose(conn1);
        }

        #endregion
    }
}