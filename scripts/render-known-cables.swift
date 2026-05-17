#!/usr/bin/env swift

// Render data/known-cables.md as docs/cables.html.
//
// Reads the markdown source, parses the fingerprint table and the
// "Patterns worth flagging" numbered list, and writes out a static
// HTML page matching the site style (privacy.html / instructions.html).
//
// Run from the repo root:
//   swift scripts/render-known-cables.swift
//
// Exits non-zero if the markdown table is malformed (missing columns,
// no rows, etc) so the build fails loudly rather than emitting a
// broken page.

import Foundation

// MARK: - Paths

let repoRoot = FileManager.default.currentDirectoryPath
let inputURL = URL(fileURLWithPath: "\(repoRoot)/data/known-cables.md")
let outputURL = URL(fileURLWithPath: "\(repoRoot)/docs/cables.html")

guard let markdown = try? String(contentsOf: inputURL, encoding: .utf8) else {
    fputs("error: could not read \(inputURL.path)\n", stderr)
    exit(2)
}

// MARK: - Inline markdown helpers

/// HTML-escape <, >, &, " in a string.
func escapeHTML(_ s: String) -> String {
    var out = s
    out = out.replacingOccurrences(of: "&", with: "&amp;")
    out = out.replacingOccurrences(of: "<", with: "&lt;")
    out = out.replacingOccurrences(of: ">", with: "&gt;")
    out = out.replacingOccurrences(of: "\"", with: "&quot;")
    return out
}

/// Replace all matches of `pattern` in `s` with the NSRegularExpression
/// template `tmpl` ($1 / $2 backreferences).
func regexReplace(_ s: String, pattern: String, with tmpl: String) -> String {
    let re = try! NSRegularExpression(pattern: pattern)
    let range = NSRange(s.startIndex..., in: s)
    return re.stringByReplacingMatches(in: s, range: range, withTemplate: tmpl)
}

/// Apply the small subset of inline markdown the source file uses:
///   `code`, [text](url), **bold**.
/// Order matters: code spans first so their inner content is left alone,
/// then links, then bold.
func renderInline(_ s: String) -> String {
    var text = escapeHTML(s)
    text = regexReplace(text, pattern: "`([^`]+)`",                with: "<code>$1</code>")
    text = regexReplace(text, pattern: "\\[([^\\]]+)\\]\\(([^)]+)\\)", with: "<a href=\"$2\">$1</a>")
    text = regexReplace(text, pattern: "\\*\\*([^*]+)\\*\\*",      with: "<strong>$1</strong>")
    return text
}

/// If `line` looks like "<digits>. <text>", return <text>; else nil.
func numberedListLeader(_ line: String) -> String? {
    let re = try! NSRegularExpression(pattern: "^\\d+\\.\\s+(.*)$")
    let range = NSRange(line.startIndex..., in: line)
    guard let m = re.firstMatch(in: line, range: range), m.numberOfRanges >= 2 else { return nil }
    guard let r = Range(m.range(at: 1), in: line) else { return nil }
    return String(line[r])
}

// MARK: - Table parsing

struct CableRow {
    let cells: [String]
}

let lines = markdown.components(separatedBy: "\n")

// Find the first markdown table after the "## Table" heading. We
// expect the header row, the separator row (|---|---|...), then data
// rows until a non-table line.
var tableStart: Int?
for (i, line) in lines.enumerated() {
    if line.hasPrefix("## Table") {
        // First table line after this heading.
        for j in (i + 1) ..< lines.count where lines[j].hasPrefix("|") {
            tableStart = j
            break
        }
        break
    }
}
guard let headerIdx = tableStart else {
    fputs("error: could not locate the cables table under '## Table'\n", stderr)
    exit(3)
}

func splitRow(_ line: String) -> [String] {
    // Markdown rows look like: | a | b | c |
    // Trim outer pipes, then split on |, then trim each cell.
    var trimmed = line.trimmingCharacters(in: .whitespaces)
    if trimmed.hasPrefix("|") { trimmed.removeFirst() }
    if trimmed.hasSuffix("|") { trimmed.removeLast() }
    return trimmed
        .components(separatedBy: "|")
        .map { $0.trimmingCharacters(in: .whitespaces) }
}

let headerCells = splitRow(lines[headerIdx])
let separatorIdx = headerIdx + 1
guard separatorIdx < lines.count, lines[separatorIdx].contains("---") else {
    fputs("error: expected separator row at line \(separatorIdx + 1)\n", stderr)
    exit(4)
}

