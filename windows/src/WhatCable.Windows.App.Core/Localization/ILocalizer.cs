namespace WhatCable.Windows.App.Core.Localization;

/// <summary>
/// Resolves localised UI strings. The WinUI app backs this with <c>ResourceLocalizer</c>, which uses
/// <c>Microsoft.Windows.ApplicationModel.Resources.ResourceManager</c> over the per-locale
/// <c>.resw</c> files; tests use <see cref="DictionaryLocalizer"/>.
/// </summary>
public interface ILocalizer
{
    /// <summary>Returns the localised string for <paramref name="key"/>, or the key itself if missing.</summary>
    string Get(string key);

    /// <summary>Returns the localised, <see cref="string.Format(string, object?[])"/>-style string for <paramref name="key"/>.</summary>
    string Format(string key, params object?[] args);
}
