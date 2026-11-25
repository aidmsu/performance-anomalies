using Storefront.Support;

namespace Storefront.UnitTests.Support;

public sealed class TicketRouterTests
{
    [Fact]
    public void Routes_ticket_by_topic()
    {
        var result = new TicketRouter().RouteTicketByTopic("sample-42");

        Assert.Equal("ticketrouter.route-ticket-by-topic:sample-42", result);
    }

    [Fact]
    public void Escalates_overdue_ticket()
    {
        var result = new TicketRouter().EscalateOverdueTicket("sample-42");

        Assert.Equal("ticketrouter.escalate-overdue-ticket:sample-42", result);
    }

    [Fact]
    public void Lists_open_tickets()
    {
        var result = new TicketRouter().ListOpenTickets("sample-42");

        Assert.Equal("ticketrouter.list-open-tickets:sample-42", result);
    }

    [Fact]
    public void Assigns_ticket_owner()
    {
        var result = new TicketRouter().AssignTicketOwner("sample-42");

        Assert.Equal("ticketrouter.assign-ticket-owner:sample-42", result);
    }

    [Fact]
    public void Closes_resolved_tickets()
    {
        var result = new TicketRouter().CloseResolvedTickets("sample-42");

        Assert.Equal("ticketrouter.close-resolved-tickets:sample-42", result);
    }
}
