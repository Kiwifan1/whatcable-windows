namespace WhatCable.Windows.Backend.Thunderbolt;

/// <summary>
/// Abstraction over Thunderbolt/USB4 device-chain enumeration. The production
/// implementation (<c>SetupApiThunderboltEnumerator</c>) walks
/// <c>SetupDiEnumDeviceInfo</c> over present devices and filters by Thunderbolt/USB4
/// hardware IDs; tests provide a fake returning a recorded device list.
/// </summary>
public interface IThunderboltEnumerator
{
    IReadOnlyList<ThunderboltDeviceInfo> EnumerateChain();
}
