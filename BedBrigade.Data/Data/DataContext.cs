using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;

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
        public virtual DbSet<UserPersist> UserPersist { get; set; }
        public virtual DbSet<Volunteer> Volunteers { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<EmailQueue> EmailQueues { get; set; }
        public virtual DbSet<VolunteerFor> VolunteersFor { get; set; }
        public virtual DbSet<SignUp> SignUps { get; set; }
        public virtual DbSet<Template> Templates { get; set; }
        public virtual DbSet<MetroArea> MetroAreas { get; set; }

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
            modelBuilder.Entity<UserPersist>()
                .HasKey(up => new { up.UserName, up.Grid });

            modelBuilder.Entity<ContactUs>()
                .HasIndex(o => o.Status);

            modelBuilder.Entity<Content>()
                .HasIndex(o => o.ContentType);

            modelBuilder.Entity<Schedule>()
                .HasIndex(o => o.EventType);

            modelBuilder.Entity<Volunteer>()
                .HasIndex(e => e.Email)
                .IsUnique();
        }
    }
}

