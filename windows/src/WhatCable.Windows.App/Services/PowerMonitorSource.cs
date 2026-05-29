using WhatCable.Windows.App.Core.Services;
using WhatCable.Windows.Backend.Power;

namespace WhatCable.Windows.App.Services;

/// <summary>
/// <see cref="IPowerMonitorSource"/> over the live Windows backend (<see cref="WmiBatteryWattageSource"/>).
/// Bridges the backend's WMI <c>BatteryStatus</c> sample onto the App.Core power-monitor contract so
/// the ViewModels stay free of any backend reference.
/// </summary>
public sealed class PowerMonitorSource : IPowerMonitorSource
{
    private readonly IBatteryWattageSource _source = new WmiBatteryWattageSource();

    public bool IsAvailable => _source.IsAvailable;

    public string? UnavailableReason => _source.UnavailableReason;

    public PowerReading? Read()
    {
        var sample = _source.Read();
        if (sample is null)
        {
            return null;
        }

        return new PowerReading
        {
            Watts = sample.Watts,
            Flow = sample.Direction switch
            {
                PowerFlowDirection.Charging => PowerFlow.Charging,
                PowerFlowDirection.Discharging => PowerFlow.Discharging,
                _ => PowerFlow.Idle,
            },
        };
    }
}
