using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using PinBox.App.Models;

namespace PinBox.App.Converters;

public sealed class NoteTypeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not PinNoteType noteType || parameter is null)
        {
            return Visibility.Collapsed;
        }

        return Enum.TryParse<PinNoteType>(parameter.ToString(), out var requestedType)
            && noteType == requestedType
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
