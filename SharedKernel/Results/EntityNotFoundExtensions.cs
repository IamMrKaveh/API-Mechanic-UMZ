namespace SharedKernel.Results;

/// <summary>
/// Shared NotFound guard for entity/DTO fetches in Application handlers.
/// Replaces the repeated pattern:
/// <code>
/// var entity = await repository.GetByIdAsync(id, ct);
/// if (entity is null)
///     return ServiceResult.NotFound("...");
/// </code>
/// with a single expression:
/// <code>
/// var entityResult = await repository.GetByIdAsync(id, ct).OrNotFoundAsync("...");
/// if (entityResult.IsFailure) return ServiceResult.Failure(entityResult.Error);
/// var entity = entityResult.Value;
/// </code>
/// or for query handlers returning <see cref="ServiceResult{T}"/>:
/// <code>
/// return (await queryService.GetByIdAsync(id, ct)).ToResultOrNotFound("...");
/// </code>
/// </summary>
public static class EntityNotFoundExtensions
{
    /// <summary>
    /// Converts a possibly-null fetched value into a <see cref="ServiceResult{T}"/>.
    /// Null becomes NotFound; non-null becomes Success.
    /// </summary>
    public static ServiceResult<T> ToResultOrNotFound<T>(this T? entity, string message)
        where T : class
        => entity is null
            ? ServiceResult<T>.NotFound(message)
            : ServiceResult<T>.Success(entity);

    /// <summary>
    /// Converts a possibly-null fetched value into a <see cref="ServiceResult{T}"/>,
    /// treating the value as missing when it is null or when <paramref name="isMissing"/> returns true
    /// (e.g. soft-deleted or inactive entities: <c>e => e.IsDeleted</c>).
    /// </summary>
    public static ServiceResult<T> ToResultOrNotFound<T>(this T? entity, Func<T, bool> isMissing, string message)
        where T : class
        => entity is null || isMissing(entity)
            ? ServiceResult<T>.NotFound(message)
            : ServiceResult<T>.Success(entity);

    /// <summary>
    /// Awaits a fetch task (e.g. <c>repository.GetByIdAsync(...)</c>) and converts
    /// a null result into NotFound. Works with any repository or query-service method
    /// returning <c>Task{T?}</c>, so no per-repository base interface is required.
    /// </summary>
    public static async Task<ServiceResult<T>> OrNotFoundAsync<T>(this Task<T?> fetchTask, string message)
        where T : class
    {
        var entity = await fetchTask.ConfigureAwait(false);
        return entity.ToResultOrNotFound(message);
    }

    /// <summary>
    /// Awaits a fetch task and converts a null result — or a result satisfying
    /// <paramref name="isMissing"/> (e.g. <c>v => v.IsDeleted</c>) — into NotFound.
    /// Covers patterns like <c>if (x is null || x.IsDeleted) return NotFound(...)</c>.
    /// </summary>
    public static async Task<ServiceResult<T>> OrNotFoundAsync<T>(
        this Task<T?> fetchTask,
        Func<T, bool> isMissing,
        string message)
        where T : class
    {
        var entity = await fetchTask.ConfigureAwait(false);
        return entity.ToResultOrNotFound(isMissing, message);
    }

    /// <summary>
    /// Projects a <see cref="ServiceResult{T}"/> failure into a non-generic
    /// <see cref="ServiceResult"/> without rebuilding the error.
    /// Use in handlers returning <see cref="ServiceResult"/>:
    /// <code>
    /// if (entityResult.IsFailure) return entityResult.ToServiceResult();
    /// </code>
    /// </summary>
    public static ServiceResult ToServiceResult<T>(this ServiceResult<T> result)
        => result.IsFailure ? ServiceResult.Failure(result.Error) : ServiceResult.Success();
}
