using AbraqAccount.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AbraqAccount.Services.Interfaces;

public interface IPurchaseMasterService
{
    // Purchase Item Group
    Task<List<PurchaseItemGroup>> GetPurchaseItemGroupsAsync(string? searchTerm);
    Task<(bool success, string message)> CreatePurchaseItemGroupAsync(PurchaseItemGroup model, string username);
    Task<PurchaseItemGroup?> GetPurchaseItemGroupByIdAsync(int id);
    Task<(bool success, string message)> UpdatePurchaseItemGroupAsync(int id, PurchaseItemGroup model, string username);
    Task<(bool success, string message)> DeletePurchaseItemGroupAsync(int id);
    Task<IEnumerable<object>> GetPurchaseItemGroupHistoryAsync(int id);

    // Purchase Item
    Task<List<PurchaseItem>> GetPurchaseItemsAsync(string? searchTerm);
    Task<(bool success, string message)> CreatePurchaseItemAsync(PurchaseItem model, string username);
    Task<PurchaseItem?> GetPurchaseItemByIdAsync(int id);
    Task<(bool success, string message)> UpdatePurchaseItemAsync(int id, PurchaseItem model, string username);
    Task<(bool success, string message)> DeletePurchaseItemAsync(int id);
    Task<IEnumerable<object>> GetPurchaseItemHistoryAsync(int id);
    
    // Special Rates & Dropdowns
    Task<List<GrowerGroup>> GetGrowerGroupsAsync();
    Task<IEnumerable<object>> GetFarmersByGroupAsync(int groupId);
    Task<(bool success, string message)> SaveSpecialRateAsync(SaveSpecialRateRequest request);
    Task LoadPurchaseItemDropdownsAsync(dynamic viewBag);

    // Purchase Order TCs
    Task<(List<PurchaseOrderTC> tcs, int totalCount, int totalPages)> GetPurchaseOrderTCsAsync(string? searchTerm, int page, int pageSize);
    Task<(bool success, string message)> CreatePurchaseOrderTCAsync(PurchaseOrderTC model, string username);
    Task<PurchaseOrderTC?> GetPurchaseOrderTCByIdAsync(int id);
    Task<(bool success, string message)> UpdatePurchaseOrderTCAsync(int id, PurchaseOrderTC model, string username);
    Task<IEnumerable<object>> GetPurchaseOrderTCHistoryAsync(int id);
}

