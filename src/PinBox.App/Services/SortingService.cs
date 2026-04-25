using PinBox.App.Models;

namespace PinBox.App.Services;

public sealed class SortingService
{
    public IEnumerable<PinNote> Apply(IEnumerable<PinNote> notes)
    {
        return notes
            .OrderBy(note => note.IsArchived)
            .ThenByDescending(note => note.IsPinned)
            .ThenByDescending(note => note.UpdatedAt);
    }
}
