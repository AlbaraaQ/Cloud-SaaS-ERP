using System.Collections.Generic;

namespace SmartAuditERP
{

	public class CreateTicketDto
	{
		public string CustomerName { get; set; }

		public string CustomerEmail { get; set; }

		public string CustomerPhone { get; set; }

		public string Subject { get; set; }

		public string Description { get; set; }

		public string Priority { get; set; }

		public string Category { get; set; }

		public string Type { get; set; }

		public int Branch { get; set; }

		public List<TicketAttachmentDto> Attachments { get; set; }
	}
}