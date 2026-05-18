using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace server.net.Data;

// Allows `dotnet ef migrations add` to run without a live Oracle connection.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseOracle("Data Source=localhost:1521/orcl;User Id=design;Password=design;")
            .Options;

        return new AppDbContext(options);
    }
}
