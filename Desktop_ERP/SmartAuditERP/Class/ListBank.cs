namespace SmartAuditERP
{

	public class ListBank
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public string Country { get; set; }

		public string City { get; set; }

		public string AccCode { get; set; }

		public string Area { get; set; }

		public string Tel { get; set; }

		public string Mobile { get; set; }

		public string Notes { get; set; }

		public bool IsDeleted { get; set; }

		public decimal DisPre { get; set; }

		public bool ChangeInPOS { get; set; }
	}
}