using WhatCable.Video.Core;

namespace WhatCable.Windows.Backend.Video;

/// <summary>
/// Maps enumerated display paths (active timing from <c>QueryDisplayConfig</c> + the raw
/// EDID) onto the <see cref="VideoPortReport"/> model: it parses the EDID through
/// <see cref="EdidParser"/>, derives the sink's maximum advertised mode, populates a
/// <see cref="VideoPortSnapshot"/>, and runs <see cref="VideoLinkDiagnostic"/>.
/// </summary>
/// <remarks>
/// On a stock PC with no vendor SDK we can read the active mode and the sink EDID but not
/// the cable's e-marker or the GPU's capability tables, so <c>AdvertisedCableClass</c> and
/// <c>SourceGpuCaps</c> are left null and the diagnostic reports "cable or source limited"
/// when the sink advertises more than the active mode.
/// </remarks>
public sealed class VideoPortAdapter
{
    private readonly IDisplayEnumerator _enumerator;

    public VideoPortAdapter(IDisplayEnumerator enumerator)
        => _enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));

    public IReadOnlyList<VideoPortReport> GetReports()
    {
        var displays = _enumerator.EnumerateDisplays();
        var reports = new List<VideoPortReport>(displays.Count);

        foreach (var display in displays)
        {
            var edid = display.EdidRaw is { Length: > 0 } ? EdidParser.Parse(display.EdidRaw) : null;
            var sinkMaxMode = DeriveSinkMaxMode(edid);

            var snapshot = new VideoPortSnapshot
            {
                ConnectorType = display.ConnectorType,
                ActiveMode = display.ActiveMode,
                SinkMaxMode = sinkMaxMode,
                EdidRaw = display.EdidRaw,
                EdidParsed = edid,
            };

            reports.Add(new VideoPortReport
            {
                DisplayName = display.Name ?? edid?.DisplayName,
                Port = snapshot,
                Diagnostic = VideoLinkDiagnostic.Analyze(snapshot),
            });
        }

        return reports;
    }

    /// <summary>
    /// The highest-bandwidth timing the sink advertises, taken across the EDID native /
    /// standard / detailed timings, the CTA-861 short video descriptors, and any DisplayID
    /// timings. Returns null when no EDID timing could be read.
    /// </summary>
    internal static VideoMode? DeriveSinkMaxMode(EdidInfo? edid)
    {
        if (edid is null)
        {
            return null;
        }

        VideoMode? best = null;
        double bestScore = -1;

        void Consider(VideoMode? mode)
        {
            if (mode is null)
            {
                return;
            }

            double score = (double)mode.WidthPx * mode.HeightPx * mode.RefreshRateHz;
            if (score > bestScore)
            {
                bestScore = score;
                best = mode;
            }
        }

        Consider(edid.NativeResolution);
        foreach (var mode in edid.SupportedModes)
        {
            Consider(mode);
        }

        if (edid.CtaInfo is { } cta)
        {
            foreach (var descriptor in cta.VideoDescriptors)
            {
                Consider(descriptor.Mode);
            }
        }

        if (edid.DisplayIdInfo is { } displayId)
        {
            foreach (var mode in displayId.SupportedModes)
            {
                Consider(mode);
            }
        }

        return best;
    }
}
