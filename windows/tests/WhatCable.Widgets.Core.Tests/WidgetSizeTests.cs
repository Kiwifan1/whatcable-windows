using Xunit;

namespace WhatCable.Widgets.Core.Tests;

public sealed class WidgetSizeTests
{
    [Theory]
    [InlineData("small", WidgetSize.Small)]
    [InlineData("SMALL", WidgetSize.Small)]
    [InlineData("medium", WidgetSize.Medium)]
    [InlineData("large", WidgetSize.Large)]
    [InlineData("", WidgetSize.Medium)]
    [InlineData(null, WidgetSize.Medium)]
    [InlineData("unknown", WidgetSize.Medium)]
    public void FromHostName_MapsKnownSizes(string? hostName, WidgetSize expected)
    {
        Assert.Equal(expected, WidgetSizes.FromHostName(hostName));
    }

    [Theory]
    [InlineData(WidgetSize.Small, "small")]
    [InlineData(WidgetSize.Medium, "medium")]
    [InlineData(WidgetSize.Large, "large")]
    public void ToHostName_RoundTrips(WidgetSize size, string expected)
    {
        Assert.Equal(expected, WidgetSizes.ToHostName(size));
        Assert.Equal(size, WidgetSizes.FromHostName(expected));
    }
}
