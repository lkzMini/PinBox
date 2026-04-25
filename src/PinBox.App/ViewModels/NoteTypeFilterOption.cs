using PinBox.App.Models;

namespace PinBox.App.ViewModels;

public sealed class NoteTypeFilterOption
{
    public NoteTypeFilterOption(string label, PinNoteType? type)
    {
        Label = label;
        Type = type;
    }

    public string Label { get; }

    public PinNoteType? Type { get; }
}
