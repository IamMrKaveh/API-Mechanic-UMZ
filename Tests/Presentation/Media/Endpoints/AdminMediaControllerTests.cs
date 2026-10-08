using Application.Media.Features.Commands.CleanupOrphanedMedia;
using Application.Media.Features.Commands.DeleteMedia;
using Application.Media.Features.Commands.ReorderMedia;
using Application.Media.Features.Commands.SetPrimaryMedia;
using Application.Media.Features.Commands.UploadMedia;
using Application.Media.Features.Queries.GetAllMedia;
using Application.Media.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Media.Endpoints;
using Presentation.Media.Requests;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Media.Endpoints;

public class AdminMediaControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly AdminMediaController _controller;

    public AdminMediaControllerTests()
    {
        _controller = new AdminMediaController(_mediator, _mapper);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetAllMedia_MapsRequestToQuery_AndReturnsOk()
    {
        // Arrange
        var request = new GetAllMediaRequest("Product", 1, 20);
        var query = new GetAllMediaQuery("Product", 1, 10);
        var paged = new PaginatedResult<MediaDto>
        {
            Items = [new MediaDto { Id = Guid.NewGuid(), FileName = "a.jpg" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        _mapper.Map<GetAllMediaQuery>(request).Returns(query);
        _mediator.Send(Arg.Any<GetAllMediaQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<MediaDto>>.Success(paged));

        // Act
        var result = await _controller.GetAllMedia(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<MediaDto>>>();
        body.Success.ShouldBeTrue();
        body.Data!.Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task UploadMedia_WithValidFile_BuildsCommand_AndReturnsCreated()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var file = Substitute.For<IFormFile>();
        file.OpenReadStream().Returns(new MemoryStream([1, 2, 3]));
        file.FileName.Returns("a.jpg");
        file.ContentType.Returns("image/jpeg");
        file.Length.Returns(3);
        var request = new UploadMediaRequest
        {
            File = file,
            EntityType = "Product",
            EntityId = entityId,
            IsPrimary = true,
            AltText = "alt"
        };
        var expected = new MediaDto { Id = Guid.NewGuid(), FileName = "a.jpg" };

        _mediator.Send(Arg.Any<UploadMediaCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<MediaDto>.Success(expected));

        // Act
        var result = await _controller.UploadMedia(request, CancellationToken.None);

        // Assert
        var created = result.ShouldBeOfType<ObjectResult>();
        created.StatusCode.ShouldBe(StatusCodes.Status201Created);
        var body = created.Value.ShouldBeOfType<ApiResponse<MediaDto>>();
        body.Success.ShouldBeTrue();
        body.Data!.FileName.ShouldBe("a.jpg");
        await _mediator.Received(1).Send(
            Arg.Is<UploadMediaCommand>(c =>
                c.FileName == "a.jpg" &&
                c.EntityType == "Product" &&
                c.EntityId == entityId &&
                c.IsPrimary),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CleanupOrphaned_SendsCommand_AndReturnsOk()
    {
        // Arrange
        _mediator.Send(Arg.Any<CleanupOrphanedMediaCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<int>.Success(5));

        // Act
        var result = await _controller.CleanupOrphaned(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<int>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldBe(5);
        await _mediator.Received(1).Send(Arg.Any<CleanupOrphanedMediaCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteMedia_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var mediaId = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeleteMediaCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.DeleteMedia(mediaId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DeleteMediaCommand>(c => c.MediaId == mediaId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetPrimaryMedia_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        var request = new SetPrimaryMediaRequest(mediaId);

        _mediator.Send(Arg.Any<SetPrimaryMediaCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.SetPrimaryMedia(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<SetPrimaryMediaCommand>(c => c.MediaId == mediaId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReorderMedia_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var orderedIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var request = new ReorderMediaRequest("Product", entityId, orderedIds);

        _mediator.Send(Arg.Any<ReorderMediaCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.ReorderMedia(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ReorderMediaCommand>(c =>
                c.EntityType == "Product" &&
                c.EntityId == entityId &&
                c.OrderedIds.SequenceEqual(orderedIds)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteMedia_WhenNotFound_MapsToNotFound()
    {
        // Arrange
        var mediaId = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeleteMediaCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.NotFound());

        // Act
        var result = await _controller.DeleteMedia(mediaId, CancellationToken.None);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void AdminMediaController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminMediaController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminMediaController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminMediaController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/media");
    }

    [Theory]
    [InlineData(nameof(AdminMediaController.GetAllMedia), null)]
    [InlineData(nameof(AdminMediaController.UploadMedia), null)]
    [InlineData(nameof(AdminMediaController.CleanupOrphaned), "orphaned")]
    [InlineData(nameof(AdminMediaController.DeleteMedia), "{mediaId:guid}")]
    [InlineData(nameof(AdminMediaController.SetPrimaryMedia), "primary")]
    [InlineData(nameof(AdminMediaController.ReorderMedia), "order")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(AdminMediaController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
