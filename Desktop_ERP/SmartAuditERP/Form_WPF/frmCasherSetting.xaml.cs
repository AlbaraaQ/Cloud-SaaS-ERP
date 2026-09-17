using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using SmartAuditERP.Form_WPF;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmCasherSetting : Window
    {
        #region ── Fields ─────────────────────────────────────

        private SqlConnection conn;

        public int a;

        #endregion

        #region ── Constructor ────────────────────────────────

        public frmCasherSetting()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
            a = 0;
        }

        #endregion

        #region ── Window Events ──────────────────────────────

        private void frmCasherSetting_Load(object sender, RoutedEventArgs e)
        {
            LoadUnits();
            LoadSettingsData();
            AutoCheck.Focus();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void Window_Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region ── Data Loading ───────────────────────────────

        private void LoadSettingsData()
        {
            SqlDataAdapter sqlDataAdapter =
                new SqlDataAdapter("SELECT * FROM CasherSetting", conn);

            DataTable dataTable = new DataTable();
            sqlDataAdapter.Fill(dataTable);

            if (dataTable.Rows.Count == 0)
                return;

            DataRow settingsRow = dataTable.Rows[0];

            AutoCheck.IsChecked = Convert.ToBoolean(settingsRow[0]);
            chkTouch.IsChecked = Convert.ToBoolean(settingsRow[1]);
            cmbUnits.SelectedValue = settingsRow[2];
            txtDevM.Text = settingsRow[3]?.ToString()?.Trim() ?? string.Empty;
            txtRahn.Text = settingsRow[4]?.ToString()?.Trim() ?? string.Empty;
            chkGroupsandItem.IsChecked = Convert.ToBoolean(settingsRow[6]);
        }

        private void LoadUnits()
        {
            try
            {
                SqlDataAdapter sqlDataAdapter =
                    new SqlDataAdapter("SELECT id, name FROM units ORDER BY id", conn);

                DataTable dataTable = new DataTable();
                sqlDataAdapter.Fill(dataTable);

                cmbUnits.ItemsSource = dataTable.DefaultView;
                cmbUnits.DisplayMemberPath = "name";
                cmbUnits.SelectedValuePath = "id";
                cmbUnits.SelectedIndex = -1;
            }
            catch
            {
            }
        }

        #endregion

        #region ── Save ───────────────────────────────────────

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            const string updateQuery =
                "UPDATE CasherSetting SET " +
                "AutoCheck = @AutoCheck, " +
                "TouchCheck = @TouchCheck, " +
                "Units = @Units, " +
                "txtDevM = @txtDevM, " +
                "txtRahn = @txtRahn, " +
                "ViewGroupsAndItems = @ViewGroupsAndItems " +
                "WHERE id = 1";

            SqlCommand sqlCommand = new SqlCommand(updateQuery, conn);

            sqlCommand.Parameters.Add("@AutoCheck", SqlDbType.Bit).Value =
                AutoCheck.IsChecked == true;

            sqlCommand.Parameters.Add("@TouchCheck", SqlDbType.Bit).Value =
                chkTouch.IsChecked == true;

            sqlCommand.Parameters.Add("@Units", SqlDbType.Int).Value =
                cmbUnits.SelectedIndex == -1
                    ? (object)(-1)
                    : cmbUnits.SelectedValue ?? (object)(-1);

            sqlCommand.Parameters.Add("@txtDevM", SqlDbType.NVarChar).Value =
                txtDevM.Text ?? string.Empty;

            sqlCommand.Parameters.Add("@txtRahn", SqlDbType.NVarChar).Value =
                txtRahn.Text ?? string.Empty;

            sqlCommand.Parameters.Add("@ViewGroupsAndItems", SqlDbType.Bit).Value =
                chkGroupsandItem.IsChecked == true;

            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                sqlCommand.ExecuteNonQuery();
                conn.Close();

                MessageBox.Show("تم حفظ الإعدادات بنجاح ✅",
                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                Close();
            }
            catch (Exception ex)
            {
                if (conn.State == System.Data.ConnectionState.Open)
                    conn.Close();

                MessageBox.Show("حدث خطأ في الحفظ" + Environment.NewLine + ex.Message,
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}