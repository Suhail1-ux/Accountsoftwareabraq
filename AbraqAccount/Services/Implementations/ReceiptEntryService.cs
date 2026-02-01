using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AbraqAccount.Data;
using AbraqAccount.Models;
using AbraqAccount.Services.Interfaces;
using AbraqAccount.Models.Common;
using AbraqAccount.Extensions;
using Microsoft.AspNetCore.Http;

namespace AbraqAccount.Services.Implementations;

public class ReceiptEntryService : IReceiptEntryService
{
    private readonly AppDbContext _context;
    private readonly ITransactionEntriesService _transactionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ReceiptEntryService(AppDbContext context, ITransactionEntriesService transactionService, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _transactionService = transactionService;
        _httpContextAccessor = httpContextAccessor;
    }

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

    #region Retrieval
    public async Task<(List<ReceiptEntryGroupViewModel> groups, int totalCount, int totalPages)> GetReceiptEntriesAsync(
        string? voucherNo, 
        string? growerGroup, 
        string? growerName, 
        string? unit,
        string? status, 
        DateTime? fromDate, 
        DateTime? toDate, 
        int page, 
        int pageSize)
    {
        try
        {
            // Query GeneralEntries with VoucherType = "Receipt Entry"
            var query = _context.GeneralEntries
                .Where(r => r.IsActive && r.VoucherType == "Receipt Entry")
                .AsQueryable();
            
            if (!string.IsNullOrEmpty(unit) && unit != "ALL")
            {
                query = query.Where(r => r.Unit == unit);
            }

            if (!string.IsNullOrEmpty(voucherNo))
            {
                query = query.Where(r => r.VoucherNo.Contains(voucherNo));
            }

            // For searching by Name/Group, we might need to filter by CreditAccountType/DebitAccountType logic
            // Ideally we should use the mapped names if possible, but they are not in DB.
            // Simplified: Filter if ID is in list of matching IDs.
            // This is complex with polymorphism. For now, assuming basic filtering or strict exact match if needed.
            // Skipping complex text search on polymorphic tables for speed unless requested.

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(r => r.EntryDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(r => r.EntryDate <= toDate.Value);
            }

            // Get matching voucher numbers first
            var matchingVoucherNos = await query
                .Select(r => r.VoucherNo)
                .Distinct()
                .ToListAsync();

            // Fetch all entries for these vouchers
            var allReceiptEntries = await _context.GeneralEntries
                .Where(r => r.IsActive && r.VoucherType == "Receipt Entry" && matchingVoucherNos.Contains(r.VoucherNo))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            
            // Group entries by VoucherNo
            var groupedEntries = allReceiptEntries
                .GroupBy(r => r.VoucherNo)
                .Select(g => new
                {
                    VoucherNo = g.Key,
                    Entries = g.OrderBy(e => e.CreatedAt).ToList()
                })
                .OrderByDescending(g => g.Entries.First().CreatedAt)
                .ToList();
            
            // Pagination
            var totalGroups = groupedEntries.Count;
            var totalPages = (int)Math.Ceiling(totalGroups / (double)pageSize);
             var paginatedGroups = groupedEntries
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            var groupedViewModels = new List<ReceiptEntryGroupViewModel>();
            
            foreach (var group in paginatedGroups)
            {
                // In Mediator pattern for Receipts:
                // Credit Entry (Party -> Mediator) had: CreditAccount = Party, DebitAccount = Mediator.
                // Debit Entry (Mediator -> Party) had: DebitAccount = Party, CreditAccount = Mediator.
                // We identify "Customer/Party" side.
                // Usually Receipts = Incoming Money. So Credit Account is the Payer.
                
                // Let's find entries where we are Crediting a Party (not the mediator).
                // Mediator is typically MasterGroup.
                // We can assume the "Real" entries are those where CreditAccountType != "Mediator" or just take all.
                
                // To reconstuct the ViewModel, we sum all amounts.
                var firstEntry = group.Entries.First();
                
                // Calculate total amount from unique lines? 
                // GeneralEntries are paired. Total Amount of the Voucher is sum of all lines? No.
                // The Voucher Total is the sum of Amounts of the entries.
                // Use the sum of amounts of all entries? Or just one side? 
                // All entries in GeneralEntries are balanced.
                // For a Receipt, usually we just sum the amount column of all rows?
                // No, if we have 2 lines (Cr Party A 100, Dr Cash 100) -> 2 rows? 
                // Wait, GeneralEntry is a PAIR. So 1 Row = 1 Debit + 1 Credit.
                // So 1 Row = Amount 100.
                // So Sum(Amount) is the total voucher value.
                
                decimal totalAmount = group.Entries.Sum(e => e.Amount);
                
                // Aggregate Names
                var creditNames = new List<string>();
                var debitNames = new List<string>();

                foreach(var entry in group.Entries)
                {
                    if (entry.CreditAccountId.HasValue) 
                        creditNames.Add(await GetAccountNameAsync(entry.CreditAccountId.Value, entry.CreditAccountType));
                    
                    if (entry.DebitAccountId.HasValue) 
                        debitNames.Add(await GetAccountNameAsync(entry.DebitAccountId.Value, entry.DebitAccountType));
                }

                var viewModel = new ReceiptEntryGroupViewModel
                {
                    VoucherNo = group.VoucherNo,
                    ReceiptDate = firstEntry.EntryDate,
                    CreditAccountName = string.Join(", ", creditNames.Distinct()),
                    DebitAccountName = string.Join(", ", debitNames.Distinct()),
                    EntryForName = firstEntry.EntryForName,
                    ReceiptAmount = totalAmount,
                    Status = firstEntry.Status,
                    CreditEntryId = firstEntry.Id, 
                    DebitEntryId = 0,
                    Unit = firstEntry.Unit
                };
                
                if (creditNames.Count > 1) viewModel.CreditAccountName += " (Split)";
                if (debitNames.Count > 1) viewModel.DebitAccountName += " (Split)";
                
                groupedViewModels.Add(viewModel);
            }

            return (groupedViewModels, totalGroups, totalPages);
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region Management
    public async Task<(bool success, string message)> CreateMultipleReceiptsAsync(ReceiptEntryBatchModel model, string? existingVoucherNo = null)
    {
        var currentUser = GetCurrentUsername();
        if (model == null || model.Entries == null || model.Entries.Count == 0)
        {
            return (false, "No entries to save.");
        }

        try
        {
             // Generate Voucher Number if not provided
            string voucherNo = existingVoucherNo;
            if (string.IsNullOrEmpty(voucherNo))
            {
                var lastEntry = await _context.GeneralEntries
                    .Where(r => r.VoucherType == "Receipt Entry")
                    .OrderByDescending(r => r.Id)
                    .FirstOrDefaultAsync();
                
                int nextNumber = 1;
                if (lastEntry != null && !string.IsNullOrEmpty(lastEntry.VoucherNo))
                {
                    var parts = lastEntry.VoucherNo.Split('/');
                    if (parts.Length > 0)
                    {
                        var numberPart = parts[parts.Length - 1];
                        if (int.TryParse(numberPart, out int lastNum)) nextNumber = lastNum + 1;
                    }
                }
                
                var currentYear = DateTime.Now.Year;
                var yearShort = currentYear.ToString().Substring(2);
                var nextYear = (currentYear + 1).ToString().Substring(2);
                voucherNo = $"RCPT/A/{yearShort}-{nextYear}/{nextNumber:D4}";
            }

            // Mediator (Internal Account/Cash)
            var mediatorAccount = await _context.MasterGroups.OrderBy(mg => mg.Id).FirstOrDefaultAsync();
            int mediatorId = mediatorAccount?.Id ?? 1;

            foreach (var entryData in model.Entries)
            {
                var ge = new GeneralEntry
                {
                    VoucherNo = voucherNo,
                    EntryDate = model.ReceiptDate,
                    MobileNo = model.MobileNo,
                    VoucherType = "Receipt Entry",
                    PaymentType = entryData.PaymentType,
                    Amount = entryData.Amount,
                    ReferenceNo = entryData.RefNoChequeUTR, // Mapped
                    Narration = entryData.Narration,
                    Status = "Unapproved",
                    CreatedAt = DateTime.Now,
                    CreatedBy = currentUser,
                    IsActive = true,
                    Unit = entryData.Unit,
                    PaymentFromSubGroupId = entryData.PaymentFromSubGroupId,
                    EntryAccountId = entryData.EntryAccountId,
                    EntryForId = entryData.EntryForId,
                    EntryForName = entryData.EntryForName,
                    Type = entryData.Type 
                };

                // Correct split posting as requested
                ge.DebitAccountId = entryData.Type == "Debit" ? entryData.AccountId : null;
                ge.DebitAccountType = entryData.Type == "Debit" ? entryData.AccountType : null;
                
                ge.CreditAccountId = entryData.Type == "Credit" ? entryData.AccountId : null;
                ge.CreditAccountType = entryData.Type == "Credit" ? entryData.AccountType : null;

                _context.GeneralEntries.Add(ge);
            }

            await _context.SaveChangesAsync();
            return (true, "Receipt Entry created successfully!");
        }
        catch (Exception ex)
        {
            return (false, "An error occurred while saving: " + ex.Message);
        }
    }
    #endregion

    #region Details
    public async Task<(bool success, object? data, string? error)> GetVoucherDetailsAsync(string voucherNo)
    {
        try
        {
            var entries = await _context.GeneralEntries
                .Where(r => r.VoucherNo == voucherNo && r.IsActive)
                .ToListAsync();
            
            if (entries == null || entries.Count == 0) return (false, null, "Voucher not found");

            // Reconstruct the view model structure
            // In the form we have "Credit" entries and "Debit" entries.
            // In GeneralEntry, a "Credit" type entry was stored as Cr=Party, Dr=Mediator.
            // We use the `Type` column which we saved as "Credit" or "Debit" to distinguish.
            
            var creditEntries = entries.Where(e => e.Type == "Credit").ToList();
            var debitEntries = entries.Where(e => e.Type == "Debit").ToList();

            var firstCredit = creditEntries.FirstOrDefault() ?? entries.First();
            var firstDebit = debitEntries.FirstOrDefault();

             // Aggregate Credit Names
            var creditNames = new List<string>();
            foreach (var ce in creditEntries)
            {
                var name = await GetAccountNameAsync(ce.CreditAccountId ?? 0, ce.CreditAccountType);
                if (!string.IsNullOrEmpty(name)) creditNames.Add(name);
            }
            var creditAccountName = string.Join(", ", creditNames.Distinct());

            // Aggregate Debit Names
            var debitNames = new List<string>();
            foreach (var de in debitEntries)
            {
                 var name = await GetAccountNameAsync(de.DebitAccountId ?? 0, de.DebitAccountType);
                 if (!string.IsNullOrEmpty(name)) debitNames.Add(name);
            }
            var debitAccountName = string.Join(", ", debitNames.Distinct());

            var result = new
            {
                success = true,
                credit = new
                {
                    voucherNo = firstCredit.VoucherNo,
                    accountName = creditAccountName,
                    amount = creditEntries.Sum(e => e.Amount),
                    paymentType = firstCredit.PaymentType,
                    refNo = firstCredit.ReferenceNo,
                    narration = firstCredit.Narration,
                    date = firstCredit.EntryDate.ToString("dd/MM/yyyy"),
                    unit = firstCredit.Unit
                },
                debit = firstDebit != null ? new
                {
                    voucherNo = firstDebit.VoucherNo,
                    accountName = debitAccountName,
                    amount = debitEntries.Sum(e => e.Amount),
                    paymentType = firstDebit.PaymentType,
                    refNo = firstDebit.ReferenceNo,
                    narration = firstDebit.Narration,
                    date = firstDebit.EntryDate.ToString("dd/MM/yyyy"),
                    unit = firstDebit.Unit
                } : null
            };

            return (true, result, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }
    #endregion

    #region Delete / Approve / Update
    public async Task<(bool success, string message)> DeleteReceiptEntryAsync(int id)
    {
         // Find by ID? No, usually delete by VoucherNo or Group. 
         // ID passed is one of the GeneralEntry IDs.
         var entry = await _context.GeneralEntries.FindAsync(id);
         if(entry == null) return (false, "Entry not found");

         var allEntries = await _context.GeneralEntries.Where(g => g.VoucherNo == entry.VoucherNo).ToListAsync();
         var currentUser = GetCurrentUsername();
         foreach(var e in allEntries) {
             e.IsActive = false; 
             e.UpdatedBy = currentUser;
             e.UpdatedAt = DateTime.Now;
             _context.Update(e);
             // Or Remove?
             // _context.GeneralEntries.Remove(e); // Hard delete or soft?
             // Let's soft delete for safety or Remove if we want clean slate.
             // Given consolidation, remove might be cleaner.
             _context.GeneralEntries.Remove(e);
         }
         await _context.SaveChangesAsync();
         return (true, "Deleted successfully");
    }

    public async Task<(bool success, string message)> UpdateReceiptVoucherAsync(string voucherNo, ReceiptEntryBatchModel model)
    {
        // ... (Logic similar to Create, remove old by VoucherNo, add new)
         try
        {
            var existingEntries = await _context.GeneralEntries.Where(r => r.VoucherNo == voucherNo).ToListAsync();
            _context.GeneralEntries.RemoveRange(existingEntries);
            await _context.SaveChangesAsync();
            return await CreateMultipleReceiptsAsync(model, voucherNo);
        }
        catch(Exception ex) { return (false, ex.Message); }
    }
    
    // Adjusted Update for generic use
    public async Task<(bool success, string message)> UpdateReceiptEntryAsync(ReceiptEntry model) {
        return (false, "Not Implemented - Use Batch Update");
    }

    public async Task<(bool success, string message)> ApproveReceiptEntryAsync(int id)
    {
         var entry = await _context.GeneralEntries.FindAsync(id);
         if(entry == null) return (false, "Entry not found");
         var allEntries = await _context.GeneralEntries.Where(g => g.VoucherNo == entry.VoucherNo).ToListAsync();
         foreach(var e in allEntries) {
             e.Status = "Approved";
             _context.Update(e);
         }
         await _context.SaveChangesAsync();
         return (true, "Approved successfully");
    }

    public async Task<(bool success, string message)> UnapproveReceiptEntryAsync(int id)
    {
         var entry = await _context.GeneralEntries.FindAsync(id);
         if(entry == null) return (false, "Entry not found");
         var allEntries = await _context.GeneralEntries.Where(g => g.VoucherNo == entry.VoucherNo).ToListAsync();
         foreach(var e in allEntries) {
             e.Status = "Unapproved";
             _context.Update(e);
         }
         await _context.SaveChangesAsync();
         return (true, "Unapproved successfully");
    }
    #endregion

    #region Helpers 
    public async Task<string> GetAccountNameAsync(int accountId, string accountType)
    {
        try
        {
            if (string.IsNullOrEmpty(accountType)) return "";
            if (string.Equals(accountType, "BankMaster", StringComparison.OrdinalIgnoreCase))
            {
                var bank = await _context.BankMasters.FindAsync(accountId);
                return bank?.AccountName ?? "";
            }
            // ... (Other lookups same as before)
            else if (string.Equals(accountType, "MasterGroup", StringComparison.OrdinalIgnoreCase))
            {
                var group = await _context.MasterGroups.FindAsync(accountId);
                return group?.Name ?? "";
            }
             else if (string.Equals(accountType, "MasterSubGroup", StringComparison.OrdinalIgnoreCase))
            {
                var subGroup = await _context.MasterSubGroups.Include(msg => msg.MasterGroup).FirstOrDefaultAsync(msg => msg.Id == accountId);
                return subGroup != null ? $"{subGroup.MasterGroup?.Name ?? ""} - {subGroup.Name}" : "";
            }
            else if (string.Equals(accountType, "SubGroupLedger", StringComparison.OrdinalIgnoreCase))
            {
                var ledger = await _context.SubGroupLedgers.Include(sgl => sgl.MasterGroup).Include(sgl => sgl.MasterSubGroup).FirstOrDefaultAsync(sgl => sgl.Id == accountId);
                return ledger != null ? $"{ledger.MasterGroup?.Name ?? ""} - {ledger.MasterSubGroup?.Name ?? ""} - {ledger.Name}" : "";
            }
            else if (string.Equals(accountType, "Farmer", StringComparison.OrdinalIgnoreCase))
            {
                 var farmer = await _context.Farmers.FindAsync(accountId);
                 return farmer?.FarmerName ?? "";
            }
            return "";
        }
        catch { return ""; }
    }
    public async Task LoadDropdownsAsync(dynamic viewBag)
    {
        try
        {
            var typeList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Credit", Text = "Credit" },
                new SelectListItem { Value = "Debit", Text = "Debit" }
            };
            viewBag.TypeList = new SelectList(typeList, "Value", "Text");

            var paymentTypeList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Mobile Pay", Text = "Mobile Pay" },
                new SelectListItem { Value = "Cash", Text = "Cash" },
                new SelectListItem { Value = "Cheque", Text = "Cheque" },
                new SelectListItem { Value = "UTR", Text = "UTR" },
                new SelectListItem { Value = "NEFT", Text = "NEFT" },
                new SelectListItem { Value = "RTGS", Text = "RTGS" }
            };
            viewBag.PaymentTypeList = new SelectList(paymentTypeList, "Value", "Text");

            // Fetch Units
            var units = await GetUnitNamesAsync();
            viewBag.Units = units;

            var entryAccounts = await _context.EntryForAccounts
                .Where(e => e.TransactionType == "ReceiptEntry")
                .OrderBy(e => e.AccountName)
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = e.AccountName
                })
                .ToListAsync();
            viewBag.EntryProfiles = new SelectList(entryAccounts, "Value", "Text");
        }
        catch (Exception)
        {
             throw;
        }
    }

