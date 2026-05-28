using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using Windows.ApplicationModel;
using WhatCable.Windows.App.Core.Services;

namespace WhatCable.Windows.App.Services;

/// <summary>
/// Launch-at-login service. Prefers the packaged <c>StartupTask</c> (MSIX build) and falls back to
/// the per-user <c>Run</c> registry key for the unpackaged Inno Setup build, which has no identity.
/// </summary>
public sealed class StartupTaskService : IStartupTaskService
{
    private const string StartupTaskId = "WhatCableStartup";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "WhatCable";

    /// <summary>Win32 <c>APPMODEL_ERROR_NO_PACKAGE</c>: returned when the process has no package identity.</summary>
    private const int AppModelErrorNoPackage = 15700;

    private readonly bool _packaged;
    private StartupTask? _task;

    public StartupTaskService()
    {
        _packaged = HasPackageIdentity();
        if (_packaged)
        {
            try
            {
                _task = StartupTask.GetAsync(StartupTaskId).AsTask().GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                _task = null;
            }
        }

        Refresh();
    }

    public bool IsEnabled { get; private set; }

    public bool CanToggle { get; private set; } = true;

    public async Task<bool> SetEnabledAsync(bool enabled)
    {
        if (_packaged && _task is not null)
        {
            if (enabled)
            {
                var state = await _task.RequestEnableAsync();
                IsEnabled = state is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
            }
            else
            {
                _task.Disable();
                IsEnabled = false;
            }
        }
        else
        {
            SetRegistryRun(enabled);
            IsEnabled = enabled;
        }

        Refresh();
        return IsEnabled;
    }

    private void Refresh()
    {
        if (_packaged && _task is not null)
        {
            CanToggle = _task.State is not (StartupTaskState.DisabledByUser or StartupTaskState.DisabledByPolicy);
            IsEnabled = _task.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
        }
        else
        {
            CanToggle = true;
            IsEnabled = ReadRegistryRun();
        }
    }

    private static bool ReadRegistryRun()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(RunValueName) is not null;
    }

    private static void SetRegistryRun(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (key is null)
        {
            return;
        }

        if (enabled)
        {
            var exe = Environment.ProcessPath ?? string.Empty;
            key.SetValue(RunValueName, $"\"{exe}\"");
        }
        else
        {
            key.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
    }

    private static bool HasPackageIdentity()
    {
        // GetCurrentPackageFullName returns APPMODEL_ERROR_NO_PACKAGE when unpackaged.
        int length = 0;
        var result = GetCurrentPackageFullName(ref length, null);
        return result != AppModelErrorNoPackage;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);
}
