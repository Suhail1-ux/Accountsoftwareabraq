namespace AbraqAccount.Models;

public class CreditNoteDetail
{
    public int Id { get; set; }
    public int CreditNoteId { get; set; }
    public string AccountType { get; set; } = string.Empty; // Discount, etc.
    public string RefNoBillNo { get; set; } = string.Empty;
    public string? HsnSacCode { get; set; }
    public decimal? Qty { get; set; }
    public decimal? Rate { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;

    // Navigation property
    public CreditNote? CreditNote { get; set; }
}




