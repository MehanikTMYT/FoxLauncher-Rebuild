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
        public DbSet<Skin> Skins { get; set; } // Добавляем DbSet для Skin
        public DbSet<Cape> Capes { get; set; } // Добавляем DbSet для Cape

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Настройка User
            modelBuilder.Entity<CabinetModule.Models.User>(entity =>
            {
                entity.Property(e => e.Email).HasMaxLength(256); // Согласно IdentityUser
                entity.Property(e => e.Username).HasMaxLength(255);
                entity.Property(e => e.UUID).HasMaxLength(36).IsRequired(); // Добавляем Uuid
                entity.Property(e => e.EmailConfirmationToken).HasMaxLength(255);
                // Индексы
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.UUID).IsUnique(); // UUID должен быть уникальным
                // Индекс для внешних ключей
                entity.HasIndex(e => e.CurrentSkinId);
                entity.HasIndex(e => e.CurrentCapeId);
            });

            // Настройка Session
            modelBuilder.Entity<Session>(entity =>
            {
                entity.Property(e => e.AccessToken).HasMaxLength(512).IsRequired();
                entity.Property(e => e.ClientToken).HasMaxLength(512).IsRequired();
                entity.HasIndex(e => e.AccessToken).IsUnique(); // Токен доступа должен быть уникальным

                // Связь: Session -> User
                entity.HasOne(d => d.User)
                      .WithMany(p => p.Sessions) // Предполагаем, что в User Sessions настроены как ICollection<Session>
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade); // Удалять сессии при удалении пользователя
            });

            // Настройка Skin
            modelBuilder.Entity<Skin>(entity =>
            {
                entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
                entity.Property(e => e.OriginalName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Hash).HasMaxLength(64); // Для SHA256

                // Связь: Skin -> User
                entity.HasOne(d => d.User)
                      .WithMany(p => p.Skins) // Скины пользователя
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade); // Удалять скины при удалении пользователя
            });

            // Настройка Cape
            modelBuilder.Entity<Cape>(entity =>
            {
                entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
                entity.Property(e => e.OriginalName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Hash).HasMaxLength(64); // Для SHA256

                // Связь: Cape -> User
                entity.HasOne(d => d.User)
                      .WithMany(p => p.Capes) // Плащи пользователя
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade); // Удалять плащи при удалении пользователя
            });
        }
    }
}