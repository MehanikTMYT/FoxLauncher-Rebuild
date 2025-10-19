using FoxLauncher.Modules.AuthModule.Data;
using FoxLauncher.Modules.CabinetModule.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Security.Claims;
using System.Security.Cryptography;

namespace FoxLauncher.Modules.CabinetModule.Controllers
{
    [ApiController]
    [Route("api/cabinet")]
    [Authorize]
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

        // GET /api/cabinet/skins
        [HttpGet("skins")]
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

        // GET /api/cabinet/capes
        [HttpGet("capes")]
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

        // POST /api/cabinet/skin
        [HttpPost("skin")]
        public async Task<ActionResult<SkinDto>> UploadSkin(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is empty.");
            }

            if (!IsValidImageFile(file))
            {
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
                return BadRequest("Skin limit reached.");
            }

            // Генерация уникального имени файла
            var fileName = $"{Guid.NewGuid()}.png"; // Или используйте UserId и счетчик
            var filePath = Path.Combine("wwwroot", "skins", fileName); // Папка для скинов

            // Убедитесь, что папка существует
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            string? hash = null; // Объявляем переменную hash до блока using
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
                // Вычисление хэша сразу после записи
                stream.Seek(0, SeekOrigin.Begin); // Сброс позиции потока на начало
                using var sha256 = SHA256.Create();
                var hashBytes = await sha256.ComputeHashAsync(stream);
                hash = Convert.ToHexString(hashBytes); // Присваиваем значение переменной hash
            } // FileStream автоматически закрывается и сбрасывает буферы

            var skin = new Skin
            {
                UserId = userId,
                FileName = fileName,
                OriginalName = file.FileName,
                Hash = hash, // Теперь hash доступна из внешней области видимости
                Size = file.Length
            };

            _context.Skins.Add(skin);
            await _context.SaveChangesAsync();

            // Установить как текущий, если это первый скин
            var user = await _context.Users.FindAsync(userId);
            if (user != null && user.CurrentSkinId == null)
            {
                user.CurrentSkinId = skin.Id;
                await _context.SaveChangesAsync();
            }

            var skinDto = new SkinDto
            {
                Id = skin.Id,
                FileName = skin.FileName,
                OriginalName = skin.OriginalName,
                UploadDate = skin.UploadDate,
                Hash = skin.Hash,
                Size = (long)skin.Size, 
                IsCurrent = true 
            };

            return CreatedAtAction(nameof(GetSkins), new { id = skin.Id }, skinDto);
        }

        // POST /api/cabinet/cape
        [HttpPost("cape")]
        public async Task<ActionResult<CapeDto>> UploadCape(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is empty.");
            }

            if (!IsValidImageFile(file))
            {
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
                return BadRequest("Cape limit reached.");
            }

            // Генерация уникального имени файла
            var fileName = $"{Guid.NewGuid()}.png"; // Или используйте UserId и счетчик
            var filePath = Path.Combine("wwwroot", "capes", fileName); // Папка для плащей

            // Убедитесь, что папка существует
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            string? hash = null; // Объявляем переменную hash до блока using
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
                // Вычисление хэша сразу после записи
                stream.Seek(0, SeekOrigin.Begin); // Сброс позиции потока на начало
                using var sha256 = SHA256.Create();
                var hashBytes = await sha256.ComputeHashAsync(stream);
                hash = Convert.ToHexString(hashBytes); // Присваиваем значение переменной hash
            } // FileStream автоматически закрывается и сбрасывает буферы

            var cape = new Cape
            {
                UserId = userId,
                FileName = fileName,
                OriginalName = file.FileName,
                Hash = hash, // Теперь hash доступна из внешней области видимости
                Size = file.Length
            };

            _context.Capes.Add(cape);
            await _context.SaveChangesAsync();

            // Установить как текущий (активный), если это первый плащ
            var user = await _context.Users.FindAsync(userId);
            if (user != null && user.CurrentCapeId == null)
            {
                user.CurrentCapeId = cape.Id;
                cape.IsActive = true; // Установить активность
                await _context.SaveChangesAsync();
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
                IsCurrent = true 
            };

            return CreatedAtAction(nameof(GetCapes), new { id = cape.Id }, capeDto);
        }

        // DELETE /api/cabinet/skin/{skinId}
        [HttpDelete("skin/{skinId}")]
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
                return NotFound("Skin not found or does not belong to the user.");
            }

            // Удалить файл с диска
            var filePath = Path.Combine("wwwroot", "skins", skin.FileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // Если это текущий скин, сбросить CurrentSkinId у пользователя
            var user = await _context.Users.FindAsync(userId);
            if (user != null && user.CurrentSkinId == skin.Id)
            {
                user.CurrentSkinId = null;
                await _context.SaveChangesAsync();
            }

            _context.Skins.Remove(skin);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE /api/cabinet/cape/{capeId}
        [HttpDelete("cape/{capeId}")]
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
                return NotFound("Cape not found or does not belong to the user.");
            }

            // Удалить файл с диска
            var filePath = Path.Combine("wwwroot", "capes", cape.FileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // Если это текущий плащ, сбросить CurrentCapeId у пользователя
            var user = await _context.Users.FindAsync(userId);
            if (user != null && user.CurrentCapeId == cape.Id)
            {
                user.CurrentCapeId = null;
                await _context.SaveChangesAsync();
            }

            _context.Capes.Remove(cape);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT /api/cabinet/skin/{skinId}/set-current
        [HttpPut("skin/{skinId}/set-current")]
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
                return NotFound("Skin not found or does not belong to the user.");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            user.CurrentSkinId = skinId;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT /api/cabinet/cape/{capeId}/set-current
        [HttpPut("cape/{capeId}/set-current")]
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
                return NotFound("Cape not found or does not belong to the user.");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Сбросить IsActive у текущего активного плаща (если он есть и не совпадает с новым)
            if (user.CurrentCapeId.HasValue && user.CurrentCapeId != capeId)
            {
                var oldCape = await _context.Capes.FindAsync(user.CurrentCapeId.Value);
                if (oldCape != null)
                {
                    oldCape.IsActive = false;
                }
            }

            user.CurrentCapeId = capeId;
            // Установить IsActive для нового плаща
            var newCape = await _context.Capes.FindAsync(capeId);
            if (newCape != null)
            {
                newCape.IsActive = true;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Вспомогательный метод для проверки типа файла
        private static bool IsValidImageFile(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return extension == ".png";
            // Для более точной проверки можно читать заголовок файла (magic bytes)
        }

        // DTO для передачи данных
        public class SkinDto
        {
            public int Id { get; set; }
            public string FileName { get; set; } = string.Empty;
            public string OriginalName { get; set; } = string.Empty;
            public DateTime UploadDate { get; set; }
            public string? Hash { get; set; }
            public long Size { get; set; }
            public bool IsCurrent { get; set; }
        }

        public class CapeDto
        {
            public int Id { get; set; }
            public string FileName { get; set; } = string.Empty;
            public string OriginalName { get; set; } = string.Empty;
            public DateTime UploadDate { get; set; }
            public string? Hash { get; set; }
            public long Size { get; set; }
            public bool IsActive { get; set; }
            public bool IsCurrent { get; set; }
        }
    }
}