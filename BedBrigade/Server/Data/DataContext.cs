using BedBrigade.Server.Services.AuthService;
using BedBrigade.Shared;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BedBrigade.Server.Data
{
    public class DataContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContext;
        public DataContext(DbContextOptions options, IHttpContextAccessor httpContext) : base(options)
        {
            _httpContext = httpContext;
        }

        public DbSet<BedRequest> BedRequests { get; set; }
        public DbSet<Configuration> Configurations { get; set; }
        public DbSet<ContactUs> ContactUs { get; set; }
        public DbSet<Content> Content { get; set; }
        public DbSet<Donation> Donations { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Media> Media { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Volunteer> Volunteers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            CreateIndexes(modelBuilder);
        }

        /// <summary>
        /// Create indexes for non primary and foreign keys
        /// </summary>
        /// <remarks>These could go on the models in the shared directory
        /// but then we would have to reference the entity framework and it would increase the client side payload.</remarks>
        /// <param name="modelBuilder"></param>
        private static void CreateIndexes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ContactUs>()
                .HasIndex(o => o.Status);

            modelBuilder.Entity<Content>()
                .HasIndex(o => o.ContentType);

            modelBuilder.Entity<Schedule>()
                .HasIndex(o => o.ScheduleType);

            modelBuilder.Entity<Volunteer>()
                .HasIndex(o => o.VolunteeringFor);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetAudit();
            return base.SaveChangesAsync(cancellationToken);
        }
        /// <summary>
        /// Populate the audit part of the db records
        /// </summary>
        /// <returns></returns>
        public override int SaveChanges()
        {
            SetAudit();
            return base.SaveChanges();
        }

        private void SetAudit()
        {
            string userId = "seed";
            if (_httpContext.HttpContext != null)
            {
                userId = _httpContext.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            }

            var tracker = ChangeTracker;
            foreach (var entry in tracker.Entries())
            {
                System.Diagnostics.Debug.WriteLine($"{entry.Entity} has state {entry.State} ");
                if (entry.Entity is BaseEntity)
                {
                    var referenceEntity = entry.Entity as BaseEntity;
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            referenceEntity.CreateDate = DateTime.UtcNow;
                            referenceEntity.CreateUser = userId;
                            referenceEntity.UpdateDate = DateTime.UtcNow;
                            referenceEntity.UpdateUser = userId;
                            break;
                        case EntityState.Deleted:
                            referenceEntity.UpdateDate = DateTime.UtcNow;
                            referenceEntity.UpdateUser = userId;
                            break;
                        case EntityState.Modified:
                            referenceEntity.UpdateDate = DateTime.UtcNow;
                            referenceEntity.UpdateUser = userId;
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}

