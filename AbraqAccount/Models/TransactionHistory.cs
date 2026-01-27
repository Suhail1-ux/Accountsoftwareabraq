using System.ComponentModel.DataAnnotations;

namespace AbraqAccount.Models;

public class TransactionHistory
{
    public int Id { get; set; }
    
    [Required]
    public string VoucherNo { get; set; } = string.Empty;
    
    [Required]
    public string VoucherType { get; set; } = string.Empty; // Receipt, Payment, Journal
    
    [Required]
    public string Action { get; set; } = string.Empty; // Insert, Edit, Delete, Approve
    
    [Required]
    public string User { get; set; } = string.Empty;
    
    [Required]
    public DateTime ActionDate { get; set; } = DateTime.Now;
    
    public string? Remarks { get; set; }
    
    public string? OldValues { get; set; } // JSON serialized old state
    
    public string? NewValues { get; set; } // JSON serialized new state
}

