using System.Data.Common;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;

using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BedBrigade.Data.Services
{
    public abstract class Repository<TEntity> : IRepository<TEntity>
        where TEntity : BaseEntity
    {
        private readonly IDbContextFactory<DataContext> _contextFactory;
        private readonly ICachingService _cachingService;
        private readonly IAuthService _authService;
        

        public Repository(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService, IAuthService authService)
        {
            _contextFactory = contextFactory;
            _cachingService = cachingService;
            _authService = authService;
        }

        //This is the email address stored in the User.Identity.Name
        public string GetUserEmail()
        {
            return _authService.Email;
        }
        public string GetUserPhone()
        {
            return _authService.Phone;
        }

        //This is the user name stored in the nameidentifier
        public string GetUserName()
        {
            return _authService.UserName;
        }

        public string? GetUserRole()
        {
            return _authService.UserRole;
        }

        public int GetUserLocationId()
        {
            return _authService.LocationId;
        }

        public string GetEntityName()
        {
            return typeof(TEntity).Name;
        }

        public bool IsUserNationalAdmin()
        {
            string roleName = GetUserRole() ?? string.Empty;

            return roleName.ToLower() == RoleNames.NationalAdmin.ToLower();
        }

        public virtual async Task<ServiceResponse<List<TEntity>>> GetAllAsync()
        {
            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), "GetAllAsync");
            List<TEntity>? cachedContent = _cachingService.Get<List<TEntity>>(cacheKey);

            if (cachedContent != null)
            {
                return new ServiceResponse<List<TEntity>>($"Found {cachedContent.Count()} {GetEntityName()} in cache", true, cachedContent);
            }

            try
            {
                using (var ctx = _contextFactory.CreateDbContext())
                {
                    var dbSet = ctx.Set<TEntity>();
                    var result = await dbSet.ToListAsync();
                    _cachingService.Set(cacheKey, result);
                    return new ServiceResponse<List<TEntity>>($"Found {result.Count()} {GetEntityName()}", true, result);
                }
            }
            catch (DbException ex)
            {
                return new ServiceResponse<List<TEntity>>($"Error GetAllAsync for {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false, null);
            }

        }

        public virtual async Task<ServiceResponse<TEntity>> GetByIdAsync(object? id)
        {
            if (id == null)
                return new ServiceResponse<TEntity>($"Could not GetByIdAsync {GetEntityName()} with null id", false);

            string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), id.ToString());
            TEntity? cachedContent = _cachingService.Get<TEntity>(cacheKey);

            if (cachedContent != null)
            {
                return new ServiceResponse<TEntity>($"Found {GetEntityName()} with id {id} in cache", true, cachedContent);
            }

            try
            {
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
            catch (DbException ex)
            {
                return new ServiceResponse<TEntity>($"Could not GetByIdAsync {GetEntityName()}  with id {id}: {ex.Message} ({ex.ErrorCode})", false);
            }


        }

        public virtual async Task<ServiceResponse<TEntity>> CreateAsync(TEntity entity)
        {
            string userName = GetUserName() ?? Defaults.DefaultUserNameAndEmail;
            entity.SetCreateAndUpdateUser(userName);
            
            try
            {
                using (var ctx = _contextFactory.CreateDbContext())
                {
                    var dbSet = ctx.Set<TEntity>();
                    await dbSet.AddAsync(entity);
                    await ctx.SaveChangesAsync();
                    _cachingService.ClearByEntityName(GetEntityName());
                    Log.Debug($"{userName} Created {GetEntityName()}{Environment.NewLine}{ObjectUtil.ObjectToString(entity)}");
                    return new ServiceResponse<TEntity>($"Created {GetEntityName()} with id {entity}", true, entity);
                }
            }
            catch (DbException ex)
            {
                return new ServiceResponse<TEntity>($"Could not create {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false);
            }
        }

        public virtual async Task<ServiceResponse<TEntity>> UpdateAsync(TEntity entity)
        {

            string userName = GetUserName() ?? Defaults.DefaultUserNameAndEmail;
            entity.SetUpdateUser(userName);

            try
            {
                
                using (var ctx = _contextFactory.CreateDbContext())
                {
                    var dbSet = ctx.Set<TEntity>();
                    var primaryKeyValue = GetPrimaryKeyValue(ctx, entity);
                    var originalEntity = await ctx.FindAsync<TEntity>(primaryKeyValue);
                    dbSet.Update(entity);
                    await ctx.SaveChangesAsync();
                    _cachingService.ClearByEntityName(GetEntityName());
                    Log.Debug($"{userName} Updated {GetEntityName()} with ID: {primaryKeyValue}{Environment.NewLine}{ObjectUtil.Differences(originalEntity, entity)}");
                    return new ServiceResponse<TEntity>($"Updated {GetEntityName()} with id {entity}", true, entity);
                }
            }
            catch (DbException ex)
            {
                return new ServiceResponse<TEntity>($"Could not update {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false);
            }
        }

        public virtual async Task<ServiceResponse<bool>> DeleteAsync(object id)
        {
            string userName = GetUserName() ?? Defaults.DefaultUserNameAndEmail;

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
                    Log.Debug($"{userName} Deleted {GetEntityName()}{Environment.NewLine}{ObjectUtil.ObjectToString(entity)}");
                    return new ServiceResponse<bool>($"Deleted {GetEntityName()} with id {id}", true);
                }
            }
            catch (DbException ex)
            {
                return new ServiceResponse<bool>($"Could not delete {GetEntityName()}: {ex.Message} ({ex.ErrorCode})", false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Unexpected error deleting {GetEntityName()} with id {id}");
                return new ServiceResponse<bool>($"Unexpected error deleting {GetEntityName()} with id {id}: {ex.Message}", false);
            }
        }

        private object GetPrimaryKeyValue<TEntity>(DbContext context, TEntity entity) where TEntity : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // Get the entity type of TEntity from the DbContext
            var entityType = context.Model.FindEntityType(typeof(TEntity));

            // Get the primary key for the entity
            var primaryKey = entityType.FindPrimaryKey();

            // Return the value of the property used as the primary key
            var keyProperty = primaryKey?.Properties.FirstOrDefault();
            return keyProperty?.GetGetter().GetClrValue(entity);
        }


    }
}
