using Application.Audit.Contracts;
using Application.Discount.Features.Commands.CancelDiscountUsage;
using Domain.Discount.Aggregates;
using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.Application.Discount.Features.Commands.CancelDiscountUsage;

public class CancelDiscountUsageHandlerTests : HandlerTestBase
{
    private readonly IDiscountRepository _repository = Substitute.For<IDiscountRepository>();
    private readonly CancelDiscountUsageHandler _sut;

    public CancelDiscountUsageHandlerTests()
    {
        _sut = new CancelDiscountUsageHandler(_repository, AuditService);
    }

    private static DiscountCode CodeWithUsage(Guid orderGuid, out DiscountCodeId codeId)
    {
        var discount = new DiscountCodeBuilder().WithCode("SAVE10").Build();
        codeId = discount.Id;
        discount.RecordUsage(UserId.NewId(), OrderId.From(orderGuid), Money.Create(10_000m, "IRT"), DateTime.UtcNow);
        return discount;
    }

    [Fact]
    public async Task Handle_WhenDiscountNotFound_ReturnsNotFound()
    {
        _repository.GetByIdWithUsagesAsync(Arg.Any<DiscountCodeId>(), Arg.Any<CancellationToken>())
            .Returns((DiscountCode?)null);

        var result = await _sut.Handle(new CancelDiscountUsageCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
        _repository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenUsageForOrderMissing_ReturnsNotFound()
    {
        var discount = CodeWithUsage(Guid.NewGuid(), out _);
        _repository.GetByIdWithUsagesAsync(Arg.Any<DiscountCodeId>(), Arg.Any<CancellationToken>()).Returns(discount);

        var result = await _sut.Handle(new CancelDiscountUsageCommand(discount.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
        _repository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenUsageExists_UpdatesAndAudits()
    {
        var orderGuid = Guid.NewGuid();
        var discount = CodeWithUsage(orderGuid, out _);
        _repository.GetByIdWithUsagesAsync(Arg.Any<DiscountCodeId>(), Arg.Any<CancellationToken>()).Returns(discount);

        var result = await _sut.Handle(new CancelDiscountUsageCommand(discount.Id.Value, orderGuid), CancellationToken.None);

        result.ShouldBeSuccess();
        _repository.Received(1).Update(discount);
        await AuditService.Received(1).LogAsync(
            "Discount",
            "CancelDiscountUsage",
            Arg.Any<IpAddress>(),
            null,
            "DiscountCode",
            discount.Id.Value.ToString(),
            Arg.Is<string>(s => s != null && s.Contains(orderGuid.ToString())),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConvertsIdsToStronglyTypedIds()
    {
        var orderGuid = Guid.NewGuid();
        var discount = CodeWithUsage(orderGuid, out _);
        DiscountCodeId? captured = null;
        _repository.GetByIdWithUsagesAsync(Arg.Do<DiscountCodeId>(x => captured = x), Arg.Any<CancellationToken>())
            .Returns(discount);

        await _sut.Handle(new CancelDiscountUsageCommand(discount.Id.Value, orderGuid), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(discount.Id.Value);
    }
}
