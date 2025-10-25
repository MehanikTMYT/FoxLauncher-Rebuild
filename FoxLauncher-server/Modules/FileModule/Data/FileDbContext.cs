using Microsoft.EntityFrameworkCore;
using FoxLauncher.Modules.FileModule.Models; 

namespace FoxLauncher.Modules.FileModule.Data
{
    public class FileDbContext : DbContext
    {
        public FileDbContext(DbContextOptions<FileDbContext> options) : base(options)
        {
        }

        // DbSet для логов загрузок (если используется)
        public DbSet<DownloadLog> DownloadLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Настройка DownloadLog (если используется)
            modelBuilder.Entity<DownloadLog>(entity =>
            {
                entity.Property(e => e.FilePath).HasMaxLength(1000).IsRequired();
                // entity.HasIndex(e => e.UserId); // Индекс для поиска по пользователю
                // entity.HasIndex(e => e.VersionId); // Индекс для поиска по версии
            });
        }
    }
}