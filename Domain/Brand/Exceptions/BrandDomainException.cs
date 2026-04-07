using Domain.Brand.ValueObjects;
using Domain.Common.Exceptions;

namespace Domain.Brand.Exceptions;

public sealed class BrandNotFoundException : DomainException
{
    public BrandId BrandId { get; }

    public override string ErrorCode => "BRAND_NOT_FOUND";

    public BrandNotFoundException(BrandId brandId)
        : base($"برند با شناسه {brandId} یافت نشد.")
    {
        BrandId = brandId;
    }
}

public sealed class BrandNameAlreadyExistsException : DomainException
{
    public BrandName Name { get; }

    public override string ErrorCode => "BRAND_NAME_ALREADY_EXISTS";

    public BrandNameAlreadyExistsException(BrandName name)
        : base($"برند با نام '{name}' قبلاً وجود دارد.")
    {
        Name = name;
    }
}

public sealed class BrandAlreadyActiveException : DomainException
{
    public BrandId BrandId { get; }

    public override string ErrorCode => "BRAND_ALREADY_ACTIVE";

    public BrandAlreadyActiveException(BrandId brandId)
        : base($"برند با شناسه {brandId} در حال حاضر فعال است.")
    {
        BrandId = brandId;
    }
}

public sealed class BrandAlreadyDeactivatedException : DomainException
{
    public BrandId BrandId { get; }

    public override string ErrorCode => "BRAND_ALREADY_DEACTIVATED";

    public BrandAlreadyDeactivatedException(BrandId brandId)
        : base($"برند با شناسه {brandId} در حال حاضر غیرفعال است.")
    {
        BrandId = brandId;
    }
}

public sealed class DeletedBrandMutationException : DomainException
{
    public BrandId BrandId { get; }

    public override string ErrorCode => "DELETED_BRAND_MUTATION";

    public DeletedBrandMutationException(BrandId brandId)
        : base($"امکان تغییر برند حذف شده با شناسه {brandId} وجود ندارد.")
    {
        BrandId = brandId;
    }
}