using AbraqAccount.Models;

namespace AbraqAccount.Services.Interfaces;

public interface ITransactionEntriesService
{
    Task<(
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
        int pageSize);

    Task LogTransactionHistoryAsync(string voucherNo, string voucherType, string action, string user, string? remarks = null, string? oldValues = null, string? newValues = null);
    Task<List<TransactionHistory>> GetTransactionHistoryAsync(string voucherNo);
}

