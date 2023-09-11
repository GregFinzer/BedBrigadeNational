using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BedBrigade.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
            
        }


        public virtual DbSet<BedRequest> BedRequests { get; set; }
        public virtual DbSet<Configuration> Configurations { get; set; }
        public virtual DbSet<ContactUs> ContactUs { get; set; }
        public virtual DbSet<Content> Content { get; set; }
        public virtual DbSet<Donation> Donations { get; set; }
        public virtual DbSet<Location> Locations { get; set; }
        public virtual DbSet<Media> Media { get; set; }
        public virtual DbSet<Schedule> Schedules { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Volunteer> Volunteers { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<EmailQueue> EmailQueues { get; set; }
        public virtual DbSet<VolunteerFor> VolunteersFor { get; set; }
        public virtual DbSet<VolunteerEvent> VolunteerEvents { get; set; }

        public virtual DbSet<Template> Templates { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            CreateIndexes(modelBuilder);
            SetSeedForLocation(modelBuilder);
        }

        private void SetSeedForLocation(ModelBuilder modelBuilder)
        {
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
                .HasIndex(o => o.EventType);

            modelBuilder.Entity<Volunteer>()
                .HasIndex(o => o.VolunteeringForId);
        }
        //TODO:  Remove this when all services derive from Repository
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetAudit("Seed");
            return await base.SaveChangesAsync(cancellationToken);
        }

        //TODO:  Remove this when all services derive from Repository
        /// <summary>
        /// Populate the audit part of the db records
        /// </summary>
        /// <returns></returns>
        public override int SaveChanges()
        {
            SetAudit("Seed");
            return base.SaveChanges();
        }

        //TODO:  Remove this when all services derive from Repository
        private void SetAudit(string userId)
        {
            var tracker = ChangeTracker;
            foreach (var entry in tracker.Entries())
            {
                System.Diagnostics.Debug.WriteLine($"{entry.Entity} has state {entry.State} ");
                if (entry.Entity is BaseEntity baseEntity)
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            if (String.IsNullOrEmpty(baseEntity.CreateUser))
                            {
                                baseEntity.CreateDate = DateTime.UtcNow;
                                baseEntity.CreateUser = userId;
                                baseEntity.UpdateDate = DateTime.UtcNow;
                                baseEntity.UpdateUser = userId;
                                baseEntity.MachineName = Environment.MachineName;
                            }
                            break;
                        case EntityState.Modified:
                            
                            if (!baseEntity.WasUpdatedInTheLastSecond())
                            {
                                baseEntity.UpdateDate = DateTime.UtcNow;
                                baseEntity.UpdateUser = userId;
                                baseEntity.MachineName = Environment.MachineName;
                            }
                            break;
                    }
                }
            }
        }
    }
}

