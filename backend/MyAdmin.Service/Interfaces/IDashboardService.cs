using MyAdmin.Core.Dtos;

namespace MyAdmin.Service.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(int currentUserId);
}
