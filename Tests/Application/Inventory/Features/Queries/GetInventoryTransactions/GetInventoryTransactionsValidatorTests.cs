using Application.Inventory.Features.Queries.GetInventoryTransactions;

namespace Tests.Application.Inventory.Features.Queries.GetInventoryTransactions;

public class GetInventoryTransactionsValidatorTests
{
    private readonly GetInventoryTransactionsValidator _sut = new();

    [Fact]
    public void Validate_WithDefaults_ReturnsIsValidTrue()
    {
        var query = new GetInventoryTransactionsQuery(null, null, null, null);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositivePage_ReturnsError(int page)
    {
        var query = new GetInventoryTransactionsQuery(null, null, null, null, page, 10);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetInventoryTransactionsQuery.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_WithPageSizeOutOfRange_ReturnsError(int pageSize)
    {
        var query = new GetInventoryTransactionsQuery(null, null, null, null, 1, pageSize);

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetInventoryTransactionsQuery.PageSize));
    }

    [Fact]
    public void Validate_WithToDateBeforeFromDate_ReturnsError()
    {
        var query = new GetInventoryTransactionsQuery(
            null, null,
            new DateTime(2026, 6, 10),
            new DateTime(2026, 6, 1));

        var result = _sut.Validate(query);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(GetInventoryTransactionsQuery.ToDate));
    }
}
