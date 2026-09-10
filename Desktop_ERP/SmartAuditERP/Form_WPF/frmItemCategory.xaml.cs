using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Printing;
using System.IO;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Button = System.Windows.Controls.Button;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using TextBox = System.Windows.Controls.TextBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmItemCategory : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private SqlConnection conn1;

        private int Code;
        private bool ISNew;
        private bool Order;

        private ObservableCollection<CategoryRow> _categoryRows;
        private ObservableCollection<CategoryNoteRow_frmItemCategory> _noteRows;

        #endregion

        #region Constructor

        public frmItemCategory()
        {
            conn = MainClass.ConnObj();
            conn1 = MainClass.ConnObj();
            Code = -1;
            ISNew = true;
            Order = false;
            _categoryRows = new ObservableCollection<CategoryRow>();
            _noteRows = new ObservableCollection<CategoryNoteRow_frmItemCategory>();

            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmActs_Load(object sender, RoutedEventArgs e)
        {
            LoadDG(0);
            GetPrinters1();
            LoadParent();
            txtName.Focus();
            LoadCode();
            LoadBranches();
            GridControl1.ItemsSource = _noteRows;
            WindowState = MainClass.Window_State == WindowState.Maximized
                ? WindowState.Maximized
                : WindowState.Normal;
        }

        #endregion

        #region Clear / New

        private void CLR()
        {
            try
            {
                _noteRows.Clear();
            }
            catch { }

            txtName.Text = "";
            txtNameEN.Text = "";
            txtCode.Text = "";
            txtNo.Text = "";
            cmbPrinters.Text = "";
            clkShowInPOS.IsChecked = false;
            Code = -1;
            picImage.Source = null;
            btnUp.Visibility = Visibility.Collapsed;
            btnDown.Visibility = Visibility.Collapsed;
            btnSaveOrder.Visibility = Visibility.Collapsed;
            Order = false;
            ISNew = true;
            cmbMainCat.SelectedIndex = -1;
            chkPrintItemsSeparately.IsChecked = false;
            cmbBranches.SelectedIndex = 0;
            chkAllBranch.IsChecked = false;
            LoadCode();
            chkPrintALLItems.IsChecked = false;
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            CLR();
            LoadDG(0);
        }

        #endregion

        #region Data Loading

        private int GetCategoryId()
        {
            int result = 0;
            try
            {
                using (SqlConnection sqlConn = MainClass.ConnObj())
                {
                    sqlConn.Open();
                    object maxIdObj = new SqlCommand("SELECT MAX(id) FROM itemsCategory", sqlConn).ExecuteScalar();
                    result = (maxIdObj == DBNull.Value) ? 1 : Convert.ToInt32(maxIdObj) + 1;

                    SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM itemsCategory WHERE id = @CatId", sqlConn);
                    checkCmd.Parameters.AddWithValue("@CatId", result);

                    while (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                    {
                        result++;
                        checkCmd.Parameters["@CatId"].Value = result;
                    }
                }
            }
            catch { }
            return result;
        }

        private void LoadCode()
        {
            try
            {
                using (SqlConnection sqlConn = MainClass.ConnObj())
                {
                    sqlConn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT ISNULL(MAX(id),0) as id FROM itemsCategory", sqlConn);
                    object result = cmd.ExecuteScalar();
                    double maxId = 0;
                    double.TryParse(result?.ToString(), out maxId);
                    txtCode.Text = MainClass.BranchNo + "00" + (maxId + 1).ToString();
                    txtNo.Text = GetCategoryId().ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void LoadDG(int displayMode)
        {
            try
            {
                _categoryRows.Clear();
                string sql = displayMode == 1
                    ? "select * from itemsCategory where IS_Deleted=0 and ShowInPOS=1 order by DispalyOrder"
                    : "select * from itemsCategory where IS_Deleted=0 order by Id";

                SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                bool isArabic = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow row = dt.Rows[i];

                    string displayName = isArabic
                        ? row["name"].ToString()
                        : string.IsNullOrEmpty(row["nameEN"].ToString())
                            ? row["name"].ToString()
                            : row["nameEN"].ToString();

                    bool showInPos = false;
                    bool.TryParse(row["ShowInPOS"].ToString(), out showInPos);

                    _categoryRows.Add(new CategoryRow
                    {
                        Column2Id = row["id"].ToString(),
                        Column5Code = row["code"].ToString(),
                        Column1Name = displayName,
                        Column4Order = row["DispalyOrder"].ToString(),
                        Column3ShowPOS = showInPos
                    });
                }

                dgvData.ItemsSource = _categoryRows;
                ISNew = false;
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء التحميل", ex);
            }
        }

        private void LoadBranches()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter("select id,name from Branches where IS_Deleted=0", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbBranches.ItemsSource = dt.DefaultView;
                cmbBranches.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowError("خطأ تحميل الفروع", ex);
            }
        }

        private void LoadParent()
        {
            try
            {
                string sql = "select code,name from itemsCategory where IS_Deleted=0 and Id Not IN(select group_id from items where IS_Deleted=0)";
                SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbMainCat.ItemsSource = dt.DefaultView;
                cmbMainCat.SelectedIndex = -1;
            }
            catch { }
        }

        private void GetPrinters1()
        {
            try
            {
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    if (!cmbPrinters.Items.Contains(printer))
                        cmbPrinters.Items.Add(printer);
                }

                EnumeratedPrintQueueTypes[] flags = new[]
                {
                    EnumeratedPrintQueueTypes.Local,
                    EnumeratedPrintQueueTypes.Connections,
                    EnumeratedPrintQueueTypes.Shared
                };

                foreach (PrintQueue pq in new LocalPrintServer().GetPrintQueues(flags))
                {
                    if (!cmbPrinters.Items.Contains(pq.Name))
                        cmbPrinters.Items.Add(pq.Name);
                }
            }
            catch { }
        }

        #endregion

        #region Save

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            await SaveDataAsync();
        }

        private async Task SaveDataAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    DXMessageBox.Show("ادخل المجموعة", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName.Focus();
                    return;
                }

                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                if (string.IsNullOrWhiteSpace(txtCode.Text))
                {
                    if (Code == -1)
                    {
                        SqlCommand maxCmd = new SqlCommand("select max(Id) from itemsCategory", conn);
                        double maxId = 0;
                        double.TryParse(maxCmd.ExecuteScalar()?.ToString(), out maxId);
                        txtCode.Text = (maxId + 1).ToString();
                    }
                    else
                    {
                        txtCode.Text = Code.ToString();
                    }
                }

                // فحص تكرار الرمز
                SqlDataAdapter codeCheck = new SqlDataAdapter(
                    "select Id from itemsCategory where code=N'" + txtCode.Text.Trim() + "' and Id<>" + Code, conn);
                DataTable codeCheckDt = new DataTable();
                codeCheck.Fill(codeCheckDt);

                if (codeCheckDt.Rows.Count > 0)
                {
                    string msg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                        ? "الرمز تم إدخاله مسبقًا"
                        : "Code is previously inserted";
                    DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtCode.Focus();
                    return;
                }

                // فحص الرئيسي
                if (cmbMainCat.SelectedValue != null)
                {
                    SqlDataAdapter parentCheck = new SqlDataAdapter(
                        "select * from itemsCategory where code=N'" + cmbMainCat.SelectedValue + "'", conn);
                    DataTable parentDt = new DataTable();
                    parentCheck.Fill(parentDt);

                    if (parentDt.Rows.Count == 0)
                    {
                        DXMessageBox.Show("المجموعة الرئيسية غير موجودة", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                int showInPOS = clkShowInPOS.IsChecked == true ? 1 : 0;
                int displayOrder = -1;

                if (clkShowInPOS.IsChecked == true)
                {
                    if (string.IsNullOrWhiteSpace(txtDisOrder?.Text))
                    {
                        SqlDataAdapter posAdapter = new SqlDataAdapter(
                            "select Id from itemsCategory where ShowInPOS=1 and IS_Deleted=0", conn);
                        DataTable posDt = new DataTable();
                        posAdapter.Fill(posDt);
                        displayOrder = posDt.Rows.Count == 0 ? 1 : posDt.Rows.Count + 1;
                    }
                    else
                    {
                        int.TryParse(txtDisOrder?.Text, out displayOrder);
                    }
                }

                string parentCode = cmbMainCat.SelectedValue?.ToString() ?? "";
                int typeVal = 2;

                if (!string.IsNullOrEmpty(parentCode))
                {
                    SqlCommand updateParent = new SqlCommand(
                        "update itemsCategory set type=@type where Code=N'" + parentCode + "'", conn);
                    updateParent.Parameters.Add("@type", SqlDbType.Int).Value = 1;
                    updateParent.ExecuteNonQuery();
                }

                // فحص تكرار الاسم
                SqlDataAdapter nameCheck = new SqlDataAdapter(
                    "select id from itemsCategory where name=N'" + txtName.Text + "' and id<>" + Code, conn);
                DataTable nameDt = new DataTable();
                nameCheck.Fill(nameDt);

                if (nameDt.Rows.Count > 0)
                {
                    string msg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                        ? "المجموعة تم إدخالها مسبقًا"
                        : "Group is previously inserted";
                    DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtName.Focus();
                    return;
                }

                int printAllItems = chkPrintALLItems.IsChecked == true ? 1 : 0;
                object imageData = DBNull.Value;

                if (picImage.Source is BitmapSource bmpSource)
                    imageData = BitmapSourceToBytes(bmpSource);

                int branchId = chkAllBranch.IsChecked == true
                    ? -1
                    : (cmbBranches.SelectedValue != null ? Convert.ToInt32(cmbBranches.SelectedValue) : -1);

                int allBranch = chkAllBranch.IsChecked == true ? 1 : 0;
                int printSeparately = chkPrintItemsSeparately.IsChecked == true ? 1 : 0;

                string printerName = cmbPrinters.Text ?? "";
                int catNo = 0;
                int.TryParse(txtNo.Text, out catNo);

                if (Code == -1)
                {
                    SqlCommand insertCmd = new SqlCommand(
                        @"insert into itemsCategory(Id,CategoryId,name,nameEN,color,printer,ShowInPOS,DispalyOrder,image,IS_Deleted,code,parentCode,
                          type,PrintAllItems,PrintItemsSeparately,BranchId,AllBranch)
                          values(@Id,@CategoryId,@name,@nameEN,0,@printer,@ShowInPOS,@DispalyOrder,@image,0,@code,@parentCode,
                          @type,@PrintAllItems,@PrintItemsSeparately,@BranchId,@AllBranch)", conn);

                    insertCmd.Parameters.Add("@Id", SqlDbType.Int).Value = catNo;
                    insertCmd.Parameters.Add("@CategoryId", SqlDbType.Int).Value = catNo;
                    insertCmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = txtName.Text;
                    insertCmd.Parameters.Add("@nameEN", SqlDbType.NVarChar).Value = txtNameEN.Text;
                    insertCmd.Parameters.Add("@printer", SqlDbType.NVarChar).Value = printerName;
                    insertCmd.Parameters.Add("@ShowInPOS", SqlDbType.Int).Value = showInPOS;
                    insertCmd.Parameters.Add("@DispalyOrder", SqlDbType.Int).Value = displayOrder;
                    insertCmd.Parameters.Add("@image", SqlDbType.Image).Value = imageData;
                    insertCmd.Parameters.Add("@code", SqlDbType.NVarChar).Value = txtCode.Text;
                    insertCmd.Parameters.Add("@parentCode", SqlDbType.NVarChar).Value = parentCode;
                    insertCmd.Parameters.Add("@type", SqlDbType.Int).Value = typeVal;
                    insertCmd.Parameters.Add("@PrintAllItems", SqlDbType.Bit).Value = printAllItems;
                    insertCmd.Parameters.Add("@PrintItemsSeparately", SqlDbType.Bit).Value = printSeparately;
                    insertCmd.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId;
                    insertCmd.Parameters.Add("@AllBranch", SqlDbType.Bit).Value = allBranch;
                    insertCmd.ExecuteNonQuery();
                }
                else
                {
                    SqlCommand updateCmd = new SqlCommand(
                        @"update itemsCategory set name=@name, nameEN=@nameEN,
                          printer=@printer, ShowInPOS=@ShowInPOS, DispalyOrder=@DispalyOrder, IS_Deleted=0,
                          image=@image, code=@code, ParentCode=@ParentCode, type=@type,
                          PrintAllItems=@PrintAllItems, PrintItemsSeparately=@PrintItemsSeparately,
                          BranchId=@BranchId, AllBranch=@AllBranch
                          where Id=@Id", conn);

                    updateCmd.Parameters.Add("@Id", SqlDbType.Int).Value = catNo;
                    updateCmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = txtName.Text;
                    updateCmd.Parameters.Add("@nameEN", SqlDbType.NVarChar).Value = txtNameEN.Text;
                    updateCmd.Parameters.Add("@printer", SqlDbType.NVarChar).Value = printerName;
                    updateCmd.Parameters.Add("@ShowInPOS", SqlDbType.Int).Value = showInPOS;
                    updateCmd.Parameters.Add("@DispalyOrder", SqlDbType.Int).Value = displayOrder;
                    updateCmd.Parameters.Add("@image", SqlDbType.Image).Value = imageData;
                    updateCmd.Parameters.Add("@code", SqlDbType.NVarChar).Value = txtCode.Text;
                    updateCmd.Parameters.Add("@ParentCode", SqlDbType.NVarChar).Value = parentCode;
                    updateCmd.Parameters.Add("@type", SqlDbType.Int).Value = typeVal;
                    updateCmd.Parameters.Add("@PrintAllItems", SqlDbType.Bit).Value = printAllItems;
                    updateCmd.Parameters.Add("@PrintItemsSeparately", SqlDbType.Bit).Value = printSeparately;
                    updateCmd.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId;
                    updateCmd.Parameters.Add("@AllBranch", SqlDbType.Bit).Value = allBranch;
                    updateCmd.ExecuteNonQuery();
                }

                // حذف وإعادة إدراج الملاحظات
                if (Code != -1)
                    new SqlCommand("delete from CategoryNotes where CatID=N'" + txtNo.Text + "'", conn).ExecuteNonQuery();

                if (conn1.State != System.Data.ConnectionState.Open)
                    conn1.Open();

                foreach (CategoryNoteRow_frmItemCategory noteRow in _noteRows)
                {
                    if (string.IsNullOrWhiteSpace(noteRow.DgvNoteCol)) continue;

                    SqlCommand noteCmd = new SqlCommand(
                        "insert into CategoryNotes(CatID,Note,AddPrice) values(@CatID,@Note,@AddPrice)", conn1);
                    noteCmd.Parameters.Add("@CatID", SqlDbType.NVarChar).Value = txtNo.Text;
                    noteCmd.Parameters.Add("@Note", SqlDbType.NVarChar).Value = noteRow.DgvNoteCol;

                    double addPrice = 0;
                    double.TryParse(noteRow.DgvAddPrice, out addPrice);
                    noteCmd.Parameters.Add("@AddPrice", SqlDbType.Float).Value = addPrice;

                    noteCmd.ExecuteNonQuery();
                }

                if (Sync.ActiveSync && Sync.SyncType > 0)
                    await SyncCategory();
                Home Home = new Home();
                if (Home._is_active)
                    ConnectBroker.mqttClient.Publish(
                        ConnectBroker.ClientCode + "ItemsCategory",
                        Encoding.UTF8.GetBytes(SendData.GetItemsCategory("")),
                        0, true);

                if (Code != -1)
                {
                    LoadDG(1);
                    UpdateOrderNo();
                    SaveOrder();
                }

                frmSavedMsg savedMsg = new frmSavedMsg();
                if (!ISNew)
                    savedMsg.lblSave.Text = "تم حفظ التعديلات بنجاح...";

                savedMsg.ShowDialog();

                if (savedMsg.Pressed == 1)
                {
                    txtName.Text = "";
                    txtName.Focus();
                    LoadParent();
                    LoadDG(0);
                    CLR();
                }
                else if (savedMsg.Pressed == 2)
                {
                    LoadParent();
                    LoadDG(0);
                    ISNew = false;
                }
                else if (savedMsg.Pressed == 3)
                {
                    Close();
                }
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

        #region Delete

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Code == -1)
                {
                    DXMessageBox.Show("اختر مجموعة ليتم حذفها", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                if (!string.IsNullOrWhiteSpace(txtName.Text))
                {
                    SqlDataAdapter linkCheck = new SqlDataAdapter(
                        "select items.id from items where items.group_id='" + Code + "' and items.IS_Deleted=0", conn);
                    DataTable linkDt = new DataTable();
                    linkCheck.Fill(linkDt);

                    if (linkDt.Rows.Count > 0)
                    {
                        string msg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                            ? "هذه المجموعة لها ارتباطات فرعية لا يمكن حذفها"
                            : "This Group is previously used in items";
                        DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                MessageBoxResult confirm = DXMessageBox.Show(
                    "هل أنت متأكد من حذف المجموعة؟",
                    "",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                {
                    new SqlCommand("delete from itemsCategory where id=" + Code, conn).ExecuteNonQuery();
                    new SqlCommand("delete from CategoryNotes where CatID=N'" + Code + "'", conn).ExecuteNonQuery();

                    DXMessageBox.Show("تم الحذف", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadDG(1);
                    UpdateOrderNo();
                    SaveOrder();
                    LoadDG(0);
                    CLR();
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ أثناء الحذف", ex);
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
            dgvData.UnselectAll();

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

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from itemsCategory order by Id asc");
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from itemsCategory order by Id desc");
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from itemsCategory where Id>" + Code + " order by Id asc");
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            Navigate("select top 1 * from itemsCategory where Id<" + Code + " order by Id desc");
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Read Data

        private void ReadData(SqlDataReader dr)
        {
            try
            {
                if (!dr.HasRows) return;

                dr.Read();
                CLR();
                ISNew = false;

                int.TryParse(dr["id"].ToString(), out Code);
                txtNo.Text = Code.ToString();
                txtName.Text = dr["name"].ToString();

                if (!string.IsNullOrEmpty(dr["parentCode"].ToString()))
                    cmbMainCat.SelectedValue = dr["parentCode"];

                txtCode.Text = dr["code"].ToString();
                clkShowInPOS.IsChecked = Convert.ToBoolean(dr["ShowInPOS"]);

                if (txtDisOrder != null)
                    txtDisOrder.Text = dr["DispalyOrder"].ToString();

                try { txtNameEN.Text = dr["nameEN"].ToString(); } catch { }

                try { cmbPrinters.Text = dr["printer"].ToString(); } catch { }

                if (dr["PrintAllItems"] != DBNull.Value)
                    chkPrintALLItems.IsChecked = !Convert.ToBoolean(dr["PrintAllItems"] == (object)0);

                if (dr["PrintItemsSeparately"] != DBNull.Value)
                    chkPrintItemsSeparately.IsChecked = !Convert.ToBoolean(dr["PrintItemsSeparately"] == (object)0);

                if (dr["BranchId"] != DBNull.Value)
                {
                    if (Convert.ToInt32(dr["BranchId"]) == -1)
                    {
                        cmbBranches.SelectedIndex = -1;
                        chkAllBranch.IsChecked = true;
                    }
                    else
                    {
                        cmbBranches.SelectedValue = dr["BranchId"];
                    }
                }
                else
                {
                    cmbBranches.SelectedIndex = 0;
                }

                chkAllBranch.IsChecked = dr["AllBranch"] != DBNull.Value && Convert.ToBoolean(dr["AllBranch"]);

                // تحميل الملاحظات
                _noteRows.Clear();

                SqlDataAdapter notesAdapter = new SqlDataAdapter(
                    "select Note,AddPrice from CategoryNotes where CatID=N'" + dr["id"] + "'", conn);
                DataTable notesDt = new DataTable();
                notesAdapter.Fill(notesDt);

                for (int i = 0; i < notesDt.Rows.Count; i++)
                {
                    _noteRows.Add(new CategoryNoteRow_frmItemCategory
                    {
                        DgvNo = (i + 1).ToString(),
                        DgvNoteCol = notesDt.Rows[i]["Note"].ToString(),
                        DgvAddPrice = notesDt.Rows[i]["AddPrice"].ToString()
                    });
                }

                GridControl1.ItemsSource = _noteRows;

                // تحميل الصورة
                SqlDataAdapter imgAdapter = new SqlDataAdapter(
                    "select image from itemsCategory where Id=" + Code, conn);
                DataTable imgDt = new DataTable();
                imgAdapter.Fill(imgDt);

                if (imgDt.Rows.Count > 0 && imgDt.Rows[0]["image"] != DBNull.Value)
                {
                    byte[] imgBytes = (byte[])imgDt.Rows[0]["image"];
                    picImage.Source = BytesToBitmapSource(imgBytes);
                }
                else
                {
                    picImage.Source = null;
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في قراءة البيانات", ex);
            }
        }

        #endregion

        #region Grid Click

        private void dgvData_CellClick(object sender, MouseButtonEventArgs e)
        {
            if (Order) return;

            CategoryRow selectedRow = dgvData.SelectedItem as CategoryRow;
            if (selectedRow == null) return;

            int rowIndex = _categoryRows.IndexOf(selectedRow);
            if (rowIndex < 0) return;

            dgv_RowChng(rowIndex);
        }

        private void dgv_RowChng(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _categoryRows.Count) return;

            CategoryRow row = _categoryRows[rowIndex];
            int.TryParse(row.Column2Id, out Code);
            txtNo.Text = Code.ToString();
            Navigate("select * from itemsCategory where Id=" + Code);
        }

        #endregion

        #region Notes Grid

        private void btnDeleteRowDgv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = sender as Button;
                if (btn == null) return;

                CategoryNoteRow_frmItemCategory noteRow = btn.Tag as CategoryNoteRow_frmItemCategory;
                if (noteRow == null) return;

                if (string.IsNullOrWhiteSpace(noteRow.DgvNoteCol)) return;

                MessageBoxResult confirm = DXMessageBox.Show(
                    "هل أنت متأكد من الحذف؟",
                    "رسالة تأكيد",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes)
                    _noteRows.Remove(noteRow);
            }
            catch (Exception ex)
            {
                ShowError("خطأ في الحذف", ex);
            }
        }

        #endregion

        #region Order

        private void btnOrderGrp_Click(object sender, RoutedEventArgs e)
        {
            LoadDG(1);
            btnUp.Visibility = Visibility.Visible;
            btnDown.Visibility = Visibility.Visible;
            btnSaveOrder.Visibility = Visibility.Visible;
            Order = true;
        }

        private void btn_Up_Click(object sender, RoutedEventArgs e)
        {
            CategoryRow selected = dgvData.SelectedItem as CategoryRow;
            if (selected == null) return;

            int idx = _categoryRows.IndexOf(selected);
            if (idx <= 0) return;

            _categoryRows.Move(idx, idx - 1);
            UpdateOrderNo();
            dgvData.SelectedItem = selected;
        }

        private void btn_Down_Click(object sender, RoutedEventArgs e)
        {
            CategoryRow selected = dgvData.SelectedItem as CategoryRow;
            if (selected == null) return;

            int idx = _categoryRows.IndexOf(selected);
            if (idx < 0 || idx >= _categoryRows.Count - 1) return;

            _categoryRows.Move(idx, idx + 1);
            UpdateOrderNo();
            dgvData.SelectedItem = selected;
        }

        private void UpdateOrderNo()
        {
            for (int i = 0; i < _categoryRows.Count; i++)
                _categoryRows[i].Column4Order = (i + 1).ToString();
        }

        private void btnSaveOrder_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirm = DXMessageBox.Show(
                "هل أنت متأكد من حفظ الترتيب الجديد للمجموعات؟",
                "",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
                SaveOrder();
        }

        private void SaveOrder()
        {
            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                for (int i = 0; i < _categoryRows.Count; i++)
                {
                    int catId = 0;
                    int.TryParse(_categoryRows[i].Column2Id, out catId);

                    SqlCommand cmd = new SqlCommand(
                        "update itemsCategory set DispalyOrder=@DispalyOrder where IS_Deleted=0 and ShowInPOS=1 and Id=" + catId,
                        conn);
                    cmd.Parameters.Add("@DispalyOrder", SqlDbType.Int).Value = i + 1;
                    cmd.ExecuteNonQuery();
                }

                CLR();
                LoadDG(0);
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

        #region Image

        private void lnkImgAdd_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog
                {
                    Filter = "All Files|*.*|JPEG|*.jpg|BMP|*.bmp|GIF|*.gif|PNG|*.png"
                };

                if (dialog.ShowDialog() == true)
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(dialog.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    picImage.Source = bitmap;
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في تحميل الصورة", ex);
            }
        }

        private void lnkImgClr_Click(object sender, MouseButtonEventArgs e)
        {
            picImage.Source = null;
        }

        private byte[] BitmapSourceToBytes(BitmapSource bitmapSource)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(ms);
                return ms.ToArray();
            }
        }

        private BitmapSource BytesToBitmapSource(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = ms;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
        }

        #endregion

        #region ComboBox

        private void cmbMainCat_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbMainCat.SelectedValue != null)
                    GenerateCode(cmbMainCat.SelectedValue.ToString());
            }
            catch { }
        }

        private void GenerateCode(string parentCode)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select COUNT(*) as ItemNo from itemsCategory where ParentCode=N'" + parentCode + "'", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    double itemNo = 0;
                    double.TryParse(dt.Rows[0]["ItemNo"].ToString(), out itemNo);
                    txtCode.Text = parentCode + (itemNo + 1).ToString();
                }
                else
                {
                    txtCode.Text = parentCode + "1";
                }
            }
            catch (Exception ex)
            {
                ShowError("خطأ في توليد الرمز", ex);
            }
        }

        #endregion

        #region CheckBox

        private void chkAllBranch_CheckedChanged(object sender, RoutedEventArgs e)
        {
            cmbBranches.IsEnabled = chkAllBranch.IsChecked != true;
        }

        private void chkPrintALLItems_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (chkPrintALLItems.IsChecked == true && string.IsNullOrWhiteSpace(cmbPrinters.Text))
            {
                string msg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                    ? "الرجاء اختيار طابعة"
                    : "Please choose printer";
                DXMessageBox.Show(msg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                chkPrintALLItems.IsChecked = false;
            }
        }

        #endregion

        #region KeyDown

        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                btnSave_Click(null, null);
        }

        private void frmUnits_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11
                && Sync.ValidAPIUrl
                && DXMessageBox.Show("هل تريد تحديث بيانات المجموعات؟", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                new ItemOper().ReadCategoriesOnline();
            }
        }

        #endregion

        #region Sync

        private async Task<bool> SyncCategory()
        {
            Category category = BindToCategory();
            CategoryCRUD crud = new CategoryCRUD(Sync.APIUrl);

            if (Sync.ValidAPIUrl)
                crud.PostCategoriesOnline(category, ISNew);
            else
                crud.AddCategoryLocally(category, ISNew);

            return true;
        }

        private Category BindToCategory()
        {
            int catId = 0;
            int.TryParse(txtNo.Text, out catId);

            int dispOrder = 0;
            int.TryParse(txtDisOrder?.Text ?? "0", out dispOrder);

            int branchId = cmbBranches.SelectedIndex > -1
                ? Convert.ToInt32(cmbBranches.SelectedValue)
                : -1;

            return new Category
            {
                ClientCode = Sync.ClientCode,
                CategoryId = catId,
                Code = txtCode.Text,
                Name = txtName.Text,
                NameEN = txtNameEN.Text,
                ParentCode = cmbMainCat.SelectedValue?.ToString() ?? "",
                Printer = cmbPrinters.Text,
                ShowInPOS = clkShowInPOS.IsChecked == true,
                ISLeaf = true,
                Active = true,
                DispalyOrder = dispOrder,
                AllBranch = chkAllBranch.IsChecked == true,
                BranchId = branchId
            };
        }

        #endregion

        #region Helpers

        // txtDisOrder — مخفي لكنه ضروري للمنطق
        private TextBox txtDisOrder => FindName("txtDisOrder_Internal") as TextBox;

        private void ShowError(string context, Exception ex)
        {
            DXMessageBox.Show(
                context + Environment.NewLine + "تفاصيل الخطأ: " + ex.Message,
                "خطأ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        #endregion
    }

    // ─── Models ───────────────────────────────────────────────────────────
    public class CategoryRow
    {
        public string Column2Id { get; set; }
        public string Column5Code { get; set; }
        public string Column1Name { get; set; }
        public string Column4Order { get; set; }
        public bool Column3ShowPOS { get; set; }
    }

    public class CategoryNoteRow_frmItemCategory
    {
        public string DgvNo { get; set; }
        public string DgvNoteCol { get; set; }
        public string DgvAddPrice { get; set; }
    }
}