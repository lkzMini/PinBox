using PinBox.App.Models;

namespace PinBox.App.Services;

public sealed class SearchService
{
    public IEnumerable<PinNote> Apply(IEnumerable<PinNote> notes, NoteQuery query)
    {
        var filtered = notes;

        if (!query.IncludeArchived)
        {
            filtered = filtered.Where(note => !note.IsArchived);
        }

        if (query.PinnedOnly)
        {
            filtered = filtered.Where(note => note.IsPinned);
        }

        if (query.Type is not null)
        {
            filtered = filtered.Where(note => note.Type == query.Type);
        }

        var searchText = (query.SearchText ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filtered = filtered.Where(note => Matches(note, searchText));
        }

        return filtered;
    }

    private static bool Matches(PinNote note, string searchText)
    {
        return Contains(note.Title, searchText)
            || Contains(note.Content, searchText)
            || note.ChecklistItems.Any(item => Contains(item.Text, searchText));
    }

    private static bool Contains(string? value, string searchText)
    {
        return value?.IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0;
    }
}
