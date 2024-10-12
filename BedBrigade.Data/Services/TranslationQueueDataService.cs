using BedBrigade.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services
{
    public class TranslationQueueDataService : Repository<TranslationQueue>, ITranslationQueueDataService
    {
        public TranslationQueueDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService, IAuthService authService) : base(contextFactory, cachingService, authService)
        {
        }
    }
}
