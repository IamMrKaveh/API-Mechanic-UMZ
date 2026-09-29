using Application.Common.Interfaces;
using Application.Order.Contracts;
using Application.Order.Features.Queries.GetUserOrders;
using Application.Order.Features.Shared;
using Domain.User.ValueObjects;
using SharedKernel.Models;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Order.Features.Queries.GetUserOrders;

public class GetUserOrdersHandlerTests : HandlerTestBase
{
    private readonly IOrderQueryService _orderQueryService = Substitute.For<IOrderQueryService>(); private readonly GetUserOrdersHandler _sut;

    public GetUserOrdersHandlerTests()
    {
        _sut = new GetUserOrdersHandler(_orderQueryService, CurrentUserService);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        CurrentUserService.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(new GetUserOrdersQuery(null, 1, 10), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _orderQueryService.DidNotReceiveWithAnyArgs().GetUserOrdersAsync(default!, default, default, default);
    }

    [Fact]
    public async Task Handle_WhenAuthenticated_PassesUserIdPageAndPageSizeToService()
    {
        var userGuid = Guid.NewGuid();
        CurrentUserService.UserId.Returns((Guid?)userGuid);

        var expected = new PaginatedResult<OrderListItemDto> { Items = new List<OrderListItemDto>(), TotalCount = 0, Page = 2, PageSize = 5 };
        UserId? capturedUser = null;
        int capturedPage = 0;
        int capturedPageSize = 0;
        _orderQueryService
            .GetUserOrdersAsync(
                Arg.Do<UserId>(u => capturedUser = u),
                Arg.Do<int>(p => capturedPage = p),
                Arg.Do<int>(ps => capturedPageSize = ps),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetUserOrdersQuery(null, 2, 5), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
        capturedUser!.Value.ShouldBe(userGuid);
        capturedPage.ShouldBe(2);
        capturedPageSize.ShouldBe(5);
    }
}
