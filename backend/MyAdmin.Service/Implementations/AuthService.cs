using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyAdmin.Core.Common;
using MyAdmin.Core.Dtos;
using MyAdmin.Infrastructure;
using MyAdmin.Service.Helpers;
using MyAdmin.Service.Interfaces;

namespace MyAdmin.Service.Implementations;

public class AuthService : IAuthService
{
    private readonly MyAdminDbContext _dbContext;
    private readonly JwtSettings _jwtSettings;

    public AuthService(MyAdminDbContext dbContext, IOptions<JwtSettings> jwtOptions)
    {
        _dbContext = dbContext;
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _dbContext.SysUsers
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == dto.Username);

        if (user == null || user.Status != 1)
            throw new UnauthorizedAccessException("Username or password is incorrect.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Username or password is incorrect.");

        var roleCodes = user.UserRoles
            .Select(ur => ur.Role.RoleCode)
            .Distinct()
            .OrderBy(code => code)
            .ToList();

        var token = GenerateToken(user, roleCodes);
        return new LoginResponseDto { Token = token };
    }

    public async Task<UserInfoDto> GetCurrentUserInfoAsync(int userId)
    {
        var user = await _dbContext.SysUsers
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new UnauthorizedAccessException("User does not exist or is inactive.");

        // 汇总目录/菜单/按钮全链路权限码（含 MenuType = 2 按钮级 PermCode），供前端 v-has-perm 使用
        var permissions = await (
            from userRole in _dbContext.SysUserRoles
            join roleMenu in _dbContext.SysRoleMenus on userRole.RoleId equals roleMenu.RoleId
            join menu in _dbContext.SysMenus on roleMenu.MenuId equals menu.Id
            where userRole.UserId == userId
                  && menu.Status == 1
                  && !string.IsNullOrWhiteSpace(menu.PermCode)
            select menu.PermCode!)
            .Distinct()
            .OrderBy(code => code)
            .ToListAsync();

        return new UserInfoDto
        {
            UserId = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Roles = user.UserRoles
                .Select(ur => ur.Role.RoleCode)
                .Distinct()
                .OrderBy(code => code)
                .ToList(),
            Permissions = permissions
        };
    }

    public async Task<List<MenuTreeDto>> GetCurrentUserMenusAsync(int userId)
    {
        var roleMenus = await (
            from userRole in _dbContext.SysUserRoles
            join roleMenu in _dbContext.SysRoleMenus on userRole.RoleId equals roleMenu.RoleId
            join menu in _dbContext.SysMenus on roleMenu.MenuId equals menu.Id
            where userRole.UserId == userId
                  && menu.Status == 1
                  && (menu.MenuType == 0 || menu.MenuType == 1)
            orderby menu.Sort, menu.Id
            select new MenuTreeDto
            {
                Id = menu.Id,
                ParentId = menu.ParentId,
                Title = menu.Title,
                Path = menu.Path,
                Component = menu.Component,
                PermCode = menu.PermCode,
                MenuType = menu.MenuType,
                Icon = menu.Icon,
                Sort = menu.Sort
            })
            .ToListAsync();

        // JWT 基础自助模块：文献大厅、个人文献中心（及业务中台父目录）向所有合法用户无条件放行
        var jwtBaselineMenus = await _dbContext.SysMenus
            .Where(m => m.Status == 1
                        && (m.MenuType == 0 || m.MenuType == 1)
                        && (m.Title == "文献大厅"
                            || m.Title == "个人文献中心"
                            || (m.MenuType == 0 && m.Path == "/business")))
            .OrderBy(m => m.Sort)
            .ThenBy(m => m.Id)
            .Select(m => new MenuTreeDto
            {
                Id = m.Id,
                ParentId = m.ParentId,
                Title = m.Title,
                Path = m.Path,
                Component = m.Component,
                PermCode = m.PermCode,
                MenuType = m.MenuType,
                Icon = m.Icon,
                Sort = m.Sort
            })
            .ToListAsync();

        return roleMenus
            .Concat(jwtBaselineMenus)
            .GroupBy(menu => menu.Id)
            .Select(group => group.First())
            .OrderBy(menu => menu.Sort)
            .ThenBy(menu => menu.Id)
            .ToList();
    }

    private string GenerateToken(Core.Entities.SysUser user, List<string> roleCodes)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("nickname", user.Nickname)
        };

        claims.AddRange(roleCodes.Select(code => new Claim(ClaimTypes.Role, code)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
