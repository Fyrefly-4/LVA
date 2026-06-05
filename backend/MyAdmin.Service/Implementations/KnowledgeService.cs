using System.Data;
using Microsoft.EntityFrameworkCore;
using MyAdmin.Core.Common;
using MyAdmin.Core.Dtos;
using MyAdmin.Core.Entities;
using MyAdmin.Infrastructure;
using MyAdmin.Service.Interfaces;

namespace MyAdmin.Service.Implementations;

public class KnowledgeService : IKnowledgeService
{
    private readonly MyAdminDbContext _dbContext;

    public KnowledgeService(MyAdminDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<KnowledgeBookDto>> GetBookListAsync(
        int pageIndex,
        int pageSize,
        string? keyword,
        string? category)
    {
        pageIndex = pageIndex < 1 ? 1 : pageIndex;
        pageSize = pageSize < 1 ? 10 : pageSize;

        var query = _dbContext.SysBooks.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var trimmedKeyword = keyword.Trim();
            query = query.Where(b => b.Title.Contains(trimmedKeyword) || b.Isbn.Contains(trimmedKeyword));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var trimmedCategory = category.Trim();
            query = query.Where(b => b.Category == trimmedCategory);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(b => b.CreateTime)
            .ThenByDescending(b => b.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new KnowledgeBookDto
            {
                Id = b.Id,
                Title = b.Title,
                Isbn = b.Isbn,
                Category = b.Category,
                Price = b.Price,
                Stock = b.Stock,
                Status = b.Status,
                CreateTime = b.CreateTime.ToString("yyyy-MM-dd HH:mm:ss")
            })
            .ToListAsync();

        return new PagedResult<KnowledgeBookDto> { Total = total, Items = items };
    }

    public async Task CreateBookAsync(KnowledgeBookSaveRequest request)
    {
        if (request is null)
            throw new InvalidOperationException("文献参数不能为空");

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new InvalidOperationException("文献名称不能为空");

        var isbn = request.Isbn?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(isbn))
            throw new InvalidOperationException("ISBN 不能为空");

        if (await _dbContext.SysBooks.AnyAsync(b => b.Isbn == isbn))
            throw new InvalidOperationException("ISBN 已存在");

        _dbContext.SysBooks.Add(new SysBook
        {
            Title = request.Title.Trim(),
            Isbn = isbn,
            Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim(),
            Price = request.Price,
            Stock = request.Stock,
            Status = request.Status,
            CreateTime = DateTime.Now
        });

        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateBookAsync(int id, KnowledgeBookSaveRequest request)
    {
        if (id <= 0)
            throw new InvalidOperationException("文献 ID 无效");

        if (request is null)
            throw new InvalidOperationException("文献参数不能为空");

        var book = await _dbContext.SysBooks.FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new InvalidOperationException("文献不存在");

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new InvalidOperationException("文献名称不能为空");

        var isbn = request.Isbn?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(isbn))
            throw new InvalidOperationException("ISBN 不能为空");

        if (await _dbContext.SysBooks.AnyAsync(b => b.Isbn == isbn && b.Id != id))
            throw new InvalidOperationException("ISBN 已存在");

        book.Title = request.Title.Trim();
        book.Isbn = isbn;
        book.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        book.Price = request.Price;
        book.Stock = request.Stock;
        book.Status = request.Status;

        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteBookAsync(int id)
    {
        if (id <= 0)
            throw new InvalidOperationException("文献 ID 无效");

        var book = await _dbContext.SysBooks.FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new InvalidOperationException("文献不存在");

        _dbContext.SysBooks.Remove(book);
        await _dbContext.SaveChangesAsync();
    }

    public async Task BorrowAsync(KnowledgeBorrowRequest request)
    {
        if (request is null)
            throw new InvalidOperationException("借阅参数不能为空");

        if (request.UserId <= 0)
            throw new InvalidOperationException("借阅人不能为空");

        if (request.BookIds is null || request.BookIds.Count == 0)
            throw new InvalidOperationException("借阅文献不能为空");

        if (request.BorrowDays <= 0)
            throw new InvalidOperationException("借阅天数必须大于 0");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var user = await _dbContext.SysUsers
                .FirstOrDefaultAsync(u => u.Id == request.UserId)
                ?? throw new InvalidOperationException("借阅人不存在");

            if (user.Status != 1)
                throw new InvalidOperationException("借阅人状态异常，无法指派借阅");

            var now = DateTime.Now;
            var returnTime = now.AddDays(request.BorrowDays);
            var logs = new List<SysBorrowLog>();

            foreach (var bookId in request.BookIds)
            {
                if (bookId <= 0)
                    throw new InvalidOperationException("存在无效的文献 ID");

                var book = await _dbContext.SysBooks
                    .FirstOrDefaultAsync(b => b.Id == bookId)
                    ?? throw new InvalidOperationException($"文献不存在：{bookId}");

                if (book.Status != 1)
                    throw new InvalidOperationException($"文献[{book.Title}]当前处于盘点维护中，无法流转");

                if (book.Stock <= 0)
                    throw new InvalidOperationException($"文献[{book.Title}]库存不足，无法流转");

                book.Stock -= 1;
                logs.Add(new SysBorrowLog
                {
                    BookId = book.Id,
                    BookTitle = book.Title,
                    UserId = user.Id,
                    Username = user.Username,
                    Nickname = user.Nickname,
                    BorrowTime = now,
                    ReturnTime = returnTime,
                    LogStatus = 0
                });
            }

            await _dbContext.SysBorrowLogs.AddRangeAsync(logs);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ReturnAsync(int logId)
    {
        if (logId <= 0)
            throw new InvalidOperationException("流转日志 ID 无效");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var log = await _dbContext.SysBorrowLogs
                .FirstOrDefaultAsync(l => l.Id == logId)
                ?? throw new InvalidOperationException("流转日志不存在");

            if (log.ActualReturnTime.HasValue || log.LogStatus == 1)
                throw new InvalidOperationException("该文献已完成归还，不能重复入库");

            var book = await _dbContext.SysBooks
                .FirstOrDefaultAsync(b => b.Id == log.BookId)
                ?? throw new InvalidOperationException("关联文献不存在，无法归还入库");

            log.ActualReturnTime = DateTime.Now;
            log.LogStatus = 1;
            book.Stock += 1;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<PagedResult<KnowledgeBorrowLogDto>> GetLogListAsync(int pageIndex, int pageSize, byte? logStatus)
    {
        pageIndex = pageIndex < 1 ? 1 : pageIndex;
        pageSize = pageSize < 1 ? 10 : pageSize;

        var now = DateTime.Now;
        await _dbContext.SysBorrowLogs
            .Where(l => l.ActualReturnTime == null && l.LogStatus == 0 && l.ReturnTime < now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(l => l.LogStatus, (byte)2));

        var query = _dbContext.SysBorrowLogs.AsNoTracking().AsQueryable();

        if (logStatus.HasValue)
            query = query.Where(l => l.LogStatus == logStatus.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(l => l.BorrowTime)
            .ThenByDescending(l => l.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new KnowledgeBorrowLogDto
            {
                Id = l.Id,
                BookId = l.BookId,
                BookTitle = l.BookTitle,
                Isbn = l.Book.Isbn,
                UserId = l.UserId,
                Username = l.Username,
                Nickname = l.Nickname,
                BorrowTime = l.BorrowTime.ToString("yyyy-MM-dd HH:mm:ss"),
                ReturnTime = l.ReturnTime.ToString("yyyy-MM-dd HH:mm:ss"),
                ActualReturnTime = l.ActualReturnTime.HasValue
                    ? l.ActualReturnTime.Value.ToString("yyyy-MM-dd HH:mm:ss")
                    : null,
                LogStatus = l.LogStatus
            })
            .ToListAsync();

        return new PagedResult<KnowledgeBorrowLogDto> { Total = total, Items = items };
    }
}
