using System.Data;
using System.Data.SqlClient;
using Microsoft.VisualBasic.CompilerServices;

namespace SmartAuditERP
{

	public class Treasury
	{
		public int ID { get; set; }

		public string Name { get; set; }

		public string AccCode { get; set; }

		public string City { get; set; }

		public string Country { get; set; }

		public string Region { get; set; }

		public Treasury(int Id)
		{
			this.Name = "";
			this.AccCode = "1211001";
			this.City = "";
			this.Country = "";
			this.Region = "";
			if (Id > -1)
			{
				this.ID = Id;
				SqlDataAdapter sqlDataAdapter;
				sqlDataAdapter = new SqlDataAdapter(selectConnection: MainClass.ConnObj(), selectCommandText: "select Acc_Code,name from Stocks where  id=" + Conversions.ToString(Id));
				DataTable dataTable;
				dataTable = new DataTable();
				sqlDataAdapter.Fill(dataTable);
				if (dataTable.Rows.Count > 0)
				{
					this.Name = Conversions.ToString(dataTable.Rows[0]["name"]);
					this.AccCode = Common.GetAccountCode(this.Name);
				}
			}
		}
	}
}