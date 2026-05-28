using System.Linq;
using WhatCable.Video.Core;
using WhatCable.Windows.Backend.Video;
using Xunit;

namespace WhatCable.Windows.Backend.Tests;

/// <summary>
/// Exercises the optional vendor GPU adapter plumbing with mocked adapters: the merge of
/// vendor link data into <see cref="VideoPortSnapshot"/>, the self-registration / discovery
/// gate, and the resulting higher-confidence diagnostic. No real vendor SDK is required.
/// </summary>
public sealed class VendorGpuAdapterTests
{
    private static byte[] LoadEdid(string name)
        => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "edid", name));

    [Fact]
    public void GetReports_WithVendorAdapter_MergesLinkInfoIntoSnapshot()
    {
        var link = new VendorGpuLinkInfo
        {
            Vendor = "NVIDIA",
            DpLinkRateGbps = 8.1,
            DpLaneCount = 4,
        };
        var vendor = new FakeVendorGpuAdapter(link);

        var enumerator = new FakeDisplayEnumerator(new DisplayInfo
        {
            Name = "Acme 4K120",
            ConnectorType = VideoConnectorType.DisplayPort,
            ActiveMode = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 60 },
            EdidRaw = LoadEdid("4k120-hdmi21.bin"),
        });

        var report = Assert.Single(new VideoPortAdapter(enumerator, new[] { (IVendorGpuAdapter)vendor }).GetReports());

        Assert.NotNull(report.Port.VendorGpuLink);
        Assert.Equal("NVIDIA", report.Port.VendorGpuLink!.Vendor);
        Assert.Equal(8.1, report.Port.VendorGpuLink.DpLinkRateGbps);
        Assert.Equal(4, report.Port.VendorGpuLink.DpLaneCount);
    }

    [Fact]
    public void GetReports_UnavailableVendorAdapter_IsNotQueried()
    {
        var vendor = new FakeVendorGpuAdapter(
            _ => throw new InvalidOperationException("must not be queried when unavailable"),
            isAvailable: false);

        var enumerator = new FakeDisplayEnumerator(new DisplayInfo
        {
            ConnectorType = VideoConnectorType.DisplayPort,
            ActiveMode = new VideoMode { WidthPx = 2560, HeightPx = 1440, RefreshRateHz = 144 },
            EdidRaw = null,
        });

        var report = Assert.Single(new VideoPortAdapter(enumerator, new[] { (IVendorGpuAdapter)vendor }).GetReports());

        Assert.Null(report.Port.VendorGpuLink);
    }

    [Fact]
    public void GetReports_VendorAdapterThrows_DoesNotBreakEnumeration()
    {
        var throwing = new FakeVendorGpuAdapter(_ => throw new InvalidOperationException("boom"));
        var working = new FakeVendorGpuAdapter(new VendorGpuLinkInfo { Vendor = "AMD" });

        var enumerator = new FakeDisplayEnumerator(new DisplayInfo
        {
            ConnectorType = VideoConnectorType.HDMI,
            ActiveMode = new VideoMode { WidthPx = 1920, HeightPx = 1080, RefreshRateHz = 60 },
            EdidRaw = null,
        });

        var report = Assert.Single(
            new VideoPortAdapter(enumerator, new IVendorGpuAdapter[] { throwing, working }).GetReports());

        Assert.NotNull(report.Port.VendorGpuLink);
        Assert.Equal("AMD", report.Port.VendorGpuLink!.Vendor);
    }

    [Fact]
    public void Discover_Disabled_ReturnsEmpty()
        => Assert.Empty(VendorGpuAdapters.Discover(enabled: false));

    [Fact]
    public void Discover_Enabled_WithoutSdks_ReturnsEmpty()
    {
        // On a developer workstation that has NVIDIA, AMD, or Intel graphics drivers installed,
        // the corresponding vendor DLL will be on the loader path and Discover() will return
        // non-empty. In that case we skip the "empty" assertion to avoid a misleading
        // "without SDKs" failure — we just verify each available adapter has a non-empty name
        // and dispose it cleanly.
        var adapters = VendorGpuAdapters.Discover(enabled: true);
        try
        {
            if (adapters.Count > 0)
            {
                foreach (var adapter in adapters)
                {
                    Assert.False(string.IsNullOrEmpty(adapter.Name));
                    Assert.True(adapter.IsAvailable);
                }
                return;
            }

            Assert.Empty(adapters);
        }
        finally
        {
            foreach (var adapter in adapters.OfType<IDisposable>())
            {
                adapter.Dispose();
            }
        }
    }
}
