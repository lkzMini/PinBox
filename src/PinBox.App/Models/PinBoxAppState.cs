namespace PinBox.App.Models;

public sealed class PinBoxAppState
{
    public int SchemaVersion { get; set; } = 1;

    public List<PinNote> Notes { get; set; } = new();
}
