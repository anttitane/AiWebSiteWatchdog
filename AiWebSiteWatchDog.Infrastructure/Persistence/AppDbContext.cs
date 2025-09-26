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
    public DbSet<GoogleOAuthToken> GoogleOAuthTokens { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserSettings>().HasKey(u => u.EmailRecipient);
            modelBuilder.Entity<EmailSettings>().HasKey(e => e.SenderEmail);
            modelBuilder.Entity<Notification>().HasKey(n => n.Id);
            modelBuilder.Entity<WatchTask>().HasKey(w => w.Id);
            modelBuilder.Entity<GoogleOAuthToken>().HasKey(t => t.Id);
            modelBuilder.Entity<GoogleOAuthToken>().HasIndex(t => t.Email).IsUnique();
            // NOTE: After adding GoogleOAuthToken entity, create a new EF Core migration:
            // dotnet ef migrations add AddGoogleOAuthTokens
            // and update the database.
            // Configure one-to-one relationship between UserSettings and EmailSettings using SenderEmail
            modelBuilder.Entity<UserSettings>()
                .HasOne(u => u.EmailSettings)
                .WithOne()
                .HasForeignKey<UserSettings>(u => u.EmailSettingsSenderEmail);
        }
    }
}
