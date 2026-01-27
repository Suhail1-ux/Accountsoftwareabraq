using System.ComponentModel.DataAnnotations;

namespace AbraqAccount.Models;

public class PurchaseOrderTC
{
    public int Id { get; set; }
    
    [Required]
    public string TCType { get; set; } = string.Empty; // "Annexure", "Other"
    
    [Required]
    public string Caption { get; set; } = string.Empty;
    
    [Required]
    public string TermsAndConditions { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class PurchaseOrderTCHistory
{
    public int Id { get; set; }
    public int PurchaseOrderTCId { get; set; }
    public string Action { get; set; } = string.Empty; // "Create", "Edit"
    public string User { get; set; } = string.Empty;
    public DateTime ActionDate { get; set; } = DateTime.Now;
    public string Remarks { get; set; } = string.Empty;
    
    // Navigation property
    public PurchaseOrderTC? PurchaseOrderTC { get; set; }
}


