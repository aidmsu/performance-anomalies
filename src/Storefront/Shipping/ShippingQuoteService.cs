using Storefront.Infrastructure;

namespace Storefront.Shipping;

public sealed record Parcel(string FromCountry, string ToCountry, decimal WeightKg);

public sealed record ShippingQuote(string Carrier, decimal Price);

/// <summary>Asks the carrier for a rate. Carrier response times vary a lot from call to call.</summary>
public sealed class ShippingQuoteService
{
    private const double FastestResponseMs = 55;
    private const double ResponseSpreadMs = 50;

    public ShippingQuote Quote(Parcel parcel)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        var responseTime = FastestResponseMs + Workload.Random("ShippingQuoteService.Quote").NextDouble() * ResponseSpreadMs;
        SimulatedNetwork.Call("carrier-rates", responseTime);
        var price = 4.90m + Math.Ceiling(parcel.WeightKg) * 1.20m;
        return new ShippingQuote(parcel.FromCountry == parcel.ToCountry ? "national-post" : "express", price);
    }
}
