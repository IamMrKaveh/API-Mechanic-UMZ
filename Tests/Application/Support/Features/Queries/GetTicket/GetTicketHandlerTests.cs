using Application.Support.Contracts;
using Application.Support.Features.Queries.GetTicket;
using Application.Support.Features.Shared;
using Domain.Support.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Support.Features.Queries.GetTicket;

public class GetTicketHandlerTests
{
    private readonly ITicketQueryService _ticketQueryService = Substitute.For<ITicketQueryService>(); private readonly GetTicketHandler _sut;

    public GetTicketHandlerTests()
    {
        _sut = new GetTicketHandler(_ticketQueryService);
    }

    [Fact]
    public async Task Handle_WhenTicketNotFound_ReturnsNotFound()
    {
        _ticketQueryService
            .GetTicketDetailAsync(Arg.Any<TicketId>(), Arg.Any<CancellationToken>())
            .Returns((TicketDto?)null);

        var query = new GetTicketQuery(Guid.NewGuid(), Guid.NewGuid(), IsAdmin: false);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenNonAdminAndUserIdMismatch_ReturnsForbidden()
    {
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

        var query = new GetTicketQuery(ticketId, requestingUserId, IsAdmin: false);

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

        var query = new GetTicketQuery(ticketId, requestingUserId, IsAdmin: false);

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

        var query = new GetTicketQuery(ticketId, requestingAdminId, IsAdmin: true);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Id.ShouldBe(ticketId);
    }
}
