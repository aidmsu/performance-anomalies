using Storefront.Infrastructure;

namespace Storefront.Shipping;

public sealed record LabelRequest(string OrderId, string Carrier, string Recipient, decimal WeightKg);

/// <summary>Renders carrier shipping labels (ZPL).</summary>
public sealed class ShippingLabelPrinter
{
    private const double RenderCostMs = 200;
    private const double FieldRenderCostMs = 3;

    private static readonly string[] ExtraFields =
    [
        "customs-tariff-code",
        "sender-phone",
        "return-barcode",
        "package-count",
    ];

    public IReadOnlyList<string> Print(LabelRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Compute.Run("render-label-template", RenderCostMs);
        var lines = new List<string> { "^XA", $"^FD{request.Recipient}^FS", $"^FD{request.Carrier}^FS" };
        foreach (var field in ExtraFields)
        {
            Compute.Run($"render-label-field-{field}", FieldRenderCostMs);
            lines.Add($"^FD{field}^FS");
        }
        lines.Add("^XZ");
        return lines;
    }
}
