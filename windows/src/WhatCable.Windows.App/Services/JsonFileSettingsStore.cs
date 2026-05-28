using System.Text.Json;
using WhatCable.Windows.App.Core.Settings;

namespace WhatCable.Windows.App.Services;

/// <summary>
/// Persists <see cref="AppSettings"/> as JSON under <c>%LOCALAPPDATA%\WhatCable\settings.json</c>.
/// A file (rather than <c>ApplicationData.Current.LocalSettings</c>) keeps a single code path that
/// works for both the packaged MSIX build and the unpackaged Inno Setup build, which has no
/// package identity.
/// </summary>
public sealed class JsonFileSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly string _path;

    public JsonFileSettingsStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WhatCable",
            "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                return JsonSerializer.Deserialize<AppSettings>(json, Options) ?? new AppSettings();
            }
        }
        catch (Exception)
        {
            // Corrupt or unreadable settings fall back to defaults rather than crashing the app.
        }

        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(settings, Options));
    }
}
