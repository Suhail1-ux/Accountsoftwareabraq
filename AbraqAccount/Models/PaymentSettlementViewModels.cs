using AbraqAccount.Models;

namespace AbraqAccount.Models;

public class PaymentSettlementBatchModel
{
    public DateTime SettlementDate { get; set; }
    public List<PaymentSettlementItemModel> Entries { get; set; } = new();
}

public class PaymentSettlementItemModel
{
    public string Type { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? RefNo { get; set; }
    public string? Narration { get; set; }
    public int? PaymentFromSubGroupId { get; set; }
    public string? PaymentFromSubGroupName { get; set; }
    public string? Unit { get; set; }
    public int? EntryAccountId { get; set; }
    public int? EntryForId { get; set; }
    public string? EntryForName { get; set; }
}

public class PaymentSettlementGroupViewModel
{
    public PaymentSettlement? CreditEntry { get; set; }
    public PaymentSettlement? DebitEntry { get; set; }
    public string PANumber { get; set; } = string.Empty;
    public DateTime SettlementDate { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public decimal PaymentAmount { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public int CreditEntryId { get; set; }
    public int DebitEntryId { get; set; }
    public decimal? ClosingBal { get; set; }
    public string? NEFTRTGSCashForm { get; set; }
    public string? Unit { get; set; }
}

