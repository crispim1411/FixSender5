using System.Globalization;
using System.Windows.Data;
using FixSender5.Models;

namespace FixSender5.View;

public class StatusToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ConnectionState status)
        {
            return status != ConnectionState.Connecting; 
        }
        return true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}