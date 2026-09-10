using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualBasic.CompilerServices;

namespace SmartAuditERP
{

	public class Geidea
	{
		[DllImport("madaapi.dll", CallingConvention = CallingConvention.StdCall)]
		public static extern int api_RequestCOMTrxn(int port, int Rate, int x, int y, int z, byte[] inOutBuff, int[] intval, int trnxType, int[] panNo, int[] purAmount, int[] stanNo, int[] dataTime, int[] expDate, int[] trxRrn, int[] authCode, ref byte[] rspCode, int[] terminalId, int[] schemeId, int[] merchantId, int[] addtlAmount, int[] ecrrefno);

		[DllImport("madaapi_v1_7.dll", CallingConvention = CallingConvention.StdCall)]
		public static extern int api_RequestCOMTrxn(int port, int Rate, int x, int y, int z, int[] inOutBuff, int[] intval, int trnxType, int[] panNo, int[] purAmount, int[] stanNo, int[] dataTime, int[] expDate, int trxRrn, int[] authCode, ref byte[] rspCode, int[] terminalId, int[] schemeId, int[] merchantId, int[] addtlAmount, int[] ecrrefno, int[] version, StringBuilder outResp, int outRespLen);

		public static byte[] ConnectGeidea(double Payment)
		{
			int[] array;
			array = new int[2];
			int[] panNo;
			panNo = new int[24];
			int[] array2;
			array2 = new int[14];
			int[] stanNo;
			stanNo = new int[8];
			int[] dataTime;
			dataTime = new int[14];
			int[] expDate;
			expDate = new int[6];
			int[] trxRrn;
			trxRrn = new int[14];
			int[] authCode;
			authCode = new int[8];
			byte[] rspCode;
			rspCode = new byte[5];
			int[] terminalId;
			terminalId = new int[18];
			int[] schemeId;
			schemeId = new int[4];
			int[] merchantId;
			merchantId = new int[17];
			int[] addtlAmount;
			addtlAmount = new int[14];
			int[] ecrrefno;
			ecrrefno = new int[18];
			int trnxType;
			trnxType = 0;
			array2[0] = 25;
			string s;
			s = Conversions.ToString(Payment * 100.0) + ";1;1!";
			byte[] bytes;
			bytes = Encoding.ASCII.GetBytes(s);
			array[0] = bytes.Length;
			Geidea.api_RequestCOMTrxn(1, 38400, 0, 8, 0, bytes, array, trnxType, panNo, array2, stanNo, dataTime, expDate, trxRrn, authCode, ref rspCode, terminalId, schemeId, merchantId, addtlAmount, ecrrefno);
			return rspCode;
		}
	}
}