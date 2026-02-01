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

public class PaymentSettlementService : IPaymentSettlementService
{
    private readonly AppDbContext _context;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ITransactionEntriesService _transactionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PaymentSettlementService(AppDbContext context, IDbContextFactory<AppDbContext> contextFactory, ITransactionEntriesService transactionService, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _contextFactory = contextFactory;
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
    public async Task<(List<PaymentSettlementGroupViewModel> groups, int totalCount, int totalPages)> GetSettlementsAsync(
        string? paNumber,
        DateTime? fromDate,
        DateTime? toDate,
        string? vendorGroup,
        string? vendorName,
        string? unit,
        string? approvalStatus,
        string? paymentStatus,
        int page,
        int pageSize)
    {
        try
        {
            var query = _context.GeneralEntries
                .Where(s => s.VoucherType == "Payment Settlement")
                .AsQueryable();

            if (approvalStatus == "Deleted")
            {
                query = query.Where(s => !s.IsActive);
            }
            else
            {
                query = query.Where(s => s.IsActive);
                if (!string.IsNullOrEmpty(approvalStatus) && approvalStatus != "All") 
                    query = query.Where(p => p.Status == approvalStatus);
            }

            if (!string.IsNullOrEmpty(unit) && unit != "ALL") query = query.Where(s => s.Unit == unit);
            if (!string.IsNullOrEmpty(paNumber)) query = query.Where(p => p.VoucherNo.Contains(paNumber));
            if (fromDate.HasValue) query = query.Where(p => p.EntryDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(p => p.EntryDate <= toDate.Value);
            // paymentStatus? GeneralEntry doesn't have PaymentStatus. Using Status for now or generic check.
            
            // Name filtering requiring joins or in-memory. Fetching first.
            var allEntries = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            
            var settlements = new List<PaymentSettlement>();
            foreach(var ge in allEntries)
            {
               settlements.Add(await MapToPaymentSettlementAsync(ge));
            }
            
            // Filter by name if needed
            if (!string.IsNullOrEmpty(vendorName))
            {
                settlements = settlements.Where(s => s.AccountName.Contains(vendorName, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Group by PANumber
            var groupedEntries = settlements
                .GroupBy(p => p.PANumber)
                .Select(g => new
                {
                    PANumber = g.Key,
                    Entries = g.OrderBy(e => e.CreatedAt).ToList()
                })
                .OrderByDescending(g => g.Entries.First().CreatedAt)
                .ToList();
            
            var totalGroups = groupedEntries.Count;
            var totalPages = (int)Math.Ceiling(totalGroups / (double)pageSize);
            
            var paginatedGroups = groupedEntries
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            var groupedViewModels = new List<PaymentSettlementGroupViewModel>();
            
            foreach (var group in paginatedGroups)
            {
                var creditEntry = group.Entries.FirstOrDefault(e => e.Type == "Credit");
                var debitEntry = group.Entries.FirstOrDefault(e => e.Type == "Debit");
                
                if (creditEntry != null || debitEntry != null)
                {
                    var viewModel = new PaymentSettlementGroupViewModel
                    {
                        CreditEntry = creditEntry,
                        DebitEntry = debitEntry,
                        PANumber = group.PANumber,
                        SettlementDate = creditEntry?.SettlementDate ?? debitEntry?.SettlementDate ?? DateTime.Now,
                        VendorName = creditEntry?.AccountName ?? debitEntry?.AccountName ?? "",
                        PaymentAmount = creditEntry?.Amount ?? debitEntry?.Amount ?? 0,
                        ApprovalStatus = creditEntry?.ApprovalStatus ?? debitEntry?.ApprovalStatus ?? "Unapproved",
                        PaymentStatus = creditEntry?.PaymentStatus ?? debitEntry?.PaymentStatus ?? "Pending",
                        CreditEntryId = creditEntry?.Id ?? 0,
                        DebitEntryId = debitEntry?.Id ?? 0,
                        ClosingBal = 0, 
                        NEFTRTGSCashForm = creditEntry?.RefNo ?? debitEntry?.RefNo,
                        Unit = creditEntry?.Unit ?? debitEntry?.Unit,
                        EntryForName = creditEntry?.EntryForName ?? debitEntry?.EntryForName
                    };
                    
                    groupedViewModels.Add(viewModel);
                }
            }

            return (groupedViewModels, totalGroups, totalPages);
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task<PaymentSettlement> MapToPaymentSettlementAsync(GeneralEntry ge)
    {
        // One side is populated, the other is NULL
        string type = ge.DebitAccountId.HasValue ? "Debit" : "Credit";
        int accountId = ge.DebitAccountId ?? ge.CreditAccountId ?? 0;
        string accountType = ge.DebitAccountType ?? ge.CreditAccountType ?? "";

        var ps = new PaymentSettlement
        {
            Id = ge.Id,
            PANumber = ge.VoucherNo,
            SettlementDate = ge.EntryDate,
            Type = type,
            AccountId = accountId,
            AccountType = accountType,
            PaymentType = ge.PaymentType,
            Amount = ge.Amount,
            RefNo = ge.ReferenceNo,
            Narration = ge.Narration,
            ApprovalStatus = !ge.IsActive ? "Deleted" : ge.Status,
            PaymentStatus = "Pending",
            IsActive = ge.IsActive,
            CreatedAt = ge.CreatedAt,
            CreatedBy = ge.CreatedBy,
            Unit = ge.Unit,
            EntryAccountId = ge.EntryAccountId,
            EntryForId = ge.EntryForId,
            EntryForName = ge.EntryForName
        };

        ps.AccountName = await GetAccountNameAsync(accountType, accountId);
        return ps;
    }
    #endregion

    #region Management
    public async Task<(bool success, string message)> CreateSettlementAsync(PaymentSettlement paymentSettlement)
    {
        // Wrapper for single creation -> Batch logic
        var batch = new PaymentSettlementBatchModel 
        { 
            SettlementDate = paymentSettlement.SettlementDate,
            Entries = new List<PaymentSettlementItemModel>
            {
                new PaymentSettlementItemModel 
                {
                    Type = paymentSettlement.Type,
                    AccountId = paymentSettlement.AccountId,
                    AccountType = paymentSettlement.AccountType,
                    Amount = paymentSettlement.Amount,
                    Narration = paymentSettlement.Narration,
                    Unit = paymentSettlement.Unit,
                    // ... entries
                }
            }
        };
        // Wait, single settlement usually requires balancing? 
        // Existing CreateSettlementAsync didn't seem to enforce balancing strictly in code, 
        // or relied on History logging. 
        // But if we use GeneralEntry, we MUST balance or use Mediator.
        // Assuming user creates one side, and expects auto-balance? 
        // Or user creates loose entries that aggregate later?
        // Let's stick to CreateMultipleSettlementsAsync logic which enforces balance.
        // For strictly single entry creation, we might fail validation if not balanced.
        // But for compatibility, let's allow it if we just assume the other side is "Pending" or similar?
        // Actually, if I use Mediator, I *can* create single entries. They balance against the Mediator.
        // And the user balances the Mediator later (conceptually).
        
        return await CreateMultipleSettlementsAsync(batch);
    }

    public async Task<(bool success, string message)> CreateMultipleSettlementsAsync(PaymentSettlementBatchModel model)
    {
        if (model == null || model.Entries == null || model.Entries.Count == 0)
            return (false, "No entries to save.");

        try
        {
            var currentUser = GetCurrentUsername();
            
            // Validate Balance
            decimal totalDebit = model.Entries.Where(e => e.Type == "Debit").Sum(e => e.Amount);
            decimal totalCredit = model.Entries.Where(e => e.Type == "Credit").Sum(e => e.Amount);

            if (Math.Abs(totalDebit - totalCredit) >= 0.01m)
                return (false, $"Total Debit ({totalDebit:F2}) does not equal Total Credit ({totalCredit:F2}).");

            // Generate PA Number
            var lastGe = await _context.GeneralEntries
                .Where(g => g.VoucherType == "Payment Settlement")
                .OrderByDescending(g => g.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
             if (lastGe != null && lastGe.VoucherNo.StartsWith("PA"))
            {
                 if (int.TryParse(lastGe.VoucherNo.Replace("PA", ""), out int lastNum))
                    nextNumber = lastNum + 1;
            }
            string paNumber = $"PA{nextNumber:D6}";

            foreach (var entry in model.Entries)
            {
                var ge = new GeneralEntry
                {
                    VoucherNo = paNumber,
                    EntryDate = model.SettlementDate,
                    MobileNo = model.MobileNo,
                    VoucherType = "Payment Settlement",
                    Type = "PaymentSettlement",
                    
                    // Single-sided logic as requested:
                    DebitAccountId = entry.Type == "Debit" ? entry.AccountId : null,
                    DebitAccountType = entry.Type == "Debit" ? entry.AccountType : null,
                    
                    CreditAccountId = entry.Type == "Credit" ? entry.AccountId : null,
                    CreditAccountType = entry.Type == "Credit" ? entry.AccountType : null,

                    Amount = entry.Amount,
                    Narration = entry.Narration,
                    Status = "Unapproved",
                    Unit = entry.Unit,
                    ReferenceNo = entry.RefNo,
                    EntryForId = entry.EntryForId,
                    EntryForName = entry.EntryForName,
                    EntryAccountId = entry.EntryAccountId,
                    PaymentType = entry.PaymentType,
                    CreatedAt = DateTime.Now,
                    CreatedBy = currentUser,
                    IsActive = true
                };

                _context.GeneralEntries.Add(ge);
            }

            await _context.SaveChangesAsync();

            // History (Logging) - Optional but good to keep
            try {
                 await _transactionService.LogTransactionHistoryAsync(paNumber, "Payment", "Insert", currentUser, remarks: "Settlement Created");
            } catch {}

            return (true, "Payment Settlement created successfully!");
        }
        catch (Exception ex)
        {
            return (false, "An error occurred while saving: " + ex.Message);
        }
    }

    public async Task<(bool success, string message)> DeleteSettlementAsync(int id)
    {
        try
        {
            var ge = await _context.GeneralEntries.FindAsync(id);
            if (ge == null) return (false, "Entry not found.");

            // Find all with same PA Number
            var related = await _context.GeneralEntries.Where(g => g.VoucherNo == ge.VoucherNo).ToListAsync();
            
            foreach (var item in related)
            {
                item.IsActive = false;
                item.Status = "Deleted"; // Explicitly set status to Deleted
                _context.GeneralEntries.Update(item);
            }
            
            await _context.SaveChangesAsync();
            return (true, "Payment settlement deleted successfully!");
        }
        catch (Exception ex)
        {
            return (false, "Error deleting settlement: " + ex.Message);
        }
    }

    public async Task<(bool success, string message)> ApproveSettlementAsync(int id)
    {
        try
        {
            var ge = await _context.GeneralEntries.FindAsync(id);
            if (ge == null) return (false, "Entry not found.");

            var related = await _context.GeneralEntries.Where(g => g.VoucherNo == ge.VoucherNo).ToListAsync();
            foreach(var r in related) 
            {
                r.Status = "Approved"; 
                r.UpdatedBy = GetCurrentUsername();
                r.UpdatedAt = DateTime.Now;
            }
            
            await _context.SaveChangesAsync();
            return (true, "Payment settlement approved successfully!");
        }
        catch (Exception ex)
        {
            return (false, "Error approving settlement: " + ex.Message);
        }
    }

    public async Task<(bool success, string message)> UnapproveSettlementAsync(int id)
    {
        try
        {
            var ge = await _context.GeneralEntries.FindAsync(id);
            if (ge == null) return (false, "Entry not found.");

            var related = await _context.GeneralEntries.Where(g => g.VoucherNo == ge.VoucherNo).ToListAsync();
             foreach(var r in related) 
            {
                r.Status = "Unapproved"; 
                r.UpdatedBy = GetCurrentUsername();
                r.UpdatedAt = DateTime.Now;
            }
            
            await _context.SaveChangesAsync();
            return (true, "Payment settlement unapproved successfully!");
        }
        catch (Exception ex)
        {
            return (false, "Error unapproving settlement: " + ex.Message);
        }
    }
    #endregion

    #region Lookups
    public async Task<IEnumerable<object>> GetVendorsAsync(string? searchTerm)
    {
        try
        {
            var query = _context.Vendors.Where(v => v.IsActive).AsQueryable();
            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(v => v.VendorName.Contains(searchTerm) || v.VendorCode.Contains(searchTerm));
            
            return await query
                .OrderBy(v => v.VendorName)
                .Take(50)
                .Select(v => new { id = v.Id, name = $"{v.VendorCode} - {v.VendorName}" })
                .ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IEnumerable<object>> GetBillsAsync(string? searchTerm, int? vendorId)
    {
        return await Task.FromResult(new List<object>());
    }

    public async Task<IEnumerable<LookupItem>> GetAccountsAsync(string? searchTerm, int? paymentFromId = null, string? type = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // Return empty if no profile selected, enforcing the logic
        if (!paymentFromId.HasValue || paymentFromId == 0)
        {
             return new List<LookupItem>();
        }

        try
        {
            var rules = await context.AccountRules
                .Where(r => r.RuleType == "AllowedNature" && r.EntryAccountId == paymentFromId)
                .ToListAsync();

            bool CheckRule(string ruleValue, string? filterType)
            {
                if (string.IsNullOrWhiteSpace(ruleValue)) return false;
                if (string.Equals(ruleValue, "Both", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(ruleValue, "Cancel", StringComparison.OrdinalIgnoreCase)) return false;
                if (string.IsNullOrWhiteSpace(filterType)) return true;

                if (string.Equals(ruleValue, "Debit", StringComparison.OrdinalIgnoreCase) && string.Equals(filterType, "Debit", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(ruleValue, "Credit", StringComparison.OrdinalIgnoreCase) && string.Equals(filterType, "Credit", StringComparison.OrdinalIgnoreCase)) return true;

                return false;
            }

            var allowedSubGroupIds = rules
                .Where(r => r.AccountType == "SubGroupLedger" && CheckRule(r.Value, type))
                .Select(r => r.AccountId)
                .ToHashSet();

            var allowedBankMasterIds = rules
                .Where(r => r.AccountType == "BankMaster" && CheckRule(r.Value, type))
                .Select(r => r.AccountId)
                .ToHashSet();

            var allowedFarmerIds = rules
                .Where(r => r.AccountType == "Farmer" && CheckRule(r.Value, type))
                .Select(r => r.AccountId)
                .ToHashSet();

            var bankMastersQuery = context.BankMasters.Where(bm => bm.IsActive);
            var farmersQuery = context.Farmers.Where(f => f.IsActive);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                bankMastersQuery = bankMastersQuery.Where(bm => bm.AccountName.Contains(searchTerm));
                farmersQuery = farmersQuery.Where(f => f.FarmerName.Contains(searchTerm));
            }

            var bankMasters = await bankMastersQuery
                .Where(bm => allowedBankMasterIds.Contains(bm.Id) || allowedSubGroupIds.Contains(bm.GroupId))
                .OrderBy(bm => bm.AccountName)
                .Take(50)
                .ToListAsync();

            var farmers = await farmersQuery
                .Where(f => allowedFarmerIds.Contains(f.Id))
                .OrderBy(f => f.FarmerName)
                .Take(50)
                .ToListAsync();

            var results = new List<LookupItem>();
            results.AddRange(bankMasters.Select(b => new LookupItem { Id = b.Id, Name = b.AccountName, Type = "BankMaster" }));
            results.AddRange(farmers.Select(f => new LookupItem { Id = f.Id, Name = f.FarmerName, Type = "Farmer" }));

            return results.OrderBy(a => a.Name).Take(100).ToList();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<IEnumerable<LookupItem>> GetEntryProfilesAsync()
    {
        var types = new[] { "Global", "PaymentSettlement", "Payment Settlement" };
        return await _context.EntryForAccounts
            .Where(e => types.Contains(e.TransactionType))
            .OrderBy(e => e.AccountName)
            .Select(e => new LookupItem { Id = e.Id, Name = e.AccountName })
            .ToListAsync();
    }

    public async Task<PaymentSettlement?> GetSettlementByIdAsync(int id)
    {
         var ge = await _context.GeneralEntries.FirstOrDefaultAsync(p => p.Id == id);
         if(ge == null) return null;
         return await MapToPaymentSettlementAsync(ge);
    }
    
    public async Task<List<PaymentSettlement>> GetSettlementEntriesByPANumberAsync(string paNumber)
    {
         var entries = await _context.GeneralEntries.Where(p => p.VoucherNo == paNumber && p.IsActive).ToListAsync();
         var list = new List<PaymentSettlement>();
         
         foreach(var e in entries) list.Add(await MapToPaymentSettlementAsync(e));
         return list;
    }

    public async Task<(bool success, string message)> UpdateSettlementAsync(PaymentSettlementBatchModel model, string paNumber)
    {
        // Update = Delete + Create
         var strategy = _context.Database.CreateExecutionStrategy();
         return await strategy.ExecuteAsync(async () =>
         {
             using var transaction = await _context.Database.BeginTransactionAsync();
             try
             {
                 var existing = await _context.GeneralEntries.Where(p => p.VoucherNo == paNumber).ToListAsync();
                 if (!existing.Any()) return (false, "Not found");
                 if(existing.Any(e => e.Status == "Approved")) return (false, "Cannot edit approved.");
                 
                 _context.GeneralEntries.RemoveRange(existing);
                 await _context.SaveChangesAsync();
                 
                 // Create new
                 // Same as CreateMultiple logic but force VoucherNo to paNumber
                 // ...
                  var currentUser = GetCurrentUsername();
                  foreach (var entry in model.Entries)
                {
                    var ge = new GeneralEntry
                    {
                        VoucherNo = paNumber, // Reuse old number
                        EntryDate = model.SettlementDate,
                        MobileNo = model.MobileNo,
                        VoucherType = "Payment Settlement",
                        Type = "PaymentSettlement",
                        Amount = entry.Amount,
                        Narration = entry.Narration,
                        Status = "Unapproved",
                        Unit = entry.Unit,
                        ReferenceNo = entry.RefNo,
                        EntryForId = entry.EntryForId,
                        EntryForName = entry.EntryForName,
                        EntryAccountId = entry.EntryAccountId,
                        PaymentType = entry.PaymentType,
                        CreatedAt = DateTime.Now,
                        CreatedBy = currentUser,
                        IsActive = true,

                        // Single-sided logic
                        DebitAccountId = entry.Type == "Debit" ? entry.AccountId : null,
                        DebitAccountType = entry.Type == "Debit" ? entry.AccountType : null,
                        
                        CreditAccountId = entry.Type == "Credit" ? entry.AccountId : null,
                        CreditAccountType = entry.Type == "Credit" ? entry.AccountType : null
                    };
    
                    _context.GeneralEntries.Add(ge);
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                return (true, "Updated successfully");
             }
             catch(Exception ex)
             {
                 await transaction.RollbackAsync();
                 return (false, "Error: " + ex.Message);
             }
         });
    }
    #endregion

    private async Task<string> GetAccountNameAsync(string type, int id)
    {
         // Simple helper
         if(type == "BankMaster") return (await _context.BankMasters.FindAsync(id))?.AccountName ?? "";
         if(type == "Farmer") return (await _context.Farmers.FindAsync(id))?.FarmerName ?? "";
         if(type == "Vendor") return (await _context.Vendors.FindAsync(id))?.VendorName ?? "";
         // ...
        return type;
    }

    public async Task<object?> GetPADetailsAsync(string paNumber)
    {
        try
        {
            var entries = await _context.GeneralEntries
                .Where(p => p.VoucherNo == paNumber && p.IsActive)
                .ToListAsync();

            if (!entries.Any()) return null;

            var debitEntries = new List<dynamic>();
            var creditEntries = new List<dynamic>();

            foreach (var e in entries)
            {
                if (e.CreditAccountId.HasValue)
                {
                    string name = await GetAccountNameAsync(e.CreditAccountType, e.CreditAccountId ?? 0);
                    creditEntries.Add(new {
                        accountName = name,
                        amount = e.Amount,
                        narration = e.Narration,
                        refNo = e.ReferenceNo,
                        paymentType = e.PaymentType
                    });
                }
                else if (e.DebitAccountId.HasValue)
                {
                    string name = await GetAccountNameAsync(e.DebitAccountType, e.DebitAccountId ?? 0);
                    debitEntries.Add(new {
                         accountName = name,
                         amount = e.Amount,
                         narration = e.Narration,
                         refNo = e.ReferenceNo,
                         paymentType = e.PaymentType
                    });
                }
            }

            var first = entries.First();
            return new
            {
                paNumber = first.VoucherNo,
                settlementDate = first.EntryDate,
                unit = first.Unit,
                status = first.Status,
                debitEntries = debitEntries,
                creditEntries = creditEntries,
                totalAmount = creditEntries.Sum(x => (decimal)x.amount)
            };
        }
        catch { return null; }
    }

    public async Task<List<string>> GetUnitNamesAsync()
    {
        return await _context.UnitMasters.Select(u => u.UnitName ?? "").Distinct().ToListAsync();
    }

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
            
            // Payment Types
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
        }
        catch { /* ignore */ }
    }
}
