namespace WhatCable.Widgets.Core;

/// <summary>
/// Stable identifiers shared between the WhatCable WinUI app (the IPC publisher) and the
/// <c>WhatCable.Widgets</c> COM server (the widget provider). These values are mirrored in the
/// app's MSIX <c>Package.appxmanifest</c> and must be kept in sync with it.
/// </summary>
public static class WidgetCatalog
{
    /// <summary>
    /// CLSID of the <c>IWidgetProvider</c> COM class, as declared in the manifest's
    /// <c>com:ExeServer</c> / <c>CreateInstance</c> elements.
    /// </summary>
    public const string ProviderClassId = "739FA740-7E5C-48DD-9AFE-C78E4FBCC14E";

    /// <summary>The <c>com.microsoft.windows.widgets</c> app-extension id from the manifest.</summary>
    public const string AppExtensionId = "uk.whatcable.widgets";

    /// <summary>
    /// Widget definition id for the cable-status widget. Matches the <c>Definition Id</c> in the
    /// manifest and the <c>WidgetContext.DefinitionId</c> the host passes back to the provider.
    /// </summary>
    public const string CableStatusDefinitionId = "WhatCable_Status_Widget";

    /// <summary>
    /// Name of the named pipe the app publishes snapshot updates on and the provider listens on.
    /// Scoped to the local machine; the provider reads, the app writes.
    /// </summary>
    public const string SnapshotPipeName = "WhatCable.Widgets.Snapshot";
}
