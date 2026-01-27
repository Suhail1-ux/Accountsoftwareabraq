using AbraqAccount.Models;

namespace AbraqAccount.Services.Interfaces;

public interface IUserPermissionService
{
    // Seed Methods
    Task SeedMenusAsync();
    
    // UI Methods
    Task<List<Menu>> GetAllMenusAsync();
    Task<List<UserPermission>> GetPermissionsForUserAsync(int userId);
    Task<(bool success, string message)> UpdatePermissionsAsync(int userId, List<UserPermission> permissions);
    Task<(bool success, string message)> SavePermissionAsync(UserPermission permission);
    
    // Check Methods
    Task<bool> HasPermissionAsync(int userId, string controllerName, string actionName, string permissionType = "View");
    
    // Helper
    Task<List<User>> GetAllUsersAsync();
}

