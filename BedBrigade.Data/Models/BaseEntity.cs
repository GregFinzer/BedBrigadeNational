using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.Data.Models
{
    public abstract class BaseEntity
    {
        public DateTime? CreateDate { get; set; }

        [MaxLength(100)]
        public String? CreateUser { get; set; } = string.Empty;

        public DateTime? UpdateDate { get; set; }

        [MaxLength(100)]
        public String? UpdateUser { get; set; } = string.Empty;

        [MaxLength(100)]
        public String? MachineName { get; set; } = string.Empty;

        public void SetCreateUser(String userName)
        {
            CreateUser = userName;
            CreateDate = DateTime.Now;
            MachineName = Environment.MachineName;
        }

        public void SetUpdateUser(String userName)
        {
            UpdateUser = userName;
            UpdateDate = DateTime.Now;
            MachineName = Environment.MachineName;
        }
    }
}
