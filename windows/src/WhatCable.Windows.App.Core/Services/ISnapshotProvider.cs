using WhatCable.Core;

namespace WhatCable.Windows.App.Core.Services;

/// <summary>
/// Supplies the latest <see cref="Snapshot"/> to the ViewModels. The WinUI app implements this
/// over <c>WhatCable.Windows.Backend.SnapshotBuilder</c>; tests use a fake.
/// </summary>
public interface ISnapshotProvider
{
    Snapshot GetSnapshot();
}
