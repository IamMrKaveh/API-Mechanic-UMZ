using Domain.Inventory.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Inventory.ValueObjects;

public class StockLedgerEntryIdTests
{
    [Fact]
    public void NewId_Always_ReturnsNonEmptyGuid()
    {
        StockLedgerEntryId.NewId().Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void From_WithNonEmptyGuid_ReturnsIdWithSameValue()
    {
        var guid = Guid.NewGuid();

        StockLedgerEntryId.From(guid).Value.ShouldBe(guid);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsDomainException()
    {
        var ex = Should.Throw<DomainException>(() => StockLedgerEntryId.From(Guid.Empty));

        ex.Message.ShouldBe("StockLedgerEntryId cannot be empty.");
    }
}