var rows: [CableRow] = []
var i = separatorIdx + 1
while i < lines.count, lines[i].hasPrefix("|") {
    let cells = splitRow(lines[i])
    guard cells.count == headerCells.count else {
        fputs("error: row \(i + 1) has \(cells.count) cells, expected \(headerCells.count)\n", stderr)
        exit(5)
    }
    rows.append(CableRow(cells: cells))
    i += 1
}

guard !rows.isEmpty else {
    fputs("error: no data rows found in cables table\n", stderr)
    exit(6)
}

// MARK: - Patterns parsing

// Find the "## Patterns worth flagging" section and grab its numbered
// list items. Each item is multi-line in the markdown; we collect
// lines until the next blank line or top-level heading.
struct Pattern {
    let body: String  // raw markdown; rendered with renderInline
}

var patterns: [Pattern] = []
if let patternsHeader = lines.firstIndex(where: { $0.hasPrefix("## Patterns") }) {
    var current: String? = nil
    for j in (patternsHeader + 1) ..< lines.count {
        let line = lines[j]
        if line.hasPrefix("## ") { break }  // next section
        if let head = numberedListLeader(line) {
            // Flush previous, start new.
            if let prev = current { patterns.append(Pattern(body: prev)) }
            current = head
        } else if line.hasPrefix("   ") {
            // Continuation of current item (markdown soft-wrap).
            let cont = line.trimmingCharacters(in: .whitespaces)
            if !cont.isEmpty {
                current = (current ?? "") + " " + cont
            }
        } else if line.trimmingCharacters(in: .whitespaces).isEmpty {
            if let prev = current {
                patterns.append(Pattern(body: prev))
                current = nil
            }
        }
    }
    if let last = current { patterns.append(Pattern(body: last)) }
}

// MARK: - Build HTML

let dateFormatter = ISO8601DateFormatter()
dateFormatter.formatOptions = [.withFullDate]
let today = dateFormatter.string(from: Date())

let cellClasses: [String] = [
    "context", "vid", "pid", "cable-vdo", "vendor", "xid", "speed", "power", "type", "source",
]

func renderHeaderCell(_ s: String, cls: String) -> String {
    "<th class=\"col-\(cls)\">\(escapeHTML(s))</th>"
}

func renderBodyCell(_ s: String, cls: String) -> String {
    "<td class=\"col-\(cls)\">\(renderInline(s))</td>"
}

let headerHTML = zip(headerCells, cellClasses)
    .map { renderHeaderCell($0.0, cls: $0.1) }
    .joined(separator: "\n            ")

let rowsHTML = rows.map { row in
    let cells = zip(row.cells, cellClasses)
        .map { renderBodyCell($0.0, cls: $0.1) }
        .joined(separator: "\n            ")
    return "          <tr>\n            \(cells)\n          </tr>"
}.joined(separator: "\n")

// The cables page now hardcodes its prose/patterns sections (see the
// template below), so the parsed `## Patterns` list is no longer
// rendered into the page. It is still parsed and reported in the final
// summary line for sanity.

// The JavaScript in this page uses innerHTML to render the table from
// cables.json. All dynamic values pass through esc() (which sets textContent
// on a detached div, then reads innerHTML) before being interpolated. The only
// unescaped fragments are our own hardcoded HTML tags (<code>, <a>, <table>).
// cables.json is generated by build-cable-db.swift from our own database,
// not from user input.

