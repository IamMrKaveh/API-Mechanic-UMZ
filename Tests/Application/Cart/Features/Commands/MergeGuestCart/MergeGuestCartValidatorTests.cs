using Application.Cart.Features.Commands.MergeGuestCart;
using Domain.Cart.Enum;

namespace Tests.Application.Cart.Features.Commands.MergeGuestCart;

public class MergeGuestCartValidatorTests
{
    private readonly MergeGuestCartValidator _sut = new();

    [Fact]
    public void Validate_WhenCommandUsesDefaultStrategy_IsValid()
    {
        var command = new MergeGuestCartCommand();

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_WhenCommandUsesDefaultStrategy_StrategyIsSumQuantities()
    {
        var command = new MergeGuestCartCommand();

        command.Strategy.ShouldBe(CartMergeStrategy.SumQuantities);
    }

    [Theory]
    [MemberData(nameof(AllMergeStrategies))]
    public void Validate_WhenStrategyIsAnyDefinedValue_IsValid(CartMergeStrategy strategy)
    {
        var command = new MergeGuestCartCommand(strategy);

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    public static IEnumerable<object[]> AllMergeStrategies()
    {
        foreach (var value in Enum.GetValues<CartMergeStrategy>())
            yield return new object[] { value };
    }
}
