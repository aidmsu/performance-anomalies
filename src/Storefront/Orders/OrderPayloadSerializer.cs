using System.Text.Json;
using Storefront.Infrastructure;

namespace Storefront.Orders;

/// <summary>Serializes every payload returned by the order API.</summary>
public static class OrderPayloadSerializer
{
    private const double SchemaValidationCostMs = 60;

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize<T>(T payload)
    {
        var json = JsonSerializer.Serialize(payload, Options);
        Compute.Run("validate-order-schema", SchemaValidationCostMs);
        return json;
    }
}
