using Microsoft.EntityFrameworkCore;
using MyAdmin.Core.Common;
using MyAdmin.Core.Dtos;
using MyAdmin.Core.Entities;
using MyAdmin.Infrastructure;
using MyAdmin.Service.Helpers;
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

    public async Task<RolePermissionDto> GetRolePermissionsAsync(int roleId)
    {
        var roleExists = await _dbContext.SysRoles.AnyAsync(r => r.Id == roleId);
        if (!roleExists)
            throw new InvalidOperationException("角色不存在");

        var allFlatMenus = await QueryEnabledMenuDtosAsync();

        // 权限配置树仅展示需 HasPermission 鉴权的模块，排除 JWT 基础自助模块
        var permissionFlatMenus = JwtBaselineMenuHelper.ExcludeFromPermissionTree(allFlatMenus);

        var checkedMenuIds = await _dbContext.SysRoleMenus
            .Where(rm => rm.RoleId == roleId)
            .Select(rm => rm.MenuId)
            .OrderBy(menuId => menuId)
            .ToListAsync();

        return new RolePermissionDto
        {
            AllMenus = MenuTreeBuilder.Build(permissionFlatMenus),
            CheckedMenuIds = JwtBaselineMenuHelper.FilterCheckedMenuIds(allFlatMenus, checkedMenuIds)
        };
    }

    public async Task SaveRolePermissionsAsync(int roleId, List<int> menuIds)
    {
        var roleExists = await _dbContext.SysRoles.AnyAsync(r => r.Id == roleId);
        if (!roleExists)
            throw new InvalidOperationException("角色不存在");

        var allFlatMenus = await QueryEnabledMenuDtosAsync();
        var jwtBaselineMenuIds = JwtBaselineMenuHelper.CollectSubtreeIds(allFlatMenus, JwtBaselineMenuHelper.BaselineRootTitles);

        // 前端提交的勾选不含 JWT 基础模块，忽略误入的 Id
        var editableMenuIds = (menuIds ?? new List<int>())
            .Distinct()
            .Where(id => !jwtBaselineMenuIds.Contains(id))
            .ToList();

        if (editableMenuIds.Count > 0)
        {
            var validMenuIds = await _dbContext.SysMenus
                .Where(m => m.Status == 1 && editableMenuIds.Contains(m.Id))
                .Select(m => m.Id)
                .ToListAsync();

            if (validMenuIds.Count != editableMenuIds.Count)
                throw new InvalidOperationException("存在无效或已禁用的菜单/按钮 ID");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var existing = await _dbContext.SysRoleMenus
                .Where(rm => rm.RoleId == roleId)
                .ToListAsync();

            // 保留角色上已有的 JWT 基础模块绑定，避免保存权限时误删
            var preservedJwtBaselineIds = existing
                .Where(rm => jwtBaselineMenuIds.Contains(rm.MenuId))
                .Select(rm => rm.MenuId)
                .ToList();

            var finalMenuIds = editableMenuIds
                .Union(preservedJwtBaselineIds)
                .Distinct()
                .ToList();

            if (existing.Count > 0)
                _dbContext.SysRoleMenus.RemoveRange(existing);

            if (finalMenuIds.Count > 0)
            {
                var newBindings = finalMenuIds.Select(menuId => new SysRoleMenu
                {
                    RoleId = roleId,
                    MenuId = menuId
                });
                await _dbContext.SysRoleMenus.AddRangeAsync(newBindings);
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<List<MenuTreeDto>> QueryEnabledMenuDtosAsync()
    {
        return await _dbContext.SysMenus
            .Where(m => m.Status == 1)
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
    }
}
