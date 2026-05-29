using WhatCable.Windows.App.Core.Settings;
using Xunit;

namespace WhatCable.Windows.App.Core.Tests;

public sealed class ProEntitlementTests
{
    [Fact]
    public void IsUnlocked_NullSettings_ReturnsFalse()
    {
        Assert.False(ProEntitlement.IsUnlocked(null));
    }

    [Fact]
    public void IsUnlocked_NoKey_ReturnsFalse()
    {
        Assert.False(ProEntitlement.IsUnlocked(new AppSettings()));
    }

    [Fact]
    public void IsUnlocked_MalformedKey_ReturnsFalse()
    {
        Assert.False(ProEntitlement.IsUnlocked(new AppSettings { ProLicenseKey = "not-a-key" }));
    }

    [Fact]
    public void IsUnlocked_ValidKey_ReturnsTrue()
    {
        var key = LicenseValidator.Create("ABCDE", "FGHJK", "MNPQ");
        Assert.True(ProEntitlement.IsUnlocked(new AppSettings { ProLicenseKey = key }));
    }
}
