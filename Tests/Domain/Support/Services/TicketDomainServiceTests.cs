using Domain.Support.Services;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.Domain.Support.Services;

public class TicketDomainServiceTests
{
    private static global::Domain.Support.Aggregates.Ticket NewTicket(UserId customerId) =>
        new TicketBuilder().WithCustomerId(customerId).Build();

    [Fact]
    public void ValidateUserAccess_WhenIsAdmin_ReturnsAllowed()
    {
        var ticket = NewTicket(UserId.NewId());

        var result = TicketDomainService.ValidateUserAccess(ticket, UserId.NewId(), isAdmin: true);

        result.HasAccess.ShouldBeTrue();
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void ValidateUserAccess_WhenCustomer_ReturnsAllowed()
    {
        var customerId = UserId.NewId();
        var ticket = NewTicket(customerId);

        var result = TicketDomainService.ValidateUserAccess(ticket, customerId, isAdmin: false);

        result.HasAccess.ShouldBeTrue();
    }

    [Fact]
    public void ValidateUserAccess_WhenStranger_ReturnsDeniedWithMessage()
    {
        var ticket = NewTicket(UserId.NewId());

        var result = TicketDomainService.ValidateUserAccess(ticket, UserId.NewId(), isAdmin: false);

        result.HasAccess.ShouldBeFalse();
        result.Error.ShouldBe("شما دسترسی به این تیکت را ندارید.");
    }

    [Fact]
    public void ValidateUserAccess_WhenAdminOverridesOwnership_ReturnsAllowed()
    {
        var ticket = NewTicket(UserId.NewId());

        var result = TicketDomainService.ValidateUserAccess(ticket, UserId.NewId(), isAdmin: true);

        result.HasAccess.ShouldBeTrue();
    }
}
