using Microsoft.EntityFrameworkCore;
using AiWebSiteWatchDog.Domain.Entities;

namespace AiWebSiteWatchDog.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public DbSet<UserSettings> UserSettings { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<WatchTask> WatchTasks { get; set; }
    public DbSet<GoogleOAuthToken> GoogleOAuthTokens { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserSettings>().HasKey(u => u.UserEmail);
            modelBuilder.Entity<Notification>().HasKey(n => n.Id);
            modelBuilder.Entity<WatchTask>().HasKey(w => w.Id);
            modelBuilder.Entity<GoogleOAuthToken>().HasKey(t => t.Id);
            modelBuilder.Entity<GoogleOAuthToken>().HasIndex(t => t.Email).IsUnique();
            // Configure one-to-many relationship between UserSettings and WatchTask
            modelBuilder.Entity<UserSettings>()
                .HasMany(u => u.WatchTasks)
                .WithOne(w => w.UserSettings)
                .HasForeignKey(w => w.UserSettingsId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
