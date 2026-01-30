namespace AbraqAccount.Models;

public class PackingSpecialRate
{
    public int Id { get; set; }
    public DateTime EffectiveDate { get; set; }
    public long? GrowerGroupId { get; set; }
    public long? FarmerId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public BankMaster? GrowerGroup { get; set; } // Renamed in mapping if needed, but keeping property name for now
    public PartySub? Farmer { get; set; }
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


