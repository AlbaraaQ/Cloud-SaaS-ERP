using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using AuditorAPI.Models;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace SmartAuditERP
{

	public class InvoiceObj
	{
		public int ID { get; set; }

		public InvoiceType InvType { get; set; }

		public string InvoiceCode { get; set; }

		public string Name { get; set; }

		public int Store { get; set; }

		public int Unit { get; set; }

		public double VAT { get; set; }

		public bool PriceIncVAT { get; set; }

		public int Treasury { get; set; }

		public double DeliveryVal { get; set; }

		public double InsureVal { get; set; }

		public double DiscountVal { get; set; }

		public double DiscountPer { get; set; }

		public bool SaleByMinus { get; set; }

		public bool HasEntry { get; set; }

		public CostType CostType { get; set; }

		public PaymentOption PayType { get; set; }

		public Pricing Pricing { get; set; }

		public Currency Currency { get; set; }

		public string VATCode { get; set; }

		public string InvAcc { get; set; }

		public string InvReturnAcc { get; set; }

		public string DiscountAcc { get; set; }

		public string InsureAcc { get; set; }

		public string DeliveryAcc { get; set; }

		public string StoreAcc { get; set; }

		public bool SyncInv { get; set; }

		public bool SyncEntry { get; set; }

		public bool ShowPayForm { get; set; }

		public bool ShowPayfrmReturn { get; set; }

		public bool SaleManIsRequire { get; set; }

		public bool LinkedReturn { get; set; }

		public bool UnLinkedReturn { get; set; }

		public double AdditionalTax { get; set; }

		public string DigitsNo { get; set; }

		public int InvertoryImpact { get; set; }

		public bool ProcessFlow { get; set; }

		public bool PaymentStatus { get; set; }

		public bool IncInvInShift { get; set; }

		public string CloudSerial { get; set; }

		public string ReInvCloudSerial { get; set; }

		public int InvDefualtCust { get; set; }

		public bool CashCustRequire { get; set; }

		public bool DetailedItemEntry { get; set; }

		public bool printmakpay { get; set; }

		public int PayTypeDefault { get; set; }

		public int Prefixe { get; set; }

		public string safeFrom { get; set; }

		public string safeTo { get; set; }

		public InvoiceObj(int Id, int ProcType)
		{
			this.Name = "";
			this.Store = 1;
			this.Unit = 1;
			this.VAT = 15.0;
			this.PriceIncVAT = false;
			this.Treasury = 1;
			this.DeliveryVal = 0.0;
			this.InsureVal = 0.0;
			this.DiscountVal = 0.0;
			this.DiscountPer = 0.0;
			this.SaleByMinus = true;
			this.HasEntry = false;
			this.CostType = CostType.AvrgCost;
			this.PayType = PaymentOption.Cash;
			this.Pricing = Pricing.Default;
			this.Currency = Currency.SAR;
			this.VATCode = "";
			this.InvAcc = "";
			this.InvReturnAcc = "";
			this.DiscountAcc = "";
			this.InsureAcc = "";
			this.DeliveryAcc = "";
			this.StoreAcc = "";
			this.SyncInv = false;
			this.SyncEntry = false;
			this.ShowPayForm = false;
			this.ShowPayfrmReturn = false;
			this.SaleManIsRequire = false;
			this.LinkedReturn = true;
			this.UnLinkedReturn = true;
			this.AdditionalTax = 0.0;
			this.DigitsNo = "N2";
			this.InvertoryImpact = 0;
			this.ProcessFlow = false;
			this.PaymentStatus = false;
			this.IncInvInShift = false;
			this.CloudSerial = "Inv";
			this.ReInvCloudSerial = "RIN";
			this.InvDefualtCust = 0;
			this.CashCustRequire = false;
			this.DetailedItemEntry = false;
			this.printmakpay = false;
			this.PayTypeDefault = 1;
			this.Prefixe = 1;
			this.safeFrom = "";
			this.safeTo = "";
			if (Id <= -1)
			{
				return;
			}
			this.InvType = (InvoiceType)Id;
			this.ID = Id;
			SqlConnection selectConnection;
			selectConnection = MainClass.ConnObj();
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter("select * from SettingGeneral where Inv_Id=" + Conversions.ToString(Id), selectConnection);
			DataTable dataTable;
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			if (dataTable.Rows.Count == 1)
			{
				try
				{
					this.PriceIncVAT = Conversions.ToBoolean(dataTable.Rows[0]["PriceIncVAT"]);
					this.SaleByMinus = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["SaleByMinus"]));
					this.Unit = Conversions.ToInteger(dataTable.Rows[0]["unit"]);
					this.DeliveryVal = Conversions.ToDouble(dataTable.Rows[0]["DeliveryVal"]);
					this.InsureVal = Conversions.ToDouble(dataTable.Rows[0]["InsureVal"]);
					this.VAT = Conversions.ToDouble(dataTable.Rows[0]["MainVAT"]);
					if (dataTable.Rows[0]["prefixe"] != DBNull.Value)
					{
						this.Prefixe = Conversions.ToInteger(dataTable.Rows[0]["prefixe"]);
					}
					if (dataTable.Rows[0]["DefaultCust"] != DBNull.Value)
					{
						this.InvDefualtCust = Conversions.ToInteger(dataTable.Rows[0]["DefaultCust"]);
					}
					if (dataTable.Rows[0]["AdditionalTax"] != DBNull.Value)
					{
						this.AdditionalTax = Conversions.ToDouble(dataTable.Rows[0]["AdditionalTax"]);
					}
					if (dataTable.Rows[0]["CloudSerial"] != DBNull.Value)
					{
						this.CloudSerial = Conversions.ToString(dataTable.Rows[0]["CloudSerial"]);
					}
					if (dataTable.Rows[0]["ReInvCloudSerial"] != DBNull.Value)
					{
						this.ReInvCloudSerial = Conversions.ToString(dataTable.Rows[0]["ReInvCloudSerial"]);
					}
					if (dataTable.Rows[0]["CostType"] != null && Conversion.Val(Operators.ConcatenateObject("", dataTable.Rows[0]["CostType"])) > 0.0)
					{
						this.CostType = (CostType)checked((int)Math.Round(Conversion.Val(Operators.ConcatenateObject("", dataTable.Rows[0]["CostType"]))));
					}
					if ((dataTable.Rows[0]["VATCode"] != null) & (dataTable.Rows[0]["VATCode"] != DBNull.Value))
					{
						this.VATCode = Conversions.ToString(dataTable.Rows[0]["VATCode"]);
					}
					if ((dataTable.Rows[0]["ItemsAcc"] != null) & (dataTable.Rows[0]["ItemsAcc"] != DBNull.Value))
					{
						this.InvAcc = Conversions.ToString(dataTable.Rows[0]["ItemsAcc"]);
					}
					if ((dataTable.Rows[0]["ReturnItemsAcc"] != null) & (dataTable.Rows[0]["ReturnItemsAcc"] != DBNull.Value))
					{
						if (ProcType == 2)
						{
							this.InvAcc = Conversions.ToString(dataTable.Rows[0]["ReturnItemsAcc"]);
						}
						else
						{
							this.InvReturnAcc = Conversions.ToString(dataTable.Rows[0]["ReturnItemsAcc"]);
						}
					}
					if ((dataTable.Rows[0]["DiscountAcc"] != null) & (dataTable.Rows[0]["DiscountAcc"] != DBNull.Value))
					{
						this.DiscountAcc = Conversions.ToString(dataTable.Rows[0]["DiscountAcc"]);
					}
					if ((dataTable.Rows[0]["InsureAcc"] != null) & (dataTable.Rows[0]["InsureAcc"] != DBNull.Value))
					{
						this.InsureAcc = Conversions.ToString(dataTable.Rows[0]["InsureAcc"]);
					}
					if ((dataTable.Rows[0]["DeliveryAcc"] != null) & (dataTable.Rows[0]["DeliveryAcc"] != DBNull.Value))
					{
						this.DeliveryAcc = Conversions.ToString(dataTable.Rows[0]["DeliveryAcc"]);
					}
					if ((dataTable.Rows[0]["StoreAcc"] != null) & (dataTable.Rows[0]["StoreAcc"] != DBNull.Value))
					{
						this.DeliveryAcc = Conversions.ToString(dataTable.Rows[0]["StoreAcc"]);
					}
					if ((dataTable.Rows[0]["DigitsNo"] != null) & (dataTable.Rows[0]["DigitsNo"] != DBNull.Value))
					{
						this.DigitsNo = Conversions.ToString(Operators.ConcatenateObject("N", dataTable.Rows[0]["DigitsNo"]));
					}
					if ((dataTable.Rows[0]["InvoiceCode"] != null) & (dataTable.Rows[0]["InvoiceCode"] != DBNull.Value))
					{
						this.InvoiceCode = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[0]["InvoiceCode"]));
					}
					else
					{
						this.InvoiceCode = Conversions.ToString(Id);
					}
					if ((dataTable.Rows[0]["SaleManIsRequire"] != null) & (dataTable.Rows[0]["SaleManIsRequire"] != DBNull.Value))
					{
						this.SaleManIsRequire = Conversions.ToBoolean(dataTable.Rows[0]["SaleManIsRequire"]);
					}
					if ((dataTable.Rows[0]["PayTypeDefault"] != null) & (dataTable.Rows[0]["PayTypeDefault"] != DBNull.Value))
					{
						this.PayTypeDefault = Conversions.ToInteger(dataTable.Rows[0]["PayTypeDefault"]);
					}
					this.HasEntry = Conversions.ToBoolean(dataTable.Rows[0]["HasEntry"]);
					this.ShowPayForm = Conversions.ToBoolean(dataTable.Rows[0]["ShowPayForm"]);
					this.Pricing = (Pricing)Conversions.ToInteger(dataTable.Rows[0]["Pricing"]);
					this.Currency = (Currency)Conversions.ToInteger(dataTable.Rows[0]["Currency"]);
					this.SyncInv = Conversions.ToBoolean(dataTable.Rows[0]["SyncInv"]);
					this.SyncEntry = Conversions.ToBoolean(dataTable.Rows[0]["SyncEntry"]);
					this.LinkedReturn = Conversions.ToBoolean(dataTable.Rows[0]["LinkedReturn"]);
					this.UnLinkedReturn = Conversions.ToBoolean(dataTable.Rows[0]["UnLinkedReturn"]);
					this.ShowPayfrmReturn = Conversions.ToBoolean(dataTable.Rows[0]["ShowPayfrmReturn"]);
					this.CashCustRequire = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["CashCustRequire"]));
					if ((dataTable.Rows[0]["ProcessFlow"] != null) & (dataTable.Rows[0]["ProcessFlow"] != DBNull.Value))
					{
						this.ProcessFlow = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["ProcessFlow"]));
					}
					if ((dataTable.Rows[0]["PaymentStatus"] != null) & (dataTable.Rows[0]["PaymentStatus"] != DBNull.Value))
					{
						this.PaymentStatus = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["PaymentStatus"]));
					}
					if ((dataTable.Rows[0]["DetailedItemEntry"] != null) & (dataTable.Rows[0]["DetailedItemEntry"] != DBNull.Value))
					{
						this.DetailedItemEntry = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["DetailedItemEntry"]));
					}
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					ProjectData.ClearProjectError();
				}
			}
			sqlDataAdapter = new SqlDataAdapter("select IsInput from InvTypes where InvType=" + Conversions.ToString(Id) + " and ProcType=" + Conversions.ToString(ProcType), selectConnection);
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			if (dataTable.Rows.Count == 1 && dataTable.Rows[0]["IsInput"] != null)
			{
				this.InvertoryImpact = checked((int)Math.Round(Conversion.Val(Operators.ConcatenateObject("", dataTable.Rows[0]["IsInput"]))));
			}
			sqlDataAdapter = new SqlDataAdapter("select * from SettingCloseShift ", selectConnection);
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			if (dataTable.Rows.Count == 1)
			{
				try
				{
					this.IncInvInShift = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["SaleInv"]));
					if (Id == 3)
					{
						this.IncInvInShift = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["POS"]));
					}
				}
				catch (Exception projectError2)
				{
					ProjectData.SetProjectError(projectError2);
					ProjectData.ClearProjectError();
				}
			}
			else if (Id == 3)
			{
				this.IncInvInShift = true;
			}
			sqlDataAdapter = new SqlDataAdapter(("select isnull(printmakpay,0) as printmakpay from SettingPrint where Inv_id=" + Conversions.ToString(Id)) ?? "", selectConnection);
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			if (dataTable.Rows.Count > 0)
			{
				this.printmakpay = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.AsEnumerable().ElementAtOrDefault(0)["printmakpay"]));
			}
		}
	}
}