using Application.Auth.Features.Queries.GetCurrentSession;
using Application.Common.Interfaces;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Auth.Features.Queries.GetCurrentSession;

public class GetCurrentSessionHandlerTests
{
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly GetCurrentSessionHandler _sut;

    public GetCurrentSessionHandlerTests()
    {
        _sut = new GetCurrentSessionHandler(_currentUserService);
    }

    [Fact]
    public async Task Handle_WhenUserAuthenticated_ReturnsMappedCurrentSessionDto()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)userId);
        _currentUserService.SessionId.Returns((Guid?)sessionId);
        _currentUserService.IpAddress.Returns("10.0.0.1");
        _currentUserService.UserAgent.Returns("xunit-runner");
        _currentUserService.IsAuthenticated.Returns(true);
        _currentUserService.IsAdmin.Returns(true);

        var result = await _sut.Handle(new GetCurrentSessionQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.UserId.ShouldBe(userId);
        result.Value.SessionId.ShouldBe(sessionId);
        result.Value.IpAddress.ShouldBe("10.0.0.1");
        result.Value.UserAgent.ShouldBe("xunit-runner");
        result.Value.IsAuthenticated.ShouldBeTrue();
        result.Value.IsAdmin.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenUserAnonymous_ReturnsDtoWithNullIdentityFieldsAndFalseFlags()
    {
        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.SessionId.Returns((Guid?)null);
        _currentUserService.IpAddress.Returns((string?)null);
        _currentUserService.UserAgent.Returns((string?)null);
        _currentUserService.IsAuthenticated.Returns(false);
        _currentUserService.IsAdmin.Returns(false);

        var result = await _sut.Handle(new GetCurrentSessionQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.UserId.ShouldBeNull();
        result.Value.SessionId.ShouldBeNull();
        result.Value.IpAddress.ShouldBeNull();
        result.Value.UserAgent.ShouldBeNull();
        result.Value.IsAuthenticated.ShouldBeFalse();
        result.Value.IsAdmin.ShouldBeFalse();
    }
}
