namespace AbraqAccount.Models;

public class SaveSpecialRateRequest
{
    public int PurchaseItemId { get; set; }
    public int? GrowerGroupId { get; set; }
    public int? FarmerId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public decimal? LabourCost { get; set; }
}

public class SavePackingRateRequest
{
    public int RecipeId { get; set; }
    public int? GrowerGroupId { get; set; }
    public int? FarmerId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public decimal? HighDensityRate { get; set; }
    public List<SavePackingRateDetail> Details { get; set; } = new();
}

public class SavePackingRateDetail
{
    public int PurchaseItemId { get; set; }
    public decimal Rate { get; set; }
}

