using System.Drawing;

namespace SmartAuditERP
{

	public class ListItems
	{
		public int id { get; set; }

		public string name { get; set; }

		public string nameEN { get; set; }

		public string code { get; set; }

		public string barcode { get; set; }

		public string Grpcode { get; set; }

		public int group_id { get; set; }

		public int unit { get; set; }

		public bool ShowInPOS { get; set; }

		public int Wscale { get; set; }

		public decimal purch_price { get; set; }

		public decimal sale_price { get; set; }

		public int limit { get; set; }

		public decimal tax { get; set; }

		public decimal discount { get; set; }

		public decimal tax_group { get; set; }

		public Image image { get; set; }

		public int store { get; set; }

		public bool IS_Deleted { get; set; }

		public int ItemType { get; set; }

		public int ItemProperty { get; set; }

		public decimal FillValue { get; set; }

		public decimal MaxQtyLimit { get; set; }

		public decimal WithholdingTax { get; set; }

		public string EgyItemCode { get; set; }

		public string EgyCodeType { get; set; }

		public decimal MaxDicountParcent { get; set; }

		public bool is_extra_tax_applied { get; set; }

		public decimal MaxDiscountAmount { get; set; }

		public bool showInAndroid { get; set; }

		public int Item_Sort { get; set; }
	}
}