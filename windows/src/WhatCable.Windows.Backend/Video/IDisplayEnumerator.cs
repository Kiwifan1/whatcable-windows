namespace WhatCable.Windows.Backend.Video;

/// <summary>
/// Abstraction over display-path enumeration. The production implementation
/// (<c>QueryDisplayConfigDisplayEnumerator</c>) walks <c>QueryDisplayConfig</c> +
/// <c>DisplayConfigGetDeviceInfo</c> and reads each monitor's EDID from the registry;
/// tests provide a fake returning recorded <see cref="DisplayInfo"/> values.
/// </summary>
public interface IDisplayEnumerator
{
    IReadOnlyList<DisplayInfo> EnumerateDisplays();
}
