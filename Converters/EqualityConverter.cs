using System;
using System.Globalization;
using System.Windows.Data;

namespace FocusPanel.Converters;

public class EqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        
        try 
        {
            // Convert both to double for numeric comparison
            double val = System.Convert.ToDouble(value);
            double param = System.Convert.ToDouble(parameter);
            return Math.Abs(val - param) < 0.001;
        }
        catch
        {
            return value.Equals(parameter);
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value.Equals(true) ? parameter : Binding.DoNothing;
    }
}