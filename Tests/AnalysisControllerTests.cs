using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using FileAnalysisService.Controllers;
using FileAnalysisService.Services;
using FileAnalysisService.Models;

namespace Tests
{
    public class AnalysisControllerTests
    {
        private class FakeAnalysisService : AnalysisService
        {
            private readonly AnalysisResult _result;
            private readonly byte[] _png;

            public FakeAnalysisService(AnalysisResult result, byte[] png)
                : base(null!, null!) // не используется
            {
                _result = result;
                _png = png;
            }

            public override Task<AnalysisResult> AnalyzeAsync(Guid fileId)
            {
                if (fileId == Guid.Empty)
                    throw new InvalidOperationException("Invalid file ID");
                return Task.FromResult(_result);
            }

            public override Task<(byte[] Content, string FileName)> EnsureWordCloudAsync(Guid fileId)
            {
                if (fileId == Guid.Empty)
                    throw new InvalidOperationException("Analysis not run");
                return Task.FromResult((_png, "cloud.png"));
            }
        }

        private class FailingService : AnalysisService
        {
            public FailingService() : base(null!, null!) { }

            public override Task<AnalysisResult> AnalyzeAsync(Guid _) =>
                throw new Exception("Boom");

            public override Task<(byte[] Content, string FileName)> EnsureWordCloudAsync(Guid _) =>
                throw new Exception("Boom");
        }

        [Fact]
        public async Task Start_ReturnsOk_WithAnalysis()
        {
            var id = Guid.NewGuid();
            var expected = new AnalysisResult
            {
                FileId = id,
                FileHash = "abc",
                Paragraphs = 2,
                Words = 4,
                Characters = 20
            };

            var controller = new AnalysisController(new FakeAnalysisService(expected, new byte[0]));
            var response = await controller.Start(id);

            var ok = Assert.IsType<OkObjectResult>(response);
            var result = Assert.IsType<AnalysisResult>(ok.Value);
            Assert.Equal(2, result.Paragraphs);
        }

        [Fact]
        public async Task Get_ReturnsCachedResult()
        {
            var id = Guid.NewGuid();
            var expected = new AnalysisResult
            {
                FileId = id,
                FileHash = "hash",
                Paragraphs = 1,
                Words = 2,
                Characters = 10
            };

            var controller = new AnalysisController(new FakeAnalysisService(expected, new byte[0]));
            var response = await controller.Get(id);

            var ok = Assert.IsType<OkObjectResult>(response);
            var result = Assert.IsType<AnalysisResult>(ok.Value);
            Assert.Equal("hash", result.FileHash);
        }

        [Fact]
        public async Task Cloud_ReturnsPngFile()
        {
            var id = Guid.NewGuid();
            var png = new byte[] { 1, 2, 3 };

            var controller = new AnalysisController(new FakeAnalysisService(new AnalysisResult { FileId = id }, png));
            var response = await controller.Cloud(id);

            var file = Assert.IsType<FileContentResult>(response);
            Assert.Equal("image/png", file.ContentType);
            Assert.Equal(png, file.FileContents);
        }

        [Fact]
        public async Task Start_Returns500_OnException()
        {
            var controller = new AnalysisController(new FailingService());
            var response = await controller.Start(Guid.Empty);

            var result = Assert.IsType<ObjectResult>(response);
            Assert.Equal(500, result.StatusCode);
        }
    }
}
