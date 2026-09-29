using Application.Discount.Features.Commands.UpdateDiscount;
using Application.Discount.Features.Shared;
using Domain.Discount.Aggregates;
using Domain.Discount.Enums;
using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;

namespace Tests.Application.Discount.Features.Commands.UpdateDiscount;

public class UpdateDiscountHandlerTests : HandlerTestBase
{
    private readonly IDiscountRepository _repository = Substitute.For<IDiscountRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly UpdateDiscountHandler _sut;
    private readonly DateTime _now = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

    public UpdateDiscountHandlerTests()
    {
        _sut = new UpdateDiscountHandler(_repository, _mapper, DateTimeProvider);
        DateTimeProvider.UtcNow.Returns(_now);
        _mapper.Map<DiscountDto>(Arg.Any<DiscountCode>())
            .Returns(ci =>
            {
                var d = ci.Arg<DiscountCode>()!;
                return new DiscountDto { Id = d.Id.Value, Code = d.Code, DiscountType = d.Value.Type.ToString(), DiscountValue = d.Value.Amount, IsActive = d.IsActive };
            });
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<DiscountCodeId>(), Arg.Any<CancellationToken>()).Returns((DiscountCode?)null);

        var result = await _sut.Handle(
            new UpdateDiscountCommand(Guid.NewGuid(), DiscountType.Percentage, 10m, null, null, null, null, true),
            CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
        _repository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesValueLimitsDatesAndReturnsDto()
    {
        var discount = new DiscountCodeBuilder().WithCode("UPD").Build();
        _repository.GetByIdAsync(Arg.Any<DiscountCodeId>(), Arg.Any<CancellationToken>()).Returns(discount);
        var expires = _now.AddDays(30);

        var result = await _sut.Handle(
            new UpdateDiscountCommand(discount.Id.Value, DiscountType.FixedAmount, 25_000m, 30_000m, 50, null, expires, true),
            CancellationToken.None);

        result.ShouldBeSuccess();
        discount.Value.Type.ShouldBe(DiscountType.FixedAmount);
        discount.Value.Amount.ShouldBe(25_000m);
        discount.MaximumDiscountAmount!.Amount.ShouldBe(30_000m);
        discount.UsageLimit.ShouldBe(50);
        discount.ExpiresAt.ShouldBe(expires);
        result.Value.DiscountValue.ShouldBe(25_000m);
        _repository.Received(1).Update(discount);
    }

    [Fact]
    public async Task Handle_WhenDeactivating_DeactivatesDiscount()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.IsActive.ShouldBeTrue();
        _repository.GetByIdAsync(Arg.Any<DiscountCodeId>(), Arg.Any<CancellationToken>()).Returns(discount);

        var result = await _sut.Handle(
            new UpdateDiscountCommand(discount.Id.Value, DiscountType.Percentage, 5m, null, null, null, null, false),
            CancellationToken.None);

        result.ShouldBeSuccess();
        discount.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_WhenReactivating_ActivatesDiscount()
    {
        var discount = new DiscountCodeBuilder().Build();
        discount.Deactivate(_now);
        discount.IsActive.ShouldBeFalse();
        _repository.GetByIdAsync(Arg.Any<DiscountCodeId>(), Arg.Any<CancellationToken>()).Returns(discount);

        var result = await _sut.Handle(
            new UpdateDiscountCommand(discount.Id.Value, DiscountType.Percentage, 5m, null, null, null, null, true),
            CancellationToken.None);

        result.ShouldBeSuccess();
        discount.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidDiscountType_ThrowsArgumentOutOfRange()
    {
        var discount = new DiscountCodeBuilder().Build();
        _repository.GetByIdAsync(Arg.Any<DiscountCodeId>(), Arg.Any<CancellationToken>()).Returns(discount);

        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            _sut.Handle(
                new UpdateDiscountCommand(discount.Id.Value, (DiscountType)999, 5m, null, null, null, null, true),
                CancellationToken.None));
    }
}
