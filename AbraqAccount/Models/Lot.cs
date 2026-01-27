namespace AbraqAccount.Models;

public class Lot
{
    public int Id { get; set; }
    public string LotNo { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public int FarmerId { get; set; }
    public string? ChamberNo { get; set; }
    public int? Cartons { get; set; }
    public int? Crates { get; set; }
    public int? Bins { get; set; }
    public string? Variety { get; set; }
    public string? Grade { get; set; }
    public DateTime ArrivalDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public GrowerGroup? GrowerGroup { get; set; }
    public Farmer? Farmer { get; set; }
}




