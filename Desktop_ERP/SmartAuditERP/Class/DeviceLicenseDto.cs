using System;

namespace SmartAuditERP
{
    public class DeviceLicenseDto
    {
        public string CompanyName { get; set; }
        public string DeviceId { get; set; }
        public string Email { get; set; }
        public DateTime LicenseDate { get; set; }
        public DateTime ExpireDate { get; set; }
        public string Signature { get; set; }

        // ✅ قيمة افتراضية لتجنب null
        public string Features { get; set; } = "";
    }
}