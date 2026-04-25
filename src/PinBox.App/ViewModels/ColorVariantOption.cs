using PinBox.App.Models;

namespace PinBox.App.ViewModels;

public sealed class ColorVariantOption
{
    public ColorVariantOption(string label, PinNoteColorVariant variant)
    {
        Label = label;
        Variant = variant;
    }

    public string Label { get; }

    public PinNoteColorVariant Variant { get; }
}
