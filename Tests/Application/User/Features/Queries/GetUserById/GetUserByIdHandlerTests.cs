using Application.User.Contracts;
using Application.Common.Interfaces;
using Application.User.Features.Queries.GetUserById;
using Application.User.Features.Shared;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.User.Features.Queries.GetUserById;

public class GetUserByIdHandlerTests
{
    private readonly IUserQueryService _userQueryService = Substitute.For<IUserQueryService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetUserByIdHandler _sut;

    public GetUserByIdHandlerTests()
    {
        _currentUserService.IsAdmin.Returns(true);
        _sut = new GetUserByIdHandler(_userQueryService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenUserFound_ReturnsSuccessWithDto()
    {
        var userGuid = Guid.NewGuid();
        var expected = new UserProfileDto { Id = userGuid };

        _userQueryService
            .GetUserProfileAsync(Arg.Is<UserId>(x => x == UserId.From(userGuid)), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetUserByIdQuery(userGuid), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotOwnerAndNotAdmin_ReturnsForbidden()
    {
        _currentUserService.IsAdmin.Returns(false);
        _currentUserService.UserId.Returns(Guid.NewGuid());

        var result = await _sut.Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
        await _userQueryService.DidNotReceiveWithAnyArgs().GetUserProfileAsync(default!);
    }

    [Fact]
    public async Task Handle_WhenCallerIsOwner_ReturnsSuccess()
    {
        _currentUserService.IsAdmin.Returns(false);
        var userGuid = Guid.NewGuid();
        _currentUserService.UserId.Returns(userGuid);
        var expected = new UserProfileDto { Id = userGuid };
        _userQueryService
            .GetUserProfileAsync(Arg.Is<UserId>(x => x == UserId.From(userGuid)), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetUserByIdQuery(userGuid), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        _userQueryService
            .GetUserProfileAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((UserProfileDto?)null);

        var result = await _sut.Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }
}
