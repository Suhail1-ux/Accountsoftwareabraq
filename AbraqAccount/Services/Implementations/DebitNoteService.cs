using Microsoft.EntityFrameworkCore;
using AbraqAccount.Data;
using AbraqAccount.Models;
using AbraqAccount.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using AbraqAccount.Models.Common;
using AbraqAccount.Extensions;

namespace AbraqAccount.Services.Implementations;

public class DebitNoteService : IDebitNoteService
{
    private readonly AppDbContext _context;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DebitNoteService(AppDbContext context, IDbContextFactory<AppDbContext> contextFactory, IHttpContextAccessor httpContextAccessor)
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
    public async Task<(List<DebitNote> notes, int totalCount, int totalPages)> GetDebitNotesAsync(
        string? unit, string? debitNoteNo, string? accountName, string? status, 
        DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        try
        {
            var query = _context.GeneralEntries
                .Where(g => g.VoucherType == "Debit Note" && (status == "Deleted" ? !g.IsActive : g.IsActive))
                .AsQueryable();

            if (!string.IsNullOrEmpty(unit) && unit != "ALL") query = query.Where(d => d.Unit == unit);
            if (!string.IsNullOrEmpty(debitNoteNo)) query = query.Where(d => d.VoucherNo.Contains(debitNoteNo));
            if (!string.IsNullOrEmpty(status) && status != "ALL" && status != "Deleted") query = query.Where(d => d.Status == status);
            if (fromDate.HasValue) query = query.Where(d => d.EntryDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(d => d.EntryDate <= toDate.Value);

            // 1. Get unique VoucherNos for pagination
            var voucherQuery = query.Select(x => x.VoucherNo).Distinct();
            
            var totalCount = await voucherQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // 2. Get specific VoucherNos for this page
            var pageVoucherNos = await voucherQuery
                .OrderByDescending(v => v)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 3. Fetch all entries for these vouchers
            var generalEntries = await _context.GeneralEntries
                .Where(x => pageVoucherNos.Contains(x.VoucherNo))
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            var notes = new List<DebitNote>();
            var groupedEntries = generalEntries.GroupBy(x => x.VoucherNo);

            foreach (var group in groupedEntries)
            {
                var first = group.First();
                
                 // Calculate Totals and aggregate names
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

                var note = await MapToDebitNoteAsync(first);
                
                 // Override with aggregated values
                note.Amount = displayAmount;
                note.CreditAccountName = string.Join(", ", creditNames.Distinct());
                if (creditNames.Count > 1) note.CreditAccountName += " (Split)";

                note.DebitAccountName = string.Join(", ", debitNames.Distinct());
                if (debitNames.Count > 1) note.DebitAccountName += " (Split)";

                notes.Add(note);
            }
            
            // Re-apply sorting to match pagination
            notes = notes.OrderByDescending(d => d.DebitNoteNo).ToList();

            if (!string.IsNullOrEmpty(accountName))
            {
                 notes = notes
                    .Where(d => (d.CreditAccountName != null && d.CreditAccountName.Contains(accountName, StringComparison.OrdinalIgnoreCase)) 
                             || (d.DebitAccountName != null && d.DebitAccountName.Contains(accountName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                 // Note: Filtering after pagination is imperfect but necessary without JOINs.
            }

            return (notes, totalCount, totalPages);
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task<DebitNote> MapToDebitNoteAsync(GeneralEntry ge)
    {
        var note = new DebitNote
        {
            Id = ge.Id,
            DebitNoteNo = ge.VoucherNo,
            DebitNoteDate = ge.EntryDate,
            DebitAccountId = ge.DebitAccountId ?? 0,
            DebitAccountType = ge.DebitAccountType,
            CreditAccountId = ge.CreditAccountId ?? 0,
            CreditAccountType = ge.CreditAccountType,
            Amount = ge.Amount,
            Narration = ge.Narration,
            Status = ge.Status,
            Unit = ge.Unit,
            EntryForId = ge.EntryForId,
            EntryForName = ge.EntryForName,
            CreatedAt = ge.CreatedAt,
            CreatedBy = ge.CreatedBy,
            IsActive = ge.IsActive,
            // Map BankMasterId from legacy mapping logic? 
            // We can try to revive it or assume it's obsolete. 
            // Given consolidation, we should rely on Accounts.
        };

        note.DebitAccountName = await GetAccountNameAsync(note.DebitAccountType, note.DebitAccountId);
        note.CreditAccountName = await GetAccountNameAsync(note.CreditAccountType, note.CreditAccountId);

        // Mock Details for UI compatibility
        note.Details = new List<DebitNoteDetail>
        {
            new DebitNoteDetail
            {
                Amount = ge.Amount,
                AccountType = "General"
            }
        };

        return note;
    }
    #endregion

    #region Create
    public async Task<(bool success, string message)> CreateBatchDebitNoteAsync(GeneralEntryBatchModel model)
    {
        var currentUser = GetCurrentUsername();
        if (model == null || model.Entries == null || model.Entries.Count == 0)
        {
            return (false, "No entries to save.");
        }

        try
        {
            // Generate Voucher Number DNXXXXXX
            var lastGe = await _context.GeneralEntries
                .Where(g => g.VoucherType == "Debit Note")
                .OrderByDescending(g => g.Id)
                .FirstOrDefaultAsync();

            int nextNoteNo = 1;
            if (lastGe != null && lastGe.VoucherNo.StartsWith("DN"))
            {
                if (int.TryParse(lastGe.VoucherNo.Replace("DN", ""), out int lastNum))
                    nextNoteNo = lastNum + 1;
            }
            string voucherNo = $"DN{nextNoteNo:D6}";

            foreach (var entryData in model.Entries)
            {
                var ge = new GeneralEntry
                {
                    VoucherNo = voucherNo,
                    EntryDate = model.EntryDate,
                    MobileNo = model.MobileNo,
                    VoucherType = "Debit Note",
                    // Split Posting:
                    // If Debit: DebitAccount = Selected, CreditAccount = null
                    // If Credit: CreditAccount = Selected, DebitAccount = null
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
                    Type = "DebitNote"
                };
                _context.GeneralEntries.Add(ge);
            }

            await _context.SaveChangesAsync();
            return (true, $"Debit Note created successfully! Voucher: {voucherNo}");
        }
        catch (Exception ex)
        {
            return (false, "Error: " + ex.Message);
        }
    }

    public async Task<(bool success, string message)> UpdateBatchDebitNoteAsync(string voucherNo, GeneralEntryBatchModel model)
    {
        try
        {
            var existingEntries = await _context.GeneralEntries.Where(r => r.VoucherNo == voucherNo).ToListAsync();
            _context.GeneralEntries.RemoveRange(existingEntries);
            await _context.SaveChangesAsync();
            
            // Re-create with same voucher number logic (need to ensure CreateBatch version can take a preset voucherNo)
            // For now, I'll inline the creation part to keep it simple and ensure same voucherNo.
            
            var currentUser = GetCurrentUsername();
            foreach (var entryData in model.Entries)
            {
                var ge = new GeneralEntry
                {
                    VoucherNo = voucherNo,
                    EntryDate = model.EntryDate,
                    MobileNo = model.MobileNo,
                    VoucherType = "Debit Note",
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
                    Type = "DebitNote"
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

    #region Delete
    public async Task<(bool success, string message)> DeleteDebitNoteAsync(int id)
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
                 rel.IsActive = false;
                 rel.UpdatedAt = DateTime.Now;
                 rel.UpdatedBy = GetCurrentUsername();
             }

             await _context.SaveChangesAsync();
             return (true, "Deleted successfully");
        }
        catch (Exception ex)
        {
             return (false, "Error: " + ex.Message);
        }
    }
    #endregion

    #region Detail
    public async Task<DebitNote?> GetDebitNoteByIdAsync(int id)
    {
        try
        {
            var ge = await _context.GeneralEntries.FirstOrDefaultAsync(g => g.Id == id);
            if (ge == null) return null;
            return await MapToDebitNoteAsync(ge);
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion



    #region Approval
    public async Task<(bool success, string message)> ApproveDebitNoteAsync(int id)
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

    public async Task<(bool success, string message)> UnapproveDebitNoteAsync(int id)
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
    public async Task LoadDropdownsAsync(dynamic viewBag)
    {
        try
        {
            var unitList = new List<SelectListItem>
            {
                new SelectListItem { Value = "UNIT-1", Text = "UNIT-1" },
                new SelectListItem { Value = "UNIT-2", Text = "UNIT-2" },
                new SelectListItem { Value = "Abraq Agro Fresh LLP", Text = "Abraq Agro Fresh LLP" }
            };
            viewBag.UnitList = new SelectList(unitList, "Value", "Text");

            var statusList = new List<SelectListItem>
            {
                new SelectListItem { Value = "UnApproved", Text = "UnApproved" },
                new SelectListItem { Value = "Approved", Text = "Approved" },
                new SelectListItem { Value = "Rejected", Text = "Rejected" }
            };
            viewBag.StatusList = new SelectList(statusList, "Value", "Text");

             var accountTypeList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Select" },
                new SelectListItem { Value = "Discount", Text = "Discount" },
                new SelectListItem { Value = "Return", Text = "Return" },
                new SelectListItem { Value = "Freight Expense", Text = "Freight Expense" },
                new SelectListItem { Value = "Debit Note", Text = "Debit Note" },
                new SelectListItem { Value = "Packing Charges", Text = "Packing Charges" },
                new SelectListItem { Value = "Carriage Expense", Text = "Carriage Expense" },
                new SelectListItem { Value = "TCS on sale (0.1%)", Text = "TCS on sale (0.1%)" }
            };
            viewBag.AccountTypeList = new SelectList(accountTypeList, "Value", "Text");

            var entryProfiles = await GetEntryProfilesAsync();
            viewBag.EntryProfiles = new SelectList(entryProfiles, "Value", "Name");
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
            using var context = await _contextFactory.CreateDbContextAsync();
            var rules = await context.AccountRules.Where(r => r.RuleType == "AllowedNature").ToListAsync();

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

            bool IsAllowed(string accountType, int accountId, string? fallbackType = null, int? fallbackId = null)
            {
                string? ruleValue = GetRuleValue(accountType, accountId, entryAccountId);
                if (ruleValue != null)
                {
                    return CheckRule(ruleValue);
                }

                if (fallbackType != null && fallbackId.HasValue)
                {
                    string? fallbackRuleValue = GetRuleValue(fallbackType, fallbackId.Value, entryAccountId);
                    if (fallbackRuleValue != null)
                    {
                        return CheckRule(fallbackRuleValue);
                    }
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

                var bankMasters = await context.BankMasters
                    .Where(bm => bm.IsActive && (string.IsNullOrEmpty(searchTerm) || bm.AccountName.Contains(searchTerm)))
                    .Where(bm => allowedBankMasterIds.Contains(bm.Id) || allowedSubGroupIds.Contains(bm.GroupId))
                    .OrderBy(bm => bm.AccountName).Take(50).ToListAsync();

                var farmers = await context.Farmers
                    .Where(f => f.IsActive && (string.IsNullOrEmpty(searchTerm) || f.FarmerName.Contains(searchTerm)))
                    .Where(f => allowedFarmerIds.Contains(f.Id) || allowedGrowerGroupIds.Contains(f.GroupId))
                    .OrderBy(f => f.FarmerName).Take(50).ToListAsync();

                var results = new List<LookupItem>();
                results.AddRange(bankMasters.Select(bm => new LookupItem { Id = bm.Id, Name = bm.AccountName, Type = "BankMaster", AccountNumber = bm.AccountNumber }));
                results.AddRange(farmers.Select(f => new LookupItem { Id = f.Id, Name = f.FarmerName, Type = "Farmer", AccountNumber = f.FarmerCode }));
                return results;
            }
            else
            {
                var bankMasters = await context.BankMasters
                   .Where(bm => bm.IsActive && (string.IsNullOrEmpty(searchTerm) || bm.AccountName.Contains(searchTerm)))
                   .OrderBy(bm => bm.AccountName).Take(200).ToListAsync();

                var farmers = await context.Farmers
                   .Where(f => f.IsActive && (string.IsNullOrEmpty(searchTerm) || f.FarmerName.Contains(searchTerm)))
                   .OrderBy(f => f.FarmerName).Take(200).ToListAsync();
                
                 var results = new List<LookupItem>();
                results.AddRange(bankMasters.Where(bm => IsAllowed("BankMaster", bm.Id, "SubGroupLedger", bm.GroupId))
                    .Select(bm => new LookupItem { Id = bm.Id, Name = bm.AccountName, Type = "BankMaster", AccountNumber = bm.AccountNumber }));
                results.AddRange(farmers.Where(f => IsAllowed("Farmer", f.Id, "GrowerGroup", f.GroupId))
                    .Select(f => new LookupItem { Id = f.Id, Name = f.FarmerName, Type = "Farmer", AccountNumber = f.FarmerCode }));
                
                return results;
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task<string> GetAccountNameAsync(string type, int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync(); 
        if (id == 0) return "N/A";
        string typeLower = type?.ToLower() ?? "";

        if (typeLower.Contains("farmer")) return (await context.Farmers.FindAsync(id))?.FarmerName ?? "";
        if (typeLower.Contains("bankmaster")) return (await context.BankMasters.FindAsync(id))?.AccountName ?? "";
        if (typeLower.Contains("growergroup")) return (await context.GrowerGroups.FindAsync(id))?.GroupName ?? "";
        if (typeLower.Contains("mastergroup")) return (await context.MasterGroups.FindAsync(id))?.Name ?? "";
        if (typeLower.Contains("subgroupledger")) {
             var s = await context.SubGroupLedgers.Include(x => x.MasterGroup).Include(x => x.MasterSubGroup).FirstOrDefaultAsync(x => x.Id == id);
             return s?.Name ?? "";
        }

        return type;
    }

    public async Task PopulateAccountNamesAsync(IEnumerable<DebitNote> notes)
    {
        foreach (var note in notes)
        {
            note.DebitAccountName = await GetAccountNameAsync(note.DebitAccountType, note.DebitAccountId);
            note.CreditAccountName = await GetAccountNameAsync(note.CreditAccountType, note.CreditAccountId);
        }
    }
    
    public async Task<int?> GetEntryProfileIdAsync(int creditAccountId, string creditType, int debitAccountId, string debitType)
    {
         using var context = await _contextFactory.CreateDbContextAsync();
         var creditRule = await context.AccountRules.FirstOrDefaultAsync(r => r.AccountType == creditType && r.AccountId == creditAccountId && r.EntryAccountId != null);
         if (creditRule != null) return creditRule.EntryAccountId;
         
          var debitRule = await context.AccountRules.FirstOrDefaultAsync(r => r.AccountType == debitType && r.AccountId == debitAccountId && r.EntryAccountId != null);
         if (debitRule != null) return debitRule.EntryAccountId;
         
         return null;
    }

    public async Task<IEnumerable<LookupItem>> GetEntryProfilesAsync()
    {
         return await _context.EntryForAccounts
                .Where(e => e.TransactionType == "DebitNote") 
                .OrderBy(e => e.AccountName)
                .Select(e => new LookupItem { Id = e.Id, Name = e.AccountName })
                .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetUnitNamesAsync()
    {
        return await _context.UnitMasters.Select(u => u.UnitName ?? "").Distinct().ToListAsync();
    }


    #endregion
}
