using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.Data.Services
{
    public interface IEmailBounceService
    {
        Task ProcessBounces(CancellationToken cancellationToken = default);
    }
}
