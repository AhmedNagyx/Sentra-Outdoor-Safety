using Microsoft.EntityFrameworkCore;
using Sentra.API.Models;

namespace Sentra.API.Data
{
    public class SentraDbContext : DbContext
    {
        public SentraDbContext(DbContextOptions<SentraDbContext> options)
            : base(options)
        {
        }

        // DbSets - These become tables in the database
        public DbSet<User> Users { get; set; }
        public DbSet<Camera> Cameras { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<Alert> Alerts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== USER CONFIGURATION =====
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Role).HasDefaultValue("Resident");
            });

            // ===== CAMERA CONFIGURATION =====
            modelBuilder.Entity<Camera>(entity =>
            {
                entity.HasOne(c => c.User)
                      .WithMany(u => u.Cameras)
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== INCIDENT CONFIGURATION =====
            modelBuilder.Entity<Incident>(entity =>
            {
                entity.HasOne(i => i.Camera)
                      .WithMany(c => c.Incidents)
                      .HasForeignKey(i => i.CameraId)
                      .OnDelete(DeleteBehavior.Cascade);

                // FIXED: Changed to NoAction to prevent cascade conflict
                entity.HasOne(i => i.ResolvedByUser)
                      .WithMany()
                      .HasForeignKey(i => i.ResolvedByUserId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // ===== ALERT CONFIGURATION =====
            modelBuilder.Entity<Alert>(entity =>
            {
                entity.HasOne(a => a.Incident)
                      .WithMany(i => i.Alerts)
                      .HasForeignKey(a => a.IncidentId)
                      .OnDelete(DeleteBehavior.Cascade);

                // FIXED: NoAction to prevent cascade conflict
                entity.HasOne(a => a.User)
                      .WithMany()
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}