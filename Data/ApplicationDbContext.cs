using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NpsProject.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace NpsProject.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
            : base(options)
        {
        }

        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<NewsArticle> NewsArticles { get; set; }
        public DbSet<Project> Projects { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // تكوين إضافي للجداول
            modelBuilder.Entity<ContactMessage>(entity =>
            {
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.IsRead);
            });

            modelBuilder.Entity<NewsArticle>(entity =>
            {
                entity.HasIndex(e => e.PublishedDate);
                entity.HasIndex(e => e.IsPublished);
            });

            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasIndex(e => e.IsActive);
            });
            modelBuilder.Entity<ApplicationUser>().ToTable("Users");

        }
    }
}
