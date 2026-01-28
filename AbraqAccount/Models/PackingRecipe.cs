using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbraqAccount.Models;

public class PackingRecipe
{
    [Key]
    public long Recipeid { get; set; }
    
    [MaxLength(10)]
    public string? RecipeCode { get; set; }
    
    [MaxLength(250)]
    public string? itemId { get; set; }
    
    public decimal? unitcost { get; set; }
    
    public decimal? labourcost { get; set; }
    
    public bool flagdeleted { get; set; }
    
    public DateTime? endeffdt { get; set; }
    
    public DateTime createddate { get; set; } = DateTime.Now;
    
    public int? createdby { get; set; }
    
    public int? updatedby { get; set; }
    
    public DateTime? updateddate { get; set; }
    
    [MaxLength(250)]
    public string? recipename { get; set; }
    
    public bool? status { get; set; } = true;
    
    public double ItemWeight { get; set; }
    
    public int? RecipePackageId { get; set; }
    
    public double HighDensityRate { get; set; }

    // Legacy fields used by UI (to be mapped in service/context)
    [NotMapped] public string RecipeName { get => recipename ?? ""; set => recipename = value; }
    [NotMapped] public string? ItemId { get => itemId; set => itemId = value; }
    [NotMapped] public decimal CostUnit { get => (decimal)ItemWeight; set => ItemWeight = (double)value; }
    [NotMapped] public decimal LabourCost { get => labourcost ?? 0; set => labourcost = value; }
    [NotMapped] public decimal Value { get => unitcost ?? 0; set => unitcost = value; }
    [NotMapped] public bool IsActive { get => status ?? false; set => status = value; }
    [NotMapped] public DateTime CreatedAt { get => createddate; set => createddate = value; }
    
    // Navigation property
    public List<PackingRecipeMaterial> Materials { get; set; } = new List<PackingRecipeMaterial>();
}

public class PackingRecipeMaterial
{
    [Key]
    public long RecipeItemId { get; set; }
    public long RecipeId { get; set; }
    public int packingitemid { get; set; }
    public double? qty { get; set; }
    public decimal? avgCost { get; set; }
    public bool flagdeleted { get; set; }
    public DateTime? endeffdt { get; set; }
    public DateTime createddate { get; set; } = DateTime.Now;
    public int? createdby { get; set; }
    public int? updatedby { get; set; }
    public DateTime? updateddate { get; set; }

    // Navigation properties
    public PackingRecipe? PackingRecipe { get; set; }
    [ForeignKey("packingitemid")]
    public PurchaseItem? PurchaseItem { get; set; }

    [NotMapped] public string? MaterialName { get; set; }

    // Legacy fields for UI compatibility
    [NotMapped] public int PackingRecipeId { get => (int)RecipeId; set => RecipeId = value; }
    [NotMapped] public int PurchaseItemId { get => packingitemid; set => packingitemid = value; }
    [NotMapped] public decimal Qty { get => (decimal)(qty ?? 0); set => qty = (double)value; }
    
    private string _uom = string.Empty;
    [NotMapped] public string UOM { get => !string.IsNullOrEmpty(PurchaseItem?.UOM) ? PurchaseItem.UOM : _uom; set => _uom = value; }
    
    [NotMapped] public decimal Value { get => avgCost ?? 0; set => avgCost = value; }
    [NotMapped] public DateTime CreatedAt { get => createddate; set => createddate = value; }
}

public class PackingRecipeSpecialRate
{
    public int Id { get; set; }
    public long PackingRecipeId { get; set; }
    public int? GrowerGroupId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public decimal? HighDensityRate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public PackingRecipe? PackingRecipe { get; set; }
    public GrowerGroup? GrowerGroup { get; set; }
    public List<PackingRecipeSpecialRateDetail> Details { get; set; } = new List<PackingRecipeSpecialRateDetail>();
}

public class PackingRecipeSpecialRateDetail
{
    public int Id { get; set; }
    public int PackingRecipeSpecialRateId { get; set; }
    public int PurchaseItemId { get; set; }
    public decimal Rate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation properties
    public PackingRecipeSpecialRate? PackingRecipeSpecialRate { get; set; }
    public PurchaseItem? PurchaseItem { get; set; }
}


