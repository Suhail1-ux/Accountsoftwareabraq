using Microsoft.EntityFrameworkCore;
using AbraqAccount.Data;
using AbraqAccount.Models;
using AbraqAccount.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AbraqAccount.Services.Implementations;

public class PackingService : IPackingService
{
    private readonly AppDbContext _context;

    public PackingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<PackingRecipe>> GetPackingRecipesAsync(string? searchTerm)
    {
        var query = _context.PackingRecipes
            .Include(p => p.Materials)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(p => 
                p.RecipeCode.Contains(searchTerm) ||
                p.RecipeName.Contains(searchTerm));
        }

        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task<(bool success, string message)> CreatePackingRecipeAsync(PackingRecipe model, IFormCollection form)
    {
        try
        {
             var materials = GetMaterialsFromForm(form);

            // Generate Recipe Code
            var lastRecipe = await _context.PackingRecipes.OrderByDescending(r => r.Id).FirstOrDefaultAsync();
            int nextCode = 1;
            if (lastRecipe != null)
            {
                if (int.TryParse(lastRecipe.RecipeCode, out int lastCode)) nextCode = lastCode + 1;
            }
            model.RecipeCode = nextCode.ToString("D4");
            model.CreatedAt = DateTime.Now;

            if (materials.Any()) model.Value = materials.Sum(m => m.Value);
            else model.Value = 0;

            _context.PackingRecipes.Add(model);
            await _context.SaveChangesAsync();

            foreach (var material in materials)
            {
                material.PackingRecipeId = model.Id;
                material.CreatedAt = DateTime.Now;
                _context.PackingRecipeMaterials.Add(material);
            }
            await _context.SaveChangesAsync();

            return (true, "Packing Recipe created successfully!");
        }
        catch (Exception ex)
        {
            return (false, "Error: " + ex.Message);
        }
    }

    public async Task<PackingRecipe?> GetPackingRecipeByIdAsync(int id)
    {
        return await _context.PackingRecipes
            .Include(p => p.Materials)
                .ThenInclude(m => m.PurchaseItem)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<(bool success, string message)> UpdatePackingRecipeAsync(int id, PackingRecipe model, List<PackingRecipeMaterial> materials)
    {
        try
        {
            var existing = await _context.PackingRecipes
                .Include(p => p.Materials)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (existing == null) return (false, "Not found");

            existing.RecipeName = model.RecipeName;
            existing.RecipeUOMName = model.RecipeUOMName;
            existing.CostUnit = model.CostUnit;
            existing.LabourCost = model.LabourCost;
            existing.HighDensityRate = model.HighDensityRate;
            existing.IsActive = model.IsActive;

            _context.PackingRecipeMaterials.RemoveRange(existing.Materials);

            if (materials != null && materials.Any())
            {
                existing.Value = materials.Sum(m => m.Value);
                foreach (var material in materials)
                {
                    if (material.PurchaseItemId > 0)
                    {
                        material.PackingRecipeId = id;
                        material.CreatedAt = DateTime.Now;
                        _context.PackingRecipeMaterials.Add(material);
                    }
                }
            }
            await _context.SaveChangesAsync();
            return (true, "Updated successfully");
        }
        catch (Exception ex)
        {
            return (false, "Error: " + ex.Message);
        }
    }

    public async Task<IEnumerable<LookupItem>> GetPackingMaterialsAsync(string? searchTerm)
    {
        var query = _context.PurchaseItems
            .Where(p => p.InventoryType == "Packing Inventory" && p.IsActive)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(p => p.ItemName.Contains(searchTerm) || p.Code.Contains(searchTerm));
        }

        return await query
            .OrderBy(p => p.ItemName)
            .Select(p => new LookupItem { 
                Id = p.Id, 
                Name = p.ItemName,
                UOM = p.UOM
            })
            .ToListAsync();
    }

    public async Task<string> GetMaterialUOMAsync(int id)
    {
        var material = await _context.PurchaseItems.FindAsync(id);
        return material?.UOM ?? "";
    }

    public async Task<object?> GetSpecialRateFormDataAsync(int id)
    {
        var recipe = await _context.PackingRecipes
            .Include(p => p.Materials)
                .ThenInclude(m => m.PurchaseItem)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recipe == null) return null;

        var growerGroups = await _context.GrowerGroups
            .Where(g => g.IsActive)
            .OrderBy(g => g.GroupName)
            .Select(g => new { id = g.Id, name = g.GroupName, code = g.GroupCode })
            .ToListAsync();

