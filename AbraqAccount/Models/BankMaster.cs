using System.ComponentModel.DataAnnotations;

namespace AbraqAccount.Models;

public class BankMaster
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(255)]
    public string AccountName { get; set; } = string.Empty;
    
    [Required]
    public int GroupId { get; set; } // SubGroupLedger ID
    
    [StringLength(500)]
    public string? Address { get; set; }
    
    [StringLength(20)]
    public string? Phone { get; set; }
    
    [StringLength(255)]
    [EmailAddress]
    public string? Email { get; set; }
    
    [StringLength(50)]
    public string? AccountNumber { get; set; }
    
    [StringLength(20)]
    public string? IfscCode { get; set; }
    
    [StringLength(255)]
    public string? BranchName { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Active"; // Active, Inactive
    
    [StringLength(255)]
    public string? CreatedBy { get; set; }
    
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    
    public bool IsActive { get; set; } = true;
    
    // Navigation property
    public SubGroupLedger? Group { get; set; }
}




