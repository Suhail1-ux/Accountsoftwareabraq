using AbraqAccount.Services.Interfaces;

namespace AbraqAccount.Services.Implementations;

public class DashboardService : IDashboardService
{
    public Task<object> GetDashboardDataAsync()
    {
        // Dashboard logic can be added here
        return Task.FromResult<object>(new { });
    }
}

