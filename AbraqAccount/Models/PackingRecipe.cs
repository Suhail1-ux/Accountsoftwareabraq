using System.ComponentModel.DataAnnotations;

namespace AbraqAccount.Models;

public class PackingRecipe
{
    public int Id { get; set; }
    
    public string RecipeCode { get; set; } = string.Empty; // Auto-generated
    
    [Required]
    public string RecipeName { get; set; } = string.Empty;
    
    [Required]
    public string RecipeUOMName { get; set; } = string.Empty;
    
    [Required]
    public decimal CostUnit { get; set; } = 1;
    
    [Required]
    public decimal LabourCost { get; set; }
    
    [Required]
    public decimal HighDensityRate { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public decimal Value { get; set; } // Calculated from materials
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation property
    public List<PackingRecipeMaterial> Materials { get; set; } = new List<PackingRecipeMaterial>();
}

public class PackingRecipeMaterial
{
    public int Id { get; set; }
    public int PackingRecipeId { get; set; }
    public int PurchaseItemId { get; set; }
    public decimal Qty { get; set; }
    public string UOM { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation properties
    public PackingRecipe? PackingRecipe { get; set; }
    public PurchaseItem? PurchaseItem { get; set; }
}

public class PackingRecipeSpecialRate
{
    public int Id { get; set; }
    public int PackingRecipeId { get; set; }
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


