using MyAdmin.Core.Common;
using MyAdmin.Core.Dtos;

namespace MyAdmin.Service.Interfaces;

public interface IKnowledgeService
{
    Task<PagedResult<KnowledgeBookDto>> GetBookListAsync(int pageIndex, int pageSize, string? keyword, string? category);
    Task CreateBookAsync(KnowledgeBookSaveRequest request);
    Task UpdateBookAsync(int id, KnowledgeBookSaveRequest request);
    Task DeleteBookAsync(int id);
    Task BorrowAsync(KnowledgeBorrowRequest request);
    Task ReturnAsync(int logId);
    Task<PagedResult<KnowledgeBorrowLogDto>> GetLogListAsync(int currentUserId, int pageIndex, int pageSize, byte? logStatus);
    Task SelfBorrowAsync(int currentUserId, KnowledgeSelfBorrowRequest request);
    Task<PagedResult<KnowledgeBorrowLogDto>> GetMyLogListAsync(int currentUserId, int pageIndex, int pageSize, byte? logStatus);
    Task SelfReturnAsync(int currentUserId, int logId);
}
