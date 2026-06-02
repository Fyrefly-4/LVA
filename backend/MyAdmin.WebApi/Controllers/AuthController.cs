using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyAdmin.Core.Common;
using MyAdmin.Core.Dtos;
using MyAdmin.Service.Interfaces;

namespace MyAdmin.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginDto dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(ApiResponse<LoginResponseDto>.Success(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Ok(ApiResponse<LoginResponseDto>.Fail(ex.Message, 401));
        }
    }

    [HttpGet("info")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserInfoDto>>> GetInfo()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Ok(ApiResponse<UserInfoDto>.Fail("Unauthenticated", 401));

        try
        {
            var result = await _authService.GetCurrentUserInfoAsync(userId);
            return Ok(ApiResponse<UserInfoDto>.Success(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Ok(ApiResponse<UserInfoDto>.Fail(ex.Message, 401));
        }
    }

    [HttpGet("menus")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<MenuTreeDto>>>> GetMenus()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Ok(ApiResponse<List<MenuTreeDto>>.Fail("Unauthenticated", 401));

        try
        {
            var flatMenus = await _authService.GetCurrentUserMenusAsync(userId);
            var tree = BuildMenuTree(flatMenus, null);
            return Ok(ApiResponse<List<MenuTreeDto>>.Success(tree));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Ok(ApiResponse<List<MenuTreeDto>>.Fail(ex.Message, 401));
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public ActionResult<ApiResponse<object?>> Logout()
    {
        return Ok(ApiResponse<object?>.Success(null));
    }

    private static List<MenuTreeDto> BuildMenuTree(IEnumerable<MenuTreeDto> menus, int? parentId)
    {
        return menus
            .Where(menu => menu.ParentId == parentId)
            .OrderBy(menu => menu.Sort)
            .ThenBy(menu => menu.Id)
            .Select(menu => new MenuTreeDto
            {
                Id = menu.Id,
                ParentId = menu.ParentId,
                Title = menu.Title,
                Path = menu.Path,
                Component = menu.Component,
                PermCode = menu.PermCode,
                MenuType = menu.MenuType,
                Icon = menu.Icon,
                Sort = menu.Sort,
                Children = BuildMenuTree(menus, menu.Id)
            })
            .ToList();
    }
}
