using Application.Discount.Features.Commands.CreateDiscount;
using Application.Discount.Features.Shared;
using Domain.Discount.Aggregates;
using Domain.Discount.Enums;
using Domain.Discount.Interfaces;

namespace Tests.Application.Discount.Features.Commands.CreateDiscount;

public class CreateDiscountHandlerTests : HandlerTestBase
{
    private readonly IDiscountRepository _repository = Substitute.For<IDiscountRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly CreateDiscountHandler _sut;
    private readonly DateTime _now = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

    public CreateDiscountHandlerTests()
    {
        _sut = new CreateDiscountHandler(_repository, _mapper, DateTimeProvider);
        DateTimeProvider.UtcNow.Returns(_now);
        _mapper.Map<DiscountDto>(Arg.Any<DiscountCode>())
            .Returns(ci =>
            {
                var d = ci.Arg<DiscountCode>()!;
                return new DiscountDto { Id = d.Id.Value, Code = d.Code, DiscountType = d.Value.Type.ToString(), DiscountValue = d.Value.Amount, CreatedAt = d.CreatedAt };
            });
    }

    [Fact]
    public async Task Handle_WhenCodeBlank_ReturnsFailureWithoutTouchingRepository()
    {
        var result = await _sut.Handle(
            new CreateDiscountCommand("  ", DiscountType.Percentage, 10m, null, null, true, null, null),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await _repository.DidNotReceiveWithAnyArgs().GetByCodeAsync(default!, default);
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenCodeAlreadyExists_ReturnsConflict()
    {
        var existing = new DiscountCodeBuilder().WithCode("DUP").Build();
        _repository.GetByCodeAsync("DUP", Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _sut.Handle(
            new CreateDiscountCommand("DUP", DiscountType.Percentage, 10m, null, null, true, null, null),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenDiscountValueInvalid_ReturnsFailure()
    {
        _repository.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((DiscountCode?)null);

        var result = await _sut.Handle(
            new CreateDiscountCommand("BAD", DiscountType.Percentage, 150m, null, null, true, null, null),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenDatesInvalid_ReturnsFailure()
    {
        _repository.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((DiscountCode?)null);

        var result = await _sut.Handle(
            new CreateDiscountCommand("D1", DiscountType.Percentage, 10m, null, null, true, _now.AddDays(5), _now.AddDays(1)),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesAddsAndReturnsDto()
    {
        _repository.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((DiscountCode?)null);
        DiscountCode? captured = null;
        await _repository.AddAsync(Arg.Do<DiscountCode>(x => captured = x), Arg.Any<CancellationToken>());

        var result = await _sut.Handle(
            new CreateDiscountCommand("NEW10", DiscountType.Percentage, 10m, 50_000m, 100, true, null, _now.AddDays(10)),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Code.ShouldBe("NEW10");
        captured.ShouldNotBeNull();
        captured!.Value.Amount.ShouldBe(10m);
        captured.MaximumDiscountAmount!.Amount.ShouldBe(50_000m);
        captured.UsageLimit.ShouldBe(100);
    }

    [Fact]
    public async Task Handle_FreeShipping_CreatesSuccessfully()
    {
        _repository.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((DiscountCode?)null);

        var result = await _sut.Handle(
            new CreateDiscountCommand("FREESHIP", DiscountType.FreeShipping, 0m, null, null, true, null, null),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.DiscountType.ShouldBe(DiscountType.FreeShipping.ToString());
    }
}
