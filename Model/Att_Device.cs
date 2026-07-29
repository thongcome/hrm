using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

[Table("Att_Device")]
public class Att_Device
{
    [Key]
    public long Id { get; set; }

    [Required, StringLength(50)]
    public string CompanyId { get; set; } = null!;

    [Required, StringLength(50)]
    public string DeviceCode { get; set; } = null!;

    [Required, StringLength(150)]
    public string DeviceName { get; set; } = null!;

    public AttDeviceType DeviceType { get; set; } = AttDeviceType.Manual;

    [StringLength(250)]
    public string? Location { get; set; }

    public bool IsActive { get; set; } = true;
}
