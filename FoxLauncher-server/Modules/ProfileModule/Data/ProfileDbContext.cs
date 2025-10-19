using Microsoft.EntityFrameworkCore;
using FoxLauncher.Modules.ProfileModule.Models;

namespace FoxLauncher.Modules.ProfileModule.Data
{
    public class ProfileDbContext : DbContext
    {
        public ProfileDbContext(DbContextOptions<ProfileDbContext> options) : base(options)
        {
        }

        public DbSet<Profile> Profiles { get; set; }
        public DbSet<Models.Version> Versions { get; set; }
        public DbSet<GameFile> GameFiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Настройка Profile
            modelBuilder.Entity<Profile>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.IconPath).HasMaxLength(255); 

                entity.HasIndex(e => e.Name).IsUnique(); // Имя профиля должно быть уникальным

                // Каскадное удаление: при удалении профиля удаляются связанные версии и файлы
                entity.HasMany(d => d.Versions)
                      .WithOne(p => p.Profile)
                      .HasForeignKey(d => d.ProfileId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Настройка Version
            modelBuilder.Entity<Models.Version>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.JarPath).HasMaxLength(1000); // Увеличим длину пути

                // Каскадное удаление: при удалении версии удаляются связанные файлы
                entity.HasMany(d => d.Files)
                      .WithOne(p => p.Version)
                      .HasForeignKey(d => d.VersionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Настройка GameFile
            modelBuilder.Entity<GameFile>(entity =>
            {
                entity.Property(e => e.FilePath).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.Hash).HasMaxLength(64).IsRequired(); // Для SHA256

                entity.HasIndex(e => e.Hash); // Индекс для быстрого поиска по хэшу
            });
        }
    }
}