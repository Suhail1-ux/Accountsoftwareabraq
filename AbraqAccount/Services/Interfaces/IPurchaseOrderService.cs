using AbraqAccount.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AbraqAccount.Services.Interfaces;

public interface IPurchaseOrderService
{
    Task<(List<PurchaseOrder> orders, int totalCount, int totalPages)> GetPurchaseOrdersAsync(
        string? poNumber, string? vendorName, string? status, string? purchaseStatus,
        DateTime? fromDate, DateTime? toDate, int page, int pageSize);

    Task<(bool success, string message)> CreatePurchaseOrderAsync(PurchaseOrder purchaseOrder, IFormCollection? form);
    Task<(bool success, string message)> CreatePurchaseOrderAsync(PurchaseOrder model);
    Task<(bool success, string message)> UpdatePurchaseOrderAsync(int id, PurchaseOrder model);

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
    Task<IEnumerable<LookupItem>> GetExpenseLedgersAsync(int? subGroupId, string? searchTerm);
    
    // Approval and Status Management
    Task<bool> ApprovePurchaseOrderAsync(int id);
    Task<bool> UnapprovePurchaseOrderAsync(int id);
    Task<bool> ChangePurchaseStatusAsync(int id, string newStatus);
    Task<PurchaseOrder?> GetPurchaseOrderByIdAsync(int id);
    Task<bool> DeletePurchaseOrderAsync(int id);
}
