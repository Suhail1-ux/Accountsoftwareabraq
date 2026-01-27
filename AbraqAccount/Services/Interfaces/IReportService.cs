using AbraqAccount.Models;

namespace AbraqAccount.Services.Interfaces;

public interface IReportService
{
    Task<BalanceSheetViewModel> GetBalanceSheetAsync(DateTime asOfDate);
}

