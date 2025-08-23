using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SigstreamTelemetryAgent.Converters   // <-- EXACT namespace
{
    public class LevelToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Fully-qualify the enum so we don't need a using in XAML compilation
            if (value is SigstreamTelemetryAgent.ViewModels.ActivityLevel level)
            {
                return level switch
                {
                    SigstreamTelemetryAgent.ViewModels.ActivityLevel.Error => Brushes.IndianRed,
                    SigstreamTelemetryAgent.ViewModels.ActivityLevel.Warn => Brushes.Goldenrod,
                    _ => Brushes.DodgerBlue
                };
            }
            return Brushes.DodgerBlue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
