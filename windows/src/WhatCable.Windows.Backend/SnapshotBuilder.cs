using WhatCable.Core;
using WhatCable.Windows.Backend.Power;
using WhatCable.Windows.Backend.Usb;

namespace WhatCable.Windows.Backend;

/// <summary>
/// Combines the USB topology and power adapters into a <see cref="Snapshot"/> matching the
/// macOS JSON schema. PD power profiles and cable e-marker data require UCSI (PR 5), so on a
/// basic host they are left null/empty and each port carries an <c>unavailable_reason</c>.
/// </summary>
public sealed class SnapshotBuilder
{
    /// <summary>Schema version emitted in the snapshot, kept in step with the macOS payload.</summary>
    public const string SchemaVersion = "0.26.0";

    private readonly UsbTopologyAdapter _usb;
    private readonly PowerAdapter _power;

    public SnapshotBuilder(IUsbEnumerator usbEnumerator, ISystemPowerSource powerSource)
    {
        _usb = new UsbTopologyAdapter(usbEnumerator);
        _power = new PowerAdapter(powerSource);
    }

    public Snapshot Build() => new()
    {
        Version = SchemaVersion,
        IsDesktopMac = false,
        Adapter = _power.GetAdapter(),
        Ports = _usb.GetPorts(),
    };

#if WINDOWS
    /// <summary>Creates a builder backed by the live Windows enumerators.</summary>
    public static SnapshotBuilder CreateDefault()
        => new(new SetupApiUsbEnumerator(), new WindowsSystemPowerSource());
#endif
}
