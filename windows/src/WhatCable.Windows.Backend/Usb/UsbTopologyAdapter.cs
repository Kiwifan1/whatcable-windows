using WhatCable.Core;

namespace WhatCable.Windows.Backend.Usb;

/// <summary>
/// Maps an enumerated USB topology (host controllers + device trees) onto the
/// platform-neutral <see cref="Port"/> / <see cref="DeviceNode"/> model used by the CLI/UI.
/// </summary>
/// <remarks>
/// On a non-UCSI PC the basic backend cannot read PD power profiles or cable e-marker
/// VDOs, so those fields are left null and each port is annotated with
/// <see cref="UnavailableReason"/> = <c>"ucsi_not_supported"</c>.
/// </remarks>
public sealed class UsbTopologyAdapter
{
    /// <summary>Reason emitted when PD / e-marker data is unavailable on a non-UCSI host.</summary>
    public const string UnavailableReason = "ucsi_not_supported";

    private readonly IUsbEnumerator _enumerator;

    public UsbTopologyAdapter(IUsbEnumerator enumerator)
        => _enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));

    public IReadOnlyList<Port> GetPorts()
    {
        var controllers = _enumerator.EnumerateHostControllers();
        var ports = new List<Port>(controllers.Count);
        var index = 0;

        foreach (var controller in controllers)
        {
            index++;
            var name = string.IsNullOrWhiteSpace(controller.Name)
                ? $"USB Host Controller {index}"
                : controller.Name;

            var children = controller.Devices
                .Select(device => ToDeviceNode(device, index.ToString()))
                .ToList();

            var deviceCount = CountDevices(controller.Devices);

            var root = new DeviceNode
            {
                Name = name,
                Speed = string.Empty,
                LocationId = $"0x{index:X2}000000",
                Children = children,
            };

            ports.Add(new Port
            {
                Name = name,
                ClassName = "UsbHostController",
                ConnectionActive = children.Count > 0,
                PdCapable = false,
                Status = children.Count > 0 ? "connected" : "available",
                Headline = "USB host controller",
                Subtitle = deviceCount == 1 ? "1 device" : $"{deviceCount} devices",
                Device = root,
                UnavailableReason = UnavailableReason,
            });
        }

        return ports;
    }

    private static DeviceNode ToDeviceNode(UsbDeviceInfo device, string parentPath)
    {
        var path = $"{parentPath}.{device.PortNumber}";
        var children = device.Children
            .Select(child => ToDeviceNode(child, path))
            .ToList();

        return new DeviceNode
        {
            Name = device.Name,
            VendorId = device.VendorId,
            ProductId = device.ProductId,
            VendorName = VendorDatabase.Name(device.VendorId),
            Speed = UsbSpeedLabels.Describe(device.Speed),
            LocationId = path,
            Children = children,
        };
    }

    private static int CountDevices(IReadOnlyList<UsbDeviceInfo> devices)
    {
        var total = 0;
        foreach (var device in devices)
        {
            total++;
            total += CountDevices(device.Children);
        }

        return total;
    }
}
