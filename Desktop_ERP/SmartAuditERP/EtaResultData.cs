using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using Microsoft.VisualBasic.CompilerServices;

namespace SmartAuditERP
{

	public partial class EtaResultData : XtraForm
	{

		public EtaResultData()
		{
			base.Load += EtaResultData_Load;
			this.InitializeComponent();
		}


		private void EtaResultData_Load(object sender, EventArgs e)
		{
		}
	}
}