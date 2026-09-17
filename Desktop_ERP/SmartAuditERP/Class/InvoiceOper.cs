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
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using log4net;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using Ws_Auditor;
using ZatcaIntegrationSDK.HelperContracts;
using SmartAuditERP.Form_WPF;

namespace SmartAuditERP
{

	public class InvoiceOper
	{
		private class ArgumentType
		{
			public Invoice inv;
		}

		public static string[] filePathDocument;

		private static readonly ILog Logger = LogManager.GetLogger(Environment.MachineName);

		private NeoleapService service;

		private string merchantToken;

		public InvoiceOper()
		{
			this.merchantToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJSQUpCIE1lcmNoYW50LVVyb3ZvIFRlc3QgVGVybWluYSAgMTIzNDU2IiwiaWF0IjoxNzQwOTg5MzE2fQ.13YXd3fqVUKJh1iyrGz14yB4sgCEW7qaog3J4DU9n3k";
		}

		public static string GetInvoiceType(int invType, int ProcType, int PayType = 0, short TaxType = 1)
		{
			string text;
			text = "";
			int num;
			num = 1;
			if (Operators.CompareString(MainClass.Language, "ar", TextCompare: false) != 0)
			{
				num = 2;
			}
			string text2;
			text2 = "";
			if (invType == 1)
			{
				text = "مشتريات";
				if (num == 2)
				{
					text = " Purchase ";
				}
			}
			if (invType == 2 || (invType == 23 && ProcType == 1))
			{
				switch (TaxType)
				{
					case 2:
						text = "فاتورة ضريبية";
						if (num == 2)
						{
							text = "Tax Invoice";
						}
						break;
					case 3:
						text = "فاتورة مبيعات";
						break;
					default:
						text = "فاتورة ضريبية مبسطة";
						if (num == 2)
						{
							text = "Simplified Tax Invoice";
						}
						break;
				}
			}
			if (invType == 3 && ProcType == 1)
			{
				switch (TaxType)
				{
					case 2:
						text = "فاتورة ضريبية";
						if (num == 2)
						{
							text = "Tax Invoice";
						}
						break;
					case 3:
						text = "فاتورة مبيعات";
						break;
					default:
						text = "فاتورة ضريبية مبسطة";
						if (num == 2)
						{
							text = "Simplified Tax Invoice";
						}
						break;
				}
			}
			if (invType == 20 && ProcType == 1)
			{
				switch (TaxType)
				{
					case 2:
						text = "فاتورة ضريبية";
						if (num == 2)
						{
							text = "Tax Invoice";
						}
						break;
					case 3:
						text = "فاتورة مبيعات";
						break;
					default:
						text = "فاتورة ضريبية مبسطة";
						if (num == 2)
						{
							text = "Simplified Tax Invoice";
						}
						break;
				}
			}
			if (invType == 23 && ProcType == 2)
			{
				switch (TaxType)
				{
					case 2:
						text = "إشعار دائن للفاتورة الضريبية ";
						text2 = "";
						if (num == 2)
						{
							text = " Credit Note for Tax Invoice ";
						}
						break;
					case 3:
						text = "مبيعات";
						break;
					default:
						text = "إشعار دائن للفاتورة الضريبية المبسطة";
						text2 = "";
						if (num == 2)
						{
							text = " Credit Note for Simplified Tax Invoice ";
						}
						break;
				}
			}
			if (invType == 21 && ProcType == 1)
			{
				text = "إشعار مدين ";
				if (num == 2)
				{
					text = "Tax Invoice";
				}
			}
			else if (invType == 21 && ProcType == 2)
			{
				text = "إشعار دائن ";
				if (num == 2)
				{
					text = "Tax Invoice";
				}
			}
			if (invType == 22 && ProcType == 1)
			{
				text = "إشعار دائن ";
				if (num == 2)
				{
					text = "Tax Invoice";
				}
			}
			else if (invType == 22 && ProcType == 2)
			{
				text = "إشعار مدين ";
				if (num == 2)
				{
					text = "Tax Invoice";
				}
			}
			if (invType == 4)
			{
				text = " إدخال ";
				if (num == 2)
				{
					text = " In Stock ";
				}
			}
			if (invType == 5)
			{
				text = " إخراج ";
				if (num == 2)
				{
					text = " Out Stock ";
				}
			}
			if ((invType == 1 || invType == 2 || invType == 3) && ProcType == 2)
			{
				text2 = " مرتجع ";
				if (num == 2)
				{
					text2 = " Return ";
				}
			}
			if (ProcType == 3 && invType > 1)
			{
				text = "معلقة";
			}
			else if (ProcType == 4 && invType > 1)
			{
				text = "عرض سعر";
			}
			else if (invType == 9)
			{
				text = " بضاعة أول المدة";
			}
			if (ProcType == 3 && invType == 3)
			{
				text = "فاتورة مؤقتة";
			}
			if (PayType == -1 || PayType == 2 || PayType == 3 || PayType == 4 || PayType == 4)
			{
			}
			if (invType > 3 || (ProcType == 4 && invType > 1))
			{
			}
			if (invType == 2 && ProcType == 2)
			{
				switch (TaxType)
				{
					case 2:
						text = "إشعار دائن للفاتورة الضريبية ";
						text2 = "";
						if (num == 2)
						{
							text = " Credit Note for Tax Invoice ";
						}
						break;
					case 3:
						text = "مبيعات";
						break;
					default:
						text = "إشعار دائن للفاتورة الضريبية المبسطة";
						text2 = "";
						if (num == 2)
						{
							text = " Credit Note for Simplified Tax Invoice ";
						}
						break;
				}
			}
			if (invType == 3 && ProcType == 2)
			{
				switch (TaxType)
				{
					case 2:
						text = "إشعار دائن للفاتورة الضريبية ";
						text2 = "";
						if (num == 2)
						{
							text = " Credit Note for Tax Invoice ";
						}
						break;
					case 3:
						text = "مبيعات";
						break;
					default:
						text = "إشعار دائن للفاتورة الضريبية المبسطة";
						text2 = "";
						if (num == 2)
						{
							text = " Credit Note for Simplified Tax Invoice ";
						}
						break;
				}
			}
			if (invType == 20 && ProcType == 2)
			{
				switch (TaxType)
				{
					case 2:
						text = "إشعار دائن للفاتورة الضريبية ";
						text2 = "";
						if (num == 2)
						{
							text = " Credit Note for Tax Invoice ";
						}
						break;
					case 3:
						text = "مبيعات";
						break;
					default:
						text = "إشعار دائن للفاتورة الضريبية المبسطة";
						text2 = "";
						if (num == 2)
						{
							text = " Credit Note for Simplified Tax Invoice ";
						}
						break;
				}
			}
			if (invType == 8)
			{
				if (ProcType == 2)
				{
					text = "فاتورة مناقلة مرسلة  ";
					text2 = "";
					if (num == 2)
					{
						text = "Transfer Invoice ";
					}
				}
				else
				{
					text = "فاتورة مناقلة مستلمة  ";
					text2 = "";
					if (num == 2)
					{
						text = " Transfer  Invoice ";
					}
				}
			}
			if (invType == 14 && ProcType == 1)
			{
				text = "فاتورة  طلب بضاعة  ";
				text2 = "";
				if (num == 2)
				{
					text = "Goods order invoice ";
				}
			}
			return text2 + " " + text;
		}

		public static string GetInvoiceTypeAr(int invType, int ProcType, int PayType = 0, short TaxType = 1)
		{
			string result;
			result = "";
			if (invType == 1)
			{
				result = "مشتريات";
			}
			if (invType == 2 && ProcType == 1)
			{
				result = TaxType switch
				{
					2 => "فاتورة ضريبية",
					3 => "فاتورة مبيعات",
					_ => "فاتورة ضريبية مبسطة",
				};
			}
			if (invType == 3 && ProcType == 1)
			{
				result = TaxType switch
				{
					2 => "فاتورة ضريبية",
					3 => "فاتورة مبيعات",
					_ => "فاتورة ضريبية مبسطة",
				};
			}
			if (invType == 20 && ProcType == 1)
			{
				result = TaxType switch
				{
					2 => "فاتورة ضريبية",
					3 => "فاتورة مبيعات",
					_ => "فاتورة ضريبية مبسطة",
				};
			}
			if (invType == 4)
			{
				result = " إدخال ";
			}
			if (invType == 5)
			{
				result = " إخراج ";
			}
			if (ProcType == 2)
			{
			}
			if (ProcType == 3 && invType > 1)
			{
				result = "معلقة";
			}
			else if (ProcType == 4 && invType > 1)
			{
				result = "عرض سعر";
			}
			else if (invType == 9)
			{
				result = " بضاعة أول المدة";
			}
			if (PayType == -1 || PayType == 2 || PayType == 3 || PayType == 4 || PayType == 4)
			{
			}
			if (invType > 3 || (ProcType == 4 && invType > 1))
			{
			}
			if (invType == 2 && ProcType == 2)
			{
				result = TaxType switch
				{
					2 => "إشعار دائن للفاتورة الضريبية ",
					3 => "مبيعات",
					_ => "إشعار دائن للفاتورة الضريبية المبسطة",
				};
			}
			if (invType == 3 && ProcType == 2)
			{
				result = TaxType switch
				{
					2 => "إشعار دائن للفاتورة الضريبية ",
					3 => "مبيعات",
					_ => "إشعار دائن للفاتورة الضريبية المبسطة",
				};
			}
			if (invType == 20 && ProcType == 2)
			{
				result = TaxType switch
				{
					2 => "إشعار دائن للفاتورة الضريبية ",
					3 => "مبيعات",
					_ => "إشعار دائن للفاتورة الضريبية المبسطة",
				};
			}
			if (invType == 8)
			{
				result = ((ProcType != 2) ? "فاتورة مناقلة مستلمة  " : "فاتورة مناقلة مرسلة  ");
			}
			if (invType == 14 && ProcType == 1)
			{
				result = "فاتورة  طلب بضاعة  ";
			}
			return result;
		}

		public static string GetInvoiceTypeEn(int invType, int ProcType, int PayType = 0, short TaxType = 1)
		{
			string result;
			result = "";
			if (invType == 1)
			{
				result = " Purchase ";
			}
			if (invType == 2 && ProcType == 1)
			{
				result = TaxType switch
				{
					2 => "Tax Invoice",
					3 => "Sale invoice",
					_ => "Simplified Tax Invoice",
				};
			}
			if (invType == 3 && ProcType == 1)
			{
				result = TaxType switch
				{
					2 => "Tax Invoice",
					3 => "Sale invoice",
					_ => "Simplified Tax Invoice",
				};
			}
			if (invType == 20 && ProcType == 1)
			{
				result = TaxType switch
				{
					2 => "Tax Invoice",
					3 => "Sale invoice",
					_ => "Simplified Tax Invoice",
				};
			}
			if (invType == 4)
			{
				result = " In Stock ";
			}
			if (invType == 5)
			{
				result = " Out Stock ";
			}
			if (ProcType == 2)
			{
			}
			if (ProcType == 3 && invType > 1)
			{
				result = "Hold";
			}
			else if (ProcType == 4 && invType > 1)
			{
				result = "Quotation";
			}
			else if (invType == 9)
			{
				result = " بضاعة أول المدة";
			}
			if (PayType == -1 || PayType == 2 || PayType == 3 || PayType == 4 || PayType == 4)
			{
			}
			if (invType > 3 || (ProcType == 4 && invType > 1))
			{
			}
			if (invType == 2 && ProcType == 2)
			{
				result = TaxType switch
				{
					2 => " Credit Note for Tax Invoice ",
					3 => "Return invoice",
					_ => " Credit Note for Simplified Tax Invoice ",
				};
			}
			if (invType == 3 && ProcType == 2)
			{
				result = TaxType switch
				{
					2 => " Credit Note for Tax Invoice ",
					3 => "Return invoice",
					_ => " Credit Note for Simplified Tax Invoice ",
				};
			}
			if (invType == 20 && ProcType == 2)
			{
				result = TaxType switch
				{
					2 => " Credit Note for Tax Invoice ",
					3 => "Return invoice",
					_ => " Credit Note for Simplified Tax Invoice ",
				};
			}
			if (invType == 8)
			{
				result = ((ProcType != 2) ? " Transfer  Invoice " : "Transfer Invoice ");
			}
			if (invType == 14 && ProcType == 1)
			{
				result = "Goods order invoice ";
			}
			return result;
		}

		public static string ToArabicLetter(double givenNumber, Currency MyCurrency = Currency.SAR)
		{
			string text;
			text = " فقط لا غير";
			string[] array;
			array = Strings.Split(Conversions.ToString(givenNumber), ".");
			string text2;
			text2 = InvoiceOper.NumberAsCurrency(Conversions.ToDouble(array[0]), MyCurrency);
			if (array.Length >= 2)
			{
				if (array[1].Length.Equals(1))
				{
					array[1] += "0";
				}
				else if (array[1].Length > 2)
				{
					array[1] = array[1].Substring(0, 2);
				}
				text2 = text2 + " و" + InvoiceOper.FractionAsCurrency(Conversions.ToDouble(array[1]), MyCurrency);
			}
			if ((Operators.CompareString(text2, null, TextCompare: false) != 0) & (Operators.CompareString(text2, "", TextCompare: false) != 0))
			{
				text2 += text;
			}
			return text2;
		}

		public static string SFormatNumber(double X)
		{
			string str;
			str = Strings.Format(Math.Floor(X), "000000000000");
			double num;
			num = Conversion.Val(Strings.Mid(str, 12, 1));
			double num2;
			num2 = num;
			string text = default(string);
			if (num2 == 1.0)
			{
				text = "واحد";
			}
			else if (num2 == 2.0)
			{
				text = "اثنان";
			}
			else if (num2 == 3.0)
			{
				text = "ثلاثة";
			}
			else if (num2 == 4.0)
			{
				text = "اربعة";
			}
			else if (num2 == 5.0)
			{
				text = "خمسة";
			}
			else if (num2 == 6.0)
			{
				text = "ستة";
			}
			else if (num2 == 7.0)
			{
				text = "سبعة";
			}
			else if (num2 == 8.0)
			{
				text = "ثمانية";
			}
			else if (num2 == 9.0)
			{
				text = "تسعة";
			}
			double num3;
			num3 = Conversion.Val(Strings.Mid(str, 11, 1));
			double num4;
			num4 = num3;
			string text2 = default(string);
			if (num4 == 1.0)
			{
				text2 = "عشر";
			}
			else if (num4 == 2.0)
			{
				text2 = "عشرون";
			}
			else if (num4 == 3.0)
			{
				text2 = "ثلاثون";
			}
			else if (num4 == 4.0)
			{
				text2 = "اربعون";
			}
			else if (num4 == 5.0)
			{
				text2 = "خمسون";
			}
			else if (num4 == 6.0)
			{
				text2 = "ستون";
			}
			else if (num4 == 7.0)
			{
				text2 = "سبعون";
			}
			else if (num4 == 8.0)
			{
				text2 = "ثمانون";
			}
			else if (num4 == 9.0)
			{
				text2 = "تسعون";
			}
			if (Operators.CompareString(text, "", TextCompare: false) != 0 && num3 > 1.0)
			{
				text2 = text + " و" + text2;
			}
			if (Operators.CompareString(text2, "", TextCompare: false) == 0 || text2 == null)
			{
				text2 = text;
			}
			if (num == 0.0 && num3 == 1.0)
			{
				text2 += "ة";
			}
			if (num == 1.0 && num3 == 1.0)
			{
				text2 = "احدى عشر";
			}
			if (num == 2.0 && num3 == 1.0)
			{
				text2 = "اثنى عشر";
			}
			if (num > 2.0 && num3 == 1.0)
			{
				text2 = text + " " + text2;
			}
			double num5;
			num5 = Conversion.Val(Strings.Mid(str, 10, 1));
			double num6;
			num6 = num5;
			string text3 = default(string);
			if (num6 == 1.0)
			{
				text3 = "مائة";
			}
			else if (num6 == 2.0)
			{
				text3 = "مائتان";
			}
			else if (num6 > 2.0)
			{
				text3 = Strings.Left(InvoiceOper.SFormatNumber(num5), checked(Strings.Len(InvoiceOper.SFormatNumber(num5)) - 1)) + "مائة";
			}
			if ((Operators.CompareString(text3, "", TextCompare: false) != 0) & (Operators.CompareString(text2, "", TextCompare: false) != 0))
			{
				text3 = text3 + " و" + text2;
			}
			if (Operators.CompareString(text3, "", TextCompare: false) == 0)
			{
				text3 = text2;
			}
			double num7;
			num7 = Conversion.Val(Strings.Mid(str, 7, 3));
			double num8;
			num8 = num7;
			string text4 = default(string);
			if (num8 == 1.0)
			{
				text4 = "الف";
			}
			else if (num8 == 2.0)
			{
				text4 = "الفان";
			}
			else if (num8 >= 3.0 && num8 <= 10.0)
			{
				text4 = InvoiceOper.SFormatNumber(num7) + " آلاف";
			}
			else if (num8 > 10.0)
			{
				text4 = InvoiceOper.SFormatNumber(num7) + " الف";
			}
			if ((Operators.CompareString(text4, "", TextCompare: false) != 0) & (Operators.CompareString(text3, "", TextCompare: false) != 0))
			{
				text4 = text4 + " و" + text3;
			}
			if (Operators.CompareString(text4, "", TextCompare: false) == 0)
			{
				text4 = text3;
			}
			double num9;
			num9 = Conversion.Val(Strings.Mid(str, 4, 3));
			double num10;
			num10 = num9;
			string text5 = default(string);
			if (num10 == 1.0)
			{
				text5 = "مليون";
			}
			else if (num10 == 2.0)
			{
				text5 = "مليونان";
			}
			else if (num10 >= 3.0 && num10 <= 10.0)
			{
				text5 = InvoiceOper.SFormatNumber(num9) + " ملايين";
			}
			else if (num10 > 10.0)
			{
				text5 = InvoiceOper.SFormatNumber(num9) + " مليون";
			}
			if ((Operators.CompareString(text5, "", TextCompare: false) != 0) & (Operators.CompareString(text4, "", TextCompare: false) != 0))
			{
				text5 = text5 + " و" + text4;
			}
			if (Operators.CompareString(text5, "", TextCompare: false) == 0)
			{
				text5 = text4;
			}
			double num11;
			num11 = Conversion.Val(Strings.Mid(str, 1, 3));
			double num12;
			num12 = num11;
			string text6 = default(string);
			if (num12 == 1.0)
			{
				text6 = "مليار";
			}
			else if (num12 == 2.0)
			{
				text6 = "ملياران";
			}
			else if (num12 > 2.0)
			{
				text6 = InvoiceOper.SFormatNumber(num11) + " مليار";
			}
			if ((Operators.CompareString(text6, "", TextCompare: false) != 0) & (Operators.CompareString(text5, "", TextCompare: false) != 0))
			{
				text6 = text6 + " و" + text5;
			}
			if (Operators.CompareString(text6, "", TextCompare: false) == 0)
			{
				text6 = text5;
			}
			return text6;
		}

		public static bool IsPreviousReturned(string GlobalId, string InvCombinedId)
		{
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter(selectConnection: MainClass.ConnObj(), selectCommandText: "select id from Inv where proc_type=2  and (Reff_No=N'" + GlobalId + "' or Reff_No=N'" + InvCombinedId + "' ) and  IS_Deleted=0 ");
			DataTable dataTable;
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			if (dataTable.Rows.Count >= 1)
			{
				return true;
			}
			return false;
		}

