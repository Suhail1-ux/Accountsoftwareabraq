namespace AbraqAccount.Models;

public class BankBook
{
    public int Id { get; set; }
    public string BankName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty; // Payment, Receipt, Contra
    public string? TransactionType { get; set; } // Dr (for Payment)
    public string? RefBankName { get; set; }
    public string? ToBankName { get; set; } // For Contra
    public string? PaymentMode { get; set; } // Cheque, Bank Transfer, Cash, TDS, Mobile Pay
    public string? TransactionNumber { get; set; } // Cheque/UTR/NEFT/RTGS Number
    public DateTime? ChequeDate { get; set; }
    public string? Particular { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
}




