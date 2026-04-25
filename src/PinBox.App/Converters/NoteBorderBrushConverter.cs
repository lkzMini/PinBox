using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using PinBox.App.Models;
using Windows.UI;

namespace PinBox.App.Converters;

public sealed class NoteBorderBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var variant = value is PinNoteColorVariant colorVariant ? colorVariant : PinNoteColorVariant.Peach;
        return new SolidColorBrush(ToColor(variant));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }

    private static Color ToColor(PinNoteColorVariant variant)
    {
        return variant switch
        {
            PinNoteColorVariant.Peach => Color.FromArgb(255, 233, 187, 165),
            PinNoteColorVariant.Sage => Color.FromArgb(255, 195, 212, 181),
            PinNoteColorVariant.Blue => Color.FromArgb(255, 184, 206, 225),
            PinNoteColorVariant.Sand => Color.FromArgb(255, 222, 200, 137),
            PinNoteColorVariant.Lavender => Color.FromArgb(255, 206, 192, 226),
            _ => Color.FromArgb(255, 233, 187, 165)
        };
    }
}
