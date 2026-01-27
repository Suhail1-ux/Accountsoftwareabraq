using Microsoft.EntityFrameworkCore;
using AbraqAccount.Data;
using AbraqAccount.Models;
using AbraqAccount.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AbraqAccount.Services.Implementations;

public class CreditNoteService : ICreditNoteService
{
    private readonly AppDbContext _context;

    public CreditNoteService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(List<CreditNote> notes, int totalCount, int totalPages)> GetCreditNotesAsync(
        string? unit, string? creditNoteNo, int? growerGroupId, int? farmerId, 
        string? status, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var query = _context.CreditNotes
            .Include(c => c.GrowerGroup)
            .Include(c => c.Farmer)
            .Where(c => c.IsActive)
            .AsQueryable();

        if (!string.IsNullOrEmpty(unit) && unit != "ALL") query = query.Where(c => c.Unit == unit);
        if (!string.IsNullOrEmpty(creditNoteNo)) query = query.Where(c => c.CreditNoteNo.Contains(creditNoteNo));
        if (growerGroupId.HasValue) query = query.Where(c => c.GroupId == growerGroupId.Value);
        if (farmerId.HasValue) query = query.Where(c => c.FarmerId == farmerId.Value);
        if (!string.IsNullOrEmpty(status) && status != "ALL") query = query.Where(c => c.Status == status);
        if (fromDate.HasValue) query = query.Where(c => c.CreditNoteDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(c => c.CreditNoteDate <= toDate.Value);

        int totalCount = await query.CountAsync();
        int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var notes = await query.OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Manual Load of Account Names for Polymorphic Columns
        foreach (var note in notes)
        {
            note.CreditAccountName = await GetAccountNameAsync(note.CreditAccountType, note.CreditAccountId);
            note.DebitAccountName = await GetAccountNameAsync(note.DebitAccountType, note.DebitAccountId);
        }

        return (notes, totalCount, totalPages);
    }

    public async Task<(bool success, string message)> CreateCreditNoteAsync(CreditNote model)
    {
        model.CreditNoteNo = await GenerateCreditNoteNoAsync();
        model.CreatedAt = DateTime.Now;
        model.IsActive = true;
        if (string.IsNullOrEmpty(model.Status)) model.Status = "UnApproved";

        _context.Add(model);
        await _context.SaveChangesAsync();
        return (true, "Created successfully");
    }

    public async Task<CreditNote?> GetCreditNoteByIdAsync(int id)
    {
        var note = await _context.CreditNotes
            .Include(c => c.GrowerGroup)
            .Include(c => c.Farmer)
            .Include(c => c.Details)
            .FirstOrDefaultAsync(m => m.Id == id);
            
        if (note != null)
        {
            note.CreditAccountName = await GetAccountNameAsync(note.CreditAccountType, note.CreditAccountId);
            note.DebitAccountName = await GetAccountNameAsync(note.DebitAccountType, note.DebitAccountId);
        }
        return note;
    }

    private async Task<string> GetAccountNameAsync(string type, int id)
    {
        if (id == 0) return "N/A";
        
        string typeLower = type?.ToLower() ?? "";
        // Debug
        Console.WriteLine($"GetAccountNameAsync: Type='{type}', ID={id}, Lower='{typeLower}'");

        if (typeLower.Contains("farmer"))
        {
            var f = await _context.Farmers.FindAsync(id);
            return f?.FarmerName ?? "Unknown Farmer";
        }
        if (typeLower.Contains("growergroup"))
        {
             var g = await _context.GrowerGroups.FindAsync(id);
             return g?.GroupName ?? "Unknown Group";
        }
        if (typeLower.Contains("bankmaster"))
        {
            var b = await _context.BankMasters.FindAsync(id);
            return b?.AccountName ?? "Unknown Bank";
        }
        if (typeLower.Contains("subgroupledger"))
        {
            var s = await _context.SubGroupLedgers.Include(x => x.MasterGroup).Include(x => x.MasterSubGroup).FirstOrDefaultAsync(x => x.Id == id);
            return s?.Name ?? "Unknown Account";
        }
        if (typeLower.Contains("mastergroup"))
        {
             var mg = await _context.MasterGroups.FindAsync(id);
             return mg?.Name ?? "Unknown Group";
        }

        return type + " (ID: " + id + ")";
    }


    public async Task<(bool success, string message)> UpdateCreditNoteAsync(CreditNote model)
    {
        try
        {
            var existing = await _context.CreditNotes.FindAsync(model.Id);
            if (existing == null) return (false, "Not found");

            _context.Entry(existing).CurrentValues.SetValues(model);
            await _context.SaveChangesAsync();
            return (true, "Updated successfully");
        }
        catch (Exception ex)
        {
            return (false, "Error: " + ex.Message);
        }
    }

    public async Task<(bool success, string message)> DeleteCreditNoteAsync(int id)
    {
        var note = await _context.CreditNotes.FindAsync(id);
        if (note != null)
        {
            note.IsActive = false;
            _context.Update(note);
            await _context.SaveChangesAsync();
            return (true, "Deleted successfully");
        }
        return (false, "Not found");
    }

    public async Task<(bool success, string message)> ApproveCreditNoteAsync(int id)
    {
        var note = await _context.CreditNotes.FindAsync(id);
        if (note == null) return (false, "Not found");

        if (note.Status == "Approved") return (false, "Already approved");

        note.Status = "Approved";
        _context.Update(note);
        await _context.SaveChangesAsync();
        return (true, "Approved successfully");
    }

    public async Task<(bool success, string message)> UnapproveCreditNoteAsync(int id)
    {
        var note = await _context.CreditNotes.FindAsync(id);
        if (note == null) return (false, "Not found");

        if (note.Status != "Approved") return (false, "Note is not approved");

        note.Status = "UnApproved";
        _context.Update(note);
        await _context.SaveChangesAsync();
        return (true, "Unapproved successfully");
    }

    public async Task LoadDropdownsAsync(dynamic viewBag, int? growerGroupId = null, int? farmerId = null)
    {
        viewBag.GrowerGroups = new SelectList(
            await _context.GrowerGroups.Where(g => g.IsActive).OrderBy(g => g.GroupName).ToListAsync(),
            "Id", "GroupName", growerGroupId
        );

        var farmers = new List<Farmer>();
        if (growerGroupId.HasValue)
        {
            farmers = await _context.Farmers.Where(f => f.GroupId == growerGroupId.Value && f.IsActive).OrderBy(f => f.FarmerName).ToListAsync();
        }
        viewBag.Farmers = new SelectList(farmers, "Id", "FarmerName", farmerId);

        var unitList = new List<SelectListItem>
        {
            new SelectListItem { Value = "UNIT-1", Text = "UNIT-1" },
            new SelectListItem { Value = "UNIT-2", Text = "UNIT-2" },
            new SelectListItem { Value = "Abraq Agro Fresh LLP", Text = "Abraq Agro Fresh LLP" }
        };
        viewBag.UnitList = new SelectList(unitList, "Value", "Text");

        // Load Entry Profiles for Credit Note
        var entryProfiles = await _context.EntryForAccounts
            .Where(e => e.TransactionType == "Global" || e.TransactionType == "CreditNote") 
            .OrderBy(e => e.AccountName)
            .Select(e => new SelectListItem
            {
                Value = e.Id.ToString(),
                Text = e.AccountName
            })
            .ToListAsync();
        viewBag.EntryProfiles = new SelectList(entryProfiles, "Value", "Text");
    }

    public async Task<IEnumerable<LookupItem>> GetFarmersByGroupAsync(int groupId)
    {
        return await _context.Farmers
            .Where(f => f.GroupId == groupId && f.IsActive)
            .OrderBy(f => f.FarmerName)
            .Select(f => new LookupItem { Id = f.Id, Name = f.FarmerName, Type = "Farmer" })
            .ToListAsync();
    }

    public async Task<IEnumerable<LookupItem>> GetEntryProfilesAsync()
    {
        return await _context.EntryForAccounts
            .Where(e => e.TransactionType == "Global" || e.TransactionType == "CreditNote") 
            .OrderBy(e => e.AccountName)
            .Select(e => new LookupItem { Id = e.Id, Name = e.AccountName })
            .ToListAsync();
    }

    public async Task<IEnumerable<LookupItem>> GetAccountsAsync(string? searchTerm, int? entryAccountId = null, string? type = null)
    {
        // Fetch Rules dictionary for fast lookup
        var rules = await _context.AccountRules
            .Where(r => r.RuleType == "AllowedNature")
            .ToListAsync();
        
        var rulesDict = rules
            .GroupBy(r => $"{r.AccountType}-{r.AccountId}")
            .ToDictionary(g => g.Key, g => g.First().Value);

        // Helper to get rule value
        string? GetRuleValue(string accountType, int accountId, int? entryId)
        {
            if (entryId.HasValue)
            {
                var specificRule = rules.FirstOrDefault(r => r.AccountType == accountType && r.AccountId == accountId && r.EntryAccountId == entryId);
                if (specificRule != null) return specificRule.Value;
            }
            var defaultRule = rules.FirstOrDefault(r => r.AccountType == accountType && r.AccountId == accountId && r.EntryAccountId == null);
            if (defaultRule != null) return defaultRule.Value;
            return null;
        }

        // Helper to check if account is allowed
        bool IsAllowed(string accountType, int accountId, string? fallbackType = null, int? fallbackId = null)
        {
            // Use provided type or default to "Credit"
            string filterType = type ?? "Credit";

            string? ruleValue = GetRuleValue(accountType, accountId, entryAccountId);

            if (ruleValue != null)
            {
                return CheckRule(ruleValue, filterType);
            }

            if (fallbackType != null && fallbackId.HasValue)
            {
                string? fallbackRuleValue = GetRuleValue(fallbackType, fallbackId.Value, entryAccountId);
                if (fallbackRuleValue != null)
                {
                    return CheckRule(fallbackRuleValue, filterType);
                }
            }

            return true; // No rule = Allowed
        }

        bool CheckRule(string ruleValue, string type)
        {
            if (ruleValue == "Both") return true;
            if (ruleValue == "Cancel") return false;
            if (ruleValue == "Debit" && type == "Debit") return true;
            if (ruleValue == "Credit" && type == "Credit") return true;
            return false;
        }

        // Search logic similar to GeneralEntryService bit for CreditNote
        var bankMastersQuery = _context.BankMasters.Where(bm => bm.IsActive);
        var farmersQuery = _context.Farmers.Where(f => f.IsActive);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            bankMastersQuery = bankMastersQuery.Where(bm => bm.AccountName.Contains(searchTerm));
            farmersQuery = farmersQuery.Where(f => f.FarmerName.Contains(searchTerm));
        }

        var bankMasters = await bankMastersQuery
            .OrderBy(bm => bm.AccountName)
            .Take(50)
            .ToListAsync();

        var farmers = await farmersQuery
            .OrderBy(f => f.FarmerName)
            .Take(50)
            .ToListAsync();

        var results = new List<LookupItem>();
        
        results.AddRange(bankMasters
            .Where(bm => IsAllowed("BankMaster", bm.Id, "SubGroupLedger", bm.GroupId))
            .Select(bm => new LookupItem { Id = bm.Id, Name = bm.AccountName, Type = "BankMaster" }));

        results.AddRange(farmers
            .Where(f => IsAllowed("Farmer", f.Id, "GrowerGroup", f.GroupId))
            .Select(f => new LookupItem { Id = f.Id, Name = f.FarmerName, Type = "Farmer" }));

        return results.OrderBy(r => r.Name).Take(100).ToList();
    }

