using AbraqAccount.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AbraqAccount.Services.Interfaces;

public interface IPurchaseTransactionService
{
    // Purchase Request
    Task<(List<PurchaseRequest> requests, int totalCount, int totalPages)> GetPurchaseRequestsAsync(
        string? poRequestNo, string? itemName, string? requestedBy, 
        string? status, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task<(bool success, string message)> CreatePurchaseRequestAsync(PurchaseRequest model, IFormCollection? form);
    Task<(bool success, string message)> CreatePurchaseRequestAsync(PurchaseRequest model);
    Task LoadRequestDropdownsAsync(dynamic viewBag);
    Task<IEnumerable<LookupItem>> GetUsersAsync(string? searchTerm);

    // Purchase Receive
    Task<(List<PurchaseReceive> receives, int totalCount, int totalPages)> GetPurchaseReceivesAsync(
        string? receiptNo, string? poNumber, string? vendorName, 
        string? status, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task<(bool success, string message)> CreatePurchaseReceiveAsync(PurchaseReceive model, Microsoft.AspNetCore.Http.IFormFile? scannedCopyBill, string webRootPath);
    Task<(bool success, string message)> CreatePurchaseReceiveAsync(PurchaseReceive model);
    Task LoadReceiveDropdownsAsync(dynamic viewBag);
    Task<IEnumerable<LookupItem>> GetVendorsAsync(string? searchTerm);
    Task<IEnumerable<LookupItem>> GetPONumbersAsync(string? searchTerm);
    Task<IEnumerable<LookupItem>> GetMasterSubGroupsAsync(int masterGroupId);
    Task<IEnumerable<LookupItem>> GetSubGroupLedgersAsync(int masterSubGroupId);
}

