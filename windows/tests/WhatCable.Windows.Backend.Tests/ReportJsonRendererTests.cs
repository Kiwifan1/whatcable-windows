using System.Text.Json;
using System.Text.Json.Nodes;
using WhatCable.Core;
using WhatCable.Video.Core;
using WhatCable.Windows.Backend.Power;
using WhatCable.Windows.Backend.Thunderbolt;
using WhatCable.Windows.Backend.Usb;
using WhatCable.Windows.Backend.Video;
using Xunit;

namespace WhatCable.Windows.Backend.Tests;

/// <summary>
/// Schema test for the CLI payload: the <c>video</c> and <c>thunderbolt</c> sections are
/// present by default and suppressed by <c>--legacy</c> (the macOS-compatible schema).
/// </summary>
public sealed class ReportJsonRendererTests
{
    private static WhatCableReport BuildReport() => new SnapshotBuilder(
        new FakeUsbEnumerator(new UsbHostController
        {
            Name = "USB Host Controller 1",
            Devices = new[]
            {
                new UsbDeviceInfo { Name = "Keyboard", PortNumber = 1, Speed = UsbSpeed.Full },
            },
        }),
        new FakeSystemPowerSource(new SystemPowerInfo { AcOnline = true }),
        new FakeDisplayEnumerator(new DisplayInfo
        {
            Name = "Acme 4K120",
            ConnectorType = VideoConnectorType.HDMI,
            ActiveMode = new VideoMode { WidthPx = 3840, HeightPx = 2160, RefreshRateHz = 60 },
        }),
        new FakeThunderboltEnumerator(new ThunderboltDeviceInfo
        {
            Name = "TB Controller",
            InstanceId = "PCI\\TB#0",
        })).BuildReport();

    [Fact]
    public void Render_Default_IncludesVideoAndThunderboltSections()
    {
        var json = ReportJsonRenderer.Render(BuildReport(), legacy: false);
        var root = JsonNode.Parse(json)!.AsObject();

        Assert.True(root.ContainsKey("video"));
        Assert.True(root.ContainsKey("thunderbolt"));
        Assert.Single(root["video"]!.AsArray());
        Assert.Single(root["thunderbolt"]!.AsArray());

        var tb = root["thunderbolt"]!.AsArray()[0]!.AsObject();
        Assert.False(tb["perLaneSpeedsAvailable"]!.GetValue<bool>());
        Assert.Equal(
            ThunderboltChainAdapter.PerLaneUnavailableReason,
            tb["unavailable_reason"]!.GetValue<string>());
    }

    [Fact]
    public void Render_Legacy_SuppressesVideoAndThunderboltSections()
    {
        var json = ReportJsonRenderer.Render(BuildReport(), legacy: true);
        var root = JsonNode.Parse(json)!.AsObject();

        Assert.False(root.ContainsKey("video"));
        Assert.False(root.ContainsKey("thunderbolt"));

        // The legacy payload still round-trips through the macOS-schema parser.
        var snapshot = JsonFormatter.Parse(json);
        Assert.Equal(SnapshotBuilder.SchemaVersion, snapshot.Version);
        Assert.Single(snapshot.Ports);
    }
}
