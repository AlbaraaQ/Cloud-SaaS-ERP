using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using DevExpress.Xpf.Core;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmViewOrders : ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private ObservableCollection<OrderRow> _orders;

        #endregion

        #region Constructor

        public frmViewOrders()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
            _orders = new ObservableCollection<OrderRow>();
            dgvSrch.ItemsSource = _orders;
        }

        #endregion

        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SearchInData();
        }

        #endregion

        #region Search

        private void txtPhoneNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            // تحديث رؤية placeholder
            placeholderSearch.Visibility = string.IsNullOrEmpty(txtPhoneNum.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;

            SearchInData();
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            SearchInData();
        }

        private void SearchInData()
        {
            try
            {
                _orders.Clear();

                string searchText = txtPhoneNum.Text.Trim();

                string sql = string.IsNullOrEmpty(searchText)
                    ? "SELECT code, name, phone_num, Date, sale_price, state FROM Inv_Tailor ORDER BY code"
                    : "SELECT code, name, phone_num, Date, sale_price, state FROM Inv_Tailor " +
                      "WHERE phone_num LIKE @search OR name LIKE @search ORDER BY code";

                using (SqlDataAdapter da = new SqlDataAdapter(sql, conn))
                {
                    if (!string.IsNullOrEmpty(searchText))
                        da.SelectCommand.Parameters.AddWithValue("@search", $"%{searchText}%");

                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    foreach (DataRow row in dt.Rows)
                    {
                        int invoiceCode = Convert.ToInt32(row["code"]);

                        double salePrice = Convert.ToDouble(row["sale_price"]);
                        double salePriceWithTax = salePrice + salePrice * 5.0 / 100.0;

                        string stateText = GetStateText(row["state"]);

                        double paidAmount = GetPaidAmount(invoiceCode);
                        double restAmount = salePriceWithTax - paidAmount;

                        _orders.Add(new OrderRow
                        {
                            code = invoiceCode,
                            name = row["name"].ToString(),
                            phone = row["phone_num"].ToString(),
                            Datee = row["Date"] != DBNull.Value
                                ? Convert.ToDateTime(row["Date"]).ToShortDateString()
                                : "",
                            Sale_price = salePriceWithTax,
                            paid = paidAmount,
                            rest = restAmount,
                            state = stateText
                        });
                    }
                }

                lblResultCount.Text = $"النتائج: {_orders.Count}";
                dgvSrch.UnselectAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في البحث: " + ex.Message, "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetStateText(object stateValue)
        {
            if (stateValue == DBNull.Value) return "غير معروف";

            if (int.TryParse(stateValue.ToString(), out int stateInt))
            {
                switch (stateInt)
                {
                    case 0: return "مستلم";
                    case 1: return "في الخياطة";
                    case 2: return "جاهز";
                    default: return "تم التسليم";
                }
            }

            return "غير معروف";
        }

        private double GetPaidAmount(int invoiceCode)
        {
            try
            {
                using (SqlDataAdapter da2 = new SqlDataAdapter(
                    "SELECT paid FROM Inv_Sub_Tailor WHERE inv_code = @code", conn))
                {
                    da2.SelectCommand.Parameters.AddWithValue("@code", invoiceCode);
                    DataTable dt2 = new DataTable();
                    da2.Fill(dt2);

                    if (dt2.Rows.Count > 0 && dt2.Rows[0]["paid"] != DBNull.Value)
                        return Convert.ToDouble(dt2.Rows[0]["paid"]);
                }
            }
            catch { }

            return 0.0;
        }

        #endregion

        #region DataGrid Events

        /// <summary>
        /// زر العرض في الجدول (بديل dgvSrch_CellClick للعمود index 8)
        /// </summary>
        private void btnViewOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is OrderRow orderRow)
            {
                try
                {
                    var AddNewSizes = new AddNewSizes();
                    AddNewSizes.Activate();
                    AddNewSizes.Show();
                    AddNewSizes.Activate();
                    AddNewSizes.showResult(orderRow.code);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خطأ في فتح التفاصيل: " + ex.Message, "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}