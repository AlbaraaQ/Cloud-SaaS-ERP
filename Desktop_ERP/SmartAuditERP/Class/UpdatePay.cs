using System;

namespace SmartAuditERP
{

	public class UpdatePay : EventArgs
	{
        private PassPayment _payType;
        public PassPayment payType => this._payType;

		public UpdatePay(PassPayment payType)
		{
			this._payType = payType;
		}
	}
}