using System.Runtime.InteropServices;
using Microsoft.Windows.Widgets.Providers;
using WinRT;

namespace WhatCable.Widgets;

/// <summary>
/// Minimal in-process COM class-object plumbing used to expose the managed
/// <see cref="IWidgetProvider"/> implementation as an out-of-process COM server, as required by the
/// Windows 11 Widgets Board host. Adapted from the Windows App SDK widget provider samples.
/// </summary>
internal static class ComGuids
{
    public const string IClassFactory = "00000001-0000-0000-C000-000000000046";
    public const string IUnknown = "00000000-0000-0000-C000-000000000046";
    public const string IInspectable = "AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90";
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid(ComGuids.IClassFactory)]
internal interface IClassFactory
{
    [PreserveSig]
    int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);

    [PreserveSig]
    int LockServer(bool fLock);
}

/// <summary>Class factory that hands the host a fresh managed widget provider instance.</summary>
internal sealed class WidgetProviderFactory<T> : IClassFactory
    where T : IWidgetProvider, new()
{
    private const int CLASS_E_NOAGGREGATION = -2147221232;
    private const int E_NOINTERFACE = -2147467262;

    // IIDs the host may request: IWidgetProvider, IInspectable (WinRT default), and IUnknown.
    private static readonly Guid IWidgetProviderIid = typeof(IWidgetProvider).GUID;
    private static readonly Guid IUnknownIid = Guid.Parse(ComGuids.IUnknown);
    private static readonly Guid IInspectableIid = Guid.Parse(ComGuids.IInspectable);

    private readonly Action _onServerIdle;

    internal WidgetProviderFactory(Action onServerIdle) => _onServerIdle = onServerIdle;

    public int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
    {
        ppvObject = IntPtr.Zero;

        if (pUnkOuter != IntPtr.Zero)
        {
            return CLASS_E_NOAGGREGATION;
        }

        if (riid == IWidgetProviderIid || riid == IUnknownIid || riid == IInspectableIid)
        {
            ppvObject = MarshalInspectable<IWidgetProvider>.FromManaged(new T());
        }
        else
        {
            return E_NOINTERFACE;
        }

        return 0;
    }

    public int LockServer(bool fLock)
    {
        uint refs = fLock
            ? NativeMethods.CoAddRefServerProcess()
            : NativeMethods.CoReleaseServerProcess();

        if (!fLock && refs == 0)
        {
            _onServerIdle();
        }

        return 0;
    }
}

/// <summary>
/// Registers the widget provider class object with COM for the lifetime of the process and revokes
/// it on dispose. <see cref="ExitWaitHandle"/> is signaled when the COM server's lock count drops to
/// zero so the host can unblock and then dispose/revoke the class object.
/// </summary>
internal sealed class WidgetProviderRegistration<TWidgetProvider> : IDisposable
    where TWidgetProvider : IWidgetProvider, new()
{
    private const uint CLSCTX_LOCAL_SERVER = 0x4;
    private const uint REGCLS_MULTIPLEUSE = 0x1;

    private readonly uint _cookie;
    private readonly ManualResetEvent _exitEvent;
    private bool _disposed;

    private WidgetProviderRegistration(uint cookie, ManualResetEvent exitEvent)
    {
        _cookie = cookie;
        _exitEvent = exitEvent;
    }

    public static WidgetProviderRegistration<TWidgetProvider> Register()
    {
        var exitEvent = new ManualResetEvent(false);
        var factory = new WidgetProviderFactory<TWidgetProvider>(() => exitEvent.Set());
        int hr = NativeMethods.CoRegisterClassObject(
            typeof(TWidgetProvider).GUID,
            factory,
            CLSCTX_LOCAL_SERVER,
            REGCLS_MULTIPLEUSE,
            out uint cookie);

        if (hr != 0)
        {
            exitEvent.Dispose();
            Marshal.ThrowExceptionForHR(hr);
        }

        return new WidgetProviderRegistration<TWidgetProvider>(cookie, exitEvent);
    }

    /// <summary>
    /// Signaled when <see cref="IClassFactory.LockServer"/> drops the COM server's reference count
    /// to zero. The main loop should wait on this before exiting the <c>using</c> scope so that
    /// <see cref="Dispose"/> can revoke the class object cleanly.
    /// </summary>
    public WaitHandle ExitWaitHandle => _exitEvent;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        NativeMethods.CoRevokeClassObject(_cookie);
        _disposed = true;
        _exitEvent.Dispose();
    }
}

/// <summary>Native COM class-object registration entry points (kept out of any generic type).</summary>
internal static class NativeMethods
{
    [DllImport("ole32.dll")]
    internal static extern int CoRegisterClassObject(
        [MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
        [MarshalAs(UnmanagedType.IUnknown)] object pUnk,
        uint dwClsContext,
        uint flags,
        out uint lpdwRegister);

    [DllImport("ole32.dll")]
    internal static extern int CoRevokeClassObject(uint dwRegister);

    [DllImport("ole32.dll")]
    internal static extern uint CoAddRefServerProcess();

    [DllImport("ole32.dll")]
    internal static extern uint CoReleaseServerProcess();
}
