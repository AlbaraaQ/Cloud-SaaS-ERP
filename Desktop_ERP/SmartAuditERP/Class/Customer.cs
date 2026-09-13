using System.Data;
using System.Data.SqlClient;
using Microsoft.VisualBasic.CompilerServices;

namespace SmartAuditERP
{

	public class Customer
	{
		public int ID { get; set; }

		public string VATno { get; set; }

		public string Name { get; set; }

		public string AccCode { get; set; }

		public string Mobile { get; set; }

		public string City { get; set; }

		public string Country { get; set; }

		public string Region { get; set; }

		public string NatianalID { get; set; }

		public string Note { get; set; }

		public string PlotIdentification { get; set; }

		public string BuildingNumber { get; set; }

		public string StreetName { get; set; }

		public string AdditionalStreetName { get; set; }

		public string District { get; set; }

		public string PostalZone { get; set; }

		public string CrNo { get; set; }

		public string area2 { get; set; }

		public string city2 { get; set; }

		public Customer(int Id)
		{
			this.VATno = "";
			this.Name = "";
			this.AccCode = "";
			this.Mobile = "";
			this.City = "";
			this.Country = "";
			this.Region = "";
			this.NatianalID = "";
			this.Note = "";
			this.PlotIdentification = "";
			this.BuildingNumber = "";
			this.StreetName = "";
			this.AdditionalStreetName = "";
			this.District = "";
			this.PostalZone = "";
			this.CrNo = "";
			this.area2 = "";
			this.city2 = "";
			if (Id > -1)
			{
				this.ID = Id;
				SqlDataAdapter sqlDataAdapter;
				sqlDataAdapter = new SqlDataAdapter(selectConnection: MainClass.ConnObj(), selectCommandText: "select * from customers where id=" + Conversions.ToString(Id));
				DataTable dataTable;
				dataTable = new DataTable();
				sqlDataAdapter.Fill(dataTable);
				if (dataTable.Rows.Count > 0)
				{
					this.VATno = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[0]["tax_no"]));
					this.Name = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[0]["name"]));
					this.AccCode = Common.GetAccountCode(this.Name);
					this.Mobile = Conversions.ToString(dataTable.Rows[0]["mobile"]);
					this.City = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[0]["city"]));
					this.Country = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[0]["country"]));
					this.Region = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[0]["area"]));
					this.NatianalID = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[0]["national_id"]));
					this.Note = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[0]["notes"]));
					this.PlotIdentification = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[0]["PlotIdentification"]));
					this.BuildingNumber = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[0]["BuildingNumber"]));
					this.StreetName = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[0]["StreetName"]));
					this.AdditionalStreetName = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[0]["AdditionalStreetName"]));
					this.District = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[0]["District"]));
					this.PostalZone = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[0]["PostalZone"]));
					this.CrNo = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[0]["CrNo"]));
					this.area2 = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[0]["area2"]));
					this.city2 = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[0]["city2"]));
				}
			}
		}
	}
}