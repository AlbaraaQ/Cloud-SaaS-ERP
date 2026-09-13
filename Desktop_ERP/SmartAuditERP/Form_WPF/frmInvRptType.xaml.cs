using DevExpress.Xpf.Core;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInvRptType : DevExpress.Xpf.Core.ThemedWindow
    {
        // ══════════════════════════════════════════════════════════
        #region Private Fields

        private readonly SqlConnection _conn;

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Constructor

        public frmInvRptType()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Button Events

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_conn.State != ConnectionState.Open)
                    _conn.Open();

                int printTypeVal = RadioButton2.IsChecked == true ? 2 : 1;

                string updateQuery = $"UPDATE sett SET val = {printTypeVal} WHERE id = 1";
                new SqlCommand(updateQuery, _conn).ExecuteNonQuery();

                DXMessageBox.Show(
                    "تم الحفظ بنجاح ✅",
                    "حفظ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    $"خطأ في الحفظ:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                if (_conn.State == ConnectionState.Open)
                    _conn.Close();
            }
        }

        #endregion
    }
}