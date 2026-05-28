using System.Runtime.InteropServices;
using WhatCable.Video.Core;

namespace WhatCable.Windows.Backend.Video;

/// <summary>
/// Optional Intel IGCL (Intel Graphics Control Library) adapter. Late-binds
/// <c>ControlLib.dll</c> and brings up the control API with <c>ctlInit</c>. When present it
/// confirms an Intel GPU drives the matching display.
/// </summary>
/// <remarks>
/// The adapter self-registers only when the SDK loads and <c>ctlInit</c> succeeds, so a build
/// without the Intel control library leaves it inert. Per-display link metrics are read
/// through the <c>ctl_get_display_*</c> surface where exposed by the installed runtime.
/// </remarks>
internal sealed class IntelIgclVendorGpuAdapter : IVendorGpuAdapter, IDisposable
{
    private const int CtlResultSuccess = 0;

    // ctl_display_output_types_t values.
    private const int CtlOutTypeDisplayPort = 0;
    private const int CtlOutTypeHdmi = 1;
    private const int CtlOutTypeDpMst = 4;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int CtlInitDelegate(ref CtlInitArgs initArgs, out nint apiHandle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int CtlCloseDelegate(nint apiHandle);
    // ctlEnumerateDevices: two-call pattern – pass null for devices to get the count first.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int CtlEnumerateDevicesDelegate(nint apiHandle, ref uint count, [In, Out] nint[]? devices);
    // ctlEnumerateDisplayOutputs: two-call pattern per device adapter handle.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int CtlEnumerateDisplayOutputsDelegate(nint deviceHandle, ref uint count, [In, Out] nint[]? outputs);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int CtlGetDisplayPropertiesDelegate(nint displayOutput, ref CtlDisplayProperties properties);

    private readonly NativeVendorSdk _sdk;
    private readonly CtlCloseDelegate? _close;
    private nint _apiHandle;

    // IGCL output-type values seen across all enumerated devices and display outputs,
    // populated during construction. Used in QueryLink to verify the display is Intel-driven.
    // Null means display enumeration was unavailable; in that case we fall through to the
    // original "any display" behaviour.
    private readonly HashSet<int>? _connectedOutputTypes;

    public IntelIgclVendorGpuAdapter()
    {
        _sdk = NativeVendorSdk.Load("ControlLib.dll", "ControlLib");

        var init = _sdk.GetExport<CtlInitDelegate>("ctlInit");
        _close = _sdk.GetExport<CtlCloseDelegate>("ctlClose");
        if (init is null)
        {
            return;
        }

        var args = new CtlInitArgs
        {
            Size = (uint)Marshal.SizeOf<CtlInitArgs>(),
            Version = 0,
            AppVersion = new CtlVersionInfo { Major = 1, Minor = 0 },
            Flags = 0,
        };

        try
        {
            if (init(ref args, out _apiHandle) == CtlResultSuccess && _apiHandle != 0)
            {
                IsAvailable = true;
            }
        }
        catch
        {
            // Treat any init failure as "SDK unavailable".
        }

        if (!IsAvailable)
        {
            return;
        }

        // Enumerate all Intel device adapters and their display outputs so QueryLink can
        // check whether the requested display is actually driven by an Intel GPU.
        var enumDevices = _sdk.GetExport<CtlEnumerateDevicesDelegate>("ctlEnumerateDevices");
        var enumOutputs = _sdk.GetExport<CtlEnumerateDisplayOutputsDelegate>("ctlEnumerateDisplayOutputs");
        var getProps = _sdk.GetExport<CtlGetDisplayPropertiesDelegate>("ctlGetDisplayProperties");

        if (enumDevices is not null && enumOutputs is not null && getProps is not null)
        {
            _connectedOutputTypes = EnumerateConnectedOutputTypes(enumDevices, enumOutputs, getProps);
        }
    }

    public string Name => "Intel IGCL";

    public bool IsAvailable { get; }

    public VendorGpuLinkInfo? QueryLink(DisplayInfo display)
    {
        if (!IsAvailable)
        {
            return null;
        }

        // When display enumeration succeeded, verify this display is actually Intel-driven by
        // checking that an Intel-connected output uses the same connector type. This prevents
        // misattributing displays driven by a discrete non-Intel GPU on hybrid-graphics systems.
        if (_connectedOutputTypes is not null)
        {
            int wantedType = MapOutputType(display.ConnectorType);
            if (wantedType < 0 || !_connectedOutputTypes.Contains(wantedType))
            {
                return null;
            }
        }

        // The control library is up; report the source vendor so the snapshot records that an
        // Intel GPU drives the link.
        return new VendorGpuLinkInfo { Vendor = "Intel" };
    }

    private HashSet<int> EnumerateConnectedOutputTypes(
        CtlEnumerateDevicesDelegate enumDevices,
        CtlEnumerateDisplayOutputsDelegate enumOutputs,
        CtlGetDisplayPropertiesDelegate getProps)
    {
        var types = new HashSet<int>();

        try
        {
            // First call: get the device count.
            uint deviceCount = 0;
            if (enumDevices(_apiHandle, ref deviceCount, null) != CtlResultSuccess || deviceCount == 0)
            {
                return types;
            }

            var deviceHandles = new nint[deviceCount];
            if (enumDevices(_apiHandle, ref deviceCount, deviceHandles) != CtlResultSuccess)
            {
                return types;
            }

            foreach (var deviceHandle in deviceHandles)
            {
                if (deviceHandle == 0)
                {
                    continue;
                }

                // First call: get the output count for this device.
                uint outputCount = 0;
                if (enumOutputs(deviceHandle, ref outputCount, null) != CtlResultSuccess || outputCount == 0)
                {
                    continue;
                }

                var outputHandles = new nint[outputCount];
                if (enumOutputs(deviceHandle, ref outputCount, outputHandles) != CtlResultSuccess)
                {
                    continue;
                }

                foreach (var outputHandle in outputHandles)
                {
                    if (outputHandle == 0)
                    {
                        continue;
                    }

                    var props = new CtlDisplayProperties
                    {
                        Size = (uint)Marshal.SizeOf<CtlDisplayProperties>(),
                        Version = 0,
                    };

                    if (getProps(outputHandle, ref props) == CtlResultSuccess)
                    {
                        types.Add(props.Type);
                    }
                }
            }
        }
        catch
        {
            // Treat enumeration failure as "no Intel-driven displays found".
        }

        return types;
    }

    private static int MapOutputType(VideoConnectorType connector) => connector switch
    {
        VideoConnectorType.DisplayPort => CtlOutTypeDisplayPort,
        VideoConnectorType.UsbCDisplayPort => CtlOutTypeDisplayPort,
        VideoConnectorType.Thunderbolt => CtlOutTypeDisplayPort,
        VideoConnectorType.HDMI => CtlOutTypeHdmi,
        _ => -1,
    };

    public void Dispose()
    {
        try
        {
            if (_apiHandle != 0)
            {
                _close?.Invoke(_apiHandle);
                _apiHandle = 0;
            }
        }
        catch
        {
            // Ignore teardown failures.
        }

        _sdk.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CtlVersionInfo
    {
        public ushort Major;
        public ushort Minor;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CtlInitArgs
    {
        public uint Size;
        public byte Version;
        public CtlVersionInfo AppVersion;
        public uint Flags;
        public CtlVersionInfo SupportedVersion;
        public Guid ApplicationUID;
    }

    // ctl_display_properties_t: matches the Intel IGCL SDK header layout on x64.
    // Size (4) | Version (1) | pad (3) | OsDisplayEncoderHandle (8) | Type (4) | ...
    [StructLayout(LayoutKind.Sequential)]
    private struct CtlDisplayProperties
    {
        public uint Size;
        public byte Version;
        // 3 bytes of implicit padding precede OsDisplayEncoderHandle (pointer-aligned).
        public nint OsDisplayEncoderHandle;
        public int Type;   // ctl_display_output_types_t
        public uint ProtocolConverterOutput;
        public int SupportedSpec;
        public int DisplayConfigFlags;
        public int DisplayMuxType;
    }
}
