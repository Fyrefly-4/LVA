using MyAdmin.Core.Common;
using MyAdmin.Core.Dtos;

namespace MyAdmin.Service.Interfaces;

public interface IRoleService
{
    Task<PagedResult<RoleDto>> GetListAsync(int? pageIndex, int? pageSize);
    Task<List<RoleDto>> GetAllAsync();
    Task CreateAsync(RoleSaveDto dto);
    Task UpdateAsync(int id, RoleSaveDto dto);
    Task DeleteAsync(int id);
}
