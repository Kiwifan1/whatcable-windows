using WhatCable.Core;
using WhatCable.Windows.App.Core.Services;

namespace WhatCable.Windows.App.Services;

/// <summary>
/// Thread-safe <see cref="ISnapshotProvider"/> that caches a pre-built <see cref="Snapshot"/>
/// supplied by the poll loop, so both the tray ViewModel and the widget publisher share the same
/// single backend query per poll interval.
/// </summary>
internal sealed class CachingSnapshotProvider : ISnapshotProvider
{
    private volatile Snapshot _current = new();

    /// <summary>Replaces the cached snapshot. Safe to call from any thread.</summary>
    public void Update(Snapshot snapshot) => _current = snapshot;

    /// <inheritdoc />
    public Snapshot GetSnapshot() => _current;
}
