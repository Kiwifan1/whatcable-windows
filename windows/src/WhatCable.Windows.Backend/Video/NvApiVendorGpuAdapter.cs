using System.Runtime.InteropServices;
using WhatCable.Video.Core;

namespace WhatCable.Windows.Backend.Video;

/// <summary>
/// Optional NVAPI adapter. Late-binds <c>nvapi64.dll</c> (the export-table entry point is
/// <c>nvapi_QueryInterface</c>, from which every other function is resolved by its 32-bit
/// function id) and, when present, reports the negotiated DisplayPort link rate and lane
/// count for the connected display via <c>NvAPI_GPU_GetDisplayPortInfo</c>.
/// </summary>
/// <remarks>
/// The adapter self-registers only when the SDK loads <em>and</em> <c>NvAPI_Initialize</c>
/// succeeds, so the default redistributable build (which ships no NVAPI) leaves it inert.
/// HDMI FRL rate / scrambling / DSC state are not exposed by the public NVAPI surface and
/// are left unset.
/// </remarks>
internal sealed class NvApiVendorGpuAdapter : IVendorGpuAdapter, IDisposable
{
    // NvAPI_QueryInterface function ids (stable across SDK versions).
    private const uint IdInitialize = 0x0150E828;
    private const uint IdUnload = 0xD22BDD7E;
    private const uint IdEnumPhysicalGPUs = 0xE5AC921F;
    private const uint IdGetConnectedDisplayIds = 0x5F719144;
    private const uint IdGetDisplayPortInfo = 0xC64FF367;

    private const int MaxPhysicalGpus = 64;
    private const int StatusOk = 0;

