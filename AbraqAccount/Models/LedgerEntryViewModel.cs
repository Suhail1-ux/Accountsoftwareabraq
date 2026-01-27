namespace AbraqAccount.Models;

public class LedgerEntryViewModel
{
    public DateTime Date { get; set; }
    public string VoucherType { get; set; } = string.Empty;
    public string VoucherBillNo { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public string Particulars { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal Balance { get; set; }
    public string? RefNoChequeUTR { get; set; }
}

