using Microsoft.EntityFrameworkCore;
using AbraqAccount.Data;
using AbraqAccount.Models;
using AbraqAccount.Services.Interfaces;
using AbraqAccount.Models.Common;
using AbraqAccount.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;

namespace AbraqAccount.Services.Implementations;

public class CreditNoteService : ICreditNoteService
{
    private readonly AppDbContext _context; 
    private readonly IDbContextFactory<AppDbContext> _contextFactory; 
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreditNoteService(AppDbContext context, IDbContextFactory<AppDbContext> contextFactory, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _contextFactory = contextFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    #region Helpers
    private string GetCurrentUsername()
    {
        try
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return "Admin";
            var userSession = session.GetObject<UserSession>(SessionKeys.UserSession);
            return userSession?.Username ?? "Admin";
        }
        catch
        {
            return "Admin";
        }
    }
    #endregion

    #region Listing
    public async Task<(List<CreditNote> notes, int totalCount, int totalPages)> GetCreditNotesAsync(
        string? unit, string? creditNoteNo, string? accountName, 
        string? status, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        try
        {
            var query = _context.GeneralEntries
                .Where(c => c.VoucherType == "Credit Note" && (status == "Deleted" ? !c.IsActive : c.IsActive))
                .AsQueryable();

            if (!string.IsNullOrEmpty(unit) && unit != "ALL") query = query.Where(c => c.Unit == unit);
            if (!string.IsNullOrEmpty(creditNoteNo)) query = query.Where(c => c.VoucherNo.Contains(creditNoteNo));
            if (!string.IsNullOrEmpty(status) && status != "ALL" && status != "Deleted") query = query.Where(c => c.Status == status);
            if (fromDate.HasValue) query = query.Where(c => c.EntryDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(c => c.EntryDate <= toDate.Value);

            // Group/Farmer search logic moved to accountName filtering below pagination

            // 1. Get unique VoucherNos for pagination
            var voucherQuery = query.Select(x => x.VoucherNo).Distinct();
            
            int totalCount = await voucherQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // 2. Get the specific VoucherNos for this page
            var pageVoucherNos = await voucherQuery
                .OrderByDescending(v => v) // Sort by VoucherNo desc (approximate for date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 3. Fetch all entries for these vouchers
            var entries = await _context.GeneralEntries
                .Where(x => pageVoucherNos.Contains(x.VoucherNo))
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            // 4. Group by VoucherNo and Map
            var notes = new List<CreditNote>();
            var groupedEntries = entries.GroupBy(x => x.VoucherNo);

            foreach (var group in groupedEntries)
            {
                var first = group.First(); // Use first entry for header info
                
                // Calculate Totals and aggregate names
                var totalAmount = group.Sum(x => x.Amount); // Depends if split... actually amount is per row. Total should be sum of Dr or Cr side? 
                // Since it's a balanced entry, Sum(Amount) will be double the actual transaction value if we sum everything.
                // Usually we want the total transaction value. 
                // Let's assume the user wants the distinct sum of one side. 
                // Or just use the max of sum(Dr) vs sum(Cr).
                var debitSum = group.Where(x => x.DebitAccountId != null).Sum(x => x.Amount);
                var creditSum = group.Where(x => x.CreditAccountId != null).Sum(x => x.Amount);
                var displayAmount = Math.Max(debitSum, creditSum);

                // For display names
                var creditNames = new List<string>();
                var debitNames = new List<string>();

                foreach(var item in group)
                {
                    if (item.CreditAccountId.HasValue) 
                        creditNames.Add(await GetAccountNameAsync(item.CreditAccountType, item.CreditAccountId.Value));
                    if (item.DebitAccountId.HasValue) 
                        debitNames.Add(await GetAccountNameAsync(item.DebitAccountType, item.DebitAccountId.Value));
                }

                var note = await MapToCreditNoteAsync(first);
                
                // Override with aggregated values
                note.Amount = displayAmount;
                note.CreditAccountName = string.Join(", ", creditNames.Distinct());
                if (creditNames.Count > 1) note.CreditAccountName += " (Split)";

                note.DebitAccountName = string.Join(", ", debitNames.Distinct());
                if (debitNames.Count > 1) note.DebitAccountName += " (Split)";
                
                notes.Add(note);
            }
            
            if (!string.IsNullOrEmpty(accountName))
            {
                notes = notes
                    .Where(n => (n.CreditAccountName != null && n.CreditAccountName.Contains(accountName, StringComparison.OrdinalIgnoreCase))
                             || (n.DebitAccountName != null && n.DebitAccountName.Contains(accountName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            return (notes, totalCount, totalPages);
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task<CreditNote> MapToCreditNoteAsync(GeneralEntry ge)
    {
        var note = new CreditNote
        {
            Id = ge.Id,
            CreditNoteNo = ge.VoucherNo,
            CreditNoteDate = ge.EntryDate,
            CreditAccountId = ge.CreditAccountId ?? 0,
            CreditAccountType = ge.CreditAccountType,
            DebitAccountId = ge.DebitAccountId ?? 0,
            DebitAccountType = ge.DebitAccountType,
            Amount = ge.Amount,
            Narration = ge.Narration,
            Status = ge.Status,
            Unit = ge.Unit,
            EntryForId = ge.EntryForId,
            EntryForName = ge.EntryForName, // Assuming you added this to CreditNote or use lookup
            CreatedAt = ge.CreatedAt,
            CreatedBy = ge.CreatedBy,
            IsActive = ge.IsActive,
            // Map legacy ids if needed
        };

        // Try to infer specific GroupId/FarmerId if relevant for UI (e.g. if one side is Farmer)
        if (note.DebitAccountType == "Farmer") note.FarmerId = note.DebitAccountId;
        else if (note.CreditAccountType == "Farmer") note.FarmerId = note.CreditAccountId;

        if (note.DebitAccountType == "GrowerGroup") note.GroupId = note.DebitAccountId;
        else if (note.CreditAccountType == "GrowerGroup") note.GroupId = note.CreditAccountId;

        note.CreditAccountName = await GetAccountNameAsync(note.CreditAccountType, note.CreditAccountId);
        note.DebitAccountName = await GetAccountNameAsync(note.DebitAccountType, note.DebitAccountId);

        // Populate Objects for View
        if (note.FarmerId > 0) note.Farmer = await _context.Farmers.FindAsync(note.FarmerId);
        if (note.GroupId > 0) note.GrowerGroup = await _context.GrowerGroups.FindAsync(note.GroupId);

        // Dummy Details
        note.Details = new List<CreditNoteDetail> { new CreditNoteDetail { Amount = ge.Amount, AccountType = "General" } };

        return note;
    }

    public async Task<(bool success, string message)> CreateBatchCreditNoteAsync(GeneralEntryBatchModel model)
    {
        var currentUser = GetCurrentUsername();
        if (model == null || model.Entries == null || model.Entries.Count == 0)
        {
            return (false, "No entries to save.");
        }

        try
        {
            // Generate Voucher Number CNXXXXXX
            var lastGe = await _context.GeneralEntries
               .Where(g => g.VoucherType == "Credit Note")
               .OrderByDescending(g => g.Id) 
               .FirstOrDefaultAsync();
             
             var nextNo = $"CN{DateTime.Now:yyyyMM}001";
             if (lastGe != null && lastGe.VoucherNo.StartsWith($"CN{DateTime.Now:yyyyMM}"))
             {
                 if (int.TryParse(lastGe.VoucherNo.Substring(8), out int lastNum))
                 {
                      nextNo = $"CN{DateTime.Now:yyyyMM}{(lastNum + 1):D3}";
                 }
             }

            foreach (var entryData in model.Entries)
            {
                var ge = new GeneralEntry
                {
                    VoucherNo = nextNo,
                    EntryDate = model.EntryDate,
                    MobileNo = model.MobileNo,
                    VoucherType = "Credit Note",

                    // ✅ Correct split posting
                    DebitAccountId = entryData.Type == "Debit" ? entryData.AccountId : null,
                    DebitAccountType = entryData.Type == "Debit" ? entryData.AccountType : null,

                    CreditAccountId = entryData.Type == "Credit" ? entryData.AccountId : null,
                    CreditAccountType = entryData.Type == "Credit" ? entryData.AccountType : null,

                    Amount = entryData.Amount,
                    Narration = entryData.Narration,
                    Status = "UnApproved",
                    Unit = entryData.Unit,
                    EntryForId = entryData.EntryForId,
                    EntryForName = entryData.EntryForName,
                    CreatedAt = DateTime.Now,
                    CreatedBy = currentUser,
                    IsActive = true,
                    Type = "CreditNote"
                };

                _context.GeneralEntries.Add(ge);
            }

            await _context.SaveChangesAsync();
            return (true, "Created successfully");
        }
        catch (Exception ex)
        {
            return (false, "Error: " + ex.Message);
        }
    }

    public async Task<(bool success, string message)> UpdateBatchCreditNoteAsync(string voucherNo, GeneralEntryBatchModel model)
    {
        try
        {
            var existingEntries = await _context.GeneralEntries.Where(r => r.VoucherNo == voucherNo).ToListAsync();
            _context.GeneralEntries.RemoveRange(existingEntries);
            await _context.SaveChangesAsync();
            
            var currentUser = GetCurrentUsername();
            foreach (var entryData in model.Entries)
            {
                var ge = new GeneralEntry
                {
                    VoucherNo = voucherNo,
                    EntryDate = model.EntryDate,
                    MobileNo = model.MobileNo,
                    VoucherType = "Credit Note",
                    DebitAccountId = entryData.Type == "Debit" ? entryData.AccountId : null,
                    DebitAccountType = entryData.Type == "Debit" ? entryData.AccountType : null,
                    CreditAccountId = entryData.Type == "Credit" ? entryData.AccountId : null,
                    CreditAccountType = entryData.Type == "Credit" ? entryData.AccountType : null,
                    Amount = entryData.Amount,
                    Narration = entryData.Narration,
                    Status = "UnApproved",
                    Unit = entryData.Unit,
                    EntryForId = entryData.EntryForId,
                    EntryForName = entryData.EntryForName,
                    CreatedAt = DateTime.Now,
                    CreatedBy = currentUser,
                    IsActive = true,
                    Type = "CreditNote"
                };
                _context.GeneralEntries.Add(ge);
            }
            await _context.SaveChangesAsync();
            return (true, "Updated successfully");
        }
        catch (Exception ex)
        {
            return (false, "Error: " + ex.Message);
        }
    }
    #endregion

    #region Detail
    public async Task<CreditNote?> GetCreditNoteByIdAsync(int id)
    {
        try
        {
            var ge = await _context.GeneralEntries.FirstOrDefaultAsync(g => g.Id == id);
            if (ge == null) return null;
            return await MapToCreditNoteAsync(ge);
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region Account Helper
    private async Task<string> GetAccountNameAsync(string type, int id)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            if (id == 0) return "N/A";
            
            string typeLower = type?.ToLower() ?? "";

            if (typeLower.Contains("farmer")) return (await context.Farmers.FindAsync(id))?.FarmerName ?? "Unknown";
            if (typeLower.Contains("growergroup")) return (await context.GrowerGroups.FindAsync(id))?.GroupName ?? "Unknown";
            if (typeLower.Contains("bankmaster")) return (await context.BankMasters.FindAsync(id))?.AccountName ?? "Unknown";
            if (typeLower.Contains("mastergroup")) return (await context.MasterGroups.FindAsync(id))?.Name ?? "Unknown";
            if (typeLower.Contains("subgroupledger")) {
                 var s = await context.SubGroupLedgers.Include(x => x.MasterGroup).FirstOrDefaultAsync(x => x.Id == id);
                 return s?.Name ?? "Unknown";
            }

            return type + " (" + id + ")";
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion


    #region Edit/Delete


    public async Task<(bool success, string message)> DeleteCreditNoteAsync(int id)
    {
        try
        {
            var ge = await _context.GeneralEntries.FindAsync(id);
            if (ge != null)
            {
                var relatedEntries = await _context.GeneralEntries
                    .Where(g => g.VoucherNo == ge.VoucherNo)
                    .ToListAsync();

                foreach (var rel in relatedEntries)
                {
                    rel.IsActive = false;
                    rel.UpdatedAt = DateTime.Now;
                    rel.UpdatedBy = GetCurrentUsername();
                }

                await _context.SaveChangesAsync();
                return (true, "Deleted successfully");
            }
            return (false, "Not found");
        }
        catch (Exception ex)
        {
            return (false, "Error: " + ex.Message);
        }
    }
    #endregion

    #region Approval
    public async Task<(bool success, string message)> ApproveCreditNoteAsync(int id)
    {
        try
        {
            var ge = await _context.GeneralEntries.FindAsync(id);
            if (ge == null) return (false, "Not found");

            var relatedEntries = await _context.GeneralEntries
                .Where(g => g.VoucherNo == ge.VoucherNo)
                .ToListAsync();

            foreach (var rel in relatedEntries)
            {
                rel.Status = "Approved";
                rel.UpdatedAt = DateTime.Now;
                rel.UpdatedBy = GetCurrentUsername();
            }

            await _context.SaveChangesAsync();
            return (true, "Approved successfully");
        }
        catch (Exception ex)
        {
            return (false, "Error: " + ex.Message);
        }
    }

    public async Task<(bool success, string message)> UnapproveCreditNoteAsync(int id)
    {
        try
        {
            var ge = await _context.GeneralEntries.FindAsync(id);
            if (ge == null) return (false, "Not found");

            var relatedEntries = await _context.GeneralEntries
                .Where(g => g.VoucherNo == ge.VoucherNo)
                .ToListAsync();

            foreach (var rel in relatedEntries)
            {
                rel.Status = "UnApproved";
                rel.UpdatedAt = DateTime.Now;
                rel.UpdatedBy = GetCurrentUsername();
            }

            await _context.SaveChangesAsync();
            return (true, "Unapproved successfully");
        }
        catch (Exception ex)
        {
            return (false, "Error: " + ex.Message);
        }
    }
    #endregion

    #region Lookups
    public async Task LoadDropdownsAsync(dynamic viewBag, int? growerGroupId = null, int? farmerId = null)
    {
        try
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
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IEnumerable<LookupItem>> GetFarmersByGroupAsync(int groupId)
    {
        try
        {
            return await _context.Farmers
                .Where(f => f.GroupId == groupId && f.IsActive)
                .OrderBy(f => f.FarmerName)
                .Select(f => new LookupItem { Id = f.Id, Name = f.FarmerName, Type = "Farmer" })
                .ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IEnumerable<LookupItem>> GetEntryProfilesAsync()
    {
        try
        {
             var types = new[] { "Global", "CreditNote" };
             return await _context.EntryForAccounts
                .Where(e => types.Contains(e.TransactionType)) 
                .OrderBy(e => e.AccountName)
                .Select(e => new LookupItem { Id = e.Id, Name = e.AccountName })
                .ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IEnumerable<LookupItem>> GetAccountsAsync(string? searchTerm, int? entryAccountId = null, string? type = null)
    {
        try
        {
            // Keeping Original Logic
            using var context = await _contextFactory.CreateDbContextAsync();

            var rules = await context.AccountRules
                .Where(r => r.RuleType == "AllowedNature")
                .ToListAsync();

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

            bool CheckRule(string ruleValue)
            {
                if (string.IsNullOrWhiteSpace(ruleValue)) return false;
                if (ruleValue.Equals("Both", StringComparison.OrdinalIgnoreCase)) return true;
                if (ruleValue.Equals("Cancel", StringComparison.OrdinalIgnoreCase)) return false;
                if (string.IsNullOrWhiteSpace(type)) return true; 

                if (ruleValue.Equals("Debit", StringComparison.OrdinalIgnoreCase) && type.Equals("Debit", StringComparison.OrdinalIgnoreCase)) return true;
                if (ruleValue.Equals("Credit", StringComparison.OrdinalIgnoreCase) && type.Equals("Credit", StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }

            // Helper to check if account is allowed (for global search fallback)
            bool IsAllowed(string accountType, int accountId, string? fallbackType = null, int? fallbackId = null)
            {
                string? ruleValue = GetRuleValue(accountType, accountId, entryAccountId);
                if (ruleValue != null) return CheckRule(ruleValue);

                if (fallbackType != null && fallbackId.HasValue)
                {
                    string? fallbackRuleValue = GetRuleValue(fallbackType, fallbackId.Value, entryAccountId);
                    if (fallbackRuleValue != null) return CheckRule(fallbackRuleValue);
                }
                return true; 
            }

            if (entryAccountId.HasValue)
            {
                var profileRules = rules.Where(r => r.EntryAccountId == entryAccountId.Value).ToList();

                var allowedSubGroupIds = profileRules.Where(r => r.AccountType == "SubGroupLedger" && CheckRule(r.Value)).Select(r => r.AccountId).ToHashSet();
                var allowedGrowerGroupIds = profileRules.Where(r => r.AccountType == "GrowerGroup" && CheckRule(r.Value)).Select(r => r.AccountId).ToHashSet();
                var allowedBankMasterIds = profileRules.Where(r => r.AccountType == "BankMaster" && CheckRule(r.Value)).Select(r => r.AccountId).ToHashSet();
                var allowedFarmerIds = profileRules.Where(r => r.AccountType == "Farmer" && CheckRule(r.Value)).Select(r => r.AccountId).ToHashSet();

                var bankMastersQuery = context.BankMasters.Where(bm => bm.IsActive);
                var farmersQuery = context.Farmers.Where(f => f.IsActive);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    bankMastersQuery = bankMastersQuery.Where(bm => bm.AccountName.Contains(searchTerm));
                    farmersQuery = farmersQuery.Where(f => f.FarmerName.Contains(searchTerm));
                }

                var bankMasters = await bankMastersQuery
                    .Where(bm => allowedBankMasterIds.Contains(bm.Id) || allowedSubGroupIds.Contains(bm.GroupId))
                    .OrderBy(bm => bm.AccountName).Take(50).ToListAsync();

                var farmers = await farmersQuery
                    .Where(f => allowedFarmerIds.Contains(f.Id) || allowedGrowerGroupIds.Contains(f.GroupId))
                    .OrderBy(f => f.FarmerName).Take(50).ToListAsync();

                var results = new List<LookupItem>();
                results.AddRange(bankMasters.Select(bm => new LookupItem { Id = bm.Id, Name = bm.AccountName, Type = "BankMaster", AccountNumber = bm.AccountNumber }));
                results.AddRange(farmers.Select(f => new LookupItem { Id = f.Id, Name = f.FarmerName, Type = "Farmer", AccountNumber = f.FarmerCode })); // Assuming FarmerCode

                return results.OrderBy(r => r.Name).Take(100).ToList();
            }
            else
            {
                var bankMastersQuery = context.BankMasters.Where(bm => bm.IsActive);
                var farmersQuery = context.Farmers.Where(f => f.IsActive);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    bankMastersQuery = bankMastersQuery.Where(bm => bm.AccountName.Contains(searchTerm));
                    farmersQuery = farmersQuery.Where(f => f.FarmerName.Contains(searchTerm));
                }

                var bankMasters = await bankMastersQuery.OrderBy(bm => bm.AccountName).Take(200).ToListAsync();
                var farmers = await farmersQuery.OrderBy(f => f.FarmerName).Take(200).ToListAsync();

                var results = new List<LookupItem>();
                results.AddRange(bankMasters.Where(bm => IsAllowed("BankMaster", bm.Id, "SubGroupLedger", bm.GroupId))
                    .Select(bm => new LookupItem { Id = bm.Id, Name = bm.AccountName, Type = "BankMaster", AccountNumber = bm.AccountNumber }));
                results.AddRange(farmers.Where(f => IsAllowed("Farmer", f.Id, "GrowerGroup", f.GroupId))
                    .Select(f => new LookupItem { Id = f.Id, Name = f.FarmerName, Type = "Farmer", AccountNumber = f.FarmerCode }));
                
                return results.OrderBy(r => r.Name).Take(100).ToList();
            }
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion
    
    // Poly helpers
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
         // Stub
         return null; 
    }

    public async Task<IEnumerable<string>> GetUnitNamesAsync()
    {
        return await _context.UnitMasters.Select(u => u.UnitName ?? "").Distinct().ToListAsync();
    }
}
