using AbraqAccount.Models;

namespace AbraqAccount.Services.Interfaces;

public interface IVendorService
{
    Task<IEnumerable<Vendor>> GetAllActiveVendorsAsync();
    Task<Vendor?> GetVendorByIdAsync(int id);
    Task<IEnumerable<object>> SearchVendorsAsync(string? searchTerm);
    Task<Vendor> CreateVendorAsync(Vendor vendor);
    Task<Vendor> UpdateVendorAsync(Vendor vendor);
    Task<bool> DeleteVendorAsync(int id);
    Task<bool> VendorExistsAsync(int id);
    Task<string> GenerateVendorCodeAsync();
}

