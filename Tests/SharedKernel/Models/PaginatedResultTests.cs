using SharedKernel.Models;

namespace Tests.SharedKernel.Models;

public class PaginatedResultTests
{
    [Fact]
    public void ParameterlessConstructor_ProducesEmptyResult()
    {
        var sut = new PaginatedResult<int>();

        sut.Items.ShouldBeEmpty();
        sut.TotalCount.ShouldBe(0);
        sut.Page.ShouldBe(0);
        sut.PageSize.ShouldBe(0);
        sut.IsEmpty.ShouldBeTrue();
        sut.TotalPages.ShouldBe(0);
    }

    [Fact]
    public void Constructor_WithValidArguments_SetsPropertiesAndComputesDerived()
    {
        var items = new List<int> { 1, 2, 3 };

        var sut = new PaginatedResult<int>(items, totalCount: 10, page: 1, pageSize: 3);

        sut.Items.Count.ShouldBe(3);
        sut.TotalCount.ShouldBe(10);
        sut.Page.ShouldBe(1);
        sut.PageSize.ShouldBe(3);
        sut.TotalPages.ShouldBe(4);
        sut.HasNextPage.ShouldBeTrue();
        sut.HasPreviousPage.ShouldBeFalse();
        sut.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_OnLastPage_HasNextPageFalseAndHasPreviousPageTrue()
    {
        var sut = new PaginatedResult<int>(new List<int> { 10 }, totalCount: 10, page: 4, pageSize: 3);

        sut.HasNextPage.ShouldBeFalse();
        sut.HasPreviousPage.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithTotalCountZero_IsEmptyIsTrueAndTotalPagesIsZero()
    {
        var sut = new PaginatedResult<int>(new List<int>(), totalCount: 0, page: 1, pageSize: 10);

        sut.IsEmpty.ShouldBeTrue();
        sut.TotalPages.ShouldBe(0);
        sut.HasNextPage.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_WithNullItems_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(
            () => new PaginatedResult<int>(null!, totalCount: 0, page: 1, pageSize: 10));
    }

    [Fact]
    public void Constructor_WithNegativeTotalCount_ThrowsArgumentOutOfRange()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => new PaginatedResult<int>(new List<int>(), totalCount: -1, page: 1, pageSize: 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositivePage_ThrowsArgumentOutOfRange(int page)
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => new PaginatedResult<int>(new List<int>(), totalCount: 5, page: page, pageSize: 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositivePageSize_ThrowsArgumentOutOfRange(int pageSize)
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => new PaginatedResult<int>(new List<int>(), totalCount: 5, page: 1, pageSize: pageSize));
    }

    [Fact]
    public void TotalPages_WithExactMultiple_DoesNotRoundUp()
    {
        var sut = new PaginatedResult<int>(new List<int> { 1 }, totalCount: 9, page: 1, pageSize: 3);

        sut.TotalPages.ShouldBe(3);
    }

    [Fact]
    public void TotalPages_WithNonMultiple_RoundsUp()
    {
        var sut = new PaginatedResult<int>(new List<int> { 1 }, totalCount: 10, page: 1, pageSize: 3);

        sut.TotalPages.ShouldBe(4);
    }

    [Fact]
    public void Create_WithEnumerable_ProducesPaginatedResult()
    {
        var sut = PaginatedResult<int>.Create(Enumerable.Range(1, 3), totalCount: 20, page: 1, pageSize: 3);

        sut.Items.Count.ShouldBe(3);
        sut.TotalCount.ShouldBe(20);
        sut.Page.ShouldBe(1);
        sut.PageSize.ShouldBe(3);
    }

    [Fact]
    public void Create_WithNullItems_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(
            () => PaginatedResult<int>.Create(null!, 0, 1, 10));
    }

    [Fact]
    public void Create_WithNegativeTotalCount_ThrowsArgumentOutOfRange()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => PaginatedResult<int>.Create(Enumerable.Empty<int>(), -1, 1, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositivePage_ThrowsArgumentOutOfRange(int page)
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => PaginatedResult<int>.Create(Enumerable.Empty<int>(), 5, page, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithPositivePageAndNonPositivePageSize_ThrowsArgumentOutOfRange(int pageSize)
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => PaginatedResult<int>.Create(Enumerable.Empty<int>(), 5, 1, pageSize));
    }

    [Fact]
    public void Deconstruct_ExposesAllFourMembers()
    {
        var sut = new PaginatedResult<int>(new List<int> { 1, 2 }, totalCount: 5, page: 1, pageSize: 2);

        var (items, totalCount, page, pageSize) = sut;

        items.Count.ShouldBe(2);
        totalCount.ShouldBe(5);
        page.ShouldBe(1);
        pageSize.ShouldBe(2);
    }
}
