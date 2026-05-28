using WhatCable.Windows.Backend.Power;
using WhatCable.Windows.Backend.Usb;

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
