using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using AuditorAPI.Models;
using DevExpress.Xpf.Core;
using SmartAuditERP.Form_WPF;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmExpensesAccount : DXWindow
    {
        #region Fields

        private SqlConnection con;

        private const string ParentAccountCode = "3121";

        #endregion

        #region Constructor

        public frmExpensesAccount()
        {
            con = MainClass.ConnObj();
            InitializeComponent();
            Loaded += frmExpensesAccount_Load;
        }

        #endregion

        #region Load

        private void frmExpensesAccount_Load(object sender, RoutedEventArgs e)
        {
            GenerateCode();
            LoadCostCenters();
        }

        #endregion

        #region Generate Code

        private void GenerateCode()
        {
            try
            {
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                    "select max(Code) from Accounts_Index where ParentCode=N'" + ParentAccountCode + "'", con);

                DataTable dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    string maxCodeValue = dataTable.Rows[0][0]?.ToString() ?? string.Empty;

                    if (!string.IsNullOrEmpty(maxCodeValue) && double.TryParse(maxCodeValue, out double maxCode))
                        txtCode.Text = ((int)(maxCode + 1)).ToString();
                    else
                        txtCode.Text = ParentAccountCode + "0001";
                }
                else
                {
                    txtCode.Text = ParentAccountCode + "0001";
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region Save

        private void SaveAccount()
        {
            try
            {
                string confirmMessage = string.Equals(MainClass.Language, "en", StringComparison.OrdinalIgnoreCase)
                    ? "Are you sure to save the account?"
                    : "هل انت متأكد من حفظ الحساب؟";

                if (MessageBox.Show(confirmMessage, "", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.No)
                    return;

                if (string.IsNullOrWhiteSpace(txtAccName.Text))
                {
                    string emptyMsg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                        ? "من فضلك أدخل اسم الحساب"
                        : "Enter Account Name";
                    MessageBox.Show(emptyMsg);
                    return;
                }

                TreeAccount treeAccount = new TreeAccount
                {
                    AccName = txtAccName.Text.Trim(),
                    AccNature = 1,
                    Code = txtCode.Text.Trim(),
                    ParentCode = ParentAccountCode,
                    IntialBalance = 0m,
                    EmpId = MainClass.EmpNo,
                    AccType = 2,
                    IsDeleted = false,
                    FinalAcc = 2,
                    LastUpdateDate = DateTime.Now,
                    ClientCode = Sync.ClientCode,
                    BranchId = MainClass.BranchNo,
                    CreateDate = DateTime.Now,
                    CostCenter = -1
                };

                if (cmbCostCenter.EditValue != null &&
                    !string.IsNullOrEmpty(cmbCostCenter.EditValue.ToString()))
                {
                    if (int.TryParse(cmbCostCenter.EditValue.ToString(), out int ccValue))
                        treeAccount.CostCenter = ccValue;
                }

                List<TreeAccount> accountList = new List<TreeAccount> { treeAccount };

                bool saveResult = new EntityOperations().SaveAccounts(accountList);

                if (saveResult)
                {
                    if (Sync.ActiveSync && Sync.SyncType > 0)
                        new AccountCRUD(Sync.APIUrl).AddTreeAccount(accountList);
                }
                else
                {
                    ShowError("خطأ أثناء الحفظ");
                    return;
                }

                string successMsg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                    ? "تمت حفظ البيانات بنجاح"
                    : "Saved";

                MessageBox.Show(successMsg, "اضافة", MessageBoxButton.OK, MessageBoxImage.Information);
                clr();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                EnsureConnectionClosed();
            }
        }

        #endregion

        #region Clear

        private void clr()
        {
            txtAccName.Text = string.Empty;
            txtCode.Text = string.Empty;
            cmbCostCenter.SelectedIndex = -1;
            txtAccName.Focus();
            GenerateCode();
        }

        #endregion

        #region Load Cost Centers

        private void LoadCostCenters()
        {
            try
            {
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                    "select code, name from cost_center " +
                    "where type=2 and BranchId=" + MainClass.BranchNo +
                    " and Is_Deleted=0 order by code", con);

                DataTable dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);

                cmbCostCenter.ItemsSource = dataTable.DefaultView;
                cmbCostCenter.DisplayMember = "name";
                cmbCostCenter.ValueMember = "code";
                cmbCostCenter.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region Search

        private void SearchAccounts()
        {
            try
            {
                GridControl1.ItemsSource = null;

                string nameFilter = string.Empty;
                if (!string.IsNullOrWhiteSpace(txtSeachName.Text))
                    nameFilter = " and AName like N'%" + txtSeachName.Text.Trim() + "%'";

                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                    "select ROW_NUMBER() OVER (ORDER BY Code) AS RowNumber, " +
                    "Code as Acc_Code, AName as Acc_Name " +
                    "from Accounts_Index " +
                    "where ParentCode=" + ParentAccountCode +
                    " and IsDeleted=0 and Type=2" + nameFilter, con);

                DataTable dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                    GridControl1.ItemsSource = dataTable.DefaultView;
                else
                    MessageBox.Show("لا توجد نتائج مطابقة.", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region Navigate

        private void Navigate(int accountCode)
        {
            try
            {
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(
                    "select Aname, isnull(CostCenter,-1) as CostCenter " +
                    "from Accounts_Index where IsDeleted=0 and Code=" + accountCode, con);

                DataTable dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                {
                    txtCode.Text = accountCode.ToString();
                    txtAccName.Text = dataTable.Rows[0]["Aname"]?.ToString() ?? string.Empty;

                    string costCenterValue = dataTable.Rows[0]["CostCenter"]?.ToString() ?? "-1";

                    if (!string.Equals(costCenterValue, "-1") &&
                        !string.Equals(costCenterValue, "0"))
                    {
                        cmbCostCenter.EditValue = costCenterValue;
                    }
                }

                XtraTabControl1.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region Button Events

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            clr();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveAccount();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MessageBox.Show("هل انت متأكد من حذف الحساب؟", "",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;

                if (string.IsNullOrWhiteSpace(txtCode.Text))
                    return;

                SqlDataAdapter checkAdapter = new SqlDataAdapter(
                    "select res_id, EntryGlobalID from Entry_sub " +
                    "where acc_no=N'" + txtCode.Text + "' " +
                    "and EntryGlobalID not in " +
                    "(select GlobalId from Entry where IS_Deleted=1)", con);

                DataTable checkTable = new DataTable();
                checkAdapter.Fill(checkTable);

                if (checkTable.Rows.Count > 0)
                {
                    string usedMsg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                        ? "هذه الحساب له ارتباطات فرعية لايمكن حذفه"
                        : "This Account previously used";
                    MessageBox.Show(usedMsg, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                EnsureConnectionOpen();

                SqlCommand deleteCommand = new SqlCommand(
                    "delete from Accounts_Index where Code='" + txtCode.Text + "'", con);
                deleteCommand.ExecuteNonQuery();

                string deleteMsg = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                    ? "تمت حذف البيانات بنجاح"
                    : "Deleted";

                string deleteTitle = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase)
                    ? "حذف"
                    : "delete";

                MessageBox.Show(deleteMsg, deleteTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                clr();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                EnsureConnectionClosed();
            }
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchAccounts();
        }

        private void RepositoryItemButtonEdit1_ButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Button clickedButton = sender as Button;
                if (clickedButton == null) return;

                DataRowView rowView = clickedButton.Tag as DataRowView;
                if (rowView == null) return;

                string accCodeText = rowView["Acc_Code"]?.ToString() ?? string.Empty;

                if (int.TryParse(accCodeText, out int accountCode))
                    Navigate(accountCode);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        #endregion

        #region Helpers

        private void EnsureConnectionOpen()
        {
            if (con.State != ConnectionState.Open)
                con.Open();
        }

        private void EnsureConnectionClosed()
        {
            if (con.State != ConnectionState.Closed)
                con.Close();
        }

        private void ShowError(string message, string title = "خطأ")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}