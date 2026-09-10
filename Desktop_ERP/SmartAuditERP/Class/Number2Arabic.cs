using System;
using Microsoft.VisualBasic.CompilerServices;

namespace SmartAuditERP
{

	public class Number2Arabic
	{
		private static string GetAla7ad(long Num_Under2)
		{
			long num;
			num = Num_Under2 - 1;
			if ((ulong)num <= 8uL)
			{
				switch (num)
				{
					case 0L:
						return "واحد";
					case 1L:
						return "أثنان ";
					case 2L:
						return "ثلاثة";
					case 3L:
						return "أربعة";
					case 4L:
						return "خمسة";
					case 5L:
						return "ستة";
					case 6L:
						return "سبعة";
					case 7L:
						return "ثمانية";
					case 8L:
						return "تسعة";
				}
			}
			return "صفر";
		}

		private static string GetAl3asharat(int Num2)
		{
			return Num2 switch
			{
				1 => "عشرة",
				2 => "عشرون ",
				3 => "ثلاثون",
				4 => "أربعون",
				5 => "خمسون",
				6 => "ستون",
				7 => "سبعون",
				8 => "ثمانون",
				9 => "تسعون",
				_ => "صفر",
			};
		}

		private static string GetAlmeaat(int Num3)
		{
			return Num3 switch
			{
				1 => "مائة",
				2 => "مائتان ",
				3 => "ثلاثة مائة",
				4 => "أربعة مائة",
				5 => "خمسة مائة",
				6 => "ستة مائة",
				7 => "سبعة مائة",
				8 => "ثمان مائة",
				9 => "تسعة مائة",
				_ => "صفر",
			};
		}

		private static string GetAloloof(int Num4)
		{
			return Num4 switch
			{
				1 => "ألف",
				2 => "ألفان",
				3 => "ثلاثةألاف",
				4 => "أربعةألاف",
				5 => "خمسةألاف",
				6 => "ستةألاف",
				7 => "سبعةألاف",
				8 => "ثمانيةألاف",
				9 => "تسعةألاف",
				_ => "صفر",
			};
		}

		private static string Get3Digits(string Num)
		{
			string text;
			text = "";
			if (Operators.CompareString(Num.ToString(), "", TextCompare: false) == 0)
			{
				Num = "0";
			}
			int num;
			num = int.Parse(Num.ToString());
			if (num >= 0)
			{
				int num2;
				num2 = num / 100;
				int num3;
				num3 = num % 100;
				int num4;
				num4 = num3 / 10;
				int num5;
				num5 = num3 % 10;
				text = Number2Arabic.GetAla7ad(num5);
				string al3asharat;
				al3asharat = Number2Arabic.GetAl3asharat(num4);
				if (num4 != 0)
				{
					if (num4 == 1 && num5 != 0)
					{
						text += al3asharat;
					}
					else if (num5 != 0 && num4 != 0)
					{
						text = text + " و " + al3asharat;
					}
					else if (num4 != 0)
					{
						text = al3asharat;
					}
				}
				string almeaat;
				almeaat = Number2Arabic.GetAlmeaat(num2);
				if (num2 > 0 && Operators.CompareString(text, "صفر", TextCompare: false) != 0)
				{
					text = almeaat + " و " + text;
				}
				if (num2 != 0 && num4 == 0 && num5 == 0)
				{
					text = almeaat;
				}
			}
			return text;
		}

		private static string Get4Digits(string Num)
		{
			string text;
			text = "";
			if (Operators.CompareString(Num.ToString(), "", TextCompare: false) == 0)
			{
				Num = "0";
			}
			int num;
			num = int.Parse(Num.ToString());
			if (num >= 0)
			{
				int num2;
				num2 = num / 1000;
				int num3;
				num3 = num % 1000 / 100;
				int num4;
				num4 = num % 100;
				int num5;
				num5 = num4 / 10;
				int num6;
				num6 = num4 % 10;
				text = Number2Arabic.GetAla7ad(num6);
				string al3asharat;
				al3asharat = Number2Arabic.GetAl3asharat(num5);
				if (num5 != 0)
				{
					if (num5 == 1 && num6 != 0)
					{
						text += al3asharat;
					}
					else if (num6 != 0 && num5 != 0)
					{
						text = text + " و " + al3asharat;
					}
					else if (num5 != 0)
					{
						text = al3asharat;
					}
				}
				string almeaat;
				almeaat = Number2Arabic.GetAlmeaat(num3);
				if (num3 > 0 && Operators.CompareString(text, "صفر", TextCompare: false) != 0)
				{
					text = almeaat + " و " + text;
				}
				if (num3 != 0 && num5 == 0 && num6 == 0)
				{
					text = almeaat;
				}
				if (num2 > 0)
				{
					string aloloof;
					aloloof = Number2Arabic.GetAloloof(num2);
					text = ((Operators.CompareString(text, "صفر", TextCompare: false) == 0) ? aloloof : (aloloof + " و " + text));
				}
			}
			return text;
		}

