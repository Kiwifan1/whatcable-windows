using WhatCable.Core;
using WhatCable.Video.Core;
using WhatCable.Windows.Backend.Power;
using WhatCable.Windows.Backend.Thunderbolt;
using WhatCable.Windows.Backend.Ucsi;
using WhatCable.Windows.Backend.Usb;
using WhatCable.Windows.Backend.Video;

namespace WhatCable.Windows.Backend;

/// <summary>
/// Combines the USB topology, power, UCSI, display, and Thunderbolt adapters into a
/// <see cref="WhatCableReport"/>: the macOS-schema <see cref="Snapshot"/> plus the Windows
/// <c>video</c> and <c>thunderbolt</c> sections. On a UCSI-capable PC the Type-C connectors are
/// surfaced with PD power profiles, cable e-marker VDOs, alt-mode lists, and (on UCSI 2.0)
/// liquid-detection state. On a host without UCSI those connectors are absent and the USB
/// host-controller ports carry an <c>unavailable_reason</c>.
/// </summary>
public sealed class SnapshotBuilder
{
    /// <summary>Schema version emitted in the snapshot, kept in step with the macOS payload.</summary>
    public const string SchemaVersion = "0.26.0";

    private readonly UsbTopologyAdapter _usb;
    private readonly PowerAdapter _power;
    private readonly UcsiAdapter? _ucsi;
    private readonly VideoPortAdapter? _video;
    private readonly ThunderboltChainAdapter? _thunderbolt;

    public SnapshotBuilder(IUsbEnumerator usbEnumerator, ISystemPowerSource powerSource)
        : this(usbEnumerator, powerSource, ucsiTransport: null, displayEnumerator: null, thunderboltEnumerator: null)
    {
    }

    public SnapshotBuilder(IUsbEnumerator usbEnumerator, ISystemPowerSource powerSource, IUcsiTransport? ucsiTransport)
        : this(usbEnumerator, powerSource, ucsiTransport, displayEnumerator: null, thunderboltEnumerator: null)
    {
    }

    public SnapshotBuilder(
        IUsbEnumerator usbEnumerator,
        ISystemPowerSource powerSource,
        IDisplayEnumerator? displayEnumerator,
        IThunderboltEnumerator? thunderboltEnumerator)
        : this(usbEnumerator, powerSource, ucsiTransport: null, displayEnumerator, thunderboltEnumerator)
    {
    }

    public SnapshotBuilder(
        IUsbEnumerator usbEnumerator,
        ISystemPowerSource powerSource,
        IUcsiTransport? ucsiTransport,
        IDisplayEnumerator? displayEnumerator,
        IThunderboltEnumerator? thunderboltEnumerator)
    {
        _usb = new UsbTopologyAdapter(usbEnumerator);
        _power = new PowerAdapter(powerSource);
        _ucsi = ucsiTransport is null ? null : new UcsiAdapter(ucsiTransport);
        _video = displayEnumerator is null ? null : new VideoPortAdapter(displayEnumerator);
        _thunderbolt = thunderboltEnumerator is null ? null : new ThunderboltChainAdapter(thunderboltEnumerator);
    }

    /// <summary>Builds just the macOS-schema snapshot.</summary>
    public Snapshot Build()
    {
        var ports = new List<Port>();

        // UCSI Type-C connectors (PD, e-marker, alt-mode, liquid detection) come first when present.
        if (_ucsi is { IsAvailable: true })
        {
            ports.AddRange(_ucsi.GetPorts());
        }

        ports.AddRange(_usb.GetPorts());

        return new Snapshot
        {
            Version = SchemaVersion,
            IsDesktopMac = false,
            Adapter = _power.GetAdapter(),
            Ports = ports,
        };
    }

    /// <summary>Per-display video link reports for the <c>video</c> CLI section.</summary>
    public IReadOnlyList<VideoPortReport> BuildVideo()
        => _video?.GetReports() ?? Array.Empty<VideoPortReport>();

    /// <summary>The Thunderbolt/USB4 device chain for the <c>thunderbolt</c> CLI section.</summary>
    public IReadOnlyList<ThunderboltDevice> BuildThunderbolt()
        => _thunderbolt?.GetChain() ?? Array.Empty<ThunderboltDevice>();

    /// <summary>Builds the full report (snapshot + Windows video / Thunderbolt sections).</summary>
    public WhatCableReport BuildReport() => new()
    {
        Snapshot = Build(),
        Video = BuildVideo(),
        Thunderbolt = BuildThunderbolt(),
    };

#if WINDOWS
    /// <summary>Creates a builder backed by the live Windows enumerators.</summary>
    public static SnapshotBuilder CreateDefault()
        => new(
            new SetupApiUsbEnumerator(),
            new WindowsSystemPowerSource(),
            new WindowsUcsiTransport(),
            new QueryDisplayConfigDisplayEnumerator(),
            new SetupApiThunderboltEnumerator());
#endif
}
