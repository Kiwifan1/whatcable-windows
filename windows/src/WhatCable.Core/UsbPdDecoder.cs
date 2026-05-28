using System.Buffers.Binary;

namespace WhatCable.Core;

public static class UsbPdDecoder
{
    public static IdHeaderVdo DecodeIdHeader(uint vdo) => new(
        UsbCommHost: ((vdo >> 31) & 1) == 1,
        UsbCommDevice: ((vdo >> 30) & 1) == 1,
        ModalOperation: ((vdo >> 26) & 1) == 1,
        UfpProductType: (ProductType)((int)((vdo >> 27) & 0b111)),
        DfpProductType: (ProductType)((int)((vdo >> 23) & 0b111)),
        VendorId: (int)(vdo & 0xFFFF));

    public static CertStatVdo DecodeCertStat(uint vdo) => new(vdo, vdo != 0);

    public static ProductVdo DecodeProduct(uint vdo) => new(
        ProductId: (int)(vdo & 0xFFFF),
        BcdDevice: (int)((vdo >> 16) & 0xFFFF));

    public static ProductTypeVdo1 DecodeProductTypeVdo1(uint vdo) => new(
        Usb4Supported: ((vdo >> 31) & 1) == 1,
        Usb3Supported: ((vdo >> 30) & 1) == 1,
        Mode: (int)(vdo & 0x1FFF),
        Raw: vdo);

    public static ProductTypeVdo2 DecodeProductTypeVdo2(uint vdo) => new(
        SbuSupported: ((vdo >> 31) & 1) == 1,
        VconnRequired: ((vdo >> 30) & 1) == 1,
        Raw: vdo);

    public static ProductTypeVdo3 DecodeProductTypeVdo3(uint vdo) => new(vdo);

    public static CableVdo DecodeCableVdo(uint vdo, bool isActive)
    {
        var speedBits = (int)(vdo & 0b111);
        var currentBits = (int)((vdo >> 5) & 0b11);
        var maxVoltageEncoded = (int)((vdo >> 9) & 0b11);
        var cableLatencyEncoded = (int)((vdo >> 13) & 0b1111);
        var vdoVersionEncoded = (int)((vdo >> 21) & 0b111);
        var cableTerminationEncoded = (int)((vdo >> 11) & 0b11);

        var warnings = new List<CableDecodeWarning>();
        if (!Enum.IsDefined(typeof(CableSpeed), speedBits)) warnings.Add(CableDecodeWarning.ReservedSpeedEncoding);
        if (!Enum.IsDefined(typeof(CableCurrent), currentBits)) warnings.Add(CableDecodeWarning.ReservedCurrentEncoding);

        var latencyInvalid = cableLatencyEncoded == 0 || (!isActive && cableLatencyEncoded >= 0b1001) || (isActive && cableLatencyEncoded >= 0b1011);
        if (latencyInvalid) warnings.Add(CableDecodeWarning.ReservedCableLatencyEncoding);

        var versionInvalid = isActive
            ? vdoVersionEncoded is not (0b000 or 0b010 or 0b011)
            : vdoVersionEncoded != 0;
        if (versionInvalid) warnings.Add(CableDecodeWarning.InvalidVdoVersion);

        var terminationInvalid = isActive ? cableTerminationEncoded < 0b10 : cableTerminationEncoded >= 0b10;
        if (terminationInvalid) warnings.Add(CableDecodeWarning.InvalidCableTermination);

        var eprCapable = ((vdo >> 17) & 1) == 1;
        if (!isActive && eprCapable && maxVoltageEncoded == 0) warnings.Add(CableDecodeWarning.EprClaimedWithLowMaxVoltage);

        var current = Enum.IsDefined(typeof(CableCurrent), currentBits) ? (CableCurrent)currentBits : CableCurrent.UsbDefault;
        var maxVolts = maxVoltageEncoded switch { 1 => 30, 2 => 40, 3 => 50, _ => 20 };
        var amps = current switch { CableCurrent.FiveAmp => 5d, _ => 3d };

        return new CableVdo(
            Speed: Enum.IsDefined(typeof(CableSpeed), speedBits) ? (CableSpeed)speedBits : CableSpeed.Usb20,
            Current: current,
            MaxVolts: maxVolts,
            MaxWatts: (int)Math.Round(maxVolts * amps),
            CableType: isActive ? CableType.Active : CableType.Passive,
            VbusThroughCable: ((vdo >> 4) & 1) == 1,
            MaxVoltageEncoded: maxVoltageEncoded,
            CableLatencyEncoded: cableLatencyEncoded,
            VdoVersionEncoded: vdoVersionEncoded,
            CableTerminationEncoded: cableTerminationEncoded,
            EprCapable: eprCapable,
            DecodeWarnings: warnings);
    }

