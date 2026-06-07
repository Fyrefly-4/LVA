namespace MyAdmin.Core.Dtos;

public class DashboardDto
{
    public DashboardSummaryDto Summary { get; set; } = new();
    public DashboardChartsDto Charts { get; set; } = new();
    public List<DashboardActivityDto> RecentActivities { get; set; } = new();
    public List<DashboardRecentBorrowDto> RecentBorrows { get; set; } = new();
}

public class DashboardSummaryDto
{
    public int? BookTotal { get; set; }
    public int? BorrowedTotal { get; set; }
    public int? StockTotal { get; set; }
    public int? OverdueTotal { get; set; }
    public int? UserTotal { get; set; }
    public int? RoleTotal { get; set; }

    public int CurrentBorrowTotal { get; set; }
    public int DueSoonTotal { get; set; }
    public int OverdueBorrowTotal { get; set; }
    public int HistoryTotal { get; set; }
}

public class DashboardChartsDto
{
    public List<DashboardTrendPointDto>? BorrowTrend { get; set; }
    public List<DashboardCategoryStatDto>? HotCategories { get; set; }
    public DashboardLogStatusDistributionDto? LogStatusDistribution { get; set; }
    public List<DashboardTrendPointDto>? UserGrowthTrend { get; set; }
    public List<DashboardCategoryStatDto>? PreferenceCategories { get; set; }
}

public class DashboardTrendPointDto
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DashboardCategoryStatDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DashboardLogStatusDistributionDto
{
    public int Borrowing { get; set; }
    public int Returned { get; set; }
    public int Overdue { get; set; }
}

public class DashboardActivityDto
{
    public string ActivityType { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string BookTitle { get; set; } = string.Empty;
}

public class DashboardRecentBorrowDto
{
    public int Id { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BorrowTime { get; set; } = string.Empty;
    public string ReturnTime { get; set; } = string.Empty;
    public byte LogStatus { get; set; }
}
