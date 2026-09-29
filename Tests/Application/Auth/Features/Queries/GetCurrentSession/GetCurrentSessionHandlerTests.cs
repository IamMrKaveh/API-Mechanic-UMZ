using Application.Auth.Features.Queries.GetCurrentSession;
using Application.Common.Interfaces;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Auth.Features.Queries.GetCurrentSession;

public class GetCurrentSessionHandlerTests : HandlerTestBase
{
    private readonly GetCurrentSessionHandler _sut;

    public GetCurrentSessionHandlerTests()
    {
        _sut = new GetCurrentSessionHandler(CurrentUserService);
    }

    [Fact]
    public async Task Handle_WhenUserAuthenticated_ReturnsMappedCurrentSessionDto()
    {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        CurrentUserService.UserId.Returns((Guid?)userId);
        CurrentUserService.SessionId.Returns((Guid?)sessionId);
        CurrentUserService.IpAddress.Returns("10.0.0.1");
        CurrentUserService.UserAgent.Returns("xunit-runner");
        CurrentUserService.IsAuthenticated.Returns(true);
        CurrentUserService.IsAdmin.Returns(true);

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
        CurrentUserService.UserId.Returns((Guid?)null);
        CurrentUserService.SessionId.Returns((Guid?)null);
        CurrentUserService.IpAddress.Returns((string?)null);
        CurrentUserService.UserAgent.Returns((string?)null);
        CurrentUserService.IsAuthenticated.Returns(false);
        CurrentUserService.IsAdmin.Returns(false);

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
