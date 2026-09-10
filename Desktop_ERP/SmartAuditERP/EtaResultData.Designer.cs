using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace SmartAuditERP
{
partial class EtaResultData
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

        public GridControl GridControl1;
        public GridView GridView1;
        public RepositoryItemHyperLinkEdit RepositoryItemHyperLinkEdit1;
        public RepositoryItemCheckEdit RepositoryItemCheckEdit1;

    void InitializeComponent()
	{
		this.GridControl1 = new global::DevExpress.XtraGrid.GridControl();
		this.GridView1 = new global::DevExpress.XtraGrid.Views.Grid.GridView();
		this.RepositoryItemHyperLinkEdit1 = new global::DevExpress.XtraEditors.Repository.RepositoryItemHyperLinkEdit();
		this.RepositoryItemCheckEdit1 = new global::DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
		((global::System.ComponentModel.ISupportInitialize)this.GridControl1).BeginInit();
		((global::System.ComponentModel.ISupportInitialize)this.GridView1).BeginInit();
		((global::System.ComponentModel.ISupportInitialize)this.RepositoryItemHyperLinkEdit1).BeginInit();
		((global::System.ComponentModel.ISupportInitialize)this.RepositoryItemCheckEdit1).BeginInit();
		base.SuspendLayout();
		this.GridControl1.Dock = global::System.Windows.Forms.DockStyle.Fill;
		this.GridControl1.Location = new global::System.Drawing.Point(0, 0);
		this.GridControl1.MainView = this.GridView1;
		this.GridControl1.Name = "GridControl1";
		this.GridControl1.RepositoryItems.AddRange(new global::DevExpress.XtraEditors.Repository.RepositoryItem[2] { this.RepositoryItemHyperLinkEdit1, this.RepositoryItemCheckEdit1 });
		this.GridControl1.Size = new global::System.Drawing.Size(1245, 655);
		this.GridControl1.TabIndex = 54;
		this.GridControl1.ViewCollection.AddRange(new global::DevExpress.XtraGrid.Views.Base.BaseView[1] { this.GridView1 });
		this.GridView1.Appearance.Row.Font = new global::System.Drawing.Font("Segoe UI", 12f);
		this.GridView1.Appearance.Row.Options.UseFont = true;
		this.GridView1.GridControl = this.GridControl1;
		this.GridView1.Name = "GridView1";
		this.GridView1.OptionsFind.AlwaysVisible = true;
		this.GridView1.OptionsView.ColumnAutoWidth = false;
		this.GridView1.OptionsView.ShowFooter = true;
		this.RepositoryItemHyperLinkEdit1.AutoHeight = false;
		this.RepositoryItemHyperLinkEdit1.Name = "RepositoryItemHyperLinkEdit1";
		this.RepositoryItemCheckEdit1.Name = "RepositoryItemCheckEdit1";
		base.AutoScaleDimensions = new global::System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = global::System.Windows.Forms.AutoScaleMode.Font;
		base.ClientSize = new global::System.Drawing.Size(1245, 655);
		base.Controls.Add(this.GridControl1);
		base.Name = "EtaResultData";
		this.Text = "Result Data ";
		((global::System.ComponentModel.ISupportInitialize)this.GridControl1).EndInit();
		((global::System.ComponentModel.ISupportInitialize)this.GridView1).EndInit();
		((global::System.ComponentModel.ISupportInitialize)this.RepositoryItemHyperLinkEdit1).EndInit();
		((global::System.ComponentModel.ISupportInitialize)this.RepositoryItemCheckEdit1).EndInit();
		base.ResumeLayout(false);
	}

    #endregion
}
}
