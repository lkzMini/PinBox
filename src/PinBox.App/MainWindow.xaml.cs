using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PinBox.App.Models;
using PinBox.App.Repositories;
using PinBox.App.Services;
using PinBox.App.ViewModels;

namespace PinBox.App;

public sealed partial class MainWindow : Window
{
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

        Closed += MainWindow_Closed;
    }

    private async void Shell_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadAsync();
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        await ViewModel.FlushSaveAsync();
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
