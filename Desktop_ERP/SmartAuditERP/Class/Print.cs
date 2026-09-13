using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using AuditorAPI.Models;
using AuditorAPI.Models.DGVmodels;
using AuditorAPI.Models.tags;
using DevExpress.XtraReports.UI;
using ETA_Invoice.Models;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using SmartAuditERP.Form_WPF;
using SmartAuditERP.Form_WPF;
using UtilitiesProj;

namespace SmartAuditERP
{

	public class Print
	{
		public List<InvPrinter> PrintersList;

		public int RptId { get; set; }

		public int PrintType { get; set; }

		public bool PrintFooter { get; set; }

		public bool PrintHeader { get; set; }

		public string defPrinter { get; set; }

		public string kitchenprinter { get; set; }

		public int PrintNo { get; set; }

		public string RptName { get; set; }

		public string RptUrl { get; set; }

		public string PrintNote { get; set; }

		public bool PrintComponentsItemsIndividually { get; set; }

		public int PrintItemType { get; set; }

		public Print(int RptId)
		{
			this.PrintType = 2;
			this.PrintFooter = false;
			this.PrintHeader = true;
			this.defPrinter = MainClass.ReportsPrinter;
			this.kitchenprinter = "";
			this.PrintNo = 1;
			this.RptName = "";
			this.RptUrl = MainClass.ReportsPath;
			this.PrintNote = "";
			this.PrintComponentsItemsIndividually = false;
			this.PrintItemType = 1;
			this.PrintersList = new List<InvPrinter>();
			this.RptId = RptId;
			SqlConnection selectConnection;
			selectConnection = MainClass.ConnObj();
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter("select * from SettingPrint where Inv_Id=" + Conversions.ToString(RptId), selectConnection);
			DataTable dataTable;
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			if (dataTable.Rows.Count != 1)
			{
				return;
			}
			checked
			{
				try
				{
					this.PrintType = Conversions.ToInteger(dataTable.Rows[0]["printType"]);
					this.PrintFooter = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["PrintFooter"]));
					this.PrintHeader = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["PrintHeader"]));
					if (!Information.IsDBNull(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["PrintComponentsItemsIndividually"])))
					{
						this.PrintComponentsItemsIndividually = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["PrintComponentsItemsIndividually"]));
					}
					this.defPrinter = Conversions.ToString(dataTable.Rows[0]["CasherPrinter"]);
					this.kitchenprinter = Conversions.ToString(dataTable.Rows[0]["kitchenprinter"]);
					this.PrintNo = Conversions.ToInteger(dataTable.Rows[0]["printNo"]);
					this.RptName = Conversions.ToString(dataTable.Rows[0]["RptName"]);
					try
					{
						this.RptUrl = Path.GetDirectoryName(Conversions.ToString(dataTable.Rows[0]["RptUrl"]));
					}
					catch (Exception projectError)
					{
						ProjectData.SetProjectError(projectError);
						ProjectData.ClearProjectError();
					}
					if ((Operators.CompareString(this.RptUrl, "", TextCompare: false) == 0) | !Directory.Exists(this.RptUrl))
					{
						this.RptUrl = MainClass.ReportsPath;
					}
					this.PrintNote = Conversions.ToString(dataTable.Rows[0]["note"]);
					try
					{
						this.PrintItemType = Conversions.ToInteger(dataTable.Rows[0]["PrintItemType"]);
					}
					catch (Exception projectError2)
					{
						ProjectData.SetProjectError(projectError2);
						ProjectData.ClearProjectError();
					}
					SqlDataAdapter sqlDataAdapter2;
					sqlDataAdapter2 = new SqlDataAdapter("select PrintName,Printer,RptName from PrinterSettings where Inv_Id=" + Conversions.ToString(RptId), selectConnection);
					DataTable dataTable2;
					dataTable2 = new DataTable();
					sqlDataAdapter2.Fill(dataTable2);
					if (dataTable2.Rows.Count > 0)
					{
						int num;
						num = dataTable2.Rows.Count - 1;
						for (int i = 0; i <= num; i++)
						{
							InvPrinter item;
							item = new InvPrinter
							{
								PrintName = Conversions.ToString(dataTable2.Rows[i]["PrintName"]),
								RptName = Conversions.ToString(dataTable2.Rows[i]["RptName"]),
								Printer = Conversions.ToString(dataTable2.Rows[i]["Printer"])
							};
							this.PrintersList.Add(item);
						}
					}
				}
				catch (Exception projectError3)
				{
					ProjectData.SetProjectError(projectError3);
					ProjectData.ClearProjectError();
				}
			}
		}

		public void Printing(int type, DataSet ds, string RptUrl, string RptName, string defPrinter, string kitchenprinter, int PrintNo)
		{
			try
			{
				if (this.PrintersList.Count > 0)
				{
                    var frmPrinter2 = new frmPrinter();
					frmPrinter2.PrintersList = PrintersList;
					frmPrinter2.ShowDialog();
					if (Operators.CompareString(frmPrinter2.SelectedPrinter, "", TextCompare: false) != 0)
					{
						defPrinter = frmPrinter2.SelectedPrinter;
						RptName = frmPrinter2.SelectedRptName;
					}
				}
				XtraReport xtraReport;
				xtraReport = XtraReport.FromFile(Path.Combine(RptUrl, RptName));
				xtraReport.DataSource = ds;
				if (this.PrintHeader)
				{
					XtraReport xtraReport2;
					xtraReport2 = XtraReport.FromFile(RptUrl + "/header.repx");
					xtraReport2.DataSource = Common.FoundationInfoDT;
					XRSubreport xRSubreport;
					xRSubreport = (XRSubreport)xtraReport.FindControl("headerRpt", ignoreCase: true);
					if (xRSubreport != null)
					{
						xRSubreport.ReportSource = xtraReport2;
					}
				}
				if (this.PrintFooter)
				{
					XtraReport xtraReport3;
					xtraReport3 = XtraReport.FromFile(RptUrl + "/footer.repx");
					xtraReport3.DataSource = Common.FoundationInfoDT;
					XRSubreport xRSubreport2;
					xRSubreport2 = (XRSubreport)xtraReport.FindControl("footerRpt", ignoreCase: true);
					if (xRSubreport2 != null)
					{
						xRSubreport2.ReportSource = xtraReport3;
					}
				}
				if (type == 3 && Operators.CompareString(xtraReport.PrinterName, "", TextCompare: false) != 0)
				{
					defPrinter = xtraReport.PrinterName;
				}
				xtraReport.PrinterName = defPrinter;
				if (type == 1 && PrintNo > 0)
				{
					xtraReport.Print();
					for (int i = 2; i <= PrintNo; i = checked(i + 1))
					{
						xtraReport.Print();
					}
					if (Operators.CompareString(kitchenprinter, "", TextCompare: false) != 0)
					{
						this.Printing(type, ds, RptUrl, "rptPOS2.repx", kitchenprinter, "", 1);
					}
				}
				else if (type == 2)
				{
					xtraReport.ShowPreviewDialog();
				}
				else
				{
					xtraReport.Print();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public void ShareFileViaWhatsAppWeb(string filePath)
		{
			try
			{
				Process.Start(new ProcessStartInfo("cmd", string.Format("/c start {0}", "https://wa.me/" + "+966559494177" + "?text=" + Uri.EscapeDataString("Here is your report: " + "https://www.example.com/src/Inv.pdf")))
				{
					CreateNoWindow = true
				});
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Console.WriteLine("Error: " + ex.Message);
				ProjectData.ClearProjectError();
			}
		}

		public DataSet BindComponentToData(Invoice Inv)
		{
			InvoiceObj invoiceObj;
			invoiceObj = new InvoiceObj((int)Inv.InvoiceType, Inv.ProcType);
			string payType;
			payType = "Cash نقدية ";
			if (Inv.PayType == -1)
			{
				payType = "Credit آجل";
			}
			else if (Inv.PayType == 2)
			{
				payType = "Bank شبكة";
			}
			else if (Inv.PayType == 4)
			{
				payType = "متعدد";
			}
			else if (Inv.PayType == 4)
			{
				payType = "ضيافة";
			}
			if ((Inv.PayType == 3) | ((Inv.PayType == 2) & (Inv.Bank > 0)))
			{
				payType = new Bank(Inv.Bank).Name;
			}
			string orderType;
			orderType = " ";
			if (Inv.OrderType == 0)
			{
				orderType = " Dine-in محلي";
			}
			else if (Inv.OrderType == 1)
			{
				orderType = "Take awayسفري";
			}
			else if (Inv.OrderType == 2)
			{
				orderType = "Family عوائل";
			}
			else if (Inv.OrderType == 3)
			{
				orderType = "Table طاولة";
			}
			else if (Inv.OrderType == 4)
			{
				orderType = "Drive thru سيارة";
			}
			if (Inv.InvoiceType == InvoiceType.BeginingInventory)
			{
				payType = "";
			}
			string saleman;
			saleman = "";
			if (Inv.Saleman > -1)
			{
				saleman = Common.GetSalesManName(Inv.Saleman);
			}
			Customer customer;
			customer = new Customer(Inv.Customer);
			short taxType;
			taxType = 1;
			if (Operators.CompareString(customer.VATno, "", TextCompare: false) != 0)
			{
				taxType = 2;
			}
			string invoiceType;
			invoiceType = InvoiceOper.GetInvoiceType((int)Inv.InvoiceType, Inv.ProcType, 0, taxType);
			List<InvoiceData> list;
			list = new List<InvoiceData>();
			new List<InvoiceData>();
			double num;
			num = 0.0;
			checked
			{
				foreach (InvoiceItem invoiceItem in Inv.InvoiceItems)
				{
					InvoiceData invoiceData;
					invoiceData = new InvoiceData();
					if (invoiceItem.ProductId != 0)
					{
						invoiceData.ItemNo = invoiceItem.ItemCode;
						invoiceData.ItemID = Conversions.ToString(invoiceItem.ItemId);
						string nameEn;
						nameEn = "";
						string nameAr;
						nameAr = "";
						string CategoryAr;
						CategoryAr = "";
						short num2;
						num2 = 1;
						string CategoryEn;
						CategoryEn = "";
						string CategoryCode;
						CategoryCode = "";
						string ItemGroupPrinter;
						ItemGroupPrinter = "";
						Common.GetItemNames(ref nameAr, ref nameEn, invoiceItem.ItemId);
						if (invoiceItem.ItemProperty == 7)
						{
							invoiceData.ItemName = invoiceItem.Description;
							invoiceData.ItemNameEn = invoiceItem.Description;
						}
						else
						{
							invoiceData.ItemName = nameAr;
							invoiceData.ItemNameEn = nameEn;
						}
						int CategoryID;
						CategoryID = num2;
						Common.GetItemCategory(ref CategoryAr, ref CategoryEn, ref CategoryCode, ref CategoryID, ref ItemGroupPrinter, invoiceItem.ItemId);
						_ = (short)CategoryID;
						invoiceData.Printer = ItemGroupPrinter;
						invoiceData.ItemGroup = CategoryAr;
						invoiceData.ItemGroupEn = CategoryEn;
						invoiceData.ItemGroupCode = CategoryCode;
						invoiceData.Description = invoiceItem.Description;
						invoiceData.ItemNotes = invoiceItem.ItemNotes;
						invoiceData.Quantity = invoiceItem.ItemQuantity.ToString(invoiceObj.DigitsNo);
						invoiceData.UnitEquality = invoiceItem.UnitEquality.ToString(invoiceObj.DigitsNo);
						invoiceData.EssitialQnty = invoiceItem.ItemPrimaryQnty.ToString(invoiceObj.DigitsNo);
						invoiceData.Unit = invoiceItem.UnitName;
						invoiceData.Total = invoiceItem.ItemTotalPrice.ToString(invoiceObj.DigitsNo);
						invoiceData.ItemTotalWithoutVAT = invoiceItem.ItemTotalPrice.ToString(invoiceObj.DigitsNo);
						invoiceData.Barcode = invoiceItem.ItemBarcode;
						invoiceData.Price = invoiceItem.ItemPrice.ToString(invoiceObj.DigitsNo);
						invoiceData.SubDiscount = invoiceItem.ItemDiscount.ToString(invoiceObj.DigitsNo);
						invoiceData.DiscPerc = invoiceItem.ItemDiscountPerc.ToString(invoiceObj.DigitsNo);
						invoiceData.sum = invoiceItem.ItemSumPrice.ToString(invoiceObj.DigitsNo);
						invoiceData.ItemExpireDate = invoiceItem.ItemExpireDate.ToString("yyyy-MM-dd");
						invoiceData.AvegCost = invoiceItem.ItemCost.ToString(invoiceObj.DigitsNo);
						invoiceData.Tax = invoiceItem.ItemVat.ToString(invoiceObj.DigitsNo);
						invoiceData.ItemPriceWithoutVAT = (invoiceItem.ItemPrice - invoiceItem.ItemVat / invoiceItem.ItemQuantity).ToString(invoiceObj.DigitsNo);
						invoiceData.WithholdingTax = invoiceItem.WithholdingTax.ToString(invoiceObj.DigitsNo);
						invoiceData.WithholdingTaxPerc = invoiceItem.WithholdingTaxPerc.ToString(invoiceObj.DigitsNo);
						invoiceData.TaxPerc = invoiceItem.ItemVatPerc.ToString(invoiceObj.DigitsNo);
						invoiceData.ItemPriceDiscount = invoiceItem.ItemPriceDiscount.ToString(invoiceObj.DigitsNo);
						invoiceData.ItemPriceAfterDiscount = invoiceItem.ItemPriceAfterDiscount.ToString(invoiceObj.DigitsNo);
						invoiceData.NetTable = invoiceItem.ItemNetPrice.ToString(invoiceObj.DigitsNo);
						invoiceData.CurrentQty = invoiceItem.ValiableInvertory.ToString(invoiceObj.DigitsNo);
						invoiceData.Store = invoiceItem.InvertoryName;
						invoiceData.PrintNote = this.PrintNote;
						invoiceData.CloudID = Inv.CloudID;
						invoiceData.CashCustomerMobile = Inv.CashCustomerMobile;
						invoiceData.CashCustomerName = Inv.CashCustomerName;
						invoiceData.currency = Inv.Currency.ToString();
						invoiceData.BranchName = Common.GetBranchName(Inv.Branch);
						new InvoiceOper();
						double num3;
						num3 = Inv.Net - Inv.VAT;
						double num4;
						num4 = Conversions.ToDouble((num3 * (invoiceObj.AdditionalTax / 100.0)).ToString(invoiceObj.DigitsNo));
						Conversions.ToDouble(((num3 + num4) * (invoiceObj.VAT / 100.0)).ToString(invoiceObj.DigitsNo));
						new decimal(Conversions.ToDouble(invoiceData.Net) / (1.0 + invoiceObj.VAT + num4));
						invoiceData.SumPrice = Inv.SumPrice.ToString(invoiceObj.DigitsNo);
						invoiceData.TotalWithoutVAT = num3.ToString(invoiceObj.DigitsNo);
						invoiceData.AdditiontalTax = num4.ToString(invoiceObj.DigitsNo);
						invoiceData.VAT = Inv.VAT.ToString(invoiceObj.DigitsNo);
						invoiceData.Net = Inv.Net.ToString(invoiceObj.DigitsNo);
						invoiceData.Discount = Inv.TotDiscount.ToString(invoiceObj.DigitsNo);
						invoiceData.Additions = Inv.Additions.ToString(invoiceObj.DigitsNo);
						invoiceData.PayNetwork = Inv.PayATM.ToString(invoiceObj.DigitsNo);
						invoiceData.TotalWithholdingTax = Inv.TotalWithholdingTax.ToString(invoiceObj.DigitsNo);
						invoiceData.Insurance = Inv.Insurance.ToString(invoiceObj.DigitsNo);
						invoiceData.Paycash = (Inv.Paid - Inv.PayATM).ToString(invoiceObj.DigitsNo);
						invoiceData.Remainder = Inv.Remainder.ToString(invoiceObj.DigitsNo);
						num += invoiceItem.ItemPrimaryQnty;
						invoiceData.TotalQty += num.ToString(invoiceObj.DigitsNo);
						invoiceData.ReffNo = Inv.ReffNo;
						invoiceData.RefDate = Inv.RefDate.ToString("yyyy-MM-dd");
						invoiceData.PayType = payType;
						invoiceData.InvGlobalID = Inv.InvGlobalID;
						invoiceData.ID = Conversions.ToString(Inv.InvoiceNo);
						if (Inv.InvCombinedId != null)
						{
							invoiceData.InvoiceNo = Inv.InvCombinedId;
						}
						else
						{
							invoiceData.InvoiceNo = Conversions.ToString(Inv.InvoiceNo);
						}
						invoiceData.InvoiceType = invoiceType;
						invoiceData.OrderNo = Conversions.ToString(Inv.OrderNo);
						invoiceData.OrderType = orderType;
						invoiceData.InvTime = Inv.InvDate.ToShortTimeString();
						invoiceData.InvDate = Inv.InvDate.ToString("yyyy-MM-dd");
						invoiceData.User = Common.GetEmpName(Inv.User);
						invoiceData.EmpName = "";
						if (Inv.User > 0)
						{
							invoiceData.EmpName = Common.GetEmployeeName(Inv.User);
						}
						invoiceData.InvNote = Inv.InvNote;
						invoiceData.Customer = customer.Name;
						invoiceData.CustMobile = customer.Mobile;
						invoiceData.Saleman = saleman;
						invoiceData.CustVATno = customer.VATno;
						invoiceData.CustAccCode = customer.AccCode;
						string CityName;
						CityName = "";
						string CountryName;
						CountryName = "";
						string RegionName;
						RegionName = "";
						Common.GetCityName(ref RegionName, ref CountryName, ref CityName, customer.Region, customer.Country, customer.City);
						invoiceData.CustCity = CityName;
						invoiceData.Custcountry = CountryName;
						invoiceData.CustArea = RegionName;
						invoiceData.CustNatianalID = customer.NatianalID;
						invoiceData.CustNote = customer.Note;
						invoiceData.CustPlotIdentification = customer.PlotIdentification;
						invoiceData.CustBuildingNumber = customer.BuildingNumber;
						invoiceData.CustStreetName = customer.StreetName;
						invoiceData.CustAdditionalStreetName = customer.AdditionalStreetName;
						invoiceData.CustDistrict = customer.District;
						invoiceData.CustPostalZone = customer.PostalZone;
						invoiceData.custCrNo = customer.CrNo;
						string address;
						address = "";
						string telePhone;
						telePhone = "";
						string mobile;
						mobile = "";
						string foundation;
						foundation = "";
						string field;
						field = "";
						string vatNo;
						vatNo = "";
						if (Common.FoundationInfoDT.Rows.Count > 0)
						{
							address = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["Address"]));
							telePhone = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["Tel"]));
							mobile = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["Mobile"]));
							foundation = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["nameA"]));
							field = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["FieldA"]));
							vatNo = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["tax_no"]));
							invoiceData.nameE = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["nameE"]));
							invoiceData.FieldE = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["FieldE"]));
							invoiceData.bsn_no = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["bsn_no"]));
							invoiceData.country = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["country"]));
							invoiceData.city = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["city"]));
							invoiceData.Email = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["Email"]));
							invoiceData.website = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["website"]));
							invoiceData.Area = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["Area"]));
							invoiceData.PlotIdentification = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["PlotIdentification"]));
							invoiceData.BuildingNumber = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["BuildingNumber"]));
							invoiceData.StreetName = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["StreetName"]));
							invoiceData.AdditionalStreetName = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["AdditionalStreetName"]));
							invoiceData.District = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["District"]));
							invoiceData.PostalZone = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["PostalZone"]));
						}
						invoiceData.Address = address;
						invoiceData.Mobile = mobile;
						invoiceData.TelePhone = telePhone;
						invoiceData.VatNo = vatNo;
						invoiceData.Foundation = foundation;
						invoiceData.Field = field;
						invoiceData.Logo = "";
						invoiceData.Header = "";
						invoiceData.footer = "";
						invoiceData.Stamp = "";
						list.Add(invoiceData);
					}
				}
				DataSet dataSet;
				dataSet = new DataSet("Data");
				new DataTable("Invoices");
				DataTable table;
				table = global::UtilitiesProj.Common.ToDataTable(list);
				dataSet.Tables.Add(table);
				list.Clear();
				return dataSet;
			}
		}

		public DataSet BindToData(Invoice Inv)
		{
			InvoiceObj invoiceObj;
			invoiceObj = new InvoiceObj((int)Inv.InvoiceType, Inv.ProcType);
			string payType;
			payType = "Cash نقدية ";
			if (Inv.PayType == -1)
			{
				payType = "Credit آجل";
			}
			else if (Inv.PayType == 2)
			{
				payType = "Bank شبكة";
			}
			else if (Inv.PayType == 3)
			{
				payType = "متعدد";
			}
			else if (Inv.PayType == 4)
			{
				payType = "ضيافة";
			}
			if ((Inv.PayType == 3) | ((Inv.PayType == 2) & (Inv.Bank > 0)))
			{
				payType = new Bank(Inv.Bank).Name;
			}
			string orderType;
			orderType = " ";
			if (Inv.OrderType == 0)
			{
				orderType = " Dine-in محلي";
			}
			else if (Inv.OrderType == 1)
			{
				orderType = "Take awayسفري";
			}
			else if (Inv.OrderType == 2)
			{
				orderType = "Family عوائل";
			}
			else if (Inv.OrderType == 3)
			{
				orderType = "Table طاولة";
			}
			else if (Inv.OrderType == 4)
			{
				orderType = "Drive thru سيارة";
			}
			if (Inv.InvoiceType == InvoiceType.BeginingInventory)
			{
				payType = "";
			}
			string saleman;
			saleman = "";
			if (Inv.Saleman > -1)
			{
				saleman = Common.GetSalesManName(Inv.Saleman);
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
			string invoiceType;
			invoiceType = InvoiceOper.GetInvoiceType((int)Inv.InvoiceType, Inv.ProcType, 0, taxType);
			string invoiceTypeEn;
			invoiceTypeEn = InvoiceOper.GetInvoiceTypeEn((int)Inv.InvoiceType, Inv.ProcType, 0, taxType);
			string invoiceTypeAr;
			invoiceTypeAr = InvoiceOper.GetInvoiceTypeAr((int)Inv.InvoiceType, Inv.ProcType, 0, taxType);
			List<InvoiceData> list;
			list = new List<InvoiceData>();
			double num;
			num = 0.0;
			checked
			{
				foreach (InvoiceItem invoiceItem in Inv.InvoiceItems)
				{
					InvoiceData invoiceData;
					invoiceData = new InvoiceData();
					if (invoiceItem.ProductId > 0)
					{
						continue;
					}
					invoiceData.ItemNo = invoiceItem.ItemCode;
					invoiceData.ItemID = Conversions.ToString(invoiceItem.ItemId);
					string nameEn;
					nameEn = "";
					string nameAr;
					nameAr = "";
					string CategoryAr;
					CategoryAr = "";
					short num2;
					num2 = 1;
					string CategoryEn;
					CategoryEn = "";
					string CategoryCode;
					CategoryCode = "";
					string ItemGroupPrinter;
					ItemGroupPrinter = "";
					Common.GetItemNames(ref nameAr, ref nameEn, invoiceItem.ItemId);
					if (invoiceItem.ItemProperty == 7)
					{
						invoiceData.ItemName = invoiceItem.Description;
						invoiceData.ItemNameEn = invoiceItem.Description;
					}
					else
					{
						invoiceData.ItemName = nameAr;
						invoiceData.ItemNameEn = nameEn;
					}
					int CategoryID;
					CategoryID = num2;
					Common.GetItemCategory(ref CategoryAr, ref CategoryEn, ref CategoryCode, ref CategoryID, ref ItemGroupPrinter, invoiceItem.ItemId);
					_ = (short)CategoryID;
					invoiceData.Printer = ItemGroupPrinter;
					invoiceData.ItemGroup = CategoryAr;
					invoiceData.ItemGroupEn = CategoryEn;
					invoiceData.ItemGroupCode = CategoryCode;
					invoiceData.Description = invoiceItem.Description;
					invoiceData.ItemNotes = invoiceItem.ItemNotes;
					invoiceData.Quantity = invoiceItem.ItemQuantity.ToString(invoiceObj.DigitsNo);
					invoiceData.UnitEquality = invoiceItem.UnitEquality.ToString(invoiceObj.DigitsNo);
					invoiceData.EssitialQnty = invoiceItem.ItemPrimaryQnty.ToString(invoiceObj.DigitsNo);
					invoiceData.Unit = invoiceItem.UnitName;
					invoiceData.Total = invoiceItem.ItemTotalPrice.ToString(invoiceObj.DigitsNo);
					invoiceData.ItemTotalWithoutVAT = invoiceItem.ItemTotalPrice.ToString(invoiceObj.DigitsNo);
					invoiceData.Barcode = invoiceItem.ItemBarcode;
					invoiceData.Price = invoiceItem.ItemPrice.ToString(invoiceObj.DigitsNo);
					invoiceData.SubDiscount = invoiceItem.ItemDiscount.ToString(invoiceObj.DigitsNo);
					invoiceData.DiscPerc = invoiceItem.ItemDiscountPerc.ToString(invoiceObj.DigitsNo);
					invoiceData.sum = invoiceItem.ItemSumPrice.ToString(invoiceObj.DigitsNo);
					invoiceData.ItemExpireDate = invoiceItem.ItemExpireDate.ToString("yyyy-MM-dd");
					invoiceData.AvegCost = invoiceItem.ItemCost.ToString(invoiceObj.DigitsNo);
					invoiceData.Tax = invoiceItem.ItemVat.ToString(invoiceObj.DigitsNo);
					invoiceData.ItemPriceWithoutVAT = (invoiceItem.ItemPrice - invoiceItem.ItemVat / invoiceItem.ItemQuantity).ToString(invoiceObj.DigitsNo);
					invoiceData.WithholdingTax = invoiceItem.WithholdingTax.ToString(invoiceObj.DigitsNo);
					invoiceData.WithholdingTaxPerc = invoiceItem.WithholdingTaxPerc.ToString(invoiceObj.DigitsNo);
					invoiceData.TaxPerc = invoiceItem.ItemVatPerc.ToString(invoiceObj.DigitsNo);
					invoiceData.ItemPriceDiscount = invoiceItem.ItemPriceDiscount.ToString(invoiceObj.DigitsNo);
					invoiceData.ItemPriceAfterDiscount = invoiceItem.ItemPriceAfterDiscount.ToString(invoiceObj.DigitsNo);
					invoiceData.NetTable = invoiceItem.ItemNetPrice.ToString(invoiceObj.DigitsNo);
					invoiceData.CurrentQty = invoiceItem.ValiableInvertory.ToString(invoiceObj.DigitsNo);
					invoiceData.Store = invoiceItem.InvertoryName;
					invoiceData.ItemCostCenter = Common.GetCostCenterName(invoiceItem.ItemCostCenter);
					invoiceData.PrintNote = this.PrintNote;
					invoiceData.ItemAdditionalTaxPerc = Conversions.ToString(invoiceItem.ItemAdditionalTaxPerc);
					invoiceData.ItemAdditionalTax = Conversions.ToString(invoiceItem.ItemAdditionalTax);
					foreach (InvoiceItemDetail invoiceItemDetail in invoiceItem.InvoiceItemDetails)
					{
						invoiceData.ItemSerialNo = invoiceItemDetail.ItemSerialNo;
					}
					invoiceData.CloudID = Inv.CloudID;
					invoiceData.CashCustomerMobile = Inv.CashCustomerMobile;
					invoiceData.CashCustomerName = Inv.CashCustomerName;
					if (Inv.InvoiceType == InvoiceType.HoldInv)
					{
						invoiceData.contract_total_value = Conversions.ToString(Inv.contract_total_value);
						invoiceData.original_work_amount = Conversions.ToString(Inv.original_work_amount);
						invoiceData.special_discount = Conversions.ToString(Inv.special_discount);
						invoiceData.amount_after_discount = Conversions.ToString(Inv.amount_after_discount);
						invoiceData.vat_amount = Conversions.ToString(Inv.vat_amount);
						invoiceData.total_with_vat = Conversions.ToString(Inv.total_with_vat);
						invoiceData.work_guarantee = Conversions.ToString(Inv.work_guarantee);
						invoiceData.net_due_this_payment = Conversions.ToString(Inv.net_due_this_payment);
						invoiceData.previously_paid_amount = Conversions.ToString(Inv.previously_paid_amount);
						invoiceData.remaining_contract_balance = Conversions.ToString(Inv.remaining_contract_balance);
					}
					string description;
					description = invoiceItem.Description;
					if (Operators.CompareString(description, "", TextCompare: false) != 0)
					{
						try
						{
							string[] array;
							array = description.Split(Conversions.ToChar(Environment.NewLine));
							invoiceData.ItemHeight = array[0].Substring(array[0].LastIndexOf(":") + 2);
							invoiceData.ItemWidth = array[1].Substring(array[0].LastIndexOf(":") + 2);
							invoiceData.Qty1 = array[2].Substring(array[0].LastIndexOf(":") + 2);
						}
						catch (Exception projectError)
						{
							ProjectData.SetProjectError(projectError);
							ProjectData.ClearProjectError();
						}
					}
					invoiceData.currency = Inv.Currency.ToString();
					invoiceData.BranchName = Common.GetBranchName(Inv.Branch);
					foreach (Glass glass in invoiceItem.Glasses)
					{
						if (Operators.CompareString(glass.orientation, "R", TextCompare: false) == 0)
						{
							invoiceData.ReSPH = glass.SPH;
							invoiceData.ReCYL = glass.CYL;
							invoiceData.ReAX = glass.AX;
							invoiceData.ReADD = glass.ADD;
							invoiceData.ReIPD = glass.IPD;
						}
						else if (Operators.CompareString(glass.orientation, "L", TextCompare: false) == 0)
						{
							invoiceData.LeSPH = glass.SPH;
							invoiceData.LeCYL = glass.CYL;
							invoiceData.LeAX = glass.AX;
							invoiceData.LeADD = glass.ADD;
							invoiceData.LeIPD = glass.IPD;
						}
					}
					InvoiceOper invoiceOper;
					invoiceOper = new InvoiceOper();
					_ = Inv.Net - Inv.VAT;
					double num3;
					num3 = Conversions.ToDouble(Inv.ExtraVAT.ToString(invoiceObj.DigitsNo));
					Conversions.ToDouble(Inv.VAT.ToString(invoiceObj.DigitsNo));
					invoiceData.SumPrice = Inv.SumPrice.ToString(invoiceObj.DigitsNo);
					invoiceData.TotalWithoutVAT = Inv.Total.ToString(invoiceObj.DigitsNo);
					invoiceData.AdditiontalTax = num3.ToString(invoiceObj.DigitsNo);
					invoiceData.VAT = Inv.VAT.ToString(invoiceObj.DigitsNo);
					invoiceData.Net = Inv.Net.ToString(invoiceObj.DigitsNo);
					if (num3 > 0.0)
					{
						new decimal(100.0 / (1.0 + invoiceObj.VAT + 1.0));
					}
					else
					{
						new decimal(Math.Round(Conversions.ToDouble(invoiceData.Net) * Conversions.ToDouble(invoiceData.VAT) / (Conversions.ToDouble(invoiceData.VAT) + 100.0), 2));
					}
					invoiceData.ArabicLetter = InvoiceOper.ToArabicLetter(Conversions.ToDouble(invoiceData.Net), Inv.Currency);
					invoiceData.ToEnWords = Conversions.ToString(invoiceOper.SpellNumberEDP(invoiceData.Net, Inv.Currency));
					invoiceData.Discount = Inv.TotDiscount.ToString(invoiceObj.DigitsNo);
					invoiceData.Additions = Inv.Additions.ToString(invoiceObj.DigitsNo);
					invoiceData.PayNetwork = Inv.PayATM.ToString(invoiceObj.DigitsNo);
					invoiceData.TotalWithholdingTax = Inv.TotalWithholdingTax.ToString(invoiceObj.DigitsNo);
					invoiceData.Insurance = Inv.Insurance.ToString(invoiceObj.DigitsNo);
					invoiceData.Paycash = (Inv.Paid - Inv.PayATM).ToString(invoiceObj.DigitsNo);
					invoiceData.Remainder = Inv.Remainder.ToString(invoiceObj.DigitsNo);
					num += invoiceItem.ItemPrimaryQnty;
					invoiceData.TotalQty += num.ToString(invoiceObj.DigitsNo);
					invoiceData.ReffNo = Inv.ReffNo;
					invoiceData.RefDate = Inv.RefDate.ToString("yyyy-MM-dd");
					invoiceData.PayType = payType;
					invoiceData.InvGlobalID = Inv.InvGlobalID;
					invoiceData.ID = Conversions.ToString(Inv.InvoiceNo);
					if (Inv.InvCombinedId != null)
					{
						invoiceData.InvoiceNo = Inv.InvCombinedId;
					}
					else
					{
						invoiceData.InvoiceNo = Conversions.ToString(Inv.InvoiceNo);
					}
					if (Conversions.ToDouble(Inv.InvCCcode) != -1.0)
					{
						invoiceData.costCenter = Common.GetCostCenterName(Inv.InvCCcode);
					}
					invoiceData.InvoiceType = invoiceType;
					invoiceData.InvoiceTypeEn = invoiceTypeEn;
					invoiceData.InvoiceTypeAr = invoiceTypeAr;
					invoiceData.OrderNo = Conversions.ToString(Inv.OrderNo);
					invoiceData.OrderType = orderType;
					invoiceData.InvTime = Inv.InvDate.ToShortTimeString();
					invoiceData.InvDate = Inv.InvDate.ToString("yyyy-MM-dd");
					invoiceData.User = Common.GetEmpName(Inv.User);
					invoiceData.EmpName = "";
					if (Inv.User > 0)
					{
						invoiceData.EmpName = Common.GetEmployeeName(Inv.User);
					}
					invoiceData.InvNote = Inv.InvNote;
					invoiceData.tableNo = Inv.TableNo;
					invoiceData.Customer = customer.Name;
					invoiceData.CustMobile = customer.Mobile;
					invoiceData.Saleman = saleman;
					invoiceData.CustVATno = customer.VATno;
					invoiceData.CustAccCode = customer.AccCode;
					invoiceData.BalancePreviews = Conversions.ToString(Inv.BalancePreviews);
					string CityName;
					CityName = "";
					string CountryName;
					CountryName = "";
					string RegionName;
					RegionName = "";
					Common.GetCityName(ref RegionName, ref CountryName, ref CityName, customer.Region, customer.Country, customer.City);
					invoiceData.CustCity = customer.city2;
					invoiceData.Custcountry = CountryName;
					invoiceData.CustArea = customer.area2;
					invoiceData.CustNatianalID = customer.NatianalID;
					invoiceData.CustNote = customer.Note;
					invoiceData.CustPlotIdentification = customer.PlotIdentification;
					invoiceData.CustBuildingNumber = customer.BuildingNumber;
					invoiceData.CustStreetName = customer.StreetName;
					invoiceData.CustAdditionalStreetName = customer.AdditionalStreetName;
					invoiceData.CustDistrict = customer.District;
					invoiceData.CustPostalZone = customer.PostalZone;
					invoiceData.custCrNo = customer.CrNo;
					string address;
					address = "";
					string telePhone;
					telePhone = "";
					string mobile;
					mobile = "";
					string text;
					text = "";
					string field;
					field = "";
					string vatNo;
					vatNo = "";
					if (Common.FoundationInfoDT.Rows.Count > 0)
					{
						address = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["Address"]));
						telePhone = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["Tel"]));
						mobile = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["Mobile"]));
						text = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["nameA"]));
						field = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["FieldA"]));
						vatNo = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["tax_no"]));
						invoiceData.nameE = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["nameE"]));
						invoiceData.FieldE = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["FieldE"]));
						invoiceData.bsn_no = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["bsn_no"]));
						invoiceData.country = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["country"]));
						invoiceData.city = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["city"]));
						invoiceData.Email = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["Email"]));
						invoiceData.website = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["website"]));
						invoiceData.Area = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["Area"]));
						invoiceData.PlotIdentification = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["PlotIdentification"]));
						invoiceData.BuildingNumber = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["BuildingNumber"]));
						invoiceData.StreetName = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["StreetName"]));
						invoiceData.AdditionalStreetName = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["AdditionalStreetName"]));
						invoiceData.District = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["District"]));
						invoiceData.PostalZone = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["PostalZone"]));
					}
					invoiceData.Address = address;
					invoiceData.Mobile = mobile;
					invoiceData.TelePhone = telePhone;
					invoiceData.VatNo = vatNo;
					invoiceData.Foundation = text;
					invoiceData.Field = field;
					invoiceData.Logo = "";
					invoiceData.Header = "";
					invoiceData.footer = "";
					invoiceData.Stamp = "";
					invoiceData.safeFrom = Inv.safeFrom;
					invoiceData.safeTo = Inv.safeTo;
					if (((Inv.InvoiceType == InvoiceType.Sale) | (Inv.InvoiceType == InvoiceType.POS) | (Inv.InvoiceType == (InvoiceType)21) | (Inv.InvoiceType == InvoiceType.HoldInv)) & ((Inv.ProcType == 1) | (Inv.ProcType == 2)))
					{
						if (EtaSetting.Active)
						{
							if (EtaSetting.Receipt)
							{
								invoiceData.QRCode = "http://invoicing.eta.gov.eg/receipts/search/" + Inv.UUID.Trim() + "/share/" + Inv.InvDate.ToString("yyyy-MM-ddTHH:mm:ssZ") + "#Total:" + Conversions.ToString(Inv.Net) + ",IssuerRIN:" + Common.FoundationInfoDT.Rows[0]["bsn_no"].ToString().Trim();
							}
							else
							{
								invoiceData.QRCode = "";
							}
						}
						else if (!MainSetting.ZatcaIntegerationActive)
						{
							QR qR;
							qR = new QR();
							qR.SellerName = text;
							qR.TaxAmount = Conversions.ToString(Convert.ToDecimal(Inv.VAT.ToString(invoiceObj.DigitsNo)));
							qR.TaxNumber = invoiceData.VatNo;
							qR.InvoiceTotal = Conversions.ToString(Convert.ToDecimal(Inv.Net.ToString(invoiceObj.DigitsNo)));
							qR.InvoiceTimeStamp = Inv.InvDate.ToString("yyyy-MM-dd'T'HH:mm");
							invoiceData.QRCode = Generator.GenerateBase64(qR);
						}
						else if (string.IsNullOrEmpty(Inv.QRCode))
						{
							QR qR2;
							qR2 = new QR();
							qR2.SellerName = text;
							qR2.TaxAmount = Conversions.ToString(Convert.ToDecimal(Inv.VAT.ToString(invoiceObj.DigitsNo)));
							qR2.TaxNumber = invoiceData.VatNo;
							qR2.InvoiceTotal = Conversions.ToString(Convert.ToDecimal(Inv.Net.ToString(invoiceObj.DigitsNo)));
							qR2.InvoiceTimeStamp = Inv.InvDate.ToString("yyyy-MM-dd'T'HH:mm");
							invoiceData.QRCode = Generator.GenerateBase64(qR2);
						}
						else
						{
							invoiceData.QRCode = Inv.QRCode.Trim();
						}
					}
					else
					{
						invoiceData.QRCode = "";
					}
					try
					{
						SqlConnection sqlConnection;
						sqlConnection = new SqlConnection(MainClass.connstr);
						if (sqlConnection.State == ConnectionState.Open)
						{
							sqlConnection.Close();
						}
						sqlConnection.Open();
						SqlCommand sqlCommand;
						sqlCommand = new SqlCommand("select * from CustomerMeasurements where CustId=@custid", sqlConnection);
						sqlCommand.Parameters.AddWithValue("@Custid", Inv.Customer);
						SqlDataReader sqlDataReader;
						sqlDataReader = sqlCommand.ExecuteReader();
						sqlDataReader.Read();
						if (sqlDataReader.HasRows)
						{
							invoiceData.Length = Conversions.ToString(sqlDataReader["Length"]);
							invoiceData.chest = Conversions.ToString(sqlDataReader["Chest"]);
							invoiceData.shoulder = Conversions.ToString(sqlDataReader["Shoulder"]);
							invoiceData.Waist = Conversions.ToString(sqlDataReader["Waist"]);
							invoiceData.Sleeve = Conversions.ToString(sqlDataReader["Sleeve"]);
							invoiceData.Neck = Conversions.ToString(sqlDataReader["Neck"]);
							invoiceData.ArmWidth = Conversions.ToString(sqlDataReader["ArmWidth"]);
							invoiceData.StepWidth = Conversions.ToString(sqlDataReader["StepWidth"]);
							invoiceData.pageNo = Conversions.ToString(sqlDataReader["pageNo"]);
							invoiceData.MeasurementNote = Conversions.ToString(sqlDataReader["MeasurementNote"]);
						}
						sqlDataReader.Close();
						sqlConnection.Close();
					}
					catch (Exception projectError2)
					{
						ProjectData.SetProjectError(projectError2);
						ProjectData.ClearProjectError();
					}
					try
					{
						SqlConnection sqlConnection2;
						sqlConnection2 = new SqlConnection(MainClass.connstr);
						if (sqlConnection2.State == ConnectionState.Open)
						{
							sqlConnection2.Close();
						}
						sqlConnection2.Open();
						SqlCommand sqlCommand2;
						sqlCommand2 = new SqlCommand("select * from InvoiceItemDetail where InvGlobalID=@InvGlobalID and ItemId=@ItemId", sqlConnection2);
						sqlCommand2.Parameters.AddWithValue("@InvGlobalID", Inv.InvGlobalID);
						sqlCommand2.Parameters.AddWithValue("@ItemId", invoiceItem.ItemId);
						SqlDataReader sqlDataReader2;
						sqlDataReader2 = sqlCommand2.ExecuteReader();
						sqlDataReader2.Read();
						if (sqlDataReader2.HasRows)
						{
							invoiceData.ItemColor = Conversions.ToString(sqlDataReader2["ItemColor"]);
							invoiceData.ItemSize = Conversions.ToString(sqlDataReader2["ItemSize"]);
							invoiceData.BatchNo = Conversions.ToString(sqlDataReader2["BatchNo"]);
						}
						sqlDataReader2.Close();
						sqlConnection2.Close();
					}
					catch (Exception projectError3)
					{
						ProjectData.SetProjectError(projectError3);
						ProjectData.ClearProjectError();
					}
					list.Add(invoiceData);
				}
				if (Inv.InvoiceItems.Count == 0)
				{
					foreach (Item item in Inv.Items)
					{
						InvoiceData invoiceData2;
						invoiceData2 = new InvoiceData();
						if (item.ProductId > 0)
						{
							continue;
						}
						invoiceData2.ItemNo = item.Code;
						invoiceData2.ItemID = Conversions.ToString(item.ItemNo);
						string nameEn2;
						nameEn2 = "";
						string nameAr2;
						nameAr2 = "";
						string CategoryAr2;
						CategoryAr2 = "";
						short num4;
						num4 = 1;
						string CategoryEn2;
						CategoryEn2 = "";
						string CategoryCode2;
						CategoryCode2 = "";
						string ItemGroupPrinter2;
						ItemGroupPrinter2 = "";
						Common.GetItemNames(ref nameAr2, ref nameEn2, item.ItemNo);
						invoiceData2.ItemName = nameAr2;
						invoiceData2.ItemNameEn = nameEn2;
						int CategoryID;
						CategoryID = num4;
						Common.GetItemCategory(ref CategoryAr2, ref CategoryEn2, ref CategoryCode2, ref CategoryID, ref ItemGroupPrinter2, item.ItemNo);
						_ = (short)CategoryID;
						invoiceData2.Printer = ItemGroupPrinter2;
						invoiceData2.ItemGroup = CategoryAr2;
						invoiceData2.ItemGroupEn = CategoryEn2;
						invoiceData2.ItemGroupCode = CategoryCode2;
						invoiceData2.Description = item.Description;
						invoiceData2.ItemNotes = item.Note;
						invoiceData2.Quantity = item.Quantity.ToString(invoiceObj.DigitsNo);
						invoiceData2.UnitEquality = item.UnitEquality.ToString(invoiceObj.DigitsNo);
						invoiceData2.EssitialQnty = item.PrimaryQnty.ToString(invoiceObj.DigitsNo);
						invoiceData2.Unit = Common.GetUnitName(item.Unit);
						double num5;
						num5 = item.Price * item.Quantity - item.ItemDiscount;
						invoiceData2.Total = num5.ToString(invoiceObj.DigitsNo);
						invoiceData2.ItemTotalWithoutVAT = (num5 - item.Vat).ToString(invoiceObj.DigitsNo);
						invoiceData2.Barcode = item.Barcode;
						invoiceData2.Price = item.Price.ToString(invoiceObj.DigitsNo);
						invoiceData2.SubDiscount = item.ItemDiscount.ToString(invoiceObj.DigitsNo);
						invoiceData2.sum = (item.Price * item.Quantity).ToString(invoiceObj.DigitsNo);
						invoiceData2.AvegCost = item.AvegCost.ToString(invoiceObj.DigitsNo);
						invoiceData2.Tax = item.Vat.ToString(invoiceObj.DigitsNo);
						invoiceData2.ItemPriceWithoutVAT = (item.Price - item.Vat / item.Quantity).ToString(invoiceObj.DigitsNo);
						invoiceData2.TaxPerc = item.VatPerc.ToString(invoiceObj.DigitsNo);
						invoiceData2.NetTable = (num5 + item.Vat).ToString(invoiceObj.DigitsNo);
						invoiceData2.CurrentQty = item.ValiableStock.ToString(invoiceObj.DigitsNo);
						invoiceData2.Store = Conversions.ToString(item.Store);
						string description;
						description = item.Description;
						if (Operators.CompareString(description, "", TextCompare: false) != 0)
						{
							try
							{
								string[] array2;
								array2 = description.Split(Conversions.ToChar(Environment.NewLine));
								invoiceData2.ItemHeight = array2[0].Substring(array2[0].LastIndexOf(":") + 2);
								invoiceData2.ItemWidth = array2[1].Substring(array2[0].LastIndexOf(":") + 2);
								invoiceData2.Qty1 = array2[2].Substring(array2[0].LastIndexOf(":") + 2);
							}
							catch (Exception projectError4)
							{
								ProjectData.SetProjectError(projectError4);
								ProjectData.ClearProjectError();
							}
						}
						InvoiceOper invoiceOper2;
						invoiceOper2 = new InvoiceOper();
						double num6;
						num6 = Inv.Net - Inv.VAT;
						double num7;
						num7 = Conversions.ToDouble((num6 * (invoiceObj.AdditionalTax / 100.0)).ToString(invoiceObj.DigitsNo));
						double num8;
						num8 = Conversions.ToDouble(((num6 + num7) * (invoiceObj.VAT / 100.0)).ToString(invoiceObj.DigitsNo));
						invoiceData2.SumPrice = Inv.SumPrice.ToString(invoiceObj.DigitsNo);
						invoiceData2.TotalWithoutVAT = num6.ToString(invoiceObj.DigitsNo);
						invoiceData2.AdditiontalTax = num7.ToString(invoiceObj.DigitsNo);
						invoiceData2.VAT = num8.ToString(invoiceObj.DigitsNo);
						invoiceData2.Net = Inv.Net.ToString(invoiceObj.DigitsNo);
						invoiceData2.ArabicLetter = InvoiceOper.ToArabicLetter(Inv.Net, Inv.Currency);
						invoiceData2.ToEnWords = Conversions.ToString(invoiceOper2.SpellNumberEDP(Conversions.ToString(Inv.Net), Inv.Currency));
						invoiceData2.Discount = Inv.TotDiscount.ToString(invoiceObj.DigitsNo);
						invoiceData2.Additions = Inv.Additions.ToString(invoiceObj.DigitsNo);
						invoiceData2.PayNetwork = Inv.PayATM.ToString(invoiceObj.DigitsNo);
						invoiceData2.Insurance = Inv.Insurance.ToString(invoiceObj.DigitsNo);
						invoiceData2.Paycash = (Inv.Paycash + Inv.Remainder).ToString(invoiceObj.DigitsNo);
						invoiceData2.Remainder = Inv.Remainder.ToString(invoiceObj.DigitsNo);
						invoiceData2.ReffNo = Inv.ReffNo;
						invoiceData2.RefDate = Inv.RefDate.ToString("yyyy-MM-dd");
						invoiceData2.PayType = payType;
						invoiceData2.InvGlobalID = Inv.InvGlobalID;
						invoiceData2.InvoiceNo = Conversions.ToString(Inv.InvoiceNo);
						invoiceData2.InvoiceType = invoiceType;
						invoiceData2.OrderNo = Conversions.ToString(Inv.OrderNo);
						invoiceData2.OrderType = orderType;
						invoiceData2.InvTime = Inv.InvDate.ToShortTimeString();
						invoiceData2.InvDate = Inv.InvDate.ToString("yyyy-MM-dd");
						invoiceData2.User = Common.GetEmpName(Inv.User);
						invoiceData2.EmpName = "";
						if (Inv.User > 0)
						{
							invoiceData2.EmpName = Common.GetEmployeeName(Inv.User);
						}
						invoiceData2.InvNote = Inv.InvNote;
						invoiceData2.Customer = customer.Name;
						invoiceData2.CustMobile = customer.Mobile;
						invoiceData2.Saleman = saleman;
						invoiceData2.CustVATno = customer.VATno;
						invoiceData2.CustAccCode = customer.AccCode;
						string CityName2;
						CityName2 = "";
						string CountryName2;
						CountryName2 = "";
						string RegionName2;
						RegionName2 = "";
						Common.GetCityName(ref RegionName2, ref CountryName2, ref CityName2, customer.Region, customer.Country, customer.City);
						invoiceData2.CustCity = CityName2;
						invoiceData2.Custcountry = CountryName2;
						invoiceData2.CustArea = RegionName2;
						invoiceData2.CustNatianalID = customer.NatianalID;
						invoiceData2.CustNote = customer.Note;
						invoiceData2.CustPlotIdentification = customer.PlotIdentification;
						invoiceData2.CustBuildingNumber = customer.BuildingNumber;
						invoiceData2.CustStreetName = customer.StreetName;
						invoiceData2.CustAdditionalStreetName = customer.AdditionalStreetName;
						invoiceData2.CustDistrict = customer.District;
						invoiceData2.CustPostalZone = customer.PostalZone;
						invoiceData2.custCrNo = customer.CrNo;
						string address2;
						address2 = "";
						string telePhone2;
						telePhone2 = "";
						string mobile2;
						mobile2 = "";
						string foundation;
						foundation = "";
						string field2;
						field2 = "";
						string vatNo2;
						vatNo2 = "";
						if (Common.FoundationInfoDT.Rows.Count > 0)
						{
							address2 = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["Address"]));
							telePhone2 = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["Tel"]));
							mobile2 = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["Mobile"]));
							foundation = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["nameA"]));
							field2 = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["FieldA"]));
							vatNo2 = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["tax_no"]));
						}
						invoiceData2.Address = address2;
						invoiceData2.Mobile = mobile2;
						invoiceData2.TelePhone = telePhone2;
						invoiceData2.VatNo = vatNo2;
						invoiceData2.Foundation = foundation;
						invoiceData2.Field = field2;
						invoiceData2.Logo = "";
						invoiceData2.Header = "";
						invoiceData2.footer = "";
						invoiceData2.Stamp = "";
						list.Add(invoiceData2);
					}
				}
				DataSet dataSet;
				dataSet = new DataSet("Data");
				new DataTable("Invoices");
				DataTable table;
				table = global::UtilitiesProj.Common.ToDataTable(list);
				dataSet.Tables.Add(table);
				list.Clear();
				return dataSet;
			}
		}

		public void SendInvoiceToEmail(XtraReport report, string RecEmail, string CustName)
		{
			try
			{
				if (!MainClass.CheckForInternetConnection())
				{
					MessageBox.Show("لا يوجد اتصال بالإنترنت، لم يتم إرسال البريد", "بريد");
					return;
				}
				using SqlDataAdapter sqlDataAdapter = new SqlDataAdapter("Select SendEmail, RecEmail, ServerName, SendPWd, port, SSL,SendInv from SettingEmail WHERE Branch_Id=" + Conversions.ToString(MainClass.BranchNo), MainClass.ConnObj());
				DataTable dataTable;
				dataTable = new DataTable();
				sqlDataAdapter.Fill(dataTable);
				SqlDataAdapter sqlDataAdapter2;
				sqlDataAdapter2 = new SqlDataAdapter("Select  nameA from Foundation", MainClass.ConnObj());
				DataTable dataTable2;
				dataTable2 = new DataTable();
				sqlDataAdapter2.Fill(dataTable2);
				if (dataTable.Rows.Count <= 0)
				{
					return;
				}
				if (Convert.ToBoolean(dataTable.AsEnumerable().ElementAtOrDefault(0)["SendInv"].ToString()))
				{
					string text;
					text = dataTable.Rows[0]["SendEmail"].ToString().Trim();
					string host;
					host = Conversions.ToString(dataTable.Rows[0]["ServerName"]);
					string password;
					password = dataTable.Rows[0]["SendPWd"].ToString().Trim();
					int port;
					port = Conversions.ToInteger(dataTable.Rows[0]["port"]);
					bool enableSsl;
					enableSsl = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["SSL"]));
					string text2;
					text2 = Conversions.ToString(dataTable2.Rows[0]["nameA"]);
					string text3;
					text3 = Application.StartupPath + "\\\\فاتورة مبيعات.pdf";
					if (File.Exists(text3))
					{
						File.Delete(text3);
					}
					report.ExportToPdf(text3);
					string body;
					body = "عزيزي العميل :" + CustName + Environment.NewLine + " : فاتورة مبيعات من " + text2 + Environment.NewLine + "التاريخ: " + DateAndTime.Now.ToString() + Environment.NewLine + "المرسل: " + MainClass.UserName + Environment.NewLine + "مع خالص التحايا …";
					using MailMessage mailMessage = new MailMessage(text, RecEmail);
					mailMessage.Subject = "فاتورة مبيعات ";
					mailMessage.Body = body;
					mailMessage.IsBodyHtml = false;
					if (File.Exists(text3))
					{
						mailMessage.Attachments.Add(new Attachment(text3));
					}
					SmtpClient smtpClient;
					smtpClient = new SmtpClient(host);
					smtpClient.EnableSsl = enableSsl;
					smtpClient.UseDefaultCredentials = false;
					smtpClient.Credentials = new NetworkCredential(text, password);
					smtpClient.Port = port;
					smtpClient.Send(mailMessage);
					if (Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0)
					{
						MessageBox.Show("تم ارسال الفاتورة بنجاح", "بريد");
					}
					else
					{
						MessageBox.Show("Email sent.", "Message");
					}
					return;
				}
				MessageBox.Show("لا توجد إعدادات بريد لهذا الفرع", "خطأ");
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				MessageBox.Show("حدث خطأ أثناء إرسال البريد: " + ex.Message, "خطأ");
				ProjectData.ClearProjectError();
			}
		}
	}
}