    // Mapping GeneralEntry back to ReceiptEntry for compatibility
    private ReceiptEntry MapToReceiptEntry(GeneralEntry ge)
    {
        if (ge == null) return null;
        
        // Determine AccountId/Type based on Type
        // If stored "Credit", means Party was Credited. So we return CreditAccount details.
        // If stored "Debit", means Party was Debited. So we return DebitAccount details.
        int accountId = 0;
        string accountType = "";
        
        if (ge.Type == "Credit")
        {
            accountId = ge.CreditAccountId ?? 0;
            accountType = ge.CreditAccountType;
        }
        else
        {
            accountId = ge.DebitAccountId ?? 0;
            accountType = ge.DebitAccountType;
        }

        return new ReceiptEntry
        {
            Id = ge.Id,
            VoucherNo = ge.VoucherNo,
            ReceiptDate = ge.EntryDate,
            MobileNo = ge.MobileNo,
            Type = ge.Type ?? "Credit", // Default
            AccountId = accountId,
            AccountType = accountType,
            PaymentType = ge.PaymentType ?? "",
            Amount = ge.Amount,
            RefNoChequeUTR = ge.ReferenceNo,
            Narration = ge.Narration,
            Status = ge.Status,
            CreatedAt = ge.CreatedAt,
            CreatedBy = ge.CreatedBy,
            UpdatedAt = ge.UpdatedAt,
            UpdatedBy = ge.UpdatedBy,
            Unit = ge.Unit,
            IsActive = ge.IsActive,
            PaymentFromSubGroupId = ge.PaymentFromSubGroupId,
            EntryAccountId = ge.EntryAccountId,
            EntryForId = ge.EntryForId,
            EntryForName = ge.EntryForName
        };
    }

