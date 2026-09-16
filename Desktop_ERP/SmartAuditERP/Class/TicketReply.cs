using System;

namespace SmartAuditERP
{

	public class TicketReply
	{
		public int ReplyID { get; set; }

		public string SenderName { get; set; }

		public string SenderType { get; set; }

		public string Message { get; set; }

		public DateTime CreatedDate { get; set; }
	}
}