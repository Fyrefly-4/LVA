using MyAdmin.Core.Common;
using MyAdmin.Core.Dtos;

namespace MyAdmin.Service.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetListAsync(int pageIndex, int pageSize, string? keyword);
    Task CreateAsync(UserSaveDto dto);
    Task UpdateAsync(int id, UserSaveDto dto);
    Task DeleteAsync(int id);
    Task BatchDeleteAsync(IEnumerable<int> ids);
}
