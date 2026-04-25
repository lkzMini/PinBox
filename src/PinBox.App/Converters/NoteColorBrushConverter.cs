using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using PinBox.App.Models;
using Windows.UI;

namespace PinBox.App.Converters;

public sealed class NoteColorBrushConverter : IValueConverter
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
            PinNoteColorVariant.Peach => Color.FromArgb(255, 246, 217, 202),
            PinNoteColorVariant.Sage => Color.FromArgb(255, 221, 232, 210),
            PinNoteColorVariant.Blue => Color.FromArgb(255, 215, 227, 239),
            PinNoteColorVariant.Sand => Color.FromArgb(255, 239, 226, 191),
            PinNoteColorVariant.Lavender => Color.FromArgb(255, 230, 221, 241),
            _ => Color.FromArgb(255, 246, 217, 202)
        };
    }
}
