using System.Management;

namespace WhatCable.Windows.Backend.Power;

/// <summary>
/// Live <see cref="IBatteryWattageSource"/> backed by WMI. Reads the <c>BatteryStatus</c> class in
/// the <c>root\wmi</c> namespace, whose <c>ChargeRate</c> / <c>DischargeRate</c> fields report the
/// instantaneous whole-system power flow in milliwatts. There is no public Windows API for per-port
/// USB-C power, so this figure is system-wide by design.
/// </summary>
public sealed class WmiBatteryWattageSource : IBatteryWattageSource
{
    /// <summary>Emitted on a system with no battery (e.g. a desktop).</summary>
    public const string ReasonNoBattery = "no_battery";

    /// <summary>Emitted when the WMI query itself fails (provider missing, access denied, …).</summary>
    public const string ReasonWmiUnavailable = "wmi_unavailable";

    private string? _unavailableReason;

    public bool IsAvailable => _unavailableReason is null && Read() is not null;

    public string? UnavailableReason => _unavailableReason;

    public BatteryPowerSample? Read()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\wmi",
                "SELECT Charging, Discharging, ChargeRate, DischargeRate FROM BatteryStatus");
            using var results = searcher.Get();

            foreach (var obj in results)
            {
                using (obj)
                {
                    var charging = ToBool(obj["Charging"]);
                    var discharging = ToBool(obj["Discharging"]);
                    var chargeRate = ToInt(obj["ChargeRate"]);
                    var dischargeRate = ToInt(obj["DischargeRate"]);

                    _unavailableReason = null;
                    return BatteryWattageCalculator.Compute(charging, discharging, chargeRate, dischargeRate);
                }
            }

            // No BatteryStatus instances => no battery present.
            _unavailableReason = ReasonNoBattery;
            return null;
        }
        catch (ManagementException)
        {
            _unavailableReason = ReasonWmiUnavailable;
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            _unavailableReason = ReasonWmiUnavailable;
            return null;
        }
    }

    private static bool ToBool(object? value) => value is bool b && b;

    private static int ToInt(object? value) => value is null ? 0 : Convert.ToInt32(value);
}
