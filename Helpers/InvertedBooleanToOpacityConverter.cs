using Microsoft.UI.Xaml.Data;
using System;

namespace Mechanical_Keyboard.Helpers
{
    public partial class InvertedBooleanToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // If CanImport is true, we want opacity 0 (invisible).
            // If CanImport is false, we want opacity 1 (visible).
            return value is bool b && b ? 0.0 : 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}