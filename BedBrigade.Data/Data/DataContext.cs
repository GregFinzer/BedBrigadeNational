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
        public virtual DbSet<Translation> Translations { get; set; }
        public virtual DbSet<ContentTranslation> ContentTranslations { get; set; }
        public virtual DbSet<TranslationQueue> TranslationQueues { get; set; }
        public virtual DbSet<ContentTranslationQueue> ContentTranslationQueues { get; set; }
        public virtual DbSet<SpokenLanguage> SpokenLanguages { get; set; }
        public virtual DbSet<GeoLocationQueue> GeoLocationQueue { get; set; }
        public virtual DbSet<SmsQueue> SmsQueue { get; set; }
        public virtual DbSet<Newsletter> Newsletters { get; set; }
        public virtual DbSet<Subscription> Subscriptions { get; set; }

        public virtual DbSet<DonationCampaign> DonationCampaigns { get; set; }
        public virtual DbSet<ContentHistory> ContentHistories { get; set; }

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
            CreateBedRequestIndexes(modelBuilder);
            CreateConfigurationIndexes(modelBuilder);
            CreateContactUsIndexes(modelBuilder);
            CreateContentIndexes(modelBuilder);
            CreateContentTranslationIndexes(modelBuilder);

            //Donation
            modelBuilder.Entity<Donation>()
                .HasIndex(o => o.LocationId);

            CreateScheduleIndexes(modelBuilder);
            CreateSignUpIndexes(modelBuilder);
            CreateSmsQueueIndexes(modelBuilder);

            //Translation
            modelBuilder.Entity<Translation>()
                .HasIndex(o => o.Culture);

            CreateUserIndexes(modelBuilder);

            //User Persist
            modelBuilder.Entity<UserPersist>()
                .HasKey(up => new { up.UserName, up.Grid });

            CreateVolunteerIndexes(modelBuilder);
            CreateNewsletterIndexes(modelBuilder);
        }

        private static void CreateNewsletterIndexes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Newsletter>()
                .HasIndex(e => e.LocationId);

            modelBuilder.Entity<Subscription>()
                .HasIndex(e => e.NewsletterId);
        }

        private static void CreateConfigurationIndexes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Configuration>()
                .HasIndex(o => new { o.LocationId, o.ConfigurationKey })
                .HasDatabaseName("IX_Configuration_LocationId_ConfigurationKey");
        }

        private static void CreateSmsQueueIndexes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SmsQueue>()
                .HasIndex(o => o.LocationId);

            modelBuilder.Entity<SmsQueue>()
                .HasIndex(o => o.ToPhoneNumber);
        }

        private static void CreateUserIndexes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(o => o.LocationId);

            modelBuilder.Entity<User>()
                .HasIndex(o => o.Phone);
        }

        private static void CreateVolunteerIndexes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Volunteer>()
                .HasIndex(e => e.Email)
                .IsUnique();

            modelBuilder.Entity<Volunteer>()
                .HasIndex(e => e.LocationId);

            modelBuilder.Entity<Volunteer>()
                .HasIndex(e => e.Phone);
        }

        private static void CreateSignUpIndexes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SignUp>()
                .HasIndex(o => o.LocationId);

            modelBuilder.Entity<SignUp>()
                .HasIndex(o => o.ScheduleId);
        }

        private static void CreateScheduleIndexes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Schedule>()
                .HasIndex(o => o.EventType);

            modelBuilder.Entity<Schedule>()
                .HasIndex(o => o.LocationId);

        }

        private static void CreateContentTranslationIndexes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ContentTranslation>()
                .HasIndex(o => o.LocationId);

            modelBuilder.Entity<ContentTranslation>()
                .HasIndex(o => o.Name);

            modelBuilder.Entity<ContentTranslation>()
                .HasIndex(o => o.Culture);
        }

        private static void CreateContentIndexes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Content>()
                .HasIndex(o => o.ContentType);

            modelBuilder.Entity<Content>()
                .HasIndex(o => o.LocationId);

            modelBuilder.Entity<Content>()
                .HasIndex(o => o.Name);
        }

        private static void CreateContactUsIndexes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ContactUs>()
                .HasIndex(o => o.Status);

            modelBuilder.Entity<ContactUs>()
                .HasIndex(o => o.LocationId);

            modelBuilder.Entity<ContactUs>()
                .HasIndex(o => o.Phone);
        }

        private static void CreateBedRequestIndexes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BedRequest>()
                .HasIndex(o => o.ScheduleId);

            modelBuilder.Entity<BedRequest>()
                .HasIndex(o => o.LocationId);

            modelBuilder.Entity<BedRequest>()
                .HasIndex(o => o.Status);

            modelBuilder.Entity<BedRequest>()
                .HasIndex(o => o.Phone);
        }
    }
}

