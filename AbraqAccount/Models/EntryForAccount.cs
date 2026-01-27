using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbraqAccount.Models;

public class EntryForAccount
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string TransactionType { get; set; } = string.Empty;



    [MaxLength(255)]
    public string AccountName { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

