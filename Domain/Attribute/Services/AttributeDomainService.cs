using Domain.Attribute.Aggregates;
using Domain.Attribute.Exceptions;
using Domain.Attribute.Interfaces;
using SharedKernel.Results;

namespace Domain.Attribute.Services;

public sealed class AttributeDomainService(IAttributeRepository repository)
{
    private readonly IAttributeRepository _repository = repository;

    public async Task<Result> ValidateDuplicateAttributeNameAsync(string name, CancellationToken ct = default)
    {
        if (await _repository.AttributeTypeExistsAsync(name, null, ct))
            return Result.Failure(new Error("Attribute.Duplicate", $"ویژگی با نام '{name}' قبلاً ثبت شده است."));

        return Result.Success();
    }
}