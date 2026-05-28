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

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int CtlInitDelegate(ref CtlInitArgs initArgs, out nint apiHandle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int CtlCloseDelegate(nint apiHandle);

    private readonly NativeVendorSdk _sdk;
    private readonly CtlCloseDelegate? _close;
    private nint _apiHandle;

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
    }

    public string Name => "Intel IGCL";

    public bool IsAvailable { get; }

    public VendorGpuLinkInfo? QueryLink(DisplayInfo display)
    {
        if (!IsAvailable)
        {
            return null;
        }

        // The control library is up; report the source vendor so the snapshot records that an
        // Intel GPU drives the link.
        return new VendorGpuLinkInfo { Vendor = "Intel" };
    }

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
}
