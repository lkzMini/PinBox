using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using PinBox.App.Models;
using PinBox.App.Repositories;
using PinBox.App.Services;
using PinBox.App.ViewModels;
using Windows.Graphics;
using WinRT.Interop;

namespace PinBox.App;

public sealed partial class MainWindow : Window
{
    private const int DefaultWindowWidth = 1180;
    private const int DefaultWindowHeight = 760;
    private const int MinWindowWidth = 1080;
    private const int MinWindowHeight = 720;

    private readonly WindowPlacementRepository _windowPlacementRepository = new();
    private AppWindow? _appWindow;
    private bool _isApplyingWindowBounds;

    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();

        ViewModel = new MainViewModel(
            new PinBoxRepository(),
            new SearchService(),
            new SortingService());

        Shell.DataContext = ViewModel;
        Title = "PinBox";

        _appWindow = GetAppWindow();
        ApplyInitialWindowPlacement();

        if (_appWindow is not null)
        {
            _appWindow.Changed += AppWindow_Changed;
        }

        Closed += MainWindow_Closed;
    }

    private async void Shell_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadAsync();
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        SaveWindowPlacement();
        await ViewModel.FlushSaveAsync();
    }

    private AppWindow? GetAppWindow()
    {
        var hWnd = WindowNative.GetWindowHandle(this);
        if (hWnd == IntPtr.Zero)
        {
            return null;
        }

        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        return AppWindow.GetFromWindowId(windowId);
    }

    private void ApplyInitialWindowPlacement()
    {
        if (_appWindow is null)
        {
            return;
        }

        var placement = _windowPlacementRepository.Load();
        var width = ClampDimension(placement?.Width ?? DefaultWindowWidth, MinWindowWidth);
        var height = ClampDimension(placement?.Height ?? DefaultWindowHeight, MinWindowHeight);

        _isApplyingWindowBounds = true;
        try
        {
            if (TryCreateSafeRestoreRect(placement, width, height, out var restoreRect))
            {
                _appWindow.MoveAndResize(restoreRect);
            }
            else
            {
                _appWindow.Resize(new SizeInt32(width, height));
            }
        }
        finally
        {
            _isApplyingWindowBounds = false;
        }
    }

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (!_isApplyingWindowBounds && args.DidSizeChange)
        {
            EnsureMinimumWindowSize();
        }
    }

    private void EnsureMinimumWindowSize()
    {
        if (_appWindow is null)
        {
            return;
        }

        var currentSize = _appWindow.Size;
        var width = ClampDimension(currentSize.Width, MinWindowWidth);
        var height = ClampDimension(currentSize.Height, MinWindowHeight);

        if (width == currentSize.Width && height == currentSize.Height)
        {
            return;
        }

        _isApplyingWindowBounds = true;
        try
        {
            _appWindow.Resize(new SizeInt32(width, height));
        }
        finally
        {
            _isApplyingWindowBounds = false;
        }
    }

    private void SaveWindowPlacement()
    {
        if (_appWindow is null)
        {
            return;
        }

        var size = _appWindow.Size;
        var position = _appWindow.Position;

        try
        {
            _windowPlacementRepository.Save(new WindowPlacementSettings
            {
                Width = ClampDimension(size.Width, MinWindowWidth),
                Height = ClampDimension(size.Height, MinWindowHeight),
                X = position.X,
                Y = position.Y
            });
        }
        catch (IOException)
        {
            // Window placement is convenience state; notes still flush below.
        }
        catch (UnauthorizedAccessException)
        {
            // Window placement is convenience state; notes still flush below.
        }
    }

    private static bool TryCreateSafeRestoreRect(
        WindowPlacementSettings? placement,
        int width,
        int height,
        out RectInt32 restoreRect)
    {
        restoreRect = default;

        if (placement?.X is not int x || placement.Y is not int y)
        {
            return false;
        }

        var center = new PointInt32(x + width / 2, y + height / 2);
        var displayArea = DisplayArea.GetFromPoint(center, DisplayAreaFallback.Nearest);
        var workArea = displayArea.WorkArea;

        var maxX = workArea.X + Math.Max(0, workArea.Width - width);
        var maxY = workArea.Y + Math.Max(0, workArea.Height - height);

        var safeX = Math.Clamp(x, workArea.X, maxX);
        var safeY = Math.Clamp(y, workArea.Y, maxY);

        restoreRect = new RectInt32(safeX, safeY, width, height);
        return true;
    }

    private static int ClampDimension(int value, int minimum)
    {
        return Math.Max(value, minimum);
    }

    private void CardPinButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PinNote note)
        {
            ViewModel.TogglePin(note);
        }
    }

    private void CardArchiveButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PinNote note)
        {
            ViewModel.ToggleArchive(note);
        }
    }

    private void CardDeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is PinNote note)
        {
            ViewModel.DeleteNote(note);
        }
    }

    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is string colorName
            && Enum.TryParse<PinNoteColorVariant>(colorName, out var colorVariant))
        {
            ViewModel.SetSelectedColor(colorVariant);
        }
    }

    private void RemoveChecklistItem_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is ChecklistItem item)
        {
            ViewModel.RemoveChecklistItem(item);
        }
    }
}
