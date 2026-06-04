using Microsoft.EntityFrameworkCore;
using projektBackend.Models;

namespace projektBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        
        public DbSet<Rezerwacja> Rezerwacje { get; set; }

        
    }
}