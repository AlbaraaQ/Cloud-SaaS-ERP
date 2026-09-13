using System;

namespace SmartAuditERP
{

	public class ListCasherClosed
	{
		public int Id { get; set; }

		public string Type { get; set; }

		public int UserId { get; set; }

		public DateTime StartTime { get; set; }

		public DateTime EndTime { get; set; }

		public decimal ToT { get; set; }

		public decimal CasherValue { get; set; }

		public decimal Diff { get; set; }

		public string GlobalID { get; set; }

		public string Branch { get; set; }

		public int ClosedID { get; set; }
	}
}