using AbraqAccount.Models;

namespace AbraqAccount.Services.Interfaces;

public interface ISettingsService
{
    // Entry For
    Task<List<EntryForAccount>> GetEntryForAccountsAsync();
    Task<(bool success, string message)> CreateEntryForAccountAsync(EntryForAccount model);
    Task<EntryForAccount?> GetEntryForAccountByIdAsync(int id);
    Task<(bool success, string message)> UpdateEntryForAccountAsync(int id, EntryForAccount model);
    Task DeleteEntryForAccountAsync(int id);

    // Transaction Rules
    Task<List<AccountRule>> GetAccountRulesAsync();
    Task<(bool success, string message)> CreateAccountRuleAsync(AccountRule model);
    Task<AccountRule?> GetAccountRuleByIdAsync(int id);
    Task<(bool success, string message)> UpdateAccountRuleAsync(int id, AccountRule model);
    Task DeleteAccountRuleAsync(int id);

    // Lookups
    Task<IEnumerable<LookupItem>> GetAvailableAccountsForRulesAsync(string accountType);

    // Dashboard
    Task<List<RulesItemViewModel>> GetRulesDashboardDataAsync(int? entryForId);
    Task<(bool success, string message)> SaveAccountRuleByValueAsync(string accountType, int accountId, string value, int? entryForId);

    Task<List<EntryForAccount>> GetFormattedEntryProfilesAsync(string transactionType);
}
