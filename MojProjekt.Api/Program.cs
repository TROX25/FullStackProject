using MojProjekt.Application.DependencyInjection;
using MojProjekt.Infrastructure.DependencyInjection;
using MojProjekt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// [] Albo proxy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Ensure the SQLite data directory + schema exist before serving requests; always run (not just in
// Development) so a fresh local checkout works with zero manual DB setup steps.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var dataDirectory = Path.Combine(app.Environment.ContentRootPath, "App_Data");
    Directory.CreateDirectory(dataDirectory);
    dbContext.Database.Migrate();

    // WAL lets concurrent readers (GET /api/listings, /api/search) proceed while a crawl's upsert
    // transaction is writing, instead of blocking behind SQLite's default rollback-journal locking.
    dbContext.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// [] Wlaczenie policy w ciągu middleware
app.UseCors("AllowAngular");

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory<Program> in MojProjekt.Api.IntegrationTests.
public partial class Program;