    public async Task<ReceiptEntry?> GetReceiptEntryByIdAsync(int id)
    {
        try
        {
            var ge = await _context.GeneralEntries.FirstOrDefaultAsync(r => r.Id == id && r.IsActive);
            return MapToReceiptEntry(ge);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<ReceiptEntry>> GetReceiptEntriesByVoucherNoAsync(string voucherNo)
    {
        try
        {
            var ges = await _context.GeneralEntries
                .Where(r => r.VoucherNo == voucherNo && r.IsActive)
                .ToListAsync();
            
            return ges.Select(MapToReceiptEntry).Where(r => r != null).ToList();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IEnumerable<LookupItem>> GetAccountsAsync(string? searchTerm, int? paymentFromId = null, string? type = null)
    {
        try
        {
            var rules = await _context.AccountRules
                .Where(r => r.RuleType == "AllowedNature")
                .ToListAsync();
            
            // Helper to check rule
            bool CheckRule(string ruleValue, string? filterType)
            {
                if (string.IsNullOrWhiteSpace(ruleValue)) return false;
                if (ruleValue.Equals("Both", StringComparison.OrdinalIgnoreCase)) return true;
                if (ruleValue.Equals("Cancel", StringComparison.OrdinalIgnoreCase)) return false;
                if (string.IsNullOrWhiteSpace(filterType)) return true; 

                if (ruleValue.Equals("Debit", StringComparison.OrdinalIgnoreCase) && filterType.Equals("Debit", StringComparison.OrdinalIgnoreCase)) return true;
                if (ruleValue.Equals("Credit", StringComparison.OrdinalIgnoreCase) && filterType.Equals("Credit", StringComparison.OrdinalIgnoreCase)) return true;
                
                return false;
            }

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

            // Fallback Ids if filtering by PaymentFromId
            HashSet<int> allowedSubGroupIds = new();
            HashSet<int> allowedGrowerGroupIds = new();
            HashSet<int> allowedBankMasterIds = new();
            HashSet<int> allowedFarmerIds = new();

            if (paymentFromId.HasValue)
            {
                 var profileRules = rules.Where(r => r.EntryAccountId == paymentFromId.Value).ToList();
                 allowedSubGroupIds = profileRules.Where(r => r.AccountType == "SubGroupLedger" && CheckRule(r.Value, type)).Select(r => r.AccountId).ToHashSet();
                 allowedGrowerGroupIds = profileRules.Where(r => r.AccountType == "GrowerGroup" && CheckRule(r.Value, type)).Select(r => r.AccountId).ToHashSet();
                 allowedBankMasterIds = profileRules.Where(r => r.AccountType == "BankMaster" && CheckRule(r.Value, type)).Select(r => r.AccountId).ToHashSet();
                 allowedFarmerIds = profileRules.Where(r => r.AccountType == "Farmer" && CheckRule(r.Value, type)).Select(r => r.AccountId).ToHashSet();
            }

            var globalBankMastersQuery = _context.BankMasters.Where(bm => bm.IsActive);
            var globalFarmersQuery = _context.Farmers.Where(f => f.IsActive);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                globalBankMastersQuery = globalBankMastersQuery.Where(bm => bm.AccountName.Contains(searchTerm));
                globalFarmersQuery = globalFarmersQuery.Where(f => f.FarmerName.Contains(searchTerm));
            }

            // Logic: If PaymentFromId is present, we filter *strictly* by allowed IDs from rules? 
            // The original logic was complex. Let's simplify: 
            // If paymentFromId is provided, we prefer strict filtering. 
            // If not, we use global filtering but check individual rules if they exist.

            List<BankMaster> bankMasters;
            List<Farmer> farmers;

            if (paymentFromId.HasValue)
            {
                bankMasters = await globalBankMastersQuery
                     .Where(bm => allowedBankMasterIds.Contains(bm.Id) || allowedSubGroupIds.Contains(bm.GroupId))
                     .OrderBy(bm => bm.AccountName)
                     .Take(50)
                     .ToListAsync();

                farmers = await globalFarmersQuery
                     .Where(f => allowedFarmerIds.Contains(f.Id) || allowedGrowerGroupIds.Contains(f.GroupId))
                     .OrderBy(f => f.FarmerName)
                     .Take(50)
                     .ToListAsync();
            }
            else
            {
                 // Global search
                 bankMasters = await globalBankMastersQuery.OrderBy(bm => bm.AccountName).Take(50).ToListAsync();
                 farmers = await globalFarmersQuery.OrderBy(f => f.FarmerName).Take(50).ToListAsync();
            }

            var allAccounts = new List<LookupItem>();
            foreach(var bm in bankMasters)
            {
                // Check rule
                if (paymentFromId.HasValue || IsAllowed("BankMaster", bm.Id, "SubGroupLedger", bm.GroupId))
                {
                    allAccounts.Add(new LookupItem { Id = bm.Id, Name = bm.AccountName, Type = "BankMaster" });
                }
            }
            foreach(var f in farmers)
            {
                 if (paymentFromId.HasValue || IsAllowed("Farmer", f.Id, "GrowerGroup", f.GroupId))
                 {
                    allAccounts.Add(new LookupItem { Id = f.Id, Name = f.FarmerName, Type = "Farmer" });
                 }
            }
            
            // Helper inside method for IsAllowed
            bool IsAllowed(string accountType, int accountId, string fallbackType, int fallbackId)
            {
                 string? ruleValue = GetRuleValue(accountType, accountId, paymentFromId);
                 if (ruleValue != null) return CheckRule(ruleValue, type);

                 string? fallbackRuleValue = GetRuleValue(fallbackType, fallbackId, paymentFromId);
                 if (fallbackRuleValue != null) return CheckRule(fallbackRuleValue, type);
                 
                 return true; 
            }

            return allAccounts.OrderBy(a => a.Name).Take(100).ToList();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<string>> GetUnitNamesAsync()
    {
        return await _context.UnitMasters.Select(u => u.UnitName ?? "").Distinct().ToListAsync();
    }

    public async Task<IEnumerable<LookupItem>> GetEntryProfilesAsync()
    {
        try
        {
            return await _context.EntryForAccounts
                .Where(e => e.TransactionType == "Global" || e.TransactionType == "ReceiptEntry")
                .OrderBy(e => e.AccountName)
                .Select(e => new LookupItem { Id = e.Id, Name = e.AccountName })
                .ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion
}

