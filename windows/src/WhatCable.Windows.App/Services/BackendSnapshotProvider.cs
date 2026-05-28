using WhatCable.Core;
using WhatCable.Windows.App.Core.Services;
using WhatCable.Windows.Backend;

namespace WhatCable.Windows.App.Services;

/// <summary>
/// <see cref="ISnapshotProvider"/> over the live Windows backend (<see cref="SnapshotBuilder"/>).
/// Rebuilds a fresh builder per poll so each snapshot reflects the current USB/power state.
/// </summary>
public sealed class BackendSnapshotProvider : ISnapshotProvider
{
    public Snapshot GetSnapshot() => SnapshotBuilder.CreateDefault().Build();
}
