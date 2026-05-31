using Microsoft.EntityFrameworkCore;
using MyAdmin.Core.Common;
using MyAdmin.Core.Dtos;
using MyAdmin.Core.Entities;
using MyAdmin.Infrastructure;
using MyAdmin.Service.Interfaces;

namespace MyAdmin.Service.Implementations;

public class RoleService : IRoleService
{
    private readonly MyAdminDbContext _dbContext;

    public RoleService(MyAdminDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<RoleDto>> GetListAsync(int? pageIndex, int? pageSize)
    {
        var query = _dbContext.SysRoles.AsQueryable();
        var total = await query.CountAsync();

        if (!pageIndex.HasValue && !pageSize.HasValue)
        {
            var allItems = await query
                .OrderBy(r => r.Id)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    RoleName = r.RoleName,
                    RoleCode = r.RoleCode,
                    Description = r.Description
                })
                .ToListAsync();

            return new PagedResult<RoleDto> { Total = total, Items = allItems };
        }

        var actualPageIndex = pageIndex.GetValueOrDefault(1);
        var actualPageSize = pageSize.GetValueOrDefault(10);
        actualPageIndex = actualPageIndex < 1 ? 1 : actualPageIndex;
        actualPageSize = actualPageSize < 1 ? 10 : actualPageSize;

        var items = await query
            .OrderBy(r => r.Id)
            .Skip((actualPageIndex - 1) * actualPageSize)
            .Take(actualPageSize)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                RoleName = r.RoleName,
                RoleCode = r.RoleCode,
                Description = r.Description
            })
            .ToListAsync();

        return new PagedResult<RoleDto> { Total = total, Items = items };
    }

    public async Task<List<RoleDto>> GetAllAsync()
    {
        return await _dbContext.SysRoles
            .OrderBy(r => r.Id)
            .Select(r => new RoleDto
            {
                Id = r.Id,
                RoleName = r.RoleName,
                RoleCode = r.RoleCode,
                Description = r.Description
            })
            .ToListAsync();
    }

    public async Task CreateAsync(RoleSaveDto dto)
    {
        if (await _dbContext.SysRoles.AnyAsync(r => r.RoleCode == dto.RoleCode))
            throw new InvalidOperationException("角色编码已存在");

        var role = new SysRole
        {
            RoleName = dto.RoleName.Trim(),
            RoleCode = dto.RoleCode.Trim(),
            Description = dto.Description,
            CreateTime = DateTime.Now
        };

        _dbContext.SysRoles.Add(role);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(int id, RoleSaveDto dto)
    {
        var role = await _dbContext.SysRoles.FindAsync(id)
            ?? throw new InvalidOperationException("角色不存在");

        if (await _dbContext.SysRoles.AnyAsync(r => r.RoleCode == dto.RoleCode && r.Id != id))
            throw new InvalidOperationException("角色编码已存在");

        role.RoleName = dto.RoleName.Trim();
        role.RoleCode = dto.RoleCode.Trim();
        role.Description = dto.Description;

        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var role = await _dbContext.SysRoles.FindAsync(id)
            ?? throw new InvalidOperationException("角色不存在");

        _dbContext.SysRoles.Remove(role);
        await _dbContext.SaveChangesAsync();
    }
}
