namespace WhatCable.Windows.App.Core.Settings;

/// <summary>In-memory <see cref="ISettingsStore"/> for tests and design-time use.</summary>
public sealed class InMemorySettingsStore : ISettingsStore
{
    private AppSettings _settings;

    public InMemorySettingsStore(AppSettings? initial = null) => _settings = initial ?? new AppSettings();

    public AppSettings Load() => _settings;

    public void Save(AppSettings settings) => _settings = settings ?? throw new ArgumentNullException(nameof(settings));
}
