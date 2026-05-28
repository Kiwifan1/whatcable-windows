namespace WhatCable.Windows.App.Core.Services;

/// <summary>The kind of plug event detected between two consecutive snapshots.</summary>
public enum PortChangeKind
{
    DeviceConnected,
    DeviceDisconnected,
    ChargerConnected,
    ChargerDisconnected,
}

/// <summary>A single connect/disconnect event worth surfacing as a toast.</summary>
public sealed record PortChange(PortChangeKind Kind, string PortName, string? Detail = null);

/// <summary>Shows OS toast notifications. Backed by <c>AppNotificationManager</c> in the WinUI app.</summary>
public interface INotificationService
{
    void Notify(string title, string message);
}
