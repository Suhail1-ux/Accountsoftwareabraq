using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbraqAccount.Models;

[Table("partysub")]
public class PartySub
{
    [Key]
    [Column("partyid")]
    public long PartyId { get; set; }

    [Required]
    [Column("partyname")]
    [StringLength(255)]
    public string PartyName { get; set; } = string.Empty;

    [Column("MainId")]
    public int? MainId { get; set; } // Link to BankMaster.Id

    [Column("village")]
    [StringLength(255)]
    public string? Village { get; set; }

    [Column("phone")]
    [StringLength(50)]
    public string? Phone { get; set; }

    [Column("status")]
    public bool? Status { get; set; }

    [Column("flagdeleted")]
    public bool? FlagDeleted { get; set; }
}
