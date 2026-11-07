using System.IO;
using System.Text.Json;
using AffectionIndicator.Models;

namespace AffectionIndicator.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly string _directory;
    private readonly string _path;

    public AppSettings Settings { get; private set; }

    public event EventHandler<AppSettings>? Changed;

    public SettingsService()
    {
        _directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AffectionIndicator");
        _path = Path.Combine(_directory, "settings.json");
        Settings = Load();
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded != null)
                {
                    loaded.Normalize();
                    return loaded;
                }
            }
        }
        catch
        {
            // A malformed settings file must not stop the indicator from starting.
        }

        var defaults = new AppSettings();
        defaults.Normalize();
        return defaults;
    }

    public void Save(AppSettings settings)
    {
        settings.Normalize();
        Settings = settings;
        try
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_path, JsonSerializer.Serialize(settings, JsonOptions));
        }
        catch
        {
            // Keep the in-memory settings even if the profile cannot be written.
        }

        Changed?.Invoke(this, settings);
    }

    public string SettingsPath => _path;
}
