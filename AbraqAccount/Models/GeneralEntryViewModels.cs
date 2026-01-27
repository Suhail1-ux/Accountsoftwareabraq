using AbraqAccount.Models;

namespace AbraqAccount.Models;


public class GeneralEntryBatchModel
{
    public DateTime EntryDate { get; set; }
    public List<GeneralEntryItemModel> Entries { get; set; } = new();
}

public class GeneralEntryItemModel
{
    public string Type { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? RefNoChequeUTR { get; set; }
    public string? Narration { get; set; }
    public string? Unit { get; set; } // Added Unit property
    public int? PaymentFromSubGroupId { get; set; }
    public string? PaymentFromSubGroupName { get; set; }
    public int? EntryAccountId { get; set; }
    public int? EntryForId { get; set; }
    public string? EntryForName { get; set; }
}

public class GeneralEntryGroupViewModel
{
    public string VoucherNo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<GeneralEntry> Entries { get; set; } = new();
    public decimal TotalDebit { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int Id { get; set; }
}

