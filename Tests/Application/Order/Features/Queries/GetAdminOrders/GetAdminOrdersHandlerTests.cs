using Application.Common.Interfaces;
using Application.Order.Contracts;
using Application.Order.Features.Queries.GetAdminOrders;
using Application.Order.Features.Shared;
using Domain.User.ValueObjects;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Order.Features.Queries.GetAdminOrders;

public class GetAdminOrdersHandlerTests
{
    private readonly IOrderQueryService _orderQueryService = Substitute.For<IOrderQueryService>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly GetAdminOrdersHandler _sut;

    public GetAdminOrdersHandlerTests()
    {
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _sut = new GetAdminOrdersHandler(_orderQueryService, _currentUser);
    }

    [Fact]
    public async Task Handle_ForwardsFiltersAndCurrentUserIdToQueryServiceAndReturnsResult()
    {
        var expected = new PaginatedResult<AdminOrderDto> { Items = new List<AdminOrderDto> { new() }, TotalCount = 1, Page = 1, PageSize = 20 };
        UserId? capturedUser = null;
        string? capturedStatus = null;
        _orderQueryService
            .GetAdminOrdersAsync(
                Arg.Do<UserId?>(u => capturedUser = u),
                Arg.Do<string?>(s => capturedStatus = s),
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<bool?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var query = new GetAdminOrdersQuery("Paid", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), true, 1, 20);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
        capturedUser.ShouldNotBeNull();
        capturedStatus.ShouldBe("Paid");
    }
}
