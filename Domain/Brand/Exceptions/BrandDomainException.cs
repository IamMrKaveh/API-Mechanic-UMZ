using Domain.Brand.ValueObjects;

namespace Domain.Brand.Exceptions;

public sealed class BrandNameAlreadyExistsException(BrandName name) : DomainException($"برند با نام '{name}' قبلاً وجود دارد.")
{
    public BrandName Name { get; } = name;

    public override string ErrorCode => "BRAND_NAME_ALREADY_EXISTS";
}

public sealed class BrandAlreadyActiveException(BrandId brandId) : DomainException($"برند با شناسه {brandId} در حال حاضر فعال است.")
{
    public BrandId BrandId { get; } = brandId;

    public override string ErrorCode => "BRAND_ALREADY_ACTIVE";
}

public sealed class BrandAlreadyDeactivatedException(BrandId brandId) : DomainException($"برند با شناسه {brandId} در حال حاضر غیرفعال است.")
{
    public BrandId BrandId { get; } = brandId;

    public override string ErrorCode => "BRAND_ALREADY_DEACTIVATED";
}