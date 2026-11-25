using Storefront.Infrastructure;

namespace Storefront.Orders;

public sealed record OrderPayload(string OrderId, string View, string Status);

/// <summary>Read side of the public order API.</summary>
public sealed class OrderApi
{
    public string GetSummary(string orderId)
    {
        ArgumentException.ThrowIfNullOrEmpty(orderId);
        SimulatedDatabase.Query("order-api.getsummary", 10);
        return OrderPayloadSerializer.Serialize(new OrderPayload(orderId, "GetSummary", "shipped"));
    }

    public string GetDetails(string orderId)
    {
        ArgumentException.ThrowIfNullOrEmpty(orderId);
        SimulatedDatabase.Query("order-api.getdetails", 25);
        return OrderPayloadSerializer.Serialize(new OrderPayload(orderId, "GetDetails", "shipped"));
    }

    public string GetHistory(string orderId)
    {
        ArgumentException.ThrowIfNullOrEmpty(orderId);
        SimulatedDatabase.Query("order-api.gethistory", 40);
        return OrderPayloadSerializer.Serialize(new OrderPayload(orderId, "GetHistory", "shipped"));
    }

    public string GetExport(string orderId)
    {
        ArgumentException.ThrowIfNullOrEmpty(orderId);
        SimulatedDatabase.Query("order-api.getexport", 80);
        return OrderPayloadSerializer.Serialize(new OrderPayload(orderId, "GetExport", "shipped"));
    }

    public string GetContract(string orderId)
    {
        ArgumentException.ThrowIfNullOrEmpty(orderId);
        SimulatedDatabase.Query("order-api.getcontract", 150);
        return OrderPayloadSerializer.Serialize(new OrderPayload(orderId, "GetContract", "shipped"));
    }

    public string GetBulkContract(string orderId)
    {
        ArgumentException.ThrowIfNullOrEmpty(orderId);
        SimulatedDatabase.Query("order-api.getbulkcontract", 300);
        return OrderPayloadSerializer.Serialize(new OrderPayload(orderId, "GetBulkContract", "shipped"));
    }
}
