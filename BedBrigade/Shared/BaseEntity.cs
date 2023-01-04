using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.Shared
{
    public abstract class BaseEntity
    {
        public DateTime? CreateDate { get; set; }

        [MaxLength(100)]
        public String CreateUser { get; set; }

        public DateTime? UpdateDate { get; set; }

        [MaxLength(100)]
        public String UpdateUser { get; set; }

        [MaxLength(100)]
        public String MachineName { get; set; }

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
