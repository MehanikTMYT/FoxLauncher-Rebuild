using FoxLauncher.Modules.AuthModule.Data;
using FoxLauncher.Modules.CabinetModule.Models;
using FoxLauncher.Modules.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using System.Security.Cryptography;


namespace FoxLauncher.Modules.CabinetModule.Controllers
{
    
    /// <summary>
    /// Контроллер для управления личным кабинетом пользователя (скины, плащи).
    /// </summary>
    [ApiController]
    [Route("api/cabinet")]
    [Authorize(Policy = "RequireUserRole")]
    public class CabinetController : ControllerBase
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<CabinetController> _logger;
        // Опциональный сервис для работы с файлами
        // private readonly IFileStorageService _fileStorageService;

        public CabinetController(AuthDbContext context, ILogger<CabinetController> logger/*, IFileStorageService fileStorageService*/)
        {
            _context = context;
            _logger = logger;
            // _fileStorageService = fileStorageService;
        }

        /// <summary>
        /// Получить список скинов текущего пользователя.
        /// </summary>
        /// <returns>Список DTO скинов пользователя.</returns>
        [HttpGet("skins")]
        [SwaggerOperation(
            Summary = "Получить список скинов",
            Description = "Возвращает список скинов, загруженных и принадлежащих аутентифицированному пользователю."
        )]
        [ProducesResponseType(typeof(IEnumerable<SkinDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<SkinDto>>> GetSkins()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var skins = await _context.Skins
                .Where(s => s.UserId == userId)
                .Select(s => new SkinDto
                {
                    Id = s.Id,
                    FileName = s.FileName,
                    OriginalName = s.OriginalName,
                    UploadDate = s.UploadDate,
                    Hash = s.Hash,
                    Size = (long)s.Size,
                    IsCurrent = s.Id == _context.Users.Where(u => u.Id == userId).Select(u => u.CurrentSkinId).FirstOrDefault()
                })
                .ToListAsync();

            return Ok(skins);
        }

        /// <summary>
        /// Получить список плащей текущего пользователя.
        /// </summary>
        /// <returns>Список DTO плащей пользователя.</returns>
        [HttpGet("capes")]
        [SwaggerOperation(
            Summary = "Получить список плащей",
            Description = "Возвращает список плащей, загруженных и принадлежащих аутентифицированному пользователю."
        )]
        [ProducesResponseType(typeof(IEnumerable<CapeDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<CapeDto>>> GetCapes()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var capes = await _context.Capes
                .Where(c => c.UserId == userId)
                .Select(c => new CapeDto
                {
                    Id = c.Id,
                    FileName = c.FileName,
                    OriginalName = c.OriginalName,
                    UploadDate = c.UploadDate,
                    Hash = c.Hash,
                    Size = (long)c.Size,
                    IsActive = c.IsActive,
                    IsCurrent = c.Id == _context.Users.Where(u => u.Id == userId).Select(u => u.CurrentCapeId).FirstOrDefault()
                })
                .ToListAsync();

            return Ok(capes);
        }

        /// <summary>
        /// Загрузить новый скин.
        /// </summary>
        /// <param name="request">Объект, содержащий файл скина.</param>
        /// <returns>DTO загруженного скина.</returns>
        [HttpPost("skin")]
        [SwaggerOperation(
            Summary = "Загрузить скин",
            Description = "Позволяет пользователю загрузить новый скин в формате PNG. Файл сохраняется на сервере, и создается запись в базе данных."
        )]
        [ProducesResponseType(typeof(SkinDto), 201)]
        [ProducesResponseType(400)] // Bad Request при пустом файле, неверном типе, лимите
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SkinDto>> UploadSkin([FromForm][SwaggerRequestBody("Объект, содержащий файл скина.")] UploadSkinRequest request)
        {
            var file = request.File; // Получаем файл из DTO

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("User {UserId} attempted to upload an empty skin file.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return BadRequest("File is empty.");
            }

            if (!IsValidImageFile(file))
            {
                _logger.LogWarning("User {UserId} attempted to upload an invalid skin file type: {FileType}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, Path.GetExtension(file.FileName));
                return BadRequest("Invalid file type. Only PNG images are allowed.");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            // Проверить лимиты (опционально)
            var skinCount = await _context.Skins.CountAsync(s => s.UserId == userId);
            if (skinCount >= 10) // Пример лимита
            {
                _logger.LogWarning("User {UserId} attempted to upload a skin but has reached the limit.", userId);
                return BadRequest("Skin limit reached.");
            }

            // Генерация уникального имени файла
            var fileName = $"{Guid.NewGuid()}.png"; // Или используйте UserId и счетчик
            var filePath = Path.Combine("wwwroot", "skins", fileName); // Папка для скинов

            // Убедитесь, что папка существует
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            string? hash = null; // Объявляем переменную hash до блока using
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                    // Вычисление хэша сразу после записи
                    stream.Seek(0, SeekOrigin.Begin); // Сброс позиции потока на начало
                    using var sha256 = SHA256.Create();
                    var hashBytes = await sha256.ComputeHashAsync(stream);
                    hash = Convert.ToHexString(hashBytes); // Присваиваем значение переменной hash
                } // FileStream автоматически закрывается и сбрасывает буферы
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving skin file {FileName} for user {UserId}.", fileName, userId);
                return StatusCode(500, "An error occurred while saving the file.");
            }


            var skin = new Skin
            {
                UserId = userId,
                FileName = fileName,
                OriginalName = file.FileName,
                Hash = hash, // Теперь hash доступна из внешней области видимости
                Size = file.Length,
                UploadDate = DateTime.UtcNow // Устанавливаем дату загрузки
            };

            _context.Skins.Add(skin);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Удаляем файл, если не удалось сохранить в БД
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                _logger.LogError(ex, "Error occurred while saving skin record to database for user {UserId}.", userId);
                return StatusCode(500, "An error occurred while saving to the database.");
            }

            // Установить как текущий, если это первый скин
            var user = await _context.Users.FindAsync(userId);
            if (user != null && user.CurrentSkinId == null)
            {
                user.CurrentSkinId = skin.Id;
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while updating user's current skin for user {UserId}.", userId);
                    // Здесь можно решить, стоит ли откатывать добавление скина или нет.
                    // Для простоты, оставим скин добавленным, но CurrentSkinId не обновится.
                }
            }

            var skinDto = new SkinDto
            {
                Id = skin.Id,
                FileName = skin.FileName,
                OriginalName = skin.OriginalName,
                UploadDate = skin.UploadDate,
                Hash = skin.Hash,
                Size = (long)skin.Size,
                IsCurrent = user?.CurrentSkinId == skin.Id // Проверяем, стал ли он текущим
            };

            _logger.LogInformation("User {UserId} successfully uploaded a new skin {SkinId} ({FileName}).", userId, skin.Id, skin.FileName);
            return CreatedAtAction(nameof(GetSkins), new { id = skin.Id }, skinDto);
        }

        /// <summary>
        /// Загрузить новый плащ.
        /// </summary>
        /// <param name="request">Объект, содержащий файл плаща.</param>
        /// <returns>DTO загруженного плаща.</returns>
        [HttpPost("cape")]
        [SwaggerOperation(
            Summary = "Загрузить плащ",
            Description = "Позволяет пользователю загрузить новый плащ в формате PNG. Файл сохраняется на сервере, и создается запись в базе данных."
        )]
        [ProducesResponseType(typeof(CapeDto), 201)]
        [ProducesResponseType(400)] // Bad Request при пустом файле, неверном типе, лимите
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<CapeDto>> UploadCape([FromForm][SwaggerRequestBody("Объект, содержащий файл плаща.")] UploadCapeRequest request)
        {
            var file = request.File; // Получаем файл из DTO

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("User {UserId} attempted to upload an empty cape file.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return BadRequest("File is empty.");
            }

            if (!IsValidImageFile(file))
            {
                _logger.LogWarning("User {UserId} attempted to upload an invalid cape file type: {FileType}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, Path.GetExtension(file.FileName));
                return BadRequest("Invalid file type. Only PNG images are allowed.");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            // Проверить лимиты (опционально)
            var capeCount = await _context.Capes.CountAsync(s => s.UserId == userId);
            if (capeCount >= 5) // Пример лимита
            {
                _logger.LogWarning("User {UserId} attempted to upload a cape but has reached the limit.", userId);
                return BadRequest("Cape limit reached.");
            }

            // Генерация уникального имени файла
            var fileName = $"{Guid.NewGuid()}.png"; // Или используйте UserId и счетчик
            var filePath = Path.Combine("wwwroot", "capes", fileName); // Папка для плащей

            // Убедитесь, что папка существует
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            string? hash = null; // Объявляем переменную hash до блока using
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                    // Вычисление хэша сразу после записи
                    stream.Seek(0, SeekOrigin.Begin); // Сброс позиции потока на начало
                    using var sha256 = SHA256.Create();
                    var hashBytes = await sha256.ComputeHashAsync(stream);
                    hash = Convert.ToHexString(hashBytes); // Присваиваем значение переменной hash
                } // FileStream автоматически закрывается и сбрасывает буферы
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving cape file {FileName} for user {UserId}.", fileName, userId);
                return StatusCode(500, "An error occurred while saving the file.");
            }

            var cape = new Cape
            {
                UserId = userId,
                FileName = fileName,
                OriginalName = file.FileName,
                Hash = hash, // Теперь hash доступна из внешней области видимости
                Size = file.Length,
                UploadDate = DateTime.UtcNow // Устанавливаем дату загрузки
            };

            _context.Capes.Add(cape);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Удаляем файл, если не удалось сохранить в БД
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                _logger.LogError(ex, "Error occurred while saving cape record to database for user {UserId}.", userId);
                return StatusCode(500, "An error occurred while saving to the database.");
            }

            // Установить как текущий (активный), если это первый плащ
            var user = await _context.Users.FindAsync(userId);
            if (user != null && user.CurrentCapeId == null)
            {
                user.CurrentCapeId = cape.Id;
                cape.IsActive = true; // Установить активность
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while updating user's current cape for user {UserId}.", userId);
                    // Здесь можно решить, стоит ли откатывать добавление плаща или нет.
                    // Для простоты, оставим плащ добавленным, но CurrentCapeId и IsActive не обновятся.
                }
            }

            var capeDto = new CapeDto
            {
                Id = cape.Id,
                FileName = cape.FileName,
                OriginalName = cape.OriginalName,
                UploadDate = cape.UploadDate,
                Hash = cape.Hash,
                Size = (long)cape.Size,
                IsActive = cape.IsActive,
                IsCurrent = user?.CurrentCapeId == cape.Id // Проверяем, стал ли он текущим
            };

            _logger.LogInformation("User {UserId} successfully uploaded a new cape {CapeId} ({FileName}).", userId, cape.Id, cape.FileName);
            return CreatedAtAction(nameof(GetCapes), new { id = cape.Id }, capeDto);
        }

