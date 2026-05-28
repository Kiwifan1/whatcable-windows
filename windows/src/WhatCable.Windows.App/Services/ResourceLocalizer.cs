using Microsoft.Windows.ApplicationModel.Resources;
using WhatCable.Windows.App.Core.Localization;

namespace WhatCable.Windows.App.Services;

/// <summary>
/// <see cref="ILocalizer"/> backed by the packaged <c>.resw</c> resources (resource map "Resources").
/// View-independent so it can be used off the UI thread; falls back to the key when a string is missing.
/// </summary>
public sealed class ResourceLocalizer : ILocalizer
{
    private readonly ResourceMap _map;

    public ResourceLocalizer()
    {
        var manager = new ResourceManager();
        _map = manager.MainResourceMap.GetSubtree("Resources");
    }

    public string Get(string key)
    {
        try
        {
            return _map.GetValue(key)?.ValueAsString ?? key;
        }
        catch (Exception)
        {
            // Missing resource: surface the key so the UI is never blank and the gap is obvious.
            return key;
        }
    }

    public string Format(string key, params object?[] args)
        => string.Format(Get(key), args);
}
