using Application.Common.Interfaces;
using Application.Support.Contracts;
using Application.Support.Features.Queries.GetTicket;
using Application.Support.Features.Shared;
using Domain.Support.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Support.Features.Queries.GetTicket;

public class GetTicketHandlerTests
{
    private readonly ITicketQueryService _ticketQueryService = Substitute.For<ITicketQueryService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetTicketHandler _sut;

    public GetTicketHandlerTests()
    {
        _sut = new GetTicketHandler(_ticketQueryService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenTicketNotFound_ReturnsNotFound()
    {
        _ticketQueryService
            .GetTicketDetailAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns((TicketDto?)null);

        var query = new GetTicketQuery(Guid.NewGuid());

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenNonAdminAndUserIdMismatch_ReturnsForbidden()
    {
        // SEC-22: identity comes from claims; a caller who is neither the owner
        // nor an admin must not read someone else's ticket, no matter what the
        // query payload contains (the payload no longer carries identity at all).
        var ticketOwnerId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();

        var dto = new TicketDto
        {
            Id = ticketId,
            UserId = ticketOwnerId,
            CustomerId = ticketOwnerId
        };

        _ticketQueryService
            .GetTicketDetailAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(dto);
        _currentUserService.IsAdmin.Returns(false);
        _currentUserService.UserId.Returns(requestingUserId);

        var query = new GetTicketQuery(ticketId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
    }

    [Fact]
    public async Task Handle_WhenNonAdminAndUserIdMatches_ReturnsSuccess()
    {
        var requestingUserId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();

        var dto = new TicketDto
        {
            Id = ticketId,
            UserId = requestingUserId,
            CustomerId = requestingUserId
        };

        _ticketQueryService
            .GetTicketDetailAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(dto);
        _currentUserService.IsAdmin.Returns(false);
        _currentUserService.UserId.Returns(requestingUserId);

        var query = new GetTicketQuery(ticketId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Id.ShouldBe(ticketId);
        result.Value.UserId.ShouldBe(requestingUserId);
    }

    [Fact]
    public async Task Handle_WhenAdminAndUserIdMismatch_ReturnsSuccess()
    {
        var ticketOwnerId = Guid.NewGuid();
        var requestingAdminId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();

        var dto = new TicketDto
        {
            Id = ticketId,
            UserId = ticketOwnerId,
            CustomerId = ticketOwnerId
        };

        _ticketQueryService
            .GetTicketDetailAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(dto);
        _currentUserService.IsAdmin.Returns(true);
        _currentUserService.UserId.Returns(requestingAdminId);

        var query = new GetTicketQuery(ticketId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Id.ShouldBe(ticketId);
    }

    [Fact]
    public async Task Handle_WhenCallerHasNoIdentityClaim_ReturnsForbidden()
    {
        var ticketOwnerId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();

        var dto = new TicketDto
        {
            Id = ticketId,
            UserId = ticketOwnerId,
            CustomerId = ticketOwnerId
        };

        _ticketQueryService
            .GetTicketDetailAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns(dto);
        _currentUserService.IsAdmin.Returns(false);
        _currentUserService.UserId.Returns((Guid?)null);

        var query = new GetTicketQuery(ticketId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
    }
}
