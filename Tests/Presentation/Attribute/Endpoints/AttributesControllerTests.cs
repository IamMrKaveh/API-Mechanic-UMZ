using Application.Attribute.Features.Queries.GetAllAttributeTypes;
using Application.Attribute.Features.Queries.GetAttributeTypeById;
using Application.Attribute.Features.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attribute.Endpoints;
using SharedKernel.Models;
using IQueryContract = Application.Common.Interfaces.IQuery<Application.Attribute.Features.Shared.AttributeTypeDto>;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;


namespace Tests.Presentation.Attribute.Endpoints;

public class AttributesControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly AttributesController _sut;

    public AttributesControllerTests()
    {
        _sut = new AttributesController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _sut.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }


    [Fact]
    public void AttributesController_IsPubliclyReadable()
    {
        typeof(AttributesController)
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), false)
            .ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetAttributeTypes_SendsThePagedQuery()
    {
        _mediator
            .Send(Arg.Any<GetAllAttributeTypesQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<AttributeTypeDto>>.Success(
                PaginatedResult<AttributeTypeDto>.Create([], 0, 1, 20)));

        var result = await _sut.GetAttributeTypes(page: 2, pageSize: 20, CancellationToken.None);

        result.ShouldBeOfType<OkObjectResult>();
        await _mediator.Received(1).Send(
            Arg.Is<GetAllAttributeTypesQuery>(q => q!.Page == 2 && q.PageSize == 20),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAttributeType_SendsTheQuery()
    {
        var id = Guid.NewGuid();
        _mediator
            .Send(Arg.Any<GetAttributeTypeByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AttributeTypeDto>.Success(new AttributeTypeDto { Id = id }));

        var result = await _sut.GetAttributeType(id, CancellationToken.None);

        result.ShouldBeOfType<OkObjectResult>();
        await _mediator.Received(1).Send(
            Arg.Is<GetAttributeTypeByIdQuery>(q => q!.Id == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void GetAttributeTypeByIdQuery_IsAQueryAndNotACommand()
    {
        typeof(GetAttributeTypeByIdQuery)
            .GetInterfaces()
            .ShouldContain(typeof(IQueryContract));
    }
}
