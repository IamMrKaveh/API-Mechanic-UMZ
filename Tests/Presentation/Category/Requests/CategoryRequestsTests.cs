using Presentation.Category.Requests;

namespace Tests.Presentation.Category.Requests;

public class CategoryRequestsTests
{
    [Fact]
    public void CreateCategoryRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new CreateCategoryRequest("Cat1", "cat-1", "Description", 1);

        request.Name.ShouldBe("Cat1");
        request.Slug.ShouldBe("cat-1");
        request.Description.ShouldBe("Description");
        request.SortOrder.ShouldBe(1);
    }

    [Fact]
    public void CreateCategoryRequest_WithDefaultSortOrder_SetsToZero()
    {
        var request = new CreateCategoryRequest("Cat1", null, null);

        request.SortOrder.ShouldBe(0);
        request.Slug.ShouldBeNull();
        request.Description.ShouldBeNull();
    }

    [Fact]
    public void CreateCategoryRequest_IsRecord_EqualityWorks()
    {
        var request1 = new CreateCategoryRequest("Cat1", "cat-1", null, 1);
        var request2 = new CreateCategoryRequest("Cat1", "cat-1", null, 1);
        var request3 = new CreateCategoryRequest("Cat2", "cat-2", null, 1);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void UpdateCategoryRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new UpdateCategoryRequest("Cat1", "cat-1", "Description", 2, true, "rv");

        request.Name.ShouldBe("Cat1");
        request.Slug.ShouldBe("cat-1");
        request.Description.ShouldBe("Description");
        request.SortOrder.ShouldBe(2);
        request.IsActive.ShouldBeTrue();
        request.RowVersion.ShouldBe("rv");
    }

    [Fact]
    public void UpdateCategoryRequest_IsRecord_EqualityWorks()
    {
        var request1 = new UpdateCategoryRequest("Cat1", null, null, 1, true, null);
        var request2 = new UpdateCategoryRequest("Cat1", null, null, 1, true, null);
        var request3 = new UpdateCategoryRequest("Cat1", null, null, 1, false, null);

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }

    [Fact]
    public void ReorderCategoriesRequest_WithItems_SetsCorrectly()
    {
        var items = new List<CategoryOrderItemRequest>
        {
            new(Guid.NewGuid(), 0),
            new(Guid.NewGuid(), 1)
        };

        var request = new ReorderCategoriesRequest(items);

        request.Items.Count.ShouldBe(2);
        request.Items[0].SortOrder.ShouldBe(0);
        request.Items[1].SortOrder.ShouldBe(1);
    }

    [Fact]
    public void CategoryOrderItemRequest_WithAllParameters_SetsCorrectly()
    {
        var categoryId = Guid.NewGuid();

        var item = new CategoryOrderItemRequest(categoryId, 3);

        item.CategoryId.ShouldBe(categoryId);
        item.SortOrder.ShouldBe(3);
    }

    [Fact]
    public void CategoryOrderItemRequest_IsRecord_EqualityWorks()
    {
        var categoryId = Guid.NewGuid();

        var item1 = new CategoryOrderItemRequest(categoryId, 0);
        var item2 = new CategoryOrderItemRequest(categoryId, 0);
        var item3 = new CategoryOrderItemRequest(categoryId, 1);

        item1.ShouldBe(item2);
        item1.ShouldNotBe(item3);
    }
}
