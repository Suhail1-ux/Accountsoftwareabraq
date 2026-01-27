namespace AbraqAccount.Models;

public class PackingSpecialRate
{
    public int Id { get; set; }
    public DateTime EffectiveDate { get; set; }
    public int? GrowerGroupId { get; set; }
    public int? FarmerId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public GrowerGroup? GrowerGroup { get; set; }
    public Farmer? Farmer { get; set; }
    public List<PackingSpecialRateDetail> Details { get; set; } = new List<PackingSpecialRateDetail>();
}

public class PackingSpecialRateDetail
{
    public int Id { get; set; }
    public int PackingSpecialRateId { get; set; }
    public int PurchaseItemId { get; set; }
    public decimal Rate { get; set; } // Standard rate
    public decimal? SpecialRate { get; set; } // Special rate
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public PackingSpecialRate? PackingSpecialRate { get; set; }
    public PurchaseItem? PurchaseItem { get; set; }
}


