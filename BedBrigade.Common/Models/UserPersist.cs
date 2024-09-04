using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BedBrigade.Common.Enums;

namespace BedBrigade.Common.Models
{
    [Table("UserPersist")]
    public class UserPersist : BaseEntity
    {
        [Key, Column(Order = 0), MaxLength(50)]
        public String UserName { get; set; }

        [Key, Column(Order = 1)]
        public PersistGrid Grid { get; set; }

        [MaxLength(-1)] // This sets the maximum length for the database to be the maximum value of the data type
        public string? Data { get; set; }
    }
}