let html = """
<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>USB-C Cable Database: E-Marker Fingerprints | WhatCable</title>
    <meta
      name="description"
      content="A growing database of USB-C cables with real e-marker fingerprints reported by WhatCable users. Search by brand, vendor, speed, or VID to identify what your cable actually is."
    >
    <link rel="icon" href="icon.png" type="image/png">
    <link rel="apple-touch-icon" href="icon.png">
    <link rel="canonical" href="https://www.whatcable.uk/cables">

    <meta property="og:type" content="website">
    <meta property="og:url" content="https://www.whatcable.uk/cables">
    <meta property="og:title" content="WhatCable: Cable database">
    <meta property="og:description" content="A growing database of USB-C cables with real e-marker fingerprints reported by WhatCable users. Search by brand, vendor, speed, or VID to identify what your cable actually is.">
    <meta property="og:image" content="https://www.whatcable.uk/screenshot.png">

    <meta name="twitter:card" content="summary_large_image">
    <meta name="twitter:title" content="WhatCable: Cable database">
    <meta name="twitter:description" content="A growing public database of USB-C cable e-marker fingerprints reported via WhatCable.">
    <meta name="twitter:image" content="https://www.whatcable.uk/screenshot.png">

    <script type="application/ld+json">
    {
      "@context": "https://schema.org",
      "@type": "Dataset",
      "name": "WhatCable cable fingerprint database",
      "description": "Crowd-sourced USB-C cable e-marker fingerprints reported via WhatCable.",
      "url": "https://www.whatcable.uk/cables",
      "isPartOf": {
        "@type": "WebSite",
        "name": "WhatCable",
        "url": "https://www.whatcable.uk"
      }
    }
    </script>

    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&family=JetBrains+Mono:wght@400;500&display=swap" rel="stylesheet">
    <style>
      :root {
        color-scheme: light;
        --ink: #0e1620;
        --ink-soft: #364152;
        --muted: #6a7585;
        --line: #e3e8ef;
        --bg: #fafbfc;
        --panel: #ffffff;
        --accent: #0c8ca6;
      }

      * { box-sizing: border-box; }
      html { scroll-behavior: smooth; }
      body {
        margin: 0;
        background: var(--bg);
        color: var(--ink);
        font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
        font-size: 16px;
        line-height: 1.6;
        -webkit-font-smoothing: antialiased;
      }
      a { color: var(--accent); text-decoration: none; }
      a:hover { text-decoration: underline; }

      .wrap {
        width: min(720px, calc(100% - 40px));
        margin: 0 auto;
      }
      .wrap.wide {
        width: min(960px, calc(100% - 40px));
      }

      .nav {
        position: sticky;
        top: 0;
        z-index: 10;
        background: rgba(250, 251, 252, 0.85);
        backdrop-filter: saturate(180%) blur(12px);
        border-bottom: 1px solid var(--line);
      }
      .nav-inner {
        width: min(1120px, calc(100% - 40px));
        height: 64px;
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 24px;
      }
      .brand {
        display: inline-flex;
        align-items: center;
        gap: 10px;
        font-weight: 700;
        font-size: 16px;
        color: var(--ink);
        text-decoration: none;
      }
      .brand:hover { text-decoration: none; }
      .mark {
        width: 28px;
        height: 28px;
        display: block;
        border-radius: 7px;
        box-shadow: 0 1px 2px rgba(15, 23, 42, 0.12);
      }
      .nav-links {
        display: flex;
        align-items: center;
        gap: 28px;
        font-size: 14px;
        color: var(--ink-soft);
      }
      .nav-links a { color: inherit; text-decoration: none; }
      .nav-links a:hover { color: var(--ink); }
      .nav .button { padding: 8px 14px; min-height: 36px; font-size: 14px; text-decoration: none; display: inline-flex; align-items: center; border-radius: 8px; font-weight: 600; line-height: 1; }
      .nav .button.secondary { background: #fff; color: var(--ink); border: 1px solid var(--line); }
      .nav .button.secondary:hover { border-color: #c8d0db; }
      .nav .button.accent { background: var(--accent); color: #fff; border-color: var(--accent); }
      .nav .button.accent:hover { background: #0a7188; }

      main { padding: 48px 0 80px; }
      h1 {
        font-size: 32px;
        font-weight: 800;
        letter-spacing: -0.01em;
        margin: 0 0 8px;
      }
      .subtitle {
        color: var(--muted);
        font-size: 14px;
        margin: 0 0 28px;
      }
      h2 {
        font-size: 20px;
        font-weight: 700;
        margin: 36px 0 12px;
      }
      p { margin: 0 0 14px; color: var(--ink-soft); }

      .search-bar {
        display: flex;
        align-items: center;
        gap: 12px;
        margin: 0 0 12px;
      }
      .search-bar input {
        flex: 1;
        padding: 10px 14px;
        border: 1px solid var(--line);
        border-radius: 8px;
        font-family: inherit;
        font-size: 14px;
        color: var(--ink);
        background: var(--panel);
        outline: none;
        transition: border-color 0.15s;
      }
      .search-bar input:focus {
        border-color: var(--accent);
      }
      .search-bar input::placeholder {
        color: var(--muted);
      }
      .search-count {
        font-size: 13px;
        color: var(--muted);
        white-space: nowrap;
      }

      .table-wrap {
        margin: 8px 0 28px;
        border: 1px solid var(--line);
        border-radius: 10px;
        background: var(--panel);
        overflow-x: auto;
      }
      table.cables {
        border-collapse: collapse;
        width: 100%;
        font-size: 13.5px;
      }
      table.cables th,
      table.cables td {
        padding: 10px 12px;
        border-bottom: 1px solid var(--line);
        vertical-align: top;
        text-align: left;
        white-space: nowrap;
      }
      table.cables th {
        background: #f4f6f9;
        font-weight: 600;
        font-size: 12px;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        color: var(--ink-soft);
      }
      table.cables tr:last-child td { border-bottom: 0; }
      table.cables td.col-context,
      table.cables th.col-context {
        white-space: normal;
        min-width: 220px;
        color: var(--ink-soft);
      }
      table.cables code {
        font-family: "JetBrains Mono", ui-monospace, SFMono-Regular, Menlo, monospace;
        font-size: 12.5px;
        color: var(--ink);
        background: #f4f6f9;
        padding: 1px 6px;
        border-radius: 4px;
      }
      .no-results {
        padding: 24px 12px;
        text-align: center;
        color: var(--muted);
        font-size: 14px;
      }
      .load-error {
        padding: 16px;
        color: #b91c1c;
        font-size: 14px;
      }

      ol.patterns {
        margin: 0 0 16px;
        padding-left: 22px;
        color: var(--ink-soft);
      }
      ol.patterns li { margin-bottom: 10px; }
      ol.patterns li strong { color: var(--ink); }

      .cta {
        margin: 32px 0 0;
        padding: 16px 18px;
        background: var(--panel);
        border: 1px solid var(--line);
        border-radius: 10px;
        font-size: 14px;
        color: var(--ink-soft);
      }
      .cta strong { color: var(--ink); }

      footer {
        padding: 32px 0;
        background: #0c1219;
        color: rgba(255, 255, 255, 0.55);
        font-size: 13.5px;
        border-top: 1px solid rgba(255, 255, 255, 0.06);
      }
      footer .wrap {
        width: min(1120px, calc(100% - 40px));
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 16px;
      }
      footer a { color: rgba(255, 255, 255, 0.85); }
      footer a:hover { color: #fff; }
      @media (max-width: 600px) {
        .nav-links a:not(.button) { display: none; }
        .nav-links { gap: 12px; }
        .nav .button { padding: 8px 12px; font-size: 13px; }
        h1 { font-size: 28px; }
        .search-input { font-size: 16px; }
        table { font-size: 13px; }
        table th, table td { padding: 10px 12px; }
        footer .wrap { flex-direction: column; align-items: flex-start; }
      }
    </style>
  </head>
  <body>
    <nav class="nav" aria-label="Primary">
      <div class="wrap nav-inner">
        <a class="brand" href="/">
          <img class="mark" src="icon.png" alt="" aria-hidden="true" width="28" height="28">
          <span>WhatCable</span>
        </a>
        <div class="nav-links">
          <a href="/instructions">Docs</a>
          <a href="/cli">CLI</a>
          <a href="/cables">Cables</a>
          <a class="button secondary" href="https://github.com/darrylmorley/whatcable">GitHub</a>
          <a class="button accent" href="/pro">Get Pro</a>
        </div>
      </div>
    </nav>

    <main>
      <div class="wrap wide">
        <h1>USB-C Cable Database</h1>
        <p class="subtitle">Last updated \(today).</p>

        <p>
          Every USB-C cable with an e-marker chip reports an identity when you
          plug it in: vendor ID, speed rating, power limit, cable type. This
          database records what cables actually report, not what the marketing
          claims.
        </p>

        <h2>What this is for</h2>
        <ul>
          <li><strong>Verify a cable before you buy.</strong> Search by brand or vendor to see what cables from that manufacturer actually report.</li>
          <li><strong>Identify a mystery cable.</strong> Got a cable with no markings? Search by VID or speed to find a match.</li>
          <li><strong>Spot suspicious patterns.</strong> Zeroed VIDs, missing XIDs, and speed claims that don't match the e-marker are all red flags.</li>
          <li><strong>Compare what you have.</strong> See how your cable stacks up against others in its class.</li>
        </ul>

        <h2>Patterns we're seeing</h2>
        <p>
          A few reports show patterns that are worth knowing about:
        </p>
        <ol class="patterns">
          <li><strong>Marketing outpaces the e-marker.</strong> #49 (Dbilida) is sold as "Thunderbolt 4 / 40 Gbps / 240 W" but reports passive USB4 Gen 3 with no USB-IF cert. The cable may carry the advertised rate, but nothing in the e-marker backs that claim.</li>
          <li><strong>Unregistered VID, no XID.</strong> #71 (UGOURD, AliExpress) reports 80 Gbps USB4 Gen 4 from an unregistered vendor with zero XID. Plausibly real silicon, but unverifiable from the identity alone.</li>
          <li><strong>Zeroed identity fields.</strong> #61 (CUKTECH No.6) has a present e-marker that reports <code>0x0000</code> for VID, PID, and no speed. This pattern usually means the e-marker was programmed with blank defaults.</li>
        </ol>

        <h2>How to read this table</h2>
        <ul>
          <li><strong>VID / PID</strong> - Vendor ID and Product ID. Assigned by USB-IF. A zeroed VID (<code>0x0000</code>) means the cable didn't identify its manufacturer.</li>
          <li><strong>Cable VDO</strong> - Cable Vendor Defined Object. A bitfield encoding the cable's speed, current rating, and construction type.</li>
          <li><strong>Vendor (USB-IF)</strong> - The registered name for that VID. "Unregistered" means the VID isn't in the USB-IF database.</li>
          <li><strong>XID</strong> - A secondary identifier some vendors use. "none" means the cable didn't report one.</li>
          <li><strong>Speed</strong> - The maximum data rate the e-marker advertises.</li>
          <li><strong>Power</strong> - Maximum current and voltage the cable claims to support.</li>
          <li><strong>Source</strong> - Link to the GitHub issue where this cable was reported.</li>
        </ul>

        <div class="search-bar" id="search-bar" hidden>
          <input type="text" id="search" placeholder="Search cables by brand, vendor, speed, VID..." autocomplete="off">
          <span class="search-count" id="search-count"></span>
        </div>

        <div id="cable-table"></div>

        <noscript>
          <div class="table-wrap">
            <table class="cables">
              <thead>
                <tr>
                  \(headerHTML)
                </tr>
              </thead>
              <tbody>
\(rowsHTML)
              </tbody>
            </table>
          </div>
        </noscript>

        <h2>Add your cable</h2>
        <p>Every report makes the database more useful. Here's how:</p>
        <ol class="patterns">
          <li><strong>Install WhatCable</strong> (free, open source). Download from <a href="/">the homepage</a> or via Homebrew.</li>
          <li><strong>Plug in a cable</strong> and open the menu bar popover.</li>
          <li><strong>Click "Report this cable".</strong> WhatCable opens a pre-filled GitHub issue with the e-marker fields. Review it and submit.</li>
        </ol>
        <p>
          Reports land at
          <a href="https://github.com/darrylmorley/whatcable/issues?q=label%3Acable-report">the cable-report tracker</a>.
          Once triaged, they appear here within a day or two.
        </p>

        <div class="cta" style="display: flex; gap: 12px; flex-wrap: wrap; align-items: center;">
          <a href="/" style="display: inline-flex; align-items: center; padding: 10px 18px; background: var(--accent); color: #fff; border-radius: 8px; font-weight: 600; font-size: 14px; text-decoration: none;">Download WhatCable</a>
          <a href="https://github.com/darrylmorley/whatcable/issues?q=label%3Acable-report" style="display: inline-flex; align-items: center; padding: 10px 18px; background: #fff; color: var(--ink); border: 1px solid var(--line); border-radius: 8px; font-weight: 600; font-size: 14px; text-decoration: none;">Browse all reports on GitHub</a>
        </div>
      </div>
    </main>

    <footer>
      <div class="wrap">
        <span>Built by <a href="https://github.com/darrylmorley">Darryl Morley</a>. MIT licensed.</span>
        <span>
          <a href="/instructions">Docs</a>
          &nbsp;·&nbsp;
          <a href="/cli">CLI</a>
          &nbsp;·&nbsp;
          <a href="/cables">Cables</a>
          &nbsp;·&nbsp;
          <a href="/pro">Pro</a>
          &nbsp;·&nbsp;
          <a href="/support">Support</a>
          &nbsp;·&nbsp;
          <a href="/privacy">Privacy</a>
          &nbsp;·&nbsp;
          <a href="https://github.com/darrylmorley/whatcable">View source on GitHub</a>
        </span>
      </div>
    </footer>

    <script>
    // Table rendering from cables.json. All dynamic values are escaped via
    // esc() (textContent -> innerHTML on a detached element). The source
    // data is our own build-cable-db.swift output, not user input.
    (function () {
      var container = document.getElementById("cable-table");
      var searchBar = document.getElementById("search-bar");
      var searchInput = document.getElementById("search");
      var searchCount = document.getElementById("search-count");
      var allCables = [];

      function esc(s) {
        var d = document.createElement("div");
        d.textContent = s;
        return d.innerHTML;
      }

      function codeCell(val) {
        if (!val) return "";
        return "<code>" + esc(val) + "</code>";
      }

      function renderTable(cables) {
        if (cables.length === 0 && allCables.length > 0) {
          container.innerHTML =
            '<div class="table-wrap"><div class="no-results">No cables match your search.</div></div>';
          searchCount.textContent = "0 of " + allCables.length;
          return;
        }

        var html = '<div class="table-wrap"><table class="cables">';
        html += "<thead><tr>";
        html += '<th class="col-context">Brand / model context</th>';
        html += '<th class="col-vid">VID</th>';
        html += '<th class="col-pid">PID</th>';
        html += '<th class="col-cable-vdo">Cable VDO</th>';
        html += '<th class="col-vendor">Vendor (USB-IF)</th>';
        html += '<th class="col-xid">XID</th>';
        html += '<th class="col-speed">Speed</th>';
        html += '<th class="col-power">Power</th>';
        html += '<th class="col-type">Type</th>';
        html += '<th class="col-source">Source</th>';
        html += "</tr></thead><tbody>";

        for (var i = 0; i < cables.length; i++) {
          var c = cables[i];
          html += "<tr>";
          html += '<td class="col-context">' + esc(c.brand) + "</td>";
          html += '<td class="col-vid">' + codeCell(c.vid) + "</td>";
          html += '<td class="col-pid">' + codeCell(c.pid) + "</td>";
          html += '<td class="col-cable-vdo">' + codeCell(c.cableVDO) + "</td>";
          html += '<td class="col-vendor">' + esc(c.vendor) + "</td>";
          html +=
            '<td class="col-xid">' +
            (c.xid === "none" ? "none" : codeCell(c.xid)) +
            "</td>";
          html += '<td class="col-speed">' + esc(c.speed) + "</td>";
          html += '<td class="col-power">' + esc(c.power) + "</td>";
          html += '<td class="col-type">' + esc(c.type) + "</td>";
          html +=
            '<td class="col-source">' +
            (c.issueURL
              ? '<a href="' + esc(c.issueURL) + '">' + esc(c.issueNum) + "</a>"
              : "") +
            "</td>";
          html += "</tr>";
        }

        html += "</tbody></table></div>";
        container.innerHTML = html;

        if (searchInput.value.trim()) {
          searchCount.textContent = cables.length + " of " + allCables.length;
        } else {
          searchCount.textContent = allCables.length + " cables";
        }
      }

      function filterCables() {
        var q = searchInput.value.trim().toLowerCase();
        if (!q) {
          renderTable(allCables);
          return;
        }
        var terms = q.split(/\\s+/);
        var filtered = allCables.filter(function (c) {
          var haystack = [
            c.brand, c.vid, c.pid, c.cableVDO, c.vendor,
            c.xid, c.speed, c.power, c.type, c.issueNum
          ].join(" ").toLowerCase();
          for (var i = 0; i < terms.length; i++) {
            if (haystack.indexOf(terms[i]) === -1) return false;
          }
          return true;
        });
        renderTable(filtered);
      }

      fetch("cables.json")
        .then(function (res) {
          if (!res.ok) throw new Error("HTTP " + res.status);
          return res.json();
        })
        .then(function (data) {
          allCables = data;
          searchBar.hidden = false;
          renderTable(allCables);
          searchInput.addEventListener("input", filterCables);
        })
        .catch(function (err) {
          container.innerHTML =
            '<div class="table-wrap"><div class="load-error">' +
            "Could not load cable data. " +
            '<a href="cables.json">View raw data</a>.</div></div>';
        });
    })();
    </script>
  </body>
</html>

"""

do {
    try html.write(to: outputURL, atomically: true, encoding: .utf8)
    print("wrote \(outputURL.path) (\(rows.count) rows, \(patterns.count) patterns)")
} catch {
    fputs("error: could not write \(outputURL.path): \(error)\n", stderr)
    exit(7)
}
