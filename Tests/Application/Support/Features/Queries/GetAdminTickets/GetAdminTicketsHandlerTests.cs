using Application.Support.Contracts;
using Application.Support.Features.Queries.GetAdminTickets;
using Application.Support.Features.Shared;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Support.Features.Queries.GetAdminTickets;

public class GetAdminTicketsHandlerTests
{
    private readonly ITicketQueryService _ticketQueryService = Substitute.For<ITicketQueryService>(); private readonly GetAdminTicketsHandler _sut;

    public GetAdminTicketsHandlerTests()
    {
        _sut = new GetAdminTicketsHandler(_ticketQueryService);

        _ticketQueryService
            .GetAdminTicketsPagedAsync(
                Arg.Any<TicketStatus>(),
                Arg.Any<TicketPriority>(),
                Arg.Any<UserId?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<TicketDto>([], 0, 1, 10));
    }

    [Fact]
    public async Task Handle_WhenStatusAndPriorityAreNull_UsesDefaults()
    {
        var query = new GetAdminTicketsQuery(Status: null, Priority: null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _ticketQueryService.Received(1).GetAdminTicketsPagedAsync(
            TicketStatus.Open,
            TicketPriority.Normal,
            Arg.Any<UserId?>(),
            1,
            10,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenStatusAndPriorityAreWhitespace_UsesDefaults(string value)
    {
        var query = new GetAdminTicketsQuery(Status: value, Priority: value);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _ticketQueryService.Received(1).GetAdminTicketsPagedAsync(
            TicketStatus.Open,
            TicketPriority.Normal,
            Arg.Any<UserId?>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStatusAndPrioritySupplied_ParsesAndPassesThem()
    {
        var query = new GetAdminTicketsQuery(Status: "Closed", Priority: "High", Page: 2, PageSize: 25);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _ticketQueryService.Received(1).GetAdminTicketsPagedAsync(
            TicketStatus.Closed,
            TicketPriority.High,
            Arg.Any<UserId?>(),
            2,
            25,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsPagedResult_ForwardsIt()
    {
        var expected = new PaginatedResult<TicketDto>([new TicketDto { Id = Guid.NewGuid() }], 1, 1, 10);
        _ticketQueryService
            .GetAdminTicketsPagedAsync(
                Arg.Any<TicketStatus>(),
                Arg.Any<TicketPriority>(),
                Arg.Any<UserId?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var query = new GetAdminTicketsQuery(Status: "Open", Priority: "Normal");

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
    }
}
