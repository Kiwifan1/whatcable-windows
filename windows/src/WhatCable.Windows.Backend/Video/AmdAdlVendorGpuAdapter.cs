using System.Runtime.InteropServices;
using WhatCable.Video.Core;

namespace WhatCable.Windows.Backend.Video;

/// <summary>
/// Optional AMD ADL adapter. Late-binds <c>atiadlxx.dll</c> and brings up an ADL2 context
/// (which requires a caller-supplied memory-allocation callback). When present it confirms an
/// AMD GPU drives the matching display and reports the DisplayPort link configuration where
/// ADL exposes it.
/// </summary>
/// <remarks>
/// The adapter self-registers only when the SDK loads and <c>ADL2_Main_Control_Create</c>
/// succeeds, so a build without the AMD driver SDK leaves it inert.
/// </remarks>
internal sealed class AmdAdlVendorGpuAdapter : IVendorGpuAdapter, IDisposable
{
    private const int AdlOk = 0;

    // ADL_DISPLAY_CONTYPE_* values used to match our VideoConnectorType.
    private const int AdlConTypeDisplayPort = 10;
    private const int AdlConTypeEdp = 11;
    private const int AdlConTypeMiniDisplayPort = 12;
    private const int AdlConTypeHdmiTypeA = 20;
    private const int AdlConTypeHdmiTypeB = 21;

    // ADL_DISPLAYINFOMASK_CONNECT: bit 0 of iDisplayInfoValue indicates a connected display.
    private const int AdlDisplayInfoMaskConnect = 0x00000001;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate nint AdlMallocCallback(int size);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MainControlCreateDelegate(AdlMallocCallback callback, int enumConnectedAdapters, out nint context);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MainControlDestroyDelegate(nint context);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NumberOfAdaptersGetDelegate(nint context, out int numAdapters);
    // ADL2_Display_DisplayInfo_Get: ADL allocates *lppInfo via the malloc callback; caller must
    // free it with Marshal.FreeHGlobal when done.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DisplayInfoGetDelegate(nint context, int adapterIndex, out int numDisplays, out nint displayInfoArray, int forceDetect);

    // Kept alive for the lifetime of the context so the GC never collects the thunk ADL holds.
    private static readonly AdlMallocCallback MallocCallback = size => Marshal.AllocHGlobal(size);

    private readonly NativeVendorSdk _sdk;
    private readonly MainControlDestroyDelegate? _destroy;
    private nint _context;

    // ADL connector-type values seen across all enumerated adapters, populated during
    // construction. Used in QueryLink to verify the requested display is AMD-driven.
    // Null means display enumeration was unavailable; in that case we fall through
    // to the original "any display" behaviour.
    private readonly HashSet<int>? _connectedAdlConnectors;

