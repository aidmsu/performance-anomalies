using Storefront.Notifications;

namespace Storefront.IntegrationTests.Notifications;

public sealed class NotificationDispatcherTests
{
    [Fact]
    public void Sends_order_confirmation()
    {
        var subject = new NotificationDispatcher().SendOrderConfirmation(new OrderConfirmation("SO-100042", "ada@example.com", "en"));

        Assert.Contains("SO-100042", subject, StringComparison.Ordinal);
    }
}
