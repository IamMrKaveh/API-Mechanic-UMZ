namespace Application.Services.Admin.Product;

public class AdminAttributeService : IAdminAttributeService
{
    private readonly IAttributeRepository _attributeRepository; 
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IUnitOfWork _unitOfWork; 
    private readonly IAppLogger<AdminAttributeService> _logger;

    public AdminAttributeService(
        IAttributeRepository attributeRepository,
        IHtmlSanitizer htmlSanitizer,
        IUnitOfWork unitOfWork,
        IAppLogger<AdminAttributeService> logger)
    {
        _attributeRepository = attributeRepository;
        _htmlSanitizer = htmlSanitizer;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<AttributeTypeDto>>> GetAllAttributeTypesAsync()
    {
        var types = await _attributeRepository.GetAllTypesWithValuesAsync();

        var result = types.Select(t => new AttributeTypeDto
        {
            Id = t.Id,
            Name = t.Name,
            DisplayName = t.DisplayName,
            SortOrder = t.SortOrder,
            IsActive = t.IsActive,
            Values = t.AttributeValues.OrderBy(v => v.SortOrder).Select(v => new AttributeValueSimpleDto
            {
                Id = v.Id,
                Value = v.Value,
                DisplayValue = v.DisplayValue,
                HexCode = v.HexCode
            })
        });

        return ServiceResult<IEnumerable<AttributeTypeDto>>.Ok(result);
    }

    public async Task<ServiceResult<AttributeTypeDto?>> GetAttributeTypeByIdAsync(int id)
    {
        var type = await _attributeRepository.GetTypeWithValuesByIdAsync(id);

        if (type == null)
        {
            return ServiceResult<AttributeTypeDto?>.Fail("Attribute type not found.");
        }

        var dto = new AttributeTypeDto
        {
            Id = type.Id,
            Name = type.Name,
            DisplayName = type.DisplayName,
            SortOrder = type.SortOrder,
            IsActive = type.IsActive,
            Values = type.AttributeValues.OrderBy(v => v.SortOrder).Select(v => new AttributeValueSimpleDto
            {
                Id = v.Id,
                Value = v.Value,
                DisplayValue = v.DisplayValue,
                HexCode = v.HexCode
            })
        };

        return ServiceResult<AttributeTypeDto?>.Ok(dto);
    }

    public async Task<ServiceResult<AttributeTypeDto>> CreateAttributeTypeAsync(CreateAttributeTypeDto dto)
    {
        var sanitizedName = _htmlSanitizer.Sanitize(dto.Name.Trim());
        var sanitizedDisplayName = _htmlSanitizer.Sanitize(dto.DisplayName.Trim());

        if (await _attributeRepository.ExistsTypeNameAsync(sanitizedName))
        {
            return ServiceResult<AttributeTypeDto>.Fail("An attribute type with this name already exists.");
        }

        var attributeType = new AttributeType
        {
            Name = sanitizedName,
            DisplayName = sanitizedDisplayName,
            SortOrder = dto.SortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _attributeRepository.AddTypeAsync(attributeType);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = new AttributeTypeDto
        {
            Id = attributeType.Id,
            Name = attributeType.Name,
            DisplayName = attributeType.DisplayName,
            SortOrder = attributeType.SortOrder,
            IsActive = attributeType.IsActive,
            Values = []
        };

        return ServiceResult<AttributeTypeDto>.Ok(resultDto);
    }

    public async Task<ServiceResult> UpdateAttributeTypeAsync(int id, UpdateAttributeTypeDto dto)
    {
        var attributeType = await _attributeRepository.GetTypeByIdAsync(id);
        if (attributeType == null)
        {
            return ServiceResult.Fail("Attribute type not found.");
        }

        if (!string.IsNullOrEmpty(dto.Name))
        {
            var sanitizedName = _htmlSanitizer.Sanitize(dto.Name.Trim());
            if (await _attributeRepository.ExistsTypeNameAsync(sanitizedName, id))
            {
                return ServiceResult.Fail("An attribute type with this name already exists.");
            }
            attributeType.Name = sanitizedName;
        }

        if (!string.IsNullOrEmpty(dto.DisplayName))
        {
            attributeType.DisplayName = _htmlSanitizer.Sanitize(dto.DisplayName.Trim());
        }

        if (dto.SortOrder.HasValue)
        {
            attributeType.SortOrder = dto.SortOrder.Value;
        }

        if (dto.IsActive.HasValue)
        {
            attributeType.IsActive = dto.IsActive.Value;
        }

        attributeType.UpdatedAt = DateTime.UtcNow;
        _attributeRepository.UpdateType(attributeType);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAttributeTypeAsync(int id)
    {
        var attributeType = await _attributeRepository.GetTypeWithValuesByIdAsync(id);

        if (attributeType == null)
        {
            return ServiceResult.Fail("Attribute type not found.");
        }

        if (await _attributeRepository.IsTypeInUseAsync(id))
        {
            return ServiceResult.Fail("Cannot delete attribute type that is in use by products.");
        }

        attributeType.IsDeleted = true;
        attributeType.DeletedAt = DateTime.UtcNow;

        foreach (var value in attributeType.AttributeValues)
        {
            value.IsDeleted = true;
            value.DeletedAt = DateTime.UtcNow;
        }

        _attributeRepository.UpdateType(attributeType);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<AttributeValueDto>> CreateAttributeValueAsync(int typeId, CreateAttributeValueDto dto)
    {
        var attributeType = await _attributeRepository.GetTypeByIdAsync(typeId);
        if (attributeType == null)
        {
            return ServiceResult<AttributeValueDto>.Fail("Attribute type not found.");
        }

        var sanitizedValue = _htmlSanitizer.Sanitize(dto.Value.Trim());
        var sanitizedDisplayValue = _htmlSanitizer.Sanitize(dto.DisplayValue.Trim());

        if (await _attributeRepository.ExistsValueAsync(typeId, sanitizedValue))
        {
            return ServiceResult<AttributeValueDto>.Fail("An attribute value with this name already exists for this type.");
        }

        var attributeValue = new AttributeValue
        {
            AttributeTypeId = typeId,
            Value = sanitizedValue,
            DisplayValue = sanitizedDisplayValue,
            HexCode = dto.HexCode,
            SortOrder = dto.SortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _attributeRepository.AddValueAsync(attributeValue);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = new AttributeValueDto(
            attributeValue.Id,
            attributeType.Name,
            attributeType.DisplayName,
            attributeValue.Value,
            attributeValue.DisplayValue,
            attributeValue.HexCode
        );

        return ServiceResult<AttributeValueDto>.Ok(resultDto);
    }

    public async Task<ServiceResult> UpdateAttributeValueAsync(int id, UpdateAttributeValueDto dto)
    {
        var attributeValue = await _attributeRepository.GetValueByIdAsync(id);
        if (attributeValue == null)
        {
            return ServiceResult.Fail("Attribute value not found.");
        }

        if (!string.IsNullOrEmpty(dto.Value))
        {
            var sanitizedValue = _htmlSanitizer.Sanitize(dto.Value.Trim());
            if (await _attributeRepository.ExistsValueAsync(attributeValue.AttributeTypeId, sanitizedValue, id))
            {
                return ServiceResult.Fail("An attribute value with this name already exists for this type.");
            }
            attributeValue.Value = sanitizedValue;
        }

        if (!string.IsNullOrEmpty(dto.DisplayValue))
        {
            attributeValue.DisplayValue = _htmlSanitizer.Sanitize(dto.DisplayValue.Trim());
        }

        if (dto.HexCode != null)
        {
            attributeValue.HexCode = dto.HexCode;
        }

        if (dto.SortOrder.HasValue)
        {
            attributeValue.SortOrder = dto.SortOrder.Value;
        }

        if (dto.IsActive.HasValue)
        {
            attributeValue.IsActive = dto.IsActive.Value;
        }

        attributeValue.UpdatedAt = DateTime.UtcNow;
        _attributeRepository.UpdateValue(attributeValue);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAttributeValueAsync(int id)
    {
        var attributeValue = await _attributeRepository.GetValueByIdAsync(id);
        if (attributeValue == null)
        {
            return ServiceResult.Fail("Attribute value not found.");
        }

        if (await _attributeRepository.IsValueInUseAsync(id))
        {
            return ServiceResult.Fail("Cannot delete attribute value that is in use by products.");
        }

        attributeValue.IsDeleted = true;
        attributeValue.DeletedAt = DateTime.UtcNow;
        _attributeRepository.UpdateValue(attributeValue);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok();
    }
}