        return new
        {
            recipeId = recipe.Id,
            recipeName = recipe.RecipeName,
            materials = recipe.Materials.Select(m => new
            {
                id = m.PurchaseItemId,
                name = m.PurchaseItem?.ItemName ?? "",
                code = m.PurchaseItem?.Code ?? ""
            }).ToList(),
            growerGroups = growerGroups
        };
    }

    public async Task<(bool success, string message)> SaveSpecialRateAsync(SavePackingRateRequest request)
    {
        try
        {
            var specialRate = new PackingRecipeSpecialRate
            {
                PackingRecipeId = request.RecipeId,
                GrowerGroupId = request.GrowerGroupId,
                EffectiveFrom = request.EffectiveFrom,
                HighDensityRate = request.HighDensityRate,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            _context.PackingRecipeSpecialRates.Add(specialRate);
            await _context.SaveChangesAsync();

            if (request.Details != null && request.Details.Any())
            {
                foreach (var detail in request.Details)
                {
                    if (detail.PurchaseItemId > 0)
                    {
                        var rateDetail = new PackingRecipeSpecialRateDetail
                        {
                            PackingRecipeSpecialRateId = specialRate.Id,
                            PurchaseItemId = detail.PurchaseItemId,
                            Rate = detail.Rate,
                            CreatedAt = DateTime.Now
                        };
                        _context.PackingRecipeSpecialRateDetails.Add(rateDetail);
                    }
                }
                await _context.SaveChangesAsync();
            }
            return (true, "Special Rate saved successfully!");
        }
        catch (Exception ex)
        {
            return (false, "Error: " + ex.Message);
        }
    }

    public async Task LoadRecipeDropdownsAsync(dynamic viewBag)
    {
        var uomList = await _context.PurchaseItems
            .Where(p => !string.IsNullOrEmpty(p.UOM))
            .Select(p => p.UOM)
            .Distinct()
            .OrderBy(u => u)
            .ToListAsync();

        viewBag.RecipeUOMName = new SelectList(uomList);
    }

    private List<PackingRecipeMaterial> GetMaterialsFromForm(IFormCollection form)
    {
        var materials = new List<PackingRecipeMaterial>();
        var materialIndex = 0;
        
        while (form.ContainsKey($"materials[{materialIndex}].PurchaseItemId"))
        {
            var purchaseItemIdStr = form[$"materials[{materialIndex}].PurchaseItemId"].ToString();
            var qtyStr = form[$"materials[{materialIndex}].Qty"].ToString();
            var uomStr = form[$"materials[{materialIndex}].UOM"].ToString();
            var valueStr = form[$"materials[{materialIndex}].Value"].ToString();

            if (int.TryParse(purchaseItemIdStr, out int purchaseItemId) && purchaseItemId > 0)
            {
                if (decimal.TryParse(qtyStr, out decimal qty) && decimal.TryParse(valueStr, out decimal value))
                {
                    var material = new PackingRecipeMaterial
                    {
                        PurchaseItemId = purchaseItemId,
                        Qty = qty,
                        UOM = uomStr ?? "",
                        Value = value,
                        CreatedAt = DateTime.Now
                    };
                    materials.Add(material);
                }
            }
            materialIndex++;
        }
        return materials;
    }

    // --- Packing Special Rate Implementation ---

    public async Task<List<PackingSpecialRate>> GetPackingSpecialRatesAsync(string? growerGroupSearch, string? growerNameSearch, string? status)
    {
        var query = _context.PackingSpecialRates
            .Include(p => p.GrowerGroup)
            .Include(p => p.Farmer)
            .AsQueryable();

        if (!string.IsNullOrEmpty(growerGroupSearch))
        {
            query = query.Where(p => 
                (p.GrowerGroup != null && p.GrowerGroup.GroupName.Contains(growerGroupSearch)) ||
                (p.GrowerGroup != null && p.GrowerGroup.GroupCode.Contains(growerGroupSearch)));
        }

        if (!string.IsNullOrEmpty(growerNameSearch))
        {
            query = query.Where(p => 
                (p.Farmer != null && p.Farmer.FarmerName.Contains(growerNameSearch)) ||
                (p.Farmer != null && p.Farmer.FarmerCode.Contains(growerNameSearch)));
        }

        if (!string.IsNullOrEmpty(status))
        {
            bool isActive = status.ToLower() == "active";
            query = query.Where(p => p.IsActive == isActive);
        }

        return await query.OrderByDescending(p => p.EffectiveDate).ToListAsync();
    }

    public async Task<(bool success, string message)> CreatePackingSpecialRateAsync(PackingSpecialRate model, IFormCollection form)
    {
        try
        {
            model.CreatedAt = DateTime.Now;
            if (!form.ContainsKey("IsActive")) model.IsActive = false;

            _context.PackingSpecialRates.Add(model);
            await _context.SaveChangesAsync();

            var details = GetSpecialRateDetailsFromForm(form, model.Id);
            if (details.Any())
            {
                _context.PackingSpecialRateDetails.AddRange(details);
                await _context.SaveChangesAsync();
            }
            
            return (true, "Created successfully");
        }
        catch (Exception ex)
        {
            return (false, "Error: " + ex.Message);
        }
    }

    public async Task<PackingSpecialRate?> GetPackingSpecialRateByIdAsync(int id)
    {
        return await _context.PackingSpecialRates
            .Include(p => p.GrowerGroup)
            .Include(p => p.Farmer)
            .Include(p => p.Details)
                .ThenInclude(d => d.PurchaseItem)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<(bool success, string message)> UpdatePackingSpecialRateAsync(int id, PackingSpecialRate model, List<PackingSpecialRateDetail> details)
    {
        try
        {
            var existing = await _context.PackingSpecialRates
                .Include(p => p.Details)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (existing == null) return (false, "Not found");

            existing.EffectiveDate = model.EffectiveDate;
            existing.GrowerGroupId = model.GrowerGroupId;
            existing.FarmerId = model.FarmerId;
            existing.IsActive = model.IsActive;

            _context.PackingSpecialRateDetails.RemoveRange(existing.Details);

            if (details != null && details.Any())
            {
                foreach (var detail in details)
                {
                    if (detail.PurchaseItemId > 0)
                    {
                        detail.PackingSpecialRateId = id;
                        detail.CreatedAt = DateTime.Now;
                        _context.PackingSpecialRateDetails.Add(detail);
                    }
                }
            }
            await _context.SaveChangesAsync();
            return (true, "Updated successfully");
        }
        catch (Exception ex)
        {
            return (false, "Error: " + ex.Message);
        }
    }

    public async Task<IEnumerable<LookupItem>> GetPackingItemsForRateAsync()
    {
        return await _context.PurchaseItems
            .Where(p => p.InventoryType == "Packing Inventory" && p.IsActive)
            .OrderBy(p => p.ItemName)
            .Select(p => new LookupItem { 
                Id = p.Id, 
                Name = p.ItemName,
                Rate = p.PurchaseCostingPerNos // Assuming I added Rate to LookupItem or using PurchaseCostingPerNos
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<LookupItem>> GetFarmersByGroupAsync(int groupId)
    {
        return await _context.Farmers
            .Where(f => f.GroupId == groupId && f.IsActive)
            .OrderBy(f => f.FarmerName)
            .Select(f => new LookupItem { Id = f.Id, Name = f.FarmerName })
            .ToListAsync();
    }

    public async Task LoadSpecialRateDropdownsAsync(dynamic viewBag)
    {
        var growerGroups = await _context.GrowerGroups
            .Where(g => g.IsActive)
            .OrderBy(g => g.GroupName)
            .ToListAsync();

        viewBag.GrowerGroupId = new SelectList(growerGroups, "Id", "GroupName");
    }

    private List<PackingSpecialRateDetail> GetSpecialRateDetailsFromForm(IFormCollection form, int specialRateId)
    {
        var details = new List<PackingSpecialRateDetail>();
        var detailIndex = 0;
        
        while (form.ContainsKey($"Details[{detailIndex}].PurchaseItemId"))
        {
            var purchaseItemIdStr = form[$"Details[{detailIndex}].PurchaseItemId"].ToString();
            var rateStr = form[$"Details[{detailIndex}].Rate"].ToString();
            var specialRateStr = form[$"Details[{detailIndex}].SpecialRate"].ToString();

            if (int.TryParse(purchaseItemIdStr, out int purchaseItemId) && purchaseItemId > 0)
            {
                if (decimal.TryParse(rateStr, out decimal rate))
                {
                    var detail = new PackingSpecialRateDetail
                    {
                        PackingSpecialRateId = specialRateId,
                        PurchaseItemId = purchaseItemId,
                        Rate = rate,
                        SpecialRate = !string.IsNullOrEmpty(specialRateStr) && decimal.TryParse(specialRateStr, out decimal specialRate) ? specialRate : null,
                        CreatedAt = DateTime.Now
                    };
                    details.Add(detail);
                }
            }
            detailIndex++;
        }
        return details;
    }
}

