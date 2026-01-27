using System.ComponentModel.DataAnnotations;

namespace AbraqAccount.Models;

public class ExpenseItem
{
    public int Id { get; set; }
    public int ExpensesIncurredId { get; set; }
    public int ItemGroupId { get; set; }
    public int ItemId { get; set; }
    public string? Description { get; set; }
    public string UOM { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }
    public string GST { get; set; } = "NA";
    public decimal GSTAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public ExpensesIncurred? ExpensesIncurred { get; set; }
    public PurchaseItemGroup? ItemGroup { get; set; }
    public PurchaseItem? Item { get; set; }
}

