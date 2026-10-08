using Presentation.Search.Requests;

namespace Tests.Presentation.Search.Requests;

public class SearchRequestsTests
{
    [Fact]
    public void SearchProductsRequest_WithDefaults_SetsCorrectly()
    {
        var request = new SearchProductsRequest();

        request.Q.ShouldBeNull();
        request.CategoryId.ShouldBeNull();
        request.BrandId.ShouldBeNull();
        request.MinPrice.ShouldBeNull();
        request.MaxPrice.ShouldBeNull();
        request.InStockOnly.ShouldBeFalse();
        request.SortBy.ShouldBeNull();
        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(20);
    }

    [Fact]
    public void SearchProductsRequest_WithAllParameters_SetsCorrectly()
    {
        var categoryId = Guid.NewGuid();

        var request = new SearchProductsRequest("phone", categoryId, null, 100, 1000, true, "Price", 2, 15);

        request.Q.ShouldBe("phone");
        request.CategoryId.ShouldBe(categoryId);
        request.MinPrice.ShouldBe(100);
        request.MaxPrice.ShouldBe(1000);
        request.InStockOnly.ShouldBeTrue();
        request.SortBy.ShouldBe("Price");
        request.Page.ShouldBe(2);
        request.PageSize.ShouldBe(15);
    }

    [Fact]
    public void GetSearchSuggestionsRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new GetSearchSuggestionsRequest("pho", 5);

        request.Q.ShouldBe("pho");
        request.MaxSuggestions.ShouldBe(5);
    }

    [Fact]
    public void GetSearchSuggestionsRequest_WithDefaultMaxSuggestions_SetsToTen()
    {
        var request = new GetSearchSuggestionsRequest("pho");

        request.MaxSuggestions.ShouldBe(10);
    }

    [Fact]
    public void FuzzySearchRequest_WithAllParameters_SetsCorrectly()
    {
        var request = new FuzzySearchRequest("phone", 2, 15);

        request.Q.ShouldBe("phone");
        request.Page.ShouldBe(2);
        request.PageSize.ShouldBe(15);
    }

    [Fact]
    public void FuzzySearchRequest_WithDefaults_SetsCorrectly()
    {
        var request = new FuzzySearchRequest("phone");

        request.Page.ShouldBe(1);
        request.PageSize.ShouldBe(20);
    }

    [Fact]
    public void SearchProductsRequest_IsRecord_EqualityWorks()
    {
        var request1 = new SearchProductsRequest(Q: "phone");
        var request2 = new SearchProductsRequest(Q: "phone");
        var request3 = new SearchProductsRequest(Q: "laptop");

        request1.ShouldBe(request2);
        request1.ShouldNotBe(request3);
    }
}
