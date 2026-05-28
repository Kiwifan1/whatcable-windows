using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Devices.Usb;
using Windows.Win32.Storage.FileSystem;

namespace WhatCable.Windows.Backend.Usb;

/// <summary>
/// Live USB topology enumerator. Walks USB host controllers via SetupAPI device interfaces,
/// then traverses each controller's hub tree with the USB hub IOCTLs, reading the negotiated
/// speed from <c>USB_NODE_CONNECTION_INFORMATION_EX</c> /
/// <c>USB_NODE_CONNECTION_INFORMATION_EX_V2</c>.
/// </summary>
/// <remarks>
/// All native access is wrapped defensively: a failure to read any single controller, hub, or
/// port is swallowed so the CLI always produces a valid (possibly partial) snapshot.
/// </remarks>
internal sealed class SetupApiUsbEnumerator : IUsbEnumerator
{
    private const uint GenericWrite = 0x40000000;
    private const int MaxHubDepth = 16;
    // SP_DEVICE_INTERFACE_DETAIL_DATA_W.cbSize: size of the fixed header only.
    private const uint DetailCbSize64 = 8;
    private const uint DetailCbSize32 = 6;
    private const byte UsbLowSpeed = 0;
    private const byte UsbFullSpeed = 1;
    private const byte UsbHighSpeed = 2;
    private const byte UsbSuperSpeed = 3;

    public IReadOnlyList<UsbHostController> EnumerateHostControllers()
    {
        var controllers = new List<UsbHostController>();

        try
        {
            EnumerateControllersCore(controllers);
        }
        catch
        {
            // Never let enumeration failures crash the CLI; return whatever we collected.
        }

        return controllers;
    }

    private static unsafe void EnumerateControllersCore(List<UsbHostController> controllers)
    {
        var guid = PInvoke.GUID_DEVINTERFACE_USB_HOST_CONTROLLER;
        using var deviceInfo = PInvoke.SetupDiGetClassDevs(
            guid,
            null,
            default,
            SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_PRESENT | SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_DEVICEINTERFACE);

        if (deviceInfo.IsInvalid)
        {
            return;
        }

        for (uint index = 0; ; index++)
        {
            var interfaceData = new SP_DEVICE_INTERFACE_DATA
            {
                cbSize = (uint)sizeof(SP_DEVICE_INTERFACE_DATA),
            };

            if (!PInvoke.SetupDiEnumDeviceInterfaces(deviceInfo, null, in guid, index, ref interfaceData))
            {
                break;
            }

            var path = GetInterfacePath(deviceInfo, interfaceData);
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            try
            {
                var controller = ReadController(path!, (int)index);
                if (controller is not null)
                {
                    controllers.Add(controller);
                }
            }
            catch
            {
                // Skip a controller we cannot open or query.
            }
        }
    }

    private static unsafe string? GetInterfacePath(SetupDiDestroyDeviceInfoListSafeHandle deviceInfo, SP_DEVICE_INTERFACE_DATA interfaceData)
    {
        uint required = 0;
        PInvoke.SetupDiGetDeviceInterfaceDetail(deviceInfo, in interfaceData, null, 0, &required, null);
        if (required == 0)
        {
            return null;
        }

        var buffer = new byte[required];
        fixed (byte* p = buffer)
        {
            var detail = (SP_DEVICE_INTERFACE_DETAIL_DATA_W*)p;
            // cbSize is the size of the fixed header only (8 on 64-bit, 6 on 32-bit).
            detail->cbSize = IntPtr.Size == 8 ? DetailCbSize64 : DetailCbSize32;
            if (!PInvoke.SetupDiGetDeviceInterfaceDetail(deviceInfo, in interfaceData, detail, required, null, null))
            {
                return null;
            }

            // DevicePath follows the 4-byte cbSize field.
            return new string((char*)(p + sizeof(uint)));
        }
    }

