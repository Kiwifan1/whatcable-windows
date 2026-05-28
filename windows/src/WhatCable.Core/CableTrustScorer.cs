namespace WhatCable.Core;

public static class CableTrustScorer
{
    public static IReadOnlyList<TrustFlag> Evaluate(int vendorId, CableVdo? cableVdo)
    {
        var flags = new List<TrustFlag>();

        if (vendorId == 0)
        {
            flags.Add(new TrustFlag("zeroVendorID", "E-marker reports no vendor identity", "Legitimate USB-IF members ship cables with a non-zero vendor ID."));
        }
        else if (vendorId != 0xFFFF && VendorDatabase.HasAuthoritativeUsbIfRegistry && !VendorDatabase.IsRegistered(vendorId))
        {
            flags.Add(new TrustFlag("vidNotInUSBIFList", "Vendor ID isn't in USB-IF's published list", "The reported vendor ID is not present in the bundled USB-IF registry data."));
        }

        if (cableVdo is not null)
        {
            foreach (var warning in cableVdo.DecodeWarnings)
            {
                var flag = warning switch
                {
                    CableDecodeWarning.ReservedSpeedEncoding => new TrustFlag(
                        "reservedSpeedEncoding",
                        "E-marker uses a reserved data-speed value",
                        "The cable e-marker reports a speed value reserved by USB-PD and should not be emitted by valid silicon."),
                    CableDecodeWarning.ReservedCurrentEncoding => new TrustFlag(
                        "reservedCurrentEncoding",
                        "E-marker uses a reserved current-rating value",
                        "The cable e-marker reports a current encoding reserved by USB-PD and should not be emitted by valid silicon."),
                    CableDecodeWarning.ReservedCableLatencyEncoding => new TrustFlag(
                        "reservedCableLatencyEncoding",
                        "E-marker uses a reserved cable-latency value",
                        "The cable e-marker reports a latency encoding that is reserved for this cable type."),
                    CableDecodeWarning.InvalidVdoVersion => new TrustFlag(
                        "invalidVDOVersion",
                        "E-marker uses an invalid VDO version",
                        "The cable VDO version field does not contain a valid value for this cable type."),
                    CableDecodeWarning.InvalidCableTermination => new TrustFlag(
                        "invalidCableTermination",
                        "E-marker uses an invalid cable-termination value",
                        "The cable termination field does not contain a valid value for this cable type."),
                    CableDecodeWarning.EprClaimedWithLowMaxVoltage => new TrustFlag(
                        "eprClaimedWithLowMaxVoltage",
                        "E-marker claims EPR support but reports only 20V max VBUS",
                        "Passive cable EPR claim conflicts with its 20V max VBUS field."),
                    _ => new TrustFlag("unknown", "Unknown cable warning", "An unknown cable trust warning was emitted.")
                };
                flags.Add(flag);
            }
        }

        return flags;
    }
}
