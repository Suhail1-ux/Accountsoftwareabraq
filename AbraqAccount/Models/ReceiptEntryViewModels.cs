using AbraqAccount.Models;

namespace AbraqAccount.Models;

public class ReceiptEntryBatchModel
{
    public DateTime ReceiptDate { get; set; }
    public string? MobileNo { get; set; }
    public List<ReceiptEntryItemModel> Entries { get; set; } = new();
}

public class ReceiptEntryItemModel
{
    public string Type { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? RefNoChequeUTR { get; set; }
    public string? Narration { get; set; }
    public int? PaymentFromSubGroupId { get; set; }
    public string? PaymentFromSubGroupName { get; set; }
    public string? Unit { get; set; }
    public int? EntryAccountId { get; set; }
    public int? EntryForId { get; set; }
    public string? EntryForName { get; set; }
}


public class ReceiptEntryGroupViewModel
{
    public ReceiptEntry? CreditEntry { get; set; }
    public ReceiptEntry? DebitEntry { get; set; }
    public string VoucherNo { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public decimal ReceiptAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CreditEntryId { get; set; }
    public int DebitEntryId { get; set; }
    public string? Unit { get; set; }
}

