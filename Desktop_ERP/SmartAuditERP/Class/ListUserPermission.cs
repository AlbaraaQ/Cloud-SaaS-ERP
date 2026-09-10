namespace SmartAuditERP
{

	public class ListUserPermission
	{
		public int UserId { get; set; }

		public int FormId { get; set; }

		public bool IsNew { get; set; }

		public bool IsSave { get; set; }

		public bool IsDelete { get; set; }

		public bool IsSearch { get; set; }

		public bool IsPrint { get; set; }

		public bool IsEdit { get; set; }
	}
}