namespace Tests.DomainTest.Inventory;

public class InventoryTransactionTests
{
    [Fact]
    public void CreateReversal_Should_Create_New_Transaction_When_Valid()
    {
        var transaction = new InventoryTransactionBuilder()
            .BuildStockIn();

        var reversal = transaction.CreateReversal();

        Assert.NotNull(reversal);
        Assert.NotEqual(transaction.Id, reversal.Id);
        Assert.True(transaction.IsReversed);
        Assert.Equal(-transaction.QuantityChange, reversal.QuantityChange);
        Assert.Equal(transaction.StockAfter, reversal.StockBefore);
    }

    [Fact]
    public void CreateReversal_Should_Throw_When_Already_Reversed()
    {
        var transaction = new InventoryTransactionBuilder()
            .BuildStockIn();

        transaction.MarkAsReversed();

        Assert.Throws<DomainException>(() =>
        {
            transaction.CreateReversal();
        });
    }

    [Fact]
    public void CreateStockOut_Should_Throw_When_Stock_Is_Not_Enough()
    {
        var builder = new InventoryTransactionBuilder()
            .WithStockBefore(2)
            .WithQuantity(5);

        Assert.Throws<DomainException>(() =>
        {
            builder.BuildStockOut();
        });
    }

    [Fact]
    public void CreateStockIn_Should_Increase_Stock()
    {
        var transaction = new InventoryTransactionBuilder()
            .WithStockBefore(10)
            .WithQuantity(5)
            .BuildStockIn();

        Assert.Equal(15, transaction.StockAfter);
        Assert.True(transaction.IsIncrease());
    }

    [Fact]
    public void CreateStockOut_Should_Decrease_Stock()
    {
        var transaction = new InventoryTransactionBuilder()
            .WithStockBefore(10)
            .WithQuantity(4)
            .BuildStockOut();

        Assert.Equal(6, transaction.StockAfter);
        Assert.True(transaction.IsDecrease());
    }

    [Fact]
    public void CreateReservation_Should_Decrease_Stock()
    {
        var transaction = new InventoryTransactionBuilder()
            .WithStockBefore(20)
            .WithQuantity(3)
            .BuildReservation();

        Assert.Equal(17, transaction.StockAfter);
        Assert.True(transaction.IsDecrease());
        Assert.True(transaction.IsReservation());
    }

    [Fact]
    public void CreateCommit_Should_Decrease_Stock()
    {
        var transaction = new InventoryTransactionBuilder()
            .WithStockBefore(15)
            .WithQuantity(5)
            .BuildCommit();

        Assert.Equal(10, transaction.StockAfter);
        Assert.True(transaction.IsCommit());
    }

    [Fact]
    public void CreateReturn_Should_Increase_Stock()
    {
        var transaction = new InventoryTransactionBuilder()
            .WithStockBefore(10)
            .WithQuantity(2)
            .BuildReturn();

        Assert.Equal(12, transaction.StockAfter);
        Assert.True(transaction.IsIncrease());
        Assert.True(transaction.IsReturn());
    }

    [Fact]
    public void CreateDamage_Should_Decrease_Stock()
    {
        var transaction = new InventoryTransactionBuilder()
            .WithStockBefore(10)
            .WithQuantity(3)
            .BuildDamage();

        Assert.Equal(7, transaction.StockAfter);
        Assert.True(transaction.IsDamage());
    }

    [Fact]
    public void CreateAdjustment_Should_Change_Stock()
    {
        var transaction = new InventoryTransactionBuilder()
            .WithStockBefore(10)
            .BuildAdjustment(4);

        Assert.Equal(14, transaction.StockAfter);
        Assert.True(transaction.IsAdjustment());
    }

    [Fact]
    public void CanBeReversed_Should_Return_False_When_Reversed()
    {
        var transaction = new InventoryTransactionBuilder()
            .BuildStockIn();

        transaction.MarkAsReversed();

        Assert.False(transaction.CanBeReversed());
    }

    [Fact]
    public void GetTransactionTypeEnum_Should_Return_Correct_Type()
    {
        var transaction = new InventoryTransactionBuilder()
            .BuildStockIn();

        var type = transaction.GetTransactionTypeEnum();

        Assert.Equal(TransactionType.StockIn.Value, type.Value);
    }
}