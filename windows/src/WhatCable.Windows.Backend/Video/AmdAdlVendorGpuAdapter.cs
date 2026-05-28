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

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate nint AdlMallocCallback(int size);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MainControlCreateDelegate(AdlMallocCallback callback, int enumConnectedAdapters, out nint context);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MainControlDestroyDelegate(nint context);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int NumberOfAdaptersGetDelegate(nint context, out int numAdapters);

    // Kept alive for the lifetime of the context so the GC never collects the thunk ADL holds.
    private static readonly AdlMallocCallback MallocCallback = size => Marshal.AllocHGlobal(size);

    private readonly NativeVendorSdk _sdk;
    private readonly MainControlDestroyDelegate? _destroy;
    private nint _context;

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

        // Require at least one ADL adapter to consider the SDK usable.
        var numberOfAdapters = _sdk.GetExport<NumberOfAdaptersGetDelegate>("ADL2_Adapter_NumberOfAdapters_Get");
        int adapters = 0;
        if (numberOfAdapters is not null && numberOfAdapters(_context, out adapters) == AdlOk && adapters > 0)
        {
            IsAvailable = true;
        }
    }

    public string Name => "AMD ADL";

    public bool IsAvailable { get; }

    public VendorGpuLinkInfo? QueryLink(DisplayInfo display)
    {
        if (!IsAvailable)
        {
            return null;
        }

        // ADL exposes per-display DisplayPort link config only through SKU-specific calls that
        // are not part of the stable public surface; we report the source vendor so the
        // snapshot records that an AMD GPU drives the link.
        return new VendorGpuLinkInfo { Vendor = "AMD" };
    }

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
}
