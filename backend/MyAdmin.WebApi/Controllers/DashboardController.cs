using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyAdmin.Core.Common;
using MyAdmin.Core.Dtos;
using MyAdmin.Service.Interfaces;

namespace MyAdmin.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<DashboardDto>>> Get()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Ok(ApiResponse<DashboardDto>.Fail("无法识别当前登录用户", 401));

        try
        {
            var result = await _dashboardService.GetDashboardAsync(userId);
            return Ok(ApiResponse<DashboardDto>.Success(result));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<DashboardDto>.Fail(ex.Message));
        }
    }
}
