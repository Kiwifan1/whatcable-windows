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
