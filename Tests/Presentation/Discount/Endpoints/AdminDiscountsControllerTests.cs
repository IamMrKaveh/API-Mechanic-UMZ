using Application.Discount.Features.Commands.CancelDiscountUsage;
using Application.Discount.Features.Commands.CreateDiscount;
using Application.Discount.Features.Commands.DeleteDiscount;
using Application.Discount.Features.Commands.UpdateDiscount;
using Application.Discount.Features.Queries.GetDiscountById;
using Application.Discount.Features.Queries.GetDiscountInfo;
using Application.Discount.Features.Queries.GetDiscounts;
using Application.Discount.Features.Queries.GetDiscountUsageReport;
using Application.Discount.Features.Shared;
using Domain.Discount.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Discount.Endpoints;
using Presentation.Discount.Requests;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Discount.Endpoints;

public class AdminDiscountsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly AdminDiscountsController _controller;

    public AdminDiscountsControllerTests()
    {
        _controller = new AdminDiscountsController(_mediator, _mapper);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetAll_WithDefaults_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var paged = new PaginatedResult<DiscountCodeDto>
        {
            Items = [new DiscountCodeDto { Id = Guid.NewGuid(), Code = "SAVE10" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        _mediator.Send(Arg.Any<GetDiscountsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<DiscountCodeDto>>.Success(paged));

        // Act
        var result = await _controller.GetAll();

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<DiscountCodeDto>>>();
        body.Success.ShouldBeTrue();
        body.Data!.Items.ShouldHaveSingleItem();
        await _mediator.Received(1).Send(
            Arg.Is<GetDiscountsQuery>(q => !q.IncludeExpired && !q.IncludeDeleted && q.Page == 1 && q.PageSize == 20),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_WithValidId_SendsQueryWithId_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new DiscountCodeDetailDto { Id = id, Code = "SAVE10" };

        _mediator.Send(Arg.Is<GetDiscountByIdQuery>(q => q.Id == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<DiscountCodeDetailDto?>.Success(expected));

        // Act
        var result = await _controller.GetById(id);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task GetUsageReport_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new DiscountUsageReportDto { DiscountCodeId = id, Code = "SAVE10", TotalUsages = 5 };

        _mediator.Send(Arg.Is<GetDiscountUsageReportQuery>(q => q.DiscountCodeId == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<DiscountUsageReportDto?>.Success(expected));

        // Act
        var result = await _controller.GetUsageReport(id);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<DiscountUsageReportDto>>();
        body.Success.ShouldBeTrue();
        body.Data!.TotalUsages.ShouldBe(5);
    }

    [Fact]
    public async Task GetDiscountInfo_WithCode_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var expected = new DiscountInfoDto { Code = "SAVE10", IsRedeemable = true };

        _mediator.Send(Arg.Is<GetDiscountInfoQuery>(q => q.Code == "SAVE10"), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<DiscountInfoDto>.Success(expected));

        // Act
        var result = await _controller.GetDiscountInfo("SAVE10");

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<DiscountInfoDto>>();
        body.Success.ShouldBeTrue();
        body.Data!.Code.ShouldBe("SAVE10");
    }

    [Fact]
    public async Task Create_MapsRequestToCommand_AndReturnsCreated()
    {
        // Arrange
        var request = new CreateDiscountRequest("SAVE10", "Percentage", 10, null, 100, null, null);
        var command = new CreateDiscountCommand("SAVE10", DiscountType.Percentage, 10, null, 100, null, null, null);
        var expected = new DiscountDto { Id = Guid.NewGuid(), Code = "SAVE10" };

        _mapper.Map<CreateDiscountCommand>(request).Returns(command);
        _mediator.Send(Arg.Any<CreateDiscountCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<DiscountDto>.Success(expected));

        // Act
        var result = await _controller.Create(request);

        // Assert
        var created = result.ShouldBeOfType<ObjectResult>();
        created.StatusCode.ShouldBe(StatusCodes.Status201Created);
        var body = created.Value.ShouldBeOfType<ApiResponse<DiscountDto>>();
        body.Success.ShouldBeTrue();
        body.Data!.Code.ShouldBe("SAVE10");
    }

    [Fact]
    public async Task Update_MapsRequestToCommandWithRouteId_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateDiscountRequest("Percentage", 15, null, 100, null, null, true);
        var mapped = new UpdateDiscountCommand(Guid.Empty, DiscountType.Percentage, 15, null, 100, null, null, true);
        var expected = new DiscountDto { Id = id, Code = "SAVE10", DiscountValue = 15 };

        _mapper.Map<UpdateDiscountCommand>(request).Returns(mapped);
        _mediator.Send(Arg.Any<UpdateDiscountCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<DiscountDto>.Success(expected));

        // Act
        var result = await _controller.Update(id, request);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdateDiscountCommand>(c => c.Id == id && c.Value == 15 && c.IsActive),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeleteDiscountCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.Delete(id);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DeleteDiscountCommand>(c => c.Id == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelDiscountUsage_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var request = new CancelDiscountUsageRequest(orderId);

        _mediator.Send(Arg.Any<CancelDiscountUsageCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.CancelDiscountUsage(id, request);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<CancelDiscountUsageCommand>(c => c.DiscountCodeId == id && c.OrderId == orderId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_WhenNotFound_MapsToNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<GetDiscountByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<DiscountCodeDetailDto?>.NotFound());

        // Act
        var result = await _controller.GetById(id);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void AdminDiscountsController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminDiscountsController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminDiscountsController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminDiscountsController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/discounts");
    }

    [Theory]
    [InlineData(nameof(AdminDiscountsController.GetAll), null)]
    [InlineData(nameof(AdminDiscountsController.GetById), "{id:guid}")]
    [InlineData(nameof(AdminDiscountsController.GetUsageReport), "{id:guid}/usage-report")]
    [InlineData(nameof(AdminDiscountsController.GetDiscountInfo), "codes/{code}")]
    [InlineData(nameof(AdminDiscountsController.Create), null)]
    [InlineData(nameof(AdminDiscountsController.Update), "{id:guid}")]
    [InlineData(nameof(AdminDiscountsController.Delete), "{id:guid}")]
    [InlineData(nameof(AdminDiscountsController.CancelDiscountUsage), "{id:guid}/usage")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(AdminDiscountsController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
