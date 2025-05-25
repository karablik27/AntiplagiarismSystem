using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FileStoringService.Data;
using FileStoringService.Models;

namespace FileStoringService.Controllers
{
    [ApiController]
    [Route("files")]
    public class FilesController : ControllerBase
    {
        private readonly FilesDbContext _db;
        private readonly IWebHostEnvironment _env;

        public FilesController(FilesDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // POST /files/store
        [HttpPost("store")]
        public async Task<IActionResult> Store(IFormFile file)
        {
            if (file == null)
            {
                Console.WriteLine("[ERROR] Файл не передан в Store");
                return BadRequest("file is null");
            }

            // 1) Посчитать MD5-хеш
            using var md5 = MD5.Create();
            using var stream = file.OpenReadStream();
            var hash = Convert.ToHexString(md5.ComputeHash(stream));

            // 2) Проверить, не заливали ли уже такой файл
            var existing = await _db.Files
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Hash == hash);
            if (existing != null)
                return Ok(new { id = existing.Id });

            // 3) Сохранить на диск
            var id = Guid.NewGuid();
            var folder = Path.Combine(_env.ContentRootPath, "StoredFiles");
            Directory.CreateDirectory(folder);
            var ext = Path.GetExtension(file.FileName);
            var savePath = Path.Combine(folder, $"{id}{ext}");
            using (var outFs = System.IO.File.Create(savePath))
                await file.CopyToAsync(outFs);

            // 4) Записать метаданные в БД
            var entry = new FileEntry
            {
                Id = id,
                Name = file.FileName,
                Hash = hash,
                Location = savePath
            };
            _db.Files.Add(entry);
            await _db.SaveChangesAsync();

            return Ok(new { id });
        }

        // GET /files/file/{id}
        [HttpGet("file/{id:guid}")]
        public async Task<IActionResult> GetFile(Guid id)
        {
            var entry = await _db.Files.FindAsync(id);
            if (entry == null)
                return NotFound();

            var bytes = await System.IO.File.ReadAllBytesAsync(entry.Location);
            return File(bytes, "application/octet-stream", entry.Name);
        }
    }
}
