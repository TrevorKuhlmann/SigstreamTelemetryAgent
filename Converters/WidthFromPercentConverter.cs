using System;
using System.Globalization;
using System.Windows.Data;

namespace SigstreamTelemetryAgent.Converters
{
    public class WidthFromPercentConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is double total && values[1] is double pct)
            {
                if (double.IsNaN(total)) return 0d;
                pct = Math.Max(0, Math.Min(100, pct));
                return total * (pct / 100.0);
            }
            return 0d;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
