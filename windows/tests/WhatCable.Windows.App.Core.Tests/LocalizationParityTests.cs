using System.Xml.Linq;
using WhatCable.Windows.App.Core.Localization;
using Xunit;

namespace WhatCable.Windows.App.Core.Tests;

/// <summary>
/// Guards the acceptance criterion that "all five locales render without missing keys": every
/// shipped <c>.resw</c> must define exactly the keys the app references, with non-empty values.
/// The resources are copied next to the test assembly (see the .csproj).
/// </summary>
public sealed class LocalizationParityTests
{
    private static readonly string[] Locales = { "en", "hy", "it", "pl", "zh-Hans" };

    private static string StringsRoot => Path.Combine(AppContext.BaseDirectory, "Strings");

    private static IReadOnlyDictionary<string, string> LoadLocale(string locale)
    {
        var path = Path.Combine(StringsRoot, locale, "Resources.resw");
        Assert.True(File.Exists(path), $"Missing .resw for locale '{locale}' at {path}");

        return XDocument.Load(path)
            .Root!
            .Elements("data")
            .ToDictionary(
                e => e.Attribute("name")!.Value,
                e => e.Element("value")?.Value ?? string.Empty);
    }

    [Fact]
    public void AllShippedLocalesExist()
    {
        foreach (var locale in Locales)
        {
            Assert.True(Directory.Exists(Path.Combine(StringsRoot, locale)), $"Locale folder '{locale}' is missing.");
        }
    }

    [Theory]
    [InlineData("en")]
    [InlineData("hy")]
    [InlineData("it")]
    [InlineData("pl")]
    [InlineData("zh-Hans")]
    public void LocaleDefinesEveryKeyWithNonEmptyValue(string locale)
    {
        var strings = LoadLocale(locale);

        var missing = StringKeys.All.Where(k => !strings.ContainsKey(k)).ToArray();
        Assert.True(missing.Length == 0, $"Locale '{locale}' is missing keys: {string.Join(", ", missing)}");

        var blank = StringKeys.All.Where(k => string.IsNullOrWhiteSpace(strings[k])).ToArray();
        Assert.True(blank.Length == 0, $"Locale '{locale}' has blank values for: {string.Join(", ", blank)}");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("hy")]
    [InlineData("it")]
    [InlineData("pl")]
    [InlineData("zh-Hans")]
    public void LocaleHasNoExtraOrDuplicateKeys(string locale)
    {
        var path = Path.Combine(StringsRoot, locale, "Resources.resw");
        var names = XDocument.Load(path).Root!.Elements("data").Select(e => e.Attribute("name")!.Value).ToArray();

        Assert.Equal(names.Length, names.Distinct().Count()); // no duplicates

        var extra = names.Where(n => !StringKeys.All.Contains(n)).ToArray();
        Assert.True(extra.Length == 0, $"Locale '{locale}' defines unexpected keys: {string.Join(", ", extra)}");
    }

    [Fact]
    public void AllLocalesShareTheSameKeySet()
    {
        var reference = LoadLocale("en").Keys.OrderBy(k => k).ToArray();

        foreach (var locale in Locales)
        {
            var keys = LoadLocale(locale).Keys.OrderBy(k => k).ToArray();
            Assert.Equal(reference, keys);
        }
    }
}
