namespace MyAdmin.Service.Helpers;

internal sealed class DashboardBuildContext
{
    public const int DueSoonDays = 3;
    public const int TrendDays = 30;

    public int UserId { get; init; }
    public HashSet<string> Permissions { get; init; } = new(StringComparer.Ordinal);
    public DateTime Now { get; init; }

    public DateTime TrendStartDate => Now.Date.AddDays(-(TrendDays - 1));

    public bool Has(string permCode) => Permissions.Contains(permCode);

    public bool CanViewBookStats => Has("system:knowledge:bookList");
    public bool CanViewBorrowAdminStats => Has("system:knowledge:adminLog");
    public bool CanViewUserStats => Has("system:user:list");
    public bool CanViewRoleStats => Has("system:role:list");
}
