using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Xml;
using AuditorAPI.Models;
using AuditorAPI.Models.DGVmodels;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using ZatcaIntegrationSDK;
using ZatcaIntegrationSDK.APIHelper;
using ZatcaIntegrationSDK.BLL;
using ZatcaIntegrationSDK.HelperContracts;

namespace SmartAuditERP
{

	public class ZatcaService
	{

		internal sealed class _Closure_0024__2_002D0
		{
			public IGrouping<double, InvoiceItem> _0024VB_0024Local_itm;

			public _Closure_0024__2_002D0(_Closure_0024__2_002D0 arg0)
			{
				if (arg0 != null)
				{
					this._0024VB_0024Local_itm = arg0._0024VB_0024Local_itm;
				}
			}

			[SpecialName]
			internal bool _Lambda_0024__1(InvoiceItem c)
			{
				return c.ItemVatPerc == this._0024VB_0024Local_itm.Key;
			}
		}

		[Serializable]

		internal sealed class _Closure_0024__
		{
			public static readonly _Closure_0024__ _0024I;

			public static Func<InvoiceItem, double> _0024I2_002D0;

			public static Func<InvoiceItem, double> _0024I2_002D2;

			static _Closure_0024__()
			{
				_Closure_0024__._0024I = new _Closure_0024__();
			}

			[SpecialName]
			internal double _Lambda_0024__2_002D0(InvoiceItem item)
			{
				return item.ItemVatPerc;
			}

			[SpecialName]
			internal double _Lambda_0024__2_002D2(InvoiceItem il)
			{
				return il.ItemQuantity * il.ItemPrice - il.ItemDiscount;
			}
		}

		private SqlConnection conn;

		public ZatcaService()
		{
			this.conn = MainClass.ConnObj();
		}

