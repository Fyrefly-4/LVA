using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyAdmin.Core.Common;
using MyAdmin.Core.Dtos;
using MyAdmin.Service.Interfaces;

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
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
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

    [HttpPost]
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
}
