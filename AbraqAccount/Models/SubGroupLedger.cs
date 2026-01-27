namespace AbraqAccount.Models;

public class SubGroupLedger
{
    public int Id { get; set; }
    public int MasterGroupId { get; set; }
    public MasterGroup? MasterGroup { get; set; }
    public int MasterSubGroupId { get; set; }
    public MasterSubGroup? MasterSubGroup { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
}





