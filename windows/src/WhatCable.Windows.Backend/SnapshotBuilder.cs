using WhatCable.Core;
using WhatCable.Windows.Backend.Power;
using WhatCable.Windows.Backend.Ucsi;
using WhatCable.Windows.Backend.Usb;

namespace WhatCable.Windows.Backend;

/// <summary>
/// Combines the USB topology, power, and UCSI adapters into a <see cref="Snapshot"/> matching the
/// macOS JSON schema. On a UCSI-capable PC the Type-C connectors are surfaced with PD power
/// profiles, cable e-marker VDOs, alt-mode lists, and (on UCSI 2.0) liquid-detection state. On a
/// host without UCSI those connectors are absent and the USB host-controller ports carry an
/// <c>unavailable_reason</c>.
/// </summary>
public sealed class SnapshotBuilder
{
    /// <summary>Schema version emitted in the snapshot, kept in step with the macOS payload.</summary>
    public const string SchemaVersion = "0.26.0";

    private readonly UsbTopologyAdapter _usb;
    private readonly PowerAdapter _power;
    private readonly UcsiAdapter? _ucsi;

    public SnapshotBuilder(IUsbEnumerator usbEnumerator, ISystemPowerSource powerSource)
        : this(usbEnumerator, powerSource, ucsiTransport: null)
    {
    }

    public SnapshotBuilder(IUsbEnumerator usbEnumerator, ISystemPowerSource powerSource, IUcsiTransport? ucsiTransport)
    {
        _usb = new UsbTopologyAdapter(usbEnumerator);
        _power = new PowerAdapter(powerSource);
        _ucsi = ucsiTransport is null ? null : new UcsiAdapter(ucsiTransport);
    }

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

#if WINDOWS
    /// <summary>Creates a builder backed by the live Windows enumerators.</summary>
    public static SnapshotBuilder CreateDefault()
        => new(new SetupApiUsbEnumerator(), new WindowsSystemPowerSource(), new WindowsUcsiTransport());
#endif
}
