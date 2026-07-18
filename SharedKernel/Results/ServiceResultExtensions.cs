using Microsoft.Extensions.Logging;

namespace SharedKernel.Results;

public static class ServiceResultExtensions
{
    public static ServiceResult ThrowIfFailure(this ServiceResult result)
    {
        if (result.IsFailure)
            throw new InvalidOperationException($"[{result.Error.Code}] {result.Error.Message}");
        return result;
    }

    public static ServiceResult<T> ThrowIfFailure<T>(this ServiceResult<T> result)
    {
        if (result.IsFailure)
            throw new InvalidOperationException($"[{result.Error.Code}] {result.Error.Message}");
        return result;
    }

    public static ServiceResult LogIfFailure(this ServiceResult result, ILogger logger)
    {
        if (result.IsFailure)
            logger.LogWarning("Operation failed. Code={Code} Type={Type} Message={Message}",
                result.Error.Code, result.Error.Type, result.Error.Message);
        return result;
    }

    public static ServiceResult<T> LogIfFailure<T>(this ServiceResult<T> result, ILogger logger)
    {
        if (result.IsFailure)
            logger.LogWarning("Operation failed. Code={Code} Type={Type} Message={Message}",
                result.Error.Code, result.Error.Type, result.Error.Message);
        return result;
    }

    public static async Task<ServiceResult<TOut>> MapAsync<TIn, TOut>(
        this Task<ServiceResult<TIn>> resultTask,
        Func<TIn, TOut> mapper)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Map(mapper);
    }

    public static async Task<ServiceResult<TOut>> BindAsync<TIn, TOut>(
        this Task<ServiceResult<TIn>> resultTask,
        Func<TIn, Task<ServiceResult<TOut>>> binder)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.IsFailure ? ServiceResult<TOut>.Failure(result.Error) : await binder(result.Value).ConfigureAwait(false);
    }
}