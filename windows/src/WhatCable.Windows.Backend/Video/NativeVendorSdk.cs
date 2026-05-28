using System.Runtime.InteropServices;

namespace WhatCable.Windows.Backend.Video;

/// <summary>
/// Late-binding host for an optional vendor GPU SDK. The native library is resolved at
/// runtime with <see cref="NativeLibrary"/> (which calls <c>LoadLibrary</c> on Windows) so a
/// missing SDK is never a hard dependency: when none of the <c>candidateDllNames</c> can be
/// loaded the host reports <see cref="IsLoaded"/> = <c>false</c> and the owning adapter
/// declines to self-register.
/// </summary>
internal sealed class NativeVendorSdk : IDisposable
{
    private nint _handle;

    private NativeVendorSdk(nint handle) => _handle = handle;

    public bool IsLoaded => _handle != 0;

    /// <summary>
    /// Attempts to load the first of <paramref name="candidateDllNames"/> that resolves.
    /// Returns a host whose <see cref="IsLoaded"/> is <c>false</c> when none can be loaded.
    /// </summary>
    public static NativeVendorSdk Load(params string[] candidateDllNames)
    {
        foreach (var name in candidateDllNames)
        {
            try
            {
                if (NativeLibrary.TryLoad(name, out var handle) && handle != 0)
                {
                    return new NativeVendorSdk(handle);
                }
            }
            catch
            {
                // Try the next candidate; a load failure simply means the SDK is absent.
            }
        }

        return new NativeVendorSdk(0);
    }

    /// <summary>
    /// Binds an exported native function to a managed delegate, or returns <c>null</c> when
    /// the export is missing. Never throws.
    /// </summary>
    public TDelegate? GetExport<TDelegate>(string exportName) where TDelegate : Delegate
    {
        if (_handle == 0)
        {
            return null;
        }

        try
        {
            if (NativeLibrary.TryGetExport(_handle, exportName, out var address) && address != 0)
            {
                return Marshal.GetDelegateForFunctionPointer<TDelegate>(address);
            }
        }
        catch
        {
            // Treat a missing or unbindable export as "not available".
        }

        return null;
    }

    public void Dispose()
    {
        if (_handle != 0)
        {
            try
            {
                NativeLibrary.Free(_handle);
            }
            catch
            {
                // Ignore unload failures.
            }

            _handle = 0;
        }
    }
}