    private async Task<string> GenerateCreditNoteNoAsync()
    {
        var lastCreditNote = await _context.CreditNotes
            .OrderByDescending(c => c.Id)
            .Select(c => new { c.Id, c.CreditNoteNo }) // Only select what's needed to avoid materialization issues
            .FirstOrDefaultAsync();

        if (lastCreditNote == null) return $"CN{DateTime.Now:yyyyMM}001";

        var lastNo = lastCreditNote.CreditNoteNo;
        if (string.IsNullOrWhiteSpace(lastNo) || !lastNo.StartsWith("CN")) return $"CN{DateTime.Now:yyyyMM}001";

        if (lastNo.Length >= 10 && int.TryParse(lastNo.Substring(8), out int lastNumber))
        {
            var currentPrefix = $"CN{DateTime.Now:yyyyMM}";
            if (lastNo.StartsWith(currentPrefix)) return $"{currentPrefix}{(lastNumber + 1):D3}";
        }
        return $"CN{DateTime.Now:yyyyMM}001";
    }

    public async Task PopulateAccountNamesAsync(IEnumerable<CreditNote> notes)
    {
        foreach (var note in notes)
        {
            note.CreditAccountName = await GetAccountNameAsync(note.CreditAccountType, note.CreditAccountId);
            note.DebitAccountName = await GetAccountNameAsync(note.DebitAccountType, note.DebitAccountId);
        }
    }

    public async Task<int?> GetEntryProfileIdAsync(int creditAccountId, string creditType, int debitAccountId, string debitType)
    {
        // specific rule for the credit account
        var creditRule = await _context.AccountRules
            .Where(r => r.AccountType == creditType && r.AccountId == creditAccountId && r.EntryAccountId != null)
            .Select(r => r.EntryAccountId)
            .FirstOrDefaultAsync();

        if (creditRule.HasValue) return creditRule;

        // specific rule for the debit account
        var debitRule = await _context.AccountRules
            .Where(r => r.AccountType == debitType && r.AccountId == debitAccountId && r.EntryAccountId != null)
            .Select(r => r.EntryAccountId)
            .FirstOrDefaultAsync();

        if (debitRule.HasValue) return debitRule;
        
        // If no explicit rules, maybe check if we can infer from "EntryForAccount" allowed types?
        // But EntryForAccount model only has Name. 
        // We could default to "Global" or similar if needed?
        // For now, return null.
        return null;
    }
}

