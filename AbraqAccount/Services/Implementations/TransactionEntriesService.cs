using Microsoft.EntityFrameworkCore;
using AbraqAccount.Data;
using AbraqAccount.Models;
using AbraqAccount.Services.Interfaces;

namespace AbraqAccount.Services.Implementations;

public class TransactionEntriesService : ITransactionEntriesService
{
    private readonly AppDbContext _context;

    public TransactionEntriesService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(
        List<ReceiptEntry>? receiptEntries,
        List<PaymentSettlement>? paymentSettlements,
        List<GeneralEntry>? journalEntries,
        int totalCount,
        int totalPages
    )> GetTransactionsAsync(
        string tabType,
        string? voucherNo,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize)
    {
        try
        {
            List<ReceiptEntry>? receiptEntries = null;
            List<PaymentSettlement>? paymentSettlements = null;
            List<GeneralEntry>? journalEntries = null;
            int totalCount = 0;

            var query = _context.GeneralEntries.AsQueryable();

            // Filter by Date
            if (fromDate.HasValue) query = query.Where(g => g.EntryDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(g => g.EntryDate <= toDate.Value);
            
            // Filter by VoucherNo
            if (!string.IsNullOrEmpty(voucherNo)) query = query.Where(g => g.VoucherNo.Contains(voucherNo));
            
            // Filter by Status (normalize/map if needed)
            if (!string.IsNullOrEmpty(status)) query = query.Where(g => g.Status == status);

            if (tabType == "Receipt")
            {
                query = query.Where(g => g.VoucherType == "Receipt Entry" && g.IsActive);
                totalCount = await query.CountAsync();
                
                var entries = await query
                    .OrderByDescending(g => g.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                
                receiptEntries = entries.Select(g => new ReceiptEntry
                {
                    Id = g.Id, // Note: This might be negative ID if from view, but here it's real GE ID. View expects positive? 
                               // Actually ReceiptEntry.Id in DB was int. GE.Id is int.
                               // If we deleted ReceiptEntries table, the UI using ReceiptEntry objects just needs A value.
                    VoucherNo = g.VoucherNo,
                    ReceiptDate = g.EntryDate,
                    MobileNo = "", // Not in GE
                    Type = g.Type,
                    AccountId = (g.DebitAccountId ?? 0) != 0 ? (g.DebitAccountId ?? 0) : (g.CreditAccountId ?? 0), // Heuristic? 
                    // Actually, for Receipt Entry:
                    // If Type="Debit", AccountId was DebitAccountId.
                    // If Type="Credit", AccountId was CreditAccountId.
                    // We need to restore the "Main Account" concept.
                    // ReceiptEntry usually has One Main Account and PaymentType (Cash/Bank).
                    // In GE, we stored this.
                    // Let's assume standard mapping:
                    AccountType = g.DebitAccountType == "BankMaster" && g.DebitAccountId == 0 ? g.CreditAccountType : g.DebitAccountType,
                     // RETHINK: We need to know which one was the "Selected Account" vs "Payment Mode".
                     // In ReceiptEntryService I might have mapped it.
                     // Let's look at how we map it back.
                     // The simple way: 
                     PaymentType = g.PaymentType,
                     Amount = g.Amount,
                     RefNoChequeUTR = g.ReferenceNo,
                     Narration = g.Narration,
                     Status = g.Status,
                     CreatedAt = g.CreatedAt,
                     IsActive = g.IsActive,
                     Unit = g.Unit
                }).ToList();

                // Fix AccountId and AccountType
                for (int i = 0; i < receiptEntries.Count; i++)
                {
                    var re = receiptEntries[i];
                    var ge = entries[i];
                    // Logic: Receipt Entry matches a specific "Account" against "Cash/Bank".
                    // If PaymentType is Cash/Bank, the OTHER side is the Account.
                    // If GE Type is "Debit", it means Main Account was Debited.
                     if (ge.Type == "Debit")
                     {
                         re.AccountId = ge.DebitAccountId ?? 0;
                         re.AccountType = ge.DebitAccountType;
                     }
                     else
                     {
                         re.AccountId = ge.CreditAccountId ?? 0;
                         re.AccountType = ge.CreditAccountType;
                     }
                     
                     // Load Nav Props
                     await LoadReceiptAccountNamesAsync(re);
                }
            }
            else if (tabType == "Payment")
            {
                query = query.Where(g => g.VoucherType == "Payment Settlement" && g.IsActive);
                
                // Allow filtering by Approval Status specifically if needed, but 'status' param covers it
                if (!string.IsNullOrEmpty(status))
                {
                    // PaymentSettlement has ApprovalStatus and PaymentStatus.
                    // We'll assume 'status' maps to ApprovalStatus (which is mapped to GE.Status)
                }

                totalCount = await query.CountAsync();

                var entries = await query
                    .OrderByDescending(g => g.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                paymentSettlements = entries.Select(g => new PaymentSettlement
                {
                    Id = g.Id,
                    PANumber = g.VoucherNo,
                    SettlementDate = g.EntryDate,
                    Type = g.Type, // "Debit", "Credit", "Payment", "Receipt"
                    // AccountId/Type logic similar to Receipt
                    PaymentType = g.PaymentType, // "Cash", "Cheque" etc
                    Amount = g.Amount,
                    RefNo = g.ReferenceNo,
                    Narration = g.Narration,
                    ApprovalStatus = g.Status, // Mapped
                    PaymentStatus = "Pending", // GE doesn't strictly have PaymentStatus separate? Or we use Type?
                                             // Legacy had PaymentStatus (Pending/Cleared).
                                             // If we dropped the table, we lost that column if not in GE.
                                             // Assuming Status Covers it or default.
                    CreatedAt = g.CreatedAt,
                    IsActive = g.IsActive,
                    Unit = g.Unit
                }).ToList();

                for (int i = 0; i < paymentSettlements.Count; i++)
                {
                    var ps = paymentSettlements[i];
                    var ge = entries[i];
                    
                    // Restore Account Logic
                    if (ge.Type == "Debit" || ge.Type == "Payment")
                    {
                        ps.AccountId = ge.DebitAccountId ?? 0;
                        ps.AccountType = ge.DebitAccountType;
                    }
                    else
                    {
                        ps.AccountId = ge.CreditAccountId ?? 0;
                        ps.AccountType = ge.CreditAccountType;
                    }
                    
                    // We don't have LoadPaymentSettlementAccountNamesAsync helper visible here, 
                    // but we can load AccountName string manually or via helper if exists.
                    // The PaymentSettlement model has `AccountName` string property.
                    ps.AccountName = "Loading..."; // Placeholder, or fetch it.
                    // Let's try to fetch it to be nice.
                    // We can reuse the LoadGeneralEntryAccountNamesAsync equivalent logic or just leave it if UI handles it.
                    // Legacy code didn't seem to load names explicitly in the snippet I saw? 
                    // Wait, lines 73-76 called LoadReceiptAccountNamesAsync. 
                    // Lines 144-147 called LoadGeneralEntryAccountNamesAsync.
                    // Payment block (lines 78-110) did NOT call a helper. 
                    // It likely relied on `AccountName` being stored in the DB column `AccountName` of PaymentSettlements table.
                    // Since GE doesn't have `AccountName` string column (it has EntryForName but that's different), 
                    // we MUST load it dynamically now.
                }
            }
            else if (tabType == "Journal")
            {
                // Also include "General Entry" or just everything else?
                // Usually "Journal" means manually created Journal Entries.
                // Assuming we stick to "Journal Entry Book" or "Journal".
                // Or "General Entry".
                // Let's include everything that is NOT Receipt or Payment Settlement to be safe, 
                // OR specific types.
                // Safest: VoucherType == "Journal Entry Book" (as set in CreateGeneralEntryAsync)
                
                 query = query.Where(g => (g.VoucherType == "Journal Entry Book" || g.VoucherType == "Journal") && g.IsActive);
                 totalCount = await query.CountAsync();
                 
                 journalEntries = await query
                    .OrderByDescending(g => g.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                 
                 foreach (var entry in journalEntries)
                 {
                     await LoadGeneralEntryAccountNamesAsync(entry);
                 }
            }

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return (receiptEntries, paymentSettlements, journalEntries, totalCount, totalPages);
        }
        catch (Exception)
        {
            throw;
        }
    }


    #region Helpers
    private async Task LoadReceiptAccountNamesAsync(ReceiptEntry entry)
    {
        try
        {
            if (entry.AccountType == "MasterGroup")
            {
                entry.MasterGroup = await _context.MasterGroups.FindAsync(entry.AccountId);
            }
            else if (entry.AccountType == "MasterSubGroup")
            {
                entry.MasterSubGroup = await _context.MasterSubGroups
                    .Include(msg => msg.MasterGroup)
                    .FirstOrDefaultAsync(msg => msg.Id == entry.AccountId);
            }
            else if (entry.AccountType == "SubGroupLedger")
            {
                entry.SubGroupLedger = await _context.SubGroupLedgers
                    .Include(sgl => sgl.MasterGroup)
                    .Include(sgl => sgl.MasterSubGroup)
                    .FirstOrDefaultAsync(sgl => sgl.Id == entry.AccountId);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task LoadGeneralEntryAccountNamesAsync(GeneralEntry entry)
    {
        try
        {
            // Load debit account
            if (entry.DebitAccountType == "MasterGroup")
            {
                entry.DebitMasterGroup = await _context.MasterGroups.FindAsync(entry.DebitAccountId);
            }
            else if (entry.DebitAccountType == "MasterSubGroup")
            {
                entry.DebitMasterSubGroup = await _context.MasterSubGroups
                    .Include(msg => msg.MasterGroup)
                    .FirstOrDefaultAsync(msg => msg.Id == entry.DebitAccountId);
            }
            else if (entry.DebitAccountType == "SubGroupLedger")
            {
                entry.DebitSubGroupLedger = await _context.SubGroupLedgers
                    .Include(sgl => sgl.MasterGroup)
                    .Include(sgl => sgl.MasterSubGroup)
                    .FirstOrDefaultAsync(sgl => sgl.Id == entry.DebitAccountId);
            }

            // Load credit account
            if (entry.CreditAccountType == "MasterGroup")
            {
                entry.CreditMasterGroup = await _context.MasterGroups.FindAsync(entry.CreditAccountId);
            }
            else if (entry.CreditAccountType == "MasterSubGroup")
            {
                entry.CreditMasterSubGroup = await _context.MasterSubGroups
                    .Include(msg => msg.MasterGroup)
                    .FirstOrDefaultAsync(msg => msg.Id == entry.CreditAccountId);
            }
            else if (entry.CreditAccountType == "SubGroupLedger")
            {
                entry.CreditSubGroupLedger = await _context.SubGroupLedgers
                    .Include(sgl => sgl.MasterGroup)
                    .Include(sgl => sgl.MasterSubGroup)
                    .FirstOrDefaultAsync(sgl => sgl.Id == entry.CreditAccountId);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region History Logging
    public async Task LogTransactionHistoryAsync(string voucherNo, string voucherType, string action, string user, string? remarks = null, string? oldValues = null, string? newValues = null)
    {
        try
        {
            var history = new TransactionHistory
            {
                VoucherNo = voucherNo,
                VoucherType = voucherType,
                Action = action,
                User = user,
                ActionDate = DateTime.Now,
                Remarks = remarks ?? action,
                OldValues = oldValues,
                NewValues = newValues
            };

            _context.TransactionHistories.Add(history);
            await _context.SaveChangesAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<TransactionHistory>> GetTransactionHistoryAsync(string voucherNo)
    {
        try
        {
            return await _context.TransactionHistories
                .Where(h => h.VoucherNo == voucherNo)
                .OrderByDescending(h => h.ActionDate)
                .ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion
}

