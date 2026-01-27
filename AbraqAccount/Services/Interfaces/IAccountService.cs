using AbraqAccount.Models;

namespace AbraqAccount.Services.Interfaces;

public interface IAccountService
{
    Task<User?> AuthenticateUserAsync(string username, string password);
    Task<bool> UserExistsAsync(string username);
}

