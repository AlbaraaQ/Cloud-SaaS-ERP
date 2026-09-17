using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using AuditorAPI.Models;
using AuditorAPI.Models.DGVmodels;
using SmartAuditERP.Form_WPF;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Ws_Auditor;
using ZatcaIntegrationSDK.HelperContracts;
using SmartAuditERP.Form_WPF;

namespace SmartAuditERP
{

	public class InvoiceOperContract
	{
		public static string[] filePathDocument;

		public Invoice MappingInvoicecontract(ref Invoicecontract Inv)
		{
			Invoice invoice;
			invoice = new Invoice();
			InvoiceObj invoiceObj;
			invoiceObj = new InvoiceObj((int)Inv.InvoiceType, Inv.ProcType);
			Invoicecontract invoicecontract;
			if (Inv.ISNew)
			{
				Inv.InvoiceNo = InvoiceOperContract.InvoiceNocontract((int)Inv.InvoiceType, Inv.ProcType, invoiceObj.Prefixe);
				Inv.InvCombinedId = string.Concat(MainClass.BranchCode + "C" + Inv.InvoiceCode, Conversions.ToString(Inv.ProcType), Conversions.ToString(Inv.InvoiceNo));
				string EntryGlobalId;
				int EntryNo;
				if (Inv.InvoiceType == InvoiceType.HoldInv)
				{
					Invoicecontract obj;
					obj = Inv;
					EntryGlobalId = obj.EntryGlobalID;
					EntryNo = 0;
					EntryOper.GetEntryGlobalID(ref EntryGlobalId, ref EntryNo);
					obj.EntryGlobalID = EntryGlobalId;
				}
				else
				{
					Inv.EntryGlobalID = "-1";
				}
				Invoicecontract obj2;
				obj2 = Inv;
				EntryGlobalId = obj2.InvGlobalID;
				EntryNo = (invoicecontract = Inv).AutoIncrementID;
				InvoiceOperContract.GetInvoiceGlobalIDcontract(ref EntryGlobalId, ref EntryNo);
				invoicecontract.AutoIncrementID = EntryNo;
				obj2.InvGlobalID = EntryGlobalId;
			}
			else
			{
				if (Operators.CompareString(Inv.EntryGlobalID, "1-0", TextCompare: false) == 0)
				{
					if (Inv.InvoiceType == InvoiceType.HoldInv)
					{
						Invoicecontract obj3;
						obj3 = Inv;
						string EntryGlobalId;
						EntryGlobalId = obj3.EntryGlobalID;
						int EntryNo;
						EntryNo = 0;
						EntryOper.GetEntryGlobalID(ref EntryGlobalId, ref EntryNo);
						obj3.EntryGlobalID = EntryGlobalId;
					}
					else
					{
						Inv.EntryGlobalID = "-1";
					}
				}
				if (!string.IsNullOrEmpty(Inv.UUID))
				{
					invoice.UUID = Inv.UUID;
				}
				else
				{
					invoice.UUID = "";
				}
			}
			Customer customer;
			customer = new Customer(Inv.Customer);
			short taxType;
			taxType = 1;
			if (Operators.CompareString(customer.VATno, "", TextCompare: false) != 0)
			{
				taxType = 2;
			}
			else if (Inv.VAT <= 0.0)
			{
				taxType = 3;
			}
			if ((Inv.InvNote == null) | (Operators.CompareString(Inv.InvNote, "", TextCompare: false) == 0))
			{
				Inv.InvNote = InvoiceOper.GetInvoiceType((int)Inv.InvoiceType, Inv.ProcType, Inv.PayType, taxType) + " برقم " + Inv.InvoiceNo;
			}
			invoice.InvCombinedId = Inv.InvCombinedId;
			invoice.InvoiceNo = Inv.InvoiceNo;
			invoice.InvGlobalID = Inv.InvGlobalID;
			invoice.CloudID = Inv.CloudID;
			invoice.QRCode = Inv.QRCode;
			invoice.PIH = InvoiceOper.GetPIH(Inv.InvoiceNo, (int)Inv.InvoiceType, Inv.ProcType, Inv.Branch);
			invoice.previousUUID = InvoiceOperContract.GetPreviousUUIDcontract(Inv.InvoiceNo, (int)Inv.InvoiceType, Inv.ProcType, Inv.Branch);
			invoice.EntryGlobalID = Inv.EntryGlobalID;
			invoice.AutoIncrementID = Inv.AutoIncrementID;
			if (Inv.AutoIncrementID == 0)
			{
				invoice.AutoIncrementID = InvoiceOperContract.GetAutoIncrementIDcontract();
			}
			invoice.InvoiceType = Inv.InvoiceType;
			invoice.ProcType = Inv.ProcType;
			invoice.AdditionalCost = Inv.AdditionalCost;
			invoice.InvoiceStatus = Inv.InvoiceStatus;
			invoice.ClientCode = Sync.ClientCode;
			invoice.Total = Inv.Total;
			invoice.Delivery = Inv.Delivery;
			invoice.SumPrice = Inv.SumPrice;
			invoice.VAT = Inv.VAT;
			invoice.PriceIncVAT = Inv.PriceIncVAT;
			invoice.ExtraVAT = Inv.ExtraVAT;
			invoice.TotalWithholdingTax = Inv.TotalWithholdingTax;
			invoice.Net = Inv.Net;
			invoice.Discount = Inv.InvDiscount;
			invoice.TotDiscount = Inv.TotDiscount;
			invoice.Additions = Inv.Additions;
			invoice.Insurance = Inv.Insurance;
			invoice.Paycash = Inv.Paycash;
			invoice.Paid = Inv.Paid;
			invoice.PayATM = Inv.PayATM;
			invoice.PayType = Inv.PayType;
			invoice.Remainder = Inv.Remainder;
			invoice.Bank = Inv.Bank;
			invoice.OrderNo = Inv.OrderNo;
			invoice.OrderType = Inv.OrderType;
			invoice.TableNo = Inv.TableNo;
			invoice.Balance_previews = Inv.BalancePreviews;
			invoice.InvDate = DateTime.Parse(Inv.InvDate.ToShortDateString() + " " + Inv.InvTime.ToShortTimeString());
			invoice.User = Inv.User;
			invoice.Branch = Inv.Branch;
			invoice.DistBranch = Sync.DistBranch;
			invoice.BranchType = Sync.BranchType;
			invoice.Treasury = Inv.Treasury;
			invoice.InvCCcode = Inv.InvCCcode;
			invoice.Saleman = Inv.Saleman;
			invoice.Customer = Inv.Customer;
			invoice.InvNote = Inv.InvNote;
			invoice.IsDeleted = false;
			invoice.VATperc = Inv.VATperc;
			invoice.ReffNo = Inv.ReffNo;
			invoice.RefDate = Inv.RefDate;
			invoice.InvAccCode = Inv.InvAccCode;
			invoice.Store = Inv.Store;
			invoice.Currency = Inv.Currency;
			invoice.InvoiceItems = Inv.InvoiceItems;
			invoice.InvoiceCosts = Inv.InvoiceCosts;
			invoice.InvoicePayments = Inv.InvoicePayments;
			invoice.CashCustomerName = Inv.CashCustomerName;
			invoice.CashCustomerMobile = Inv.CashCustomerMobile;
			invoice.PaymentStatus = (int)Inv.PaymentStatus;
			invoice.BalancePreviews = Inv.BalancePreviews;
			invoice.contract_total_value = Inv.contract_total_value;
			invoice.original_work_amount = Inv.original_work_amount;
			invoice.special_discount = Inv.special_discount;
			invoice.amount_after_discount = Inv.amount_after_discount;
			invoice.vat_amount = Inv.vat_amount;
			invoice.total_with_vat = Inv.total_with_vat;
			invoice.work_guarantee = Inv.work_guarantee;
			invoice.net_due_this_payment = Inv.net_due_this_payment;
			invoice.previously_paid_amount = Inv.previously_paid_amount;
			invoice.remaining_contract_balance = Inv.remaining_contract_balance;
			if ((invoice.InvoiceType == (InvoiceType)21) | (invoice.InvoiceType == InvoiceType.ReturnSales))
			{
				invoice.InvCombinedId = Conversions.ToString(MainClass.BranchNo) + "N" + Conversions.ToString((int)invoice.InvoiceType) + Conversions.ToString(invoice.InvoiceNo);
			}
			List<Item> list;
			list = new List<Item>();
			foreach (InvoiceItem invoiceItem in Inv.InvoiceItems)
			{
				Item item;
				item = new Item();
				item.InvGlobalID = invoice.InvGlobalID;
				item.ClientCode = Sync.ClientCode;
				item.AutoIncrementID = Inv.AutoIncrementID;
				invoiceItem.InvGlobalID = invoice.InvGlobalID;
				invoiceItem.ClientCode = Sync.ClientCode;
				item.Code = invoiceItem.ItemCode;
				item.Name = invoiceItem.ItemName;
				item.ItemNo = invoiceItem.ItemId;
				item.ProcType = invoiceItem.InvertoryImpact;
				item.Description = invoiceItem.Description;
				item.Note = invoiceItem.ItemNotes;
				item.Quantity = invoiceItem.ItemQuantity;
				item.UnitEquality = invoiceItem.UnitEquality;
				item.PrimaryQnty = invoiceItem.ItemPrimaryQnty;
				item.Unit = invoiceItem.UnitID;
				item.UnitCode = invoiceItem.UnitCode;
				item.Price = invoiceItem.ItemPrice;
				item.ItemPriceWithoutVAT = invoiceItem.ItemPriceWithoutVAT;
				item.ItemDiscount = invoiceItem.ItemDiscount;
				item.Vat = invoiceItem.ItemVat;
				item.VatPerc = invoiceItem.ItemVatPerc;
				invoiceItem.ItemAdditionalTaxPerc = invoiceItem.ItemAdditionalTaxPerc;
				item.ItemAdditionalTax = invoiceItem.ItemAdditionalTax;
				item.WithholdingTax = invoiceItem.WithholdingTax;
				item.WithholdingTaxPerc = invoiceItem.WithholdingTaxPerc;
				item.Barcode = invoiceItem.ItemBarcode;
				if (invoiceItem.InvertoryImpact == 1)
				{
					item.ValiableStock = invoiceItem.ValiableInvertory + invoiceItem.ItemPrimaryQnty;
				}
				else if (invoiceItem.InvertoryImpact == 2)
				{
					item.ValiableStock = invoiceItem.ValiableInvertory - invoiceItem.ItemPrimaryQnty;
				}
				else
				{
					item.ValiableStock = invoiceItem.ValiableInvertory;
				}
				if ((Inv.InvoiceType == InvoiceType.Purchase) | (Inv.InvoiceType == InvoiceType.BeginingInventory))
				{
					invoiceItem.ItemCost = ItemOper.ItemDiscountValue(new decimal(Inv.SumPrice), new decimal(Inv.InvDiscount), invoiceItem.ItemPrice * invoiceItem.ItemQuantity, new decimal(invoiceItem.ItemPrimaryQnty), new decimal(invoiceItem.ItemDiscount));
					invoiceItem.ItemAddedCost = ItemOper.ItemAdditonalCos(Inv.Total, Convert.ToDouble(Inv.AdditionalCost), invoiceItem.ItemPrice * invoiceItem.ItemPrimaryQnty, invoiceItem.ItemPrimaryQnty);
				}
				else if (item.ValiableStock < 0.0)
				{
					invoiceItem.ItemCost = 0.0;
				}
				else
				{
					invoiceItem.ItemCost = ItemOper.AvgCost(invoiceItem.ItemId, MainClass.BranchNo);
				}
				item.AvegCost = invoiceItem.ItemCost;
				item.ItemAddedCost = invoiceItem.ItemAddedCost;
				item.Store = invoiceItem.InvertoryId;
				if (DateTime.Compare(invoiceItem.ItemExpireDate, DateTime.MinValue) <= 0)
				{
					item.ExpireDate = DateTime.Now.AddYears(2);
				}
				else
				{
					item.ExpireDate = invoiceItem.ItemExpireDate;
				}
				item.ItemCostCenter = invoiceItem.ItemCostCenter;
				item.Note = invoiceItem.ItemNotes;
				item.ProductId = invoiceItem.ProductId;
				item.EgyItemCode = invoiceItem.EgyItemCode;
				item.EgyCodeType = invoiceItem.EgyCodeType;
				list.Add(item);
			}
			List<InvoiceItem> Items;
			Items = (invoicecontract = Inv).InvoiceItems;
			this.BindingCompositeItems(ref Items, Inv.InvertoryImpact);
			invoicecontract.InvoiceItems = Items;
			invoice.Items = list;
			return invoice;
		}

		public static int InvoiceNocontract(int InvType, int ProcType, int Prefixe)
		{
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			if (sqlConnection.State != ConnectionState.Open)
			{
				sqlConnection.Open();
			}
			int num;
			num = checked((int)Convert.ToInt64(RuntimeHelpers.GetObjectValue(new SqlCommand("select ISNULL(MAX(id), 0) from InvContratct where branch=" + Conversions.ToString(MainClass.BranchNo) + " and inv_type=" + Conversions.ToString(InvType) + " and proc_type=" + Conversions.ToString(ProcType), sqlConnection).ExecuteScalar())) + 1);
			if (num == 1)
			{
				num = Prefixe;
			}
			if (sqlConnection.State != ConnectionState.Closed)
			{
				sqlConnection.Close();
			}
			return num;
		}

