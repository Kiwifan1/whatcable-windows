using WhatCable.Widgets.Core;
using WhatCable.Windows.Backend;

namespace WhatCable.Windows.App.Services;

/// <summary>
/// Publishes a pre-built backend report to the WhatCable widget provider over the snapshot named
/// pipe. Called from the app's poll loop so a pinned widget refreshes within one poll interval of a
/// USB-C or display event. Best-effort: when no widget is pinned the provider isn't listening and
/// <see cref="WidgetSnapshotPipe.PublishAsync"/> simply returns without throwing.
/// </summary>
public sealed class WidgetPublisher
{
    /// <summary>
    /// Publishes <paramref name="report"/> to the widget provider pipe. The caller is responsible
    /// for building the report; this method does not invoke the backend again.
    /// </summary>
    public async Task PublishAsync(WhatCableReport report, CancellationToken cancellationToken = default)
    {
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
