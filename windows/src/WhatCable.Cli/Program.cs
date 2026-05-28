// CLI entry point. PR 4 wires up the USB topology + power adapters via the Windows backend.
// PR 5 adds UCSI (PDOs, e-marker); PR 6 adds the Thunderbolt chain and the video section
// (the --legacy flag will then suppress that new section).
using WhatCable.Core;
using WhatCable.Windows.Backend;

namespace WhatCable.Cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        string? command = null;
        foreach (var arg in args)
        {
            if (!arg.StartsWith('-') && command is null)
            {
                command = arg;
            }
        }

        // --legacy is accepted but a no-op until PR 6 adds the video section to suppress.
        // --json is the default (and currently only) output format.

        switch (command)
        {
            case "snapshot":
                return RunSnapshot();
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

    private static int RunSnapshot()
    {
        Snapshot snapshot;
        try
        {
            snapshot = SnapshotBuilder.CreateDefault().Build();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"whatcable-cli: failed to build snapshot: {ex.Message}");
            return 1;
        }

        Console.Out.Write(JsonFormatter.Render(snapshot));
        return 0;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("Usage: whatcable-cli <command> [options]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Commands:");
        Console.Error.WriteLine("  snapshot    Emit a USB/power snapshot as JSON.");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Options:");
        Console.Error.WriteLine("  --json      Output JSON (default).");
        Console.Error.WriteLine("  --legacy    Suppress the video section (no-op until PR 6).");
    }
}
