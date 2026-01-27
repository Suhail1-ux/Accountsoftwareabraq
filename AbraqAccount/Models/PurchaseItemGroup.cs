namespace AbraqAccount.Models;

public class PurchaseItemGroup
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty; // Auto-generated 4-digit code (e.g., "0001", "0033")
    public string Name { get; set; } = string.Empty; // Inventory Item Group Name
    public bool IsActive { get; set; } = true; // Status: Active/Inactive
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}


