// CLI entry point. Wired up to backend adapters in PR 4 (USB + power),
// PR 5 (UCSI), and PR 6 (TB chain + video). JSON formatter is ported in PR 2.
namespace WhatCable.Cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        System.Console.Error.WriteLine("whatcable-cli: scaffold build. Functionality added in PR 4+.");
        return 0;
    }
}
