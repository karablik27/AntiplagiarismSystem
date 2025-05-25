using FileStoringService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1) EF Core + Postgres
builder.Services.AddDbContext<FilesDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// 2) MVC и Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3) Health check
builder.Services.AddHealthChecks();

var app = builder.Build();

// 4) Swagger UI
app.UseSwagger();
app.UseSwaggerUI();

// 5) Маршруты
app.MapControllers();

// 6) Health endpoint
app.MapHealthChecks("/health");

// 7) Авто-миграции
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FilesDbContext>();
    db.Database.Migrate();
}

app.Run();
