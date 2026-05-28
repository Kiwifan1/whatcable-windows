using WhatCable.Windows.Backend.Power;
using WhatCable.Windows.Backend.Thunderbolt;
using WhatCable.Windows.Backend.Usb;
using WhatCable.Windows.Backend.Video;

namespace WhatCable.Windows.Backend.Tests;

/// <summary>In-memory <see cref="IUsbEnumerator"/> returning a recorded device tree.</summary>
internal sealed class FakeUsbEnumerator : IUsbEnumerator
{
    private readonly IReadOnlyList<UsbHostController> _controllers;

    public FakeUsbEnumerator(params UsbHostController[] controllers)
        => _controllers = controllers;

    public IReadOnlyList<UsbHostController> EnumerateHostControllers() => _controllers;
}

/// <summary>In-memory <see cref="ISystemPowerSource"/> returning a fixed power state.</summary>
internal sealed class FakeSystemPowerSource : ISystemPowerSource
{
    private readonly SystemPowerInfo _info;

    public FakeSystemPowerSource(SystemPowerInfo info) => _info = info;

    public SystemPowerInfo GetPowerInfo() => _info;
}

/// <summary>In-memory <see cref="IDisplayEnumerator"/> returning recorded display paths.</summary>
internal sealed class FakeDisplayEnumerator : IDisplayEnumerator
{
    private readonly IReadOnlyList<DisplayInfo> _displays;

    public FakeDisplayEnumerator(params DisplayInfo[] displays) => _displays = displays;

    public IReadOnlyList<DisplayInfo> EnumerateDisplays() => _displays;
}

/// <summary>In-memory <see cref="IThunderboltEnumerator"/> returning a recorded chain.</summary>
internal sealed class FakeThunderboltEnumerator : IThunderboltEnumerator
{
    private readonly IReadOnlyList<ThunderboltDeviceInfo> _devices;

    public FakeThunderboltEnumerator(params ThunderboltDeviceInfo[] devices) => _devices = devices;

    public IReadOnlyList<ThunderboltDeviceInfo> EnumerateChain() => _devices;
}
