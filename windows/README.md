# WhatCable for Windows

Windows port of [WhatCable](https://whatcable.uk), plus a new **video-cable diagnostic** for HDMI / DisplayPort / USB-C-as-DP outputs.

> **Status:** feature-complete for v1 (PRs 1–11). The tray app, CLI, Windows 11 widgets, and Pro tier are wired end-to-end. See the [PR plan](#pr-plan) for the per-PR breakdown and the [capability matrix](#capability-vs-the-macos-app) for what each feature needs from your hardware.

## Contents

- [What you get](#what-you-get)
- [Install](#install)
- [Using the app](#using-the-app)
- [Using the CLI](#using-the-cli)
- [Troubleshooting](#troubleshooting)
- [Why a rewrite (not a port)](#why-a-rewrite-not-a-port)
- [Solution layout](#solution-layout)
- [Build](#build)
- [Target platforms](#target-platforms)
- [Widgets](#widgets)
- [Capability vs. the macOS app](#capability-vs-the-macos-app)
- [PR plan](#pr-plan)
- [CLI compatibility](#cli-compatibility)

## What you get

- **Tray app** — a system-tray icon with a popover that explains every USB-C port in plain English: what's plugged in, the negotiated link speed, the charging state, and (on UCSI PCs) the power contract and cable e-marker.
- **Video-cable diagnostic** (new on Windows) — for each HDMI / DisplayPort / USB-C-DP output it shows the connector, the active mode, the sink's maximum capability, and a "what's limiting the link" verdict derived from EDID / CTA-861 / DisplayID.
- **`whatcable-cli.exe`** — the same diagnostics from your terminal, emitting JSON that is schema-compatible with the macOS CLI (plus `video` and `thunderbolt` sections).
- **Windows 11 widgets** — small / medium / large Adaptive Card widgets for the Widgets Board and the lock screen.
- **Pro tier** — pin diagrams, a liquid-detection indicator, and a live Power Monitor graph, unlocked with an offline licence key. See [Pro](#pro).

How much of this lights up depends on your hardware. The short version: a USB-C **device list with negotiated speeds** and the **video diagnostic** work everywhere; **PD / e-marker / pin-diagram** detail needs a **UCSI-capable PC** (most 2019+ laptops). The full breakdown is in the [capability matrix](#capability-vs-the-macos-app).

## Install

WhatCable for Windows ships two ways. Both are published on the [Releases page](https://github.com/darrylmorley/whatcable/releases/latest).

### Installer (recommended)

Download `WhatCable-Setup-<version>.exe` (an Inno Setup installer) and run it. It installs the tray app to `Program Files`, adds a Start-menu entry, and offers to launch the app when it finishes. Launch-at-login is controlled from inside the app (**Settings → Launch at login**), not by the installer.

To uninstall, use **Settings → Apps** (or **Add or remove programs**) and remove *WhatCable*.

### MSIX package

Download `WhatCable-<version>.msix` if you prefer a packaged install (it carries the Windows 11 widget registration). Double-click it and confirm in the App Installer dialog. The MSIX is code-signed; on a machine that doesn't yet trust the signing certificate, export the signing certificate as a Base-64 X.509 `.cer` from the package's digital-signature details and install it into **Local Machine → Trusted People** first.

> Widgets are a Windows 11 22H2+ feature and are only registered by the MSIX install. The unpackaged installer build provides the tray app and CLI but not the widget provider.

### CLI only

The CLI (`whatcable-cli.exe`) is bundled with both installs. With the installer, it lives next to the app under `Program Files\WhatCable\`; add that folder to your `PATH` (or copy `whatcable-cli.exe` somewhere already on your `PATH`) to call it from any terminal. The MSIX package does not currently declare an App Execution Alias, so if you want to invoke the CLI directly from a terminal, use the installer build.

### Requirements

- Windows 10 22H2 or Windows 11 (widgets need Windows 11 22H2+).
- x64 or ARM64.
- No driver install. PD / e-marker detail additionally needs a UCSI-capable PC (see the [capability matrix](#capability-vs-the-macos-app)).

## Using the app

Click the tray icon to open the popover. Each USB-C port is a row; click it to expand the detail:

- **Charging** — whether the port is charging, and on UCSI PCs the negotiated voltage/current and any bottleneck.
- **Data link** — the negotiated USB / USB4 speed and what (cable, host, or device) is limiting it.
- **Cable** — on UCSI PCs, the decoded e-marker (current rating, max voltage, speed class) and any trust signals.
- **Video** — for display outputs, the connector, active mode, sink maximum, and the "what's limiting the link" verdict.

**Settings** (gear icon) covers launch-at-login, connect/disconnect toast notifications, the polling interval, language, and Pro licence management.

### Pro

Pro adds pin diagrams (UCSI), a liquid-detection indicator (UCSI 2.0), and a system-wide live Power Monitor graph. Unlock it with an offline key in the format `WCPRO-XXXXX-XXXXX-XXXXX` under **Settings → Pro**.

Each Pro feature reports an `unavailable_reason` when the hardware can't supply the data, rather than failing silently.

## Using the CLI

`whatcable-cli.exe` prints a JSON snapshot of every USB-C port, the power adapter, the Thunderbolt chain, and connected displays.

```powershell
# Full snapshot (default)
whatcable-cli snapshot

# macOS-compatible schema only (drops the video + thunderbolt sections)
whatcable-cli snapshot --legacy

# Pipe into jq
whatcable-cli snapshot | jq .ports[0].dataLink
```

The `video` and `thunderbolt` sections are Windows-only additions; pass `--legacy` to suppress them for tooling that still consumes the macOS schema. Per-lane Thunderbolt speeds are intentionally absent (no public Windows API) — each chain entry documents this with an `unavailable_reason`.

A representative `whatcable-cli snapshot` payload (USB-C dock with an SSD downstream, a 4K120 HDMI display, and a Thunderbolt 4 dock, on a non-UCSI PC):

```json
{
  "adapter": {
    "description": "Battery 100% — fully charged (AC connected).",
    "isWireless": false,
    "source": "AC"
  },
  "isDesktopMac": false,
  "ports": [
    {
      "bullets": [],
      "className": "UsbHostController",
      "connectionActive": true,
      "device": {
        "children": [
          {
            "children": [
              {
                "children": [],
                "locationID": "1.1.1",
                "name": "External SSD",
                "productID": 0,
                "speed": "USB 3.2 Gen 2 (10 Gbps)",
                "vendorID": 0
              }
            ],
            "locationID": "1.1",
            "name": "USB4 Dock",
            "productID": 0,
            "speed": "USB 3.2 Gen 2 (10 Gbps)",
            "vendorID": 0
          }
        ],
        "locationID": "0x01000000",
        "name": "USB Host Controller",
        "productID": 0,
        "speed": "",
        "vendorID": 0
      },
      "headline": "USB host controller",
      "name": "USB Host Controller",
      "pdCapable": false,
      "powerSources": [],
      "status": "connected",
      "subtitle": "2 devices",
      "unavailable_reason": "ucsi_not_supported"
    }
  ],
  "thunderbolt": [
    {
      "enumerationIndex": 0,
      "instanceId": "PCI\\TB#0",
      "name": "Thunderbolt 4 Dock",
      "perLaneSpeedsAvailable": false,
      "unavailable_reason": "per_lane_speeds_unavailable_on_windows"
    }
  ],
  "thunderboltSwitches": [],
  "version": "0.26.0",
  "video": [
    {
      "diagnostic": {
        "bottleneck": "Unknown",
        "details": [
          "Active mode: 3840×2160@120Hz",
          "Insufficient information to determine bottleneck."
        ],
        "verdict": "? Unable to determine what's limiting the video link."
      },
      "displayName": "Acme 4K120",
      "port": {
        "activeMode": {
          "heightPx": 2160,
          "interlaced": false,
          "refreshRateHz": 120,
          "widthPx": 3840
        },
        "connectorType": "HDMI"
      }
    }
  ]
}
```

> On a UCSI PC the `ports[].powerSources`, cable e-marker, and `pdCapable: true` fields are populated, and `unavailable_reason` disappears. When the display's EDID can be read, the `video[].port` object gains `edidParsed` and `sinkMaxMode`, and the `diagnostic.bottleneck` becomes a concrete verdict (`None`, `Cable`, `Source`, …) instead of `Unknown`.

## Troubleshooting

**The popover shows ports but no PD / charger / cable detail.**
PD contracts, charger PDOs, and cable e-marker decode come from **UCSI** (`UsbControllerInterface`). If your PC doesn't expose a UCSI controller (common on desktops and pre-2019 laptops), those rows show `ucsi_not_supported`. The USB device list and the video diagnostic still work. This is a hardware/firmware limitation, not a bug.

**The cable e-marker is blank even on a UCSI PC.**
Windows only reads the e-marker when the controller has negotiated above 3 A. A low-wattage charger may never trigger the read. Try a higher-wattage USB-C charger.

**The Thunderbolt chain lists devices but no per-lane speeds.**
There is no public Windows API for per-lane Thunderbolt speeds, so each chain entry carries `unavailable_reason: per_lane_speeds_unavailable_on_windows`. The device chain itself is enumerated via SetupAPI.

**The video diagnostic says "Unable to determine what's limiting the link".**
That means the display's EDID couldn't be read (some virtual displays, KVMs, and capture devices don't expose one). The connector and active mode still show; the bottleneck verdict needs EDID / CTA-861 / DisplayID data.

**Widgets don't appear in the Widgets Board.**
Widgets require Windows 11 22H2+ and the **MSIX** install (they're registered through the package manifest). The unpackaged installer build doesn't include the widget provider.

**Pro features show "unavailable".**
Pin diagrams and liquid detection need UCSI (liquid detection needs UCSI 2.0, which is rare); the per-port Power Monitor is explicitly unavailable on Windows — the graph is system-wide via WMI. Each feature reports its own `unavailable_reason`.

**Filing a bug.**
Attach the output of `whatcable-cli snapshot` (it contains no personal data — just device names, speeds, and IDs) to a [GitHub issue](https://github.com/darrylmorley/whatcable/issues).

## Why a rewrite (not a port)

The macOS app depends on Apple-only IOKit services (`AppleHPMInterface`, `AppleTypeCPhy`, `IOThunderboltLink`, `AppleSmartBattery`) that have no Windows equivalents. The Windows version is a clean-sheet WinUI 3 / C# implementation that reuses the **data models, decoders, and JSON shape** from `Sources/WhatCableCore` so that downstream tooling and the `cables.json` site data keep working across platforms.

## Solution layout

```
windows/
├── WhatCable.sln
├── Directory.Build.props
├── src/
│   ├── WhatCable.Core/              # platform-neutral models, VDO/e-marker decoders, cable & vendor DB
│   ├── WhatCable.Video.Core/        # EDID / CTA-861 / DisplayID parsers + video-link diagnostic
│   ├── WhatCable.Windows.Backend/   # P/Invoke adapters: UCSI, USB, power, TB chain, display, EDID, vendor GPU
│   ├── WhatCable.Windows.App.Core/  # platform-neutral tray/settings ViewModels, settings store, localisation
│   ├── WhatCable.Windows.App/       # WinUI 3 tray app + popover + Settings
│   ├── WhatCable.Widgets.Core/      # testable widget core: Adaptive Card templates + app↔provider IPC
│   ├── WhatCable.Widgets/           # Windows 11 Widget Provider COM server
│   └── WhatCable.Cli/               # whatcable-cli.exe — JSON-compatible with the macOS CLI
└── tests/
    ├── WhatCable.Core.Tests/
    └── WhatCable.Video.Core.Tests/
```

The unpackaged (non-Store) build is produced with the Inno Setup script in `windows/installer/WhatCable.iss` from a self-contained `dotnet publish` of `WhatCable.Windows.App`.

## Build

Prerequisites (Windows 10 22H2 / Windows 11):

- Visual Studio 2022 17.10+ with the **.NET desktop development** and **Windows App SDK C# Templates** workloads, **or**
- .NET 8 SDK + Windows App SDK 1.6 runtime, building from the command line.

```powershell
cd windows
dotnet restore WhatCable.sln
dotnet build WhatCable.sln -c Release
dotnet test  WhatCable.sln -c Release
```

The WinUI 3 app and Widget Provider produce x64 / ARM64 binaries. The CLI and class libraries are AnyCPU.

## Target platforms

| Component | Minimum OS | Architectures |
|---|---|---|
| `WhatCable.Cli` | Windows 10 22H2 | x64, ARM64 |
| `WhatCable.Windows.App` | Windows 10 22H2 | x64, ARM64 |
| `WhatCable.Widgets` | Windows 11 22H2 | x64, ARM64 |

The widget provider is gated to Windows 11 because the Widgets Board / lock-screen widget host doesn't exist on Windows 10.

## Widgets

`WhatCable.Widgets` is an out-of-process **Widget Provider COM server** (`IWidgetProvider`) registered through the app's MSIX manifest (`Package.appxmanifest`: a `com:ExeServer` plus a `com.microsoft.windows.widgets` app extension). It renders three Adaptive Card sizes:

| Size | Content |
|---|---|
| Small | Charging status + active port link speed |
| Medium | Per-port summary table |
| Large | Full snapshot incl. per-port detail and video diagnostic verdicts |

All card rendering and the IPC contract live in the testable `WhatCable.Widgets.Core` library (plain `net8.0`, no Windows App SDK), so the templates are unit-tested off the widget host. The COM activation plumbing stays in the Windows-only `WhatCable.Widgets` executable.

The tray app pushes live state to the provider over a named pipe (`WidgetSnapshotPipe`) on each backend poll, so a pinned widget refreshes within one poll interval (≤5s) of a USB-C or display event. On Windows 11 23H2+, the same size-aware widgets are eligible for the lock screen; users enable them via **Settings → Personalization → Lock screen**.

Free-floating desktop widgets remain a macOS-only feature — Windows has no equivalent API.

## Capability vs. the macOS app

| macOS feature | Windows status in v1 |
|---|---|
| Per-port USB device list with negotiated speed | ✅ Full (SetupAPI) |
| PDO list / charger profiles | ⚠️ UCSI-capable PCs only |
| Cable e-marker VDO decode + trust signals | ⚠️ UCSI-capable PCs only |
| Charging diagnostic banner | ✅ Battery-level banner; ⚠️ full PD bottleneck only on UCSI PCs |
| Data-link diagnostic ("what's limiting the link") | ✅ Full for USB; ✅ Full for Video |
| Thunderbolt fabric topology | ⚠️ Device-chain list only, no per-lane speeds |
| **Video diagnostic (new)**: connector, active mode, sink max, inferred cable class, "what's limiting" verdict | ✅ Full via `QueryDisplayConfig` + EDID/CTA-861/DisplayID; richer with vendor SDK |
| Pin diagrams (Pro) | ⚠️ UCSI PCs only, derived from `GET_ALTERNATE_MODES` |
| Liquid detection (Pro) | ⚠️ UCSI 2.0 PCs only (very limited hardware) |
| Cable resistance estimation (Pro) | ❌ No Windows API |
| Power Monitor live graph (Pro) | ✅ System-wide via WMI; ❌ per-port |
| WidgetKit widgets (small/medium/large) | ⚠️ Windows Widgets Board + lock-screen via Widget Provider API; ❌ free-floating desktop widgets |
| Menu bar / tray app | ✅ Full |
| Launch at login | ✅ `StartupTask` |
| Notifications on connect/disconnect | ✅ Toast notifications |
| Localisation (en / hy / it / pl / zh-Hans) | ✅ `.resw` resources |

## PR plan

| # | Scope |
|---|---|
| 1 | **Scaffolding** (this PR): solution, project skeletons, CI workflow, README. |
| 2 | Port `WhatCableCore` data models, decoders, and `JSONFormatter` to `WhatCable.Core`. xUnit fixtures from `Tests/WhatCableCoreTests`. |
| 3 | `WhatCable.Video.Core` — EDID, CTA-861 (HDMI 2.1 HF-VSDB, HDR static metadata, VRR), DisplayID 2.0 (DP UHBR) parsers, `VideoPortSnapshot`, `VideoLinkDiagnostic`, known-video-cables table. |
| 4 | USB-C backend (basic): `UsbTopologyAdapter` + `PowerAdapter` + CLI integration. Usable preview build. |
| 5 | USB-C backend (UCSI): PDOs, cable property, alt-mode, Discover Identity / Discover Modes, UCSI 2.0 liquid-detection notification. |
| 6 | Thunderbolt chain + Video backend wired end-to-end (`QueryDisplayConfig`, EDID registry read). Video section in CLI JSON. |
| 7 | Optional NVAPI / AMD ADL / Intel IGCL adapters, feature-flagged. |
| 8 | WinUI 3 tray app, Settings, MSIX manifest, Inno Setup installer, launch-at-login, notifications, localisation. |
| 9 | Widget Provider COM server, Adaptive Card templates, Widgets Board + lock-screen registration. |
| 10 | Pro tier: pin diagrams (UCSI), liquid-detection indicator, Power Monitor graph. ✅ Delivered: offline-key license gate (`ProEntitlement`), UCSI-derived pin diagrams with per-cable-class SVG, UCSI 2.0 liquid banner + toast, system-wide WMI Power Monitor (per-port explicitly unavailable). Each feature reports `unavailable_reason` when hardware support is missing; non-Pro users get an upsell affordance. |
| 11 | **Docs + `whatcable.uk` Windows section + screenshots** (this PR). ✅ Delivered: full user guide in this README (install, usage, troubleshooting), a Windows feature page on `whatcable.uk` (download links, capability matrix, FAQ, CLI JSON example), `CHANGELOG.md` covering PRs 1–11, a `SECURITY.md` Windows section, and a tag-triggered release workflow that builds and code-signs the MSIX + Inno installer artefacts. |

## CLI compatibility

`whatcable-cli.exe` emits the same JSON schema as `whatcable-cli` on macOS, with new `video` and `thunderbolt` sections appended. Pass `--legacy` to suppress those sections for tooling that still consumes the macOS schema. Per-lane Thunderbolt speeds are intentionally absent (no public Windows API); each chain entry documents this with `unavailable_reason`.
