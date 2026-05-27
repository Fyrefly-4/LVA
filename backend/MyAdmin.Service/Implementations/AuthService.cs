using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyAdmin.Core.Common;
using MyAdmin.Core.Dtos;
using MyAdmin.Infrastructure;
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
            throw new UnauthorizedAccessException("用户名或密码错误");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("用户名或密码错误");

        var roleCodes = user.UserRoles.Select(ur => ur.Role.RoleCode).ToList();
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
            throw new UnauthorizedAccessException("用户不存在或已失效");

        return new UserInfoDto
        {
            UserId = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Roles = user.UserRoles.Select(ur => ur.Role.RoleCode).ToList()
        };
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
