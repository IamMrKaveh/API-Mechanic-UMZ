using Application.Common.Interfaces;

namespace Application.Common.Behaviors;

public interface IIdempotentCommand
{
    Guid IdempotencyKey { get; }
}

public sealed class IdempotencyBehavior<TRequest, TResponse>(
    IIdempotencyService idempotencyService) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentCommand
    where TResponse : class
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var cached = await idempotencyService.GetResultAsync(request.IdempotencyKey, ct);
        if (cached is not null)
            return System.Text.Json.JsonSerializer.Deserialize<TResponse>(cached)!;

        var response = await next(ct);

        await idempotencyService.MarkAsProcessedAsync(
            request.IdempotencyKey,
            System.Text.Json.JsonSerializer.Serialize(response),
            ct);

        return response;
    }
}