using Application.Common.Interfaces;
using Application.Support.Contracts;
using Application.Support.Features.Queries.GetTicketDetails;
using Application.Support.Features.Shared;
using Domain.Support.Aggregates;
using Domain.Support.Interfaces;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Support.Features.Queries.GetTicketDetails;

public class GetTicketDetailsHandlerTests : HandlerTestBase
{
    private readonly ITicketRepository _ticketRepository = Substitute.For<ITicketRepository>(); private readonly ITicketQueryService _ticketQueryService = Substitute.For<ITicketQueryService>(); private readonly GetTicketDetailsHandler _sut; private readonly Guid _userGuid = Guid.NewGuid();

    public GetTicketDetailsHandlerTests()
    {
        CurrentUserService.UserId.Returns((Guid?)_userGuid);
        _sut = new GetTicketDetailsHandler(_ticketRepository, _ticketQueryService, CurrentUserService);
    }

    [Fact]
    public async Task Handle_WhenTicketNotFound_ReturnsNotFound()
    {
        _ticketRepository
            .GetByIdWithMessagesAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns((Ticket?)null);

        var query = new GetTicketDetailsQuery(Guid.NewGuid());

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        await _ticketQueryService.DidNotReceive().GetTicketDetailAsync(
            Arg.Any<TicketId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNonAdminAndUserHasNoAccess_ReturnsForbidden()
    {
        var otherOwnerId = UserId.NewId();
        var ticket = new TicketBuilder().WithCustomerId(otherOwnerId).Build();

        _ticketRepository
            .GetByIdWithMessagesAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(ticket);

        var query = new GetTicketDetailsQuery(ticket.Id.Value);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
        await _ticketQueryService.DidNotReceive().GetTicketDetailAsync(
            Arg.Any<TicketId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAccessAllowedButDtoNotFound_ReturnsNotFound()
    {
        var customerId = UserId.From(_userGuid);
        var ticket = new TicketBuilder().WithCustomerId(customerId).Build();

        _ticketRepository
            .GetByIdWithMessagesAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(ticket);
        _ticketQueryService
            .GetTicketDetailAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns((TicketDto?)null);

        var query = new GetTicketDetailsQuery(ticket.Id.Value);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenCustomerHasAccessAndDtoExists_ReturnsSuccess()
    {
        var customerId = UserId.From(_userGuid);
        var ticket = new TicketBuilder().WithCustomerId(customerId).Build();
        var dto = new TicketDto { Id = ticket.Id.Value, UserId = _userGuid };

        _ticketRepository
            .GetByIdWithMessagesAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(ticket);
        _ticketQueryService
            .GetTicketDetailAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(dto);

        var query = new GetTicketDetailsQuery(ticket.Id.Value);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task Handle_WhenAdminPerClaimsAndDtoExists_ReturnsSuccessRegardlessOfOwnership()
    {
        // SEC-22: the admin role comes from the authenticated principal.
        CurrentUserService.IsAdmin.Returns(true);
        var otherOwnerId = UserId.NewId();
        var ticket = new TicketBuilder().WithCustomerId(otherOwnerId).Build();
        var dto = new TicketDto { Id = ticket.Id.Value, UserId = otherOwnerId.Value };

        _ticketRepository
            .GetByIdWithMessagesAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(ticket);
        _ticketQueryService
            .GetTicketDetailAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(dto);

        var query = new GetTicketDetailsQuery(ticket.Id.Value);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task Handle_WhenNonAdminPerClaimsAndNotOwner_ReturnsForbidden()
    {
        // SEC-22: no payload flag can elevate the caller to admin.
        CurrentUserService.IsAdmin.Returns(false);
        var otherOwnerId = UserId.NewId();
        var ticket = new TicketBuilder().WithCustomerId(otherOwnerId).Build();

        _ticketRepository
            .GetByIdWithMessagesAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(ticket);

        var result = await _sut.Handle(new GetTicketDetailsQuery(ticket.Id.Value), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
    }

    [Fact]
    public void Query_DoesNotCarryAnyRoleOrUserFlags()
    {
        // Invariant: authorization data must never travel in the query payload.
        var properties = typeof(GetTicketDetailsQuery)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        properties.ShouldNotContain("IsAdmin");
        properties.ShouldNotContain("RequestingUserId");
        properties.ShouldNotContain("UserId");
    }
}