		public InvoiceReportingResponse IntegrateInvoice(ref global::AuditorAPI.Models.Invoice invoice, bool IsProduction, bool IsSimulation, bool isDebit)
		{
			InvoiceReportingResponse invoiceReportingResponse;
			invoiceReportingResponse = new InvoiceReportingResponse();
			Customer customer;
			customer = new Customer(invoice.Customer);
			string value;
			value = "388";
			string name;
			name = ((!((Operators.CompareString(customer.VATno, "", TextCompare: false) == 0) | (customer.VATno == null))) ? "0100000" : "0200000");
			if ((invoice.InvoiceType == InvoiceType.Sale) | (invoice.InvoiceType == InvoiceType.POS) | (invoice.InvoiceType == (InvoiceType)20))
			{
				value = ((invoice.ProcType != 2) ? "388" : "381");
				if (isDebit)
				{
					value = "383";
				}
			}
			else if (invoice.InvoiceType == (InvoiceType)21)
			{
				if (invoice.ProcType == 2)
				{
					value = "381";
				}
				else
				{
					value = "383";
					if (isDebit)
					{
						value = "383";
					}
				}
			}
			UBLXML uBLXML;
			uBLXML = new UBLXML();
			global::ZatcaIntegrationSDK.Invoice invoice2;
			invoice2 = new global::ZatcaIntegrationSDK.Invoice();
			new global::ZatcaIntegrationSDK.Result();
			invoice2.ID = Conversions.ToString(invoice.InvoiceNo);
			invoice2.PIH = invoice.PIH;
			invoice2.IssueDate = invoice.InvDate.ToString("yyyy-MM-dd");
			invoice2.IssueTime = invoice.InvDate.ToString("HH:mm:ss");
			invoice2.invoiceTypeCode.id = Conversions.ToInteger(value);
			invoice2.invoiceTypeCode.Name = name;
			invoice2.DocumentCurrencyCode = "SAR";
			invoice2.TaxCurrencyCode = "SAR";
			invoice2.AdditionalDocumentReferenceICV.UUID = invoice.InvoiceNo;
			if (Operators.CompareString(invoice2.invoiceTypeCode.Name, "0100000", TextCompare: false) == 0)
			{
				invoice2.delivery.ActualDeliveryDate = invoice2.IssueDate;
			}
			string paymentMeansCode;
			paymentMeansCode = "10";
			if (invoice.PayType == -1)
			{
				paymentMeansCode = "30";
			}
			else if (invoice.PayType == 2)
			{
				paymentMeansCode = ((invoice.Bank <= 2) ? "48" : "42");
			}
			invoice2.paymentmeans.PaymentMeansCode = paymentMeansCode;
			if (invoice.ProcType == 2)
			{
				invoice2.billingReference.InvoiceDocumentReferenceID = invoice.ReffNo;
			}
			if (((invoice.InvoiceType == InvoiceType.Sale) | (invoice.InvoiceType == InvoiceType.POS) | (invoice.InvoiceType == (InvoiceType)20)) & (invoice.ProcType == 2))
			{
				invoice2.paymentmeans.InstructionNote = " Refund.";
				invoice2.billingReference.InvoiceDocumentReferenceID = invoice.ReffNo;
			}
			if (invoice.InvoiceType == (InvoiceType)21)
			{
				invoice2.paymentmeans.InstructionNote = " EditPrice.";
				invoice2.billingReference.InvoiceDocumentReferenceID = invoice.ReffNo;
			}
			invoice2.TaxTotal.TaxSubtotal.taxCategory.ID = "S";
			invoice2.TaxTotal.TaxSubtotal.taxCategory.Percent = new decimal(invoice.VATperc);
			if (invoice.VATperc == 0.0)
			{
				invoice2.TaxTotal.TaxSubtotal.taxCategory.ID = "Z";
				invoice2.TaxTotal.TaxSubtotal.taxCategory.TaxExemptionReason = "Medicines and medical equipment";
				invoice2.TaxTotal.TaxSubtotal.taxCategory.TaxExemptionReasonCode = "VATEX-SA-35";
			}
			invoice.ZatcaSent = false;
			if (Common.FoundationInfoDT.Rows.Count > 0)
			{
				invoice2.SupplierParty.partyLegalEntity.RegistrationName = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["nameA"]));
				invoice2.SupplierParty.partyTaxScheme.CompanyID = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["tax_no"]));
				invoice2.SupplierParty.partyIdentification.ID = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["bsn_no"]));
				invoice2.SupplierParty.partyIdentification.schemeID = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["crtype"]));
				invoice2.SupplierParty.postalAddress.StreetName = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["StreetName"]));
				invoice2.SupplierParty.postalAddress.BuildingNumber = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["BuildingNumber"]));
				invoice2.SupplierParty.postalAddress.PlotIdentification = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["PlotIdentification"]));
				invoice2.SupplierParty.postalAddress.CitySubdivisionName = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["District"]));
				invoice2.SupplierParty.postalAddress.PostalZone = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["PostalZone"]));
				invoice2.SupplierParty.postalAddress.CityName = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["city"]));
				invoice2.SupplierParty.postalAddress.country.IdentificationCode = "SA";
				invoice2.SupplierParty.postalAddress.CountrySubentity = Conversions.ToString(Operators.ConcatenateObject("", Common.FoundationInfoDT.Rows[0]["Area"]));
			}
			else
			{
				string errorMessage;
				errorMessage = "الرجاء ادخال بيانات المنشأة";
				if (Operators.CompareString(MainClass.Language, "ar", TextCompare: false) != 0)
				{
					errorMessage = "Please enter foundation data!";
				}
				invoiceReportingResponse.ErrorMessage = errorMessage;
				invoiceReportingResponse.success = false;
			}
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
			invoice2.CustomerParty.partyIdentification.ID = customer.CrNo;
			invoice2.CustomerParty.partyIdentification.schemeID = "CRN";
			invoice2.CustomerParty.postalAddress.StreetName = customer.StreetName;
			invoice2.CustomerParty.postalAddress.AdditionalStreetName = customer.AdditionalStreetName;
			invoice2.CustomerParty.postalAddress.BuildingNumber = customer.BuildingNumber;
			invoice2.CustomerParty.postalAddress.PlotIdentification = customer.PlotIdentification;
			if (string.IsNullOrEmpty(customer.City))
			{
				invoice2.CustomerParty.postalAddress.CityName = customer.city2;
			}
			else
			{
				invoice2.CustomerParty.postalAddress.CityName = customer.City;
			}
			if (string.IsNullOrEmpty(customer.Region))
			{
				invoice2.CustomerParty.postalAddress.CountrySubentity = customer.area2;
			}
			else
			{
				invoice2.CustomerParty.postalAddress.CountrySubentity = customer.Region;
			}
			invoice2.CustomerParty.postalAddress.PostalZone = customer.PostalZone;
			invoice2.CustomerParty.postalAddress.CitySubdivisionName = customer.District;
			invoice2.CustomerParty.postalAddress.country.IdentificationCode = "SA";
			invoice2.CustomerParty.partyLegalEntity.RegistrationName = customer.Name;
			invoice2.CustomerParty.partyTaxScheme.CompanyID = customer.VATno;
			AllowanceCharge allowanceCharge;
			allowanceCharge = new AllowanceCharge();
			AllowanceCharge allowanceCharge2;
			allowanceCharge2 = new AllowanceCharge();
			using (IEnumerator<IGrouping<double, InvoiceItem>> enumerator = (from item in invoice.InvoiceItems
																			 group item by item.ItemVatPerc).GetEnumerator())
			{
				_Closure_0024__2_002D0 closure_0024__2_002D = default(_Closure_0024__2_002D0);
				while (enumerator.MoveNext())
				{
					closure_0024__2_002D = new _Closure_0024__2_002D0(closure_0024__2_002D);
					closure_0024__2_002D._0024VB_0024Local_itm = enumerator.Current;
					foreach (InvoiceItem item in closure_0024__2_002D._0024VB_0024Local_itm.Where(closure_0024__2_002D._Lambda_0024__1).ToList())
					{
						decimal d;
						d = new decimal(item.ItemTotalPrice / invoice.InvoiceItems.Sum((_Closure_0024__._0024I2_002D2 == null) ? (_Closure_0024__._0024I2_002D2 = [SpecialName] (InvoiceItem il) => il.ItemQuantity * il.ItemPrice - il.ItemDiscount) : _Closure_0024__._0024I2_002D2) * invoice.Discount);
						AllowanceCharge allowanceCharge3;
						allowanceCharge3 = ((item.ItemVatPerc == 0.0) ? allowanceCharge2 : allowanceCharge);
						allowanceCharge3.Amount = decimal.Add(allowanceCharge3.Amount, d);
						allowanceCharge3.AllowanceChargeReason = "discount";
						allowanceCharge3.taxCategory.ID = ((item.ItemVatPerc == 0.0) ? "Z" : "S");
						allowanceCharge3.taxCategory.Percent = new decimal(item.ItemVatPerc);
					}
				}
			}
			if (decimal.Compare(allowanceCharge.Amount, 0m) != 0)
			{
				invoice2.allowanceCharges.Add(allowanceCharge);
			}
			if (decimal.Compare(allowanceCharge2.Amount, 0m) != 0)
			{
				invoice2.allowanceCharges.Add(allowanceCharge2);
			}
			decimal num;
			num = default(decimal);
			decimal num2;
			num2 = default(decimal);
			decimal d2;
			d2 = default(decimal);
			foreach (AllowanceCharge allowanceCharge4 in invoice2.allowanceCharges)
			{
				if (decimal.Compare(allowanceCharge4.Amount, 0m) > 0)
				{
					num = decimal.Add(num, allowanceCharge4.Amount);
				}
			}
			num = Math.Round(num, 2);
			foreach (InvoiceItem invoiceItem in invoice.InvoiceItems)
			{
				InvoiceLine invoiceLine;
				invoiceLine = new InvoiceLine();
				invoiceLine.InvoiceQuantity = new decimal(invoiceItem.ItemQuantity);
				invoiceLine.ID = Conversions.ToString(invoiceItem.ItemId);
				invoiceLine.item.classifiedTaxCategory.ID = ((invoiceItem.ItemVatPerc == 0.0) ? "Z" : "S");
				invoiceLine.item.classifiedTaxCategory.Percent = (float)invoiceItem.ItemVatPerc;
				invoiceLine.price.EncludingVat = false;
				invoiceLine.item.Name = invoiceItem.ItemName;
				decimal num3;
				if (invoice.PriceIncVAT)
				{
					invoiceLine.price.PriceAmount = new decimal(invoiceItem.ItemPrice / Convert.ToDouble(decimal.Add(1m, decimal.Divide(Convert.ToDecimal(invoiceItem.ItemVatPerc), 100m))));
					invoiceLine.price.PriceAmount = Math.Round(invoiceLine.price.PriceAmount, 2);
					num3 = Math.Round(invoiceLine.price.PriceAmount, 2);
				}
				else
				{
					invoiceLine.price.PriceAmount = Math.Round(Convert.ToDecimal(invoiceItem.ItemPrice), 2);
					num3 = Math.Round(Convert.ToDecimal(invoiceItem.ItemPrice), 2);
				}
				decimal num4;
				num4 = new decimal(Math.Round(invoiceItem.ItemQuantity, 2));
				decimal num5;
				num5 = Math.Round(decimal.Subtract(d2: new decimal(Math.Round(invoiceItem.ItemDiscount, 2)), d1: decimal.Multiply(num3, num4)), 2);
				decimal num6;
				num6 = Math.Round(decimal.Multiply(num5, decimal.Divide(Convert.ToDecimal(invoiceItem.ItemVatPerc), 100m)), 2);
				decimal roundingAmount;
				roundingAmount = Math.Round(decimal.Add(num5, num6), 2);
				invoiceLine.price.allowanceCharge.AllowanceChargeReason = "discount";
				invoiceLine.price.allowanceCharge.Amount = new decimal(invoiceItem.ItemDiscount);
				invoiceLine.taxTotal.TaxSubtotal.taxCategory.ID = "S";
				invoiceLine.taxTotal.TaxSubtotal.taxCategory.Percent = new decimal(invoiceItem.ItemVatPerc);
				if (invoiceItem.ItemVatPerc == 0.0)
				{
					invoiceLine.taxTotal.TaxSubtotal.taxCategory.ID = "Z";
					invoiceLine.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReason = "Medicines and medical equipment";
					invoiceLine.taxTotal.TaxSubtotal.taxCategory.TaxExemptionReasonCode = "VATEX-SA-35";
				}
				invoiceLine.InvoiceQuantity = num4;
				invoiceLine.price.PriceAmount = num3;
				invoiceLine.taxTotal.TaxSubtotal.TaxableAmount = num5;
				invoiceLine.taxTotal.TaxSubtotal.TaxAmount = num6;
				invoiceLine.LineExtensionAmount = Convert.ToSingle(num5);
				invoiceLine.taxTotal.TaxSubtotal.RoundingAmount = roundingAmount;
				invoice2.InvoiceLines.Add(invoiceLine);
				num2 = decimal.Add(num2, num5);
				d2 = decimal.Add(d2, num6);
				invoice2.LineExtensionAmount = decimal.Add(invoice2.LineExtensionAmount, num5);
			}
			decimal d3;
			d3 = num2;
			num2 = Math.Round(decimal.Subtract(num2, num), 2);
			decimal num7;
			num7 = default(decimal);
			foreach (InvoiceLine invoiceLine2 in invoice2.InvoiceLines)
			{
				decimal d4;
				d4 = Math.Round(decimal.Subtract(decimal.Multiply(invoiceLine2.InvoiceQuantity, invoiceLine2.price.PriceAmount), invoiceLine2.price.allowanceCharge.Amount), 2);
				num7 = decimal.Add(num7, Math.Round(decimal.Multiply(decimal.Subtract(d4, Math.Round(decimal.Multiply(decimal.Divide(d4, d3), num), 2)), decimal.Divide(invoiceLine2.taxTotal.TaxSubtotal.taxCategory.Percent, 100m)), 2));
			}
			num7 = num7;
			decimal invNet;
			invNet = Math.Round(decimal.Add(num2, num7), 2);
			invoice2.InvTaxExclusiveAmount = num2;
			invoice2.TaxTotal.TaxAmount = Math.Round(num7, 2);
			invoice2.InvNet = invNet;
			ZatcaCredential zatcaCredential;
			zatcaCredential = this.LoadZatcaCredential();
			global::ZatcaIntegrationSDK.Result result;
			result = uBLXML.GenerateInvoiceXML(invoice2, "", zatcaCredential.CSID, zatcaCredential.PrivateKey);
			InvoiceReportingResponse result2;
			if (result.IsValid)
			{
				invoice.InvoiceHash = result.InvoiceHash;
				invoice.UUID = result.UUID;
				invoice.QRCode = result.QRCode;
				invoice.PIH = result.PIH;
				invoice.EncodedInvoice = result.EncodedInvoice;
				try
				{
					ApiRequestLogic apiRequestLogic;
					apiRequestLogic = new ApiRequestLogic();
					InvoiceReportingRequest invoiceReportingRequest;
					invoiceReportingRequest = new InvoiceReportingRequest();
					invoiceReportingRequest.invoiceHash = result.InvoiceHash;
					invoiceReportingRequest.uuid = invoice.UUID;
					invoiceReportingRequest.invoice = result.EncodedInvoice;
					new InvoiceReportingResponse();
					InvoiceReportingResponse invoiceReportingResponse2;
					if (Operators.CompareString(invoice2.invoiceTypeCode.Name, "0100000", TextCompare: false) == 0)
					{
						invoiceReportingResponse2 = apiRequestLogic.CallReportingAPI(GlobalVariables.ToBase64Encode(zatcaCredential.CSID), zatcaCredential.Secret, invoiceReportingRequest, IsProduction, IsSimulation, isSimplified: false);
						invoice.EncodedInvoice = invoiceReportingResponse2.ClearedInvoice;
						invoice.QRCode = this.GetEncodedInvoiceQRCode(invoice.EncodedInvoice);
					}
					else
					{
						invoiceReportingResponse2 = apiRequestLogic.CallReportingAPI(GlobalVariables.ToBase64Encode(zatcaCredential.CSID), zatcaCredential.Secret, invoiceReportingRequest, IsProduction, IsSimulation, isSimplified: true);
					}
					bool flag;
					flag = false;
					if (invoiceReportingResponse2.ErrorMessage != null)
					{
						flag = true;
					}
					if (invoiceReportingResponse2.validationResults != null && invoiceReportingResponse2.validationResults.ErrorMessages.Count > 0)
					{
						flag = true;
					}
					if (!flag)
					{
						invoiceReportingResponse2.success = true;
						invoiceReportingResponse.success = true;
						invoice.ZatcaSent = true;
					}
					else
					{
						invoiceReportingResponse.success = false;
					}
					if (Operators.CompareString(invoice2.invoiceTypeCode.Name, "0100000", TextCompare: false) == 0)
					{
						invoiceReportingResponse.ClearanceStatus = invoiceReportingResponse2.ClearanceStatus;
						invoiceReportingResponse.ClearedInvoice = invoiceReportingResponse2.ClearedInvoice;
						invoiceReportingResponse.ErrorMessage = invoiceReportingResponse2.ErrorMessage;
						invoiceReportingResponse.validationResults = invoiceReportingResponse2.validationResults;
					}
					else
					{
						invoiceReportingResponse = invoiceReportingResponse2;
						invoiceReportingResponse.ErrorMessage += invoiceReportingResponse2.ReportingStatus.ToString();
					}
					result2 = invoiceReportingResponse;
				}
				catch (Exception ex)
				{
					ProjectData.SetProjectError(ex);
					Console.WriteLine(ex.Message);
					result2 = new InvoiceReportingResponse();
					ProjectData.ClearProjectError();
				}
			}
			else
			{
				MessageBox.Show(result.ErrorMessage);
				invoiceReportingResponse.success = false;
				result2 = invoiceReportingResponse;
			}
			return result2;
		}

		public ZatcaCredential LoadZatcaCredential()
		{
			ZatcaCredential zatcaCredential;
			zatcaCredential = new ZatcaCredential();
			try
			{
				SqlDataAdapter sqlDataAdapter;
				sqlDataAdapter = new SqlDataAdapter("select * from ZatcaCredential", this.conn);
				DataTable dataTable;
				dataTable = new DataTable();
				sqlDataAdapter.Fill(dataTable);
				if (dataTable.Rows.Count > 0)
				{
					zatcaCredential.CSR = Conversions.ToString(dataTable.Rows[0]["CSR"]);
					zatcaCredential.PrivateKey = Conversions.ToString(dataTable.Rows[0]["PrivateKey"]);
					zatcaCredential.CSID = Conversions.ToString(dataTable.Rows[0]["P_CSID"]);
					zatcaCredential.Secret = Conversions.ToString(dataTable.Rows[0]["P_Secret"]);
				}
				return zatcaCredential;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Interaction.MsgBox(ex.Message);
				ProjectData.ClearProjectError();
			}
			return zatcaCredential;
		}

		private string GetEncodedInvoiceQRCode(string EncodedInvoice)
		{
			string result = default(string);
			try
			{
				if (string.IsNullOrEmpty(EncodedInvoice))
				{
					result = "";
					return result;
				}
				string xml;
				xml = GlobalVariables.Base64Dencode(EncodedInvoice);
				XmlDocument xmlDocument;
				xmlDocument = new XmlDocument();
				xmlDocument.PreserveWhitespace = true;
				xmlDocument.LoadXml(xml);
				result = this.GetNodeInnerText(xmlDocument, "/*[local-name() = 'Invoice']/*[local-name() = 'AdditionalDocumentReference' and *[local-name()='ID' and .='QR']]/*[local-name() = 'Attachment']/*[local-name() = 'EmbeddedDocumentBinaryObject']");
				return result;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Console.Write(ex.Message);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		private string GetNodeInnerText(XmlDocument xml, string obj1)
		{
			XmlNamespaceManager xmlNamespaceManager;
			xmlNamespaceManager = new XmlNamespaceManager(xml.NameTable);
			xmlNamespaceManager.AddNamespace("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
			xmlNamespaceManager.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
			xmlNamespaceManager.AddNamespace("ext", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2");
			XmlNode xmlNode;
			xmlNode = xml.SelectSingleNode(obj1, xmlNamespaceManager);
			return (xmlNode == null) ? "" : xmlNode.InnerText;
		}

		public global::AuditorAPI.Models.Invoice CalculateInvoiceTotals(ref global::AuditorAPI.Models.Invoice invoice)
		{
			decimal num;
			num = default(decimal);
			decimal value;
			value = default(decimal);
			decimal num2;
			num2 = default(decimal);
			decimal num3;
			num3 = default(decimal);
			decimal value2;
			value2 = default(decimal);
			foreach (InvoiceItem invoiceItem in invoice.InvoiceItems)
			{
				if (invoice.PriceIncVAT)
				{
					invoiceItem.ItemPrice /= 1.0 + invoiceItem.ItemVatPerc / 100.0;
					invoiceItem.ItemPrice = Math.Round(invoiceItem.ItemPrice, 2);
				}
				else
				{
					invoiceItem.ItemPrice = Math.Round(invoiceItem.ItemPrice, 2);
				}
				invoiceItem.ItemSumPrice = Math.Round(invoiceItem.ItemPrice * invoiceItem.ItemQuantity, 2);
				num = new decimal(Convert.ToDouble(num) + invoiceItem.ItemSumPrice);
			}
			foreach (InvoiceItem invoiceItem2 in invoice.InvoiceItems)
			{
				decimal value3;
				value3 = new decimal((decimal.Compare(num, 0m) > 0) ? (invoiceItem2.ItemSumPrice / Convert.ToDouble(num)) : 0.0);
				decimal value4;
				value4 = new decimal(Math.Round(invoice.Discount * Convert.ToDouble(value3), 2));
				invoiceItem2.ItemTotalPrice = Math.Round(invoiceItem2.ItemSumPrice - invoiceItem2.ItemDiscount - Convert.ToDouble(value4), 2);
				invoiceItem2.ItemVat = Math.Round(invoiceItem2.ItemTotalPrice * (invoiceItem2.ItemVatPerc / 100.0), 2);
				invoiceItem2.ItemNetPrice = Math.Round(invoiceItem2.ItemTotalPrice + invoiceItem2.ItemVat, 2);
				value = new decimal(Convert.ToDouble(value) + (invoiceItem2.ItemDiscount + Convert.ToDouble(value4)));
				num2 = new decimal(Convert.ToDouble(num2) + invoiceItem2.ItemTotalPrice);
				num3 = new decimal(Convert.ToDouble(num3) + invoiceItem2.ItemVat);
				value2 = new decimal(Convert.ToDouble(value2) + invoiceItem2.ItemNetPrice);
			}
			invoice.SumPrice = Convert.ToDouble(Math.Round(num, 2));
			invoice.Discount = Math.Round(invoice.Discount, 2);
			invoice.Total = Convert.ToDouble(Math.Round(num2, 2));
			invoice.VAT = Convert.ToDouble(Math.Round(num3, 2));
			invoice.Net = Convert.ToDouble(Math.Round(decimal.Add(num2, num3), 2));
			return invoice;
		}
	}
}