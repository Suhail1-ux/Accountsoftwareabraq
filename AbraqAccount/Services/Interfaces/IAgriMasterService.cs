using AbraqAccount.Models;

namespace AbraqAccount.Services.Interfaces;

public interface IAgriMasterService
{
    // Grower Groups
    Task<List<GrowerGroup>> GetGrowerGroupsAsync();
    Task<(bool success, string message)> CreateGrowerGroupAsync(GrowerGroup model);
    Task<GrowerGroup?> GetGrowerGroupByIdAsync(int id);
    Task<(bool success, string message)> UpdateGrowerGroupAsync(int id, GrowerGroup model);
    Task<(bool success, string message)> DeleteGrowerGroupAsync(int id);
    Task<string> GenerateGroupCodeAsync();
    Task<List<Farmer>> GetGroupFarmersAsync(int groupId);
    Task<List<Lot>> GetGroupLotsAsync(int groupId);
    
    // Farmers
    Task<List<Farmer>> GetAllFarmersAsync();
    Task<(bool success, string message)> CreateFarmerAsync(Farmer model);
    Task<Farmer?> GetFarmerByIdAsync(int id);
    Task<(bool success, string message)> UpdateFarmerAsync(int id, Farmer model);
    Task<(bool success, string message)> DeleteFarmerAsync(int id);
    Task<string> GenerateFarmerCodeAsync(int groupId);
    Task<string> GetGroupNameAsync(int groupId);
    
    // Lots
    Task<(bool success, string message)> CreateLotAsync(Lot model);
    Task<Lot?> GetLotByIdAsync(int id);
    Task<(bool success, string message)> UpdateLotAsync(int id, Lot model);
    Task<(bool success, string message)> DeleteLotAsync(int id);
    Task LoadLotDropdownsAsync(dynamic viewBag, int? groupId, int? farmerId);
}

