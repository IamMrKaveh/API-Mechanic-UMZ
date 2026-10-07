using Presentation.Inventory.Requests;

namespace Tests.Presentation.Inventory.Requests;

public class InventoryRequestsTests
{
    [Fact]
    public void ReverseInventoryTransactionRequest_WithAllParameters_SetsCorrectly()
    {
        var variantId = Guid.NewGuid();

        var request = new ReverseInventoryTransactionRequest(variantId, "key-1", "reason");

        request.VariantId.ShouldBe(variantId);
        request.IdempotencyKey.ShouldBe("key-1");
        request.Reason.ShouldBe("reason");
    }

    [Fact]
    public void ReverseInventoryTransactionRequest_IsRecord_EqualityWorks()
    {
        var variantId = Guid.NewGuid();

        var request1 = new ReverseInventoryTransactionRequest(variantId, "key-1", "reason");
        var request2 = new ReverseInventoryTransactionRequest(variantId, "key-1", "reason");
        var request3 = new ReverseInventoryTransactionRequest(variantId, "key-2", "reason");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void AdjustStockRequest_WithAllParameters_SetsCorrectly()
    {
        var variantId = Guid.NewGuid();

        var request = new AdjustStockRequest(variantId, 5, "correction");

        request.VariantId.ShouldBe(variantId);
        request.QuantityChange.ShouldBe(5);
        request.Reason.ShouldBe("correction");
    }

    [Fact]
    public void BulkAdjustStockRequest_WithItems_SetsCorrectly()
    {
        var variantId = Guid.NewGuid();
        var items = new List<BulkAdjustStockItemRequest> { new(variantId, 3) };

        var request = new BulkAdjustStockRequest(items, "bulk");

        request.Items.Count.ShouldBe(1);
        request.Items[0].VariantId.ShouldBe(variantId);
        request.Items[0].QuantityChange.ShouldBe(3);
        request.Reason.ShouldBe("bulk");
    }

    [Fact]
    public void BulkAdjustStockItemRequest_WithAllParameters_SetsCorrectly()
    {
        var variantId = Guid.NewGuid();

        var item = new BulkAdjustStockItemRequest(variantId, -2);

        item.VariantId.ShouldBe(variantId);
        item.QuantityChange.ShouldBe(-2);
    }

    [Fact]
    public void RecordDamageRequest_WithAllParameters_SetsCorrectly()
    {
        var variantId = Guid.NewGuid();

        var request = new RecordDamageRequest(variantId, 2, "broken");

        request.VariantId.ShouldBe(variantId);
        request.Quantity.ShouldBe(2);
        request.Reason.ShouldBe("broken");
    }

    [Fact]
    public void BulkStockInRequest_WithItems_SetsCorrectly()
    {
        var variantId = Guid.NewGuid();
        var items = new List<BulkStockInItemRequest> { new(variantId, 10, "note") };

        var request = new BulkStockInRequest(items, "import");

        request.Items.Count.ShouldBe(1);
        request.Items[0].VariantId.ShouldBe(variantId);
        request.Items[0].Quantity.ShouldBe(10);
        request.Items[0].Notes.ShouldBe("note");
        request.Reason.ShouldBe("import");
    }

    [Fact]
    public void BulkStockInItemRequest_WithNullNotes_SetsCorrectly()
    {
        var variantId = Guid.NewGuid();

        var item = new BulkStockInItemRequest(variantId, 10, null);

        item.VariantId.ShouldBe(variantId);
        item.Quantity.ShouldBe(10);
        item.Notes.ShouldBeNull();
    }

    [Fact]
    public void BatchAvailabilityRequest_WithVariantIds_SetsCorrectly()
    {
        var variantId = Guid.NewGuid();
        ICollection<Guid> ids = [variantId];

        var request = new BatchAvailabilityRequest(ids);

        request.VariantIds.ShouldContain(variantId);
        request.VariantIds.Count.ShouldBe(1);
    }

    [Fact]
    public void ApproveReturnRequest_WithDefaultReason_SetsToNull()
    {
        var request = new ApproveReturnRequest();

        request.Reason.ShouldBeNull();
    }

    [Fact]
    public void ApproveReturnRequest_WithReason_SetsCorrectly()
    {
        var request = new ApproveReturnRequest("damaged");

        request.Reason.ShouldBe("damaged");
    }
}
