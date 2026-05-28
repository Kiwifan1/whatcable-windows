using WhatCable.Windows.App.Core.Settings;
using Xunit;

namespace WhatCable.Windows.App.Core.Tests;

public sealed class LicenseValidatorTests
{
    [Fact]
    public void Create_ProducesKeyThatValidates()
    {
        var key = LicenseValidator.Create("ABCDE", "FGHJK", "MNPQ");

        Assert.True(LicenseValidator.IsValid(key));
        Assert.StartsWith("WCPRO-", key);
    }

    [Fact]
    public void IsValid_IsCaseAndWhitespaceInsensitive()
    {
        var key = LicenseValidator.Create("ABCDE", "FGHJK", "MNPQ");

        Assert.True(LicenseValidator.IsValid("  " + key.ToLowerInvariant() + "  "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-key")]
    [InlineData("WCPRO-ABCDE-FGHJK")] // too few groups
    [InlineData("WRONG-ABCDE-FGHJK-MNPQR")] // bad prefix
    [InlineData("WCPRO-ABCDE-FGHJK-MNPQI")] // contains excluded letter I
    public void IsValid_RejectsMalformedKeys(string? key)
    {
        Assert.False(LicenseValidator.IsValid(key));
    }

    [Fact]
    public void IsValid_RejectsTamperedChecksum()
    {
        var key = LicenseValidator.Create("ABCDE", "FGHJK", "MNPQ");
        // Flip the final checksum character to a different valid-alphabet char.
        var tampered = key[..^1] + (key[^1] == '0' ? '1' : '0');

        Assert.False(LicenseValidator.IsValid(tampered));
    }
}
