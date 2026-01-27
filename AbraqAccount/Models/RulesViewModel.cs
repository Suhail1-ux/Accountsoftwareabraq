namespace AbraqAccount.Models;

public class RulesListViewModel
{
    public List<RulesItemViewModel> Items { get; set; } = new List<RulesItemViewModel>();
}

public class RulesItemViewModel
{
    public string AccountType { get; set; } = string.Empty; // MasterGroup, BankMaster, etc.
    public int AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Hierarchy { get; set; } = string.Empty; // e.g. "Assets > Current Assets"
    public string RuleValue { get; set; } = "Both"; // Current setting
    public string GroupName { get; set; } = string.Empty; // For UI Grouping
}

