using Application.User.Contracts;
using Application.User.Features.Queries.GetUsers;
using Application.User.Features.Shared;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.User.Features.Queries.GetUsers;

public class GetUsersHandlerTests
{
    private readonly IUserQueryService _userQueryService = Substitute.For<IUserQueryService>(); private readonly GetUsersHandler _sut;

    public GetUsersHandlerTests()
    {
        _sut = new GetUsersHandler(_userQueryService);
    }

    [Fact]
    public async Task Handle_WithDefaultQuery_DelegatesToQueryServiceAndReturnsSuccess()
    {
        var page = new PaginatedResult<UserProfileDto>([], 0, 1, 20);
        _userQueryService
            .GetUsersPagedAsync(null, null, null, false, 1, 20, Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _sut.Handle(new GetUsersQuery(false, 1, 20), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(page);
    }

    [Fact]
    public async Task Handle_WithIncludeDeletedAndPagination_PassesParametersToQueryService()
    {
        var query = new GetUsersQuery(true, 3, 5);
        var page = new PaginatedResult<UserProfileDto>([], 0, 3, 5);

        _userQueryService
            .GetUsersPagedAsync(null, null, null, true, 3, 5, Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        await _userQueryService.Received(1).GetUsersPagedAsync(
            null,
            null,
            null,
            true,
            3,
            5,
            Arg.Any<CancellationToken>());
    }
}
