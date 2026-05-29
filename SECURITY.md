# Security Policy

## Reporting a vulnerability

Please report security vulnerabilities **privately**, not in a public issue.

- Use GitHub's [private vulnerability reporting](https://github.com/darrylmorley/whatcable/security/advisories/new)
  ("Report a vulnerability" under the repository's **Security** tab), or
- Email the maintainer at the address listed on the
  [GitHub profile](https://github.com/darrylmorley).

Please include the affected component (macOS app, CLI, Windows app, Windows CLI, or
the website), the version, your platform, and steps to reproduce. We aim to
acknowledge reports within a few days and will keep you updated on the fix and
disclosure timeline.

## Supported versions

Security fixes are applied to the latest released version of each component. Older
releases are not patched; please update to the current release.

## macOS app & CLI

The macOS app and CLI are distributed as a Developer ID-signed and Apple-notarised
build (via Homebrew cask or the GitHub Releases `.zip`). The app uses sandbox-friendly,
read-only IOKit queries and submits nothing to a server. Cable reports are opt-in and
open a pre-filled GitHub issue in your browser that *you* choose to submit.

## Windows app & CLI

The Windows port (`windows/`) is a clean-sheet WinUI 3 / C# application plus a
`whatcable-cli.exe`. Security-relevant properties:

- **Distribution & signing.** Official binaries are published only on the
  [GitHub Releases page](https://github.com/darrylmorley/whatcable/releases). Release
  artefacts (the MSIX package and the Inno Setup installer) are produced by the
  tagged `windows-release` GitHub Actions workflow and **code-signed** with the
  project's certificate when signing secrets are configured. Verify the publisher in
  the file's **Digital Signatures** properties before running, and prefer the MSIX,
  whose signature Windows validates at install time. Do not run builds obtained from
  any other source.
- **No elevation, no driver.** The app and CLI run as the current user. They do not
  install a kernel driver and do not require administrator rights to read USB-C,
  power, Thunderbolt, or display state. The Inno installer requests elevation only to
  write to `Program Files`; the in-app launch-at-login toggle uses a per-user
  `StartupTask` / Run key, never a machine-wide service.
- **Read-only hardware access.** Diagnostics use read-only system APIs (SetupAPI,
  UCSI `IOCTL`s, `QueryDisplayConfig`, the EDID registry, WMI battery counters, and
  optional vendor GPU SDKs). The app does not write to device firmware or change any
  negotiated contract.
- **Optional vendor SDKs.** NVAPI / AMD ADL / Intel IGCL adapters are dynamically
  loaded only when present and only when the build was compiled with
  `EnableVendorGpuAdapters`. CI builds ship with them disabled.
- **No telemetry / network calls.** Neither the app nor the CLI phones home. Pro
  licensing is validated **offline** from a Crockford base-32 key
  (`WCPRO-XXXXX-XXXXX-XXXXX`); there is no licence server. Settings and the licence
  are stored in the user's local profile.
- **CLI output is safe to share.** `whatcable-cli snapshot` JSON contains device
  names, link speeds, and USB vendor/product IDs — no credentials or personal
  documents — so it is safe to attach to a bug report.

If you find a way for any of the above to read or write data it shouldn't, or to
escalate privileges, please report it privately as described above.
