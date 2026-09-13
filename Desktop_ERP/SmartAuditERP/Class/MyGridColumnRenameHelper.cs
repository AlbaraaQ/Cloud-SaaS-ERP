using System;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.Skins;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;

namespace SmartAuditERP
{

    public class MyGridColumnRenameHelper
    {
        private GridView gridView;

        private TextEdit headerEdit;

        private GridColumn editedColumn;

        public bool IsEditing => this.editedColumn != null;

        public MyGridColumnRenameHelper(GridView view)
        {
            this.gridView = view;
            this.Initialize();
            this.SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            this.gridView.DoubleClick += gridView_DoubleClick;
            this.headerEdit.Leave += headerEdit_Leave;
            this.headerEdit.KeyDown += headerEdit_KeyDown;
        }

        private GridColumn GetColumn(DXMouseEventArgs args)
        {
            GridHitInfo gridHitInfo;
            gridHitInfo = this.gridView.CalcHitInfo(args.Location);
            if (gridHitInfo.InColumnPanel)
            {
                return gridHitInfo.Column;
            }
            return null;
        }

        private Color GetColor()
        {
            return CommonSkins.GetSkin(this.gridView.GridControl.LookAndFeel).TranslateColor(SystemColors.Control);
        }

        private void Initialize()
        {
            this.gridView.OptionsCustomization.AllowSort = false;
            this.headerEdit = new TextEdit();
            this.headerEdit.Hide();
            this.headerEdit.Parent = this.gridView.GridControl;
            this.headerEdit.BorderStyle = BorderStyles.NoBorder;
        }

        private void ShowCaptionEditor(GridColumn column)
        {
            Rectangle bounds;
            bounds = (this.gridView.GetViewInfo() as GridViewInfo).ColumnsInfo[column].Bounds;
            checked
            {
                bounds.Width -= 3;
                bounds.Height -= 3;
                bounds.Y += 3;
                this.headerEdit.BackColor = this.GetColor();
                this.headerEdit.SetBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
                this.headerEdit.EditValue = column.GetCaption();
                this.headerEdit.Show();
                this.headerEdit.Focus();
            }
        }

        private void StartColumnCaptionEditing(GridColumn column)
        {
            this.ShowCaptionEditor(column);
            this.editedColumn = column;
        }

        private void EndColumnCaptionEditing()
        {
            if (this.IsEditing)
            {
                this.editedColumn.Caption = this.headerEdit.Text;
                this.headerEdit.Hide();
                this.editedColumn = null;
            }
        }

        private void gridView_DoubleClick(object sender, EventArgs e)
        {
            GridColumn column;
            column = this.GetColumn(e as DXMouseEventArgs);
            if (column != null)
            {
                this.StartColumnCaptionEditing(column);
            }
        }

        private void headerEdit_Leave(object sender, EventArgs e)
        {
            this.EndColumnCaptionEditing();
        }

        private void headerEdit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Return)
            {
                this.EndColumnCaptionEditing();
            }
        }
    }
}