using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BatuLabAiExcel;

/// <summary>
/// Converts boolean values to foreground brushes
/// </summary>
public class BoolToForegroundConverter : IValueConverter
{
    private static readonly SolidColorBrush UserBrush = Brushes.White;
    private static readonly SolidColorBrush AssistantBrush = new(Color.FromRgb(51, 51, 51)); // #333333

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isUser)
        {
            return isUser ? UserBrush : AssistantBrush;
        }
        
        return AssistantBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("ConvertBack is not supported for BoolToForegroundConverter");
    }
}