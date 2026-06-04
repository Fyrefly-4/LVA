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
public class KnowledgeController : ControllerBase
{
    private readonly IKnowledgeService _knowledgeService;

    public KnowledgeController(IKnowledgeService knowledgeService)
    {
        _knowledgeService = knowledgeService;
    }

    [HttpPost("book")]
    [HasPermission("system:knowledge:create")]
    public async Task<ActionResult<ApiResponse<object?>>> CreateBook([FromBody] KnowledgeBookSaveRequest request)
    {
        try
        {
            await _knowledgeService.CreateBookAsync(request);
            return Ok(ApiResponse<object?>.Success(null));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<object?>.Fail(ex.Message));
        }
    }

    [HttpPut("book/{id:int}")]
    [HasPermission("system:knowledge:edit")]
    public async Task<ActionResult<ApiResponse<object?>>> UpdateBook(int id, [FromBody] KnowledgeBookSaveRequest request)
    {
        try
        {
            await _knowledgeService.UpdateBookAsync(id, request);
            return Ok(ApiResponse<object?>.Success(null));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<object?>.Fail(ex.Message));
        }
    }

    [HttpDelete("book/{id:int}")]
    [HasPermission("system:knowledge:delete")]
    public async Task<ActionResult<ApiResponse<object?>>> DeleteBook(int id)
    {
        try
        {
            await _knowledgeService.DeleteBookAsync(id);
            return Ok(ApiResponse<object?>.Success(null));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<object?>.Fail(ex.Message));
        }
    }

    [HttpGet("book/list")]
    [HasPermission("system:knowledge:bookList")]
    public async Task<ActionResult<ApiResponse<PagedResult<KnowledgeBookDto>>>> GetBookList(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] string? category = null)
    {
        try
        {
            var result = await _knowledgeService.GetBookListAsync(pageIndex, pageSize, keyword, category);
            return Ok(ApiResponse<PagedResult<KnowledgeBookDto>>.Success(result));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<PagedResult<KnowledgeBookDto>>.Fail(ex.Message));
        }
    }

    [HttpPost("borrow")]
    [HasPermission("system:knowledge:borrow")]
    public async Task<ActionResult<ApiResponse<object?>>> Borrow([FromBody] KnowledgeBorrowRequest request)
    {
        try
        {
            await _knowledgeService.BorrowAsync(request);
            return Ok(ApiResponse<object?>.Success(null));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<object?>.Fail(ex.Message));
        }
    }

    [HttpPost("return/{logId:int}")]
    [HasPermission("system:knowledge:return")]
    public async Task<ActionResult<ApiResponse<object?>>> Return(int logId)
    {
        try
        {
            await _knowledgeService.ReturnAsync(logId);
            return Ok(ApiResponse<object?>.Success(null));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<object?>.Fail(ex.Message));
        }
    }

    [HttpGet("log/list")]
    public async Task<ActionResult<ApiResponse<PagedResult<KnowledgeBorrowLogDto>>>> GetLogList(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] byte? logStatus = null)
    {
        try
        {
            var result = await _knowledgeService.GetLogListAsync(pageIndex, pageSize, logStatus);
            return Ok(ApiResponse<PagedResult<KnowledgeBorrowLogDto>>.Success(result));
        }
        catch (Exception ex)
        {
            return Ok(ApiResponse<PagedResult<KnowledgeBorrowLogDto>>.Fail(ex.Message));
        }
    }
}
