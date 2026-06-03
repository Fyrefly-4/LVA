using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyAdmin.Core.Common;
using MyAdmin.Core.Dtos;
using MyAdmin.Service.Interfaces;
using MyAdmin.WebApi.Attributes;

namespace MyAdmin.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RoleController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet("list")]
    public async Task<ActionResult<ApiResponse<PagedResult<RoleDto>>>> GetList(
        [FromQuery] int? pageIndex = null,
        [FromQuery] int? pageSize = null)
    {
        try
        {
            var result = await _roleService.GetListAsync(pageIndex, pageSize);
            return Ok(ApiResponse<PagedResult<RoleDto>>.Success(result));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<PagedResult<RoleDto>>.Fail(ex.Message));
        }
    }

    [HttpGet("all")]
    public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetAll()
    {
        try
        {
            var result = await _roleService.GetAllAsync();
            return Ok(ApiResponse<List<RoleDto>>.Success(result));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<List<RoleDto>>.Fail(ex.Message));
        }
    }

    [HttpPost]
    [HasPermission("system:role:create")]
    public async Task<ActionResult<ApiResponse<object?>>> Create([FromBody] RoleSaveDto dto)
    {
        try
        {
            await _roleService.CreateAsync(dto);
            return Ok(ApiResponse<object?>.Success(null));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<object?>.Fail(ex.Message));
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<object?>>> Update(int id, [FromBody] RoleSaveDto dto)
    {
        try
        {
            await _roleService.UpdateAsync(id, dto);
            return Ok(ApiResponse<object?>.Success(null));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<object?>.Fail(ex.Message));
        }
    }

    [HttpDelete("{id:int}")]
    [HasPermission("system:role:delete")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id)
    {
        try
        {
            await _roleService.DeleteAsync(id);
            return Ok(ApiResponse<object?>.Success(null));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<object?>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 获取全量菜单树及当前角色已勾选的 MenuId（el-tree 赋权回显）。
    /// </summary>
    [HttpGet("{id:int}/permissions")]
    public async Task<ActionResult<ApiResponse<RolePermissionDto>>> GetPermissions(int id)
    {
        try
        {
            var result = await _roleService.GetRolePermissionsAsync(id);
            return Ok(ApiResponse<RolePermissionDto>.Success(result));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<RolePermissionDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// 保存角色勾选的权限 MenuId 集合（请求体为平铺的 int 数组，不包 dto 壳）。
    /// </summary>
    [HttpPost("{id:int}/permissions")]
    public async Task<ActionResult<ApiResponse<object?>>> SavePermissions(int id, [FromBody] List<int> menuIds)
    {
        try
        {
            await _roleService.SaveRolePermissionsAsync(id, menuIds ?? new List<int>());
            return Ok(ApiResponse<object?>.Success(null));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<object?>.Fail(ex.Message));
        }
    }
}
