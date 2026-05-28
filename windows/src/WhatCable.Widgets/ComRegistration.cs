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

    public int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
    {
        ppvObject = IntPtr.Zero;

        if (pUnkOuter != IntPtr.Zero)
        {
            Marshal.ThrowExceptionForHR(CLASS_E_NOAGGREGATION);
        }

        if (riid == typeof(T).GUID || riid == Guid.Parse(ComGuids.IUnknown))
        {
            ppvObject = MarshalInspectable<IWidgetProvider>.FromManaged(new T());
        }
        else
        {
            Marshal.ThrowExceptionForHR(E_NOINTERFACE);
        }

        return 0;
    }

    public int LockServer(bool fLock) => 0;
}

/// <summary>
/// Registers the widget provider class object with COM for the lifetime of the process and revokes
/// it on dispose. <see cref="GetDisposedEvent"/> lets a headless host block until the last widget is
/// removed.
/// </summary>
internal sealed class WidgetProviderRegistration<TWidgetProvider> : IDisposable
    where TWidgetProvider : IWidgetProvider, new()
{
    private const uint CLSCTX_LOCAL_SERVER = 0x4;
    private const uint REGCLS_MULTIPLEUSE = 0x1;

    private readonly uint _cookie;
    private readonly ManualResetEvent _disposedEvent = new(false);
    private bool _disposed;

    private WidgetProviderRegistration(uint cookie) => _cookie = cookie;

    public static WidgetProviderRegistration<TWidgetProvider> Register()
    {
        var factory = new WidgetProviderFactory<TWidgetProvider>();
        int hr = NativeMethods.CoRegisterClassObject(
            typeof(TWidgetProvider).GUID,
            factory,
            CLSCTX_LOCAL_SERVER,
            REGCLS_MULTIPLEUSE,
            out uint cookie);

        if (hr != 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        return new WidgetProviderRegistration<TWidgetProvider>(cookie);
    }

    public ManualResetEvent GetDisposedEvent() => _disposedEvent;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        NativeMethods.CoRevokeClassObject(_cookie);
        _disposed = true;
        _disposedEvent.Set();
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
}
