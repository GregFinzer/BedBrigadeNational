using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.Data.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }
        [Required, MaxLength(128)]
        public string? Name { get; set; }
    }
}