    public static ActiveCableVdo2 DecodeActiveCableVdo2(uint vdo) => new(
        MaxOperatingTempC: (int)((vdo >> 24) & 0xFF),
        ShutdownTempC: (int)((vdo >> 16) & 0xFF),
        Usb4Supported: ((vdo >> 8) & 1) == 0,
        Usb2Supported: ((vdo >> 5) & 1) == 0,
        Usb32Supported: ((vdo >> 4) & 1) == 0,
        TwoLanesSupported: ((vdo >> 3) & 1) == 1,
        OpticallyIsolated: ((vdo >> 2) & 1) == 1,
        Usb4AsymmetricMode: ((vdo >> 1) & 1) == 1,
        UsbGen2OrHigher: (vdo & 1) == 1,
        Raw: vdo);

    public static uint? VdoFromData(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4) return null;
        return BinaryPrimitives.ReadUInt32LittleEndian(data);
    }
}

public enum ProductType { Undefined = 0, PdUsbHub = 1, PdUsbPeripheral = 2, PassiveCable = 3, ActiveCable = 4, Ama = 5, Vpd = 6, Other = 7 }
public enum CableSpeed { Usb20 = 0, Usb32Gen1 = 1, Usb32Gen2 = 2, Usb4Gen3 = 3, Usb4Gen4 = 4 }
public enum CableCurrent { UsbDefault = 0, ThreeAmp = 1, FiveAmp = 2 }
public enum CableType { Passive = 0, Active = 1 }
public enum CableDecodeWarning { ReservedSpeedEncoding, ReservedCurrentEncoding, ReservedCableLatencyEncoding, InvalidVdoVersion, InvalidCableTermination, EprClaimedWithLowMaxVoltage }

public sealed record IdHeaderVdo(bool UsbCommHost, bool UsbCommDevice, bool ModalOperation, ProductType UfpProductType, ProductType DfpProductType, int VendorId);
public sealed record CertStatVdo(uint Xid, bool IsPresent);
public sealed record ProductVdo(int ProductId, int BcdDevice);
public sealed record ProductTypeVdo1(bool Usb4Supported, bool Usb3Supported, int Mode, uint Raw);
public sealed record ProductTypeVdo2(bool SbuSupported, bool VconnRequired, uint Raw);
public sealed record ProductTypeVdo3(uint Raw);
public sealed record CableVdo(CableSpeed Speed, CableCurrent Current, int MaxVolts, int MaxWatts, CableType CableType, bool VbusThroughCable, int MaxVoltageEncoded, int CableLatencyEncoded, int VdoVersionEncoded, int CableTerminationEncoded, bool EprCapable, IReadOnlyList<CableDecodeWarning> DecodeWarnings);
public sealed record ActiveCableVdo2(int MaxOperatingTempC, int ShutdownTempC, bool Usb4Supported, bool Usb2Supported, bool Usb32Supported, bool TwoLanesSupported, bool OpticallyIsolated, bool Usb4AsymmetricMode, bool UsbGen2OrHigher, uint Raw);
