using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using PinBox.App.Helpers;
using PinBox.App.Models;
using PinBox.App.Repositories;
using PinBox.App.Services;

namespace PinBox.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly PinBoxRepository _repository;
    private readonly SearchService _searchService;
    private readonly SortingService _sortingService;
    private readonly Dictionary<Guid, List<PropertyChangedEventHandler>> _checklistSubscriptions = new();
    private readonly Dictionary<Guid, ObservableCollection<ChecklistItem>> _observedChecklistCollections = new();
    private string _searchText = string.Empty;
    private NoteTypeFilterOption _selectedTypeFilter;
    private bool _showPinnedOnly;
    private bool _showArchived;
    private PinNote? _selectedNote;
    private string _saveStatus = "Autosaves locally";
    private CancellationTokenSource? _saveDebounceCts;
    private bool _isLoading;

    public MainViewModel(
        PinBoxRepository repository,
        SearchService searchService,
        SortingService sortingService)
    {
        _repository = repository;
        _searchService = searchService;
        _sortingService = sortingService;

        TypeFilters = new ReadOnlyCollection<NoteTypeFilterOption>(new[]
        {
            new NoteTypeFilterOption("All types", null),
            new NoteTypeFilterOption("Notes", PinNoteType.PlainText),
            new NoteTypeFilterOption("Checklists", PinNoteType.Checklist)
        });

        ColorVariants = new ReadOnlyCollection<ColorVariantOption>(new[]
        {
            new ColorVariantOption("Muted peach", PinNoteColorVariant.Peach),
            new ColorVariantOption("Soft sage", PinNoteColorVariant.Sage),
            new ColorVariantOption("Dusty blue", PinNoteColorVariant.Blue),
            new ColorVariantOption("Warm sand", PinNoteColorVariant.Sand),
            new ColorVariantOption("Light lavender", PinNoteColorVariant.Lavender)
        });

        _selectedTypeFilter = TypeFilters[0];

        CreatePlainNoteCommand = new RelayCommand(CreatePlainNote);
        CreateChecklistNoteCommand = new RelayCommand(CreateChecklistNote);
        ToggleSelectedPinCommand = new RelayCommand(() => TogglePin(SelectedNote), () => SelectedNote is not null);
        ArchiveSelectedCommand = new RelayCommand(() => ToggleArchive(SelectedNote), () => SelectedNote is not null);
        DeleteSelectedCommand = new RelayCommand(() => DeleteNote(SelectedNote), () => SelectedNote is not null);
        AddChecklistItemCommand = new RelayCommand(AddChecklistItem, () => SelectedNote?.Type == PinNoteType.Checklist);
    }

    public ObservableCollection<PinNote> Notes { get; } = new();

    public ObservableCollection<PinNote> FilteredNotes { get; } = new();

    public IReadOnlyList<NoteTypeFilterOption> TypeFilters { get; }

    public IReadOnlyList<ColorVariantOption> ColorVariants { get; }

    public RelayCommand CreatePlainNoteCommand { get; }

    public RelayCommand CreateChecklistNoteCommand { get; }

    public RelayCommand ToggleSelectedPinCommand { get; }

    public RelayCommand ArchiveSelectedCommand { get; }

    public RelayCommand DeleteSelectedCommand { get; }

    public RelayCommand AddChecklistItemCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                RefreshFilteredNotes();
            }
        }
    }

    public NoteTypeFilterOption SelectedTypeFilter
    {
        get => _selectedTypeFilter;
        set
        {
            if (SetProperty(ref _selectedTypeFilter, value ?? TypeFilters[0]))
            {
                RefreshFilteredNotes();
            }
        }
    }

    public bool ShowPinnedOnly
    {
        get => _showPinnedOnly;
        set
        {
            if (SetProperty(ref _showPinnedOnly, value))
            {
                RefreshFilteredNotes();
            }
        }
    }

    public bool ShowArchived
    {
        get => _showArchived;
        set
        {
            if (SetProperty(ref _showArchived, value))
            {
                RefreshFilteredNotes();
            }
        }
    }

    public PinNote? SelectedNote
    {
        get => _selectedNote;
        set
        {
            if (SetProperty(ref _selectedNote, value))
            {
                OnPropertyChanged(nameof(HasSelection));
                RaiseSelectionCommands();
            }
        }
    }

    public bool HasSelection => SelectedNote is not null;

    public bool HasFilteredNotes => FilteredNotes.Count > 0;

    public bool HasAnyNotes => Notes.Count > 0;

    public string EmptyStateTitle => HasAnyNotes ? "No boxes match this view" : "Your board is ready";

    public string EmptyStateBody => HasAnyNotes
        ? "Try clearing search, changing the type filter, or showing archived boxes."
        : "Create a quick note or a tiny checklist. Keep it calm, visible, and useful.";

    public string SaveStatus
    {
        get => _saveStatus;
        private set => SetProperty(ref _saveStatus, value);
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _isLoading = true;

        try
        {
            var state = await _repository.LoadAsync(cancellationToken);

            Notes.Clear();
            foreach (var note in state.Notes.Where(static note => note is not null))
            {
                NormalizeLoadedNote(note);
                SubscribeNote(note);
                Notes.Add(note);
            }

            RefreshFilteredNotes();
            SelectedNote = FilteredNotes.FirstOrDefault();
            SaveStatus = Notes.Count == 0 ? "Ready for your first box" : "Loaded from local storage";
        }
        finally
        {
            _isLoading = false;
        }
    }

    public async Task FlushSaveAsync(CancellationToken cancellationToken = default)
    {
        _saveDebounceCts?.Cancel();
        await SaveNowAsync(cancellationToken);
    }

    public void CreatePlainNote()
    {
        var note = new PinNote
        {
            Title = "New note",
            Content = string.Empty,
            Type = PinNoteType.PlainText,
            ColorVariant = PickNextColor(),
            CreatedAt = DateTimeOffset.Now,
            UpdatedAt = DateTimeOffset.Now
        };

        AddNote(note);
    }

    public void CreateChecklistNote()
    {
        var note = new PinNote
        {
            Title = "New checklist",
            Type = PinNoteType.Checklist,
            ColorVariant = PickNextColor(),
            CreatedAt = DateTimeOffset.Now,
            UpdatedAt = DateTimeOffset.Now
        };

        note.ChecklistItems.Add(new ChecklistItem { Text = "First item" });
        AddNote(note);
    }

    public void TogglePin(PinNote? note)
    {
        if (note is null)
        {
            return;
        }

        note.IsPinned = !note.IsPinned;
        RefreshFilteredNotes(keepSelection: note);
        QueueSave();
    }

    public void ToggleArchive(PinNote? note)
    {
        if (note is null)
        {
            return;
        }

        note.IsArchived = !note.IsArchived;
        RefreshFilteredNotes(keepSelection: ShowArchived ? note : null);
        if (!ShowArchived && note.IsArchived)
        {
            SelectedNote = FilteredNotes.FirstOrDefault();
        }

        QueueSave();
    }

    public void DeleteNote(PinNote? note)
    {
        if (note is null)
        {
            return;
        }

        UnsubscribeNote(note);
        Notes.Remove(note);
        RefreshFilteredNotes();

        if (SelectedNote == note)
        {
            SelectedNote = FilteredNotes.FirstOrDefault();
        }

        QueueSave(immediateStatus: "Deleted locally");
    }

    public void SetSelectedColor(PinNoteColorVariant colorVariant)
    {
        if (SelectedNote is null)
        {
            return;
        }

        SelectedNote.ColorVariant = colorVariant;
        QueueSave();
    }

    public void AddChecklistItem()
    {
        if (SelectedNote?.Type != PinNoteType.Checklist)
        {
            return;
        }

        var checklistItems = EnsureChecklistItems(SelectedNote);
        var item = new ChecklistItem { Text = string.Empty };
        checklistItems.Add(item);
        SelectedNote.TouchExternalChange();
        AddChecklistItemCommand.RaiseCanExecuteChanged();
        QueueSave(immediateStatus: "Checklist updated");
    }

    public void RemoveChecklistItem(ChecklistItem item)
    {
        if (SelectedNote?.Type != PinNoteType.Checklist)
        {
            return;
        }

        var checklistItems = EnsureChecklistItems(SelectedNote);
        if (checklistItems.Remove(item))
        {
            SelectedNote.TouchExternalChange();
            QueueSave(immediateStatus: "Checklist updated");
        }
    }

    private void AddNote(PinNote note)
    {
        NormalizeLoadedNote(note);
        SubscribeNote(note);
        Notes.Add(note);
        RefreshFilteredNotes(keepSelection: note);
        QueueSave(immediateStatus: "New box created");
    }

    private void RefreshFilteredNotes(PinNote? keepSelection = null)
    {
        var query = new NoteQuery
        {
            SearchText = SearchText,
            Type = SelectedTypeFilter.Type,
            PinnedOnly = ShowPinnedOnly,
            IncludeArchived = ShowArchived
        };

        var filtered = _sortingService.Apply(_searchService.Apply(Notes, query)).ToList();

        FilteredNotes.Clear();
        foreach (var note in filtered)
        {
            FilteredNotes.Add(note);
        }

        if (keepSelection is not null && FilteredNotes.Contains(keepSelection))
        {
            SelectedNote = keepSelection;
        }
        else if (SelectedNote is not null && !FilteredNotes.Contains(SelectedNote))
        {
            SelectedNote = FilteredNotes.FirstOrDefault();
        }

        OnPropertyChanged(nameof(HasFilteredNotes));
        OnPropertyChanged(nameof(HasAnyNotes));
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateBody));
    }

    private void NormalizeLoadedNote(PinNote note)
    {
        if (note.Id == Guid.Empty)
        {
            note.Id = Guid.NewGuid();
        }

        if (note.CreatedAt == default)
        {
            note.CreatedAt = DateTimeOffset.Now;
        }

        if (note.UpdatedAt == default)
        {
            note.UpdatedAt = note.CreatedAt;
        }

        var checklistItems = note.ChecklistItems;
        var sanitizedChecklistItems = checklistItems.Where(static item => item is not null).ToList();

        if (sanitizedChecklistItems.Count != checklistItems.Count)
        {
            checklistItems.Clear();

            foreach (var item in sanitizedChecklistItems)
            {
                checklistItems.Add(item);
            }
        }

        foreach (var item in checklistItems)
        {
            if (item.Id == Guid.Empty)
            {
                item.Id = Guid.NewGuid();
            }

            if (item.CreatedAt == default)
            {
                item.CreatedAt = note.CreatedAt;
            }

            if (item.UpdatedAt == default)
            {
                item.UpdatedAt = item.CreatedAt;
            }
        }
    }

    private void SubscribeNote(PinNote note)
    {
        note.PropertyChanged += Note_PropertyChanged;

        var checklistItems = EnsureChecklistItems(note);
        foreach (var item in checklistItems)
        {
            SubscribeChecklistItem(note, item);
        }
    }

    private void UnsubscribeNote(PinNote note)
    {
        note.PropertyChanged -= Note_PropertyChanged;

        if (_observedChecklistCollections.Remove(note.Id, out var observedCollection))
        {
            observedCollection.CollectionChanged -= ChecklistItems_CollectionChanged;
        }

        if (_checklistSubscriptions.TryGetValue(note.Id, out var handlers))
        {
            foreach (var item in note.ChecklistItems)
            {
                foreach (var handler in handlers)
                {
                    item.PropertyChanged -= handler;
                }
            }

            _checklistSubscriptions.Remove(note.Id);
        }
    }

    private void SubscribeChecklistItem(PinNote note, ChecklistItem item)
    {
        PropertyChangedEventHandler handler = (_, args) =>
        {
            if (!IsPersistedChecklistItemProperty(args.PropertyName))
            {
                return;
            }

            note.TouchExternalChange();
            QueueSave();
        };

        item.PropertyChanged += handler;

        if (!_checklistSubscriptions.TryGetValue(note.Id, out var handlers))
        {
            handlers = new List<PropertyChangedEventHandler>();
            _checklistSubscriptions[note.Id] = handlers;
        }

        handlers.Add(handler);
    }

    private void ChecklistItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is not ObservableCollection<ChecklistItem> collection)
        {
            return;
        }

        var note = Notes.FirstOrDefault(candidate => ReferenceEquals(candidate.ChecklistItems, collection));
        if (note is null)
        {
            return;
        }

        if (e.NewItems is not null)
        {
            foreach (ChecklistItem item in e.NewItems)
            {
                SubscribeChecklistItem(note, item);
            }
        }

        note.TouchExternalChange();
        QueueSave(immediateStatus: "Checklist updated");
    }

    private void Note_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isLoading || sender is not PinNote note)
        {
            return;
        }

        if (!IsPersistedNoteProperty(e.PropertyName))
        {
            return;
        }

        if (e.PropertyName is nameof(PinNote.ChecklistItems))
        {
            EnsureChecklistItems(note);
        }

        QueueSave();

        // Do not call RefreshFilteredNotes here. Text edits arrive on every key press;
        // rebuilding the board collection during typing causes WinUI to rebind and steal focus.
        if (e.PropertyName is nameof(PinNote.Type))
        {
            RaiseSelectionCommands();
        }
    }

    private ObservableCollection<ChecklistItem> EnsureChecklistItems(PinNote note)
    {
        var checklistItems = note.ChecklistItems;

        if (_observedChecklistCollections.TryGetValue(note.Id, out var observedCollection)
            && !ReferenceEquals(observedCollection, checklistItems))
        {
            observedCollection.CollectionChanged -= ChecklistItems_CollectionChanged;
            _observedChecklistCollections.Remove(note.Id);
        }

        if (!_observedChecklistCollections.TryGetValue(note.Id, out _))
        {
            checklistItems.CollectionChanged -= ChecklistItems_CollectionChanged;
            checklistItems.CollectionChanged += ChecklistItems_CollectionChanged;
            _observedChecklistCollections[note.Id] = checklistItems;
        }

        return checklistItems;
    }

    private static bool IsPersistedNoteProperty(string? propertyName)
    {
        return propertyName is nameof(PinNote.Title)
            or nameof(PinNote.Content)
            or nameof(PinNote.Type)
            or nameof(PinNote.ColorVariant)
            or nameof(PinNote.IsPinned)
            or nameof(PinNote.IsArchived)
            or nameof(PinNote.ChecklistItems);
    }

    private static bool IsPersistedChecklistItemProperty(string? propertyName)
    {
        return propertyName is nameof(ChecklistItem.Text)
            or nameof(ChecklistItem.IsCompleted);
    }

    private PinNoteColorVariant PickNextColor()
    {
        var values = Enum.GetValues<PinNoteColorVariant>();
        return values[Notes.Count % values.Length];
    }

    private void RaiseSelectionCommands()
    {
        ToggleSelectedPinCommand.RaiseCanExecuteChanged();
        ArchiveSelectedCommand.RaiseCanExecuteChanged();
        DeleteSelectedCommand.RaiseCanExecuteChanged();
        AddChecklistItemCommand.RaiseCanExecuteChanged();
    }

    private void QueueSave(string immediateStatus = "Autosaving locally...")
    {
        if (_isLoading)
        {
            return;
        }

        SaveStatus = immediateStatus;

        _saveDebounceCts?.Cancel();
        var cts = new CancellationTokenSource();
        _saveDebounceCts = cts;

        _ = SaveAfterDelayAsync(cts);
    }

    private async Task SaveAfterDelayAsync(CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(450, cts.Token);
            await SaveNowAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // A newer edit is waiting to be saved. That's expected.
        }
    }

    private async Task SaveNowAsync(CancellationToken cancellationToken = default)
    {
        if (_isLoading)
        {
            return;
        }

        try
        {
            var state = new PinBoxAppState
            {
                Notes = Notes.ToList()
            };

            await _repository.SaveAsync(state, cancellationToken);
            SaveStatus = $"Saved locally · {DateTimeOffset.Now.LocalDateTime:t}";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            SaveStatus = $"Save failed: {ex.Message}";
        }
    }
}
