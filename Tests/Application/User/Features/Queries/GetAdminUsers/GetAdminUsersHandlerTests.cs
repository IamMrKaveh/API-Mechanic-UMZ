using Application.User.Contracts;
using Application.User.Features.Queries.GetAdminUsers;
using Application.User.Features.Shared;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.User.Features.Queries.GetAdminUsers;

public class GetAdminUsersHandlerTests
{
    private readonly IUserQueryService _userQueryService = Substitute.For<IUserQueryService>(); private readonly GetAdminUsersHandler _sut;

    public GetAdminUsersHandlerTests()
    {
        _sut = new GetAdminUsersHandler(_userQueryService);
    }

    [Fact]
    public async Task Handle_MapsAllQueryFieldsIntoFilterAndReturnsSuccess()
    {
        var createdAfter = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetAdminUsersQuery(
            Search: "ali",
            Role: "admin",
            IsActive: true,
            IsAdmin: true,
            MinTotalSpent: 100m,
            CreatedAfter: createdAfter,
            IncludeDeleted: true,
            Page: 2,
            PageSize: 15);

        var page = new PaginatedResult<AdminUserListItemDto>([], 0, 2, 15);
        _userQueryService
            .GetAdminUsersPagedAsync(Arg.Any<AdminUserFilterParams>(), Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(page);

        await _userQueryService.Received(1).GetAdminUsersPagedAsync(
            Arg.Is<AdminUserFilterParams>(f =>
                f!.Search == "ali" &&
                f.Role == "admin" &&
                f.IsActive == true &&
                f.IsAdmin == true &&
                f.MinTotalSpent == 100m &&
                f.CreatedAfter == createdAfter &&
                f.IncludeDeleted == true &&
                f.Page == 2 &&
                f.PageSize == 15),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDefaultQuery_PassesDefaultFilter()
    {
        var page = new PaginatedResult<AdminUserListItemDto>(new List<AdminUserListItemDto>(), 0, 1, 20);
        _userQueryService
            .GetAdminUsersPagedAsync(Arg.Any<AdminUserFilterParams>(), Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _sut.Handle(new GetAdminUsersQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        await _userQueryService.Received(1).GetAdminUsersPagedAsync(
            Arg.Is<AdminUserFilterParams>(f =>
                f!.Search == null &&
                f.Role == null &&
                f.IsActive == null &&
                f.IsAdmin == null &&
                f.MinTotalSpent == null &&
                f.CreatedAfter == null &&
                f.IncludeDeleted == false &&
                f.Page == 1 &&
                f.PageSize == 20),
            Arg.Any<CancellationToken>());
    }
}
