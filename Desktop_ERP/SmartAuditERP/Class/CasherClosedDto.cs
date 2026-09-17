using System;
using System.Collections.Generic;

namespace SmartAuditERP
{

	public class CasherClosedDto
	{
		public int Id { get; set; }

		public int Type { get; set; }

		public int UserId { get; set; }

		public DateTime StartTime { get; set; }

		public DateTime EndTime { get; set; }

		public double ToT { get; set; }

		public double CasherValue { get; set; }

		public double Diff { get; set; }

		public string GlobalID { get; set; }

		public int Branch { get; set; }

		public string ClosedId { get; set; }

		public List<CasherClosedSubDto> CasherClosedSubs { get; set; }
	}
}