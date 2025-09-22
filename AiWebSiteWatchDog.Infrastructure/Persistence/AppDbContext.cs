using Microsoft.EntityFrameworkCore;
using AiWebSiteWatchDog.Domain.Entities;

namespace AiWebSiteWatchDog.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public DbSet<UserSettings> UserSettings { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<WatchTask> WatchTasks { get; set; }
        public DbSet<EmailSettings> EmailSettings { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserSettings>().HasKey(u => u.Email);
            modelBuilder.Entity<EmailSettings>().HasKey(e => e.Id);
            modelBuilder.Entity<Notification>().HasKey(n => n.Id);
            modelBuilder.Entity<WatchTask>().HasKey(w => w.Id);
            // Configure one-to-one relationship between UserSettings and EmailSettings
            modelBuilder.Entity<UserSettings>()
                .HasOne(u => u.EmailSettings)
                .WithOne()
                .HasForeignKey<UserSettings>(u => u.EmailSettingsId);
        }
    }
}
