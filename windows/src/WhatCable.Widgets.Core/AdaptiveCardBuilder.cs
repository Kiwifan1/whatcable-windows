using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using WhatCable.Core;

namespace WhatCable.Widgets.Core;

/// <summary>
/// Builds the Adaptive Card JSON the widget provider hands to the Windows 11 Widgets Board for each
/// of the three sizes:
/// <list type="bullet">
///   <item><description><b>Small</b> — charging status + the active port's link speed.</description></item>
///   <item><description><b>Medium</b> — a per-port summary table.</description></item>
///   <item><description><b>Large</b> — the full snapshot plus video diagnostic verdicts.</description></item>
/// </list>
/// The cards are fully rendered here (no host-side templating) so the output is deterministic and
/// unit-testable. The provider sends the result as the widget <c>Template</c> with an empty
/// <c>Data</c> binding.
/// </summary>
public static class AdaptiveCardBuilder
{
    private const string AdaptiveCardSchema = "http://adaptivecards.io/schemas/adaptive-card.json";
    private const string AdaptiveCardVersion = "1.5";

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Builds the Adaptive Card JSON for the given payload and size.</summary>
    public static string Build(WidgetPayload payload, WidgetSize size)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var body = size switch
        {
            WidgetSize.Small => BuildSmall(payload.Snapshot),
            WidgetSize.Large => BuildLarge(payload),
            _ => BuildMedium(payload.Snapshot),
        };

        var card = new JsonObject
        {
            ["type"] = "AdaptiveCard",
            ["$schema"] = AdaptiveCardSchema,
            ["version"] = AdaptiveCardVersion,
            ["body"] = body,
        };

        return card.ToJsonString(WriteOptions);
    }

    private static JsonArray BuildSmall(Snapshot snapshot)
    {
        var speed = WidgetSnapshotSummary.ActivePortSpeed(snapshot);
        return new JsonArray
        {
            Header("WhatCable"),
            TextBlock(WidgetSnapshotSummary.ChargingStatus(snapshot), weight: "Bolder", wrap: true),
            TextBlock(
                string.IsNullOrEmpty(speed) ? "Nothing connected" : speed,
                isSubtle: true,
                wrap: true),
        };
    }

    private static JsonArray BuildMedium(Snapshot snapshot)
    {
        var body = new JsonArray
        {
            Header("WhatCable"),
            TextBlock(WidgetSnapshotSummary.ChargingStatus(snapshot), weight: "Bolder", wrap: true),
        };

        var rows = WidgetSnapshotSummary.PortRows(snapshot);
        if (rows.Count == 0)
        {
            body.Add(TextBlock("No USB-C ports detected", isSubtle: true, wrap: true));
            return body;
        }

        foreach (var row in rows)
        {
            body.Add(PortRowColumns(row));
        }

        return body;
    }

    private static JsonArray BuildLarge(WidgetPayload payload)
    {
        var snapshot = payload.Snapshot;
        var body = new JsonArray
        {
            Header("WhatCable"),
            TextBlock(WidgetSnapshotSummary.ChargingStatus(snapshot), weight: "Bolder", wrap: true),
            TextBlock(
                $"{WidgetSnapshotSummary.ActivePortCount(snapshot)} of {WidgetSnapshotSummary.PortCount(snapshot)} ports active",
                isSubtle: true,
                wrap: true),
        };

        foreach (var port in snapshot.Ports)
        {
            body.Add(PortDetailBlock(port));
        }

        if (payload.VideoVerdicts.Count > 0)
        {
            body.Add(TextBlock("Video diagnostics", weight: "Bolder", spacing: "Medium", wrap: true));
            foreach (var verdict in payload.VideoVerdicts)
            {
                body.Add(PortRowColumns(new PortSummaryRow(
                    Name: verdict.DisplayName,
                    Active: true,
                    Status: verdict.Verdict,
                    Detail: string.Empty)));
            }
        }

        return body;
    }

    private static JsonObject PortDetailBlock(Port port)
    {
        var items = new JsonArray
        {
            TextBlock(port.Name, weight: "Bolder", wrap: true),
            TextBlock(
                port.ConnectionActive
                    ? (string.IsNullOrWhiteSpace(port.Headline) ? "Connected" : port.Headline)
                    : "Nothing connected",
                isSubtle: true,
                wrap: true),
        };

        if (port.ConnectionActive && !string.IsNullOrWhiteSpace(port.Subtitle))
        {
            items.Add(TextBlock(port.Subtitle!, isSubtle: true, wrap: true));
        }

        return new JsonObject
        {
            ["type"] = "Container",
            ["spacing"] = "Small",
            ["items"] = items,
        };
    }

    private static JsonObject PortRowColumns(PortSummaryRow row)
    {
        var statusText = string.IsNullOrEmpty(row.Detail) ? row.Status : $"{row.Status} · {row.Detail}";
        return new JsonObject
        {
            ["type"] = "ColumnSet",
            ["spacing"] = "Small",
            ["columns"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "Column",
                    ["width"] = "auto",
                    ["items"] = new JsonArray { TextBlock(row.Active ? "●" : "○", color: row.Active ? "Good" : "Attention") },
                },
                new JsonObject
                {
                    ["type"] = "Column",
                    ["width"] = "stretch",
                    ["items"] = new JsonArray
                    {
                        TextBlock(row.Name, weight: "Bolder", wrap: true),
                        TextBlock(statusText, isSubtle: true, wrap: true),
                    },
                },
            },
        };
    }

    private static JsonObject Header(string text) => new()
    {
        ["type"] = "TextBlock",
        ["text"] = text,
        ["size"] = "Small",
        ["weight"] = "Bolder",
        ["isSubtle"] = true,
        ["spacing"] = "None",
    };

    private static JsonObject TextBlock(
        string text,
        string? weight = null,
        bool isSubtle = false,
        bool wrap = false,
        string? color = null,
        string? spacing = null)
    {
        var block = new JsonObject
        {
            ["type"] = "TextBlock",
            ["text"] = text,
        };

        if (weight is not null)
        {
            block["weight"] = weight;
        }

        if (isSubtle)
        {
            block["isSubtle"] = true;
        }

        if (wrap)
        {
            block["wrap"] = true;
        }

        if (color is not null)
        {
            block["color"] = color;
        }

        if (spacing is not null)
        {
            block["spacing"] = spacing;
        }

        return block;
    }
}
