using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.Data.Models
{
    public class VolunteerFor
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 VolunteerForId { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; }
    }
}
