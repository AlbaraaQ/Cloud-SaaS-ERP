namespace SmartAuditERP
{

	public class CasherClosedSubDto
	{
		public int Id { get; set; }

		public int ClosedId { get; set; }

		public double CashTotal { get; set; }

		public double SAfeNetVal { get; set; }

		public double ReturnSum { get; set; }

		public double NetworkSum { get; set; }

		public double AdditionVal { get; set; }

		public double PostPoneSales { get; set; }

		public double PostPoneRet { get; set; }

		public double InsurVal { get; set; }

		public double Discount { get; set; }

		public double AllVAT { get; set; }

		public double HostingVal { get; set; }

		public double Expenses { get; set; }

		public double Purchases { get; set; }

		public double ExtraTax { get; set; }

		public string GlobalID { get; set; }
	}
}