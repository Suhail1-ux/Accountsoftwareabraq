using AbraqAccount.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AbraqAccount.Services.Interfaces;

public interface IExpensesIncurredService
{
    // Index/List operations
    Task<(IEnumerable<ExpensesIncurred> expenses, int totalCount, int totalPages)> GetExpensesAsync(
        string? voucherNo,
        DateTime? fromDate,
        DateTime? toDate,
        string? expenseGroup,
        string? expenseSubGroup,
        string? expenseLedger,
        string? vendorName,
        string? status,
        int page,
        int pageSize);

    // CRUD operations
    Task<ExpensesIncurred?> GetExpenseByIdAsync(int id, bool includeItems = false, bool includeMiscCharges = false);
    Task<ExpensesIncurred> CreateExpenseAsync(ExpensesIncurred expense);
    Task<ExpensesIncurred> UpdateExpenseAsync(int id, ExpensesIncurred expense);
    Task<bool> DeleteExpenseAsync(int id);
    Task<bool> ApproveExpenseAsync(int id);
    Task<bool> UnapproveExpenseAsync(int id);

    // Lookup/Search operations
    Task<IEnumerable<LookupItem>> GetExpenseGroupsAsync(string? searchTerm);
    Task<IEnumerable<LookupItem>> GetExpenseSubGroupsAsync(int? groupId, string? searchTerm);
    Task<IEnumerable<LookupItem>> GetAccountsAsync(string? searchTerm, string? type = null);
    Task<IEnumerable<LookupItem>> GetItemGroupsAsync(string? searchTerm);
    Task<IEnumerable<LookupItem>> GetItemNamesAsync(int? itemGroupId, string? searchTerm);
    Task<IEnumerable<LookupItem>> GetExpenseLedgersAsync(int? subGroupId, string? searchTerm);

    Task<IEnumerable<string>> GetVehiclesAsync(string? searchTerm);
    Task<IEnumerable<VehInfo>> GetVehiclesListAsync();
    
    // Helper operations
    Task LoadAccountNamesAsync(ExpensesIncurred expense);
    Task<List<SelectListItem>> GetPaymentModeListAsync();
}

