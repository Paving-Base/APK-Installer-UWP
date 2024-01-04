using Microsoft.Toolkit;
using System;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;

namespace APKInstaller.Helpers.Converter
{
    public class FileSizeToFriendlyStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string result = Converters.ToFileSizeString(System.Convert.ToInt64(value));
            return targetType.IsInstanceOfType(result) ? result : XamlBindingHelper.ConvertValue(targetType, result);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            targetType.IsInstanceOfType(value) ? value : XamlBindingHelper.ConvertValue(targetType, value);
    }
}