        /// <summary>
        /// Удалить скин по ID.
        /// </summary>
        /// <param name="skinId">ID скина для удаления.</param>
        /// <returns>Результат операции.</returns>
        [HttpDelete("skin/{skinId}")]
        [SwaggerOperation(
            Summary = "Удалить скин",
            Description = "Удаляет скин, принадлежащий пользователю, по его ID. Также удаляет файл с диска."
        )]
        [ProducesResponseType(204)] // No Content при успехе
        [ProducesResponseType(401)]
        [ProducesResponseType(404)] // Not Found при отсутствии скина или несоответствии пользователю
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteSkin(int skinId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var skin = await _context.Skins
                .Where(s => s.Id == skinId && s.UserId == userId)
                .FirstOrDefaultAsync();

            if (skin == null)
            {
                _logger.LogWarning("User {UserId} attempted to delete a non-existent or non-owned skin {SkinId}.", userId, skinId);
                return NotFound("Skin not found or does not belong to the user.");
            }

            // Удалить файл с диска
            var filePath = Path.Combine("wwwroot", "skins", skin.FileName);
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogDebug("Deleted skin file {FilePath} for user {UserId}.", filePath, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting skin file {FilePath} for user {UserId}.", filePath, userId);
                // Важно: файл не удален, но запись в БД будет удалена.
                // В реальности, может потребоваться более сложная логика отката.
            }


            // Если это текущий скин, сбросить CurrentSkinId у пользователя
            var user = await _context.Users.FindAsync(userId);
            if (user != null && user.CurrentSkinId == skin.Id)
            {
                user.CurrentSkinId = null;
                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogDebug("Reset CurrentSkinId for user {UserId} after deleting skin {SkinId}.", userId, skin.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while resetting user's current skin after deletion for user {UserId}.", userId);
                    // Опционально: можно откатить удаление скина из БД
                }
            }

            _context.Skins.Remove(skin);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} successfully deleted skin {SkinId}.", userId, skin.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting skin record from database for user {UserId}, skinId {SkinId}.", userId, skinId);
                return StatusCode(500, "An error occurred while deleting from the database.");
            }

            return NoContent();
        }

        /// <summary>
        /// Удалить плащ по ID.
        /// </summary>
        /// <param name="capeId">ID плаща для удаления.</param>
        /// <returns>Результат операции.</returns>
        [HttpDelete("cape/{capeId}")]
        [SwaggerOperation(
            Summary = "Удалить плащ",
            Description = "Удаляет плащ, принадлежащий пользователю, по его ID. Также удаляет файл с диска."
        )]
        [ProducesResponseType(204)] // No Content при успехе
        [ProducesResponseType(401)]
        [ProducesResponseType(404)] // Not Found при отсутствии плаща или несоответствии пользователю
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteCape(int capeId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var cape = await _context.Capes
                .Where(c => c.Id == capeId && c.UserId == userId)
                .FirstOrDefaultAsync();

            if (cape == null)
            {
                _logger.LogWarning("User {UserId} attempted to delete a non-existent or non-owned cape {CapeId}.", userId, capeId);
                return NotFound("Cape not found or does not belong to the user.");
            }

            // Удалить файл с диска
            var filePath = Path.Combine("wwwroot", "capes", cape.FileName);
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogDebug("Deleted cape file {FilePath} for user {UserId}.", filePath, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting cape file {FilePath} for user {UserId}.", filePath, userId);
                // Важно: файл не удален, но запись в БД будет удалена.
                // В реальности, может потребоваться более сложная логика отката.
            }

            // Если это текущий плащ, сбросить CurrentCapeId у пользователя
            var user = await _context.Users.FindAsync(userId);
            if (user != null && user.CurrentCapeId == cape.Id)
            {
                user.CurrentCapeId = null;
                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogDebug("Reset CurrentCapeId for user {UserId} after deleting cape {CapeId}.", userId, cape.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while resetting user's current cape after deletion for user {UserId}.", userId);
                    // Опционально: можно откатить удаление плаща из БД
                }
            }

            _context.Capes.Remove(cape);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} successfully deleted cape {CapeId}.", userId, cape.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting cape record from database for user {UserId}, capeId {CapeId}.", userId, capeId);
                return StatusCode(500, "An error occurred while deleting from the database.");
            }

            return NoContent();
        }

        /// <summary>
        /// Установить скин по ID как текущий для пользователя.
        /// </summary>
        /// <param name="skinId">ID скина для установки.</param>
        /// <returns>Результат операции.</returns>
        [HttpPut("skin/{skinId}/set-current")]
        [SwaggerOperation(
            Summary = "Установить скин как текущий",
            Description = "Устанавливает скин, принадлежащий пользователю, как текущий (отображаемый в игре)."
        )]
        [ProducesResponseType(204)] // No Content при успехе
        [ProducesResponseType(401)]
        [ProducesResponseType(404)] // Not Found при отсутствии скина или несоответствии пользователю, или отсутствии пользователя
        [ProducesResponseType(500)]
        public async Task<IActionResult> SetCurrentSkin(int skinId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            // Проверить, принадлежит ли скин пользователю
            var skinExists = await _context.Skins
                .AnyAsync(s => s.Id == skinId && s.UserId == userId);

            if (!skinExists)
            {
                _logger.LogWarning("User {UserId} attempted to set non-existent or non-owned skin {SkinId} as current.", userId, skinId);
                return NotFound("Skin not found or does not belong to the user.");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found while trying to set current skin.", userId);
                return NotFound("User not found.");
            }

            user.CurrentSkinId = skinId;
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} set skin {SkinId} as current.", userId, skinId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while setting current skin for user {UserId}.", userId);
                return StatusCode(500, "An error occurred while updating the database.");
            }

            return NoContent();
        }

        /// <summary>
        /// Установить плащ по ID как текущий (активный) для пользователя.
        /// </summary>
        /// <param name="capeId">ID плаща для установки.</param>
        /// <returns>Результат операции.</returns>
        [HttpPut("cape/{capeId}/set-current")]
        [SwaggerOperation(
            Summary = "Установить плащ как текущий",
            Description = "Устанавливает плащ, принадлежащий пользователю, как текущий (активный, отображаемый в игре). Деактивирует предыдущий активный плащ."
        )]
        [ProducesResponseType(204)] // No Content при успехе
        [ProducesResponseType(401)]
        [ProducesResponseType(404)] // Not Found при отсутствии плаща или несоответствии пользователю, или отсутствии пользователя
        [ProducesResponseType(500)]
        public async Task<IActionResult> SetCurrentCape(int capeId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            // Проверить, принадлежит ли плащ пользователю
            var capeExists = await _context.Capes
                .AnyAsync(c => c.Id == capeId && c.UserId == userId);

            if (!capeExists)
            {
                _logger.LogWarning("User {UserId} attempted to set non-existent or non-owned cape {CapeId} as current.", userId, capeId);
                return NotFound("Cape not found or does not belong to the user.");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found while trying to set current cape.", userId);
                return NotFound("User not found.");
            }

            // Сбросить IsActive у текущего активного плаща (если он есть и не совпадает с новым)
            if (user.CurrentCapeId.HasValue && user.CurrentCapeId != capeId)
            {
                var oldCape = await _context.Capes.FindAsync(user.CurrentCapeId.Value);
                if (oldCape != null)
                {
                    oldCape.IsActive = false;
                    _logger.LogDebug("Deactivated previous cape {CapeId} for user {UserId}.", oldCape.Id, userId);
                }
            }

            user.CurrentCapeId = capeId;
            // Установить IsActive для нового плаща
            var newCape = await _context.Capes.FindAsync(capeId);
            if (newCape != null)
            {
                newCape.IsActive = true;
                _logger.LogDebug("Activated new cape {CapeId} for user {UserId}.", newCape.Id, userId);
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} set cape {CapeId} as current and active.", userId, capeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while setting current cape for user {UserId}.", userId);
                return StatusCode(500, "An error occurred while updating the database.");
            }

            return NoContent();
        }

        // Вспомогательный метод для проверки типа файла
        private static bool IsValidImageFile(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return extension == ".png";
            // Для более точной проверки можно читать заголовок файла (magic bytes)
        }

        
    }
}