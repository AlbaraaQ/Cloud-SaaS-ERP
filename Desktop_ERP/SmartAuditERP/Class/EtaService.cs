using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AuditorAPI.Models;
using AuditorAPI.Models.DGVmodels;
using SmartAuditERP.Form_WPF;
using ETA_Invoice.Models;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using QRCoder;
using Valley;

namespace SmartAuditERP
{

	public class EtaService
	{
		public bool sendtoETA(Invoice invoice)
		{
			List<InvoiceLine> list;
			list = new List<InvoiceLine>();
			Customer customer;
			customer = new Customer(invoice.Customer);
			double num;
			num = 0.0;
			double num2;
			num2 = 0.0;
			string text;
			InvoiceBody invoiceBody;
			checked
			{
				foreach (InvoiceItem invoiceItem in invoice.InvoiceItems)
				{
					InvoiceLine invoiceLine;
					invoiceLine = new InvoiceLine();
					invoiceLine.description = invoiceItem.ItemName;
					invoiceLine.itemType = invoiceItem.EgyCodeType;
					invoiceLine.itemCode = invoiceItem.EgyItemCode;
					invoiceLine.unitType = invoiceItem.UnitCode;
					invoiceLine.quantity = (int)Math.Round(invoiceItem.ItemQuantity);
					invoiceLine.internalCode = invoiceItem.ItemCode;
					invoiceLine.salesTotal = Conversions.ToDouble((invoiceItem.ItemPriceWithoutVAT * invoiceItem.ItemQuantity).ToString(Common.DigitsNo));
					invoiceLine.total = Math.Round(invoiceItem.ItemTotalPrice - invoiceItem.ItemDiscount + invoiceItem.ItemVat - (invoiceItem.ItemTotalPrice - invoiceItem.ItemDiscount) * invoiceItem.WithholdingTaxPerc / 100.0, 5);
					invoiceLine.valueDifference = 0.0;
					invoiceLine.totalTaxableFees = 0.0;
					invoiceLine.netTotal = Conversions.ToDouble((invoiceItem.ItemPriceWithoutVAT * invoiceItem.ItemQuantity - invoiceItem.ItemDiscount).ToString(Common.DigitsNo));
					invoiceLine.itemsDiscount = invoiceItem.ItemDiscount;
					InvoiceLine invoiceLine2;
					invoiceLine2 = invoiceLine;
					invoiceLine2.unitValue = new UnitValue
					{
						currencySold = "EGP",
						amountEGP = Conversions.ToDouble(invoiceItem.ItemPriceWithoutVAT.ToString(Common.DigitsNo)),
						amountSold = 0.0,
						currencyExchangeRate = 0.0
					};
					invoiceLine2.discount = new Discount
					{
						amount = Conversions.ToDouble(invoiceItem.ItemDiscount.ToString(Common.DigitsNo)),
						rate = (int)Math.Round(invoiceItem.ItemDiscountPerc)
					};
					invoiceLine2.taxableItems = new List<TaxableItem>();
					invoiceLine2.taxableItems.Add(new TaxableItem
					{
						taxType = "T1",
						amount = Conversions.ToDouble(invoiceItem.ItemVat.ToString(Common.DigitsNo)),
						rate = invoiceItem.ItemVatPerc,
						subType = "V009"
					});
					invoiceLine2.taxableItems.Add(new TaxableItem
					{
						taxType = "T4",
						amount = Math.Round((invoiceItem.ItemTotalPrice - invoiceItem.ItemDiscount) * invoiceItem.WithholdingTaxPerc / 100.0, 5),
						rate = invoiceItem.WithholdingTaxPerc,
						subType = "W002"
					});
					num += Math.Round((invoiceItem.ItemTotalPrice - invoiceItem.ItemDiscount) * invoiceItem.WithholdingTaxPerc / 100.0, 5);
					num2 += invoiceItem.ItemDiscount;
					list.Add(invoiceLine2);
				}
				text = ((!((Operators.CompareString(EtaSetting.DocumentTypeVersion, "V1.0", TextCompare: false) == 0) | (Operators.CompareString(EtaSetting.DocumentTypeVersion, "v1.0", TextCompare: false) == 0))) ? "0.9" : "1.0");
				invoiceBody = new InvoiceBody();
				invoiceBody.invoiceLines = list;
			}
			bool result = default(bool);
			if (Common.FoundationInfoDT.Rows.Count > 0)
			{
				invoiceBody.issuer = new Issuer();
				Issuer issuer;
				issuer = invoiceBody.issuer;
				Address address;
				address = new Address();
				object obj;
				obj = EtaSetting.BranchCode ?? "";
				if (obj == null)
				{
					obj = "";
				}
				address.branchID = (string)obj;
				address.country = "EG";
				address.governate = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["Area"]), ""));
				address.regionCity = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["city"]), ""));
				address.street = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["StreetName"]), ""));
				address.buildingNumber = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["BuildingNumber"]), ""));
				address.postalCode = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["PostalZone"]), ""));
				address.floor = "1";
				address.room = "1";
				address.landmark = "s";
				address.additionalInformation = "d";
				issuer.address = address;
				invoiceBody.issuer.type = "B";
				invoiceBody.issuer.id = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["tax_no"]), ""));
				invoiceBody.issuer.name = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["nameA"]), ""));
				invoiceBody.receiver = new Receiver();
				if ((object)customer != DBNull.Value && customer != null)
				{
					string RegionName;
					RegionName = customer.Region;
					string CountryName;
					Customer customer2;
					CountryName = (customer2 = customer).Country;
					string CityName;
					Customer customer3;
					CityName = (customer3 = customer).City;
					Common.GetCityName(ref RegionName, ref CountryName, ref CityName, customer.Region, customer.Country, customer.City);
					customer3.City = CityName;
					customer2.Country = CountryName;
					customer.Region = RegionName;
				}
				invoiceBody.receiver.address = new receiverAddress
				{
					country = ((customer.Country ?? "") ?? ""),
					governate = ((customer.Region ?? "") ?? ""),
					regionCity = ((customer.City ?? "") ?? ""),
					street = ((customer.StreetName ?? "") ?? ""),
					buildingNumber = ((customer.BuildingNumber ?? "") ?? ""),
					postalCode = ((customer.PostalZone ?? "") ?? ""),
					floor = "1",
					room = "1",
					landmark = "s",
					additionalInformation = "s"
				};
				if ((customer.VATno == null) | ((object)customer.VATno == DBNull.Value) | (Operators.CompareString(customer.VATno.ToString(), "", TextCompare: false) == 0))
				{
					invoiceBody.receiver.type = "P";
					Receiver receiver;
					receiver = invoiceBody.receiver;
					object obj2;
					obj2 = customer.NatianalID ?? "";
					if (obj2 == null)
					{
						obj2 = "";
					}
					receiver.id = (string)obj2;
				}
				else
				{
					invoiceBody.receiver.type = "B";
					Receiver receiver2;
					receiver2 = invoiceBody.receiver;
					object obj3;
					obj3 = customer.VATno ?? "";
					if (obj3 == null)
					{
						obj3 = "";
					}
					receiver2.id = (string)obj3;
				}
				invoiceBody.receiver.name = (customer.Name ?? "") ?? "";
				invoiceBody.documentType = "I";
				invoiceBody.salesOrderReference = "";
				if (invoice.ProcType == 2)
				{
					invoiceBody.documentType = "C";
					invoiceBody.salesOrderReference = Conversions.ToString(Operators.CompareString("", invoice.ReffNo, TextCompare: false) == 0);
				}
				invoiceBody.documentTypeVersion = (text ?? "") ?? "";
				invoiceBody.dateTimeIssued = (Conversions.ToDate(invoice.InvDate.ToShortDateString()).ToString("yyyy-MM-ddTHH:mm:ssZ") ?? "") ?? "";
				invoiceBody.taxpayerActivityCode = EtaSetting.ActivityCode;
				invoiceBody.internalID = (Conversions.ToString(invoice.InvoiceNo) ?? "") ?? "";
				invoiceBody.purchaseOrderReference = "";
				invoiceBody.purchaseOrderDescription = "";
				invoiceBody.salesOrderDescription = "";
				invoiceBody.proformaInvoiceNumber = "";
				invoiceBody.payment = new Payment
				{
					bankName = "",
					bankAddress = "",
					bankAccountNo = "",
					bankAccountIBAN = "",
					swiftCode = "",
					terms = ""
				};
				invoiceBody.delivery = new Delivery
				{
					approach = "",
					packaging = "",
					dateValidity = "",
					exportPort = "",
					countryOfOrigin = "EG",
					grossWeight = 0.0,
					netWeight = 0.0,
					terms = ""
				};
				invoiceBody.totalDiscountAmount = Convert.ToDouble(new decimal(invoice.TotDiscount));
				invoiceBody.totalSalesAmount = Conversions.ToDouble((invoice.Total + invoice.TotDiscount).ToString(Common.DigitsNo));
				invoiceBody.netAmount = Conversions.ToDouble(new decimal(invoice.Total).ToString(Common.DigitsNo));
				List<TaxTotal> list2;
				list2 = new List<TaxTotal>();
				try
				{
					if (decimal.Compare(new decimal(invoice.VAT), 0m) > 0)
					{
						list2.Add(new TaxTotal
						{
							taxType = "T1",
							amount = Conversions.ToDouble(new decimal(invoice.VAT).ToString(Common.DigitsNo))
						});
					}
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					ProjectData.ClearProjectError();
				}
				try
				{
					if (decimal.Compare(new decimal(num), 0m) > 0)
					{
						list2.Add(new TaxTotal
						{
							taxType = "T4",
							amount = Conversions.ToDouble(new decimal(num).ToString(Common.DigitsNo))
						});
					}
				}
				catch (Exception projectError2)
				{
					ProjectData.SetProjectError(projectError2);
					ProjectData.ClearProjectError();
				}
				invoiceBody.taxTotals = list2;
				invoiceBody.totalAmount = Conversions.ToDouble(new decimal(invoice.Net).ToString(Common.DigitsNo));
				invoiceBody.extraDiscountAmount = 0.0;
				invoiceBody.totalItemsDiscountAmount = Conversions.ToDouble(num2.ToString(Common.DigitsNo));
				try
				{
                    var EtaResultData = new EtaResultData();
                    SendEinvoice sendEinvoice;
					sendEinvoice = new SendEinvoice();
					sendEinvoice.ActivationKey = "";
					sendEinvoice.varEinvoiceType = EtaSetting.EinvoieType;
					sendEinvoice.client_id = EtaSetting.EtaClientId;
					sendEinvoice.client_secret_1 = EtaSetting.EtaClientSecret1;
					sendEinvoice.client_secret_2 = EtaSetting.EtaClientSecret2;
					sendEinvoice.TokenCertificate = EtaSetting.SignType;
					sendEinvoice.pinpass = EtaSetting.PinPass;
					sendEinvoice.DocumentVerson = EtaSetting.DocumentTypeVersion;
					sendEinvoice.FalconSignerURL = "";
					sendEinvoice.sendinvoice(invoiceBody);
					EtaResultData.GridControl1.DataSource = sendEinvoice.returnds;
					EtaResultData.GridControl1.DataSource = sendEinvoice.returnds.Tables[0];
					EtaResultData.ShowDialog();
					if (sendEinvoice.returnds != null)
					{
						new InvoiceOper().UpdateInvoiceStatus(invoice.InvGlobalID, sendEinvoice.returnds.Tables["acceptedDocuments"].Rows[0]["uuid"].ToString().Trim());
					}
					try
					{
					}
					catch (Exception projectError3)
					{
						ProjectData.SetProjectError(projectError3);
						result = false;
						ProjectData.ClearProjectError();
					}
				}
				catch (Exception projectError4)
				{
					ProjectData.SetProjectError(projectError4);
					result = false;
					ProjectData.ClearProjectError();
				}
			}
			else
			{
				if (Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0)
				{
					Interaction.MsgBox("الرجاء ادخال بيانات المنشأة");
				}
				else
				{
					Interaction.MsgBox("Please enter foundation data!");
				}
				result = false;
			}
			return result;
		}

		public void Generate_QR(string mycompanyname, string mycompanyname_Tax_No, string InvNo, DateTime InvDate, double InvTax, double InvTotal)
		{
			try
			{
				Console.WriteLine(new QRCode(new QRCodeGenerator().CreateQrCode("الشركة : " + mycompanyname + "\r\nVAT : " + mycompanyname_Tax_No + "\r\nرقم الفاتورة : " + Conversions.ToString(InvDate) + "\r\nالتاريخ : " + Strings.Format(InvDate, "yyyy-MM-dd") + " " + Strings.Format(DateAndTime.TimeOfDay, "HH:mm:ss").ToString() + "\r\nالاجمالى : " + Conversions.ToString(InvTax) + "\r\nالضريبة : " + Conversions.ToString(InvTotal), QRCodeGenerator.ECCLevel.Q)));
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		private async Task CancelInvoice(object Inv_UUID)
		{
			try
			{
				SendEinvoice sendEinvoice;
				sendEinvoice = new SendEinvoice();
				sendEinvoice.ActivationKey = "";
				sendEinvoice.varEinvoiceType = EtaSetting.EinvoieType;
				sendEinvoice.client_id = EtaSetting.EtaClientId;
				sendEinvoice.client_secret_1 = EtaSetting.EtaClientSecret1;
				sendEinvoice.client_secret_2 = EtaSetting.EtaClientSecret2;
				sendEinvoice.Cancel_invoice(Conversions.ToString(RuntimeHelpers.GetObjectValue(Inv_UUID)));
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public async Task PrintEInvoice(object Inv_UUID)
		{
			try
			{
				SendEinvoice sendEinvoice;
				sendEinvoice = new SendEinvoice();
				sendEinvoice.ActivationKey = "";
				sendEinvoice.varEinvoiceType = EtaSetting.EinvoieType;
				sendEinvoice.client_id = EtaSetting.EtaClientId;
				sendEinvoice.client_secret_1 = EtaSetting.EtaClientSecret1;
				sendEinvoice.client_secret_2 = EtaSetting.EtaClientSecret2;
				sendEinvoice.print_invoice(Conversions.ToString(RuntimeHelpers.GetObjectValue(Inv_UUID)));
				Process.Start(sendEinvoice.returnds.Tables["rootNode"].Rows[0]["publicUrl"].ToString());
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		private async Task GetEInvoiceData(object Inv_UUID)
		{
			try
			{
                var EtaResultData = new EtaResultData();
                SendEinvoice sendEinvoice;
				sendEinvoice = new SendEinvoice();
				sendEinvoice.ActivationKey = "";
				sendEinvoice.varEinvoiceType = EtaSetting.EinvoieType;
				sendEinvoice.client_id = EtaSetting.EtaClientId;
				sendEinvoice.client_secret_1 = EtaSetting.EtaClientSecret1;
				sendEinvoice.client_secret_2 = EtaSetting.EtaClientSecret2;
				sendEinvoice.print_invoice(Conversions.ToString(RuntimeHelpers.GetObjectValue(Inv_UUID)));
				EtaResultData.GridControl1.DataSource = sendEinvoice.returnds;
				EtaResultData.GridControl1.DataSource = sendEinvoice.returnds.Tables[0];
				EtaResultData.ShowDialog();
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}
	}
}