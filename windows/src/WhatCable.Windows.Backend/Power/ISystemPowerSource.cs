namespace WhatCable.Windows.Backend.Power;

/// <summary>
/// Abstraction over the system power query. The production implementation
/// (<c>WindowsSystemPowerSource</c>) calls <c>GetSystemPowerStatus</c> /
/// <c>CallNtPowerInformation</c>; tests provide a fake.
/// </summary>
public interface ISystemPowerSource
{
    SystemPowerInfo GetPowerInfo();
}
