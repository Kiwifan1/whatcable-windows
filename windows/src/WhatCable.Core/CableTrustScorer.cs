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
        else if (vendorId != 0xFFFF && !VendorDatabase.IsRegistered(vendorId))
        {
            flags.Add(new TrustFlag("vidNotInUSBIFList", "Vendor ID isn't in USB-IF's published list", "The reported vendor ID is not present in the bundled USB-IF registry data."));
        }

        if (cableVdo is not null)
        {
            foreach (var warning in cableVdo.DecodeWarnings)
            {
                var code = warning switch
                {
                    CableDecodeWarning.ReservedSpeedEncoding => "reservedSpeedEncoding",
                    CableDecodeWarning.ReservedCurrentEncoding => "reservedCurrentEncoding",
                    CableDecodeWarning.ReservedCableLatencyEncoding => "reservedCableLatencyEncoding",
                    CableDecodeWarning.InvalidVdoVersion => "invalidVDOVersion",
                    CableDecodeWarning.InvalidCableTermination => "invalidCableTermination",
                    CableDecodeWarning.EprClaimedWithLowMaxVoltage => "eprClaimedWithLowMaxVoltage",
                    _ => "unknown"
                };
                flags.Add(new TrustFlag(code, code, code));
            }
        }

        return flags;
    }
}
