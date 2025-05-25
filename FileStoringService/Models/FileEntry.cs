using System;
namespace FileStoringService.Models
{
    public class FileEntry
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Hash { get; set; }
        public string Location { get; set; }
    }
}


