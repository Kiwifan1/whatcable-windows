using WhatCable.Core;
using WhatCable.Video.Core;
using WhatCable.Windows.Backend.Power;
using WhatCable.Windows.Backend.Thunderbolt;
using WhatCable.Windows.Backend.Usb;
using WhatCable.Windows.Backend.Video;

namespace WhatCable.Windows.Backend;

/// <summary>
/// Combines the USB topology, power, display, and Thunderbolt adapters into a
/// <see cref="WhatCableReport"/>: the macOS-schema <see cref="Snapshot"/> plus the Windows
/// <c>video</c> and <c>thunderbolt</c> sections. PD power profiles and cable e-marker data
/// require UCSI (PR 5), so on a basic host they are left null/empty and each port carries an
/// <c>unavailable_reason</c>.
/// </summary>
public sealed class SnapshotBuilder
{
    /// <summary>Schema version emitted in the snapshot, kept in step with the macOS payload.</summary>
    public const string SchemaVersion = "0.26.0";

    private readonly UsbTopologyAdapter _usb;
    private readonly PowerAdapter _power;
    private readonly VideoPortAdapter? _video;
    private readonly ThunderboltChainAdapter? _thunderbolt;

    public SnapshotBuilder(IUsbEnumerator usbEnumerator, ISystemPowerSource powerSource)
        : this(usbEnumerator, powerSource, displayEnumerator: null, thunderboltEnumerator: null)
    {
    }

    public SnapshotBuilder(
        IUsbEnumerator usbEnumerator,
        ISystemPowerSource powerSource,
        IDisplayEnumerator? displayEnumerator,
        IThunderboltEnumerator? thunderboltEnumerator)
    {
        _usb = new UsbTopologyAdapter(usbEnumerator);
        _power = new PowerAdapter(powerSource);
        _video = displayEnumerator is null ? null : new VideoPortAdapter(displayEnumerator);
        _thunderbolt = thunderboltEnumerator is null ? null : new ThunderboltChainAdapter(thunderboltEnumerator);
    }

    /// <summary>Builds just the macOS-schema snapshot.</summary>
    public Snapshot Build() => new()
    {
        Version = SchemaVersion,
        IsDesktopMac = false,
        Adapter = _power.GetAdapter(),
        Ports = _usb.GetPorts(),
    };

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
            new QueryDisplayConfigDisplayEnumerator(),
            new SetupApiThunderboltEnumerator());
#endif
}
