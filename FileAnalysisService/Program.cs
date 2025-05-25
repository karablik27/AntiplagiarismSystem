using FileAnalysisService.Data;
using FileAnalysisService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1) Подключаем EF Core + PostgreSQL
builder.Services.AddDbContext<AnalysisDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// 2) Получаем адреса downstream-сервисов
var fsBase = builder.Configuration["FileStoring__BaseUrl"] // <-- двойное подчёркивание
          ?? builder.Configuration["FileStoring:BaseUrl"]   // <-- поддержка JSON-формата
          ?? throw new InvalidOperationException("Missing config: FileStoring BaseUrl");

var wcBase = builder.Configuration["WordCloud__BaseUrl"]
          ?? builder.Configuration["WordCloud:BaseUrl"]
          ?? "https://quickchart.io";

// 3) HTTP-клиенты
builder.Services.AddHttpClient("FS", c =>
    c.BaseAddress = new Uri(fsBase));

builder.Services.AddHttpClient("WC", c =>
    c.BaseAddress = new Uri(wcBase));

// 4) Регистрация зависимостей
builder.Services.AddScoped<AnalysisService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 5) Маршруты и Swagger
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGet("/health", () => Results.Ok()).WithTags("Health");

// 6) Миграции БД при запуске
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();
    db.Database.Migrate();
}

app.Run();
