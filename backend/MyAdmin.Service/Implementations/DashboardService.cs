using Microsoft.EntityFrameworkCore;
using MyAdmin.Core.Dtos;
using MyAdmin.Infrastructure;
using MyAdmin.Service.Helpers;
using MyAdmin.Service.Interfaces;

namespace MyAdmin.Service.Implementations;

public class DashboardService : IDashboardService
{
    private readonly MyAdminDbContext _dbContext;

    public DashboardService(MyAdminDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardDto> GetDashboardAsync(int currentUserId)
    {
        var now = DateTime.Now;
        var permissions = await UserPermissionHelper.GetPermissionCodesAsync(_dbContext, currentUserId);
        var ctx = new DashboardBuildContext
        {
            UserId = currentUserId,
            Permissions = permissions,
            Now = now
        };

        await MarkOverdueLogsAsync(now);

        var summary = await BuildSummaryAsync(ctx);
        var charts = await BuildChartsAsync(ctx);
        var activities = await BuildRecentActivitiesAsync(ctx);
        var recentBorrows = await BuildRecentBorrowsAsync(ctx);

        return new DashboardDto
        {
            Summary = summary,
            Charts = charts,
            RecentActivities = activities,
            RecentBorrows = recentBorrows
        };
    }

    private async Task MarkOverdueLogsAsync(DateTime now)
    {
        await _dbContext.SysBorrowLogs
            .Where(l => l.ActualReturnTime == null && l.LogStatus == 0 && l.ReturnTime < now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(l => l.LogStatus, (byte)2));
    }

    private async Task<DashboardSummaryDto> BuildSummaryAsync(DashboardBuildContext ctx)
    {
        var summary = new DashboardSummaryDto();

        if (ctx.CanViewBookStats)
        {
            summary.BookTotal = await _dbContext.SysBooks
                .AsNoTracking()
                .CountAsync(b => b.Status != 2);

            summary.StockTotal = await _dbContext.SysBooks
                .AsNoTracking()
                .Where(b => b.Status != 2)
                .SumAsync(b => b.Stock);
        }

        if (ctx.CanViewBorrowAdminStats)
        {
            summary.BorrowedTotal = await _dbContext.SysBorrowLogs
                .AsNoTracking()
                .CountAsync(l => l.LogStatus == 0);

            summary.OverdueTotal = await _dbContext.SysBorrowLogs
                .AsNoTracking()
                .CountAsync(l => l.LogStatus == 2);
        }

        if (ctx.CanViewUserStats)
        {
            summary.UserTotal = await _dbContext.SysUsers.AsNoTracking().CountAsync();
        }

        if (ctx.CanViewRoleStats)
        {
            summary.RoleTotal = await _dbContext.SysRoles.AsNoTracking().CountAsync();
        }

        var dueSoonEnd = ctx.Now.AddDays(DashboardBuildContext.DueSoonDays);

        summary.CurrentBorrowTotal = await _dbContext.SysBorrowLogs
            .AsNoTracking()
            .CountAsync(l => l.UserId == ctx.UserId && l.LogStatus == 0);

        summary.DueSoonTotal = await _dbContext.SysBorrowLogs
            .AsNoTracking()
            .CountAsync(l => l.UserId == ctx.UserId
                && l.LogStatus == 0
                && l.ReturnTime >= ctx.Now
                && l.ReturnTime <= dueSoonEnd);

        summary.OverdueBorrowTotal = await _dbContext.SysBorrowLogs
            .AsNoTracking()
            .CountAsync(l => l.UserId == ctx.UserId && l.LogStatus == 2);

        summary.HistoryTotal = await _dbContext.SysBorrowLogs
            .AsNoTracking()
            .CountAsync(l => l.UserId == ctx.UserId && l.LogStatus == 1);

        return summary;
    }

    private async Task<DashboardChartsDto> BuildChartsAsync(DashboardBuildContext ctx)
    {
        var charts = new DashboardChartsDto();

        if (ctx.CanViewBorrowAdminStats)
        {
            charts.BorrowTrend = await BuildBorrowTrendAsync(ctx);
            charts.HotCategories = await BuildHotCategoriesAsync();
            charts.LogStatusDistribution = await BuildLogStatusDistributionAsync();
        }

        if (ctx.CanViewUserStats)
        {
            charts.UserGrowthTrend = await BuildUserGrowthTrendAsync(ctx);
        }

        charts.PreferenceCategories = await BuildPreferenceCategoriesAsync(ctx);

        return charts;
    }

    private async Task<List<DashboardTrendPointDto>> BuildBorrowTrendAsync(DashboardBuildContext ctx)
    {
        var startDate = ctx.TrendStartDate;

        var raw = await _dbContext.SysBorrowLogs
            .AsNoTracking()
            .Where(l => l.BorrowTime >= startDate)
            .GroupBy(l => l.BorrowTime.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        return FillTrendPoints(startDate, ctx.Now.Date, raw.ToDictionary(x => x.Date, x => x.Count));
    }

    private async Task<List<DashboardCategoryStatDto>> BuildHotCategoriesAsync()
    {
        return await (
            from log in _dbContext.SysBorrowLogs.AsNoTracking()
            join book in _dbContext.SysBooks.AsNoTracking() on log.BookId equals book.Id
            group log by (book.Category ?? "未分类") into g
            orderby g.Count() descending
            select new DashboardCategoryStatDto
            {
                Category = g.Key,
                Count = g.Count()
            })
            .Take(5)
            .ToListAsync();
    }

    private async Task<DashboardLogStatusDistributionDto> BuildLogStatusDistributionAsync()
    {
        var statusCounts = await _dbContext.SysBorrowLogs
            .AsNoTracking()
            .GroupBy(l => l.LogStatus)
            .Select(g => new { LogStatus = g.Key, Count = g.Count() })
            .ToListAsync();

        return new DashboardLogStatusDistributionDto
        {
            Borrowing = statusCounts.FirstOrDefault(x => x.LogStatus == 0)?.Count ?? 0,
            Returned = statusCounts.FirstOrDefault(x => x.LogStatus == 1)?.Count ?? 0,
            Overdue = statusCounts.FirstOrDefault(x => x.LogStatus == 2)?.Count ?? 0
        };
    }

    private async Task<List<DashboardTrendPointDto>> BuildUserGrowthTrendAsync(DashboardBuildContext ctx)
    {
        var startDate = ctx.TrendStartDate;

        var raw = await _dbContext.SysUsers
            .AsNoTracking()
            .Where(u => u.CreateTime >= startDate)
            .GroupBy(u => u.CreateTime.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        return FillTrendPoints(startDate, ctx.Now.Date, raw.ToDictionary(x => x.Date, x => x.Count));
    }

    private async Task<List<DashboardCategoryStatDto>> BuildPreferenceCategoriesAsync(DashboardBuildContext ctx)
    {
        return await (
            from log in _dbContext.SysBorrowLogs.AsNoTracking()
            join book in _dbContext.SysBooks.AsNoTracking() on log.BookId equals book.Id
            where log.UserId == ctx.UserId
            group log by (book.Category ?? "未分类") into g
            orderby g.Count() descending
            select new DashboardCategoryStatDto
            {
                Category = g.Key,
                Count = g.Count()
            })
            .ToListAsync();
    }

    private static List<DashboardTrendPointDto> FillTrendPoints(
        DateTime startDate,
        DateTime endDate,
        Dictionary<DateTime, int> counts)
    {
        var points = new List<DashboardTrendPointDto>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            counts.TryGetValue(date, out var count);
            points.Add(new DashboardTrendPointDto
            {
                Date = date.ToString("yyyy-MM-dd"),
                Count = count
            });
        }

        return points;
    }

    private async Task<List<DashboardActivityDto>> BuildRecentActivitiesAsync(DashboardBuildContext ctx)
    {
        if (!ctx.CanViewBorrowAdminStats)
            return new List<DashboardActivityDto>();

        var recentBorrows = await _dbContext.SysBorrowLogs
            .AsNoTracking()
            .OrderByDescending(l => l.BorrowTime)
            .ThenByDescending(l => l.Id)
            .Take(10)
            .Select(l => new DashboardActivityDto
            {
                ActivityType = "borrow",
                Time = l.BorrowTime.ToString("yyyy-MM-dd HH:mm:ss"),
                Username = l.Username,
                Nickname = l.Nickname,
                BookTitle = l.BookTitle
            })
            .ToListAsync();

        var recentReturns = await _dbContext.SysBorrowLogs
            .AsNoTracking()
            .Where(l => l.ActualReturnTime != null)
            .OrderByDescending(l => l.ActualReturnTime)
            .ThenByDescending(l => l.Id)
            .Take(10)
            .Select(l => new DashboardActivityDto
            {
                ActivityType = "return",
                Time = l.ActualReturnTime!.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                Username = l.Username,
                Nickname = l.Nickname,
                BookTitle = l.BookTitle
            })
            .ToListAsync();

        return recentBorrows
            .Concat(recentReturns)
            .OrderByDescending(a => a.Time)
            .Take(10)
            .ToList();
    }

    private async Task<List<DashboardRecentBorrowDto>> BuildRecentBorrowsAsync(DashboardBuildContext ctx)
    {
        return await _dbContext.SysBorrowLogs
            .AsNoTracking()
            .Where(l => l.UserId == ctx.UserId)
            .OrderByDescending(l => l.BorrowTime)
            .ThenByDescending(l => l.Id)
            .Take(5)
            .Select(l => new DashboardRecentBorrowDto
            {
                Id = l.Id,
                BookTitle = l.BookTitle,
                BorrowTime = l.BorrowTime.ToString("yyyy-MM-dd HH:mm:ss"),
                ReturnTime = l.ReturnTime.ToString("yyyy-MM-dd HH:mm:ss"),
                LogStatus = l.LogStatus
            })
            .ToListAsync();
    }
}