		public static string GetPreviousUUIDcontract(int InvoiceId, int InvType, int ProcType, int BranchID)
		{
			string result;
			try
			{
				int num;
				num = checked(InvoiceId - 1);
				SqlDataAdapter sqlDataAdapter;
				sqlDataAdapter = new SqlDataAdapter(selectConnection: MainClass.ConnObj(), selectCommandText: "Select IsNull(UUID,0) as PreviousUUID from InvContratct where id= " + Conversions.ToString(num) + "  and  inv_type=" + Conversions.ToString(InvType) + " and proc_type=" + Conversions.ToString(ProcType) + " and branch=" + Conversions.ToString(BranchID));
				DataTable dataTable;
				dataTable = new DataTable();
				sqlDataAdapter.Fill(dataTable);
				result = ((dataTable.Rows.Count <= 0) ? "" : ((!Conversions.ToBoolean(Operators.AndObject(Operators.CompareObjectNotEqual(dataTable.Rows[0]["PreviousUUID"], "0", TextCompare: false), Operators.CompareString(dataTable.Rows[0]["PreviousUUID"].ToString(), "", TextCompare: false) != 0))) ? "" : dataTable.Rows[0]["PreviousUUID"].ToString().Trim()));
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = "";
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static string GetPIH(int InvoiceId, int InvType, int ProcType, int BranchID)
		{
			string result = default(string);
			try
			{
				int num;
				num = checked(InvoiceId - 1);
				SqlDataAdapter sqlDataAdapter;
				sqlDataAdapter = new SqlDataAdapter(selectConnection: MainClass.ConnObj(), selectCommandText: "Select IsNull(InvoiceHash,0) as InvoiceHash from inv where id= " + Conversions.ToString(num) + "  and  inv_type=" + Conversions.ToString(InvType) + " and proc_type=" + Conversions.ToString(ProcType) + " and branch=" + Conversions.ToString(BranchID));
				DataTable dataTable;
				dataTable = new DataTable();
				sqlDataAdapter.Fill(dataTable);
				if (dataTable.Rows.Count > 0)
				{
					result = ((!Operators.ConditionalCompareObjectNotEqual(dataTable.Rows[0]["InvoiceHash"], "0", TextCompare: false)) ? "NWZlY2ViNjZmZmM4NmYzOGQ5NTI3ODZjNmQ2OTZjNzljMmRiYzIzOWRkNGU5MWI0NjcyOWQ3M2EyN2ZiNTdlOQ==" : dataTable.Rows[0]["InvoiceHash"].ToString().Trim());
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = "NWZlY2ViNjZmZmM4NmYzOGQ5NTI3ODZjNmQ2OTZjNzljMmRiYzIzOWRkNGU5MWI0NjcyOWQ3M2EyN2ZiNTdlOQ==";
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static void GetInvoiceGlobalIDcontract(ref string InvGlobalID, ref int AutoIncrementID)
		{
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			if (sqlConnection.State != ConnectionState.Open)
			{
				sqlConnection.Open();
			}
			new SqlCommand();
			checked
			{
				AutoIncrementID = (int)Convert.ToInt64(RuntimeHelpers.GetObjectValue(new SqlCommand("select ISNULL(MAX(proc_id), 0) from InvContratct", sqlConnection).ExecuteScalar()));
				do
				{
					AutoIncrementID++;
					if (Sync.ActiveSync)
					{
						InvGlobalID = Sync.ClientCode + "-" + MainClass.BranchNo + "-" + Conversions.ToString(AutoIncrementID);
					}
					else
					{
						InvGlobalID = MainClass.BranchNo + "-" + Conversions.ToString(AutoIncrementID);
					}
				}
				while (Convert.ToDouble(Operators.ConcatenateObject("", new SqlCommand("Select COUNT(*) from InvContratct where InvGlobalID= '" + InvGlobalID + "'", sqlConnection).ExecuteScalar())) > 0.0);
				if (sqlConnection.State != ConnectionState.Closed)
				{
					sqlConnection.Close();
				}
			}
		}

		public static int GetAutoIncrementIDcontract()
		{
			try
			{
				SqlConnection sqlConnection;
				sqlConnection = MainClass.ConnObj();
				if (sqlConnection.State != ConnectionState.Open)
				{
					sqlConnection.Open();
				}
				new SqlCommand();
				return checked((int)Convert.ToInt64(RuntimeHelpers.GetObjectValue(new SqlCommand("select ISNULL(MAX(proc_id), 1) from InvContratct", sqlConnection).ExecuteScalar())));
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return 1;
		}

		private void BindingCompositeItems(ref List<InvoiceItem> Items, int InvertoryImpact)
		{
			checked
			{
				try
				{
					SqlConnection selectConnection;
					selectConnection = MainClass.ConnObj();
					new List<InvoiceItem>();
					List<InvoiceItem> list;
					list = Items;
					foreach (InvoiceItem item in Items.ToList())
					{
						SqlDataAdapter sqlDataAdapter;
						sqlDataAdapter = new SqlDataAdapter("Select ItemComponents.Id, ItemComponents.ComponentId,ItemComponents.store, ItemComponents.itemId, ItemComponents.quantity, ItemComponents.price, ItemComponents.total,ItemComponents.type,ItemComponents.unit as unit_id, items.id as item_ID, items.Code as item_Code, items.name as Item_name,ItemUnits.perc as UnitEquality  From ItemComponents left join items on ItemComponents.ComponentId= items.id left join ItemUnits on ItemUnits.ItemId=ItemComponents.ComponentId where ItemUnits.unit=ItemComponents.unit and  ItemComponents.itemId=" + Conversions.ToString(item.ItemId), selectConnection);
						DataTable dataTable;
						dataTable = new DataTable();
						sqlDataAdapter.Fill(dataTable);
						if (dataTable.Rows.Count <= 0)
						{
							continue;
						}
						decimal value;
						value = default(decimal);
						int num;
						num = dataTable.Rows.Count - 1;
						for (int i = 0; i <= num; i++)
						{
							InvoiceItem invoiceItem;
							invoiceItem = new InvoiceItem();
							invoiceItem.InvGlobalID = item.InvGlobalID;
							invoiceItem.ClientCode = Sync.ClientCode;
							invoiceItem.InvertoryImpact = InvertoryImpact;
							invoiceItem.ItemRowIndex = item.ItemRowIndex;
							invoiceItem.ItemName = Conversions.ToString(dataTable.Rows[i]["Item_name"]);
							invoiceItem.ItemId = Conversions.ToInteger(dataTable.Rows[i]["ComponentId"]);
							invoiceItem.ItemCode = Conversions.ToString(dataTable.Rows[i]["item_Code"]);
							invoiceItem.UnitName = Conversions.ToString(dataTable.Rows[i]["unit_id"]);
							invoiceItem.InvertoryName = item.InvertoryName;
							invoiceItem.Description = "";
							invoiceItem.ItemQuantity = Conversion.Val(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["quantity"])) * item.ItemPrimaryQnty;
							invoiceItem.UnitEquality = Conversion.Val(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["UnitEquality"]));
							invoiceItem.ItemPrimaryQnty = Conversion.Val(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["quantity"])) * item.ItemPrimaryQnty * invoiceItem.UnitEquality;
							invoiceItem.UnitID = Conversions.ToInteger(dataTable.Rows[i]["unit_id"]);
							double num2;
							num2 = ItemOper.AvgCost(Convert.ToInt16(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["ComponentId"])), MainClass.BranchNo);
							if (num2 != 0.0)
							{
								invoiceItem.ItemPrice = num2;
							}
							else
							{
								invoiceItem.ItemPrice = Conversions.ToDouble(dataTable.Rows[i]["price"]);
							}
							invoiceItem.ItemCost = ItemOper.AvgCost(Conversions.ToInteger(dataTable.Rows[i]["ComponentId"]), MainClass.BranchNo);
							invoiceItem.ItemVat = 0.0;
							invoiceItem.ItemVatPerc = 0.0;
							invoiceItem.ItemBarcode = "";
							invoiceItem.ItemDiscount = 0.0;
							invoiceItem.ValiableInvertory = Inventory.CalcItemStock(item.InvertoryId, Conversions.ToInteger(dataTable.Rows[i]["ComponentId"]), MainClass.BranchNo);
							invoiceItem.InvertoryId = item.InvertoryId;
							invoiceItem.ItemExpireDate = DateTime.Now.AddYears(1);
							invoiceItem.ItemNotes = "";
							invoiceItem.ProductId = item.ItemId;
							value = new decimal(Convert.ToDouble(value) + invoiceItem.ItemCost * invoiceItem.ItemPrimaryQnty);
							list.Add(invoiceItem);
						}
						item.ItemCost = Convert.ToDouble(new decimal(Convert.ToDouble(value) / item.ItemPrimaryQnty));
					}
					Items = list;
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					ProjectData.ClearProjectError();
				}
			}
		}

		public Entry BindToEntry(Invoice inv)
		{
			Entry result;
			try
			{
				Customer customer;
				customer = new Customer(inv.Customer);
				InvoiceObj invoiceObj;
				invoiceObj = new InvoiceObj((int)inv.InvoiceType, inv.ProcType);
				Bank bank;
				bank = new Bank(inv.Bank);
				Treasury treasury;
				treasury = new Treasury(inv.Treasury);
				Entry entry;
				entry = new Entry();
				List<Account> list;
				list = new List<Account>();
				new Account();
				double num;
				num = inv.Net;
				double num2;
				num2 = inv.Paycash;
				double num3;
				num3 = inv.PayATM;
				double num4;
				num4 = inv.VAT;
				double num5;
				num5 = inv.Remainder;
				double num6;
				num6 = inv.TotDiscount;
				if (((inv.InvoiceType == InvoiceType.Sale) | (inv.InvoiceType == InvoiceType.POS) | (inv.InvoiceType == InvoiceType.HoldInv)) & (inv.PayType == 7))
				{
					num = num5;
					num2 = 0.0;
					num3 = 0.0;
					num4 = num - num / (1.0 + invoiceObj.VAT / 100.0);
					num5 = 0.0;
					num6 = 0.0;
				}
				entry.EntryGlobalID = inv.EntryGlobalID;
				entry.ClientCode = Sync.ClientCode;
				entry.EntryNo = Conversions.ToInteger(inv.EntryGlobalID.Substring(checked(inv.EntryGlobalID.LastIndexOf("-") + 1)));
				entry.EntryDate = inv.InvDate;
				entry.ReffNo = Conversions.ToString(inv.InvoiceNo);
				entry.RefDate = inv.InvDate;
				entry.Type = (EntryType)inv.InvoiceType;
				if ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 2))
				{
					entry.Type = EntryType.ReturnPurchase;
				}
				else if (((inv.InvoiceType == InvoiceType.Sale) | (inv.InvoiceType == InvoiceType.POS) | (inv.InvoiceType == InvoiceType.HoldInv)) & (inv.ProcType == 2))
				{
					entry.Type = EntryType.ReturnSale;
				}
				else if ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 2))
				{
					entry.Type = (EntryType)30;
				}
				else if ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 1))
				{
					entry.Type = (EntryType)31;
				}
				else if ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 2))
				{
					entry.Type = (EntryType)32;
				}
				else if ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 1))
				{
					entry.Type = (EntryType)33;
				}
				else if ((inv.InvoiceType == InvoiceType.HoldInv) & (inv.ProcType == 1))
				{
					entry.Type = EntryType.Sale;
				}
				entry.State = 1;
				entry.Note = inv.InvNote;
				entry.Branch = inv.Branch;
				entry.EmpID = inv.User;
				entry.DistBranch = Sync.DistBranch;
				entry.BranchType = Sync.BranchType;
				double credit;
				credit = 0.0;
				double debt;
				debt = 0.0;
				if (num > 0.0)
				{
					Account account;
					if ((inv.PayType == -1) | (inv.PayType == 7) | (inv.PayType == 5) | (inv.PaymentStatus == 0) | (inv.PaymentStatus == 2))
					{
						account = new Account();
						if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.HoldInv) & (inv.ProcType == 1)))
						{
							credit = 0.0;
							debt = num;
							if ((inv.PaymentStatus == 0) | (inv.PaymentStatus == 2))
							{
								debt = num5;
							}
						}
						else if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.HoldInv) & (inv.ProcType == 2)))
						{
							credit = num;
							debt = 0.0;
							if ((inv.PaymentStatus == 0) | (inv.PaymentStatus == 2))
							{
								credit = num5;
							}
						}
						account.EntryGlobalID = entry.EntryGlobalID;
						account.EntryNo = entry.EntryNo;
						account.Name = customer.Name;
						account.Code = customer.AccCode;
						if (Operators.CompareString(customer.AccCode, "", TextCompare: false) == 0)
						{
							account.Code = "12310001";
							account.Name = "قيمة غير مسددة  ";
						}
						if (inv.PayType == 5)
						{
							account.Code = "3110002";
							account.Name = "ضيافة ";
						}
						account.Debt = debt;
						account.Credit = credit;
						account.Note = inv.InvNote + "  :" + customer.Name;
						account.CCcode = Conversions.ToString(-1);
						account.ClientCode = entry.ClientCode;
						list.Add(account);
					}
					if (invoiceObj.DetailedItemEntry)
					{
						foreach (InvoiceItem invoiceItem in inv.InvoiceItems)
						{
							account = new Account();
							if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.HoldInv) & (inv.ProcType == 1)))
							{
								if (inv.PriceIncVAT)
								{
									double num7;
									num7 = invoiceItem.ItemPrice * invoiceItem.ItemQuantity * inv.TotDiscount / inv.SumPrice;
									double num8;
									num8 = num7 - num7 / (1.0 + invoiceObj.VAT / 100.0);
									credit = invoiceItem.ItemNetPrice - invoiceItem.ItemVat + num8;
									debt = 0.0;
								}
								else
								{
									credit = invoiceItem.ItemNetPrice - invoiceItem.ItemVat;
									debt = 0.0;
								}
							}
							else if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.HoldInv) & (inv.ProcType == 2)))
							{
								if (inv.PriceIncVAT)
								{
									double num9;
									num9 = invoiceItem.ItemPrice * invoiceItem.ItemQuantity * inv.TotDiscount / inv.SumPrice;
									double num10;
									num10 = num9 - num9 / (1.0 + invoiceObj.VAT / 100.0);
									credit = 0.0;
									debt = invoiceItem.ItemNetPrice - invoiceItem.ItemVat + num10;
								}
								else
								{
									credit = 0.0;
									debt = invoiceItem.ItemNetPrice - invoiceItem.ItemVat;
								}
							}
							account.EntryGlobalID = entry.EntryGlobalID;
							account.EntryNo = entry.EntryNo;
							account.Name = Conversions.ToString((int)inv.InvoiceType);
							account.Code = inv.InvAccCode;
							account.Debt = debt;
							account.Credit = credit;
							account.Note = inv.InvNote + "  : " + customer.Name + " - " + invoiceItem.ItemName;
							account.CCcode = Conversions.ToString((!string.IsNullOrEmpty(invoiceItem.ItemCostCenter)) ? invoiceItem.ItemCostCenter : ((object)(-1)));
							account.ClientCode = entry.ClientCode;
							list.Add(account);
						}
					}
					else
					{
						account = new Account();
						if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.HoldInv) & (inv.ProcType == 1)))
						{
							credit = num - num4 + num6;
							debt = 0.0;
						}
						else if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.HoldInv) & (inv.ProcType == 2)))
						{
							credit = 0.0;
							debt = num - num4 + num6;
						}
						account.EntryGlobalID = entry.EntryGlobalID;
						account.EntryNo = entry.EntryNo;
						account.Name = Conversions.ToString((int)inv.InvoiceType);
						account.Code = inv.InvAccCode;
						account.Debt = debt;
						account.Credit = credit;
						account.Note = inv.InvNote + "  : " + customer.Name;
						account.CCcode = inv.InvCCcode;
						account.ClientCode = entry.ClientCode;
						list.Add(account);
					}
					if (inv.ExtraVAT > 0.0)
					{
						account = new Account();
						if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.HoldInv) & (inv.ProcType == 1)))
						{
							credit = inv.ExtraVAT;
							debt = 0.0;
						}
						else if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.HoldInv) & (inv.ProcType == 2)))
						{
							credit = 0.0;
							debt = inv.ExtraVAT;
						}
						account.EntryGlobalID = entry.EntryGlobalID;
						account.EntryNo = entry.EntryNo;
						account.Name = "الضريبة الإنتقائية";
						account.Code = Conversions.ToString(2222002);
						account.Debt = debt;
						account.Credit = credit;
						account.Note = inv.InvNote + "  :" + customer.Name;
						account.CCcode = Conversions.ToString(-1);
						account.ClientCode = entry.ClientCode;
						list.Add(account);
					}
					account = new Account();
					if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.HoldInv) & (inv.ProcType == 1)))
					{
						credit = num4 - inv.ExtraVAT;
						debt = 0.0;
					}
					else if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.HoldInv) & (inv.ProcType == 2)))
					{
						credit = 0.0;
						debt = num4 - inv.ExtraVAT;
					}
					account.EntryGlobalID = entry.EntryGlobalID;
					account.EntryNo = entry.EntryNo;
					account.Name = "الضريبة المضافة";
					account.Code = Conversions.ToString(2222001);
					account.Debt = debt;
					account.Credit = credit;
					account.Note = inv.InvNote + "  :" + customer.Name;
					account.CCcode = Conversions.ToString(-1);
					account.ClientCode = entry.ClientCode;
					list.Add(account);
					if (num6 > 0.0)
					{
						account = new Account();
						if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.HoldInv) & (inv.ProcType == 1)))
						{
							credit = 0.0;
							debt = num6;
						}
						else if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.HoldInv) & (inv.ProcType == 2)))
						{
							credit = num6;
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
						account.CCcode = Conversions.ToString(-1);
						account.ClientCode = entry.ClientCode;
						list.Add(account);
					}
					if (num2 > 0.0)
					{
						account = new Account();
						if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.HoldInv) & (inv.ProcType == 1)))
						{
							credit = 0.0;
							debt = num2;
						}
						else if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.HoldInv) & (inv.ProcType == 2)))
						{
							credit = num2;
							debt = 0.0;
						}
						account.EntryGlobalID = entry.EntryGlobalID;
						account.EntryNo = entry.EntryNo;
						account.Name = treasury.Name;
						account.Code = treasury.AccCode;
						account.Debt = debt;
						account.Credit = credit;
						account.Note = inv.InvNote;
						account.CCcode = Conversions.ToString(-1);
						account.ClientCode = entry.ClientCode;
						list.Add(account);
					}
					if (num3 > 0.0)
					{
						account = new Account();
						if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.HoldInv) & (inv.ProcType == 1)))
						{
							credit = 0.0;
							debt = num3;
						}
						else if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.HoldInv) & (inv.ProcType == 2)))
						{
							credit = num3;
							debt = 0.0;
						}
						account.EntryGlobalID = entry.EntryGlobalID;
						account.EntryNo = entry.EntryNo;
						account.Name = bank.Name;
						account.Code = bank.AccCode;
						account.Debt = debt;
						account.Credit = credit;
						account.Note = inv.InvNote;
						account.CCcode = Conversions.ToString(-1);
						account.ClientCode = entry.ClientCode;
						list.Add(account);
					}
				}
				entry.Accounts = list;
				result = entry;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2;
				ex2 = ex;
				string text;
				text = "Error in Binding data";
				if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
				{
					text = "Error in Binding data";
				}
				MessageBox.Show((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) != 0) ? (text + Environment.NewLine + "Error details: " + ex2.Message) : (text + Environment.NewLine + "تفاصيل الخطأ: " + ex2.Message), "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				result = null;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public bool SaveInvoice(Invoice inv, Entry Entr, bool IsNew)
		{
			bool result;
			if (MainClass.EmpNo < 1)
			{
				Interaction.MsgBox("المستخدم ليس لديه الصلاحية لحفظ الفاتورة ");
				result = false;
			}
			else if (((inv.InvoiceType == InvoiceType.Purchase) | (inv.InvoiceType == InvoiceType.Sale) | (inv.InvoiceType == InvoiceType.HoldInv) | (inv.InvoiceType == InvoiceType.POS) | (inv.InvoiceType == InvoiceType.BeginingInventory) | (inv.InvoiceType == InvoiceType.InventoryIn) | (inv.InvoiceType == InvoiceType.InventoryOut)) && ((inv.Store == -1) | (inv.Store == 0)))
			{
				Interaction.MsgBox("المستخدم غير مرتبط بمستودع ");
				result = false;
			}
			else if (((inv.InvoiceType == InvoiceType.Purchase) | (inv.InvoiceType == InvoiceType.Sale) | (inv.InvoiceType == InvoiceType.POS) | (inv.InvoiceType == InvoiceType.HoldInv)) && inv.PayType == 1 && ((inv.Treasury == -1) | (inv.Treasury == 0)))
			{
				Interaction.MsgBox("المستخدم غير مرتبط بصندوق ");
				result = false;
			}
			else if ((inv.Net < 0.0) & ((inv.InvoiceType == InvoiceType.Sale) | (inv.InvoiceType == InvoiceType.POS) | (inv.InvoiceType == InvoiceType.HoldInv)))
			{
				MessageBox.Show("يجب أن يكون الصافي اكبر من الصفر", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				result = false;
			}
			else
			{
				if (!((inv.InvoiceType == InvoiceType.Purchase) | (inv.InvoiceType == InvoiceType.Sale) | (inv.InvoiceType == InvoiceType.HoldInv)) || inv.PayType != -1)
				{
					goto IL_01fd;
				}
				if ((inv.Customer == 1) | (inv.Customer == -1))
				{
					MessageBox.Show("يجب اختيار العميل", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					result = false;
				}
				else
				{
					if (!((inv.Customer == 2) | (inv.Customer == -1)))
					{
						goto IL_01fd;
					}
					MessageBox.Show("يجب اختيار المورد", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					result = false;
				}
			}
			goto IL_29e0;
		IL_01fd:
			if (((inv.InvoiceType == InvoiceType.Purchase) | (inv.InvoiceType == InvoiceType.Sale) | (inv.InvoiceType == InvoiceType.POS) | (inv.InvoiceType == InvoiceType.HoldInv)) && inv.PayType == -1 && !InvoiceOper.isCreditCust(inv.Customer))
			{
				if ((inv.InvoiceType == InvoiceType.Sale) | (inv.InvoiceType == InvoiceType.POS))
				{
					MessageBox.Show(" يجب اختيار عميل آجل  ", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				}
				else if (inv.InvoiceType == InvoiceType.Purchase)
				{
					MessageBox.Show(" يجب اختيار مورد آجل  ", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				}
				result = false;
			}
			else
			{
				if (MainClass.IsTrial)
				{
					SqlConnection sqlConnection;
					sqlConnection = MainClass.ConnObj();
					if (sqlConnection.State != ConnectionState.Open)
					{
						sqlConnection.Open();
					}
					new SqlCommand();
					new SqlCommand();
					SqlCommand sqlCommand;
					sqlCommand = new SqlCommand("select count(id) from Entry ", sqlConnection);
					SqlCommand sqlCommand2;
					sqlCommand2 = new SqlCommand("select count(Proc_id) from Inv ", sqlConnection);
					if ((Conversion.Val(Operators.ConcatenateObject("", sqlCommand.ExecuteScalar())) >= 50.0) | (Conversion.Val(Operators.ConcatenateObject("", sqlCommand2.ExecuteScalar())) >= 50.0))
					{
						string text;
						text = "نأسف لقد وصلت لأقصى حد ادخال للنسخة التجريبية، يمكنك شراء البرنامج وتفعيله من خلال بيانات الدعم الفني بالبرنامج";
						if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
						{
							text = "Sorry,You reach the maximum of entries, you can purchase the app and activate it from support data in the app";
						}
						MessageBox.Show(text, "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
						MainClass.IsTrial = true;
						result = false;
						goto IL_29e0;
					}
					if (sqlConnection.State != ConnectionState.Open)
					{
						sqlConnection.Close();
					}
				}
				int[] source;
				source = new int[4] { 2, 3, 21, 23 };
				int[] source2;
				source2 = new int[2] { 1, 2 };
				if (source.Contains((int)inv.InvoiceType) && source2.Contains(inv.ProcType) && MainSetting.ZatcaIntegerationActive && (InvoiceOper.IsTaxCustomer(inv.Customer) || !MainSetting.ZatcaSyncManual))
				{
					ZatcaService zatcaService;
					zatcaService = new ZatcaService();
					bool isDebit;
					isDebit = false;
					if ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 1))
					{
						isDebit = true;
					}
					InvoiceReportingResponse invoiceReportingResponse;
					invoiceReportingResponse = zatcaService.IntegrateInvoice(ref inv, MainSetting.IsProductionZatca, MainSetting.IsSimulationZatca, isDebit);
					SqlConnection sqlConnection2;
					sqlConnection2 = MainClass.ConnObj();
					if (inv.ZatcaSent)
					{
						using (SqlCommand sqlCommand3 = new SqlCommand("UPDATE InvContratct SET ZatcaSent = 1, InvoiceHash = @InvoiceHash WHERE InvGlobalID = @InvGlobalID", sqlConnection2))
						{
							sqlConnection2.Open();
							sqlCommand3.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = inv.InvGlobalID;
							sqlCommand3.Parameters.Add("@InvoiceHash", SqlDbType.NVarChar).Value = inv.InvoiceHash;
							sqlCommand3.ExecuteNonQuery();
						}
						sqlConnection2.Close();
						ZatcaResponse zatcaResponse;
						zatcaResponse = new ZatcaResponse();
						zatcaResponse.InvGlobalID = inv.InvGlobalID;
						zatcaResponse.Status = ((!string.IsNullOrEmpty(invoiceReportingResponse.ReportingStatus)) ? invoiceReportingResponse.ReportingStatus : invoiceReportingResponse.ClearanceStatus);
						if (invoiceReportingResponse.validationResults != null)
						{
							foreach (ErrorModel errorMessage in invoiceReportingResponse.validationResults.ErrorMessages)
							{
								ZatcaResponse zatcaResponse2;
								(zatcaResponse2 = zatcaResponse).Message = zatcaResponse2.Message + "Status:" + errorMessage.Status.ToString() + " \r\n Message: " + errorMessage.Message;
							}
							foreach (WarningModel warningMessage in invoiceReportingResponse.validationResults.WarningMessages)
							{
								ZatcaResponse zatcaResponse2;
								(zatcaResponse2 = zatcaResponse).Message = zatcaResponse2.Message + "Status:" + warningMessage.Status.ToString() + " \r\n Message: " + warningMessage.Message;
							}
						}
						if (zatcaResponse.Message == null)
						{
							zatcaResponse.Message = "";
						}
						InvoiceOperContract.InsertZatcaResponse(zatcaResponse);
					}
					else
					{
						ZatcaResponse zatcaResponse3;
						zatcaResponse3 = new ZatcaResponse();
						zatcaResponse3.InvGlobalID = inv.InvGlobalID;
						zatcaResponse3.Status = ((!string.IsNullOrEmpty(invoiceReportingResponse.ReportingStatus)) ? invoiceReportingResponse.ReportingStatus : invoiceReportingResponse.ClearanceStatus);
						if (invoiceReportingResponse.validationResults != null)
						{
							foreach (ErrorModel errorMessage2 in invoiceReportingResponse.validationResults.ErrorMessages)
							{
								ZatcaResponse zatcaResponse2;
								(zatcaResponse2 = zatcaResponse3).Message = zatcaResponse2.Message + "Status:" + errorMessage2.Status.ToString() + " \r\n Message: " + errorMessage2.Message;
							}
							foreach (WarningModel warningMessage2 in invoiceReportingResponse.validationResults.WarningMessages)
							{
								ZatcaResponse zatcaResponse2;
								(zatcaResponse2 = zatcaResponse3).Message = zatcaResponse2.Message + "Status:" + warningMessage2.Status.ToString() + " \r\n Message: " + warningMessage2.Message;
							}
						}
						if (zatcaResponse3.Message == null)
						{
							zatcaResponse3.Message = "";
						}
						InvoiceOperContract.InsertZatcaResponse(zatcaResponse3);
						invoiceReportingResponse.ErrorMessage = zatcaResponse3.Message;
					}
					if (!invoiceReportingResponse.success)
					{
						Interaction.MsgBox(invoiceReportingResponse.ErrorMessage);
						result = false;
						goto IL_29e0;
					}
				}
				if ((Sync.ActiveSync & (Sync.BranchType == 4)) && inv.Customer == 1)
				{
					string text2;
					text2 = "يرجى أختيار عميل اخر";
					if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
					{
						text2 = "Please select another customer";
					}
					MessageBox.Show(text2, "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					result = false;
				}
				else
				{
					if (!(MainClass.CheckForInternetConnection() & (User.CurrentCloudUser != null)) || !(Sync.ActiveSync & (Sync.BranchType == 4) & (inv.InvoiceType == InvoiceType.POS)))
					{
						goto IL_099c;
					}
					if (inv.PriceIncVAT & (User.CurrentCloudUser.TypeTax == 1))
					{
						string text3;
						text3 = " يرجى التوافق مع المدقق السحابي لنظام العمل شامل الضريبه او غير شامل الضريبة ";
						if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
						{
							text3 = "Please refer to cloud check for working system, tax-included price or tax-excluded price";
						}
						MessageBox.Show(text3, "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
						result = false;
					}
					else
					{
						if (!(!inv.PriceIncVAT & (User.CurrentCloudUser.TypeTax == 2)))
						{
							goto IL_099c;
						}
						string text4;
						text4 = " يرجى التوافق مع المدقق السحابي لنظام العمل شامل الضريبه او غير شامل الضريبة ";
						if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
						{
							text4 = "Please refer to cloud check for working system, tax-included price or tax-excluded price";
						}
						MessageBox.Show(text4, "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
						result = false;
					}
				}
			}
			goto IL_29e0;
		IL_29e0:
			return result;
		IL_099c:
			SqlConnection sqlConnection3;
			sqlConnection3 = MainClass.ConnObj();
			if (sqlConnection3.State != ConnectionState.Open)
			{
				sqlConnection3.Open();
			}
			SqlTransaction sqlTransaction;
			sqlTransaction = sqlConnection3.BeginTransaction();
			new SqlCommand();
			checked
			{
				try
				{
					SqlCommand sqlCommand4;
					if (IsNew)
					{
						sqlCommand4 = new SqlCommand(StoredQueries.InsertInvcontract, sqlConnection3, sqlTransaction);
					}
					else
					{
						new SqlCommand("delete from InvContratct_Sub where InvGlobalID= N'" + inv.InvGlobalID + "'", sqlConnection3, sqlTransaction).ExecuteNonQuery();
						new SqlCommand("delete from InvoiceItemDetail where InvGlobalID=N'" + inv.InvGlobalID + "'", sqlConnection3, sqlTransaction).ExecuteNonQuery();
						new SqlCommand("delete from InvoiceCost where InvGlobalID=N'" + inv.InvGlobalID + "'", sqlConnection3, sqlTransaction).ExecuteNonQuery();
						new SqlCommand("delete from Glasses where InvGlobalID=N'" + inv.InvGlobalID + "'", sqlConnection3, sqlTransaction).ExecuteNonQuery();
						sqlCommand4 = new SqlCommand(StoredQueries.UpdateInvcontract, sqlConnection3, sqlTransaction);
					}
					int num;
					num = -1;
					if (Entr != null)
					{
						num = Entr.EntryNo;
					}
					sqlCommand4.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = inv.InvGlobalID;
					sqlCommand4.Parameters.Add("@CloudID", SqlDbType.NVarChar).Value = inv.CloudID;
					sqlCommand4.Parameters.Add("@proc_type", SqlDbType.Int).Value = inv.ProcType;
					sqlCommand4.Parameters.Add("@id", SqlDbType.Int).Value = inv.InvoiceNo;
					sqlCommand4.Parameters.Add("@date", SqlDbType.DateTime).Value = inv.InvDate;
					sqlCommand4.Parameters.Add("@inv_type", SqlDbType.Int).Value = inv.InvoiceType;
					sqlCommand4.Parameters.Add("@safe", SqlDbType.Int).Value = inv.Store;
					sqlCommand4.Parameters.Add("@stock", SqlDbType.Int).Value = inv.Treasury;
					sqlCommand4.Parameters.Add("@cust_id", SqlDbType.Int).Value = inv.Customer;
					sqlCommand4.Parameters.Add("@CashCustomerName", SqlDbType.NVarChar).Value = inv.CashCustomerName;
					sqlCommand4.Parameters.Add("@CashCustomerMobile", SqlDbType.NVarChar).Value = inv.CashCustomerMobile;
					sqlCommand4.Parameters.Add("@sales_emp", SqlDbType.Int).Value = inv.User;
					sqlCommand4.Parameters.Add("@InvTotal", SqlDbType.Float).Value = inv.Total;
					sqlCommand4.Parameters.Add("@AdditionsTot", SqlDbType.Float).Value = inv.Delivery;
					sqlCommand4.Parameters.Add("@Insurance", SqlDbType.Float).Value = inv.Insurance;
					sqlCommand4.Parameters.Add("@tot_net", SqlDbType.Float).Value = inv.Net;
					sqlCommand4.Parameters.Add("@InvProfit", SqlDbType.Float).Value = 0;
					sqlCommand4.Parameters.Add("@paid", SqlDbType.Float).Value = inv.Paid;
					sqlCommand4.Parameters.Add("@minus", SqlDbType.Float).Value = inv.Discount;
					sqlCommand4.Parameters.Add("@tax", SqlDbType.Float).Value = inv.VAT;
					sqlCommand4.Parameters.Add("@EntryID", SqlDbType.Int).Value = num;
					sqlCommand4.Parameters.Add("@cash", SqlDbType.Float).Value = inv.Paycash;
					sqlCommand4.Parameters.Add("@visa", SqlDbType.Float).Value = inv.PayATM;
					sqlCommand4.Parameters.Add("@branch", SqlDbType.Int).Value = inv.Branch;
					sqlCommand4.Parameters.Add("@IS_Buy", SqlDbType.Bit).Value = 0;
					sqlCommand4.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = inv.IsDeleted;
					sqlCommand4.Parameters.Add("@notes", SqlDbType.NVarChar).Value = inv.InvNote;
					sqlCommand4.Parameters.Add("@Reff_No ", SqlDbType.NVarChar).Value = inv.ReffNo;
					sqlCommand4.Parameters.Add("@Reff_date ", SqlDbType.DateTime).Value = inv.RefDate;
					sqlCommand4.Parameters.Add("@salesman", SqlDbType.Int).Value = inv.Saleman;
					sqlCommand4.Parameters.Add("@pay_type", SqlDbType.Int).Value = inv.PayType;
					sqlCommand4.Parameters.Add("@bank", SqlDbType.Int).Value = inv.Bank;
					sqlCommand4.Parameters.Add("@Sync", SqlDbType.Bit).Value = inv.Sent;
					sqlCommand4.Parameters.Add("@ExtraVAT", SqlDbType.Float).Value = inv.ExtraVAT;
					sqlCommand4.Parameters.Add("@AdditionalCost", SqlDbType.Float).Value = inv.AdditionalCost;
					sqlCommand4.Parameters.Add("@InvoiceStatus", SqlDbType.Int).Value = inv.InvoiceStatus;
					sqlCommand4.Parameters.Add("@PriceIncVAT", SqlDbType.Bit).Value = Convert.ToInt16(inv.PriceIncVAT);
					sqlCommand4.Parameters.Add("@PaymentStatus", SqlDbType.Int).Value = inv.PaymentStatus;
					sqlCommand4.Parameters.Add("@InvCombinedId ", SqlDbType.NVarChar).Value = inv.InvCombinedId;
					sqlCommand4.Parameters.Add("@CurrencyCode ", SqlDbType.NVarChar).Value = inv.Currency.ToString();
					sqlCommand4.Parameters.Add("@ItemsDiscount", SqlDbType.Float).Value = inv.TotDiscount - inv.Discount;
					sqlCommand4.Parameters.Add("@InvCost", SqlDbType.Float).Value = inv.InvoiceCost;
					sqlCommand4.Parameters.Add("@FreeVATSales", SqlDbType.Float).Value = inv.FreeVATSales;
					sqlCommand4.Parameters.Add("@InvSum", SqlDbType.Float).Value = inv.SumPrice;
					sqlCommand4.Parameters.Add("@VATPercent", SqlDbType.Float).Value = inv.VATperc;
					sqlCommand4.Parameters.Add("@QRCode", SqlDbType.NVarChar).Value = inv.QRCode;
					sqlCommand4.Parameters.Add("@InvoiceHash", SqlDbType.NVarChar).Value = inv.InvoiceHash;
					sqlCommand4.Parameters.Add("@UUID", SqlDbType.NVarChar).Value = inv.UUID;
					sqlCommand4.Parameters.Add("@ZatcaSent", SqlDbType.Bit).Value = inv.ZatcaSent;
					sqlCommand4.Parameters.Add("@TotalWithholdingTax", SqlDbType.Float).Value = inv.TotalWithholdingTax;
					sqlCommand4.Parameters.Add("@TableNo", SqlDbType.NVarChar).Value = inv.TableNo;
					sqlCommand4.Parameters.Add("@Balance_previews", SqlDbType.Float).Value = inv.Balance_previews;
					sqlCommand4.Parameters.Add("@contract_total_value", SqlDbType.Float).Value = inv.contract_total_value;
					sqlCommand4.Parameters.Add("@original_work_amount", SqlDbType.Float).Value = inv.original_work_amount;
					sqlCommand4.Parameters.Add("@special_discount", SqlDbType.Float).Value = inv.special_discount;
					sqlCommand4.Parameters.Add("@amount_after_discount", SqlDbType.Float).Value = inv.amount_after_discount;
					sqlCommand4.Parameters.Add("@vat_amount", SqlDbType.Float).Value = inv.vat_amount;
					sqlCommand4.Parameters.Add("@total_with_vat", SqlDbType.Float).Value = inv.total_with_vat;
					sqlCommand4.Parameters.Add("@work_guarantee", SqlDbType.Float).Value = inv.work_guarantee;
					sqlCommand4.Parameters.Add("@net_due_this_payment", SqlDbType.Float).Value = inv.net_due_this_payment;
					sqlCommand4.Parameters.Add("@previously_paid_amount", SqlDbType.Float).Value = inv.previously_paid_amount;
					sqlCommand4.Parameters.Add("@remaining_contract_balance", SqlDbType.Float).Value = inv.remaining_contract_balance;
					sqlCommand4.ExecuteNonQuery();
					foreach (InvoiceItem invoiceItem in inv.InvoiceItems)
					{
						sqlCommand4 = new SqlCommand(StoredQueries.InsertInvSubcontract, sqlConnection3, sqlTransaction);
						sqlCommand4.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = inv.InvGlobalID;
						sqlCommand4.Parameters.Add("@proc_id", SqlDbType.Int).Value = inv.AutoIncrementID;
						sqlCommand4.Parameters.Add("@proc_type", SqlDbType.Int).Value = invoiceItem.InvertoryImpact;
						if (DateTime.Compare(invoiceItem.ItemExpireDate, DateTime.MinValue) <= 0)
						{
							invoiceItem.ItemExpireDate = DateTime.Now.AddYears(2);
						}
						sqlCommand4.Parameters.Add("@expire_date", SqlDbType.DateTime).Value = invoiceItem.ItemExpireDate;
						sqlCommand4.Parameters.Add("@Store", SqlDbType.Float).Value = invoiceItem.InvertoryId;
						sqlCommand4.Parameters.Add("@ItemId", SqlDbType.Int).Value = invoiceItem.ItemId;
						sqlCommand4.Parameters.Add("@unit", SqlDbType.Int).Value = invoiceItem.UnitID;
						sqlCommand4.Parameters.Add("@UnitEquality", SqlDbType.Float).Value = invoiceItem.UnitEquality;
						sqlCommand4.Parameters.Add("@val", SqlDbType.Float).Value = invoiceItem.ItemPrimaryQnty;
						sqlCommand4.Parameters.Add("@val1", SqlDbType.Float).Value = invoiceItem.ItemQuantity;
						sqlCommand4.Parameters.Add("@exchange_price", SqlDbType.Float).Value = invoiceItem.ItemPrice;
						sqlCommand4.Parameters.Add("@discount", SqlDbType.Float).Value = invoiceItem.ItemDiscount;
						sqlCommand4.Parameters.Add("@ItemAddedCost", SqlDbType.Float).Value = invoiceItem.ItemAddedCost;
						sqlCommand4.Parameters.Add("@taxperc", SqlDbType.Float).Value = invoiceItem.ItemVatPerc;
						sqlCommand4.Parameters.Add("@taxval", SqlDbType.Float).Value = invoiceItem.ItemVat;
						sqlCommand4.Parameters.Add("@Description", SqlDbType.NVarChar).Value = invoiceItem.Description;
						if (invoiceItem.ItemNotes == null)
						{
							sqlCommand4.Parameters.Add("@notes", SqlDbType.NVarChar).Value = "";
						}
						else
						{
							sqlCommand4.Parameters.Add("@notes", SqlDbType.NVarChar).Value = invoiceItem.ItemNotes;
						}
						sqlCommand4.Parameters.Add("@ProductId", SqlDbType.Int).Value = invoiceItem.ProductId;
						sqlCommand4.Parameters.Add("@AvrgCost", SqlDbType.Float).Value = invoiceItem.ItemCost;
						if (invoiceItem.InvertoryImpact == 1)
						{
							invoiceItem.ValiableInvertory += invoiceItem.ItemPrimaryQnty;
						}
						else if (invoiceItem.InvertoryImpact == 2)
						{
							invoiceItem.ValiableInvertory -= invoiceItem.ItemPrimaryQnty;
						}
						sqlCommand4.Parameters.Add("@CurrentQnty", SqlDbType.Float).Value = invoiceItem.ValiableInvertory;
						sqlCommand4.Parameters.Add("@WithholdingTax", SqlDbType.Float).Value = invoiceItem.WithholdingTax;
						sqlCommand4.Parameters.Add("@WithholdingTaxPerc", SqlDbType.Float).Value = invoiceItem.WithholdingTaxPerc;
						sqlCommand4.Parameters.Add("@ItemPriceWithoutVAT", SqlDbType.Float).Value = invoiceItem.ItemPriceWithoutVAT;
						sqlCommand4.Parameters.Add("@ItemCostCenter", SqlDbType.NVarChar).Value = ((!string.IsNullOrEmpty(invoiceItem.ItemCostCenter)) ? invoiceItem.ItemCostCenter : "");
						sqlCommand4.Parameters.Add("@ItemAdditionalTax", SqlDbType.Float).Value = invoiceItem.ItemAdditionalTax;
						sqlCommand4.Parameters.Add("@ItemAdditionalTaxPerc", SqlDbType.Float).Value = invoiceItem.ItemAdditionalTaxPerc;
						sqlCommand4.ExecuteNonQuery();
						foreach (InvoiceItemDetail invoiceItemDetail in invoiceItem.InvoiceItemDetails)
						{
							if (sqlConnection3.State != ConnectionState.Open)
							{
								sqlConnection3.Open();
							}
							sqlCommand4 = new SqlCommand("INSERT into InvoiceItemDetail(ItemDetailId,ItemIncrId,InvGlobalID,ItemId,InvertoryImpact,ItemSerialNo,BatchNo,ItemProductionDate,ItemExpireDate,ItemHeight,ItemWidth,ItemColor,ItemSize,ItemProperty,FillValue,FillRatio,ItemQuantity,ItemBarcode) values(@ItemDetailId,@ItemIncrId,@InvGlobalID,@ItemId,@InvertoryImpact,@ItemSerialNo,@BatchNo,@ItemProductionDate,@ItemExpireDate,@ItemHeight,@ItemWidth,@ItemColor,@ItemSize,@ItemProperty,@FillValue,@FillRatio,@ItemQuantity,@ItemBarcode)", sqlConnection3, sqlTransaction);
							sqlCommand4.Parameters.Add("@ItemDetailId", SqlDbType.Int).Value = 1;
							sqlCommand4.Parameters.Add("@ItemIncrId", SqlDbType.Int).Value = 1;
							sqlCommand4.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = inv.InvGlobalID;
							sqlCommand4.Parameters.Add("@ItemId", SqlDbType.Int).Value = invoiceItemDetail.ItemId;
							sqlCommand4.Parameters.Add("@InvertoryImpact", SqlDbType.Int).Value = invoiceItem.InvertoryImpact;
							sqlCommand4.Parameters.Add("@ItemSerialNo", SqlDbType.NVarChar).Value = invoiceItemDetail.ItemSerialNo;
							sqlCommand4.Parameters.Add("@BatchNo", SqlDbType.NVarChar).Value = invoiceItemDetail.BatchNo;
							sqlCommand4.Parameters.Add("@ItemProductionDate", SqlDbType.DateTime).Value = invoiceItemDetail.ItemProductionDate;
							sqlCommand4.Parameters.Add("@ItemExpireDate", SqlDbType.DateTime).Value = invoiceItemDetail.ItemExpireDate;
							sqlCommand4.Parameters.Add("@ItemHeight", SqlDbType.Float).Value = invoiceItemDetail.ItemHeight;
							sqlCommand4.Parameters.Add("@ItemWidth", SqlDbType.Float).Value = invoiceItemDetail.ItemWidth;
							sqlCommand4.Parameters.Add("@ItemColor", SqlDbType.Int).Value = invoiceItemDetail.ItemColor;
							sqlCommand4.Parameters.Add("@ItemSize", SqlDbType.Float).Value = invoiceItemDetail.ItemSize;
							sqlCommand4.Parameters.Add("@ItemProperty", SqlDbType.Int).Value = invoiceItemDetail.ItemProperty;
							sqlCommand4.Parameters.Add("@FillValue", SqlDbType.Float).Value = invoiceItemDetail.FillValue;
							sqlCommand4.Parameters.Add("@FillRatio", SqlDbType.Float).Value = invoiceItemDetail.FillRatio;
							sqlCommand4.Parameters.Add("@ItemQuantity", SqlDbType.Float).Value = invoiceItemDetail.ItemQuantity;
							sqlCommand4.Parameters.Add("@ItemBarcode", SqlDbType.NVarChar).Value = invoiceItemDetail.ItemBarcode;
							sqlCommand4.ExecuteNonQuery();
						}
						foreach (Glass glass in invoiceItem.Glasses)
						{
							if (sqlConnection3.State != ConnectionState.Open)
							{
								sqlConnection3.Open();
							}
							sqlCommand4 = new SqlCommand("Insert into Glasses(InvGlobalID,ItemId,orientation,SPH,CYL,AX,[ADD],IPD) values(@InvGlobalID,@ItemId,@orientation,@SPH,@CYL,@AX,@ADD,@IPD)", sqlConnection3, sqlTransaction);
							sqlCommand4.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = inv.InvGlobalID;
							sqlCommand4.Parameters.Add("@ItemId", SqlDbType.Int).Value = invoiceItem.ItemId;
							sqlCommand4.Parameters.Add("@orientation", SqlDbType.VarChar).Value = glass.orientation;
							sqlCommand4.Parameters.Add("@SPH", SqlDbType.VarChar).Value = glass.SPH;
							sqlCommand4.Parameters.Add("@CYL", SqlDbType.VarChar).Value = glass.CYL;
							sqlCommand4.Parameters.Add("@AX", SqlDbType.VarChar).Value = glass.AX;
							sqlCommand4.Parameters.Add("@ADD", SqlDbType.VarChar).Value = glass.ADD;
							sqlCommand4.Parameters.Add("@IPD", SqlDbType.VarChar).Value = glass.IPD;
							sqlCommand4.ExecuteNonQuery();
						}
					}
					if (inv.InvoiceCosts.Count > 0)
					{
						foreach (InvoiceCost invoiceCost in inv.InvoiceCosts)
						{
							if (sqlConnection3.State != ConnectionState.Open)
							{
								sqlConnection3.Open();
							}
							sqlCommand4 = new SqlCommand("insert into InvoiceCost(InvoiceCostId,InvoiceCostNo,InvGlobalID,CostId,CostName,Cost,CostDate,Note,costCenter)  values(@InvoiceCostId,@InvoiceCostNo,@InvGlobalID,@CostId,@CostName,@Cost,@CostDate,@Note,@costCenter)", sqlConnection3, sqlTransaction);
							sqlCommand4.Parameters.Add("@InvoiceCostId", SqlDbType.Int).Value = invoiceCost.InvoiceCostId;
							sqlCommand4.Parameters.Add("@InvoiceCostNo", SqlDbType.Int).Value = invoiceCost.InvoiceCostNo;
							sqlCommand4.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = inv.InvGlobalID;
							sqlCommand4.Parameters.Add("@CostId", SqlDbType.Int).Value = invoiceCost.CostId;
							sqlCommand4.Parameters.Add("@CostName", SqlDbType.NVarChar).Value = invoiceCost.CostName;
							sqlCommand4.Parameters.Add("@Cost", SqlDbType.Float).Value = invoiceCost.Cost;
							sqlCommand4.Parameters.Add("@CostDate", SqlDbType.DateTime).Value = DateTime.Now;
							sqlCommand4.Parameters.Add("@Note", SqlDbType.NVarChar).Value = invoiceCost.Note;
							sqlCommand4.Parameters.Add("@costCenter", SqlDbType.Int).Value = invoiceCost.costCenter;
							sqlCommand4.ExecuteNonQuery();
						}
					}
					foreach (InvoicePayment invoicePayment in inv.InvoicePayments)
					{
						if (sqlConnection3.State != ConnectionState.Open)
						{
							sqlConnection3.Open();
						}
						invoicePayment.PaymentId = (int)Math.Round(Conversion.Val(Operators.ConcatenateObject("", new SqlCommand("select IsNull(max(PaymentId),0) from InvoicePayments ", sqlConnection3, sqlTransaction).ExecuteScalar())) + 1.0);
						if (inv.PaymentStatus == 1)
						{
							invoicePayment.PaymentDate = inv.InvDate;
							invoicePayment.DueDate = inv.InvDate;
						}
						if (invoicePayment.Remainder < 0.0)
						{
							if (invoicePayment.PayType == -1)
							{
								invoicePayment.CashPayment = 0.0;
								invoicePayment.MadaPayment = 0.0;
								invoicePayment.VisaPayment = 0.0;
								invoicePayment.Paid = 0.0;
								invoicePayment.Remainder = 0.0;
							}
							else if (invoicePayment.PayType == 1)
							{
								invoicePayment.CashPayment = invoicePayment.Paid + invoicePayment.Remainder;
								invoicePayment.MadaPayment = 0.0;
								invoicePayment.VisaPayment = 0.0;
								invoicePayment.Paid += invoicePayment.Remainder;
								invoicePayment.Remainder = 0.0;
							}
							else if (invoicePayment.PayType == 2)
							{
								invoicePayment.CashPayment = 0.0;
								invoicePayment.MadaPayment = invoicePayment.Paid + invoicePayment.Remainder;
								invoicePayment.VisaPayment = 0.0;
								invoicePayment.Paid += invoicePayment.Remainder;
								invoicePayment.Remainder = 0.0;
							}
							else if (invoicePayment.PayType == 3)
							{
								invoicePayment.CashPayment = invoicePayment.Paid + invoicePayment.Remainder - invoicePayment.MadaPayment;
								invoicePayment.Paid = invoicePayment.MadaPayment + invoicePayment.CashPayment;
								invoicePayment.Remainder = 0.0;
							}
						}
						else
						{
							invoicePayment.Remainder = inv.Net - inv.Paid;
						}
						sqlCommand4 = new SqlCommand("insert into InvoicePayments(PaymentId,InvGlobalID,PayType,Paid,Remainder,BankId,DueDate,PaymentDate,Treasury,CashPayment,MadaPayment,VisaPayment,EmpId,PaymentStatus)  values(@PaymentId,@InvGlobalID,@PayType,@Paid,@Remainder,@BankId,@DueDate,@PaymentDate,@Treasury,@CashPayment,@MadaPayment,@VisaPayment,@EmpId,@PaymentStatus)", sqlConnection3, sqlTransaction);
						sqlCommand4.Parameters.Add("@PaymentId", SqlDbType.Int).Value = invoicePayment.PaymentId;
						sqlCommand4.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = inv.InvGlobalID;
						sqlCommand4.Parameters.Add("@PayType", SqlDbType.Int).Value = invoicePayment.PayType;
						sqlCommand4.Parameters.Add("@Paid", SqlDbType.Float).Value = invoicePayment.Paid;
						sqlCommand4.Parameters.Add("@Remainder", SqlDbType.Float).Value = invoicePayment.Remainder;
						sqlCommand4.Parameters.Add("@BankId", SqlDbType.Int).Value = invoicePayment.BankId;
						sqlCommand4.Parameters.Add("@Treasury", SqlDbType.Int).Value = invoicePayment.Treasury;
						sqlCommand4.Parameters.Add("@DueDate", SqlDbType.DateTime).Value = invoicePayment.DueDate;
						sqlCommand4.Parameters.Add("@PaymentDate", SqlDbType.DateTime).Value = invoicePayment.PaymentDate;
						sqlCommand4.Parameters.Add("@CashPayment", SqlDbType.Float).Value = invoicePayment.CashPayment;
						sqlCommand4.Parameters.Add("@MadaPayment", SqlDbType.Float).Value = invoicePayment.MadaPayment;
						sqlCommand4.Parameters.Add("@VisaPayment", SqlDbType.Float).Value = invoicePayment.VisaPayment;
						sqlCommand4.Parameters.Add("@EmpId", SqlDbType.Int).Value = MainClass.EmpNo;
						sqlCommand4.Parameters.Add("@PaymentStatus", SqlDbType.Int).Value = invoicePayment.PaymentStatus;
						sqlCommand4.ExecuteNonQuery();
					}
					if (inv.InvoiceItems.Count == 0)
					{
						foreach (Item item in inv.Items)
						{
							sqlCommand4 = new SqlCommand(StoredQueries.InsertInvSub, sqlConnection3, sqlTransaction);
							sqlCommand4.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = inv.InvGlobalID;
							sqlCommand4.Parameters.Add("@proc_id", SqlDbType.Int).Value = inv.AutoIncrementID;
							if (inv.ProcType != item.ProcType)
							{
								sqlCommand4.Parameters.Add("@proc_type", SqlDbType.Int).Value = item.ProcType;
							}
							else
							{
								sqlCommand4.Parameters.Add("@proc_type", SqlDbType.Int).Value = inv.ProcType;
							}
							if (DateTime.Compare(item.ExpireDate, DateTime.MinValue) <= 0)
							{
								item.ExpireDate = DateTime.Now.AddYears(2);
							}
							sqlCommand4.Parameters.Add("@expire_date", SqlDbType.DateTime).Value = item.ExpireDate;
							sqlCommand4.Parameters.Add("@Store", SqlDbType.Float).Value = item.Store;
							sqlCommand4.Parameters.Add("@ItemId", SqlDbType.Int).Value = item.ItemNo;
							sqlCommand4.Parameters.Add("@unit", SqlDbType.Int).Value = item.Unit;
							sqlCommand4.Parameters.Add("@UnitEquality", SqlDbType.Float).Value = item.UnitEquality;
							sqlCommand4.Parameters.Add("@val", SqlDbType.Float).Value = item.PrimaryQnty;
							sqlCommand4.Parameters.Add("@val1", SqlDbType.Float).Value = item.Quantity;
							sqlCommand4.Parameters.Add("@exchange_price", SqlDbType.Float).Value = item.Price;
							sqlCommand4.Parameters.Add("@discount", SqlDbType.Float).Value = item.ItemDiscount;
							sqlCommand4.Parameters.Add("@taxperc", SqlDbType.Float).Value = item.VatPerc;
							sqlCommand4.Parameters.Add("@taxval", SqlDbType.Float).Value = item.Vat;
							sqlCommand4.Parameters.Add("@Description", SqlDbType.NVarChar).Value = item.Description;
							sqlCommand4.Parameters.Add("@ProductId", SqlDbType.Int).Value = item.ProductId;
							sqlCommand4.Parameters.Add("@AvrgCost", SqlDbType.Float).Value = item.AvegCost;
							sqlCommand4.Parameters.Add("@ItemAddedCost", SqlDbType.Float).Value = item.ItemAddedCost;
							sqlCommand4.Parameters.Add("@CurrentQnty", SqlDbType.Float).Value = item.ValiableStock;
							sqlCommand4.Parameters.Add("@WithholdingTax", SqlDbType.Float).Value = item.WithholdingTax;
							sqlCommand4.Parameters.Add("@WithholdingTaxPerc", SqlDbType.Float).Value = item.WithholdingTaxPerc;
							sqlCommand4.Parameters.Add("@ItemAdditionalTax", SqlDbType.Float).Value = item.ItemAdditionalTax;
							sqlCommand4.Parameters.Add("@ItemAdditionalTaxPerc", SqlDbType.Float).Value = item.ItemAdditionalTaxPerc;
							if (item.Note == null)
							{
								sqlCommand4.Parameters.Add("@notes", SqlDbType.NVarChar).Value = "";
							}
							else
							{
								sqlCommand4.Parameters.Add("@notes", SqlDbType.NVarChar).Value = item.Note;
							}
							sqlCommand4.Parameters.Add("@ItemPriceWithoutVAT", SqlDbType.Float).Value = item.Price;
							sqlCommand4.Parameters.Add("@ItemCostCenter", SqlDbType.NVarChar).Value = ((!string.IsNullOrEmpty(item.ItemCostCenter)) ? item.ItemCostCenter : "");
							sqlCommand4.ExecuteNonQuery();
						}
					}
					if (!string.IsNullOrEmpty(inv.EncodedInvoice))
					{
						new SqlCommand("Delete from ZatcaEncodedInvoice where InvGlobalID=N'" + inv.InvGlobalID + "'", sqlConnection3, sqlTransaction).ExecuteNonQuery();
						int num2;
						num2 = Convert.ToInt32(RuntimeHelpers.GetObjectValue(new SqlCommand("select ISNULL(MAX(Id), 0) from ZatcaEncodedInvoice", sqlConnection3, sqlTransaction).ExecuteScalar())) + 1;
						sqlCommand4 = new SqlCommand("Insert into ZatcaEncodedInvoice (Id,InvGlobalID,EncodedInvoice,CreatedDate,UUID)values (@Id,@InvGlobalID,@EncodedInvoice,@CreatedDate,@UUID)", sqlConnection3, sqlTransaction);
						sqlCommand4.Parameters.Add("@Id", SqlDbType.Int).Value = num2;
						sqlCommand4.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = inv.InvGlobalID;
						sqlCommand4.Parameters.Add("@EncodedInvoice", SqlDbType.NVarChar).Value = inv.EncodedInvoice;
						sqlCommand4.Parameters.Add("@CreatedDate", SqlDbType.DateTime).Value = DateTime.Now;
						sqlCommand4.Parameters.Add("@UUID", SqlDbType.NVarChar).Value = inv.UUID;
						sqlCommand4.ExecuteNonQuery();
					}
					if (InvoiceOperContract.filePathDocument != null)
					{
						string[] array;
						array = InvoiceOperContract.filePathDocument;
						for (int i = 0; i < array.Length; i++)
						{
							_ = array[i];
							InvoiceOperContract.insertDocument(inv.InvGlobalID, "Inv");
						}
					}
					if (Entr == null)
					{
						sqlTransaction.Commit();
						goto IL_291b;
					}
					if (new EntryOper().SaveEnty(Entr))
					{
						sqlTransaction.Commit();
						goto IL_291b;
					}
					sqlTransaction.Rollback();
					string text5;
					text5 = "خطأ أثناء الحفظ";
					if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
					{
						text5 = "error in saving";
					}
					MessageBox.Show(text5, "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					result = false;
					goto end_IL_09ce;
				IL_291b:
					result = true;
				end_IL_09ce:;
				}
				catch (Exception ex)
				{
					ProjectData.SetProjectError(ex);
					Exception ex2;
					ex2 = ex;
					sqlTransaction.Rollback();
					string text6;
					text6 = "خطأ أثناء الحفظ";
					if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
					{
						text6 = "error in saving";
					}
					MessageBox.Show((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) != 0) ? (text6 + Environment.NewLine + "Error details: " + ex2.Message) : (text6 + Environment.NewLine + "تفاصيل الخطأ: " + ex2.Message), "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					result = false;
					ProjectData.ClearProjectError();
				}
				finally
				{
					if (sqlConnection3.State != ConnectionState.Closed)
					{
						sqlConnection3.Close();
					}
				}
				goto IL_29e0;
			}
		}

		public static void InsertZatcaResponse(ZatcaResponse ZatcaResponse)
		{
			try
			{
				SqlConnection sqlConnection;
				sqlConnection = MainClass.ConnObj();
				using (sqlConnection)
				{
					sqlConnection.Open();
					using (SqlCommand sqlCommand = new SqlCommand("IF EXISTS (SELECT 1 FROM ZatcaResponse WHERE InvGlobalID = @InvGlobalID) DELETE FROM ZatcaResponse WHERE InvGlobalID = @InvGlobalID", sqlConnection))
					{
						sqlCommand.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = ZatcaResponse.InvGlobalID;
						sqlCommand.ExecuteNonQuery();
					}
					using SqlCommand sqlCommand2 = new SqlCommand("INSERT INTO ZatcaResponse (InvGlobalID, Message, Status) VALUES (@InvGlobalID, @Message, @Status)", sqlConnection);
					sqlCommand2.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = ZatcaResponse.InvGlobalID;
					sqlCommand2.Parameters.Add("@Message", SqlDbType.NVarChar).Value = ZatcaResponse.Message;
					sqlCommand2.Parameters.Add("@Status", SqlDbType.NVarChar).Value = ZatcaResponse.Status;
					sqlCommand2.ExecuteNonQuery();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				Console.Write("");
				ProjectData.ClearProjectError();
			}
		}

		public static void insertDocument(string InvGlobalID, string Filatype)
		{
			string text;
			text = Application.StartupPath + "\\Documents";
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			SqlConnection sqlConnection;
			sqlConnection = new SqlConnection(MainClass.connstr);
			string text2;
			text2 = text;
			if (!Directory.Exists(text2))
			{
				Directory.CreateDirectory(text2);
			}
			string[] array;
			array = InvoiceOperContract.filePathDocument;
			foreach (string text3 in array)
			{
				if (sqlConnection.State == ConnectionState.Open)
				{
					sqlConnection.Close();
				}
				sqlConnection.Open();
				int num;
				num = Convert.ToInt32(RuntimeHelpers.GetObjectValue(new SqlCommand("SELECT ISNULL(MAX(id), 0) + 1 FROM Documents", sqlConnection).ExecuteScalar()));
				string text4;
				text4 = Path.Combine(text2, Filatype + "(" + InvGlobalID + ")_" + Conversions.ToString(num) + Path.GetExtension(text3));
				string fileName;
				fileName = Path.GetFileName(text3);
				File.Copy(text3, text4, overwrite: true);
				using SqlCommand sqlCommand = new SqlCommand("INSERT INTO Documents (FileName, FileUrl,GlobalID,type) VALUES (@FileName, @FileUrl,@GlobalID,2)", sqlConnection);
				sqlCommand.Parameters.AddWithValue("@FileName", fileName);
				sqlCommand.Parameters.AddWithValue("@FileUrl", text4);
				sqlCommand.Parameters.AddWithValue("@GlobalID", InvGlobalID);
				sqlCommand.ExecuteNonQuery();
			}
			sqlConnection.Close();
		}

		public async Task<bool> SyncInvoice(Invoice inv, Entry Eny, bool IsNew)
		{
			List<Invoice> list;
			list = new List<Invoice>();
			InvoiceCRUD invoiceCRUD;
			invoiceCRUD = new InvoiceCRUD(Sync.APIUrl);
			EntryOper entryOper;
			entryOper = new EntryOper();
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			new SqlCommand();
			new Result();
			inv.Received = false;
			bool result = default(bool);
			if (Sync.BranchType == 4)
			{
				if (!(Sync.ValidAPIUrl & !MainClass.IsTrial))
				{
					return result;
				}
				if (!MainClass.CheckForInternetConnection())
				{
					invoiceCRUD.AddInvoiceLocally(inv, IsNew);
					return false;
				}
				ClsInvoice clsInvoice;
				clsInvoice = new ClsInvoice(Sync.APIUrl, "");
				new ClsLogin(Sync.APIUrl, "");
				list.Add(inv);
				new List<PaymentType>();
				Result result2;
				result2 = await clsInvoice.Save(paymentTypes: InvoiceOper.GetInvoiceCloudPaymentType(), invoices: list, currentUser: User.CurrentCloudUser);
				if (result2.IsValid)
				{
					try
					{
						if (sqlConnection.State != ConnectionState.Open)
						{
							sqlConnection.Open();
						}
						foreach (Invoice item in list)
						{
							if (item != null)
							{
								item.Sent = true;
								new SqlCommand("update InvContratct set Sync=1 where InvGlobalID=N'" + item.InvGlobalID + "'", sqlConnection).ExecuteNonQuery();
							}
						}
						return true;
					}
					catch (Exception projectError)
					{
						ProjectData.SetProjectError(projectError);
						result = false;
						ProjectData.ClearProjectError();
						return result;
					}
				}
				if (Sync.SyncCloudFirst)
				{
					MsgGeneralAlter msgGeneralAlter;
					msgGeneralAlter = new MsgGeneralAlter();
					msgGeneralAlter.lblMsg.Text = "تنبيه";
					msgGeneralAlter.txtAlarm.Text = result2.ErrorMessage.ToString();
					msgGeneralAlter.ShowDialog();
					return false;
				}
				invoiceCRUD.AddInvoiceLocally(inv, IsNew);
				return result;
			}
			if (Sync.ValidAPIUrl & !MainClass.IsTrial)
			{
				list = (List<Invoice>)(await invoiceCRUD.PostInvoicesOnline(inv, IsNew));
			}
			else
			{
				invoiceCRUD.AddInvoiceLocally(inv, IsNew);
			}
			if (list.Count > 0)
			{
				if (sqlConnection.State != ConnectionState.Open)
				{
					sqlConnection.Open();
				}
				foreach (Invoice item2 in list)
				{
					if (item2 != null)
					{
						new SqlCommand("update Inv set Sync=1 where InvGlobalID=N'" + item2.InvGlobalID + "'", sqlConnection).ExecuteNonQuery();
					}
				}
			}
			entryOper.SyncEntry(Eny, IsNew);
			return result;
		}

		private static string GetEntryGlobalID(int EntryID, int InvoiceType, int Branch)
		{
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter(selectConnection: MainClass.ConnObj(), selectCommandText: "select GlobalID from Entry where id=" + Conversions.ToString(EntryID) + " and type=" + Conversions.ToString(InvoiceType) + " and  IS_Deleted=0 and branch=" + Conversions.ToString(Branch));
			DataTable dataTable;
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			if (dataTable.Rows.Count > 0)
			{
				return Conversions.ToString(dataTable.Rows[0][0]);
			}
			if (EntryID != -1)
			{
				string EntryGlobalId;
				EntryGlobalId = "";
				EntryOper.GenerateEntryGlobalIDByEntryNo(ref EntryGlobalId, EntryID, Branch);
				return EntryGlobalId;
			}
			return Conversions.ToString(-1);
		}

		public static void BindingInvoice(ref Invoicecontract Invo, string sqlstr)
		{
			try
			{
				SqlConnection sqlConnection;
				sqlConnection = MainClass.ConnObj();
				if (sqlConnection.State != ConnectionState.Open)
				{
					sqlConnection.Open();
				}
				SqlCommand sqlCommand;
				sqlCommand = new SqlCommand(sqlstr, sqlConnection);
				if (sqlConnection.State != ConnectionState.Open)
				{
					sqlConnection.Open();
				}
				SqlDataReader sqlDataReader;
				sqlDataReader = sqlCommand.ExecuteReader();
				if (sqlDataReader.HasRows)
				{
					sqlDataReader.Read();
					Invo.ISNew = false;
					Invo.IsPrinted = true;
					Invo.IsLoaded = false;
					Invo.InvGlobalID = Conversions.ToString(sqlDataReader["InvGlobalID"]);
					Invo.QRCode = Conversions.ToString(sqlDataReader["QRCode"]);
					if (sqlDataReader["CloudID"] != DBNull.Value)
					{
						Invo.CloudID = Conversions.ToString(sqlDataReader["CloudID"]);
					}
					else
					{
						Invo.CloudID = "";
					}
					if (Operators.ConditionalCompareObjectNotEqual(sqlDataReader["EntryID"], -1, TextCompare: false))
					{
						int num;
						num = Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlDataReader["inv_type"]));
						if (Conversions.ToBoolean(Operators.AndObject(num == 2, Operators.CompareObjectEqual(sqlDataReader["proc_type"], 2, TextCompare: false))))
						{
							num = 22;
						}
						else if (Conversions.ToBoolean(Operators.AndObject(num == 1, Operators.CompareObjectEqual(sqlDataReader["proc_type"], 2, TextCompare: false))))
						{
							num = 21;
						}
						Invo.EntryGlobalID = InvoiceOperContract.GetEntryGlobalID(Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlDataReader["EntryID"])), num, Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlDataReader["branch"])));
					}
					else
					{
						Invo.EntryGlobalID = Conversions.ToString(-1);
					}
					Invo.ProcType = Conversions.ToInteger(sqlDataReader["proc_type"]);
					Invo.PriceIncVAT = Conversions.ToBoolean(sqlDataReader["PriceIncVAT"]);
					Invo.InvoiceNo = checked((int)Math.Round(Convert.ToDouble(Operators.ConcatenateObject("", sqlDataReader["id"]))));
					Invo.InvDate = Conversions.ToDate(Operators.ConcatenateObject("", sqlDataReader["date"]));
					Invo.InvTime = Conversions.ToDate(Convert.ToDateTime(Operators.ConcatenateObject("", sqlDataReader["date"])).ToShortTimeString());
					Invo.InvoiceType = (InvoiceType)Conversions.ToInteger(sqlDataReader["inv_type"]);
					Invo.Branch = checked((int)Math.Round(Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["branch"]))));
					if (sqlDataReader["InvCombinedId"] != DBNull.Value)
					{
						Invo.InvCombinedId = Conversions.ToString(sqlDataReader["InvCombinedId"]);
					}
					else
					{
						Invo.InvCombinedId = string.Concat(Conversions.ToString(Invo.Branch) + Conversions.ToString((int)Invo.InvoiceType), Conversions.ToString(Invo.ProcType), Conversions.ToString(Invo.InvoiceNo));
					}
					Invo.AdditionalCost = Conversions.ToDecimal(sqlDataReader["AdditionalCost"]);
					Invo.InvoiceStatus = Conversions.ToInteger(sqlDataReader["InvoiceStatus"]);
					Invo.Treasury = Conversions.ToInteger(Operators.ConcatenateObject("", sqlDataReader["stock"]));
					Invo.Store = Conversions.ToInteger(Operators.ConcatenateObject("", sqlDataReader["safe"]));
					Invo.InvDiscount = Conversions.ToDouble(Operators.ConcatenateObject("", sqlDataReader["minus"]));
					Invo.TotDiscount = Invo.InvDiscount;
					Invo.VAT = Conversions.ToDouble(Operators.ConcatenateObject("", sqlDataReader["tax"]));
					Invo.TotalWithholdingTax = Conversions.ToDouble(Operators.ConcatenateObject("", sqlDataReader["TotalWithholdingTax"]));
					Invo.Net = Conversions.ToDouble(Operators.ConcatenateObject("", sqlDataReader["tot_net"]));
					Invo.Total = Conversions.ToDouble(Operators.ConcatenateObject("", sqlDataReader["InvTotal"]));
					Invo.ExtraVAT = Conversions.ToDouble(Operators.ConcatenateObject("", sqlDataReader["ExtraVAT"]));
					Invo.Paid = Conversions.ToDouble(Operators.ConcatenateObject("", sqlDataReader["paid"]));
					Invo.Paycash = Conversions.ToDouble(Operators.ConcatenateObject("", sqlDataReader["cash"]));
					Invo.PayATM = Conversions.ToDouble(Operators.ConcatenateObject("", sqlDataReader["Visa"]));
					Invo.Remainder = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["tot_net"])) - Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["paid"]));
					Invo.Delivery = Conversions.ToDouble(sqlDataReader["AdditionsTot"]);
					Invo.Additions = Invo.Delivery;
					Invo.Insurance = Conversions.ToDouble(sqlDataReader["Insurance"]);
					Invo.InvNote = Conversions.ToString(Operators.ConcatenateObject("", sqlDataReader["notes"]));
					try
					{
						Invo.RefDate = Conversions.ToDate(Operators.ConcatenateObject("", sqlDataReader["Reff_date"]));
					}
					catch (Exception projectError)
					{
						ProjectData.SetProjectError(projectError);
						ProjectData.ClearProjectError();
					}
					if (sqlDataReader["Balance_previews"] != DBNull.Value)
					{
						Invo.BalancePreviews = Conversions.ToSingle(Operators.ConcatenateObject("", sqlDataReader["Balance_previews"]));
					}
					else
					{
						Invo.BalancePreviews = 0f;
					}
					Invo.ReffNo = Conversions.ToString(Operators.ConcatenateObject("", sqlDataReader["Reff_No"]));
					Invo.PayType = Conversions.ToInteger(Operators.ConcatenateObject("", sqlDataReader["pay_type"]));
					Invo.Bank = Conversions.ToInteger(sqlDataReader["bank"]);
					Invo.Customer = Conversions.ToInteger(Operators.ConcatenateObject("", sqlDataReader["cust_id"]));
					Invo.Saleman = Conversions.ToInteger(Operators.ConcatenateObject("", sqlDataReader["salesman"]));
					Invo.User = Conversions.ToInteger(Operators.ConcatenateObject("", sqlDataReader["sales_emp"]));
					Invo.UUID = Conversions.ToString(Operators.ConcatenateObject("", sqlDataReader["UUID"]));
					Invo.contract_total_value = Conversions.ToDouble(Operators.ConcatenateObject("", sqlDataReader["contract_total_value"]));
					Invo.original_work_amount = Conversions.ToDouble(Operators.ConcatenateObject("", sqlDataReader["original_work_amount"]));
					Invo.special_discount = Conversions.ToDouble(Operators.ConcatenateObject("", sqlDataReader["special_discount"]));
					Invo.amount_after_discount = Conversions.ToDouble(Operators.ConcatenateObject("", sqlDataReader["amount_after_discount"]));
					Invo.vat_amount = Conversions.ToDouble(Operators.ConcatenateObject("", sqlDataReader["vat_amount"]));
					Invo.total_with_vat = Conversions.ToDouble(Operators.ConcatenateObject("", sqlDataReader["total_with_vat"]));
					Invo.work_guarantee = Conversions.ToDouble(Operators.ConcatenateObject("", sqlDataReader["work_guarantee"]));
					Invo.net_due_this_payment = Conversions.ToDouble(Operators.ConcatenateObject("", sqlDataReader["net_due_this_payment"]));
					Invo.previously_paid_amount = Conversions.ToDouble(Operators.ConcatenateObject("", sqlDataReader["previously_paid_amount"]));
					Invo.remaining_contract_balance = Conversions.ToDouble(Operators.ConcatenateObject("", sqlDataReader["remaining_contract_balance"]));
					if (sqlDataReader["TableNo"] != DBNull.Value)
					{
						Invo.TableNo = Conversions.ToString(sqlDataReader["TableNo"]);
					}
					if ((sqlDataReader["PaymentStatus"] == null) | (sqlDataReader["PaymentStatus"] == DBNull.Value))
					{
						Invo.PaymentStatus = PaymentStatus.Paid;
					}
					else
					{
						Invo.PaymentStatus = (PaymentStatus)Conversions.ToInteger(Operators.ConcatenateObject("", sqlDataReader["PaymentStatus"]));
					}
					if (sqlDataReader["CashCustomerMobile"] != DBNull.Value)
					{
						Invo.CashCustomerMobile = Conversions.ToString(sqlDataReader["CashCustomerMobile"]);
					}
					else
					{
						Invo.CashCustomerMobile = "";
					}
					if (sqlDataReader["CashCustomerName"] != DBNull.Value)
					{
						Invo.CashCustomerName = Conversions.ToString(sqlDataReader["CashCustomerName"]);
					}
					else
					{
						Invo.CashCustomerName = "";
					}
					sqlDataReader.Close();
					SqlDataAdapter sqlDataAdapter;
					sqlDataAdapter = new SqlDataAdapter("SELECT \r\n    Inv.id ,\r\n    Inv.proc_type ,\r\n    Inv.ItemId,\r\n    Inv.Description,\r\n    Inv.notes ,\r\n    Inv.unit ,\r\n    Inv.val ,\r\n    Inv.val1 ,\r\n    Inv.exchange_price ,\r\n    Inv.discount ,\r\n    Inv.ItemPriceWithoutVAT ,\r\n    Inv.AvrgCost ,\r\n    Inv.taxval,\r\n    Inv.taxperc ,\r\n    Inv.CurrentQnty ,\r\n    Inv.store ,\r\n    Inv.expire_date ,\r\n    Inv.WithholdingTaxPerc ,\r\n    Inv.WithholdingTax,\r\n    ISNULL(Inv.ItemCostCenter, '') AS ItemCostCenter,\r\n    ISNULL(Inv.ItemAdditionalTaxPerc, 0) AS ItemAdditionalTaxPerc,\r\n    ISNULL(Inv.ItemAdditionalTax, 0) AS ItemAdditionalTax,\r\n    I.name AS ItemName,\r\n    I.nameEN AS ItemNameEn,\r\n    I.code AS ItemCode,\r\n    I.ItemProperty,\r\n    ISNULL(I.ItemType,0) AS ItemType,\r\n    ISNULL(I.is_extra_tax_applied,0) AS isExtraTaxApplied,\r\n    U.name AS UnitName,\r\n    ISNULL(U.UnitCode, '') AS UnitCode,\r\n    Max(IU.barcode) AS UnitBarcode,\r\n    I.EgyCodeType,\r\n    I.EgyItemCode\t\t\t\t\t\r\nFROM \r\n    InvContratct_Sub AS Inv\r\nLEFT JOIN \r\n    Items AS I ON Inv.ItemId = I.id \r\nLEFT JOIN \r\n    Units AS U ON Inv.unit = U.id \r\nLEFT JOIN \r\n    ItemUnits AS IU ON Inv.ItemId = IU.ItemId AND Inv.unit = IU.unit\r\nWHERE \r\n    Inv.InvGlobalID = N'" + Invo.InvGlobalID + "' \r\n    AND Inv.ProductId = 0 \r\n\r\n\tgroup by \r\n\tInv.id  ,\r\n    Inv.proc_type  ,\r\n    Inv.ItemId,\r\n    Inv.Description,\r\n    Inv.notes ,\r\n    Inv.unit ,\r\n    Inv.val ,\r\n    Inv.val1 ,\r\n    Inv.exchange_price,\r\n    Inv.discount ,\r\n    Inv.ItemPriceWithoutVAT ,\r\n    Inv.AvrgCost ,\r\n    Inv.taxval ,\r\n    Inv.taxperc ,\r\n    Inv.CurrentQnty ,\r\n    Inv.store ,\r\n    Inv.expire_date,\r\n    Inv.WithholdingTaxPerc ,\r\n    Inv.WithholdingTax ,\r\n    ISNULL(Inv.ItemAdditionalTaxPerc,0) ,\r\n    ISNULL(Inv.ItemAdditionalTax,0) ,\r\n    ISNULL(Inv.ItemCostCenter, '') ,\r\n    I.name ,\r\n    I.nameEN ,\r\n    I.code ,\r\n    I.ItemProperty,\r\n    ISNULL(I.ItemType,0),\r\n    ISNULL(I.is_extra_tax_applied,0),\r\n    U.name ,\r\n    ISNULL(U.UnitCode, '') ,\r\n    I.EgyCodeType,\r\n    I.EgyItemCode\t\t\t\t\t\r\nORDER BY \r\n    Inv.id;\r\n", sqlConnection);
					DataTable dataTable;
					dataTable = new DataTable();
					sqlDataAdapter.Fill(dataTable);
					checked
					{
						if (dataTable.Rows.Count > 0)
						{
							int num2;
							num2 = dataTable.Rows.Count - 1;
							for (int i = 0; i <= num2; i++)
							{
								try
								{
									InvoiceItem Itm;
									Itm = new InvoiceItem();
									Itm.ItemRowIndex = i + 1;
									Itm.InvGlobalID = Invo.InvGlobalID;
									Itm.InvertoryImpact = Conversions.ToInteger(dataTable.Rows[i]["proc_type"]);
									if (dataTable.Rows[i]["ItemProperty"] != DBNull.Value)
									{
										Itm.ItemProperty = Conversions.ToInteger(dataTable.Rows[i]["ItemProperty"]);
									}
									Itm.ItemType = Conversions.ToInteger(dataTable.Rows[i]["ItemType"]);
									Itm.ItemCode = Conversions.ToString(dataTable.Rows[i]["ItemCode"]);
									Itm.ItemId = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["ItemId"]));
									Itm.ItemName = Conversions.ToString(dataTable.Rows[i]["ItemName"]);
									Itm.Description = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["Description"]));
									Itm.ItemNotes = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["notes"]));
									Itm.UnitName = Conversions.ToString(dataTable.Rows[i]["UnitName"]);
									Itm.UnitCode = Conversions.ToString(dataTable.Rows[i]["UnitCode"]);
									Itm.UnitID = Conversions.ToInteger(dataTable.Rows[i]["unit"]);
									Itm.ItemBarcode = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["UnitBarcode"]));
									Itm.ItemPrimaryQnty = Conversions.ToDouble(Convert.ToString(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["val"])));
									Itm.ItemQuantity = Conversions.ToDouble(Convert.ToString(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["val1"])));
									Itm.UnitEquality = Common.DivisionOperation(new decimal(Itm.ItemPrimaryQnty), new decimal(Itm.ItemQuantity));
									Itm.ItemPrice = Conversions.ToDouble(dataTable.Rows[i]["exchange_price"]);
									if (!string.IsNullOrEmpty(Conversions.ToString(dataTable.Rows[i]["ItemPriceWithoutVAT"])))
									{
										Itm.ItemPriceWithoutVAT = Conversions.ToDouble(dataTable.Rows[i]["ItemPriceWithoutVAT"]);
									}
									else
									{
										Itm.ItemPriceWithoutVAT = Conversions.ToDouble(dataTable.Rows[i]["exchange_price"]);
									}
									Itm.ItemSumPrice = Itm.ItemQuantity * Itm.ItemPrice;
									Itm.ItemDiscount = Conversion.Val(Operators.ConcatenateObject("", dataTable.Rows[i]["discount"]));
									Itm.WithholdingTax = Conversion.Val(Operators.ConcatenateObject("", dataTable.Rows[i]["WithholdingTax"]));
									Itm.WithholdingTaxPerc = Conversion.Val(Operators.ConcatenateObject("", dataTable.Rows[i]["WithholdingTaxPerc"]));
									Itm.ItemDiscountPerc = Common.DivisionOperation(new decimal(Itm.ItemDiscount), new decimal(Itm.ItemSumPrice)) * 100.0;
									Itm.EgyCodeType = Conversions.ToString(dataTable.Rows[i]["EgyCodeType"]);
									Itm.EgyItemCode = Conversions.ToString(dataTable.Rows[i]["EgyItemCode"]);
									Invo.TotDiscount += Itm.ItemDiscount;
									Invo.SumPrice += Itm.ItemSumPrice;
									Invoicecontract obj;
									obj = Invo;
									obj.TotalQty = new decimal(Convert.ToDouble(obj.TotalQty) + Itm.ItemPrimaryQnty);
									if ((dataTable.Rows[i]["AvrgCost"] == null) | (dataTable.Rows[i]["AvrgCost"] == DBNull.Value))
									{
										Itm.ItemCost = 0.0;
									}
									else
									{
										Itm.ItemCost = Conversions.ToDouble(dataTable.Rows[i]["AvrgCost"]);
									}
									Itm.ItemTotalPrice = Itm.ItemSumPrice - Itm.ItemDiscount;
									Itm.ItemPriceDiscount = Itm.ItemDiscount / Itm.ItemQuantity;
									Itm.ItemPriceAfterDiscount = Itm.ItemTotalPrice / Itm.ItemQuantity;
									Itm.ItemVat = Conversions.ToDouble(dataTable.Rows[i]["taxval"]);
									Itm.isExtraTaxApplied = Conversions.ToBoolean(dataTable.Rows[i]["isExtraTaxApplied"]);
									Itm.ItemAdditionalTax = Conversions.ToDouble(dataTable.Rows[i]["ItemAdditionalTax"]);
									Itm.ItemAdditionalTaxPerc = Conversions.ToDouble(dataTable.Rows[i]["ItemAdditionalTaxPerc"]);
									Itm.ItemVatPerc = Conversions.ToDouble(dataTable.Rows[i]["taxperc"]);
									if (Invo.PriceIncVAT)
									{
										Itm.ItemTotalPrice -= Itm.ItemVat;
										if (Itm.isExtraTaxApplied)
										{
											Itm.ItemTotalPrice += Itm.ItemAdditionalTax * (Itm.ItemVatPerc / 100.0);
										}
									}
									Itm.ValiableInvertory = Conversions.ToDouble(dataTable.Rows[i]["CurrentQnty"]);
									Itm.ItemCostCenter = Conversions.ToString(dataTable.Rows[i]["ItemCostCenter"]);
									Itm.ItemNetPrice = Itm.ItemTotalPrice + Itm.ItemVat + Itm.ItemAdditionalTax;
									Invo.SumCost += Itm.ItemCost * Itm.ItemPrimaryQnty;
									Invo.InvProfit = Invo.Total - Invo.SumCost;
									try
									{
										if (dataTable.Rows[i]["store"] != null && Conversion.Val(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["store"])) > 0.0)
										{
											Itm.InvertoryId = Convert.ToInt32(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["store"]));
											Itm.InvertoryName = Common.GetStoreName(Itm.InvertoryId);
										}
									}
									catch (Exception projectError2)
									{
										ProjectData.SetProjectError(projectError2);
										ProjectData.ClearProjectError();
									}
									try
									{
										Itm.ItemExpireDate = Conversions.ToDate(Operators.ConcatenateObject("", dataTable.Rows[i]["expire_date"]));
									}
									catch (Exception projectError3)
									{
										ProjectData.SetProjectError(projectError3);
										ProjectData.ClearProjectError();
									}
									InvoiceOperContract.BindingItemDetails(ref Itm);
									InvoiceOperContract.glassOtions(ref Itm);
									Invo.InvoiceItems.Add(Itm);
								}
								catch (Exception projectError4)
								{
									ProjectData.SetProjectError(projectError4);
									ProjectData.ClearProjectError();
								}
							}
						}
						InvoiceOperContract.BindingCostDetaile(ref Invo);
						sqlDataAdapter = new SqlDataAdapter("select CCcode from Entry_sub where branch=" + Conversions.ToString(MainClass.BranchNo) + " and EntryGlobalID=N'" + Invo.EntryGlobalID + "' and acc_no=N'" + Invo.InvAccCode + "'", sqlConnection);
						dataTable = new DataTable();
						sqlDataAdapter.Fill(dataTable);
						if (dataTable.Rows.Count > 0 && dataTable.Rows[0]["CCcode"] != DBNull.Value)
						{
							Invo.InvCCcode = Conversions.ToString(Convert.ToInt32(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["CCcode"])));
						}
					}
				}
				else
				{
					Invo = null;
				}
			}
			catch (Exception projectError5)
			{
				ProjectData.SetProjectError(projectError5);
				Invo = null;
				ProjectData.ClearProjectError();
			}
		}

		public static void BindingItemDetails(ref InvoiceItem Itm)
		{
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter(selectConnection: MainClass.ConnObj(), selectCommandText: "select * from InvoiceItemDetail where InvGlobalID=N'" + Itm.InvGlobalID + "' and ItemId=" + Conversions.ToString(Itm.ItemId) + " ");
			DataTable dataTable;
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			new InvoiceItemDetail();
			checked
			{
				if (dataTable.Rows.Count > 0)
				{
					int num;
					num = dataTable.Rows.Count - 1;
					for (int i = 0; i <= num; i++)
					{
						InvoiceItemDetail invoiceItemDetail;
						invoiceItemDetail = new InvoiceItemDetail();
						invoiceItemDetail.ItemDetailId = Conversions.ToInteger(dataTable.Rows[i]["ItemDetailId"]);
						invoiceItemDetail.ItemIncrId = Conversions.ToInteger(dataTable.Rows[i]["ItemIncrId"]);
						invoiceItemDetail.InvGlobalID = Conversions.ToString(dataTable.Rows[i]["InvGlobalID"]);
						invoiceItemDetail.ItemId = Conversions.ToInteger(dataTable.Rows[i]["ItemId"]);
						invoiceItemDetail.InvertoryImpact = Conversions.ToInteger(dataTable.Rows[i]["InvertoryImpact"]);
						invoiceItemDetail.ItemSerialNo = Conversions.ToString(dataTable.Rows[i]["ItemSerialNo"]);
						invoiceItemDetail.BatchNo = Conversions.ToString(dataTable.Rows[i]["BatchNo"]);
						invoiceItemDetail.ItemProductionDate = Conversions.ToDate(dataTable.Rows[i]["ItemProductionDate"]);
						invoiceItemDetail.ItemExpireDate = Conversions.ToDate(dataTable.Rows[i]["ItemExpireDate"]);
						invoiceItemDetail.ItemHeight = Conversions.ToDouble(dataTable.Rows[i]["ItemHeight"]);
						invoiceItemDetail.ItemWidth = Conversions.ToDouble(dataTable.Rows[i]["ItemWidth"]);
						invoiceItemDetail.ItemColor = Conversions.ToString(dataTable.Rows[i]["ItemColor"]);
						invoiceItemDetail.ItemSize = Conversions.ToString(dataTable.Rows[i]["ItemSize"]);
						invoiceItemDetail.ItemProperty = Conversions.ToInteger(dataTable.Rows[i]["ItemProperty"]);
						invoiceItemDetail.FillValue = Conversions.ToDouble(dataTable.Rows[i]["FillValue"]);
						invoiceItemDetail.FillRatio = Conversions.ToDouble(dataTable.Rows[i]["FillRatio"]);
						invoiceItemDetail.ItemQuantity = Conversions.ToDouble(dataTable.Rows[i]["ItemQuantity"]);
						invoiceItemDetail.ItemBarcode = Conversions.ToString(dataTable.Rows[i]["ItemBarcode"]);
						Itm.InvoiceItemDetails.Add(invoiceItemDetail);
					}
				}
			}
		}

		public static void glassOtions(ref InvoiceItem Itm)
		{
			checked
			{
				try
				{
					SqlDataAdapter sqlDataAdapter;
					sqlDataAdapter = new SqlDataAdapter(selectConnection: MainClass.ConnObj(), selectCommandText: "select * from Glasses where InvGlobalID=N'" + Itm.InvGlobalID + "' and ItemId=" + Conversions.ToString(Itm.ItemId) + " ");
					DataTable dataTable;
					dataTable = new DataTable();
					sqlDataAdapter.Fill(dataTable);
					new Glass();
					int num;
					num = dataTable.Rows.Count - 1;
					for (int i = 0; i <= num; i++)
					{
						Glass glass;
						glass = new Glass();
						glass.InvGlobalID = Itm.InvGlobalID;
						glass.ItemId = Itm.ItemId;
						glass.orientation = Conversions.ToString(dataTable.Rows[i]["orientation"]);
						glass.SPH = Conversions.ToString(dataTable.Rows[i]["SPH"]);
						glass.CYL = Conversions.ToString(dataTable.Rows[i]["CYL"]);
						glass.AX = Conversions.ToString(dataTable.Rows[i]["AX"]);
						glass.ADD = Conversions.ToString(dataTable.Rows[i]["ADD"]);
						glass.IPD = Conversions.ToString(dataTable.Rows[i]["IPD"]);
						Itm.Glasses.Add(glass);
					}
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					ProjectData.ClearProjectError();
				}
			}
		}

		public static void BindingCostDetaile(ref Invoicecontract Invo)
		{
			checked
			{
				try
				{
					SqlDataAdapter sqlDataAdapter;
					sqlDataAdapter = new SqlDataAdapter(selectConnection: MainClass.ConnObj(), selectCommandText: "select * from InvoiceCost where InvGlobalID=N'" + Invo.InvGlobalID + "'");
					DataTable dataTable;
					dataTable = new DataTable();
					sqlDataAdapter.Fill(dataTable);
					new InvoiceCost();
					if (dataTable.Rows.Count > 0)
					{
						int num;
						num = dataTable.Rows.Count - 1;
						for (int i = 0; i <= num; i++)
						{
							InvoiceCost invoiceCost;
							invoiceCost = new InvoiceCost();
							invoiceCost.InvoiceCostId = Conversions.ToInteger(dataTable.Rows[i]["InvoiceCostId"]);
							invoiceCost.InvGlobalID = Conversions.ToString(dataTable.Rows[i]["InvGlobalID"]);
							invoiceCost.InvoiceCostNo = Conversions.ToInteger(dataTable.Rows[i]["InvoiceCostNo"]);
							invoiceCost.CostId = Conversions.ToInteger(dataTable.Rows[i]["CostId"]);
							invoiceCost.CostName = Conversions.ToString(dataTable.Rows[i]["CostName"]);
							invoiceCost.Cost = Conversions.ToSingle(dataTable.Rows[i]["Cost"]);
							invoiceCost.CostDate = Conversions.ToDate(dataTable.Rows[i]["CostDate"]);
							invoiceCost.Note = Conversions.ToString(dataTable.Rows[i]["Note"]);
							invoiceCost.costCenter = Conversions.ToInteger(dataTable.Rows[i]["costCenter"]);
							Invo.InvoiceCosts.Add(invoiceCost);
						}
					}
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					ProjectData.ClearProjectError();
				}
			}
		}

		public static int InvoiceNo(int InvType, int ProcType, int Prefixe)
		{
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			if (sqlConnection.State != ConnectionState.Open)
			{
				sqlConnection.Open();
			}
			int num;
			num = checked((int)Convert.ToInt64(RuntimeHelpers.GetObjectValue(new SqlCommand("select ISNULL(MAX(id), 0) from InvContratct where branch=" + Conversions.ToString(MainClass.BranchNo) + " and inv_type=" + Conversions.ToString(InvType) + " and proc_type=" + Conversions.ToString(ProcType), sqlConnection).ExecuteScalar())) + 1);
			if (num == 1)
			{
				num = Prefixe;
			}
			if (sqlConnection.State != ConnectionState.Closed)
			{
				sqlConnection.Close();
			}
			return num;
		}

		public static int GetAutoIncrementID()
		{
			try
			{
				SqlConnection sqlConnection;
				sqlConnection = MainClass.ConnObj();
				if (sqlConnection.State != ConnectionState.Open)
				{
					sqlConnection.Open();
				}
				new SqlCommand();
				return checked((int)Convert.ToInt64(RuntimeHelpers.GetObjectValue(new SqlCommand("select ISNULL(MAX(proc_id), 1) from InvContratct", sqlConnection).ExecuteScalar())));
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return 1;
		}

		public static bool DeleteInvoice(Invoicecontract Invoic)
		{
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			if (sqlConnection.State != ConnectionState.Open)
			{
				sqlConnection.Open();
			}
			SqlTransaction sqlTransaction;
			sqlTransaction = sqlConnection.BeginTransaction();
			new SqlCommand();
			bool result;
            var Home = new Home();
            try
			{
				if (MessageBox.Show("  هل انت متأكد من الحذف  ", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					new SqlCommand("update InvContratct set IS_Deleted=1 where InvGlobalID=N'" + Invoic.InvGlobalID + "'", sqlConnection, sqlTransaction).ExecuteNonQuery();
					if (Operators.CompareString(Invoic.EntryGlobalID, "-1", TextCompare: false) != 0)
					{
						new SqlCommand("update Entry set IS_Deleted=1 ,state=2 where GlobalID=N'" + Invoic.EntryGlobalID + "'", sqlConnection, sqlTransaction).ExecuteNonQuery();
					}
					if (Home._is_active && ConnectBroker.IsConnectedToInternet() && ConnectBroker.CheckConnectionAndBroker())
					{
						string cond;
						cond = "where InvGlobalID=N'" + Invoic.InvGlobalID + "'";
						string invGlobalID;
						invGlobalID = "WHERE InvGlobalID=N'" + Invoic.InvGlobalID + "'";
						string cond2;
						cond2 = "where GlobalID=N'" + Invoic.EntryGlobalID + "'";
						string cond3;
						cond3 = "where EntryGlobalID=N'" + Invoic.EntryGlobalID + "'";
						SendData.SendDataa("Inv", Invoic.InvGlobalID, SendData.GetInv(cond));
						SendData.SendDataa("InvSub", Invoic.InvGlobalID, SendData.GetInvSub(invGlobalID));
						SendData.SendDataa("entry", Invoic.EntryGlobalID, SendData.GetEntryData(cond2));
						SendData.SendDataa("entryDetails", Invoic.EntryGlobalID, SendData.GetEntrySubData(cond3));
					}
					if (Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0)
					{
						MessageBox.Show("تم الحذف", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					}
					else
					{
						MessageBox.Show("Deleted", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					}
					sqlTransaction.Commit();
					result = true;
				}
				else
				{
					result = false;
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2;
				ex2 = ex;
				sqlTransaction.Rollback();
				string text;
				text = "خطأ أثناء الحفظ";
				if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
				{
					text = "error in saving";
				}
				MessageBox.Show((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) != 0) ? (text + Environment.NewLine + "Error details: " + ex2.Message) : (text + Environment.NewLine + "تفاصيل الخطأ: " + ex2.Message), "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				result = false;
				ProjectData.ClearProjectError();
			}
			finally
			{
				if (sqlConnection.State != ConnectionState.Closed)
				{
					sqlConnection.Close();
				}
			}
			return result;
		}

		public static void InsertFromInv(ref Invoicecontract Invo, int Ptype)
		{
			try
			{
				frmInvoiceSrch frmInvoiceSrch2;
				frmInvoiceSrch2 = new frmInvoiceSrch();
				frmInvoiceSrch2.cmbProcType.IsEnabled = false;
				MainClass.ApplyPermissionToForm(frmInvoiceSrch2);
				MainClass.DoApplyUserSett(frmInvoiceSrch2);
				frmInvoiceSrch2.ProcType = Ptype;
				if ((Ptype == 4) & (Invo.InvoiceType == InvoiceType.POS))
				{
					frmInvoiceSrch2.InvType = 2;
				}
				else if ((Ptype == 4) & (Invo.InvoiceType == InvoiceType.Sale))
				{
					frmInvoiceSrch2.cmbProcType.IsEnabled = true;
					frmInvoiceSrch2.InvType = 2;
				}
				else if (Invo.InvoiceType == (InvoiceType)21)
				{
					frmInvoiceSrch2.cmbProcType.IsEnabled = true;
					frmInvoiceSrch2.InvType = 2;
					frmInvoiceSrch2.Inv_typeCridet = 21;
				}
				else if (Invo.InvoiceType == InvoiceType.ReturnSales)
				{
					frmInvoiceSrch2.cmbProcType.IsEnabled = true;
					frmInvoiceSrch2.InvType = 1;
					frmInvoiceSrch2.Inv_typeCridet = 22;
				}
				else if (Invo.InvoiceType == InvoiceType.HoldInv)
				{
					frmInvoiceSrch2.cmbProcType.IsEnabled = true;
					frmInvoiceSrch2.InvType = 23;
					frmInvoiceSrch2.Inv_typeCridet = 23;
				}
				else
				{
					frmInvoiceSrch2.InvType = (int)Invo.InvoiceType;
				}
				frmInvoiceSrch2.ShowDialog();
				if (!(frmInvoiceSrch2.ISDone & (Operators.CompareString(frmInvoiceSrch2.InvGlobalID, "-1", TextCompare: false) != 0)))
				{
					return;
				}
				InvoiceOperContract.BindingInvoice(ref Invo, "select * from InvContratct where IS_Deleted=0 and InvGlobalID=N'" + frmInvoiceSrch2.InvGlobalID + "'");
				frmInvoiceItems frmInvoiceItems2;
				frmInvoiceItems2 = new frmInvoiceItems();
				MainClass.ApplyPermissionToForm(frmInvoiceItems2);
				MainClass.DoApplyUserSett(frmInvoiceItems2);
				frmInvoiceItems2.InvGlobalId = frmInvoiceSrch2.InvGlobalID;
				frmInvoiceItems2.InvType = (int)Invo.InvoiceType;
				frmInvoiceItems2.invcontract = Invo;
				if (Invo.ProcType != 2)
				{
					if (Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0)
					{
						frmInvoiceItems2.btnadd.DataContext = " إدراج";
						frmInvoiceItems2.btnAddall.DataContext = "إدراج الكل ";
					}
					else
					{
						frmInvoiceItems2.btnadd.DataContext = "Insert";
						frmInvoiceItems2.btnAddall.DataContext = "Insert all ";
					}
				}
				frmInvoiceItems2.ShowDialog();
				if (!frmInvoiceItems2.ISDone)
				{
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}
	}
}