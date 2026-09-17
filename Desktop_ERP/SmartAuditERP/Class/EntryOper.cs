using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AuditorAPI.Models;
using SmartAuditERP.Form_WPF;
using MessageBox = System.Windows.MessageBox;

namespace SmartAuditERP
{
    public class EntryOper
    {
        #region Fields

        public static string[] filePathDocument;

        #endregion

        #region Helper: Get Error Message

        private static string GetErrorMessage(string arabicMsg, string englishMsg, string details)
        {
            if (string.Equals(MainClass.Language, "ar", StringComparison.Ordinal))
                return $"{arabicMsg}{Environment.NewLine}تفاصيل الخطأ: {details}";

            return $"{englishMsg}{Environment.NewLine}Error details: {details}";
        }

        #endregion

        #region Helper: Format Amount

        private static double FormatAmount(double value)
        {
            if (double.TryParse($"{value:0.##}", out double result))
                return result;
            return value;
        }

        private static void SetDebtCredit(Account account, double amount)
        {
            if (amount > 0.0)
            {
                account.Debt = FormatAmount(amount);
                account.Credit = 0.0;
            }
            else if (amount < 0.0)
            {
                account.Debt = 0.0;
                account.Credit = FormatAmount(-1.0 * amount);
            }
        }

        #endregion

        #region BindInvoiceToEntry

        public Entry BindInvoiceToEntry(Invoice inv)
        {
            try
            {
                Customer customer = new Customer(inv.Customer);
                Bank bank = new Bank(inv.Bank);
                Treasury treasury = new Treasury(inv.Treasury);

                Entry entry = new Entry();
                List<Account> list = new List<Account>();

                entry.EntryGlobalID = inv.EntryGlobalID;
                entry.ClientCode = Sync.ClientCode;
                entry.EntryNo = int.Parse(
                    inv.EntryGlobalID.Substring(inv.EntryGlobalID.LastIndexOf('-') + 1));
                entry.EntryDate = inv.InvDate;
                entry.ReffNo = inv.InvoiceNo.ToString();
                entry.RefDate = inv.InvDate;
                entry.Type = (EntryType)inv.InvoiceType;

                if (inv.InvoiceType == InvoiceType.Purchase && inv.ProcType == 2)
                    entry.Type = EntryType.ReturnPurchase;
                else if ((inv.InvoiceType == InvoiceType.Sale ||
                          inv.InvoiceType == InvoiceType.POS) && inv.ProcType == 2)
                    entry.Type = EntryType.ReturnSale;

                entry.State = 1;
                entry.Note = inv.InvNote;
                entry.Branch = inv.Branch;
                entry.EmpID = inv.User;
                entry.DistBranch = Sync.DistBranch;
                entry.BranchType = Sync.BranchType;

                double credit = 0.0;
                double debt = 0.0;

                if (inv.Net > 0.0)
                {
                    bool isSaleOrPosProc1OrPurchaseProc2 =
                        (inv.InvoiceType == InvoiceType.Sale && inv.ProcType == 1) ||
                        (inv.InvoiceType == InvoiceType.POS && inv.ProcType == 1) ||
                        (inv.InvoiceType == InvoiceType.Purchase && inv.ProcType == 2);

                    bool isSaleOrPosProc2OrPurchaseProc1 =
                        (inv.InvoiceType == InvoiceType.Sale && inv.ProcType == 2) ||
                        (inv.InvoiceType == InvoiceType.POS && inv.ProcType == 2) ||
                        (inv.InvoiceType == InvoiceType.Purchase && inv.ProcType == 1);

                    // ─── حساب العميل (آجل أو ضيافة) ───
                    if (inv.PayType == -1 || inv.PayType == 5)
                    {
                        Account account = new Account();

                        if (isSaleOrPosProc1OrPurchaseProc2)
                        {
                            credit = 0.0;
                            debt = inv.Net;
                        }
                        else if (isSaleOrPosProc2OrPurchaseProc1)
                        {
                            credit = inv.Net;
                            debt = 0.0;
                        }

                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;
                        account.Name = customer.Name;
                        account.Code = customer.AccCode;

                        if (inv.PayType == 5)
                        {
                            account.Code = "3110002";
                            account.Name = "ضيافة ";
                        }

                        account.Debt = debt;
                        account.Credit = credit;
                        account.Note = $"{inv.InvNote}  :{customer.Name}";
                        account.CCcode = (-1).ToString();
                        list.Add(account);
                    }

                    // ─── حساب المبيعات/المشتريات ───
                    {
                        Account account = new Account();

                        if (isSaleOrPosProc1OrPurchaseProc2)
                        {
                            credit = inv.Net - inv.VAT + inv.Discount;
                            debt = 0.0;
                        }
                        else if (isSaleOrPosProc2OrPurchaseProc1)
                        {
                            credit = 0.0;
                            debt = inv.Net - inv.VAT + inv.Discount;
                        }

                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;
                        account.Name = ((int)inv.InvoiceType).ToString();
                        account.Code = Convert.ToDouble(inv.InvAccCode).ToString();
                        account.Debt = debt;
                        account.Credit = credit;
                        account.Note = $"{inv.InvNote}  :{customer.Name}";
                        account.CCcode = (-1).ToString();
                        list.Add(account);
                    }

                    // ─── الضريبة ───
                    {
                        Account account = new Account();

                        if (isSaleOrPosProc1OrPurchaseProc2)
                        {
                            credit = inv.VAT;
                            debt = 0.0;
                        }
                        else if (isSaleOrPosProc2OrPurchaseProc1)
                        {
                            credit = 0.0;
                            debt = inv.VAT;
                        }

                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;
                        account.Name = "الضريبة المضافة";
                        account.Code = 2222001.ToString();
                        account.Debt = debt;
                        account.Credit = credit;
                        account.Note = $"{inv.InvNote}  :{customer.Name}";
                        account.CCcode = (-1).ToString();
                        list.Add(account);
                    }

                    // ─── الخصم ───
                    if (inv.Discount > 0.0)
                    {
                        Account account = new Account();

                        if (isSaleOrPosProc1OrPurchaseProc2)
                        {
                            credit = 0.0;
                            debt = inv.Discount;
                        }
                        else if (isSaleOrPosProc2OrPurchaseProc1)
                        {
                            credit = inv.Discount;
                            debt = 0.0;
                        }

                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;

                        if (inv.InvoiceType == InvoiceType.Purchase)
                        {
                            account.Name = " خصم مكتسب ";
                            account.Code = "3200003";
                        }
                        else
                        {
                            account.Name = " خصم ممنوح";
                            account.Code = "4100003";
                        }

                        account.Debt = debt;
                        account.Credit = credit;
                        account.Note = "خصومات";
                        account.CCcode = (-1).ToString();
                        list.Add(account);
                    }

                    // ─── النقدي ───
                    if (inv.Paycash > 0.0)
                    {
                        Account account = new Account();

                        if (isSaleOrPosProc1OrPurchaseProc2)
                        {
                            credit = 0.0;
                            debt = inv.Paycash;
                        }
                        else if (isSaleOrPosProc2OrPurchaseProc1)
                        {
                            credit = inv.Paycash;
                            debt = 0.0;
                        }

                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;
                        account.Name = treasury.Name;
                        account.Code = treasury.AccCode;
                        account.Debt = debt;
                        account.Credit = credit;
                        account.Note = $"{inv.InvNote}  :{customer.Name}";
                        account.CCcode = (-1).ToString();
                        list.Add(account);
                    }

                    // ─── ATM ───
                    if (inv.PayATM > 0.0)
                    {
                        Account account = new Account();

                        if (isSaleOrPosProc1OrPurchaseProc2)
                        {
                            credit = 0.0;
                            debt = inv.PayATM;
                        }
                        else if (isSaleOrPosProc2OrPurchaseProc1)
                        {
                            credit = inv.PayATM;
                            debt = 0.0;
                        }

                        account.EntryGlobalID = entry.EntryGlobalID;
                        account.EntryNo = entry.EntryNo;
                        account.Name = bank.Name;
                        account.Code = bank.AccCode;
                        account.Debt = debt;
                        account.Credit = credit;
                        account.Note = $"{inv.InvNote}  :{customer.Name}";
                        account.CCcode = (-1).ToString();
                        list.Add(account);
                    }
                }

                entry.Accounts = list;
                return entry;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    GetErrorMessage("خطأ في ربط البيانات",
                                    "error in Binding data",
                                    ex.Message),
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return null;
            }
        }

