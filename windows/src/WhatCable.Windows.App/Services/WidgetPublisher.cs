using WhatCable.Widgets.Core;
using WhatCable.Windows.Backend;

namespace WhatCable.Windows.App.Services;

/// <summary>
/// Publishes the latest backend report to the WhatCable widget provider over the snapshot named
/// pipe. Called from the app's poll loop so a pinned widget refreshes within one poll interval of a
/// USB-C or display event. Best-effort: when no widget is pinned the provider isn't listening and
/// <see cref="WidgetSnapshotPipe.PublishAsync"/> simply returns without throwing.
/// </summary>
public sealed class WidgetPublisher
{
    /// <summary>Builds the current report and publishes it to the widget provider.</summary>
    public async Task PublishAsync(CancellationToken cancellationToken = default)
    {
        var report = await Task.Run(() => SnapshotBuilder.CreateDefault().BuildReport(), cancellationToken)
            .ConfigureAwait(false);

        var payload = new WidgetPayload
        {
            Snapshot = report.Snapshot,
            VideoVerdicts = report.Video
                .Select(v => new VideoVerdict(
                    string.IsNullOrWhiteSpace(v.DisplayName) ? "Display" : v.DisplayName!,
                    v.Diagnostic.Verdict))
                .Where(v => !string.IsNullOrWhiteSpace(v.Verdict))
                .ToList(),
        };

        await WidgetSnapshotPipe.PublishAsync(payload, cancellationToken: cancellationToken);
    }
}
