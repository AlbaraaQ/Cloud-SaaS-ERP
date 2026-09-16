using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows.Forms;
using DevExpress.Data;
using DevExpress.Utils;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraReports.UI;
using Microsoft.VisualBasic.CompilerServices;
using UtilitiesProj;

namespace SmartAuditERP
{

	public class Report
	{
		public void CreateDgv(ref GridView GridView1)
		{
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "م" : "#"),
				Name = "AutoIncrementID",
				FieldName = "AutoIncrementID",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "نوع الفاتورة" : "Invoice type"),
				Name = "InvoiceType",
				FieldName = "InvoiceType",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "نوع الفاتورة" : "Invoice type"),
				Name = "InvoiceTypeTxt",
				FieldName = "InvoiceTypeTxt",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "الرقم العام" : "General No"),
				Name = "InvGlobalID",
				FieldName = "InvGlobalID",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "رقم الفاتورة " : "Invoice No"),
				Name = "InvoiceNo",
				FieldName = "InvoiceNo",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "رقم المرجع " : "Reff No"),
				Name = "ReffNo",
				FieldName = "ReffNo",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "التاريخ " : "Invoice date"),
				Name = "InvDate",
				FieldName = "InvDate",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "نوع الدفع" : "pay type"),
				Name = "PayType",
				FieldName = "PayType",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "نوع الدفع" : "pay type"),
				Name = "PaymentTxt",
				FieldName = "PaymentTxt",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? " العميل" : "Customer"),
				Name = "Customer",
				FieldName = "Customer",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? " العميل" : "Customer"),
				Name = "ClientTxt",
				FieldName = "ClientTxt",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "المجموع" : "Sum"),
				Name = "SumPrice",
				FieldName = "SumPrice",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "الخصومات" : "Discount"),
				Name = "TotDiscount",
				FieldName = "TotDiscount",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "الإجمالي" : "Total"),
				Name = "Total",
				FieldName = "Total",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "الضريبة" : "Tax"),
				Name = "VAT",
				FieldName = "VAT",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "الصافي" : "Net"),
				Name = "Net",
				FieldName = "Net",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "المستودع" : "Invertory"),
				Name = "InvertoryName",
				FieldName = "InvertoryName",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "الفرع" : "Branch"),
				Name = "BranchTxt",
				FieldName = "BranchTxt",
				Visible = false,
				VisibleIndex = 9
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "حالة الفاتورة" : "Invoice Status"),
				Name = "InvoiceStatus",
				FieldName = "InvoiceStatus",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "اسم المندوب" : "Salesman"),
				Name = "Salesman",
				FieldName = "Salesman",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "الربح" : "Profit"),
				Name = "InvProfit",
				FieldName = "InvProfit",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "نسبة الربح" : "profit Ratio"),
				Name = "profitRatio",
				FieldName = "profitRatio",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "متوسط التكلفة" : "Avg cost"),
				Name = "SumCost",
				FieldName = "SumCost",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "تكلفة إضافية" : "Additional Cost"),
				Name = "AdditionalCost",
				FieldName = "AdditionalCost",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "المستخدم" : "User"),
				Name = "User",
				FieldName = "User",
				Visible = true
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "المستخدم" : "User"),
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
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "Paycash",
				Name = "Paycash",
				FieldName = "Paycash",
				Visible = false
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "PayATM",
				Name = "PayATM",
				FieldName = "PayATM",
				Visible = false
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "ExtraVAT",
				Name = "ExtraVAT",
				FieldName = "ExtraVAT",
				Visible = false
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "TotalTax",
				Name = "TotalTax",
				FieldName = "TotalTax",
				Visible = false
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = "InvoiceTime",
				Name = "InvoiceTime",
				FieldName = "InvoiceTime",
				Visible = false
			});
		}

		public void LoadDGvSetting(ref GridView GridView1)
		{
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "تفاصيل" : "User"),
				Name = "details",
				FieldName = "details",
				Visible = false
			});
			GridView1.Columns.Add(new GridColumn
			{
				Caption = ((Operators.CompareString(MainClass.Language, "ar", TextCompare: false) == 0) ? "عرض الفاتورة" : "Show "),
				Name = "showInv",
				FieldName = "showInv",
				Visible = true
			});
			GridView1.Columns["InvGlobalID"].Visible = false;
			GridView1.Columns["InvoiceStatus"].Visible = false;
			GridView1.Columns["Salesman"].Visible = false;
			GridView1.Columns["InvProfit"].Visible = false;
			GridView1.Columns["profitRatio"].Visible = false;
			GridView1.Columns["SumCost"].Visible = false;
			GridView1.Columns["AdditionalCost"].Visible = false;
			GridView1.Columns["InvoiceType"].Visible = false;
			GridView1.Columns["Customer"].Visible = false;
			GridView1.Columns["User"].Visible = false;
			GridView1.Columns["PayType"].Visible = false;
			GridView1.Columns["AutoIncrementID"].OptionsColumn.AllowEdit = false;
			GridView1.Columns["AutoIncrementID"].OptionsColumn.ReadOnly = true;
			GridView1.Columns["AutoIncrementID"].AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["AutoIncrementID"].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["AutoIncrementID"].Width = 60;
			GridView1.Columns["AutoIncrementID"].SummaryItem.SummaryType = SummaryItemType.Count;
			GridView1.Columns["InvoiceTypeTxt"].OptionsColumn.AllowEdit = false;
			GridView1.Columns["InvoiceTypeTxt"].OptionsColumn.ReadOnly = true;
			GridView1.Columns["InvoiceTypeTxt"].AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["InvoiceTypeTxt"].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["InvoiceTypeTxt"].Width = 120;
			GridView1.Columns["InvoiceNo"].OptionsColumn.AllowEdit = false;
			GridView1.Columns["InvoiceNo"].OptionsColumn.ReadOnly = true;
			GridView1.Columns["InvoiceNo"].AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["InvoiceNo"].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["InvoiceNo"].Width = 75;
			GridView1.Columns["ReffNo"].OptionsColumn.AllowEdit = false;
			GridView1.Columns["ReffNo"].OptionsColumn.ReadOnly = true;
			GridView1.Columns["ReffNo"].AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["ReffNo"].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["ReffNo"].Width = 75;
			GridView1.Columns["InvDate"].OptionsColumn.AllowEdit = false;
			GridView1.Columns["InvDate"].OptionsColumn.ReadOnly = true;
			GridView1.Columns["InvDate"].AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["InvDate"].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["InvDate"].Width = 120;
			GridView1.Columns["InvDate"].DisplayFormat.FormatType = FormatType.DateTime;
			GridView1.Columns["InvDate"].DisplayFormat.FormatString = "MM/d/yyyy hh:mm tt";
			GridView1.Columns["SumPrice"].OptionsColumn.AllowEdit = false;
			GridView1.Columns["SumPrice"].OptionsColumn.ReadOnly = true;
			GridView1.Columns["SumPrice"].AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["SumPrice"].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["SumPrice"].Width = 75;
			GridView1.Columns["SumPrice"].DisplayFormat.FormatType = FormatType.Numeric;
			GridView1.Columns["SumPrice"].DisplayFormat.FormatString = "{0:" + Common.DigitsNo + "}";
			GridView1.Columns["SumPrice"].SummaryItem.DisplayFormat = "{0:" + Common.DigitsNo + "}";
			GridView1.Columns["SumPrice"].SummaryItem.FieldName = "SumPrice";
			GridView1.Columns["SumPrice"].SummaryItem.SummaryType = SummaryItemType.Custom;
			GridView1.Columns["TotDiscount"].OptionsColumn.AllowEdit = false;
			GridView1.Columns["TotDiscount"].OptionsColumn.ReadOnly = true;
			GridView1.Columns["TotDiscount"].AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["TotDiscount"].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["TotDiscount"].Width = 75;
			GridView1.Columns["TotDiscount"].DisplayFormat.FormatType = FormatType.Numeric;
			GridView1.Columns["TotDiscount"].DisplayFormat.FormatString = "{0:" + Common.DigitsNo + "}";
			GridView1.Columns["TotDiscount"].SummaryItem.DisplayFormat = "{0:" + Common.DigitsNo + "}";
			GridView1.Columns["TotDiscount"].SummaryItem.FieldName = "TotDiscount";
			GridView1.Columns["TotDiscount"].SummaryItem.SummaryType = SummaryItemType.Custom;
			GridView1.Columns["Total"].OptionsColumn.AllowEdit = false;
			GridView1.Columns["Total"].OptionsColumn.ReadOnly = true;
			GridView1.Columns["Total"].AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["Total"].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["Total"].Width = 75;
			GridView1.Columns["Total"].DisplayFormat.FormatType = FormatType.Numeric;
			GridView1.Columns["Total"].DisplayFormat.FormatString = "{0:" + Common.DigitsNo + "}";
			GridView1.Columns["Total"].SummaryItem.DisplayFormat = "{0:" + Common.DigitsNo + "}";
			GridView1.Columns["Total"].SummaryItem.FieldName = "Total";
			GridView1.Columns["Total"].SummaryItem.SummaryType = SummaryItemType.Custom;
			GridView1.Columns["VAT"].OptionsColumn.AllowEdit = false;
			GridView1.Columns["VAT"].OptionsColumn.ReadOnly = true;
			GridView1.Columns["VAT"].AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["VAT"].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["VAT"].Width = 75;
			GridView1.Columns["VAT"].DisplayFormat.FormatType = FormatType.Numeric;
			GridView1.Columns["VAT"].DisplayFormat.FormatString = "{0:" + Common.DigitsNo + "}";
			GridView1.Columns["VAT"].SummaryItem.DisplayFormat = "{0:" + Common.DigitsNo + "}";
			GridView1.Columns["VAT"].SummaryItem.FieldName = "VAT";
			GridView1.Columns["VAT"].SummaryItem.SummaryType = SummaryItemType.Custom;
			GridView1.Columns["Net"].OptionsColumn.AllowEdit = false;
			GridView1.Columns["Net"].OptionsColumn.ReadOnly = true;
			GridView1.Columns["Net"].AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["Net"].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["Net"].Width = 75;
			GridView1.Columns["Net"].DisplayFormat.FormatType = FormatType.Numeric;
			GridView1.Columns["Net"].DisplayFormat.FormatString = "{0:" + Common.DigitsNo + "}";
			GridView1.Columns["Net"].SummaryItem.DisplayFormat = "{0:" + Common.DigitsNo + "}";
			GridView1.Columns["Net"].SummaryItem.FieldName = "Net";
			GridView1.Columns["Net"].SummaryItem.SummaryType = SummaryItemType.Custom;
			GridView1.Columns["InvertoryName"].OptionsColumn.AllowEdit = false;
			GridView1.Columns["InvertoryName"].OptionsColumn.ReadOnly = true;
			GridView1.Columns["InvertoryName"].AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["InvertoryName"].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["InvertoryName"].Width = 200;
			GridView1.Columns["BranchTxt"].OptionsColumn.AllowEdit = false;
			GridView1.Columns["BranchTxt"].OptionsColumn.ReadOnly = true;
			GridView1.Columns["BranchTxt"].AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["BranchTxt"].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["BranchTxt"].Width = 200;
			GridView1.Columns["ClientTxt"].OptionsColumn.AllowEdit = false;
			GridView1.Columns["ClientTxt"].OptionsColumn.ReadOnly = true;
			GridView1.Columns["ClientTxt"].AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["ClientTxt"].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["ClientTxt"].Width = 200;
			GridView1.Columns["UserTxt"].OptionsColumn.AllowEdit = false;
			GridView1.Columns["UserTxt"].OptionsColumn.ReadOnly = true;
			GridView1.Columns["UserTxt"].AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["UserTxt"].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["UserTxt"].Width = 200;
			GridView1.Columns["PaymentTxt"].OptionsColumn.AllowEdit = false;
			GridView1.Columns["PaymentTxt"].OptionsColumn.ReadOnly = true;
			GridView1.Columns["PaymentTxt"].AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["PaymentTxt"].AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;
			GridView1.Columns["PaymentTxt"].Width = 100;
		}

		public DataSet BindToData(GridView GridView1, string rptType, string FromDate, string ToDate)
		{
			checked
			{
				DataSet result;
				try
				{
					string left;
					left = "";
					if (Operators.CompareString(left, "", TextCompare: false) != 0)
					{
					}
					List<InventoryData> list;
					list = new List<InventoryData>();
					int num;
					num = GridView1.RowCount - 1;
					for (int i = 0; i <= num; i++)
					{
						InventoryData inventoryData;
						inventoryData = new InventoryData();
						inventoryData.ProcessType = rptType;
						inventoryData.InvType = GridView1.GetRowCellDisplayText(i, "InvoiceTypeTxt");
						inventoryData.InvoiceNo = GridView1.GetRowCellDisplayText(i, "InvoiceNo");
						inventoryData.RefsInvNo = GridView1.GetRowCellDisplayText(i, "ReffNo");
						inventoryData.InvDate = GridView1.GetRowCellDisplayText(i, "InvDate");
						inventoryData.InvTime = ((!string.IsNullOrEmpty(GridView1.GetRowCellDisplayText(i, "InvoiceTime"))) ? GridView1.GetRowCellDisplayText(i, "InvoiceTime") : "");
						inventoryData.Clint = GridView1.GetRowCellDisplayText(i, "ClientTxt");
						inventoryData.Total = GridView1.GetRowCellDisplayText(i, "SumPrice");
						inventoryData.NetDiscount = GridView1.GetRowCellDisplayText(i, "TotDiscount");
						inventoryData.NetBeforeTax = GridView1.GetRowCellDisplayText(i, "Total");
						inventoryData.Tax = GridView1.GetRowCellDisplayText(i, "VAT");
						inventoryData.Paid = ((!string.IsNullOrEmpty(GridView1.GetRowCellDisplayText(i, "Paid"))) ? GridView1.GetRowCellDisplayText(i, "Paid") : "");
						inventoryData.Remainder = ((!string.IsNullOrEmpty(GridView1.GetRowCellDisplayText(i, "Remainder"))) ? GridView1.GetRowCellDisplayText(i, "Remainder") : "");
						inventoryData.ExtraTax = ((!string.IsNullOrEmpty(GridView1.GetRowCellDisplayText(i, "ExtraVAT"))) ? GridView1.GetRowCellDisplayText(i, "ExtraVAT") : "0");
						inventoryData.TotalTax = ((!string.IsNullOrEmpty(GridView1.GetRowCellDisplayText(i, "TotalTax"))) ? GridView1.GetRowCellDisplayText(i, "TotalTax") : "0");
						inventoryData.Net = GridView1.GetRowCellDisplayText(i, "Net");
						inventoryData.SafeName = GridView1.GetRowCellDisplayText(i, "InvertoryName");
						inventoryData.EmpName = GridView1.GetRowCellDisplayText(i, "UserTxt");
						inventoryData.BranchName = GridView1.GetRowCellDisplayText(i, "BranchTxt");
						inventoryData.Cash = GridView1.GetRowCellDisplayText(i, "Paycash");
						inventoryData.Network = GridView1.GetRowCellDisplayText(i, "PayATM");
						inventoryData.Paytype = GridView1.GetRowCellDisplayText(i, "PaymentTxt");
						inventoryData.InventoryType = rptType;
						inventoryData.Sum = GridView1.Columns["SumPrice"].SummaryText;
						inventoryData.SumNetDiscount = GridView1.Columns["TotDiscount"].SummaryText;
						inventoryData.Total1 = GridView1.Columns["Total"].SummaryText;
						inventoryData.SumTax = GridView1.Columns["VAT"].SummaryText;
						inventoryData.NetTotal = GridView1.Columns["Net"].SummaryText;
						inventoryData.SummaryExtraTax = GridView1.Columns["ExtraVAT"].SummaryText;
						inventoryData.SummaryTotalTax = GridView1.Columns["TotalTax"].SummaryText;
						inventoryData.SummaryPaid = GridView1.Columns["Paid"].SummaryText;
						inventoryData.SummaryRemainder = GridView1.Columns["Remainder"].SummaryText;
						inventoryData.FromDate = FromDate;
						inventoryData.ToDate = ToDate;
						string filterPanelText;
						filterPanelText = GridView1.FilterPanelText;
						if (Operators.CompareString(filterPanelText, "", TextCompare: false) != 0)
						{
							inventoryData.FilterText = filterPanelText;
						}
						inventoryData.User = Common.GetEmpName(MainClass.EmpNo);
						inventoryData.PrintDate = DateTime.Now.ToShortDateString();
						list.Add(inventoryData);
					}
					DataSet dataSet;
					dataSet = new DataSet("Name");
					DataTable table;
					table = global::UtilitiesProj.Common.ToDataTable(list);
					dataSet.Tables.Add(table);
					list.Clear();
					result = dataSet;
				}
				catch (Exception ex)
				{
					ProjectData.SetProjectError(ex);
					Console.WriteLine(ex.Message);
					result = new DataSet();
					ProjectData.ClearProjectError();
				}
				return result;
			}
		}

		public void Printing(int type, DataSet ds, string RptUrl, string RptName, string defPrinter, int PrintNo)
		{
			try
			{
				if (Operators.CompareString(RptUrl, "", TextCompare: false) == 0)
				{
					MessageBox.Show("يجب  تحديد مسار التقرير ", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					return;
				}
				if (!Directory.Exists(RptUrl) | !File.Exists(RptUrl + "\\\\" + RptName))
				{
					MessageBox.Show("المسار الحالي للتقارير  غير موجود او تم تعديله", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					return;
				}
				if (Operators.CompareString(RptName, "", TextCompare: false) == 0)
				{
					MessageBox.Show("يجب  إدخال اسم التقرير من الإعدادات", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					return;
				}
				XtraReport xtraReport;
				xtraReport = XtraReport.FromFile(RptUrl + "\\" + RptName);
				xtraReport.DataSource = ds;
				XtraReport xtraReport2;
				xtraReport2 = XtraReport.FromFile(RptUrl + "\\header.repx");
				xtraReport2.DataSource = Common.FoundationInfoDT;
				XRSubreport xRSubreport;
				xRSubreport = (XRSubreport)xtraReport.FindControl("headerRpt", ignoreCase: true);
				if (xRSubreport != null)
				{
					xRSubreport.ReportSource = xtraReport2;
				}
				XtraReport xtraReport3;
				xtraReport3 = XtraReport.FromFile(RptUrl + "\\footer.repx");
				xtraReport3.DataSource = Common.FoundationInfoDT;
				XRSubreport xRSubreport2;
				xRSubreport2 = (XRSubreport)xtraReport.FindControl("footerRpt", ignoreCase: true);
				if (xRSubreport2 != null)
				{
					xRSubreport2.ReportSource = xtraReport3;
				}
				if (Operators.CompareString(defPrinter, "", TextCompare: false) != 0)
				{
					xtraReport.PrinterName = defPrinter;
					if (type == 1)
					{
						for (int i = 1; i <= PrintNo; i = checked(i + 1))
						{
							xtraReport.Print();
						}
					}
					else
					{
						xtraReport.ShowPreviewDialog();
					}
					xtraReport.Dispose();
				}
				else
				{
					MessageBox.Show("يجب  تحديد الطابعة من الإعدادات", "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				}
			}
			catch (Exception projectError)
			{
				ProjectData.SetProjectError(projectError);
				MessageBox.Show("حدث خطأ اثناء الطباعة");
				ProjectData.ClearProjectError();
			}
		}

        // ✅ أضف هذا في كلاس Report
        public DataSet BindToData(
            DataTable dt,
            string rptType,
            string FromDate,
            string ToDate,
            // مجاميع المحسوبة من الكود الخلفي
            string sumTotal = "0",
            string sumDiscount = "0",
            string sumNet = "0",
            string sumVAT = "0",
            string sumNetFinal = "0")
        {
            try
            {
                var list = new List<InventoryData>();

                foreach (DataRow row in dt.Rows)
                {
                    var inventoryData = new InventoryData
                    {
                        ProcessType = rptType,
                        InvType = row["InvoiceTypeTxt"]?.ToString() ?? "",
                        InvoiceNo = row["InvoiceNo"]?.ToString() ?? "",
                        RefsInvNo = row["ReffNo"]?.ToString() ?? "",
                        InvDate = row["InvDate"]?.ToString() ?? "",
                        InvTime = row["InvoiceTime"]?.ToString() ?? "",
                        Clint = row["ClientTxt"]?.ToString() ?? "",
                        Total = row["SumPrice"]?.ToString() ?? "",
                        NetDiscount = row["TotDiscount"]?.ToString() ?? "",
                        NetBeforeTax = row["Total"]?.ToString() ?? "",
                        Tax = row["VAT"]?.ToString() ?? "",
                        Paid = row["Paid"]?.ToString() ?? "",
                        Remainder = row["Remainder"]?.ToString() ?? "",
                        ExtraTax = row["ExtraVAT"]?.ToString() ?? "0",
                        TotalTax = row["TotalTax"]?.ToString() ?? "0",
                        Net = row["Net"]?.ToString() ?? "",
                        SafeName = row["InvertoryName"]?.ToString() ?? "",
                        EmpName = row["UserTxt"]?.ToString() ?? "",
                        BranchName = row["BranchTxt"]?.ToString() ?? "",
                        Cash = row["Paycash"]?.ToString() ?? "",
                        Network = row["PayATM"]?.ToString() ?? "",
                        Paytype = row["PaymentTxt"]?.ToString() ?? "",
                        InventoryType = rptType,

                        // المجاميع المحسوبة
                        Sum = sumTotal,
                        SumNetDiscount = sumDiscount,
                        Total1 = sumNet,
                        SumTax = sumVAT,
                        NetTotal = sumNetFinal,
                        SummaryExtraTax = "0",
                        SummaryTotalTax = "0",
                        SummaryPaid = "0",
                        SummaryRemainder = "0",
                        FilterText = "",

                        FromDate = FromDate,
                        ToDate = ToDate,
                        User = Common.GetEmpName(MainClass.EmpNo),
                        PrintDate = DateTime.Now.ToShortDateString()
                    };

                    list.Add(inventoryData);
                }

                var dataSet = new DataSet("Name");
                var table = global::UtilitiesProj.Common.ToDataTable(list);
                dataSet.Tables.Add(table);
                list.Clear();
                return dataSet;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new DataSet();
            }
        }
    }
}