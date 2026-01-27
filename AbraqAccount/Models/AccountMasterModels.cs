namespace AbraqAccount.Models;

public class MasterGroupImportModel
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class MasterSubGroupImportModel
{
    public string MasterGroupName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class SubGroupLedgerImportModel
{
    public string MasterGroupName { get; set; } = string.Empty;
    public string MasterSubGroupName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

