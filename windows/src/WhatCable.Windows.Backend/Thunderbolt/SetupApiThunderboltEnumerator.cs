using Windows.Win32;
using Windows.Win32.Devices.DeviceAndDriverInstallation;
using Windows.Win32.Foundation;

namespace WhatCable.Windows.Backend.Thunderbolt;

/// <summary>
/// Live Thunderbolt/USB4 device-chain enumerator.
/// </summary>
/// <remarks>
/// Windows exposes no stable, vendor-independent device-interface class for Thunderbolt
/// controllers (and no public per-lane speed API), so we walk the present device tree with
/// SetupAPI (<see cref="PInvoke.SetupDiEnumDeviceInfo"/>) and select the Thunderbolt/USB4
/// host routers and devices by their hardware IDs. The resulting ordered list is the
/// device chain; per-lane speeds are intentionally omitted (see <see cref="ThunderboltDevice"/>).
/// All native access is wrapped defensively so enumeration never crashes the CLI.
/// </remarks>
internal sealed class SetupApiThunderboltEnumerator : IThunderboltEnumerator
{
    // Hardware-ID / instance markers that identify Thunderbolt or USB4 routers.
    private static readonly string[] Markers = { "USB4", "THUNDERBOLT" };

    public IReadOnlyList<ThunderboltDeviceInfo> EnumerateChain()
    {
        var devices = new List<ThunderboltDeviceInfo>();

        try
        {
            EnumerateCore(devices);
        }
        catch
        {
            // Never let enumeration failures crash the CLI; return whatever we collected.
        }

        return devices;
    }

    private static unsafe void EnumerateCore(List<ThunderboltDeviceInfo> devices)
    {
        using var deviceInfo = PInvoke.SetupDiGetClassDevs(
            (Guid?)null,
            null,
            default,
            SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_PRESENT | SETUP_DI_GET_CLASS_DEVS_FLAGS.DIGCF_ALLCLASSES);

        if (deviceInfo.IsInvalid)
        {
            return;
        }

        for (uint index = 0; ; index++)
        {
            var data = new SP_DEVINFO_DATA { cbSize = (uint)sizeof(SP_DEVINFO_DATA) };
            if (!PInvoke.SetupDiEnumDeviceInfo(deviceInfo, index, ref data))
            {
                break;
            }

            var instanceId = GetInstanceId(deviceInfo, data);
            if (string.IsNullOrEmpty(instanceId) || !IsThunderbolt(instanceId!, GetHardwareIds(deviceInfo, data)))
            {
                continue;
            }

            devices.Add(new ThunderboltDeviceInfo
            {
                Name = GetFriendlyName(deviceInfo, data),
                InstanceId = instanceId!,
            });
        }
    }

    private static bool IsThunderbolt(string instanceId, string hardwareIds)
    {
        foreach (var marker in Markers)
        {
            if (instanceId.Contains(marker, StringComparison.OrdinalIgnoreCase)
                || hardwareIds.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static unsafe string? GetInstanceId(SetupDiDestroyDeviceInfoListSafeHandle deviceInfo, SP_DEVINFO_DATA data)
    {
        uint required = 0;
        PInvoke.SetupDiGetDeviceInstanceId(deviceInfo, data, default, 0, &required);
        if (required == 0)
        {
            return null;
        }

        Span<char> buffer = required <= 512 ? stackalloc char[(int)required] : new char[required];
        fixed (char* p = buffer)
        {
            if (!PInvoke.SetupDiGetDeviceInstanceId(deviceInfo, data, new PWSTR(p), required, null))
            {
                return null;
            }

            return new string(p);
        }
    }

    private static string GetHardwareIds(SetupDiDestroyDeviceInfoListSafeHandle deviceInfo, SP_DEVINFO_DATA data)
        => ReadStringProperty(deviceInfo, data, SETUP_DI_REGISTRY_PROPERTY.SPDRP_HARDWAREID) ?? string.Empty;

    private static string? GetFriendlyName(SetupDiDestroyDeviceInfoListSafeHandle deviceInfo, SP_DEVINFO_DATA data)
        => ReadStringProperty(deviceInfo, data, SETUP_DI_REGISTRY_PROPERTY.SPDRP_FRIENDLYNAME)
           ?? ReadStringProperty(deviceInfo, data, SETUP_DI_REGISTRY_PROPERTY.SPDRP_DEVICEDESC);

    private static unsafe string? ReadStringProperty(SetupDiDestroyDeviceInfoListSafeHandle deviceInfo, SP_DEVINFO_DATA data, SETUP_DI_REGISTRY_PROPERTY property)
    {
        uint required = 0;
        PInvoke.SetupDiGetDeviceRegistryProperty(deviceInfo, data, property, null, Span<byte>.Empty, &required);
        if (required == 0)
        {
            return null;
        }

        var buffer = new byte[required];
        if (!PInvoke.SetupDiGetDeviceRegistryProperty(deviceInfo, data, property, null, buffer, null))
        {
            return null;
        }

        fixed (byte* p = buffer)
        {
            var value = new string((char*)p).TrimEnd('\0');
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}
