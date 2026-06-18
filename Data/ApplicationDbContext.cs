using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ResourceHub.Models;

namespace ResourceHub.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<ResourceHub.Models.Resource> Resources { get; set; }

        public DbSet<ResourceComment> ResourceComments { get; set; }

        public DbSet<ResourceRating> ResourceRatings { get; set; }

        public DbSet<ResourceReport> ResourceReports { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ResourceRating>()
                .HasIndex(r => new { r.ResourceId, r.UserId })
                .IsUnique();

            builder.Entity<ResourceReport>()
                .HasIndex(r => new { r.ResourceId, r.UserId })
                .IsUnique();
        }
    }
}
