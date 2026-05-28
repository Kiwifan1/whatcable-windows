namespace WhatCable.Windows.App.Core.Settings;

/// <summary>Loads and persists <see cref="AppSettings"/>.</summary>
public interface ISettingsStore
{
    AppSettings Load();

    void Save(AppSettings settings);
}
