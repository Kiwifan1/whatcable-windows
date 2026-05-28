// Widget Provider COM server entry point. Implementation added in PR 9.
namespace WhatCable.Widgets;

internal static class Program
{
    private static int Main(string[] args)
    {
        // PR 9: register IWidgetProvider / IWidgetProvider2 COM factories,
        // process -RegisterProcessAsComServer activation, render Adaptive Cards.
        return 0;
    }
}