    public AmdAdlVendorGpuAdapter()
    {
        _sdk = NativeVendorSdk.Load("atiadlxx.dll", "atiadlxy.dll");

        var create = _sdk.GetExport<MainControlCreateDelegate>("ADL2_Main_Control_Create");
        _destroy = _sdk.GetExport<MainControlDestroyDelegate>("ADL2_Main_Control_Destroy");
        if (create is null)
        {
            return;
        }

        try
        {
            if (create(MallocCallback, 1, out _context) != AdlOk || _context == 0)
            {
                return;
            }
        }
        catch
        {
            return;
        }

        // ADL2_Adapter_NumberOfAdapters_Get only sets an int output; it does not
        // allocate a buffer via the malloc callback, so there is nothing to free.
        var numberOfAdapters = _sdk.GetExport<NumberOfAdaptersGetDelegate>("ADL2_Adapter_NumberOfAdapters_Get");
        int adapters = 0;
        if (numberOfAdapters is null || numberOfAdapters(_context, out adapters) != AdlOk || adapters <= 0)
        {
            return;
        }

        // Enumerate all connected displays for every ADL adapter. ADL allocates each
        // display-info array via the malloc callback; we free it immediately after reading
        // with Marshal.FreeHGlobal so no unmanaged memory is leaked.
        var displayInfoGet = _sdk.GetExport<DisplayInfoGetDelegate>("ADL2_Display_DisplayInfo_Get");
        if (displayInfoGet is not null)
        {
            var connectors = new HashSet<int>();
            int stride = Marshal.SizeOf<AdlDisplayInfo>();

            for (int i = 0; i < adapters; i++)
            {
                int numDisplays = 0;
                nint displayPtr = 0;
                if (displayInfoGet(_context, i, out numDisplays, out displayPtr, 0) != AdlOk || displayPtr == 0)
                {
                    continue;
                }

                try
                {
                    for (int j = 0; j < numDisplays; j++)
                    {
                        var info = Marshal.PtrToStructure<AdlDisplayInfo>(displayPtr + j * stride);
                        // Only count displays that are physically connected.
                        if ((info.InfoValue & AdlDisplayInfoMaskConnect) != 0)
                        {
                            connectors.Add(info.Connector);
                        }
                    }
                }
                finally
                {
                    // Free the ADL-allocated buffer regardless of whether reading succeeded.
                    if (displayPtr != 0)
                    {
                        Marshal.FreeHGlobal(displayPtr);
                    }
                }
            }

            _connectedAdlConnectors = connectors;
        }

        IsAvailable = true;
    }

    public string Name => "AMD ADL";

    public bool IsAvailable { get; }

    public VendorGpuLinkInfo? QueryLink(DisplayInfo display)
    {
        if (!IsAvailable)
        {
            return null;
        }

        // When display enumeration succeeded, verify this display is actually AMD-driven by
        // checking that an ADL-connected display uses the same connector type. This prevents
        // misattributing displays driven by a discrete non-AMD GPU on hybrid-graphics systems.
        if (_connectedAdlConnectors is not null)
        {
            int wantedConnector = MapConnector(display.ConnectorType);
            if (wantedConnector == 0 || !_connectedAdlConnectors.Contains(wantedConnector))
            {
                return null;
            }
        }

        // ADL exposes per-display DisplayPort link config only through SKU-specific calls that
        // are not part of the stable public surface; we report the source vendor so the
        // snapshot records that an AMD GPU drives the link.
        return new VendorGpuLinkInfo { Vendor = "AMD" };
    }

    private static int MapConnector(VideoConnectorType connector) => connector switch
    {
        VideoConnectorType.DisplayPort => AdlConTypeDisplayPort,
        VideoConnectorType.UsbCDisplayPort => AdlConTypeDisplayPort,
        VideoConnectorType.Thunderbolt => AdlConTypeDisplayPort,
        VideoConnectorType.HDMI => AdlConTypeHdmiTypeA,
        _ => 0,
    };

    public void Dispose()
    {
        try
        {
            if (_context != 0)
            {
                _destroy?.Invoke(_context);
                _context = 0;
            }
        }
        catch
        {
            // Ignore teardown failures.
        }

        _sdk.Dispose();
    }

    // ADL display identifier (ADLDisplayID).
    [StructLayout(LayoutKind.Sequential)]
    private struct AdlDisplayId
    {
        public int LogicalIndex;
        public int PhysicalIndex;
        public int LogicalAdapterIndex;
        public int PhysicalAdapterIndex;
    }

    // ADLDisplayInfo: layout matches the AMD ADL SDK header (adl_structures.h).
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct AdlDisplayInfo
    {
        public AdlDisplayId DisplayId;
        public int ControllerIndex;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string DisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string ManufacturerName;
        public int DisplayType;
        public int OutputType;
        public int Connector;   // ADL_DISPLAY_CONTYPE_*
        public int InfoValue;   // ADL_DISPLAYINFOMASK_* flags (bit 0 = connected)
        public int InfoMask;
    }
}
