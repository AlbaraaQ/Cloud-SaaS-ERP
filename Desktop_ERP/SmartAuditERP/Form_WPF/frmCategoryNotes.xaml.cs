using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SmartAuditERP.Form_WPF;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmCategoryNotes : Window
    {
        #region ── Fields ─────────────────────────────────────

        private SqlConnection conn;

        private List<CategoryNoteRow> _allRows = new List<CategoryNoteRow>();

        public bool ISDone { get; set; } = false;
        public string cond { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public double AddPrice { get; set; } = 0.0;
        public int Qty { get; set; } = 0;

        #endregion

        #region ── Constructor ────────────────────────────────

        public frmCategoryNotes()
        {
            InitializeComponent();
            conn = MainClass.ConnObj();
        }

        #endregion

        #region ── Window Events ──────────────────────────────

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadGridItems(Qty);
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        #endregion

        #region ── Data Loading ───────────────────────────────

        public void LoadGridItems(int qty)
        {
            try
            {
                string sqlQuery = $"SELECT Note, AddPrice FROM CategoryNotes {cond}";

                SqlDataAdapter adapter = new SqlDataAdapter(sqlQuery, conn);
                DataTable rawTable = new DataTable();
                adapter.Fill(rawTable);

                _allRows = new List<CategoryNoteRow>();

                foreach (DataRow row in rawTable.Rows)
                {
                    _allRows.Add(new CategoryNoteRow
                    {
                        DgvNote = row["Note"]?.ToString() ?? string.Empty,
                        DgvAddPrice = row["AddPrice"]?.ToString() ?? "0",
                        DgvQty = qty.ToString()
                    });
                }

                GridControl1.ItemsSource = _allRows;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء تحميل البيانات: " + ex.Message,
                    "❌ خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region ── Search ─────────────────────────────────────

        private void txtSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string searchText = txtSearch.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                GridControl1.ItemsSource = _allRows;
            }
            else
            {
                GridControl1.ItemsSource = _allRows
                    .Where(r => r.DgvNote.ToLower().Contains(searchText))
                    .ToList();
            }
        }

        #endregion

        #region ── Grid Events ────────────────────────────────

        private void GridControl1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && GridControl1.Items.Count > 0)
            {
                SelectSingleRow();
            }
        }

        private void GridControl1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (GridControl1.Items.Count > 0)
            {
                SelectSingleRow();
            }
        }

        private void GridControl1_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
        }

        #endregion

        #region ── Selection Helpers ──────────────────────────

        private void SelectSingleRow()
        {
            if (GridControl1.SelectedItem is CategoryNoteRow selectedRow)
            {
                ISDone = true;
                Note = selectedRow.DgvNote;
                AddPrice = double.TryParse(selectedRow.DgvAddPrice, out double price)
                           ? price : 0.0;
                Close();
            }
        }

        #endregion

        #region ── Button Events ──────────────────────────────

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            ISDone = false;
            Close();
        }

        private void IconButton1_Click(object sender, RoutedEventArgs e)
        {
            var selectedRows = GridControl1.SelectedItems
                .OfType<CategoryNoteRow>()
                .ToList();

            if (!selectedRows.Any())
            {
                MessageBox.Show("يرجى تحديد صف واحد على الأقل",
                    "⚠️ تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Note = string.Empty;
            AddPrice = 0.0;

            foreach (CategoryNoteRow row in selectedRows)
            {
                Note += $" - {row.DgvNote}{row.DgvQty}";
                AddPrice += double.TryParse(row.DgvAddPrice, out double price) ? price : 0.0;
            }

            ISDone = true;
            Close();
        }

        #endregion
    }

    #region ── Model ──────────────────────────────────────────

    public class CategoryNoteRow
    {
        public string DgvNote { get; set; }
        public string DgvAddPrice { get; set; }
        public string DgvQty { get; set; }
    }

    #endregion
}