using FileStoringService.Models;
using Microsoft.EntityFrameworkCore;

namespace FileStoringService.Data
{
    public class FilesDbContext : DbContext
    {
        public FilesDbContext(DbContextOptions<FilesDbContext> options)
            : base(options)
        {
        }

        public DbSet<FileEntry> Files { get; set; }
    }
}
