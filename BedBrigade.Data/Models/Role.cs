using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Data.Models;

public class Role
{
    [Key]
    public int RoleId { get; set; }
    [Required, MaxLength(128)]
    public string? Name { get; set; }
}