        #endregion

        #region BindReceiptToEntry

        public Entry BindReceiptToEntry(Receipt receipt)
        {
            try
            {
                Entry entry = new Entry();
                List<Account> list = new List<Account>();

                entry.EntryGlobalID = receipt.EntryGlobalID;
                entry.ClientCode = Sync.ClientCode;
                entry.EntryNo = int.Parse(
                    receipt.EntryGlobalID.Substring(receipt.EntryGlobalID.LastIndexOf('-') + 1));
                entry.EntryDate = DateTime.Now;
                entry.ReffNo = receipt.ReceiptNo.ToString();
                entry.RefDate = receipt.ReceiptDate.Value;
                entry.Type = (EntryType)receipt.ReceiptType.Value;
                entry.State = 1;
                entry.Note = receipt.Notes;
                entry.Branch = receipt.BranchID.Value;
                entry.EmpID = receipt.EmpId.Value;
                entry.DistBranch = Sync.DistBranch;
                entry.BranchType = Sync.BranchType;

                if (receipt.Payment.HasValue && receipt.Payment.Value > 0.0)
                {
                    if (receipt.PaymentType.HasValue && receipt.PaymentType == 1)
                    {
                        string receiptInfo =
                            $"{receipt.ReceiptType}{receipt.PaymentType} رقم:{receipt.ReceiptNo}";

                        // ─── الدائن ───
                        Account creditAccount = new Account
                        {
                            EntryGlobalID = entry.EntryGlobalID,
                            EntryNo = entry.EntryNo,
                            Name = receipt.CreditAcc,
                            Code = receipt.CreditAcc,
                            Debt = 0.0,
                            Credit = receipt.NetVal.Value,
                            Note = receiptInfo,
                            CCcode = (-1).ToString()
                        };
                        list.Add(creditAccount);

                        // ─── المدين ───
                        Account debitAccount = new Account
                        {
                            EntryGlobalID = entry.EntryGlobalID,
                            EntryNo = entry.EntryNo,
                            Name = receipt.DebitAcc,
                            Code = receipt.DebitAcc,
                            Debt = receipt.Payment.Value,
                            Credit = 0.0,
                            Note = receiptInfo,
                            CCcode = receipt.Cccode
                        };
                        list.Add(debitAccount);

                        // ─── الضريبة ───
                        if (receipt.VAT.HasValue && receipt.VAT.Value > 0.0)
                        {
                            Account vatAccount = new Account
                            {
                                EntryGlobalID = entry.EntryGlobalID,
                                EntryNo = entry.EntryNo,
                                Name = "الضريبة المضافة ",
                                Code = "2222001",
                                Debt = receipt.VAT.Value,
                                Credit = 0.0,
                                Note = receiptInfo,
                                CCcode = receipt.Cccode
                            };
                            list.Add(vatAccount);
                        }
                    }
                }

                entry.Accounts = list;
                return entry;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    GetErrorMessage("خطأ في ربط البيانات",
                                    "error in Binding data",
                                    ex.Message),
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return null;
            }
        }

        #endregion

        #region BindCloseShiftToEntry

