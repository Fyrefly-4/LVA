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
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("list")]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetList(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? keyword = null)
    {
        try
        {
            var result = await _userService.GetListAsync(pageIndex, pageSize, keyword);
            return Ok(ApiResponse<PagedResult<UserDto>>.Success(result));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<PagedResult<UserDto>>.Fail(ex.Message));
        }
    }

    [HttpPost]
    [HasPermission("system:user:create")]
    public async Task<ActionResult<ApiResponse<object?>>> Create([FromBody] UserSaveDto dto)
    {
        try
        {
            await _userService.CreateAsync(dto);
            return Ok(ApiResponse<object?>.Success(null));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<object?>.Fail(ex.Message));
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<object?>>> Update(int id, [FromBody] UserSaveDto dto)
    {
        try
        {
            await _userService.UpdateAsync(id, dto);
            return Ok(ApiResponse<object?>.Success(null));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<object?>.Fail(ex.Message));
        }
    }

    [HttpDelete("{id:int}")]
    [HasPermission("system:user:delete")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(int id)
    {
        try
        {
            await _userService.DeleteAsync(id);
            return Ok(ApiResponse<object?>.Success(null));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<object?>.Fail(ex.Message));
        }
    }

    [HttpPost("batch-delete")]
    public async Task<ActionResult<ApiResponse<object?>>> BatchDelete([FromBody] List<int> ids)
    {
        try
        {
            await _userService.BatchDeleteAsync(ids);
            return Ok(ApiResponse<object?>.Success(null));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<object?>.Fail(ex.Message));
        }
    }
}
