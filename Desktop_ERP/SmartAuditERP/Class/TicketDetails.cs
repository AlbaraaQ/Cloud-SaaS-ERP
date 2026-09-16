using System;

namespace SmartAuditERP
{

	public class TicketDetails
	{
		public string TicketNumber { get; set; }

		public string CustomerName { get; set; }

		public string CustomerPhone { get; set; }

		public string Subject { get; set; }

		public string Description { get; set; }

		public string Status { get; set; }

		public string resolution { get; set; }

		public DateTime createdDate { get; set; }
	}
}