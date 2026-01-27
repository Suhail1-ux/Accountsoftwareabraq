using System.ComponentModel.DataAnnotations;

namespace AbraqAccount.Models;

public class UOMHistory
{
    public int Id { get; set; }
    public int UOMId { get; set; }
    public string Action { get; set; } = string.Empty; // Create, Update, Delete
    public string User { get; set; } = string.Empty;
    public DateTime ActionDate { get; set; } = DateTime.Now;
    public string? Remarks { get; set; }
    
    public UOM? UOM { get; set; }
}

