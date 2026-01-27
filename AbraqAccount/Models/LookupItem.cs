namespace AbraqAccount.Models;

public class LookupItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? AccountNumber { get; set; }
    public string? UOM { get; set; }
    public int? GroupId { get; set; }
    public decimal? Rate { get; set; }
}
