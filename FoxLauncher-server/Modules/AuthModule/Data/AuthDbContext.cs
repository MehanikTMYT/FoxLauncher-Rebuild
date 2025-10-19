using FoxLauncher.Modules.AuthModule.Models;
using FoxLauncher.Modules.CabinetModule.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FoxLauncher.Modules.AuthModule.Data
{
    public class AuthDbContext : IdentityDbContext<CabinetModule.Models.User, IdentityRole<int>, int>
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        public DbSet<Session> Sessions { get; set; }
        public DbSet<Skin> Skins { get; set; } // ��������� DbSet ��� Skin
        public DbSet<Cape> Capes { get; set; } // ��������� DbSet ��� Cape

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ��������� User
            modelBuilder.Entity<CabinetModule.Models.User>(entity =>
            {
                entity.Property(e => e.Email).HasMaxLength(256); // �������� IdentityUser
                entity.Property(e => e.Username).HasMaxLength(255);
                entity.Property(e => e.UUID).HasMaxLength(36).IsRequired(); // ��������� Uuid
                entity.Property(e => e.EmailConfirmationToken).HasMaxLength(255);
                // �������
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.UUID).IsUnique(); // UUID ������ ���� ����������
                // ������ ��� ������� ������
                entity.HasIndex(e => e.CurrentSkinId);
                entity.HasIndex(e => e.CurrentCapeId);
            });

            // ��������� Session
            modelBuilder.Entity<Session>(entity =>
            {
                entity.Property(e => e.AccessToken).HasMaxLength(512).IsRequired();
                entity.Property(e => e.ClientToken).HasMaxLength(512).IsRequired();
                entity.HasIndex(e => e.AccessToken).IsUnique(); // ����� ������� ������ ���� ����������

                // �����: Session -> User
                entity.HasOne(d => d.User)
                      .WithMany(p => p.Sessions) // ������������, ��� � User Sessions ��������� ��� ICollection<Session>
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade); // ������� ������ ��� �������� ������������
            });

            // ��������� Skin
            modelBuilder.Entity<Skin>(entity =>
            {
                entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
                entity.Property(e => e.OriginalName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Hash).HasMaxLength(64); // ��� SHA256

                // �����: Skin -> User
                entity.HasOne(d => d.User)
                      .WithMany(p => p.Skins) // ����� ������������
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade); // ������� ����� ��� �������� ������������
            });

            // ��������� Cape
            modelBuilder.Entity<Cape>(entity =>
            {
                entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
                entity.Property(e => e.OriginalName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Hash).HasMaxLength(64); // ��� SHA256

                // �����: Cape -> User
                entity.HasOne(d => d.User)
                      .WithMany(p => p.Capes) // ����� ������������
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade); // ������� ����� ��� �������� ������������
            });
        }
    }
}