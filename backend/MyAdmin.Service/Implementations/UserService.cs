using Microsoft.EntityFrameworkCore;
using MyAdmin.Core.Common;
using MyAdmin.Core.Dtos;
using MyAdmin.Core.Entities;
using MyAdmin.Infrastructure;
using MyAdmin.Service.Interfaces;

namespace MyAdmin.Service.Implementations;

public class UserService : IUserService
{
    private readonly MyAdminDbContext _dbContext;

    public UserService(MyAdminDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<UserDto>> GetListAsync(int pageIndex, int pageSize, string? keyword)
    {
        pageIndex = pageIndex < 1 ? 1 : pageIndex;
        pageSize = pageSize < 1 ? 10 : pageSize;

        var query = _dbContext.SysUsers
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(u =>
                u.Username.Contains(keyword) ||
                u.Nickname.Contains(keyword) ||
                (u.Email != null && u.Email.Contains(keyword)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(u => u.CreateTime)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Nickname = u.Nickname,
                Email = u.Email,
                Status = u.Status,
                Roles = u.UserRoles.Select(ur => ur.Role.RoleCode).ToList(),
                CreateTime = u.CreateTime.ToString("yyyy-MM-dd HH:mm:ss")
            })
            .ToListAsync();

        return new PagedResult<UserDto> { Total = total, Items = items };
    }

    public async Task CreateAsync(UserSaveDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username))
            throw new InvalidOperationException("用户名不能为空");

        if (string.IsNullOrWhiteSpace(dto.Password))
            throw new InvalidOperationException("密码不能为空");

        if (await _dbContext.SysUsers.AnyAsync(u => u.Username == dto.Username))
            throw new InvalidOperationException("用户名已存在");

        await ValidateRoleIdsAsync(dto.RoleIds);

        var user = new SysUser
        {
            Username = dto.Username.Trim(),
            Nickname = dto.Nickname.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Email = dto.Email,
            Status = dto.Status,
            CreateTime = DateTime.Now
        };

        _dbContext.SysUsers.Add(user);
        await _dbContext.SaveChangesAsync();

        await SyncUserRolesAsync(user.Id, dto.RoleIds);
    }

    public async Task UpdateAsync(int id, UserSaveDto dto)
    {
        var user = await _dbContext.SysUsers.FindAsync(id)
            ?? throw new InvalidOperationException("用户不存在");

        user.Nickname = dto.Nickname.Trim();
        user.Email = dto.Email;
        user.Status = dto.Status;

        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        await ValidateRoleIdsAsync(dto.RoleIds);
        await SyncUserRolesAsync(id, dto.RoleIds);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var user = await _dbContext.SysUsers.FindAsync(id)
            ?? throw new InvalidOperationException("用户不存在");

        _dbContext.SysUsers.Remove(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task BatchDeleteAsync(IEnumerable<int> ids)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
            return;

        var users = await _dbContext.SysUsers
            .Where(u => idList.Contains(u.Id))
            .ToListAsync();

        _dbContext.SysUsers.RemoveRange(users);
        await _dbContext.SaveChangesAsync();
    }

    private async Task ValidateRoleIdsAsync(IEnumerable<int> roleIds)
    {
        var idList = roleIds.Distinct().ToList();
        if (idList.Count == 0)
            throw new InvalidOperationException("至少分配一个角色");

        var existingCount = await _dbContext.SysRoles.CountAsync(r => idList.Contains(r.Id));
        if (existingCount != idList.Count)
            throw new InvalidOperationException("存在无效的角色ID");
    }

    private async Task SyncUserRolesAsync(int userId, IEnumerable<int> roleIds)
    {
        var idList = roleIds.Distinct().ToList();
        var existing = await _dbContext.SysUserRoles
            .Where(ur => ur.UserId == userId)
            .ToListAsync();

        _dbContext.SysUserRoles.RemoveRange(existing);

        foreach (var roleId in idList)
        {
            _dbContext.SysUserRoles.Add(new SysUserRole
            {
                UserId = userId,
                RoleId = roleId
            });
        }

        await _dbContext.SaveChangesAsync();
    }
}
