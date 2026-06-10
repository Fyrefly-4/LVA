using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using MyAdmin.Core.Common;
using MyAdmin.Infrastructure;

namespace MyAdmin.WebApi.Attributes;

public sealed class HasPermissionAttribute : TypeFilterAttribute
{
    public HasPermissionAttribute(string permission)
        : base(typeof(HasPermissionFilter))
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("permission cannot be empty.", nameof(permission));

        Arguments = new object[] { permission };
    }
}

public sealed class HasPermissionFilter : IAsyncActionFilter
{
    private readonly MyAdminDbContext _dbContext;
    private readonly string _permission;

    public HasPermissionFilter(MyAdminDbContext dbContext, string permission)
    {
        _dbContext = dbContext;
        _permission = permission;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        try
        {
            var userIdClaim = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                context.Result = new ObjectResult(ApiResponse<object?>.Fail("Unauthenticated", 401))
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
                return;
            }

            // 匹配 SysMenu.PermCode（含 MenuType = 2 按钮级），与 GetInfo 吐出的 permissions 口径一致
            var hasPermission = await (
                from userRole in _dbContext.SysUserRoles
                join roleMenu in _dbContext.SysRoleMenus on userRole.RoleId equals roleMenu.RoleId
                join menu in _dbContext.SysMenus on roleMenu.MenuId equals menu.Id
                where userRole.UserId == userId
                      && menu.Status == 1
                      && menu.PermCode == _permission
                select menu.Id)
                .AnyAsync();

            if (!hasPermission)
            {
                context.Result = new ObjectResult(ApiResponse<object?>.Fail("Forbidden", 403))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            await next();
        }
        catch (Exception ex)
        {
            context.Result = new ObjectResult(ApiResponse<object?>.Fail(ex.GetBaseException().Message, 500))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
