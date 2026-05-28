using Xunit;

namespace WhatCable.Video.Core.Tests;

public sealed class DisplayIdParserTests
{
    [Fact]
    public void Parse_DisplayId20Header_UsesUpperNibbleForVersion()
    {
        var displayIdBlock = new byte[128];
        displayIdBlock[0] = 0x70;
        displayIdBlock[1] = 0x20;
        displayIdBlock[2] = 0x00;
        displayIdBlock[3] = 0x02;

        var displayId = DisplayIdParser.Parse(displayIdBlock);

        Assert.NotNull(displayId);
        Assert.Equal("2.0", displayId!.Version);
    }

    [Fact]
    public void Parse_DisplayId02Header_IsRejected()
    {
        var displayIdBlock = new byte[128];
        displayIdBlock[0] = 0x70;
        displayIdBlock[1] = 0x02;
        displayIdBlock[2] = 0x00;
        displayIdBlock[3] = 0x02;

        var displayId = DisplayIdParser.Parse(displayIdBlock);

        Assert.Null(displayId);
    }
}
