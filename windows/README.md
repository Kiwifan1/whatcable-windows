# WhatCable for Windows

Windows port of [WhatCable](https://whatcable.uk), plus a new **video-cable diagnostic** for HDMI / DisplayPort / USB-C-as-DP outputs.

> **Status:** PR 1 of 11 — scaffolding only. No working functionality yet. See the [PR plan](#pr-plan) below.

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
│   ├── WhatCable.Windows.App/       # WinUI 3 tray app + popover + Settings
│   ├── WhatCable.Widgets/           # Windows 11 Widget Provider COM server
│   └── WhatCable.Cli/               # whatcable-cli.exe — JSON-compatible with the macOS CLI
└── tests/
    ├── WhatCable.Core.Tests/
    └── WhatCable.Video.Core.Tests/
```

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
| 10 | Pro tier: pin diagrams (UCSI), liquid-detection indicator, Power Monitor graph. |
| 11 | Docs + `whatcable.uk` Windows section + screenshots. |

## CLI compatibility

`whatcable-cli.exe` is intended to emit the same JSON schema as `whatcable-cli` on macOS, with a new `video` section appended. A `--legacy` flag will be added in PR 6 to suppress the new section for tooling that hasn't been updated yet.
