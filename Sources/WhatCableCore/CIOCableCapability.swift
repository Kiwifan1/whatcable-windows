import Foundation

/// Cable capability data from Apple's CIO (Thunderbolt) transport controller.
///
/// These properties come from `IOPortTransportStateCIO`, which appears
/// dynamically when a Thunderbolt link is active. They represent the TB
/// controller's own assessment of the cable, independent of the USB-PD
/// e-marker. This matters because some active TB4 cables (e.g. CalDigit
/// 2M) report "passive" in their e-marker while the CIO controller
/// correctly identifies their TB capability.
///
/// Value mappings are based on 9 confirmed data points (7 TB4, 2 TB5).
/// TB3 samples are still needed. See `research/cio-value-mappings.md`.
public struct CIOCableCapability: Identifiable, Hashable, Sendable {
    public let id: UInt64
    /// Port correlation key matching `PowerSource.portKey`.
    public let portKey: String

    /// CIO protocol mode of the downstream controller. Not a generation
    /// counter: 1 = legacy TB3 (pre-USB4), 2 = USB4/CIO native (TB4 and
    /// TB5 both report 2). See `research/cio-value-mappings.md`.
    public let cableGeneration: Int?
    /// Cable speed capability. The one CIO field that genuinely tracks
    /// bandwidth: 3 = 40 Gbps (TB4), 4 = 80 Gbps (TB5). TB3 expected
    /// to be 2 but unconfirmed.
    public let cableSpeed: Int?
    /// CIO tunnel protocol version. Parallels `cableGeneration`:
    /// 2 = legacy TB3, 3 = USB4/CIO native (TB4 and TB5 both report 3).
    /// Not USB4 Gen numbering.
    public let generation: Int?
    /// Whether the cable/link supports asymmetric mode (120/40 Gbps).
    public let asymmetricModeSupported: Bool?
    /// True for TB3 legacy adapter connections, false for native USB4/TB4+.
    public let legacyAdapter: Bool?
    /// Link training mode reported by CIO. 1 = legacy TB3, 2 = USB4/CIO
    /// native (TB4 and TB5). Same pattern as cableGeneration/generation.
    public let linkTrainingMode: Int?

    public init(
        id: UInt64,
        portKey: String,
        cableGeneration: Int?,
        cableSpeed: Int?,
        generation: Int?,
        asymmetricModeSupported: Bool?,
        legacyAdapter: Bool?,
        linkTrainingMode: Int?
    ) {
        self.id = id
        self.portKey = portKey
        self.cableGeneration = cableGeneration
        self.cableSpeed = cableSpeed
        self.generation = generation
        self.asymmetricModeSupported = asymmetricModeSupported
        self.legacyAdapter = legacyAdapter
        self.linkTrainingMode = linkTrainingMode
    }

    /// Human-readable speed label for a confirmed `cableSpeed` value,
    /// or `nil` when the code is unrecognised.
    ///
    /// Confirmed mappings: 3 = 40 Gbps (TB4, 7 samples), 4 = 80 Gbps
    /// (TB5, 2 samples). Returns `nil` for unknown codes so callers
    /// can fall back to a generic bullet rather than leaking raw IOKit
    /// numbers into user-facing text.
    public static func speedLabel(for cableSpeed: Int) -> String? {
        switch cableSpeed {
        case 3: return String(localized: "40 Gbps capable", bundle: _coreLocalizedBundle)
        default: return nil
        }
    }
}
