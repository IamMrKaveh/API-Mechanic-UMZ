using System.Collections.Concurrent;
using System.Reflection;
using Application.Common.Validation;
using FluentValidation.Results;

namespace Application.Common.Behaviors;

/// <summary>
/// Enforces the pagination contract for every request that carries a page and a
/// page size, whether or not the feature has a FluentValidation validator.
///
/// Without this a caller could ask for an unbounded page size and make the
/// application materialise an arbitrary number of rows.
/// </summary>
public sealed class PaginationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ConcurrentDictionary<Type, PaginationShape?> Shapes = new();


    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var shape = Shapes.GetOrAdd(request.GetType(), static type => PaginationShape.Resolve(type));

        if (shape is not null)
        {
            var page = (int)(shape.Page.GetValue(request) ?? 0);
            var pageSize = (int)(shape.PageSize.GetValue(request) ?? 0);

            if (page < 1)
                throw new ValidationException(
                [
                    new ValidationFailure(
                        "Page",
                        "شماره صفحه باید بزرگ‌تر از صفر باشد.")
                ]);

            if (pageSize < 1 || pageSize > PaginationDefaults.MaxPageSize)
                throw new ValidationException(
                [
                    new ValidationFailure(
                        "PageSize",
                        $"حجم صفحه باید بین ۱ و {PaginationDefaults.MaxPageSize} باشد.")
                ]);

        }

        return await next(ct);
    }

    private sealed class PaginationShape
    {
        private PaginationShape(PropertyInfo page, PropertyInfo pageSize)
        {
            Page = page;
            PageSize = pageSize;
        }

        public PropertyInfo Page { get; }
        public PropertyInfo PageSize { get; }

        public static PaginationShape? Resolve(Type type)
        {
            var page = type.GetProperty("Page", BindingFlags.Instance | BindingFlags.Public);
            var pageSize = type.GetProperty("PageSize", BindingFlags.Instance | BindingFlags.Public);

            if (page is null || pageSize is null)
                return null;

            if (page.PropertyType != typeof(int) || pageSize.PropertyType != typeof(int))
                return null;

            if (!page.CanRead || !pageSize.CanRead)
                return null;

            return new PaginationShape(page, pageSize);
        }
    }
}
