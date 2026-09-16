using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using DevExpress.Xpf.Core;
using Microsoft.Win32;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using FontFamily = System.Windows.Media.FontFamily;
using PrintDialog = System.Windows.Controls.PrintDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInvSumByType : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;

        public int invtype;

        private bool PrintHeader;
        private bool PrintFooter;
        private bool PrintStamp;
        private int PrintType;
        private int PrintNo;
        private string defPrinter;
        private string RptName;
        private string RptUrl;

        private List<InvSumByTypeRow> _invoiceRows;

        #endregion

        #region Constructor

        public frmInvSumByType()
        {
            conn = MainClass.ConnObj();
            invtype = 1;
            PrintHeader = true;
            PrintFooter = true;
            PrintStamp = true;
            PrintNo = 1;
            RptName = "";
            RptUrl = "";
            _invoiceRows = new List<InvSumByTypeRow>();

            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmInvSumByType_Load(object sender, RoutedEventArgs e)
        {
            txtDateFrom.SelectedDate = DateTime.Today;
            txtDateTo.SelectedDate = DateTime.Today;

            if (string.Equals(MainClass.Language, "en", StringComparison.OrdinalIgnoreCase))
            {
                var items = cmbType.Items;
                if (items.Count > 0)
                    ((ComboBoxItem)items[0]).Content = "postponed";
                if (items.Count > 1)
                    ((ComboBoxItem)items[1]).Content = "cash";
            }

            cmbType.SelectedIndex = 1;
            loadPrintSettings();
        }

        #endregion

        #region Filter Events

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            cmbType.IsEnabled = chkAll.IsChecked != true;
        }

        private void chkAllPeriod_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool enablePeriod = chkAllPeriod.IsChecked != true;
            txtDateFrom.IsEnabled = enablePeriod;
            txtDateTo.IsEnabled = enablePeriod;
        }

        #endregion

        #region Show Result

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            Thread workerThread = new Thread(ShowResult);
            workerThread.SetApartmentState(ApartmentState.STA);
            workerThread.IsBackground = true;
            workerThread.Start();
        }

        private void ShowResult()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    ProgressBar1.Visibility = Visibility.Visible;
                    ProgressBar1.IsIndeterminate = true;
                    dgvItems.ItemsSource = null;
                    _invoiceRows.Clear();
                    txtSumPurch.Text = "0.00";
                    txtSumSale.Text = "0.00";
                });

                string payTypeFilter = "";
                if (chkAll.IsChecked != true)
                {
                    int selectedIndex = 0;
                    Dispatcher.Invoke(() => selectedIndex = cmbType.SelectedIndex);

                    if (selectedIndex == 0)
                        payTypeFilter = " inv.pay_type=-1 and ";
                    else if (selectedIndex == 1)
                        payTypeFilter = " inv.pay_type>0 and ";
                }

                string invTypeFilter = "";
                if (invtype == 1)
                    invTypeFilter = " inv.inv_type=1 and ";
                else if (invtype == 2)
                    invTypeFilter = " (inv.inv_type=2 or inv.inv_type=3) and ";

                string branchFilter = "";
                if (MainClass.BranchNo != -1)
                    branchFilter = "inv.branch=" + MainClass.BranchNo + " and ";

                string periodFilter = "";
                bool allPeriod = true;
                Dispatcher.Invoke(() => allPeriod = chkAllPeriod.IsChecked == true);

                if (!allPeriod)
                    periodFilter = " date>=@date1 and date<=@date2 and ";

                string sql =
                    "select id, date, pay_type, InvTotal, inv_type from inv where " +
                    branchFilter + payTypeFilter + invTypeFilter +
                    " proc_type=1 and " + periodFilter + " IS_Deleted=0 order by id";

                SqlDataAdapter adapter = new SqlDataAdapter(sql, conn);

                DateTime dateFrom = DateTime.Today;
                DateTime dateTo = DateTime.Today;

                Dispatcher.Invoke(() =>
                {
                    dateFrom = txtDateFrom.SelectedDate ?? DateTime.Today;
                    dateTo = (txtDateTo.SelectedDate ?? DateTime.Today).AddHours(24);
                });

                adapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateFrom.ToShortDateString();
                adapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dateTo;

                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count == 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        ProgressBar1.IsIndeterminate = false;
                        ProgressBar1.Visibility = Visibility.Collapsed;
                        lblRowCount.Text = "عدد السجلات: 0";
                    });
                    return;
                }

                double totalPurch = 0.0;
                double totalSale = 0.0;

                List<InvSumByTypeRow> rows = new List<InvSumByTypeRow>();

                Dispatcher.Invoke(() =>
                {
                    ProgressBar1.IsIndeterminate = false;
                    ProgressBar1.Maximum = dataTable.Rows.Count;
                    ProgressBar1.Value = 0;
                });

                bool isArabic = string.Equals(MainClass.Language, "ar", StringComparison.OrdinalIgnoreCase);

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    DataRow row = dataTable.Rows[i];

                    double payTypeVal = 0;
                    double.TryParse(row["pay_type"].ToString(), out payTypeVal);

                    string payTypeText = "";
                    if (payTypeVal == -1)
                        payTypeText = isArabic ? "آجلة" : "postponed";
                    else if (payTypeVal > 0)
                        payTypeText = isArabic ? "نقدية" : "cash";

                    double invTypeVal = 0;
                    double.TryParse(row["inv_type"].ToString(), out invTypeVal);

                    int invTypeInt = invTypeVal == 1 ? 1 : (invTypeVal == 2 ? 2 : 3);

                    double invTotal = 0;
                    double.TryParse(row["InvTotal"].ToString(), out invTotal);

                    string dateText = "";
                    try
                    {
                        dateText = Convert.ToDateTime(row["date"]).ToShortDateString();
                    }
                    catch { }

                    InvSumByTypeRow newRow = new InvSumByTypeRow
                    {
                        Column1 = payTypeText,
                        Column2 = row["id"].ToString(),
                        Column3 = dateText,
                        Column4 = string.Format("{0:N2}", invTotal),
                        Column6 = string.Format("{0:N2}", invTotal),
                        Column7 = invTypeInt.ToString()
                    };

                    rows.Add(newRow);

                    totalPurch += invTotal;
                    totalSale += invTotal;

                    Dispatcher.Invoke(() => ProgressBar1.Value = i + 1);
                }

                Dispatcher.Invoke(() =>
                {
                    _invoiceRows = rows;
                    dgvItems.ItemsSource = _invoiceRows;
                    txtSumPurch.Text = string.Format("{0:N2}", totalPurch);
                    txtSumSale.Text = string.Format("{0:N2}", totalSale);
                    lblRowCount.Text = "عدد السجلات: " + rows.Count.ToString("N0");
                    ProgressBar1.IsIndeterminate = false;
                    ProgressBar1.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ShowError(ex);
                    ProgressBar1.IsIndeterminate = false;
                    ProgressBar1.Visibility = Visibility.Collapsed;
                });
            }
        }

        #endregion

        #region Grid Button

        private void btnShowInvoice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = sender as Button;
                if (btn == null) return;

                InvSumByTypeRow row = btn.Tag as InvSumByTypeRow;
                if (row == null) return;

                int invTypeInt = 0;
                int.TryParse(row.Column7, out invTypeInt);

                int invoiceId = 0;
                int.TryParse(row.Column2, out invoiceId);

                if (invTypeInt == 1)
                {
                    frmSalePurch form = new frmSalePurch();
                    form.ProcType = 1;
                    form.Show();
                    form.WindowState = WindowState.Maximized;
                    form.Navigate("select * from Inv where inv_type=1 and proc_type=1 and id=" + invoiceId);
                    form.Activate();
                }
                else if (invTypeInt == 2)
                {
                    frmSalesInvoice form = new frmSalesInvoice();
                    form.ProcType = 1;
                    form.Show();
                    form.WindowState = WindowState.Maximized;
                    form.Navigate("select * from Inv where IS_Deleted=0 and proc_type=1 and inv_type=2 and id=" + invoiceId);
                    form.Activate();
                }
                else if (invTypeInt == 3)
                {
                    frmPOS form = new frmPOS();
                    form.Show();
                    form.WindowState = WindowState.Maximized;
                    form.Navigate("select * from Inv where IS_Deleted=0 and proc_type=1 and inv_type=3 and id=" + invoiceId);
                    form.Activate();
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region Print Settings

        private void loadPrintSettings()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter("select * from SettingPrint where Inv_Id=12", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count != 1) return;

                try
                {
                    PrintType = Convert.ToInt32(dt.Rows[0]["printType"]);
                    PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                    PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                    PrintStamp = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                    defPrinter = dt.Rows[0]["CasherPrinter"] == DBNull.Value ? "" : dt.Rows[0]["CasherPrinter"].ToString();
                    if (string.IsNullOrWhiteSpace(defPrinter))
                        defPrinter = Common.GetDefaultPrinter();
                    PrintNo = Convert.ToInt32(dt.Rows[0]["printNo"]);
                }
                catch { }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region Buttons

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(2);
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDevexpress(1);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Print / Preview

        private void PrintDevexpress(int printMode)
        {
            try
            {
                RptUrl = MainClass.ReportsPath;
                RptName = "rptInvTypeClient.repx";
                defPrinter = MainClass.ReportsPrinter;

                if (_invoiceRows == null || _invoiceRows.Count == 0)
                {
                    DXMessageBox.Show("لا توجد عمليات بالجدول", "",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (string.IsNullOrWhiteSpace(RptUrl))
                {
                    DXMessageBox.Show("يجب تحديد مسار التقرير", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string fullPath = Path.Combine(RptUrl, RptName);
                if (!Directory.Exists(RptUrl) || !File.Exists(fullPath))
                {
                    DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                FlowDocument document = BuildPrintDocument();

                if (printMode == 2)
                {
                    ShowPreviewWindow(document);
                }
                else
                {
                    PrintDialog printDialog = new PrintDialog();
                    if (printDialog.ShowDialog() == true)
                    {
                        document.PageHeight = printDialog.PrintableAreaHeight;
                        document.PageWidth = printDialog.PrintableAreaWidth;
                        document.PagePadding = new Thickness(25);
                        document.ColumnGap = 0;
                        document.ColumnWidth = printDialog.PrintableAreaWidth;

                        IDocumentPaginatorSource source = document;
                        printDialog.PrintDocument(source.DocumentPaginator, Title);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private FlowDocument BuildPrintDocument()
        {
            FlowDocument document = new FlowDocument
            {
                FlowDirection = System.Windows.FlowDirection.RightToLeft,
                FontFamily = new FontFamily("Tahoma"),
                FontSize = 11,
                PagePadding = new Thickness(20),
                ColumnWidth = 900
            };

            Paragraph title = new Paragraph(new Run(Title))
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#213A7A"))
            };
            document.Blocks.Add(title);

            string periodText = chkAllPeriod.IsChecked == true
                ? "كل الفترة"
                : "من " + (txtDateFrom.SelectedDate?.ToShortDateString() ?? "")
                  + " إلى " + (txtDateTo.SelectedDate?.ToShortDateString() ?? "");

            Paragraph info = new Paragraph(new Run("📅 الفترة: " + periodText))
            {
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            document.Blocks.Add(info);

            Table table = new Table { CellSpacing = 0 };

            for (int i = 0; i < 5; i++)
                table.Columns.Add(new TableColumn());

            TableRowGroup rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            TableRow headerRow = new TableRow();
            rowGroup.Rows.Add(headerRow);
            AddHeaderCell(headerRow, "نوع الفاتورة");
            AddHeaderCell(headerRow, "رقم الفاتورة");
            AddHeaderCell(headerRow, "التاريخ");
            AddHeaderCell(headerRow, "إجمالي المشتريات");
            AddHeaderCell(headerRow, "إجمالي المبيعات");

            foreach (InvSumByTypeRow item in _invoiceRows)
            {
                TableRow row = new TableRow();
                rowGroup.Rows.Add(row);
                AddBodyCell(row, item.Column1);
                AddBodyCell(row, item.Column2);
                AddBodyCell(row, item.Column3);
                AddBodyCell(row, item.Column4);
                AddBodyCell(row, item.Column6);
            }

            document.Blocks.Add(table);

            Paragraph totals = new Paragraph
            {
                Margin = new Thickness(0, 12, 0, 0),
                FontWeight = FontWeights.Bold
            };
            totals.Inlines.Add(new Run("📦 إجمالي المشتريات: " + txtSumPurch.Text + "    "));
            totals.Inlines.Add(new Run("📈 إجمالي المبيعات: " + txtSumSale.Text));
            document.Blocks.Add(totals);

            return document;
        }

        private void ShowPreviewWindow(FlowDocument document)
        {
            System.Windows.Window previewWindow = new System.Windows.Window
            {
                Title = "معاينة قبل الطباعة",
                Width = 1000,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                FlowDirection = System.Windows.FlowDirection.RightToLeft,
                Background = Brushes.White
            };

            DocumentViewer viewer = new DocumentViewer { Document = document };
            previewWindow.Content = viewer;
            previewWindow.ShowDialog();
        }

        private void AddHeaderCell(TableRow row, string text)
        {
            TableCell cell = new TableCell(new Paragraph(new Run(text)))
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDE7FF")),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(5)
            };
            cell.Blocks.FirstBlock.SetValue(TextElement.FontWeightProperty, FontWeights.Bold);
            cell.Blocks.FirstBlock.SetValue(Block.TextAlignmentProperty, TextAlignment.Center);
            row.Cells.Add(cell);
        }

        private void AddBodyCell(TableRow row, string text)
        {
            TableCell cell = new TableCell(new Paragraph(new Run(text ?? "")))
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(4)
            };
            cell.Blocks.FirstBlock.SetValue(Block.TextAlignmentProperty, TextAlignment.Center);
            row.Cells.Add(cell);
        }

        #endregion

        #region Export

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_invoiceRows == null || _invoiceRows.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للتصدير.", "",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    FileName = "إجمالي_فواتير_بحسب_النوع",
                    Filter = "Excel File (*.xls)|*.xls|CSV File (*.csv)|*.csv",
                    DefaultExt = ".xls"
                };

                if (saveDialog.ShowDialog() != true) return;

                if (saveDialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    ExportToCsv(saveDialog.FileName);
                else
                    ExportToExcelHtml(saveDialog.FileName);

                Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ExportToCsv(string fileName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("نوع الفاتورة,رقم الفاتورة,التاريخ,إجمالي المشتريات,إجمالي المبيعات");

            foreach (InvSumByTypeRow item in _invoiceRows)
            {
                sb.AppendLine(
                    CsvVal(item.Column1) + "," +
                    CsvVal(item.Column2) + "," +
                    CsvVal(item.Column3) + "," +
                    CsvVal(item.Column4) + "," +
                    CsvVal(item.Column6));
            }

            File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);
        }

        private void ExportToExcelHtml(string fileName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<html><head><meta charset='utf-8'/>");
            sb.AppendLine("<style>table{border-collapse:collapse;font-family:Tahoma;font-size:12px;}");
            sb.AppendLine("th,td{border:1px solid #999;padding:5px;text-align:center;}");
            sb.AppendLine("th{background:#DDE7FF;font-weight:bold;}</style></head>");
            sb.AppendLine("<body dir='rtl'>");
            sb.AppendLine("<h2 style='text-align:center;'>" + Title + "</h2>");
            sb.AppendLine("<table><tr>");
            sb.AppendLine("<th>نوع الفاتورة</th><th>رقم الفاتورة</th><th>التاريخ</th><th>إجمالي المشتريات</th><th>إجمالي المبيعات</th>");
            sb.AppendLine("</tr>");

            foreach (InvSumByTypeRow item in _invoiceRows)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine("<td>" + HtmlVal(item.Column1) + "</td>");
                sb.AppendLine("<td>" + HtmlVal(item.Column2) + "</td>");
                sb.AppendLine("<td>" + HtmlVal(item.Column3) + "</td>");
                sb.AppendLine("<td>" + HtmlVal(item.Column4) + "</td>");
                sb.AppendLine("<td>" + HtmlVal(item.Column6) + "</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table><br/>");
            sb.AppendLine("<b>إجمالي المشتريات: " + txtSumPurch.Text + " | إجمالي المبيعات: " + txtSumSale.Text + "</b>");
            sb.AppendLine("</body></html>");

            File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);
        }

        #endregion

        #region Helpers

        private string GetEmpName(int emp)
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter("select name from Employees where id=" + emp, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0][0].ToString() : "";
            }
            catch { return ""; }
        }

        private string CsvVal(string text)
        {
            return "\"" + (text ?? "").Replace("\"", "\"\"") + "\"";
        }

        private string HtmlVal(string text)
        {
            return (text ?? "")
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        private void ShowError(Exception ex)
        {
            DXMessageBox.Show(
                "خطأ" + Environment.NewLine + "تفاصيل الخطأ: " + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }

    // ─── Model للصف ───────────────────────────────────────────────────
    public class InvSumByTypeRow
    {
        public string Column1 { get; set; }
        public string Column2 { get; set; }
        public string Column3 { get; set; }
        public string Column4 { get; set; }
        public string Column6 { get; set; }
        public string Column7 { get; set; }
    }
}