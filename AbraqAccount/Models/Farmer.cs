namespace AbraqAccount.Models;

public class Farmer
{
    public int Id { get; set; }
    public string FarmerCode { get; set; } = string.Empty;
    public string FarmerName { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public string? Village { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;

    // Navigation property
    public GrowerGroup? GrowerGroup { get; set; }
    public ICollection<Lot> Lots { get; set; } = new List<Lot>();
}




