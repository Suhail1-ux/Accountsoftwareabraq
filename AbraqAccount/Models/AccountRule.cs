using System.ComponentModel.DataAnnotations;

namespace AbraqAccount.Models;

public class AccountRule
{
    public int Id { get; set; }
    
    [Required]
    public string AccountType { get; set; } = string.Empty; // MasterGroup, MasterSubGroup, SubGroupLedger, BankMaster, etc.
    
    public int AccountId { get; set; }
    
    [Required]
    public string RuleType { get; set; } = "AllowedNature"; // AllowedNature
    
    [Required]
    public string Value { get; set; } = "Both"; // Debit, Credit, Both
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
    
    public int? EntryAccountId { get; set; } // Optional: Link to EntryForAccount for profile-based rules
}

