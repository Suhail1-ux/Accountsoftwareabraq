namespace AbraqAccount.Services.Interfaces;

public interface IDashboardService
{
    // Dashboard typically just displays data from other services
    // Add methods here if dashboard has specific business logic
    Task<object> GetDashboardDataAsync();
}

