using Application.Cache.Contracts;
using Application.Order.Features.Commands.CreateOrderStatus;
using Domain.Order.Entities;
using Domain.Order.Interfaces;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Order.Features.Commands.CreateOrderStatus;

public class CreateOrderStatusHandlerTests
{
    private readonly IOrderStatusRepository _repository = Substitute.For<IOrderStatusRepository>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly CreateOrderStatusHandler _sut;

    public CreateOrderStatusHandlerTests()
    {
        _sut = new CreateOrderStatusHandler(_repository, _cacheService);
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ReturnsValidation()
    {
        _repository.ExistsByNameAsync("paid", null, Arg.Any<CancellationToken>()).Returns(true);

        var command = new CreateOrderStatusCommand("paid", "Paid", null, null, 0, false, false);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveByPrefixAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenNameUnique_CreatesStatusPersistsInvalidatesCacheAndReturnsMappedDto()
    {
        _repository.ExistsByNameAsync("paid", null, Arg.Any<CancellationToken>()).Returns(false);

        OrderStatus? captured = null;
        await _repository.AddAsync(Arg.Do<OrderStatus>(s => captured = s), Arg.Any<CancellationToken>());

        var command = new CreateOrderStatusCommand("paid", "Paid", "check", "#00FF00", 3, true, true);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        captured.ShouldNotBeNull();
        captured!.Name.ShouldBe("paid");
        captured.DisplayName.ShouldBe("Paid");
        captured.Icon.ShouldBe("check");
        captured.Color.ShouldBe("#00FF00");
        captured.SortOrder.ShouldBe(3);
        captured.AllowCancel.ShouldBeTrue();
        captured.AllowEdit.ShouldBeTrue();

        result.Value.Id.ShouldBe(captured.Id.Value);
        result.Value.Name.ShouldBe("paid");
        result.Value.DisplayName.ShouldBe("Paid");
        result.Value.AllowCancel.ShouldBeTrue();
        result.Value.AllowEdit.ShouldBeTrue();

        await _cacheService.Received(1).RemoveByPrefixAsync("order-status:", Arg.Any<CancellationToken>());
    }
}
