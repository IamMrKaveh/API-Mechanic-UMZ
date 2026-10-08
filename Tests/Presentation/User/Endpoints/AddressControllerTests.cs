using Application.User.Features.Commands.CreateUserAddress;
using Application.User.Features.Commands.DeleteUserAddress;
using Application.User.Features.Commands.UpdateUserAddress;
using Application.User.Features.Queries.GetUserAddresses;
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
using SharedKernel.Results;

namespace Tests.Presentation.User.Endpoints;

public class AddressControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly AddressController _controller;

    public AddressControllerTests()
    {
        _controller = new AddressController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetUserAddresses_SendsQuery_AndReturnsOk()
    {
        // Arrange
        IEnumerable<UserAddressDto> expected =
            [new UserAddressDto { Id = Guid.NewGuid(), Title = "Home" }];

        _mediator.Send(Arg.Any<GetUserAddressesQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IEnumerable<UserAddressDto>>.Success(expected));

        // Act
        var result = await _controller.GetUserAddresses(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<IEnumerable<UserAddressDto>>>();
        body.Data.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task AddAddress_WithValidRequest_MapsToCommand_AndReturnsOk()
    {
        // Arrange
        var request = new CreateUserAddressRequest("Home", "Ali", "09123456789", "Tehran", "Tehran", "Street 1", "12345", true, null, null);
        var expected = new UserAddressDto { Id = Guid.NewGuid(), Title = "Home" };

        _mediator.Send(Arg.Any<CreateUserAddressCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<UserAddressDto>.Success(expected));

        // Act
        var result = await _controller.AddAddress(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<CreateUserAddressCommand>(c =>
                c.Title == "Home" &&
                c.ReceiverName == "Ali" &&
                c.PostalCode == "12345" &&
                c.IsDefault),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAddress_WithValidRequest_MapsToCommandWithRouteId_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateUserAddressRequest("Work", "Sara", "09987654321", "Tehran", "Tehran", "Street 2", "54321", false, null, null);

        _mediator.Send(Arg.Any<UpdateUserAddressCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.UpdateAddress(id, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdateUserAddressCommand>(c => c.AddressId == id && c.Title == "Work"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAddress_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeleteUserAddressCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.DeleteAddress(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DeleteUserAddressCommand>(c => c.AddressId == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AddressController_HasAuthorizeAttribute()
    {
        typeof(AddressController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void AddressController_HasRouteAttribute()
    {
        var routeAttr = typeof(AddressController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/profile/addresses");
    }

    [Theory]
    [InlineData(nameof(AddressController.GetUserAddresses), null)]
    [InlineData(nameof(AddressController.AddAddress), null)]
    [InlineData(nameof(AddressController.UpdateAddress), "{id:guid}")]
    [InlineData(nameof(AddressController.DeleteAddress), "{id:guid}")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(AddressController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
