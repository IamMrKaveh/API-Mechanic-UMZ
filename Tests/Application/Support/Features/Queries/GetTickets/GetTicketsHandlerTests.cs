using Application.Common.Interfaces;
using Application.Support.Contracts;
using Application.Support.Features.Queries.GetTickets;
using Application.Support.Features.Shared;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Support.Features.Queries.GetTickets;

public class GetTicketsHandlerTests
{
    private readonly ITicketQueryService _ticketQueryService = Substitute.For<ITicketQueryService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly GetTicketsHandler _sut; private readonly Guid _userGuid = Guid.NewGuid();

    public GetTicketsHandlerTests()
    {
        _currentUserService.UserId.Returns((Guid?)_userGuid);

        _ticketQueryService
            .GetTicketsPagedAsync(
                Arg.Any<UserId?>(),
                Arg.Any<TicketStatus?>(),
                Arg.Any<TicketPriority?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<TicketListItemDto>([], 0, 1, 10));

        _sut = new GetTicketsHandler(_ticketQueryService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenStatusAndPriorityAreNull_PassesNullFilters()
    {
        var query = new GetTicketsQuery(Status: null, Priority: null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _ticketQueryService.Received(1).GetTicketsPagedAsync(
            Arg.Is<UserId?>(id => id != null && id.Value == _userGuid),
            null,
            null,
            1,
            10,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenStatusAndPriorityAreWhitespace_PassesNullFilters(string value)
    {
        var query = new GetTicketsQuery(Status: value, Priority: value);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _ticketQueryService.Received(1).GetTicketsPagedAsync(
            Arg.Any<UserId?>(),
            null,
            null,
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStatusAndPrioritySupplied_ParsesAndPassesThem()
    {
        var query = new GetTicketsQuery(Status: "AwaitingReply", Priority: "Urgent", Page: 3, PageSize: 5);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _ticketQueryService.Received(1).GetTicketsPagedAsync(
            Arg.Any<UserId?>(),
            TicketStatus.AwaitingReply,
            TicketPriority.Urgent,
            3,
            5,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsPagedResult_ForwardsIt()
    {
        var expected = new PaginatedResult<TicketListItemDto>([], 0, 1, 10);
        _ticketQueryService
            .GetTicketsPagedAsync(
                Arg.Any<UserId?>(),
                Arg.Any<TicketStatus?>(),
                Arg.Any<TicketPriority?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var query = new GetTicketsQuery(null, null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
    }
}
