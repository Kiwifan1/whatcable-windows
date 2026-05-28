using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace WhatCable.Windows.App.Converters;

/// <summary>
/// Collapses an element when its bound <see cref="bool"/> is <c>false</c>. WinUI 3 ships no
/// built-in bool→Visibility converter, so the detail/settings views use this one. Pass
/// <c>Invert</c> as the converter parameter to flip the mapping.
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var flag = value is bool b && b;
        if (string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase))
        {
            flag = !flag;
        }

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility visibility && visibility == Visibility.Visible;
}