		private static void Cal3Digits(string strType, ref string strNnumber, ref string ReturnValue)
		{
			if (strNnumber.Length <= 0)
			{
				return;
			}
			checked
			{
				if (strNnumber.Length < 3)
				{
					strNnumber = "000" + strNnumber;
					strNnumber = strNnumber.Substring(strNnumber.Length - 3, 3);
				}
				string text;
				text = Number2Arabic.Get3Digits(strNnumber.Substring(strNnumber.Length - 3, 3));
				text = ((Operators.CompareString(text, "صفر", TextCompare: false) == 0) ? "" : (text + strType));
				if (Operators.CompareString(ReturnValue, "صفر", TextCompare: false) != 0)
				{
					if (Operators.CompareString(text, "", TextCompare: false) != 0)
					{
						if (Operators.CompareString(ReturnValue, "", TextCompare: false) != 0)
						{
							ReturnValue = text + " و " + ReturnValue;
						}
						else
						{
							ReturnValue = text;
						}
					}
				}
				else
				{
					ReturnValue = text;
				}
				strNnumber = strNnumber.Substring(0, strNnumber.Length - 3);
			}
		}

		private static string Convert(string strNnumber)
		{
			string strType;
			strType = " ألف ";
			string strType2;
			strType2 = " مليون ";
			string strType3;
			strType3 = " مليار ";
			string strType4;
			strType4 = " بليون ";
			string strType5;
			strType5 = " ألف بليون ";
			string strType6;
			strType6 = " مليون بليون ";
			string strType7;
			strType7 = " مليار بليون ";
			string strType8;
			strType8 = " بليون بليون ";
			if (Operators.CompareString(strNnumber.ToString(), "", TextCompare: false) == 0)
			{
				strNnumber = "0";
			}
			checked
			{
				if (strNnumber.Length < 3)
				{
					strNnumber = "000" + strNnumber;
					strNnumber = strNnumber.Substring(strNnumber.Length - 3, 3);
				}
				string ReturnValue;
				if (strNnumber.Length == 4)
				{
					ReturnValue = Number2Arabic.Get4Digits(strNnumber);
				}
				else
				{
					ReturnValue = Number2Arabic.Get3Digits(strNnumber.Substring(strNnumber.Length - 3, 3));
					strNnumber = strNnumber.Substring(0, strNnumber.Length - 3);
					Number2Arabic.Cal3Digits(strType, ref strNnumber, ref ReturnValue);
					Number2Arabic.Cal3Digits(strType2, ref strNnumber, ref ReturnValue);
					Number2Arabic.Cal3Digits(strType3, ref strNnumber, ref ReturnValue);
					Number2Arabic.Cal3Digits(strType4, ref strNnumber, ref ReturnValue);
					Number2Arabic.Cal3Digits(strType5, ref strNnumber, ref ReturnValue);
					Number2Arabic.Cal3Digits(strType6, ref strNnumber, ref ReturnValue);
					Number2Arabic.Cal3Digits(strType7, ref strNnumber, ref ReturnValue);
					Number2Arabic.Cal3Digits(strType8, ref strNnumber, ref ReturnValue);
				}
				return ReturnValue;
			}
		}

		public static string ameral(string strNnumber)
		{
			string text;
			text = "";
			checked
			{
				try
				{
					long num;
					num = (long)double.Parse(strNnumber);
					string text2;
					text2 = num.ToString();
					int num2;
					num2 = strNnumber.Length - text2.Length;
					long num3;
					num3 = ((strNnumber.Length <= text2.Length) ? 0 : long.Parse(strNnumber.Substring(strNnumber.Length - num2 + 1, num2 - 1)));
					if (num > 0)
					{
						text = Number2Arabic.Convert(num.ToString());
					}
					if (num3 != 0)
					{
						text = ((Operators.CompareString(text, "", TextCompare: false) == 0) ? (Number2Arabic.Convert(num3.ToString()) + " من " + Number2Arabic.Convert(((int)Math.Pow(10.0, num2 - 1)).ToString())) : (text + " -  و " + Number2Arabic.Convert(num3.ToString()) + " من " + Number2Arabic.Convert(((int)Math.Pow(10.0, num2 - 1)).ToString())));
					}
				}
				catch (Exception projectError)
				{
					ProjectData.SetProjectError(projectError);
					strNnumber = "0";
					text = Number2Arabic.Convert(strNnumber);
					ProjectData.ClearProjectError();
				}
				return text;
			}
		}
	}
}