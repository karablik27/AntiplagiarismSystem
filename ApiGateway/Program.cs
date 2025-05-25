using System.Net.Http.Headers;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ───── 1. HTTP-клиенты ─────
builder.Services.AddHttpClient("FS", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Downstreams:FileStoring"]!);
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddHttpClient("FA", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Downstreams:FileAnalysis"]!);
    c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

// ───── 2. Swagger Gateway ─────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("gateway", new OpenApiInfo { Title = "API Gateway", Version = "v1" });
});

var app = builder.Build();

// ───── 3. Проксируем Swagger JSON ДО UseSwagger ─────
app.MapWhen(ctx => ctx.Request.Path == "/swagger/file-storing/swagger.json", appBuilder =>
{
    appBuilder.Run(async ctx =>
    {
        Console.WriteLine("▶️ Swagger JSON file-storing");
        await ctx.Proxy("FS", "/swagger/v1/swagger.json");
    });
});

app.MapWhen(ctx => ctx.Request.Path == "/swagger/file-analysis/swagger.json", appBuilder =>
{
    appBuilder.Run(async ctx =>
    {
        Console.WriteLine("▶️ Swagger JSON file-analysis");
        await ctx.Proxy("FA", "/swagger/v1/swagger.json");
    });
});

// ───── 4. Swagger UI ─────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/gateway/swagger.json", "Gateway v1");
    c.SwaggerEndpoint("/swagger/file-storing/swagger.json", "File Storing Service");
    c.SwaggerEndpoint("/swagger/file-analysis/swagger.json", "File Analysis Service");
});

// ───── 5. Business endpoints — File Storing ─────
app.MapMethods("/files/store", new[] { "POST" },
    ctx => ctx.Proxy("FS", "/files/store"))
   .WithOpenApi(op => { op.Summary = "Загрузить файл"; return op; });

app.MapMethods("/files/file/{id:guid}", new[] { "GET" },
    ctx => ctx.Proxy("FS", $"/files/file/{ctx.GetRouteValue("id")}"))
   .WithOpenApi(op => { op.Summary = "Получить файл по id"; return op; });

// ───── 6. Business endpoints — File Analysis (с /files/analysis!) ─────
app.MapMethods("/files/analysis/{fileId:guid}/start", new[] { "POST" },
    ctx => ctx.Proxy("FA", $"/files/analysis/{ctx.GetRouteValue("fileId")}/start"))
   .WithOpenApi(op => { op.Summary = "Проанализировать файл"; return op; });

app.MapMethods("/files/analysis/{fileId:guid}", new[] { "GET" },
    ctx => ctx.Proxy("FA", $"/files/analysis/{ctx.GetRouteValue("fileId")}"))
   .WithOpenApi(op => { op.Summary = "Получить результат анализа"; return op; });

app.MapMethods("/files/analysis/{fileId:guid}/wordcloud", new[] { "GET" },
    ctx => ctx.Proxy("FA", $"/files/analysis/{ctx.GetRouteValue("fileId")}/wordcloud"))
   .WithOpenApi(op => { op.Summary = "Получить облако слов"; return op; });

app.MapGet("/health", () => Results.Ok("Gateway is healthy"));

app.Run();

// ───── 7. Прокси-хелпер ─────
static class ProxyExtensions
{
    private static readonly HashSet<string> HopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "connection", "keep-alive", "proxy-authenticate", "proxy-authorization",
        "te", "trailer", "transfer-encoding", "upgrade", "content-length"
    };

    public static async Task Proxy(this HttpContext ctx, string clientName, string path)
    {
        var client = ctx.RequestServices.GetRequiredService<IHttpClientFactory>()
                          .CreateClient(clientName);

        var req = new HttpRequestMessage(new HttpMethod(ctx.Request.Method), path);

        if (ctx.Request.ContentLength > 0 || ctx.Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            req.Content = new StreamContent(ctx.Request.Body);
            if (ctx.Request.ContentType != null)
                req.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(ctx.Request.ContentType);
        }

        foreach (var (k, v) in ctx.Request.Headers)
            if (!HopHeaders.Contains(k))
                req.Headers.TryAddWithoutValidation(k, v.ToArray());

        HttpResponseMessage resp;
        try
        {
            resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ctx.RequestAborted);
        }
        catch
        {
            ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await ctx.Response.WriteAsync($"Downstream '{clientName}' недоступен");
            return;
        }

        ctx.Response.StatusCode = (int)resp.StatusCode;
        foreach (var (k, v) in resp.Headers)
            if (!HopHeaders.Contains(k))
                ctx.Response.Headers[k] = v.ToArray();
        foreach (var (k, v) in resp.Content.Headers)
            if (!HopHeaders.Contains(k))
                ctx.Response.Headers[k] = v.ToArray();

        await resp.Content.CopyToAsync(ctx.Response.Body);
    }
}
