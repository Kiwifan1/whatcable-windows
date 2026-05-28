namespace WhatCable.Windows.Backend.Thunderbolt;

/// <summary>
/// Maps an enumerated Thunderbolt/USB4 device list onto the <see cref="ThunderboltDevice"/>
/// chain emitted in the CLI. The list is ordered as enumerated (controllers first), with
/// per-device enumeration indexes (not topological depth), and with
/// per-lane speeds explicitly marked unavailable — see <see cref="ThunderboltDevice"/>.
/// </summary>
public sealed class ThunderboltChainAdapter
{
    /// <summary>Reason emitted because Windows exposes no per-lane Thunderbolt speed API.</summary>
    public const string PerLaneUnavailableReason = "per_lane_speeds_unavailable_on_windows";

    private readonly IThunderboltEnumerator _enumerator;

    public ThunderboltChainAdapter(IThunderboltEnumerator enumerator)
        => _enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));

    public IReadOnlyList<ThunderboltDevice> GetChain()
    {
        var devices = _enumerator.EnumerateChain();
        var chain = new List<ThunderboltDevice>(devices.Count);
        var enumerationIndex = 0;

        foreach (var device in devices)
        {
            chain.Add(new ThunderboltDevice
            {
                Name = device.Name,
                InstanceId = device.InstanceId,
                EnumerationIndex = enumerationIndex,
                PerLaneSpeedsAvailable = false,
                UnavailableReason = PerLaneUnavailableReason,
            });
            enumerationIndex++;
        }

        return chain;
    }
}
