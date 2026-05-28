namespace WhatCable.Windows.Backend.Ucsi;

/// <summary>
/// Abstraction over a UCSI (USB Type-C Connector System Software Interface) transport.
/// </summary>
/// <remarks>
/// The production implementation (<c>WindowsUcsiTransport</c>) issues the
/// <c>IOCTL_UCM_UCSI_*</c> commands exposed by the UCM connector class driver. Tests provide a
/// fake backed by captured byte streams recorded from real Intel / AMD UCSI PCs, so the decode
/// and capability-gating logic can be exercised off-Windows.
///
/// Each <c>Get*</c> method returns the raw little-endian DATA portion of the UCSI MESSAGE_IN for
/// the requested command, or <see langword="null"/> when the command is unsupported, the
/// connector index is out of range, or the underlying transport fails. Connectors are addressed
/// with a 1-based index, matching the UCSI connector numbering.
/// </remarks>
public interface IUcsiTransport
{
    /// <summary>True when a UCSI provider was found and can answer commands.</summary>
    bool IsAvailable { get; }

    /// <summary>
    /// UCSI specification version as a BCD value (for example <c>0x0200</c> for UCSI 2.0,
    /// <c>0x0120</c> for UCSI 1.2). Zero when unknown.
    /// </summary>
    ushort Version { get; }

    /// <summary>Number of Type-C connectors reported by the platform (from GET_CAPABILITY).</summary>
    int ConnectorCount { get; }

    /// <summary>GET_CONNECTOR_CAPABILITY data for the connector.</summary>
    byte[]? GetConnectorCapability(int connector);

    /// <summary>GET_CONNECTOR_STATUS data for the connector.</summary>
    byte[]? GetConnectorStatus(int connector);

    /// <summary>
    /// GET_PDOS data for the connector: an array of 32-bit Power Data Objects.
    /// </summary>
    /// <param name="partner">
    /// When true the partner's PDOs are requested; when false the local connector's PDOs.
    /// </param>
    /// <param name="source">When true the source (provider) PDOs; when false the sink (consumer) PDOs.</param>
    byte[]? GetPdos(int connector, bool partner, bool source);

    /// <summary>GET_CABLE_PROPERTY data for the connector (e-marked cable properties).</summary>
    byte[]? GetCableProperty(int connector);

    /// <summary>
    /// GET_ALTERNATE_MODES data for the given recipient: an array of {SVID (16-bit), VDO (32-bit)} records.
    /// </summary>
    byte[]? GetAlternateModes(int connector, UcsiRecipient recipient);

    /// <summary>GET_CAM_SUPPORTED data for the connector: a bitmap of supported alternate-mode indices.</summary>
    byte[]? GetCamSupported(int connector);

    /// <summary>GET_CURRENT_CAM data for the connector: the currently entered alternate-mode index.</summary>
    byte[]? GetCurrentCam(int connector);

    /// <summary>
    /// GET_PD_MESSAGE (UCSI 2.0) data for a Structured VDM exchange (Discover Identity / Discover
    /// Modes) directed at the given recipient. Returns the response message payload (4-byte VDOs).
    /// </summary>
    byte[]? GetPdMessage(int connector, UcsiRecipient recipient, UcsiPdResponse response);

    /// <summary>
    /// Liquid-detection state for the connector (UCSI 2.0). Returns <see langword="null"/> when the
    /// platform does not implement the LIQUID_DETECTION notification.
    /// </summary>
    bool? GetLiquidDetected(int connector);
}

/// <summary>Recipient of a UCSI command or PD message (SOP / SOP' / SOP'').</summary>
public enum UcsiRecipient
{
    /// <summary>The attached port partner (SOP).</summary>
    Connector = 0,

    /// <summary>The cable plug nearest the connector (SOP').</summary>
    SopPrime = 1,

    /// <summary>The cable plug farthest from the connector (SOP'').</summary>
    SopDoublePrime = 2,
}

/// <summary>Type of Structured VDM response requested via GET_PD_MESSAGE.</summary>
public enum UcsiPdResponse
{
    /// <summary>Discover Identity ACK VDOs.</summary>
    DiscoverIdentity = 0,

    /// <summary>Discover SVIDs ACK VDOs.</summary>
    DiscoverSvids = 1,

    /// <summary>Discover Modes ACK VDOs.</summary>
    DiscoverModes = 2,
}
