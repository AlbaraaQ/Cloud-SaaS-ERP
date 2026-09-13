using System.Data;
using System.Data.SqlClient;
using Microsoft.VisualBasic.CompilerServices;

namespace SmartAuditERP
{

	public class Bank
	{
		public int ID { get; set; }

		public string Name { get; set; }

		public string AccCode { get; set; }

		public string City { get; set; }

		public decimal DisPrec { get; set; }

		public string Country { get; set; }

		public string Region { get; set; }

		public Bank(int Id)
		{
			this.Name = "";
			this.AccCode = "1221001";
			this.City = "";
			this.DisPrec = 0m;
			this.Country = "";
			this.Region = "";
			if (Id > -1)
			{
				this.ID = Id;
				SqlDataAdapter sqlDataAdapter;
				sqlDataAdapter = new SqlDataAdapter(selectConnection: MainClass.ConnObj(), selectCommandText: "select name,Acc_Code ,DisPre from banks Where id= " + Conversions.ToString(Id));
				DataTable dataTable;
				dataTable = new DataTable();
				sqlDataAdapter.Fill(dataTable);
				if (dataTable.Rows.Count > 0)
				{
					this.Name = Conversions.ToString(dataTable.Rows[0][0]);
					this.AccCode = Common.GetAccountCode(this.Name);
					this.DisPrec = Conversions.ToDecimal(dataTable.Rows[0]["DisPre"]);
				}
			}
		}
	}
}