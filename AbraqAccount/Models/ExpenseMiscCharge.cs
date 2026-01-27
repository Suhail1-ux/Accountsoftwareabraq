using System.ComponentModel.DataAnnotations;

namespace AbraqAccount.Models;

public class ExpenseMiscCharge
{
    public int Id { get; set; }
    public int ExpensesIncurredId { get; set; }
    public string ExpenseType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Tax { get; set; } = "Select";
    public decimal GSTAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation property
    public ExpensesIncurred? ExpensesIncurred { get; set; }
}