    private static UsbHostController? ReadController(string interfacePath, int index)
    {
        using var hcd = OpenDevice(interfacePath);
        if (hcd is null || hcd.IsInvalid)
        {
            return null;
        }

        var rootHubName = GetRootHubName(hcd);
        if (string.IsNullOrEmpty(rootHubName))
        {
            return new UsbHostController { Name = $"USB Host Controller {index + 1}", InterfacePath = interfacePath };
        }

        using var rootHub = OpenDevice(@"\\.\" + rootHubName);
        var devices = new List<UsbDeviceInfo>();
        if (rootHub is not null && !rootHub.IsInvalid)
        {
            EnumerateHub(rootHub, devices, depth: 0);
        }

        return new UsbHostController
        {
            Name = $"USB Host Controller {index + 1}",
            InterfacePath = interfacePath,
            Devices = devices,
        };
    }

    private static unsafe string? GetRootHubName(SafeHandle hcd)
    {
        USB_ROOT_HUB_NAME header = default;
        if (!DeviceIoControl(hcd, PInvoke.IOCTL_USB_GET_ROOT_HUB_NAME, null, 0, &header, (uint)sizeof(USB_ROOT_HUB_NAME), out _))
        {
            return null;
        }

        var length = header.ActualLength;
        if (length == 0 || length > 4096)
        {
            return null;
        }

        var buffer = new byte[length];
        fixed (byte* p = buffer)
        {
            if (!DeviceIoControl(hcd, PInvoke.IOCTL_USB_GET_ROOT_HUB_NAME, null, 0, p, length, out _))
            {
                return null;
            }

            return new string((char*)(p + sizeof(uint) /* ActualLength */));
        }
    }

    private static unsafe void EnumerateHub(SafeHandle hub, List<UsbDeviceInfo> output, int depth)
    {
        if (depth > MaxHubDepth)
        {
            return;
        }

        USB_NODE_INFORMATION nodeInfo = default;
        nodeInfo.NodeType = USB_HUB_NODE.UsbHub;
        if (!DeviceIoControl(hub, PInvoke.IOCTL_USB_GET_NODE_INFORMATION, &nodeInfo, (uint)sizeof(USB_NODE_INFORMATION), &nodeInfo, (uint)sizeof(USB_NODE_INFORMATION), out _))
        {
            return;
        }

        int portCount = nodeInfo.u.HubInformation.HubDescriptor.bNumberOfPorts;
        for (int port = 1; port <= portCount; port++)
        {
            try
            {
                var device = ReadPort(hub, port, depth);
                if (device is not null)
                {
                    output.Add(device);
                }
            }
            catch
            {
                // Ignore an unreadable port.
            }
        }
    }

    private static unsafe UsbDeviceInfo? ReadPort(SafeHandle hub, int port, int depth)
    {
        int size = USB_NODE_CONNECTION_INFORMATION_EX.SizeOf(30);
        var buffer = new byte[size];
        fixed (byte* p = buffer)
        {
            var info = (USB_NODE_CONNECTION_INFORMATION_EX*)p;
            info->ConnectionIndex = (uint)port;
            if (!DeviceIoControl(hub, PInvoke.IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX, p, (uint)size, p, (uint)size, out _))
            {
                return null;
            }

            if (info->ConnectionStatus != USB_CONNECTION_STATUS.DeviceConnected)
            {
                return null;
            }

            var descriptor = info->DeviceDescriptor;
            var isHub = info->DeviceIsHub.Value != 0;
            var speed = MapSpeed(info->Speed, hub, port);
            var name = ReadProductName(hub, port, descriptor.iProduct);

            var children = new List<UsbDeviceInfo>();
            if (isHub)
            {
                var childHubName = GetExternalHubName(hub, port);
                if (!string.IsNullOrEmpty(childHubName))
                {
                    using var childHub = OpenDevice(@"\\.\" + childHubName);
                    if (childHub is not null && !childHub.IsInvalid)
                    {
                        EnumerateHub(childHub, children, depth + 1);
                    }
                }
            }

            return new UsbDeviceInfo
            {
                Name = name,
                VendorId = descriptor.idVendor,
                ProductId = descriptor.idProduct,
                Speed = speed,
                PortNumber = port,
                IsHub = isHub,
                Children = children,
            };
        }
    }

    private static unsafe UsbSpeed MapSpeed(byte speedByte, SafeHandle hub, int port)
    {
        var baseSpeed = speedByte switch
        {
            UsbLowSpeed => UsbSpeed.Low,
            UsbFullSpeed => UsbSpeed.Full,
            UsbHighSpeed => UsbSpeed.High,
            UsbSuperSpeed => UsbSpeed.SuperSpeed,
            _ => UsbSpeed.Unknown,
        };

        if (baseSpeed != UsbSpeed.SuperSpeed)
        {
            return baseSpeed;
        }

        USB_NODE_CONNECTION_INFORMATION_EX_V2 v2 = default;
        v2.ConnectionIndex = (uint)port;
        v2.Length = (uint)sizeof(USB_NODE_CONNECTION_INFORMATION_EX_V2);
        if (DeviceIoControl(hub, PInvoke.IOCTL_USB_GET_NODE_CONNECTION_INFORMATION_EX_V2, &v2, v2.Length, &v2, v2.Length, out _)
            && v2.Flags.Anonymous.DeviceIsOperatingAtSuperSpeedPlusOrHigher)
        {
            return UsbSpeed.SuperSpeedPlus;
        }

        return baseSpeed;
    }

    private static unsafe string? GetExternalHubName(SafeHandle hub, int port)
    {
        USB_NODE_CONNECTION_NAME header = default;
        header.ConnectionIndex = (uint)port;
        if (!DeviceIoControl(hub, PInvoke.IOCTL_USB_GET_NODE_CONNECTION_NAME, &header, (uint)sizeof(USB_NODE_CONNECTION_NAME), &header, (uint)sizeof(USB_NODE_CONNECTION_NAME), out _))
        {
            return null;
        }

        var length = header.ActualLength;
        if (length == 0 || length > 4096)
        {
            return null;
        }

        var buffer = new byte[length];
        fixed (byte* p = buffer)
        {
            var name = (USB_NODE_CONNECTION_NAME*)p;
            name->ConnectionIndex = (uint)port;
            if (!DeviceIoControl(hub, PInvoke.IOCTL_USB_GET_NODE_CONNECTION_NAME, p, length, p, length, out _))
            {
                return null;
            }

            // NodeName follows ConnectionIndex (uint) + ActualLength (uint).
            return new string((char*)(p + (2 * sizeof(uint))));
        }
    }

    private static unsafe string? ReadProductName(SafeHandle hub, int port, byte stringIndex)
    {
        if (stringIndex == 0)
        {
            return null;
        }

        const int headerSize = 12; // ConnectionIndex (4) + SetupPacket (8)
        const int payload = 256;
        var buffer = new byte[headerSize + payload];
        fixed (byte* p = buffer)
        {
            var request = (USB_DESCRIPTOR_REQUEST*)p;
            request->ConnectionIndex = (uint)port;
            request->SetupPacket.bmRequest = 0x80; // device-to-host, standard, device
            request->SetupPacket.bRequest = 0x06;  // GET_DESCRIPTOR
            request->SetupPacket.wValue = (ushort)((0x03 << 8) | stringIndex); // STRING descriptor
            request->SetupPacket.wIndex = 0x0409;  // US English
            request->SetupPacket.wLength = payload;

            if (!DeviceIoControl(hub, PInvoke.IOCTL_USB_GET_DESCRIPTOR_FROM_NODE_CONNECTION, p, (uint)buffer.Length, p, (uint)buffer.Length, out var returned)
                || returned <= headerSize + 2)
            {
                return null;
            }

            var descLength = buffer[headerSize];
            if (descLength <= 2)
            {
                return null;
            }

            var charCount = (descLength - 2) / 2;
            var value = new string((char*)(p + headerSize + 2), 0, charCount);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }

    private static SafeHandle? OpenDevice(string path)
    {
        var handle = PInvoke.CreateFile(
            path,
            GenericWrite,
            FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
            null,
            FILE_CREATION_DISPOSITION.OPEN_EXISTING,
            0,
            null);

        return handle;
    }

    private static unsafe bool DeviceIoControl(SafeHandle device, uint code, void* input, uint inputSize, void* output, uint outputSize, out uint returned)
    {
        uint bytesReturned = 0;
        var ok = PInvoke.DeviceIoControl(device, code, input, inputSize, output, outputSize, &bytesReturned, null);
        returned = bytesReturned;
        return ok;
    }
}
