using System.ComponentModel.DataAnnotations;

namespace AbraqAccount.Models;

public class PaymentType
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "";
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
