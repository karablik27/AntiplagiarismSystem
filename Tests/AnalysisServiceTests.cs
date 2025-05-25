using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FileAnalysisService.Services;
using FileAnalysisService.Data;
using FileAnalysisService.Models;

namespace Tests
{
    public class AnalysisServiceTests
    {
        private AnalysisDbContext CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<AnalysisDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AnalysisDbContext(options);
        }

        private IHttpClientFactory CreateFakeFactory(string content, byte[]? binary = null)
        {
            var handler = new FakeHandler(content, binary);
            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://fake")
            };
            return new SingleClientFactory(client);
        }

        [Fact]
        public async Task AnalyzeAsync_CalculatesStatisticsCorrectly()
        {
            string text = "Hello world.\n\nThis is a test.";
            var db = CreateInMemoryDb();
            var factory = CreateFakeFactory(text);
            var service = new AnalysisService(factory, db);
            var fileId = Guid.NewGuid();

            var result = await service.AnalyzeAsync(fileId);

            Assert.Equal(2, result.Paragraphs);
            Assert.Equal(6, result.Words);
            Assert.Equal(text.Length, result.Characters);
        }

        [Fact]
        public async Task AnalyzeAsync_ReturnsCached_WhenCalledAgain()
        {
            string text = "Cached file text.";
            var db = CreateInMemoryDb();
            var factory = CreateFakeFactory(text);
            var service = new AnalysisService(factory, db);
            var fileId = Guid.NewGuid();

            var first = await service.AnalyzeAsync(fileId);
            var second = await service.AnalyzeAsync(fileId);

            Assert.Equal(first.FileHash, second.FileHash);
            Assert.Equal(first.Characters, second.Characters);
        }

        [Fact]
        public async Task AnalyzeAsync_Throws_WhenDuplicateHashFound()
        {
            string text = "Same hash text.";
            var db = CreateInMemoryDb();
            var factory = CreateFakeFactory(text);
            var service = new AnalysisService(factory, db);

            var firstId = Guid.NewGuid();
            var secondId = Guid.NewGuid();
            await service.AnalyzeAsync(firstId);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AnalyzeAsync(secondId));
            Assert.Contains("100 % совпадение", ex.Message);
        }
    }

    public class FakeHandler : HttpMessageHandler
    {
        private readonly string _textResponse;
        private readonly byte[]? _binaryResponse;

        public FakeHandler(string text, byte[]? binary = null)
        {
            _textResponse = text;
            _binaryResponse = binary ?? Encoding.UTF8.GetBytes(text);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = new ByteArrayContent(_binaryResponse);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }

    public class SingleClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public SingleClientFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }
}
