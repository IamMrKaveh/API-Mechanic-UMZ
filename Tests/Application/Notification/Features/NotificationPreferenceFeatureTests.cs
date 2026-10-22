using Application.Notification.Features.Commands.UpdateNotificationPreferences;
using Application.Notification.Features.Queries.GetNotificationPreferences;
using Application.Notification.Features.Shared;
using Domain.Notification.Aggregates;
using Domain.Notification.Interfaces;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.Application.Notification.Features;

public class NotificationPreferenceFeatureTests : HandlerTestBase
{
    private readonly INotificationPreferenceRepository _repository
        = Substitute.For<INotificationPreferenceRepository>();

    [Fact]
    public async Task GetPreferences_WithoutAPreferenceRecord_EnablesEveryType()
    {
        var userId = UserId.NewId();
        CurrentUserService.UserId.Returns(userId.Value);
        _repository.GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((NotificationPreference?)null);

        var handler = new GetNotificationPreferencesHandler(_repository, CurrentUserService);

        var result = await handler.Handle(
            new GetNotificationPreferencesQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value!.Types.ShouldNotBeEmpty();
        result.Value.Types.ShouldAllBe(t => t.IsEnabled);
    }

    [Fact]
    public async Task GetPreferences_WithADisabledType_ReportsItAsDisabled()
    {
        var userId = UserId.NewId();
        CurrentUserService.UserId.Returns(userId.Value);

        var preference = NotificationPreference.Create(
            NotificationPreferenceId.NewId(), userId, DateTime.UtcNow);
        preference.Disable(NotificationType.DiscountCode, DateTime.UtcNow);

        _repository.GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(preference);

        var handler = new GetNotificationPreferencesHandler(_repository, CurrentUserService);

        var result = await handler.Handle(
            new GetNotificationPreferencesQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        var discount = result.Value!.Types.Single(t => t.Value == NotificationType.DiscountCode.Value);
        discount.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdatePreferences_WithoutARecord_CreatesOne()
    {
        var userId = UserId.NewId();
        CurrentUserService.UserId.Returns(userId.Value);
        _repository.GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((NotificationPreference?)null);

        var handler = new UpdateNotificationPreferencesHandler(
            _repository, UnitOfWork, AuditService, DateTimeProvider, CurrentUserService);

        var result = await handler.Handle(
            new UpdateNotificationPreferencesCommand([NotificationType.DiscountCode.Value]),
            CancellationToken.None);

        result.ShouldBeSuccess();
        await _repository.Received(1).AddAsync(
            Arg.Is<NotificationPreference>(p =>
                p!.DisabledTypeNames.Contains(NotificationType.DiscountCode.Value)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatePreferences_WithAnExistingRecord_ReplacesTheDisabledTypes()
    {
        var userId = UserId.NewId();
        CurrentUserService.UserId.Returns(userId.Value);

        var preference = NotificationPreference.Create(
            NotificationPreferenceId.NewId(), userId, DateTime.UtcNow);
        preference.Disable(NotificationType.OrderCreated, DateTime.UtcNow);

        _repository.GetByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(preference);

        var handler = new UpdateNotificationPreferencesHandler(
            _repository, UnitOfWork, AuditService, DateTimeProvider, CurrentUserService);

        var result = await handler.Handle(
            new UpdateNotificationPreferencesCommand([NotificationType.DiscountCode.Value]),
            CancellationToken.None);

        result.ShouldBeSuccess();
        preference.IsEnabled(NotificationType.OrderCreated).ShouldBeTrue();
        preference.IsEnabled(NotificationType.DiscountCode).ShouldBeFalse();
        _repository.Received(1).Update(preference);
    }
}
