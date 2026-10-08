using Application.Search.Features.Queries.FuzzySearch;
using Application.Search.Features.Queries.GetSearchSuggestions;
using Application.Search.Features.Queries.GlobalSearch;
using Application.Search.Features.Queries.SearchProducts;
using Application.Search.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Search.Endpoints;
using Presentation.Search.Requests;
using SharedKernel.Results;

namespace Tests.Presentation.Search.Endpoints;

public class SearchControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly SearchController _controller;

    public SearchControllerTests()
    {
        _controller = new SearchController(_mediator, _mapper);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task SearchProducts_MapsRequestToQuery_AndReturnsOk()
    {
        // Arrange
        var request = new SearchProductsRequest(Q: "phone", Page: 1, PageSize: 20);
        var query = new SearchProductsQuery("phone", null, null, null, null, false, null, 1, 20);
        var expected = new SearchResultDto<ProductSearchResultItemDto>
        {
            Items = [new ProductSearchResultItemDto { ProductId = Guid.NewGuid(), Name = "Phone" }],
            Total = 1,
            Page = 1,
            PageSize = 20
        };

        _mapper.Map<SearchProductsQuery>(request).Returns(query);
        _mediator.Send(Arg.Any<SearchProductsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<SearchResultDto<ProductSearchResultItemDto>>.Success(expected));

        // Act
        var result = await _controller.SearchProducts(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<SearchResultDto<ProductSearchResultItemDto>>>();
        body.Data!.Total.ShouldBe(1);
    }

    [Fact]
    public async Task SearchGlobal_WithQuery_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var expected = new GlobalSearchResultDto { Query = "phone" };

        _mediator.Send(Arg.Any<GlobalSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<GlobalSearchResultDto>.Success(expected));

        // Act
        var result = await _controller.SearchGlobal("phone", CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<GlobalSearchResultDto>>();
        body.Data!.Query.ShouldBe("phone");
        await _mediator.Received(1).Send(
            Arg.Is<GlobalSearchQuery>(q => q.Q == "phone"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSuggestions_MapsRequestToQuery_AndReturnsOk()
    {
        // Arrange
        var request = new GetSearchSuggestionsRequest("pho", 5);
        var query = new GetSearchSuggestionsQuery("pho", 5);
        var expected = new List<string> { "phone", "photo" };

        _mapper.Map<GetSearchSuggestionsQuery>(request).Returns(query);
        _mediator.Send(Arg.Any<GetSearchSuggestionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<List<string>>.Success(expected));

        // Act
        var result = await _controller.GetSuggestions(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<List<string>>>();
        body.Data!.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SearchWithFuzzy_MapsRequestToQuery_AndReturnsOk()
    {
        // Arrange
        var request = new FuzzySearchRequest("phone", 1, 20);
        var query = new FuzzySearchQuery("phone", 1, 20);
        var expected = new SearchResultDto<ProductSearchResultItemDto>
        {
            Items = [],
            Total = 0,
            Page = 1,
            PageSize = 20
        };

        _mapper.Map<FuzzySearchQuery>(request).Returns(query);
        _mediator.Send(Arg.Any<FuzzySearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<SearchResultDto<ProductSearchResultItemDto>>.Success(expected));

        // Act
        var result = await _controller.SearchWithFuzzy(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public void SearchController_HasRouteAttribute()
    {
        var routeAttr = typeof(SearchController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/search");
    }

    [Theory]
    [InlineData(nameof(SearchController.SearchProducts), "products")]
    [InlineData(nameof(SearchController.SearchGlobal), "global")]
    [InlineData(nameof(SearchController.GetSuggestions), "suggestions")]
    [InlineData(nameof(SearchController.SearchWithFuzzy), "products/fuzzy")]
    public void Actions_HaveExpectedHttpGetTemplate(string methodName, string expectedTemplate)
    {
        var method = typeof(SearchController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var httpGet = method!.GetCustomAttributes(typeof(HttpGetAttribute), false)
            .OfType<HttpGetAttribute>()
            .SingleOrDefault();
        httpGet.ShouldNotBeNull();
        httpGet!.Template.ShouldBe(expectedTemplate);
    }
}
