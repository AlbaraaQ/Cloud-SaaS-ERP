using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace SmartAuditERP
{

	public partial class frmBarcodeSetting : Form
	{

		private Rectangle drawingRect;

		private float rotationAngle;

		private double W;

		private double H;
		public frmBarcodeSetting()
		{
			this.drawingRect = Rectangle.Empty;
			this.rotationAngle = 0f;
			this.W = 10.0;
			this.H = 10.0;
			this.InitializeComponent();
			this.drawingRect = new Rectangle(50, 50, 100, 100);
		}

		private void GroupBox1_Paint(object sender, PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using Pen pen = new Pen(Color.LightGreen, 4f);
			using Matrix matrix = new Matrix();
			matrix.RotateAt(this.rotationAngle, new PointF((float)this.drawingRect.X + (float)this.drawingRect.Width / 2f, (float)this.drawingRect.Y + (float)this.drawingRect.Height / 2f));
			e.Graphics.Transform = matrix;
			e.Graphics.DrawRectangle(pen, this.drawingRect);
		}

		private void btnPaint_Click(object sender, EventArgs e)
		{
			this.drawingRect.Location = new Point(100, 100);
			this.drawingRect.Size = checked(new Size((int)Math.Round(this.W), (int)Math.Round(this.H)));
			this.GroupBox1.Invalidate();
		}

		private void btnRotate_Click(object sender, EventArgs e)
		{
			this.rotationAngle = 45f;
			this.GroupBox1.Invalidate();
		}

		private void txtWidth_TextChanged(object sender, EventArgs e)
		{
			if (Conversion.Val(this.txtWidth.Text) > 10.0)
			{
				this.W = Conversion.Val(this.txtWidth.Text);
			}
		}

		private void txtHeight_TextChanged(object sender, EventArgs e)
		{
			if (Conversion.Val(this.txtHeight.Text) > 10.0)
			{
				this.H = Conversion.Val(this.txtHeight.Text);
			}
		}
	}
}