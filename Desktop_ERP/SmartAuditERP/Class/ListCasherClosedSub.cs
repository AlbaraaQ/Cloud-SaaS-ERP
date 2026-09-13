namespace SmartAuditERP
{

	public class ListCasherClosedSub
	{
		public int Id { get; set; }

		public int ClosedId { get; set; }

		public decimal CashTotal { get; set; }

		public decimal SAfeNetVal { get; set; }

		public decimal ReturnSum { get; set; }

		public decimal NetworkSum { get; set; }

		public decimal AdditionVal { get; set; }

		public decimal PostPoneSales { get; set; }

		public decimal PostPoneRet { get; set; }

		public decimal InsurVal { get; set; }

		public decimal Discount { get; set; }

		public decimal AllVAT { get; set; }

		public decimal ExtraTax { get; set; }

		public decimal Expenses { get; set; }

		public decimal Purchases { get; set; }

		public decimal HostingVal { get; set; }

		public bool IsDeleted { get; set; }

		public string GlobalID { get; set; }
	}
}