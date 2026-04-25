using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using PinBox.App.Helpers;

namespace PinBox.App.Models;

public sealed class PinNote : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private string _title = string.Empty;
    private string _content = string.Empty;
    private PinNoteType _type = PinNoteType.PlainText;
    private DateTimeOffset _createdAt = DateTimeOffset.Now;
    private DateTimeOffset _updatedAt = DateTimeOffset.Now;
    private PinNoteColorVariant _colorVariant = PinNoteColorVariant.Peach;
    private bool _isPinned;
    private bool _isArchived;
    private ObservableCollection<ChecklistItem>? _checklistItems = new();

    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Title
    {
        get => _title;
        set
        {
            var normalizedValue = value ?? string.Empty;

            if (SetAndTouch(ref _title, normalizedValue))
            {
                OnPropertyChanged(nameof(DisplayTitle));
                OnPropertyChanged(nameof(Preview));
            }
        }
    }

    public string Content
    {
        get => _content;
        set
        {
            var normalizedValue = value ?? string.Empty;

            if (SetAndTouch(ref _content, normalizedValue))
            {
                OnPropertyChanged(nameof(Preview));
            }
        }
    }

    public PinNoteType Type
    {
        get => _type;
        set
        {
            if (SetAndTouch(ref _type, value))
            {
                OnPropertyChanged(nameof(TypeLabel));
                OnPropertyChanged(nameof(TypeGlyph));
                OnPropertyChanged(nameof(CardSubtitle));
                OnPropertyChanged(nameof(Preview));
                OnPropertyChanged(nameof(ChecklistSummary));
            }
        }
    }

    public DateTimeOffset CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    public DateTimeOffset UpdatedAt
    {
        get => _updatedAt;
        set
        {
            if (SetProperty(ref _updatedAt, value))
            {
                OnPropertyChanged(nameof(UpdatedDescription));
                OnPropertyChanged(nameof(CardSubtitle));
            }
        }
    }

    public PinNoteColorVariant ColorVariant
    {
        get => _colorVariant;
        set
        {
            if (SetAndTouch(ref _colorVariant, value))
            {
                OnPropertyChanged(nameof(ColorLabel));
            }
        }
    }

    public bool IsPinned
    {
        get => _isPinned;
        set
        {
            if (SetAndTouch(ref _isPinned, value))
            {
                OnPropertyChanged(nameof(PinActionText));
                OnPropertyChanged(nameof(CardSubtitle));
            }
        }
    }

    public bool IsArchived
    {
        get => _isArchived;
        set
        {
            if (SetAndTouch(ref _isArchived, value))
            {
                OnPropertyChanged(nameof(ArchiveActionText));
                OnPropertyChanged(nameof(CardSubtitle));
            }
        }
    }

    public ObservableCollection<ChecklistItem> ChecklistItems
    {
        get => _checklistItems ??= new ObservableCollection<ChecklistItem>();
        set
        {
            var normalizedItems = value ?? new ObservableCollection<ChecklistItem>();

            if (SetAndTouch(ref _checklistItems, normalizedItems))
            {
                RefreshChecklistDerivedProperties();
            }
        }
    }

    public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? "Untitled box" : Title.Trim();

    public string TypeLabel => Type == PinNoteType.Checklist ? "Checklist" : "Note";

    public string TypeGlyph => Type == PinNoteType.Checklist ? "☑" : "✎";

    public string ColorLabel => ColorVariant switch
    {
        PinNoteColorVariant.Peach => "Muted peach",
        PinNoteColorVariant.Sage => "Soft sage",
        PinNoteColorVariant.Blue => "Dusty blue",
        PinNoteColorVariant.Sand => "Warm sand",
        PinNoteColorVariant.Lavender => "Light lavender",
        _ => "Soft color"
    };

    public string PinActionText => IsPinned ? "Unpin" : "Pin";

    public string ArchiveActionText => IsArchived ? "Unarchive" : "Archive";

    public string UpdatedDescription => $"Updated {UpdatedAt.LocalDateTime:g}";

    public string CardSubtitle
    {
        get
        {
            var state = IsArchived ? "Archived" : IsPinned ? "Pinned" : "Board";
            return $"{TypeGlyph} {TypeLabel} · {state}";
        }
    }

    public string ChecklistSummary
    {
        get
        {
            if (Type != PinNoteType.Checklist)
            {
                return string.Empty;
            }

            var total = ChecklistItems.Count;
            var complete = ChecklistItems.Count(item => item.IsCompleted);
            return total == 0 ? "No items yet" : $"{complete}/{total} complete";
        }
    }

    public string Preview
    {
        get
        {
            if (Type == PinNoteType.Checklist)
            {
                var items = ChecklistItems
                    .Where(item => !string.IsNullOrWhiteSpace(item.Text))
                    .Take(4)
                    .Select(item => $"{(item.IsCompleted ? "✓" : "□")} {item.Text.Trim()}");

                var preview = string.Join(Environment.NewLine, items);
                return string.IsNullOrWhiteSpace(preview) ? "Add a few simple checklist items." : preview;
            }

            return string.IsNullOrWhiteSpace(Content) ? "Write a quick note here." : Content.Trim();
        }
    }

    public void TouchExternalChange()
    {
        UpdatedAt = DateTimeOffset.Now;
        RefreshChecklistDerivedProperties();
    }

    public void RefreshChecklistDerivedProperties()
    {
        OnPropertyChanged(nameof(Preview));
        OnPropertyChanged(nameof(ChecklistSummary));
    }

    private bool SetAndTouch<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!SetProperty(ref field, value, propertyName))
        {
            return false;
        }

        UpdatedAt = DateTimeOffset.Now;
        return true;
    }
}
