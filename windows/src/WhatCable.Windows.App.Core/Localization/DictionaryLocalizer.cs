namespace WhatCable.Windows.App.Core.Localization;

/// <summary>
/// In-memory <see cref="ILocalizer"/> used by unit tests and as a safe fallback. Falls back to
/// the resource key itself when a string is missing so the UI never renders blank.
/// </summary>
public sealed class DictionaryLocalizer : ILocalizer
{
    private readonly IReadOnlyDictionary<string, string> _strings;

    public DictionaryLocalizer(IReadOnlyDictionary<string, string> strings)
        => _strings = strings ?? throw new ArgumentNullException(nameof(strings));

    public string Get(string key)
        => _strings.TryGetValue(key, out var value) ? value : key;

    public string Format(string key, params object?[] args)
        => string.Format(Get(key), args);
}
