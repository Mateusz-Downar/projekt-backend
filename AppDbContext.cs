using Microsoft.EntityFrameworkCore;
using MojeApi.Models;

namespace MojeApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Produkt> Produkty { get; set; }
    }
}