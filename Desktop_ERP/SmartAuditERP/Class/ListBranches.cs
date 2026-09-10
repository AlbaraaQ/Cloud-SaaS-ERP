namespace SmartAuditERP
{

	public class ListBranches
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public string Tel { get; set; }

		public string Mobile { get; set; }

		public string Fax { get; set; }

		public string Email { get; set; }

		public string Address { get; set; }

		public string Notes { get; set; }

		public bool IsDeleted { get; set; }

		public bool IsDefault { get; set; }

		public string Code { get; set; }

		public int BranchId { get; set; }

		public string CustomersAcc { get; set; }

		public string SupliersAcc { get; set; }

		public string TreasuriesAcc { get; set; }

		public string BanksAcc { get; set; }

		public string InventoryAcc { get; set; }

		public string AppAcc { get; set; }

		public bool IsActive { get; set; }

		public string CostCenter { get; set; }

		public string EmployeeAcc { get; set; }
	}
}