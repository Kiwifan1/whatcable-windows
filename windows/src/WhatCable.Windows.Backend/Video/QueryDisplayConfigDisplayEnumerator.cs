using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using WhatCable.Video.Core;

namespace WhatCable.Windows.Backend.Video;

/// <summary>
/// Live display enumerator. Reads the active display paths with
/// <c>QueryDisplayConfig</c> + <c>DisplayConfigGetDeviceInfo</c> (connector type, active
/// timing, friendly name, HDR state) and pairs each with the monitor's EDID read from the
/// registry under
/// <c>HKLM\SYSTEM\CurrentControlSet\Enum\DISPLAY\…\Device Parameters\EDID</c>.
/// </summary>
/// <remarks>
/// All native access is wrapped defensively so a failure to read any single path or EDID is
/// swallowed and the CLI still produces a valid (possibly partial) snapshot.
/// </remarks>
internal sealed class QueryDisplayConfigDisplayEnumerator : IDisplayEnumerator
{
    public IReadOnlyList<DisplayInfo> EnumerateDisplays()
    {
        var displays = new List<DisplayInfo>();

        try
        {
            EnumerateCore(displays);
        }
        catch
        {
            // Never let enumeration failures crash the CLI; return whatever we collected.
        }

        return displays;
    }

    private static unsafe void EnumerateCore(List<DisplayInfo> displays)
    {
        uint pathCount = 0;
        uint modeCount = 0;
        if (PInvoke.GetDisplayConfigBufferSizes(QUERY_DISPLAY_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS, &pathCount, &modeCount) != 0)
        {
            return;
        }

        var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
        var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

        fixed (DISPLAYCONFIG_PATH_INFO* pPaths = paths)
        fixed (DISPLAYCONFIG_MODE_INFO* pModes = modes)
        {
            if (PInvoke.QueryDisplayConfig(QUERY_DISPLAY_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS, &pathCount, pPaths, &modeCount, pModes, null) != 0)
            {
                return;
            }
        }

        for (uint i = 0; i < pathCount; i++)
        {
            try
            {
                var display = ReadPath(paths[i], modes);
                if (display is not null)
                {
                    displays.Add(display);
                }
            }
            catch
            {
                // Skip a path we cannot read.
            }
        }
    }

    private static DisplayInfo? ReadPath(DISPLAYCONFIG_PATH_INFO path, DISPLAYCONFIG_MODE_INFO[] modes)
    {
        var activeMode = ReadActiveMode(path, modes);
        var (name, devicePath) = ReadTargetDeviceName(path);
        var connector = MapConnector(path.targetInfo.outputTechnology);
        var edid = ReadEdidFromRegistry(devicePath);

        return new DisplayInfo
        {
            Name = name,
            ConnectorType = connector,
            ActiveMode = activeMode,
            HdrEnabled = ReadHdrEnabled(path),
            EdidRaw = edid,
        };
    }

    private static VideoMode? ReadActiveMode(DISPLAYCONFIG_PATH_INFO path, DISPLAYCONFIG_MODE_INFO[] modes)
    {
        uint sourceIdx = path.sourceInfo.Anonymous.modeInfoIdx;
        if (sourceIdx >= modes.Length)
        {
            return null;
        }

        var sourceMode = modes[sourceIdx].Anonymous.sourceMode;
        int width = (int)sourceMode.width;
        int height = (int)sourceMode.height;
        if (width == 0 || height == 0)
        {
            return null;
        }

        double refresh = 0;
        var rate = path.targetInfo.refreshRate;
        if (rate.Denominator != 0)
        {
            refresh = (double)rate.Numerator / rate.Denominator;
        }

        return new VideoMode
        {
            WidthPx = width,
            HeightPx = height,
            RefreshRateHz = Math.Round(refresh, 3),
        };
    }

    private static unsafe (string? Name, string? DevicePath) ReadTargetDeviceName(DISPLAYCONFIG_PATH_INFO path)
    {
        var request = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
        request.header.type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
        request.header.size = (uint)sizeof(DISPLAYCONFIG_TARGET_DEVICE_NAME);
        request.header.adapterId = path.targetInfo.adapterId;
        request.header.id = path.targetInfo.id;

        if (PInvoke.DisplayConfigGetDeviceInfo(&request.header) != 0)
        {
            return (null, null);
        }

        var name = request.monitorFriendlyDeviceName.ToString();
        var devicePath = request.monitorDevicePath.ToString();
        return (string.IsNullOrWhiteSpace(name) ? null : name,
                string.IsNullOrWhiteSpace(devicePath) ? null : devicePath);
    }

    private static unsafe bool ReadHdrEnabled(DISPLAYCONFIG_PATH_INFO path)
    {
        var request = new DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO();
        request.header.type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO;
        request.header.size = (uint)sizeof(DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO);
        request.header.adapterId = path.targetInfo.adapterId;
        request.header.id = path.targetInfo.id;

        if (PInvoke.DisplayConfigGetDeviceInfo(&request.header) != 0)
        {
            return false;
        }

        return request.Anonymous.Anonymous.advancedColorEnabled;
    }

    private static VideoConnectorType MapConnector(DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY technology)
        => technology switch
        {
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_HD15 => VideoConnectorType.VGA,
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DVI => VideoConnectorType.DVI,
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_HDMI => VideoConnectorType.HDMI,
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EXTERNAL => VideoConnectorType.DisplayPort,
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EMBEDDED => VideoConnectorType.DisplayPort,
            DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY.DISPLAYCONFIG_OUTPUT_TECHNOLOGY_UDI_EXTERNAL => VideoConnectorType.DisplayPort,
            _ => VideoConnectorType.Unknown,
        };

    /// <summary>
    /// Reads the EDID for a monitor from the registry. The <paramref name="monitorDevicePath"/>
    /// returned by <c>DisplayConfigGetDeviceInfo</c> looks like
    /// <c>\\?\DISPLAY#DELA0B6#5&amp;…&amp;UID4352#{guid}</c>; the <c>#</c>-separated parts map to
    /// <c>HKLM\SYSTEM\CurrentControlSet\Enum\DISPLAY\DELA0B6\5&amp;…&amp;UID4352\Device Parameters\EDID</c>.
    /// </summary>
    private static byte[]? ReadEdidFromRegistry(string? monitorDevicePath)
    {
        if (string.IsNullOrEmpty(monitorDevicePath))
        {
            return null;
        }

        const string prefix = @"\\?\";
        var trimmed = monitorDevicePath!.StartsWith(prefix, StringComparison.Ordinal)
            ? monitorDevicePath.Substring(prefix.Length)
            : monitorDevicePath;
        var parts = trimmed.Split('#');
        if (parts.Length < 3 || !string.Equals(parts[0], "DISPLAY", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var subKey = $@"SYSTEM\CurrentControlSet\Enum\DISPLAY\{parts[1]}\{parts[2]}\Device Parameters";
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(subKey);
            return key?.GetValue("EDID") as byte[];
        }
        catch
        {
            return null;
        }
    }
}
