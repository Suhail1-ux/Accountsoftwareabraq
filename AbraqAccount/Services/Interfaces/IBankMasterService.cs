using AbraqAccount.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AbraqAccount.Services.Interfaces;

public interface IBankMasterService
{
    Task<IEnumerable<BankMaster>> GetAllActiveBankMastersAsync();
    Task<BankMaster?> GetBankMasterByIdAsync(int id);
    Task<BankMaster> CreateBankMasterAsync(BankMaster bankMaster, string username);
    Task<BankMaster> UpdateBankMasterAsync(int id, BankMaster bankMaster);
    Task<bool> DeleteBankMasterAsync(int id);
    Task<bool> BankMasterExistsAsync(int id);
    Task<bool> AccountNameExistsAsync(string accountName, int? excludeId = null);
    Task<bool> AccountNumberExistsAsync(string accountNumber, int? excludeId = null);
    Task<SelectList> GetGroupsSelectListAsync(int? selectedValue = null);
    Task FixNullAccountNamesAsync();
    Task<(bool success, string message, List<string> steps)> MigrateDatabaseAsync();
}

