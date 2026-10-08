using Application.User.Features.Commands.ChangeUserRole;
using Application.User.Features.Commands.ChangeUserStatus;
using Application.User.Features.Commands.CreateUser;
using Application.User.Features.Commands.DeleteUser;
using Application.User.Features.Commands.RestoreUser;
using Application.User.Features.Commands.UpdateUser;
using Application.User.Features.Queries.GetAdminUsers;
using Application.User.Features.Queries.GetUserById;
using Application.User.Features.Queries.GetUsers;
using Application.User.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.User.Endpoints;
using Presentation.User.Requests;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.User.Endpoints;

public class AdminUserControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly AdminUserController _controller;

    public AdminUserControllerTests()
    {
        _controller = new AdminUserController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetUsers_WithDefaults_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var paged = new PaginatedResult<UserProfileDto>
        {
            Items = [new UserProfileDto { Id = Guid.NewGuid(), PhoneNumber = "09123456789" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        _mediator.Send(Arg.Any<GetUsersQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<UserProfileDto>>.Success(paged));

        // Act
        var result = await _controller.GetUsers();

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetUsersQuery>(q => !q.IncludeDeleted && q.Page == 1 && q.PageSize == 20),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAdminUsers_WithFilters_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var paged = new PaginatedResult<AdminUserListItemDto>
        {
            Items = [new AdminUserListItemDto { Id = Guid.NewGuid(), PhoneNumber = "09123456789" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        _mediator.Send(Arg.Any<GetAdminUsersQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<AdminUserListItemDto>>.Success(paged));

        // Act
        var result = await _controller.GetAdminUsers(search: "ali", isAdmin: true);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetAdminUsersQuery>(q => q.Search == "ali" && q.IsAdmin == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUser_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new UserProfileDto { Id = id, PhoneNumber = "09123456789" };

        _mediator.Send(Arg.Is<GetUserByIdQuery>(q => q.Id == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<UserProfileDto?>.Success(expected));

        // Act
        var result = await _controller.GetUser(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task CreateUser_WithValidRequest_MapsToCommand_AndReturnsOk()
    {
        // Arrange
        var request = new AdminCreateUserRequest("09123456789", "Ali", "Rezaei", "a@x.com", true);
        var expected = new UserProfileDto { Id = Guid.NewGuid(), PhoneNumber = "09123456789" };

        _mediator.Send(Arg.Any<CreateUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<UserProfileDto>.Success(expected));

        // Act
        var result = await _controller.CreateUser(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<CreateUserCommand>(c => c.PhoneNumber == "09123456789" && c.IsAdmin),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RestoreUser_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<RestoreUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.RestoreUser(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<RestoreUserCommand>(c => c.Id == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateUser_WithValidRequest_MapsToCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateProfileRequest("Ali", "Rezaei");

        _mediator.Send(Arg.Any<UpdateUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.UpdateUser(id, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdateUserCommand>(c => c.Id == id && c.FirstName == "Ali"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteUser_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeleteUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.DeleteUser(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DeleteUserCommand>(c => c.Id == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeUserStatus_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new ChangeUserStatusRequest(false);

        _mediator.Send(Arg.Any<ChangeUserStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.ChangeUserStatus(id, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ChangeUserStatusCommand>(c => c.UserId == id && !c.IsActive),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeUserRole_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new ChangeUserRoleRequest(true);

        _mediator.Send(Arg.Any<ChangeUserRoleCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.ChangeUserRole(id, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ChangeUserRoleCommand>(c => c.UserId == id && c.IsAdmin),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AdminUserController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminUserController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminUserController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminUserController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/users");
    }

    [Theory]
    [InlineData(nameof(AdminUserController.GetUsers), null)]
    [InlineData(nameof(AdminUserController.GetAdminUsers), "rich")]
    [InlineData(nameof(AdminUserController.GetUser), "{id:guid}")]
    [InlineData(nameof(AdminUserController.CreateUser), null)]
    [InlineData(nameof(AdminUserController.RestoreUser), "{id:guid}/restore")]
    [InlineData(nameof(AdminUserController.UpdateUser), "{id:guid}")]
    [InlineData(nameof(AdminUserController.DeleteUser), "{id:guid}")]
    [InlineData(nameof(AdminUserController.ChangeUserStatus), "{id:guid}/status")]
    [InlineData(nameof(AdminUserController.ChangeUserRole), "{id:guid}/role")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(AdminUserController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
