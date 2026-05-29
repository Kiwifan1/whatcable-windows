using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WhatCable.Core;
using WhatCable.Windows.App.Core.Localization;
using WhatCable.Windows.App.Core.Pro;

namespace WhatCable.Windows.App.Core.ViewModels;

/// <summary>
/// One node in the per-port USB device tree, mirroring the macOS popover's connected-device list.
/// </summary>
public sealed partial class UsbDeviceViewModel : ObservableObject
{
    public UsbDeviceViewModel(DeviceNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Name = string.IsNullOrWhiteSpace(node.Name) ? null : node.Name;
        Speed = node.Speed;
        VendorId = node.VendorId;
        ProductId = node.ProductId;
        Children = new ObservableCollection<UsbDeviceViewModel>(
            (node.Children ?? Array.Empty<DeviceNode>()).Select(c => new UsbDeviceViewModel(c)));
    }

    public string? Name { get; }

    public string Speed { get; }

    public int VendorId { get; }

    public int ProductId { get; }

    public ObservableCollection<UsbDeviceViewModel> Children { get; }
}

/// <summary>
/// ViewModel for a single USB-C / video port. Surfaces the popover summary (headline / subtitle /
/// status glyph) and the detail sections: USB device tree, PD profiles, cable e-marker trust
/// signals, and (when present) the video diagnostic. No UI-thread dependency — fully testable.
/// </summary>
public sealed partial class PortViewModel : ObservableObject
{
    private readonly ILocalizer _localizer;
    private readonly bool _proUnlocked;

    public PortViewModel(Port port, ILocalizer localizer, bool proUnlocked = false)
    {
        Port = port ?? throw new ArgumentNullException(nameof(port));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _proUnlocked = proUnlocked;

        Devices = new ObservableCollection<UsbDeviceViewModel>();
        if (port.Device is { } device)
        {
            Devices.Add(new UsbDeviceViewModel(device));
        }

        PowerProfiles = new ObservableCollection<ChargerProfile>(port.PowerSources);
        TrustSignals = new ObservableCollection<TrustFlag>(port.Cable?.TrustFlags ?? Array.Empty<TrustFlag>());

        _ucsi = UcsiPortView.FromPort(port);
        PinDiagram = _proUnlocked ? PinDiagramRenderer.Render(_ucsi) : null;
    }

    private readonly UcsiPortView? _ucsi;

    public Port Port { get; }

    public string Name => Port.Name;

    public string Headline => string.IsNullOrEmpty(Port.Headline)
        ? _localizer.Get(StringKeys.TrayNothingConnected)
        : Port.Headline;

    public string Subtitle => Port.Subtitle;

    public string Status => Port.Status;

    public bool IsConnected => Port.ConnectionActive;

    /// <summary>Glyph (Segoe Fluent Icons) reflecting connection state, for the popover list.</summary>
    public string Glyph => IsConnected ? "\uE703" : "\uE711"; // PlugConnected / Cancel

    public bool IsUnavailable => Port.UnavailableReason is not null;

    public string? UnavailableReason => Port.UnavailableReason;

    public ObservableCollection<UsbDeviceViewModel> Devices { get; }

    public ObservableCollection<ChargerProfile> PowerProfiles { get; }

    public ObservableCollection<TrustFlag> TrustSignals { get; }

    public Cable? Cable => Port.Cable;

    public bool HasDevices => Devices.Count > 0;

    public bool HasPowerProfiles => PowerProfiles.Count > 0;

    public bool HasTrustSignals => TrustSignals.Count > 0;

    public bool HasCable => Port.Cable is not null;

    /// <summary>True when the user has unlocked Pro features.</summary>
    public bool IsProUnlocked => _proUnlocked;

    /// <summary>True when this is a UCSI connector that the pin diagram (Pro) applies to.</summary>
    public bool SupportsPinDiagram => _ucsi is not null;

    /// <summary>The rendered pin diagram (Pro), or null when Pro is locked.</summary>
    public PinDiagram? PinDiagram { get; }

    /// <summary>True when a pin diagram is available and should be shown.</summary>
    public bool ShowPinDiagram => _proUnlocked && PinDiagram is { Available: true };

    /// <summary>True when the pin diagram is a Pro feature the user could unlock for this port.</summary>
    public bool ShowPinDiagramUpsell => !_proUnlocked && _ucsi is { Connected: true };

    /// <summary>Inline SVG markup for the pin diagram, when available.</summary>
    public string? PinDiagramSvg => ShowPinDiagram ? PinDiagram!.Svg : null;

    /// <summary>Machine-readable reason the pin diagram is unavailable, when applicable.</summary>
    public string? PinDiagramUnavailableReason
        => _proUnlocked ? PinDiagram?.UnavailableReason : null;
}
