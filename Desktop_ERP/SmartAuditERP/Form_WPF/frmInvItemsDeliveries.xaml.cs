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
using AuditorAPI.Models;
using AuditorAPI.Models.DGVmodels;
using DevExpress.Xpf.Core;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmInvItemsDeliveries : DevExpress.Xpf.Core.ThemedWindow
    {
        // ══════════════════════════════════════════════════════════
        #region Public Fields

        public int selectedCli;
        public int StoreId;
        public string sql;
        public string Cond;
        public string InvGlobalId;
        public int ItemID;
        public int InvType;
        public bool ISDone;
        public InvoiceDGV Inv;

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Private Fields

        private readonly SqlConnection _conn;
        private ObservableCollection<DeliveryItemRow> _deliveryItems;

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Constructor

        public frmInvItemsDeliveries()
        {
            InitializeComponent();

            _conn = MainClass.ConnObj();
            StoreId = 1;
            InvGlobalId = "1";
            ItemID = -1;
            InvType = 1;
            ISDone = false;
            Inv = new InvoiceDGV();
            _deliveryItems = new ObservableCollection<DeliveryItemRow>();

            GridControl1.ItemsSource = _deliveryItems;
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Window Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDeliveryItems();
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Header Events

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void txtSrchDgv_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                SearchByBarcode(txtSrchDgv.Text.Trim());
            }
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Grid Events

        private void GridControl1_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header?.ToString()?.Contains("كمية التسليم") != true)
                return;

            if (e.Row.Item is not DeliveryItemRow row)
                return;

            if (e.EditingElement is not TextBox tb)
                return;

            if (!double.TryParse(tb.Text, out double enteredQty))
                return;

            double maxAllowed = row.ItemQuantity - row.DeliveriedItemQuantity;

            if (enteredQty > maxAllowed)
            {
                DXMessageBox.Show(
                    "لقد أدخلت كمية أكبر من كمية الصنف في الفاتورة",
                    "تنبيه",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                tb.Text = maxAllowed.ToString();
                row.DeliveryItemQuantity = maxAllowed;
            }
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Footer Events

        private void btnDelivery_Click(object sender, RoutedEventArgs e)
        {
            DeliveryInvoiceItems();
        }

        private void btnDeliveryAll_Click(object sender, RoutedEventArgs e)
        {
            // ضبط كمية التسليم للحد الأقصى لكل صنف
            foreach (var row in _deliveryItems)
            {
                row.DeliveryItemQuantity = row.ItemQuantity - row.DeliveriedItemQuantity;
            }

            GridControl1.Items.Refresh();
            DeliveryInvoiceItems();
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Data Loading

        /// <summary>
        /// تحميل أصناف الفاتورة مع كميات التسليم السابقة
        /// </summary>
        private void LoadDeliveryItems()
        {
            try
            {
                _deliveryItems.Clear();

                string invQuery = $@"
                    SELECT Inv_sub.ItemId,
                           Items.name       AS ItemName,
                           Units.Name       AS UnitName,
                           Units.id         AS UnitId,
                           Inv_sub.val      AS ItemQuantity,
                           Inv_sub.val      AS DeliveryItemQuantity,
                           0                AS DeliveriedItemQuantity
                    FROM   Inv_Sub
                    LEFT JOIN Items ON Inv_Sub.ItemId = Items.id
                    LEFT JOIN Units ON Units.id        = Inv_Sub.unit
                    WHERE  InvGlobalId = N'{Inv.InvGlobalID}'";

                var invTable = new DataTable();
                new SqlDataAdapter(invQuery, _conn).Fill(invTable);

                foreach (DataRow row in invTable.Rows)
                {
                    int itemId = row["ItemId"] == DBNull.Value
                        ? 0
                        : Convert.ToInt32(row["ItemId"]);

                    double itemQty = row["ItemQuantity"] == DBNull.Value
                        ? 0.0
                        : Convert.ToDouble(row["ItemQuantity"]);

                    // جلب الكمية المسلمة سابقاً
                    double deliveredQty = GetDeliveredQuantity(itemId);

                    double remainingQty = itemQty - deliveredQty;

                    _deliveryItems.Add(new DeliveryItemRow
                    {
                        ItemId = itemId,
                        ItemName = row["ItemName"]?.ToString() ?? "",
                        UnitName = row["UnitName"]?.ToString() ?? "",
                        UnitId = row["UnitId"] == DBNull.Value ? 0 : Convert.ToInt32(row["UnitId"]),
                        ItemQuantity = itemQty,
                        DeliveriedItemQuantity = deliveredQty,
                        DeliveryItemQuantity = remainingQty > 0 ? remainingQty : 0,
                        BatchDeliveryDate = DateTime.Now
                    });
                }

                UpdateStatusLabel();
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    $"خطأ في تحميل البيانات:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// جلب الكمية المسلمة سابقاً للصنف في هذه الفاتورة
        /// </summary>
        private double GetDeliveredQuantity(int itemId)
        {
            try
            {
                string query = $@"
                    SELECT ISNULL(SUM(ItemQuantity), 0) AS DeliveriedItemQuantity
                    FROM   ItemBatchDeliveries
                    WHERE  InvGlobalID = N'{Inv.InvGlobalID}'
                      AND  ItemId      = {itemId}";

                var table = new DataTable();
                new SqlDataAdapter(query, _conn).Fill(table);

                if (table.Rows.Count > 0 && table.Rows[0][0] != DBNull.Value)
                    return Convert.ToDouble(table.Rows[0]["DeliveriedItemQuantity"]);
            }
            catch { /* نتجاوز الخطأ ونرجع 0 */ }

            return 0.0;
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region Business Logic

        /// <summary>
        /// تنفيذ عملية التسليم وحفظها في قاعدة البيانات
        /// </summary>
        private void DeliveryInvoiceItems()
        {
            try
            {
                using var sqlConn = MainClass.ConnObj();

                if (sqlConn.State != System.Data.ConnectionState.Open)
                    sqlConn.Open();

                int batchDelivIncId = GetNextBatchDelivIncId(sqlConn);
                int batchDelivNo = GetNextBatchDelivNo(sqlConn, Inv.InvGlobalID);

                var deliveryList = new List<ItemBatchDelivery>();
                bool isDeliveredAll = true;

                foreach (var row in _deliveryItems)
                {
                    var delivery = new ItemBatchDelivery
                    {
                        BatchDelivNo = batchDelivNo,
                        BatchDelivIncId = batchDelivIncId,
                        InvGlobalID = Inv.InvGlobalID,
                        ItemId = row.ItemId,
                        ItemUnitId = row.UnitId,
                        ItemQuantity = (float)row.DeliveryItemQuantity,
                        BatchDeliveryDate = DateTime.Now,
                        InvertoryEmp = MainClass.EmpNo,
                        RecipientID = Inv.Customer,
                        Note = $"تسليم فاتورة رقم {Inv.InvoiceNo}"
                    };

                    // هل لا تزال هناك كمية غير مسلمة؟
                    double totalDelivered = delivery.ItemQuantity + row.DeliveriedItemQuantity;
                    if (totalDelivered < row.ItemQuantity)
                        isDeliveredAll = false;

                    deliveryList.Add(delivery);
                }

                new ItemOper().SaveInvoiceItemDeliveries(deliveryList, isDeliveredAll);

                ISDone = true;

                DXMessageBox.Show(
                    "تم اعتماد التسليم بنجاح ✅",
                    "نجاح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Close();
            }
            catch (Exception ex)
            {
                string errorMsg = MainClass.Language == "ar"
                    ? $"خطأ أثناء تسليم الكميات\nتفاصيل الخطأ: {ex.Message}"
                    : $"Error in saving\nError details: {ex.Message}";

                DXMessageBox.Show(errorMsg, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// البحث عن صنف بالباركود وتحديد الصف المقابل في الجدول
        /// </summary>
        private void SearchByBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return;

            // نبحث عن الصنف في قاعدة البيانات
            try
            {
                string query = $@"
                    SELECT id FROM Items
                    WHERE barcode = N'{barcode}'
                      AND IS_Deleted = 0";

                var table = new DataTable();
                new SqlDataAdapter(query, _conn).Fill(table);

                if (table.Rows.Count == 0)
                {
                    DXMessageBox.Show(
                        "لم يتم العثور على الصنف بهذا الباركود",
                        "بحث",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                int foundItemId = Convert.ToInt32(table.Rows[0]["id"]);

                var targetRow = _deliveryItems.FirstOrDefault(r => r.ItemId == foundItemId);
                if (targetRow != null)
                {
                    GridControl1.SelectedItem = targetRow;
                    GridControl1.ScrollIntoView(targetRow);
                }
            }
            catch (Exception ex)
            {
                DXMessageBox.Show(
                    $"خطأ في البحث:\n{ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// جلب المعرف التسلسلي الجديد لعملية التسليم
        /// </summary>
        private int GetNextBatchDelivIncId(SqlConnection conn)
        {
            var cmd = new SqlCommand(
                "SELECT ISNULL(MAX(BatchDelivIncId), 0) FROM ItemBatchDeliveries",
                conn);
            return Convert.ToInt32(cmd.ExecuteScalar()) + 1;
        }

        /// <summary>
        /// جلب رقم الدفعة الجديدة لهذه الفاتورة
        /// </summary>
        private int GetNextBatchDelivNo(SqlConnection conn, string invGlobalId)
        {
            var cmd = new SqlCommand(
                $"SELECT ISNULL(MAX(BatchDelivNo), 0) FROM ItemBatchDeliveries WHERE InvGlobalID = N'{invGlobalId}'",
                conn);
            return Convert.ToInt32(cmd.ExecuteScalar()) + 1;
        }

        #endregion

        // ══════════════════════════════════════════════════════════
        #region UI Helpers

        private void UpdateStatusLabel()
        {
            int totalItems = _deliveryItems.Count;
            double totalQty = _deliveryItems.Sum(r => r.ItemQuantity);

            lblStatus.Text = MainClass.Language == "ar"
                ? $"📦 إجمالي الأصناف: {totalItems}   |   الكمية الكلية: {totalQty:N2}"
                : $"📦 Total Items: {totalItems}   |   Total Qty: {totalQty:N2}";
        }

        #endregion
    }

    // ══════════════════════════════════════════════════════════════
    #region Model: DeliveryItemRow

    /// <summary>
    /// نموذج صف تسليم الأصناف في DataGrid
    /// </summary>
    public class DeliveryItemRow : INotifyPropertyChanged
    {
        public int ItemId { get; set; }
        public int UnitId { get; set; }
        public string ItemName { get; set; }
        public string UnitName { get; set; }

        public double ItemQuantity { get; set; }
        public double DeliveriedItemQuantity { get; set; }
        public DateTime BatchDeliveryDate { get; set; }

        private double _deliveryItemQuantity;
        public double DeliveryItemQuantity
        {
            get => _deliveryItemQuantity;
            set
            {
                _deliveryItemQuantity = value;
                OnPropertyChanged(nameof(DeliveryItemQuantity));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    #endregion
}