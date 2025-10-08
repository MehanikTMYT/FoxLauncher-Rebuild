using FoxLauncher.Modules.AuthModule.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FoxLauncher.Modules.AuthModule.Data
{
    public class AuthDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        public DbSet<Session> Sessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ��������� User
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Email).HasMaxLength(256); 
                entity.Property(e => e.Username).HasMaxLength(255);
                entity.Property(e => e.Uuid).HasMaxLength(36).IsRequired(); 
                entity.Property(e => e.EmailConfirmationToken).HasMaxLength(255);
                // �������
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Uuid).IsUnique(); 
            });

            // ��������� Session
            modelBuilder.Entity<Session>(entity =>
            {
                entity.Property(e => e.AccessToken).HasMaxLength(512).IsRequired();
                entity.Property(e => e.ClientToken).HasMaxLength(512).IsRequired();
                entity.HasIndex(e => e.AccessToken).IsUnique(); 

                // �����: Session -> User
                entity.HasOne(d => d.User)
                      .WithMany(p => p.Sessions) 
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade); // ������� ������ ��� �������� ������������
            });
        }
    }
}