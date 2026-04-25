using System.Text.Json;
using System.Text.Json.Serialization;
using PinBox.App.Models;

namespace PinBox.App.Repositories;

public sealed class PinBoxRepository
{
    private const string AppFolderName = "PinBox";
    private const string FileName = "pinbox-notes.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public string DataFilePath
    {
        get
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, AppFolderName, FileName);
        }
    }

    public async Task<PinBoxAppState> LoadAsync(CancellationToken cancellationToken = default)
    {
        var path = DataFilePath;

        if (!File.Exists(path))
        {
            return new PinBoxAppState();
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var state = await JsonSerializer.DeserializeAsync<PinBoxAppState>(stream, JsonOptions, cancellationToken);

            return NormalizeLoadedState(state);
        }
        catch (JsonException)
        {
            return new PinBoxAppState();
        }
    }

    public async Task SaveAsync(PinBoxAppState state, CancellationToken cancellationToken = default)
    {
        var path = DataFilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var tempPath = $"{path}.tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, state, JsonOptions, cancellationToken);
        }

        File.Copy(tempPath, path, overwrite: true);
        File.Delete(tempPath);
    }

    private static PinBoxAppState NormalizeLoadedState(PinBoxAppState? state)
    {
        state ??= new PinBoxAppState();
        state.Notes ??= new List<PinNote>();

        if (state.SchemaVersion <= 0)
        {
            state.SchemaVersion = 1;
        }

        return state;
    }
}
