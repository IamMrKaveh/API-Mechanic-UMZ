namespace Tests.DomainTest.Inventory;

public class WarehouseStockTests
{
    private static WarehouseStock CreateStock(int warehouseId = 1, int variantId = 1) => WarehouseStock.Create(warehouseId, variantId);

    [Fact]
    public void Create_ShouldInitializeWithZeroQuantities()
    {
        var stock = CreateStock();

        stock.OnHand.Should().Be(0);
        stock.Reserved.Should().Be(0);
        stock.Damaged.Should().Be(0);
        stock.Available.Should().Be(0);
    }

    [Fact]
    public void Available_ShouldBeOnHandMinusReserved()
    {
        var stock = CreateStock();
        stock.AddStock(10);
        stock.Reserve(3);

        stock.Available.Should().Be(7);
    }

    [Fact]
    public void AddStock_ShouldIncreaseOnHand()
    {
        var stock = CreateStock();

        stock.AddStock(10);

        stock.OnHand.Should().Be(10);
    }

    [Fact]
    public void AddStock_MultipleTimesAddsCorrectly()
    {
        var stock = CreateStock();

        stock.AddStock(5);
        stock.AddStock(3);

        stock.OnHand.Should().Be(8);
    }

    [Fact]
    public void AddStock_WithZeroQuantity_ShouldThrowDomainException()
    {
        var stock = CreateStock();

        var act = () => stock.AddStock(0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddStock_WithNegativeQuantity_ShouldThrowDomainException()
    {
        var stock = CreateStock();

        var act = () => stock.AddStock(-5);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reserve_ShouldIncreaseReserved()
    {
        var stock = CreateStock();
        stock.AddStock(10);

        stock.Reserve(3);

        stock.Reserved.Should().Be(3);
    }

    [Fact]
    public void Reserve_ShouldDecreaseAvailable()
    {
        var stock = CreateStock();
        stock.AddStock(10);

        stock.Reserve(3);

        stock.Available.Should().Be(7);
    }

    [Fact]
    public void Reserve_WhenInsufficientAvailable_ShouldThrowDomainException()
    {
        var stock = CreateStock();
        stock.AddStock(5);

        var act = () => stock.Reserve(10);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reserve_WithZeroQuantity_ShouldThrowDomainException()
    {
        var stock = CreateStock();
        stock.AddStock(10);

        var act = () => stock.Reserve(0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ReleaseReservation_ShouldDecreaseReserved()
    {
        var stock = CreateStock();
        stock.AddStock(10);
        stock.Reserve(5);

        stock.ReleaseReservation(3);

        stock.Reserved.Should().Be(2);
    }

    [Fact]
    public void ReleaseReservation_ShouldIncreaseAvailable()
    {
        var stock = CreateStock();
        stock.AddStock(10);
        stock.Reserve(5);

        stock.ReleaseReservation(5);

        stock.Available.Should().Be(10);
    }

    [Fact]
    public void ReleaseReservation_WhenExceedsReserved_ShouldClampToZero()
    {
        var stock = CreateStock();
        stock.AddStock(10);
        stock.Reserve(3);

        stock.ReleaseReservation(10);

        stock.Reserved.Should().Be(0);
    }

    [Fact]
    public void CommitReservation_ShouldReduceOnHandAndReserved()
    {
        var stock = CreateStock();
        stock.AddStock(10);
        stock.Reserve(5);

        stock.CommitReservation(5);

        stock.OnHand.Should().Be(5);
        stock.Reserved.Should().Be(0);
    }

    [Fact]
    public void CommitReservation_WithZeroQuantity_ShouldThrowDomainException()
    {
        var stock = CreateStock();
        stock.AddStock(10);

        var act = () => stock.CommitReservation(0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkAsDamaged_ShouldReduceOnHandAndIncreaseDamaged()
    {
        var stock = CreateStock();
        stock.AddStock(10);

        stock.MarkAsDamaged(3);

        stock.OnHand.Should().Be(7);
        stock.Damaged.Should().Be(3);
    }

    [Fact]
    public void MarkAsDamaged_WhenInsufficientOnHand_ShouldThrowDomainException()
    {
        var stock = CreateStock();
        stock.AddStock(5);

        var act = () => stock.MarkAsDamaged(10);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkAsDamaged_WithZeroQuantity_ShouldThrowDomainException()
    {
        var stock = CreateStock();
        stock.AddStock(10);

        var act = () => stock.MarkAsDamaged(0);

        act.Should().Throw<DomainException>();
    }
}