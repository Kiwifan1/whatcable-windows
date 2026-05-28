using System.Collections.Generic;
using System.Linq;

namespace WhatCable.Video.Core;

/// <summary>
/// Database of known video cable certifications.
/// </summary>
public static class KnownVideoCablesDatabase
{
    private static readonly List<KnownVideoCable> Cables = new List<KnownVideoCable>
    {
        // HDMI Ultra High Speed (HDMI 2.1) - Certified cables
        new KnownVideoCable
        {
            CertificationId = "HDMI-UHS-48G-001",
            CableClass = VideoCableClass.HdmiUltraHighSpeed,
            Vendor = "Belkin",
            Model = "Ultra High Speed HDMI Cable",
            Type = "HDMI"
        },
        new KnownVideoCable
        {
            CertificationId = "HDMI-UHS-48G-002",
            CableClass = VideoCableClass.HdmiUltraHighSpeed,
            Vendor = "Cable Matters",
            Model = "48Gbps 8K HDMI Cable",
            Type = "HDMI"
        },
        new KnownVideoCable
        {
            CertificationId = "HDMI-UHS-48G-003",
            CableClass = VideoCableClass.HdmiUltraHighSpeed,
            Vendor = "Zeskit",
            Model = "Maya 8K 48Gbps",
            Type = "HDMI"
        },

        // HDMI High Speed (HDMI 2.0)
        new KnownVideoCable
        {
            CertificationId = "HDMI-HS-18G-001",
            CableClass = VideoCableClass.HdmiHighSpeed,
            Vendor = "AmazonBasics",
            Model = "High-Speed HDMI Cable",
            Type = "HDMI"
        },
        new KnownVideoCable
        {
            CertificationId = "HDMI-HS-18G-002",
            CableClass = VideoCableClass.HdmiHighSpeed,
            Vendor = "Monoprice",
            Model = "Certified Premium High Speed HDMI",
            Type = "HDMI"
        },

        // DisplayPort 1.4 HBR3
        new KnownVideoCable
        {
            CertificationId = "DP14-HBR3-001",
            CableClass = VideoCableClass.DisplayPort14,
            Vendor = "Cable Matters",
            Model = "8K DisplayPort 1.4 Cable",
            Type = "DisplayPort"
        },
        new KnownVideoCable
        {
            CertificationId = "DP14-HBR3-002",
            CableClass = VideoCableClass.DisplayPort14,
            Vendor = "Club 3D",
            Model = "DisplayPort 1.4 HBR3",
            Type = "DisplayPort"
        },

        // DisplayPort 2.0 UHBR
        new KnownVideoCable
        {
            CertificationId = "DP20-UHBR20-001",
            CableClass = VideoCableClass.DisplayPort20UHBR20,
            Vendor = "Cable Matters",
            Model = "DisplayPort 2.0 UHBR20 Cable",
            Type = "DisplayPort"
        },
        new KnownVideoCable
        {
            CertificationId = "DP20-UHBR13-001",
            CableClass = VideoCableClass.DisplayPort20UHBR13_5,
            Vendor = "Club 3D",
            Model = "DisplayPort 2.0 UHBR13.5",
            Type = "DisplayPort"
        },

        // USB-C to DisplayPort cables
        new KnownVideoCable
        {
            CertificationId = "USBC-DP14-001",
            CableClass = VideoCableClass.UsbC3_1Gen2,
            Vendor = "Cable Matters",
            Model = "USB-C to DisplayPort Cable",
            Type = "USB-C"
        },
        new KnownVideoCable
        {
            CertificationId = "USBC-DP20-001",
            CableClass = VideoCableClass.UsbC4Gen3,
            Vendor = "CalDigit",
            Model = "Thunderbolt 4 USB-C Cable",
            Type = "USB-C"
        },

        // USB4 / Thunderbolt 4 (supports DP 2.0)
        new KnownVideoCable
        {
            CertificationId = "TB4-DP20-001",
            CableClass = VideoCableClass.UsbC4Gen3,
            Vendor = "Apple",
            Model = "Thunderbolt 4 Pro Cable",
            Type = "Thunderbolt"
        },
        new KnownVideoCable
        {
            CertificationId = "TB4-DP20-002",
            CableClass = VideoCableClass.UsbC4Gen3,
            Vendor = "CalDigit",
            Model = "Thunderbolt 4 Cable",
            Type = "Thunderbolt"
        }
    };

    /// <summary>
    /// Get all known video cables.
    /// </summary>
    public static IReadOnlyList<KnownVideoCable> GetAll() => Cables;

    /// <summary>
    /// Look up a cable by certification ID.
    /// </summary>
    public static KnownVideoCable? FindByCertificationId(string certificationId)
    {
        if (string.IsNullOrWhiteSpace(certificationId))
            return null;

        return Cables.FirstOrDefault(c => c.CertificationId.Equals(certificationId, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Find cables by cable class.
    /// </summary>
    public static IReadOnlyList<KnownVideoCable> FindByClass(VideoCableClass cableClass)
    {
        return Cables.Where(c => c.CableClass == cableClass).ToList();
    }

    /// <summary>
    /// Find cables by vendor.
    /// </summary>
    public static IReadOnlyList<KnownVideoCable> FindByVendor(string vendor)
    {
        if (string.IsNullOrWhiteSpace(vendor))
            return new List<KnownVideoCable>();

        return Cables.Where(c => c.Vendor != null && c.Vendor.IndexOf(vendor, System.StringComparison.OrdinalIgnoreCase) >= 0).ToList();
    }
}
