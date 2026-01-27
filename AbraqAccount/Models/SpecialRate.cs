namespace AbraqAccount.Models;

public class SpecialRate
{
    public int Id { get; set; }
    public int PurchaseItemId { get; set; }
    public int? GrowerGroupId { get; set; }
    public int? FarmerId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public decimal? LabourCost { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public PurchaseItem? PurchaseItem { get; set; }
    public GrowerGroup? GrowerGroup { get; set; }
    public Farmer? Farmer { get; set; }
}


