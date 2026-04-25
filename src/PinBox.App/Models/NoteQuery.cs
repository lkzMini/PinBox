namespace PinBox.App.Models;

public sealed class NoteQuery
{
    public string SearchText { get; init; } = string.Empty;

    public PinNoteType? Type { get; init; }

    public bool PinnedOnly { get; init; }

    public bool IncludeArchived { get; init; }
}
