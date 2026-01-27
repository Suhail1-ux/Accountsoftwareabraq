using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbraqAccount.Models;

[Table("vehinfo")]
public class VehInfo
{
    [Key]
    [Column("vid")]
    public int Vid { get; set; }

    [Column("drivername")]
    public string? DriverName { get; set; }

    [Column("vehno")]
    public string? VehNo { get; set; }

    [Column("vehtype")]
    public string? VehType { get; set; }

    [Column("status")]
    public bool? Status { get; set; }

    [Column("flagdeleted")]
    public int? FlagDeleted { get; set; }

    [Column("contactno")]
    public string? ContactNo { get; set; }

    [Column("userid")]
    public int? UserId { get; set; }

    [Column("createddate")]
    public DateTime? CreatedDate { get; set; }
}
