using PinBox.App.Helpers;

namespace PinBox.App.Models;

public sealed class ChecklistItem : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private string _text = string.Empty;
    private bool _isCompleted;
    private DateTimeOffset _createdAt = DateTimeOffset.Now;
    private DateTimeOffset _updatedAt = DateTimeOffset.Now;

    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Text
    {
        get => _text;
        set
        {
            var normalizedValue = value ?? string.Empty;

            if (SetProperty(ref _text, normalizedValue))
            {
                Touch();
            }
        }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            if (SetProperty(ref _isCompleted, value))
            {
                Touch();
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
        set => SetProperty(ref _updatedAt, value);
    }

    private void Touch()
    {
        UpdatedAt = DateTimeOffset.Now;
    }
}
