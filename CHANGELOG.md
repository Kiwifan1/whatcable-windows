# Changelog

All notable changes to WhatCable are documented here. The macOS app and CLI are
versioned independently on the [Releases page](https://github.com/darrylmorley/whatcable/releases);
this file tracks the **Windows port**, which was developed as a stacked series of
eleven pull requests (PRs 1–11) under [`windows/`](windows/).

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the project aims to follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased] — Windows port v1

The first Windows release. A clean-sheet WinUI 3 / C# implementation (not a port of
the macOS Swift code), reusing the data models, decoders, and JSON shape from
`Sources/WhatCableCore` so downstream tooling and the `cables.json` site data keep
working across platforms. Adds a brand-new video-cable diagnostic that has no macOS
equivalent.

### Added

- **Scaffolding (PR 1).** `windows/WhatCable.sln`, project skeletons, the
  `windows-ci.yml` CI workflow (build + test + CLI smoke test on `windows-latest`),
  and the initial README.
- **Core models (PR 2).** Ported the platform-neutral data models, VDO / e-marker
  decoders, cable & vendor database, and `JsonFormatter` to `WhatCable.Core`, with
  xUnit fixtures carried over from `Tests/WhatCableCoreTests`.
- **Video core (PR 3).** `WhatCable.Video.Core`: EDID, CTA-861 (HDMI 2.1 HF-VSDB,
  HDR static metadata, VRR), and DisplayID 2.0 (DP UHBR) parsers, the
  `VideoPortSnapshot` model, the `VideoLinkDiagnostic` engine, and a
  known-video-cables table.
- **USB-C backend, basic (PR 4).** `UsbTopologyAdapter` (SetupAPI device tree with
  negotiated speeds) and `PowerAdapter`, wired into the CLI for a usable preview build.
- **USB-C backend, UCSI (PR 5).** PDO / charger profiles, cable properties, alt-mode,
  Discover Identity / Discover Modes, and the UCSI 2.0 liquid-detection notification.
- **Thunderbolt + video end-to-end (PR 6).** Thunderbolt chain enumeration and the
  video backend (`QueryDisplayConfig` + EDID registry read) wired through to a new
  `video` (and `thunderbolt`) section in the CLI JSON.
- **Vendor GPU adapters (PR 7).** Optional NVAPI / AMD ADL / Intel IGCL adapters,
  gated behind the `EnableVendorGpuAdapters` MSBuild flag (off in CI, on in installer
  builds), to enrich the video diagnostic where a vendor SDK is present.
- **Tray app (PR 8).** WinUI 3 tray app with the port popover and Settings, the MSIX
  manifest, the Inno Setup installer script, launch-at-login (`StartupTask` / per-user
  Run-key fallback), connect/disconnect toast notifications, and localisation
  (`en`, `hy`, `it`, `pl`, `zh-Hans`).
- **Widgets (PR 9).** A Windows 11 Widget Provider COM server with small / medium /
  large Adaptive Card templates, registered for the Widgets Board and (on 23H2+) the
  lock screen. Card rendering and the app↔provider IPC live in the testable
  `WhatCable.Widgets.Core` library.
- **Pro tier (PR 10).** UCSI-derived pin diagrams with per-cable-class SVG,
  a UCSI 2.0 liquid-detection banner + toast, and a system-wide WMI Power Monitor
  graph. These Pro features are **always unlocked for everyone** — there is no
  licence key to enter (`ProEntitlement.IsUnlocked` always returns `true`). Each
  Pro feature reports an `unavailable_reason` when the hardware can't supply the data.
- **Docs, website & release (PR 11).** Full user guide in `windows/README.md`
  (install, usage, troubleshooting, capability matrix), a Windows feature page on
  `whatcable.uk` (download links, capability matrix, FAQ, CLI JSON example), this
  `CHANGELOG.md`, a Windows section in `SECURITY.md`, and a tag-triggered release
  workflow that builds and code-signs the MSIX + Inno installer artefacts.

### Compatibility notes

- `whatcable-cli.exe` emits the same JSON schema as the macOS CLI, with extra `video`
  and `thunderbolt` sections appended. Pass `--legacy` to suppress those for tooling
  that still consumes the macOS schema.
- Per-lane Thunderbolt speeds are intentionally absent — there is no public Windows
  API — and each chain entry documents this with an `unavailable_reason`.
- PD contracts, charger PDOs, cable e-marker decode, pin diagrams, and liquid
  detection require a **UCSI-capable PC**. The USB device list and the video
  diagnostic work without UCSI. See the capability matrix in `windows/README.md`.

[Unreleased]: https://github.com/darrylmorley/whatcable/compare/main...HEAD
