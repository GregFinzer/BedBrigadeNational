using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services
{
    public abstract class Repository<TEntity> : IRepository<TEntity>
        where TEntity : class
    {
        private readonly IDbContextFactory<DataContext> _contextFactory;
        private readonly ICachingService _cachingService;
        private readonly AuthenticationStateProvider _authProvider;
        

        public Repository(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService, AuthenticationStateProvider authProvider)
        {
            _contextFactory = contextFactory;
            _cachingService = cachingService;
            _authProvider = authProvider;
        }

        public async Task<string> GetUserName()
        {
            AuthenticationState? state = await _authProvider.GetAuthenticationStateAsync();

            if (state != null && state.User != null && state.User.Identity != null && state.User.Identity.IsAuthenticated && !String.IsNullOrEmpty(state.User.Identity.Name))
            {
                return state.User.Identity.Name;
            }

            return "Anonymous";
        }

        public string GetEntityName()
        {
            return typeof(TEntity).Name;
        }

        public async Task<ServiceResponse<List<TEntity>>> GetAllAsync()
        {
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), "GetAllAsync");
            List<TEntity>? cachedContent = _cachingService.Get<List<TEntity>>(cacheKey);

            if (cachedContent != null)
            {
                return new ServiceResponse<List<TEntity>>($"Found {cachedContent.Count()} {GetEntityName()} in cache", true, cachedContent);
            }

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<TEntity>();
                var result = await dbSet.ToListAsync();
                _cachingService.Set(cacheKey, result);
                return new ServiceResponse<List<TEntity>>($"Found {result.Count()} {GetEntityName()}", true, result);
            }
        }

        public async Task<ServiceResponse<TEntity>> GetByIdAsync(object id)
        {
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), id.ToString());
            TEntity? cachedContent = _cachingService.Get<TEntity>(cacheKey);

            if (cachedContent != null)
            {
                return new ServiceResponse<TEntity>($"Found {GetEntityName()} with id {id} in cache", true, cachedContent);
            }

            using (var ctx = _contextFactory.CreateDbContext())
            {
                var dbSet = ctx.Set<TEntity>();
                var result = await dbSet.FindAsync(id);

                if (result != null)
                {
                    _cachingService.Set(cacheKey, result);
                    return new ServiceResponse<TEntity>($"Found {GetEntityName()} with id {id}", true, result);
                }

                return new ServiceResponse<TEntity>($"Could not find {GetEntityName()} with id {id}", false);
            }
        }

        public async Task<ServiceResponse<TEntity>> CreateAsync(TEntity entity)
        {
            if (entity is BaseEntity baseEntity)
            {
                baseEntity.SetCreateUser(await GetUserName());
                baseEntity.SetUpdateUser(await GetUserName());
            }

            try
            {
                using (var ctx = _contextFactory.CreateDbContext())
                {
                    var dbSet = ctx.Set<TEntity>();
                    await dbSet.AddAsync(entity);
                    await ctx.SaveChangesAsync();
                    _cachingService.ClearByEntityName(GetEntityName());
                    return new ServiceResponse<TEntity>($"Created {GetEntityName()} with id {entity}", true, entity);
                }
            }
            catch (DbException ex)
            {
                return new ServiceResponse<TEntity>($"Could not create {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false);
            }
        }

        public async Task<ServiceResponse<TEntity>> UpdateAsync(TEntity entity)
        {
            if (entity is BaseEntity baseEntity)
            {
                baseEntity.SetUpdateUser(await GetUserName());
            }

            try
            {
                using (var ctx = _contextFactory.CreateDbContext())
                {
                    var dbSet = ctx.Set<TEntity>();
                    dbSet.Update(entity);
                    await ctx.SaveChangesAsync();
                    _cachingService.ClearByEntityName(GetEntityName());
                    return new ServiceResponse<TEntity>($"Updated {GetEntityName()} with id {entity}", true, entity);
                }
            }
            catch (DbException ex)
            {
                return new ServiceResponse<TEntity>($"Could not update {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false);
            }
        }

        public async Task<ServiceResponse<bool>> DeleteAsync(object id)
        {
            try
            {
                using (var ctx = _contextFactory.CreateDbContext())
                {
                    var dbSet = ctx.Set<TEntity>();
                    var entity = await dbSet.FindAsync(id);

                    if (entity == null)
                    {
                        return new ServiceResponse<bool>($"Could not find {GetEntityName()} with id {id}", false);
                    }

                    dbSet.Remove(entity);
                    await ctx.SaveChangesAsync();
                    _cachingService.ClearByEntityName(GetEntityName());
                    return new ServiceResponse<bool>($"Deleted {GetEntityName()} with id {id}", true);
                }
            }
            catch (DbException ex)
            {
                return new ServiceResponse<bool>($"Could not delete {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false);
            }
        }
    }
}
