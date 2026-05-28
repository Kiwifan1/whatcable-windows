using System.Globalization;

namespace WhatCable.Windows.App.Core.Settings;

/// <summary>
/// Offline format/checksum validation for Pro license keys. This is a structural check only —
/// it does not perform any network activation — but it lets the Settings window give immediate
/// feedback and reject obviously malformed keys.
/// </summary>
/// <remarks>
/// A key looks like <c>WCPRO-ABCDE-FGHJK-MNPQR</c>: a fixed <c>WCPRO</c> prefix followed by three
/// 5-character groups drawn from Crockford base-32 (no I, L, O, U). The final character of the
/// last group is a checksum over every preceding alphanumeric character.
/// </remarks>
public static class LicenseValidator
{
    private const string Prefix = "WCPRO";
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"; // Crockford base-32

    public static bool IsValid(string? licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            return false;
        }

        var normalized = Normalize(licenseKey);
        var groups = normalized.Split('-');
        if (groups.Length != 4 || groups[0] != Prefix)
        {
            return false;
        }

        for (var i = 1; i < groups.Length; i++)
        {
            if (groups[i].Length != 5 || groups[i].Any(c => !Alphabet.Contains(c)))
            {
                return false;
            }
        }

        var payload = string.Concat(groups[1], groups[2], groups[3][..4]);
        return ComputeChecksum(payload) == groups[3][4];
    }

    /// <summary>Upper-cases and trims a key so case/whitespace differences don't matter.</summary>
    public static string Normalize(string licenseKey)
        => licenseKey.Trim().ToUpperInvariant();

    private static char ComputeChecksum(string payload)
    {
        var sum = 0;
        foreach (var c in payload)
        {
            sum += Alphabet.IndexOf(c) + 1;
        }

        return Alphabet[sum % Alphabet.Length];
    }

    /// <summary>
    /// Test/support helper that builds a well-formed key from two 5-char groups by appending the
    /// correct checksum character, so fixtures don't hard-code magic strings.
    /// </summary>
    public static string Create(string group1, string group2, string group3FirstFour)
    {
        ArgumentNullException.ThrowIfNull(group1);
        ArgumentNullException.ThrowIfNull(group2);
        ArgumentNullException.ThrowIfNull(group3FirstFour);

        var g1 = group1.ToUpperInvariant();
        var g2 = group2.ToUpperInvariant();
        var g3 = group3FirstFour.ToUpperInvariant();
        if (g1.Length != 5 || g2.Length != 5 || g3.Length != 4)
        {
            throw new ArgumentException("Groups must be 5/5/4 characters.", nameof(group3FirstFour));
        }

        var payload = string.Concat(g1, g2, g3);
        var checksum = ComputeChecksum(payload);
        return string.Create(CultureInfo.InvariantCulture, $"{Prefix}-{g1}-{g2}-{g3}{checksum}");
    }
}
