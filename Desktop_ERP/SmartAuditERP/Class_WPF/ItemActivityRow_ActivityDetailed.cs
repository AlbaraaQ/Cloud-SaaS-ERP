// ══════════════════════════════════════════════════════════
// Model: ItemActivityRow_ActivityDetailed.cs
// ضعه في مجلد Models أو في نهاية نفس ملف Code-Behind
// ══════════════════════════════════════════════════════════

using System.ComponentModel;

namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// نموذج صف حركة الصنف التفصيلي — يتطابق مع أعمدة DataGrid
    /// </summary>
    public class ItemActivityRow_ActivityDetailed : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this,
               new PropertyChangedEventArgs(name));

        public int    DgvNo         { get; set; }
        public string DgvInvType    { get; set; } = "";
        public string DgvStore      { get; set; } = "";
        public string DgvItemCode   { get; set; } = "";
        public string DgvItemName   { get; set; } = "";
        public string DgvInvNo      { get; set; } = "";
        public string DgvRefNo      { get; set; } = "";
        public string DgvDate       { get; set; } = "";
        public string DgvClient     { get; set; } = "";
        public string DgvUnit       { get; set; } = "";
        public double DgvQtyInv     { get; set; }
        public double DgvPriceInv   { get; set; }
        public double DgvTotInv     { get; set; }
        public double DgvIncomeQty  { get; set; }
        public double DgvOutcomeQty { get; set; }
        public double DgvBlc        { get; set; }
        public double DgvPrice      { get; set; }
        public double DgvTot        { get; set; }
    }
}