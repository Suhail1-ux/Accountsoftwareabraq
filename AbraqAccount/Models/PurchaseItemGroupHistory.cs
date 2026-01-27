namespace AbraqAccount.Models;

public class PurchaseItemGroupHistory
{
    public int Id { get; set; }
    public int PurchaseItemGroupId { get; set; }
    public string Action { get; set; } = string.Empty; // Insert, Edit, Delete
    public string User { get; set; } = string.Empty;
    public DateTime ActionDate { get; set; }
    public string? Remarks { get; set; }
    public string? OldValues { get; set; } // JSON string for old values
    public string? NewValues { get; set; } // JSON string for new values

    // Navigation property
    public PurchaseItemGroup? PurchaseItemGroup { get; set; }
}


