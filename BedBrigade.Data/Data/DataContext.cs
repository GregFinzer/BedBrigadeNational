﻿using BedBrigade.Data.Models;
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

            modelBuilder.Entity<BedRequest>()
                .HasIndex(o => o.ScheduleId);
        }
    }
}

