namespace SmartAuditERP
{

	public class ListSafes
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public string Branch { get; set; }

		public int Status { get; set; }

		public int IsDefault { get; set; }

		public string Notes { get; set; }

		public bool IsDeleted { get; set; }
	}
}