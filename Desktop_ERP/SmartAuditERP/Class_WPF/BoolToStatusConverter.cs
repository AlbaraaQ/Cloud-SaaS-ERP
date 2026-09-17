using System;
using System.Globalization;
using System.Windows.Data;

namespace SmartAuditERP.Form_WPF
{
    public class BoolToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool isActive && isActive ? "✅ نشط" : "❌ غير نشط";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}