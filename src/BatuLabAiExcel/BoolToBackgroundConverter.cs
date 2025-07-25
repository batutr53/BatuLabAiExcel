using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BatuLabAiExcel;

/// <summary>
/// Converts boolean values to background brushes
/// </summary>
public class BoolToBackgroundConverter : IValueConverter
{
    private static readonly SolidColorBrush UserBrush = new(Color.FromRgb(0, 120, 212)); // #0078D4
    private static readonly SolidColorBrush AssistantBrush = new(Color.FromRgb(245, 245, 245)); // #F5F5F5

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
        throw new NotImplementedException("ConvertBack is not supported for BoolToBackgroundConverter");
    }
}