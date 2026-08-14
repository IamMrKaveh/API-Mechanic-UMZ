using Application.Common.Interfaces;
using Application.Wishlist.Contracts;
using Application.Wishlist.Features.Queries.GetWishlistById;
using Application.Wishlist.Features.Shared;
using Domain.User.ValueObjects;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Wishlist.Features.Queries.GetWishlistById;

public class GetWishlistByIdHandlerTests
{
    private readonly IWishlistQueryService _wishlistQueryService = Substitute.For<IWishlistQueryService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly GetWishlistByIdHandler _sut;

    public GetWishlistByIdHandlerTests()
    {
        _sut = new GetWishlistByIdHandler(_wishlistQueryService, _currentUserService);
    }

    private static PaginatedResult<WishlistItemDto> EmptyPage(int page, int pageSize)
        => new(Array.Empty<WishlistItemDto>(), 0, page, pageSize);

    [Fact]
    public async Task Handle_WhenTargetUserIdProvided_UsesTargetUserId()
    {
        var targetUserGuid = Guid.NewGuid();
        var currentUserGuid = Guid.NewGuid();

        _currentUserService.UserId.Returns((Guid?)currentUserGuid);
        _wishlistQueryService
            .GetPagedAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(EmptyPage(2, 5));

        var query = new GetWishlistByIdQuery(targetUserGuid, 2, 5);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _wishlistQueryService.Received(1).GetPagedAsync(
            Arg.Is<UserId>(u => u == UserId.From(targetUserGuid)),
            2,
            5,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTargetUserIdIsNull_FallsBackToCurrentUser()
    {
        var currentUserGuid = Guid.NewGuid();

        _currentUserService.UserId.Returns((Guid?)currentUserGuid);
        _wishlistQueryService
            .GetPagedAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(EmptyPage(1, 10));

        var query = new GetWishlistByIdQuery(1, 10);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _wishlistQueryService.Received(1).GetPagedAsync(
            Arg.Is<UserId>(u => u == UserId.From(currentUserGuid)),
            1,
            10,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenBothTargetAndCurrentUserAreNull_ThrowsInvalidOperationException()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var query = new GetWishlistByIdQuery(1, 10);

        var act = async () => await _sut.Handle(query, CancellationToken.None);

        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData(0, 0, 1, 10)]
    [InlineData(-3, -7, 1, 10)]
    [InlineData(4, 25, 4, 25)]
    public async Task Handle_WhenPagingValuesInvalid_UsesDefaultsOtherwisePassesThrough(
        int inputPage,
        int inputPageSize,
        int expectedPage,
        int expectedPageSize)
    {
        var currentUserGuid = Guid.NewGuid();

        _currentUserService.UserId.Returns((Guid?)currentUserGuid);
        _wishlistQueryService
            .GetPagedAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(EmptyPage(expectedPage, expectedPageSize));

        var query = new GetWishlistByIdQuery(inputPage, inputPageSize);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _wishlistQueryService.Received(1).GetPagedAsync(
            Arg.Is<UserId>(u => u == UserId.From(currentUserGuid)),
            expectedPage,
            expectedPageSize,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsPage_ReturnsThatPageAsValue()
    {
        var currentUserGuid = Guid.NewGuid();
        var items = new List<WishlistItemDto>
    {
        new(Guid.NewGuid(), Guid.NewGuid(), "Product-1", 100m, true, null, DateTime.UtcNow),
        new(Guid.NewGuid(), Guid.NewGuid(), "Product-2", 250m, false, "icon.png", DateTime.UtcNow)
    };
        var page = new PaginatedResult<WishlistItemDto>(items, totalCount: 2, page: 1, pageSize: 10);

        _currentUserService.UserId.Returns((Guid?)currentUserGuid);
        _wishlistQueryService
            .GetPagedAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _sut.Handle(new GetWishlistByIdQuery(1, 10), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(page);
        result.Value.Items.Count.ShouldBe(2);
    }
}
