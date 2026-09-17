// ═══════════════════════════════════════════════════════════
//                     Model Classes
// ═══════════════════════════════════════════════════════════

namespace SmartAuditERP.Form_WPF
{
    // ─── frmSafeGrd Models ───

    /// <summary>
    /// نموذج صف جدول كشف الجرد (GridControl1)
    /// </summary>
    public class InventoryGridItem
    {
        public int    DgvNo             { get; set; }
        public string DgvItemCode       { get; set; }
        public string DgvItemName       { get; set; }
        public string DgvBarcode        { get; set; }
        public double DgvQty            { get; set; }
        public double DgvAveCost        { get; set; }
        public double DgvTotalCost      { get; set; }
        public double DgvSalePrice      { get; set; }
        public double DgvTotalSalePrice { get; set; }
        public int    DgvItemId         { get; set; }
    }

    // ─── frmSafes Models ───

    /// <summary>
    /// نموذج صف قائمة المخازن (dgvSafes)
    /// </summary>
    public class SafeListItem
    {
        public int    SafeId     { get; set; }
        public string SafeName   { get; set; }
        public string BranchName { get; set; }
    }

    /// <summary>
    /// نموذج صف جدول مسئولي المخزن (dgvSafeEmps)
    /// </summary>
    public class SafeEmployeeItem
    {
        public int EmpId { get; set; }
    }
}