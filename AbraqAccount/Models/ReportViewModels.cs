namespace AbraqAccount.Models;

public class BalanceSheetViewModel
{
    public DateTime AsOfDate { get; set; }
    
    // Assets Side
    public List<BS_GroupViewModel> Assets { get; set; } = new();
    public decimal TotalAssets { get; set; }

    // Liabilities & Equity Side
    public List<BS_GroupViewModel> Liabilities { get; set; } = new();
    public decimal TotalLiabilities { get; set; }
    
    public decimal NetProfitLoss { get; set; } // Income - Expenses
    public decimal TotalLiabilitiesAndEquity { get; set; } // Should match TotalAssets
}

public class BS_GroupViewModel
{
    public string GroupName { get; set; } = string.Empty; // e.g., "Current Assets"
    public decimal TotalAmount { get; set; }
    public List<BS_AccountViewModel> Accounts { get; set; } = new();
}

public class BS_AccountViewModel
{
    public string AccountName { get; set; } = string.Empty; // Ledger Name
    public decimal Amount { get; set; }
}

