using System.Windows.Forms;

namespace SmartAuditERP
{

	public class MyDataGridView : DataGridView
	{
		protected override bool ProcessDataGridViewKey(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Return)
			{
				if (base.CurrentCell.ColumnIndex == checked(base.Columns.Count - 1))
				{
					base.CurrentCell = base[0, base.CurrentRow.Index];
					return base.ProcessDataGridViewKey(e);
				}
				if ((base.CurrentCell.ColumnIndex == 1) | (base.CurrentCell.ColumnIndex == 3) | (base.CurrentCell.ColumnIndex == 6))
				{
					return base.ProcessSpaceKey(e.KeyCode);
				}
				return base.ProcessLeftKey(e.KeyData);
			}
			return base.ProcessDataGridViewKey(e);
		}

		protected override bool ProcessDialogKey(Keys keyData)
		{
			if (keyData == Keys.Return)
			{
				if (base.CurrentCell.ColumnIndex == checked(base.Columns.Count - 1))
				{
					base.CurrentCell = base[0, base.CurrentRow.Index];
					return base.ProcessDialogKey(keyData);
				}
				if ((base.CurrentCell.ColumnIndex == 1) | (base.CurrentCell.ColumnIndex == 3) | (base.CurrentCell.ColumnIndex == 6))
				{
					return base.ProcessSpaceKey(keyData);
				}
				return base.ProcessLeftKey(keyData);
			}
			return base.ProcessDialogKey(keyData);
		}
	}
}