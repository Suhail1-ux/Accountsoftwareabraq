using System.ComponentModel.DataAnnotations;

namespace AbraqAccount.Models;

public class Menu
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty; // Display Name (e.g. "Account Master")
    
    public string? ControllerName { get; set; }
    public string? ActionName { get; set; }
    
    public string? IconClass { get; set; } // e.g. "bi bi-wallet2"
    
    public int? ParentId { get; set; } // For submenu items
    public Menu? Parent { get; set; }
    
    public int DisplayOrder { get; set; } = 0;
    
    public bool IsActive { get; set; } = true;
    
    public List<Menu> Children { get; set; } = new List<Menu>();
}