        public Entry BindCloseShiftToEntry(CloseShift inv, bool EnterBalanceRequired)
        {
            try
            {
                Treasury treasury = new Treasury(inv.Treasury);

                Entry entry = new Entry();
                List<Account> list = new List<Account>();
                var banks = new List<CloseShiftBanks>();

                string branchCostCenter = Common.GetBranchCostCenter(MainClass.BranchNo);
                string employeeName = Common.GetEmployeeName(inv.EmpId);

                entry.EntryGlobalID = inv.EntryGlobalID;
                entry.ClientCode = Sync.ClientCode;
                entry.EntryNo = int.Parse(
                    inv.EntryGlobalID.Substring(inv.EntryGlobalID.LastIndexOf('-') + 1));
                entry.EntryDate = inv.CloseTime;
                entry.ReffNo = inv.ClosedId.ToString();
                entry.RefDate = inv.CloseTime;
                entry.Type = EntryType.EntryReceipt;
                entry.State = 1;
                entry.Note = $"اغلاق اليومية خاصة الموظف {employeeName} رقم{inv.ClosedId}";
                entry.Branch = MainClass.BranchNo;
                entry.EmpID = inv.EmpId;
                entry.DistBranch = Sync.DistBranch;
                entry.BranchType = Sync.BranchType;
                entry.ISDeleted = false;

                decimal cashSales = 0m;
                decimal netCash = 0m;
                decimal madaTotal = 0m;
                decimal vatTotal = 0m;
                decimal salesNet = 0m;
                decimal returnNet = 0m;
                decimal additions = 0m;
                decimal insurance = 0m;
                decimal discount = 0m;
                decimal extraVAT = 0m;
                decimal remainder = 0m;
                decimal paidAmount = 0m;
                decimal creditSales = 0m;

                foreach (CloseShiftDetail detail in inv.CloseShiftDetails)
                {
                    InvoiceObj invObj = new InvoiceObj(detail.InvoiceType, 1);

                    double vatAmount;
                    double netAmount;

                    if (detail.PayType == 7)
                    {
                        double total = Convert.ToDouble(detail.Cash + detail.Mada + detail.Visa);
                        vatAmount = total - Math.Round(total / (1.0 + invObj.VAT / 100.0), 3);
                        netAmount = total - vatAmount;
                    }
                    else
                    {
                        vatAmount = Convert.ToDouble(detail.VAT);
                        netAmount = Convert.ToDouble(detail.InvTotal);
                    }

                    bool isSaleType = detail.InvoiceType == 3 ||
                                      detail.InvoiceType == 2 ||
                                      detail.InvoiceType == 11;
                    bool isPaidType = detail.PayType != -1 && detail.PayType != 5;

                    if (isSaleType && detail.ProcType == 1 && isPaidType)
                        cashSales += detail.Cash;
                    else if (isSaleType && detail.ProcType == 2 && isPaidType)
                        cashSales -= detail.Cash;
                    else if (detail.InvoiceType == 11 && detail.ProcType == 3)
                        cashSales += detail.Cash;

                    if (detail.InvoiceType == 1 && detail.ProcType == 1 && isPaidType)
                        cashSales -= detail.Cash;
                    else if (detail.InvoiceType == 1 && detail.ProcType == 2 && isPaidType)
                        cashSales += detail.Cash;

                    if (detail.PayType == -1 && isSaleType)
                        creditSales += (decimal)netAmount;

                    if (detail.InvoiceType != 3 || !isPaidType)
                        continue;

                    if (detail.ProcType == 1)
                    {
                        netCash += detail.Cash;

                        if (detail.PayType == 2 && detail.BankId > 2)
                        {
                            banks.Add(new CloseShiftBanks
                            {
                                BankId = detail.BankId,
                                Amount = detail.Mada
                            });
                        }
                        else
                        {
                            madaTotal += detail.Mada;
                        }

                        vatTotal += (decimal)vatAmount;
                        extraVAT += detail.ExtraVAT;
                        salesNet += (decimal)netAmount;

                        if (detail.Additions != 0m)
                        {
                            decimal addVal = detail.Additions;
                            if (invObj.PriceIncVAT)
                            {
                                double netAdd = Math.Round(
                                    Convert.ToDouble(detail.Additions) / (1.0 + invObj.VAT / 100.0),
                                    2, MidpointRounding.AwayFromZero);
                                addVal -= (decimal)(Convert.ToDouble(detail.Additions) - netAdd);
                            }
                            additions += addVal;
                        }

                        insurance += detail.Insurance;
                        discount += detail.Discount;
                        remainder += detail.Remainder;

                        if (detail.PaymentStatus == 3)
                        {
                            paidAmount += detail.Cash;
                            paidAmount += detail.Mada;
                        }
                    }
                    else if (detail.ProcType == 2)
                    {
                        netCash -= detail.Cash;

                        if (detail.PayType == 2 && detail.BankId > 2)
                        {
                            var bankEntry = new CloseShiftBanks { BankId = detail.BankId };
                            bankEntry.Amount -= detail.Mada;
                            banks.Add(bankEntry);
                        }
                        else
                        {
                            madaTotal += detail.Mada;
                        }

                        vatTotal -= (decimal)vatAmount;
                        extraVAT -= detail.ExtraVAT;
                        returnNet += (decimal)netAmount;

                        if (detail.Additions != 0m)
                        {
                            decimal addVal = detail.Additions;
                            if (invObj.PriceIncVAT)
                            {
                                string raw = (Convert.ToDouble(detail.Additions) /
                                              (1.0 + invObj.VAT / 100.0)).ToString();
                                if (raw.IndexOf('.') > -1)
                                    raw = raw.Substring(0, raw.IndexOf('.') + 3);
                                double netAdd = double.TryParse(
                                    string.IsNullOrEmpty(raw) ? "0" : raw,
                                    out double parsed) ? parsed : 0;
                                addVal -= (decimal)(Convert.ToDouble(detail.Additions) - netAdd);
                            }
                            additions -= addVal;
                        }

                        insurance -= detail.Insurance;
                        discount -= detail.Discount;
                    }
                }

                if (!EnterBalanceRequired && inv.CashierBalance == 0m)
                    inv.CashierBalance = netCash;

                // ─── دالة مساعدة داخلية لإضافة حساب ───
                void AddAccount(string code, string name, double amount, string note, string cc)
                {
                    Account acc = new Account
                    {
                        EntryGlobalID = entry.EntryGlobalID,
                        EntryNo = entry.EntryNo,
                        Name = name,
                        Code = code,
                        CCcode = cc,
                        Note = note
                    };
                    SetDebtCredit(acc, amount);
                    list.Add(acc);
                }

                string GetTreasuryCode() =>
                    MainClass.EmpNo == 0 ? treasury.AccCode : User.TreasuryAcc;

                string GetTreasuryName() =>
                    MainClass.EmpNo == 0
                        ? treasury.Name
                        : Common.GetAccountName(int.Parse(User.TreasuryAcc));

                string noteEmp = $" : خاصة الموظف: رقم{inv.ClosedId}";

                // ─── صندوق النقدي (مبيعات) ───
                if (netCash != 0m)
                    AddAccount(GetTreasuryCode(), GetTreasuryName(),
                        Convert.ToDouble(netCash),
                        $" نقدي في الصندوق {employeeName}{noteEmp}",
                        (-1).ToString());

                // ─── الشبكة ───
                if (madaTotal > 0m)
                {
                    string madaName = Common.GetAccountName(int.Parse("1221001"));
                    AddAccount("1221001", madaName,
                        Convert.ToDouble(madaTotal),
                        $" شبكة {employeeName}{noteEmp}",
                        (-1).ToString());
                }

                // ─── بنوك إضافية ───
                if (banks.Count > 0)
                {
                    var grouped = banks
                        .GroupBy(b => b.BankId)
                        .Select(g => new { BankId = g.Key, Total = g.Sum(z => z.Amount) });

                    foreach (var item in grouped)
                    {
                        Bank bank = new Bank(item.BankId);
                        AddAccount(bank.AccCode, bank.Name,
                            Convert.ToDouble(item.Total),
                            $" {bank.Name} {employeeName}{noteEmp}",
                            (-1).ToString());
                    }
                }

                // ─── صافي المبيعات ───
                salesNet += discount;
                if (salesNet > 0m)
                {
                    string salesName = Common.GetAccountName(int.Parse("4100001"));
                    Account salesAcc = new Account
                    {
                        EntryGlobalID = entry.EntryGlobalID,
                        EntryNo = entry.EntryNo,
                        Name = salesName,
                        Code = 4100001.ToString(),
                        CCcode = branchCostCenter,
                        Note = $" مبيعات {employeeName}{noteEmp}"
                    };
                    double salesVal = Convert.ToDouble(salesNet);
                    if (salesVal < 0.0) { salesAcc.Debt = FormatAmount(-salesVal); salesAcc.Credit = 0.0; }
                    else { salesAcc.Debt = 0.0; salesAcc.Credit = FormatAmount(salesVal); }
                    list.Add(salesAcc);
                }

                // ─── مرتجع المبيعات ───
                if (returnNet > 0m)
                {
                    string retName = Common.GetAccountName(int.Parse("4100002"));
                    Account retAcc = new Account
                    {
                        EntryGlobalID = entry.EntryGlobalID,
                        EntryNo = entry.EntryNo,
                        Name = retName,
                        Code = 4100002.ToString(),
                        CCcode = branchCostCenter,
                        Note = $" مرتجع مبيعات {employeeName}{noteEmp}"
                    };
                    double retVal = Convert.ToDouble(returnNet);
                    if (retVal < 0.0) { retAcc.Debt = 0.0; retAcc.Credit = FormatAmount(-retVal); }
                    else { retAcc.Debt = FormatAmount(retVal); retAcc.Credit = 0.0; }
                    list.Add(retAcc);
                }

                // ─── الضريبة الانتقائية ───
                if (extraVAT != 0m)
                {
                    string evName = Common.GetAccountName(int.Parse("2222002"));
                    AddAccount("2222002", evName,
                        Convert.ToDouble(extraVAT),
                        $"الضريبة الإنتقائية{employeeName}{noteEmp}",
                        (-1).ToString());
                }

                // ─── الضريبة المضافة ───
                if (vatTotal != 0m)
                {
                    double vatVal = Convert.ToDouble(vatTotal - extraVAT);
                    string vatName = Common.GetAccountName(int.Parse("2222001"));
                    AddAccount("2222001", vatName, vatVal,
                        $" الضريبة المضافة {employeeName}{noteEmp}",
                        (-1).ToString());
                }

                // ─── الإضافات ───
                if (additions != 0m)
                {
                    string addName = Common.GetAccountName(int.Parse("4200002"));
                    AddAccount("4200002", addName,
                        Convert.ToDouble(additions),
                        $" إضافات {employeeName}{noteEmp}",
                        (-1).ToString());
                }

                // ─── التأمين ───
                if (insurance != 0m)
                {
                    string insName = Common.GetAccountName(int.Parse("22210001"));
                    AddAccount("22210001", insName,
                        Convert.ToDouble(insurance),
                        $" تأمين {employeeName}{noteEmp}",
                        (-1).ToString());
                }

                // ─── الخصومات ───
                if (discount != 0m)
                {
                    string discName = Common.GetAccountName(int.Parse("4100003"));
                    AddAccount("4100003", discName,
                        Convert.ToDouble(discount),
                        $" خصومات ممنوحة {employeeName}{noteEmp}",
                        (-1).ToString());
                }

                // ─── المبالغ المستحقة ───
                if (remainder != 0m)
                {
                    string remName = Common.GetAccountName(int.Parse("12310001"));
                    AddAccount("12310001", remName,
                        Convert.ToDouble(remainder),
                        $" مبالغ مستحقة  {employeeName}{noteEmp}",
                        (-1).ToString());
                }

                // ─── المبالغ المسددة ───
                if (paidAmount > 0m)
                {
                    string paidName = Common.GetAccountName(int.Parse("12310001"));
                    Account paidAcc = new Account
                    {
                        EntryGlobalID = entry.EntryGlobalID,
                        EntryNo = entry.EntryNo,
                        Name = paidName,
                        Code = 12310001.ToString(),
                        Debt = 0.0,
                        Credit = FormatAmount(Convert.ToDouble(paidAmount)),
                        Note = $" مبالغ تم تسديدة  {employeeName}{noteEmp}",
                        CCcode = (-1).ToString()
                    };
                    list.Add(paidAcc);
                }

                // ─── نقدي مشتريات ───
                if (cashSales != 0m)
                {
                    Account cashPurchAcc = new Account
                    {
                        EntryGlobalID = entry.EntryGlobalID,
                        EntryNo = entry.EntryNo,
                        Name = GetTreasuryName(),
                        Code = GetTreasuryCode(),
                        Note = $" نقدي في الصندوق {employeeName}{noteEmp}",
                        CCcode = (-1).ToString()
                    };
                    if (cashSales > 0m) { cashPurchAcc.Debt = 0.0; cashPurchAcc.Credit = Convert.ToDouble(Math.Abs(cashSales)); }
                    else { cashPurchAcc.Debt = Convert.ToDouble(Math.Abs(cashSales)); cashPurchAcc.Credit = 0.0; }
                    list.Add(cashPurchAcc);
                }

                // ─── عهدة الإغلاق ───
                if (inv.CashierBalance != 0m)
                {
                    string balName = Common.GetAccountName(1211002);
                    Account balAcc = new Account
                    {
                        EntryGlobalID = entry.EntryGlobalID,
                        EntryNo = entry.EntryNo,
                        Name = balName,
                        Code = 1211002.ToString(),
                        Note = $" عهدة الإغلاق {employeeName}{noteEmp}",
                        CCcode = (-1).ToString()
                    };
                    if (inv.CashierBalance > 0m)
                    {
                        balAcc.Debt = Convert.ToDouble(inv.CashierBalance);
                        balAcc.Credit = 0.0;
                    }
                    else
                    {
                        balAcc.Debt = 0.0;
                        balAcc.Credit = FormatAmount(Convert.ToDouble(-1m * inv.CashierBalance));
                    }
                    list.Add(balAcc);
                }

                // ─── فرق الصندوق ───
                string diffName = Common.GetAccountName(3110004);
                if (cashSales > inv.CashierBalance)
                {
                    Account diffAcc = new Account
                    {
                        EntryGlobalID = entry.EntryGlobalID,
                        EntryNo = entry.EntryNo,
                        Name = diffName,
                        Code = 3110004.ToString(),
                        Debt = FormatAmount(Convert.ToDouble(cashSales - inv.CashierBalance)),
                        Credit = 0.0,
                        Note = $" فرق بالصندوق {employeeName}{noteEmp}",
                        CCcode = (-1).ToString()
                    };
                    list.Add(diffAcc);
                }
                else if (cashSales < inv.CashierBalance)
                {
                    Account diffAcc = new Account
                    {
                        EntryGlobalID = entry.EntryGlobalID,
                        EntryNo = entry.EntryNo,
                        Name = diffName,
                        Code = 3110004.ToString(),
                        Debt = 0.0,
                        Credit = FormatAmount(Convert.ToDouble(inv.CashierBalance - cashSales)),
                        Note = $" فرق بالصندوق {employeeName}{noteEmp}",
                        CCcode = (-1).ToString()
                    };
                    list.Add(diffAcc);
                }

                entry.Accounts = list;
                return entry;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    GetErrorMessage("خطأ في ربط البيانات",
                                    "error in Binding data",
                                    ex.Message),
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return null;
            }
        }

