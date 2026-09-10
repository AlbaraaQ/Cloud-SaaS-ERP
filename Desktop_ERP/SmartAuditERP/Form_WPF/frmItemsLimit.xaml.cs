using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace SmartAuditERP.Form_WPF
{
    public partial class frmItemsLimit : DevExpress.Xpf.Core.ThemedWindow
    {
        #region Fields

        private ObservableCollection<ItemLimitRow> _rows;

        #endregion

        #region Constructor

        public frmItemsLimit()
        {
            _rows = new ObservableCollection<ItemLimitRow>();
            InitializeComponent();
        }

        #endregion

        #region Load

        private void frmItemsLimit_Load(object sender, RoutedEventArgs e)
        {
            dgvData.ItemsSource = _rows;
        }

        #endregion

        #region Public Methods

        public void LoadData(System.Data.DataTable dt)
        {
            _rows.Clear();

            if (dt == null || dt.Rows.Count == 0)
            {
                dgvData.ItemsSource = _rows;
                return;
            }

            foreach (System.Data.DataRow row in dt.Rows)
            {
                _rows.Add(new ItemLimitRow
                {
                    Column3 = SafeStr(row, 0),
                    Column2 = SafeStr(row, 1),
                    Column4 = SafeStr(row, 2),
                    Column1 = SafeStr(row, 3),
                    Column5 = SafeStr(row, 4),
                    Column7 = SafeStr(row, 5),
                    Column6 = SafeStr(row, 6)
                });
            }

            dgvData.ItemsSource = _rows;
        }

        private string SafeStr(System.Data.DataRow row, int colIndex)
        {
            try
            {
                if (colIndex >= row.Table.Columns.Count) return "";
                return row[colIndex] == System.DBNull.Value ? "" : row[colIndex].ToString();
            }
            catch { return ""; }
        }

        #endregion
    }

    public class ItemLimitRow
    {
        public string Column3 { get; set; }
        public string Column2 { get; set; }
        public string Column4 { get; set; }
        public string Column1 { get; set; }
        public string Column5 { get; set; }
        public string Column7 { get; set; }
        public string Column6 { get; set; }
    }
}