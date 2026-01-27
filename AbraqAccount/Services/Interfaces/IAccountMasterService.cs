using AbraqAccount.Models;

namespace AbraqAccount.Services.Interfaces;

public interface IAccountMasterService
{
    // Master Group
    Task<List<MasterGroup>> GetMasterGroupsAsync();
    Task<(bool success, string message)> CreateMasterGroupAsync(MasterGroup model);
    Task<MasterGroup?> GetMasterGroupByIdAsync(int id);
    Task<(bool success, string message)> UpdateMasterGroupAsync(int id, MasterGroup model);
    Task DeleteMasterGroupAsync(int id);
    Task<(bool success, string message, int imported, int updated, List<string> errors)> ImportMasterGroupsAsync(List<MasterGroupImportModel> importData);

    // Master Sub Group
    Task<(List<MasterSubGroup> subGroups, List<MasterGroup> masterGroups)> GetMasterSubGroupsWithParentsAsync();
    Task<(bool success, string message)> CreateMasterSubGroupAsync(MasterSubGroup model);
    Task<MasterSubGroup?> GetMasterSubGroupByIdAsync(int id);
    Task<(bool success, string message)> UpdateMasterSubGroupAsync(int id, MasterSubGroup model);
    Task DeleteMasterSubGroupAsync(int id);
    Task<(bool success, string message, int imported, int updated, List<string> errors)> ImportMasterSubGroupsAsync(List<MasterSubGroupImportModel> importData);
    Task<IEnumerable<object>> GetMasterSubGroupsForDropdownAsync(int masterGroupId);

    // Sub Group Ledger
    Task<(List<SubGroupLedger> ledgers, List<MasterSubGroup> masterSubGroups)> GetSubGroupLedgersWithParentsAsync();
    Task<(bool success, string message)> CreateSubGroupLedgerAsync(SubGroupLedger model);
    Task<SubGroupLedger?> GetSubGroupLedgerByIdAsync(int id);
    Task<(bool success, string message)> UpdateSubGroupLedgerAsync(int id, SubGroupLedger model);
    Task DeleteSubGroupLedgerAsync(int id);
    Task<(bool success, string message, int imported, int updated, List<string> errors)> ImportSubGroupLedgersAsync(List<SubGroupLedgerImportModel> importData);
    
    // Utilities
    Task EnsureCodeColumnExistsAsync();
}

