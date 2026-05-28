namespace WhatCable.Windows.Backend.Ucsi;

/// <summary>
/// Live Windows UCSI transport.
/// </summary>
/// <remarks>
/// <para>
/// UCSI on Windows is mediated by the in-box UCM connector class extension
/// (<c>UcmUcsiCx</c>): a platform's ACPI/embedded-controller UCSI mailbox is driven by a client
/// driver, and the standard UCSI commands (<c>GET_CONNECTOR_CAPABILITY</c>,
/// <c>GET_CONNECTOR_STATUS</c>, <c>GET_PDOS</c>, <c>GET_CABLE_PROPERTY</c>,
/// <c>GET_ALTERNATE_MODES</c>, <c>GET_CAM_SUPPORTED</c>, <c>GET_PD_MESSAGE</c>) are issued over the
/// kernel-mode <c>IOCTL_UCM_UCSI_*</c> interface. That interface is driver-to-driver; there is no
/// in-box user-mode device interface that forwards raw UCSI command blocks to a desktop app.
/// </para>
/// <para>
/// This class is the integration seam for that transport. It implements <see cref="IUcsiTransport"/>
/// so the decode / capability-gating pipeline (<see cref="UcsiDecoder"/>, <see cref="UcsiAdapter"/>)
/// is fully wired and exercised today via captured byte streams, and lights up the snapshot's PD /
/// e-marker / alt-mode / liquid-detection fields the moment a real command channel is supplied —
/// for example the small UCM passthrough filter driver that ships in a later PR. Until that channel
/// is present the transport reports <see cref="IsAvailable"/> = <see langword="false"/> and the
/// snapshot degrades gracefully (no Type-C connectors, USB host-controller ports carry their
/// <c>unavailable_reason</c>).
/// </para>
/// </remarks>
internal sealed class WindowsUcsiTransport : IUcsiTransport
{
    public bool IsAvailable => false;

    public ushort Version => 0;

    public int ConnectorCount => 0;

    public byte[]? GetConnectorCapability(int connector) => null;

    public byte[]? GetConnectorStatus(int connector) => null;

    public byte[]? GetPdos(int connector, bool partner, bool source) => null;

    public byte[]? GetCableProperty(int connector) => null;

    public byte[]? GetAlternateModes(int connector, UcsiRecipient recipient) => null;

    public byte[]? GetCamSupported(int connector) => null;

    public byte[]? GetPdMessage(int connector, UcsiRecipient recipient, UcsiPdResponse response) => null;

    public bool? GetLiquidDetected(int connector) => null;
}
