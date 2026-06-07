using Microsoft.EntityFrameworkCore;
using MyAdmin.Infrastructure;

namespace MyAdmin.Service.Helpers;

public static class UserPermissionHelper
{
    public static async Task<HashSet<string>> GetPermissionCodesAsync(MyAdminDbContext db, int userId)
    {
        var permissions = await (
            from userRole in db.SysUserRoles
            join roleMenu in db.SysRoleMenus on userRole.RoleId equals roleMenu.RoleId
            join menu in db.SysMenus on roleMenu.MenuId equals menu.Id
            where userRole.UserId == userId
                  && menu.Status == 1
                  && !string.IsNullOrWhiteSpace(menu.PermCode)
            select menu.PermCode!)
            .Distinct()
            .ToListAsync();

        return permissions.ToHashSet(StringComparer.Ordinal);
    }

    public static async Task<bool> HasPermissionAsync(MyAdminDbContext db, int userId, string permCode)
    {
        return await (
            from userRole in db.SysUserRoles
            join roleMenu in db.SysRoleMenus on userRole.RoleId equals roleMenu.RoleId
            join menu in db.SysMenus on roleMenu.MenuId equals menu.Id
            where userRole.UserId == userId
                  && menu.Status == 1
                  && menu.PermCode == permCode
            select menu.Id)
            .AnyAsync();
    }
}
