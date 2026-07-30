using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MojProjekt.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add/update` build an AppDbContext directly, without spinning up the
/// full Api host (and therefore without needing every application service registered) at design time.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite("Data Source=App_Data/mojprojekt.db");
        return new AppDbContext(optionsBuilder.Options);
    }
}
