using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using WhatCable.Windows.App.Core.Services;

namespace WhatCable.Windows.App.Core.ViewModels;

/// <summary>
/// Backs the Pro Power Monitor live graph shown in the popover. Holds a rolling window of
/// system-wide watt samples (from <see cref="IPowerMonitorSource"/>) and projects them into
/// normalised plot points the view can draw as a polyline.
/// </summary>
/// <remarks>
/// The reading is whole-system: Windows exposes no public per-port USB-C power telemetry, so a
/// per-port breakdown is explicitly unavailable. Non-Pro users get the upsell state; on a battery-
/// less desktop the monitor reports <see cref="UnavailableReason"/>.
/// </remarks>
public sealed partial class PowerMonitorViewModel : ObservableObject
{
    /// <summary>Default number of samples retained for the rolling graph (~1 min at a 1 Hz poll).</summary>
    public const int DefaultCapacity = 60;

    private readonly IPowerMonitorSource? _source;
    private readonly bool _proUnlocked;
    private readonly int _capacity;

    public PowerMonitorViewModel(IPowerMonitorSource? source, bool proUnlocked, int capacity = DefaultCapacity)
    {
        _source = source;
        _proUnlocked = proUnlocked;
        _capacity = Math.Max(2, capacity);
        Samples = new ObservableCollection<double>();
    }

    /// <summary>The retained watt samples, oldest first.</summary>
    public ObservableCollection<double> Samples { get; }

    [ObservableProperty]
    private double _currentWatts;

    [ObservableProperty]
    private double _peakWatts;

    [ObservableProperty]
    private PowerFlow _flow;

    /// <summary>True when the user has unlocked Pro and may see live data.</summary>
    public bool IsProUnlocked => _proUnlocked;

    /// <summary>True when Pro is locked, so the view should show the upsell affordance instead.</summary>
    public bool ShowUpsell => !_proUnlocked;

    /// <summary>True when Pro is unlocked but the hardware can't supply readings (e.g. no battery).</summary>
    public bool IsUnavailable => _proUnlocked && (_source is null || !_source.IsAvailable) && Samples.Count == 0;

    /// <summary>Machine-readable reason readings are unavailable, when applicable.</summary>
    public string? UnavailableReason => IsUnavailable ? (_source?.UnavailableReason ?? "power_monitor_unavailable") : null;

    /// <summary>True when there is at least one sample to plot.</summary>
    public bool HasData => Samples.Count > 0;

    /// <summary>Per-port power is never available on Windows; documented for the UI.</summary>
    public bool PerPortAvailable => false;

    /// <summary>Reason per-port power is unavailable on Windows.</summary>
    public string PerPortUnavailableReason => "windows_no_per_port_power_api";

    /// <summary>Human-readable current power, e.g. "12.5 W" or a localised dash when no data.</summary>
    public string CurrentWattsText => HasData
        ? string.Create(CultureInfo.CurrentCulture, $"{CurrentWatts:0.0} W")
        : "—";

    /// <summary>
    /// Takes one reading from the source and appends it to the rolling window. Does nothing when Pro
    /// is locked. Safe to call from a UI-poll timer.
    /// </summary>
    public void Sample()
    {
        if (!_proUnlocked || _source is null)
        {
            NotifyDerived();
            return;
        }

        var reading = _source.Read();
        if (reading is null)
        {
            NotifyDerived();
            return;
        }

        Append(reading.Watts);
        Flow = reading.Flow;
        CurrentWatts = reading.Watts;
        if (reading.Watts > PeakWatts)
        {
            PeakWatts = reading.Watts;
        }

        NotifyDerived();
    }

    private void Append(double watts)
    {
        Samples.Add(watts);
        while (Samples.Count > _capacity)
        {
            Samples.RemoveAt(0);
        }
    }

    /// <summary>
    /// Projects the samples onto a polyline within the given pixel box. Y is inverted (0 watts at the
    /// bottom). Returns an empty list when there is no data. The Y scale uses
    /// <see cref="PeakWatts"/> so the line keeps a stable reference as new samples arrive.
    /// </summary>
    public IReadOnlyList<(double X, double Y)> GetPlotPoints(double width, double height)
    {
        if (Samples.Count == 0 || width <= 0 || height <= 0)
        {
            return Array.Empty<(double, double)>();
        }

        var max = Math.Max(PeakWatts, Samples.Max());
        if (max <= 0)
        {
            max = 1;
        }

        var points = new List<(double, double)>(Samples.Count);
        var step = Samples.Count == 1 ? 0 : width / (Samples.Count - 1);
        for (var i = 0; i < Samples.Count; i++)
        {
            var x = step * i;
            var y = height - (Samples[i] / max * height);
            points.Add((x, y));
        }

        return points;
    }

    private void NotifyDerived()
    {
        OnPropertyChanged(nameof(HasData));
        OnPropertyChanged(nameof(IsUnavailable));
        OnPropertyChanged(nameof(UnavailableReason));
        OnPropertyChanged(nameof(CurrentWattsText));
    }
}
