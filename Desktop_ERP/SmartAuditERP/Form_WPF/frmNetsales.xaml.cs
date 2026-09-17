using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using FontFamily = System.Windows.Media.FontFamily;
using PrintDialog = System.Windows.Controls.PrintDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmNetsales : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private SqlConnection conn;
        private bool   PrintHeader = true;
        private bool   PrintFooter = true;
        private bool   PrintStamp  = true;
        private int    PrintType   = 0;
        private int    PrintNo     = 1;
        private string defPrinter  = "";
        private string RptName     = "";
        private string RptUrl      = "";

        private ObservableCollection<NetSalesRow> _rows;

        #endregion

        #region Constructor

        public frmNetsales()
        {
            conn  = MainClass.ConnObj();
            _rows = new ObservableCollection<NetSalesRow>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmNetsales_Load(object sender, RoutedEventArgs e)
        {
            txtDateFrom.SelectedDate = DateTime.Today;
            txtDateTo.SelectedDate   = DateTime.Today;

            dgvItems.ItemsSource = _rows;

            LoadItems();
            LoadPrintSettings();
        }

        public void LoadItems()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter(
                    "select id,name from Items where IS_Deleted=0 order by id", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                cmbCurrency.ItemsSource = dt.DefaultView;
                cmbCurrency.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        #endregion

        #region CheckBox

        private void chkAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (cmbCurrency == null) return;
            cmbCurrency.IsEnabled = chkAll.IsChecked != true;
        }

        #endregion

        #region Show Result

        private void btnView_Click(object sender, RoutedEventArgs e)
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
                bool allSelected  = false;
                object selectedId = null;

                Dispatcher.Invoke(() =>
                {
                    allSelected  = chkAll.IsChecked == true;
                    selectedId   = cmbCurrency.SelectedValue;
                });

                if (!allSelected && selectedId == null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        DXMessageBox.Show("يجب اختيار الصنف", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                        cmbCurrency.Focus();
                    });
                    return;
                }

                string itemSql = "select id,name from Items where IS_Deleted=0";
                if (!allSelected)
                    itemSql += $" and id={selectedId}";
                itemSql += " order by id";

                SqlDataAdapter itemAdapter = new SqlDataAdapter(itemSql, conn);
                DataTable itemsDt = new DataTable();
                itemAdapter.Fill(itemsDt);

                string branchFilter = MainClass.BranchNo != -1
                    ? $"inv.branch={MainClass.BranchNo} and "
                    : "";

                double totalSales = 0.0, totalCost = 0.0, totalProfit = 0.0;

                DateTime dtFrom = DateTime.Today, dtTo = DateTime.Today;
                Dispatcher.Invoke(() =>
                {
                    dtFrom = txtDateFrom.SelectedDate ?? DateTime.Today;
                    dtTo   = (txtDateTo.SelectedDate ?? DateTime.Today).AddHours(24);
                });

                Dispatcher.Invoke(() =>
                {
                    _rows.Clear();
                    ProgressBar1.Visibility = Visibility.Visible;
                    ProgressBar1.Maximum    = itemsDt.Rows.Count;
                    ProgressBar1.Value      = 0;
                });

                for (int i = 0; i < itemsDt.Rows.Count; i++)
                {
                    object itemId   = itemsDt.Rows[i]["id"];
                    string itemName = itemsDt.Rows[i]["name"].ToString();

                    SqlDataAdapter saleAdapter = new SqlDataAdapter(
                        $"select inv.id, inv.date, val as val, (val1*exchange_price) as sum " +
                        $"from inv,inv_sub where {branchFilter} Inv_Sub.ItemId={itemId} " +
                        $"and date>=@date1 and date<=@date2 " +
                        $"and inv.proc_type=1 and ((inv_sub.proc_type=2 and inv.inv_type=2) or (inv_sub.proc_type=3 and inv.inv_type=3)) " +
                        $"and inv.InvGlobalID=inv_sub.InvGlobalID and IS_Deleted=0", conn);
                    saleAdapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dtFrom.ToShortDateString();
                    saleAdapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dtTo;
                    DataTable saleDt = new DataTable();
                    saleAdapter.Fill(saleDt);

                    if (saleDt.Rows.Count > 0)
                    {
                        for (int j = 0; j < saleDt.Rows.Count; j++)
                        {
                            double returnQty = 0, returnSum = 0;
                            double saleQty   = 0, saleSum   = 0;

                            double.TryParse(saleDt.Rows[j]["val"].ToString(), out saleQty);
                            double.TryParse(saleDt.Rows[j]["sum"].ToString(), out saleSum);
                            totalSales += saleSum;

                            SqlDataAdapter returnAdapter = new SqlDataAdapter(
                                $"select val as val, (val1*exchange_price) as sum from inv,inv_sub " +
                                $"where Reff_No={saleDt.Rows[j]["id"]} and Inv_Sub.ItemId={itemId} " +
                                $"and date>=@date1 and date<=@date2 " +
                                $"and inv.proc_type=2 and ((inv_sub.proc_type=2 and inv.inv_type=2) or (inv_sub.proc_type=3 and inv.inv_type=3)) " +
                                $"and inv.InvGlobalID=inv_sub.InvGlobalID and IS_Deleted=0", conn);
                            returnAdapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dtFrom.ToShortDateString();
                            returnAdapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = dtTo;
                            DataTable returnDt = new DataTable();
                            returnAdapter.Fill(returnDt);

                            for (int k = 0; k < returnDt.Rows.Count; k++)
                            {
                                double rqty = 0, rsum = 0;
                                double.TryParse(returnDt.Rows[k]["val"].ToString(), out rqty);
                                double.TryParse(returnDt.Rows[k]["sum"].ToString(), out rsum);
                                returnQty += rqty;
                                returnSum += rsum;
                            }

                            saleQty  -= returnQty;
                            saleSum  -= returnSum;
                            totalSales -= returnSum;

                            if (saleQty == 0) continue;

                            double totalPurchCost = 0;
                            int    totalPurchQty  = 0;
                            double avgCost        = 0;

                            SqlDataAdapter purchAdapter = new SqlDataAdapter(
                                $"select sum(val),sum(val1*exchange_price) from inv,inv_sub " +
                                $"where {branchFilter} ItemId={itemId} " +
                                $"and date<=@date2 and inv.inv_type=1 and inv.proc_type=1 " +
                                $"and inv_sub.proc_type=1 and inv.InvGlobalID=inv_sub.InvGlobalID and IS_Deleted=0", conn);
                            purchAdapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = Convert.ToDateTime(saleDt.Rows[j]["date"]);
                            DataTable purchDt = new DataTable();
                            purchAdapter.Fill(purchDt);

                            if (purchDt.Rows.Count > 0 && !string.IsNullOrEmpty(purchDt.Rows[0][1]?.ToString()))
                            {
                                double.TryParse(purchDt.Rows[0][1].ToString(), out totalPurchCost);
                                int.TryParse(purchDt.Rows[0][0].ToString(), out totalPurchQty);
                            }

                            SqlDataAdapter adjAdapter = new SqlDataAdapter(
                                $"select sum(val),sum(val1*exchange_price) from inv,inv_sub " +
                                $"where {branchFilter} ItemId={itemId} " +
                                $"and date<=@date2 and inv.inv_type=1 and inv.proc_type=3 " +
                                $"and inv.InvGlobalID=inv_sub.InvGlobalID and IS_Deleted=0", conn);
                            adjAdapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = Convert.ToDateTime(saleDt.Rows[j]["date"]);
                            DataTable adjDt = new DataTable();
                            adjAdapter.Fill(adjDt);

                            if (adjDt.Rows.Count > 0 && !string.IsNullOrEmpty(adjDt.Rows[0][1]?.ToString()))
                            {
                                double.TryParse(adjDt.Rows[0][1].ToString(), out double adjCost);
                                int.TryParse(adjDt.Rows[0][0].ToString(), out int adjQty);
                                totalPurchCost += adjCost;
                                totalPurchQty  += adjQty;
                            }

                            try
                            {
                                if (totalPurchQty != 0)
                                    avgCost = Math.Floor(totalPurchCost / totalPurchQty * 100000000.0) / 100000000.0;
                                else
                                {
                                    SqlDataAdapter lastPriceAdapter = new SqlDataAdapter(
                                        $"select purch_price from Currency_Lastprice_Sub where currency1={itemId}", conn);
                                    DataTable lastPriceDt = new DataTable();
                                    lastPriceAdapter.Fill(lastPriceDt);
                                    if (lastPriceDt.Rows.Count > 0)
                                        double.TryParse(lastPriceDt.Rows[0][0].ToString(), out avgCost);
                                }

                                double itemCostTotal = Math.Round(saleQty * avgCost, 2);
                                double itemProfit    = saleSum - itemCostTotal;
                                totalCost   += itemCostTotal;
                                totalProfit += Math.Round(itemProfit, 2);

                                Dispatcher.Invoke(() =>
                                {
                                    _rows.Add(new NetSalesRow
                                    {
                                        Column1 = itemsDt.Rows[i][0].ToString(),
                                        Column2 = itemName,
                                        Column3 = $"{saleQty:0.####}",
                                        Column4 = $"{saleSum:0.####}",
                                        Column5 = $"{itemCostTotal:0.####}",
                                        Column6 = $"{Math.Round(itemProfit, 2):0.####}"
                                    });

                                    UpdateRowCount();
                                });
                            }
                            catch (Exception ex)
                            {
                                ShowError(ex);
                            }
                        }
                    }

                    Dispatcher.Invoke(() => ProgressBar1.Value++);
                }

                Dispatcher.Invoke(() =>
                {
                    txtVal2.Text = $"{totalSales:0.####}";
                    txtVal3.Text = $"{totalCost:0.####}";
                    txtVal4.Text = $"{totalProfit:0.####}";
                    ProgressBar1.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowError(ex));
            }
        }

        #endregion

        #region Print & Export

        private void btnPreview_Click(object sender, RoutedEventArgs e) { PrintDevexpress(2); }
        private void btnPrint_Click(object sender, RoutedEventArgs e)   { PrintDevexpress(1); }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_rows.Count == 0)
                {
                    DXMessageBox.Show("لا توجد بيانات للتصدير", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    FileName   = "صافي_المبيعات",
                    Filter     = "Excel File (*.xls)|*.xls|CSV File (*.csv)|*.csv",
                    DefaultExt = ".xls"
                };

                if (saveDialog.ShowDialog() != true) return;

                ExportToExcelHtml(saveDialog.FileName);
                Process.Start(new ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ExportToExcelHtml(string fileName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<html><head><meta charset='utf-8'/>");
            sb.AppendLine("<style>table{border-collapse:collapse;font-family:Tahoma;font-size:11px;}");
            sb.AppendLine("th,td{border:1px solid #999;padding:4px;text-align:center;}");
            sb.AppendLine("th{background:#DDE7FF;font-weight:bold;}</style></head>");
            sb.AppendLine("<body dir='rtl'>");
            sb.AppendLine($"<h2 style='text-align:center;'>{Title}</h2>");
            sb.AppendLine("<table><tr>");
            sb.AppendLine("<th>رقم الصنف</th><th>الصنف</th><th>الكمية المباعة</th>");
            sb.AppendLine("<th>إجمالي قيمة المبيعات</th><th>إجمالي تكلفة المبيعات</th><th>الأرباح</th>");
            sb.AppendLine("</tr>");

            foreach (NetSalesRow row in _rows)
            {
                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{row.Column1}</td><td>{row.Column2}</td><td>{row.Column3}</td>");
                sb.AppendLine($"<td>{row.Column4}</td><td>{row.Column5}</td><td>{row.Column6}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine($"<tr><td colspan='3'><b>الإجمالي</b></td><td>{txtVal2.Text}</td><td>{txtVal3.Text}</td><td>{txtVal4.Text}</td></tr>");
            sb.AppendLine("</table></body></html>");

            File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);
        }

        private void PrintDevexpress(int printMode)
        {
            try
            {
                RptUrl     = MainClass.ReportsPath;
                RptName    = "rptNetsales.repx";
                defPrinter = MainClass.ReportsPrinter;

                if (_rows.Count == 0)
                {
                    DXMessageBox.Show("لا توجد عمليات بالجدول", "", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (string.IsNullOrWhiteSpace(RptUrl))
                {
                    DXMessageBox.Show("يجب تحديد مسار التقرير", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string fullPath = Path.Combine(RptUrl, RptName);
                if (!Directory.Exists(RptUrl) || !File.Exists(fullPath))
                {
                    DXMessageBox.Show("المسار الحالي للتقارير غير موجود أو تم تعديله", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                FlowDocument document = BuildPrintDocument();

                if (printMode == 2)
                {
                    System.Windows.Window pw = new System.Windows.Window
                    {
                        Title                 = "معاينة قبل الطباعة",
                        Width                 = 950,
                        Height                = 700,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        FlowDirection         = System.Windows.FlowDirection.RightToLeft,
                        Background            = System.Windows.Media.Brushes.White
                    };
                    pw.Content = new DocumentViewer { Document = document };
                    pw.ShowDialog();
                }
                else
                {
                    PrintDialog pd = new PrintDialog();
                    if (pd.ShowDialog() == true)
                    {
                        document.PageHeight  = pd.PrintableAreaHeight;
                        document.PageWidth   = pd.PrintableAreaWidth;
                        document.PagePadding = new Thickness(25);
                        document.ColumnGap   = 0;
                        document.ColumnWidth = pd.PrintableAreaWidth;

                        IDocumentPaginatorSource src = document;
                        for (int i = 0; i < PrintNo; i++)
                            pd.PrintDocument(src.DocumentPaginator, Title);
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
            FlowDocument doc = new FlowDocument
            {
                FlowDirection = System.Windows.FlowDirection.RightToLeft,
                FontFamily    = new FontFamily("Tahoma"),
                FontSize      = 9,
                PagePadding   = new Thickness(15),
                ColumnWidth   = 900
            };

            doc.Blocks.Add(new Paragraph(new Run(Title))
            {
                FontSize = 14, FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#213A7A"))
            });

            string period = $"من {txtDateFrom.SelectedDate?.ToShortDateString()} إلى {txtDateTo.SelectedDate?.ToShortDateString()}";
            doc.Blocks.Add(new Paragraph(new Run($"📅 الفترة: {period}"))
            {
                FontSize = 11, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10)
            });

            Table table = new Table { CellSpacing = 0 };
            for (int i = 0; i < 6; i++)
                table.Columns.Add(new TableColumn());

            TableRowGroup group = new TableRowGroup();
            table.RowGroups.Add(group);

            TableRow headerRow = new TableRow();
            group.Rows.Add(headerRow);
            AddHeaderCell(headerRow, "رقم الصنف");
            AddHeaderCell(headerRow, "الصنف");
            AddHeaderCell(headerRow, "الكمية المباعة");
            AddHeaderCell(headerRow, "إجمالي المبيعات");
            AddHeaderCell(headerRow, "إجمالي التكلفة");
            AddHeaderCell(headerRow, "الأرباح");

            foreach (NetSalesRow row in _rows)
            {
                TableRow dataRow = new TableRow();
                group.Rows.Add(dataRow);
                AddBodyCell(dataRow, row.Column1);
                AddBodyCell(dataRow, row.Column2);
                AddBodyCell(dataRow, row.Column3);
                AddBodyCell(dataRow, row.Column4);
                AddBodyCell(dataRow, row.Column5);
                AddBodyCell(dataRow, row.Column6);
            }

            doc.Blocks.Add(table);

            Paragraph summary = new Paragraph { Margin = new Thickness(0, 10, 0, 0), FontWeight = FontWeights.Bold };
            summary.Inlines.Add(new Run($"إجمالي المبيعات: {txtVal2.Text}   |   إجمالي التكلفة: {txtVal3.Text}   |   إجمالي الأرباح: {txtVal4.Text}"));
            doc.Blocks.Add(summary);

            return doc;
        }

        private void AddHeaderCell(TableRow row, string text)
        {
            TableCell cell = new TableCell(new Paragraph(new Run(text)))
            {
                Background      = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDE7FF")),
                BorderBrush     = System.Windows.Media.Brushes.Gray,
                BorderThickness = new Thickness(0.5),
                Padding         = new Thickness(4)
            };
            cell.Blocks.FirstBlock.SetValue(TextElement.FontWeightProperty, FontWeights.Bold);
            cell.Blocks.FirstBlock.SetValue(Block.TextAlignmentProperty, TextAlignment.Center);
            row.Cells.Add(cell);
        }

        private void AddBodyCell(TableRow row, string text)
        {
            TableCell cell = new TableCell(new Paragraph(new Run(text ?? "")))
            {
                BorderBrush     = System.Windows.Media.Brushes.Gray,
                BorderThickness = new Thickness(0.5),
                Padding         = new Thickness(3)
            };
            cell.Blocks.FirstBlock.SetValue(Block.TextAlignmentProperty, TextAlignment.Center);
            row.Cells.Add(cell);
        }

        private void LoadPrintSettings()
        {
            try
            {
                SqlDataAdapter adapter = new SqlDataAdapter("select * from SettingPrint where Inv_Id=12", conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                if (dt.Rows.Count != 1) return;

                PrintType   = Convert.ToInt32(dt.Rows[0]["printType"]);
                PrintFooter = Convert.ToBoolean(dt.Rows[0]["PrintFooter"]);
                PrintHeader = Convert.ToBoolean(dt.Rows[0]["PrintHeader"]);
                PrintStamp  = Convert.ToBoolean(dt.Rows[0]["PrintStamp"]);
                defPrinter  = dt.Rows[0]["CasherPrinter"]?.ToString() ?? "";

                if (string.IsNullOrEmpty(defPrinter))
                    defPrinter = Common.GetDefaultPrinter();

                PrintNo = Convert.ToInt32(dt.Rows[0]["printNo"]);
            }
            catch { }
        }

        #endregion

        #region Close & Helpers

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void UpdateRowCount()
        {
            lblRowCount.Text = $"عدد السجلات: {_rows.Count:N0}";
        }

        private void ShowError(Exception ex)
        {
            DXMessageBox.Show("خطأ" + Environment.NewLine + ex.Message,
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }

    public class NetSalesRow
    {
        public string Column1 { get; set; }
        public string Column2 { get; set; }
        public string Column3 { get; set; }
        public string Column4 { get; set; }
        public string Column5 { get; set; }
        public string Column6 { get; set; }
    }
}