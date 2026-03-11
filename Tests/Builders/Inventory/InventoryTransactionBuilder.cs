using Domain.Inventory.Entities;

namespace Tests.Builders.Inventory;

public class InventoryTransactionBuilder
{
    private int _variantId = 1;
    private int _quantity = 5;
    private int _stockBefore = 10;
    private int? _userId = 1;
    private string? _referenceNumber = "REF-1";

    public InventoryTransactionBuilder WithVariant(int variantId)
    {
        _variantId = variantId;
        return this;
    }

    public InventoryTransactionBuilder WithQuantity(int quantity)
    {
        _quantity = quantity;
        return this;
    }

    public InventoryTransactionBuilder WithStockBefore(int stockBefore)
    {
        _stockBefore = stockBefore;
        return this;
    }

    public InventoryTransactionBuilder WithUser(int? userId)
    {
        _userId = userId;
        return this;
    }

    public InventoryTransactionBuilder WithReference(string reference)
    {
        _referenceNumber = reference;
        return this;
    }

    public InventoryTransaction BuildStockIn()
    {
        return InventoryTransaction.CreateStockIn(
            _variantId,
            _quantity,
            _stockBefore,
            _userId,
            referenceNumber: _referenceNumber
        );
    }

    public InventoryTransaction BuildStockOut()
    {
        return InventoryTransaction.CreateStockOut(
            _variantId,
            _quantity,
            _stockBefore,
            _userId,
            referenceNumber: _referenceNumber
        );
    }

    public InventoryTransaction BuildReservation()
    {
        return InventoryTransaction.CreateReservation(
            _variantId,
            _quantity,
            _stockBefore,
            orderItemId: 1,
            _userId,
            referenceNumber: _referenceNumber
        );
    }

    public InventoryTransaction BuildCommit()
    {
        return InventoryTransaction.CreateCommit(
            _variantId,
            _quantity,
            _stockBefore,
            orderItemId: 1,
            _userId,
            referenceNumber: _referenceNumber
        );
    }

    public InventoryTransaction BuildReturn()
    {
        return InventoryTransaction.CreateReturn(
            _variantId,
            _quantity,
            _stockBefore,
            orderItemId: 1,
            _userId,
            correlationId: _referenceNumber
        );
    }

    public InventoryTransaction BuildDamage()
    {
        return InventoryTransaction.CreateDamage(
            _variantId,
            _quantity,
            _stockBefore,
            _userId ?? 1,
            "damage"
        );
    }

    public InventoryTransaction BuildAdjustment(int quantityChange)
    {
        return InventoryTransaction.CreateAdjustment(
            _variantId,
            quantityChange,
            _stockBefore,
            _userId ?? 1,
            "adjustment"
        );
    }
}