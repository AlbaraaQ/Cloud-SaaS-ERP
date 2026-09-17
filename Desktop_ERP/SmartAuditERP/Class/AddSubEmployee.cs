using System;
using Microsoft.VisualBasic;

namespace SmartAuditERP
{

	public class AddSubEmployee
	{
		public string GlobalID;

		public int _id;

		public int BranchID;

		public string EntryGlobalID;

		public int ReceiptType;

		public bool State;

		public DateTime ReceiptDate;

		public bool ISDeleted;

		public string Notes;

		public string _name;

		public string CreditAcc;

		public string DebitAcc;

		public decimal _Net;

		public AddSubEmployee()
		{
			this.GlobalID = "";
			this._id = 0;
			this.BranchID = 1;
			this.EntryGlobalID = "";
			this.ReceiptType = 28;
			this.State = true;
			this.ReceiptDate = DateAndTime.Now.Date;
			this.ISDeleted = false;
			this.Notes = "";
			this._name = "";
			this.CreditAcc = "";
			this.DebitAcc = "";
			this._Net = default(decimal);
		}
	}
}