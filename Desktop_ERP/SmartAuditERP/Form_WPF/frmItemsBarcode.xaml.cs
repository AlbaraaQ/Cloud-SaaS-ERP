using DevExpress.Xpf.Core;
using DevExpress.XtraReports.UI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Printing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmItemsBarcode : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;

        private string PrintDocType = "Barcode";
        private string StrPrinterName = "Microsoft XPS Document Writer";
        private string ItemName = "";
        private string BarcodePrinter = "";
        private string RptName = "";
        private string RptUrl = "";
        private string _foundation = "";

        private double defVAT = 0.0;
        private bool isBold = false;

        private double pixel = 96.0;
        private int fontSize = 12;
        private int x = 1;
        private int y = 1;
        private int H = 0;
        private int W = 0;

        #endregion

        #region Constructor

        public frmItemsBarcode()
        {
            conn = MainClass.ConnObj();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmItemsBarcode_Load(object sender, RoutedEventArgs e)
        {
            LoadAllItems();
            LoadTaxSettings();
            LoadFoundationName();

            H = (int)Math.Round(pixel);
            W = (int)Math.Round(1.25 * pixel);

            LoadPrinters();
            LoadSetting();

            txtDateInit();
        }

        private void txtDateInit()
        {
            ProDate.SelectedDate = DateTime.Today;
            ExpDate.SelectedDate = DateTime.Today;
        }

        private void LoadFoundationName()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter("select * from Foundation", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                    _foundation = dt.Rows[0]["nameA"]?.ToString() ?? "";
            }
            catch { }
        }

        #endregion

        #region Load Printers

        private void LoadPrinters()
        {
            try
            {
                foreach (string printerName in PrinterSettings.InstalledPrinters)
                {
                    cmbPrinters.Items.Add(printerName);
                }

                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select RptUrl from SettingPrint where inv_id=1", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    try
                    {
                        string rptPath = Path.GetDirectoryName(dt.Rows[0]["RptUrl"]?.ToString() ?? "");

                        if (!string.IsNullOrWhiteSpace(rptPath) && Directory.Exists(rptPath))
                            RptUrl = rptPath;
                        else
                            RptUrl = MainClass.ReportsPath;
                    }
                    catch
                    {
                        RptUrl = MainClass.ReportsPath;
                    }
                }
                else
                {
                    RptUrl = MainClass.ReportsPath;
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region Load Setting

        private void LoadSetting()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select * from SettingBarcode where id=1", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count == 0) return;

                DataRow row = dt.Rows[0];

                string printer = row["BarcodePrinter"]?.ToString() ?? "";
                cmbPrinters.Text = printer;
                BarcodePrinter = printer;

                if (row["PriceInclTax"] != DBNull.Value)
                {
                    bool priceInclTax = Convert.ToBoolean(row["PriceInclTax"]);
                    chkPrice.IsChecked = priceInclTax;
                    ckPrice2.IsChecked = !priceInclTax;
                }

                if (row["PrintItemName"] != DBNull.Value)
                    ckItemNam.IsChecked = Convert.ToBoolean(row["PrintItemName"]);

                if (row["Printfoundation"] != DBNull.Value)
                    ckboxFoundName.IsChecked = Convert.ToBoolean(row["Printfoundation"]);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region Load Data

        public void LoadAllItems()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id, name from Items where IS_Deleted=0 order by id",
                    MainClass.conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbItems.ItemsSource = dt.DefaultView;
                cmbItems.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        public void LoadTaxSettings()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select MainVAT from SettingGeneral where Inv_Id=1", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    double.TryParse(dt.Rows[0]["MainVAT"]?.ToString(), out defVAT);
                }
            }
            catch { }
        }

        private void LoadItemUnits(int itemId)
        {
            try
            {
                string sql = "select ItemUnits.barcode, units.name " +
                             "from ItemUnits INNER JOIN units ON ItemUnits.unit = units.id " +
                             "where ItemUnits.barcode IS NOT NULL and ItemUnits.ItemId=" + itemId;

                SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbItemUnits.ItemsSource = dt.DefaultView;

                if (dt.Rows.Count > 0)
                    cmbItemUnits.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region ComboBox Events

        private void cmbItems_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbItems.SelectedValue != null)
                {
                    int itemId = Convert.ToInt32(cmbItems.SelectedValue);
                    LoadItemUnits(itemId);
                }
            }
            catch { }
        }

        private void cmbItemUnits_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                LookupItemDetails();
            }
            catch { }
        }

        private void cmbPrinters_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            BarcodePrinter = cmbPrinters.SelectedItem?.ToString() ?? "";
        }

        #endregion

        #region Item Details

        private void LookupItemDetails()
        {
            try
            {
                if (cmbItemUnits.SelectedValue == null || cmbItems.SelectedValue == null)
                    return;

                using SqlCommand cmd = new SqlCommand(
                    "SELECT IU.barcode, IU.sale, I.tax, I.nameEN, I.code " +
                    "FROM Items I INNER JOIN ItemUnits IU ON I.id = IU.ItemID " +
                    "WHERE IU.barcode = @Barcode AND IU.ItemID = @ItemID", conn);

                cmd.Parameters.AddWithValue("@Barcode", cmbItemUnits.SelectedValue.ToString());
                cmd.Parameters.AddWithValue("@ItemID", Convert.ToInt64(cmbItems.SelectedValue));

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];

                    txtBarcode.Text = row["barcode"]?.ToString() ?? "";
                    txtItemCode.Text = row["code"]?.ToString() ?? "";
                    txtNameEn.Text = row["nameEN"]?.ToString() ?? "";

                    double salePrice = Convert.ToDouble(row["sale"]);
                    double vatRate = 0.0;

                    int taxValue = Convert.ToInt32(row["tax"]);
                    if (taxValue != 0)
                        vatRate = defVAT / 100.0;

                    txtPrice2.Text = salePrice.ToString("F2");
                    txtPrice.Text = (salePrice + salePrice * vatRate).ToString("F2");

                    ItemName = cmbItems.Text ?? "";
                }
                else
                {
                    ClearItemFields();
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ClearItemFields()
        {
            txtBarcode.Text = "";
            txtItemCode.Text = "";
            txtNameEn.Text = "";
            txtPrice2.Text = "0.00";
            txtPrice.Text = "0.00";
        }

        #endregion

        #region Print

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(2, 1);
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(1, 1);
        }

        private void btnView2_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(2, 2);
        }

        private void btnPrint2_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(1, 2);
        }

        private void PrintDevexpress(int printType, int printNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(RptUrl))
                {
                    RptUrl = MainClass.ReportsPath;
                    return;
                }

                string rptFileName = printNo == 1 ? "Barcode.repx" : "‏‏Barcode2.repx";
                RptName = rptFileName;
                string fullPath = Path.Combine(RptUrl, RptName);

                if (!Directory.Exists(RptUrl))
                {
                    DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!File.Exists(fullPath))
                {
                    DXMessageBox.Show("ملف التقرير غير موجود", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(BarcodePrinter))
                {
                    DXMessageBox.Show("يجب تحديد الطابعة", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtQuat.Text))
                {
                    DXMessageBox.Show("يجب تحديد كمية الطباعة", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DataSet dataSource = BindToData();
                if (dataSource == null) return;

                var xtraReport = DevExpress.XtraReports.UI.XtraReport.FromFile(fullPath);
                xtraReport.DataSource = dataSource;
                xtraReport.PrinterName = BarcodePrinter;

                int qty = 1;
                int.TryParse(txtQuat.Text, out qty);

                if (printType == 1)
                {
                    for (int i = 0; i < qty; i++)
                        xtraReport.Print();
                }
                else
                {
                    xtraReport.ShowPreviewDialog();
                }

                xtraReport.Dispose();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private DataSet BindToData()
        {
            try
            {
                if (chkPrice.IsChecked == true && ckPrice2.IsChecked == true)
                {
                    DXMessageBox.Show("يجب تحديد السعر بالضريبة أو بدون", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }

                InvoiceData invoiceData = new InvoiceData();

                invoiceData.ItemName = ckItemNam.IsChecked == true
                    ? cmbItems.Text ?? ""
                    : "";

                invoiceData.ItemNameEn = txtNameEn.Text;
                invoiceData.ItemNo = txtItemCode.Text;

                string price = "";
                if (chkPrice.IsChecked == true)
                {
                    double p = 0;
                    double.TryParse(txtPrice.Text, out p);
                    price = Math.Round(p, 2).ToString("0.00");
                }
                else if (ckPrice2.IsChecked == true)
                {
                    double p = 0;
                    double.TryParse(txtPrice2.Text, out p);
                    price = Math.Round(p, 2).ToString("0.00");
                }

                invoiceData.Price = price;
                invoiceData.Barcode = txtBarcode.Text;

                if (ChkPrintExpir.IsChecked == true)
                {
                    invoiceData.ProDate = ProDate.SelectedDate?.ToShortDateString() ?? "";
                    invoiceData.ExpDate = ExpDate.SelectedDate?.ToShortDateString() ?? "";
                }
                else
                {
                    invoiceData.ProDate = "";
                    invoiceData.ExpDate = "";
                }

                invoiceData.Foundation = ckboxFoundName.IsChecked == true ? _foundation : "";

                List<InvoiceData> list = new List<InvoiceData> { invoiceData };

                DataSet dataSet = new DataSet("Name");
                DataTable table = global::UtilitiesProj.Common.ToDataTable(list);
                dataSet.Tables.Add(table);

                return dataSet;
            }
            catch (Exception ex)
            {
                ShowError(ex);
                return null;
            }
        }

        #endregion

        #region Design

        private void btnDesgin_Click(object sender, RoutedEventArgs e)
        {
            Common.OpenReportDesgin("Barcode.repx", 0);
        }

        private void btnDesgin2_Click(object sender, RoutedEventArgs e)
        {
            Common.OpenReportDesgin("‏‏Barcode2.repx", 22);
        }

        #endregion

        #region Save Settings

        private void btnSaveSetting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                int isBoldInt = isBold ? 1 : 0;
                BarcodePrinter = cmbPrinters.Text;

                int priceInclTax = chkPrice.IsChecked == true ? 1 : 0;
                int printItemName = ckItemNam.IsChecked == true ? 1 : 0;
                int printFoundation = ckboxFoundName.IsChecked == true ? 1 : 0;

                string sql = $"update SettingBarcode set " +
                             $"Width={W}, Height={H}, " +
                             $"MoveHerz={x}, MoveVerti={y}, " +
                             $"FontSize={fontSize}, FontStyle={isBoldInt}, " +
                             $"BarcodePrinter='{BarcodePrinter}', " +
                             $"PriceInclTax={priceInclTax}, " +
                             $"PrintItemName={printItemName}, " +
                             $"Printfoundation={printFoundation}";

                new SqlCommand(sql, conn).ExecuteNonQuery();

                DXMessageBox.Show("تم الحفظ", "", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadSetting();
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

        #region CheckBox Events

        private void ChkPrintExpir_Click(object sender, RoutedEventArgs e)
        {
            pnlDates.Visibility = ChkPrintExpir.IsChecked == true
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        #endregion

        #region KeyDown

        private void txtQuat_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                btnPrint_Click(null, null);
        }

        #endregion

        #region Close

        private void Button4_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Helpers

        private string GetUnitName(int id)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select name from units where id=" + id, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        private string GetDefaultPrinter()
        {
            try
            {
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    PrinterSettings ps = new PrinterSettings { PrinterName = printer };
                    if (ps.IsDefaultPrinter) return printer;
                }
            }
            catch { }
            return "";
        }

        private void ShowError(Exception ex)
        {
            DXMessageBox.Show("خطأ" + Environment.NewLine + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}