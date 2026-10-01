using Application.Discount.Features.Commands.DeleteDiscount;
using Domain.Discount.Aggregates;
using Domain.Discount.Interfaces;
using Domain.Discount.ValueObjects;

namespace Tests.Application.Discount.Features.Commands.DeleteDiscount;

public class DeleteDiscountHandlerTests : HandlerTestBase
{
    private readonly IDiscountRepository _repository = Substitute.For<IDiscountRepository>();
    private readonly DeleteDiscountHandler _sut;
    private readonly DateTime _now = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

    public DeleteDiscountHandlerTests()
    {
        _sut = new DeleteDiscountHandler(_repository, DateTimeProvider);
        SetUtcNow(_now);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<DiscountCodeId>(), Arg.Any<CancellationToken>()).Returns((DiscountCode?)null);

        var result = await _sut.Handle(new DeleteDiscountCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
        _repository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenFound_DeactivatesUpdatesAndReturnsSuccess()
    {
        var discount = new DiscountCodeBuilder().WithCode("DEL").Build();
        discount.IsActive.ShouldBeTrue();
        _repository.GetByIdAsync(Arg.Any<DiscountCodeId>(), Arg.Any<CancellationToken>()).Returns(discount);

        var result = await _sut.Handle(new DeleteDiscountCommand(discount.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        discount.IsActive.ShouldBeFalse();
        _repository.Received(1).Update(discount);
    }

    [Fact]
    public async Task Handle_ConvertsGuidToDiscountCodeId()
    {
        var discount = new DiscountCodeBuilder().Build();
        DiscountCodeId? captured = null;
        _repository.GetByIdAsync(Arg.Do<DiscountCodeId>(x => captured = x), Arg.Any<CancellationToken>()).Returns(discount);

        await _sut.Handle(new DeleteDiscountCommand(discount.Id.Value), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(discount.Id.Value);
    }
}