    // NV_MONITOR_CONN_TYPE values used to match our connector classification.
    private const uint MonitorConnVga = 1;
    private const uint MonitorConnHdmi = 4;
    private const uint MonitorConnDvi = 5;
    private const uint MonitorConnDisplayPort = 7;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint QueryInterfaceDelegate(uint id);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SimpleStatusDelegate();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EnumPhysicalGpusDelegate([Out] nint[] handles, ref uint count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GetConnectedDisplayIdsDelegate(nint gpu, [In, Out] NvGpuDisplayId[]? ids, ref uint count, uint flags);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int GetDisplayPortInfoDelegate(nint gpu, uint displayId, ref NvDisplayPortInfo info);

    private readonly NativeVendorSdk _sdk;
    private readonly SimpleStatusDelegate? _unload;
    private readonly EnumPhysicalGpusDelegate? _enumGpus;
    private readonly GetConnectedDisplayIdsDelegate? _getConnectedDisplayIds;
    private readonly GetDisplayPortInfoDelegate? _getDisplayPortInfo;

    public NvApiVendorGpuAdapter()
    {
        _sdk = NativeVendorSdk.Load("nvapi64.dll", "nvapi.dll");

        var queryInterface = _sdk.GetExport<QueryInterfaceDelegate>("nvapi_QueryInterface");
        if (queryInterface is null)
        {
            return;
        }

        var initialize = Resolve<SimpleStatusDelegate>(queryInterface, IdInitialize);
        if (initialize is null || initialize() != StatusOk)
        {
            return;
        }

        _unload = Resolve<SimpleStatusDelegate>(queryInterface, IdUnload);
        _enumGpus = Resolve<EnumPhysicalGpusDelegate>(queryInterface, IdEnumPhysicalGPUs);
        _getConnectedDisplayIds = Resolve<GetConnectedDisplayIdsDelegate>(queryInterface, IdGetConnectedDisplayIds);
        _getDisplayPortInfo = Resolve<GetDisplayPortInfoDelegate>(queryInterface, IdGetDisplayPortInfo);

        IsAvailable = _enumGpus is not null && _getConnectedDisplayIds is not null;
    }

    public string Name => "NVAPI";

    public bool IsAvailable { get; }

    public VendorGpuLinkInfo? QueryLink(DisplayInfo display)
    {
        if (!IsAvailable || _enumGpus is null || _getConnectedDisplayIds is null)
        {
            return null;
        }

        uint wantedConnector = MapConnector(display.ConnectorType);
        if (wantedConnector == 0)
        {
            return null;
        }

        var handles = new nint[MaxPhysicalGpus];
        uint gpuCount = 0;
        if (_enumGpus(handles, ref gpuCount) != StatusOk)
        {
            return null;
        }

        for (uint g = 0; g < gpuCount; g++)
        {
            var gpu = handles[g];

            uint idCount = 0;
            if (_getConnectedDisplayIds(gpu, null, ref idCount, 0) != StatusOk || idCount == 0)
            {
                continue;
            }

            var ids = new NvGpuDisplayId[idCount];
            ids[0].version = MakeVersion(Marshal.SizeOf<NvGpuDisplayId>(), 3);
            if (_getConnectedDisplayIds(gpu, ids, ref idCount, 0) != StatusOk)
            {
                continue;
            }

            for (uint i = 0; i < idCount; i++)
            {
                if (ids[i].connectorType != wantedConnector)
                {
                    continue;
                }

                if (wantedConnector == MonitorConnDisplayPort && _getDisplayPortInfo is not null)
                {
                    var link = ReadDisplayPortLink(gpu, ids[i].displayId);
                    if (link is not null)
                    {
                        return link;
                    }
                }

                // The connector matched but no richer link metrics are available on this
                // NVAPI surface; report the vendor so the snapshot still records the source.
                return new VendorGpuLinkInfo { Vendor = "NVIDIA" };
            }
        }

        return null;
    }

    private VendorGpuLinkInfo? ReadDisplayPortLink(nint gpu, uint displayId)
    {
        var info = new NvDisplayPortInfo { version = MakeVersion(Marshal.SizeOf<NvDisplayPortInfo>(), 1) };
        if (_getDisplayPortInfo!(gpu, displayId, ref info) != StatusOk)
        {
            return null;
        }

        // NV_DP_LINK_RATE is expressed in multiples of 0.27 Gbps; NV_DP_LANE_COUNT is the
        // lane count directly (1/2/4).
        double? rate = info.curLinkRate > 0 ? Math.Round(info.curLinkRate * 0.27, 2) : null;
        int? lanes = info.curLaneCount > 0 ? (int)info.curLaneCount : null;
        if (rate is null && lanes is null)
        {
            return null;
        }

        return new VendorGpuLinkInfo
        {
            Vendor = "NVIDIA",
            DpLinkRateGbps = rate,
            DpLaneCount = lanes,
        };
    }

    private static uint MapConnector(VideoConnectorType connector) => connector switch
    {
        VideoConnectorType.HDMI => MonitorConnHdmi,
        VideoConnectorType.DisplayPort => MonitorConnDisplayPort,
        VideoConnectorType.UsbCDisplayPort => MonitorConnDisplayPort,
        VideoConnectorType.Thunderbolt => MonitorConnDisplayPort,
        VideoConnectorType.DVI => MonitorConnDvi,
        VideoConnectorType.VGA => MonitorConnVga,
        _ => 0,
    };

    private static TDelegate? Resolve<TDelegate>(QueryInterfaceDelegate queryInterface, uint id)
        where TDelegate : Delegate
    {
        try
        {
            var ptr = queryInterface(id);
            return ptr == 0 ? null : Marshal.GetDelegateForFunctionPointer<TDelegate>(ptr);
        }
        catch
        {
            return null;
        }
    }

    // NVAPI versioned-struct macro: low 16 bits = struct size, high bits = version number.
    private static uint MakeVersion(int structSize, uint version) => (uint)structSize | (version << 16);

    public void Dispose()
    {
        try
        {
            _unload?.Invoke();
        }
        catch
        {
            // Ignore teardown failures.
        }

        _sdk.Dispose();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NvGpuDisplayId
    {
        public uint version;
        public uint connectorType;
        public uint displayId;
        public uint flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NvDisplayPortInfo
    {
        public uint version;
        public uint dpcdVer;
        public uint maxLinkRate;
        public uint maxLaneCount;
        public uint curLinkRate;
        public uint curLaneCount;
        public uint colorFormat;
        public uint dynamicRange;
        public uint bpc;
        public uint colorimetry;
        public uint flags;
    }
}
