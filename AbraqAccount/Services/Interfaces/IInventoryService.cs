using AbraqAccount.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AbraqAccount.Services.Interfaces;

public interface IInventoryService
{
    // Material Issue
    Task<(List<MaterialIssue> issues, int totalCount, int totalPages)> GetMaterialIssuesAsync(
        string? materialIssueNo, string? deliveredGroup, string? deliveredTo, string? orderBy, 
        string? status, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task<(bool success, string message)> CreateMaterialIssueAsync(MaterialIssue model, Microsoft.AspNetCore.Http.IFormCollection? form);
    Task<(bool success, string message)> CreateMaterialIssueAsync(MaterialIssue model);
    Task<IEnumerable<LookupItem>> GetPurchaseItemsAsync(string? searchTerm);

    // Material Stock Ledger
    Task<List<PurchaseItem>> GetStockLedgerReportAsync(
        DateTime? fromDate, DateTime? toDate, string? itemGroup, 
        string? itemName, string? uom, string? unit, 
        string? vendorName, string? reportType, string? commonSearch);
    Task LoadStockLedgerDropdownsAsync(dynamic viewBag);
}

