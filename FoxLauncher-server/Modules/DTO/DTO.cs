using Swashbuckle.AspNetCore.Annotations; 
using System.ComponentModel.DataAnnotations; 

namespace FoxLauncher.Modules.DTO
{
    // --- DTO для загрузки файлов ---
    /// <summary>
    /// DTO для передачи файла скина.
    /// </summary>
    [SwaggerSchema("Объект, содержащий файл скина для загрузки.")]
    public class UploadSkinRequest
    {
        /// <summary>
        /// Файл скина в формате PNG.
        /// </summary>
        [Required(ErrorMessage = "Файл скина обязателен.")]
        [SwaggerSchema("Файл скина в формате PNG.", Format = "binary")]
        public IFormFile File { get; set; } = null!;
    }

    /// <summary>
    /// DTO для передачи файла плаща.
    /// </summary>
    [SwaggerSchema("Объект, содержащий файл плаща для загрузки.")]
    public class UploadCapeRequest
    {
        /// <summary>
        /// Файл плаща в формате PNG.
        /// </summary>
        [Required(ErrorMessage = "Файл плаща обязателен.")]
        [SwaggerSchema("Файл плаща в формате PNG.", Format = "binary")]
        public IFormFile File { get; set; } = null!;
    }

    // --- DTO для передачи данных ---
    // Эти DTO можно использовать как в контроллерах, так и в возвращаемых типах действий.

    /// <summary>
    /// DTO для передачи данных о скине.
    /// </summary>
    [SwaggerSchema("Информация о скине пользователя.")]
    public class SkinDto
    {
        /// <summary>
        /// Уникальный идентификатор скина.
        /// </summary>
        [SwaggerSchema("Уникальный идентификатор скина.")]
        public int Id { get; set; }

        /// <summary>
        /// Имя файла скина на сервере.
        /// </summary>
        [SwaggerSchema("Имя файла скина на сервере.")]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Оригинальное имя файла, предоставленное пользователем.
        /// </summary>
        [SwaggerSchema("Оригинальное имя файла, предоставленное пользователем.")]
        public string OriginalName { get; set; } = string.Empty;

        /// <summary>
        /// Дата и время загрузки скина.
        /// </summary>
        [SwaggerSchema("Дата и время загрузки скина.")]
        public DateTime UploadDate { get; set; }

        /// <summary>
        /// SHA256-хэш файла скина.
        /// </summary>
        [SwaggerSchema("SHA256-хэш файла скина.")]
        public string? Hash { get; set; }

        /// <summary>
        /// Размер файла скина в байтах.
        /// </summary>
        [SwaggerSchema("Размер файла скина в байтах.")]
        public long Size { get; set; }

        /// <summary>
        /// Является ли этот скин текущим для пользователя.
        /// </summary>
        [SwaggerSchema("Является ли этот скин текущим для пользователя.")]
        public bool IsCurrent { get; set; }
    }

    /// <summary>
    /// DTO для передачи данных о плаще.
    /// </summary>
    [SwaggerSchema("Информация о плаще пользователя.")]
    public class CapeDto
    {
        /// <summary>
        /// Уникальный идентификатор плаща.
        /// </summary>
        [SwaggerSchema("Уникальный идентификатор плаща.")]
        public int Id { get; set; }

        /// <summary>
        /// Имя файла плаща на сервере.
        /// </summary>
        [SwaggerSchema("Имя файла плаща на сервере.")]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Оригинальное имя файла, предоставленное пользователем.
        /// </summary>
        [SwaggerSchema("Оригинальное имя файла, предоставленное пользователем.")]
        public string OriginalName { get; set; } = string.Empty;

        /// <summary>
        /// Дата и время загрузки плаща.
        /// </summary>
        [SwaggerSchema("Дата и время загрузки плаща.")]
        public DateTime UploadDate { get; set; }

        /// <summary>
        /// SHA256-хэш файла плаща.
        /// </summary>
        [SwaggerSchema("SHA256-хэш файла плаща.")]
        public string? Hash { get; set; }

        /// <summary>
        /// Размер файла плаща в байтах.
        /// </summary>
        [SwaggerSchema("Размер файла плаща в байтах.")]
        public long Size { get; set; }

        /// <summary>
        /// Активен ли этот плащ (отображается в игре).
        /// </summary>
        [SwaggerSchema("Активен ли этот плащ (отображается в игре).")]
        public bool IsActive { get; set; }

        /// <summary>
        /// Является ли этот плащ текущим для пользователя.
        /// </summary>
        [SwaggerSchema("Является ли этот плащ текущим для пользователя.")]
        public bool IsCurrent { get; set; }
    }
}