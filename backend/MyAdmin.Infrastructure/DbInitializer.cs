using Microsoft.EntityFrameworkCore;
using MyAdmin.Core.Entities;

namespace MyAdmin.Infrastructure;

public static class DbInitializer
{
    public static async Task SeedAsync(MyAdminDbContext context)
    {
        await context.Database.MigrateAsync();

        if (await context.SysRoles.AnyAsync())
            return;

        var adminRole = new SysRole
        {
            RoleName = "管理员",
            RoleCode = "admin",
            Description = "系统高权限",
            CreateTime = DateTime.Now
        };

        var userRole = new SysRole
        {
            RoleName = "普通用户",
            RoleCode = "user",
            Description = "普通业务用户",
            CreateTime = DateTime.Now
        };

        context.SysRoles.AddRange(adminRole, userRole);
        await context.SaveChangesAsync();

        var adminUser = new SysUser
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            Nickname = "超级管理员",
            Email = "admin@example.com",
            Status = 1,
            CreateTime = DateTime.Now
        };

        context.SysUsers.Add(adminUser);
        await context.SaveChangesAsync();

        context.SysUserRoles.Add(new SysUserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });

        await context.SaveChangesAsync();
    }
}
