using Application.Common.Interfaces;
using Application.User.Contracts;
using Application.User.Features.Queries.GetUserDashboard;
using Application.User.Features.Shared;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.User.Features.Queries.GetUserDashboard;

public class GetUserDashboardHandlerTests
{
    private readonly IUserQueryService _userQueryService = Substitute.For<IUserQueryService>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly GetUserDashboardHandler _sut;

    public GetUserDashboardHandlerTests()
    {
        _sut = new GetUserDashboardHandler(_userQueryService, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenDashboardNotFound_ReturnsNotFound()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _userQueryService
            .GetUserDashboardAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((UserDashboardDto?)null);

        var result = await _sut.Handle(new GetUserDashboardQuery(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenDashboardExists_ReturnsMappedDashboard()
    {
        var userGuid = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)userGuid);

        var createdAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastLogin = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);

        var profile = new UserProfileDto
        {
            Id = userGuid,
            CreatedAt = createdAt,
            LastLoginAt = lastLogin,
            UserAddresses = new List<UserAddressDto>
        {
            new() { Id = Guid.NewGuid() },
            new() { Id = Guid.NewGuid() }
        }
        };

        var source = new UserDashboardDto
        {
            UserProfile = profile,
            TotalOrders = 12,
            TotalSpent = 500m,
            WishlistCount = 3,
            UnreadNotifications = 7,
            CompletedOrders = 8,
            OpenTickets = 2
        };

        _userQueryService
            .GetUserDashboardAsync(Arg.Is<UserId>(x => x == UserId.From(userGuid)), Arg.Any<CancellationToken>())
            .Returns(source);

        var result = await _sut.Handle(new GetUserDashboardQuery(), CancellationToken.None);

        result.ShouldBeSuccess();

        var dashboard = result.Value;
        dashboard.UserProfile.ShouldBe(profile);
        dashboard.TotalOrders.ShouldBe(12);
        dashboard.TotalSpent.ShouldBe(500m);
        dashboard.WishlistCount.ShouldBe(3);
        dashboard.UnreadNotifications.ShouldBe(7);
        dashboard.CompletedOrders.ShouldBe(8);
        dashboard.OpenTickets.ShouldBe(2);
        dashboard.MemberSince.ShouldBe(createdAt);
        dashboard.LastLoginAt.ShouldBe(lastLogin);
        dashboard.ActiveAddresses.ShouldBe(2);
    }
}
