using System.ComponentModel.DataAnnotations;

namespace AbraqAccount.Models;

public class MaterialIssue
{
    public int Id { get; set; }
    
    public string MaterialIssueNo { get; set; } = string.Empty; // Auto-generated
    
    [Required]
    public DateTime OrderDate { get; set; } = DateTime.Now;
    
    [Required]
    public DateTime DeliveryDate { get; set; } = DateTime.Now;
    
    [Required]
    public string DeliveredTo { get; set; } = string.Empty;
    
    [Required]
    public string OrderBy { get; set; } = string.Empty;
    
    public string? VehicleInfo { get; set; }
    
    public string? Remarks { get; set; }
    
    public decimal? Qty { get; set; } // Total quantity
    
    public string Status { get; set; } = "Completed"; // Completed, Pending, etc.
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation property
    public List<MaterialIssueItem> Items { get; set; } = new List<MaterialIssueItem>();
}

public class MaterialIssueItem
{
    public int Id { get; set; }
    public int MaterialIssueId { get; set; }
    
    public int? PurchaseItemId { get; set; }
    
    [Required]
    public string ItemName { get; set; } = string.Empty;
    
    public string? UOM { get; set; }
    
    public decimal? BalanceQty { get; set; }
    
    [Required]
    public decimal IssuedQty { get; set; }
    
    public bool IsReturnable { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation property
    public MaterialIssue? MaterialIssue { get; set; }
    public PurchaseItem? PurchaseItem { get; set; }
}


