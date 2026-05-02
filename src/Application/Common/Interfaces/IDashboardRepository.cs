using Application.UseCases.Dashboard.Dtos;
using System.Threading.Tasks;

namespace Application.Common.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardDto> GetDashboardStatsAsync();
}
