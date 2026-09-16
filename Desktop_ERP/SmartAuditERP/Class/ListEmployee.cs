using System;

namespace SmartAuditERP
{

	public class ListEmployee
	{
		public int Id { get; set; }

		public string Name { get; set; }

		public string Manag { get; set; }

		public string Dep { get; set; }

		public string State { get; set; }

		public string Job { get; set; }

		public string Branch { get; set; }

		public DateTime BirthDate { get; set; }

		public string InsuranceNo { get; set; }

		public DateTime WorkDate { get; set; }

		public string MaritalState { get; set; }

		public string Nationality { get; set; }

		public string Sex { get; set; }

		public string Tel { get; set; }

		public string Mobile { get; set; }

		public string Email { get; set; }

		public string Address { get; set; }

		public string Notes { get; set; }

		public string Image { get; set; }

		public decimal SalaryBasic { get; set; }

		public decimal SalaryAdd { get; set; }

		public decimal SalaryOther { get; set; }

		public decimal House { get; set; }

		public decimal Food { get; set; }

		public decimal Travel { get; set; }

		public decimal Medical { get; set; }

		public bool IsDeleted { get; set; }

		public string AccCode { get; set; }

		public string CardNo { get; set; }

		public string BankNo { get; set; }

		public string BankName { get; set; }
	}
}