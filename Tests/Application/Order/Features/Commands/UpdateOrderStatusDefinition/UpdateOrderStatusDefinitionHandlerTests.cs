using Application.Audit.Contracts;
using Application.Cache.Contracts;
using Application.Order.Features.Commands.UpdateOrderStatusDefinition;
using Domain.Order.Entities;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Order.Features.Commands.UpdateOrderStatusDefinition;

public class UpdateOrderStatusDefinitionHandlerTests
{
    private readonly IOrderStatusRepository _repository = Substitute.For<IOrderStatusRepository>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly UpdateOrderStatusDefinitionHandler _sut;

    public UpdateOrderStatusDefinitionHandlerTests()
    {
        _sut = new UpdateOrderStatusDefinitionHandler(_repository, _auditService, _cacheService);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns((OrderStatus?)null);

        var result = await _sut.Handle(
            new UpdateOrderStatusDefinitionCommand(Guid.NewGuid(), "Paid", null, null, 0, false, false, null),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenRowVersionMalformed_ReturnsValidation()
    {
        var status = OrderStatus.Create("paid", "Paid");
        _repository.GetByIdAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns(status);

        var result = await _sut.Handle(
            new UpdateOrderStatusDefinitionCommand(status.Id.Value, "Paid", null, null, 0, false, false, "@@"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        _repository.DidNotReceive().Update(Arg.Any<OrderStatus>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesStatusPersistsAndInvalidatesCache()
    {
        var status = OrderStatus.Create("paid", "Paid");
        _repository.GetByIdAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns(status);

        var result = await _sut.Handle(
            new UpdateOrderStatusDefinitionCommand(status.Id.Value, "Paid (Updated)", "check", "#111111", 5, true, true, null),
            CancellationToken.None);

        result.ShouldBeSuccess();
        status.DisplayName.ShouldBe("Paid (Updated)");
        status.Icon.ShouldBe("check");
        status.Color.ShouldBe("#111111");
        status.SortOrder.ShouldBe(5);
        status.AllowCancel.ShouldBeTrue();
        status.AllowEdit.ShouldBeTrue();
        _repository.Received(1).Update(status, Arg.Any<byte[]?>());
        await _cacheService.Received(1).RemoveByPrefixAsync("order-status:", Arg.Any<CancellationToken>());
    }
}
