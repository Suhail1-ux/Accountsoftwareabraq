using AbraqAccount.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AbraqAccount.Services.Interfaces;

public interface IPurchaseOrderService
{
    Task<(List<PurchaseOrder> orders, int totalCount, int totalPages)> GetPurchaseOrdersAsync(
        string? poNumber, string? vendorName, string? status, 
        DateTime? fromDate, DateTime? toDate, int page, int pageSize);

    Task<(bool success, string message)> CreatePurchaseOrderAsync(PurchaseOrder purchaseOrder, IFormCollection? form);
    Task<(bool success, string message)> CreatePurchaseOrderAsync(PurchaseOrder model);

    Task LoadDropdownsAsync(dynamic viewBag);
    
    // Reports
    Task<List<PurchaseOrder>> GetPurchaseOrderReportAsync(
        DateTime? fromDate, DateTime? toDate, string? vendorName, 
        string? itemGroup, string? itemNam, string? uom, 
        string? billingTo, string? deliveryAddress, string? status);
    Task LoadReportDropdownsAsync(dynamic viewBag);
    
    // AJAX Lookups
    Task<IEnumerable<LookupItem>> GetVendorsAsync(string? searchTerm);
    Task<IEnumerable<LookupItem>> GetItemGroupsAsync();
    Task<IEnumerable<LookupItem>> GetItemsByGroupAsync(int groupId);
    Task<IEnumerable<LookupItem>> GetTermsConditionsAsync();
}
