using WhatCable.Core;
using WhatCable.Windows.App.Core.Services;

namespace WhatCable.Windows.App.Core.Tests;

internal sealed class FakeSnapshotProvider : ISnapshotProvider
{
    private readonly Queue<Snapshot> _queue;
    private Snapshot _last;

    public FakeSnapshotProvider(params Snapshot[] snapshots)
    {
        if (snapshots.Length == 0)
        {
            throw new ArgumentException("At least one snapshot is required.", nameof(snapshots));
        }

        _queue = new Queue<Snapshot>(snapshots);
        _last = snapshots[0];
    }

    public Snapshot GetSnapshot()
    {
        if (_queue.Count > 0)
        {
            _last = _queue.Dequeue();
        }

        return _last;
    }
}

internal sealed class RecordingNotificationService : INotificationService
{
    public List<(string Title, string Message)> Notifications { get; } = new();

    public void Notify(string title, string message) => Notifications.Add((title, message));
}

internal sealed class FakeStartupTaskService : IStartupTaskService
{
    public FakeStartupTaskService(bool isEnabled = false, bool canToggle = true)
    {
        IsEnabled = isEnabled;
        CanToggle = canToggle;
    }

    public bool IsEnabled { get; private set; }

    public bool CanToggle { get; set; }

    /// <summary>When set, <see cref="SetEnabledAsync"/> forces this effective state (OS denial).</summary>
    public bool? ForcedResult { get; set; }

    public Task<bool> SetEnabledAsync(bool enabled)
    {
        IsEnabled = ForcedResult ?? enabled;
        return Task.FromResult(IsEnabled);
    }
}

internal static class SnapshotFactory
{
    public static DeviceNode Device(string name, params DeviceNode[] children) => new()
    {
        Name = name,
        Speed = "SuperSpeed",
        Children = children.Length == 0 ? null : children,
    };

    public static Port Port(string name, bool active, DeviceNode? device = null) => new()
    {
        Name = name,
        ClassName = "USB4PlatformPolicyClient",
        ConnectionActive = active,
        Status = active ? "Active" : "Nothing connected",
        Headline = active ? "USB device" : string.Empty,
        Device = device,
    };

    public static Snapshot Snapshot(AdapterSnapshot? adapter, params Port[] ports) => new()
    {
        Version = "0.26.0",
        Adapter = adapter,
        Ports = ports,
    };
}

internal sealed class FakePowerMonitorSource : IPowerMonitorSource
{
    private readonly Queue<PowerReading?> _readings;

    public FakePowerMonitorSource(bool isAvailable = true, string? unavailableReason = null, params PowerReading?[] readings)
    {
        IsAvailable = isAvailable;
        UnavailableReason = unavailableReason;
        _readings = new Queue<PowerReading?>(readings);
    }

    public bool IsAvailable { get; }

    public string? UnavailableReason { get; }

    public PowerReading? Read() => _readings.Count > 0 ? _readings.Dequeue() : null;
}

/// <summary>Builds serialized UCSI JSON payloads matching the backend's connector schema.</summary>
internal static class UcsiJson
{
    public static System.Text.Json.JsonElement Build(
        int versionBcd = 0x0200,
        bool connected = true,
        bool usb3 = false,
        bool? liquidDetected = null,
        bool liquidDetectionSupported = false,
        int? cableSpeed = null,
        (int Svid, uint Vdo, string Name, bool Active)[]? alternateModes = null)
    {
        var modes = (alternateModes ?? Array.Empty<(int, uint, string, bool)>())
            .Select(m => new { svid = m.Svid, vdo = m.Vdo, name = m.Name, active = m.Active })
            .ToArray();

        object payload = new
        {
            versionBcd,
            status = new { connected },
            capability = new { usb3 },
            available = new { liquidDetection = liquidDetectionSupported },
            liquidDetected,
            cableVdo = cableSpeed is { } cs ? new { Speed = cs } : null,
            alternateModes = modes,
        };

        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    public static Port Port(string name, bool active, System.Text.Json.JsonElement ucsi) => new()
    {
        Name = name,
        ClassName = "USB4PlatformPolicyClient",
        ConnectionActive = active,
        Status = active ? "Active" : "Nothing connected",
        Headline = active ? "USB device" : string.Empty,
        Ucsi = ucsi,
    };
}
