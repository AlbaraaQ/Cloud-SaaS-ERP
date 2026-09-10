namespace SmartAuditERP
{
    /// <summary>
    /// ﬂ·«” »Ì«‰«  «·ﬁÌÊœ Ê«· ﬁ«—Ì— «·„«·Ì… - „ÊÕœ ·Ã„Ì⁄ «·Ê«ÃÂ« 
    /// </summary>
    public class RestrictionData
    {
        #region Account Info

        public string AccountName { get; set; }
        public string AccountNameBranch { get; set; }
        public string AccountCode { get; set; }
        public string MainAccountName { get; set; }
        public string MainAccountCode { get; set; }
        public string Description { get; set; }

        #endregion

        #region Client / Supplier Info

        public string Clint { get; set; }
        public string ClintCode { get; set; }
        public string ClintName { get; set; }
        public string ClintMobileNo { get; set; }
        public string ClintPhoneNo { get; set; }

        #endregion

        #region Debit & Credit

        public string Credit { get; set; }
        public string Dept { get; set; }
        public string CreditBalance { get; set; }
        public string DeptBalance { get; set; }
        public string CreditIntial { get; set; }
        public string DeptIntial { get; set; }
        public string CreditFinal { get; set; }
        public string DeptFinal { get; set; }
        public string CreditPervious { get; set; }
        public string DeptPervious { get; set; }
        public string CreditPerviousSum { get; set; }
        public string DeptPerviousSum { get; set; }

        #endregion

        #region Totals & Summaries

        public string SumCredit { get; set; }
        public string SumDept { get; set; }
        public string TotCredit { get; set; }
        public string TotDept { get; set; }
        public string TotCredit0 { get; set; }
        public string TotDept0 { get; set; }
        public string TotCreditFin { get; set; }
        public string TotDeptFin { get; set; }
        public string CreditDiff { get; set; }
        public string DeptDiff { get; set; }
        public string TotPeriod { get; set; }
        public string Total { get; set; }

        #endregion

        #region Balance & Status

        public string Balance { get; set; }
        public string BalanceInperiod { get; set; }
        public string BalanceType { get; set; }
        public string Status { get; set; }
        public string Note { get; set; }

        #endregion

        #region Entry / Restriction Info

        public string RestrNo { get; set; }
        public string RestrType { get; set; }
        public string RestDate { get; set; }
        public string RestTime { get; set; }

        #endregion

        #region Process Info

        public string ProcessNo { get; set; }
        public string ProcessType { get; set; }

        #endregion

        #region Invoice Info

        public string InvNo { get; set; }
        public string InvDate { get; set; }
        public string InvoiceType { get; set; }
        public string InvoiceCount { get; set; }
        public string Item { get; set; }

        #endregion

        #region Branch & Cost Center

        public string Branch { get; set; }
        public string CostCenter { get; set; }
        public string CodeCostCenter { get; set; }
        public string MainCostCenter { get; set; }
        public string CodeMainCostCenter { get; set; }

        #endregion

        #region Store & Stock

        public string Store { get; set; }
        public string StoreCode { get; set; }
        public string Stock { get; set; }
        public string StockCode { get; set; }
        public string stockVal { get; set; }
        public string InventryType { get; set; }

        #endregion

        #region Financial Info

        public string Safe { get; set; }
        public string SafeCode { get; set; }
        public string Cash { get; set; }
        public string Stall { get; set; }
        public string Profit { get; set; }
        public string Profit1 { get; set; }
        public string Profit2 { get; set; }
        public string Income { get; set; }
        public string Outcome { get; set; }
        public string LastPayment { get; set; }

        #endregion

        #region Employee & User

        public string Employee { get; set; }
        public string EmpName { get; set; }
        public string User { get; set; }

        #endregion

        #region Date Range

        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string PrintDate { get; set; }

        #endregion

        #region Foundation / Company Info

        public string Foundation { get; set; }
        public string Address { get; set; }
        public string Mobile { get; set; }
        public string TelePhone { get; set; }
        public string VatNo { get; set; }
        public string Field { get; set; }

        #endregion

        #region Print & Report Settings

        public string ArabicLetter { get; set; }
        public string Logo { get; set; }
        public string Header { get; set; }
        public string footer { get; set; }
        public string Stamp { get; set; }

        #endregion
    }
}