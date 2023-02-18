using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.Data.Models
{
    public class VolunteerFor
    {
        [Key]
        public Int32 VolunteerForId { get; set; }
        [Required, MaxLength(50)]
        public string Name { get; set; }
    }
}
