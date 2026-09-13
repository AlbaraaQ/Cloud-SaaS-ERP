using System.Windows.Forms;

namespace SmartAuditERP
{
partial class frmBarcodeSetting
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

        public GroupBox GroupBox1;
        public GroupBox GroupBox2;
        public TextBox txtY;
        public TextBox txtWidth;
        public TextBox txtHeight;
        public TextBox txtX;
        public Button btnPaint;
        public Label Label4;
        public Label Label3;
        public Label lblheight;
        public Label Label1;

    void InitializeComponent()
	{
            this.GroupBox1.Paint += (this.GroupBox1_Paint);
            this.txtWidth.TextChanged += (this.txtWidth_TextChanged);
            this.txtHeight.TextChanged += (this.txtHeight_TextChanged);
            this.btnPaint.Click += (this.btnPaint_Click);

		this.GroupBox1 = new global::System.Windows.Forms.GroupBox();
		this.GroupBox2 = new global::System.Windows.Forms.GroupBox();
		this.Label4 = new global::System.Windows.Forms.Label();
		this.Label3 = new global::System.Windows.Forms.Label();
		this.lblheight = new global::System.Windows.Forms.Label();
		this.Label1 = new global::System.Windows.Forms.Label();
		this.txtY = new global::System.Windows.Forms.TextBox();
		this.txtWidth = new global::System.Windows.Forms.TextBox();
		this.txtHeight = new global::System.Windows.Forms.TextBox();
		this.txtX = new global::System.Windows.Forms.TextBox();
		this.btnPaint = new global::System.Windows.Forms.Button();
		this.GroupBox2.SuspendLayout();
		base.SuspendLayout();
		this.GroupBox1.Location = new global::System.Drawing.Point(12, 23);
		this.GroupBox1.Name = "GroupBox1";
		this.GroupBox1.Size = new global::System.Drawing.Size(497, 161);
		this.GroupBox1.TabIndex = 0;
		this.GroupBox1.TabStop = false;
		this.GroupBox1.Text = "GroupBox1";
		this.GroupBox2.Controls.Add(this.Label4);
		this.GroupBox2.Controls.Add(this.Label3);
		this.GroupBox2.Controls.Add(this.lblheight);
		this.GroupBox2.Controls.Add(this.Label1);
		this.GroupBox2.Controls.Add(this.txtY);
		this.GroupBox2.Controls.Add(this.txtWidth);
		this.GroupBox2.Controls.Add(this.txtHeight);
		this.GroupBox2.Controls.Add(this.txtX);
		this.GroupBox2.Location = new global::System.Drawing.Point(12, 202);
		this.GroupBox2.Name = "GroupBox2";
		this.GroupBox2.Size = new global::System.Drawing.Size(497, 94);
		this.GroupBox2.TabIndex = 1;
		this.GroupBox2.TabStop = false;
		this.GroupBox2.Text = "GroupBox2";
		this.Label4.AutoSize = true;
		this.Label4.Location = new global::System.Drawing.Point(169, 68);
		this.Label4.Name = "Label4";
		this.Label4.RightToLeft = global::System.Windows.Forms.RightToLeft.Yes;
		this.Label4.Size = new global::System.Drawing.Size(17, 13);
		this.Label4.TabIndex = 7;
		this.Label4.Text = "Y:";
		this.Label3.AutoSize = true;
		this.Label3.Location = new global::System.Drawing.Point(169, 26);
		this.Label3.Name = "Label3";
		this.Label3.RightToLeft = global::System.Windows.Forms.RightToLeft.Yes;
		this.Label3.Size = new global::System.Drawing.Size(17, 13);
		this.Label3.TabIndex = 6;
		this.Label3.Text = "X:";
		this.lblheight.AutoSize = true;
		this.lblheight.Location = new global::System.Drawing.Point(417, 75);
		this.lblheight.Name = "lblheight";
		this.lblheight.Size = new global::System.Drawing.Size(40, 13);
		this.lblheight.TabIndex = 5;
		this.lblheight.Text = "الإرتفاع";
		this.Label1.AutoSize = true;
		this.Label1.Location = new global::System.Drawing.Point(439, 19);
		this.Label1.Name = "Label1";
		this.Label1.Size = new global::System.Drawing.Size(38, 13);
		this.Label1.TabIndex = 4;
		this.Label1.Text = "Label1";
		this.txtY.Location = new global::System.Drawing.Point(82, 74);
		this.txtY.Name = "txtY";
		this.txtY.Size = new global::System.Drawing.Size(71, 20);
		this.txtY.TabIndex = 3;
		this.txtWidth.Location = new global::System.Drawing.Point(316, 19);
		this.txtWidth.Name = "txtWidth";
		this.txtWidth.Size = new global::System.Drawing.Size(71, 20);
		this.txtWidth.TabIndex = 2;
		this.txtHeight.Location = new global::System.Drawing.Point(316, 68);
		this.txtHeight.Name = "txtHeight";
		this.txtHeight.Size = new global::System.Drawing.Size(71, 20);
		this.txtHeight.TabIndex = 1;
		this.txtX.Location = new global::System.Drawing.Point(82, 19);
		this.txtX.Name = "txtX";
		this.txtX.Size = new global::System.Drawing.Size(71, 20);
		this.txtX.TabIndex = 0;
		this.btnPaint.Location = new global::System.Drawing.Point(165, 333);
		this.btnPaint.Name = "btnPaint";
		this.btnPaint.Size = new global::System.Drawing.Size(179, 23);
		this.btnPaint.TabIndex = 2;
		this.btnPaint.Text = "معاينة";
		this.btnPaint.UseVisualStyleBackColor = true;
		base.AutoScaleDimensions = new global::System.Drawing.SizeF(6f, 13f);
		base.AutoScaleMode = global::System.Windows.Forms.AutoScaleMode.Font;
		this.BackColor = global::System.Drawing.Color.WhiteSmoke;
		base.ClientSize = new global::System.Drawing.Size(550, 391);
		base.Controls.Add(this.btnPaint);
		base.Controls.Add(this.GroupBox2);
		base.Controls.Add(this.GroupBox1);
		base.Name = "frmBarcodeSetting";
		this.Text = "frmBarcodeSetting";
		this.GroupBox2.ResumeLayout(false);
		this.GroupBox2.PerformLayout();
		base.ResumeLayout(false);
	}

    #endregion
}
}
