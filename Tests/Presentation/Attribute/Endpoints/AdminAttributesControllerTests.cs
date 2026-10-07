using Application.Attribute.Features.Commands.CreateAttributeType;
using Application.Attribute.Features.Commands.CreateAttributeValue;
using Application.Attribute.Features.Commands.DeleteAttributeType;
using Application.Attribute.Features.Commands.DeleteAttributeValue;
using Application.Attribute.Features.Commands.UpdateAttributeType;
using Application.Attribute.Features.Commands.UpdateAttributeValue;
using Application.Attribute.Features.Queries.GetAllAttributeTypes;
using Application.Attribute.Features.Queries.GetAttributeTypeById;
using Application.Attribute.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Attribute.Endpoints;
using Presentation.Attribute.Requests;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Attribute.Endpoints;

public class AdminAttributesControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly AdminAttributesController _controller;

    public AdminAttributesControllerTests()
    {
        _controller = new AdminAttributesController(_mediator, _mapper);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetAllAttributeTypes_SendsQuery_AndReturnsOk()
    {
        var expectedDto = new AttributeTypeDto { Id = Guid.NewGuid(), Name = "color", DisplayName = "Color" };
        var paginatedResult = new PaginatedResult<AttributeTypeDto>
        {
            Items = [expectedDto],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mediator.Send(Arg.Any<GetAllAttributeTypesQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<AttributeTypeDto>>.Success(paginatedResult));

        var result = await _controller.GetAllAttributeTypes(CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<AttributeTypeDto>>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldNotBeNull();
        body.Data!.Items.ShouldHaveSingleItem();
        body.Data.Items[0].Name.ShouldBe("color");
        await _mediator.Received(1).Send(Arg.Any<GetAllAttributeTypesQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAttributeType_WithValidId_SendsQueryWithId_AndReturnsOk()
    {
        var attributeId = Guid.NewGuid();
        var expectedDto = new AttributeTypeDto { Id = attributeId, Name = "size", DisplayName = "Size" };

        _mediator.Send(Arg.Is<GetAttributeTypeByIdQuery>(q => q.Id == attributeId), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AttributeTypeDto>.Success(expectedDto));

        var result = await _controller.GetAttributeType(attributeId, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<AttributeTypeDto>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldNotBeNull();
        body.Data!.Id.ShouldBe(attributeId);
    }

    [Fact]
    public async Task CreateAttributeType_WithValidRequest_MapsToCommand_AndReturnsCreated()
    {
        var request = new CreateAttributeTypeRequest("color", "Color", 1);
        var command = new CreateAttributeTypeCommand("color", "Color", 1);
        var expectedDto = new AttributeTypeDto { Id = Guid.NewGuid(), Name = "color", DisplayName = "Color", SortOrder = 1 };

        _mediator.Send(Arg.Is<CreateAttributeTypeCommand>(c =>
            c.Name == request.Name &&
            c.DisplayName == request.DisplayName &&
            c.SortOrder == request.SortOrder), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AttributeTypeDto>.Success(expectedDto));

        var result = await _controller.CreateAttributeType(request, CancellationToken.None);

        var created = result.ShouldBeOfType<ObjectResult>();
        created.StatusCode.ShouldBe(StatusCodes.Status201Created);
        var body = created.Value.ShouldBeOfType<ApiResponse<AttributeTypeDto>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldNotBeNull();
        body.Data!.Name.ShouldBe("color");
    }

    [Fact]
    public async Task CreateAttributeValue_WithValidRequest_MapsToCommand_AndReturnsCreated()
    {
        var typeId = Guid.NewGuid();
        var request = new CreateAttributeValueRequest("red", "Red", "#FF0000", 1);
        var command = new CreateAttributeValueCommand(typeId, "red", "Red", "#FF0000", 1);
        var expectedDto = new AttributeValueDto { Id = Guid.NewGuid(), Value = "red", DisplayValue = "Red", HexCode = "#FF0000", SortOrder = 1 };

        _mediator.Send(Arg.Is<CreateAttributeValueCommand>(c =>
            c.TypeId == typeId &&
            c.Value == request.Value &&
            c.DisplayValue == request.DisplayValue &&
            c.HexCode == request.HexCode &&
            c.SortOrder == request.SortOrder), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AttributeValueDto>.Success(expectedDto));

        var result = await _controller.CreateAttributeValue(typeId, request, CancellationToken.None);

        var created = result.ShouldBeOfType<ObjectResult>();
        created.StatusCode.ShouldBe(StatusCodes.Status201Created);
        var body = created.Value.ShouldBeOfType<ApiResponse<AttributeValueDto>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldNotBeNull();
        body.Data!.Value.ShouldBe("red");
    }

    [Fact]
    public async Task UpdateAttributeType_WithValidRequest_MapsToCommand_AndReturnsOk()
    {
        var attributeId = Guid.NewGuid();
        var request = new UpdateAttributeTypeRequest("color", "Color", 2, true);
        var command = new UpdateAttributeTypeCommand(attributeId, "color", "Color", 2, true);

        _mediator.Send(Arg.Is<UpdateAttributeTypeCommand>(c =>
            c.Id == attributeId &&
            c.Name == request.Name &&
            c.DisplayName == request.DisplayName &&
            c.SortOrder == request.SortOrder &&
            c.IsActive == request.IsActive), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _controller.UpdateAttributeType(attributeId, request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse>();
        body.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateAttributeValue_WithValidRequest_MapsToCommand_AndReturnsOk()
    {
        var valueId = Guid.NewGuid();
        var request = new UpdateAttributeValueRequest("blue", "Blue", "#0000FF", 2, true);
        var command = new UpdateAttributeValueCommand(valueId, "blue", "Blue", "#0000FF", 2, true);

        _mediator.Send(Arg.Is<UpdateAttributeValueCommand>(c =>
            c.Id == valueId &&
            c.Value == request.Value &&
            c.DisplayValue == request.DisplayValue &&
            c.HexCode == request.HexCode &&
            c.SortOrder == request.SortOrder &&
            c.IsActive == request.IsActive), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _controller.UpdateAttributeValue(valueId, request, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse>();
        body.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteAttributeType_WithValidId_SendsCommand_AndReturnsOk()
    {
        var attributeId = Guid.NewGuid();

        _mediator.Send(Arg.Is<DeleteAttributeTypeCommand>(c => c.Id == attributeId), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _controller.DeleteAttributeType(attributeId, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse>();
        body.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteAttributeValue_WithValidId_SendsCommand_AndReturnsOk()
    {
        var valueId = Guid.NewGuid();

        _mediator.Send(Arg.Is<DeleteAttributeValueCommand>(c => c.Id == valueId), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _controller.DeleteAttributeValue(valueId, CancellationToken.None);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse>();
        body.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAttributeType_WhenNotFound_MapsToNotFound()
    {
        var attributeId = Guid.NewGuid();

        _mediator.Send(Arg.Any<GetAttributeTypeByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AttributeTypeDto>.NotFound());

        var result = await _controller.GetAttributeType(attributeId, CancellationToken.None);

        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        var body = notFound.Value.ShouldBeOfType<ApiResponse<AttributeTypeDto>>();
        body.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAttributeType_WhenNotFound_MapsToNotFound()
    {
        var attributeId = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeleteAttributeTypeCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.NotFound());

        var result = await _controller.DeleteAttributeType(attributeId, CancellationToken.None);

        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        var body = notFound.Value.ShouldBeOfType<ApiResponse>();
        body.Success.ShouldBeFalse();
    }

    [Fact]
    public void AdminAttributesController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminAttributesController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminAttributesController_HasApiControllerAttribute()
    {
        typeof(AdminAttributesController).GetCustomAttributes(typeof(ApiControllerAttribute), true)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void AdminAttributesController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminAttributesController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/attributes");
    }

    [Fact]
    public void CreateAttributeType_HasHttpPostAttribute()
    {
        var method = typeof(AdminAttributesController).GetMethod(nameof(AdminAttributesController.CreateAttributeType));
        method.ShouldNotBeNull();
        var httpPost = method!.GetCustomAttributes(typeof(HttpPostAttribute), false)
            .OfType<HttpPostAttribute>()
            .SingleOrDefault();
        httpPost.ShouldNotBeNull();
        httpPost!.Template.ShouldBeNull();
    }

    [Fact]
    public void CreateAttributeValue_HasHttpPostAttribute_WithTypeIdRoute()
    {
        var method = typeof(AdminAttributesController).GetMethod(nameof(AdminAttributesController.CreateAttributeValue));
        method.ShouldNotBeNull();
        var httpPost = method!.GetCustomAttributes(typeof(HttpPostAttribute), false)
            .OfType<HttpPostAttribute>()
            .SingleOrDefault();
        httpPost.ShouldNotBeNull();
        httpPost!.Template.ShouldBe("{typeId:guid}/values");
    }

    [Fact]
    public void UpdateAttributeType_HasHttpPutAttribute_WithIdRoute()
    {
        var method = typeof(AdminAttributesController).GetMethod(nameof(AdminAttributesController.UpdateAttributeType));
        method.ShouldNotBeNull();
        var httpPut = method!.GetCustomAttributes(typeof(HttpPutAttribute), false)
            .OfType<HttpPutAttribute>()
            .SingleOrDefault();
        httpPut.ShouldNotBeNull();
        httpPut!.Template.ShouldBe("{id:guid}");
    }

    [Fact]
    public void UpdateAttributeValue_HasHttpPutAttribute_WithValuesIdRoute()
    {
        var method = typeof(AdminAttributesController).GetMethod(nameof(AdminAttributesController.UpdateAttributeValue));
        method.ShouldNotBeNull();
        var httpPut = method!.GetCustomAttributes(typeof(HttpPutAttribute), false)
            .OfType<HttpPutAttribute>()
            .SingleOrDefault();
        httpPut.ShouldNotBeNull();
        httpPut!.Template.ShouldBe("values/{id:guid}");
    }

    [Fact]
    public void DeleteAttributeType_HasHttpDeleteAttribute_WithIdRoute()
    {
        var method = typeof(AdminAttributesController).GetMethod(nameof(AdminAttributesController.DeleteAttributeType));
        method.ShouldNotBeNull();
        var httpDelete = method!.GetCustomAttributes(typeof(HttpDeleteAttribute), false)
            .OfType<HttpDeleteAttribute>()
            .SingleOrDefault();
        httpDelete.ShouldNotBeNull();
        httpDelete!.Template.ShouldBe("{id:guid}");
    }

    [Fact]
    public void DeleteAttributeValue_HasHttpDeleteAttribute_WithValuesIdRoute()
    {
        var method = typeof(AdminAttributesController).GetMethod(nameof(AdminAttributesController.DeleteAttributeValue));
        method.ShouldNotBeNull();
        var httpDelete = method!.GetCustomAttributes(typeof(HttpDeleteAttribute), false)
            .OfType<HttpDeleteAttribute>()
            .SingleOrDefault();
        httpDelete.ShouldNotBeNull();
        httpDelete!.Template.ShouldBe("values/{id:guid}");
    }
}