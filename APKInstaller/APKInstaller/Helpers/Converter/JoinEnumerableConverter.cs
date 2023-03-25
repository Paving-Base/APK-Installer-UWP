﻿using System;
using System.Collections;
using System.Linq;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;

namespace APKInstaller.Helpers.Converter
{
    public class JoinEnumerableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            object result = value is IEnumerable list ? string.Join(parameter.ToString(), list.Cast<object>()) : value;
            return targetType.IsInstanceOfType(result) ? result : XamlBindingHelper.ConvertValue(targetType, result);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return targetType.IsInstanceOfType(value) ? value : XamlBindingHelper.ConvertValue(targetType, value);
        }
    }
}
