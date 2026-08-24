using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IBedRequestEstimatedWaitDataService : IRepository<BedRequest>
    {
        Task FillEstimatedWait(BedRequest bedRequest);
    }
}
