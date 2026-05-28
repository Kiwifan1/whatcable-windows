using System.Linq;
using Xunit;

namespace WhatCable.Video.Core.Tests;

public sealed class KnownVideoCablesDatabaseTests
{
    [Fact]
    public void GetAll_ReturnsNonEmptyList()
    {
        var cables = KnownVideoCablesDatabase.GetAll();

        Assert.NotNull(cables);
        Assert.NotEmpty(cables);
    }

    [Fact]
    public void GetAll_ContainsHdmiUltraHighSpeedCables()
    {
        var cables = KnownVideoCablesDatabase.GetAll();

        var hdmi21Cables = cables.Where(c => c.CableClass == VideoCableClass.HdmiUltraHighSpeed).ToList();
        Assert.NotEmpty(hdmi21Cables);
    }

    [Fact]
    public void GetAll_ContainsDisplayPort20Cables()
    {
        var cables = KnownVideoCablesDatabase.GetAll();

        var dp20Cables = cables.Where(c =>
            c.CableClass == VideoCableClass.DisplayPort20Uhbr20 ||
            c.CableClass == VideoCableClass.DisplayPort20Uhbr135).ToList();
        Assert.NotEmpty(dp20Cables);
    }

    [Fact]
    public void FindByCertificationId_ValidId_ReturnsCable()
    {
        var cable = KnownVideoCablesDatabase.FindByCertificationId("PLACEHOLDER-HDMI-UHS-48G-001");

        Assert.NotNull(cable);
        Assert.Equal("PLACEHOLDER-HDMI-UHS-48G-001", cable.CertificationId);
        Assert.Equal(VideoCableClass.HdmiUltraHighSpeed, cable.CableClass);
    }

    [Fact]
    public void FindByCertificationId_InvalidId_ReturnsNull()
    {
        var cable = KnownVideoCablesDatabase.FindByCertificationId("INVALID-ID-999");

        Assert.Null(cable);
    }

    [Fact]
    public void FindByCertificationId_NullId_ReturnsNull()
    {
        var cable = KnownVideoCablesDatabase.FindByCertificationId(null);

        Assert.Null(cable);
    }

    [Fact]
    public void FindByCertificationId_CaseInsensitive()
    {
        var cable = KnownVideoCablesDatabase.FindByCertificationId("placeholder-hdmi-uhs-48g-001");

        Assert.NotNull(cable);
        Assert.Equal("PLACEHOLDER-HDMI-UHS-48G-001", cable.CertificationId);
    }

    [Fact]
    public void FindByClass_HdmiUltraHighSpeed_ReturnsMultipleCables()
    {
        var cables = KnownVideoCablesDatabase.FindByClass(VideoCableClass.HdmiUltraHighSpeed);

        Assert.NotNull(cables);
        Assert.NotEmpty(cables);
        Assert.All(cables, c => Assert.Equal(VideoCableClass.HdmiUltraHighSpeed, c.CableClass));
    }

    [Fact]
    public void FindByClass_DisplayPort14_ReturnsCables()
    {
        var cables = KnownVideoCablesDatabase.FindByClass(VideoCableClass.DisplayPort14);

        Assert.NotNull(cables);
        Assert.NotEmpty(cables);
    }

    [Fact]
    public void FindByVendor_ValidVendor_ReturnsCables()
    {
        var cables = KnownVideoCablesDatabase.FindByVendor("Cable Matters");

        Assert.NotNull(cables);
        Assert.NotEmpty(cables);
        Assert.All(cables, c => Assert.Contains("Cable Matters", c.Vendor));
    }

    [Fact]
    public void FindByVendor_PartialName_ReturnsCables()
    {
        var cables = KnownVideoCablesDatabase.FindByVendor("Cable");

        Assert.NotNull(cables);
        Assert.NotEmpty(cables);
    }

    [Fact]
    public void FindByVendor_NullVendor_ReturnsEmptyList()
    {
        var cables = KnownVideoCablesDatabase.FindByVendor(null);

        Assert.NotNull(cables);
        Assert.Empty(cables);
    }

    [Fact]
    public void FindByVendor_UnknownVendor_ReturnsEmptyList()
    {
        var cables = KnownVideoCablesDatabase.FindByVendor("NonExistentVendor");

        Assert.NotNull(cables);
        Assert.Empty(cables);
    }

    [Fact]
    public void AllCables_HaveRequiredFields()
    {
        var cables = KnownVideoCablesDatabase.GetAll();

        foreach (var cable in cables)
        {
            Assert.False(string.IsNullOrWhiteSpace(cable.CertificationId));
            Assert.False(string.IsNullOrWhiteSpace(cable.Type));
            Assert.NotEqual(VideoCableClass.Unknown, cable.CableClass);
        }
    }
}
