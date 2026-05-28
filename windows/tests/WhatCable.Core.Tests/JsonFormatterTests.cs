using Xunit;

namespace WhatCable.Core.Tests;

public sealed class JsonFormatterTests
{
    [Fact]
    public void GoldenFile_RoundTripsMacOsSnapshot_ByteForByte()
    {
        var expected = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "snapshot-golden.json"));
        var snapshot = JsonFormatter.Parse(expected);
        var actual = JsonFormatter.Render(snapshot);
        Assert.Equal(expected.Replace("\r\n", "\n"), actual.Replace("\r\n", "\n"));
    }

    [Fact]
    public void DatabaseLookups_WorkForCuratedFixtures()
    {
        Assert.True(VendorDatabase.IsRegistered(0x01B6));
        Assert.Equal("Apple Inc.", VendorDatabase.Name(0x05AC));
        var cable = CableDatabase.CuratedCable(0x01B6, 0x4003, 0x110A2644);
        Assert.NotNull(cable);
        Assert.Contains("CalDigit", cable!.Brand);
    }

    [Fact]
    public void TrustScorer_DoesNotTreatFixtureVendorListAsAuthoritative()
    {
        Assert.False(VendorDatabase.HasAuthoritativeUsbIfRegistry);
        Assert.DoesNotContain(CableTrustScorer.Evaluate(0x1234, null), flag => flag.Code == "vidNotInUSBIFList");
    }
}
