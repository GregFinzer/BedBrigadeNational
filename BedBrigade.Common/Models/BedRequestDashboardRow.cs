using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.Common.Models
{
    public class BedRequestDashboardRow
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public int Requests { get; set; }
        public int Beds { get; set; }
    }
}