		public static bool isReturned(InvoiceDGV inv)
		{
			checked
			{
				bool result;
				try
				{
					SqlConnection selectConnection;
					selectConnection = MainClass.ConnObj();
					new List<InvoiceItem>();
					SqlDataAdapter sqlDataAdapter;
					sqlDataAdapter = new SqlDataAdapter("select id,InvGlobalID,Reff_No from Inv where (inv_type=1 or inv_type=2 or inv_type=3) and proc_type=2  and (Reff_No=N'" + inv.InvGlobalID + "' or Reff_No=N'" + inv.ReffNo + "' or Date=N'" + Conversions.ToString(inv.InvDate) + "' ) and  IS_Deleted=0 ", selectConnection);
					DataTable dataTable;
					dataTable = new DataTable();
					sqlDataAdapter.Fill(dataTable);
					if (dataTable.Rows.Count >= 1)
					{
						int num;
						num = dataTable.Rows.Count - 1;
						int num2;
						num2 = 0;
						while (true)
						{
							if (num2 <= num)
							{
								List<InvoiceItem> list;
								list = new List<InvoiceItem>();
								InvoiceOper.BindItems(list, Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject("select ItemId,InvGlobalID,unit,val from Inv_sub where  InvGlobalID in(select InvGlobalID from inv where (inv_type=1 or inv_type=2 or inv_type=3) and InvGlobalID=N'", dataTable.Rows[num2]["InvGlobalID"]), "') ")));
								if (list == null)
								{
									if (num2 == dataTable.Rows.Count - 1)
									{
										result = false;
										break;
									}
								}
								else
								{
									if (InvoiceOper.isSameItems(inv.InvoiceItems.OrderBy([SpecialName] (InvoiceItem x) => x.ItemId).ToList(), list.OrderBy([SpecialName] (InvoiceItem x) => x.ItemId).ToList()))
									{
										result = true;
										break;
									}
									if (num2 == dataTable.Rows.Count - 1)
									{
										result = false;
										break;
									}
								}
								num2++;
								continue;
							}
							result = true;
							break;
						}
					}
					else
					{
						result = false;
					}
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					result = false;
					ProjectData.ClearProjectError();
				}
				return result;
			}
		}

		public static bool isnotice(InvoiceDGV inv)
		{
			bool result;
			try
			{
				SqlConnection selectConnection;
				selectConnection = MainClass.ConnObj();
				new List<InvoiceItem>();
				SqlDataAdapter sqlDataAdapter;
				sqlDataAdapter = new SqlDataAdapter("select id,InvGlobalID,Reff_No from Inv where (inv_type=21 or inv_type=22)and proc_type=" + Conversions.ToString(inv.ProcType) + "  and (Reff_No=N'" + inv.InvGlobalID + "' or Reff_No=N'" + inv.ReffNo + "' or Date=N'" + Conversions.ToString(inv.InvDate) + "' ) and  IS_Deleted=0 ", selectConnection);
				DataTable dataTable;
				dataTable = new DataTable();
				sqlDataAdapter.Fill(dataTable);
				result = dataTable.Rows.Count > 0 && (Operators.ConditionalCompareObjectNotEqual(dataTable.Rows[0]["Reff_No"], "-1", TextCompare: false) ? true : false);
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = false;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static void BindItems(List<InvoiceItem> invoiceItemsList, string Query)
		{
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter(Query, MainClass.ConnObj());
			DataTable dataTable;
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			checked
			{
				if (dataTable.Rows.Count > 0)
				{
					int num;
					num = dataTable.Rows.Count - 1;
					for (int i = 0; i <= num; i++)
					{
						InvoiceItem invoiceItem;
						invoiceItem = new InvoiceItem();
						invoiceItem.ItemId = Conversions.ToInteger(dataTable.Rows[i]["ItemId"]);
						invoiceItem.InvGlobalID = Conversions.ToString(dataTable.Rows[i]["InvGlobalID"]);
						invoiceItem.UnitID = Conversions.ToInteger(dataTable.Rows[i]["unit"]);
						invoiceItem.ItemQuantity = Conversions.ToDouble(dataTable.Rows[i]["val"]);
						invoiceItemsList.Add(invoiceItem);
					}
				}
			}
		}

		private static bool isSameItems(List<InvoiceItem> invItemList, List<InvoiceItem> ReInvItemList)
		{
			checked
			{
				foreach (InvoiceItem invItem in invItemList)
				{
					int num;
					num = ReInvItemList.Count - 1;
					for (int i = 0; i <= num; i++)
					{
						if (invItem.Equals(ReInvItemList[i]))
						{
							return true;
						}
						if (i == ReInvItemList.Count - 1)
						{
							return false;
						}
					}
				}
				return true;
			}
		}

		public static bool IsHasRefrence(string ReffId)
		{
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter(selectConnection: MainClass.ConnObj(), selectCommandText: "select id from Inv where proc_type=1  and InvCombinedId =N'" + ReffId + "' and  IS_Deleted=0 ");
			DataTable dataTable;
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			if (dataTable.Rows.Count >= 1)
			{
				return true;
			}
			return false;
		}

		public static string NumberAsCurrency(double givenNumber, Currency MyCurrency = Currency.SAR)
		{
			string text;
			text = InvoiceOper.SFormatNumber(givenNumber);
			if (((Operators.CompareString(text, "", TextCompare: false) != 0) & (Operators.CompareString(text, null, TextCompare: false) != 0)) && givenNumber <= 2.0)
			{
				if (text.StartsWith("واحد"))
				{
					text = text.Substring(4);
				}
				else if (text.StartsWith("اثنان"))
				{
					text = text.Substring(5);
				}
			}
			string left;
			left = Strings.UCase(Conversions.ToString((int)MyCurrency));
			string text2;
			if (Operators.CompareString(left, Conversions.ToString(2), TextCompare: false) == 0)
			{
				double num;
				num = givenNumber;
				text2 = ((num == 0.0) ? "" : ((num == 2.0) ? " اثنان دولار" : ((!(num >= 3.0) || !(num <= 10.0)) ? "دولار" : " دولار")));
			}
			else if (Operators.CompareString(left, Conversions.ToString(12), TextCompare: false) == 0)
			{
				double num2;
				num2 = givenNumber;
				text2 = ((num2 == 0.0) ? "" : ((num2 == 2.0) ? " جنيهان مصريان" : ((!(num2 >= 3.0) || !(num2 <= 10.0)) ? " جنيه مصري" : " جنيهات مصرية")));
			}
			else if (Operators.CompareString(left, Conversions.ToString(13), TextCompare: false) == 0)
			{
				double num3;
				num3 = givenNumber;
				text2 = ((num3 == 0.0) ? "" : ((num3 == 2.0) ? " جنيهان سودانيان" : ((!(num3 >= 3.0) || !(num3 <= 10.0)) ? " جنيه سوداني" : " جنيهات سودانية")));
			}
			else if (Operators.CompareString(left, Conversions.ToString(15), TextCompare: false) == 0)
			{
				double num4;
				num4 = givenNumber;
				text2 = ((num4 == 0.0) ? "ليرة سورية" : ((num4 == 1.0) ? "ليرة سورية" : ((num4 == 2.0) ? "ليرتان سوريتان" : ((!(num4 >= 3.0) || !(num4 <= 10.0)) ? "ليرة سورية" : "ليرات سورية"))));
			}
			else
			{
				double num5;
				num5 = givenNumber;
				text2 = ((num5 == 0.0) ? "" : ((num5 == 2.0) ? " اثنان ريال" : ((!(num5 >= 3.0) || !(num5 <= 10.0)) ? "ريال" : " ريال")));
			}
			return text + " " + text2;
		}

		public static string FractionAsCurrency(double givenNumber, Currency MyCurrency = Currency.SAR)
		{
			string text;
			text = InvoiceOper.SFormatNumber(givenNumber);
			if (((Operators.CompareString(text, "", TextCompare: false) != 0) & (Operators.CompareString(text, null, TextCompare: false) != 0)) && givenNumber <= 2.0)
			{
				if (text.StartsWith("واحد"))
				{
					text = text.Substring(4);
				}
				else if (text.StartsWith("اثنان"))
				{
					text = text.Substring(5);
				}
			}
			string left;
			left = Strings.UCase(Conversions.ToString((int)MyCurrency));
			string text2;
			if (Operators.CompareString(left, Conversions.ToString(2), TextCompare: false) == 0)
			{
				double num;
				num = givenNumber;
				text2 = ((num == 0.0) ? "" : ((num == 2.0) ? " سنتان" : ((!(num >= 3.0) || !(num <= 10.0)) ? "سنت" : " سنتات")));
			}
			else if (Operators.CompareString(left, Conversions.ToString(12), TextCompare: false) == 0)
			{
				double num2;
				num2 = givenNumber;
				text2 = ((num2 == 0.0) ? "" : ((num2 == 2.0) ? " قرشان" : ((!(num2 >= 3.0) || !(num2 <= 10.0)) ? " قرشا" : " قروش")));
			}
			else if (Operators.CompareString(left, Conversions.ToString(13), TextCompare: false) == 0)
			{
				double num3;
				num3 = givenNumber;
				text2 = ((num3 == 0.0) ? "" : ((num3 == 2.0) ? " قرشان" : ((!(num3 >= 3.0) || !(num3 <= 10.0)) ? " قرشا" : " قروش")));
			}
			else
			{
				double num4;
				num4 = givenNumber;
				text2 = ((num4 == 0.0) ? "" : ((num4 == 2.0) ? " هللتين" : ((!(num4 >= 3.0) || !(num4 <= 10.0)) ? " هللة" : " ه\u064eل\u064eلات")));
			}
			return text + " " + text2;
		}

		public object SpellNumberEDP(string MyNumber, Currency MyCurrency = Currency.SAR)
		{
			string[] array;
			array = new string[10] { null, null, " Thousand ", " Million ", " Billion ", " Trillion ", null, null, null, null };
			MyNumber = Strings.Trim(Conversion.Str(MyNumber));
			double num;
			num = Strings.InStr(MyNumber, ".");
			object objectValue = default(object);
			object obj = default(object);
			checked
			{
				if (num > 0.0)
				{
					objectValue = RuntimeHelpers.GetObjectValue(this.GetTens(Strings.Left(Strings.Mid(MyNumber, (int)Math.Round(num + 1.0)) + "00", 2)));
					MyNumber = Strings.Trim(Strings.Left(MyNumber, (int)Math.Round(num - 1.0)));
				}
				int num2;
				num2 = 1;
				while (Operators.CompareString(MyNumber, "", TextCompare: false) != 0)
				{
					object objectValue2;
					objectValue2 = RuntimeHelpers.GetObjectValue(this.GetHundreds(Strings.Right(MyNumber, 3)));
					if (Operators.ConditionalCompareObjectNotEqual(objectValue2, "", TextCompare: false))
					{
						obj = Operators.ConcatenateObject(Operators.ConcatenateObject(objectValue2, array[num2]), obj);
					}
					MyNumber = ((Strings.Len(MyNumber) <= 3) ? "" : Strings.Left(MyNumber, Strings.Len(MyNumber) - 3));
					num2++;
				}
			}
			string left;
			left = Strings.UCase(Conversions.ToString((int)MyCurrency));
			object right;
			object right2;
			object right3;
			object right4;
			if (Operators.CompareString(left, Conversions.ToString(2), TextCompare: false) == 0)
			{
				right = "Dollar";
				right2 = "Dollars";
				right3 = "Cent";
				right4 = "Cents";
			}
			else if (Operators.CompareString(left, Conversions.ToString(4), TextCompare: false) == 0)
			{
				right = "Dirham";
				right2 = "Dirhams";
				right3 = "Fil";
				right4 = "Fils";
			}
			else if (Operators.CompareString(left, Conversions.ToString(9), TextCompare: false) == 0)
			{
				right = "Pound";
				right2 = "Pounds";
				right3 = "Penny";
				right4 = "Pence";
			}
			else if (Operators.CompareString(left, Conversions.ToString(10), TextCompare: false) == 0)
			{
				right = "Euro";
				right2 = "Euros";
				right3 = "Cent";
				right4 = "Cents";
			}
			else if (Operators.CompareString(left, Conversions.ToString(11), TextCompare: false) == 0)
			{
				right = "Yen";
				right2 = "Yens";
				right3 = "Sen";
				right4 = "Sens";
			}
			else if (Operators.CompareString(left, Conversions.ToString(8), TextCompare: false) == 0)
			{
				right = "Riyal";
				right2 = "Riyals";
				right3 = "Halala";
				right4 = "Halalas";
			}
			else
			{
				right = "Riyal";
				right2 = "Riyals";
				right3 = "Halala";
				right4 = "Halalas";
			}
			object left2;
			left2 = obj;
			obj = (Operators.ConditionalCompareObjectEqual(left2, "", TextCompare: false) ? "" : ((!Operators.ConditionalCompareObjectEqual(left2, "One", TextCompare: false)) ? Operators.ConcatenateObject(Operators.ConcatenateObject(obj, " "), right2) : Operators.ConcatenateObject("One ", right)));
			object left3;
			left3 = objectValue;
			return Operators.ConcatenateObject(obj, Operators.ConditionalCompareObjectEqual(left3, "", TextCompare: false) ? "" : ((!Operators.ConditionalCompareObjectEqual(left3, "One", TextCompare: false)) ? Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(" and ", objectValue), " "), right4) : Operators.ConcatenateObject(" and One ", right3)));
		}

		public object GetHundreds(object MyNumber)
		{
			if (Conversion.Val(RuntimeHelpers.GetObjectValue(MyNumber)) != 0.0)
			{
				MyNumber = Strings.Right(Conversions.ToString(Operators.ConcatenateObject("000", MyNumber)), 3);
				string left = default(string);
				if (Operators.CompareString(Strings.Mid(Conversions.ToString(MyNumber), 1, 1), "0", TextCompare: false) != 0)
				{
					left = Conversions.ToString(Operators.ConcatenateObject(this.GetDigit(Strings.Mid(Conversions.ToString(MyNumber), 1, 1)), " Hundred "));
				}
				return (Operators.CompareString(Strings.Mid(Conversions.ToString(MyNumber), 2, 1), "0", TextCompare: false) == 0) ? Conversions.ToString(Operators.ConcatenateObject(left, this.GetDigit(Strings.Mid(Conversions.ToString(MyNumber), 3)))) : Conversions.ToString(Operators.ConcatenateObject(left, this.GetTens(Strings.Mid(Conversions.ToString(MyNumber), 2))));
			}
			object result = default(object);
			return result;
		}

		public object GetTens(object TensText)
		{
			string text;
			text = "";
			if (Conversion.Val(Strings.Left(Conversions.ToString(TensText), 1)) == 1.0)
			{
				double num;
				num = Conversion.Val(RuntimeHelpers.GetObjectValue(TensText));
				if (num == 10.0)
				{
					text = "Ten";
				}
				else if (num == 11.0)
				{
					text = "Eleven";
				}
				else if (num == 12.0)
				{
					text = "Twelve";
				}
				else if (num == 13.0)
				{
					text = "Thirteen";
				}
				else if (num == 14.0)
				{
					text = "Fourteen";
				}
				else if (num == 15.0)
				{
					text = "Fifteen";
				}
				else if (num == 16.0)
				{
					text = "Sixteen";
				}
				else if (num == 17.0)
				{
					text = "Seventeen";
				}
				else if (num == 18.0)
				{
					text = "Eighteen";
				}
				else if (num == 19.0)
				{
					text = "Nineteen";
				}
			}
			else
			{
				double num2;
				num2 = Conversion.Val(Strings.Left(Conversions.ToString(TensText), 1));
				if (num2 == 2.0)
				{
					text = "Twenty ";
				}
				else if (num2 == 3.0)
				{
					text = "Thirty ";
				}
				else if (num2 == 4.0)
				{
					text = "Forty ";
				}
				else if (num2 == 5.0)
				{
					text = "Fifty ";
				}
				else if (num2 == 6.0)
				{
					text = "Sixty ";
				}
				else if (num2 == 7.0)
				{
					text = "Seventy ";
				}
				else if (num2 == 8.0)
				{
					text = "Eighty ";
				}
				else if (num2 == 9.0)
				{
					text = "Ninety ";
				}
				text = Conversions.ToString(Operators.ConcatenateObject(text, this.GetDigit(Strings.Right(Conversions.ToString(TensText), 1))));
			}
			return text;
		}

		public object GetDigit(object Digit)
		{
			double num;
			num = Conversion.Val(RuntimeHelpers.GetObjectValue(Digit));
			if (num == 1.0)
			{
				return "One";
			}
			if (num == 2.0)
			{
				return "Two";
			}
			if (num == 3.0)
			{
				return "Three";
			}
			if (num == 4.0)
			{
				return "Four";
			}
			if (num == 5.0)
			{
				return "Five";
			}
			if (num == 6.0)
			{
				return "Six";
			}
			if (num == 7.0)
			{
				return "Seven";
			}
			if (num == 8.0)
			{
				return "Eight";
			}
			if (num == 9.0)
			{
				return "Nine";
			}
			return "";
		}

		public bool SaveInvoice(Invoice inv, Entry Entr, bool IsNew)
		{
			bool result;
			if (MainClass.EmpNo < 1)
			{
				Interaction.MsgBox("المستخدم ليس لديه الصلاحية لحفظ الفاتورة ");
				result = false;
			}
			else if (((inv.InvoiceType == InvoiceType.Purchase) | (inv.InvoiceType == InvoiceType.Sale) | (inv.InvoiceType == InvoiceType.POS) | (inv.InvoiceType == InvoiceType.BeginingInventory) | (inv.InvoiceType == InvoiceType.InventoryIn) | (inv.InvoiceType == InvoiceType.InventoryOut)) && ((inv.Store == -1) | (inv.Store == 0)))
			{
				Interaction.MsgBox("المستخدم غير مرتبط بمستودع ");
				result = false;
			}
			else if (((inv.InvoiceType == InvoiceType.Purchase) | (inv.InvoiceType == InvoiceType.Sale) | (inv.InvoiceType == InvoiceType.POS)) && inv.PayType == 1 && ((inv.Treasury == -1) | (inv.Treasury == 0)))
			{
				Interaction.MsgBox("المستخدم غير مرتبط بصندوق ");
				result = false;
			}
			else if ((inv.Net < 0.0) & ((inv.InvoiceType == InvoiceType.Sale) | (inv.InvoiceType == InvoiceType.POS)))
			{
				MessageBox.Show("يجب أن يكون الصافي اكبر من الصفر", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				result = false;
			}
			else if (inv.Customer == 0)
			{
				if (inv.InvoiceType == InvoiceType.Purchase)
				{
					MessageBox.Show(" يجب اختيار مورد   ", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				}
				else
				{
					MessageBox.Show(" يجب اختيار عميل   ", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				}
				result = false;
			}
			else
			{
				if (!((inv.InvoiceType == InvoiceType.Purchase) | (inv.InvoiceType == InvoiceType.Sale)) || inv.PayType != -1)
				{
					goto IL_0222;
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
						goto IL_0222;
					}
					MessageBox.Show("يجب اختيار المورد", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					result = false;
				}
			}
			goto IL_264b;
		IL_264b:
			return result;
		IL_0222:
			if (((inv.InvoiceType == InvoiceType.Purchase) | (inv.InvoiceType == InvoiceType.Sale) | (inv.InvoiceType == InvoiceType.POS)) && inv.PayType == -1 && !InvoiceOper.isCreditCust(inv.Customer))
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
						goto IL_264b;
					}
					if (sqlConnection.State != ConnectionState.Open)
					{
						sqlConnection.Close();
					}
				}
				if ((inv.OrderType == 3) & (Operators.CompareString(inv.TableNo, "", TextCompare: false) == 0))
				{
					string text2;
					text2 = "يجب كتابة رقم الطاولة";
					if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
					{
						text2 = "The table number must be written";
					}
					MessageBox.Show(text2, "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					result = false;
				}
				else if ((Sync.ActiveSync & (Sync.BranchType == 4)) && inv.Customer == 1)
				{
					string text3;
					text3 = "يرجى أختيار عميل اخر";
					if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
					{
						text3 = "Please select another customer";
					}
					MessageBox.Show(text3, "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					result = false;
				}
				else
				{
					if (!(MainClass.CheckForInternetConnection() & (User.CurrentCloudUser != null)) || !(Sync.ActiveSync & (Sync.BranchType == 4) & (inv.InvoiceType == InvoiceType.POS)))
					{
						goto IL_0594;
					}
					if (inv.PriceIncVAT & (User.CurrentCloudUser.TypeTax == 1))
					{
						string text4;
						text4 = " يرجى التوافق مع المدقق السحابي لنظام العمل شامل الضريبه او غير شامل الضريبة ";
						if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
						{
							text4 = "Please refer to cloud check for working system, tax-included price or tax-excluded price";
						}
						MessageBox.Show(text4, "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
						result = false;
					}
					else
					{
						if (!(!inv.PriceIncVAT & (User.CurrentCloudUser.TypeTax == 2)))
						{
							goto IL_0594;
						}
						string text5;
						text5 = " يرجى التوافق مع المدقق السحابي لنظام العمل شامل الضريبه او غير شامل الضريبة ";
						if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
						{
							text5 = "Please refer to cloud check for working system, tax-included price or tax-excluded price";
						}
						MessageBox.Show(text5, "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
						result = false;
					}
				}
			}
			goto IL_264b;
		IL_0594:
			int[] source;
			source = new int[4] { 2, 3, 21, 20 };
			int[] source2;
			source2 = new int[2] { 1, 2 };
			if (MainSetting.ZatcaIntegerationActive && InvoiceOper.IsTaxCustomer(inv.Customer) && source.Contains((int)inv.InvoiceType) && source2.Contains(inv.ProcType))
			{
				SystemResponse<InvoiceReportingResponse> systemResponse;
				systemResponse = this.SendZatca(inv);
				if (!systemResponse.IsSuccess)
				{
					MessageBox.Show(systemResponse.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					result = false;
					goto IL_264b;
				}
				ZatcaResponse zatcaResponse;
				zatcaResponse = new ZatcaResponse();
				zatcaResponse.InvGlobalID = inv.InvGlobalID;
				zatcaResponse.Status = systemResponse.ResponseObject.ReportingStatus;
				zatcaResponse.Message = systemResponse.Message;
				InvoiceOper.InsertZatcaResponse(zatcaResponse);
				inv.ZatcaSent = true;
			}
			SqlConnection sqlConnection2;
			sqlConnection2 = MainClass.ConnObj();
			if (sqlConnection2.State != ConnectionState.Open)
			{
				sqlConnection2.Open();
			}
			SqlTransaction sqlTransaction;
			sqlTransaction = sqlConnection2.BeginTransaction();
			new SqlCommand();
			try
			{
				SqlCommand sqlCommand3;
				if (IsNew)
				{
					sqlCommand3 = new SqlCommand(StoredQueries.InsertInv, sqlConnection2, sqlTransaction);
				}
				else
				{
					new SqlCommand("delete from Inv_Sub where InvGlobalID= N'" + inv.InvGlobalID + "'", sqlConnection2, sqlTransaction).ExecuteNonQuery();
					new SqlCommand("delete from InvoiceItemDetail where InvGlobalID=N'" + inv.InvGlobalID + "'", sqlConnection2, sqlTransaction).ExecuteNonQuery();
					new SqlCommand("delete from InvoiceCost where InvGlobalID=N'" + inv.InvGlobalID + "'", sqlConnection2, sqlTransaction).ExecuteNonQuery();
					new SqlCommand("delete from Glasses where InvGlobalID=N'" + inv.InvGlobalID + "'", sqlConnection2, sqlTransaction).ExecuteNonQuery();
					sqlCommand3 = new SqlCommand(StoredQueries.UpdateInv, sqlConnection2, sqlTransaction);
				}
				int num;
				num = -1;
				if (Entr != null)
				{
					num = Entr.EntryNo;
				}
				sqlCommand3.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = inv.InvGlobalID;
				sqlCommand3.Parameters.Add("@CloudID", SqlDbType.NVarChar).Value = inv.CloudID;
				sqlCommand3.Parameters.Add("@proc_type", SqlDbType.Int).Value = inv.ProcType;
				sqlCommand3.Parameters.Add("@id", SqlDbType.Int).Value = inv.InvoiceNo;
				sqlCommand3.Parameters.Add("@date", SqlDbType.DateTime).Value = inv.InvDate;
				sqlCommand3.Parameters.Add("@inv_type", SqlDbType.Int).Value = inv.InvoiceType;
				sqlCommand3.Parameters.Add("@OrderType", SqlDbType.Int).Value = inv.OrderType;
				sqlCommand3.Parameters.Add("@safe", SqlDbType.Int).Value = inv.Store;
				sqlCommand3.Parameters.Add("@stock", SqlDbType.Int).Value = inv.Treasury;
				sqlCommand3.Parameters.Add("@cust_id", SqlDbType.Int).Value = inv.Customer;
				sqlCommand3.Parameters.Add("@CashCustomerName", SqlDbType.NVarChar).Value = inv.CashCustomerName;
				sqlCommand3.Parameters.Add("@CashCustomerMobile", SqlDbType.NVarChar).Value = inv.CashCustomerMobile;
				sqlCommand3.Parameters.Add("@sales_emp", SqlDbType.Int).Value = inv.User;
				sqlCommand3.Parameters.Add("@InvTotal", SqlDbType.Float).Value = inv.Total;
				sqlCommand3.Parameters.Add("@AdditionsTot", SqlDbType.Float).Value = inv.Delivery;
				sqlCommand3.Parameters.Add("@Insurance", SqlDbType.Float).Value = inv.Insurance;
				sqlCommand3.Parameters.Add("@tot_net", SqlDbType.Float).Value = inv.Net;
				sqlCommand3.Parameters.Add("@InvProfit", SqlDbType.Float).Value = 0;
				sqlCommand3.Parameters.Add("@paid", SqlDbType.Float).Value = inv.Paid;
				sqlCommand3.Parameters.Add("@minus", SqlDbType.Float).Value = inv.Discount;
				sqlCommand3.Parameters.Add("@tax", SqlDbType.Float).Value = inv.VAT;
				sqlCommand3.Parameters.Add("@EntryID", SqlDbType.Int).Value = num;
				sqlCommand3.Parameters.Add("@cash", SqlDbType.Float).Value = inv.Paycash;
				sqlCommand3.Parameters.Add("@visa", SqlDbType.Float).Value = inv.PayATM;
				sqlCommand3.Parameters.Add("@branch", SqlDbType.Int).Value = inv.Branch;
				sqlCommand3.Parameters.Add("@IS_Buy", SqlDbType.Bit).Value = 0;
				sqlCommand3.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = inv.IsDeleted;
				sqlCommand3.Parameters.Add("@notes", SqlDbType.NVarChar).Value = inv.InvNote;
				sqlCommand3.Parameters.Add("@Reff_No ", SqlDbType.NVarChar).Value = inv.ReffNo;
				sqlCommand3.Parameters.Add("@Reff_date ", SqlDbType.DateTime).Value = inv.RefDate;
				sqlCommand3.Parameters.Add("@salesman", SqlDbType.Int).Value = inv.Saleman;
				sqlCommand3.Parameters.Add("@pay_type", SqlDbType.Int).Value = inv.PayType;
				sqlCommand3.Parameters.Add("@bank", SqlDbType.Int).Value = inv.Bank;
				sqlCommand3.Parameters.Add("@Sync", SqlDbType.Bit).Value = inv.Sent;
				sqlCommand3.Parameters.Add("@ExtraVAT", SqlDbType.Float).Value = inv.ExtraVAT;
				sqlCommand3.Parameters.Add("@AdditionalCost", SqlDbType.Float).Value = inv.AdditionalCost;
				sqlCommand3.Parameters.Add("@InvoiceStatus", SqlDbType.Int).Value = inv.InvoiceStatus;
				sqlCommand3.Parameters.Add("@PriceIncVAT", SqlDbType.Bit).Value = Convert.ToInt16(inv.PriceIncVAT);
				sqlCommand3.Parameters.Add("@PaymentStatus", SqlDbType.Int).Value = inv.PaymentStatus;
				sqlCommand3.Parameters.Add("@InvCombinedId ", SqlDbType.NVarChar).Value = inv.InvCombinedId;
				sqlCommand3.Parameters.Add("@CurrencyCode ", SqlDbType.NVarChar).Value = inv.Currency.ToString();
				sqlCommand3.Parameters.Add("@ItemsDiscount", SqlDbType.Float).Value = inv.TotDiscount - inv.Discount;
				sqlCommand3.Parameters.Add("@InvCost", SqlDbType.Float).Value = inv.InvoiceCost;
				sqlCommand3.Parameters.Add("@FreeVATSales", SqlDbType.Float).Value = inv.FreeVATSales;
				sqlCommand3.Parameters.Add("@InvSum", SqlDbType.Float).Value = inv.SumPrice;
				sqlCommand3.Parameters.Add("@VATPercent", SqlDbType.Float).Value = inv.VATperc;
				sqlCommand3.Parameters.Add("@QRCode", SqlDbType.NVarChar).Value = inv.QRCode;
				sqlCommand3.Parameters.Add("@InvoiceHash", SqlDbType.NVarChar).Value = inv.InvoiceHash;
				sqlCommand3.Parameters.Add("@UUID", SqlDbType.NVarChar).Value = inv.UUID;
				sqlCommand3.Parameters.Add("@ZatcaSent", SqlDbType.Bit).Value = inv.ZatcaSent;
				sqlCommand3.Parameters.Add("@TotalWithholdingTax", SqlDbType.Float).Value = inv.TotalWithholdingTax;
				sqlCommand3.Parameters.Add("@TableNo", SqlDbType.NVarChar).Value = inv.TableNo;
				sqlCommand3.Parameters.Add("@Balance_previews", SqlDbType.Float).Value = inv.Balance_previews;
				sqlCommand3.ExecuteNonQuery();
				foreach (InvoiceItem invoiceItem in inv.InvoiceItems)
				{
					sqlCommand3 = new SqlCommand(StoredQueries.InsertInvSub, sqlConnection2, sqlTransaction);
					sqlCommand3.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = inv.InvGlobalID;
					sqlCommand3.Parameters.Add("@proc_id", SqlDbType.Int).Value = inv.AutoIncrementID;
					sqlCommand3.Parameters.Add("@proc_type", SqlDbType.Int).Value = invoiceItem.InvertoryImpact;
					if (DateTime.Compare(invoiceItem.ItemExpireDate, DateTime.MinValue) <= 0)
					{
						invoiceItem.ItemExpireDate = DateTime.Now.AddYears(2);
					}
					sqlCommand3.Parameters.Add("@expire_date", SqlDbType.DateTime).Value = invoiceItem.ItemExpireDate;
					sqlCommand3.Parameters.Add("@Store", SqlDbType.Float).Value = invoiceItem.InvertoryId;
					sqlCommand3.Parameters.Add("@ItemId", SqlDbType.Int).Value = invoiceItem.ItemId;
					sqlCommand3.Parameters.Add("@unit", SqlDbType.Int).Value = invoiceItem.UnitID;
					sqlCommand3.Parameters.Add("@UnitEquality", SqlDbType.Float).Value = invoiceItem.UnitEquality;
					sqlCommand3.Parameters.Add("@val", SqlDbType.Float).Value = invoiceItem.ItemPrimaryQnty;
					sqlCommand3.Parameters.Add("@val1", SqlDbType.Float).Value = invoiceItem.ItemQuantity;
					sqlCommand3.Parameters.Add("@exchange_price", SqlDbType.Float).Value = invoiceItem.ItemPrice;
					sqlCommand3.Parameters.Add("@discount", SqlDbType.Float).Value = invoiceItem.ItemDiscount;
					sqlCommand3.Parameters.Add("@ItemAddedCost", SqlDbType.Float).Value = invoiceItem.ItemAddedCost;
					sqlCommand3.Parameters.Add("@taxperc", SqlDbType.Float).Value = invoiceItem.ItemVatPerc;
					sqlCommand3.Parameters.Add("@taxval", SqlDbType.Float).Value = invoiceItem.ItemVat;
					sqlCommand3.Parameters.Add("@Description", SqlDbType.NVarChar).Value = invoiceItem.Description;
					if (invoiceItem.ItemNotes == null)
					{
						sqlCommand3.Parameters.Add("@notes", SqlDbType.NVarChar).Value = "";
					}
					else
					{
						sqlCommand3.Parameters.Add("@notes", SqlDbType.NVarChar).Value = invoiceItem.ItemNotes;
					}
					sqlCommand3.Parameters.Add("@ProductId", SqlDbType.Int).Value = invoiceItem.ProductId;
					sqlCommand3.Parameters.Add("@AvrgCost", SqlDbType.Float).Value = invoiceItem.ItemCost;
					if (invoiceItem.InvertoryImpact == 1)
					{
						invoiceItem.ValiableInvertory += invoiceItem.ItemPrimaryQnty;
					}
					else if (invoiceItem.InvertoryImpact == 2)
					{
						invoiceItem.ValiableInvertory -= invoiceItem.ItemPrimaryQnty;
					}
					sqlCommand3.Parameters.Add("@CurrentQnty", SqlDbType.Float).Value = invoiceItem.ValiableInvertory;
					sqlCommand3.Parameters.Add("@WithholdingTax", SqlDbType.Float).Value = invoiceItem.WithholdingTax;
					sqlCommand3.Parameters.Add("@WithholdingTaxPerc", SqlDbType.Float).Value = invoiceItem.WithholdingTaxPerc;
					sqlCommand3.Parameters.Add("@ItemPriceWithoutVAT", SqlDbType.Float).Value = invoiceItem.ItemPriceWithoutVAT;
					sqlCommand3.Parameters.Add("@ItemCostCenter", SqlDbType.NVarChar).Value = ((!string.IsNullOrEmpty(invoiceItem.ItemCostCenter)) ? invoiceItem.ItemCostCenter : "");
					sqlCommand3.Parameters.Add("@ItemAdditionalTax", SqlDbType.Float).Value = invoiceItem.ItemAdditionalTax;
					sqlCommand3.Parameters.Add("@ItemAdditionalTaxPerc", SqlDbType.Float).Value = invoiceItem.ItemAdditionalTaxPerc;
					sqlCommand3.ExecuteNonQuery();
					foreach (InvoiceItemDetail invoiceItemDetail in invoiceItem.InvoiceItemDetails)
					{
						if (sqlConnection2.State != ConnectionState.Open)
						{
							sqlConnection2.Open();
						}
						sqlCommand3 = new SqlCommand("INSERT into InvoiceItemDetail(ItemDetailId,ItemIncrId,InvGlobalID,ItemId,InvertoryImpact,ItemSerialNo,BatchNo,ItemProductionDate,ItemExpireDate,ItemHeight,ItemWidth,ItemColor,ItemSize,ItemProperty,FillValue,FillRatio,ItemQuantity,ItemBarcode) values(@ItemDetailId,@ItemIncrId,@InvGlobalID,@ItemId,@InvertoryImpact,@ItemSerialNo,@BatchNo,@ItemProductionDate,@ItemExpireDate,@ItemHeight,@ItemWidth,@ItemColor,@ItemSize,@ItemProperty,@FillValue,@FillRatio,@ItemQuantity,@ItemBarcode)", sqlConnection2, sqlTransaction);
						sqlCommand3.Parameters.Add("@ItemDetailId", SqlDbType.Int).Value = 1;
						sqlCommand3.Parameters.Add("@ItemIncrId", SqlDbType.Int).Value = 1;
						sqlCommand3.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = inv.InvGlobalID;
						sqlCommand3.Parameters.Add("@ItemId", SqlDbType.Int).Value = invoiceItemDetail.ItemId;
						sqlCommand3.Parameters.Add("@InvertoryImpact", SqlDbType.Int).Value = invoiceItem.InvertoryImpact;
						sqlCommand3.Parameters.Add("@ItemSerialNo", SqlDbType.NVarChar).Value = invoiceItemDetail.ItemSerialNo;
						sqlCommand3.Parameters.Add("@BatchNo", SqlDbType.NVarChar).Value = invoiceItemDetail.BatchNo;
						sqlCommand3.Parameters.Add("@ItemProductionDate", SqlDbType.DateTime).Value = invoiceItemDetail.ItemProductionDate;
						sqlCommand3.Parameters.Add("@ItemExpireDate", SqlDbType.DateTime).Value = invoiceItemDetail.ItemExpireDate;
						sqlCommand3.Parameters.Add("@ItemHeight", SqlDbType.Float).Value = invoiceItemDetail.ItemHeight;
						sqlCommand3.Parameters.Add("@ItemWidth", SqlDbType.Float).Value = invoiceItemDetail.ItemWidth;
						sqlCommand3.Parameters.Add("@ItemColor", SqlDbType.NVarChar).Value = invoiceItemDetail.ItemColor;
						sqlCommand3.Parameters.Add("@ItemSize", SqlDbType.NVarChar).Value = invoiceItemDetail.ItemSize;
						sqlCommand3.Parameters.Add("@ItemProperty", SqlDbType.Int).Value = invoiceItemDetail.ItemProperty;
						sqlCommand3.Parameters.Add("@FillValue", SqlDbType.Float).Value = invoiceItemDetail.FillValue;
						sqlCommand3.Parameters.Add("@FillRatio", SqlDbType.Float).Value = invoiceItemDetail.FillRatio;
						sqlCommand3.Parameters.Add("@ItemQuantity", SqlDbType.Float).Value = invoiceItemDetail.ItemQuantity;
						sqlCommand3.Parameters.Add("@ItemBarcode", SqlDbType.NVarChar).Value = invoiceItemDetail.ItemBarcode;
						sqlCommand3.ExecuteNonQuery();
					}
					foreach (Glass glass in invoiceItem.Glasses)
					{
						if (sqlConnection2.State != ConnectionState.Open)
						{
							sqlConnection2.Open();
						}
						sqlCommand3 = new SqlCommand("Insert into Glasses(InvGlobalID,ItemId,orientation,SPH,CYL,AX,[ADD],IPD) values(@InvGlobalID,@ItemId,@orientation,@SPH,@CYL,@AX,@ADD,@IPD)", sqlConnection2, sqlTransaction);
						sqlCommand3.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = inv.InvGlobalID;
						sqlCommand3.Parameters.Add("@ItemId", SqlDbType.Int).Value = invoiceItem.ItemId;
						sqlCommand3.Parameters.Add("@orientation", SqlDbType.VarChar).Value = glass.orientation;
						sqlCommand3.Parameters.Add("@SPH", SqlDbType.VarChar).Value = glass.SPH;
						sqlCommand3.Parameters.Add("@CYL", SqlDbType.VarChar).Value = glass.CYL;
						sqlCommand3.Parameters.Add("@AX", SqlDbType.VarChar).Value = glass.AX;
						sqlCommand3.Parameters.Add("@ADD", SqlDbType.VarChar).Value = glass.ADD;
						sqlCommand3.Parameters.Add("@IPD", SqlDbType.VarChar).Value = glass.IPD;
						sqlCommand3.ExecuteNonQuery();
					}
				}
				if (inv.InvoiceCosts.Count > 0)
				{
					foreach (InvoiceCost invoiceCost in inv.InvoiceCosts)
					{
						if (sqlConnection2.State != ConnectionState.Open)
						{
							sqlConnection2.Open();
						}
						sqlCommand3 = new SqlCommand("insert into InvoiceCost(InvoiceCostId,InvoiceCostNo,InvGlobalID,CostId,CostName,Cost,CostDate,Note,costCenter)  values(@InvoiceCostId,@InvoiceCostNo,@InvGlobalID,@CostId,@CostName,@Cost,@CostDate,@Note,@costCenter)", sqlConnection2, sqlTransaction);
						sqlCommand3.Parameters.Add("@InvoiceCostId", SqlDbType.Int).Value = invoiceCost.InvoiceCostId;
						sqlCommand3.Parameters.Add("@InvoiceCostNo", SqlDbType.Int).Value = invoiceCost.InvoiceCostNo;
						sqlCommand3.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = inv.InvGlobalID;
						sqlCommand3.Parameters.Add("@CostId", SqlDbType.Int).Value = invoiceCost.CostId;
						sqlCommand3.Parameters.Add("@CostName", SqlDbType.NVarChar).Value = invoiceCost.CostName;
						sqlCommand3.Parameters.Add("@Cost", SqlDbType.Float).Value = invoiceCost.Cost;
						sqlCommand3.Parameters.Add("@CostDate", SqlDbType.DateTime).Value = DateTime.Now;
						sqlCommand3.Parameters.Add("@Note", SqlDbType.NVarChar).Value = invoiceCost.Note;
						sqlCommand3.Parameters.Add("@costCenter", SqlDbType.Int).Value = invoiceCost.costCenter;
						sqlCommand3.ExecuteNonQuery();
					}
				}
				checked
				{
					foreach (InvoicePayment invoicePayment in inv.InvoicePayments)
					{
						if (sqlConnection2.State != ConnectionState.Open)
						{
							sqlConnection2.Open();
						}
						invoicePayment.PaymentId = (int)Math.Round(Conversion.Val(Operators.ConcatenateObject("", new SqlCommand("select IsNull(max(PaymentId),0) from InvoicePayments ", sqlConnection2, sqlTransaction).ExecuteScalar())) + 1.0);
						if (inv.PaymentStatus == 1)
						{
							invoicePayment.PaymentDate = inv.InvDate;
							invoicePayment.DueDate = inv.InvDate;
						}
						else if (inv.PaymentStatus == 3)
						{
							invoicePayment.PaymentDate = DateAndTime.Now;
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
						sqlCommand3 = new SqlCommand("insert into InvoicePayments(PaymentId,InvGlobalID,PayType,Paid,Remainder,BankId,DueDate,PaymentDate,Treasury,CashPayment,MadaPayment,VisaPayment,EmpId,PaymentStatus)  values(@PaymentId,@InvGlobalID,@PayType,@Paid,@Remainder,@BankId,@DueDate,@PaymentDate,@Treasury,@CashPayment,@MadaPayment,@VisaPayment,@EmpId,@PaymentStatus)", sqlConnection2, sqlTransaction);
						sqlCommand3.Parameters.Add("@PaymentId", SqlDbType.Int).Value = invoicePayment.PaymentId;
						sqlCommand3.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = inv.InvGlobalID;
						sqlCommand3.Parameters.Add("@PayType", SqlDbType.Int).Value = invoicePayment.PayType;
						sqlCommand3.Parameters.Add("@Paid", SqlDbType.Float).Value = invoicePayment.Paid;
						sqlCommand3.Parameters.Add("@Remainder", SqlDbType.Float).Value = invoicePayment.Remainder;
						sqlCommand3.Parameters.Add("@BankId", SqlDbType.Int).Value = invoicePayment.BankId;
						sqlCommand3.Parameters.Add("@Treasury", SqlDbType.Int).Value = invoicePayment.Treasury;
						sqlCommand3.Parameters.Add("@DueDate", SqlDbType.DateTime).Value = invoicePayment.DueDate;
						sqlCommand3.Parameters.Add("@PaymentDate", SqlDbType.DateTime).Value = invoicePayment.PaymentDate;
						sqlCommand3.Parameters.Add("@CashPayment", SqlDbType.Float).Value = invoicePayment.CashPayment;
						sqlCommand3.Parameters.Add("@MadaPayment", SqlDbType.Float).Value = invoicePayment.MadaPayment;
						sqlCommand3.Parameters.Add("@VisaPayment", SqlDbType.Float).Value = invoicePayment.VisaPayment;
						sqlCommand3.Parameters.Add("@EmpId", SqlDbType.Int).Value = MainClass.EmpNo;
						sqlCommand3.Parameters.Add("@PaymentStatus", SqlDbType.Int).Value = invoicePayment.PaymentStatus;
						sqlCommand3.ExecuteNonQuery();
					}
					if (inv.InvoiceItems.Count == 0)
					{
						foreach (Item item in inv.Items)
						{
							sqlCommand3 = new SqlCommand(StoredQueries.InsertInvSub, sqlConnection2, sqlTransaction);
							sqlCommand3.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = inv.InvGlobalID;
							sqlCommand3.Parameters.Add("@proc_id", SqlDbType.Int).Value = inv.AutoIncrementID;
							if (inv.ProcType != item.ProcType)
							{
								sqlCommand3.Parameters.Add("@proc_type", SqlDbType.Int).Value = item.ProcType;
							}
							else
							{
								sqlCommand3.Parameters.Add("@proc_type", SqlDbType.Int).Value = inv.ProcType;
							}
							if (DateTime.Compare(item.ExpireDate, DateTime.MinValue) <= 0)
							{
								item.ExpireDate = DateTime.Now.AddYears(2);
							}
							sqlCommand3.Parameters.Add("@expire_date", SqlDbType.DateTime).Value = item.ExpireDate;
							sqlCommand3.Parameters.Add("@Store", SqlDbType.Float).Value = item.Store;
							sqlCommand3.Parameters.Add("@ItemId", SqlDbType.Int).Value = item.ItemNo;
							sqlCommand3.Parameters.Add("@unit", SqlDbType.Int).Value = item.Unit;
							sqlCommand3.Parameters.Add("@UnitEquality", SqlDbType.Float).Value = item.UnitEquality;
							sqlCommand3.Parameters.Add("@val", SqlDbType.Float).Value = item.PrimaryQnty;
							sqlCommand3.Parameters.Add("@val1", SqlDbType.Float).Value = item.Quantity;
							sqlCommand3.Parameters.Add("@exchange_price", SqlDbType.Float).Value = item.Price;
							sqlCommand3.Parameters.Add("@discount", SqlDbType.Float).Value = item.ItemDiscount;
							sqlCommand3.Parameters.Add("@taxperc", SqlDbType.Float).Value = item.VatPerc;
							sqlCommand3.Parameters.Add("@taxval", SqlDbType.Float).Value = item.Vat;
							sqlCommand3.Parameters.Add("@Description", SqlDbType.NVarChar).Value = item.Description;
							sqlCommand3.Parameters.Add("@ProductId", SqlDbType.Int).Value = item.ProductId;
							sqlCommand3.Parameters.Add("@AvrgCost", SqlDbType.Float).Value = item.AvegCost;
							sqlCommand3.Parameters.Add("@ItemAddedCost", SqlDbType.Float).Value = item.ItemAddedCost;
							sqlCommand3.Parameters.Add("@CurrentQnty", SqlDbType.Float).Value = item.ValiableStock;
							sqlCommand3.Parameters.Add("@WithholdingTax", SqlDbType.Float).Value = item.WithholdingTax;
							sqlCommand3.Parameters.Add("@WithholdingTaxPerc", SqlDbType.Float).Value = item.WithholdingTaxPerc;
							sqlCommand3.Parameters.Add("@ItemAdditionalTax", SqlDbType.Float).Value = item.ItemAdditionalTax;
							sqlCommand3.Parameters.Add("@ItemAdditionalTaxPerc", SqlDbType.Float).Value = item.ItemAdditionalTaxPerc;
							if (item.Note == null)
							{
								sqlCommand3.Parameters.Add("@notes", SqlDbType.NVarChar).Value = "";
							}
							else
							{
								sqlCommand3.Parameters.Add("@notes", SqlDbType.NVarChar).Value = item.Note;
							}
							sqlCommand3.Parameters.Add("@ItemPriceWithoutVAT", SqlDbType.Float).Value = item.Price;
							sqlCommand3.Parameters.Add("@ItemCostCenter", SqlDbType.NVarChar).Value = ((!string.IsNullOrEmpty(item.ItemCostCenter)) ? item.ItemCostCenter : "");
							sqlCommand3.ExecuteNonQuery();
						}
					}
					if (!string.IsNullOrEmpty(inv.EncodedInvoice))
					{
						this.InsertZatcaEncodedInvoice(inv);
					}
					if (InvoiceOper.filePathDocument != null)
					{
						string[] array;
						array = InvoiceOper.filePathDocument;
						for (int i = 0; i < array.Length; i++)
						{
							_ = array[i];
							InvoiceOper.insertDocument(inv.InvGlobalID, "Inv");
						}
					}
					if (Entr == null)
					{
						sqlTransaction.Commit();
						goto IL_23e6;
					}
					if (new EntryOper().SaveEnty(Entr))
					{
						sqlTransaction.Commit();
						goto IL_23e6;
					}
					sqlTransaction.Rollback();
					string text6;
					text6 = "خطأ أثناء الحفظ";
					if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
					{
						text6 = "error in saving";
					}
					MessageBox.Show(text6, "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					result = false;
					goto end_IL_06aa;
				}
			IL_23e6:
				if (MainSetting.ZatcaIntegerationActive && !InvoiceOper.IsTaxCustomer(inv.Customer) && source.Contains((int)inv.InvoiceType) && source2.Contains(inv.ProcType))
				{
					SystemResponse<InvoiceReportingResponse> systemResponse2;
					systemResponse2 = this.SendZatca(inv);
					if (systemResponse2.IsSuccess)
					{
						inv.ZatcaSent = true;
						if (!string.IsNullOrEmpty(inv.EncodedInvoice))
						{
							if (sqlConnection2.State != ConnectionState.Open)
							{
								sqlConnection2.Open();
							}
							sqlCommand3 = new SqlCommand("update Inv set QRCode=@QRCode ,InvoiceHash=@InvoiceHash,UUID=@UUID,ZatcaSent=@ZatcaSent where InvGlobalID=@InvGlobalID", sqlConnection2, sqlTransaction);
							sqlCommand3.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = inv.InvGlobalID;
							sqlCommand3.Parameters.Add("@QRCode", SqlDbType.NVarChar).Value = inv.QRCode;
							sqlCommand3.Parameters.Add("@InvoiceHash", SqlDbType.NVarChar).Value = inv.InvoiceHash;
							sqlCommand3.Parameters.Add("@UUID", SqlDbType.NVarChar).Value = inv.UUID;
							sqlCommand3.Parameters.Add("@ZatcaSent", SqlDbType.Bit).Value = inv.ZatcaSent;
							sqlCommand3.ExecuteNonQuery();
							this.InsertZatcaEncodedInvoice(inv);
						}
					}
					ZatcaResponse zatcaResponse2;
					zatcaResponse2 = new ZatcaResponse();
					zatcaResponse2.InvGlobalID = inv.InvGlobalID;
					zatcaResponse2.Status = systemResponse2.ResponseObject.ReportingStatus;
					zatcaResponse2.Message = systemResponse2.Message;
					InvoiceOper.InsertZatcaResponse(zatcaResponse2);
				}
				result = true;
			end_IL_06aa:;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2;
				ex2 = ex;
				sqlTransaction.Rollback();
				string text7;
				text7 = "خطأ أثناء الحفظ";
				if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
				{
					text7 = "error in saving";
				}
				MessageBox.Show((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) != 0) ? (text7 + Environment.NewLine + "Error details: " + ex2.Message) : (text7 + Environment.NewLine + "تفاصيل الخطأ: " + ex2.Message), "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				result = false;
				ProjectData.ClearProjectError();
			}
			finally
			{
				if (sqlConnection2.State != ConnectionState.Closed)
				{
					sqlConnection2.Close();
				}
			}
			goto IL_264b;
		}

		private void InsertZatcaEncodedInvoice(object Inv)
		{
			try
			{
				new SqlCommand();
				SqlConnection sqlConnection;
				sqlConnection = MainClass.ConnObj();
				if (sqlConnection.State != ConnectionState.Open)
				{
					sqlConnection.Open();
				}
				new SqlCommand(Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject("Delete from ZatcaEncodedInvoice where InvGlobalID=N'", NewLateBinding.LateGet(Inv, null, "InvGlobalID", new object[0], null, null, null)), "'")), sqlConnection).ExecuteNonQuery();
				int num;
				num = checked((int)(Convert.ToInt64(RuntimeHelpers.GetObjectValue(new SqlCommand("select ISNULL(MAX(Id), 0) from ZatcaEncodedInvoice", sqlConnection).ExecuteScalar())) + 1));
				SqlCommand sqlCommand;
				sqlCommand = new SqlCommand("Insert into ZatcaEncodedInvoice (Id,InvGlobalID,EncodedInvoice,CreatedDate,UUID)values (@Id,@InvGlobalID,@EncodedInvoice,@CreatedDate,@UUID)", sqlConnection);
				sqlCommand.Parameters.Add("@Id", SqlDbType.Int).Value = num;
				sqlCommand.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = RuntimeHelpers.GetObjectValue(NewLateBinding.LateGet(Inv, null, "InvGlobalID", new object[0], null, null, null));
				sqlCommand.Parameters.Add("@EncodedInvoice", SqlDbType.NVarChar).Value = RuntimeHelpers.GetObjectValue(NewLateBinding.LateGet(Inv, null, "EncodedInvoice", new object[0], null, null, null));
				sqlCommand.Parameters.Add("@CreatedDate", SqlDbType.DateTime).Value = DateTime.Now;
				sqlCommand.Parameters.Add("@UUID", SqlDbType.NVarChar).Value = RuntimeHelpers.GetObjectValue(NewLateBinding.LateGet(Inv, null, "UUID", new object[0], null, null, null));
				sqlCommand.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				InvoiceOper.Logger.Error("Invoice Oper  " + ex.Message + " " + MainClass.UserName);
				ProjectData.ClearProjectError();
			}
		}

		public void ReadInvoiceOnline(Invoice inv)
		{
			int[] source;
			source = new int[1] { 20 };
			int[] source2;
			source2 = new int[2] { 1, 2 };
			if (((MainSetting.ZatcaIntegerationActive && InvoiceOper.IsTaxCustomer(inv.Customer)) & (inv.InvoiceType == (InvoiceType)20)) && source.Contains((int)inv.InvoiceType) && source2.Contains(inv.ProcType))
			{
				SystemResponse<InvoiceReportingResponse> systemResponse;
				systemResponse = this.SendZatca(inv);
				if (!systemResponse.IsSuccess)
				{
					MessageBox.Show(systemResponse.Message, "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					return;
				}
				ZatcaResponse zatcaResponse;
				zatcaResponse = new ZatcaResponse();
				zatcaResponse.InvGlobalID = inv.InvGlobalID;
				zatcaResponse.Status = systemResponse.ResponseObject.ReportingStatus;
				zatcaResponse.Message = systemResponse.Message;
				InvoiceOper.InsertZatcaResponse(zatcaResponse);
				inv.ZatcaSent = true;
			}
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			if (sqlConnection.State != ConnectionState.Open)
			{
				sqlConnection.Open();
			}
			SqlTransaction sqlTransaction;
			sqlTransaction = sqlConnection.BeginTransaction();
			new SqlCommand();
			bool flag;
			flag = true;
			try
			{
				if (Convert.ToDouble(Operators.ConcatenateObject("", new SqlCommand("Select COUNT(*) from Inv where   InvGlobalID= '" + inv.InvGlobalID + "'", sqlConnection, sqlTransaction).ExecuteScalar())) > 0.0)
				{
					flag = false;
				}
				SqlCommand sqlCommand;
				if (flag)
				{
					sqlCommand = new SqlCommand(StoredQueries.InsertInv, sqlConnection, sqlTransaction);
				}
				else
				{
					new SqlCommand("delete from Inv_Sub where InvGlobalID= '" + inv.InvGlobalID + "'", sqlConnection, sqlTransaction).ExecuteNonQuery();
					sqlCommand = new SqlCommand(StoredQueries.UpdateInv, sqlConnection, sqlTransaction);
				}
				int num;
				num = 0;
				if (!string.IsNullOrEmpty(inv.EntryGlobalID))
				{
					num = Conversions.ToInteger(inv.EntryGlobalID.Substring(checked(inv.EntryGlobalID.LastIndexOf("-") + 1)));
				}
				sqlCommand.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = inv.InvGlobalID;
				sqlCommand.Parameters.Add("@CloudID", SqlDbType.NVarChar).Value = "";
				sqlCommand.Parameters.Add("@proc_type", SqlDbType.Int).Value = inv.ProcType;
				sqlCommand.Parameters.Add("@id", SqlDbType.Int).Value = inv.InvoiceNo;
				sqlCommand.Parameters.Add("@date", SqlDbType.DateTime).Value = inv.InvDate;
				sqlCommand.Parameters.Add("@inv_type", SqlDbType.Int).Value = inv.InvoiceType;
				sqlCommand.Parameters.Add("@OrderType", SqlDbType.Int).Value = inv.OrderType;
				sqlCommand.Parameters.Add("@safe", SqlDbType.Int).Value = inv.Store;
				sqlCommand.Parameters.Add("@stock", SqlDbType.Int).Value = inv.Treasury;
				sqlCommand.Parameters.Add("@cust_id", SqlDbType.Int).Value = inv.Customer;
				sqlCommand.Parameters.Add("@sales_emp", SqlDbType.Int).Value = inv.User;
				sqlCommand.Parameters.Add("@InvTotal", SqlDbType.Float).Value = inv.Total;
				sqlCommand.Parameters.Add("@AdditionsTot", SqlDbType.Float).Value = inv.Delivery;
				sqlCommand.Parameters.Add("@Insurance", SqlDbType.Float).Value = inv.Insurance;
				sqlCommand.Parameters.Add("@tot_net", SqlDbType.Float).Value = inv.Net;
				sqlCommand.Parameters.Add("@InvProfit", SqlDbType.Float).Value = inv.InvProfit;
				sqlCommand.Parameters.Add("@paid", SqlDbType.Float).Value = inv.Paycash + inv.PayATM;
				sqlCommand.Parameters.Add("@minus", SqlDbType.Float).Value = inv.Discount;
				sqlCommand.Parameters.Add("@tax", SqlDbType.Float).Value = inv.VAT;
				sqlCommand.Parameters.Add("@EntryID", SqlDbType.Int).Value = num;
				sqlCommand.Parameters.Add("@cash", SqlDbType.Float).Value = inv.Paycash;
				sqlCommand.Parameters.Add("@visa", SqlDbType.Float).Value = inv.PayATM;
				sqlCommand.Parameters.Add("@branch", SqlDbType.Int).Value = inv.Branch;
				sqlCommand.Parameters.Add("@IS_Buy", SqlDbType.Bit).Value = 0;
				sqlCommand.Parameters.Add("@IS_Deleted", SqlDbType.Bit).Value = inv.IsDeleted;
				sqlCommand.Parameters.Add("@notes", SqlDbType.NVarChar).Value = inv.InvNote;
				sqlCommand.Parameters.Add("@Reff_No ", SqlDbType.NVarChar).Value = inv.ReffNo;
				sqlCommand.Parameters.Add("@Reff_date ", SqlDbType.DateTime).Value = inv.RefDate;
				sqlCommand.Parameters.Add("@salesman", SqlDbType.Int).Value = inv.Saleman;
				sqlCommand.Parameters.Add("@pay_type", SqlDbType.Int).Value = inv.PayType;
				sqlCommand.Parameters.Add("@bank", SqlDbType.Int).Value = inv.Bank;
				sqlCommand.Parameters.Add("@Sync", SqlDbType.Bit).Value = 1;
				sqlCommand.Parameters.Add("@ExtraVAT", SqlDbType.Float).Value = inv.ExtraVAT;
				sqlCommand.Parameters.Add("@AdditionalCost", SqlDbType.Float).Value = inv.AdditionalCost;
				sqlCommand.Parameters.Add("@InvoiceStatus", SqlDbType.Int).Value = inv.InvoiceStatus;
				sqlCommand.Parameters.Add("@PriceIncVAT", SqlDbType.Bit).Value = Convert.ToInt16(inv.PriceIncVAT);
				sqlCommand.Parameters.Add("@PaymentStatus", SqlDbType.Float).Value = inv.PaymentStatus;
				if (inv.InvCombinedId == null)
				{
					inv.InvCombinedId = string.Concat(Conversions.ToString(inv.Branch) + Conversions.ToString((int)inv.InvoiceType), Conversions.ToString(inv.ProcType), Conversions.ToString(inv.InvoiceNo));
				}
				sqlCommand.Parameters.Add("@InvCombinedId ", SqlDbType.NVarChar).Value = inv.InvCombinedId;
				sqlCommand.Parameters.Add("@CurrencyCode ", SqlDbType.NVarChar).Value = inv.Currency.ToString();
				sqlCommand.Parameters.Add("@ItemsDiscount", SqlDbType.Float).Value = inv.TotDiscount - inv.Discount;
				sqlCommand.Parameters.Add("@InvCost", SqlDbType.Float).Value = inv.InvoiceCost;
				sqlCommand.Parameters.Add("@FreeVATSales", SqlDbType.Float).Value = inv.FreeVATSales;
				sqlCommand.Parameters.Add("@InvSum", SqlDbType.Float).Value = inv.SumPrice;
				sqlCommand.Parameters.Add("@VATPercent", SqlDbType.Float).Value = inv.VATperc;
				sqlCommand.Parameters.Add("@CashCustomerName", SqlDbType.NVarChar).Value = "";
				sqlCommand.Parameters.Add("@CashCustomerMobile", SqlDbType.NVarChar).Value = "";
				sqlCommand.Parameters.Add("@QRCode", SqlDbType.NVarChar).Value = inv.QRCode;
				sqlCommand.Parameters.Add("@InvoiceHash", SqlDbType.NVarChar).Value = inv.InvoiceHash;
				sqlCommand.Parameters.Add("@UUID", SqlDbType.NVarChar).Value = inv.UUID;
				sqlCommand.Parameters.Add("@ZatcaSent", SqlDbType.Bit).Value = inv.ZatcaSent;
				sqlCommand.Parameters.Add("@TotalWithholdingTax", SqlDbType.Float).Value = 0;
				sqlCommand.Parameters.Add("@TableNo", SqlDbType.NVarChar).Value = "";
				sqlCommand.Parameters.Add("@Balance_previews", SqlDbType.Float).Value = inv.Balance_previews;
				sqlCommand.ExecuteNonQuery();
				foreach (Item item in inv.Items)
				{
					if (inv.AutoIncrementID == 0)
					{
						inv.AutoIncrementID = InvoiceOper.GetAutoIncrementID();
					}
					sqlCommand = new SqlCommand(StoredQueries.InsertInvSub, sqlConnection, sqlTransaction);
					sqlCommand.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = inv.InvGlobalID;
					sqlCommand.Parameters.Add("@proc_id", SqlDbType.Int).Value = inv.AutoIncrementID;
					sqlCommand.Parameters.Add("@proc_type", SqlDbType.Int).Value = item.ProcType;
					sqlCommand.Parameters.Add("@expire_date", SqlDbType.DateTime).Value = item.ExpireDate;
					sqlCommand.Parameters.Add("@Store", SqlDbType.Float).Value = item.Store;
					sqlCommand.Parameters.Add("@ItemId", SqlDbType.Int).Value = item.ItemNo;
					sqlCommand.Parameters.Add("@unit", SqlDbType.Int).Value = item.Unit;
					sqlCommand.Parameters.Add("@UnitEquality", SqlDbType.Float).Value = item.UnitEquality;
					sqlCommand.Parameters.Add("@val", SqlDbType.Float).Value = item.PrimaryQnty;
					sqlCommand.Parameters.Add("@val1", SqlDbType.Float).Value = item.Quantity;
					sqlCommand.Parameters.Add("@exchange_price", SqlDbType.Float).Value = item.Price;
					sqlCommand.Parameters.Add("@discount", SqlDbType.Float).Value = item.ItemDiscount;
					sqlCommand.Parameters.Add("@taxperc", SqlDbType.Float).Value = item.VatPerc;
					sqlCommand.Parameters.Add("@taxval", SqlDbType.Float).Value = item.Vat;
					sqlCommand.Parameters.Add("@Description", SqlDbType.NVarChar).Value = item.Description;
					sqlCommand.Parameters.Add("@ProductId", SqlDbType.Int).Value = item.ProductId;
					sqlCommand.Parameters.Add("@AvrgCost", SqlDbType.Float).Value = item.AvegCost;
					sqlCommand.Parameters.Add("@CurrentQnty", SqlDbType.Float).Value = item.ValiableStock;
					sqlCommand.Parameters.Add("@ItemAddedCost", SqlDbType.Float).Value = item.ItemAddedCost;
					sqlCommand.Parameters.Add("@ItemPriceWithoutVAT", SqlDbType.Float).Value = item.Price;
					sqlCommand.Parameters.Add("@WithholdingTax", SqlDbType.Float).Value = 0;
					sqlCommand.Parameters.Add("@WithholdingTaxPerc", SqlDbType.Float).Value = 0;
					sqlCommand.Parameters.Add("@ItemAdditionalTax", SqlDbType.Float).Value = item.ItemAdditionalTax;
					sqlCommand.Parameters.Add("@ItemAdditionalTaxPerc", SqlDbType.Float).Value = item.ItemAdditionalTaxPerc;
					if (item.Note == null)
					{
						sqlCommand.Parameters.Add("@notes", SqlDbType.NVarChar).Value = "";
					}
					else
					{
						sqlCommand.Parameters.Add("@notes", SqlDbType.NVarChar).Value = item.Note;
					}
					sqlCommand.Parameters.Add("@ItemCostCenter", SqlDbType.NVarChar).Value = ((!string.IsNullOrEmpty(item.ItemCostCenter)) ? item.ItemCostCenter : "");
					sqlCommand.ExecuteNonQuery();
				}
				if (!string.IsNullOrEmpty(inv.EncodedInvoice))
				{
					this.InsertZatcaEncodedInvoice(inv);
				}
				if (string.IsNullOrEmpty(inv.EntryGlobalID) && inv.InvoiceType == (InvoiceType)20 && ((inv.ProcType == 1) | (inv.ProcType == 2)))
				{
					if ((inv.InvoiceType == (InvoiceType)20) & (inv.ProcType == 1))
					{
						inv.InvAccCode = "4100001";
					}
					else if ((inv.InvoiceType == (InvoiceType)20) & (inv.ProcType == 2))
					{
						inv.InvAccCode = "4100002";
					}
					string EntryGlobalId;
					EntryGlobalId = "";
					int EntryNo;
					EntryNo = 0;
					new Entry();
					InvoiceOper invoiceOper;
					invoiceOper = new InvoiceOper();
					if (Convert.ToDouble(Operators.ConcatenateObject("", new SqlCommand(("Select COUNT(*) from Entry where type=20 and doc_no=" + Conversions.ToString(inv.InvoiceNo)) ?? "", sqlConnection, sqlTransaction).ExecuteScalar())) > 0.0)
					{
						SqlDataReader sqlDataReader;
						sqlDataReader = new SqlCommand(("Select GlobalID from Entry where type=20 and doc_no=" + Conversions.ToString(inv.InvoiceNo)) ?? "", sqlConnection, sqlTransaction).ExecuteReader();
						sqlDataReader.Read();
						if (sqlDataReader.HasRows)
						{
							EntryGlobalId = sqlDataReader["GlobalID"].ToString();
						}
						sqlDataReader.Close();
					}
					if (string.IsNullOrEmpty(EntryGlobalId))
					{
						EntryOper.GetEntryGlobalID(ref EntryGlobalId, ref EntryNo);
					}
					inv.EntryGlobalID = EntryGlobalId;
					Entry entry;
					entry = invoiceOper.BindToEntry(inv);
					if (!new EntryOper().SaveEnty(entry))
					{
						sqlTransaction.Rollback();
						string text;
						text = "خطأ أثناء الحفظ";
						if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
						{
							text = "error in saving";
						}
						MessageBox.Show(text, "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					}
				}
				sqlTransaction.Commit();
				if (!MainSetting.ZatcaIntegerationActive || InvoiceOper.IsTaxCustomer(inv.Customer) || inv.InvoiceType != (InvoiceType)20 || !source.Contains((int)inv.InvoiceType) || !source2.Contains(inv.ProcType))
				{
					return;
				}
				SystemResponse<InvoiceReportingResponse> systemResponse2;
				systemResponse2 = this.SendZatca(inv);
				if (systemResponse2.IsSuccess)
				{
					inv.ZatcaSent = true;
					if (!string.IsNullOrEmpty(inv.EncodedInvoice))
					{
						if (sqlConnection.State != ConnectionState.Open)
						{
							sqlConnection.Open();
						}
						sqlCommand = new SqlCommand("update Inv set QRCode=@QRCode ,InvoiceHash=@InvoiceHash,UUID=@UUID,ZatcaSent=@ZatcaSent where InvGlobalID=@InvGlobalID", sqlConnection, sqlTransaction);
						sqlCommand.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = inv.InvGlobalID;
						sqlCommand.Parameters.Add("@QRCode", SqlDbType.NVarChar).Value = inv.QRCode;
						sqlCommand.Parameters.Add("@InvoiceHash", SqlDbType.NVarChar).Value = inv.InvoiceHash;
						sqlCommand.Parameters.Add("@UUID", SqlDbType.NVarChar).Value = inv.UUID;
						sqlCommand.Parameters.Add("@ZatcaSent", SqlDbType.Bit).Value = inv.ZatcaSent;
						sqlCommand.ExecuteNonQuery();
						this.InsertZatcaEncodedInvoice(inv);
					}
				}
				ZatcaResponse zatcaResponse2;
				zatcaResponse2 = new ZatcaResponse();
				zatcaResponse2.InvGlobalID = inv.InvGlobalID;
				zatcaResponse2.Status = systemResponse2.ResponseObject.ReportingStatus;
				zatcaResponse2.Message = systemResponse2.Message;
				InvoiceOper.InsertZatcaResponse(zatcaResponse2);
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2;
				ex2 = ex;
				sqlTransaction.Rollback();
				string text2;
				text2 = "خطأ أثناء المزامنة";
				if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
				{
					text2 = "error in saving";
				}
				MessageBox.Show((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) != 0) ? (text2 + Environment.NewLine + "Error details: " + ex2.Message) : (text2 + Environment.NewLine + "تفاصيل الخطأ: " + ex2.Message), "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				ProjectData.ClearProjectError();
			}
			finally
			{
				if (sqlConnection.State != ConnectionState.Closed)
				{
					sqlConnection.Close();
				}
			}
		}

		public SystemResponse<InvoiceReportingResponse> SendZatca(Invoice inv)
		{
			SystemResponse<InvoiceReportingResponse> systemResponse;
			systemResponse = new SystemResponse<InvoiceReportingResponse>();
			systemResponse.ResponseObject = new InvoiceReportingResponse();
			systemResponse.IsSuccess = false;
			try
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
				MainClass.ConnObj();
				if (inv.ZatcaSent)
				{
					systemResponse.IsSuccess = true;
					systemResponse.Message = "تم ارسال الفاتورة بنجاح";
					systemResponse.Message = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "تم ارسال الفاتورة بنجاح" : "Invoice submitted successfully.");
					return systemResponse;
				}
				systemResponse.Message = ((invoiceReportingResponse != null) ? JsonConvert.SerializeObject(invoiceReportingResponse.validationResults?.ErrorMessages) : invoiceReportingResponse.ErrorMessage);
				systemResponse.Message += ((invoiceReportingResponse != null) ? JsonConvert.SerializeObject(invoiceReportingResponse.validationResults?.WarningMessages) : invoiceReportingResponse.ErrorMessage);
				systemResponse.IsSuccess = false;
				return systemResponse;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2;
				ex2 = ex;
				systemResponse.IsSuccess = false;
				systemResponse.Message = ex2.Message;
				ProjectData.ClearProjectError();
			}
			return systemResponse;
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
				if (((inv.InvoiceType == InvoiceType.Sale) | (inv.InvoiceType == InvoiceType.POS) | (inv.InvoiceType == (InvoiceType)20)) & (inv.PayType == 7))
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
				else if (((inv.InvoiceType == InvoiceType.Sale) | (inv.InvoiceType == InvoiceType.POS)) & (inv.ProcType == 2))
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
				else if ((inv.InvoiceType == (InvoiceType)20) & (inv.ProcType == 1))
				{
					entry.Type = (EntryType)20;
				}
				else if ((inv.InvoiceType == (InvoiceType)20) & (inv.ProcType == 2))
				{
					entry.Type = (EntryType)201;
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
						if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)20) & (inv.ProcType == 1)))
						{
							credit = 0.0;
							debt = num;
							if ((inv.PaymentStatus == 0) | (inv.PaymentStatus == 2))
							{
								debt = num5;
							}
						}
						else if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)20) & (inv.ProcType == 2)))
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
						account.salesman = inv.Saleman;
						account.ClientCode = entry.ClientCode;
						list.Add(account);
					}
					if (invoiceObj.DetailedItemEntry)
					{
						foreach (InvoiceItem invoiceItem in inv.InvoiceItems)
						{
							account = new Account();
							if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 2)))
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
							else if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 1)))
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
						if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)20) & (inv.ProcType == 1)))
						{
							credit = num - num4 + num6;
							debt = 0.0;
						}
						else if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)20) & (inv.ProcType == 2)))
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
						if (string.IsNullOrEmpty(inv.InvCCcode))
						{
							inv.InvCCcode = Conversions.ToString(-1);
						}
						account.CCcode = inv.InvCCcode;
						account.salesman = inv.Saleman;
						account.ClientCode = entry.ClientCode;
						list.Add(account);
					}
					if (inv.ExtraVAT > 0.0)
					{
						account = new Account();
						if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 2)))
						{
							credit = inv.ExtraVAT;
							debt = 0.0;
						}
						else if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 1)))
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
						account.salesman = inv.Saleman;
						account.ClientCode = entry.ClientCode;
						list.Add(account);
					}
					account = new Account();
					if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)20) & (inv.ProcType == 1)))
					{
						credit = num4 - inv.ExtraVAT;
						debt = 0.0;
					}
					else if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)20) & (inv.ProcType == 2)))
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
					account.salesman = inv.Saleman;
					account.ClientCode = entry.ClientCode;
					list.Add(account);
					if (num6 > 0.0)
					{
						account = new Account();
						if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 2)))
						{
							credit = 0.0;
							debt = num6;
						}
						else if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 1)))
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
						if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)20) & (inv.ProcType == 1)))
						{
							credit = 0.0;
							debt = num2;
						}
						else if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)20) & (inv.ProcType == 2)))
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
						account.salesman = inv.Saleman;
						account.ClientCode = entry.ClientCode;
						list.Add(account);
					}
					if (num3 > 0.0)
					{
						account = new Account();
						if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 1)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 2)) | ((inv.InvoiceType == (InvoiceType)20) & (inv.ProcType == 1)))
						{
							credit = 0.0;
							debt = num3;
						}
						else if (((inv.InvoiceType == InvoiceType.Sale) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.POS) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.Purchase) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)21) & (inv.ProcType == 2)) | ((inv.InvoiceType == InvoiceType.ReturnSales) & (inv.ProcType == 1)) | ((inv.InvoiceType == (InvoiceType)20) & (inv.ProcType == 2)))
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
						account.salesman = inv.Saleman;
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

		public static string GetSalesEmpNameByInvNo(string GobalId)
		{
			string result;
			try
			{
				SqlConnection selectConnection;
				selectConnection = MainClass.ConnObj();
				if (Operators.CompareString(GobalId, "", TextCompare: false) != 0)
				{
					SqlDataAdapter sqlDataAdapter;
					sqlDataAdapter = new SqlDataAdapter("Select users.username from Employees, Inv, users where users.emp=Employees.id And Employees.id=Inv.sales_emp And InvGlobalId=N'" + GobalId + "'", selectConnection);
					DataTable dataTable;
					dataTable = new DataTable();
					sqlDataAdapter.Fill(dataTable);
					result = ((dataTable.Rows.Count <= 0) ? "" : Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[0][0])));
				}
				else
				{
					result = MainClass.UserName;
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = "";
				ProjectData.ClearProjectError();
			}
			return result;
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
								new SqlCommand("update Inv set Sync=1 where InvGlobalID=N'" + item.InvGlobalID + "'", sqlConnection).ExecuteNonQuery();
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

		public static int OrderNo(int InvType, int ProcType)
		{
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			if (sqlConnection.State != ConnectionState.Open)
			{
				sqlConnection.Open();
			}
			SqlCommand sqlCommand;
			sqlCommand = new SqlCommand("select count(*) from Inv where branch=" + Conversions.ToString(MainClass.BranchNo) + " and inv_type=" + Conversions.ToString(InvType) + " and proc_type=" + Conversions.ToString(ProcType) + " and date>=@date1 and date<=@date2 ", sqlConnection);
			DateTime dateTime;
			dateTime = Conversions.ToDate(DateTime.Now.ToShortDateString());
			sqlCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateTime;
			sqlCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = DateTime.Now;
			int result;
			result = checked((int)Convert.ToInt64(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) + 1);
			if (sqlConnection.State != ConnectionState.Closed)
			{
				sqlConnection.Close();
			}
			return result;
		}

		public static int RentOrderNo()
		{
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			if (sqlConnection.State != ConnectionState.Open)
			{
				sqlConnection.Open();
			}
			SqlCommand sqlCommand;
			sqlCommand = new SqlCommand("select ISNULL( COUNT(*), 0) from RentInvoice where date>=@date1 and date<=@date2", sqlConnection);
			DateTime dateTime;
			dateTime = Conversions.ToDate(DateTime.Now.ToShortDateString());
			sqlCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = dateTime;
			sqlCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = DateTime.Now;
			int result;
			result = checked((int)Convert.ToInt64(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar())) + 1);
			if (sqlConnection.State != ConnectionState.Closed)
			{
				sqlConnection.Close();
			}
			return result;
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
			num = checked((int)Convert.ToInt64(RuntimeHelpers.GetObjectValue(new SqlCommand("select ISNULL(MAX(id), 0) from Inv where branch=" + Conversions.ToString(MainClass.BranchNo) + " and inv_type=" + Conversions.ToString(InvType) + " and proc_type=" + Conversions.ToString(ProcType), sqlConnection).ExecuteScalar())) + 1);
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

		public static bool VerifyIsNewInvoice(InvoiceDGV Inv)
		{
			bool result;
			result = false;
			try
			{
				SqlConnection sqlConnection;
				sqlConnection = MainClass.ConnObj();
				if (sqlConnection.State != ConnectionState.Open)
				{
					sqlConnection.Open();
				}
				SqlCommand sqlCommand;
				sqlCommand = new SqlCommand("SELECT TOP 1 * FROM inv WHERE id = @InvoiceNo and branch=@branch and inv_type=@inv_type and proc_type=@proc_type and PaymentStatus=1 and IS_Deleted=0 ", sqlConnection);
				sqlCommand.Parameters.AddWithValue("@InvoiceNo", Inv.InvoiceNo);
				sqlCommand.Parameters.AddWithValue("@branch", Inv.Branch);
				sqlCommand.Parameters.AddWithValue("@inv_type", Convert.ToInt32((int)Inv.InvoiceType));
				sqlCommand.Parameters.AddWithValue("@proc_type", Inv.ProcType);
				SqlDataReader sqlDataReader;
				sqlDataReader = sqlCommand.ExecuteReader();
				checked
				{
					if (sqlDataReader.Read())
					{
						int num;
						num = 0;
						if (Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlDataReader["cust_id"])) != Inv.Customer)
						{
							num++;
						}
						if (Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlDataReader["pay_type"])) != Inv.PayType)
						{
							num++;
						}
						if (DateTime.Compare(Convert.ToDateTime(RuntimeHelpers.GetObjectValue(sqlDataReader["date"])), Inv.InvDate) != 0)
						{
							num++;
						}
						if (Convert.ToDouble(Convert.ToDecimal(RuntimeHelpers.GetObjectValue(sqlDataReader["tot_net"]))) != Inv.Net)
						{
							num++;
						}
						if (Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlDataReader["branch"])) != Inv.Branch)
						{
							num++;
						}
						if (num < 3)
						{
							result = false;
						}
					}
					sqlDataReader.Close();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static void GetInvoiceGlobalID(ref string InvGlobalID, ref int AutoIncrementID)
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
				AutoIncrementID = (int)Convert.ToInt64(RuntimeHelpers.GetObjectValue(new SqlCommand("select ISNULL(MAX(proc_id), 0) from Inv", sqlConnection).ExecuteScalar()));
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
				while (Convert.ToDouble(Operators.ConcatenateObject("", new SqlCommand("Select COUNT(*) from inv where InvGlobalID= '" + InvGlobalID + "'", sqlConnection).ExecuteScalar())) > 0.0);
				if (sqlConnection.State != ConnectionState.Closed)
				{
					sqlConnection.Close();
				}
			}
		}

		public static void GetInvoiceCloudID(ref string CloudID, int InvType, int ProcType, string CloudSerial, string ReInvCloudSerial)
		{
			if (ProcType == 3 || ProcType == 4)
			{
				CloudID = "";
				return;
			}
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			if (sqlConnection.State != ConnectionState.Open)
			{
				sqlConnection.Open();
			}
			new SqlCommand();
			ClsInvoice clsInvoice;
			clsInvoice = new ClsInvoice(Sync.APIUrl, "");
			string text;
			text = "";
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter("select CloudID from inv where inv_type=" + Conversions.ToString(InvType) + " and proc_type=" + Conversions.ToString(ProcType) + " and branch=" + Conversions.ToString(MainClass.BranchNo) + " order by id desc", sqlConnection);
			DataTable dataTable;
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			if (dataTable.Rows.Count > 0)
			{
				if (Conversions.ToBoolean(Operators.AndObject(dataTable.Rows[0]["CloudID"] != DBNull.Value, Operators.CompareObjectNotEqual(dataTable.Rows[0]["CloudID"], "", TextCompare: false))))
				{
					text = Conversions.ToString(dataTable.Rows[0]["CloudID"]);
				}
			}
			else if (MainClass.CheckForInternetConnection())
			{
				int invType;
				invType = 1;
				if (InvType == 3 && ProcType == 1)
				{
					invType = 1;
				}
				else if (InvType == 3 && ProcType == 2)
				{
					invType = 2;
				}
				text = clsInvoice.GetLastSerialInvoice(invType, User.CurrentCloudUser, CloudSerial, ReInvCloudSerial);
			}
			else if (InvType == 3 && ProcType == 1)
			{
				CloudID = CloudSerial + ".0";
			}
			else if (InvType == 3 && ProcType == 2)
			{
				CloudID = ReInvCloudSerial + ".0";
			}
			text = text.Substring(4);
			int num;
			num = ((Operators.CompareString(text, "", TextCompare: false) != 0) ? Convert.ToInt32(text) : 0);
			do
			{
				num = checked(num + 1);
				if ((Sync.ActiveSync & (Sync.BranchType == 4)) | InvoiceOper.CheckCloudID())
				{
					if (InvType == 3 && ProcType == 1)
					{
						CloudID = CloudSerial + "." + Conversions.ToString(num);
					}
					else if (InvType == 3 && ProcType == 2)
					{
						CloudID = ReInvCloudSerial + "." + Conversions.ToString(num);
					}
				}
				else
				{
					CloudID = "";
				}
			}
			while (Convert.ToDouble(Operators.ConcatenateObject("", new SqlCommand("Select COUNT(*) from inv where CloudID=N'" + CloudID + "' and inv_type=" + Conversions.ToString(InvType) + " and proc_type=" + Conversions.ToString(ProcType), sqlConnection).ExecuteScalar())) > 0.0);
			if (sqlConnection.State != ConnectionState.Closed)
			{
				sqlConnection.Close();
			}
		}

		public static int GetAutoIncrementID()
		{
			int result;
			try
			{
				SqlConnection sqlConnection;
				sqlConnection = MainClass.ConnObj();
				if (sqlConnection.State != ConnectionState.Open)
				{
					sqlConnection.Open();
				}
				result = Convert.ToInt32(RuntimeHelpers.GetObjectValue(new SqlCommand("SELECT ISNULL(MAX(proc_id), 0) + 1 FROM Inv", sqlConnection).ExecuteScalar()));
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = 1;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public Invoice BindInvoByID(string GID)
		{
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			if (sqlConnection.State != ConnectionState.Open)
			{
				sqlConnection.Open();
			}
			SqlDataReader sqlDataReader;
			sqlDataReader = new SqlCommand("select * from Inv where  InvGlobalID=N'" + GID + "'", sqlConnection).ExecuteReader();
			if (sqlDataReader.HasRows)
			{
				sqlDataReader.Read();
				Invoice invoice;
				invoice = new Invoice();
				invoice.AutoIncrementID = Conversions.ToInteger(Operators.ConcatenateObject("", sqlDataReader["proc_id"]));
				invoice.InvGlobalID = Conversions.ToString(Operators.ConcatenateObject("", sqlDataReader["InvGlobalID"]));
				invoice.CloudID = Conversions.ToString(Operators.ConcatenateObject("", sqlDataReader["CloudID"]));
				invoice.UUID = sqlDataReader["UUID"].ToString().Trim() ?? "";
				invoice.InvoiceHash = sqlDataReader["InvoiceHash"].ToString().Trim() ?? "";
				if (sqlDataReader["InvCombinedId"] != DBNull.Value)
				{
					invoice.InvCombinedId = Conversions.ToString(sqlDataReader["InvCombinedId"]);
				}
				else
				{
					invoice.InvCombinedId = string.Concat(Conversions.ToString(invoice.Branch) + Conversions.ToString((int)invoice.InvoiceType), Conversions.ToString(invoice.ProcType), Conversions.ToString(invoice.InvoiceNo));
				}
				invoice.InvoiceNo = checked((int)Math.Round(Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["id"]))));
				invoice.InvoiceType = (InvoiceType)checked((int)Math.Round(Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["Inv_type"]))));
				InvoiceObj invoiceObj;
				invoiceObj = new InvoiceObj((int)invoice.InvoiceType, invoice.ProcType);
				invoice.Bank = Conversions.ToInteger(sqlDataReader["bank"]);
				checked
				{
					invoice.ProcType = (int)Math.Round(Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["proc_type"])));
					invoice.OrderNo = -1;
					invoice.OrderType = Conversions.ToInteger(sqlDataReader["OrderType"]);
					invoice.InvDate = Conversions.ToDate(sqlDataReader["date"]);
					invoice.User = (int)Math.Round(Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["sales_emp"])));
					invoice.Branch = (int)Math.Round(Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["branch"])));
					invoice.DistBranch = Sync.DistBranch;
					invoice.ClientCode = Sync.ClientCode;
					invoice.BranchType = Sync.BranchType;
					invoice.Received = false;
					invoice.Treasury = (int)Math.Round(Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["stock"])));
					invoice.Saleman = (int)Math.Round(Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["salesman"])));
					invoice.Total = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["InvTotal"]));
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
						else if (Conversions.ToBoolean(Operators.AndObject(num == 3, Operators.CompareObjectEqual(sqlDataReader["proc_type"], 2, TextCompare: false))))
						{
							num = 22;
						}
						invoice.EntryGlobalID = InvoiceOper.GetEntryGlobalID(Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlDataReader["EntryID"])), num, Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlDataReader["branch"])));
					}
					else
					{
						invoice.EntryGlobalID = Conversions.ToString(-1);
					}
					invoice.Delivery = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["AdditionsTot"]));
					invoice.SumPrice = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["InvTotal"]));
					invoice.VAT = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["tax"]));
					invoice.TotalWithholdingTax = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["TotalWithholdingTax"]));
					invoice.Net = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["tot_net"]));
					invoice.Discount = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["minus"]));
					invoice.Additions = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["AdditionsTot"]));
					invoice.Insurance = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["Insurance"]));
					invoice.Paycash = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["cash"]));
					invoice.PayATM = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["visa"]));
					invoice.PayType = (int)Math.Round(Convert.ToDouble(Operators.ConcatenateObject("", sqlDataReader["pay_type"])));
					if (invoice.PayType == 1)
					{
						invoice.Paycash = invoice.Net;
						invoice.PayATM = 0.0;
						invoice.Paid = invoice.Net;
					}
					else if (invoice.PayType == 2)
					{
						invoice.Paycash = 0.0;
						invoice.PayATM = invoice.Net;
						invoice.Paid = invoice.Net;
					}
					invoice.Remainder = Convert.ToDouble(Operators.ConcatenateObject("", sqlDataReader["paid"])) - Convert.ToDouble(Operators.ConcatenateObject("", sqlDataReader["tot_net"]));
					invoice.Customer = (int)Math.Round(Convert.ToDouble(Operators.ConcatenateObject("", sqlDataReader["cust_id"])));
					invoice.InvNote = Conversions.ToString(Operators.ConcatenateObject("", sqlDataReader["notes"]));
					invoice.IsDeleted = Conversions.ToBoolean(sqlDataReader["IS_Deleted"]);
					invoice.VATperc = invoiceObj.VAT;
					invoice.ReffNo = Conversions.ToString(sqlDataReader["Reff_No"]);
					invoice.RefDate = Conversions.ToDate(sqlDataReader["Reff_date"]);
					invoice.Store = Conversions.ToInteger(Operators.ConcatenateObject("", sqlDataReader["safe"]));
					invoice.InvNote = Conversions.ToString(Operators.ConcatenateObject("", sqlDataReader["notes"]));
					invoice.TotDiscount = invoice.Discount;
					if (invoice.ProcType == 1)
					{
						invoice.InvAccCode = invoiceObj.InvAcc;
					}
					else
					{
						invoice.InvAccCode = invoiceObj.InvReturnAcc;
					}
					invoice.InvCCcode = Conversions.ToString(-1);
					sqlDataReader.Close();
					List<Item> list;
					list = new List<Item>();
					List<InvoiceItem> invoiceItems;
					invoiceItems = new List<InvoiceItem>();
					SqlDataAdapter sqlDataAdapter;
					sqlDataAdapter = new SqlDataAdapter("select * from Inv_Sub where InvGlobalID=N'" + invoice.InvGlobalID + "'", sqlConnection);
					DataTable dataTable;
					dataTable = new DataTable();
					sqlDataAdapter.Fill(dataTable);
					int num2;
					num2 = dataTable.Rows.Count - 1;
					for (int i = 0; i <= num2; i++)
					{
						Item item;
						item = new Item();
						item.ClientCode = Sync.ClientCode;
						item.InvGlobalID = invoice.InvGlobalID;
						item.AutoIncrementID = invoice.AutoIncrementID;
						item.Name = Common.GetItemName(Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["ItemId"])));
						item.ItemNo = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["ItemId"]));
						item.Code = Common.GetItemCode(Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["ItemId"])));
						item.UnitName = Common.GetUnitName(Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["unit"])));
						item.StoreName = Common.GetStoreName(Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["store"])));
						item.ProcType = invoice.ProcType;
						item.Description = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["Description"]));
						item.Quantity = Conversions.ToDouble(Operators.ConcatenateObject("", dataTable.Rows[i]["val1"]));
						item.UnitEquality = Conversions.ToDouble(Operators.ConcatenateObject("", dataTable.Rows[i]["UnitEquality"]));
						item.PrimaryQnty = Conversions.ToDouble(Operators.ConcatenateObject("", dataTable.Rows[i]["val"]));
						item.Unit = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["unit"]));
						item.Price = Conversions.ToDouble(Operators.ConcatenateObject("", dataTable.Rows[i]["exchange_price"]));
						item.AvegCost = Conversions.ToDouble(Operators.ConcatenateObject("", dataTable.Rows[i]["AvrgCost"]));
						item.Vat = Conversions.ToDouble(Operators.ConcatenateObject("", dataTable.Rows[i]["taxval"]));
						item.VatPerc = Conversions.ToDouble(Operators.ConcatenateObject("", dataTable.Rows[i]["taxperc"]));
						item.Barcode = "";
						item.ItemDiscount = Conversions.ToDouble(dataTable.Rows[i]["discount"]);
						item.ValiableStock = Conversions.ToDouble(dataTable.Rows[i]["CurrentQnty"]);
						item.Store = Conversions.ToDouble(Operators.ConcatenateObject("", dataTable.Rows[i]["store"]));
						item.WithholdingTax = Conversions.ToDouble(Operators.ConcatenateObject("", dataTable.Rows[i]["WithholdingTax"]));
						item.WithholdingTaxPerc = Conversions.ToDouble(Operators.ConcatenateObject("", dataTable.Rows[i]["WithholdingTaxPerc"]));
						invoice.TotDiscount += item.ItemDiscount;
						if (Operators.ConditionalCompareObjectNotEqual(Operators.ConcatenateObject("", dataTable.Rows[i]["expire_date"]), "", TextCompare: false))
						{
							item.ExpireDate = Conversions.ToDate(Operators.ConcatenateObject("", dataTable.Rows[i]["expire_date"]));
						}
						item.Note = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["notes"]));
						item.ProductId = 0;
						list.Add(item);
					}
					invoice.Items = list;
					invoice.InvoiceItems = invoiceItems;
					sqlDataAdapter = new SqlDataAdapter("select CCcode from Entry_sub where branch=" + Conversions.ToString(MainClass.BranchNo) + " and EntryGlobalID=N'" + invoice.EntryGlobalID + "' and acc_no=N'" + invoice.InvAccCode + "'", sqlConnection);
					dataTable = new DataTable();
					sqlDataAdapter.Fill(dataTable);
					if (dataTable.Rows.Count > 0 && dataTable.Rows[0]["CCcode"] != DBNull.Value)
					{
						invoice.InvCCcode = Conversions.ToString(Convert.ToInt32(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["CCcode"])));
					}
					return invoice;
				}
			}
			return null;
		}

		public static object ExtractInvDiscount(double InvTotal, double ItemsTotal, double InvDiscount)
		{
			return ItemsTotal / InvTotal * InvDiscount;
		}

		public static object GetInvDiscountByPercentage(double InvTotal, double InvDiscountPerc)
		{
			return InvDiscountPerc / 100.0 * InvTotal;
		}

		public static double GetDiscountPercentage(double InvTotal, double InvDiscount)
		{
			return InvDiscount / InvTotal * 100.0;
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

		public static void BindingInvoice(ref InvoiceDGV Invo, string sqlstr)
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
						Invo.EntryGlobalID = InvoiceOper.GetEntryGlobalID(Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlDataReader["EntryID"])), num, Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlDataReader["branch"])));
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
					Invo.OrderType = Conversions.ToInteger(sqlDataReader["OrderType"]);
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
					if (sqlDataReader["ZatcaSent"] != DBNull.Value)
					{
						Invo.ZatcaSent = Conversions.ToBoolean(sqlDataReader["ZatcaSent"]);
					}
					else
					{
						Invo.ZatcaSent = false;
					}
					sqlDataReader.Close();
					SqlDataAdapter sqlDataAdapter;
					sqlDataAdapter = new SqlDataAdapter("SELECT \r\n    Inv.id ,\r\n    Inv.proc_type ,\r\n    Inv.ItemId,\r\n    Inv.Description,\r\n    Inv.notes ,\r\n    Inv.unit ,\r\n    Inv.val ,\r\n    Inv.val1 ,\r\n    Inv.exchange_price ,\r\n    Inv.discount ,\r\n    Inv.ItemPriceWithoutVAT ,\r\n    Inv.AvrgCost ,\r\n    Inv.taxval,\r\n    Inv.taxperc ,\r\n    Inv.CurrentQnty ,\r\n    Inv.store ,\r\n    Inv.expire_date ,\r\n    Inv.WithholdingTaxPerc ,\r\n    Inv.WithholdingTax,\r\n    ISNULL(Inv.ItemCostCenter, '') AS ItemCostCenter,\r\n    ISNULL(Inv.ItemAdditionalTaxPerc, 0) AS ItemAdditionalTaxPerc,\r\n    ISNULL(Inv.ItemAdditionalTax, 0) AS ItemAdditionalTax,\r\n    I.name AS ItemName,\r\n    I.nameEN AS ItemNameEn,\r\n    I.code AS ItemCode,\r\n    I.ItemProperty,\r\n    ISNULL(I.ItemType,0) AS ItemType,\r\n    ISNULL(I.is_extra_tax_applied,0) AS isExtraTaxApplied,\r\n    U.name AS UnitName,\r\n    ISNULL(U.UnitCode, '') AS UnitCode,\r\n    Max(IU.barcode) AS UnitBarcode,\r\n    I.EgyCodeType,\r\n    I.EgyItemCode\t\t\t\t\t\r\nFROM \r\n    Inv_Sub AS Inv\r\nLEFT JOIN \r\n    Items AS I ON Inv.ItemId = I.id \r\nLEFT JOIN \r\n    Units AS U ON Inv.unit = U.id \r\nLEFT JOIN \r\n    ItemUnits AS IU ON Inv.ItemId = IU.ItemId AND Inv.unit = IU.unit\r\nWHERE \r\n    Inv.InvGlobalID = N'" + Invo.InvGlobalID + "' \r\n    AND Inv.ProductId = 0 \r\n\r\n\tgroup by \r\n\tInv.id  ,\r\n    Inv.proc_type  ,\r\n    Inv.ItemId,\r\n    Inv.Description,\r\n    Inv.notes ,\r\n    Inv.unit ,\r\n    Inv.val ,\r\n    Inv.val1 ,\r\n    Inv.exchange_price,\r\n    Inv.discount ,\r\n    Inv.ItemPriceWithoutVAT ,\r\n    Inv.AvrgCost ,\r\n    Inv.taxval ,\r\n    Inv.taxperc ,\r\n    Inv.CurrentQnty ,\r\n    Inv.store ,\r\n    Inv.expire_date,\r\n    Inv.WithholdingTaxPerc ,\r\n    Inv.WithholdingTax ,\r\n    ISNULL(Inv.ItemAdditionalTaxPerc,0) ,\r\n    ISNULL(Inv.ItemAdditionalTax,0) ,\r\n    ISNULL(Inv.ItemCostCenter, '') ,\r\n    I.name ,\r\n    I.nameEN ,\r\n    I.code ,\r\n    I.ItemProperty,\r\n    ISNULL(I.ItemType,0),\r\n    ISNULL(I.is_extra_tax_applied,0),\r\n    U.name ,\r\n    ISNULL(U.UnitCode, '') ,\r\n    I.EgyCodeType,\r\n    I.EgyItemCode\t\t\t\t\t\r\nORDER BY \r\n    Inv.id;\r\n", sqlConnection);
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
									InvoiceDGV obj;
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
									InvoiceOper.BindingItemDetails(ref Itm);
									InvoiceOper.glassOtions(ref Itm);
									Invo.InvoiceItems.Add(Itm);
								}
								catch (Exception projectError4)
								{
									ProjectData.SetProjectError(projectError4);
									ProjectData.ClearProjectError();
								}
							}
						}
						InvoiceOper.BindingCostDetaile(ref Invo);
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

		public List<InvoiceDGV> BindingListOfInvoices(string sqlstr)
		{
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			if (sqlConnection.State != ConnectionState.Open)
			{
				sqlConnection.Open();
			}
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter(sqlstr, sqlConnection);
			DataTable dataTable;
			dataTable = new DataTable();
			new DataTable();
			List<InvoiceDGV> list;
			list = new List<InvoiceDGV>();
			sqlDataAdapter.Fill(dataTable);
			checked
			{
				if (dataTable.Rows.Count > 0)
				{
					int num;
					num = dataTable.Rows.Count - 1;
					for (int i = 0; i <= num; i++)
					{
						try
						{
							InvoiceDGV invoiceDGV;
							invoiceDGV = new InvoiceDGV();
							invoiceDGV.ISNew = false;
							invoiceDGV.IsPrinted = true;
							invoiceDGV.IsLoaded = false;
							invoiceDGV.InvGlobalID = Conversions.ToString(dataTable.Rows[i]["InvGlobalID"]);
							invoiceDGV.InvGlobalID = Conversions.ToString(dataTable.Rows[i]["CloudID"]);
							if (Operators.ConditionalCompareObjectNotEqual(dataTable.Rows[i]["EntryID"], -1, TextCompare: false))
							{
								invoiceDGV.EntryGlobalID = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(dataTable.Rows[i]["branch"], "-"), ""), dataTable.Rows[i]["EntryID"]));
							}
							else
							{
								invoiceDGV.EntryGlobalID = Conversions.ToString(-1);
							}
							invoiceDGV.ProcType = Conversions.ToInteger(dataTable.Rows[i]["proc_type"]);
							invoiceDGV.InvoiceNo = (int)Math.Round(Convert.ToDouble(Operators.ConcatenateObject("", dataTable.Rows[i]["id"])));
							invoiceDGV.InvDate = Conversions.ToDate(Operators.ConcatenateObject("", dataTable.Rows[i]["date"]));
							invoiceDGV.InvTime = Conversions.ToDate(Convert.ToDateTime(Operators.ConcatenateObject("", dataTable.Rows[i]["date"])).ToShortTimeString());
							invoiceDGV.InvoiceType = unchecked((InvoiceType)Conversions.ToInteger(dataTable.Rows[i]["inv_type"]));
							invoiceDGV.AdditionalCost = Conversions.ToDecimal(dataTable.Rows[i]["AdditionalCost"]);
							invoiceDGV.InvoiceStatus = Conversions.ToInteger(dataTable.Rows[i]["InvoiceStatus"]);
							invoiceDGV.Treasury = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["stock"]));
							invoiceDGV.Store = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["safe"]));
							invoiceDGV.InvDiscount = Conversions.ToDouble(Operators.ConcatenateObject("", dataTable.Rows[i]["minus"]));
							invoiceDGV.TotDiscount = invoiceDGV.InvDiscount;
							invoiceDGV.VAT = Conversions.ToDouble(Operators.ConcatenateObject("", dataTable.Rows[i]["tax"]));
							invoiceDGV.TotalWithholdingTax = Conversions.ToDouble(Operators.ConcatenateObject("", dataTable.Rows[i]["TotalWithholdingTax"]));
							invoiceDGV.Net = Conversions.ToDouble(Operators.ConcatenateObject("", dataTable.Rows[i]["tot_net"]));
							invoiceDGV.Total = invoiceDGV.Net - invoiceDGV.VAT;
							invoiceDGV.Paid = Conversions.ToDouble(Operators.ConcatenateObject("", dataTable.Rows[i]["paid"]));
							invoiceDGV.Remainder = Convert.ToDouble(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["paid"])) - Convert.ToDouble(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["tot_net"]));
							invoiceDGV.InvNote = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["notes"]));
							try
							{
								invoiceDGV.RefDate = Conversions.ToDate(Operators.ConcatenateObject("", dataTable.Rows[i]["Reff_date"]));
							}
							catch (Exception projectError)
							{
								ProjectData.SetProjectError(projectError);
								ProjectData.ClearProjectError();
							}
							invoiceDGV.ReffNo = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["Reff_No"]));
							invoiceDGV.PayType = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["pay_type"]));
							invoiceDGV.Bank = Conversions.ToInteger(dataTable.Rows[i]["bank"]);
							invoiceDGV.Customer = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["cust_id"]));
							invoiceDGV.Saleman = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["salesman"]));
							invoiceDGV.User = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["sales_emp"]));
							sqlDataAdapter = new SqlDataAdapter("select * from Inv_Sub where InvGlobalID=N'" + invoiceDGV.InvGlobalID + "' and ProductId=0 order by id", sqlConnection);
							DataTable dataTable2;
							dataTable2 = new DataTable();
							sqlDataAdapter.Fill(dataTable2);
							if (dataTable2.Rows.Count > 0)
							{
								int num2;
								num2 = dataTable2.Rows.Count - 1;
								for (int j = 0; j <= num2; j++)
								{
									try
									{
										InvoiceItem invoiceItem;
										invoiceItem = new InvoiceItem();
										invoiceItem.ItemRowIndex = i + 1;
										invoiceItem.ItemCode = ItemOper.GetItemCode((int)Math.Round(Convert.ToDouble(RuntimeHelpers.GetObjectValue(dataTable2.Rows[j]["ItemId"]))));
										invoiceItem.ItemId = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable2.Rows[j]["ItemId"]));
										invoiceItem.ItemName = ItemOper.GetItemName((int)Math.Round(Convert.ToDouble(RuntimeHelpers.GetObjectValue(dataTable2.Rows[j]["ItemId"]))));
										invoiceItem.Description = Conversions.ToString(Operators.ConcatenateObject("", dataTable2.Rows[j]["Description"]));
										invoiceItem.UnitName = Common.GetUnitName(Convert.ToInt32(RuntimeHelpers.GetObjectValue(dataTable2.Rows[j]["unit"])));
										invoiceItem.UnitID = Conversions.ToInteger(dataTable2.Rows[j]["unit"]);
										invoiceItem.ItemPrimaryQnty = Conversions.ToDouble(Convert.ToString(RuntimeHelpers.GetObjectValue(dataTable2.Rows[j]["val"])));
										invoiceItem.ItemQuantity = Conversions.ToDouble(Convert.ToString(RuntimeHelpers.GetObjectValue(dataTable2.Rows[j]["val1"])));
										invoiceItem.UnitEquality = Common.DivisionOperation(new decimal(invoiceItem.ItemPrimaryQnty), new decimal(invoiceItem.ItemQuantity));
										invoiceItem.ItemPrice = Conversions.ToDouble(dataTable2.Rows[j]["exchange_price"]);
										invoiceItem.ItemSumPrice = invoiceItem.ItemQuantity * invoiceItem.ItemPrice;
										invoiceItem.ItemDiscount = Conversion.Val(Operators.ConcatenateObject("", dataTable2.Rows[j]["discount"]));
										invoiceItem.ItemDiscountPerc = Common.DivisionOperation(new decimal(invoiceItem.ItemDiscount), new decimal(invoiceItem.ItemSumPrice)) * 100.0;
										invoiceDGV.TotDiscount += invoiceItem.ItemDiscount;
										invoiceDGV.SumPrice += invoiceItem.ItemSumPrice;
										if ((dataTable2.Rows[j]["AvrgCost"] == null) | (dataTable2.Rows[j]["AvrgCost"] == DBNull.Value))
										{
											invoiceItem.ItemCost = 0.0;
										}
										else
										{
											invoiceItem.ItemCost = Conversions.ToDouble(dataTable2.Rows[j]["AvrgCost"]);
										}
										invoiceItem.ItemTotalPrice = invoiceItem.ItemSumPrice - invoiceItem.ItemDiscount;
										invoiceItem.ItemVat = Conversions.ToDouble(dataTable2.Rows[j]["taxval"]);
										if (invoiceDGV.PriceIncVAT)
										{
											invoiceItem.ItemTotalPrice -= invoiceItem.ItemVat;
										}
										invoiceItem.ItemVatPerc = Conversions.ToDouble(dataTable2.Rows[j]["taxperc"]);
										invoiceItem.WithholdingTax = Conversions.ToDouble(dataTable2.Rows[j]["WithholdingTax"]);
										invoiceItem.WithholdingTaxPerc = Conversions.ToDouble(dataTable2.Rows[j]["WithholdingTaxPerc"]);
										invoiceItem.ItemNetPrice = invoiceItem.ItemTotalPrice + invoiceItem.ItemVat;
										invoiceItem.ValiableInvertory = Conversions.ToDouble(dataTable2.Rows[j]["CurrentQnty"]);
										try
										{
											if (dataTable2.Rows[j]["store"] != null && Conversion.Val(RuntimeHelpers.GetObjectValue(dataTable2.Rows[j]["store"])) > 0.0)
											{
												invoiceItem.InvertoryId = Convert.ToInt32(RuntimeHelpers.GetObjectValue(dataTable2.Rows[j]["store"]));
												invoiceItem.InvertoryName = Common.GetStoreName(invoiceItem.InvertoryId);
											}
										}
										catch (Exception projectError2)
										{
											ProjectData.SetProjectError(projectError2);
											ProjectData.ClearProjectError();
										}
										invoiceDGV.InvoiceItems.Add(invoiceItem);
									}
									catch (Exception projectError3)
									{
										ProjectData.SetProjectError(projectError3);
										ProjectData.ClearProjectError();
									}
								}
							}
							list.Add(invoiceDGV);
						}
						catch (Exception projectError4)
						{
							ProjectData.SetProjectError(projectError4);
							ProjectData.ClearProjectError();
						}
					}
				}
				return list;
			}
		}

		public List<InvoiceTxt> BindingListOfInvoices1(string sqlstr, bool withItems, DateTime Date1, DateTime date2)
		{
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			if (sqlConnection.State != ConnectionState.Open)
			{
				sqlConnection.Open();
			}
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter(sqlstr, sqlConnection);
			sqlDataAdapter.SelectCommand.Parameters.Add("@date1", SqlDbType.DateTime).Value = Date1;
			sqlDataAdapter.SelectCommand.Parameters.Add("@date2", SqlDbType.DateTime).Value = date2;
			DataTable dataTable;
			dataTable = new DataTable();
			new DataTable();
			List<InvoiceTxt> list;
			list = new List<InvoiceTxt>();
			new InvoiceObj(2, 1);
			string paymentTxt;
			paymentTxt = "";
			sqlDataAdapter.Fill(dataTable);
			checked
			{
				if (dataTable.Rows.Count > 0)
				{
					int num;
					num = dataTable.Rows.Count - 1;
					for (int i = 0; i <= num; i++)
					{
						try
						{
							InvoiceTxt invoiceTxt;
							invoiceTxt = new InvoiceTxt();
							InvoiceObj invoiceObj;
							invoiceObj = new InvoiceObj(Conversions.ToInteger(dataTable.Rows[i]["inv_type"]), Conversions.ToInteger(dataTable.Rows[i]["proc_type"]));
							SqlDataAdapter sqlDataAdapter2;
							sqlDataAdapter2 = new SqlDataAdapter(Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject("SELECT ISNULL(tax_no, '') AS tax_no FROM customers WHERE id=", dataTable.Rows[i]["cust_id"]), "")), sqlConnection);
							DataTable dataTable2;
							dataTable2 = new DataTable();
							sqlDataAdapter2.Fill(dataTable2);
							short taxType;
							if (dataTable2.Rows.Count > 0)
							{
								string text;
								text = dataTable2.Rows[0]["tax_no"].ToString();
								taxType = unchecked((short)((Operators.CompareString(text, "", TextCompare: false) == 0) ? 1 : ((!Versioned.IsNumeric(text)) ? 2 : ((Convert.ToDouble(text) <= 0.0) ? 1 : 2))));
							}
							else
							{
								taxType = 1;
							}
							string invoiceType;
							invoiceType = InvoiceOper.GetInvoiceType((int)Math.Round(Conversion.Val(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["Inv_type"]))), (int)Math.Round(Conversion.Val(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["Proc_type"]))), (int)Math.Round(Conversion.Val(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["pay_type"]))), taxType);
							invoiceTxt.ISNew = false;
							invoiceTxt.IsPrinted = true;
							invoiceTxt.IsLoaded = false;
							invoiceTxt.InvGlobalID = Conversions.ToString(dataTable.Rows[i]["InvGlobalID"]);
							invoiceTxt.CloudID = Conversions.ToString(dataTable.Rows[i]["CloudID"]);
							invoiceTxt.EntryGlobalID = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject(Operators.ConcatenateObject(dataTable.Rows[i]["branch"], "-"), ""), dataTable.Rows[i]["EntryID"]));
							invoiceTxt.ProcType = Conversions.ToInteger(dataTable.Rows[i]["proc_type"]);
							invoiceTxt.InvoiceNo = (int)Math.Round(Convert.ToDouble(Operators.ConcatenateObject("", dataTable.Rows[i]["id"])));
							invoiceTxt.InvDate = Conversions.ToDate(Operators.ConcatenateObject("", dataTable.Rows[i]["date"]));
							invoiceTxt.InvTime = Conversions.ToDate(invoiceTxt.InvDate.ToString("hh':'mm':'ss tt"));
							invoiceTxt.InvoiceTime = invoiceTxt.InvDate.ToString("hh':'mm':'ss tt");
							invoiceTxt.InvoiceType = unchecked((InvoiceType)Conversions.ToInteger(dataTable.Rows[i]["inv_type"]));
							invoiceTxt.InvoiceTypeTxt = invoiceType;
							invoiceTxt.AdditionalCost = Conversions.ToDecimal(dataTable.Rows[i]["AdditionalCost"]);
							invoiceTxt.InvoiceStatus = Conversions.ToInteger(dataTable.Rows[i]["InvoiceStatus"]);
							invoiceTxt.Treasury = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["stock"]));
							invoiceTxt.Store = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["safe"]));
							invoiceTxt.InvDiscount = Conversions.ToDouble(Operators.ConcatenateObject("", dataTable.Rows[i]["minus"]));
							invoiceTxt.TotDiscount = invoiceTxt.InvDiscount;
							invoiceTxt.VAT = Conversions.ToDouble(dataTable.Rows[i]["tax"]);
							invoiceTxt.Paid = Conversions.ToDouble(Operators.ConcatenateObject("", dataTable.Rows[i]["paid"]));
							invoiceTxt.Remainder = Convert.ToDouble(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["paid"])) - Convert.ToDouble(RuntimeHelpers.GetObjectValue(dataTable.Rows[i]["tot_net"]));
							invoiceTxt.InvNote = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["notes"]));
							invoiceTxt.VATperc = invoiceObj.VAT;
							invoiceTxt.ExtraVATPerc = invoiceObj.AdditionalTax;
							invoiceTxt.PriceIncVAT = invoiceObj.PriceIncVAT;
							invoiceTxt.BranchTxt = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["BranchName"]));
							try
							{
								invoiceTxt.RefDate = Conversions.ToDate(Operators.ConcatenateObject("", dataTable.Rows[i]["Reff_date"]));
							}
							catch (Exception projectError)
							{
								ProjectData.SetProjectError(projectError);
								ProjectData.ClearProjectError();
							}
							invoiceTxt.AutoIncrementID = i + 1;
							invoiceTxt.ReffNo = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["Reff_No"]));
							invoiceTxt.PayType = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["pay_type"]));
							if (Conversion.Val(Operators.ConcatenateObject("", dataTable.Rows[i]["pay_type"])) == -1.0)
							{
								paymentTxt = "آجل";
							}
							else if (Conversion.Val(Operators.ConcatenateObject("", dataTable.Rows[i]["pay_type"])) == 1.0)
							{
								paymentTxt = "نقدي";
							}
							else if (Conversion.Val(Operators.ConcatenateObject("", dataTable.Rows[i]["pay_type"])) == 2.0)
							{
								paymentTxt = "شبكة";
							}
							else if (Conversion.Val(Operators.ConcatenateObject("", dataTable.Rows[i]["pay_type"])) == 4.0)
							{
								paymentTxt = "متعدد";
							}
							invoiceTxt.PaymentTxt = paymentTxt;
							invoiceTxt.Bank = Conversions.ToInteger(dataTable.Rows[i]["bank"]);
							invoiceTxt.Customer = Conversions.ToInteger(dataTable.Rows[i]["cust_id"]);
							if (dataTable.Rows[i]["CustName"] == DBNull.Value)
							{
								invoiceTxt.ClientTxt = "";
							}
							else
							{
								invoiceTxt.ClientTxt = Conversions.ToString(dataTable.Rows[i]["CustName"]);
							}
							invoiceTxt.Saleman = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["salesman"]));
							invoiceTxt.SalesmanTxt = Conversions.ToString(Operators.ConcatenateObject("", dataTable.Rows[i]["salesman"]));
							invoiceTxt.User = Conversions.ToInteger(Operators.ConcatenateObject("", dataTable.Rows[i]["sales_emp"]));
							if (dataTable.Rows[i]["username"] == DBNull.Value)
							{
								invoiceTxt.UserTxt = "";
							}
							else
							{
								invoiceTxt.UserTxt = Conversions.ToString(dataTable.Rows[i]["username"]);
							}
							invoiceTxt.InvertoryName = Common.GetStoreName(Conversions.ToInteger(dataTable.Rows[i]["safe"]));
							if (!withItems)
							{
								invoiceTxt.TotDiscount = Conversions.ToDouble(Operators.AddObject(invoiceTxt.TotDiscount, dataTable.Rows[i]["ItemDiscount"]));
								invoiceTxt.SumPrice = Conversions.ToDouble(dataTable.Rows[i]["sumPrice"]);
							}
							invoiceTxt.FreeVATSales = Conversions.ToDouble(dataTable.Rows[i]["FreeVATSales"]);
							invoiceTxt.Total = Conversions.ToDouble(dataTable.Rows[i]["InvTotal"]);
							invoiceTxt.TotalWithholdingTax = Conversions.ToDouble(dataTable.Rows[i]["TotalWithholdingTax"]);
							invoiceTxt.Net = Conversions.ToDouble(dataTable.Rows[i]["tot_net"]);
							list.Add(invoiceTxt);
						}
						catch (Exception projectError2)
						{
							ProjectData.SetProjectError(projectError2);
							ProjectData.ClearProjectError();
						}
					}
				}
				return list;
			}
		}

		public static void BindingCostDetaile(ref InvoiceDGV Invo)
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

		public static void InsertFromInv(ref InvoiceDGV Invo, int Ptype)
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
				else
				{
					frmInvoiceSrch2.InvType = (int)Invo.InvoiceType;
				}
				frmInvoiceSrch2.ShowDialog();
				if (!(frmInvoiceSrch2.ISDone & (Operators.CompareString(frmInvoiceSrch2.InvGlobalID, "-1", TextCompare: false) != 0)))
				{
					return;
				}
				InvoiceOper.BindingInvoice(ref Invo, "select * from Inv where IS_Deleted=0 and InvGlobalID=N'" + frmInvoiceSrch2.InvGlobalID + "'");
				frmInvoiceItems frmInvoiceItems2;
				frmInvoiceItems2 = new frmInvoiceItems();
				MainClass.ApplyPermissionToForm(frmInvoiceItems2);
				MainClass.DoApplyUserSett(frmInvoiceItems2);
				frmInvoiceItems2.InvGlobalId = frmInvoiceSrch2.InvGlobalID;
				frmInvoiceItems2.InvType = (int)Invo.InvoiceType;
				frmInvoiceItems2.inv = Invo;
				if (Invo.ProcType != 2)
				{
					if (Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0)
					{
						frmInvoiceItems2.btnadd.Content = " إدراج";
						frmInvoiceItems2.btnAddall.Content = "إدراج الكل ";
					}
					else
					{
						frmInvoiceItems2.btnadd.Content = "Insert";
						frmInvoiceItems2.btnAddall.Content = "Insert all ";
					}
				}
				frmInvoiceItems2.ShowDialog();
				if (frmInvoiceItems2.ISDone)
				{
					Invo = frmInvoiceItems2.inv;
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		private static bool CheckCloudID()
		{
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			if (sqlConnection.State != ConnectionState.Open)
			{
				sqlConnection.Open();
			}
			new SqlCommand();
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter("select top 5 CloudID from inv where inv_type=3 AND proc_type=1 AND CloudID IS NOT NULL AND CloudID <> '' order by id desc", sqlConnection);
			DataTable dataTable;
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			if (dataTable.Rows.Count > 0)
			{
				if (Conversions.ToBoolean(Operators.AndObject(dataTable.Rows[0]["CloudID"] != DBNull.Value, Operators.CompareObjectNotEqual(dataTable.Rows[0]["CloudID"], "", TextCompare: false))))
				{
					return true;
				}
				return false;
			}
			return false;
		}

		public Invoice MappingInvoice(ref InvoiceDGV Inv)
		{
			Invoice invoice;
			invoice = new Invoice();
			InvoiceObj invoiceObj;
			invoiceObj = new InvoiceObj((int)Inv.InvoiceType, Inv.ProcType);
			if (InvoiceOper.VerifyIsNewInvoice(Inv) & (Inv.InvoiceType != InvoiceType.SafesTransferTo) & (Inv.InvoiceType != InvoiceType.Purchase))
			{
				Inv.ISNew = true;
			}
			InvoiceDGV invoiceDGV;
			if (Inv.ISNew)
			{
				Inv.InvoiceNo = InvoiceOper.InvoiceNo((int)Inv.InvoiceType, Inv.ProcType, invoiceObj.Prefixe);
				Inv.OrderNo = InvoiceOper.OrderNo((int)Inv.InvoiceType, Inv.ProcType);
				Inv.InvCombinedId = string.Concat(MainClass.BranchCode + Inv.InvoiceCode, Conversions.ToString(Inv.ProcType), Conversions.ToString(Inv.InvoiceNo));
				int EntryNo;
				string EntryGlobalId;
				if ((Inv.InvoiceType == InvoiceType.Purchase) | (Inv.InvoiceType == InvoiceType.Sale))
				{
					InvoiceDGV obj;
					obj = Inv;
					EntryGlobalId = obj.EntryGlobalID;
					EntryNo = 0;
					EntryOper.GetEntryGlobalID(ref EntryGlobalId, ref EntryNo);
					obj.EntryGlobalID = EntryGlobalId;
				}
				else if ((Inv.InvoiceType == InvoiceType.POS) & ((Inv.PayType == 5) | (Inv.PayType == -1) | (Inv.PayType == 7)))
				{
					InvoiceDGV obj2;
					obj2 = Inv;
					EntryGlobalId = obj2.EntryGlobalID;
					EntryNo = 0;
					EntryOper.GetEntryGlobalID(ref EntryGlobalId, ref EntryNo);
					obj2.EntryGlobalID = EntryGlobalId;
				}
				else if (Inv.InvoiceType == (InvoiceType)21)
				{
					InvoiceDGV obj3;
					obj3 = Inv;
					EntryGlobalId = obj3.EntryGlobalID;
					EntryNo = 0;
					EntryOper.GetEntryGlobalID(ref EntryGlobalId, ref EntryNo);
					obj3.EntryGlobalID = EntryGlobalId;
				}
				else if (Inv.InvoiceType == InvoiceType.ReturnSales)
				{
					InvoiceDGV obj4;
					obj4 = Inv;
					EntryGlobalId = obj4.EntryGlobalID;
					EntryNo = 0;
					EntryOper.GetEntryGlobalID(ref EntryGlobalId, ref EntryNo);
					obj4.EntryGlobalID = EntryGlobalId;
				}
				else
				{
					Inv.EntryGlobalID = "-1";
				}
				InvoiceDGV obj5;
				obj5 = Inv;
				EntryGlobalId = obj5.InvGlobalID;
				EntryNo = (invoiceDGV = Inv).AutoIncrementID;
				InvoiceOper.GetInvoiceGlobalID(ref EntryGlobalId, ref EntryNo);
				invoiceDGV.AutoIncrementID = EntryNo;
				obj5.InvGlobalID = EntryGlobalId;
				if (Sync.ActiveSync & (Sync.BranchType == 4) & (Inv.InvoiceType == InvoiceType.POS) & (Inv.ProcType != 3) & (Inv.ProcType != 4))
				{
					EntryGlobalId = (invoiceDGV = Inv).CloudID;
					InvoiceOper.GetInvoiceCloudID(ref EntryGlobalId, (int)Inv.InvoiceType, Inv.ProcType, invoiceObj.CloudSerial, invoiceObj.ReInvCloudSerial);
					invoiceDGV.CloudID = EntryGlobalId;
				}
				else if (InvoiceOper.CheckCloudID())
				{
					EntryGlobalId = (invoiceDGV = Inv).CloudID;
					InvoiceOper.GetInvoiceCloudID(ref EntryGlobalId, (int)Inv.InvoiceType, Inv.ProcType, invoiceObj.CloudSerial, invoiceObj.ReInvCloudSerial);
					invoiceDGV.CloudID = EntryGlobalId;
				}
				else
				{
					Inv.CloudID = "";
				}
			}
			else
			{
				if (Operators.CompareString(Inv.EntryGlobalID, "1-0", TextCompare: false) == 0)
				{
					if ((Inv.InvoiceType == InvoiceType.Purchase) | (Inv.InvoiceType == InvoiceType.Sale))
					{
						InvoiceDGV obj6;
						obj6 = Inv;
						string EntryGlobalId;
						EntryGlobalId = obj6.EntryGlobalID;
						int EntryNo;
						EntryNo = 0;
						EntryOper.GetEntryGlobalID(ref EntryGlobalId, ref EntryNo);
						obj6.EntryGlobalID = EntryGlobalId;
					}
					else if ((Inv.InvoiceType == InvoiceType.POS) & ((Inv.PayType == 5) | (Inv.PayType == -1)))
					{
						InvoiceDGV obj7;
						obj7 = Inv;
						string EntryGlobalId;
						EntryGlobalId = obj7.EntryGlobalID;
						int EntryNo;
						EntryNo = 0;
						EntryOper.GetEntryGlobalID(ref EntryGlobalId, ref EntryNo);
						obj7.EntryGlobalID = EntryGlobalId;
					}
					else
					{
						Inv.EntryGlobalID = "-1";
					}
				}
				if (Sync.ActiveSync & (Sync.BranchType == 4) & (Inv.InvoiceType == InvoiceType.POS) & (Inv.ProcType != 3) & (Operators.CompareString(Inv.CloudID, "", TextCompare: false) == 0))
				{
					string EntryGlobalId;
					EntryGlobalId = (invoiceDGV = Inv).CloudID;
					InvoiceOper.GetInvoiceCloudID(ref EntryGlobalId, (int)Inv.InvoiceType, Inv.ProcType, invoiceObj.CloudSerial, invoiceObj.ReInvCloudSerial);
					invoiceDGV.CloudID = EntryGlobalId;
				}
				else
				{
					Inv.CloudID = Inv.CloudID;
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
			invoice.previousUUID = InvoiceOper.GetPreviousUUID(Inv.InvoiceNo, (int)Inv.InvoiceType, Inv.ProcType, Inv.Branch);
			invoice.EntryGlobalID = Inv.EntryGlobalID;
			invoice.AutoIncrementID = Inv.AutoIncrementID;
			if (Inv.AutoIncrementID == 0)
			{
				invoice.AutoIncrementID = InvoiceOper.GetAutoIncrementID();
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
			Items = (invoiceDGV = Inv).InvoiceItems;
			this.BindingCompositeItems(ref Items, Inv.InvertoryImpact);
			invoiceDGV.InvoiceItems = Items;
			invoice.Items = list;
			return invoice;
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

		public static bool DeleteInvoice(InvoiceDGV Invoic)
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
			try
			{
                var Home = new Home();
                if (MessageBox.Show("  هل انت متأكد من الحذف  ", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					new SqlCommand("update Inv set IS_Deleted=1 where InvGlobalID=N'" + Invoic.InvGlobalID + "'", sqlConnection, sqlTransaction).ExecuteNonQuery();
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

		public static bool InvoicePayments(ref InvoiceDGV Invoic, InvoiceObj InvObj)
		{
			if (Invoic.Net > 0.0)
			{
				InvoicePayment invoicePayment;
				invoicePayment = new InvoicePayment();
				invoicePayment.PayType = Invoic.PayType;
				invoicePayment.PaymentStatus = 0;
				invoicePayment.CashPayment = 0.0;
				invoicePayment.MadaPayment = 0.0;
				invoicePayment.VisaPayment = 0.0;
				invoicePayment.Paid = 0.0;
				invoicePayment.Remainder = Invoic.Net;
				invoicePayment.BankId = -1;
				invoicePayment.Treasury = Invoic.Treasury;
				invoicePayment.DueDate = DateTime.Now;
				invoicePayment.InvGlobalID = Invoic.InvGlobalID;
				invoicePayment.PaymentDate = DateTime.Now;
				invoicePayment.PaymentId = 0;
				Invoic.InvoicePayments.Add(invoicePayment);
				if (Invoic.ISNew)
				{
					if (InvObj.PaymentStatus & InvObj.ShowPayForm)
					{
						MsgPayment msgPayment;
						msgPayment = new MsgPayment();
						msgPayment.ShowDialog();
						if (((msgPayment.Action == PaymentStatus.Unpaid) | (msgPayment.Action == PaymentStatus.PaidPartially) | (msgPayment.Action == PaymentStatus.PostPaid)) & (Invoic.Customer <= 1))
						{
							Invoic.PaymentStatus = msgPayment.Action;
							Interaction.MsgBox("يجب إدخال العميل ");
							return false;
						}
						if ((msgPayment.Action == PaymentStatus.Unpaid) | (msgPayment.Action == PaymentStatus.PostPaid))
						{
							Invoic.Paid = 0.0;
							Invoic.IsPrinted = true;
							Invoic.PaymentStatus = msgPayment.Action;
							if (msgPayment.Action == PaymentStatus.Unpaid)
							{
								Invoic.PayType = 0;
							}
							else
							{
								Invoic.PayType = -1;
							}
							return true;
						}
						Invoic.Remainder = Invoic.Net - Invoic.Paid;
						Invoic.PaymentStatus = msgPayment.Action;
						return InvoiceOper.PayInvoice(ref Invoic, InvObj);
					}
					if (InvObj.ShowPayForm)
					{
						return InvoiceOper.PayInvoice(ref Invoic, InvObj);
					}
					if (Invoic.PayType == -1)
					{
						Invoic.PaymentStatus = PaymentStatus.PostPaid;
						Invoic.Paycash = 0.0;
						Invoic.PayATM = 0.0;
						Invoic.Remainder = Invoic.Net;
						Invoic.Paid = 0.0;
					}
					else if (Invoic.PayType == 1)
					{
						Invoic.PaymentStatus = PaymentStatus.Paid;
						Invoic.Paycash = Invoic.Net;
						Invoic.PayATM = 0.0;
						Invoic.Remainder = 0.0;
						Invoic.Paid = Invoic.Net;
					}
					else if (Invoic.PayType == 2)
					{
						Invoic.PaymentStatus = PaymentStatus.Paid;
						Invoic.Paycash = 0.0;
						Invoic.PayATM = Invoic.Net;
						Invoic.Remainder = 0.0;
						Invoic.Paid = Invoic.Net;
					}
					return true;
				}
				if (!Invoic.ISNew & InvObj.PaymentStatus & InvObj.ShowPayForm)
				{
					Invoic.PaymentStatus = PaymentStatus.RePaid;
					Invoic.Remainder = Invoic.Net - Invoic.Paid;
					return InvoiceOper.PayInvoice1(ref Invoic, InvObj);
				}
				bool result = default(bool);
				return result;
			}
			return false;
		}

		public static bool PayInvoice1(ref InvoiceDGV Invoic, InvoiceObj InvObj)
		{
			bool result;
			try
			{
				if (Invoic.Net > 0.0)
				{
					frmPOSPay frmPOSPay2;
					frmPOSPay2 = new frmPOSPay();
					MainClass.ApplyPermissionToForm(frmPOSPay2);
					MainClass.DoApplyUserSett(frmPOSPay2);
					frmPOSPay2.Invoic = Invoic;
					frmPOSPay2.DigitsNo = InvObj.DigitsNo;
					frmPOSPay2.ShowDialog();
					if (frmPOSPay2.isDone)
					{
						Invoic = frmPOSPay2.Invoic;
						if (Invoic.Paid >= Invoic.Net)
						{
							Invoic.PaymentStatus = PaymentStatus.Paid;
						}
						frmPOSPay2.Close();
						if (!Invoic.ISNew)
						{
							Invoic.IsPrinted = false;
						}
						result = true;
					}
					else
					{
						result = false;
					}
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
				string text;
				text = "خطأ أثناء إدخال الدفع";
				if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
				{
					text = "error in deleting";
				}
				MessageBox.Show((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) != 0) ? (text + Environment.NewLine + "Error details: " + ex2.Message) : (text + Environment.NewLine + "تفاصيل الخطأ: " + ex2.Message), "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				result = false;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static void PayInvoiceWithoutShowPayFrm(ref InvoiceDGV Invoic, InvoiceObj InvObj)
		{
			if (Invoic.Net > 0.0 && Invoic.ISNew)
			{
				InvoicePayment invoicePayment;
				invoicePayment = new InvoicePayment();
				invoicePayment.PayType = Invoic.PayType;
				invoicePayment.PaymentStatus = 1;
				invoicePayment.CashPayment = Invoic.Net;
				invoicePayment.MadaPayment = 0.0;
				invoicePayment.VisaPayment = 0.0;
				invoicePayment.Paid = Invoic.Net;
				Invoic.Paid += invoicePayment.Paid;
				invoicePayment.Remainder = 0.0;
				invoicePayment.BankId = -1;
				invoicePayment.Treasury = Invoic.Treasury;
				invoicePayment.DueDate = DateTime.Now;
				invoicePayment.InvGlobalID = Invoic.InvGlobalID;
				invoicePayment.PaymentDate = DateTime.Now;
				invoicePayment.PaymentId = 0;
				Invoic.Paycash += invoicePayment.CashPayment;
				Invoic.PayATM += invoicePayment.MadaPayment;
				Invoic.Remainder = invoicePayment.Remainder;
				Invoic.InvoicePayments.Add(invoicePayment);
			}
		}

		public static bool PayInvoice(ref InvoiceDGV Invoic, InvoiceObj InvObj)
		{
			bool result;
			try
			{
				if (Invoic.Net > 0.0)
				{
					if (!Invoic.ISNew)
					{
						if (Invoic.PaymentStatus == PaymentStatus.Paid)
						{
							Invoic.IsPrinted = true;
							Invoic.PaymentStatus = PaymentStatus.Unpaid;
							Invoic.Remainder = 0.0;
							InvoicePayment invoicePayment;
							invoicePayment = new InvoicePayment();
							invoicePayment.PayType = Invoic.PayType;
							invoicePayment.PaymentStatus = 0;
							invoicePayment.CashPayment = 0.0;
							invoicePayment.MadaPayment = 0.0;
							invoicePayment.VisaPayment = 0.0;
							invoicePayment.Paid = 0.0;
							Invoic.Paycash = invoicePayment.CashPayment;
							Invoic.PayATM = invoicePayment.MadaPayment;
							Invoic.Remainder = 0.0;
							Invoic.Paid = 0.0;
							invoicePayment.BankId = 0;
							invoicePayment.Treasury = Invoic.Treasury;
							invoicePayment.DueDate = DateTime.Now;
							invoicePayment.InvGlobalID = Invoic.InvGlobalID;
							invoicePayment.PaymentDate = DateTime.Now;
							invoicePayment.PaymentId = 0;
							Invoic.InvoicePayments.Add(invoicePayment);
						}
						else
						{
							Invoic.IsPrinted = false;
							Invoic.PaymentStatus = PaymentStatus.RePaid;
							if (InvObj.PaymentStatus && Invoic.Paid < Invoic.Net)
							{
								Invoic.PaymentStatus = PaymentStatus.PaidPartially;
							}
							Invoic.Remainder = Invoic.Net - Invoic.Paid;
						}
						goto IL_040b;
					}
					if (!InvObj.PaymentStatus)
					{
						goto IL_040b;
					}
					MsgPayment msgPayment;
					msgPayment = new MsgPayment();
					msgPayment.ShowDialog();
					if (((msgPayment.Action == PaymentStatus.Unpaid) | (msgPayment.Action == PaymentStatus.PaidPartially) | (msgPayment.Action == PaymentStatus.PostPaid)) & (Invoic.Customer <= 1))
					{
						Invoic.PaymentStatus = msgPayment.Action;
						Interaction.MsgBox("يجب إدخال العميل ");
						result = false;
					}
					else
					{
						if (!((msgPayment.Action == PaymentStatus.Unpaid) | (msgPayment.Action == PaymentStatus.PostPaid)))
						{
							Invoic.Remainder = Invoic.Net - Invoic.Paid;
							Invoic.PaymentStatus = msgPayment.Action;
							goto IL_040b;
						}
						Invoic.Paid = 0.0;
						Invoic.PaymentStatus = msgPayment.Action;
						if (msgPayment.Action == PaymentStatus.Unpaid)
						{
							Invoic.PayType = 0;
						}
						else
						{
							Invoic.PayType = -1;
						}
						InvoicePayment invoicePayment2;
						invoicePayment2 = new InvoicePayment();
						invoicePayment2.PayType = Invoic.PayType;
						invoicePayment2.PaymentStatus = 0;
						invoicePayment2.CashPayment = 0.0;
						invoicePayment2.MadaPayment = 0.0;
						invoicePayment2.VisaPayment = 0.0;
						invoicePayment2.Paid = 0.0;
						Invoic.Paid += invoicePayment2.Paid;
						invoicePayment2.Remainder = Invoic.Net;
						invoicePayment2.BankId = -1;
						invoicePayment2.Treasury = Invoic.Treasury;
						invoicePayment2.DueDate = DateTime.Now;
						invoicePayment2.InvGlobalID = Invoic.InvGlobalID;
						invoicePayment2.PaymentDate = DateTime.Now;
						invoicePayment2.PaymentId = 0;
						Invoic.Paycash += invoicePayment2.CashPayment;
						Invoic.PayATM += invoicePayment2.MadaPayment;
						Invoic.Remainder = invoicePayment2.Remainder;
						Invoic.InvoicePayments.Add(invoicePayment2);
						result = true;
					}
				}
				else
				{
					Invoic.IsPrinted = false;
					result = false;
				}
				goto end_IL_0001;
			IL_040b:
				frmPOSBill frmPOSBill2;
				frmPOSBill2 = new frmPOSBill();
				MainClass.ApplyPermissionToForm(frmPOSBill2);
				MainClass.DoApplyUserSett(frmPOSBill2);
				frmPOSBill2.Invoic = Invoic;
				frmPOSBill2.DigitsNo = InvObj.DigitsNo;
				frmPOSBill2.ShowDialog();
				if (frmPOSBill2.isDone)
				{
					Invoic = frmPOSBill2.Invoic;
					if (Invoic.Paid >= Invoic.Net)
					{
						Invoic.PaymentStatus = PaymentStatus.RePaid;
					}
					frmPOSBill2.Close();
					result = true;
				}
				else
				{
					result = false;
				}
			end_IL_0001:;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				Exception ex2;
				ex2 = ex;
				string text;
				text = "خطأ أثناء إدخال الدفع";
				if (Operators.CompareString(MainClass.Language, "en", TextCompare: false) == 0)
				{
					text = "error in deleting";
				}
				MessageBox.Show((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) != 0) ? (text + Environment.NewLine + "Error details: " + ex2.Message) : (text + Environment.NewLine + "تفاصيل الخطأ: " + ex2.Message), "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				result = false;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public void CreateDgv(ref GridView GridView1)
		{
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "م",
				Name = "AutoIncrementID",
				FieldName = "AutoIncrementID",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "نوع الفاتورة",
				Name = "InvoiceType",
				FieldName = "InvoiceType",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "نوع الفاتورة",
				Name = "InvoiceTypeTxt",
				FieldName = "InvoiceTypeTxt",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "الرقم العام",
				Name = "InvGlobalID",
				FieldName = "InvGlobalID",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "رقم الفاتورة ",
				Name = "InvoiceNo",
				FieldName = "InvoiceNo",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "رقم المرجع ",
				Name = "ReffNo",
				FieldName = "ReffNo",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "التاريخ ",
				Name = "InvDate",
				FieldName = "InvDate",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "نوع الدفع",
				Name = "PayType",
				FieldName = "PayType",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "نوع الدفع",
				Name = "PaymentTxt",
				FieldName = "PaymentTxt",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = " العميل",
				Name = "Customer",
				FieldName = "Customer",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = " العميل",
				Name = "ClientTxt",
				FieldName = "ClientTxt",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "المجموع",
				Name = "SumPrice",
				FieldName = "SumPrice",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "الخصومات",
				Name = "TotDiscount",
				FieldName = "TotDiscount",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "الإجمالي",
				Name = "Total",
				FieldName = "Total",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "الضريبة",
				Name = "VAT",
				FieldName = "VAT",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "الصافي",
				Name = "Net",
				FieldName = "Net",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "المستودع",
				Name = "InvertoryName",
				FieldName = "InvertoryName",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "حالة الفاتورة",
				Name = "InvoiceStatus",
				FieldName = "InvoiceStatus",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "اسم المندوب",
				Name = "Salesman",
				FieldName = "Salesman",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "الربح",
				Name = "InvProfit",
				FieldName = "InvProfit",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "نسبة الربح",
				Name = "profitRatio",
				FieldName = "profitRatio",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "متوسط التكلفة",
				Name = "SumCost",
				FieldName = "SumCost",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "تكلفة إضافية",
				Name = "AdditionalCost",
				FieldName = "AdditionalCost",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "المستخدم",
				Name = "User",
				FieldName = "User",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "المستخدم",
				Name = "UserTxt",
				FieldName = "UserTxt",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "ProcType",
				Name = "ProcType",
				FieldName = "ProcType",
				Visible = false
			});
		}

		public static void InvoiceCalc(ref InvoiceDGV Invo)
		{
			try
			{
				Invo.Total = 0.0;
				Invo.SumPrice = 0.0;
				Invo.TotDiscount = 0.0;
				Invo.ItemsDiscount = 0.0;
				Invo.VAT = 0.0;
				Invo.ExtraVAT = 0.0;
				Invo.Net = 0.0;
				Invo.SumCost = 0.0;
				double num;
				num = 0.0;
				double num2;
				num2 = 0.0;
				foreach (InvoiceItem invoiceItem in Invo.InvoiceItems)
				{
					if (invoiceItem.ItemId > 0)
					{
						num = 0.0;
						num2 = 0.0;
						invoiceItem.ItemPrimaryQnty = invoiceItem.ItemQuantity * invoiceItem.UnitEquality;
						invoiceItem.ItemSumPrice = invoiceItem.ItemQuantity * invoiceItem.ItemPrice;
						invoiceItem.ItemTotalPrice = invoiceItem.ItemSumPrice - invoiceItem.ItemDiscount;
						double num3;
						num3 = invoiceItem.ItemTotalPrice;
						if ((num3 > 0.0) & (invoiceItem.ItemVatPerc > 0.0))
						{
							if (Invo.PriceIncVAT)
							{
								num = num3 - num3 / (1.0 + invoiceItem.ItemVatPerc / 100.0);
								num3 -= num;
								if (invoiceItem.isExtraTaxApplied & (Invo.ExtraVATPerc > 0.0))
								{
									num2 = num3 - num3 / (1.0 + Invo.ExtraVATPerc / 100.0);
									num3 -= num2;
								}
								invoiceItem.ItemTotalPrice = num3;
							}
							else
							{
								if (invoiceItem.isExtraTaxApplied & (Invo.ExtraVATPerc > 0.0))
								{
									num2 = num3 * (Invo.ExtraVATPerc / 100.0);
								}
								num = (num3 + num2) * (invoiceItem.ItemVatPerc / 100.0);
							}
						}
						if (Invo.InvoiceType == InvoiceType.BeginingInventory)
						{
							num = 0.0;
						}
						if (Invo.VATperc == 0.0)
						{
							num = 0.0;
						}
						invoiceItem.ItemVat = num;
						invoiceItem.ItemNetPrice = num3 + num + num2;
					}
					Invo.SumPrice += invoiceItem.ItemSumPrice;
					Invo.Net += invoiceItem.ItemNetPrice;
					Invo.ExtraVAT += num2;
					Invo.VAT += num + num2;
					Invo.Total += invoiceItem.ItemTotalPrice;
					Invo.SumCost += invoiceItem.ItemCost * invoiceItem.ItemPrimaryQnty;
					Invo.ItemsDiscount += invoiceItem.ItemDiscount;
				}
				Invo.TotDiscount = Invo.ItemsDiscount;
				if (!(Invo.InvDiscount > 0.0))
				{
					return;
				}
				Invo.Total = Invo.SumPrice - Invo.InvDiscount;
				if (Invo.PriceIncVAT)
				{
					Invo.VAT = Invo.Total - Invo.Total / (1.0 + Invo.VATperc / 100.0);
					Invo.Total -= Invo.VAT;
					if ((Invo.ExtraVAT > 0.0) & (Invo.ExtraVATPerc > 0.0))
					{
						Invo.ExtraVAT = Invo.Total - Invo.Total / (1.0 + Invo.ExtraVATPerc / 100.0);
						Invo.VAT += Invo.ExtraVAT;
						Invo.Total -= Invo.ExtraVAT;
					}
				}
				else
				{
					if ((Invo.ExtraVAT > 0.0) & (Invo.ExtraVATPerc > 0.0))
					{
						Invo.ExtraVAT = Invo.Total * (Invo.ExtraVATPerc / 100.0);
					}
					Invo.VAT = (Invo.Total + Invo.ExtraVAT) * (Invo.VATperc / 100.0);
					Invo.VAT += Invo.ExtraVAT;
				}
				Invo.Net = Invo.Total + Invo.VAT;
				Invo.TotDiscount += Invo.InvDiscount;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void CheckForOffer(ref InvoiceDGV Invo)
		{
			try
			{
				SqlConnection selectConnection;
				selectConnection = MainClass.ConnObj();
				SqlDataAdapter sqlDataAdapter;
				sqlDataAdapter = new SqlDataAdapter("select * from Offer where OfferStartDate<=@CurrentDate and OfferExpire>=@CurrentDate and (offerType=1 or offerType=3) and ISDeleted=0 order by OfferId Desc", selectConnection);
				sqlDataAdapter.SelectCommand.Parameters.Add("@CurrentDate", SqlDbType.DateTime).Value = DateTime.Now;
				DataTable dataTable;
				dataTable = new DataTable();
				sqlDataAdapter.Fill(dataTable);
				if (dataTable.Rows.Count <= 0 || Operators.ConditionalCompareObjectNotEqual(dataTable.Rows[0]["InvoiceID"], Invo.InvoiceType, TextCompare: false))
				{
					return;
				}
				if (Operators.ConditionalCompareObjectEqual(dataTable.Rows[0]["offerType"], 1, TextCompare: false))
				{
					if (Convert.ToDouble(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["OfferValueTarget"])) > 0.0)
					{
						if (Invo.Net >= Convert.ToDouble(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["OfferValueTarget"])))
						{
							if (Convert.ToDouble(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["OfferValue"])) > 0.0)
							{
								Invo.InvDiscount = Convert.ToDouble(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["OfferValue"]));
							}
							else if (Convert.ToDouble(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["OfferPercentage"])) > 0.0)
							{
								Invo.InvDiscount = Conversions.ToDouble(InvoiceOper.GetInvDiscountByPercentage(Invo.SumPrice, Conversion.Val(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["OfferPercentage"]))));
							}
						}
					}
					else if (Convert.ToDouble(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["OfferValue"])) > 0.0)
					{
						Invo.InvDiscount = Convert.ToDouble(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["OfferValue"]));
					}
					else if (Convert.ToDouble(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["OfferPercentage"])) > 0.0)
					{
						Invo.InvDiscount = Conversions.ToDouble(InvoiceOper.GetInvDiscountByPercentage(Invo.SumPrice, Conversion.Val(RuntimeHelpers.GetObjectValue(dataTable.Rows[0]["OfferPercentage"]))));
					}
				}
				else
				{
					if (!Operators.ConditionalCompareObjectEqual(dataTable.Rows[0]["offerType"], 3, TextCompare: false))
					{
						return;
					}
					SqlDataAdapter sqlDataAdapter2;
					sqlDataAdapter2 = new SqlDataAdapter("select id,OfferId,ClientID,OfferItemValue,OfferItemPercentage,CatatogryID from OfferForClient where ClientID=" + Conversions.ToString(Invo.Customer), selectConnection);
					DataTable dataTable2;
					dataTable2 = new DataTable();
					sqlDataAdapter2.Fill(dataTable2);
					if (dataTable2.Rows.Count > 0 && Operators.ConditionalCompareObjectEqual(dataTable2.Rows[0]["CatatogryID"], 0, TextCompare: false))
					{
						if (Convert.ToDouble(RuntimeHelpers.GetObjectValue(dataTable2.Rows[0]["OfferItemValue"])) > 0.0)
						{
							Invo.InvDiscount = Convert.ToDouble(RuntimeHelpers.GetObjectValue(dataTable2.Rows[0]["OfferItemValue"]));
						}
						else if (Convert.ToDouble(RuntimeHelpers.GetObjectValue(dataTable2.Rows[0]["OfferItemPercentage"])) > 0.0)
						{
							Invo.InvDiscount = Conversions.ToDouble(InvoiceOper.GetInvDiscountByPercentage(Invo.SumPrice, Conversion.Val(RuntimeHelpers.GetObjectValue(dataTable2.Rows[0]["OfferItemPercentage"]))));
						}
					}
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static bool isCreditCust(int cutID)
		{
			bool result = default(bool);
			try
			{
				SqlDataAdapter sqlDataAdapter;
				sqlDataAdapter = new SqlDataAdapter(selectConnection: MainClass.ConnObj(), selectCommandText: "select AccountCode from Customers where id=" + Conversions.ToString(cutID) + " and AccountCode <> -1 ");
				DataTable dataTable;
				dataTable = new DataTable();
				sqlDataAdapter.Fill(dataTable);
				if (dataTable.Rows.Count > 0)
				{
					result = true;
					return result;
				}
				result = false;
				return result;
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static bool IsTaxCustomer(int cutID)
		{
			bool result;
			try
			{
				SqlConnection sqlConnection;
				sqlConnection = MainClass.ConnObj();
				using (sqlConnection)
				{
					sqlConnection.Open();
					using SqlCommand sqlCommand = new SqlCommand("SELECT ISNULL(tax_no, 0) AS tax_no FROM Customers WHERE id = @cutID", sqlConnection);
					sqlCommand.Parameters.AddWithValue("@cutID", cutID);
					object objectValue;
					objectValue = RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar());
					if (objectValue != null && !Convert.IsDBNull(RuntimeHelpers.GetObjectValue(objectValue)))
					{
						string text;
						text = objectValue.ToString().Trim();
						result = !string.IsNullOrEmpty(text) && Operators.CompareString(text, "0", TextCompare: false) != 0 && text.Length == 15 && text.StartsWith("3") && text.EndsWith("3");
					}
					else
					{
						result = false;
					}
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = false;
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public static List<PaymentType> GetInvoiceCloudPaymentType()
		{
			List<PaymentType> list;
			list = new List<PaymentType>();
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter("select Name,CloudPaytypeID,Paytype,isnull(BankId,0) as BankId from CloudPaytypeSetting where CloudPaytypeID is not null", MainClass.ConnObj());
			DataTable dataTable;
			dataTable = new DataTable();
			sqlDataAdapter.Fill(dataTable);
			checked
			{
				if (dataTable.Rows.Count > 0)
				{
					int num;
					num = dataTable.Rows.Count - 1;
					for (int i = 0; i <= num; i++)
					{
						PaymentType paymentType;
						paymentType = new PaymentType();
						paymentType.CloudPaytypeID = Conversions.ToString(dataTable.Rows[i]["CloudPaytypeID"]);
						paymentType.Name = Conversions.ToString(dataTable.Rows[i]["Name"]);
						paymentType.Paytype = Conversions.ToInteger(dataTable.Rows[i]["Paytype"]);
						paymentType.BankId = Conversions.ToInteger(dataTable.Rows[i]["BankId"]);
						list.Add(paymentType);
					}
				}
				return list;
			}
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

		public static string GetPreviousUUID(int InvoiceId, int InvType, int ProcType, int BranchID)
		{
			string result;
			try
			{
				int num;
				num = checked(InvoiceId - 1);
				SqlDataAdapter sqlDataAdapter;
				sqlDataAdapter = new SqlDataAdapter(selectConnection: MainClass.ConnObj(), selectCommandText: "Select IsNull(UUID,0) as PreviousUUID from inv where id= " + Conversions.ToString(num) + "  and  inv_type=" + Conversions.ToString(InvType) + " and proc_type=" + Conversions.ToString(ProcType) + " and branch=" + Conversions.ToString(BranchID));
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

		public static string GetInvoiceUUID(string InvCombinedId, int InvType, int ProcType, int BranchID)
		{
			string result;
			try
			{
				string text;
				text = InvCombinedId.Trim();
				SqlDataAdapter sqlDataAdapter;
				sqlDataAdapter = new SqlDataAdapter(selectConnection: MainClass.ConnObj(), selectCommandText: "Select IsNull(UUID,0) as referenceUUID from inv where InvCombinedId=N'" + text + "'  and  inv_type=" + Conversions.ToString(InvType) + " and proc_type=" + Conversions.ToString(ProcType) + " and branch=" + Conversions.ToString(BranchID));
				DataTable dataTable;
				dataTable = new DataTable();
				sqlDataAdapter.Fill(dataTable);
				result = ((dataTable.Rows.Count <= 0) ? "" : ((!Conversions.ToBoolean(Operators.AndObject(Operators.CompareObjectNotEqual(dataTable.Rows[0]["referenceUUID"], "0", TextCompare: false), Operators.CompareString(dataTable.Rows[0]["referenceUUID"].ToString(), "", TextCompare: false) != 0))) ? "" : dataTable.Rows[0]["referenceUUID"].ToString().Trim()));
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				result = "";
				ProjectData.ClearProjectError();
			}
			return result;
		}

		public void UpdateInvoiceStatus(string InvGlobalID, string UUID)
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
				sqlCommand = new SqlCommand("UPDATE Inv set UUID=@UUID,ZatcaSent=1 WHERE InvGlobalID=@InvGlobalID ", sqlConnection);
				sqlCommand.Parameters.Add("@InvGlobalID", SqlDbType.NVarChar).Value = InvGlobalID;
				sqlCommand.Parameters.Add("@UUID", SqlDbType.NVarChar).Value = UUID;
				sqlCommand.ExecuteNonQuery();
				if (sqlConnection.State != ConnectionState.Closed)
				{
					sqlConnection.Close();
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				ProjectData.ClearProjectError();
			}
		}

		public static void InvoiceProperties(int InvType, int ProcType, ref bool PriceIncVAT, ref double TaxPerc)
		{
			if (InvType <= -1)
			{
				return;
			}
			SqlConnection sqlConnection;
			sqlConnection = MainClass.ConnObj();
			using SqlCommand sqlCommand = new SqlCommand("SELECT ISNULL(PriceIncVAT, 0) AS PriceIncVAT, MainVAT FROM SettingGeneral WHERE Inv_Id = @InvType", sqlConnection);
			sqlCommand.Parameters.AddWithValue("@InvType", InvType);
			sqlConnection.Open();
			SqlDataReader sqlDataReader;
			sqlDataReader = sqlCommand.ExecuteReader();
			if (sqlDataReader.HasRows)
			{
				sqlDataReader.Read();
				PriceIncVAT = Convert.ToBoolean(RuntimeHelpers.GetObjectValue(sqlDataReader["PriceIncVAT"]));
				TaxPerc = Convert.ToDouble(RuntimeHelpers.GetObjectValue(sqlDataReader["MainVAT"]));
			}
			sqlDataReader.Close();
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
			string docsRootPath;
			docsRootPath = Properties.Settings.Default.DocsRootPath;
			using SqlConnection sqlConnection = new SqlConnection(MainClass.connstr);
			sqlConnection.Open();
			string[] array;
			array = InvoiceOper.filePathDocument;
			foreach (string text in array)
			{
				int num;
				using (SqlCommand sqlCommand = new SqlCommand("SELECT ISNULL(MAX(id), 0) + 1 FROM Documents", sqlConnection))
				{
					num = Convert.ToInt32(RuntimeHelpers.GetObjectValue(sqlCommand.ExecuteScalar()));
				}
				string text2;
				text2 = Path.Combine(docsRootPath, Filatype + "(" + InvGlobalID + ")_" + num + Path.GetExtension(text));
				string fileName;
				fileName = Path.GetFileName(text);
				File.Copy(text, text2, overwrite: true);
				using SqlCommand sqlCommand2 = new SqlCommand("INSERT INTO Documents (FileName, FileUrl, GlobalID, type) VALUES (@FileName, @FileUrl, @GlobalID, 2)", sqlConnection);
				sqlCommand2.Parameters.AddWithValue("@FileName", fileName);
				sqlCommand2.Parameters.AddWithValue("@FileUrl", text2);
				sqlCommand2.Parameters.AddWithValue("@GlobalID", InvGlobalID);
				sqlCommand2.ExecuteNonQuery();
			}
		}

		public static object getdocuments(ref DataTable dtt, string InvGlobalID)
		{
			SqlConnection sqlConnection;
			sqlConnection = new SqlConnection(MainClass.connstr);
			SqlCommand sqlCommand;
			sqlCommand = new SqlCommand("SELECT Id,FileName, Fileurl FROM Documents WHERE type=2 and GlobalID = @GlobalID", sqlConnection);
			sqlCommand.Parameters.AddWithValue("@GlobalID", InvGlobalID);
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter(sqlCommand);
			DataTable dataTable;
			dataTable = new DataTable();
			try
			{
				sqlConnection.Open();
				sqlDataAdapter.Fill(dataTable);
				if (dataTable.Rows.Count > 0)
				{
					dtt = dataTable;
				}
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				MessageBox.Show("حدث خطأ أثناء استرجاع البيانات: " + ex.Message);
				ProjectData.ClearProjectError();
			}
			finally
			{
				sqlConnection.Close();
			}
			object result = default(object);
			return result;
		}

		public static object GetCodeCustomerbyid(int id)
		{
			SqlConnection sqlConnection;
			sqlConnection = new SqlConnection(MainClass.connstr);
			SqlCommand sqlCommand;
			sqlCommand = new SqlCommand("SELECT AccountCode FROM Customers WHERE id=@id ", sqlConnection);
			sqlCommand.Parameters.AddWithValue("@id", id);
			SqlDataAdapter sqlDataAdapter;
			sqlDataAdapter = new SqlDataAdapter(sqlCommand);
			DataTable dataTable;
			dataTable = new DataTable();
			object result = default(object);
			try
			{
				sqlConnection.Open();
				sqlDataAdapter.Fill(dataTable);
				result = ((dataTable.Rows.Count <= 0) ? Conversions.ToInteger("-1") : Conversions.ToInteger(dataTable.Rows[0]["AccountCode"]));
				return result;
			}
			catch (Exception ex)
			{
				ProjectData.SetProjectError(ex);
				MessageBox.Show("حدث خطأ أثناء استرجاع البيانات: " + ex.Message);
				ProjectData.ClearProjectError();
			}
			finally
			{
				sqlConnection.Close();
			}
			return result;
		}
	}
}