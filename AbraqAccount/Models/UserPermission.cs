using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbraqAccount.Models;

public class UserPermission
{
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }
    
    [Required]
    public int MenuId { get; set; }
    public Menu? Menu { get; set; }
    
    public bool CanView { get; set; } = false;
    public bool CanCreate { get; set; } = false;
    public bool CanEdit { get; set; } = false;
    public bool CanDelete { get; set; } = false;
    public bool CanPrint { get; set; } = false;
    
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}

