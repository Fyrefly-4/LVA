using MyAdmin.Core.Dtos;

namespace MyAdmin.Service.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginDto dto);
    Task<UserInfoDto> GetCurrentUserInfoAsync(int userId);
    Task<List<MenuTreeDto>> GetCurrentUserMenusAsync(int userId);
}
