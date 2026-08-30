using Application.Auth.Contracts;
using Application.Auth.Features.Commands.GoogleLogin;
using Application.Auth.Features.Shared;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using RefreshTokens = Domain.Security.ValueObjects.RefreshToken;
using Users = Domain.User.Aggregates.User;

namespace Tests.Application.Auth.Features.Commands.GoogleLogin;

public class GoogleLoginHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly ISessionService _sessionService = Substitute.For<ISessionService>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly GoogleLoginHandler _sut;

    public GoogleLoginHandlerTests()
    {
        _sut = new GoogleLoginHandler(_userRepository, _jwtTokenGenerator, _sessionService, _currentUser);
    }

    private static RefreshTokenResult BuildSuccessfulSessionResult(Guid userId) =>
        new(Guid.NewGuid(), RefreshTokens.Generate().Value, DateTime.UtcNow.AddDays(30), userId);

    private void SetupSuccessfulSession(Guid userId)
    {
        _sessionService
            .CreateSessionAsync(Arg.Any<UserId>(), Arg.Any<IpAddress>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RefreshTokenResult>.Success(BuildSuccessfulSessionResult(userId)));

        _jwtTokenGenerator
            .GenerateAccessToken(Arg.Any<Users>(), Arg.Any<SessionId>())
            .Returns("generated-access-token");
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_CreatesNewUserWithProvidedProfileAndAddsToRepository()
    {
        _userRepository
            .GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns((Users?)null);

        SetupSuccessfulSession(Guid.NewGuid());

        var command = new GoogleLoginCommand("newuser@example.com", "Ali", "Rezaei", "google-oauth2|12345");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        await _userRepository.Received(1).AddAsync(
            Arg.Is<Users>(u =>
                u!.Email == Email.Create("newuser@example.com") &&
                u.FullName.FirstName == "Ali" &&
                u.FullName.LastName == "Rezaei" &&
                u.PasswordHash == string.Empty &&
                u.PhoneNumber == null &&
                u.IsActive),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserExists_DoesNotCreateNewUser()
    {
        var existingUser = new UserBuilder().WithEmail("existing@example.com").Build();

        _userRepository
            .GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(existingUser);

        SetupSuccessfulSession(existingUser.Id.Value);

        var command = new GoogleLoginCommand("existing@example.com", "Sara", "Ahmadi", "google-oauth2|67890");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        await _userRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenIpAddressIsAvailable_UsesProvidedIpForSessionCreation()
    {
        var existingUser = new UserBuilder().WithEmail("ip-user@example.com").Build();

        _userRepository
            .GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(existingUser);

        _currentUser.IpAddress.Returns("192.168.1.10");

        SetupSuccessfulSession(existingUser.Id.Value);

        var command = new GoogleLoginCommand("ip-user@example.com", "Reza", "Karimi", "google-oauth2|1");

        await _sut.Handle(command, CancellationToken.None);

        await _sessionService.Received(1).CreateSessionAsync(
            Arg.Any<UserId>(),
            Arg.Is<IpAddress>(ip => ip == IpAddress.Create("192.168.1.10")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenIpAddressIsMissing_UsesUnknownIpForSessionCreation()
    {
        var existingUser = new UserBuilder().WithEmail("no-ip@example.com").Build();

        _userRepository
            .GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(existingUser);

        _currentUser.IpAddress.Returns((string?)null);

        SetupSuccessfulSession(existingUser.Id.Value);

        var command = new GoogleLoginCommand("no-ip@example.com", "Mina", "Sadeghi", "google-oauth2|2");

        await _sut.Handle(command, CancellationToken.None);

        await _sessionService.Received(1).CreateSessionAsync(
            Arg.Any<UserId>(),
            Arg.Is<IpAddress>(ip => ip == IpAddress.Unknown),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesUserAgentToSessionService()
    {
        var existingUser = new UserBuilder().WithEmail("agent-user@example.com").Build();

        _userRepository
            .GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(existingUser);

        _currentUser.UserAgent.Returns("Mozilla/5.0 TestAgent");

        SetupSuccessfulSession(existingUser.Id.Value);

        var command = new GoogleLoginCommand("agent-user@example.com", "Kian", "Moradi", "google-oauth2|3");

        await _sut.Handle(command, CancellationToken.None);

        await _sessionService.Received(1).CreateSessionAsync(
            Arg.Any<UserId>(),
            Arg.Any<IpAddress>(),
            "Mozilla/5.0 TestAgent",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSessionCreationFails_ReturnsFailureWithSameErrorAndDoesNotGenerateAccessToken()
    {
        var existingUser = new UserBuilder().WithEmail("fail-session@example.com").Build();

        _userRepository
            .GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(existingUser);

        _sessionService
            .CreateSessionAsync(Arg.Any<UserId>(), Arg.Any<IpAddress>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RefreshTokenResult>.Unauthorized("جلسه ایجاد نشد."));

        var command = new GoogleLoginCommand("fail-session@example.com", "Nima", "Hosseini", "google-oauth2|4");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
        _jwtTokenGenerator.DidNotReceiveWithAnyArgs().GenerateAccessToken(default!, default!);
    }

    [Fact]
    public async Task Handle_WhenSessionCreationSucceeds_GeneratesAccessTokenWithUserAndSessionIdFromSession()
    {
        var existingUser = new UserBuilder().WithEmail("token-user@example.com").Build();

        _userRepository
            .GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var sessionResult = BuildSuccessfulSessionResult(existingUser.Id.Value);

        _sessionService
            .CreateSessionAsync(Arg.Any<UserId>(), Arg.Any<IpAddress>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RefreshTokenResult>.Success(sessionResult));

        _jwtTokenGenerator
            .GenerateAccessToken(Arg.Any<Users>(), Arg.Any<SessionId>())
            .Returns("access-token-value");

        var command = new GoogleLoginCommand("token-user@example.com", "Yasmin", "Ghasemi", "google-oauth2|5");

        await _sut.Handle(command, CancellationToken.None);

        _jwtTokenGenerator.Received(1).GenerateAccessToken(
            Arg.Is<Users>(u => u == existingUser),
            Arg.Is<SessionId>(s => s!.Value == sessionResult.SessionId));
    }

    [Fact]
    public async Task Handle_WhenSessionCreationSucceeds_ReturnsTokenResultWithAccessTokenAndRefreshToken()
    {
        var existingUser = new UserBuilder().WithEmail("result-user@example.com").Build();

        _userRepository
            .GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var sessionResult = BuildSuccessfulSessionResult(existingUser.Id.Value);

        _sessionService
            .CreateSessionAsync(Arg.Any<UserId>(), Arg.Any<IpAddress>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<RefreshTokenResult>.Success(sessionResult));

        _jwtTokenGenerator
            .GenerateAccessToken(Arg.Any<Users>(), Arg.Any<SessionId>())
            .Returns("final-access-token");

        var command = new GoogleLoginCommand("result-user@example.com", "Pouya", "Tehrani", "google-oauth2|6");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.AccessToken.ShouldBe("final-access-token");
        result.Value.RefreshToken.ShouldBe(sessionResult.RefreshToken);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_LooksUpByNormalizedEmail()
    {
        _userRepository
            .GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns((Users?)null);

        SetupSuccessfulSession(Guid.NewGuid());

        var command = new GoogleLoginCommand("Mixed.Case@Example.com", "Arya", "Fallahi", "google-oauth2|7");

        await _sut.Handle(command, CancellationToken.None);

        await _userRepository.Received(1).GetByEmailAsync(
            Arg.Is<Email>(e => e == Email.Create("Mixed.Case@Example.com")),
            Arg.Any<CancellationToken>());
    }
}