        #endregion

        #region SaveEntry

        public bool SaveEnty(Entry entry)
        {
            using SqlConnection conn = MainClass.ConnObj();

            if (conn.State != ConnectionState.Open)
                conn.Open();

            SqlTransaction transaction = conn.BeginTransaction();

            try
            {
                // ─── التحقق من وجود القيد ───
                bool isNew = true;
                using (SqlCommand checkCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Entry WHERE GlobalID = @GlobalID",
                    conn, transaction))
                {
                    checkCmd.Parameters.AddWithValue("@GlobalID", entry.EntryGlobalID);
                    isNew = Convert.ToInt32(checkCmd.ExecuteScalar()) == 0;
                }

                if (!isNew)
                {
                    using SqlCommand delCmd = new SqlCommand(
                        "DELETE FROM Entry_Sub WHERE EntryGlobalID = @GlobalID",
                        conn, transaction);
                    delCmd.Parameters.AddWithValue("@GlobalID", entry.EntryGlobalID);
                    delCmd.ExecuteNonQuery();
                }

                string sql = isNew ? StoredQueries.InsertEntry : StoredQueries.UpdateEntry;
                using (SqlCommand cmd = new SqlCommand(sql, conn, transaction))
                {
                    cmd.Parameters.Add("@GlobalID", SqlDbType.VarChar).Value = entry.EntryGlobalID;
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = entry.EntryNo;
                    cmd.Parameters.Add("@date", SqlDbType.DateTime).Value = entry.EntryDate;
                    cmd.Parameters.Add("@doc_no", SqlDbType.Int).Value = entry.ReffNo;
                    cmd.Parameters.Add("@type", SqlDbType.Int).Value = (int)entry.Type;
                    cmd.Parameters.Add("@state", SqlDbType.Int).Value = entry.State;
                    cmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value = entry.Note ?? string.Empty;
                    cmd.Parameters.Add("@branch", SqlDbType.Int).Value = entry.Branch;
                    cmd.Parameters.Add("@EmpID", SqlDbType.Int).Value = entry.EmpID;
                    cmd.Parameters.Add("@Sync", SqlDbType.Bit).Value = false;
                    cmd.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = entry.ISDeleted;
                    cmd.Parameters.Add("@IsVAT", SqlDbType.Bit).Value = entry.IsVAT;
                    cmd.ExecuteNonQuery();
                }

                foreach (Account account in entry.Accounts)
                {
                    using SqlCommand subCmd = new SqlCommand(
                        StoredQueries.InsertEntrySub, conn, transaction);
                    subCmd.Parameters.Add("@EntryGlobalID", SqlDbType.NVarChar).Value = entry.EntryGlobalID;
                    subCmd.Parameters.Add("@res_id", SqlDbType.Int).Value = account.EntryNo;
                    subCmd.Parameters.Add("@dept", SqlDbType.Float).Value = account.Debt;
                    subCmd.Parameters.Add("@credit", SqlDbType.Float).Value = account.Credit;
                    subCmd.Parameters.Add("@acc_no", SqlDbType.Int).Value = account.Code;
                    subCmd.Parameters.Add("@CCcode", SqlDbType.Int).Value = account.CCcode;
                    subCmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value = account.Note ?? string.Empty;
                    subCmd.Parameters.Add("@branch", SqlDbType.Int).Value = entry.Branch;
                    subCmd.Parameters.Add("@salesman", SqlDbType.Int).Value = account.salesman;
                    subCmd.ExecuteNonQuery();
                }

                if (filePathDocument != null)
                {
                    foreach (string _ in filePathDocument)
                        insertDocument(entry.EntryGlobalID);
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                MessageBox.Show(
                    GetErrorMessage("خطأ أثناء حفظ القيد",
                                    "error in saving",
                                    ex.Message),
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        #endregion

        #region ReadEntryOnline

        public void ReadEntyOnline(Entry entry)
        {
            using SqlConnection conn = MainClass.ConnObj();

            if (conn.State != ConnectionState.Open)
                conn.Open();

            SqlTransaction transaction = conn.BeginTransaction();

            try
            {
                bool isNew = true;
                using (SqlCommand checkCmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Entry WHERE GlobalID = @GlobalID",
                    conn, transaction))
                {
                    checkCmd.Parameters.AddWithValue("@GlobalID", entry.EntryGlobalID);
                    isNew = Convert.ToInt32(checkCmd.ExecuteScalar()) == 0;
                }

                if (!isNew)
                {
                    using SqlCommand delCmd = new SqlCommand(
                        "DELETE FROM Entry_Sub WHERE EntryGlobalID = @GlobalID",
                        conn, transaction);
                    delCmd.Parameters.AddWithValue("@GlobalID", entry.EntryGlobalID);
                    delCmd.ExecuteNonQuery();
                }

                string sql = isNew ? StoredQueries.InsertEntry : StoredQueries.UpdateEntry;
                using (SqlCommand cmd = new SqlCommand(sql, conn, transaction))
                {
                    cmd.Parameters.Add("@GlobalID", SqlDbType.VarChar).Value = entry.EntryGlobalID;
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = entry.EntryNo;
                    cmd.Parameters.Add("@date", SqlDbType.DateTime).Value = entry.EntryDate;
                    cmd.Parameters.Add("@doc_no", SqlDbType.Int).Value = entry.ReffNo;
                    cmd.Parameters.Add("@type", SqlDbType.Int).Value = (int)entry.Type;
                    cmd.Parameters.Add("@state", SqlDbType.Int).Value = entry.State;
                    cmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value = entry.Note ?? string.Empty;
                    cmd.Parameters.Add("@branch", SqlDbType.Int).Value = entry.Branch;
                    cmd.Parameters.Add("@EmpID", SqlDbType.Int).Value = entry.EmpID;
                    cmd.Parameters.Add("@Sync", SqlDbType.Bit).Value = true;
                    cmd.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = entry.ISDeleted;
                    cmd.Parameters.Add("@IsVAT", SqlDbType.Bit).Value = entry.IsVAT;
                    cmd.ExecuteNonQuery();
                }

                foreach (Account account in entry.Accounts)
                {
                    using SqlCommand subCmd = new SqlCommand(
                        StoredQueries.InsertEntrySub, conn, transaction);
                    subCmd.Parameters.Add("@EntryGlobalID", SqlDbType.NVarChar).Value = entry.EntryGlobalID;
                    subCmd.Parameters.Add("@res_id", SqlDbType.Int).Value = account.EntryNo;
                    subCmd.Parameters.Add("@dept", SqlDbType.Float).Value = account.Debt;
                    subCmd.Parameters.Add("@credit", SqlDbType.Float).Value = account.Credit;
                    subCmd.Parameters.Add("@acc_no", SqlDbType.Int).Value = account.Code;
                    subCmd.Parameters.Add("@CCcode", SqlDbType.Int).Value = account.CCcode;
                    subCmd.Parameters.Add("@notes", SqlDbType.NVarChar).Value = account.Note ?? string.Empty;
                    subCmd.Parameters.Add("@branch", SqlDbType.Int).Value = entry.Branch;
                    subCmd.Parameters.Add("salesman", SqlDbType.Int).Value = entry.salesman;
                    subCmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                MessageBox.Show(
                    GetErrorMessage("خطأ أثناء المزامنة",
                                    "error in saving",
                                    ex.Message),
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region ShowEntrySource

        public static void ShowEntrySource(string glid)
        {
            using SqlConnection conn = MainClass.ConnObj();

            SqlDataAdapter entryAdapter = new SqlDataAdapter(
                $"SELECT doc_no, type, id, branch FROM Entry WHERE GlobalId = N'{glid}'", conn);
            DataTable entryTable = new DataTable();
            entryAdapter.Fill(entryTable);

            if (entryTable.Rows.Count == 0)
                return;

            DataRow entryRow = entryTable.Rows[0];
            int docNo = Convert.ToInt32(entryRow["id"]);
            int branch = Convert.ToInt32(entryRow["branch"]);
            int entryType = Convert.ToInt32(entryRow["type"]);

            SqlDataAdapter invAdapter = new SqlDataAdapter(
                $"SELECT id, proc_type, inv_type FROM Inv " +
                $"WHERE IS_Deleted=0 AND EntryID={docNo} AND branch={branch}", conn);
            DataTable invTable = new DataTable();
            invAdapter.Fill(invTable);

            if (invTable.Rows.Count > 0)
            {
                DataRow row = invTable.Rows[0];
                int invType = Convert.ToInt32(row["inv_type"]);
                int procType = Convert.ToInt32(row["proc_type"]);
                int invId = Convert.ToInt32(row["id"]);

                string invQuery(int it, int pt) =>
                    $"SELECT * FROM Inv WHERE IS_Deleted=0 AND inv_type={it} AND proc_type={pt} AND id={invId} AND branch={branch}";

                if (invType == 1 && procType == 1)
                {
                    frmInvPurch f = new frmInvPurch();
                    f.InvType = 1; f.ProcType = 1;
                    f.WindowState = System.Windows.WindowState.Maximized;
                    f.Show(); f.Navigate(invQuery(1, 1)); f.Activate();
                }
                else if (invType == 1 && procType == 2)
                {
                    frmInvPurch f = new frmInvPurch { Title = "مرتجع مشتريات" };
                    f.InvType = 1; f.ProcType = 2;
                    f.WindowState = System.Windows.WindowState.Maximized;
                    f.Show(); f.Navigate(invQuery(1, 2)); f.Activate();
                }
                else if (invType == 2 && procType == 1)
                {
                    frmInvSale f = new frmInvSale();
                    f.InvType = 2; f.ProcType = 1;
                    f.WindowState = System.Windows.WindowState.Maximized;
                    f.Show(); f.Navigate(invQuery(2, 1)); f.Activate();
                }
                else if (invType == 2 && procType == 2)
                {
                    frmInvSale f = new frmInvSale { Title = "مرتجع مبيعات " };
                    f.InvType = 2; f.ProcType = 2;
                    f.WindowState = System.Windows.WindowState.Maximized;
                    f.Show(); f.Navigate(invQuery(2, 2)); f.Activate();
                }
                else if (invType == 3 && procType == 1)
                {
                    frmInvPOS f = new frmInvPOS { ProcType = 1 };
                    f.WindowState = System.Windows.WindowState.Maximized;
                    f.Show(); f.Navigate(invQuery(3, 1)); f.Activate();
                }
                else if (invType == 3 && procType == 2)
                {
                    frmInvPOS f = new frmInvPOS { ProcType = 2, Title = "مرتجع" };
                    f.WindowState = System.Windows.WindowState.Maximized;
                    f.Show(); f.Navigate(invQuery(3, 2)); f.Activate();
                }
                return;
            }

            // ─── قيود أخرى ───
            string docNoStr = entryRow["doc_no"].ToString();
            string branchStr = branch.ToString();

            void ShowReceipt<T>(string receiptType, Func<T> create, Action<T> configure)
                where T : DevExpress.Xpf.Core.ThemedWindow
            {
                T f = create();
                configure(f);
                f.Show();
                f.Activate();
            }

            switch (entryType)
            {
                case 3:
                    frminvoice inv2 = new frminvoice();
                    inv2.WindowState = System.Windows.WindowState.Maximized;
                    inv2.Show();
                    inv2.Navigate($"SELECT * FROM RentInvoice WHERE IS_Deleted=0 AND id={docNoStr} AND branch={branchStr}");
                    inv2.Activate();
                    break;

                case 5:
                    frmSandQ sq = new frmSandQ();
                    sq.Show();
                    sq.Navigate($"SELECT * FROM Receipts WHERE ReceiptType=5 AND ISDeleted=0 AND ReceiptNo={docNoStr} AND BranchID={branchStr}");
                    sq.Activate();
                    break;

                case 6:
                    frmSandD sd = new frmSandD();
                    sd.Show();
                    sd.Navigate($"SELECT * FROM Receipts WHERE ReceiptType=6 AND ISDeleted=0 AND ReceiptNo={docNoStr} AND BranchID={branchStr}");
                    sd.Activate();
                    break;

                case 7:
                    frmSandQD sqd = new frmSandQD();
                    sqd.Show();
                    sqd.Navigate($"SELECT * FROM Receipts WHERE ReceiptType=7 AND ISDeleted=0 AND ReceiptNo={docNoStr} AND BranchID={branchStr}");
                    sqd.Activate();
                    break;

                case 8:
                    frmSandSD ssd = new frmSandSD();
                    ssd.Show();
                    ssd.Navigate($"SELECT * FROM Receipts WHERE ReceiptType=8 AND ISDeleted=0 AND ReceiptNo={docNoStr} AND BranchID={branchStr}");
                    ssd.Activate();
                    break;

                case 9:
                    frmSandVAT svat = new frmSandVAT();
                    svat.Show();
                    svat.Navigate($"SELECT * FROM Receipts WHERE ReceiptType=9 AND ISDeleted=0 AND ReceiptNo={docNoStr} AND BranchID={branchStr}");
                    svat.Activate();
                    break;

                case 0:
                    FrmIntialRestraiction fir = new FrmIntialRestraiction();
                    fir.Show();
                    fir.Navigate($"SELECT * FROM Entry WHERE IS_Deleted=0 AND type=0 AND GlobalId=N'{glid}'");
                    fir.Activate();
                    break;

                case 10:
                    FrmNewEntry fne = new FrmNewEntry();
                    fne.Show();
                    fne.Navigate($"SELECT * FROM Entry WHERE IS_Deleted=0 AND type=10 AND GlobalId=N'{glid}'");
                    fne.Activate();
                    break;

                case 16:
                    frmCreditNote fcn = new frmCreditNote();
                    fcn.Show();
                    fcn.Navigate($"SELECT * FROM CreditDeptNotes WHERE IS_Deleted=0 AND Doc_Type=1 AND Doc_No={docNoStr}");
                    fcn.Activate();
                    break;

                case 17:
                    frmDebtNote fdn = new frmDebtNote();
                    fdn.Show();
                    fdn.Navigate($"SELECT * FROM CreditDeptNotes WHERE IS_Deleted=0 AND Doc_Type=2 AND Doc_No={docNoStr}");
                    fdn.Activate();
                    break;

                case 13:
                    frmRptEntries fre = new frmRptEntries();
                    fre.Show();
                    fre.Navigate($"SELECT * FROM Entry WHERE IS_Deleted=0 AND type=13 AND GlobalId=N'{glid}'");
                    fre.Activate();
                    break;
            }
        }

        #endregion

        #region BindEntryByID

        public Entry BindEntryByID(string glId)
        {
            using SqlConnection conn = MainClass.ConnObj();

            SqlDataAdapter entryAdapter = new SqlDataAdapter(
                $"SELECT * FROM Entry WHERE GlobalID = N'{glId}'", conn);
            DataTable entryTable = new DataTable();
            entryAdapter.Fill(entryTable);

            if (entryTable.Rows.Count == 0)
                return null;

            DataRow row = entryTable.Rows[0];
            Entry entry = new Entry
            {
                EntryGlobalID = row["GlobalID"].ToString(),
                ClientCode = Sync.ClientCode,
                EntryNo = Convert.ToInt32(row["id"]),
                EntryDate = Convert.ToDateTime(row["date"]),
                ReffNo = row["doc_no"].ToString(),
                RefDate = Convert.ToDateTime(row["date"]),
                Type = (EntryType)Convert.ToInt32(row["type"]),
                State = Convert.ToInt32(row["state"]),
                Note = row["notes"].ToString(),
                Branch = Convert.ToInt32(row["branch"]),
                EmpID = Convert.ToInt32(row["EmpID"]),
                DistBranch = Sync.DistBranch,
                BranchType = Sync.BranchType,
                Received = false
            };

            SqlDataAdapter subAdapter = new SqlDataAdapter(
                $"SELECT * FROM Entry_sub WHERE EntryGlobalID = N'{glId}'", conn);
            DataTable subTable = new DataTable();
            subAdapter.Fill(subTable);

            List<Account> list = new List<Account>();

            foreach (DataRow subRow in subTable.Rows)
            {
                double.TryParse(subRow["dept"].ToString(), out double debt);
                double.TryParse(subRow["credit"].ToString(), out double credit);

                Account account = new Account
                {
                    EntryGlobalID = entry.EntryGlobalID,
                    EntryNo = entry.EntryNo,
                    Name = Common.GetAccountName(Convert.ToInt32(subRow["acc_no"])),
                    Code = subRow["acc_no"].ToString(),
                    Debt = debt,
                    Credit = credit,
                    Note = subRow["notes"].ToString(),
                    CCcode = subRow["CCcode"].ToString(),
                    ClientCode = Sync.ClientCode
                };
                list.Add(account);
            }

            entry.Accounts = list;
            return entry;
        }

        #endregion

        #region GetEntryGlobalID

        public static void GetEntryGlobalID(ref string entryGlobalId, ref int entryNo)
        {
            using SqlConnection conn = MainClass.ConnObj();

            if (conn.State != ConnectionState.Open)
                conn.Open();

            using SqlCommand maxCmd = new SqlCommand(
                $"SELECT ISNULL(MAX(id), 0) FROM Entry WHERE branch = {MainClass.BranchNo}", conn);
            entryNo = Convert.ToInt32(maxCmd.ExecuteScalar());

            do
            {
                entryNo++;

                entryGlobalId = Sync.ActiveSync
                    ? $"{Sync.ClientCode}-{MainClass.BranchNo}-{entryNo}"
                    : $"{MainClass.BranchNo}-{entryNo}";

                using SqlCommand checkCmd = new SqlCommand(
                    $"SELECT COUNT(*) FROM Entry WHERE GlobalID = '{entryGlobalId}'", conn);
                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count == 0) break;
            }
            while (true);
        }

        #endregion

        #region GenerateEntryGlobalIDByEntryNo

        public static void GenerateEntryGlobalIDByEntryNo(
            ref string entryGlobalId, int entryNo, int branchNo)
        {
            entryGlobalId = Sync.ActiveSync
                ? $"{Sync.ClientCode}-{branchNo}-{entryNo}"
                : $"{branchNo}-{entryNo}";
        }

        #endregion

        #region GetEntryNobyType

        public static void GetEntryNobyType(int entryType, ref int entryNo)
        {
            string branchFilter = MainClass.BranchNo != -1
                ? $" AND branch = {MainClass.BranchNo}"
                : string.Empty;

            using SqlConnection conn = MainClass.ConnObj();
            if (conn.State == ConnectionState.Closed)
                conn.Open();

            using SqlCommand cmd = new SqlCommand(
                $"SELECT ISNULL(MAX(doc_no), 0) FROM Entry WHERE type = {entryType}{branchFilter}", conn);

            double.TryParse(cmd.ExecuteScalar()?.ToString(), out double maxVal);
            entryNo = Convert.ToInt32(Math.Round(maxVal + 1.0));
        }

        #endregion

        #region InvoiceNo

        public static int InvoiceNo(int invType, int procType)
        {
            using SqlConnection conn = MainClass.ConnObj();
            if (conn.State != ConnectionState.Open)
                conn.Open();

            using SqlCommand cmd = new SqlCommand(
                $"SELECT ISNULL(MAX(id), 0) FROM Inv " +
                $"WHERE branch = {MainClass.BranchNo} " +
                $"AND inv_type = {invType} " +
                $"AND proc_type = {procType}", conn);

            return Convert.ToInt32(cmd.ExecuteScalar()) + 1;
        }

        #endregion

        #region ReadEntryGlobalID

        public static string ReadEntryGlobalID(string docNo, int type, int branchNo)
        {
            using SqlConnection conn = MainClass.ConnObj();
            if (conn.State != ConnectionState.Open)
                conn.Open();

            using SqlCommand cmd = new SqlCommand(
                $"SELECT GlobalID FROM Entry " +
                $"WHERE branch = {branchNo} AND type = {type} AND doc_no = {docNo}", conn);

            return cmd.ExecuteScalar()?.ToString() ?? string.Empty;
        }

        #endregion

        #region SyncEntry

        public async Task<bool> SyncEntry(Entry entry, bool isNew)
        {
            if (entry == null)
                return false;

            EntryCRUD crud = new EntryCRUD(Sync.APIUrl);
            List<Entry> synced = new List<Entry>();
            entry.Received = false;

            if (Sync.ValidAPIUrl)
                synced = (List<Entry>)await crud.PostEntriesOnline(entry, isNew);
            else
                crud.AddEntryLocally(entry, isNew);

            if (synced.Count == 0)
                return false;

            using SqlConnection conn = MainClass.ConnObj();
            if (conn.State != ConnectionState.Open)
                conn.Open();

            foreach (Entry synced_entry in synced)
            {
                if (synced_entry == null) continue;

                using SqlCommand cmd = new SqlCommand(
                    $"UPDATE Entry SET Sync=1 WHERE GlobalID = N'{synced_entry.EntryGlobalID}'", conn);
                cmd.ExecuteNonQuery();
            }

            return true;
        }

        #endregion

        #region insertDocument

        public static void insertDocument(string invGlobalID)
        {
            string docsRoot = Properties.Settings.Default.DocsRootPath;

            if (!Directory.Exists(docsRoot))
                Directory.CreateDirectory(docsRoot);

            if (filePathDocument == null || filePathDocument.Length == 0)
                return;

            using SqlConnection conn = new SqlConnection(MainClass.connstr);

            foreach (string filePath in filePathDocument)
            {
                if (conn.State == ConnectionState.Open)
                    conn.Close();
                conn.Open();

                using SqlCommand maxCmd = new SqlCommand(
                    "SELECT ISNULL(MAX(id), 0) + 1 FROM Documents", conn);
                int nextId = Convert.ToInt32(maxCmd.ExecuteScalar());
                string extension = Path.GetExtension(filePath);
                string destPath = Path.Combine(docsRoot, $"P{nextId}{extension}");
                string fileName = Path.GetFileName(filePath);

                File.Copy(filePath, destPath, overwrite: true);

                using SqlCommand insertCmd = new SqlCommand(
                    "INSERT INTO Documents (FileName, FileUrl, GlobalID, type) " +
                    "VALUES (@FileName, @FileUrl, @GlobalID, 1)", conn);
                insertCmd.Parameters.AddWithValue("@FileName", fileName);
                insertCmd.Parameters.AddWithValue("@FileUrl", destPath);
                insertCmd.Parameters.AddWithValue("@GlobalID", invGlobalID);
                insertCmd.ExecuteNonQuery();
            }

            conn.Close();
        }

        #endregion

        #region getdocuments

        public static void getdocuments(ref DataTable dtt, string invGlobalID)
        {
            using SqlConnection conn = new SqlConnection(MainClass.connstr);
            using SqlCommand cmd = new SqlCommand(
                "SELECT Id, FileName, FileUrl FROM Documents " +
                "WHERE type = 1 AND GlobalID = @GlobalID", conn);

            cmd.Parameters.AddWithValue("@GlobalID", invGlobalID);

            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            DataTable dataTable = new DataTable();

            try
            {
                conn.Open();
                adapter.Fill(dataTable);

                if (dataTable.Rows.Count > 0)
                    dtt = dataTable;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"حدث خطأ أثناء استرجاع البيانات: {ex.Message}",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion
    }
}