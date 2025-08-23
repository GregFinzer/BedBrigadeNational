using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

namespace BedBrigade.Data.Services
{
    public class CommonService : ICommonService
    {
        private readonly ICachingService _cachingService;
        private readonly IDbContextFactory<DataContext> _contextFactory;

        public CommonService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService)
        {
            _cachingService = cachingService;
            _contextFactory = contextFactory;
        }
        
        public async Task<ServiceResponse<List<TEntity>>> GetAllForLocationAsync<TEntity>(IRepository<TEntity> repository, int locationId) where TEntity : class, ILocationId
        {
            string cacheKey = _cachingService.BuildCacheKey(repository.GetEntityName(), $"GetAllForLocationAsync with LocationId {locationId}");
            var cachedContent = _cachingService.Get<List<TEntity>>(cacheKey);

            if (cachedContent != null)
                return new ServiceResponse<List<TEntity>>($"Found {cachedContent.Count} {repository.GetEntityName()} records in cache with LocationId {locationId}", true, cachedContent); ;

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<TEntity>();
                var result = await dbSet.Where(b => b.LocationId == locationId).ToListAsync();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<TEntity>>($"Found {result.Count()} {repository.GetEntityName()} records with LocationId {locationId}", true, result);
            }
        }


        public async Task<ServiceResponse<List<string>>> GetDistinctEmail<TEntity>(IRepository<TEntity> repository)
            where TEntity : class, IEmail
        {
            string cacheKey = _cachingService.BuildCacheKey(repository.GetEntityName(), $"GetDistinctEmail");
            var cachedContent = _cachingService.Get<List<string>>(cacheKey);

            if (cachedContent != null)
                return new ServiceResponse<List<string>>($"Found {cachedContent.Count} {repository.GetEntityName()} records in cache", true, cachedContent); ;

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<TEntity>();
                var result = await dbSet.Select(b => b.Email).Distinct().ToListAsync();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<string>>($"Found {result.Count()} {repository.GetEntityName()} records", true, result);
            }
        }

        public async Task<ServiceResponse<List<string>>> GetDistinctEmailByLocation<TEntity>(IRepository<TEntity> repository, int locationId) where TEntity : class, IEmail, ILocationId
        {
            string cacheKey = _cachingService.BuildCacheKey(repository.GetEntityName(), $"GetDistinctEmailByLocation({locationId}");
            var cachedContent = _cachingService.Get<List<string>>(cacheKey);

            if (cachedContent != null)
                return new ServiceResponse<List<string>>($"Found {cachedContent.Count} {repository.GetEntityName()} records in cache", true, cachedContent); ;

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<TEntity>();
                var result = await dbSet.Where(o => o.LocationId == locationId).Select(b => b.Email).Distinct().ToListAsync();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<string>>($"Found {result.Count()} {repository.GetEntityName()} records", true, result);
            }
        }

        public async Task<ServiceResponse<List<TEntity>>> GetAllForLocationList<TEntity>(IRepository<TEntity> repository, List<int> locationIds) where TEntity : class, ILocationId
        {
            string cacheKey = _cachingService.BuildCacheKey(repository.GetEntityName(), $"GetAllForLocationList with LocationIds {string.Join(",", locationIds)}");
            var cachedContent = _cachingService.Get<List<TEntity>>(cacheKey);

            if (cachedContent != null)
                return new ServiceResponse<List<TEntity>>($"Found {cachedContent.Count} {repository.GetEntityName()} records in cache with LocationIds {string.Join(",", locationIds)}", true, cachedContent); ;

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<TEntity>();
                var result = await dbSet.Where(b => locationIds.Contains(b.LocationId))
                    .OrderBy(o => o.CreateDate)
                    .ToListAsync();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<TEntity>>($"Found {result.Count()} {repository.GetEntityName()} records with LocationIds {string.Join(",", locationIds)}", true, result);
            }
        }

        public async Task<ServiceResponse<TEntity>> GetByPhone<TEntity>(IRepository<TEntity> repository, string phone) where TEntity : class, IPhone, ILocationId
        {
            string cacheKey = _cachingService.BuildCacheKey(repository.GetEntityName(), $"GetByPhone({phone})");
            var cachedContent = _cachingService.Get<TEntity>(cacheKey);

            if (cachedContent != null)
                return new ServiceResponse<TEntity>($"Found {repository.GetEntityName()} record in cache with phone {phone}", true, cachedContent); ;

            using (var ctx = _contextFactory.CreateDbContext())
            {
                string phoneWithNumbersOnly = StringUtil.ExtractDigits(phone);
                string formattedPhone = phoneWithNumbersOnly.FormatPhoneNumber();

                var dbSet = ctx.Set<TEntity>();
                var result = await dbSet.Where(b => b.Phone == formattedPhone || b.Phone == phoneWithNumbersOnly).OrderByDescending(o => o.UpdateDate).FirstOrDefaultAsync();

                if (result == null)
                    return new ServiceResponse<TEntity>($"No {repository.GetEntityName()} record found with phone {phone}", false);

                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<TEntity>($"Found {repository.GetEntityName()} record with phone {phone}", true, result);
            }
        }

        public async Task<ServiceResponse<List<string>>> GetDistinctPhone<TEntity>(IRepository<TEntity> repository) where TEntity : class, IPhone
        {
            string cacheKey = _cachingService.BuildCacheKey(repository.GetEntityName(), $"GetDistinctPhone");
            var cachedContent = _cachingService.Get<List<string>>(cacheKey);
            if (cachedContent != null)
                return new ServiceResponse<List<string>>($"Found {cachedContent.Count} {repository.GetEntityName()} records in cache", true, cachedContent); ;
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<TEntity>();
                var result = await dbSet.Where(o => !String.IsNullOrEmpty(o.Phone)).Select(b => b.Phone).Distinct().ToListAsync();
                result = result.Select(p => p.FormatPhoneNumber()).Distinct().ToList();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<string>>($"Found {result.Count()} {repository.GetEntityName()} records", true, result);
            }
        }

        public async Task<ServiceResponse<List<string>>> GetDistinctPhoneByLocation<TEntity>(
            IRepository<TEntity> repository, int locationId) where TEntity : class, IPhone, ILocationId
        {
            string cacheKey = _cachingService.BuildCacheKey(repository.GetEntityName(),
                $"GetDistinctPhoneByLocation({locationId}");
            var cachedContent = _cachingService.Get<List<string>>(cacheKey);
            if (cachedContent != null)
                return new ServiceResponse<List<string>>(
                    $"Found {cachedContent.Count} {repository.GetEntityName()} records in cache", true, cachedContent);
            ;
            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<TEntity>();
                var result = await dbSet.Where(o => o.LocationId == locationId
                                                    && !String.IsNullOrEmpty(o.Phone))
                    .Select(b => b.Phone).Distinct().ToListAsync();
                result = result.Select(p => p.FormatPhoneNumber()).Distinct().ToList();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<string>>($"Found {result.Count()} {repository.GetEntityName()} records",
                    true, result);

            }
        }

        public async Task<ServiceResponse<TEntity>> GetByEmail<TEntity>(IRepository<TEntity> repository, string email) where TEntity : class, IEmail, ILocationId
        {
            string cacheKey = _cachingService.BuildCacheKey(repository.GetEntityName(), $"GetByEmail({email})");
            var cachedContent = _cachingService.Get<TEntity>(cacheKey);

            if (cachedContent != null)
                return new ServiceResponse<TEntity>($"Found {repository.GetEntityName()} record in cache with email {email}", true, cachedContent); ;

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<TEntity>();
                var result = await dbSet.Where(b => b.Email.ToLower() == email.ToLower()).OrderByDescending(o => o.UpdateDate).FirstOrDefaultAsync();

                if (result == null)
                    return new ServiceResponse<TEntity>($"No {repository.GetEntityName()} record found with email {email}", false);

                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<TEntity>($"Found {repository.GetEntityName()} record with email {email}", true, result);
            }
        }
    }
}
