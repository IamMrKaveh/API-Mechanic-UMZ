namespace Application.Common.Interfaces.Admin;

public interface IAdminAttributeService
{
    Task<ServiceResult<IEnumerable<AttributeTypeDto>>> GetAllAttributeTypesAsync();
    Task<ServiceResult<AttributeTypeDto?>> GetAttributeTypeByIdAsync(int id);
    Task<ServiceResult<AttributeTypeDto>> CreateAttributeTypeAsync(CreateAttributeTypeDto dto);
    Task<ServiceResult> UpdateAttributeTypeAsync(int id, UpdateAttributeTypeDto dto);
    Task<ServiceResult> DeleteAttributeTypeAsync(int id);
    Task<ServiceResult<AttributeValueDto>> CreateAttributeValueAsync(int typeId, CreateAttributeValueDto dto);
    Task<ServiceResult> UpdateAttributeValueAsync(int id, UpdateAttributeValueDto dto);
    Task<ServiceResult> DeleteAttributeValueAsync(int id);
}