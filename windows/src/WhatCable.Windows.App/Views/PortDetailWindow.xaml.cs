using Microsoft.UI.Xaml;
using WhatCable.Windows.App.Core.Localization;
using WhatCable.Windows.App.Core.ViewModels;

namespace WhatCable.Windows.App.Views;

/// <summary>
/// Read-only detail view for a single port, opened from the tray popover. Mirrors the macOS
/// popover's expanded sections: connected USB devices, PD power profiles, cable e-marker trust
/// signals, and the cable summary. Section headers are localised; data is bound to
/// <see cref="PortViewModel"/>.
/// </summary>
public sealed partial class PortDetailWindow : Window
{
    private readonly PortViewModel _port;
    private readonly ILocalizer _localizer;

    public PortDetailWindow(PortViewModel port, ILocalizer localizer)
    {
        _port = port ?? throw new ArgumentNullException(nameof(port));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));

        InitializeComponent();

        Title = _port.Name;
        Root.DataContext = _port;
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        DevicesHeader.Text = _localizer.Get(StringKeys.PortConnectedDevices);
        PdHeader.Text = _localizer.Get(StringKeys.PortPdSource);
        TrustHeader.Text = _localizer.Get(StringKeys.PortCableTrustSignals);
        CableHeader.Text = _localizer.Get(StringKeys.PortActiveCableElectronics);
    }
}
