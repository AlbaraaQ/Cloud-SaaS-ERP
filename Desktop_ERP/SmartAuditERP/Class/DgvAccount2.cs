// усн: DgvAccount2.cs
using System.ComponentModel;

namespace SmartAuditERP
{
    public class DgvAccount2 : INotifyPropertyChanged
    {
        private int _accRowIndex;
        private string _accCode = string.Empty;
        private string _accName = string.Empty;
        private double _accDebit;
        private double _accCredit;
        private int _accCostCenter = -1;
        private string _accDescription = string.Empty;
        private string _salesman = "-1";

        public int AccRowIndex { get => _accRowIndex; set { _accRowIndex = value; OnPropertyChanged(nameof(AccRowIndex)); } }
        public string AccCode { get => _accCode; set { _accCode = value; OnPropertyChanged(nameof(AccCode)); } }
        public string AccName { get => _accName; set { _accName = value; OnPropertyChanged(nameof(AccName)); } }
        public double AccDebit { get => _accDebit; set { _accDebit = value; OnPropertyChanged(nameof(AccDebit)); } }
        public double AccCredit { get => _accCredit; set { _accCredit = value; OnPropertyChanged(nameof(AccCredit)); } }
        public int AccCostCenter { get => _accCostCenter; set { _accCostCenter = value; OnPropertyChanged(nameof(AccCostCenter)); } }
        public string AccDescription { get => _accDescription; set { _accDescription = value; OnPropertyChanged(nameof(AccDescription)); } }
        public string salesman { get => _salesman; set { _salesman = value; OnPropertyChanged(nameof(salesman)); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

// усн: SearchEntryRow.cs
namespace SmartAuditERP
{
    public class SearchEntryRow
    {
        public string GlobalID { get; set; } = string.Empty;
        public int EntryNo { get; set; }
        public string EntryDate { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}