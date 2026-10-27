using Microsoft.EntityFrameworkCore; 

namespace Totalforsvaret.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Registration> Registrations { get; set; }
}