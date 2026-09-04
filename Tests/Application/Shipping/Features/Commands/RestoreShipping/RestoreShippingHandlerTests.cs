using Application.Audit.Contracts;
using Application.Cache.Contracts;
using Application.Common.Interfaces;
using Application.Shipping.Features.Commands.RestoreShipping;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using DomainShipping = Domain.Shipping.Aggregates.Shipping;

namespace Tests.Application.Shipping.Features.Commands.RestoreShipping;

public class RestoreShippingHandlerTests
{
    private readonly IShippingRepository _shippingRepository = Substitute.For<IShippingRepository>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly RestoreShippingHandler _sut;

    public RestoreShippingHandlerTests()
    {
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _sut = new RestoreShippingHandler(_shippingRepository, _currentUser, _auditService, _cacheService);
    }

    [Fact]
    public async Task Handle_WhenShippingNotFound_ReturnsNotFoundAndDoesNotUpdateOrAudit()
    {
        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns((DomainShipping?)null);

        var result = await _sut.Handle(new RestoreShippingCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _shippingRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _auditService.DidNotReceiveWithAnyArgs().LogAdminEventAsync(default!, default!, default!);
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveByPrefixAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenShippingIsInactive_RestoresActivatesAndReturnsSuccess()
    {
        var shipping = new ShippingBuilder().AsDeleted().Build();
        shipping.IsActive.ShouldBeFalse();

        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns(shipping);

        var result = await _sut.Handle(new RestoreShippingCommand(shipping.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        shipping.IsActive.ShouldBeTrue();
        _shippingRepository.Received(1).Update(shipping);
        await _cacheService.Received(1).RemoveByPrefixAsync("shippings:", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSuccess_LogsAdminEventWithExpectedTitleAndDetail()
    {
        var shipping = new ShippingBuilder().AsDeleted().Build();
        var adminGuid = Guid.NewGuid();
        _currentUser.UserId.Returns((Guid?)adminGuid);

        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns(shipping);

        var command = new RestoreShippingCommand(shipping.Id.Value);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        await _auditService.Received(1).LogAdminEventAsync(
            "RestoreShippingMethod",
            Arg.Is<UserId>(u => u == UserId.From(adminGuid)),
            $"Restored shipping method ID: {command.Id}");
    }

    [Fact]
    public async Task Handle_WhenShippingAlreadyActive_ReturnsSuccessAndStillUpdatesAndAudits()
    {
        var shipping = new ShippingBuilder().Build();
        shipping.IsActive.ShouldBeTrue();

        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns(shipping);

        var result = await _sut.Handle(new RestoreShippingCommand(shipping.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        shipping.IsActive.ShouldBeTrue();
        _shippingRepository.Received(1).Update(shipping);
        await _auditService.Received(1).LogAdminEventAsync(
            "RestoreShippingMethod",
            Arg.Any<UserId>(),
            Arg.Any<string>());
    }
}
