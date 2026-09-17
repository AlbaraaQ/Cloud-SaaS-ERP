using System;

namespace SmartAuditERP
{

	public class ListEntry
	{
		public string GlobalID { get; set; }

		public int ID { get; set; }

		public DateTime EntryDate { get; set; }

		public string DocNo { get; set; }

		public string Type { get; set; }

		public int State { get; set; }

		public string Notes { get; set; }

		public int Branch { get; set; }

		public bool IS_Deleted { get; set; }

		public int EmpID { get; set; }

		public bool Sync { get; set; }

		public bool IsVAT { get; set; }
	}
}