using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AuditorAPI.Models.DGVmodels;
using DevExpress.Xpf.Core;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInvoiceItems : DevExpress.Xpf.Core.ThemedWindow
    {
        // ══════════════════════════════════════════════════════════
        #region Public Fields

        public int selectedCli;
        public int StoreId;
        public string sql;
        public string Cond;
        public string InvGlobalId;
        public InvoiceDGV inv;
        public Invoicecontract invcontract;
        public int ItemID;
        public int InvType;
        public bool ISDone;
        public int RowIndex;

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Private Fields

        private readonly SqlConnection _conn;
        private DataTable _dt;
        private ObservableCollection<InvoiceItemRow> _rows;

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Constructor

        public frmInvoiceItems()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            StoreId = 1;
            InvGlobalId = "1";
            inv = new InvoiceDGV();
            invcontract = new Invoicecontract();
            ItemID = -1;
            InvType = 1;
            ISDone = false;
            RowIndex = -1;
            _dt = new DataTable();
            _rows = new ObservableCollection<InvoiceItemRow>();

            GridControl1.ItemsSource = _rows;
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDgvStyle();
            LoadData();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CloseWithoutResult();
            }
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Header Events

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            CloseWithoutResult();
        }

        private void btnadd_Click(object sender, RoutedEventArgs e)
        {
            InsertSelectedItems();

            if (MainClass.ReturnYearPreviews)
                MainClass.connstr = MainClass.originalConnStr;
        }

        private void btnAddall_Click(object sender, RoutedEventArgs e)
        {
            ISDone = true;

            if (MainClass.ReturnYearPreviews)
            {
                MainClass.connstr = MainClass.originalConnStr;
                MainClass.ReturnYearPreviews = false;
            }

            Close();
        }

        private void txtSrchDgv_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return) return;

            string barcode = txtSrchDgv.Text.Trim();
            var found = _rows.FirstOrDefault(r =>
                r.ItemBarcode == barcode || r.ItemCode == barcode);

            if (found != null)
            {
                GridControl1.SelectedItem = found;
                GridControl1.ScrollIntoView(found);
                txtSrchDgv.Text = string.Empty;
            }
            else
            {
                DXMessageBox.Show("غير موجود", "بحث",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Grid Events

        private void GridControl1_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header?.ToString() != "الكمية")
                return;

            if (e.Row.Item is not InvoiceItemRow row)
                return;

            if (e.EditingElement is not TextBox tb)
                return;

            if (!double.TryParse(tb.Text, out double enteredQty))
            {
                DXMessageBox.Show("يجب إدخال أرقام فقط.", "تحقق",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                tb.Text = row.RowQty.ToString();
                return;
            }

            if (enteredQty > row.RowQty)
            {
                DXMessageBox.Show(
                    MainClass.Language == "en"
                        ? "It is not possible to return more than the quantity on the invoice."
                        : "لا يمكن إرجاع بأكثر من كميته في الفاتورة.",
                    "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);

                tb.Text = row.RowQty.ToString();
                row.ItemQty = row.RowQty;
                return;
            }

            // تحديث الخصم تلقائياً بناءً على الكمية الجديدة
            if (row.RowQty != 0 && row.OriginalDisc != 0)
            {
                row.ItemDiscount = row.OriginalDisc / row.RowQty * enteredQty;
            }
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Data Loading

        public void LoadData()
        {
            try
            {
                _rows.Clear();
                GridControl1.ItemsSource = null;

                string query = BuildLoadQuery();
                var table = new DataTable();
                new SqlDataAdapter(query, _conn).Fill(table);

                foreach (DataRow row in table.Rows)
                {
                    _rows.Add(new InvoiceItemRow
                    {
                        RowNo = row["RowNo"] == DBNull.Value ? 0 : Convert.ToInt32(row["RowNo"]),
                        InvGlobalID = row["InvGlobalID"]?.ToString() ?? "",
                        Item_ID = row["Item_ID"] == DBNull.Value ? 0 : Convert.ToInt32(row["Item_ID"]),
                        ItemQty = row["ItemQty"] == DBNull.Value ? 0 : Convert.ToDouble(row["ItemQty"]),
                        RowQty = row["RowQty"] == DBNull.Value ? 0 : Convert.ToDouble(row["RowQty"]),
                        ItemPrice = row["ItemPrice"] == DBNull.Value ? 0 : Convert.ToDouble(row["ItemPrice"]),
                        ItemVAT = row["ItemVAT"] == DBNull.Value ? 0 : Convert.ToDouble(row["ItemVAT"]),
                        ItemDiscount = row["ItemDiscount"] == DBNull.Value ? 0 : Convert.ToDouble(row["ItemDiscount"]),
                        OriginalDisc = row["originalDisc"] == DBNull.Value ? 0 : Convert.ToDouble(row["originalDisc"]),
                        ItemUnit = row["ItemUnit"]?.ToString() ?? "",
                        ItemName = row["ItemName"]?.ToString() ?? "",
                        ItemCode = row["ItemCode"]?.ToString() ?? "",
                        ItemBarcode = row["ItemBarcode"]?.ToString() ?? "",
                        ItemNet = row["ItemNet"] == DBNull.Value ? 0 : Convert.ToDouble(row["ItemNet"])
                    });
                }

                GridControl1.ItemsSource = _rows;
                UpdateStatus();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    $"خطأ في تحميل البيانات:\n{ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string BuildLoadQuery()
        {
            if (InvType == 23)
            {
                return $@"
                    SELECT ROW_NUMBER() OVER(ORDER BY inv_sub.id ASC) AS RowNo,
                           Inv_sub.InvGlobalID,
                           inv_sub.ItemId   AS Item_ID,
                           val1             AS ItemQty,
                           exchange_price   AS ItemPrice,
                           taxval           AS ItemVAT,
                           inv_sub.discount AS ItemDiscount,
                           inv_sub.discount AS originalDisc,
                           units.name       AS ItemUnit,
                           items.name       AS ItemName,
                           items.code       AS ItemCode,
                           ItemUnits.barcode AS ItemBarcode,
                           IIF(inv.PriceIncVAT = 0,
                               (val1 * exchange_price) + taxval - inv_sub.discount,
                               (val1 * exchange_price) - inv_sub.discount) AS ItemNet,
                           Inv_sub.val1     AS RowQty
                    FROM   InvContratct_Sub inv_sub
                    LEFT JOIN units    ON Inv_Sub.unit    = units.Id
                    LEFT JOIN ItemUnits ON Inv_Sub.ItemId = ItemUnits.ItemId
                              AND Inv_Sub.unit = ItemUnits.unit
                    LEFT JOIN InvContratct Inv ON Inv_Sub.InvGlobalID = inv.InvGlobalID
                    LEFT JOIN items ON Inv_sub.ItemId = items.id
                    WHERE  Inv_Sub.InvGlobalID = N'{InvGlobalId}'
                      AND  Inv_Sub.ProductId   = 0";
            }

            return $@"
                SELECT ROW_NUMBER() OVER(ORDER BY inv_sub.id ASC) AS RowNo,
                       Inv_sub.InvGlobalID,
                       inv_sub.ItemId   AS Item_ID,
                       val1             AS ItemQty,
                       exchange_price   AS ItemPrice,
                       taxval           AS ItemVAT,
                       inv_sub.discount AS ItemDiscount,
                       inv_sub.discount AS originalDisc,
                       units.name       AS ItemUnit,
                       items.name       AS ItemName,
                       items.code       AS ItemCode,
                       ItemUnits.barcode AS ItemBarcode,
                       IIF(inv.PriceIncVAT = 0,
                           (val1 * exchange_price) + taxval - inv_sub.discount,
                           (val1 * exchange_price) - inv_sub.discount) AS ItemNet,
                       Inv_sub.val1     AS RowQty
                FROM   Inv_Sub
                LEFT JOIN units    ON Inv_Sub.unit    = units.Id
                LEFT JOIN ItemUnits ON Inv_Sub.ItemId = ItemUnits.ItemId
                          AND Inv_Sub.unit = ItemUnits.unit
                LEFT JOIN Inv   ON Inv_Sub.InvGlobalID = inv.InvGlobalID
                LEFT JOIN items ON Inv_sub.ItemId = items.id
                WHERE  Inv_Sub.InvGlobalID = N'{InvGlobalId}'
                  AND  Inv_Sub.ProductId   = 0";
        }

        private void LoadDgvStyle()
        {
            // في WPF يتحكم StringFormat في العرض مباشرة في XAML
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Business Logic

        private void InsertSelectedItems()
        {
            try
            {
                var selectedRows = GridControl1.SelectedItems
                    .Cast<InvoiceItemRow>()
                    .ToList();

                if (selectedRows.Count == 0)
                {
                    DXMessageBox.Show("يجب اختيار صف", "تنبيه",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    ISDone = false;
                    return;
                }

                foreach (var selectedRow in selectedRows)
                {
                    foreach (var invoiceItem in inv.InvoiceItems)
                    {
                        if (invoiceItem.ItemId == selectedRow.Item_ID)
                        {
                            invoiceItem.ItemQuantity = selectedRow.ItemQty;
                            invoiceItem.ItemDiscount = selectedRow.ItemDiscount;
                        }
                    }
                }

                RemoveNonSelectedItems(selectedRows);

                ISDone = true;
                Close();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    $"خطأ:\n{ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveNonSelectedItems(List<InvoiceItemRow> selectedRows)
        {
            var selectedIds = selectedRows.Select(r => r.Item_ID).ToHashSet();
            var itemsToRemove = inv.InvoiceItems
                .Where(i => !selectedIds.Contains(i.ItemId))
                .ToList();

            foreach (var item in itemsToRemove)
                inv.InvoiceItems.Remove(item);
        }

        private void CloseWithoutResult()
        {
            if (InvType == 23)
            {
                invcontract = new Invoicecontract();
            }
            else
            {
                inv = new InvoiceDGV();
            }

            ISDone = false;
            Close();
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region UI Helpers

        private void UpdateStatus()
        {
            lblStatus.Text = $"🧾 إجمالي الأصناف: {_rows.Count}";
        }

        #endregion
    }

    // ══════════════════════════════════════════════════════════════
    #region Model: InvoiceItemRow

    public class InvoiceItemRow : INotifyPropertyChanged
    {
        public int RowNo { get; set; }
        public string InvGlobalID { get; set; }
        public int Item_ID { get; set; }
        public double RowQty { get; set; }
        public double OriginalDisc { get; set; }
        public double ItemPrice { get; set; }
        public double ItemVAT { get; set; }
        public double ItemNet { get; set; }
        public string ItemUnit { get; set; }
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public string ItemBarcode { get; set; }

        private double _itemQty;
        public double ItemQty
        {
            get => _itemQty;
            set { _itemQty = value; OnPropertyChanged(nameof(ItemQty)); }
        }

        private double _itemDiscount;
        public double ItemDiscount
        {
            get => _itemDiscount;
            set { _itemDiscount = value; OnPropertyChanged(nameof(ItemDiscount)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    #endregion
}