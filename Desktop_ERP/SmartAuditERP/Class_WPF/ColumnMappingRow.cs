// ضعه في مجلد Models أو في نفس ملف Code-Behind
// ملف: ColumnMappingRow.cs

using System.ComponentModel;

namespace SmartAuditERP.Form_WPF
{
    /// <summary>
    /// يمثل صفًا واحدًا في جدول تطابق الأعمدة (dgvImportedData).
    /// </summary>
    public class ColumnMappingRow : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _fieldName = string.Empty;
        private int _mappedColumnIndex = -1;
        private string _defaultValue = string.Empty;

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public string FieldName
        {
            get => _fieldName;
            set { _fieldName = value; OnPropertyChanged(nameof(FieldName)); }
        }

        public int MappedColumnIndex
        {
            get => _mappedColumnIndex;
            set { _mappedColumnIndex = value; OnPropertyChanged(nameof(MappedColumnIndex)); }
        }

        public string DefaultValue
        {
            get => _defaultValue;
            set { _defaultValue = value; OnPropertyChanged(nameof(DefaultValue)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}