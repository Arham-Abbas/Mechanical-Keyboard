using System;
using Microsoft.UI.Xaml.Data;

namespace Mechanical_Keyboard.Helpers
{
    public partial class InvertedBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return !(value is bool b && b);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return !(value is bool b && b);
        }
    }
}
