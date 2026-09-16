using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmOffers : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;

        private int Code = -1;
        private int Emp_No = -1;
        private int RowIndex = -1;
        private string SrchName = "";

        private ObservableCollection<OfferItemRow> _itemRows;
        private ObservableCollection<OfferSrchRow> _srchRows;

        #endregion

        #region Constructor

        public frmOffers()
        {
            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();

            _itemRows = new ObservableCollection<OfferItemRow>();
            _srchRows = new ObservableCollection<OfferSrchRow>();

            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmOffers_Load(object sender, RoutedEventArgs e)
        {
            txtDate.SelectedDate = DateTime.Today;
            txtOfferStartDate.SelectedDate = DateTime.Today;
            txtOfferExpireDate.SelectedDate = DateTime.Today;
            txtOfferStartTime.Text = "00:00";
            txtOfferExpireTime.Text = "23:59";

            dgvItems.ItemsSource = _itemRows;
            dgvOfferSrch.ItemsSource = _srchRows;

            LoadAccount();
            LoadOfferNo();
            LoadDG("");
            LoadClients();
            LoadCatagories();

            cmbInvoices.SelectedIndex = 0;

            WindowState = MainClass.Window_State == WindowState.Maximized
                ? WindowState.Maximized
                : WindowState.Normal;
        }

        #endregion

        #region Load Data

        private void LoadAccount()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select Code,AName from Accounts_Index where type=2", conn1);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbOfferAccount.ItemsSource = dt.DefaultView;
                cmbOfferAccount.SelectedIndex = -1;
            }
            catch { }
        }

        private void LoadClients()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id,Name from Customers where type=1 and IS_Deleted=0", conn1);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbClients.ItemsSource = dt.DefaultView;
                cmbClients.SelectedIndex = -1;
            }
            catch { }
        }

        public void LoadCatagories()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id,name from ItemsCategory where type=2 order by id", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbItemCatag.ItemsSource = dt.DefaultView;
                cmbItemCatag.SelectedIndex = -1;
            }
            catch { }
        }

        private void LoadOfferNo()
        {
            try
            {
                if (conn1.State != System.Data.ConnectionState.Open)
                    conn1.Open();

                object result = new SqlCommand("select max(OfferId) from Offer", conn1).ExecuteScalar();
                double maxId = 0;
                double.TryParse(result?.ToString(), out maxId);
                txtOfferNo.Text = ((int)(maxId + 1)).ToString();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void LoadDG(string condition)
        {
            try
            {
                _srchRows.Clear();

                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select * from Offer where ISDeleted=0 {condition}", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    _srchRows.Add(new OfferSrchRow
                    {
                        Column17 = dt.Rows[i]["OfferID"].ToString(),
                        Column11 = dt.Rows[i]["OfferName"].ToString(),
                        Column2 = dt.Rows[i]["OfferType"].ToString(),
                        Column19 = dt.Rows[i]["OfferStartDate"] != DBNull.Value ? Convert.ToDateTime(dt.Rows[i]["OfferStartDate"]).ToString("dd/MM/yyyy HH:mm") : "",
                        Column20 = dt.Rows[i]["OfferExpire"] != DBNull.Value ? Convert.ToDateTime(dt.Rows[i]["OfferExpire"]).ToString("dd/MM/yyyy HH:mm") : "",
                        Column14 = dt.Rows[i]["OfferValue"].ToString(),
                        Column15 = dt.Rows[i]["OfferPercentage"].ToString(),
                        Column13 = dt.Rows[i]["OfferValueTarget"].ToString(),
                        Column16 = Common.GetEmpName(Convert.ToInt32(dt.Rows[i]["EmpId"]))
                    });
                }

                dgvOfferSrch.UnselectAll();
                lblSrchCount.Text = $"عدد السجلات: {_srchRows.Count:N0}";
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void LoadOfferItems(int offerId)
        {
            try
            {
                _itemRows.Clear();

                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select * from OfferItems where OfferId={offerId}", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    int itemId = Convert.ToInt32(dt.Rows[i]["ItemId"]);

                    double offerNatural = 0;
                    double.TryParse(dt.Rows[i]["OfferNatural"].ToString(), out offerNatural);

                    if (offerNatural == 1.0) rbPrice.IsChecked = true;
                    else if (offerNatural == 2.0) rbQnty.IsChecked = true;
                    else rbService.IsChecked = true;

                    bool isGrouped = Convert.ToBoolean(dt.Rows[i]["IsGroupedItems"]);
                    if (isGrouped) rbSetOfItemsOffer.IsChecked = true;
                    else rbOneItem.IsChecked = true;

                    _itemRows.Add(new OfferItemRow
                    {
                        AccRowIndex = i + 1,
                        ColCode = Common.GetItemCode(itemId),
                        ColId = itemId.ToString(),
                        ColName = Common.GetItemName(itemId),
                        ColTarget = dt.Rows[i]["OfferTargetQnty"].ToString(),
                        ColPriceOrig = "",
                        ColUnit = Common.GetUnitName(Convert.ToInt32(dt.Rows[i]["unit"])),
                        ColPrice = dt.Rows[i]["OfferItemPrice"].ToString(),
                        ColTotal = dt.Rows[i]["OfferTotalPrice"].ToString(),
                        ColDiscount = dt.Rows[i]["OfferItemValue"].ToString(),
                        ColDiscountPct = dt.Rows[i]["OfferItemPercentage"].ToString()
                    });
                }
            }
            catch { }
        }

        #endregion

        #region Clear / New

        private void CLR()
        {
            txtDate.SelectedDate = DateTime.Today;
            txtOfferStartDate.SelectedDate = DateTime.Today;
            txtOfferExpireDate.SelectedDate = DateTime.Today;
            txtOfferStartTime.Text = "00:00";
            txtOfferExpireTime.Text = "23:59";
            txtOfferTarget.Text = "0";
            txtOfferValue.Text = "0";
            txtOfferPercentage.Text = "0";
            txtOfferName.Text = "";
            cmbInvoices.SelectedIndex = 0;
            cmbClients.SelectedIndex = -1;
            cmbItemCatag.SelectedIndex = -1;
            txtSrchName.Text = "";
            Code = -1;

            _itemRows.Clear();
            LoadAccount();
            LoadOfferNo();
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            CLR();
        }

        #endregion

        #region Save

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtOfferName.Text))
                {
                    DXMessageBox.Show("يجب إدخال اسم العرض", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtOfferName.Focus();
                    return;
                }

                double pct = 0, val = 0;
                double.TryParse(txtOfferPercentage.Text, out pct);
                double.TryParse(txtOfferValue.Text, out val);

                if (pct > 0 && val > 0)
                {
                    DXMessageBox.Show("يجب تحديد نوع العرض، قيمة أو نسبة", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int selectedTabIndex = TabControl1.SelectedIndex;

                if (selectedTabIndex == 2 && cmbClients.SelectedIndex == -1)
                {
                    DXMessageBox.Show("يجب تحديد العميل", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbClients.Focus();
                    return;
                }

                if (rbOneCatagory.IsChecked == true && cmbItemCatag.SelectedIndex == -1)
                {
                    DXMessageBox.Show("يجب تحديد المجموعة", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    cmbItemCatag.Focus();
                    return;
                }

                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                int accountCode = cmbOfferAccount.SelectedIndex > -1
                    ? Convert.ToInt32(cmbOfferAccount.SelectedValue)
                    : -1;

                int invoiceId = cmbInvoices.SelectedIndex > 0
                    ? cmbInvoices.SelectedIndex + 1
                    : 0;

                int offerType = selectedTabIndex + 1;

                DateTime startDateTime = BuildDateTime(
                    txtOfferStartDate.SelectedDate ?? DateTime.Today,
                    txtOfferStartTime.Text);

                DateTime expireDateTime = BuildDateTime(
                    txtOfferExpireDate.SelectedDate ?? DateTime.Today,
                    txtOfferExpireTime.Text);

                string sql = Code == -1
                    ? "INSERT INTO [dbo].[Offer]([OfferID],[OfferType],[OfferStartDate],[OfferExpire],[OfferName],[OfferDate],[OfferAccount],[InvoiceID],[OfferValue],[OfferPercentage],[OfferValueTarget],[OfferQntyTarget],[EmpId],[ISDeleted]) VALUES(@OfferID,@OfferType,@OfferStartDate,@OfferExpire,@OfferName,@OfferDate,@OfferAccount,@InvoiceID,@OfferValue,@OfferPercentage,@OfferValueTarget,@OfferQntyTarget,@EmpId,@ISDeleted)"
                    : "UPDATE [dbo].[Offer] SET [OfferType]=@OfferType,[OfferStartDate]=@OfferStartDate,[OfferExpire]=@OfferExpire,[OfferName]=@OfferName,[OfferDate]=@OfferDate,[OfferAccount]=@OfferAccount,[InvoiceID]=@InvoiceID,[OfferValue]=@OfferValue,[OfferPercentage]=@OfferPercentage,[OfferValueTarget]=@OfferValueTarget,[OfferQntyTarget]=@OfferQntyTarget,[EmpId]=@EmpId WHERE [OfferID]=@OfferID";

                SqlCommand cmd = new SqlCommand(sql, conn);
                double offerNo = 0;
                double.TryParse(txtOfferNo.Text, out offerNo);

                cmd.Parameters.Add("@OfferID", SqlDbType.Int).Value = (int)offerNo;
                cmd.Parameters.Add("@OfferType", SqlDbType.Int).Value = offerType;
                cmd.Parameters.Add("@OfferStartDate", SqlDbType.DateTime).Value = startDateTime;
                cmd.Parameters.Add("@OfferExpire", SqlDbType.DateTime).Value = expireDateTime;
                cmd.Parameters.Add("@OfferName", SqlDbType.NVarChar).Value = txtOfferName.Text;
                cmd.Parameters.Add("@OfferDate", SqlDbType.DateTime).Value = DateTime.Now;
                cmd.Parameters.Add("@OfferAccount", SqlDbType.NVarChar).Value = accountCode;
                cmd.Parameters.Add("@InvoiceID", SqlDbType.NVarChar).Value = invoiceId;

                double offerValue = 0, offerPct = 0, offerTarget = 0;
                double.TryParse(txtOfferValue.Text, out offerValue);
                double.TryParse(txtOfferPercentage.Text, out offerPct);
                double.TryParse(txtOfferTarget.Text, out offerTarget);

                cmd.Parameters.Add("@OfferValue", SqlDbType.Float).Value = offerValue;
                cmd.Parameters.Add("@OfferPercentage", SqlDbType.Float).Value = offerPct;
                cmd.Parameters.Add("@OfferValueTarget", SqlDbType.Float).Value = offerTarget;
                cmd.Parameters.Add("@OfferQntyTarget", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@EmpId", SqlDbType.Int).Value = MainClass.EmpNo;
                cmd.Parameters.Add("@ISDeleted", SqlDbType.Bit).Value = 0;
                cmd.ExecuteNonQuery();

                if (selectedTabIndex == 1) ItemsOffer();
                if (selectedTabIndex == 2) ClientOffer();

                frmSavedMsg savedMsg = new frmSavedMsg();
                if (Code != -1) savedMsg.lblSave.Text = "تم حفظ التعديلات بنجاح...";
                savedMsg.ShowDialog();

                if (savedMsg.Pressed == 1)
                    CLR();
                else if (savedMsg.Pressed == 3)
                {
                    CLR();
                    Close();
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                    conn.Close();
            }
        }

        private void ItemsOffer()
        {
            if (Code != -1)
            {
                double offerNo = 0;
                double.TryParse(txtOfferNo.Text, out offerNo);
                new SqlCommand($"delete from OfferItems where OfferId={(int)offerNo}", conn).ExecuteNonQuery();
            }

            string sql = @"INSERT INTO [dbo].[OfferItems]
                ([OfferId],[CatatogryID],[ItemID],[OfferNatural],[IsGroupedItems],[unit],
                [OfferTargetQnty],[OfferItemTargetQnty],[OfferItemPrice],[OfferTotalPrice],
                [OfferItemValue],[OfferItemPercentage],[OfferItemStock])
                VALUES(@OfferId,@CatatogryID,@ItemID,@OfferNatural,@IsGroupedItems,@unit,
                @OfferTargetQnty,@OfferItemTargetQnty,@OfferItemPrice,@OfferTotalPrice,
                @OfferItemValue,@OfferItemPercentage,@OfferItemStock)";

            double offerNum = 0;
            double.TryParse(txtOfferNo.Text, out offerNum);

            foreach (OfferItemRow item in _itemRows)
            {
                if (string.IsNullOrEmpty(item.ColId) || item.ColId == "0") continue;

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@OfferId", SqlDbType.Int).Value = (int)offerNum;
                cmd.Parameters.Add("@CatatogryID", SqlDbType.Int).Value = 0;
                cmd.Parameters.Add("@ItemID", SqlDbType.Int).Value = ToIntSafe(item.ColId);
                cmd.Parameters.Add("@OfferNatural", SqlDbType.Int).Value = rbPrice.IsChecked == true ? 1 : rbQnty.IsChecked == true ? 2 : 3;
                cmd.Parameters.Add("@unit", SqlDbType.Int).Value = Common.GetUnitID(item.ColUnit ?? "");
                cmd.Parameters.Add("@IsGroupedItems", SqlDbType.Bit).Value = rbOneItem.IsChecked == true ? 0 : 1;
                cmd.Parameters.Add("@OfferTargetQnty", SqlDbType.Float).Value = ToDoubleSafe(item.ColTarget);
                cmd.Parameters.Add("@OfferItemTargetQnty", SqlDbType.Float).Value = ToDoubleSafe(item.ColTarget);
                cmd.Parameters.Add("@OfferItemPrice", SqlDbType.Float).Value = ToDoubleSafe(item.ColPrice);
                cmd.Parameters.Add("@OfferTotalPrice", SqlDbType.Float).Value = ToDoubleSafe(item.ColTotal);
                cmd.Parameters.Add("@OfferItemValue", SqlDbType.Float).Value = ToDoubleSafe(item.ColDiscount);
                cmd.Parameters.Add("@OfferItemPercentage", SqlDbType.Float).Value = ToDoubleSafe(item.ColDiscountPct);
                cmd.Parameters.Add("@OfferItemStock", SqlDbType.Float).Value = ToDoubleSafe(item.ColTarget);
                cmd.ExecuteNonQuery();
            }
        }

        private void ClientOffer()
        {
            try
            {
                double offerNum = 0;
                double.TryParse(txtOfferNo.Text, out offerNum);

                if (Code != -1)
                    new SqlCommand($"delete from OfferForClient where OfferId={(int)offerNum}", conn).ExecuteNonQuery();

                string sql = @"INSERT INTO [dbo].[OfferForClient]
                    ([OfferId],[ClientID],[CatatogryID],[ItemID],[IsForAllItems],[unit],
                    [OfferTargetQnty],[OfferItemValue],[OfferItemPercentage])
                    VALUES(@OfferId,@ClientID,@CatatogryID,@ItemID,@IsForAllItems,@unit,
                    @OfferTargetQnty,@OfferItemValue,@OfferItemPercentage)";

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@OfferId", SqlDbType.Int).Value = (int)offerNum;
                cmd.Parameters.Add("@ClientID", SqlDbType.Int).Value = cmbClients.SelectedValue ?? (object)0;
                cmd.Parameters.Add("@unit", SqlDbType.Int).Value = 1;
                cmd.Parameters.Add("@OfferTargetQnty", SqlDbType.Float).Value = 0;
                cmd.Parameters.Add("@OfferItemValue", SqlDbType.Float).Value = ToDoubleSafe(txtClientOfferValue.Text);
                cmd.Parameters.Add("@OfferItemPercentage", SqlDbType.Float).Value = ToDoubleSafe(txtClientOfferPercentage.Text);

                if (rbOneCatagory.IsChecked == true)
                {
                    cmd.Parameters.Add("@CatatogryID", SqlDbType.Int).Value = cmbItemCatag.SelectedValue ?? (object)0;
                    cmd.Parameters.Add("@ItemID", SqlDbType.Int).Value = 0;
                    cmd.Parameters.Add("@IsForAllItems", SqlDbType.Bit).Value = 0;
                }
                else if (rbAllItemsClientOffer.IsChecked == true)
                {
                    cmd.Parameters.Add("@CatatogryID", SqlDbType.Int).Value = 0;
                    cmd.Parameters.Add("@ItemID", SqlDbType.Int).Value = 0;
                    cmd.Parameters.Add("@IsForAllItems", SqlDbType.Bit).Value = 1;
                }
                else
                {
                    cmd.Parameters.Add("@CatatogryID", SqlDbType.Int).Value = 0;
                    cmd.Parameters.Add("@ItemID", SqlDbType.Int).Value = 0;
                    cmd.Parameters.Add("@IsForAllItems", SqlDbType.Bit).Value = 0;
                }

                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        #endregion

        #region Delete

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Code == -1)
                {
                    DXMessageBox.Show("اختر عرضًا ليتم حذفه", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string msg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                    ? "هل تريد حذف العرض؟" : "Do you want to delete this Offer?";

                if (DXMessageBox.Show(msg, "تنبيه", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if (conn.State != System.Data.ConnectionState.Open)
                        conn.Open();

                    new SqlCommand($"update Offer set ISDeleted=1 where OfferId={Code}", conn).ExecuteNonQuery();
                    CLR();
                    DXMessageBox.Show("تم الحذف", "", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                    conn.Close();
            }
        }

        #endregion

        #region Navigation

        public void Navigate(string sqlStr)
        {
            dgvOfferSrch.UnselectAll();

            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                SqlCommand cmd = new SqlCommand(sqlStr, conn);
                ReadData(cmd.ExecuteReader());
            }
            catch (Exception ex)
            {
                ShowError(ex);
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

                double id = 0;
                double.TryParse(dr["OfferID"].ToString(), out id);
                Code = (int)id;

                txtOfferName.Text = dr["OfferName"]?.ToString() ?? "";
                txtOfferNo.Text = dr["OfferID"].ToString();

                if (dr["OfferDate"] != DBNull.Value)
                    txtDate.SelectedDate = Convert.ToDateTime(dr["OfferDate"]);

                if (dr["OfferStartDate"] != DBNull.Value)
                {
                    DateTime startDt = Convert.ToDateTime(dr["OfferStartDate"]);
                    txtOfferStartDate.SelectedDate = startDt;
                    txtOfferStartTime.Text = startDt.ToString("HH:mm");
                }

                if (dr["OfferExpire"] != DBNull.Value)
                {
                    DateTime expDt = Convert.ToDateTime(dr["OfferExpire"]);
                    txtOfferExpireDate.SelectedDate = expDt;
                    txtOfferExpireTime.Text = expDt.ToString("HH:mm");
                }

                int invIdx = 0;
                int.TryParse(dr["InvoiceID"].ToString(), out invIdx);
                cmbInvoices.SelectedIndex = Math.Max(0, invIdx - 1);

                double accCode = 0;
                double.TryParse(dr["OfferAccount"].ToString(), out accCode);
                if (accCode != -1)
                    cmbOfferAccount.SelectedValue = dr["OfferAccount"].ToString();

                int offerType = 0;
                int.TryParse(dr["OfferType"].ToString(), out offerType);
                TabControl1.SelectedIndex = Math.Max(0, offerType - 1);

                if (offerType == 1)
                {
                    double.TryParse(dr["OfferValue"].ToString(), out double ov);
                    double.TryParse(dr["OfferValueTarget"].ToString(), out double ovt);
                    double.TryParse(dr["OfferPercentage"].ToString(), out double op);
                    txtOfferValue.Text = ov.ToString();
                    txtOfferTarget.Text = ovt.ToString();
                    txtOfferPercentage.Text = op.ToString();
                }

                dr.Close();

                if (offerType == 2)
                    LoadOfferItems(Code);

                if (offerType == 3)
                {
                    SqlDataAdapter adapter = new SqlDataAdapter(
                        $"select * from OfferForClient where OfferId={Code}", conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        cmbClients.SelectedValue = dt.Rows[0]["ClientID"];
                        txtClientOfferValue.Text = dt.Rows[0]["OfferItemValue"].ToString();
                        txtClientOfferPercentage.Text = dt.Rows[0]["OfferItemPercentage"].ToString();

                        string catId = dt.Rows[0]["CatatogryID"]?.ToString() ?? "0";
                        if (catId != "0")
                        {
                            rbOneCatagory.IsChecked = true;
                            cmbItemCatag.SelectedValue = dt.Rows[0]["CatatogryID"];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from Offer where ISDeleted=0 order by OfferID asc");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from Offer where ISDeleted=0 order by OfferID desc");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            double offerNo = 0;
            double.TryParse(txtOfferNo.Text, out offerNo);
            Navigate($"select top 1 * from Offer where ISDeleted=0 and OfferID>{(int)offerNo} order by OfferID asc");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            double offerNo = 0;
            double.TryParse(txtOfferNo.Text, out offerNo);
            Navigate($"select top 1 * from Offer where ISDeleted=0 and OfferID<{(int)offerNo} order by OfferID desc");
        }

        #endregion

        #region Search

        private void btnInvSrch_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void Search()
        {
            if (!string.IsNullOrWhiteSpace(txtOfferNoSrch.Text))
            {
                double offerNo = 0;
                double.TryParse(txtOfferNoSrch.Text, out offerNo);
                LoadDG($"and OfferID={(int)offerNo}");
            }
            else if (!string.IsNullOrWhiteSpace(txtSrchName.Text))
            {
                LoadDG($"and OfferName like N'%{txtSrchName.Text}%'");
            }
            else if (ckAllOfferTypes.IsChecked != true && cmbOfferType.SelectedIndex >= 0)
            {
                LoadDG($"and OfferType={cmbOfferType.SelectedIndex}");
            }
            else
            {
                LoadDG("");
            }
        }

        private void dgvSrch_CellClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                OfferSrchRow selected = dgvOfferSrch.SelectedItem as OfferSrchRow;
                if (selected == null) return;

                int.TryParse(selected.Column17, out Code);
                Navigate($"select * from Offer where OfferID={Code}");
                TabControl2.SelectedIndex = 0;
            }
            catch { }
        }

        #endregion

        #region Grid Events

        private void BtnDeleteItemRow_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            OfferItemRow row = btn.Tag as OfferItemRow;
            if (row == null) return;

            if (DXMessageBox.Show("هل تريد حذف البند؟", "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                _itemRows.Remove(row);
        }

        private void dgvItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int idx = dgvItems.SelectedIndex;
            if (idx >= 0) RowIndex = idx;
        }

        private void dgvItems_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                OfferItemRow row = e.Row.Item as OfferItemRow;
                if (row == null) return;

                string colName = e.Column.SortMemberPath;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    double target = ToDoubleSafe(row.ColTarget);
                    double price = ToDoubleSafe(row.ColPrice);
                    double total = ToDoubleSafe(row.ColTotal);
                    double disc = ToDoubleSafe(row.ColDiscount);
                    double pct = ToDoubleSafe(row.ColDiscountPct);
                    double orig = ToDoubleSafe(row.ColPriceOrig);

                    if (colName == "ColTarget" || colName == "ColPrice")
                        row.ColTotal = (price * target).ToString("N2");

                    if (colName == "ColTotal" && target > 0)
                    {
                        row.ColPrice = (total / target).ToString("N2");
                        row.ColDiscount = (orig * target - total).ToString("N2");
                        if (orig * target != 0)
                            row.ColDiscountPct = (ToDoubleSafe(row.ColDiscount) / (orig * target) * 100).ToString("N2");
                    }

                    if (colName == "ColDiscount" && target > 0)
                    {
                        double netTotal = orig * target;
                        row.ColTotal = (netTotal - disc).ToString("N2");
                        row.ColPrice = (ToDoubleSafe(row.ColTotal) / target).ToString("N2");
                        if (netTotal != 0)
                            row.ColDiscountPct = (disc / netTotal * 100).ToString("N2");
                    }

                    if (colName == "ColDiscountPct" && target > 0)
                    {
                        double netTotal = orig * target;
                        double discVal = pct * netTotal / 100.0;
                        row.ColDiscount = discVal.ToString("N2");
                        row.ColTotal = (netTotal - discVal).ToString("N2");
                        if (target != 0)
                            row.ColPrice = (ToDoubleSafe(row.ColTotal) / target).ToString("N2");
                    }
                }));
            }
            catch { }
        }

        #endregion

        #region RadioButton Events

        private void rbPrice_CheckedChanged(object sender, RoutedEventArgs e) { }
        private void rbQnty_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (GbOfferItemtypes != null)
                GbOfferItemtypes.Visibility = rbQnty.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }
        private void rbService_CheckedChanged(object sender, RoutedEventArgs e) { }

        private void rbDiffItem_CheckedChanged(object sender, RoutedEventArgs e) { }

        private void rbSetOfItemsOffer_CheckedChanged(object sender, RoutedEventArgs e) { }

        private void rbOneCatagory_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (gbCatagory != null)
                gbCatagory.Visibility = rbOneCatagory.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void rbSomeItemsClientOffer_CheckedChanged(object sender, RoutedEventArgs e) { }

        private void TabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void CheckBox2_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbInvTypeSrch != null)
                cmbInvTypeSrch.IsEnabled = CheckBox2.IsChecked != true;
        }

        #endregion

        #region Client Search

        private void cmbClients_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                frmSrchClient srchForm = new frmSrchClient();
                MainClass.ApplyPermissionToForm(srchForm);
                MainClass.DoApplyUserSett(srchForm);
                srchForm.Type = 1;
                srchForm.ShowDialog();

                if (!string.IsNullOrEmpty(srchForm.Clientname))
                    cmbClients.SelectedValue = srchForm.ClientId;
            }
            catch { }
        }

        #endregion

        #region Item Search

        private void addNewItem()
        {
            try
            {
                frmItemsSrch srchForm = new frmItemsSrch();
                MainClass.ApplyPermissionToForm(srchForm);
                MainClass.DoApplyUserSett(srchForm);
                srchForm.sql = "select id, name, nameEN, sale_price, unit from Items where IS_Deleted=0 order by id";
                srchForm.search = "select id, name, nameEN, sale_price, unit from Items";
                srchForm.Itemname = "";
                srchForm.StoreId = 1;
                srchForm.txtSrchNm.Text = SrchName;
                srchForm.ShowDialog();

                if (srchForm.ISDone && srchForm.ItemId > 0)
                {
                    foreach (int itemId in srchForm.Itemlist)
                        SearchByID(itemId, 0);
                }
            }
            catch { }
        }

        private void SearchByID(int selectedId, int unitId)
        {
            try
            {
                if (selectedId <= 0) return;

                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select name,id,Code,nameEN from Items where IS_Deleted=0 and id={selectedId}", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0) return;

                string itemName = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                    ? dt.Rows[0]["name"].ToString()
                    : (string.IsNullOrEmpty(dt.Rows[0]["nameEN"]?.ToString())
                        ? dt.Rows[0]["name"].ToString()
                        : dt.Rows[0]["nameEN"].ToString());

                OfferItemRow newRow = new OfferItemRow
                {
                    AccRowIndex = _itemRows.Count + 1,
                    ColCode = dt.Rows[0]["Code"].ToString(),
                    ColId = dt.Rows[0]["id"].ToString(),
                    ColName = itemName,
                    ColTarget = "1",
                    ColUnit = "",
                    ColPrice = "0",
                    ColTotal = "0",
                    ColDiscount = "0",
                    ColDiscountPct = "0"
                };

                _itemRows.Add(newRow);
                LoadItemInfo(selectedId, newRow);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void LoadItemInfo(int id, OfferItemRow row)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    $"select units.name as unit, purch_price as purch, sale_price as sale, Items.barcode as barcode from Items, units where items.unit=units.id and items.id={id}",
                    conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    row.ColUnit = dt.Rows[0]["unit"].ToString();
                    row.ColPriceOrig = dt.Rows[0]["sale"].ToString();
                    row.ColPrice = dt.Rows[0]["sale"].ToString();
                    row.ColTotal = dt.Rows[0]["sale"].ToString();
                }
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

        #region Close

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        #endregion

        #region Helpers

        private DateTime BuildDateTime(DateTime date, string timeText)
        {
            if (TimeSpan.TryParse(timeText?.Trim(), out TimeSpan ts))
                return date.Date.Add(ts);
            return date.Date;
        }

        private double ToDoubleSafe(string text)
        {
            double.TryParse(text?.Trim(), out double result);
            return result;
        }

        private int ToIntSafe(string text)
        {
            int.TryParse(text?.Trim(), out int result);
            return result;
        }

        private void ShowError(Exception ex)
        {
            DXMessageBox.Show("خطأ" + Environment.NewLine + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }

    // ─── Model Classes ──────────────────────────────────────────────────────

    public class OfferItemRow
    {
        public int AccRowIndex { get; set; }
        public string ColCode { get; set; }
        public string ColId { get; set; }
        public string ColName { get; set; }
        public string ColTarget { get; set; }
        public string ColPriceOrig { get; set; }
        public string ColUnit { get; set; }
        public string ColPrice { get; set; }
        public string ColTotal { get; set; }
        public string ColDiscount { get; set; }
        public string ColDiscountPct { get; set; }
    }

    public class OfferSrchRow
    {
        public string Column17 { get; set; }
        public string Column11 { get; set; }
        public string Column2 { get; set; }
        public string Column19 { get; set; }
        public string Column20 { get; set; }
        public string Column14 { get; set; }
        public string Column15 { get; set; }
        public string Column13 { get; set; }
        public string Column16 { get; set; }
    }
}