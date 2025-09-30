using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.Common.Enums
{
    public enum QueueStatus
    {
        Queued,
        Locked,
        Sent,
        Failed,
        Received
    }
}
