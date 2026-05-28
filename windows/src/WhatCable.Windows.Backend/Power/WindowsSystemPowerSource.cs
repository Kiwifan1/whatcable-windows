using Windows.Win32;
using Windows.Win32.System.Power;

namespace WhatCable.Windows.Backend.Power;

/// <summary>
/// Live Windows implementation of <see cref="ISystemPowerSource"/> using
/// <c>GetSystemPowerStatus</c> for AC/charge state and
/// <c>CallNtPowerInformation(SystemBatteryState)</c> for runtime/charging refinement.
/// </summary>
internal sealed class WindowsSystemPowerSource : ISystemPowerSource
{
    private const byte AcOnlineFlag = 1;
    private const byte BatteryPercentUnknown = 255;
    private const byte BatteryFlagNoSystemBattery = 128;
    private const byte BatteryFlagCharging = 8;
    private const byte BatteryFlagUnknown = 255;
    private const uint LifeTimeUnknown = unchecked((uint)-1);

    public SystemPowerInfo GetPowerInfo()
    {
        if (!PInvoke.GetSystemPowerStatus(out var status))
        {
            return new SystemPowerInfo();
        }

        var acOnline = status.ACLineStatus == AcOnlineFlag;
        var batteryPresent = status.BatteryFlag != BatteryFlagNoSystemBattery
            && status.BatteryFlag != BatteryFlagUnknown;

        int? percent = status.BatteryLifePercent == BatteryPercentUnknown
            ? null
            : status.BatteryLifePercent;

        var charging = (status.BatteryFlag & BatteryFlagCharging) != 0;

        int? remainingMinutes = status.BatteryLifeTime == LifeTimeUnknown
            ? null
            : (int)(status.BatteryLifeTime / 60);

        TryRefineFromBatteryState(ref batteryPresent, ref charging, ref remainingMinutes);

        return new SystemPowerInfo
        {
            AcOnline = acOnline,
            BatteryPresent = batteryPresent,
            Charging = charging,
            BatteryPercent = percent,
            RemainingMinutes = remainingMinutes,
        };
    }

    private static unsafe void TryRefineFromBatteryState(
        ref bool batteryPresent,
        ref bool charging,
        ref int? remainingMinutes)
    {
        SYSTEM_BATTERY_STATE state = default;
        var status = PInvoke.CallNtPowerInformation(
            POWER_INFORMATION_LEVEL.SystemBatteryState,
            null,
            0,
            &state,
            (uint)sizeof(SYSTEM_BATTERY_STATE));

        // NTSTATUS success is non-negative; bail out otherwise.
        if (status.Value < 0)
        {
            return;
        }

        if (state.BatteryPresent)
        {
            batteryPresent = true;
        }

        if (state.Charging)
        {
            charging = true;
        }

        if (remainingMinutes is null && !state.AcOnLine && state.EstimatedTime != LifeTimeUnknown && state.EstimatedTime > 0)
        {
            remainingMinutes = (int)(state.EstimatedTime / 60);
        }
    }
}
