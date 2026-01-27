namespace AbraqAccount.Models;

public class GrowerGroup
{
    public int Id { get; set; }
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string GroupType { get; set; } = string.Empty; // FPO, AGENT, FARMER_GROUP
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Village { get; set; }
    public string? Tehsil { get; set; }
    public string BillingMode { get; set; } = string.Empty; // GROUP, FARMER
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;

    // Navigation property
    public ICollection<Farmer> Farmers { get; set; } = new List<Farmer>();
}




