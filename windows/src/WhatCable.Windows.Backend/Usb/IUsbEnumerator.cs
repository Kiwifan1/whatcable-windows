namespace WhatCable.Windows.Backend.Usb;

/// <summary>
/// Abstraction over USB host-controller enumeration. The production implementation
/// (<c>SetupApiUsbEnumerator</c>) walks SetupAPI + hub IOCTLs; tests provide a fake
/// returning recorded device trees.
/// </summary>
public interface IUsbEnumerator
{
    IReadOnlyList<UsbHostController> EnumerateHostControllers();
}
