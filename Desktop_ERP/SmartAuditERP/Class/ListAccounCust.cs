using System;

namespace SmartAuditERP
{

	public class ListAccounCust
	{
		public string Code { get; set; }

		public string AName { get; set; }

		public int Nature { get; set; }

		public int AccountType { get; set; }

		public int? ParentCode { get; set; }

		public DateTime AccDate { get; set; }

		public string UserName { get; set; }

		public decimal IValue { get; set; }

		public decimal Total_Debts { get; set; }

		public decimal Total_Credits { get; set; }

		public decimal Account_Value { get; set; }

		public int Acc_branch { get; set; }

		public int CostCenter { get; set; }

		public bool IsDeleted { get; set; }

		public double FinalAcc { get; set; }
	}
}