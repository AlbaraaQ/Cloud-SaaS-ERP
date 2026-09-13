namespace SmartAuditERP
{

	public class ListItemsCategory
	{
		public int CategoryId { get; set; }

		public string Code { get; set; }

		public string ParentCode { get; set; }

		public int Type { get; set; }

		public string Name { get; set; }

		public string NameEN { get; set; }

		public string Color { get; set; }

		public string Printer { get; set; }

		public string Image { get; set; }

		public bool ShowInPOS { get; set; }

		public int DisplayOrder { get; set; }

		public bool IS_Deleted { get; set; }

		public bool PrintAllItems { get; set; }

		public bool PrintItemsSeparately { get; set; }

		public int BranchId { get; set; }

		public bool AllBranch { get; set; }

		public int Id { get; set; }
	}
}