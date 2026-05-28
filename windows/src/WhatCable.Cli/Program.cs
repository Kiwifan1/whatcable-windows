// CLI entry point. PR 4 wires up the USB topology + power adapters via the Windows backend.
// PR 5 adds UCSI (PDOs, e-marker); PR 6 adds the Thunderbolt chain and the video section.
// The --legacy flag suppresses the new (non-macOS) sections for tools still consuming the
// macOS schema.
using WhatCable.Windows.Backend;

namespace WhatCable.Cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        string? command = null;
        var legacy = false;
        foreach (var arg in args)
        {
            if (string.Equals(arg, "--legacy", StringComparison.Ordinal))
            {
                legacy = true;
                continue;
            }

            if (!arg.StartsWith('-') && command is null)
            {
                command = arg;
            }
        }

        // --json is the default (and currently only) output format.

        switch (command)
        {
            case "snapshot":
                return RunSnapshot(legacy);
            case null:
            case "-h":
            case "--help":
                PrintUsage();
                return 0;
            default:
                Console.Error.WriteLine($"whatcable-cli: unknown command '{command}'.");
                PrintUsage();
                return 1;
        }
    }

    private static int RunSnapshot(bool legacy)
    {
        WhatCableReport report;
        try
        {
            report = SnapshotBuilder.CreateDefault().BuildReport();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"whatcable-cli: failed to build snapshot: {ex.Message}");
            return 1;
        }

        Console.Out.Write(ReportJsonRenderer.Render(report, legacy));
        return 0;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("Usage: whatcable-cli <command> [options]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Commands:");
        Console.Error.WriteLine("  snapshot    Emit a USB/power/video snapshot as JSON.");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Options:");
        Console.Error.WriteLine("  --json      Output JSON (default).");
        Console.Error.WriteLine("  --legacy    Suppress the video and thunderbolt sections (macOS-compatible schema).");
    }
}
