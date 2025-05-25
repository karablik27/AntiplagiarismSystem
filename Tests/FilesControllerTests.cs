using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FileStoringService.Controllers;
using FileStoringService.Data;
using FileStoringService.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Tests;

public class FilesControllerTests
{
    private FilesDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<FilesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FilesDbContext(options);
    }

    private class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "FakeApp";
        public string WebRootPath { get; set; } = "/tmp";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider WebRootFileProvider { get; set; } = new PhysicalFileProvider("/tmp");
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Path.GetTempPath());
    }

    private IFormFile CreateFakeFormFile(string content, string fileName = "test.txt")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName);
    }

    [Fact]
    public async Task Store_ReturnsOk_AndSavesFile()
    {
        var db = CreateInMemoryDb();
        var env = new FakeWebHostEnvironment();
        var controller = new FilesController(db, env);
        var file = CreateFakeFormFile("test content");

        var result = await controller.Store(file) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Contains("id", result.Value?.ToString());
        Assert.Equal(1, await db.Files.CountAsync());
    }

    [Fact]
    public async Task Store_ReturnsSameId_ForDuplicate()
    {
        var db = CreateInMemoryDb();
        var env = new FakeWebHostEnvironment();
        var controller = new FilesController(db, env);
        var file1 = CreateFakeFormFile("duplicate content");
        var file2 = CreateFakeFormFile("duplicate content");

        var result1 = await controller.Store(file1) as OkObjectResult;
        var result2 = await controller.Store(file2) as OkObjectResult;

        Assert.Equal(result1?.Value?.ToString(), result2?.Value?.ToString());
        Assert.Equal(1, await db.Files.CountAsync());
    }

    [Fact]
    public async Task GetFile_ReturnsNotFound_WhenFileMissing()
    {
        var db = CreateInMemoryDb();
        var env = new FakeWebHostEnvironment();
        var controller = new FilesController(db, env);

        var result = await controller.GetFile(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetFile_ReturnsFile_WhenExists()
    {
        var db = CreateInMemoryDb();
        var env = new FakeWebHostEnvironment();
        var content = "file content";
        var fileId = Guid.NewGuid();
        var path = Path.Combine(env.ContentRootPath, $"{fileId}.txt");
        await File.WriteAllTextAsync(path, content);

        db.Files.Add(new FileEntry
        {
            Id = fileId,
            Name = "file.txt",
            Hash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(content))),
            Location = path
        });
        await db.SaveChangesAsync();

        var controller = new FilesController(db, env);

        var result = await controller.GetFile(fileId) as FileContentResult;

        Assert.NotNull(result);
        Assert.Equal("application/octet-stream", result.ContentType);
        Assert.Equal("file.txt", result.FileDownloadName);
    }
}
