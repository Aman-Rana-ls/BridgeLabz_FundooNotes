using Microsoft.EntityFrameworkCore;
using ModelLayer.Models;

namespace RepoLayer.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<ApplicationUser> Users { get; set; }
    }
}
